"""Pack every authored firearm illustration once; map all 606 catalog IDs.

Regular variants share the same approved 144 series illustrations but keep
their distinct procedural geometry. No recolor or fabricated product art.
"""
import json
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw, ImageChops
from prepare import tile_image

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'Assets/Resources/Art'
OUT = ROOT / 'Assets/StreamingAssets/WeaponFourView'
MAKERS = ('kestrel','ironwake','voltarc','mireline','cinder','aegis')
CLASSES = ('assault','handgun','shotgun','lmg','smg','sniper')
SERIES = ('patrol','carbine','battle','marksman'),('compact','service','revolver','hand-cannon'),('pump','coach','short-drum','semi-auto'),('belt-box','pan','drum','heavy'),('receiver','top-magazine','helical','drum'),('wood-bolt','chassis','bullpup','anti-materiel')
VARIANTS = ('Field','Short','Reinforced','Range')


def clipped(sheet, rect, metadata):
    """Respect the same authored hull and exclusion regions as item portraits."""
    x0,y0,x1,y1=rect
    crop=sheet.crop(rect).convert('RGBA')
    mask=Image.new('L',crop.size,0)
    polygon=[(round(p['x']*sheet.width)-x0,round((1-p['y'])*sheet.height)-y0)
             for p in metadata.get('hull',[])]
    if len(polygon)>=3:
        ImageDraw.Draw(mask).polygon(polygon,fill=255)
    else:
        ImageDraw.Draw(mask).rectangle((0,0,crop.width,crop.height),fill=255)
    draw=ImageDraw.Draw(mask)
    for box in metadata.get('exclusions',[]):
        left=round(box['x']*sheet.width)-x0
        top=round((1-box['y']-box['height'])*sheet.height)-y0
        right=round((box['x']+box['width'])*sheet.width)-x0
        bottom=round((1-box['y'])*sheet.height)-y0
        draw.rectangle((left,top,right,bottom),fill=0)
    crop.putalpha(ImageChops.multiply(crop.getchannel('A'),mask))
    return crop


