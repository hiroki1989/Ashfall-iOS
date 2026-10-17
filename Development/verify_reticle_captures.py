"""Measure actual game framebuffer captures; never writes or transforms image pixels."""
from pathlib import Path
import csv,json,sys
from PIL import Image
import numpy as np
from scipy.ndimage import label

folder=Path(sys.argv[1]);rows=list(csv.DictReader((folder/'reticle-accuracy-settings.csv').open()))
measured=[]
for row in rows:
    pixels=np.asarray(Image.open(folder/row['image']).convert('RGB'));height,width=pixels.shape[:2]
    assert (width,height)==(int(row['width']),int(row['height']))
    scale=float(row['uiScale']);center=np.array([float(row['logicalCenterX'])*scale,float(row['logicalCenterY'])*scale])
    x,y=map(round,center);radius=round(24*scale);crop=pixels[y-radius:y+radius+1,x-radius:x+radius+1]
    # Reticle bars are cyan-white; white center is deliberately excluded.
    mask=(crop[:,:,0]>140)&(crop[:,:,0]<245)&(crop[:,:,1]>225)&(crop[:,:,2]>210)
    components,count=label(mask);bars=[]
    for component in range(1,count+1):
        yy,xx=np.where(components==component)
        if len(xx)<max(1,round(3*scale)):continue
        point=np.array([xx.mean()+x-radius,yy.mean()+y-radius]);delta=point-center
        if np.linalg.norm(delta)>20*scale:continue
        bars.append(delta)
    assert len(bars)==4,(row['image'],'expected four adjacent bars',bars)
    expected=float(row['baseGap'])+3
    for delta in bars:
        axial=max(abs(delta));cross=min(abs(delta));assert cross<max(1.2,scale),('off-center bar',row['image'],delta)
        assert abs(axial/scale-expected)<2,('wrong accuracy gap',row['image'],delta,expected)
    white=pixels[max(0,y-2):y+2,max(0,x-2):x+2];assert np.any((white>245).all(axis=2)),('missing central white point',row['image'])
    measured.append(dict(image=row['image'],accuracy=float(row['accuracy']),uiScale=scale,barCenterDistance=float(np.mean([max(abs(d)) for d in bars])/scale),barOffsets=[d.tolist() for d in bars]))
for scale in set(m['uiScale'] for m in measured):
    rifle=next(m for m in measured if m['accuracy']==94 and m['uiScale']==scale)
    shotgun=next(m for m in measured if m['accuracy']==52 and m['uiScale']==scale)
    assert shotgun['barCenterDistance']>rifle['barCenterDistance']+4
result=dict(result='PASS',captures=len(measured),checks='Actual center white point; four adjacent cardinal bars; rifle tighter than shotgun; two resolutions',measurements=measured)
(folder/'reticle-pixel-result.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))
