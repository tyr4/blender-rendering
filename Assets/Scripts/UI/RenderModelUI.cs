using System;
using System.IO;
using System.Transactions;
using UnityEngine;
using UnityEngine.UI;

public class RenderModelUI : MonoBehaviour
{
    [SerializeField] private Image modelImage;
    [SerializeField] private GameObject helperText;
    
    private void Start()
    {
        helperText.SetActive(true);

        modelImage.preserveAspect = true;
        modelImage.color = new Color(1, 1, 1, 0);
        
        RenderModelManager.OnModelRender += OnModelRender;
    }

    private void OnDestroy()
    {
        RenderModelManager.OnModelRender -= OnModelRender;
    }

    private void OnModelRender(string filepath)
    {
        helperText.SetActive(false);
        UpdateModelSnapshot(filepath);
    }

    
    private void UpdateModelSnapshot(string filepath)
    {
        ConsoleLog.RenderLog(
            $"UpdateModelSnapshot on {gameObject.name} " +
            $"({GetInstanceID()}), modelImage = {modelImage}"
        );

        var sprite = LoadSpriteFromFile(filepath);
        if (sprite == null) return;

        if (modelImage == null)
        {
            Debug.LogError(
                $"modelImage is NULL! " +
                $"RenderModelUI={GetInstanceID()}, " +
                $"GameObject={gameObject.name}, " +
                $"Scene={gameObject.scene.name}"
            );
            return;
        }

        modelImage.sprite = sprite;
        modelImage.color = new Color(1, 1, 1, 1);
    }

    private Sprite LoadSpriteFromFile(string filepath)
    {
        if (!File.Exists(filepath))
        {
            Debug.LogError($"{filepath} doesn't exist");
            return null;
        }

        byte[] fileData = File.ReadAllBytes(filepath);
        Texture2D texture = new Texture2D(2, 2) // filler parameters, replaced once the image loads
        {
            filterMode = FilterMode.Point
        };

        if (!texture.LoadImage(fileData))
        {
            Debug.LogError($"Failed to load data from {filepath}");
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        return sprite;
    }
}
