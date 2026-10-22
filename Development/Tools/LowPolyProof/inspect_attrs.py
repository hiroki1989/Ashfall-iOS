import bpy,json,collections
from pathlib import Path
root=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof/ArmRigCandidate')
bpy.ops.wm.open_mainfile(filepath=str(root/'plain-body-arm-rig-candidate.blend'))
obj=next(o for o in bpy.data.objects if o.name=='plain_body')
attr=obj.data.attributes['source_vertex']
values=[x.value for x in attr.data]
with open(root/'vertex-map.json') as f: parts=json.load(f)['parts']
arm={k:set(v) for k,v in parts.items() if k!='body'}
union=set().union(*arm.values())
print('body vertices',len(values),'mapped arm vertices exact',sum(v in union for v in values),'unique',len(set(values)))
by=collections.Counter(); per=collections.Counter()
for poly in obj.data.polygons:
 ids=[values[i] for i in poly.vertices]
 hits=sum(v in union for v in ids)
 by[hits]+=1
 if hits: per[next((k for k,s in arm.items() if sum(v in s for v in ids)>=2),'single')] +=1
print('face hits',dict(by));print('per',dict(per))
