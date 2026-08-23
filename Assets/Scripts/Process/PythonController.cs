using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SimpleFileBrowser;

public class PythonController : MonoBehaviour
{
    public static PythonController Instance { get; private set; }
    private PythonProcessManager _processManager;
        
    private SettingsManager _settingsManager;
    public UserSettings settings { get; private set; }
    private string _settingsPath;

    public static event Action OnSceneLoaded;
    public static event Action OnFbxLoaded;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        _processManager = new PythonProcessManager();
        _settingsManager = new SettingsManager();
        settings = _settingsManager.settings;

        _processManager.Initialize();
        _processManager.OnLineReceived += OnLineReceived;
    }

    private void OnDestroy()
    {
        _processManager.OnLineReceived -= OnLineReceived;
    }

    private void OnLineReceived(string line)
    {
        Debug.Log($"UITE CE AM PRIMIT: {line}");
    }

    private void SendCommand(string command)
    {
        settings.current_command = command;

        _processManager.SendCommand(settings);
    }
    
    public async Task<string> SendCommandAsync(string command)
    {
        settings.current_command = command;

        try
        {
            string response = await _processManager.SendCommandAsync(settings);
            return response;
        }
        catch (Exception e)
        {
            Debug.LogError($"Command {command} failed: {e.Message}");
            return null;
        }
    }
    
    // TODO: separate this below -----------------------------------------
    
    private void SetFilters(string categoryName, params string[] extensions)
    {
        // FileBrowser.SetFilters( true, new FileBrowser.Filter( "Scenes", ".blend" ));
        
        FileBrowser.SetFilters(false, new FileBrowser.Filter(categoryName, extensions));
        // FileBrowser.SetDefaultFilter(extensions[0]);
    }

    public void PickSceneButtonWrapper()
    {
        StartCoroutine(PickSceneButton());
    }

    private IEnumerator PickSceneButton()
    {
        SetFilters("Scenes", ".blend");
        
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, 
            true, 
            null, 
            null, 
            "Select Files", 
            "Load"
            );

        if (!FileBrowser.Success) yield break;

        Debug.Log(FileBrowser.Result[0]);
        settings.scene_path = FileBrowser.Result[0];
        
        PickSceneHandler();
    }

    private void PickSceneHandler()
    {
        SendCommand("load_scene");
        OnSceneLoaded?.Invoke();
    }

    public void PickFbxButtonWrapper()
    {
        StartCoroutine(PickFbxButton());
    }
    
    private IEnumerator PickFbxButton()
    {
        SetFilters("FBX", ".fbx");
        
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, 
            true, 
            null, 
            null, 
            "Select Files", 
            "Load"
        );

        if (!FileBrowser.Success) yield break;

        Debug.Log(FileBrowser.Result[0]);
        settings.fbx_path = FileBrowser.Result[0];
        
        PickFbxHandler();
    }

    private async void PickFbxHandler()
    {
        try
        {
            string fbxResponse = await SendCommandAsync("load_fbx");
            string fbxStatus = Utils.GetJsonStatusResponse(fbxResponse);
            if (fbxStatus == "error")
            {
                Debug.LogError("n ai scena fraiere");
                return;
            }
            
            string armatureResponse = await SendCommandAsync("get_fbx_armatures");
            Debug.Log(armatureResponse);
            
            OnFbxLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void PickSceneFbxButtonWrapper()
    {
        StartCoroutine(PickSceneFbxButton());
    }

    private IEnumerator PickSceneFbxButton()
    {
        SetFilters("Scenes & FBX", ".blend", ".fbx");
        
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, 
            true, 
            null, 
            null, 
            "Select Files", 
            "Load"
        );

        if (!FileBrowser.Success) yield break;
        if (FileBrowser.Result.Length != 2)
        {
            Debug.LogError($"AI NEVOIE DE 2 FILE NU {FileBrowser.Result.Length}");
            yield break;
            
        }
        
        if (FileBrowser.Result[0].EndsWith(".fbx") && FileBrowser.Result[1].EndsWith(".blend"))
        {
            settings.fbx_path = FileBrowser.Result[0];
            settings.scene_path = FileBrowser.Result[1];
        }
        else if (FileBrowser.Result[1].EndsWith(".fbx") && FileBrowser.Result[0].EndsWith(".blend"))
        {
            settings.scene_path = FileBrowser.Result[0];
            settings.fbx_path = FileBrowser.Result[1];
        }
        else
        {
            Debug.LogError("NU AI SELECTAT CE TREBUIE");
            yield break;
        }
        
        PickSceneHandler();
        PickFbxHandler();
    }

    public async Task<List<string>> GetFbxArmatures()
    {
        try
        {
            string response = await SendCommandAsync("get_fbx_armatures");
            if (string.IsNullOrEmpty(response)) return null;
            
            var message = Utils.GetJsonMessageResponse(response);
            if (message == null) return null;
        
            var animations = message["animations"]!.ToObject<List<string>>();
            
            return animations;
        }
        
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
    }
}
