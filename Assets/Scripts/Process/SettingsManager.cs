using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class SettingsManager
{
    private readonly string _path;
    public UserSettings settings { get; private set; }

    public SettingsManager()
    {
        _path = Application.persistentDataPath + "/settings.json";
        settings = Load(_path);
    }

    public void Save(string path)
    {
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    private UserSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("No settings file found at " + path);
            
            settings = new UserSettings
            {
                python_interpreter = @"D:\Unity Projects\blender-rendering-tool\blender-rendering-tool\.venv\Scripts\python.exe",
                python_file_path = @"D:\Unity Projects\blender-rendering-tool\blender-rendering-tool\Base Generator\main.py",
                scene_path = "D:\\Blender Stuff\\Scenes\\empty_scene.blend",
                fbx_path = "D:\\Blender Stuff\\Models\\robot\\episode_71.fbx",
                render_temp_output_path = "D:\\Blender Stuff\\Output\\robot_test\\",
                render_temp_output_name = "anim_",
                spritesheet_output_path = "D:\\Blender Stuff\\Output\\robot_test\\",
                camera_orthographic_scale = 5.7f,
                directions = 4,
                resolution_y = 98
            };

            return settings;
        }
        
        string json = File.ReadAllText(path);
        Debug.Log($"loaded {json}");
        
        return JsonConvert.DeserializeObject<UserSettings>(json);
    }
}
