"""Native-source UV masks. No raster resampling or image mutation."""
import numpy as np
from scipy.spatial import ConvexHull
import math

def add_layers(cell):
    r=cell['rect']; w=r['width']; h=r['height']; bh=cell['bodyHeightPixels']
    pose=cell['pose']; d=cell['direction']; single=cell['texture']=='LowReadyFrontLeft-v1'
    base={'low':'BaseLow8-v1','aim':'BaseAim8-v1','compact':'BaseCompactAim8-v2','compact-low':'BaseCompactLow8-v1'}[pose]
    wl={'low':'WeaponlessLow8-v1','aim':'WeaponlessAim8-v1','compact':'WeaponlessCompactAim8-v1','compact-low':'WeaponlessCompactLow8-v1'}[pose]
    bw={'low':'BaseWeaponlessLow8-v1','aim':'BaseWeaponlessAim8-v1','compact':'BaseWeaponlessCompactAim8-v1','compact-low':'BaseWeaponlessCompactLow8-v1'}[pose]
    if single:base='BaseLowFrontLeft-v1';wl='WeaponlessLowFrontLeft-v1';bw='BaseWeaponlessLowFrontLeft-v1'
    cell['baseTexture']=base;cell['weaponlessTexture']=wl;cell['baseWeaponlessTexture']=bw
    def v(x,y):return {'x':round(max(0,min(w,x)),3),'y':round(max(0,min(h,y)),3)}
    def rect(x0,y0,x1,y1):return [v(x0,y0),v(x1,y0),v(x1,y1),v(x0,y1)]
    def ellipse(cx,cy,rx,ry):return [v(cx+rx*math.cos(i*math.pi/8),cy+ry*math.sin(i*math.pi/8)) for i in range(16)]
    c=cell['beltCenter']['x']; belt=cell['beltY']; foot=h*(1-cell['pivot']['y'])
    grips=[cell['grip'],cell['support']]
    radius=bh*.046
    hands=[(p['x']-radius,p['y']-radius,p['x']+radius,p['y']+radius) for p in grips]
    occluded=d==4 and pose!='low'
    if occluded:
        hands=[]
        cell['occludedSlots']=[2]
    cell['handPolygons']=[{'points':ellipse((b[0]+b[2])/2,(b[1]+b[3])/2,radius*.80,radius*.90)} for b in hands]
    # Source body persists except for actual armor removal regions. Arms slot is
    # the gloves; shoulder plates/harness/bedroll belong to the chest fieldkit.
    # Tight per-view head bounds. Broad upper-body rectangles incorrectly kept
    # shoulder plates when only Head was equipped (observed in stage3b).
    if single: headbox=(406-r['x'],0,657-r['x'],340-r['y'])
    else:
        hx=[233,588,944,1354,222,572,939,1351][d]-(d%4)*384
        hx+=(d%4)*384-r['x']
        top=0
        headbox=(hx-bh*.103,top,hx+bh*.103,bh*.223)
    head=rect(*headbox)
    head_bottom=headbox[3]
    chest=(0,bh*.14,w,min(h,belt+bh*.062))
    # Subdivide chest around both glove boxes so removing gloves cannot leave
    # glove ghosts in the independent chest layer.
    excludes=hands+[headbox]
    xs=sorted(set([chest[0],chest[2]]+[max(chest[0],min(chest[2],b[i])) for b in excludes for i in (0,2)]))
    ys=sorted(set([chest[1],chest[3]]+[max(chest[1],min(chest[3],b[i])) for b in excludes for i in (1,3)]))
    chestpolys=[]
    for x0,x1 in zip(xs,xs[1:]):
        for y0,y1 in zip(ys,ys[1:]):
            if x1-x0<.1 or y1-y0<.1:continue
            cx=(x0+x1)*.5;cy=(y0+y1)*.5
            if any(b[0]<=cx<=b[2] and b[1]<=cy<=b[3] for b in excludes):continue
            chestpolys.append(rect(x0,y0,x1,y1))
    ankle=foot-bh*.165
    # Isolated kneepads and boot contours avoid a horizontal fabric cut across
    # both thighs/ankles. Keep the original full-outfit source unchanged.
    knee_y=foot-bh*.31
    if single:
        knees=[(416-r['x'],1051-r['y']),(719-r['x'],1061-r['y'])]
        boot_shapes=[[(253,1333),(340,1250),(482,1260),(502,1368),(471,1396),(288,1404)],[(738,1310),(845,1293),(872,1458),(815,1476),(741,1448)]]
        bootpolys=[[v(px-r['x'],py-r['y']) for px,py in poly] for poly in boot_shapes]
    else:
        knees=[(c-bh*.16,knee_y),(c+bh*.15,knee_y)]
        bootpolys=[ellipse(c-bh*.19,foot-bh*.08,bh*.13,bh*.09),ellipse(c+bh*.18,foot-bh*.08,bh*.13,bh*.09)]
    legpolys=[ellipse(kx,ky,bh*.095,bh*.10) for kx,ky in knees]
    legpolys += [ellipse(c-bh*.19,belt+bh*.09,bh*.095,bh*.105),ellipse(c+bh*.19,belt+bh*.09,bh*.095,bh*.105)]
    # All thigh pouches must belong to a removable slot, including the lateral
    # extent beyond torso center; Chest now covers the complete waist width.
    slot_polys=[[head],chestpolys,[ellipse((b[0]+b[2])/2,(b[1]+b[3])/2,radius*.8,radius*.9) for b in hands],legpolys,bootpolys]
    cell['overlays']=[{'slot':slot,'visualVariant':4,'polygons':[{'points':p} for p in polys],'maskNote':'native source UV; original equipment pixels retained; chest excludes glove areas'} for slot,polys in enumerate(slot_polys)]
    # A convex gun-envelope hole, with original hand masks restoring fingers on
    # top. Background remains transparent; generated weaponless is only a cloth
    # patch inside this hole, never a replacement for the original arms.
    if occluded:
        cell['weaponPolygons']=[]
        cell['weaponOccluded']=True;cell['handsOccluded']=True
    else:
        pts=[]
        for k in ('stock','grip','support','muzzle'):
            if k=='stock' and pose=='low' and d in (3,4,5):continue
            p=cell[k];rad=bh*(.018 if k=='muzzle' else .035)
            pts.extend([(p['x']-rad,p['y']-rad),(p['x']+rad,p['y']-rad),(p['x']+rad,p['y']+rad),(p['x']-rad,p['y']+rad)])
        if pose in ('low','aim'):
            p=cell['grip'];q=cell['support']
            pts.append(((p['x']+q['x'])*.5,(p['y']+q['y'])*.5-bh*.10))
        pts=np.array(pts); hull=ConvexHull(pts)
        cell['weaponPolygons']=[{'points':[v(*pts[i]) for i in hull.vertices]}]
    return cell
