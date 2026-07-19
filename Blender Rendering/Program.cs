using System.Diagnostics;
using System.Text.Json;

class Program
{
    private static Process _process;
    private static StreamWriter _stdin;
    private static StreamReader _stdout;

    private static void StartPythonProcess(string pythonExe, string scriptPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
        
        _process = Process.Start(psi)!;
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine("[python stderr] " + e.Data);
        };
        _process.BeginErrorReadLine();

        string startLine = _stdout.ReadLine();
        Console.WriteLine($"piton in viata: {startLine}");
    }

    public static string SendCommand(object command)
    {
        var endToken = "RESPONSE_END_STDOUT";
        string json = JsonSerializer.Serialize(command);
        _stdin.WriteLine(json);
        _stdin.Flush();

        // var output = new List<string>();

        while (true)
        {
            string? line = _stdout.ReadLine();
            
            if (line == null)
            {
                Console.WriteLine("linie null bos");
                continue;                
            }

            Console.WriteLine(line);
            
            if (line.StartsWith(endToken))
            {
                return line.Substring(endToken.Length);
            }
        }
    }
    
    public static void Main(string[] args)
    {
        StartPythonProcess("/home/mihai/PycharmProjects/blender-stuff/.venv/bin/python", "/home/mihai/PycharmProjects/blender-stuff/main.py");
        
        var settings = new Settings();

        settings.current_command = "init_scene";
        string response = SendCommand(settings);
        Console.WriteLine("response: " + response);

        settings.current_command = "render_single_frame";
        response = SendCommand(settings);
        Console.WriteLine("response: " + response);
        
        settings.current_command = "shutdown";
        response = SendCommand(new {current_command = "shutdown"});
        Console.WriteLine("response: " + response);
        
        _process.WaitForExit(10000);
    }
}