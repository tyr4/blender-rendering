import os
import json
import sys
import traceback
import datetime


import bpy
from PIL import Image, ImageDraw
import math

has_loaded_scene = False

def debug_log(msg, flush = True):
    with open("D:/debug_log.txt", "a") as f:
        f.write(f"{datetime.datetime.now()} - {msg}\n")
        
    print(msg, flush=flush)

def _print_objects():
    for obj in bpy.data.objects:
        debug_log(f"{obj.name} | users: {obj.users} | fake_user: {obj.use_fake_user}")

def _print_animations():
    anims = list(bpy.data.actions)

    for i, anim in enumerate(anims):
        print(f"{i} - {anim.name}")
        
def return_objects() -> dict:
    data = {"objects": []}

    for obj in bpy.data.objects:
        data["objects"].append(obj.name)
    
    return data
    

def return_animations() -> dict:
    data = {"animations": []}
    
    for anim in bpy.data.actions:
        data["animations"].append(anim.name)
        
    return data

def load_scene(scene_path: str):
    global has_loaded_scene
    has_loaded_scene = True
    
    return bpy.ops.wm.open_mainfile(filepath=scene_path)

def load_fbx_model(scene_path: str):
    # has_loaded_fbx = True
    debug_log("uite ba dau load la fbx")
    debug_log(scene_path)
    
    result = bpy.ops.import_scene.fbx('EXEC_DEFAULT', filepath=scene_path)
    debug_log(f"am {result}")
    
    return result

def get_armature_object():
    armatures = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']

    return armatures[0]

def get_camera():
    return bpy.data.objects["Camera"]

def get_reposition_object():
    return bpy.data.objects["Reposition"]

def get_main_parent_object():
    return bpy.data.objects["Parent Object"]

def change_object_rotation(obj, vector3_rotation: tuple):
    rotation_x, rotation_y, rotation_z = vector3_rotation

    obj.rotation_euler.x = math.radians(rotation_x)
    obj.rotation_euler.y = math.radians(rotation_y)
    obj.rotation_euler.z = math.radians(rotation_z)

def change_object_position(obj, vector3_position: tuple):
    obj.location = vector3_position

def change_camera_orthographic_size(size: float):
    camera = get_camera()

    camera.data.ortho_scale = size

def change_camera_shift_y(shift: float):
    camera = get_camera()

    camera.data.shift_y = shift

def init_scene(settings: dict, load_fbx: bool = False):
    load_scene(settings["scene_path"])

    if settings["fbx_path"] and load_fbx:
        load_fbx_model(settings["fbx_path"])

        parent_obj = get_reposition_object()
        armature = get_armature_object()

        armature.parent = parent_obj

    bpy.context.scene.camera = get_camera()

def render_animation(settings: dict, anim_index: int):
    armature = get_armature_object()
    target_action = bpy.data.actions[anim_index]

    anim_data = armature.animation_data_create()
    anim_data.action = target_action
    anim_data.action_slot = anim_data.action_suitable_slots[0]

    start, end = target_action.frame_range
    scene = bpy.context.scene
    scene.frame_start = int(start)
    scene.frame_end = int(end)

    scene.render.filepath = settings["render_temp_output_path"] + settings["render_temp_output_name"]
    scene.render.image_settings.file_format = "PNG"

    apply_settings_to_scene(settings)

    bpy.ops.render.render(use_viewport=True, write_still=True, animation=True)

    print(start, end)

def clean_anim_files(settings: dict):
    path = settings["render_temp_output_path"]
    start_name = settings["render_temp_output_name"]

    files = sorted(f for f in os.listdir(path) if (f.startswith(start_name)))

    for file in files:
        os.remove(path + file)

