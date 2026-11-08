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
        public int landmarkId;
    }

    // Curates the strongest non-faction-boss legendaries across firearms,
    // armor, modules, and melee weapons, and binds each one exclusively to a specific
    // regular dungeon boss (83 HuntTarget dungeons) or a named overworld landmark boss (30 landmarks).
    // Exclusive legendaries never roll from non-boss or non-matching sources; when the
    // assigned dungeon boss or landmark boss drops a legendary, it has a 50% chance to be its exclusive.
    public static class DungeonBossExclusive
    {
        public const int LandmarkBossCount = 30;

        // Top 30 strongest legendary firearms for dungeon bosses (out of 90 = 33.3%):
        // 5 per manufacturer: the 4 strongest bespoke triggers (variants 0..3) + the strongest expanded mechanism (variant 14: Final Bearing / heavy blast + outer shockwave).
        static readonly int[] ExclusiveGunVariants = { 0, 1, 2, 3, 14 };

        // Next 12 strongest legendary firearms for overworld landmark bosses (2 per manufacturer: variants 4 & 13):
        static readonly int[] LandmarkExclusiveGunVariants = { 4, 13 };

        // Top 8 strongest legendary armor pieces for dungeon bosses (out of 24 active slots = 33.3%):
        static readonly int[] ExclusiveArmorIndices = { 1, 6, 11, 16, 19, 22, 26, 28 };

        // Next 4 strongest legendary armor pieces for overworld landmark bosses:
        static readonly int[] LandmarkExclusiveArmorIndices = { 2, 7, 12, 23 };

        static readonly HashSet<string> exclusiveGunKeys = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> exclusiveArmorKeys = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> exclusiveModuleIds = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<int> exclusiveMeleeIndices = new HashSet<int>();

        static readonly Dictionary<string, int> itemKeyToStage = new Dictionary<string, int>(StringComparer.Ordinal);
        static readonly Dictionary<int, ExclusiveLegendaryDrop> stageToExclusive = new Dictionary<int, ExclusiveLegendaryDrop>();

        static readonly Dictionary<string, int> itemKeyToLandmark = new Dictionary<string, int>(StringComparer.Ordinal);
        static readonly Dictionary<int, ExclusiveLegendaryDrop> landmarkToExclusive = new Dictionary<int, ExclusiveLegendaryDrop>();

        static readonly int[] nonExclusiveGunCatalogIndices;
        static readonly int[] nonExclusiveArmorIndices;
        static readonly ModuleProduct[][] nonExclusiveModulesByFamily;
        static readonly int[] nonExclusiveMeleeIndices;

        static DungeonBossExclusive()
        {
            var drops = new List<ExclusiveLegendaryDrop>(84);

            // 1. 30 Legendary Firearms for Dungeon Bosses (5 per manufacturer, 30 / 90 = 1/3)
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
                        catalogId = key,
                        stageId = -1,
                        landmarkId = -1
                    });
                }
            }

            // 2. 8 Legendary Armor Pieces for Dungeon Bosses (8 / 24 = 1/3)
            for (int i = 0; i < ExclusiveArmorIndices.Length; i++)
            {
                int armorIndex = ExclusiveArmorIndices[i];
                string key = LegendaryCatalog.Key(true, armorIndex);
                exclusiveArmorKeys.Add(key);
                drops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Armor,
                    index = armorIndex,
                    catalogId = key,
                    stageId = -1,
                    landmarkId = -1
                });
            }

            // 3. Legendary Modules ranked by combat impact
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

            int moduleTargetCount = 28; // 28 for dungeon bosses
            for (int i = 0; i < Mathf.Min(moduleTargetCount, legendaryModules.Count); i++)
            {
                var mod = legendaryModules[i];
                exclusiveModuleIds.Add(mod.id);
                drops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Module,
                    index = mod.designVariant,
                    catalogId = mod.id,
                    stageId = -1,
                    landmarkId = -1
                });
            }

            // 4. Legendary Melee Weapons ranked by legendary hit effect & base DPS
            var meleeCandidates = new List<int>(MeleeWeaponCatalog.CatalogCount);
            for (int i = 0; i < MeleeWeaponCatalog.CatalogCount; i++) meleeCandidates.Add(i);
            meleeCandidates.Sort((a, b) =>
            {
                float scoreA = ScoreLegendaryMelee(a);
                float scoreB = ScoreLegendaryMelee(b);
                int cmp = scoreB.CompareTo(scoreA);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            int meleeTargetCount = 17; // 17 for dungeon bosses
            for (int i = 0; i < Mathf.Min(meleeTargetCount, meleeCandidates.Count); i++)
            {
                int meleeIdx = meleeCandidates[i];
                exclusiveMeleeIndices.Add(meleeIdx);
                string key = MeleeKey(meleeIdx);
                drops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Melee,
                    index = meleeIdx,
                    catalogId = key,
                    stageId = -1,
                    landmarkId = -1
                });
            }

            // Deterministically assign each of the 83 exclusive legendaries to a distinct HuntTarget boss dungeon
            // across stages 0..199 (excluding stage 0 tutorial so stage 2..198 are used cleanly).
            int total = drops.Count; // 83
            for (int i = 0; i < total; i++)
            {
                int stageId = 2 + ((i * 23) % 99) * 2;
                while (stageToExclusive.ContainsKey(stageId))
                    stageId = (stageId + 2) % ExpeditionMap.RegularStageCount;

                var entry = drops[i];
                entry.stageId = stageId;
                drops[i] = entry;
                stageToExclusive[stageId] = entry;
                itemKeyToStage[entry.catalogId] = stageId;
            }

            // 5. Build 30 Exclusive Legendaries for the 30 Overworld Landmark Bosses
            // (12 Guns + 4 Armors + 8 Modules + 6 Melee = 30 Landmark Boss Exclusives)
            var landmarkDrops = new List<ExclusiveLegendaryDrop>(LandmarkBossCount);
            for (int maker = 0; maker < 6; maker++)
            {
                for (int v = 0; v < LandmarkExclusiveGunVariants.Length; v++)
                {
                    int variant = LandmarkExclusiveGunVariants[v];
                    int catIndex = LegendaryCatalog.CatalogIndex(maker, variant);
                    string key = LegendaryCatalog.Key(false, catIndex);
                    exclusiveGunKeys.Add(key);
                    landmarkDrops.Add(new ExclusiveLegendaryDrop
                    {
                        category = ExclusiveLegendaryCategory.Gun,
                        index = catIndex,
                        catalogId = key,
                        stageId = -1
                    });
                }
            }
            for (int i = 0; i < LandmarkExclusiveArmorIndices.Length; i++)
            {
                int armorIndex = LandmarkExclusiveArmorIndices[i];
                string key = LegendaryCatalog.Key(true, armorIndex);
                exclusiveArmorKeys.Add(key);
                landmarkDrops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Armor,
                    index = armorIndex,
                    catalogId = key,
                    stageId = -1
                });
            }
            for (int i = moduleTargetCount; i < Mathf.Min(moduleTargetCount + 8, legendaryModules.Count); i++)
            {
                var mod = legendaryModules[i];
                exclusiveModuleIds.Add(mod.id);
                landmarkDrops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Module,
                    index = mod.designVariant,
                    catalogId = mod.id,
                    stageId = -1
                });
            }
            for (int i = meleeTargetCount; i < Mathf.Min(meleeTargetCount + 6, meleeCandidates.Count); i++)
            {
                int meleeIdx = meleeCandidates[i];
                exclusiveMeleeIndices.Add(meleeIdx);
                string key = MeleeKey(meleeIdx);
                landmarkDrops.Add(new ExclusiveLegendaryDrop
                {
                    category = ExclusiveLegendaryCategory.Melee,
                    index = meleeIdx,
                    catalogId = key,
                    stageId = -1
                });
            }

            for (int i = 0; i < landmarkDrops.Count && i < LandmarkBossCount; i++)
            {
                // Interleave categories across the 30 landmarks so neighboring landmarks offer diverse gear types
                int landmarkId = (i * 7) % LandmarkBossCount;
                while (landmarkToExclusive.ContainsKey(landmarkId))
                    landmarkId = (landmarkId + 1) % LandmarkBossCount;

                var entry = landmarkDrops[i];
                entry.landmarkId = landmarkId;
                landmarkDrops[i] = entry;
                landmarkToExclusive[landmarkId] = entry;
                itemKeyToLandmark[entry.catalogId] = landmarkId;
            }

            // Build non-exclusive pools for the remaining general-pool legendaries
            // (never includes any dungeon boss exclusive or landmark boss exclusive)
            var gunPool = new List<int>(48);
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

            var armorPool = new List<int>(12);
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

            var meleePool = new List<int>(27);
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

        public static bool TryGetLandmarkExclusive(int landmarkId, out ExclusiveLegendaryDrop drop) =>
            landmarkToExclusive.TryGetValue(landmarkId, out drop);

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

        static string ExtractItemKey(object item)
        {
            if (item == null) return null;
            string relic = item is Gun rg ? rg.bossRelicId : item is ArmorItem ra ? ra.bossRelicId : item is ArtifactItem rm ? rm.bossRelicId : item is MeleeWeaponItem rw ? rw.bossRelicId : null;
            if (!string.IsNullOrEmpty(relic)) return null;
            if (item is Gun gun && !string.IsNullOrEmpty(gun.legendaryKey)) return gun.legendaryKey;
            if (item is ArmorItem armor && !string.IsNullOrEmpty(armor.legendaryKey)) return armor.legendaryKey;
            if (item is ArtifactItem module && (module.rarity == Rarity.Legendary || module.uniqueEffect > 0) && !string.IsNullOrEmpty(module.catalogId)) return module.catalogId;
            if (item is MeleeWeaponItem melee && melee.uniqueEffect)
            {
                MeleeWeaponCatalog.InferIdentity(melee);
                return melee.catalogId;
            }
            return null;
        }

        public static bool TryGetExclusiveStageForItem(object item, out int stageId)
        {
            stageId = -1;
            string key = ExtractItemKey(item);
            return !string.IsNullOrEmpty(key) && itemKeyToStage.TryGetValue(key, out stageId);
        }

        public static bool TryGetExclusiveLandmarkForItem(object item, out int landmarkId)
        {
            landmarkId = -1;
            string key = ExtractItemKey(item);
            return !string.IsNullOrEmpty(key) && itemKeyToLandmark.TryGetValue(key, out landmarkId);
        }

        public static string DungeonNumberLabel(int stageId, bool japanese)
        {
            string num = (stageId + 1).ToString("00");
            return japanese
                ? "【ダンジョン " + num + " ボス固有ドロップ】"
                : "[Dungeon " + num + " Boss Exclusive]";
        }

        public static string LandmarkBossLabel(int landmarkId, bool japanese)
        {
            string title = OverworldGeography.LandmarkShortTitle(landmarkId, japanese);
            return japanese
                ? "【ランドマーク「" + title + "」ボス固有ドロップ】"
                : "[Landmark '" + title + "' Boss Exclusive]";
        }
    }
}
