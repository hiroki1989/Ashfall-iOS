"""Measure the approved orthographic cutouts for Kestrel helmet geometry."""
import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path('D:/Unity/AshfallArtValidation/equipment3d-batch/kestrel-field-helmet/projection-views')
OUT=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelFieldHelmet/Candidates')
OUT.mkdir(parents=True,exist_ok=True)
arrays={n:np.asarray(Image.open(ROOT/f'{n}.png').getchannel('A')) for n in ('front','right','back','left')}
def bounds(arr,row):
    strip=arr[max(0,row-4):min(1024,row+5)]>128
    xs=np.where(strip)[1]
    return [int(xs.min()),int(xs.max())] if len(xs) else [512,512]

xs=[]
for i in range(29):
    z=.1425-.285*i/28
    front_row=round(61+901*i/28)
    side_row=round(66+890*i/28)
    f=bounds(arrays['front'],front_row)
    r=bounds(arrays['right'],side_row)
    rx=min(.110,max(.005,(f[1]-f[0])*.5*.245/843))
    yback=(512-r[0])*.26/905
    yfront=(512-r[1])*.26/905
    xs.append({'z':round(z,6),'rx':round(rx,6),'yfront':round(yfront,6),'yback':round(yback,6),'frontPixels':f,'sidePixels':r})
report={'productId':'distinct.armor.030','dimensionsMeters':[.245,.26,.285],
        'viewPixelBounds':{'front':[90,61,933,962],'right':[59,66,964,956],'back':[62,64,961,958]},
        'rings':xs}
(OUT/'silhouette-measurements.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Kestrel silhouette rings',len(xs),'mid',xs[14])
