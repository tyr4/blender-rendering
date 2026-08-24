using System;
using UnityEngine;

public class RenderModelManager : MonoBehaviour
{
    public static event Action<string> OnModelRender;

    private PythonController _controller;

    private void Start()
    {
        _controller = PythonController.Instance;
        
        PythonController.OnSceneLoaded += RenderModel;
        PythonController.OnFbxLoaded += RenderModel;
        PythonController.OnSettingsChanged += RenderModel;
    }

    private void OnDestroy()
    {
        PythonController.OnSceneLoaded -= RenderModel;
        PythonController.OnFbxLoaded -= RenderModel;
        PythonController.OnSettingsChanged -= RenderModel;
    }
    
    private async void RenderModel()
    {
        try
        {
            try
            {
                var response = await _controller.SendCommandAsync("render_single_frame");
                var filepath = Utils.GetJsonMessageResponse(response)!.ToObject<string>();
                
                OnModelRender?.Invoke(filepath);
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
}
