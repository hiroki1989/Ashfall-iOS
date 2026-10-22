from pathlib import Path
from PIL import Image
import numpy as np,json
from scipy.ndimage import binary_fill_holes,label,distance_transform_edt
R=Path('D:/Unity/Ashfall-iOS/Assets/ArtSource/GeneratedEquipment/KestrelFieldHelmet');O=R/'Candidates/TwoViewOnly';O.mkdir(exist_ok=True)
src=Image.open(R/'kestrel-field-helmet-blueprint-sheet.png').convert('RGB');w,h=src.size
atlas=Image.new('RGBA',(2048,1024));metadata=[]
for index,(name,box) in enumerate([('front',(0,0,w//2,h//2)),('back',(0,h//2,w//2,h))]):
 im=src.crop(box);a=np.array(im);edge=np.concatenate([a[:10].reshape(-1,3),a[-10:].reshape(-1,3),a[:,:10].reshape(-1,3),a[:,-10:].reshape(-1,3)]);bg=np.median(edge,axis=0)
 # Only the connected helmet silhouette; background remains truly transparent
 # in the extracted sources. Source RGB is never repainted.
 mask=np.linalg.norm(a.astype(float)-bg,axis=2)>35;lab,n=label(mask);sizes=np.bincount(lab.ravel());sizes[0]=0;mask=binary_fill_holes(lab==sizes.argmax());ys,xs=np.where(mask);bbox=(int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1))
 alpha=np.where(mask,255,0).astype('uint8');rgba=Image.fromarray(np.dstack([a,alpha]));rgba.save(O/(name+'-source.png'))
 crop=rgba.crop(bbox);occupancy=.90*1.05;scale=1024*occupancy/max(crop.size);size=tuple(round(v*scale) for v in crop.size);resized=crop.resize(size,Image.Resampling.LANCZOS);r=np.array(resized);opaque=r[:,:,3]>16;ii=distance_transform_edt(~opaque,return_distances=False,return_indices=True);filled=r[tuple(ii)];filled[:,:,3]=255
 tile=np.empty((1024,1024,4),dtype='uint8');tile[:]=np.r_[np.median(filled[:,:,:3][opaque],axis=0),255];x0=(1024-size[0])//2;y0=(1024-size[1])//2;tile[y0:y0+size[1],x0:x0+size[0]]=filled
 # Extend nearest visible RGB through the whole tile to keep UV edge padding opaque.
 fullmask=np.zeros((1024,1024),bool);fullmask[y0:y0+size[1],x0:x0+size[0]]=opaque;jj=distance_transform_edt(~fullmask,return_distances=False,return_indices=True);tile=tile[tuple(jj)];tile[:,:,3]=255
 atlas.paste(Image.fromarray(tile),(index*1024,0));metadata.append({'view':name,'source_crop_sheet_pixels':box,'source_bbox_local':bbox,'placed_bbox':[x0,y0,x0+size[0],y0+size[1]],'occupied_long_edge':max(size)/1024,'mirrored':False})
atlas.save(O/'helmet-front-back-atlas.png');(O/'atlas-manifest.json').write_text(json.dumps({'source':str(R/'kestrel-field-helmet-blueprint-sheet.png'),'atlas_size':[2048,1024],'baseline_occupancy':.90,'occupancy_scale':1.05,'occupancy':.945,'views':metadata,'projection':'Pending Blender. Front/back only; no side image, no camera-dependent switch.'},indent=2));print(metadata)
