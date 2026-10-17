"""Measure alpha only; never modify artwork pixels."""
from pathlib import Path
from PIL import Image
import numpy as np
from scipy.ndimage import label,find_objects
import json
from LayerMasks import add_layers
root=Path(__file__).parent
# Coordinates are hand-inspected source PNG pixels. Rear occluded contact points
# are projected estimates, explicitly marked, rather than visible-image claims.
points={
'compact-low':[(185,483,262,221,253,189,226,178,224,190,225),(530,486,263,593,252,558,225,545,231,557,223),(922,494,274,1031,264,1001,232,986,234,999,231),(1348,480,269,1470,258,1436,238,1421,240,1435,237),(176,983,758,185,725,183,703,179,710,183,703),(575,986,765,444,726,477,714,489,720,477,714),(993,984,766,881,753,918,718,928,726,919,718),(1355,983,767,1304,742,1331,713,1343,720,1332,712)],
'compact':[(186,482,260,183,157,183,135,189,149,181,134),(531,487,262,657,159,625,151,615,169,625,151),(924,493,272,1108,99,1068,117,1060,132,1067,116),(1349,479,267,1510,50,1467,75,1450,87,1468,75),(177,984,758,185,583,181,610,185,600,181,612),(575,987,766,427,530,450,568,463,578,450,568),(994,985,766,841,593,880,613,889,632,881,613),(1355,983,767,1261,668,1295,654,1304,666,1295,654)],
'low':[(223,496,270,388,330,214,224,281,255,154,157),(585,499,278,751,334,592,231,660,269,516,164),(930,503,286,1115,329,990,232,1040,270,905,177),(1341,500,286,1157,324,1260,237,1226,265,1320,198),(218,987,766,51,789,141,709,108,743,205,674),(584,989,768,405,791,513,712,478,751,590,677),(948,993,767,773,810,873,747,838,769,963,685),(584,1475,788,109,948,528,648,405,782,679,488)],
'aim':[(190,482,260,172,244,162,170,180,208,169,125),(532,487,260,709,264,552,168,600,205,478,119),(915,492,271,1177,118,990,145,1054,132,912,122),(1369,478,263,1499,48,1433,91,1461,78,1405,108),(191,977,760,204,588,203,617,202,600,207,612),(577,986,764,449,541,499,586,481,567,541,610),(1001,981,762,750,605,955,634,891,620,1031,614),(1345,982,763,1235,738,1337,653,1303,681,1369,620)]
}
cells=[]
for pose,coords in points.items():
    fn={'low':'LowReady8-v1','aim':'Aim8-v3','compact':'CompactAim8-v4','compact-low':'CompactLowReady8-v1'}[pose]
    im=Image.open(root/(fn+'.png')); w,h=im.size
    lab,n=label(np.array(im)[:,:,3]>128)
    regs=[]
    for i,s in enumerate(find_objects(lab)):
        if s and np.sum(lab[s]==i+1)>2000:
            regs.append((s[1].start,s[0].start,s[1].stop,s[0].stop))
    regs.sort(key=lambda r:(r[1]//500,r[0]))
    for d,(fx,fy,belt,mx,my,gx,gy,sx,sy,tx,ty) in enumerate(coords):
        texture=fn
        if pose=='low' and d==7:
            texture='LowReadyFrontLeft-v1'; rect=(102,63,872,1476)
        else: rect=regs[d]
        x,y,x2,y2=rect; x=max(0,x-3);y=max(0,y-3);x2=min((1024 if texture=='LowReadyFrontLeft-v1' else w),x2+3);y2=min((1536 if texture=='LowReadyFrontLeft-v1' else h),y2+3)
        rw=x2-x;rh=y2-y
        def point(px,py):return {'x':px-x,'y':py-y}
        seam=belt+28
        poly=[(x,y),(x2,y),(x2,seam),(x,seam)]
        if pose=='low':
            if d==0: poly=[(x,y),(x2,y),(x2,338),(382,339),(302,278),(302,seam),(x,seam)]
            if d==1: poly=[(x,y),(x2,y),(x2,341),(743,341),(664,280),(664,seam),(x,seam)]
            if d==2: poly=[(x,y),(x2,y),(x2,337),(1109,337),(1040,280),(1000,seam),(x,seam)]
            if d==3: poly=[(x,y),(x2,y),(x2,seam),(1245,seam),(1230,275),(1162,335),(1150,322),(1224,253),(x,253)]
            if d==6: poly=[(x,y),(x2,y),(x2,seam),(866,seam),(840,773),(781,817),(768,807),(831,750),(x,750)]
            if d==7: poly=[(x,y),(x2,y),(x2,seam),(380,seam),(384,797),(116,959),(x,944),(361,752),(x,752)]
        cell={'pose':pose,'direction':d,'texture':texture,'rect':{'x':x,'y':y,'width':rw,'height':rh},'pivot':{'x':round((fx-x)/rw,6),'y':round(1-(fy-y)/rh,6)},'muzzle':point(mx,my),'grip':point(gx,gy),'support':point(sx,sy),'stock':point(tx,ty),'beltY':belt-y,'beltCenter':point(fx,belt),'bodyHeightPixels':rh-6,'contactVisibility':'partially occluded; contact anchors estimated' if d in (3,4,5) else 'visible hand-measured','upperPolygon':[point(px,py) for px,py in poly]}
        if pose=='compact':
            starts=[(183,109),(624,126),(1060,99),(1475,60),(185,600),(468,551),(891,593),(1298,635)]
            cell['barrelStart']=point(*starts[d])
        dims=Image.open(root/(texture+'.png')).size
        cell['sourceWidth']=dims[0];cell['sourceHeight']=dims[1]
        cells.append(add_layers(cell))
from LowerMeasurements import landmarks,heightPixels
lower=[]
runNames=['Front','FrontRight','Right','BackRight','Back','BackLeft','Left','FrontLeft']
for d,name in enumerate(runNames):
    texture='Run'+name+('8-v2' if name=='Front' else '8-v1')
    im=Image.open(root/(texture+'.png')); w,h=im.size
    a=np.array(im)[:,:,3]>128
    lab,n=label(a); regs=[]
    for i,s in enumerate(find_objects(lab)):
        if s and np.sum(lab[s]==i+1)>2000:regs.append((s[1].start,s[0].start,s[1].stop,s[0].stop))
    regs.sort(key=lambda r:(r[1]//500,r[0]))
    assert len(regs)==8,(name,len(regs))
    for frame,(x,y,x2,y2) in enumerate(regs):
        top=y
        xs=np.where(a[y+8,x:x2])[0]
        hx=float(x+np.median(xs)); hy=y+12
        x=max(0,x-3);y=max(0,y-3);x2=min(w,x2+3);y2=min(h,y2+3)
        rw=x2-x;rh=y2-y
        ox=(frame%4)*384;oy=(frame//4)*512
        rkx,rky,lkx,lky,rfx,rfy,lfx,lfy=landmarks[d][frame]
        def p(px,py):return {'x':round(px-x,3),'y':round(py-y,3)}
        floor=max(rfy,lfy)+oy
        lc={'direction':d,'frame':frame,'texture':texture,'sourceWidth':w,'sourceHeight':h,'rect':{'x':x,'y':y,'width':rw,'height':rh},'pivot':{'x':round((hx-x)/rw,6),'y':round(1-(floor-y)/rh,6)},'bodyHeightPixels':heightPixels[d],'beltY':hy-y,'hip':p(hx,hy),'rightKnee':p(ox+rkx,oy+rky),'leftKnee':p(ox+lkx,oy+lky),'rightFoot':p(ox+rfx,oy+rfy),'leftFoot':p(ox+lfx,oy+lfy),'measurementNote':'hand-inspected native pixel landmarks; hidden knee centers estimated; approximately 5-12px precision'}
        lc['baseTexture']='RunBase'+name+'8-v1'
        if d==1 and frame==0:
            # Native RunFrontRight8-v1 first frame. Near pouch-bearing leg is
            # anatomical right; rear curled boot is anatomical left.
            lc['pelvisPolygon']=[p(px,py) for px,py in [(170,54),(296,54),(310,151),(255,202),(200,185),(159,139)]]
            lc['rightLegPolygon']=[p(px,py) for px,py in [(208,142),(294,143),(325,223),(335,337),(376,379),(413,427),(411,474),(316,477),(270,443),(248,354),(230,286),(212,228)]]
            lc['leftLegPolygon']=[p(px,py) for px,py in [(167,133),(214,158),(241,216),(201,252),(146,277),(119,319),(102,347),(67,348),(51,312),(63,255),(98,219),(151,199)]]
        def box(x0,y0,x1,y1):
            return [{'x':max(0,min(rw,xx)),'y':max(0,min(rh,yy))} for xx,yy in [(x0,y0),(x1,y0),(x1,y1),(x0,y1)]]
        boots=[]
        for key in ('rightFoot','leftFoot'):
            q=lc[key];boots.append((max(0,q['x']-65),max(0,q['y']-106),min(rw,q['x']+65),min(rh,q['y']+9)))
        xs=sorted(set([0,rw]+[v for b in boots for v in (b[0],b[2])]))
        ys=sorted(set([0,rh]+[v for b in boots for v in (b[1],b[3])]))
        leg=[]
        for x0,x1 in zip(xs,xs[1:]):
            for y0,y1 in zip(ys,ys[1:]):
                cx=(x0+x1)/2;cy=(y0+y1)/2
                if x1>x0 and y1>y0 and not any(b[0]<=cx<=b[2] and b[1]<=cy<=b[3] for b in boots):leg.append({'points':box(x0,y0,x1,y1)})
        lc['overlays']=[{'slot':3,'visualVariant':4,'polygons':leg},{'slot':4,'visualVariant':4,'polygons':[{'points':box(*b)} for b in boots]}]
        lower.append(lc)
from RigidD1 import apply_rigid
apply_rigid(cells,lower)
out={'version':1,'visualVariant':4,'height':2.2,'coordinateConvention':'rect top-left pixels; pivot rect normalized bottom-left','cells':cells,'lowerCells':lower}
(root/'Poses-v1.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
print('Wrote',len(cells),'pose cells; artwork unchanged')
