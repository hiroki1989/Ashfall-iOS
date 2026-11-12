import bpy
from mathutils import Vector

base = 'D:/Unity/Ashfall-iOS/Assets/Resources/Characters'
out = 'D:/Unity/AssetIntegration/Iona'
for name in ('Iona', 'Briggs'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'{base}/{name}/{name}.fbx')
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x = 800
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    camera_data = bpy.data.cameras.new('Review camera')
    camera = bpy.data.objects.new('Review camera', camera_data)
    scene.collection.objects.link(camera)
    camera.location = (0, -6, 1.1)
    target = Vector((0, 0, 1.08))
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = 2.65
    scene.camera = camera
    scene.render.filepath = f'{out}/{name.lower()}-blender-rig-final.png'
    bpy.ops.render.render(write_still=True)
    print('REVIEW_RENDER', name, scene.render.filepath, flush=True)
