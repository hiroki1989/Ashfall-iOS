using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public enum ExclusiveLegendaryCategory { Gun, Armor, Module, Melee }

    public struct ExclusiveLegendaryDrop
    {
        public ExclusiveLegendaryCategory category;
        public int index;
        public string catalogId;
        public int stageId;
    }

    // Curates the top ~1/3 strongest non-faction-boss legendaries across firearms,
    // armor, modules, and melee weapons, and binds each one exclusively to a specific
    // regular dungeon boss (stages 0..199, displayed in UI as 01..200).
    // Exclusive legendaries never roll from non-boss or non-matching sources; when the
    // assigned dungeon boss drops a legendary, it has a 50% chance to be its exclusive.
    public static class DungeonBossExclusive
    {
        // Top 30 strongest legendary firearms (out of 90 = 33.3%):
        // 5 per manufacturer: the 4 strongest bespoke triggers (variants 0..3) + the strongest expanded mechanism (variant 14: Final Bearing / heavy blast + outer shockwave).
        static readonly int[] ExclusiveGunVariants = { 0, 1, 2, 3, 14 };

        // Top 8 strongest legendary armor pieces (out of 24 active slots = 33.3%):
        // armor-0-1 (Ghost Ledger), armor-1-1 (Iron Epitaph), armor-2-1 (Blackout Jacket),
        // armor-3-1 (Grave Bloom), armor-3-4 (Undying Root), armor-4-2 (Fuse Match),
        // armor-5-1 (Widow's Wall), armor-5-3 (Safe Passage).
        static readonly int[] ExclusiveArmorIndices = { 1, 6, 11, 16, 19, 22, 26, 28 };

        static readonly HashSet<string> exclusiveGunKeys = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> exclusiveArmorKeys = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> exclusiveModuleIds = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<int> exclusiveMeleeIndices = new HashSet<int>();

        static readonly Dictionary<string, int> itemKeyToStage = new Dictionary<string, int>(StringComparer.Ordinal);
        static readonly Dictionary<int, ExclusiveLegendaryDrop> stageToExclusive = new Dictionary<int, ExclusiveLegendaryDrop>();

        static readonly int[] nonExclusiveGunCatalogIndices;
        static readonly int[] nonExclusiveArmorIndices;
        static readonly ModuleProduct[][] nonExclusiveModulesByFamily;
        static readonly int[] nonExclusiveMeleeIndices;

        static DungeonBossExclusive()
        {
            var drops = new List<ExclusiveLegendaryDrop>(84);

            // 1. 30 Legendary Firearms (5 per manufacturer, 30 / 90 = 1/3)
            for (int maker = 0; maker < 6; maker++)
            {
                for (int v = 0; v < ExclusiveGunVariants.Length; v++)
                {
                    int variant = ExclusiveGunVariants[v];
                    int catIndex = LegendaryCatalog.CatalogIndex(maker, variant);
                    string key = LegendaryCatalog.Key(false, catIndex);
                    exclusiveGunKeys.Add(key);
                    drops.Add(new ExclusiveLegendaryDrop
                    {
                        category = ExclusiveLegendaryCategory.Gun,
                        index = catIndex,
                        catalogId = key
                    });
                }
            }

            // 2. 8 Legendary Armor Pieces (8 / 24 = 1/3)
            for (int i = 0; i < ExclusiveArmorIndices.Length; i++)
            {
                int armorIndex = ExclusiveArmorIndices[i];
                string key = LegendaryCatalog.Key(true, armorIndex);
                exclusiveArmorKeys.Add(key);
                drops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Armor,
                    index = armorIndex,
                    catalogId = key
                });
            }

            // 3. 28 Legendary Modules (28 / 84 = 1/3), ranked by combat impact (invulnerability, multi-target AoE/line strike, stagger, barrier, and output scale)
            var legendaryModules = new List<ModuleProduct>();
            foreach (var p in ModuleProductCatalog.All)
                if (p.legendary && p.family >= 4 && p.family <= 6)
                    legendaryModules.Add(p);

            legendaryModules.Sort((a, b) =>
            {
                float scoreA = ScoreLegendaryModule(a);
                float scoreB = ScoreLegendaryModule(b);
                int cmp = scoreB.CompareTo(scoreA);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.id, b.id);
            });

            int moduleTargetCount = 28; // 28 / 84 = 1/3
            for (int i = 0; i < Mathf.Min(moduleTargetCount, legendaryModules.Count); i++)
            {
                var mod = legendaryModules[i];
                exclusiveModuleIds.Add(mod.id);
                drops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Module,
                    index = mod.designVariant,
                    catalogId = mod.id
                });
            }

            // 4. 17 Legendary Melee Weapons (17 / 50 = 34%, closest integer to 1/3), ranked by legendary hit effect & base DPS
            var meleeCandidates = new List<int>(MeleeWeaponCatalog.CatalogCount);
            for (int i = 0; i < MeleeWeaponCatalog.CatalogCount; i++) meleeCandidates.Add(i);
            meleeCandidates.Sort((a, b) =>
            {
                float scoreA = ScoreLegendaryMelee(a);
                float scoreB = ScoreLegendaryMelee(b);
                int cmp = scoreB.CompareTo(scoreA);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            int meleeTargetCount = 17;
            for (int i = 0; i < Mathf.Min(meleeTargetCount, meleeCandidates.Count); i++)
            {
                int meleeIdx = meleeCandidates[i];
                exclusiveMeleeIndices.Add(meleeIdx);
                string key = MeleeKey(meleeIdx);
                drops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Melee,
                    index = meleeIdx,
                    catalogId = key
                });
            }

            // Deterministically assign each of the 83 exclusive legendaries to a distinct HuntTarget boss dungeon
            // across stages 0..199 (excluding stage 0 tutorial so stage 1..199 are used cleanly, or evenly across HuntTarget stages).
            int total = drops.Count; // 83
            for (int i = 0; i < total; i++)
            {
                // Even-numbered stages (2, 4, ..., 198) are dedicated HuntTarget boss dungeons;
                // spread across 83 distinct dungeons in 0..199.
                int stageId = 2 + ((i * 23) % 99) * 2;
                while (stageToExclusive.ContainsKey(stageId))
                    stageId = (stageId + 2) % ExpeditionMap.RegularStageCount;

                var entry = drops[i];
                entry.stageId = stageId;
                drops[i] = entry;
                stageToExclusive[stageId] = entry;
                itemKeyToStage[entry.catalogId] = stageId;
            }

            // Build non-exclusive pools for the remaining 2/3 of legendaries
            var gunPool = new List<int>(60);
            for (int maker = 0; maker < 6; maker++)
            {
                for (int variant = 0; variant < 15; variant++)
                {
                    int catIdx = LegendaryCatalog.CatalogIndex(maker, variant);
                    string key = LegendaryCatalog.Key(false, catIdx);
                    if (!exclusiveGunKeys.Contains(key))
                        gunPool.Add(catIdx);
                }
            }
            nonExclusiveGunCatalogIndices = gunPool.ToArray();

            var armorPool = new List<int>(16);
            for (int maker = 0; maker < 6; maker++)
            {
                for (int variant = 1; variant <= 4; variant++)
                {
                    int idx = maker * 5 + variant;
                    string key = LegendaryCatalog.Key(true, idx);
                    if (!exclusiveArmorKeys.Contains(key))
                        armorPool.Add(idx);
                }
            }
            nonExclusiveArmorIndices = armorPool.ToArray();

            nonExclusiveModulesByFamily = new ModuleProduct[3][];
            for (int f = 4; f <= 6; f++)
            {
                var famList = new List<ModuleProduct>();
                foreach (var p in ModuleProductCatalog.All)
                    if (p.family == f && !exclusiveModuleIds.Contains(p.id))
                        famList.Add(p);
                nonExclusiveModulesByFamily[f - 4] = famList.ToArray();
            }

            var meleePool = new List<int>(33);
            for (int i = 0; i < MeleeWeaponCatalog.CatalogCount; i++)
                if (!exclusiveMeleeIndices.Contains(i))
                    meleePool.Add(i);
            nonExclusiveMeleeIndices = meleePool.ToArray();
        }

        static float ScoreLegendaryModule(ModuleProduct p)
        {
            var r = DistinctEquipmentDesigns.Find(p.id);
            if (r == null) return 0f;
            int mode = (r.attachment + r.body * 2) % 10;
            // Mode weights based on combat power:
            // 4 (invulnerability), 1 (3-target 6x burst), 7 (3-target 5x line pierce), 9 (2-target 4x + 0.7s delay), 0 (+3 barrier), 8 (instant melee reset), 2 (-0.45s dodge cd)
            float modeScore = mode == 4 ? 100f : mode == 1 ? 92f : mode == 7 ? 86f : mode == 9 ? 82f : mode == 0 ? 75f : mode == 8 ? 64f : mode == 2 ? 58f : mode == 3 ? 52f : 40f;
            float makerBonus = r.maker == 5 ? 14f : (r.maker == 1 || r.maker == 4) ? 9f : 5f;
            float ratio = p.powerScale / Mathf.Max(.4f, p.cooldownScale);
            return modeScore + makerBonus + ratio * 15f;
        }

        static float ScoreLegendaryMelee(int index)
        {
            int family = index % MeleeWeaponCatalog.FamilyCount;
            int design = index / MeleeWeaponCatalog.FamilyCount;
            string makerId = ManufacturerCatalog.ForDesign(family, design);
            int maker = ManufacturerCatalog.Index(makerId);
            int mode = (design + 2 * family) % 4;
            float effectScore = 30f;
            switch (maker)
            {
                case 5: effectScore = mode == 3 ? 100f : mode == 1 ? 92f : mode == 0 ? 84f : 72f; break;
                case 4: effectScore = mode == 2 ? 96f : mode == 0 ? 90f : mode == 3 ? 76f : 55f; break;
                case 0: effectScore = mode == 0 ? 88f : mode == 3 ? 80f : mode == 2 ? 74f : 65f; break;
                case 2: effectScore = mode == 1 ? 86f : mode == 0 ? 82f : mode == 2 ? 75f : 68f; break;
                case 3: effectScore = mode == 0 ? 85f : mode == 1 ? 79f : mode == 3 ? 77f : 58f; break;
                case 1: effectScore = mode == 2 ? 83f : mode == 1 ? 73f : mode == 3 ? 70f : 56f; break;
            }
            float[] output = { 1f, .82f, 1.28f, .76f, 1.13f, 1.08f, .92f, 1.16f, 1.05f, 1.24f };
            float[] cadence = { 1f, .78f, 1.24f, .71f, 1.14f, 1.12f, .94f, 1.02f, 1.18f, 1.22f };
            float dpsBias = (output[design] / cadence[design]) * 18f + family * 2.5f;
            return effectScore + dpsBias;
        }

        static string MeleeKey(int index)
        {
            int family = index % MeleeWeaponCatalog.FamilyCount;
            int design = index / MeleeWeaponCatalog.FamilyCount;
            return "melee-" + family.ToString("00") + "-" + design.ToString("00");
        }

        public static bool IsExclusiveGun(string legendaryKey) => !string.IsNullOrEmpty(legendaryKey) && exclusiveGunKeys.Contains(legendaryKey);
        public static bool IsExclusiveArmor(string legendaryKey) => !string.IsNullOrEmpty(legendaryKey) && exclusiveArmorKeys.Contains(legendaryKey);
        public static bool IsExclusiveModule(string catalogId) => !string.IsNullOrEmpty(catalogId) && exclusiveModuleIds.Contains(catalogId);
        public static bool IsExclusiveMelee(int catalogIndex) => exclusiveMeleeIndices.Contains(catalogIndex);

        public static int RollNonExclusiveGunCatalogIndex(System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return nonExclusiveGunCatalogIndices[random.Next(nonExclusiveGunCatalogIndices.Length)];
        }

        public static int RollNonExclusiveArmorIndex(System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return nonExclusiveArmorIndices[random.Next(nonExclusiveArmorIndices.Length)];
        }

        public static ModuleProduct RollNonExclusiveLegendaryModule(System.Random random, int family)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            int fIndex = Mathf.Clamp(family - 4, 0, 2);
            var pool = nonExclusiveModulesByFamily[fIndex];
            return pool.Length > 0 ? pool[random.Next(pool.Length)] : ModuleProductCatalog.Roll(random, family);
        }

        public static int RollNonExclusiveMeleeIndex(System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return nonExclusiveMeleeIndices[random.Next(nonExclusiveMeleeIndices.Length)];
        }

        public static bool TryGetStageExclusive(int stageId, out ExclusiveLegendaryDrop drop) =>
            stageToExclusive.TryGetValue(stageId, out drop);

        public static object CreateStageExclusiveItem(ExclusiveLegendaryDrop drop, System.Random random, int level)
        {
            level = Mathf.Max(1, level);
            switch (drop.category)
            {
                case ExclusiveLegendaryCategory.Gun:
                    return LegendaryCatalog.CreateGun(drop.index, random, level);
                case ExclusiveLegendaryCategory.Armor:
                    return LegendaryCatalog.CreateArmor(drop.index, random, level);
                case ExclusiveLegendaryCategory.Module:
                    var prod = ModuleProductCatalog.Find(drop.catalogId);
                    return ArtifactCatalog.CreateCatalog(drop.catalogId, Rarity.Legendary, level, random, prod != null ? Mathf.Clamp(prod.family - 4, 0, 2) : 0);
                case ExclusiveLegendaryCategory.Melee:
                    return MeleeWeaponCatalog.Catalog(drop.index, Rarity.Legendary, level, random);
                default:
                    return null;
            }
        }

        public static bool TryGetExclusiveStageForItem(object item, out int stageId)
        {
            stageId = -1;
            if (item == null) return false;
            string relic = item is Gun rg ? rg.bossRelicId : item is ArmorItem ra ? ra.bossRelicId : item is ArtifactItem rm ? rm.bossRelicId : item is MeleeWeaponItem rw ? rw.bossRelicId : null;
            if (!string.IsNullOrEmpty(relic)) return false;

            if (item is Gun gun && !string.IsNullOrEmpty(gun.legendaryKey))
                return itemKeyToStage.TryGetValue(gun.legendaryKey, out stageId);
            if (item is ArmorItem armor && !string.IsNullOrEmpty(armor.legendaryKey))
                return itemKeyToStage.TryGetValue(armor.legendaryKey, out stageId);
            if (item is ArtifactItem module && (module.rarity == Rarity.Legendary || module.uniqueEffect > 0) && !string.IsNullOrEmpty(module.catalogId))
                return itemKeyToStage.TryGetValue(module.catalogId, out stageId);
            if (item is MeleeWeaponItem melee && melee.uniqueEffect)
            {
                MeleeWeaponCatalog.InferIdentity(melee);
                if (!string.IsNullOrEmpty(melee.catalogId))
                    return itemKeyToStage.TryGetValue(melee.catalogId, out stageId);
            }
            return false;
        }

        public static string DungeonNumberLabel(int stageId, bool japanese)
        {
            string num = (stageId + 1).ToString("00");
            return japanese
                ? "【ダンジョン " + num + " ボス固有ドロップ】"
                : "[Dungeon " + num + " Boss Exclusive]";
        }
    }
}
