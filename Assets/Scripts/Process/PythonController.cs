using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    
    public static event Action<string> OnActionBegin;
    public static event Action<string> OnActionFinish;
    
    public static event Action OnSceneLoaded;
    public static event Action OnSceneLoadedNoFbx;
    
    public static event Action OnFbxLoaded;
    
    public static event Action<List<string>> OnArmatureLoaded;
    public static event Action OnSettingsChanged;
    public static event Action OnRenderRequested;
    public static event Action<AnimationClip> OnAnimationClipGenerated;
    public static event Action<Dictionary<string, JToken>> OnObjectDataRequested;

    private void Awake()
    {
        Instance = this;
        
        _settingsManager = new SettingsManager();
        settings = _settingsManager.Load();
    }
    
    private void Start()
    {
        _processManager = new PythonProcessManager();

        _processManager.Initialize();
        _processManager.OnLineReceived += OnLineReceived;

        SliderUpdateManager.OnSettingsChangedRequest += ApplySceneSettings;
        SceneSettingsUI.OnSceneUILoaded += GetAllObjectData;
        OnObjectDataRequested += UpdateSettingsOnObjectDataReceived;
    }

    private void OnDestroy()
    {
        _processManager.OnLineReceived -= OnLineReceived;
        SliderUpdateManager.OnSettingsChangedRequest -= ApplySceneSettings;
        SceneSettingsUI.OnSceneUILoaded -= GetAllObjectData;
        OnObjectDataRequested -= UpdateSettingsOnObjectDataReceived;
    }

    public void SaveSettings()
    {
        _settingsManager.Save(settings);
    }

    private void OnLineReceived(string line)
    {
        ConsoleLog.PythonLog($"Received line from Python: {line}");
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

    private void BuildAnimationDictionary(List<string> animations)
    {
        settings.animation_dict.Clear();

        foreach (var anim in animations)
        {
            settings.animation_dict.Add(anim, true);
        }
    }

    public void SetAnimationToggle(string anim, bool value)
    {
        if (settings.animation_dict.Count == 0) return;
        
        settings.animation_dict[anim] = value;
    }

    public void SetRequestedRenderAnimation(int index)
    {
        settings.selected_anim = index;
        RenderSingleAnimation();
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

        settings.scene_path = FileBrowser.Result[0];
        settings.fbx_path = "";
        
        LoadScene();
    }

    private async void LoadScene()
    {
        try
        {
            OnActionBegin?.Invoke("Load Scene");

            string response = await SendCommandAsync("load_scene");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error loading Scene");
                OnActionFinish?.Invoke("Load Scene");
                
                return;
            }

            OnSceneLoaded?.Invoke();
            
            var animations = await GetFbxAnimations();
            if (animations.Count == 0) return;
            
            BuildAnimationDictionary(animations);
            OnArmatureLoaded?.Invoke(animations);
            OnSceneLoadedNoFbx?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Load Scene");
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

        settings.fbx_path = FileBrowser.Result[0];
        
        LoadFbx();
    }

    private async void LoadFbx()
    {
        try
        {
            OnActionBegin?.Invoke("Load FBX");

            string fbxResponse = await SendCommandAsync("load_fbx");
            string fbxStatus = Utils.GetJsonStatusResponse(fbxResponse);
            if (fbxStatus == "error")
            {
                Debug.LogError("Error loading FBX");
                OnActionFinish?.Invoke("Load FBX");
                
                return;
            }
            
            var animations = await GetFbxAnimations();
            if (animations.Count != 0) OnArmatureLoaded?.Invoke(animations);
            
            OnFbxLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Load FBX");
        }
    }

    private async void ReloadFbx()
    {
        try
        {
            OnActionBegin?.Invoke("Delete Armature");

            string response = await SendCommandAsync("delete_armature");
            string status = Utils.GetJsonStatusResponse(response);
            
            if (status == "error")
            {
                Debug.LogError("Error deleting Armature");
                OnActionFinish?.Invoke("Delete Armature");
                
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
            OnActionFinish?.Invoke("Delete Armature");
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
            Debug.LogError($"You need to choose 2 files, not {FileBrowser.Result.Length}");
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
            Debug.LogError("You must select a .scene and an .fbx file");
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

    public void ExportAllAnimationsButton()
    {
        GenerateAllSpritesheets();
    }
    
    public void ExportSelectedAnimationsButton()
    {
        GenerateSelectedSpritesheets();
    }
    
    public async Task<List<string>> GetFbxAnimations()
    {
        try
        {
            OnActionBegin?.Invoke("Get FBX armature");

            string response = await SendCommandAsync("get_fbx_armatures");
            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("Error loading FBX armature");
                OnActionFinish?.Invoke("Get FBX armature");
                
                return null;
            }
            
            var message = Utils.GetJsonMessageResponse(response);
            if (message == null)
            {
                Debug.LogError("Response is empty");
                OnActionFinish?.Invoke("Get FBX armature");
                
                return null;
            }
        
            var animations = message["animations"]!.ToObject<List<string>>();
            
            return animations;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return null;
        }
        finally
        {
            OnActionFinish?.Invoke("Get FBX armature");
        }
    }

    public async void ApplySceneSettings()
    {
        try
        {
            OnActionBegin?.Invoke("Apply settings to scene");
            
            SaveSettings();

            string response = await SendCommandAsync("apply_settings");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error applying settings");
                OnActionFinish?.Invoke("Apply settings to scene");
                
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
            OnActionFinish?.Invoke("Apply settings to scene");
        }
    }

    private async void CenterArmatureToCamera()
    {
        try
        {
            OnActionBegin?.Invoke("Centering object to camera");
            
            string response = await SendCommandAsync("center_to_camera");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error centering to camera");
                OnActionFinish?.Invoke("Centering object to camera");
                
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
            OnActionFinish?.Invoke("Centering object to camera");
            GetAllObjectData(); // very important to keep this AFTER OnActionFinish
        }
    }
    
    private async void GetAllObjectData()
    {
        try
        {
            OnActionBegin?.Invoke("Get all object data");
            
            string response = await SendCommandAsync("get_all_object_data");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error loading all object data");
                OnActionFinish?.Invoke("Get all object data");
                
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
            OnActionFinish?.Invoke("Get all object data");
        }
    }

    private void UpdateSettingsOnObjectDataReceived(Dictionary<string, JToken> data)
    {
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

        if (snapshotToggles.camera_orthographic_scale)
            currentSettings.camera_orthographic_scale = _snapshotSettings.camera_orthographic_scale;
        
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

    private async void GenerateAllSpritesheets()
    {
        try
        {
            OnActionBegin?.Invoke("Generate all spritesheets");
            
            string response = await SendCommandAsync("generate_all_spritesheets");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error generating all spritesheets");
                OnActionFinish?.Invoke("Generate all spritesheets");
                
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Generate all spritesheets");
        }
    }

    private async void GenerateSelectedSpritesheets()
    {
        try
        {
            OnActionBegin?.Invoke("Generate all spritesheets");
            
            string response = await SendCommandAsync("generate_selected_spritesheets");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error generating all spritesheets");
                OnActionFinish?.Invoke("Generate all spritesheets");
                
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Generate all spritesheets");
        }
    }

    private async void RenderSingleAnimation()
    {
        try
        {
            OnActionBegin?.Invoke("Render single anim");
            
            string response = await SendCommandAsync("render_single_anim");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error rendering single anim");
                OnActionFinish?.Invoke("Render single anim");
                
                return;
            }

            var message = Utils.GetJsonMessageResponse(response);
            var spritesheetPath = message["output_path"]!.ToObject<string>();
            var columns = message["columns"]!.ToObject<int>();
            var rows = message["rows"]!.ToObject<int>();
            var frameCount = message["frame_count"]!.ToObject<int>();

            var anim = AnimationClipGenerator.CreateAnimationClip(spritesheetPath, settings.fps, columns, rows, frameCount);
            OnAnimationClipGenerated?.Invoke(anim);
            ConsoleLog.ProcessLog($"UITE ANIMATION {anim}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Render single anim");
        }
    }
}
