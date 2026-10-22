"""One local Hunyuan3D-2mini shape-only candidate; never run texture generation."""
import json
import sys
import time
import traceback
from pathlib import Path

import torch
from PIL import Image

MODEL_SOURCE = Path(r"D:\Unity\TripoSRLab\hunyuan3d2-mini-local")
MODEL_CACHE = Path(r"D:\Unity\TripoSRLab\hf-cache\hub\models--tencent--Hunyuan3D-2mini\snapshots\f90a0f7df7d5e6f71109cf333f6a95a0ae3194a6")
OUT = Path(r"D:\Unity\Ashfall-iOS\Assets\ArtSource\GeneratedEquipment\KestrelHarrierPatrol")
SOURCE = OUT / "kestrel-harrier-left-source.png"
TARGET = OUT / "kestrel-harrier-shape-only.glb"
sys.path.insert(0, str(MODEL_SOURCE))
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline

stats = {"model": "Hunyuan3D-2mini", "source": str(SOURCE), "texture_generation": False,
         "steps": 30, "octree_resolution": 256, "num_chunks": 6000, "seed": 20261021}
try:
    stats["gpu"] = torch.cuda.get_device_name(0)
    start = time.time()
    pipeline = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(
        str(MODEL_CACHE), subfolder="hunyuan3d-dit-v2-mini", variant="fp16",
        device="cpu", local_files_only=True)
    from accelerate import cpu_offload_with_hook
    prior = None
    for component in (pipeline.conditioner, pipeline.model, pipeline.vae):
        _, prior = cpu_offload_with_hook(component, torch.device("cuda:0"), prev_module_hook=prior)
    pipeline.device = torch.device("cuda")
    stats["load_seconds"] = round(time.time() - start, 2)
    image = Image.open(SOURCE).convert("RGBA")
    start = time.time()
    with torch.inference_mode():
        mesh = pipeline(image=image, num_inference_steps=30,
                        octree_resolution=256, num_chunks=6000,
                        generator=torch.Generator(device="cuda").manual_seed(stats["seed"]),
                        output_type="trimesh")[0]
    stats["generation_seconds"] = round(time.time() - start, 2)
    stats["vertices"] = len(mesh.vertices)
    stats["faces"] = len(mesh.faces)
    mesh.export(str(TARGET))
    stats["output"] = str(TARGET)
    stats["status"] = "success"
except Exception:
    stats["status"] = "failed"
    stats["traceback"] = traceback.format_exc()
    raise
finally:
    (OUT / "shape-run.json").write_text(json.dumps(stats, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in stats.items() if k != "traceback"}), flush=True)
