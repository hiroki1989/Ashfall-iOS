"""Explicit one-item shape-only worker. Refuses low-memory/concurrent workloads.

Default mode only checks resources. --run is required; no scheduler or service.
Outputs remain outside Resources until visual QA approves them.
"""
import argparse,ctypes,json,os,subprocess,sys,time
from pathlib import Path

BASE=Path('D:/Unity/TripoSRLab')
OUT=Path('D:/Unity/AshfallArtValidation/equipment-generation')

class Memory(ctypes.Structure):
    _fields_=[('length',ctypes.c_ulong),('load',ctypes.c_ulong),('total',ctypes.c_ulonglong),
              ('available',ctypes.c_ulonglong),('page_total',ctypes.c_ulonglong),
              ('page_available',ctypes.c_ulonglong),('virtual_total',ctypes.c_ulonglong),
              ('virtual_available',ctypes.c_ulonglong),('extended',ctypes.c_ulonglong)]

def resources():
    m=Memory();m.length=ctypes.sizeof(m);ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(m))
    gpu=subprocess.check_output(['nvidia-smi','--query-gpu=memory.free','--format=csv,noheader,nounits'],text=True)
    unity=subprocess.run(['powershell','-NoProfile','-Command',
        "@(Get-Process Unity -ErrorAction SilentlyContinue).Count"],capture_output=True,text=True)
    return dict(ram_free_gb=round(m.available/2**30,2),gpu_free_mb=int(gpu.strip().splitlines()[0]),
                unity_processes=int(unity.stdout.strip() or '0'))

def main():
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path);p.add_argument('--id');p.add_argument('--run',action='store_true')
    a=p.parse_args();r=resources();r['safe']=r['unity_processes']==0
    r['memory_review_required']=True
    r['note']='Historical successful 2mini run did not measure free RAM. No verified 9GB RAM requirement. Explicit measured-resource review required before running.'
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'last-preflight.json').write_text(json.dumps(r,indent=2))
    print(json.dumps(r),flush=True)
    if not a.run:return
    if not r['safe']:raise SystemExit('RESOURCE_GUARD: close Unity; no model was loaded')
    raise SystemExit('Historical RAM requirement was unverified. Use reviewed original shape-only settings with resource monitoring; this provisional launcher is disabled pending correction.')
    if not a.source or not a.source.is_file() or not a.id or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789.-_' for c in a.id):
        raise SystemExit('Valid --source and lowercase --id required')
    lock=OUT/'shape-worker.lock'
    try:fd=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY)
    except FileExistsError:raise SystemExit('Another worker or unfinished lock exists; inspect before retry')
    os.write(fd,str(os.getpid()).encode());os.close(fd)
    try:
        os.environ.update(HF_HOME=str(BASE/'hf-cache'),HF_HUB_CACHE=str(BASE/'hf-cache/hub'),
                          TMP=str(BASE/'tmp'),TEMP=str(BASE/'tmp'),OMP_NUM_THREADS='4',
                          PYTORCH_CUDA_ALLOC_CONF='max_split_size_mb:128')
        sys.path.insert(0,str(BASE/'hunyuan3d2-mini-local'))
        import torch
        from PIL import Image
        from accelerate import cpu_offload_with_hook
        from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
        torch.set_num_threads(4)
        cache=BASE/'hf-cache/hub/models--tencent--Hunyuan3D-2mini/snapshots/f90a0f7df7d5e6f71109cf333f6a95a0ae3194a6'
        pipe=Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(str(cache),subfolder='hunyuan3d-dit-v2-mini',variant='fp16',device='cpu',local_files_only=True)
        previous=None
        for c in (pipe.conditioner,pipe.model,pipe.vae):_,previous=cpu_offload_with_hook(c,torch.device('cuda:0'),prev_module_hook=previous)
        pipe.device=torch.device('cuda');start=time.time()
        with torch.inference_mode():
            mesh=pipe(image=Image.open(a.source).convert('RGBA'),num_inference_steps=30,octree_resolution=256,
                      num_chunks=6000,generator=torch.Generator(device='cuda').manual_seed(20261022),output_type='trimesh')[0]
        target=OUT/a.id;target.mkdir(exist_ok=True);mesh.export(str(target/'shape.glb'))
        (target/'shape-report.json').write_text(json.dumps(dict(source=str(a.source),seconds=time.time()-start,
            texture_generation=False,faces=len(mesh.faces),status='shape_candidate_requires_visual_qa'),indent=2))
    finally:lock.unlink(missing_ok=True)

if __name__=='__main__':main()

