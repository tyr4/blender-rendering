import os
import json
import re
import sys
import traceback
import datetime

import bpy
from PIL import Image, ImageDraw
import math

bpy_package_dir = os.path.dirname(bpy.__file__)
addons_core_path = os.path.join(bpy_package_dir, "scripts", "addons_core")

if addons_core_path not in sys.path:
    sys.path.append(addons_core_path)
    
import io_scene_fbx.import_fbx as import_fbx_module

_original_blen_read_light = import_fbx_module.blen_read_light

def patched_blen_read_light(fbx_tmpl, fbx_obj, settings):
    try:
        return _original_blen_read_light(fbx_tmpl, fbx_obj, settings)
    except AttributeError as e:
        if "cast_shadow" in str(e):
            debug_log(f"Skipping known FBX light-import bug: {e}")
            return None
        raise

import_fbx_module.blen_read_light = patched_blen_read_light

has_loaded_scene = False

def debug_log(*args, flush = True):
    with open("D:/debug_log.txt", "a") as f:
        f.write(f"{datetime.datetime.now()} - {args}\n\n")
        
    # print(args, flush=flush)

def _print_objects():
    for obj in bpy.data.objects:
        debug_log(f"{obj.name} | users: {obj.users} | fake_user: {obj.use_fake_user}")

def _print_animations():
    anims = list(bpy.data.actions)

    for i, anim in enumerate(anims):
        debug_log(f"{i} - {anim.name}")
        
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
    
    bpy.ops.wm.open_mainfile(filepath=scene_path)
    debug_scene()

def debug_scene():
    debug_log("========== SCENE STATE ==========")
    debug_log(f"File: {bpy.data.filepath}")
    debug_log(f"Scene: {bpy.context.scene.name}")

    for obj in bpy.data.objects:
        debug_log(
            f"{obj.name} | "
            f"location={tuple(obj.location)} | "
            f"rotation={tuple(obj.rotation_euler)} | "
            f"scale={tuple(obj.scale)} | "
            f"parent={obj.parent.name if obj.parent else None}"
        )
        
def get_all_object_data(settings: dict):
    data = {"camera_orthographic_scale": 0, 
            "camera_position": [],
            "starting_rotation": [],
            "parent_object_position": [],
            "parent_object_rotation": [],
            "reposition_object_position": [],
            "reposition_object_rotation": []
            }
    
    camera = get_camera()
    reposition = get_reposition_object()
    parent = get_main_parent_object()
    starting_rotation = settings.get("starting_rotation")
        
    data["camera_orthographic_scale"] = camera.data.ortho_scale
    
    # cast because they are mathutils.Vector
    data["camera_position"] = list(camera.location)
    data["starting_rotation"] = list(starting_rotation) if starting_rotation is not None else [0.0, 0.0, 0.0]
    
    data["parent_object_position"] = list(parent.location)
    data["parent_object_rotation"] = [math.degrees(a) for a in parent.rotation_euler]

    data["reposition_object_position"] = list(reposition.location)
    data["reposition_object_rotation"] = [math.degrees(a) for a in reposition.rotation_euler]
    
    return data

def load_fbx_model(scene_path: str):
    # has_loaded_fbx = True
    debug_log("uite ba dau load la fbx")
    debug_log(scene_path)
    
    result = bpy.ops.import_scene.fbx('EXEC_DEFAULT', filepath=scene_path)
    debug_log(f"am {result}")
    
    parent_obj = get_reposition_object()
    armature = get_armature_object()
    armature.parent = parent_obj
    
    return result

def get_armature_object():
    armatures = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']

    return armatures[0]

def delete_armature_object():
    reposition = get_reposition_object()

    def collect_descendants(obj):
        descendants = []
        for child in obj.children:
            descendants.append(child)
            descendants.extend(collect_descendants(child))
        return descendants

    to_delete = collect_descendants(reposition)

    for obj in to_delete:
        debug_log(f"deleting: {obj.name}")
        bpy.data.objects.remove(obj, do_unlink=True)

    # make sure leftover animations can be deleted
    for action in bpy.data.actions:
        action.use_fake_user = False
    
    # delete orphaned objects
    purged = bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=False, do_recursive=True)
    debug_log(f"purged: {purged}")

def get_camera():
    return bpy.data.objects["Camera"]

