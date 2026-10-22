# Continuous arm reproducibility inputs

Runtime assets (QA only):
- Assets/Resources/Art/GeneratedBase/continuous-arm.fbx (+ .meta, readable + vertex colors)
- Assets/Resources/Art/GeneratedBase/continuous-arm-atlas.png (+ .meta, max 4096)
- Assets/Scripts/GeneratedCommonBodyRig.cs
- Existing CharacterArmFit.cs and CharacterLowerBodyFit.cs remain required.

Re-export tool: D:/Unity/AshfallArtValidation/body-surface-fix/continuous_arm_build.py
Validation/render: D:/Unity/AshfallArtValidation/body-surface-fix/continuous_arm_validate.py
Run in Blender CPU background with -t 4. Never launch Unity from these scripts.

Authoring source inputs:
- D:/Unity/Ashfall-iOS/Assets/ArtSource/LowPolyProof/ArmRigCandidate/plain-body-arm-rig-candidate.blend
  (contains original full authored source, original split body/arms, source_vertex attributes).
- D:/Unity/AshfallArtValidation/body-surface-fix/surface-fixed-candidate.blend
  (body UV/cap source; cap is removed by this tool).
- D:/Unity/AshfallArtValidation/body-surface-fix/continuous-arm/continuous-arm-atlas.png
  (left repaired armless body surface, right original arm front/back/profile with RGB dilation).
- D:/Unity/AshfallArtValidation/body-surface-fix/continuous-arm/profile-bounds.json
- D:/Unity/AshfallArtValidation/body-surface-fix/continuous-arm/profile-skin-bounds.json

The atlas itself and bounds JSON are exact prepared inputs for reproducible re-export.
No new image generation is needed. Original atlas provenance:
QA Assets/Resources/Art/GeneratedBase/surface-fixed-candidate-atlas.png (left half),
QA Assets/Resources/Art/GeneratedBase/arm-rig-candidate-atlas.png (right half, RGB dilated per tile).

Output continuous-arm.blend preserves the original source IDs, restored 524 bridge faces,
and point-domain ArmAttachment. Runtime does not depend on the .blend or JSON files.
