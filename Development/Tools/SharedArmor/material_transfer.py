"""Transfer illustration material palette to approved atlas, preserving all UV detail."""
import json,sys,hashlib
from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter
from scipy.cluster.vq import kmeans2
ROOT=Path('D:/Unity/Ashfall-iOS');OUT=Path('D:/Unity/AshfallArtValidation/shared-armor/palette');OUT.mkdir(exist_ok=True)
q=json.loads(Path('D:/Unity/AshfallArtValidation/equipment-generation/queue.json').read_text(encoding='utf-8'));jobs={j['key']:j for j in q['jobs']};base=np.array(Image.open(ROOT/'Assets/Resources/Art/GeneratedWarden/armor-rebuilt-atlas.png').convert('RGB')).astype(np.float32)
# Full original atlas is reused per slot; slot meshes select their original UVs.
# Do not reproject image silhouettes across limbs or rewrite UVs.
y=base@np.array([.2126,.7152,.0722]);bins=np.clip((y*16).astype(int),0,4095);gray=np.arange(4096)/16
sources={}
baseqs=np.quantile(y,np.linspace(.03,.97,64));base_median=np.median(base.reshape(-1,3),axis=0);metal=((base.max(2)-base.min(2))<35)&(y>60);metal_median=np.median(base[metal],axis=0)
def pixels(j):
 key=(j['source'],j['cell'])
 if key in sources:return sources[key]
 im=Image.open(ROOT/j['source']).convert('RGBA');w,h=im.size;c=j['crop_uv'];im=im.crop((round(c['x']*w),round((1-c['y']-c['height'])*h),round((c['x']+c['width'])*w),round((1-c['y'])*h)));a=np.array(im);rgb=a[:,:,:3][a[:,:,3]>100].astype(np.float32)
 # Reject nearly black outline/white specular swatches, preserve them in original texture instead.
 lum=rgb@np.array([.2126,.7152,.0722]);rgb=rgb[(lum>22)&(lum<230)]
 if len(rgb)<20:raise ValueError('No material swatches')
 sources[key]=rgb;return rgb
def run(p):
 j=jobs[p['jobs'][0]];slot=j.get('slot',j.get('index',0))
 if slot==0:return
 rgb=pixels(j);lum=rgb@np.array([.2126,.7152,.0722]);order=np.argsort(lum);rgb=rgb[order];lum=lum[order]
 # Robust tonal transfer keeps original lights/shadows and surface detail.
 quant=np.linspace(.03,.97,64)
 # Sorted luminance swatches retain painted material character, without copying object silhouettes.
 idx=np.linspace(0,len(rgb)-1,64).astype(int);target=rgb[idx]
 lut=np.stack([np.interp(gray,baseqs,target[:,k]) for k in range(3)],axis=-1)
 mapped=lut[bins];mapped_y=mapped@np.array([.2126,.7152,.0722]);mapped*=np.clip((y+15)/(mapped_y+15),.55,1.65)[:,:,None]
 # Original multiscale brush/cloth/metal shading remains dominant in luminance.
 ratio=np.clip(np.median(rgb,axis=0)/np.maximum(base_median,20),.35,2.3)
 tinted=np.clip(base*ratio,0,255)
 result=.65*tinted+.35*mapped
 if len(p['jobs'])>1:
  extra=pixels(jobs[p['jobs'][1]]);extra_med=np.median(extra,axis=0);sat=base.max(2)-base.min(2);metal=(sat<35)&(y>60)
  extra_ratio=np.clip(extra_med/np.maximum(metal_median,20),.5,1.8)
  result[metal]=.65*result[metal]+.35*np.clip(base[metal]*extra_ratio,0,255)
 result=np.clip(result,0,255).astype('uint8');path=OUT/(p['id']+'.png');Image.fromarray(result).save(path,optimize=False)
 return dict(id=p['id'],slot=slot,sources=p['jobs'],method='UV-preserving material transfer; approved folds/seams/wear retained',sha256=hashlib.sha256(path.read_bytes()).hexdigest())
ids=set(sys.argv[1:]);report=[]
for p in q['products']:
 if p['kind']!='armor' or (ids and p['id'] not in ids):continue
 r=run(p)
 if r:report.append(r);print(r['id'],flush=True)
(OUT/'manifest.json').write_text(json.dumps(report,indent=2));print('COUNT',len(report))

