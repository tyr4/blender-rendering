#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class AnimationClipGenerator
{
    private static string ToUnityRelativePath(string absolutePath)
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
            Debug.LogError($"No texture importer found at {assetPath}");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        
        // replace this part with actual x/y passed
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
}
#endif