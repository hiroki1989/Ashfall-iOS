"""Make an opaque, edge-extended front/back paint atlas for planar mesh projection."""
from pathlib import Path
from PIL import Image
import numpy as np
from scipy.ndimage import gaussian_filter

ROOT=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedBase/Candidates')
ROOT.mkdir(parents=True,exist_ok=True)
PREP=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/BaseBody/Candidates')
sharp_parts=[]
side_parts=[]
for name in ('front','back'):
    im=Image.open(PREP/f'{name}-projection.png').convert('RGBA')
    a=np.asarray(im)
    alpha=a[:,:,3]>128
    ys,xs=np.where(alpha)
    a=a[ys.min():ys.max()+1,xs.min():xs.max()+1]
    saturation=a[:,:,:3].max(2).astype(np.int16)-a[:,:,:3].min(2).astype(np.int16)
    intensity=a[:,:,:3].mean(2)
    valid=(a[:,:,3]>200)&((saturation>15)|(intensity<130))
    # Paint every atlas pixel using the closest actual garment/skin pixel.
    # This is required where the 3D silhouette extends past the painting.
    rgb=np.empty_like(a[:,:,:3])
    available=np.flatnonzero(valid.any(1))
    for y in range(a.shape[0]):
        source_y=available[np.argmin(np.abs(available-y))]
        xs=np.flatnonzero(valid[source_y])
        nearest=np.searchsorted(xs,np.arange(a.shape[1]))
        nearest=np.clip(nearest,0,len(xs)-1)
        left=np.clip(nearest-1,0,len(xs)-1)
        nearest=np.where(np.abs(xs[left]-np.arange(a.shape[1]))<np.abs(xs[nearest]-np.arange(a.shape[1])),left,nearest)
        sample=xs[nearest]
        inset=min(38,max(1,(xs[-1]-xs[0])//5))
        sample=np.where(np.arange(a.shape[1])<xs[0],min(xs[-1],xs[0]+inset),sample)
        sample=np.where(np.arange(a.shape[1])>xs[-1],max(xs[0],xs[-1]-inset),sample)
        rgb[y]=a[source_y,sample,:3]
    # Side faces see pixels outside the orthographic silhouette. Smooth only
    # those extended pixels to avoid row-sized stripes along arms and boots.
    smooth=np.stack([gaussian_filter(rgb[:,:,c].astype(np.float32),sigma=(65,22)) for c in range(3)],axis=2)
    rgb=np.where(valid[:,:,None],rgb,np.clip(smooth,0,255).astype(np.uint8))
    # The source photo has a pale antialias fringe around its dark hair. Side
    # geometry samples the extended region, so use the actual hair palette.
    upper=int(a.shape[0]*.15)
    fringe=(~valid[:upper])
    hair=np.array([48,39,35],dtype=np.uint8)
    rgb[:upper]=np.where(fringe[:,:,None],hair,rgb[:upper])
    sharp=Image.fromarray(np.dstack((rgb,np.full(valid.shape,255,dtype=np.uint8))),'RGBA').resize((1024,2048),Image.Resampling.LANCZOS)
    # Side profiles sample the outer horizontal thirds of each source. Blur
    # these columns vertically while preserving the center of front/back art.
    vertical=np.stack([gaussian_filter(rgb[:,:,c].astype(np.float32),sigma=(72,2)) for c in range(3)],axis=2)
    x=np.linspace(0,1,rgb.shape[1])
    distance=np.abs(x-.5)
    weight=np.clip((distance-.16)/.20,0,1)
    weight=weight*weight*(3-2*weight)
    rgb=np.clip(rgb.astype(np.float32)*(1-weight[None,:,None])+vertical*weight[None,:,None],0,255).astype(np.uint8)
    opaque=Image.fromarray(np.dstack((rgb,np.full(valid.shape,255,dtype=np.uint8))),'RGBA').resize((1024,2048),Image.Resampling.LANCZOS)
    sharp.save(ROOT/f'{name}-edge-extended.png')
    opaque.save(ROOT/f'{name}-side-smoothed.png')
    sharp_parts.append(sharp);side_parts.append(opaque)
atlas=Image.new('RGBA',(4096,2048))
for slot,part in enumerate((sharp_parts[0],sharp_parts[1],side_parts[0],side_parts[1])):
    atlas.paste(part,(slot*1024,0))
atlas.save(ROOT/'body-atlas-candidate.png')
print('ATLAS',atlas.size)
