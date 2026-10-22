import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof/ApprovedSculpt/lowpoly-proof.blend')
for o in bpy.data.objects:
 if o.type=='MESH' and ('silhouette' in o.name or 'Warden sculpt' in o.name):
  pts=[o.matrix_world@Vector(c) for c in o.bound_box]
  print(o.name,'loc',tuple(o.location),'scale',tuple(o.scale),'local',tuple((min(v[i] for v in o.bound_box),max(v[i] for v in o.bound_box)) for i in range(3)),'world',tuple((min(v[i] for v in pts),max(v[i] for v in pts)) for i in range(3)))
