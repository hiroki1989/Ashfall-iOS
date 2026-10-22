import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelHarrierPatrol/ExportCandidate/kestrel-harrier.blend')
ps=[o.matrix_world@v.co for o in bpy.data.objects if o.type=='MESH' for v in o.data.vertices if abs((o.matrix_world@v.co).x+.17)<.015]
print('HANDGUARD_UNDERSIDE_Z',min(p.z for p in ps),max(p.z for p in ps))
