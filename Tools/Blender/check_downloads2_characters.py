"""Read-only sanity checks for the four imported character FBX files."""
import json
from pathlib import Path

import bpy

ROOT = Path("D:/Unity/Ashfall-iOS/Assets/Resources/Characters")
report = {}
for name in ("Iona", "Briggs", "Raven", "ValeBoss"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = ROOT / name / (name + ".fbx")
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    verts = [v for o in meshes for v in o.data.vertices]
    faces = sum(len(o.data.polygons) for o in meshes)
    groups = {o.name: len(o.vertex_groups) for o in meshes}
    unweighted = sum(not v.groups for v in verts)
    bone_names = [b.name for arm in arms for b in arm.data.bones]
    actions = [a.name for a in bpy.data.actions]
    report[name] = dict(meshes=len(meshes), armatures=len(arms), faces=faces,
                        vertices=len(verts), unweighted=unweighted,
                        vertex_groups=groups, bone_count=len(bone_names),
                        sample_bones=bone_names[:16], actions=actions)
print("DOWNLOADS2_FBX_AUDIT " + json.dumps(report))
