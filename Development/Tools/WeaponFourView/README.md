# Weapon four-view projection candidate

This tool reads an existing three-dimensional firearm and approved side paintings. `prepare.py` packs left, right, muzzle/front and stock/back into four **disjoint** 512 px tiles of one 1024×1024 opaque atlas. When end paintings are unavailable, those tiles receive a neutral median color sampled from the side painting. They are deliberately marked as missing art, not described as painted end views. Source FBX and PNG files are never modified.

`project.py` imports the FBX in Blender, determines bore/side/vertical axes from its bounds, gives each polygon exactly one atlas island, exports candidate FBX/GLB and renders left/right/front/back QA images. End caps use separate end tiles, avoiding the old two-view projection's repeated side silhouette on the muzzle or stock. Top and bottom polygons choose the nearer side by normal or location. This is an automated starting point; weapon-specific geometry and painted features still require visual review.

Representative command (Blender 5.4; ordinary Python needs Pillow, NumPy, SciPy):

```
python Development/Tools/WeaponFourView/prepare.py --left Assets/ArtSource/LowPolyProof/TwoViewComponent/warden-rifle-source-0.png --right Assets/ArtSource/LowPolyProof/TwoViewComponent/warden-rifle-source-1.png --out Assets/StreamingAssets/WeaponFourView/warden-rifle-atlas.png
blender -b --python Development/Tools/WeaponFourView/project.py -- --mesh Assets/Resources/Art/GeneratedWarden/rifle-mobile.fbx --atlas Assets/StreamingAssets/WeaponFourView/warden-rifle-atlas.png --out Assets/StreamingAssets/WeaponFourView/warden-rifle-four-view.glb --fbx Assets/StreamingAssets/WeaponFourView/warden-rifle-four-view.fbx --qa Development/Tools/WeaponFourView/qa/warden-rifle
```

For a new product, provide its own FBX and its own four paintings (`--left --right --front --back`). Do not reuse another product's illustration or claim recolors as unique products. The generated candidate remains in `StreamingAssets`; Unity product registration requires Coding/PMO to place a validated mesh with `trigger`, `support`, `muzzle` landmarks and matching atlas in `Resources/Art/GeneratedEquipment/gun/<catalogId>/mesh` and `atlas`. Unity does not natively load this GLB from StreamingAssets. The existing Warden FBX has no product-specific markers; this prototype is not a drop-in exact-product asset.

Visual QA: `qa/warden-rifle/{left,right,front,back}.png`. Both sides show the approved source motif without a doubled whole-gun painting on the muzzle or stock. The source side paintings have fine detail crowding at the foregrip, and the generated end tiles are intentionally flat. This candidate is **not approved for production** until dedicated end paintings, source alignment, gun grip/socket checks and in-game four-angle review pass.