def get_reposition_object():
    return bpy.data.objects["Reposition"]

def get_main_parent_object():
    return bpy.data.objects["Parent Object"]

def change_object_rotation(obj, vector3_rotation: tuple):
    debug_log(
        f"{obj.name} | "
        f"local_loc={tuple(obj.location)} | "
        f"local_rot={tuple(obj.rotation_euler)} | "
        f"world_loc={tuple(obj.matrix_world.translation)} | "
        f"world_matrix={tuple(obj.matrix_world)}"
    )
    
    rotation_x, rotation_y, rotation_z = vector3_rotation

    debug_log("before obj rotation:", obj, obj.rotation_euler.x, obj.rotation_euler.y, obj.rotation_euler.z, vector3_rotation)

    obj.rotation_euler.x = math.radians(rotation_x)
    obj.rotation_euler.y = math.radians(rotation_y)
    obj.rotation_euler.z = math.radians(rotation_z)

    debug_log("after obj rotation: ", obj, obj.rotation_euler.x, obj.rotation_euler.y, obj.rotation_euler.z, vector3_rotation)

def change_object_position(obj, vector3_position: tuple):
    debug_log(
        f"{obj.name} | "
        f"local_loc={tuple(obj.location)} | "
        f"local_rot={tuple(obj.rotation_euler)} | "
        f"world_loc={tuple(obj.matrix_world.translation)} | "
        f"world_matrix={tuple(obj.matrix_world)}"
    )

    debug_log("before obj location: ", obj, obj.location, vector3_position)
    obj.location = vector3_position
    debug_log("after obj location: ", obj, obj.location, vector3_position)

def change_camera_orthographic_size(size: float):
    camera = get_camera()

    camera.data.ortho_scale = size

def init_scene(settings: dict, load_fbx: bool = False):
    load_scene(settings["scene_path"])

    if settings["fbx_path"] and load_fbx:
        load_fbx_model(settings["fbx_path"])

        parent_obj = get_reposition_object()
        armature = get_armature_object()

        armature.parent = parent_obj

    bpy.context.scene.camera = get_camera()

def render_animation(settings: dict, anim_index: int) -> int:
    clean_anim_files(settings)
    
    armature = get_armature_object()
    target_action = bpy.data.actions[anim_index]

    anim_data = armature.animation_data_create()
    anim_data.action = target_action
    
    suitable_slots = anim_data.action_suitable_slots
    if len(suitable_slots) > 0:
        anim_data.action_slot = suitable_slots[0]
    else:
        debug_log(f"Action '{target_action.name}' has no suitable slots, creating one")
        new_slot = target_action.slots.new(id_type='OBJECT', name=target_action.name)
        anim_data.action_slot = new_slot

    start, end = target_action.frame_range
    scene = bpy.context.scene
    scene.frame_start = int(start)
    scene.frame_end = int(end)
    actual_frames = scene.frame_end - scene.frame_start + 1

    scene.render.filepath = settings["render_temp_output_path"] + settings["render_temp_output_name"]
    scene.render.image_settings.file_format = "PNG"

    # apply_settings_to_scene(settings)

    bpy.ops.render.render(use_viewport=True, write_still=True, animation=True)
    debug_log(start, end)
    
    return actual_frames

def clean_anim_files(settings: dict):
    path = settings["render_temp_output_path"]
    start_name = settings["render_temp_output_name"]

    files = sorted(f for f in os.listdir(path) if (f.startswith(start_name)))

    for file in files:
        os.remove(path + file)

def save_spritesheet(settings: dict, output_name: str, total_frames: int):
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

    output_path = os.path.join(settings["spritesheet_output_path"], output_name)
    sheet.save(output_path)
    
    return {"output_path": output_path,
            "columns": columns,
            "rows": rows,
            "frame_count": total_frames
           } 

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
        debug_log("Warning: anim_index given but no armature found, rendering scene as-is", flush=True)
        
    scene = bpy.context.scene
    scene.frame_current = frame
   
    filepath = settings["render_temp_output_path"] + settings["render_temp_output_name"] + "static"
    scene.render.filepath = filepath

    # apply_settings_to_scene(settings)

    bpy.ops.render.render(use_viewport=True, write_still=True, animation=False)
    add_borders(scene.render.filepath + ".png", scene.render.filepath + "_border.png")

    debug_log(f"Rendered frame")
    return filepath + "_border.png"

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

