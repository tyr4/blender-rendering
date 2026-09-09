#nullable enable
using System.Collections.Generic;

[System.Serializable]
public class UserSettings
{
    public string request_id { get; set; } = "";
    public string current_command { get; set; } = "";
    public string python_interpreter { get; set; } = "";
    public string python_file_path { get; set; } = "";
    public string scene_path { get; set; } = "";
    public string fbx_path { get; set; } = "";
    public string render_temp_output_path { get; set; } = "";
    public string render_temp_output_name { get; set; } = "anim_";
    public string spritesheet_output_path { get; set; } = "";
    public int directions { get; set; } = 4;
    public int resolution_x { get; set; } = 128;
    public int resolution_y { get; set; } = 128;
    public float fps { get; set; } = 24f;
    public float? camera_orthographic_scale { get; set; }
    public float[]? camera_position { get; set; }
    public float[]? starting_rotation { get; set; }
    public float[]? parent_object_position { get; set; }
    public float[]? parent_object_rotation { get; set; } 
    public float[]? reposition_object_position { get; set; } 
    public float[]? reposition_object_rotation { get; set; }
    public Dictionary<string, bool> animation_dict { get; set; } = new();
    public int selected_anim { get; set; } = 0;
    
    public UserSettings() { }

    public UserSettings(UserSettings other)
    {
        request_id = other.request_id;
        current_command = other.current_command;
        python_interpreter = other.python_interpreter;
        python_file_path = other.python_file_path;
        scene_path = other.scene_path;
        fbx_path = other.fbx_path;
        render_temp_output_path = other.render_temp_output_path;
        render_temp_output_name = other.render_temp_output_name;
        spritesheet_output_path = other.spritesheet_output_path;
        directions = other.directions;
        resolution_x = other.resolution_x;
        resolution_y = other.resolution_y;
        fps = other.fps;
        camera_orthographic_scale = other.camera_orthographic_scale;
        camera_position = (float[]?)other.camera_position?.Clone();
        starting_rotation = (float[]?)other.starting_rotation?.Clone();
        parent_object_position = (float[]?)other.parent_object_position?.Clone();
        parent_object_rotation = (float[]?)other.parent_object_rotation?.Clone();
        reposition_object_position = (float[]?)other.reposition_object_position?.Clone();
        reposition_object_rotation = (float[]?)other.reposition_object_rotation?.Clone();
        animation_dict = new Dictionary<string, bool>(other.animation_dict);
        selected_anim = other.selected_anim;
    }
}