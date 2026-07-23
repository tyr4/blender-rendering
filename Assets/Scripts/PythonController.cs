using UnityEngine;

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
    public void SendCommand(string command)
    {
        _settings.current_command = command;
        _processManager.SendCommand(_settings);
    }
}
