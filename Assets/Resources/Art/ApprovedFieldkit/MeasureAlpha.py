from PIL import Image
import numpy as np
from scipy.ndimage import label, find_objects
from pathlib import Path
root=Path(__file__).parent
for f in [p.name for p in root.glob('Run*8-v1.png')]:
    if not (root/f).exists(): continue
    im=Image.open(root/f)
    a=np.array(im)[:,:,3]>128
    lab,n=label(a)
    regs=[]
    for i,s in enumerate(find_objects(lab)):
        if s and np.sum(lab[s]==i+1)>2000:
            regs.append((s[1].start,s[0].start,s[1].stop,s[0].stop,int(np.sum(lab[s]==i+1))))
    print(f,im.size,sorted(regs,key=lambda r:(r[1]//500,r[0])))
