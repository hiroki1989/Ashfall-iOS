import bpy, os, math, shutil
from mathutils import Vector, Matrix

base='D:/Unity/Ashfall-iOS/Assets/Resources/Characters'
backup='D:/Unity/SteamPortBackups/hub-npc-before-rig-20261111'
os.makedirs(backup, exist_ok=True)
for name in ('Iona','Briggs'):
    path=f'{base}/{name}/{name}.fbx'
    if not os.path.exists(f'{backup}/{name}.fbx'): shutil.copy2(path,f'{backup}/{name}.fbx')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # Always rebuild from the untouched source; repeated exports must not
    # accumulate arm deformation in the coat or sleeves.
    bpy.ops.import_scene.fbx(filepath=f'{backup}/{name}.fbx')
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    if len(meshes)!=1: raise RuntimeError(f'{name}: expected one mesh, got {len(meshes)}')
    mesh=meshes[0]
    bpy.context.view_layer.objects.active=mesh
    bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    pts=[v.co for v in mesh.data.vertices]
    z0=min(p.z for p in pts); h=max(p.z for p in pts)-z0
    print(name,'bounds',[(min(p[i] for p in pts),max(p[i] for p in pts)) for i in range(3)], flush=True)
    armdata=bpy.data.armatures.new(name+'Rig'); arm=bpy.data.objects.new(name+'Rig',armdata); bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm; arm.select_set(True); mesh.select_set(False)
    bpy.ops.object.mode_set(mode='EDIT')
    root=armdata.edit_bones.new('body'); root.head=(0,0,z0+.35*h); root.tail=(0,0,z0+.75*h)
    for sign,label in ((1,'L'),(-1,'R')):
        b=armdata.edit_bones.new('upper_arm_'+label); b.head=(sign*.115*h,0,z0+.755*h); b.tail=(sign*.22*h,0,z0+.65*h); b.parent=root
    bpy.ops.object.mode_set(mode='OBJECT')
    body=mesh.vertex_groups.new(name='body'); groups={s:mesh.vertex_groups.new(name='upper_arm_'+label) for s,label in ((1,'L'),(-1,'R'))}
    for v in mesh.data.vertices:
        p=v.co; x=abs(p.x)/h; z=(p.z-z0)/h
        # Side vertices below the armpit belong to the sleeves; blend smoothly near shoulder seam.
        if name == 'Briggs':
            # The coat skirt projects sideways at hip height. Weight only the
            # actual arms; widen the torso exclusion toward the waist.
            edge=.115 + .12*max(0.0,min(1.0,(.60-z)/.15))
            w=max(0.0,min(1.0,(x-edge)/.035))*max(0.0,min(1.0,(z-.38)/.07))*max(0.0,min(1.0,(.84-z)/.04))
        else:
            edge=.115 + .045*max(0.0,min(1.0,(.52-z)/.12))
            w=max(0.0,min(1.0,(x-edge)/.045))*max(0.0,min(1.0,(z-.32)/.07))*max(0.0,min(1.0,(.84-z)/.04))
        if w>0: groups[1 if p.x>=0 else -1].add([v.index],w,'REPLACE')
        if w<1: body.add([v.index],1-w,'REPLACE')
    mesh.parent=arm; mod=mesh.modifiers.new('Hub NPC rig','ARMATURE'); mod.object=arm
    for sign,label in ((1,'L'),(-1,'R')):
        pb=arm.pose.bones['upper_arm_'+label]
        pb.matrix_basis=pb.bone.matrix_local.to_3x3().inverted().to_4x4() @ Matrix.Rotation(math.radians(sign*17),4,'Y') @ pb.bone.matrix_local.to_3x3().to_4x4()
    bpy.context.view_layer.update()
    # Make the relaxed pose the exported rest geometry while retaining the skeleton and skin weights.
    bpy.context.view_layer.objects.active=mesh
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.view_layer.objects.active=arm
    for pb in arm.pose.bones: pb.matrix_basis.identity()
    mod=mesh.modifiers.new('Hub NPC rig','ARMATURE'); mod.object=arm
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); mesh.select_set(True); bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE',apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False)
    print('DONE',name,flush=True)
