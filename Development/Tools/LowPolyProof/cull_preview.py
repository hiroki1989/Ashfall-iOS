import bpy,bmesh
from mathutils import Vector
from pathlib import Path
root=Path('D:/Unity/Ashfall-iOS')
source=root/'Assets/ArtSource/LowPolyProof/ArmRigCandidate/plain-body-arm-rig-candidate.blend'
out=root/'Assets/ArtSource/LowPolyProof/ArmRigSide'
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene
for obj in bpy.data.objects:
    if obj.type=='MESH': obj.hide_render=obj.name!='plain_body'
body=next(o for o in bpy.data.objects if o.name=='plain_body')
# Blender is Z-up; retain torso and trousers while removing the scan's hanging arms.
bm=bmesh.new();bm.from_mesh(body.data)
removed=0
for face in list(bm.faces):
    c=face.calc_center_median(); z=c.z
    if .76<=z<=1.57:
        edge=.22 if z>=1.40 else .18 if z>=1.20 else .17 if z>=.95 else .19
        if abs(c.x)>edge:
            bm.faces.remove(face);removed+=1
bm.to_mesh(body.data);bm.free();body.data.update()
print('CULLED',removed,'faces remain',len(body.data.polygons))
cam=bpy.data.cameras.new('Cull QA');camera=bpy.data.objects.new('Cull QA',cam);scene.collection.objects.link(camera);cam.type='ORTHO';cam.ortho_scale=2.05;scene.camera=camera
for view,loc in [('front',(0,-4,.91)),('side',(4,0,.91))]:
    camera.location=loc;camera.rotation_euler=(Vector((0,0,.91))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/f'culled-tight-{view}.png');bpy.ops.render.render(write_still=True)
