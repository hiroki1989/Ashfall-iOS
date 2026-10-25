"""Import the six separately painted, ten-product legendary sheets."""
import json
from pathlib import Path
from shutil import copy2
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path.home() / '.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30'
DEST = ROOT / 'Assets/Resources/Art/BespokeLegendariesExpanded'
FILES = {
    'kestrel':'20dab41e-63cc-4433-9b62-4561b28a2e3d',
    'ironwake':'62abb4b5-a9b4-4fec-82dd-6ad98b811429',
    'voltarc':'6f2e6402-2a45-4b87-b8b8-97334711c7a8',
    'mireline':'bb31c5d4-f114-4685-8ef3-7569064b5286',
    'cinder':'273325bb-22b6-4bc1-8485-7c9407341543',
    'aegis':'d4b6f9dc-294e-429a-b7e9-9d59f609eb0a',
}

def main():
    DEST.mkdir(parents=True,exist_ok=True)
    for maker, token in FILES.items():
        target=DEST / f'{maker}-legendaries-v2.png'
        copy2(SOURCE / f'exec-{token}.png', target)
        with Image.open(target) as image:
            w,h=image.size
            cells=[]
            for row in range(2):
                for col in range(5):
                    x0,x1=col*w//5,(col+1)*w//5
                    y0,y1=row*h//2,(row+1)*h//2
                    alpha=image.crop((x0,y0,x1,y1)).getchannel('A')
                    box=alpha.point(lambda v:255 if v>24 else 0).getbbox() or (0,0,x1-x0,y1-y0)
                    l,t,r,b=box; pad=3
                    l=max(0,l-pad);t=max(0,t-pad);r=min(x1-x0,r+pad);b=min(y1-y0,b+pad)
                    cells.append({'x':(x0+l)/w,'y':1-(y0+b)/h,'width':(r-l)/w,'height':(b-t)/h})
            (DEST/f'{maker}-legendaries-v2-bounds.json').write_text(json.dumps({'cells':cells},indent=2),encoding='utf-8')
    print('Imported 60 bespoke legendary paintings')

if __name__=='__main__':main()
