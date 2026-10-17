# Inked runtime assets / 2026-10-14

Generation originals: C:/Users/hiroki/.codex/generated_images/01a12e24-2a7c-7762-9cd9-15ed31ba4c30/. Saved runtime targets all under Assets/Resources/Art/LayeredPlayer/InkedArt/.

| Target | Original |
|---|---|
| Body_Front.png | exec-803031b0-8d7d-4cb1-a443-715e45508490.png |
| Body_FrontRight.png | exec-d010c3d6-32b6-442e-97e0-263dcc2c130a.png |
| Body_Right.png | exec-e6921602-a334-45b2-8a68-73bae8aac8bb.png |
| Body_BackRight.png | exec-49c94f3b-4b75-4693-880f-d4da49c19bf4.png |
| Body_Back.png | exec-32b5d6b8-c7ad-4570-b790-7d0df3fa1e83.png |
| Armor_Front.png | exec-30091c35-bb62-4e03-97fa-41808c5c7d07.png |
| Armor_FrontRight.png | exec-005d8904-4ad9-48e2-ad6b-e73dff09cad0.png |
| Armor_Right.png | exec-1787d450-2a2d-4bf1-a7f1-8befc50c8253.png |
| Armor_BackRight.png | exec-15465377-3287-48e6-b213-c317f5be0f49.png |
| Armor_Back.png | exec-78da9f23-0e78-44bd-93b8-d72f7a2ddf5f.png |
| Weapons_6.png | exec-7327d895-162f-47bf-86fe-d34a17320514.png |

Built-in image_gen edits; originals preserved. Body five directions, armor five directions/two field kits, modern weapon models12/13. Native PNGs are unmodified. Metadata scripts read alpha only; measured splits and excluded neighbor pixels are recorded in JSON. Existing armor V5 and other weapon atlases remain.

## Body prompt
undefined

For each direction append: Target facing is {Right|BackRight|Back}. Preserve this EXACT viewing direction, do not substitute front view. Style reference only. Front append: This input target is the FRONT facing body atlas; preserve FRONT facing, do not change to three-quarter. Reference2 is style only.

## Armor prompt
undefined

Append Target view: {FrontRight|Front|Right|BackRight|Back}; keep this exact view.

## Weapon prompt
EDIT image1 modern weapon 4x4 atlas. Image2 is approved ink STYLE only. Preserve EXACT all16 placements, silhouettes, viewpoints, grip/muzzle/stock locations and geometry. Top8 images same olive modern carbine, bottom8 same wood pump shotgun. Restyle to crisp detailed graphic novel ink rendering like reference2: strong clean dark outer contours, precise internal fine black lines on receiver seams, screws, rail slots, scratches, magazine ribs, stock/optic. Three-tone cel shadow shapes, metallic cool highlights, olive polymer and brown wood remain separate colors. Same realistic scale and detailed real-gun construction, no chibi/oversize optic/no changed components, no new models, no angle inversions. Genuine transparent alpha, no backdrop or captions. EXACT source 4x4 cell positions and empty margins, no touching neighboring guns.

All calls use the corresponding original Body_*, TacticalArt/ArmorV4_* or WeaponsModern_6.png as edit target, and Assets/ArtSamples/FieldKit-Inked-Sample-v2.png as style reference, transparent_background=true.

