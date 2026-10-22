from PIL import Image
import numpy as np,json
from pathlib import Path
from scipy.ndimage import distance_transform_edt
O=Path('D:/Unity/AshfallArtValidation/hidden-surface-art');A=Path('D:/Unity/Ashfall-iOS/Assets/Resources/Art/GeneratedWarden/HiddenSurfaceSource')
for r in json.loads((O/'camera-bounds.json').read_text()):
 name=r['name'];p=A/(name+'-painted-v2.png');p=p if p.exists() else A/(name+'-painted-v1.png')
 paint=Image.open(A/'leg-negative-x-inner-painted-v2.png').convert('RGBA').transpose(Image.Transpose.FLIP_LEFT_RIGHT) if name=='leg-positive-x-inner' else Image.open(p).convert('RGBA');src=np.array(paint.resize((512,1024),Image.Resampling.LANCZOS));clay=np.array(Image.open(O/(name+'-clay.png')).convert('RGBA'));valid=src[:,:,3]>230;target=clay[:,:,3]>100;_,fill=distance_transform_edt(~valid,return_indices=True);src[:,:,:3]=src[fill[0],fill[1],:3];out=np.zeros((1024,512,3),dtype=np.uint8)
 # Match row silhouettes instead of extending silhouette colours into missing UV coordinates.
 for y in range(1024):
  a=np.flatnonzero(valid[y]);b=np.flatnonzero(target[y]);
  if not len(a) or not len(b):continue
  u=np.linspace(a[0],a[-1],b[-1]-b[0]+1);xi=np.rint(u).astype(int);out[y,b[0]:b[-1]+1]=src[y,xi,:3]
 present=target&(out.max(axis=2)>0);_,idx=distance_transform_edt(~present,return_indices=True);out=out[idx[0],idx[1]]
 Image.fromarray(out).save(O/(name+'-registered.png'))