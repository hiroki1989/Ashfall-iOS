# Warden hidden surface projection sources

Dedicated imagegen paint sources for currently occluded cloth/inner boot surfaces. Existing atlas, FBX and runtime were not changed.

Geometry/template: armor-rebuilt.fbx, imported in Blender 5.2.2, rotation/scale applied. Coordinate system is Blender Z-up. Cameras and extracted geometry bounds are in camera-bounds.json. Orthographic projection is exact for the clay templates; generated paint approximates that registration. All images retain native alpha.

Projection convention: take camera right/up axes from camera_rotation_euler, subtract center, divide by orthographic_scale horizontally adjusted by aspect=0.5. Image coordinates use top-left origin. Normalized frame must be preserved; do not fit alpha bounds. Clay templates were 512x1024; generated images are 887x1774, same 1:2 ratio.

- torso-positive-x-painted-v1 / torso-negative-x-painted-v1: arm-free torso views. Sample hidden torso/jacket surfaces only. Lower cropped thigh/pouches are context and not authoritative leg art. Arm socket remains open; do not project transparent socket onto real cloth.
- leg-*-painted-v1: REJECTED. Camouflage extends too far down into boot shaft. Retained only for traceability; use v2 after inspection.

Templates/render script/logs: D:/Unity/AshfallArtValidation/hidden-surface-art. Existing four kit images remain design authority. Fine folds/seams are inferred in newly visible surfaces; these are projection candidates, not proof of final Unity appearance. Review seam continuity after projection before runtime adoption.

2026-10-21 continuation: leg-negative-x-inner-painted-v2.png corrects tall leather boot boundary at ~57% canvas height using built-in imagegen; toe left, inner surface has no duplicated laces, armour or pouch. Positive inner side reuses mirrored v2 inside projection preprocessing, registered independently to its own clay row silhouette. Original v1 remains rejected.
Final accepted runtime atlas: ../../GeneratedWarden/armor-rebuilt-atlas.png (same folder parent). Dedicated images are source only; Blender bake selects occluded torso/leg side surfaces using BVH. UV, mesh, rig unchanged. Source alpha/silhouette is registered to the exact clay camera before projection. Boundary feather is 4 atlas pixels. Processing scripts/logs: D:/Unity/AshfallArtValidation/hidden-surface-art. No Hunyuan texture inference.
Prompt v2: Edit clay template preserving exact orthographic framing/silhouette, trousers upper57%, tall scuffed dark brown leather combat boot lower43%; same worn tan tactical clothing and inked painted-realistic style; no inner knee armour/pouches/duplicate laces; transparent background. Built-in image_gen used, no CLI/API fallback.
