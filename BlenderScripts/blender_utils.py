import bpy
import os
import math
import mathutils

def reset_scene():
    """Wipes all objects, meshes, and materials in the current blend context."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # Remove any leftover orphan data
    for block in bpy.data.meshes:
        bpy.data.meshes.remove(block, do_unlink=True)
    for block in bpy.data.materials:
        bpy.data.materials.remove(block, do_unlink=True)

def get_or_create_material(name, base_color=(0.8, 0.8, 0.8, 1.0), metallic=0.0, roughness=0.5):
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
    bpy.ops.object.shade_smooth()
    if material is not None:
        assign_material(obj, material)
    return obj

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

def export_fbx(output_filepath):
    """Exports all scene meshes to an FBX file configured for Unity coordinate system."""
    os.makedirs(os.path.dirname(output_filepath), exist_ok=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(
        filepath=output_filepath,
        use_selection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH'}
    )
    print(f"[Blender] Successfully exported: {output_filepath}")
