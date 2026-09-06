using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class SettingsManager
{
    private readonly string _path = Application.persistentDataPath + "/settings.json";

    public void Save(UserSettings settings)
    {
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(_path, json);
    }

    public UserSettings Load()
    {
        if (!File.Exists(_path))
        {
            Debug.LogWarning("No settings file found at " + _path + ", creating a new one");
            
            var settings = new UserSettings
            {
                python_interpreter = @"D:\Unity Projects\blender-rendering-tool\blender-rendering-tool\.venv\Scripts\python.exe",
                python_file_path = @"D:\Unity Projects\blender-rendering-tool\blender-rendering-tool\Base Generator\main.py",
                // scene_path = @"D:\Blender Stuff\Scenes\empty_scene.blend",
                // fbx_path = @"D:\Blender Stuff\Models\robot\episode_71.fbx",
                // render_temp_output_path = "D:\\Blender Stuff\\Output\\robot_test\\",
                // render_temp_output_name = "anim_",
                // spritesheet_output_path = "D:\\Blender Stuff\\Output\\robot_test\\",
            };

            return settings;
        }
        
        string json = File.ReadAllText(_path);
        ConsoleLog.ProcessLog($"Loaded settings: {json}");
        
        return JsonConvert.DeserializeObject<UserSettings>(json);
    }
}
