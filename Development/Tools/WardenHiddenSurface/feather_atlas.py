from PIL import Image
from scipy.ndimage import distance_transform_edt
import numpy as np
from pathlib import Path
O=Path('D:/Unity/AshfallArtValidation/hidden-surface-art');A=Path('D:/Unity/Ashfall-iOS/Assets/Resources/Art/GeneratedWarden')
a=np.array(Image.open(O/'armor-hidden-patched-atlas.png').convert('RGB')).astype(float);b=np.array(Image.open(A/'armor-rebuilt-atlas.png').convert('RGB')).astype(float);m=np.array(Image.open(O/'hidden-mask.png').convert('L'))>128;d=distance_transform_edt(m);w=np.minimum(d/4,1);w=w*w*(3-2*w);out=a*w[:,:,None]+b*(1-w[:,:,None]);Image.fromarray(np.uint8(np.clip(out,0,255))).save(O/'armor-hidden-feathered-atlas.png')