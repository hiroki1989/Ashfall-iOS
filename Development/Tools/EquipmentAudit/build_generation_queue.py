"""List unique illustration/shape work; never treats procedural art as generated art."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=Path('D:/Unity/AshfallArtValidation/equipment-generation')
MAKERS=('kestrel','ironwake','voltarc','mireline','cinder','aegis')

def main():
    jobs={};products=[]
    def job(key,kind,maker,**values):
        if key not in jobs:
            jobs[key]=dict(key=key,kind=kind,maker=maker,status='needs_source_review',**values)
        return key
    for kind,count in (('gun',606),('armor',500)):
        for i in range(count):
            legendary=i<30;maker=i//5 if legendary else i%6
            local=i%5 if legendary else (i-30)//6+5
            item=f'distinct.{kind}.{i:03d}'
            if legendary:
                keys=[job(item,kind,MAKERS[maker],legendary=True,illustration='bespoke',index=i%5,
                          source=f'Assets/Resources/Art/BespokeLegendaries/{MAKERS[maker]}-legendaries-v1.png',
                          cell=(5 if kind=='armor' else 0)+i%5,
                          portrait_implementation='BespokeLegendaryPortraits.cs / PopItemPreview.shader')]
            elif kind=='gun':
                k=(i-30)//6;c=k//16;s=k//4%4
                keys=[job(f'gun.{MAKERS[maker]}.class{c}.series{s}',kind,MAKERS[maker],
                          legendary=False,gun_class=c,series=s,
                          source=f'Assets/Resources/Art/GunSeries/{MAKERS[maker]}-series-v1.png',cell=c*4+s)]
            else:
                slot=local%5;body=local%6;extra=local//6%6
                source=f'Assets/Resources/Art/GroundedEquipment/{MAKERS[maker]}-armor-parts-v1.png'
                if not (ROOT/source).exists():source=source.replace('GroundedEquipment','DistinctEquipment')
                keys=[job(f'armor.{MAKERS[maker]}.slot{slot}.body{body}',kind,MAKERS[maker],
                          legendary=False,slot=slot,source=source,cell=slot*6+body,part='main'),
                      job(f'armor.{MAKERS[maker]}.slot{slot}.extra{extra}',kind,MAKERS[maker],
                          legendary=False,slot=slot,part='accessory',cell=slot*6+extra,
                          source=f'Assets/Resources/Art/DistinctEquipment/{MAKERS[maker]}-armor-accessories-v1.png')]
            directory=ROOT/'Assets/Resources/Art/GeneratedEquipment'/kind/item
            ready=(directory/'mesh.fbx').exists() and (directory/'atlas.png').exists()
            products.append(dict(id=item,kind=kind,jobs=keys,status='awaiting_runtime_qa' if ready else 'pending'))
    # Resolve the authored cell metadata rather than guessing a regular atlas grid.
    # Coordinates in bounds/hull/exclusions use full-texture UV, bottom-left origin.
    for entry in jobs.values():
        source=ROOT/entry['source']
        if not source.is_file():raise FileNotFoundError(source)
        metadata=source.with_name(source.stem+'-bounds.json')
        if not metadata.is_file():raise FileNotFoundError(metadata)
        cells=json.loads(metadata.read_text(encoding='utf-8-sig'))['cells']
        cell=cells[entry['cell']]
        entry.update(source_exists=True,bounds_source=metadata.relative_to(ROOT).as_posix(),
                     crop_uv={k:cell[k] for k in ('x','y','width','height')},
                     uv_origin='bottom_left',
                     clip_hull=cell.get('hull',[])[:64],
                     clip_exclusions=cell.get('exclusions',[])[:32] if not entry['legendary'] else [],
                     clip_space='full_texture_uv')
        if entry['legendary']:
            if len(cells)!=10 or len(entry['clip_hull'])<3:raise ValueError(f'Invalid bespoke metadata: {metadata}')
            entry['clip_rule']='convex hull: reject mixed signed edge crosses beyond +/-0.000002; preserve source alpha'
            entry['portrait_padding']=dict(local_uv='(uv - 0.08) / 0.84',crop_only_for_3d=True)
    art=ROOT/'Assets/Resources/Art'
    inventory=[]
    for folder in ('ApprovedFieldkit','BespokeLegendaries','DistinctEquipment','GroundedEquipment','GunSeries'):
        for source in sorted((art/folder).glob('*.png')):
            metadata=source.with_name(source.stem+'-bounds.json')
            inventory.append(dict(source=source.relative_to(ROOT).as_posix(),exists=source.is_file(),
                                  bytes=source.stat().st_size,
                                  bounds_source=metadata.relative_to(ROOT).as_posix() if metadata.is_file() else None))
    OUT.mkdir(parents=True,exist_ok=True)
    result=dict(contract=dict(chest_includes_sleeves=True,belt_height_m=1.04,feet_seam_m=.2882,
                    head_retained=True,hide_equipped_base=True,module_3d=False,
                    texture='fixed face-exclusive projection; armor front/back, gun left/right',
                    shape='illustration-derived independent mesh; no old-primitive substitution',
                    concurrency=1,ai_texture_generation=False,auto_publish=False),
                source_inventory=inventory,jobs=list(jobs.values()),products=products)
    (OUT/'queue.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(dict(products=len(products),unique_jobs=len(jobs),gun_jobs=sum(j['kind']=='gun' for j in jobs.values()),
                         armor_jobs=sum(j['kind']=='armor' for j in jobs.values()),
                         resolved_sources=len(jobs),legendary_sources=sum(j['legendary'] for j in jobs.values()),
                         source_sheets=len(inventory),output=str(OUT/'queue.json'))))

if __name__=='__main__':main()
