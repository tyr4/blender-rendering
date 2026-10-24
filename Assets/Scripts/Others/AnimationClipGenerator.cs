#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class AnimationClipGenerator
{
    public static string ToUnityRelativePath(string absolutePath)
    {
        string normalizedAbsolute = absolutePath.Replace('\\', '/');
        string dataPath = Application.dataPath; // already forward-slash

        if (normalizedAbsolute.StartsWith(dataPath))
        {
            return "Assets" + normalizedAbsolute.Substring(dataPath.Length);
        }

        Debug.LogError($"Path {absolutePath} is outside the project's Assets folder");
        return null;
    }
    
    private static Sprite[] SliceSpritesheet(string assetPath, int columns, int rows, int frameCount)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            // try to refresh the project files first
            ConsoleLog.ProcessLog($"No texture importer found at {assetPath}, refreshing Asset Database...");
            AssetDatabase.Refresh();
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            // now it would actually not exist
            if (importer == null)
            {
                Debug.LogError($"No texture importer found at {assetPath}");
                return null;
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        
        int frameWidth = texture.width / columns;
        int frameHeight = texture.height / rows;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        
        var spriteRects = new List<SpriteRect>();
        var names = new List<string>();
        int index = 0;

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < columns; col++)
            {
                if (index >= frameCount) break;
                string frameName = $"frame_{index}";
                spriteRects.Add(new SpriteRect
                {
                    name = frameName,
                    rect = new Rect(col * frameWidth, row * frameHeight, frameWidth, frameHeight),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center
                });
                names.Add(frameName);
                index++;
            }
            if (index >= frameCount) break;
        }
        
        dataProvider.SetSpriteRects(spriteRects.ToArray());

        // names need to be pushed through the NameFileIdDataProvider so imported sprites get stable IDs
        var nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var nameFileIdPairs = new List<SpriteNameFileIdPair>();
        foreach (var rect in spriteRects)
        {
            nameFileIdPairs.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));
        }
        nameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);

        dataProvider.Apply();
        importer.SaveAndReimport();

        // pull the actual generated Sprite sub-assets back out, in the same order
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sprites = new Sprite[names.Count];
        foreach (var asset in allAssets)
        {
            if (asset is Sprite sprite)
            {
                int frameIndex = int.Parse(sprite.name.Replace("frame_", ""));
                sprites[frameIndex] = sprite;
            }
        }

        return sprites;
    }

    private static AnimationClip CreateClipFromSprites(Sprite[] sprites, string savePath, float frameRate = 24f)
    {
        var clip = new AnimationClip { frameRate = frameRate};
        clip.wrapMode = WrapMode.Loop;
        
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        
        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        
        AssetDatabase.CreateAsset(clip, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        return clip;
    }

    public static AnimationClip CreateAnimationClip(string spritesheetAbsolutePath, float frameRate, int columns, int rows, int frameCount)
    {
        string assetPath = ToUnityRelativePath(spritesheetAbsolutePath);
        if (assetPath == null)
        {
            Debug.LogError($"Couldn't resolve project-relative path for {spritesheetAbsolutePath}");
            return null;
        }

        var sprites = SliceSpritesheet(assetPath, columns, rows, frameCount);

        if (sprites == null)
        {
            Debug.LogError($"Couldn't slice spritesheet at {assetPath}");
            return null;
        }

        string directory = System.IO.Path.GetDirectoryName(assetPath);
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        string savePath = System.IO.Path.Combine(directory, fileName + ".anim").Replace('\\', '/');

        var clip = CreateClipFromSprites(sprites, savePath, frameRate);
        
        return clip;
    }

    public static AnimationClip RebuildClipSkippingKeyframes(AnimationClip source, List<bool> skipFlags)
    {
        if (source == null)
        {
            Debug.LogError("source clip is null");
            return null;
        }
        
        var sourcePath = AssetDatabase.GetAssetPath(source);
        var newClip = new AnimationClip { frameRate = source.frameRate };
        var settings = AnimationUtility.GetAnimationClipSettings(source);
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        // object reference curves (sprites, materials etc)
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
            // ValidateLength(keys.Length, skipFlags, binding.propertyName);
            
            var kept = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < keys.Length; i++)
            {
                if (i < skipFlags.Count && skipFlags[i]) continue;
                
                kept.Add(keys[i]);
            }

            Retime(kept, source.frameRate);
            
            AnimationUtility.SetObjectReferenceCurve(newClip, binding, kept.ToArray());
        }
        
        // float reference curves (position, rotation etc)
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            var keys = curve.keys;
            // ValidateLength(keys.Length, skipFlags, binding.propertyName);

            var kept = new List<Keyframe>();
            for (int i = 0; i < keys.Length; i++)
            {
                if (i < skipFlags.Count && skipFlags[i]) continue;
                kept.Add(keys[i]);
            }
            RetimeFloatKeys(kept, source.frameRate);

            var newCurve = new AnimationCurve(kept.ToArray());
            AnimationUtility.SetEditorCurve(newClip, binding, newCurve);
        }
        
        AnimationUtility.SetAnimationEvents(newClip, AnimationUtility.GetAnimationEvents(source));

        var savePath = sourcePath.Replace(".anim", "_skipped.anim");
        AssetDatabase.CreateAsset(newClip, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return newClip;
    }
    //
    // private static void ValidateLength(int keyCount, List<bool> skipFlags, string propertyName)
    // {
    //     if (keyCount != skipFlags.Count)
    //     {
    //         Debug.LogWarning(
    //             $"RebuildClipSkippingKeyframes: skipFlags length ({skipFlags.Count}) " +
    //             $"doesn't match keyframe count ({keyCount}) for '{propertyName}'. " +
    //             "Indices past the shorter list will be kept as-is.");
    //     }
    // }
    //
    private static void Retime(List<ObjectReferenceKeyframe> keys, float frameRate)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            var k = keys[i];
            k.time = i / frameRate;
            keys[i] = k;
        }
    }

    private static void RetimeFloatKeys(List<Keyframe> keys, float frameRate)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            var k = keys[i];
            k.time = i / frameRate;
            keys[i] = k;
        }
    }
}
#endif