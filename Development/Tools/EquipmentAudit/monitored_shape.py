"""Explicit foreground shape-only trial with parent memory watchdog; no service."""
import argparse,json,os,subprocess,sys,time
from pathlib import Path
import psutil
ROOT=Path('D:/Unity/AshfallArtValidation/equipment-generation')
p=argparse.ArgumentParser();p.add_argument('--id',required=True);a=p.parse_args()
folder=ROOT/a.id
if not (folder/'source.png').is_file():raise SystemExit('Source needs preparation')
if any(x.name().lower()=='unity.exe' for x in psutil.process_iter()):raise SystemExit('Unity must be closed')
if (folder/'shape.glb').exists():raise SystemExit('Preserving existing candidate')
lock=ROOT/'shape-worker.lock'
try:fd=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY)
except FileExistsError:raise SystemExit('Another worker lock exists; inspect first')
os.write(fd,str(os.getpid()).encode());os.close(fd)
# Matches the original successful local settings; no arbitrary 9GB start rule.
worker='''import os,sys,time,json
from pathlib import Path
sys.path.insert(0,'D:/Unity/TripoSRLab/shape-runtime')
import torch
from PIL import Image
from accelerate import cpu_offload_with_hook
sys.path.insert(0,'D:/Unity/TripoSRLab/hunyuan3d2-mini-local')
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
folder=Path(sys.argv[1]);torch.set_num_threads(4)
start=time.time()
cache='D:/Unity/TripoSRLab/hf-cache/hub/models--tencent--Hunyuan3D-2mini/snapshots/f90a0f7df7d5e6f71109cf333f6a95a0ae3194a6'
pipe=Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(cache,subfolder='hunyuan3d-dit-v2-mini',variant='fp16',device='cpu',local_files_only=True)
prev=None
for component in (pipe.conditioner,pipe.model,pipe.vae):_,prev=cpu_offload_with_hook(component,torch.device('cuda:0'),prev_module_hook=prev)
pipe.device=torch.device('cuda')
print('SHAPE_GENERATING',flush=True)
with torch.inference_mode():mesh=pipe(image=Image.open(folder/'source.png').convert('RGBA'),num_inference_steps=30,octree_resolution=256,num_chunks=6000,generator=torch.Generator(device='cuda').manual_seed(20261019),output_type='trimesh')[0]
mesh.export(str(folder/'shape.glb'))
(folder/'shape-report.json').write_text(json.dumps(dict(model='Hunyuan3D-2mini',steps=30,octree=256,chunks=6000,fp16=True,cpu_offload=True,ai_texture=False,seconds=time.time()-start,faces=len(mesh.faces),status='candidate_not_approved'),indent=2))
print('SHAPE_OK',flush=True)
'''
(folder/'shape_worker.py').write_text(worker,encoding='utf-8')
env=os.environ.copy();env.update(HF_HOME='D:/Unity/TripoSRLab/hf-cache',TEMP='D:/Unity/TripoSRLab/tmp',TMP='D:/Unity/TripoSRLab/tmp',OMP_NUM_THREADS='4')
minimum=999;peak=0;reason=None
try:
 with (folder/'shape.log').open('w') as log:
  child=subprocess.Popen([sys.executable,str(folder/'shape_worker.py'),str(folder)],env=env,stdout=log,stderr=subprocess.STDOUT)
  while child.poll() is None:
   mem=psutil.virtual_memory();free=mem.available/2**30;minimum=min(minimum,free)
   try:peak=max(peak,psutil.Process(child.pid).memory_info().rss/2**30)
   except psutil.Error:pass
   # Emergency headroom, not a claim about model's minimum requirements.
   if free<1.25 or mem.percent>96:
    reason='Low physical RAM headroom';child.terminate();child.wait(timeout=20);break
   time.sleep(1)
  report=dict(exit_code=child.returncode,minimum_available_ram_gb=round(minimum,2),peak_worker_rss_gb=round(peak,2),abort_reason=reason)
  (folder/'resource-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report),flush=True)
finally:lock.unlink(missing_ok=True)

