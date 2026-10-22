import bpy,json
from pathlib import Path
p=Path('D:/Unity/Ashfall-iOS');bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(p/'Assets/Resources/Art/GeneratedWarden/armor-rebuilt.fbx'))
meshes=[o for o in bpy.data.objects if o.type=='MESH'];o=max(meshes,key=lambda o:len(o.data.polygons));m=o.data;m.calc_loop_triangles()
r={'vertices':[list(o.matrix_world@v.co) for v in m.vertices],'triangles':[[t.vertices[:], [list(m.uv_layers[0].data[i].uv) for i in t.loops]] for t in m.loop_triangles]}
out=Path('D:/Unity/AshfallArtValidation/shared-armor');out.mkdir(exist_ok=True);(out/'source-mesh.json').write_text(json.dumps(r));print('BOUNDS',[(min(v[i] for v in r['vertices']),max(v[i] for v in r['vertices'])) for i in range(3)])
