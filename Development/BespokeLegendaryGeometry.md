# Bespoke legendary geometry

New factory: BespokeLegendaryModels3D. Index = manufacturer*5 + category/slot.
Weapons: precision rifle, handgun, shotgun, LMG, SMG. Armor: head, chest, arms, legs, feet.
All sixty switch cases author complete constructions without calling ordinary recipes. Fabrication primitives, anatomical hand grips, and fitted cloth/armor surfaces are shared. This is individually specified procedural geometry, not sixty separately sculpted art assets. WastelandKeyArt.png inspected before construction; exact visual fidelity requires runtime image comparison.

Weapons 00..04 Kestrel: monolithic scoped bolt rifle with third-shot escapement; long match pistol with impact recorder; twin sporting barrels with kill tally; balanced precision belt receiver with reload lever; compact straight action with dash inertia slide.
Weapons 05..09 Ironwake: hydraulic bolt rifle with side ram; transverse-drum hand cannon; stacked siege shotgun with long piston; riveted fortress LMG with twin hydraulic reservoirs; stamped SMG with copper cylinder.
Weapons 10..14 Voltarc: long ceramic rail rifle with seventeen windings; arc pistol with vacuum tube; fork-barrel lightning shotgun; generator LMG with twenty-two windings and transverse feed; bullpup pulse SMG with rear magazine.
Weapons 15..19 Mireline: surgical needle rifle with return hose; rescue injector pistol; twin-fluid dispersal shotgun; perfusion machine gun with large recovery vessel; sealed quarantine SMG.
Weapons 20..24 Cinder: menu-derived wood skeletal stock, long steel tube, two upright amber vessels, brass return pipe and vented brake; saw-cut hand cannon; welded stacked furnace shotgun; reclaimed finned machine gun; scrapyard open-upper SMG with angled feed.
Weapons 25..29 Aegis: security long rifle with cheek shield; buckler pistol; door-breach blast-deflector shotgun; roof-shrouded riot LMG; telescopic close-protection SMG with side shield.

Armor 00..04 Kestrel: open-crown impact-recording optics; tailored kill-calibrated chest unit; reload exchange-slide cuff; dash stabilizing shin; periodic clockwork heel.
Armor 05..09 Ironwake: hydraulic brow helmet; twin-accumulator siege chest; ram-locking reload forearm; hydraulic dash knee; pressure sole foundry boot.
Armor 10..14 Voltarc: twin-tube damage arrestor helmet; kill-charge ceramic distribution vest; reload induction cuff; dash dielectric greave; periodically grounded boot.
Armor 15..19 Mireline: sealed hood with damage respirator; sterile kill-recovery vest with fluid vessel/hose; reload syringe bracer; compression shin with dash fluid vessel; periodic pump overshoe.
Armor 20..24 Cinder: round goggles and dual-filter respirator, existing hair remains visible (no helmet); torn olive field jacket with leather pouches and asymmetric rust shoulder; leather/steel pressure-dial bracers; olive trousers with faceted rust knees; crossed-lace worn boots with buckle.
Armor 25..29 Aegis: reaction-visor security helmet; kill-reaction battery cuirass; reload forearm shield; overlapping dash knee/shin laminate; periodic barrier toe/heel battery.

Armor trigger order damage/kill/reload/dash/periodic. Gun trigger order third-shot/hit/kill/reload/dash. Geometry represents the corresponding mechanism; actual effect behavior remains LegendaryCatalog's responsibility.

Mesh lifetime uses Arsenal3D primitive owners and ManufacturerEquipment3D.MeshLifetime for generated continuous surfaces. Compact batches by material. Supplements attach separate shoulder/thigh pieces to their own bones, with no rigid bridge across elbow/knee. Continuous shell bases extend outside existing garment dimensions. Cloth fidelity, clipping across every animation, all-direction aesthetics and illustration matching remain pending actual integrated screenshots.