def save_spritesheet(settings: dict, output_name: str):
    render_path = settings["render_temp_output_path"]
    render_name = settings["render_temp_output_name"]

    files = sorted(f for f in os.listdir(render_path) if (f.startswith(render_name)))
    images = [Image.open(os.path.join(render_path, f)) for f in files]

    frame_width, frame_height = images[0].size

    columns = math.ceil(math.sqrt(len(images)))
    rows = math.ceil(len(images) / columns)

    sheet = Image.new("RGBA", (columns * frame_width, rows * frame_height))

    for i, img in enumerate(images):
        x = (i % columns) * frame_width
        y = (i // columns) * frame_height
        sheet.paste(img, (x, y))

    sheet.save(os.path.join(settings["spritesheet_output_path"], output_name))

def render_single_frame(settings: dict,
                        anim_index: int | None = None,
                        frame: int = 1) -> str:
    armatures = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']
    armature = armatures[0] if armatures else None

    if armature is not None and anim_index is not None:
        target_action = bpy.data.actions[anim_index]
        anim_data = armature.animation_data_create()
        anim_data.action = target_action
        anim_data.action_slot = anim_data.action_suitable_slots[0]
    elif armature is None:
        print("Warning: anim_index given but no armature found, rendering scene as-is", flush=True)
        
    scene = bpy.context.scene
    scene.frame_current = frame

    filepath = settings["render_temp_output_path"] + settings["render_temp_output_name"] + "static"
    scene.render.filepath = filepath

    apply_settings_to_scene(settings)

    bpy.ops.render.render(use_viewport=True, write_still=True, animation=False)
    add_borders(scene.render.filepath + ".png", scene.render.filepath + "_border.png")

    print(f"Rendered frame")
    return filepath + ".png"

def add_borders(image_path: str, output_path: str, border_size: int = 2, border_color: tuple = (255, 0, 0, 255)):
    img = Image.open(image_path)
    draw = ImageDraw.Draw(img)

    width, height = img.size

    draw.rectangle(
        [(0, 0), (width - 1, height - 1)],
        outline=border_color,
        width=border_size
    )

    img.save(output_path)

def run_calculator(settings):
    # init_scene(settings)
    apply_settings_to_scene(settings)

    step = 360 / settings["directions"]
    obj = get_main_parent_object()
    anims = list(bpy.data.actions)

    for anim in range(len(anims)):
        for i in range(settings["directions"]):
            parent_rotation = (0, 0, i * step)
            final_rotation = settings["starting_rotation"] + parent_rotation

            change_object_rotation(obj, vector3_rotation=final_rotation)
            output_name = anims[anim].name + f"_{int(i * step)}.png"

            render_animation(settings=settings, anim_index=anim)
            save_spritesheet(settings=settings, output_name=output_name)

        clean_anim_files(settings)

def validate_settings(settings: dict):
    required_fields = ("scene_path", "render_temp_output_path", "render_temp_output_name", "spritesheet_output_path", "directions")
    missing_fields = []

    for key, value in settings.items():
        if not settings[key] and key in required_fields:
            missing_fields.append(key)

    if missing_fields:
        return {"status": "error", "message": f"missing required fields: {missing_fields}"}

    return {"status": "ok"}

def apply_settings_to_scene(settings: dict):
    scene = bpy.context.scene
    camera = get_camera()
    parent_obj = get_main_parent_object()
    reposition_obj = get_reposition_object()

    # camera settings
    shift_y = settings["camera_shift_y"]
    camera_pos = settings["camera_position"]
    camera_ortho_scale = settings["camera_orthographic_scale"]

    if shift_y is not None:
        change_camera_shift_y(settings["camera_shift_y"])
    if camera_pos is not None:
        change_object_position(camera, camera_pos)
    if camera_ortho_scale is not None:
        change_camera_orthographic_size(camera_ortho_scale)

    # reposition object settings
    reposition_position = settings["reposition_object_position"]
    reposition_rotation = settings["reposition_object_rotation"]

    if reposition_position:
        change_object_position(reposition_obj, reposition_position)
    if reposition_rotation:
        change_object_rotation(reposition_obj, reposition_rotation)

    # parent object settings
    parent_position = settings["parent_object_position"]
    parent_rotation = settings["parent_object_rotation"]

    if parent_position:
        change_object_position(parent_obj, parent_position)
    if parent_rotation:
        change_object_rotation(parent_obj, parent_rotation)

    # render settings
    scene.render.resolution_x = settings["resolution_x"]
    scene.render.resolution_y = settings["resolution_y"]
    scene.render.resolution_percentage = 100

def shutdown():
    print("am intrat in shutdown")
    exit(0)

def send_response(result: dict, settings: dict = None):
    if settings and "request_id" in settings:
        result["request_id"] = settings["request_id"]

    debug_log("RESPONSE_END_STDOUT" + json.dumps(result), flush=True)

    return result

def handle_command(settings: dict):
    cmd = settings["current_command"]
    valid = validate_settings(settings)

    if valid["status"] != "ok":
        return send_response({"message": valid["message"]}, settings)

    try:
        print(f"entering {cmd} with:\n {settings}\n", flush=True)

        # data commands
        if cmd == "get_fbx_armatures":
            rez = return_animations()
            return send_response({"status": "success", "message": rez}, settings)
        
        elif cmd == "get_objects":
            rez = return_objects()
            return send_response({"status": "success", "message": rez}, settings)

        # scene commands
        elif cmd == "load_scene":
            init_scene(settings)
        
        # dont think this is ever used
        elif cmd == "load_scene_no_fbx":
            init_scene(settings, load_fbx=False)

        elif cmd == "load_fbx":
            if has_loaded_scene:
                load_fbx_model(settings["fbx_path"])
                debug_log("allegedly am dat load la fbx")
            else:
                return send_response({"status": "error", "message": f"scene has not been loaded"}, settings)

        # global scene commands
        elif cmd == "apply_settings":
            apply_settings_to_scene(settings)
            
        elif cmd == "render_single_frame":
            filepath = render_single_frame(settings)
            return send_response({"status": "success", "message": filepath}, settings)

        elif cmd == "shutdown":
            shutdown()
            
        else:
            return send_response({"status": "error", "message": f"invalid command: {cmd}"}, settings)

    except KeyError as e:
        return send_response({"status": "error", "message": f"missing required field: {e}"}, settings)
    except Exception as e:
        return send_response({"status": "error", "message": str(e), "traceback": traceback.format_exc()}, settings)

    return send_response({"status": f"finished {cmd}"}, settings)

# main entry point when the script is called
def main():
    send_response({"status": "ok"})
    # args = parse_args()

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            settings = json.loads(line)
        except json.JSONDecodeError:
            # send_response({"status": "error", "message": "invalid json"})
            continue

        result = handle_command(settings)

spritesheet_output_name = "spritesheet"

data = bpy.data
objects = list(data.objects)
# settings_dict = {"current_command": "",
#                  "scene_path": "/home/mihai/Blender Stuff/Scenes/spider_test.blend", # required
#                  "fbx_path": "",
#                  "render_temp_output_path": "/home/mihai/Blender Stuff/Output/spider_test/", # required
#                  "render_temp_output_name": "anim_", # required
#                  "spritesheet_output_path": "/home/mihai/Blender Stuff/Output/spider_test/", # required
#                  "directions": 4, # required
#                  "resolution_x": 128,
#                  "resolution_y": 128,
#                  "camera_orthographic_scale": None, # float
#                  "camera_shift_y": None, # float
#                  "camera_position": None, # Vector
#                  "starting_rotation": Vector((0, 0, 0)), # Vector
#                  "reposition_object_position": None, # Vector
#                  "reposition_object_rotation": None # Vector
#                  }

if __name__ == "__main__":
    main()