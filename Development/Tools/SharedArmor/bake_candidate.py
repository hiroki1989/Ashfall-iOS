"""Bake item front paint onto approved fixed UV; preserve source model and rig."""
import json,sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt
ROOT=Path('D:/Unity/Ashfall-iOS');OUT=Path('D:/Unity/AshfallArtValidation/shared-armor');S=2048
cache=OUT/'uv-position.npz'
if not cache.exists():
 data=json.loads((OUT/'source-mesh.json').read_text());verts=np.array(data['vertices']);position=np.zeros((S,S,3),np.float32);normal=np.zeros((S,S,3),np.float32);valid=np.zeros((S,S),bool)
 for indices,uv in data['triangles']:
  p=verts[indices];t=np.array(uv)*[S-1,-(S-1)]+[0,S-1];lo=np.maximum(np.floor(t.min(0)).astype(int),0);hi=np.minimum(np.ceil(t.max(0)).astype(int),S-1)
  if np.any(hi<lo):continue
  x,y=np.meshgrid(np.arange(lo[0],hi[0]+1),np.arange(lo[1],hi[1]+1));d=(t[1,1]-t[2,1])*(t[0,0]-t[2,0])+(t[2,0]-t[1,0])*(t[0,1]-t[2,1])
  if abs(d)<1e-9:continue
  a=((t[1,1]-t[2,1])*(x-t[2,0])+(t[2,0]-t[1,0])*(y-t[2,1]))/d;b=((t[2,1]-t[0,1])*(x-t[2,0])+(t[0,0]-t[2,0])*(y-t[2,1]))/d;c=1-a-b;inside=(a>=-.001)&(b>=-.001)&(c>=-.001)
  yy=y[inside];xx=x[inside];position[yy,xx]=a[inside,None]*p[0]+b[inside,None]*p[1]+c[inside,None]*p[2];n=np.cross(p[1]-p[0],p[2]-p[0]);normal[yy,xx]=n/max(np.linalg.norm(n),1e-9);valid[yy,xx]=True
 np.savez_compressed(cache,position=position,normal=normal,valid=valid)
d=np.load(cache);pos=d['position'];norm=d['normal'];valid=d['valid'];height=pos[:,:,2]+1
queue=json.loads(Path('D:/Unity/AshfallArtValidation/equipment-generation/queue.json').read_text(encoding='utf-8'));jobs={j['key']:j for j in queue['jobs']}
base=np.array(Image.open(ROOT/'Assets/Resources/Art/GeneratedWarden/armor-rebuilt-atlas.png').convert('RGB'))
def bake(product):
 j=jobs[product['jobs'][0]];slot=j.get('slot',j.get('index',0))
 if slot==0:return
 im=Image.open(ROOT/j['source']).convert('RGBA');w,h=im.size;c=j['crop_uv'];im=im.crop((round(c['x']*w),round((1-c['y']-c['height'])*h),round((c['x']+c['width'])*w),round((1-c['y'])*h)));arr=np.array(im);mask=arr[:,:,3]>32
 if not mask.any():return
 ys,xs=np.where(mask);arr=arr[ys.min():ys.max()+1,xs.min():xs.max()+1];mask=arr[:,:,3]>32;ii=distance_transform_edt(~mask,return_distances=False,return_indices=True);paint=arr[ii[0],ii[1],:3]
 ranges={1:(1.14,1.84,.51),2:(.79,1.14,.50),3:(.43,1.25,.31),4:(.10,.43,.27)};low,high,half=ranges[slot]
 u=(pos[:,:,0]/half+1)/2;v=(high-height)/(high-low)
 # A fixed source projection independent of viewing camera. No alpha holes.
 ix=np.clip((u*(paint.shape[1]-1)).astype(int),0,paint.shape[1]-1);iy=np.clip((v*(paint.shape[0]-1)).astype(int),0,paint.shape[0]-1)
 color=paint[iy,ix];result=base.copy()
 region=valid&(height>=low-.03)&(height<=high+.03)
 if slot==1:region&=((height>=1.25)|(abs(pos[:,:,0])>.29))
 elif slot==2:region&=abs(pos[:,:,0])>.29
 elif slot in (3,4):region&=abs(pos[:,:,0])<.35
 # Candidate front projection only. Back uses original structural paint with source palette,
 # avoiding mirrored front-facing badges/pockets on the rear. Needs visual acceptance.
 front=region&(norm[:,:,1]<-.15);result[front]=color[front]
 back=region&~front
 target=np.median(paint[mask],axis=0);ref=np.median(base[region],axis=0);ratio=np.clip(target/np.maximum(ref,20),.4,2)
 result[back]=np.clip(base[back]*ratio,0,255).astype('uint8')
 out=OUT/'candidates';out.mkdir(exist_ok=True);Image.fromarray(result).save(out/(product['id']+'.png'))
 (out/(product['id']+'.json')).write_text(json.dumps(dict(source=j,slot=slot,geometry='approved Warden unchanged',status='front projection/back palette candidate, not accepted',front_pixels=int(front.sum())),indent=2))
 print(product['id'],slot,flush=True)
ids=sys.argv[1:]
for p in queue['products']:
 if p['kind']=='armor' and p['id'] in ids:bake(p)
