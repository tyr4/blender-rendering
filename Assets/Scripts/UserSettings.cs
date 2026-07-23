[System.Serializable]
public class UserSettings
{
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
    public float? camera_orthographic_scale { get; set; } = 8f;
    public float? camera_shift_y { get; set; }
    public float[]? camera_position { get; set; }
    public float[] starting_rotation { get; set; } = { 0, 0, 0 };
    public float[]? parent_object_position { get; set; }
    public float[]? parent_object_rotation { get; set; } 
    public float[]? reposition_object_position { get; set; } 
    public float[]? reposition_object_rotation { get; set; } = { 0, 0, 0 };
}