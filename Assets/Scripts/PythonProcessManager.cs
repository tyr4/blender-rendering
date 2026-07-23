using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PythonProcessManager
{
    private Process _process;

    private StreamWriter _stdin;
    private StreamReader _stdout;

    public event Action<string> OnLineReceived;

    public void Initialize()
    {
        StartPythonProcess();
        Task.Run(ReadLoop);
    }
    
    private string GetExecutablePath()
    {
        return Path.Combine(Application.streamingAssetsPath, "Python", "main.exe");
    }

    private void StartPythonProcess()
    {
        var path = GetExecutablePath();
        ProcessStartInfo psi;
        
        #if UNITY_EDITOR
        var settings = new SettingsManager();
            psi = new ProcessStartInfo
            {
                FileName = settings.settings.python_interpreter,
                ArgumentList = { settings.settings.python_file_path },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        #else
            psi = new ProcessStartInfo
            {
                FileName = path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        #endif
        
        psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
        
        _process = Process.Start(psi)!;
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log("[python stderr] " + e.Data);
        };
        _process.BeginErrorReadLine();
    }

    public bool IsRunning => _process is { HasExited: false };

    public void Kill()
    {
        if (IsRunning)
            _process.Kill();
    }

    private async Task ReadLoop()
    {
        while (!_process.HasExited)
        {
            string line = await _stdout.ReadLineAsync();
            
            if (line == null) continue;
            
            OnLineReceived?.Invoke(line);
        }
    }

    public void SendCommand(UserSettings settings)
    {
        string json = JsonConvert.SerializeObject(settings);

        Debug.Log($"UITE JSON: {json}");
        _stdin.WriteLine(json);
        _stdin.Flush();
    }
}
