"""Read native alpha only; transfer semantic anchors with the preserved outline."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from scipy.ndimage import label, binary_dilation
from scipy.spatial import ConvexHull
root=Path(__file__).resolve().parents[1]/'Assets/Resources/Art/LayeredPlayer'
target=root/'InkedArt'
old=json.loads((root/'TacticalArt/ModernWeaponRegions.json').read_text())['regions']
anchors=json.loads((root/'TacticalArt/ModernWeaponAnchors.json').read_text())['anchors']
a=np.asarray(Image.open(target/'Weapons_6.png'))[:,:,3];h,w=a.shape
labels,_=label(a>96);regions=[]
for cell in range(16):
    model=12+cell//8;direction=cell%8;cx=cell%4;cy=cell//4
    sizes=np.bincount(labels[round(cy*h/4):round((cy+1)*h/4),round(cx*w/4):round((cx+1)*w/4)].ravel());sizes[0]=0
    mask=binary_dilation(labels==sizes.argmax(),iterations=1);yy,xx=np.where(mask)
    pts=np.column_stack((xx,yy));outline=pts[ConvexHull(pts).vertices]
    region=dict(model=model,direction=direction,x=float(xx.min()/w),y=float(yy.min()/h),width=float((xx.max()-xx.min()+1)/w),height=float((yy.max()-yy.min()+1)/h),hull=[dict(x=float(x/w),y=float(y/h)) for x,y in outline])
    assert len(outline)<=64
    previous=next(r for r in old if r['model']==model and r['direction']==direction)
    anchor=next(r for r in anchors if r['model']==model and r['direction']==direction)
    for name in ['grip','muzzle','support','stock']:
        p=anchor[name];gx=(cx+p['x'])/4;gy=(cy+p['y'])/4
        p['x']=4*(region['x']+(gx-previous['x'])/previous['width']*region['width'])-cx
        p['y']=4*(region['y']+(gy-previous['y'])/previous['height']*region['height'])-cy
    regions.append(region)
(target/'WeaponRegions.json').write_text(json.dumps(dict(regions=regions),indent=2))
(target/'WeaponAnchors.json').write_text(json.dumps(dict(anchors=anchors),indent=2))
print('16 native weapon hulls; anchors transferred using measured old/new outlines; visual grip review required')
