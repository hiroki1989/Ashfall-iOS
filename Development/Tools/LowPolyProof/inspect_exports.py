import bpy
from pathlib import Path
p=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof/ExportCandidates')
for file in p.glob('*.fbx'):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(file))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 print(file.name,[(o.name,len(o.data.polygons),list(o.data.uv_layers.keys()),tuple(round(q,3) for q in o.dimensions)) for o in meshes])
