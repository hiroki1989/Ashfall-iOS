# Gun classes and manufacturer series — 2026-10-19

Inventory illustrations remain paintings. Normal gun catalog: 6 manufacturers × 6 visible gun classes × 4 structural series × 4 editions = 576; plus 30 existing dedicated named legendaries = 606. Existing catalog ID strings are retained, and loading does not reroll saved statistics. Exact distinct catalog items now use their recipe's class for held pose/ammunition classification.

| Gun class | Series 1 | Series 2 | Series 3 | Series 4 |
|---|---|---|---|---|
| Assault rifle | Conventional patrol | Short bullpup | Heavy battle rifle | Scoped marksman |
| Handgun | Compact | Service automatic | Six-shot revolver | Hand cannon |
| Shotgun | Tubular pump | Side-by-side break action | Short drum | Box-fed semiautomatic |
| LMG | Belt box | Top pan | Drum | Heavy support |
| SMG | Folding compact | Top magazine | Helical | Drum |
| Sniper | Wood-stock bolt | Precision chassis | Bullpup | Heavy anti-materiel |

Every line inherits the manufacturer's construction and mechanical tendencies from GroundedDesignDirection.md. Kestrel adds walnut/precision adjustment; Ironwake forged straps and recoil dampers; Voltarc serviceable valve-era electrical hardware; Mireline sealed medical reservoirs and hoses; Cinder repaired timber/steel and pressure plumbing; Aegis security reinforcement and canvas retention. No clean space-gun palette.

Series have tradeoffs in damage, shot interval, accuracy, capacity and reload. Each has Field/Short/Reinforced/Range editions. The latter three alter 3D length to 85%/104%/117%, with stronger locking bands or sights as appropriate. Portraits represent the whole structural series; four editions share its illustration. These are 144 painted series designs, not 576 individually painted guns. Names/descriptions are Japanese and English, with no text embedded in images. Each sheet is 4 columns (series) × 6 rows (class), side view, muzzle right, genuine alpha, 24 measured bounds.

Implementation: GunSeriesDesigns drives recipe names/models/stats; GunSeriesModels3D drives equipped/drop geometry; GunSeriesPortraits drives painted inventory/catalog cards. Bespoke named legendary routes remain separate.

Visual follow-up: handgun revolver frames expose the cylinder through separate top/lower straps; Kestrel/Ironwake/Cinder hand cannons use a heavy revolver construction. Break-action shotguns use a curved wooden wrist with an open trigger bow. Mireline top-feed series uses its illustrated lower box feed, and Kestrel/Mireline spiral-feed casing is vertical; series labels describe the shared action lineage rather than claiming a specific receiver mount on every manufacturer.
