import bpy
import datetime
import hashlib
import os
import math
import mathutils

FIXED_EXPORT_TIME = datetime.datetime(2026, 1, 1, 0, 0, 0)
EXPORTS = []


class _PinnedClock(datetime.datetime):
    @classmethod
    def now(cls, tz=None):
        return FIXED_EXPORT_TIME


class _PinnedDatetime:
    datetime = _PinnedClock


def _stable_hash(key):
    digest = hashlib.sha1(repr(key).encode("utf-8")).digest()
    return int.from_bytes(digest[:8], "little") & (2 ** 63 - 1)


def pin_fbx_exporter():
    """Make FBX bytes depend only on the scene: a fixed header time and object ids
    hashed from their keys instead of Python's per-process string hash."""
    from io_scene_fbx import export_fbx_bin, fbx_utils

    export_fbx_bin.datetime = _PinnedDatetime
    fbx_utils.hash = _stable_hash
    fbx_utils._keys_to_uuids.clear()
    fbx_utils._uuids_to_keys.clear()


def scene_stats():
    """Triangles, size in metres, floor height, and materials of every mesh in the scene."""
    tris = 0
    materials = set()
    lows = [float("inf")] * 3
    highs = [float("-inf")] * 3
    # bound_box follows the armature's current action frame; size and floor describe the bind pose.
    posed = [obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE' and obj.data.pose_position != 'REST']
    for armature in posed:
        armature.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        mesh = obj.data
        mesh.calc_loop_triangles()
        tris += len(mesh.loop_triangles)
        for slot in obj.material_slots:
            if slot.material is not None:
                materials.add(slot.material.name)
        for corner in obj.bound_box:
            point = obj.matrix_world @ mathutils.Vector(corner)
            for axis in range(3):
                lows[axis] = min(lows[axis], point[axis])
                highs[axis] = max(highs[axis], point[axis])
    for armature in posed:
        armature.data.pose_position = 'POSE'
    bpy.context.view_layer.update()
    if tris == 0:
        lows = highs = [0.0, 0.0, 0.0]
    return {
        "tris": tris,
        "width": round(highs[0] - lows[0], 4),
        "depth": round(highs[1] - lows[1], 4),
        "height": round(highs[2] - lows[2], 4),
        "floor": round(lows[2], 4),
        "centerX": round((highs[0] + lows[0]) * 0.5, 4),
        "centerY": round((highs[1] + lows[1]) * 0.5, 4),
        "materials": sorted(materials),
    }

def reset_scene():
    """Wipes all objects, meshes, and materials in the current blend context."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # Remove any leftover orphan data
    for block in bpy.data.meshes:
        bpy.data.meshes.remove(block, do_unlink=True)
    for block in bpy.data.materials:
        bpy.data.materials.remove(block, do_unlink=True)

def get_or_create_material(name, base_color=(0.8, 0.8, 0.8, 1.0), metallic=0.0, roughness=0.5, emission=0.0, emission_color=None):
    """Creates or retrieves a Principled BSDF material with PBR properties."""
    if name in bpy.data.materials:
        mat = bpy.data.materials[name]
    else:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf is not None:
        # Base Color
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = base_color
        # Metallic
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        # Roughness
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if emission > 0:
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission_color or base_color
    return mat

def assign_material(obj, mat):
    """Assigns a material to the object's active material slot."""
    if obj is None or mat is None:
        return
    if len(obj.data.materials) == 0:
        obj.data.materials.append(mat)
    else:
        obj.data.materials[0] = mat

def create_box(name, location, dimensions, material=None, rotation=(0, 0, 0), bevel_radius=0.0):
    """Creates a rectangular box primitive with dimensions (dx, dy, dz)."""
    # Blender primitive_cube_add size=1 creates 1x1x1 cube centered at (0,0,0)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    if material is not None:
        assign_material(obj, material)
        
    if bevel_radius > 0.001:
        mod = obj.modifiers.new(name="Bevel", type='BEVEL')
        mod.width = bevel_radius
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier="Bevel")
        
    return obj

def create_cylinder(name, location, radius, depth, vertices=16, material=None, rotation=(0, 0, 0)):
    """Creates a cylinder primitive."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, 
        radius=radius, 
        depth=depth, 
        location=location, 
        rotation=rotation
    )
    obj = bpy.context.active_object
    obj.name = name
    if material is not None:
        assign_material(obj, material)
    return obj

def create_cone(name, location, radius1, radius2, depth, vertices=16, material=None, rotation=(0, 0, 0)):
    """Creates a cone or truncated cone."""
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius1,
        radius2=radius2,
        depth=depth,
        location=location,
        rotation=rotation
    )
    obj = bpy.context.active_object
    obj.name = name
    if material is not None:
        assign_material(obj, material)
    return obj

def create_sphere(name, location, radius, segments=16, rings=12, material=None):
    """Creates a UV sphere."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=radius,
        location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    canonical_faces(obj)
    bpy.ops.object.shade_smooth()
    if material is not None:
        assign_material(obj, material)
    return obj

