from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np
from scipy.ndimage import binary_fill_holes, label

ROOT=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/BaseBody/Candidates')
ROOT.mkdir(parents=True,exist_ok=True)
SOURCES={
 'front':Path(r'C:/Users/hiroki/AppData/Local/Temp/codex-clipboard-ce85cca4-af4f-4514-b590-c7a481cce0b3.png'),
 'back':Path(r'C:/Users/hiroki/.codex/generated_images/01a15e0c-5dc7-7492-840b-2e5c1044e649/exec-9ab12ceb-4703-4dd6-a029-3eb8192852b9.png'),
}
for name,source in SOURCES.items():
 im=Image.open(source).convert('RGB')
 im.save(ROOT/f'{name}-original.png')
 a=np.asarray(im).astype(np.int16)
 saturation=a.max(2)-a.min(2)
 intensity=a.mean(2)
 foreground=(saturation>13)|(intensity<155)
 # Isolate the central full-body figure; background gradients remain neutral.
 labs,count=label(foreground)
 sizes=np.bincount(labs.ravel());sizes[0]=0
 mask=binary_fill_holes(labs==sizes.argmax())
 ys,xs=np.where(mask)
 crop=im.crop((xs.min(),ys.min(),xs.max()+1,ys.max()+1))
 alpha=Image.fromarray((mask.astype('uint8')*255)).crop((xs.min(),ys.min(),xs.max()+1,ys.max()+1)).filter(ImageFilter.GaussianBlur(.55))
 crop.putalpha(alpha)
 scale=min(900/crop.width,1380/crop.height)
 size=(round(crop.width*scale),round(crop.height*scale))
 crop=crop.resize(size,Image.Resampling.LANCZOS)
 canvas=Image.new('RGBA',(1024,1536),(0,0,0,0))
 canvas.alpha_composite(crop,((1024-size[0])//2,(1536-size[1])//2))
 canvas.save(ROOT/f'{name}-projection.png')
 print(name,'source',im.size,'crop',size,'opaque_fraction',round(np.mean(mask),3))
