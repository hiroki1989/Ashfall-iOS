# Cinder Scrap Helmet candidate

- Authoritative icon: `Assets/Resources/Art/DistinctEquipment/cinder-armor-parts-v1.png`, first helmet cell. `front-existing.png` is an exact crop of that cell, not a replacement portrait.
- `blueprint-front-back-v2.png` was created with the built-in imagegen edit path from the cropped icon: orthographic front and back, original outline language, chipped asymmetric orange paint, grey steel, deep eye aperture, jaw guard, side pivots, and rivets. No UI or text. V1 kept as a rejected symmetric-paint draft.
- `front-blueprint-v2.png` and `back-blueprint-v2.png` are source crops for Hunyuan3D-2mini shape-only and Blender front/back two-direction projection. The generated rear is an inference because the icon has no true rear view.
- `Candidates/cinder-scrap-helmet-two-view-atlas-v2.png` has opaque edge extension (`alpha_min=255`) and 94% image occupancy to prevent transparent seams. It is a production candidate, not a game asset.
- Built-in imagegen final edit prompt: “Keep the orthographic two-view helmet geometry, scale, front/back layout and transparent background; change only the paint to one asymmetrical diagonal orange stripe crossing the left upper shell and a grey right crown as in the existing icon, extending the same painted motif plausibly across the back; retain black aperture, jaw guard, side pivots, scratches and rivets; no text, UI, ground or shadow.”
- Acceptance requires independent curved helmet geometry matching the aperture, jaw and nape in front/side/back renders. A valid atlas alone does not qualify.
