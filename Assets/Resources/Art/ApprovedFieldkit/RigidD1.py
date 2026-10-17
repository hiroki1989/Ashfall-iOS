"""Native rigid parts landmarks. Original PNG is never resampled."""
from pathlib import Path
from PIL import Image
import numpy as np
import json
def apply_rigid(cells,lower):
    specs=[
      ('rightThigh',(475,5,282,477),(588,109),(677,438),1530),
      ('rightShin',(868,7,184,490),(964,90),(971,438),1595),
      ('rightBoot',(1172,149,343,337),(1252,280),(1465,406),2475),
      ('leftThigh',(108,494,274,468),(235,604),(195,925),1455),
      ('leftShin',(505,504,184,488),(596,587),(605,936),1600),
      ('leftBoot',(848,624,372,351),(943,757),(1170,913),2750),
    ]
    def part(name,rect,start,end,bh):
        x,y,w,h=rect
        return {'name':name,'texture':'RigidD1-v2','sourceWidth':1536,'sourceHeight':1024,'rect':{'x':x,'y':y,'width':w,'height':h},'start':{'x':start[0]-x,'y':start[1]-y},'end':{'x':end[0]-x,'y':end[1]-y},'pivot':{'x':(start[0]-x)/w,'y':1-(start[1]-y)/h},'bodyHeightPixels':bh}
    parts=[part(*s) for s in specs]
    for item in parts:
        rr=item['rect']
        im=Image.open(Path(__file__).parent/(item['texture']+'.png'))
        alpha=np.array(im)[rr['y']:rr['y']+rr['height'],rr['x']:rr['x']+rr['width'],3]
        yy,xx=np.where(alpha>128)
        if 'Boot' in item['name']:
            width=float(xx.max()-xx.min()+1)
        else:
            start=item['start'];end=item['end']
            axis=np.array([end['x']-start['x'],end['y']-start['y']],dtype=float)
            normal=np.array([-axis[1],axis[0]])/np.linalg.norm(axis)
            projected=xx*normal[0]+yy*normal[1]
            width=float(projected.max()-projected.min()+1)
        item['visibleWidthPixels']=round(width,4)
    parts[2]['sole']={'x':1371-1172,'y':453-149}
    parts[5]['sole']={'x':1067-848,'y':942-624}
    neutral=json.loads((Path(__file__).parent/'NeutralD1-v1.json').read_text(encoding='utf-8'))
    source=np.array(Image.open(Path(__file__).parent/'LowReady8-v1.png'))[:,:,3]
    for item in parts:
        side='right' if item['name'].startswith('right') else 'left'
        kind=item['name'][len(side):]
        keys={'Thigh':('Hip','Knee'),'Shin':('Knee','Ankle'),'Boot':('Ankle','Sole')}[kind]
        ss=neutral[side+keys[0]];se=neutral[side+keys[1]]
        target=np.array([se['x']-ss['x'],se['y']-ss['y']],float)
        a=item['start']; b=item['sole'] if kind=='Boot' else item['end']
        origin=np.array([a['x'],a['y']],float);axis=np.array([b['x']-a['x'],b['y']-a['y']],float)
        angle=np.arctan2(target[1],target[0])-np.arctan2(axis[1],axis[0])
        rot=np.array([[np.cos(angle),-np.sin(angle)],[np.sin(angle),np.cos(angle)]])
        rr=item['rect'];im=np.array(Image.open(Path(__file__).parent/(item['texture']+'.png')))
        yy,xx=np.where(im[rr['y']:rr['y']+rr['height'],rr['x']:rr['x']+rr['width'],3]>128)
        transformed=(np.column_stack([xx,yy])-origin)@rot.T
        mid=(axis@rot.T)*.5
        scan=transformed[np.abs(transformed[:,1]-mid[1])<=.75,0]
        item['midCrossSectionPixels']=round(float(scan.max()-scan.min()+1),4)
        item['midCrossSectionNote']='alpha horizontal span at joint midpoint after rotation to source neutral joint direction; protruding pouches included if intersecting scan'
    pelvis=part('pelvis',(38,54,363,302),(228,98),(245,242),1800)
    pelvis['rightHip']={'x':180-38,'y':220-54}
    pelvis['leftHip']={'x':292-38,'y':207-54}
    for lc in lower:
        if lc['direction']==1:
            lc['rigidParts']=parts;lc['pelvisPart']=pelvis
            # Original lower waist visible upper edge; no nominal belt offset.
            lc['waistAttach']={'x':lc['hip']['x'],'y':max(0,lc['hip']['y']-8)}
    for c in cells:
        if c['direction']==1:
            # Complete upper's cloth hem, source native top-left.
            yy={'low':284,'aim':274,'compact':273,'compact-low':273}[c['pose']]
            xx={'low':582,'aim':527,'compact':524,'compact-low':523}[c['pose']]
            c['hemAttach']={'x':xx-c['rect']['x'],'y':yy-c['rect']['y']}
