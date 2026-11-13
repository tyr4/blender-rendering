using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class UnityPackageMerger : EditorWindow
{
    private enum ConflictMode { LastWins, FirstWins }

    [SerializeField] private List<string> _packages = new();

    private SerializedObject _so;
    private Vector2 _scroll;
    private ConflictMode _conflictMode = ConflictMode.LastWins;
    private string _outputName = "Merged";

    [MenuItem("Tools/UnityPackage Merger")]
    private static void Open() => GetWindow<UnityPackageMerger>("Package Merger");

    private void OnEnable() => _so = new SerializedObject(this);

    private void OnGUI()
    {
        _so.Update();

        DrawDropZone();

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(260));
        EditorGUILayout.PropertyField(_so.FindProperty("_packages"), new GUIContent("Packages (merge order)"), true);
        EditorGUILayout.EndScrollView();
        _so.ApplyModifiedProperties();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add file..."))
            {
                string p = EditorUtility.OpenFilePanel("Select .unitypackage", "", "unitypackage");
                if (!string.IsNullOrEmpty(p) && !_packages.Contains(p)) _packages.Add(p);
            }
            if (GUILayout.Button("Sort by name"))
                _packages = _packages.OrderBy(p => Path.GetFileName(p),
                    Comparer<string>.Create(EditorUtility.NaturalCompare)).ToList();
            if (GUILayout.Button("Clear")) _packages.Clear();
        }

        int n = _packages.Count(p => !string.IsNullOrEmpty(p));
        EditorGUILayout.HelpBox(n == 0 ? "Add .unitypackage files to merge." : $"{n} packages will be merged.",
            MessageType.None);

        EditorGUILayout.Space();
        _conflictMode = (ConflictMode)EditorGUILayout.EnumPopup("Same GUID in several packages", _conflictMode);

        _outputName = EditorGUILayout.TextField("Output name", _outputName);
        string outputPath = GetOutputPath();
        if (outputPath != null)
            EditorGUILayout.HelpBox($"Will be saved to:\n{outputPath}", MessageType.None);

        EditorGUILayout.Space();
        bool ready = n > 0 && !string.IsNullOrWhiteSpace(_outputName);
        using (new EditorGUI.DisabledScope(!ready))
            if (GUILayout.Button("Merge Packages", GUILayout.Height(30)))
                Merge();
    }

    private void DrawDropZone()
    {
        var rect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "Drop .unitypackage files here");

        var e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            var dropped = DragAndDrop.paths
                .Where(p => p.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFullPath)
                .ToList();
            // natural name order within one drop, appended after existing entries
            dropped.Sort((a, b) => EditorUtility.NaturalCompare(Path.GetFileName(a), Path.GetFileName(b)));
            foreach (var p in dropped)
                if (!_packages.Contains(p)) _packages.Add(p);
            e.Use();
        }
    }

    // ───────────────────────── merge ─────────────────────────

    private void Merge()
    {
        var packages = _packages.Where(p => !string.IsNullOrEmpty(p)).ToList();
        string output = Path.GetFullPath(GetOutputPath());
        
        if (packages.Any(p => Path.GetFullPath(p) == output))
        {
            EditorUtility.DisplayDialog("Package Merger", "The output file can't also be an input.", "OK");
            return;
        }

        string temp = Path.Combine(Path.GetTempPath(), "UnityPackageMerge_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        try
        {
            var previousGuids = new HashSet<string>();
            int overridden = 0;

            for (int i = 0; i < packages.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Merging packages",
                    $"Reading {Path.GetFileName(packages[i])} ({i + 1}/{packages.Count})",
                    (float)i / packages.Count);

                var currentGuids = new HashSet<string>();
                ExtractPackage(packages[i], temp, _conflictMode, previousGuids, currentGuids);

                overridden += currentGuids.Count(previousGuids.Contains);
                previousGuids.UnionWith(currentGuids);
            }

            EditorUtility.DisplayProgressBar("Merging packages", "Writing output...", 0.95f);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            WriteTarGz(temp, output);

            WarnAboutPathCollisions(temp);

            AssetDatabase.Refresh();
            Debug.Log($"Merged {packages.Count} packages into {output} " +
                      $"({previousGuids.Count} assets, {overridden} GUID conflicts resolved by {_conflictMode}).");
            EditorUtility.RevealInFinder(output);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            try { Directory.Delete(temp, true); } catch { /* temp cleanup is best-effort */ }
        }
    }

    // Two different GUIDs pointing at the same asset path would clash on import.
    private static void WarnAboutPathCollisions(string root)
    {
        var byPath = new Dictionary<string, List<string>>();
        foreach (var dir in Directory.GetDirectories(root))
        {
            string pf = Path.Combine(dir, "pathname");
            if (!File.Exists(pf)) continue;
            string path = File.ReadAllText(pf).Split('\n')[0].Trim();
            if (!byPath.TryGetValue(path, out var list)) byPath[path] = list = new List<string>();
            list.Add(Path.GetFileName(dir));
        }
        foreach (var kv in byPath.Where(kv => kv.Value.Count > 1))
            Debug.LogWarning($"Path collision: '{kv.Key}' is used by {kv.Value.Count} different GUIDs.");
    }

    // ───────────────────────── tar.gz reading ─────────────────────────

    private static void ExtractPackage(string packagePath, string tempRoot, ConflictMode mode,
                                       HashSet<string> previousGuids, HashSet<string> currentGuids)
    {
        using var fs = File.OpenRead(packagePath);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);

        var header = new byte[512];
        var buffer = new byte[81920];
        string longName = null;

        while (ReadFully(gz, header, 512) == 512)
        {
            if (header.All(b => b == 0)) break; // end-of-archive

            string name = ReadString(header, 0, 100);
            if (ReadString(header, 257, 5) == "ustar")
            {
                string prefix = ReadString(header, 345, 155);
                if (prefix.Length > 0) name = prefix + "/" + name;
            }
            if (longName != null) { name = longName; longName = null; }

            long size = ParseOctal(header, 124, 12);
            char type = (char)header[156];
            long padded = (size + 511) / 512 * 512;

            if (type == 'L') // GNU long file name
            {
                var data = new byte[size];
                ReadFully(gz, data, (int)size);
                Skip(gz, padded - size, buffer);
                longName = Encoding.UTF8.GetString(data).TrimEnd('\0');
                continue;
            }

            if (type != '0' && type != '\0') { Skip(gz, padded, buffer); continue; }

            name = name.Replace('\\', '/');
            if (name.StartsWith("./")) name = name.Substring(2);
            var parts = name.Split('/');
            bool valid = parts.Length == 2 && parts.All(p => p.Length > 0 && p != "." && p != "..");
            if (!valid) { Skip(gz, padded, buffer); continue; }

            string guid = parts[0];
            if (mode == ConflictMode.FirstWins && previousGuids.Contains(guid))
            {
                Skip(gz, padded, buffer);
                continue;
            }

            currentGuids.Add(guid);
            string dir = Path.Combine(tempRoot, guid);
            Directory.CreateDirectory(dir);

            using (var outFile = File.Create(Path.Combine(dir, parts[1])))
            {
                long remaining = size;
                while (remaining > 0)
                {
                    int read = gz.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read <= 0) throw new EndOfStreamException($"Unexpected end of {packagePath}");
                    outFile.Write(buffer, 0, read);
                    remaining -= read;
                }
            }
            Skip(gz, padded - size, buffer);
        }
    }

    private static int ReadFully(Stream s, byte[] buf, int count)
    {
        int total = 0;
        while (total < count)
        {
            int r = s.Read(buf, total, count - total);
            if (r <= 0) break;
            total += r;
        }
        return total;
    }

    private static void Skip(Stream s, long count, byte[] buffer)
    {
        while (count > 0)
        {
            int r = s.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
            if (r <= 0) break;
            count -= r;
        }
    }

    private static string ReadString(byte[] buf, int offset, int length)
    {
        int end = offset;
        while (end < offset + length && buf[end] != 0) end++;
        return Encoding.UTF8.GetString(buf, offset, end - offset);
    }

    private static long ParseOctal(byte[] buf, int offset, int length)
    {
        string s = ReadString(buf, offset, length).Trim();
        return s.Length == 0 ? 0 : Convert.ToInt64(s, 8);
    }

    // ───────────────────────── tar.gz writing ─────────────────────────
    
    // saved next to the first package in the list
    private string GetOutputPath()
    {
        var first = _packages.FirstOrDefault(p => !string.IsNullOrEmpty(p));
        if (first == null) return null;

        string name = Path.GetFileName(string.IsNullOrWhiteSpace(_outputName) ? "Merged" : _outputName.Trim());
        if (!name.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            name += ".unitypackage";

        return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(first)), name);
    }
    
    private static void WriteTarGz(string sourceRoot, string outputPath)
    {
        using var fs = File.Create(outputPath);
        using var gz = new GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal);

        foreach (var dir in Directory.GetDirectories(sourceRoot))
        {
            string guid = Path.GetFileName(dir);
            foreach (var file in Directory.GetFiles(dir))
                WriteEntry(gz, guid + "/" + Path.GetFileName(file), file);
        }

        gz.Write(new byte[1024], 0, 1024); // two empty blocks = end of archive
    }

    private static void WriteEntry(Stream tar, string name, string filePath)
    {
        long size = new FileInfo(filePath).Length;
        var h = new byte[512];

        Encoding.UTF8.GetBytes(name).CopyTo(h, 0);
        WriteOctal(h, 100, 8, 420);   // mode 0644
        WriteOctal(h, 108, 8, 0);     // uid
        WriteOctal(h, 116, 8, 0);     // gid
        WriteOctal(h, 124, 12, size);
        WriteOctal(h, 136, 12, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        for (int i = 148; i < 156; i++) h[i] = (byte)' '; // checksum placeholder
        h[156] = (byte)'0';
        Encoding.ASCII.GetBytes("ustar\0").CopyTo(h, 257);
        Encoding.ASCII.GetBytes("00").CopyTo(h, 263);

        long sum = h.Sum(b => (long)b);
        Encoding.ASCII.GetBytes(Convert.ToString(sum, 8).PadLeft(6, '0')).CopyTo(h, 148);
        h[154] = 0;
        h[155] = (byte)' ';

        tar.Write(h, 0, 512);

        using (var src = File.OpenRead(filePath))
            src.CopyTo(tar);

        int pad = (int)((512 - size % 512) % 512);
        if (pad > 0) tar.Write(new byte[pad], 0, pad);
    }

    private static void WriteOctal(byte[] buf, int offset, int length, long value)
    {
        string s = Convert.ToString(value, 8).PadLeft(length - 1, '0');
        Encoding.ASCII.GetBytes(s).CopyTo(buf, offset);
        buf[offset + length - 1] = 0;
    }
}