def canonical_faces(obj):
    """Blender's UV sphere comes out with its faces in a different order each run, which
    reshuffles the unwrap and the FBX bytes. Rebuild it with faces in a fixed order."""
    mesh = obj.data
    verts = [v.co.copy() for v in mesh.vertices]
    faces = []
    for poly in mesh.polygons:
        ring = list(poly.vertices)
        start = ring.index(min(ring))
        faces.append((tuple(ring[start:] + ring[:start]), poly.material_index))
    faces.sort(key=lambda face: face[0])
    mesh.clear_geometry()
    mesh.from_pydata(verts, [], [face[0] for face in faces])
    for poly, (_ring, material_index) in zip(mesh.polygons, faces):
        poly.material_index = material_index
    mesh.update()


def join_objects(obj_list, final_name):
    """Joins a list of mesh objects into one single object with all material slots intact."""
    valid_objs = [o for o in obj_list if o is not None and o.type == 'MESH']
    if not valid_objs:
        return None
    bpy.ops.object.select_all(action='DESELECT')
    for o in valid_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = valid_objs[0]
    bpy.ops.object.join()
    final_obj = bpy.context.active_object
    final_obj.name = final_name
    return final_obj

def set_origin_to_bottom(obj):
    """Sets the object's origin to its lowest Z boundary point (ground level)."""
    if obj is None or obj.type != 'MESH':
        return
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    
    # Calculate bounding box in world space
    bbox = [obj.matrix_world @ mathutils.Vector(corner) for corner in obj.bound_box]
    min_z = min(v.z for v in bbox)
    
    # Shift geometry so that lowest Z aligns with world 0
    bpy.context.scene.cursor.location = (obj.location.x, obj.location.y, min_z)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    obj.location.z -= min_z
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

def unwrap_mesh(obj):
    """Smart-project a UV set so the mesh is not exported with empty coordinates."""
    if obj is None or obj.type != 'MESH':
        return
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.151917, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')


def prepare_character(mesh):
    """UV the joined body and skin it to the shared humanoid armature."""
    unwrap_mesh(mesh)
    attach_humanoid(mesh)


def attach_humanoid(mesh):
    from character_rig import clips_for, humanoid_bones

    if mesh is None:
        return None
    armature = bpy.data.armatures.new("Humanoid")
    arm_obj = bpy.data.objects.new(mesh.name + "_Rig", armature)
    bpy.context.scene.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')
    created = {}
    for name, parent, head, tail in humanoid_bones():
        bone = armature.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.use_connect = False
        created[name] = bone
    for name, parent, _head, _tail in humanoid_bones():
        if parent is not None:
            created[name].parent = created[parent]
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.object.select_all(action='DESELECT')
    mesh.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    _key_clips(arm_obj, clips_for(mesh.name))
    return arm_obj


def _key_clips(arm_obj, clips):
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='POSE')
    if arm_obj.animation_data is None:
        arm_obj.animation_data_create()
    for clip_name, keys in clips.items():
        action = bpy.data.actions.new(clip_name)
        arm_obj.animation_data.action = action
        action.use_fake_user = True
        for bone_name, frame, rotation in keys:
            bone = arm_obj.pose.bones.get(bone_name)
            if bone is None:
                continue
            bone.rotation_mode = 'XYZ'
            bone.rotation_euler = (
                math.radians(rotation[0]),
                math.radians(rotation[1]),
                math.radians(rotation[2]),
            )
            bone.keyframe_insert(data_path="rotation_euler", frame=frame)
    bpy.ops.object.mode_set(mode='OBJECT')


def export_fbx(output_filepath, animated=False, texture_size=None, unwrap=True):
    """Exports the scene to an FBX file configured for Unity's coordinate system."""
    os.makedirs(os.path.dirname(output_filepath), exist_ok=True)
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'MESH' and not animated and unwrap:
            unwrap_mesh(obj)
    import vertex_ao
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'MESH':
            vertex_ao.bake(obj)
    bpy.ops.object.select_all(action='SELECT')
    stats = scene_stats()
    pin_fbx_exporter()
    bpy.ops.export_scene.fbx(
        filepath=output_filepath,
        use_selection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH', 'ARMATURE'} if animated else {'MESH'},
        add_leaf_bones=False,
        bake_anim=animated,
        bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0,
        colors_type='SRGB',
    )
    from texture_set import SIZE, model_meta_text, write_set
    write_set(output_filepath, texture_size or SIZE)
    if not os.path.isfile(output_filepath + ".meta"):
        with open(output_filepath + ".meta", "w", encoding="utf-8", newline="\n") as handle:
            handle.write(model_meta_text(output_filepath))
    stats["path"] = output_filepath
    EXPORTS.append(stats)
    print(f"[Blender] Successfully exported: {output_filepath}")
