# Ironwake chest design / orthographic UV trial

Target: `distinct.armor.006`, legendary Ironwake chest. The source card is
`Assets/Resources/Art/BespokeLegendaries/ironwake-legendaries-v1.png`, cell 6.
`reference-original.png` is a lossless crop of that card and is **reference
only**; it is never projected onto the 3D model.

`ironwake06-orthographic-4view-v1.png` is an imagegen edit of the reference.
Views are front, back, wearer's left side, wearer's right side from left to
right. All have a matched collar, shoulder, cuff and waist line, transparent
background, and flat lighting. The art preserves the reference's rusted
steel-blue cuirass, brown harness/belt, olive work jacket, copper canisters,
gauge, and pouch. The back has its own panel and harness rather than a mirrored
gauge. No text or weapon is baked into the art.

`bake_orthographic_atlas.py` uses the approved Warden UV mesh map in
`D:/Unity/AshfallArtValidation/shared-armor/uv-position.npz`. It writes
`ironwake06-chest-uv-candidate-v1.png` (2048 square, RGB, same UV layout as
`armor-rebuilt-atlas.png`) and a white-pixel coverage mask. The original atlas
is preserved outside the chest slot. This is **a candidate**, not yet wired
into `SharedArmorTextures`; PMO owns the Unity connection and in-game QA.

Projection contract: Blender mesh X is left/right, Y depth (front = -Y), Z up
(-1..1); Unity chest garment is z+1 = 1.115..1.855 m, centered at X=0 and
spanning about -0.52..+0.52 m including sleeves. Front and back use X and
height. Side views use Y depth and height. Mesh normals select and blend the
four orthographic views with a cubed directional weight at diagonal seams.
The color transfer is 91% blueprint and 9% approved atlas fine grain. The
wearer's left canisters stay on the corresponding side when projected.

Generation prompt, built-in imagegen edit mode:

> Reconstruct the provided Ironwake scrap-metal chest armor as four matched
> straight orthographic views (front/back/left/right) of the garment only,
> collar to hem and sleeves to wrist, on genuine transparent background.
> Preserve dark olive cloth, worn steel-blue rusted chest/shoulder armor,
> crossed leather straps and buckles, gauge and paired copper canisters, and
> waist pouch. Design the rear separately; never mirror the front gauge.
> Believable folds and seams, flat neutral texture lighting, no person,
> weapon, scenery, labels or UI.

QA before adoption: render the chest alone at front, 45°, side, back and during
running/aiming. Check exact gauge/canister side, collar/waist alignment,
sleeve seam, UV holes and side-view handoff. Reject this candidate if the
design swims, repeats, or crosses a silhouette boundary; correct source
placement or the projection contract before bulk processing.
