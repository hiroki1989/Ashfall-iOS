"""Copy approved generated gun sheets into Resources with deterministic cell bounds.

The generated images are two columns by four rows.  Alpha is retained; every
series uses its own painted cell and the four balance variants share that art.
"""
import json
from pathlib import Path
from shutil import copy2
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SRC = Path.home() / '.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30'
DEST = ROOT / 'Assets/Resources/Art/GunSeriesExpanded'
FILES = {
    'kestrel': ['4ce9073e-4066-4d39-96ed-644d36abee4e','fa6fe961-cceb-480c-9e4d-2c3e2131d08c','8ef63952-f7c7-4fc9-8ae1-fe1475cffb34','960abee0-14a2-416d-847a-c27db5b471a9','f517d559-536f-478b-a65f-3ba67ee38087','9e898e2b-b0c3-4469-bfdd-4b7ee3a9838d'],
    'ironwake': ['6ff3983c-851b-4c6b-8513-8dfe6a0c5dc7','5eb295d4-078b-4186-8dba-e4f095587a2e','82ba06ad-e170-484a-899d-5584cf4b3d27','7bd22aed-1b97-45e7-ac82-4dfcbef0bf1a','de987120-1db7-4a14-9d5f-a7871a47dea5','081c92a6-5087-44ee-9221-690e307d4da4'],
    'voltarc': ['2c3f979a-8fc1-4652-8540-609bb07ebe6b','53a31292-c2b3-486e-a170-440e680d2c08','960577ad-18fb-4cb8-b75f-c5e021e1a1c1','bb54d049-b51e-4c9c-8b60-0278c1ee5f16','d7855e05-2a74-41f1-9d27-5e8f0e1548f8','6103874c-3f57-45de-9c63-9337b6d2654b'],
    'mireline': ['a1e67d89-9dd0-4389-a026-75b2d4345c2a','cf73a878-143b-4d3c-a120-809932b11054','94054d5a-41e5-42b2-ada1-ee99bbeb11f5','c8e4b8f0-ee5b-4f7b-9b77-2a7045e4afb2','ed000317-0315-4ead-a878-e7cd0bd8c52c','5640aefc-0e2d-4772-bd80-9c580c5df319'],
    'cinder': ['860daf06-0eae-43a7-823c-cdc3067de4e3','44c83578-517b-4a2b-87be-3f5c38d781da','557a3a89-a214-4dd0-9982-26833cfe2272','27105e12-657c-49ad-9d2b-e052a6e07bd7','870214ca-6d00-4561-afb3-eac7181268eb','3883b29b-20d7-42f9-97bf-595d97a89adf'],
    'aegis': ['971a344f-ff84-44e2-a890-c84b29617179','8a3785c1-29b8-4645-b7a1-b53496f8086f','cfbf478c-cc4f-4528-9329-a45301095719','ed5fe3b5-b7d2-41b4-89f1-540a48cb91f3','62d93784-f1d1-440c-957f-f8be1b460029','428937cf-c6b9-4654-b271-5efe99c3065d'],
}
CLASSES = ('assault','handgun','shotgun','lmg','smg','sniper')


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    count = 0
    for maker, names in FILES.items():
        for cls, token in zip(CLASSES, names):
            original = SRC / f'exec-{token}.png'
            target = DEST / f'{maker}-{cls}-series-v2.png'
            if not original.exists():
                raise FileNotFoundError(original)
            copy2(original, target)
            with Image.open(target) as image:
                w, h = image.size
                if w < 500 or h < 500:
                    raise ValueError((target, image.size))
                cells = []
                for row in range(4):
                    for col in range(2):
                        x0, x1 = col*w//2, (col+1)*w//2
                        y0, y1 = row*h//4, (row+1)*h//4
                        cell = image.crop((x0,y0,x1,y1)).getchannel('A')
                        box = cell.point(lambda v: 255 if v > 24 else 0).getbbox()
                        if box is None:
                            box = (0,0,x1-x0,y1-y0)
                        l,t,r,b = box
                        pad = 4
                        l=max(0,l-pad); t=max(0,t-pad); r=min(x1-x0,r+pad); b=min(y1-y0,b+pad)
                        left,bottom,width,height=(x0+l)/w,1-(y0+b)/h,(r-l)/w,(b-t)/h
                        cells.append({'x':left,'y':bottom,'width':width,'height':height})
                (DEST / f'{maker}-{cls}-series-v2-bounds.json').write_text(json.dumps({'cells':cells},indent=2),encoding='utf-8')
                count += 1
    print(f'Imported {count} sheets, {count*8} distinct series paintings')


if __name__ == '__main__':
    main()
