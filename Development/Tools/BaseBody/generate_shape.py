"""Shape-only local Hunyuan3D-2mini pass. Never runs texture generation."""
import json,os,sys,time,traceback
from pathlib import Path
os.environ['HF_HOME']=r'D:\Unity\TripoSRLab\hf-cache'
os.environ['HF_HUB_CACHE']=r'D:\Unity\TripoSRLab\hf-cache\hub'
os.environ['TMPDIR']=r'D:\Unity\TripoSRLab\tmp'
sys.path.insert(0,r'D:\Unity\TripoSRLab\hunyuan3d2-mini-local')
from PIL import Image
import torch
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
from accelerate import cpu_offload_with_hook

out=Path(r'D:\Unity\AshfallArtValidation\base-body-v1');out.mkdir(parents=True,exist_ok=True)
src=Path(r'D:\Unity\Ashfall-iOS\Assets\ArtSource\BaseBody\Candidates\front-projection.png')
target=out/'base-body-hunyuan2mini-shape-only.glb'
stats={'model':'tencent/Hunyuan3D-2mini','source':str(src),'texture_generation':False,'steps':30,'octree_resolution':256,'num_chunks':6000,'seed':20261021}
try:
    stats['device']=torch.cuda.get_device_name(0)
    print('LOAD_SHAPE_ONLY',stats['device'],flush=True)
    model_root=Path(r'D:\Unity\TripoSRLab\hf-cache\hub\models--tencent--Hunyuan3D-2mini\snapshots\f90a0f7df7d5e6f71109cf333f6a95a0ae3194a6')
    pipe=Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(model_root),subfolder='hunyuan3d-dit-v2-mini',variant='fp16',device='cpu',local_files_only=True)
    prev=None
    for component in (pipe.conditioner,pipe.model,pipe.vae):_,prev=cpu_offload_with_hook(component,torch.device('cuda:0'),prev_module_hook=prev)
    pipe.device=torch.device('cuda')
    print('RUN_SHAPE_ONLY',flush=True);t=time.time()
    with torch.inference_mode():
        mesh=pipe(image=Image.open(src).convert('RGBA'),num_inference_steps=30,octree_resolution=256,num_chunks=6000,generator=torch.Generator(device='cuda').manual_seed(stats['seed']),output_type='trimesh')[0]
    stats.update(seconds=round(time.time()-t,2),vertices=int(len(mesh.vertices)),faces=int(len(mesh.faces)))
    mesh.export(str(target));stats.update(output=str(target),status='success')
    print('SHAPE_DONE',json.dumps(stats),flush=True)
except Exception as ex:
    stats.update(status='failed',error=repr(ex),traceback=traceback.format_exc());print('SHAPE_FAILED',repr(ex),flush=True);raise
finally:
    (out/'shape-stats.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