def mat4_mul_point(matrix_world, point):
    """
    matrix_world: Blender's matrix_world (still a bpy/mathutils object,
                   but we only ever READ from it via indexing - no
                   Vector/Matrix construction required)
    point: (x, y, z) tuple
    """
    x, y, z = point
    row0 = matrix_world[0]
    row1 = matrix_world[1]
    row2 = matrix_world[2]

    out_x = row0[0]*x + row0[1]*y + row0[2]*z + row0[3]
    out_y = row1[0]*x + row1[1]*y + row1[2]*z + row1[3]
    out_z = row2[0]*x + row2[1]*y + row2[2]*z + row2[3]

    return (out_x, out_y, out_z)


def mat4_mul_direction(matrix_world, direction):
    """Same as above but ignores translation - for rotating a direction vector."""
    x, y, z = direction
    row0 = matrix_world[0]
    row1 = matrix_world[1]
    row2 = matrix_world[2]

    out_x = row0[0]*x + row0[1]*y + row0[2]*z
    out_y = row1[0]*x + row1[1]*y + row1[2]*z
    out_z = row2[0]*x + row2[1]*y + row2[2]*z

    return (out_x, out_y, out_z)


def mat4_invert(matrix_world):
    return matrix_world.inverted()


def get_world_bounds_center(root_obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    corners = []

    def collect(obj):
        eval_obj = obj.evaluated_get(depsgraph)
        if eval_obj.type == 'MESH' and eval_obj.data:
            for c in eval_obj.bound_box:
                corners.append(mat4_mul_point(eval_obj.matrix_world, tuple(c)))
        for child in obj.children:
            collect(child)

    collect(root_obj)

    if not corners:
        t = root_obj.matrix_world.translation
        return (t[0], t[1], t[2])

    xs = [c[0] for c in corners]
    ys = [c[1] for c in corners]
    zs = [c[2] for c in corners]

    return (
        (min(xs) + max(xs)) / 2,
        (min(ys) + max(ys)) / 2,
        (min(zs) + max(zs)) / 2,
    )


def center_object_to_camera(root_obj, camera, move_obj):
    bounds_center = get_world_bounds_center(root_obj)

    cam_inv = mat4_invert(camera.matrix_world)
    cx, cy, cz = mat4_mul_point(cam_inv, bounds_center)

    offset_cam_space = (-cx, -cy, 0.0)
    ox, oy, oz = mat4_mul_direction(camera.matrix_world, offset_cam_space)

    world_pos = move_obj.matrix_world.translation
    move_obj.matrix_world.translation = (
        world_pos[0] + ox,
        world_pos[1] + oy,
        world_pos[2] + oz,
    )

    bpy.context.view_layer.update()

def sanitize_filename(name: str) -> str:
    return re.sub(r'[\\/:*?"<>|]', '_', name)

def run_calculator(settings):
    # init_scene(settings)
    apply_settings_to_scene(settings)

    step = 360 / settings["directions"]
    obj = get_main_parent_object()
    anims = list(bpy.data.actions)

    for anim in range(len(anims)):
        for i in range(settings["directions"]):
            parent_rotation = (0, 0, i * step)
            final_rotation = tuple(a + b for a, b in zip(settings["starting_rotation"], parent_rotation))
            debug_log(f"degrees {i*step} rotation {final_rotation}")
            
            change_object_rotation(obj, vector3_rotation=final_rotation)
            output_name = sanitize_filename(anims[anim].name) + f"_{int(i * step)}.png"

            frames = render_animation(settings=settings, anim_index=anim)
            save_spritesheet(settings=settings, output_name=output_name, total_frames=frames)

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
    camera_pos = settings["camera_position"]
    camera_ortho_scale = settings["camera_orthographic_scale"]

    if camera_pos is not None:
        change_object_position(camera, camera_pos)
    if camera_ortho_scale is not None:
        change_camera_orthographic_size(camera_ortho_scale)

    # reposition object settings
    reposition_position = settings["reposition_object_position"]
    reposition_rotation = settings["reposition_object_rotation"]

    debug_log("reposition obj", reposition_position, reposition_rotation)
    if reposition_position:
        change_object_position(reposition_obj, reposition_position)
    if reposition_rotation:
        change_object_rotation(reposition_obj, reposition_rotation)

    # parent object settings
    parent_position = settings["parent_object_position"]
    parent_rotation = settings["parent_object_rotation"]

    debug_log("parent obj", parent_position, parent_rotation)
    if parent_position:
        change_object_position(parent_obj, parent_position)
    if parent_rotation:
        change_object_rotation(parent_obj, parent_rotation)

    # render settings
    scene.render.resolution_x = settings["resolution_x"]
    scene.render.resolution_y = settings["resolution_y"]
    scene.render.resolution_percentage = 100

    bpy.context.view_layer.update()

def send_response(result: dict, settings: dict = None):
    if settings and "request_id" in settings:
        result["request_id"] = settings["request_id"]

    print("RESPONSE_END_STDOUT" + json.dumps(result), flush=True)

    return result

def handle_command(settings: dict):
    cmd = settings["current_command"]
    valid = validate_settings(settings)

    if valid["status"] != "ok":
        return send_response({"message": valid["message"]}, settings)

    try:
        debug_log(f"entering {cmd} with:\n {settings}\n", flush=True)

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
            
        elif cmd == "delete_armature":
            delete_armature_object()

        # global scene commands
        elif cmd == "apply_settings":
            apply_settings_to_scene(settings)
            
        elif cmd == "render_single_frame":
            filepath = render_single_frame(settings)
            return send_response({"status": "success", "message": filepath}, settings)
        
        elif cmd == "center_to_camera":
            armature = get_armature_object()
            camera = get_camera()
            reposition_obj = get_reposition_object()
            center_object_to_camera(armature, camera, reposition_obj)
            
        elif cmd == "get_all_object_data":
            result = get_all_object_data(settings)
            debug_log(result)
            
            return send_response({"status": "success", "message": result}, settings)
            
        elif cmd == "generate_all_spritesheets":
            run_calculator(settings)
            return send_response({"status": "success", "message": "generated all spritesheets"}, settings)
            
        elif cmd == "generate_selected_spritesheets":
            # key anim string, value enabled/disabled
            anim_dict: dict = settings["animation_dict"]
            debug_log(anim_dict)
            
            for i, (key, value) in enumerate(anim_dict.items()):
                if value is True:
                    render_animation(settings, anim_index=i)

            return send_response({"status": "success", "message": "finished rendering selected animations"}, settings)
        
        elif cmd == "render_single_anim":
            animations = bpy.data.actions
            anim_index = settings["selected_anim"]
            output_name = sanitize_filename(animations[anim_index].name) + ".png"
            
            frames = render_animation(settings, anim_index=anim_index)
            spritesheet_data = save_spritesheet(settings, output_name, frames)
            clean_anim_files(settings)
            
            return send_response({"status": "success", "message": spritesheet_data}, settings)
            
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
            debug_log(f"RECEIVED JSON {settings}")
        except json.JSONDecodeError:
            # send_response({"status": "error", "message": "invalid json"})
            continue

        result = handle_command(settings)

# spritesheet_output_name = "spritesheet"

# data = bpy.data
# objects = list(data.objects)
settings_dict = {"current_command": "",
                 "scene_path": "D:\\Blender Stuff\\Scenes\\empty_scene.blend", # required
                 "fbx_path": "D:\\Blender Stuff\\Models\\robot\\episode_71.fbx",
                 "render_temp_output_path": "D:\\Blender Stuff\\Output\\robot_test\\", # required
                 "render_temp_output_name": "anim_", # required
                 "spritesheet_output_path": "D:\\Blender Stuff\\Output\\robot_test\\", # required
                 "directions": 4, # required
                 "resolution_x": 98,
                 "resolution_y": 98,
                 "camera_orthographic_scale": 5.7, # float
                 "camera_position": None, # Vector
                 "starting_rotation": (0, 0, 0), # Vector
                 "reposition_object_position": None, # Vector
                 "reposition_object_rotation": None, # Vector
                 "parent_object_position": (1, 0, 0),
                 "parent_object_rotation": None
                 }



if __name__ == "__main__":
    main()
    # init_scene(settings_dict, load_fbx=True)
    # render_animation(settings_dict, anim_index=1)
    # init_scene(settings_dict, load_fbx=True)
    # render_single_frame(settings_dict)