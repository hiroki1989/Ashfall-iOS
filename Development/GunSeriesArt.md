# Gun series illustrations / 銃シリーズ原画 — 2026-10-19

## Deliverables / 成果
Built-in image_gen was used; no CLI/API fallback. Six maker sheets, 24 complete guns each, 144 complete illustrations. Assets: `Assets/Resources/Art/GunSeries/{maker}-series-v1.png`, corresponding `-bounds.json`, and Unity metas. Maker IDs: kestrel, ironwake, voltarc, mireline, cinder, aegis.
各シート4列×6行、横向き・銃口右。行は assault / handgun / shotgun / LMG / SMG / sniper。列は各クラス内の4シリーズ。部品合成用ではなく完成銃1挺の絵。

Kestrel native 1774×887; other sheets native1536×1024. Native PNGs are copied without pixel modification; originals preserved in the built-in generated_images directory below. Ironwake background extraction was a built-in edit; both initial and edited originals remain.

## Bounds and validation / 切出しと確認
24 cells in row-major order, top row first. JSON cells contain normalized bottom-left Unity UV `x,y,width,height`. Tight bounding boxes use alpha>24 inside fixed 4×6 cells; no cross-cell union. Additional pixelBoundsTopLeft is diagnostic. Each cell has a nonempty alpha shape. Mipmaps disabled, clamp, bilinear, nPOTScale0; 2048 limit retains native resolution.
全6枚を目視確認し、24挺・クラス行・右向き・文字なし・全体のメーカー差を確認。alpha=0画素割合: Kestrel60.9%, Ironwake61.0%, Voltarc59.9%, Mireline59.2%, Cinder65.2%, Aegis60.4%。透明部のRGBに茶灰色が残るシートがあるがalphaは0であり画像処理による背景削除は行っていない。
Some silhouettes approach fixed cell boundaries; bounds intentionally clip to their cell. Heavy hand cannons use large revolver architecture in several makers. This is inventory side-profile illustration, not a new directional combat sprite set or proof of exact equipped-model geometry.

## Source originals / 原本
- kestrel: `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-1ac3ed7f-d645-410a-a37c-84b9a9d793e1.png`
- ironwake: `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-71fceb4f-38d7-4096-a042-17ef71f8b421.png`; pre-extraction original `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-21ebf181-50b1-4acf-8211-9aa59bffd33f.png`
- voltarc: `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-4d952b6f-ec75-4184-a126-7c2405617626.png`
- mireline: `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-73076007-7233-498d-8fda-1440c9186974.png`
- cinder: `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-b3b95d9e-ffd6-41d9-bafe-3c1863da9dc7.png`
- aegis: `C:/Users/hiroki/.codex/generated_images/01a1520e-6598-7c12-98a3-f4120a81c593/exec-18538ba7-bced-46af-b8d9-e9f55ec053e7.png`

## Final prompt set / 最終プロンプト
Kestrel uses the same class/series layout below, with precise walnut, blued dark steel, measuring dials, detailed painted color and thin ink, worn repaired survivor gear, fully visible horizontal guns pointing right and generous isolated cell gutters; no text/UI/people/background.
Shared prompt for Ironwake, Voltarc, Mireline, Cinder, Aegis, with maker suffix:
```text
Use case: stylized-concept. Production transparent weapon inventory atlas. EXACTLY 24 COMPLETE DIFFERENT firearms in fixed FOUR columns SIX rows, all strictly horizontal side profile stock LEFT muzzle RIGHT. Landscape canvas. Each gun centered in its own equal rectangular cell, generous transparent gutters, entire weapon fully visible. Detailed painted realistic proportions, thin ink outlines, worn survivor equipment, modern firearms with serviceable retrofuture additions. Rows top-bottom, columns left-right: R1 assault: conventional assault rifle, bullpup assault rifle, battle rifle, marksman rifle. R2 handgun: compact pistol, service pistol, revolver, hand cannon. R3 shotgun: pump, break action side-by-side, short drum-mag shotgun, semiauto shotgun. R4 LMG: belt box, top disc, drum, heavy support. R5 SMG: compact folding stock, top magazine, helical magazine, drum. R6 sniper: wood-stock bolt action, precision chassis, long bullpup, heavy anti-materiel. Each is a whole functioning gun, distinct architecture, not component parts. No text/labels/logos/UI/grid lines/people/hands/background/floor/shadows/neon. Real transparent alpha. Maker identity: 
```
Maker suffixes:
- IRONWAKE: old military forged industrial construction, heavy dark forged steel, rivets, olive canvas wraps, hydraulic recoil dampers, visibly robust army weapon mechanisms, dull brass small fittings, worn field repairs. Each model has distinct military receiver and barrel structure.
- VOLTARC: modern firearms integrated with small valve-era electric battery packs, protected amber vacuum tubes, ceramic insulators, copper conductors and service panels; dark steel, bakelite brown grips, muted olive packs. Electric additions functional and restrained, not neon or sci-fi rayguns. Every weapon independently designed.
- MIRELINE: medical/civil-defence firearms, faded pale sage and ivory service panels, dark steel, olive canvas wrappings, small protected pale green medicine glass reservoirs and short rubber siphon tubes, brass fittings. Worn repaired functional equipment, realistic conventional firearm bodies. No medical text or crosses. Each gun distinct.
- CINDER: reclaimed timber and patched black steel, rust-worn brass furnace pressure plumbing, protected amber glass pressure chambers, mechanical gauges, leather bindings and repaired survivor constructions. Wood skeletal stocks and long steel actions. Grounded rugged functional guns rather than fantasy guns; each architecture distinct.
- AEGIS: police and convoy security service firearms, dark navy steel panels, dark navy canvas grip wraps, small off-white ceramic protective plates integrated with receivers, restrained orange safety tabs, heavy protective shrouds but recognizable modern mechanisms, worn survivor scuffs and field repairs. No lettering or emblems. Each model distinct.

Ironwake edit prompt:
```text
Use case: background-extraction. Remove ALL of the grey brown background to real transparent alpha. Keep exactly all 24 complete guns unchanged in same fixed 4 columns x6 rows, same design colors proportions positions and sizes. Only remove backdrop. No background pixels between guns, no shadow, no added content, no text.
```


2026-10-19 runtime correction: fixed grid bounds admitted neighbouring stocks/magazines and cut guns crossing cell boundaries. Main analysed connected alpha silhouettes (metadata only), records each whole main gun's bounding rectangle and convex hull; runtime GunSeriesPortraits applies this hull through PopItemPreview before caching the painted preview. Original PNG pixels are unchanged. Each series remains a whole painted gun.
