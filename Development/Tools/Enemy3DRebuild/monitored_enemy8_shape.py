"""One foreground Hunyuan3D-2mini shape-only attempt with RAM abort."""
from pathlib import Path
import json, os, subprocess, sys, time
import psutil

repo = Path(__file__).resolve().parents[3]
source = repo / 'Assets/ArtSource/Enemy3DRebuild/NewMesh/Enemy_8/source-front.png'
work = Path('D:/Unity/AshfallArtValidation/enemy8-new-mesh')
work.mkdir(parents=True, exist_ok=True)
if not source.is_file():
    raise SystemExit('Prepare source-front.png first')
if (work / 'shape.glb').exists():
    raise SystemExit('Candidate exists; preserving it')
busy = {'unity.exe', 'blender.exe'}
if any(p.info['name'].lower() in busy for p in psutil.process_iter(['name']) if p.info['name']):
    raise SystemExit('Unity or Blender running')
lock = work / 'worker.lock'
fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
os.write(fd, str(os.getpid()).encode()); os.close(fd)
worker = '''import sys,time,json
from pathlib import Path
sys.path.insert(0,'D:/Unity/TripoSRLab/.venv2/Lib/site-packages')
sys.path.insert(0,'D:/Unity/AshfallArtValidation/enemy8-new-mesh/deps')
sys.path.insert(0,'D:/Unity/TripoSRLab/shape-runtime')
import torch
from PIL import Image
from accelerate import cpu_offload_with_hook
sys.path.insert(0,'D:/Unity/TripoSRLab/hunyuan3d2-mini-local')
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline
src=Path(sys.argv[1]);out=Path(sys.argv[2]);torch.set_num_threads(4)
cache='D:/Unity/TripoSRLab/hf-cache/hub/models--tencent--Hunyuan3D-2mini/snapshots/f90a0f7df7d5e6f71109cf333f6a95a0ae3194a6'
pipe=Hunyuan3DDiTFlowMatchingPipeline.from_pretrained(cache,subfolder='hunyuan3d-dit-v2-mini',variant='fp16',device='cpu',local_files_only=True)
prev=None
for part in (pipe.conditioner,pipe.model,pipe.vae): _,prev=cpu_offload_with_hook(part,torch.device('cuda:0'),prev_module_hook=prev)
pipe.device=torch.device('cuda');print('SHAPE_GENERATING',flush=True)
with torch.inference_mode(): mesh=pipe(image=Image.open(src).convert('RGBA'),num_inference_steps=30,octree_resolution=256,num_chunks=6000,generator=torch.Generator(device='cuda').manual_seed(20261024),output_type='trimesh')[0]
mesh.export(str(out));print('SHAPE_OK faces='+str(len(mesh.faces)),flush=True)
'''
script = work / 'worker.py'; script.write_text(worker, encoding='utf-8')
env=os.environ.copy();env.update(HF_HOME='D:/Unity/TripoSRLab/hf-cache',TEMP='D:/Unity/TripoSRLab/tmp',TMP='D:/Unity/TripoSRLab/tmp',OMP_NUM_THREADS='4')
minimum=999;peak=0;reason=None
try:
    with (work / 'shape.log').open('w') as log:
        child=subprocess.Popen([sys.executable,str(script),str(source),str(work/'shape.glb')],env=env,stdout=log,stderr=subprocess.STDOUT)
        while child.poll() is None:
            ram=psutil.virtual_memory();free=ram.available/2**30;minimum=min(minimum,free)
            try: peak=max(peak,psutil.Process(child.pid).memory_info().rss/2**30)
            except psutil.Error: pass
            if free<1.25 or ram.percent>96:
                reason='Low physical RAM headroom';child.terminate()
                try: child.wait(timeout=20)
                except subprocess.TimeoutExpired: child.kill();child.wait()
                break
            time.sleep(1)
    report={'exitCode':child.returncode,'minimumAvailableGB':round(minimum,2),'peakWorkerRSSGB':round(peak,2),'abortReason':reason}
    (work/'resource-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report))
finally:
    lock.unlink(missing_ok=True)
