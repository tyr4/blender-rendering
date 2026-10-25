using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class ClipPackageExporter
{
    public static void Export(IEnumerable<AnimationClip> clips, List<JToken> spritesheetTokens, bool skipClips)
    {
        var paths = new HashSet<string>();
        var outputPath = ToPackagePath(AssetDatabase.GetAssetPath(clips.First()));
        var sprites = LoadSpritesheets(spritesheetTokens);

        if (!skipClips)
        {
            foreach (var clip in clips.Where(c => c != null))
            {
                var p = AssetDatabase.GetAssetPath(clip);
                if (!string.IsNullOrEmpty(p)) paths.Add(p);
            }    
        }

        // sprites are sub-assets of their texture, so export the texture file
        foreach (var sprite in sprites.Where(s => s != null))
        {
            var p = AssetDatabase.GetAssetPath(sprite.texture);
            if (!string.IsNullOrEmpty(p)) paths.Add(p);
        }

        if (paths.Count == 0)
        {
            Debug.LogError("Nothing to export: no valid asset paths found.");
            return;
        }

        AssetDatabase.ExportPackage(
            paths.ToArray(),
            outputPath,
            ExportPackageOptions.IncludeDependencies);

        ConsoleLog.ProcessLog($"Exported {paths.Count} assets to {outputPath}");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string ToPackagePath(string clipAssetPath)
    {
        // "Armature_Armature_IDLE_0" (no folder, no extension)
        string name = Path.GetFileNameWithoutExtension(clipAssetPath);

        // drop the trailing "_0" (any number): "Armature_Armature_IDLE"
        name = Regex.Replace(name, @"_\d+$", "");

        // write outside Assets so Unity doesn't import the package
        string exportDir = Path.Combine(Application.dataPath, "Exports");
        Directory.CreateDirectory(exportDir);

        return Path.Combine(exportDir, name + ".unitypackage");
    }
    
    private static List<Sprite> LoadSpritesheets(List<JToken> initial)
    {
        List<Sprite> spritesheets = new();
        
        foreach (var token in initial)
        {
            var spritesheetPath = token["output_path"]!.ToObject<string>();
            var sanitizedPath = AnimationClipGenerator.ToUnityRelativePath(spritesheetPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sanitizedPath);
            
            spritesheets.Add(sprite);
        }

        return spritesheets;
    }
}