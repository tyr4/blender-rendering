using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();
    
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
        
        // #if UNITY_EDITOR
        var settings = new SettingsManager(); // one time use for initializing the process
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
        // #else
        //     psi = new ProcessStartInfo
        //     {
        //         FileName = path,
        //         RedirectStandardInput = true,
        //         RedirectStandardOutput = true,
        //         RedirectStandardError = true,
        //         UseShellExecute = false,
        //         CreateNoWindow = true
        //     };
        // #endif
        
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

            string requestId = TryExtractRequestId(line);
            if (requestId != null && _pending.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(line);
            }
        }
    }

    private string TryExtractRequestId(string line)
    {
        try
        {
            var message = (string)Utils.GetJsonValue(line, "request_id");

            return message;
        }
        catch
        {
            return null; // not json
        }
    }

    public void SendCommand(UserSettings settings)
    {
        string json = JsonConvert.SerializeObject(settings);

        Debug.Log($"UITE JSON: {json}");
        _stdin.WriteLine(json);
        _stdin.Flush();
    }

    public Task<string> SendCommandAsync(UserSettings settings, TimeSpan? timeout = null)
    {
        settings.request_id = Guid.NewGuid().ToString();

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[settings.request_id] = tcs;

        string json = JsonConvert.SerializeObject(settings);

        _ = Task.Run(() =>
        {
            try
            {
                Debug.Log($"about to write {json}");
                _stdin.WriteLine(json);
                Debug.Log("wrote");
                
                _stdin.Flush();
                Debug.Log("flushed");
            }
            catch (Exception e)
            {
                _pending.TryRemove(settings.request_id, out _);
                tcs.TrySetException(e);
            }
        });
        
        if (timeout.HasValue)
        {
            _ = TimeoutAfter(settings.request_id, tcs, timeout.Value);
        }

        return tcs.Task;
    }

    private async Task TimeoutAfter(string requestId, TaskCompletionSource<string> tcs, TimeSpan timeout)
    {
        await Task.Delay(timeout);
        if (_pending.TryRemove(requestId, out var _))
        {
            tcs.TrySetException(new TimeoutException($"Python did not respond to request {requestId}"));
        }
    }
}
