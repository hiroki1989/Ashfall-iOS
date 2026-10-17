"""Read native PNG alpha and emit geometry metadata; never modifies image pixels."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from scipy.ndimage import label, binary_dilation
from scipy.spatial import ConvexHull

root=Path(__file__).resolve().parents[1]/'Assets/Resources/Art/LayeredPlayer/HandgunArt'
# Measured (grip, muzzle, support, rear-of-action) in original atlas pixels.
# Rear-of-action defines the pistol barrel axis, not the bottom of its grip.
points={
14:[[(181,187),(64,147),(102,124),(160,51)],[(389,211),(552,113),(500,117),(417,69)],[(714,203),(899,96),(822,100),(698,96)],[(1061,212),(1177,57),(1133,110),(1072,157)],[(147,444),(175,320),(169,359),(123,388)],[(423,501),(330,346),(365,385),(440,435)],[(853,499),(604,385),(665,393),(845,390)],[(1076,492),(1186,447),(1141,422),(1069,382)]],
15:[[(177,791),(75,773),(113,727),(165,672)],[(399,824),(550,742),(499,728),(364,681)],[(825,835),(610,724),(677,733),(904,730)],[(1090,838),(1175,672),(1134,725),(1084,766)],[(136,1072),(174,965),(153,1005),(132,1019)],[(463,1155),(327,974),(377,1017),(493,1051)],[(828,1151),(592,986),(668,1000),(908,1004)],[(1114,1148),(999,1064),(1035,1029),(1145,1002)]],
16:[[(191,187),(66,209),(109,163),(190,61)],[(396,194),(566,193),(505,140),(372,74)],[(717,213),(946,132),(864,117),(669,109)],[(1049,208),(1170,66),(1125,112),(1048,143)],[(161,462),(194,317),(175,369),(154,410)],[(442,486),(348,348),(377,391),(453,416)],[(853,516),(612,415),(677,416),(904,420)],[(1111,497),(1002,479),(1050,420),(1122,363)]],
17:[[(192,800),(64,838),(106,782),(193,662)],[(392,800),(552,839),(488,785),(372,686)],[(691,811),(969,746),(881,744),(662,746)],[(1066,825),(1183,658),(1137,724),(1064,749)],[(157,1098),(213,931),(183,1002),(149,1048)],[(439,1127),(336,930),(380,1001),(434,1053)],[(836,1094),(578,1006),(671,1014),(914,1017)],[(1091,1091),(1005,1129),(1041,1072),(1121,996)]]}
regions=[];anchors=[]
corrections={(14,2,'grip'):(729,199),(14,6,'muzzle'):(608,385),(14,6,'stock'):(842,386),(15,2,'muzzle'):(613,724),(15,6,'muzzle'):(596,986),(15,7,'grip'):(1108,1137),(15,7,'muzzle'):(1002,1064),(16,6,'muzzle'):(620,415),(16,7,'muzzle'):(1004,480),(16,7,'stock'):(1124,366),(17,5,'grip'):(448,1127),(17,6,'grip'):(834,1092),(17,6,'muzzle'):(584,1011),(17,7,'stock'):(1129,1004)}
for sheet in (7,8):
    image=Image.open(root/f'Handguns_{sheet}.png');alpha=np.asarray(image)[:,:,3];height,width=alpha.shape
    labels,count=label(alpha>96)
    for cell in range(16):
        model=sheet*2+cell//8;direction=cell%8
        left=round(cell%4*width/4);top=round(cell//4*height/4);right=round((cell%4+1)*width/4);bottom=round((cell//4+1)*height/4)
        sizes=np.bincount(labels[top:bottom,left:right].ravel());sizes[0]=0;mask=binary_dilation(labels==sizes.argmax(),iterations=1)
        yy,xx=np.where(mask)
        hull=ConvexHull(np.column_stack((xx,yy)));outline=np.column_stack((xx,yy))[hull.vertices]
        assert len(outline)<=64
        regions.append(dict(model=model,direction=direction,x=float(xx.min()/width),y=float(yy.min()/height),width=float((xx.max()-xx.min()+1)/width),height=float((yy.max()-yy.min()+1)/height),hull=[dict(x=round(float(x/width),7),y=round(float(y/height),7)) for x,y in outline]))
        anchor=dict(model=model,direction=direction)
        for name,(x,y) in zip(('grip','muzzle','support','stock'),points[model][direction]):
            x,y=corrections.get((model,direction,name),(x,y))
            assert 0<=x<width and 0<=y<height,(model,direction,name,x,y)
            anchor[name]=dict(x=round((x-left)/(width/4),6),y=round((y-top)/(height/4),6))
        anchors.append(anchor)
assert len(regions)==len(anchors)==32
(root/'HandgunWeaponRegions.json').write_text(json.dumps(dict(regions=regions),indent=2),encoding='utf-8')
(root/'HandgunWeaponAnchors.json').write_text(json.dumps(dict(anchors=anchors),indent=2),encoding='utf-8')
(root/'HandgunsReady.json').write_text(json.dumps(dict(models=[14,15,16,17],views=8,nativeAtlasSize=[width,height])),encoding='utf-8')
print('32 regions and 32 measured anchors; native PNGs unchanged')
