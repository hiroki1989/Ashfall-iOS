"""Prepare a review-only Knox candidate; never overwrite the supplied source."""
import bpy, glob, os, shutil
bpy.ops.wm.read_factory_settings(use_empty=True)
source=glob.glob('D:/downloads2/knox/**/*.fbx',recursive=True)[0]
out='D:/Unity/AssetIntegration/KnoxCandidate'; os.makedirs(out,exist_ok=True)
bpy.ops.import_scene.fbx(filepath=source)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
rig.data.pose_position='REST'
for mod in mesh.modifiers:
 if mod.type=='ARMATURE': mod.show_viewport=False
bpy.context.view_layer.objects.active=mesh
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True)
mod=mesh.modifiers.new('Mobile polygon reduction','DECIMATE'); mod.ratio=.45
bpy.ops.object.modifier_apply(modifier=mod.name)
for mod in mesh.modifiers:
 if mod.type=='ARMATURE': mod.show_viewport=True
print('KNOX_CANDIDATE polygons',len(mesh.data.polygons),'bones',len(rig.data.bones),'groups',len(mesh.vertex_groups))
for f in glob.glob(os.path.dirname(source)+'/*.png'): shutil.copy2(f,out)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=out+'/Knox_candidate.fbx',use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY')
