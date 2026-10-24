"""Lightweight orthographic silhouette check without starting another Unity."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[3]
mesh_dir = root / "Assets/Resources/Art/Enemy3DRebuild/NewMesh/Enemy_8"
out_dir = Path("D:/Unity/AshfallArtValidation/enemy8-offline-shape")
out_dir.mkdir(parents=True, exist_ok=True)
parts = {
    "Head": (0, 1.78, 0), "Chest": (0, 1.34, 0), "Pelvis": (0, .96, 0),
    "UpperArm_L": (-.305, 1.64, 0), "UpperArm_R": (.305, 1.64, 0),
    "Forearm_L": (-.305, 1.34, 0), "Forearm_R": (.305, 1.34, 0),
    "Thigh_L": (-.151, .935, 0), "Thigh_R": (.151, .935, 0),
    "Shin_L": (-.151, .505, 0), "Shin_R": (.151, .505, 0),
    "Foot_L": (-.151, .075, 0), "Foot_R": (.151, .075, 0),
}
colors = {"Head": "#e6b747", "Chest": "#9aaab0", "Pelvis": "#76666a",
          "UpperArm": "#db8861", "Forearm": "#d54153", "Thigh": "#8e9eac",
          "Shin": "#adb7bc", "Foot": "#695a4a"}

textures = {}
for texture_name in ("texture-atlas", "texture-atlas-body", "texture-boots"):
    textures[texture_name] = np.asarray(Image.open(mesh_dir / f"{texture_name}.png").convert("RGB"))

for view in ("front", "side", "back"):
    tris = []
    for name, joint in parts.items():
        vertices = []
        uvs = []
        for line in (mesh_dir / f"{name}.obj").open(encoding="ascii"):
            if line.startswith("v "):
                vertices.append(np.array([float(v) for v in line.split()[1:]]) + joint)
            elif line.startswith("vt "):
                uvs.append(tuple(float(v) for v in line.split()[1:]))
            elif line.startswith("f "):
                ids = [int(p.split("/")[0])-1 for p in line.split()[1:]]
                uv_ids = [int(p.split("/")[1])-1 for p in line.split()[1:]]
                a, b, c = (vertices[i] for i in ids)
                if view == "front":
                    depth = (a[2]+b[2]+c[2])/3
                    points = [(v[0], v[1]) for v in (a,b,c)]
                elif view == "back":
                    depth = -(a[2]+b[2]+c[2])/3
                    points = [(-v[0], v[1]) for v in (a,b,c)]
                else:
                    depth = (a[0]+b[0]+c[0])/3
                    points = [(v[2], v[1]) for v in (a,b,c)]
                uv = np.mean([uvs[i] for i in uv_ids], axis=0)
                texture_name = "texture-boots" if name.startswith("Foot") else \
                    "texture-atlas-body" if name in ("Chest", "Pelvis") else "texture-atlas"
                texture = textures[texture_name]
                tx = int(np.clip(uv[0] * (texture.shape[1]-1), 0, texture.shape[1]-1))
                ty = int(np.clip((1-uv[1]) * (texture.shape[0]-1), 0, texture.shape[0]-1))
                tris.append((depth, points,
                             next(color for key,color in colors.items() if name.startswith(key)),
                             tuple(int(c) for c in texture[ty,tx])))
    tris.sort(key=lambda entry: entry[0])
    for mode in ("shape", "texture"):
        canvas = Image.new("RGB", (800, 900), "#252c35")
        draw = ImageDraw.Draw(canvas)
        for _, points, shape_color, texture_color in tris:
            draw.polygon([(int(400+x*330), int(820-y*330)) for x,y in points],
                         fill=shape_color if mode == "shape" else texture_color)
        canvas.save(out_dir / f"{view}-{mode}.png")
print(out_dir)
