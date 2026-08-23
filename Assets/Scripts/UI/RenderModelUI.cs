using System;
using System.IO;
using System.Transactions;
using UnityEngine;
using UnityEngine.UI;

public class RenderModelUI : MonoBehaviour
{
    [SerializeField] private Image modelImage;
    [SerializeField] private GameObject helperText;
    
    private PythonController _controller;
    
    // TODO: add onsceneloaded event as well + helper text enable/disable for scene load
    private void Start()
    {
        _controller = PythonController.Instance;
        helperText.SetActive(true);

        PythonController.OnSceneLoaded += RenderModelOnLoad;
        PythonController.OnFbxLoaded += RenderModelOnLoad;
    }

    private void OnDestroy()
    {
        PythonController.OnSceneLoaded -= RenderModelOnLoad;
        PythonController.OnFbxLoaded -= RenderModelOnLoad;
    }

    private async void RenderModelOnLoad()
    {
        try
        {
            helperText.SetActive(false);
        
            try
            {
                var response = await _controller.SendCommandAsync("render_single_frame");
                var filepath = Utils.GetJsonMessageResponse(response)!.ToObject<string>();
                UpdateModelSnapshot(filepath);
            }
        
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    
    private void UpdateModelSnapshot(string filepath)
    {
        var sprite = LoadSpriteFromFile(filepath);
        if (sprite == null) return;
        
        modelImage.sprite = sprite;
    }

    private Sprite LoadSpriteFromFile(string filepath)
    {
        if (!File.Exists(filepath))
        {
            Debug.LogError($"n ai fila bos {filepath}");
            return null;
        }

        byte[] fileData = File.ReadAllBytes(filepath);
        Texture2D texture = new Texture2D(2, 2); // filler extensions, replaced once the image loads

        if (!texture.LoadImage(fileData))
        {
            Debug.LogError($"failed to load data from {filepath}");
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        return sprite;
    }
}
