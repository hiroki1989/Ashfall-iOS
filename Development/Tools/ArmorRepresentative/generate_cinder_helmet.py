"""Shape-only Hunyuan3D-2mini candidate; never run beside another GPU job."""
import json
import sys
import time
import traceback
from pathlib import Path

import torch
from PIL import Image

root = Path(r"D:\Unity\TripoSRLab\hunyuan3d2-mini-local")
sys.path.insert(0, str(root))
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline

source = Path(r"D:\Unity\Ashfall-iOS\Assets\ArtSource\GeneratedEquipment\CinderScrapHelmet\front-blueprint-v2.png")
out = Path(r"D:\Unity\Ashfall-iOS\Assets\ArtSource\GeneratedEquipment\CinderScrapHelmet\Candidates")
out.mkdir(parents=True, exist_ok=True)
target = out / "cinder-scrap-helmet-shape-only.glb"
model_root = Path(r"D:\Unity\TripoSRLab\hf-cache\hub\models--tencent--Hunyuan3D-2mini\snapshots\f90a0f7df7d5e6f71109cf333f6a95a0ae3194a6")
stats = {"model": "tencent/Hunyuan3D-2mini", "texture_generation": False,
         "input": str(source), "steps": 30, "octree_resolution": 256,
         "num_chunks": 6000, "seed": 20261023}
try:
    stats["device"] = torch.cuda.get_device_name(0)
    stats["gpu_free_mib_before"] = torch.cuda.mem_get_info()[0] // (1024 * 1024)
    start = time.time()
    pipe = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(
        str(model_root), subfolder="hunyuan3d-dit-v2-mini", variant="fp16",
        device="cpu", local_files_only=True)
    from accelerate import cpu_offload_with_hook
    previous = None
    for component in (pipe.conditioner, pipe.model, pipe.vae):
        _, previous = cpu_offload_with_hook(component, torch.device("cuda:0"), prev_module_hook=previous)
    pipe.device = torch.device("cuda")
    stats["load_seconds"] = round(time.time() - start, 2)
    start = time.time()
    with torch.inference_mode():
        mesh = pipe(image=Image.open(source).convert("RGBA"),
                    num_inference_steps=30, octree_resolution=256, num_chunks=6000,
                    generator=torch.Generator(device="cuda").manual_seed(stats["seed"]),
                    output_type="trimesh")[0]
    stats["generation_seconds"] = round(time.time() - start, 2)
    stats["vertices"] = len(mesh.vertices)
    stats["faces"] = len(mesh.faces)
    mesh.export(str(target))
    stats["output"] = str(target)
    stats["file_bytes"] = target.stat().st_size
    stats["status"] = "success"
except Exception as exc:
    stats["status"] = "failed"
    stats["error"] = repr(exc)
    stats["traceback"] = traceback.format_exc()
    raise
finally:
    (out / "shape-stats.json").write_text(json.dumps(stats, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(stats, ensure_ascii=False), flush=True)
