from pathlib import Path
import json
from PIL import Image,ImageOps
import numpy as np
from scipy.ndimage import distance_transform_edt,binary_fill_holes
R=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof');O=Path('D:/Unity/AshfallArtValidation/body-surface-fix')
atlas=Image.open(R/'ArmRigSide/plain-body-four-view-atlas.png').convert('RGBA')
atlas.paste(Image.open(R/'BodyTwoView/body-head-two-view.png').convert('RGBA').resize((2048,1024)),(0,0))
src=Image.open(R/'SideSurfaceRepair/right-armless-source.png').convert('RGB').resize((683,1024),Image.Resampling.LANCZOS)
a=np.array(src);edge=np.median(np.concatenate([a[:,:24],a[:,-24:]],axis=1),axis=1);dist=np.linalg.norm(a.astype(float)-edge[:,None,:],axis=2);sat=(a.max(axis=2).astype(float)-a.min(axis=2))/np.maximum(a.max(axis=2),1);mask=binary_fill_holes((sat>.12)|(a.max(axis=2)<100))
# Dilate RGB to all empty pixels once; surface mapping can never pick transparent
# backdrop or introduce shoulder cutouts. This changes only the two profile tiles.
filled=a.copy()
# Extend each scanline from the visible person into its own background; a
# global nearest search pulled grey foot shadows and skin from adjacent rows.
for y in range(a.shape[0]):
 good=np.flatnonzero(((sat[y]>.18)|((a[y].max(axis=1)<85)&(y<210))) & (np.arange(683)>240)&(np.arange(683)<440))
 if len(good)>4:
  left,right=int(np.percentile(good,4)),int(np.percentile(good,96))
  filled[y,:left]=a[y,left];filled[y,right+1:]=a[y,right]
 else:
  previous=max(0,y-1);filled[y]=filled[previous]
photo=Image.fromarray(filled).convert('RGBA')
rows=[]
for y in range(1024):
 good=np.flatnonzero(((sat[y]>.16)|((a[y].max(axis=1)<85)&(y<210))) & (np.arange(683)>240)&(np.arange(683)<440))
 rows.append([int(np.percentile(good,3)),int(np.percentile(good,97))] if len(good)>4 else [300,370])
(O/'profile-rows.json').write_text(json.dumps(rows))
for x,p in [(0,photo),(1024,ImageOps.mirror(photo))]:
 tile=Image.new('RGBA',(1024,1024));tile.paste(p,(170,0));ar=np.array(tile);valid=ar[:,:,3]>0;ii=distance_transform_edt(~valid,return_distances=False,return_indices=True);ar=ar[tuple(ii)];ar[:,:,3]=255;atlas.paste(Image.fromarray(ar),(x,1024))
atlas.save(O/'surface-fixed-candidate-atlas.png')
