"""Prepare reviewed-source candidates, preserving authored alpha/hull/exclusions."""
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
ROOT=Path(__file__).resolve().parents[3]
OUT=Path('D:/Unity/AshfallArtValidation/equipment-generation')
def extract(key):
 q=json.loads((OUT/'queue.json').read_text(encoding='utf-8'));j=next(x for x in q['jobs'] if x['key']==key)
 im=Image.open(ROOT/j['source']).convert('RGBA');w,h=im.size
 mask=Image.new('L',im.size,255)
 if j['clip_hull']:
  mask=Image.new('L',im.size,0);ImageDraw.Draw(mask).polygon([(p['x']*w,(1-p['y'])*h) for p in j['clip_hull']],fill=255)
 draw=ImageDraw.Draw(mask)
 for e in j['clip_exclusions']:draw.rectangle((e['x']*w,(1-e['y']-e['height'])*h,(e['x']+e['width'])*w,(1-e['y'])*h),fill=0)
 im.putalpha(ImageChops.multiply(im.getchannel('A'),mask))
 c=j['crop_uv'];rect=(int(c['x']*w),int((1-c['y']-c['height'])*h),int((c['x']+c['width'])*w+.999),int((1-c['y'])*h+.999))
 im=im.crop(rect);box=im.getchannel('A').getbbox()
 if not box:raise ValueError('Empty art')
 im=im.crop(box);im.thumbnail((900,900),Image.Resampling.LANCZOS)
 canvas=Image.new('RGBA',(1024,1024));canvas.alpha_composite(im,((1024-im.width)//2,(1024-im.height)//2))
 target=OUT/key;target.mkdir(exist_ok=True);canvas.save(target/'source.png');(target/'source.json').write_text(json.dumps(j,indent=2),encoding='utf-8');print(str(target/'source.png'))
if __name__=='__main__':
 import sys
 extract(sys.argv[1])
