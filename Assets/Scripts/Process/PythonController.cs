using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SimpleFileBrowser;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PythonController : MonoBehaviour
{
    public static PythonController Instance { get; private set; }
    private PythonProcessManager _processManager;
        
    private SettingsManager _settingsManager;
    public UserSettings settings { get; private set; }
    
    private UserSettings _snapshotSettings;
    public SnapshotToggles snapshotToggles { get; set; } = new();
    private AnimationClip _currentAnim;
    
    private string _settingsPath;
    private const string DefaultScenePath = @"\Default Scene\empty_scene.blend";

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
    public static event Action<AnimationClip> OnAnimationClipWithSkipsGenerated;
    public static event Action<List<AnimationClip>> OnBulkAnimationsFinished;
    public static event Action<Dictionary<string, JToken>> OnObjectDataRequested;
    public static event Action<List<UserSettings>> OnBatchModeBegin; 
    public static event Action OnBatchModeFinish; 

    
    private List<UserSettings> _batchFiles = new();
    private List<AnimationClip> _batchClips = new();
    private bool _startedBatchRendering = false;

    private void Awake()
    {
        Instance = this;
        
        _settingsManager = new SettingsManager();
        settings = _settingsManager.Load();
        
        // refresh states after they get saved between sessions
        settings.animation_dict.Clear();
        settings.selected_anim_skip_keyframes.Clear();
        settings.render_as_mesh = true;
        settings.extra_light = false;
    }
    
    private void Start()
    {
        _processManager = new PythonProcessManager();

        _processManager.Initialize();
        _processManager.OnLineReceived += OnLineReceived;

        SliderUpdateManager.OnSettingsChangedRequest += ApplySceneSettingsHandler;
        SceneSettingsUI.OnSceneUILoaded += GetAllObjectData;
        KeyframeComponent.OnKeyframeToggleClicked += OnKeyframeToggleClicked;
        BatchModeExportHandler.OnBatchModelSelected += LoadModelFromBatchHandler;
        
        OnObjectDataRequested += UpdateSettingsOnObjectDataReceived;
        OnBulkAnimationsFinished += StoreBatchModeClips;
    }

    private void OnDestroy()
    {
        _processManager.OnLineReceived -= OnLineReceived;
        
        SliderUpdateManager.OnSettingsChangedRequest -= ApplySceneSettingsHandler;
        SceneSettingsUI.OnSceneUILoaded -= GetAllObjectData;
        KeyframeComponent.OnKeyframeToggleClicked -= OnKeyframeToggleClicked;
        BatchModeExportHandler.OnBatchModelSelected -= LoadModelFromBatchHandler;
        
        OnObjectDataRequested -= UpdateSettingsOnObjectDataReceived;
        OnBulkAnimationsFinished -= StoreBatchModeClips;
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
        // settings.selected_anim_skip_keyframes.Clear();
        RenderSingleAnimation();
    }

    public List<bool> GetCurrentAnimationSkipList()
    {
        if (_currentAnim == null) return null;
        
        return settings.selected_anim_skip_keyframes.GetValueOrDefault(_currentAnim.name);
    }

    public void SetAnimationSkipListToValue(bool value)
    {
        var list = GetCurrentAnimationSkipList();
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
            list[i] = value;
    }

    public async void LoadDefaultSceneButton()
    {
        try
        {
            var path = Application.streamingAssetsPath + DefaultScenePath;
            settings.scene_path = path;
        
            await LoadScene();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
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
        
        var task = LoadScene();
    }

    private async Task LoadScene()
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
            ApplyExtraLight();
        }
    }

    public void PickFbxButtonWrapper()
    {
        StartCoroutine(PickFbxButton());
    }

    private bool EligibleForBatchMode()
    {
        var count = 0;
        
        foreach (var filename in FileBrowser.Result)
        {
            if (filename.EndsWith(".fbx"))
                count++;
        }

        return count > 1;
    }

    private List<string> GetFbxFilenames()
    {
        List<String> buf = new();

        foreach (var filename in FileBrowser.Result)
        {
            if (filename.EndsWith(".fbx"))
                buf.Add(filename);
        }

        return buf;
    }
    
    private string GetFirstFbx()
    {
        foreach (var filename in FileBrowser.Result)
        {
            if (filename.EndsWith(".fbx"))
                return filename;
        }
        
        return "";
    }
    
    private string GetFirstScene()
    {
        foreach (var filename in FileBrowser.Result)
        {
            if (filename.EndsWith(".blend"))
                return filename;
        }
        
        return "";
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

        if (!EligibleForBatchMode())
        {
            settings.fbx_path = FileBrowser.Result[0];
            var task = LoadFbx();
        }
        else
        {
            StartBatchMode();
        }
    }

    private async Task LoadFbx()
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
            BuildAnimationDictionary(animations);
            
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

    private async Task DeleteArmature()
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

    private async void ReloadFbx()
    {
        try
        {
            OnActionBegin?.Invoke("Reload Fbx");
            
            await DeleteArmature();
            await LoadFbx();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Reload Fbx");
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

        var firstScene = GetFirstScene();
        var firstFbx = GetFirstFbx();
        
        if (!EligibleForBatchMode())
        {
            settings.fbx_path = firstFbx;
            settings.scene_path = firstScene;
            
            var sceneTask = LoadScene();
            var fbxTask = LoadFbx();
        }

        else if (!String.IsNullOrEmpty(firstScene))
        {
            StartBatchMode(firstScene);
        }
    }

    public async void ReloadSceneButton()
    {
        try
        {
            await LoadScene();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void ReloadFbxButton()
    {
        ReloadFbx();
    }

    public async void ReloadSceneFbxButton()
    {
        try
        {
            await LoadScene();
            await LoadFbx();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public async void CenterArmatureButton()
    {
        try
        {
            await CenterArmatureToCamera();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void ExportAllAnimationsButton()
    {
        ExportAllAnimationsHandler();
        // GenerateAllAnimations();
    }
    
    public async Task ExportSelectedAnimationsButton()
    {
        await ExportSelectedAnimationsHandler();
    }
    
    private async Task<List<string>> GetFbxAnimations()
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

    private async void ApplySceneSettingsHandler()
    {
        try
        {
            await ApplySceneSettings();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    
    public async Task ApplySceneSettings()
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

    private async Task CenterArmatureToCamera()
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
    
    public async void ApplyExtraLight()
    {
        try
        {
            OnActionBegin?.Invoke("Applying extra light");
            
            string response = await SendCommandAsync("extra_light");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error applying extra light");
                OnActionFinish?.Invoke("Applying extra light");
                
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
            OnActionFinish?.Invoke("Applying extra light");
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
        settings.camera_rotation = data["camera_rotation"].ToObject<float[]>();
        //
        settings.starting_rotation = data["starting_rotation"].ToObject<float[]>();
        
        settings.parent_object_position = data["parent_object_position"].ToObject<float[]>();
        settings.parent_object_rotation = data["parent_object_rotation"].ToObject<float[]>();
        
        settings.reposition_object_position = data["reposition_object_position"].ToObject<float[]>();
        settings.reposition_object_rotation = data["reposition_object_rotation"].ToObject<float[]>();
        settings.render_as_mesh = data["render_as_mesh"].ToObject<bool>();
        settings.extra_light = data["extra_light"].ToObject<bool>();
        
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

    private async Task<List<AnimationClip>> GenerateAnimationsAsync(JToken message)
    {
        List<AnimationClip> generatedClips = new();
        var list = message.ToObject<List<JToken>>();
            
#if UNITY_EDITOR
        // make sure the editor knows the spritesheets are there
        AssetDatabase.Refresh();
#endif

        foreach (var spritesheet in list)
        {
            var clip = CreateAnimationClip(spritesheet);
            if (clip == null)
            {
                Debug.LogError("Error generating animation clip, clip is null");
                await Task.Yield();
                continue;
            }
            
            generatedClips.Add(clip);
            ConsoleLog.RenderLog($"Generated animation clip: {clip.name}");
            await Task.Yield();
        }

        return generatedClips;
    }
    
    private async void ExportAllAnimationsHandler()
    {
        try
        {
            OnActionBegin?.Invoke("Generate all animations");
            
            string response = await SendCommandAsync("generate_all_animations");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error generating all animations");
                OnActionFinish?.Invoke("Generate all animations");
                
                return;
            }
            
            var message = Utils.GetJsonMessageResponse(response);
            Debug.Log(message);
            var clips = await GenerateAnimationsAsync(message);
            var spritesheets = message.ToObject<List<JToken>>();
            
            ClipPackageExporter.Export(clips, spritesheets, settings.render_as_mesh);
            
            OnBulkAnimationsFinished?.Invoke(clips);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Generate all animations");
        }
    }

    private async Task ExportSelectedAnimationsHandler()
    {
        try
        {
            OnActionBegin?.Invoke("Generate selected animations");
            
            string response = await SendCommandAsync("generate_selected_animations");
            var status = Utils.GetJsonStatusResponse(response);

            if (status == "error")
            {
                Debug.LogError("Error generating selected animations");
                OnActionFinish?.Invoke("Generate selected animations");
                
                return;
            }
            
            var message = Utils.GetJsonMessageResponse(response);
            var clips = await GenerateAnimationsAsync(message);
            var spritesheets = message.ToObject<List<JToken>>();
            
            ClipPackageExporter.Export(clips, spritesheets, settings.render_as_mesh);

            OnBulkAnimationsFinished?.Invoke(clips);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnActionFinish?.Invoke("Generate selected animations");
        }
    }

    private AnimationClip CreateAnimationClip(JToken message)
    {
        var spritesheetPath = message["output_path"]!.ToObject<string>();
        var columns = message["columns"]!.ToObject<int>();
        var rows = message["rows"]!.ToObject<int>();
        var frameCount = message["frame_count"]!.ToObject<int>();
        
        return AnimationClipGenerator.CreateAnimationClip(spritesheetPath, settings.fps, columns, rows, frameCount);
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
            var anim = CreateAnimationClip(message);
            _currentAnim = anim;
            
            OnAnimationClipGenerated?.Invoke(anim);
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

    private void OnKeyframeToggleClicked(string animName, int frameIndex, bool value)
    {
        if (!settings.selected_anim_skip_keyframes.TryGetValue(animName, out var toggleList))
        {
            toggleList = new();
            settings.selected_anim_skip_keyframes[animName] = toggleList;
        }
        
        while (toggleList.Count <= frameIndex)
            toggleList.Add(false);
        
        toggleList[frameIndex] = value;
        RegenerateAnimationClip(toggleList);
    }

    private void RegenerateAnimationClip(List<bool> toggleList)
    {
        AnimationClip anim;
        try
        {
            anim = AnimationClipGenerator.RebuildClipSkippingKeyframes(_currentAnim, toggleList);
        }
        catch (Exception e)
        {
            Debug.LogError($"RebuildClipSkippingKeyframes threw: {e}");
            return;
        }

        if (anim == null)
        {
            Debug.LogError("RebuildClipSkippingKeyframes returned null");
            return;
        }
    
        OnAnimationClipWithSkipsGenerated?.Invoke(anim);
    }

    private void StartBatchMode(string sceneToLoad = null)
    {
        var files = GetFbxFilenames();

        if (sceneToLoad != null)
        {
            settings.scene_path = sceneToLoad;
            var task = LoadScene();
        }
        
        _batchFiles.Clear();
        foreach (var file in files)
        {
            var copy = new UserSettings(settings)
            {
                fbx_path = file
            };
            _batchFiles.Add(copy);
            ConsoleLog.ProcessLog($"Added {file} to list");
        }
        
        OnBatchModeBegin?.Invoke(_batchFiles);
    }

    private void LoadModelFromBatchHandler(UserSettings modelSettings)
    {
        var task = LoadModelFromBatch(modelSettings);
    }
    
    private async Task LoadModelFromBatch(UserSettings modelSettings)
    {
        settings = modelSettings;
        
        await DeleteArmature();
        await LoadFbx();
        await ApplySceneSettings();
        await CenterArmatureToCamera();
    }

    public async void ExportBatchButton()
    {
        try
        {
            _startedBatchRendering = true;
            _batchClips.Clear();
            
            foreach (var model in _batchFiles)
            {
                await LoadModelFromBatch(model);
                await ExportSelectedAnimationsButton();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        finally
        {
            OnBatchModeFinish?.Invoke();
            OnBulkAnimationsFinished?.Invoke(_batchClips);
            _startedBatchRendering = false;
        }
    }

    private void StoreBatchModeClips(List<AnimationClip> clips)
    {
        if (!_startedBatchRendering) return;
        
        _batchClips.AddRange(clips);
    }
}
