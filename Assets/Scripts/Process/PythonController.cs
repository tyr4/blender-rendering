using System.Collections;
using UnityEngine;
using SimpleFileBrowser;

public class PythonController : MonoBehaviour
{
    private PythonProcessManager _processManager;
        
    private SettingsManager _settingsManager;
    private UserSettings _settings;
    private string _settingsPath;

    private void Start()
    {
        _processManager = new PythonProcessManager();
        _settingsManager = new SettingsManager();
        _settings = _settingsManager.settings;

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
    
    // button triggered commands
    private void SendCommand(string command)
    {
        _settings.current_command = command;
        _processManager.SendCommand(_settings);
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
        _settings.scene_path = FileBrowser.Result[0];
        SendCommand("load_scene");
    }
}
