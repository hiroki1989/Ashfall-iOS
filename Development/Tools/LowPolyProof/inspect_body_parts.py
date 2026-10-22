import bpy
from mathutils import Vector
from pathlib import Path
root=Path('D:/Unity/Ashfall-iOS')
source=root/'Assets/ArtSource/LowPolyProof/ArmRigCandidate/plain-body-arm-rig-candidate.blend'
out=root/'Assets/ArtSource/LowPolyProof/ArmRigSide'
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene
for obj in bpy.data.objects:
    if obj.type=='MESH':
        pts=[obj.matrix_world@v.co for v in obj.data.vertices]
        if pts:
            lo=Vector((min(p.x for p in pts),min(p.y for p in pts),min(p.z for p in pts)))
            hi=Vector((max(p.x for p in pts),max(p.y for p in pts),max(p.z for p in pts)))
            print('PART_BOUNDS',obj.name,tuple(round(v,3) for v in lo),tuple(round(v,3) for v in hi),len(obj.data.polygons))
        obj.hide_render=obj.name!='plain_body'
cam=bpy.data.cameras.new('Body part QA'); camera=bpy.data.objects.new('Body part QA',cam);scene.collection.objects.link(camera)
camera.location=(0,-4,0.91);camera.rotation_euler=(Vector((0,0,.91))-camera.location).to_track_quat('-Z','Y').to_euler();cam.type='ORTHO';cam.ortho_scale=2.05;scene.camera=camera
scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/'qa-plain-body-only.png')
bpy.ops.render.render(write_still=True)
