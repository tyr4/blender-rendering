using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SimpleFileBrowser;

public class PythonController : MonoBehaviour
{
    public static PythonController Instance { get; private set; }
    private PythonProcessManager _processManager;
        
    private SettingsManager _settingsManager;
    public UserSettings settings { get; set; }
    
    private UserSettings _snapshotSettings;
    public SnapshotToggles snapshotToggles { get; set; } = new();
    
    private string _settingsPath;

    private readonly SemaphoreSlim _commandLock = new(1, 1);
    
    public static event Action OnActionBegin;
    public static event Action OnActionFinish;
    
    public static event Action OnSceneLoaded;
    public static event Action OnSceneLoadedNoFbx;
    
    public static event Action OnFbxLoaded;
    
    public static event Action<List<string>> OnArmatureLoaded;
    public static event Action OnSettingsChanged;
    public static event Action OnRenderRequested;
    public static event Action<Dictionary<string, JToken>> OnObjectDataRequested;

    private void Awake()
    {
        Instance = this;
        
        _settingsManager = new SettingsManager();
        settings = _settingsManager.settings;
    }
    
    private void Start()
    {
        _processManager = new PythonProcessManager();

        _processManager.Initialize();
        _processManager.OnLineReceived += OnLineReceived;

        SliderUpdateManager.OnSettingsChangedRequest += ApplySceneSettings;
        UpdateSceneSettingsUI.OnSceneUILoaded += GetAllObjectData;
        OnObjectDataRequested += UpdateSettingsOnObjectDataReceived;
    }

    private void OnDestroy()
    {
        _processManager.OnLineReceived -= OnLineReceived;
        SliderUpdateManager.OnSettingsChangedRequest -= ApplySceneSettings;
        UpdateSceneSettingsUI.OnSceneUILoaded -= GetAllObjectData;
        OnObjectDataRequested -= UpdateSettingsOnObjectDataReceived;
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
        await _commandLock.WaitAsync();
        
        try
        {
            settings.current_command = command;
            var settingsWithSnapshotToggles = ApplySnapshotToggles();
            
            return await _processManager.SendCommandAsync(settingsWithSnapshotToggles);
        }
        catch (Exception e)
        {
            Debug.LogError($"Command {command} failed: {e.Message}");
            return null;
        }
        finally
        {
            _commandLock.Release();
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
        settings.fbx_path = "";
        
        LoadScene();
    }

    private async void LoadScene()
    {
        try
        {
            OnActionBegin?.Invoke();

            string response = await SendCommandAsync("load_scene");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("n am putut da load la scena");
                OnActionFinish?.Invoke();
                
                return;
            }

            OnSceneLoaded?.Invoke();
            
            var animations = await GetFbxAnimations();
            if (animations.Count == 0) return;

            OnArmatureLoaded?.Invoke(animations);
            OnSceneLoadedNoFbx?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke();
        }
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
        
        LoadFbx();
    }

    private async void LoadFbx()
    {
        try
        {
            OnActionBegin?.Invoke();

            string fbxResponse = await SendCommandAsync("load_fbx");
            string fbxStatus = Utils.GetJsonStatusResponse(fbxResponse);
            if (fbxStatus == "error")
            {
                Debug.LogError("error loading fbx");
                OnActionFinish?.Invoke();
                
                return;
            }
            
            var animations = await GetFbxAnimations();
            if (animations.Count != 0) OnArmatureLoaded?.Invoke(animations);
            
            OnFbxLoaded?.Invoke();
            OnActionFinish?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke();
        }
    }