def pack(paint, label, filename, source_kind, cell, source_file, tile_size=512):
    side0, side0_meta = tile_image(paint.copy(), label, tile_size)
    side1, side1_meta = tile_image(ImageOps.mirror(paint), label+' / mirrored', tile_size)
    end0, end0_meta = tile_image(paint.copy(), label+' / muzzle neutral', tile_size, end=True)
    end1, end1_meta = tile_image(paint.copy(), label+' / stock neutral', tile_size, end=True)
    atlas = Image.new('RGBA',(tile_size*2,tile_size*2))
    for i, image in enumerate((side0,side1,end0,end1)):
        atlas.paste(image,((i%2)*tile_size,(i//2)*tile_size))
    path = OUT/'Atlases'/filename
    path.parent.mkdir(parents=True,exist_ok=True)
    atlas.save(path,optimize=True)
    info = {'schema':1,'atlas':str(path.relative_to(OUT)).replace('\\','/'),
            'tilePixels':tile_size,'layoutTopLeft':[['left','right'],['front','back']],
            'views':dict(zip(('left','right','front','back'),
                             (side0_meta,side1_meta,end0_meta,end1_meta))),
            'sourceKind':source_kind,'sourceFile':str(source_file.relative_to(ROOT)).replace('\\','/'),
            'sourceCell':cell,'paintedSides':1,'mirroredSides':1,'paintedEndCaps':0,
            'status':'projection-input'}
    path.with_suffix('.json').write_text(json.dumps(info,ensure_ascii=False,indent=2),encoding='utf-8')
    return str(path.relative_to(OUT)).replace('\\','/')


def regular():
    entries=[]
    for maker, name in enumerate(MAKERS):
        sheet_path=SOURCE/'GunSeries'/f'{name}-series-v1.png'
        bounds=json.loads(sheet_path.with_name(sheet_path.stem+'-bounds.json').read_text(encoding='utf-8'))
        with Image.open(sheet_path) as sheet:
            for cls in range(6):
                for series in range(4):
                    cell=cls*4+series
                    rect=bounds['pixelBoundsTopLeft'][cell]
                    crop=clipped(sheet,tuple(rect),bounds['cells'][cell])
                    base=f'{name}-{CLASSES[cls]}-{SERIES[cls][series]}'
                    atlas=pack(crop,f'{sheet_path.name} cell {cell}',base+'.png','GunSeries',cell,sheet_path)
                    for variant in range(4):
                        index=30+(cls*16+series*4+variant)*6+maker
                        entries.append({'catalogId':f'distinct.gun.{index:03d}',
                                        'name':f'{name.title()} {SERIES[cls][series]} {CLASSES[cls]} / {VARIANTS[variant]}',
                                        'maker':maker,'gunClass':cls,'series':series,'seriesVariant':variant,
                                        'atlas':atlas,'geometry':'GunSeriesModels3D.Build',
                                        'paintingSharedWithinSeries':True,'status':'texture-ready-mesh-uv-pending'})
    # Eight freshly painted series per maker/class. The two-by-four sheet is
    # deliberately separate from the older four-series composition.
    for maker,name in enumerate(MAKERS):
        for cls,class_name in enumerate(CLASSES):
            sheet_path=SOURCE/'GunSeriesExpanded'/f'{name}-{class_name}-series-v2.png'
            cells=json.loads(sheet_path.with_name(sheet_path.stem+'-bounds.json').read_text(encoding='utf-8'))['cells']
            with Image.open(sheet_path) as sheet:
                for series in range(8):
                    b=cells[series];w,h=sheet.size
                    left=round(b['x']*w);right=round((b['x']+b['width'])*w)
                    top=round((1-b['y']-b['height'])*h);bottom=round((1-b['y'])*h)
                    crop=sheet.crop((left,top,right,bottom)).convert('RGBA')
                    atlas=pack(crop,f'{sheet_path.name} cell {series}',f'{name}-{class_name}-expanded-{series}.png','GunSeriesExpanded',series,sheet_path,256)
                    for variant in range(4):
                        index=606+(cls*32+series*4+variant)*6+maker
                        entries.append({'catalogId':f'distinct.gun.{index:03d}',
                                        'name':f'{name.title()} expanded {class_name} {series} / {VARIANTS[variant]}',
                                        'maker':maker,'gunClass':cls,'series':series+4,'seriesVariant':variant,
                                        'atlas':atlas,'geometry':'GunSeriesModels3D.Build',
                                        'paintingSharedWithinSeries':True,'status':'texture-ready-mesh-uv-pending'})
    return entries


def legendary():
    entries=[]
    for maker,name in enumerate(MAKERS):
        sheet_path=SOURCE/'BespokeLegendaries'/f'{name}-legendaries-v1.png'
        cells=json.loads(sheet_path.with_name(sheet_path.stem+'-bounds.json').read_text(encoding='utf-8'))['cells']
        with Image.open(sheet_path) as sheet:
            width,height=sheet.size
            for cell in range(5):
                b=cells[cell]
                left=round(b['x']*width); right=round((b['x']+b['width'])*width)
                top=round((1-b['y']-b['height'])*height); bottom=round((1-b['y'])*height)
                crop=clipped(sheet,(left,top,right,bottom),b)
                atlas=pack(crop,f'{sheet_path.name} cell {cell}',f'{name}-legendary-{cell}.png','BespokeLegendaries',cell,sheet_path)
                index=maker*5+cell
                entries.append({'catalogId':f'distinct.gun.{index:03d}',
                                'name':f'{name.title()} legendary {cell}', 'maker':maker,
                                'atlas':atlas,'geometry':'BespokeLegendaryModels3D.BuildWeapon',
                                'status':'texture-ready-mesh-uv-pending'})
    for maker,name in enumerate(MAKERS):
        sheet_path=SOURCE/'BespokeLegendariesExpanded'/f'{name}-legendaries-v2.png'
        cells=json.loads(sheet_path.with_name(sheet_path.stem+'-bounds.json').read_text(encoding='utf-8'))['cells']
        with Image.open(sheet_path) as sheet:
            w,h=sheet.size
            for cell,b in enumerate(cells):
                left=round(b['x']*w);right=round((b['x']+b['width'])*w)
                top=round((1-b['y']-b['height'])*h);bottom=round((1-b['y'])*h)
                crop=sheet.crop((left,top,right,bottom)).convert('RGBA')
                atlas=pack(crop,f'{sheet_path.name} cell {cell}',f'{name}-legendary-expanded-{cell}.png','BespokeLegendariesExpanded',cell,sheet_path,256)
                index=1758+maker*10+cell
                entries.append({'catalogId':f'distinct.gun.{index:03d}',
                                'name':f'{name.title()} legendary expanded {cell}', 'maker':maker,
                                'atlas':atlas,'geometry':'BespokeLegendaryModels3D.BuildWeapon',
                                'status':'texture-ready-mesh-uv-pending'})
    return entries


def main():
    entries=regular()+legendary()
    if len(entries)!=1818 or len({e['catalogId'] for e in entries})!=1818:
        raise AssertionError('catalog coverage is not 1818 unique gun IDs')
    manifest={'schema':1,'productCount':1818,'uniquePaintings':522,
              'regularProductCount':1728,'legendaryProductCount':90,
              'runtimeRequirement':'Apply the four-view UV projection to each currently generated mesh, preserving procedural series and variant geometry. Texture alone does not register an exact product model.',
              'products':sorted(entries,key=lambda e:e['catalogId'])}
    (OUT/'product-catalog.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'products':len(entries),'atlases':522,'output':str(OUT/'product-catalog.json')}))


if __name__=='__main__':
    main()