    private async void ReloadFbx()
    {
        try
        {
            OnActionBegin?.Invoke();

            string response = await SendCommandAsync("delete_armature");
            string status = Utils.GetJsonStatusResponse(response);
            
            if (status == "error")
            {
                Debug.LogError("n a mers delete armature");
                OnActionFinish?.Invoke();
                
                return;
            }
            
            LoadFbx();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke();
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
        
        LoadScene();
        LoadFbx();
    }

    public void ReloadSceneButton()
    {
        LoadScene();
    }

    public void ReloadFbxButton()
    {
        ReloadFbx();
    }

    public void ReloadSceneFbxButton()
    {
        LoadScene();
        LoadFbx();
    }

    public void CenterArmatureButton()
    {
        CenterArmatureToCamera();
    }
    
    public async Task<List<string>> GetFbxAnimations()
    {
        try
        {
            OnActionBegin?.Invoke();

            string response = await SendCommandAsync("get_fbx_armatures");
            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("response is empty");
                OnActionFinish?.Invoke();
                
                return null;
            }
            
            var message = Utils.GetJsonMessageResponse(response);
            if (message == null)
            {
                Debug.LogError("message is empty");
                OnActionFinish?.Invoke();
                
                return null;
            }
        
            var animations = message["animations"]!.ToObject<List<string>>();
            Debug.Log(animations + " count " + animations.Count);
            
            return animations;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
        finally
        {
            OnActionFinish?.Invoke();
        }
    }

    public async void ApplySceneSettings()
    {
        try
        {
            OnActionBegin?.Invoke();

            string response = await SendCommandAsync("apply_settings");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("status is empty");
                OnActionFinish?.Invoke();
                
                return;
            }
            
            OnSettingsChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke();
        }
    }

    private async void CenterArmatureToCamera()
    {
        try
        {
            OnActionBegin?.Invoke();
            
            string response = await SendCommandAsync("center_to_camera");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("status is empty");
                OnActionFinish?.Invoke();
                
                return;
            }
            
            OnRenderRequested?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke();
            GetAllObjectData(); // very important to keep this AFTER OnActionFinish
        }
    }
    
    private async void GetAllObjectData()
    {
        try
        {
            OnActionBegin?.Invoke();
            
            string response = await SendCommandAsync("get_all_object_data");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("status is empty");
                OnActionFinish?.Invoke();
                
                return;
            }
            
            var dict = Utils.GetJsonMessageResponse(response).ToObject<Dictionary<string, JToken>>();
            
            OnObjectDataRequested?.Invoke(dict);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke();
        }
    }

    private void UpdateSettingsOnObjectDataReceived(Dictionary<string, JToken> data)
    {
        Debug.Log($"received keys: {string.Join(", ", data.Keys)}");
        
        settings.camera_orthographic_scale = data["camera_orthographic_scale"].ToObject<float>();
        settings.camera_position = data["camera_position"].ToObject<float[]>();
        //
        settings.starting_rotation = data["starting_rotation"].ToObject<float[]>();
        
        settings.parent_object_position = data["parent_object_position"].ToObject<float[]>();
        settings.parent_object_rotation = data["parent_object_rotation"].ToObject<float[]>();
        
        settings.reposition_object_position = data["reposition_object_position"].ToObject<float[]>();
        settings.reposition_object_rotation = data["reposition_object_rotation"].ToObject<float[]>();

        _snapshotSettings ??= new UserSettings(settings);
    }

    private UserSettings ApplySnapshotToggles()
    {
        var currentSettings = new UserSettings(settings);

        if (snapshotToggles.camera_position) 
            currentSettings.camera_position = _snapshotSettings.camera_position;

        if (snapshotToggles.starting_rotation)
            currentSettings.starting_rotation = _snapshotSettings.starting_rotation;
        
        if (snapshotToggles.parent_object_position)
            currentSettings.parent_object_position = _snapshotSettings.parent_object_position;
        
        if (snapshotToggles.parent_object_rotation)
            currentSettings.parent_object_rotation = _snapshotSettings.parent_object_rotation;

        if (snapshotToggles.reposition_object_position)
            currentSettings.reposition_object_position = _snapshotSettings.reposition_object_position;

        if (snapshotToggles.reposition_object_rotation)
            currentSettings.reposition_object_rotation = _snapshotSettings.reposition_object_rotation;
        
        return currentSettings;
    }
}
