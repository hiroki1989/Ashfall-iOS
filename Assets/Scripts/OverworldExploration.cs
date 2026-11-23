using PlayerPrefs = NeonVault.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string FogMilestoneKey = "Vault.OverworldFogRewardTier.v1";
        const string BountySaveKey = "Vault.OverworldBounty.v1";
        const string LoreSaveKey = "Vault.OverworldLore.v1";

        public const int LandmarkCacheSiteBase = 400000;
        public const int LandmarkRecordSiteBase = 410000;
        public const int OverworldFogFullSurveyCells = 26000;
        public const int OverworldBountyCount = 20;

        static readonly int[] FogMilestoneThresholds = { 10, 25, 50, 75, 100 };

        int revealedFogCellCount;
        int loadedFogMilestoneTier = -1;
        int cachedFogMilestoneSlot = -1;

        bool bountyLoaded;
        int activeBountyIndex;
        int bountyKillCount;
        WorldEnemySpawn activeBountySpawn;

        bool loreLoaded;
        uint collectedLoreMask;
        int activeLorePopupId = -1;
        bool overworldLoreArchiveOpen;
        Vector2 overworldLoreScroll;
        int selectedLoreArchiveId;

        [Serializable]
        class OverworldBountySave
        {
            public int index;
            public int kills;
        }

        public void ResetOverworldExplorationRuntimeState()
        {
            revealedFogCellCount = 0;
            loadedFogMilestoneTier = -1;
            cachedFogMilestoneSlot = -1;
            bountyLoaded = false;
            activeBountyIndex = 0;
            bountyKillCount = 0;
            activeBountySpawn = null;
            loreLoaded = false;
            collectedLoreMask = 0;
            activeLorePopupId = -1;
            overworldLoreArchiveOpen = false;
            selectedLoreArchiveId = 0;
        }

        // ------------------------------------------------------------------
        // 1. 30 Landmarks -> 5 Facility Categories, Passives, Scan, Caches & Fast Travel
        // ------------------------------------------------------------------

        // 0: Recon/Comms (Wide Scan + Rare Find), 1: Foundry/Ordnance (Damage),
        // 2: Bastion/Defense (Max HP + Mitigation), 3: Refinery/Logistics (Scrap + Ammo Cap),
        // 4: Sanctuary/Transit (Move Speed + Action Skill Charge)
        public static int LandmarkCategory(int id)
        {
            switch (id)
            {
                case 0: case 4: case 6: case 12: case 20: case 28: return 0;
                case 7: case 15: case 16: case 18: case 24: case 29: return 1;
                case 1: case 5: case 13: case 22: case 23: case 27: return 2;
                case 10: case 11: case 14: case 17: case 21: case 26: return 3;
                default: return 4;
            }
        }

        public int DefeatedLandmarkCountInCategory(int category)
        {
            if (smoke) return 0;
            int count = 0;
            for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
                if (LandmarkCategory(i) == category && IsLandmarkBossDefeated(i)) count++;
            return count;
        }

        public int TotalDefeatedLandmarkCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
                    if (IsLandmarkBossDefeated(i)) count++;
                return count;
            }
        }

        public float LandmarkRareFindBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(0) * 0.04f;
        public float LandmarkDamageBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(1) * 0.025f;
        public float LandmarkHealthBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(2) * 0.05f;
        public float LandmarkMitigationBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(2) * 0.015f;
        public float LandmarkScrapBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(3) * 0.04f;
        public float LandmarkAmmoCapacityBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(3) * 0.05f;
        public float LandmarkMoveSpeedBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(4) * 0.025f;
        public float LandmarkSkillCooldownBonus => smoke ? 0f : DefeatedLandmarkCountInCategory(4) * 0.03f;

        public static string LandmarkBenefitLabel(int landmarkId, bool japanese)
        {
            int cat = LandmarkCategory(landmarkId);
            switch (cat)
            {
                case 0:
                    return japanese
                        ? "制圧恩恵：周囲1,350m開示 ／ レア発見率 +4%"
                        : "Conquest: 1,350m scan / Rare Find +4%";
                case 1:
                    return japanese
                        ? "制圧恩恵：周囲680m開示 ／ 全武器・近接威力 +2.5%"
                        : "Conquest: 680m scan / All Weapon & Melee DMG +2.5%";
                case 2:
                    return japanese
                        ? "制圧恩恵：周囲680m開示 ／ 最大HP +5%・被ダメ軽減 +1.5%"
                        : "Conquest: 680m scan / Max HP +5% & Mitigation +1.5%";
                case 3:
                    return japanese
                        ? "制圧恩恵：周囲680m開示 ／ スクラップ +4%・弾薬上限 +5%"
                        : "Conquest: 680m scan / Scrap +4% & Ammo Cap +5%";
                default:
                    return japanese
                        ? "制圧恩恵：周囲680m開示 ／ 移動速度 +2.5%・スキル回転 +3%"
                        : "Conquest: 680m scan / Move Speed +2.5% & Skill Charge +3%";
            }
        }

        public string LandmarkTotalPassiveSummary(bool japanese)
        {
            int total = TotalDefeatedLandmarkCount;
            if (total == 0)
                return japanese
                    ? "ランドマーク未制圧（撃破で永続バフ＋広域地図開示＋ファストトラベル解放）"
                    : "No landmarks conquered (defeat bosses for passives, map scan & fast travel)";
            return japanese
                ? "制圧 " + total + "/30：威力+" + (LandmarkDamageBonus * 100f).ToString("0.#")
                  + "% / HP+" + (LandmarkHealthBonus * 100f).ToString("0")
                  + "% / 軽減+" + (LandmarkMitigationBonus * 100f).ToString("0.#")
                  + "% / 金+" + (LandmarkScrapBonus * 100f).ToString("0")
                  + "% / 運+" + (LandmarkRareFindBonus * 100f).ToString("0")
                  + "% / 速+" + (LandmarkMoveSpeedBonus * 100f).ToString("0.#") + "%"
                : "Conquered " + total + "/30: DMG+" + (LandmarkDamageBonus * 100f).ToString("0.#")
                  + "% / HP+" + (LandmarkHealthBonus * 100f).ToString("0")
                  + "% / MIT+" + (LandmarkMitigationBonus * 100f).ToString("0.#")
                  + "% / SCR+" + (LandmarkScrapBonus * 100f).ToString("0")
                  + "% / LCK+" + (LandmarkRareFindBonus * 100f).ToString("0")
                  + "% / SPD+" + (LandmarkMoveSpeedBonus * 100f).ToString("0.#") + "%";
        }

        public Vector3 LandmarkCachePosition(int id)
        {
            id = Mathf.Clamp(id, 0, OverworldGeography.LandmarkCount - 1);
            return FindClearOffsetNear(WorldGeography.Landmarks[id], new Vector3(-4.8f, .65f, -6.5f));
        }

        public Vector3 LandmarkRecordPosition(int id)
        {
            id = Mathf.Clamp(id, 0, OverworldGeography.LandmarkCount - 1);
            return FindClearOffsetNear(WorldGeography.Landmarks[id], new Vector3(4.8f, .65f, -6.5f));
        }

        Vector3 FindClearOffsetNear(Vector3 center, Vector3 preferredOffset)
        {
            Vector3 p = center + preferredOffset;
            p.y = .65f;
            if (!WorldGeography.Blocked(p, .6f) && !WorldEntranceBuildings.Blocked(p, .6f, WorldGeography))
                return p;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector3 c = center + new Vector3(Mathf.Cos(a) * 6.5f, .65f, Mathf.Sin(a) * 6.5f);
                if (!WorldGeography.Blocked(c, .6f) && !WorldEntranceBuildings.Blocked(c, .6f, WorldGeography))
                    return c;
            }
            return p;
        }

        public bool IsLandmarkCacheOpened(int id)
        {
            if (id < 0 || id >= OverworldGeography.LandmarkCount) return false;
            return PlayerPrefs.GetInt("Vault.LandmarkCacheOpened." + id, 0) != 0;
        }

        void MarkLandmarkCacheOpened(int id)
        {
            if (id < 0 || id >= OverworldGeography.LandmarkCount) return;
            PlayerPrefs.SetInt("Vault.LandmarkCacheOpened." + id, 1);
            PlayerPrefs.Save();
        }

        void OnLandmarkConquered(int landmarkId, Vector3 bossPos, bool firstKill)
        {
            if (landmarkId < 0 || landmarkId >= OverworldGeography.LandmarkCount) return;
            float scanRadius = LandmarkCategory(landmarkId) == 0 ? 1350f : 680f;
            Vector3 center = WorldGeography.Landmarks[landmarkId];
            RevealOverworldFogRect(center, scanRadius, scanRadius, true);

            int newlyDiscovered = 0;
            float sq = scanRadius * scanRadius;
            for (int s = 0; s < WorldGeography.Entrances.Length; s++)
            {
                Vector3 d = WorldGeography.Entrances[s] - center;
                d.y = 0f;
                if (d.sqrMagnitude <= sq && !IsOverworldDungeonDiscovered(s))
                {
                    DiscoverOverworldDungeon(s);
                    newlyDiscovered++;
                }
            }

            if (firstKill)
            {
                if (DungeonBossExclusive.TryGetLandmarkExclusive(landmarkId, out var excl))
                {
                    int level = OverworldGeography.LandmarkBossLevel(landmarkId, DebtLevel);
                    var item = DungeonBossExclusive.CreateStageExclusiveItem(excl, rng, level);
                    Vector3 dropPos = bossPos + Vector3.right * .9f;
                    if (item is Gun g) AddDrop(g, dropPos);
                    else if (item is ArmorItem a) AddArmorDrop(a, dropPos);
                    else if (item is ArtifactItem m) AddArtifactDrop(m, dropPos);
                    else if (item is MeleeWeaponItem w) AddMeleeDrop(w, dropPos);
                    PlayLegendaryRewardSound();
                }
                RefreshPermanentStats();
                RefreshOverworldActivities();
                string msgEn = "CONQUERED / " + OverworldGeography.LandmarkShortTitle(landmarkId, false)
                    + " — Sealed Cache & Fast Travel unlocked"
                    + (newlyDiscovered > 0 ? " (+" + newlyDiscovered + " sites mapped)" : "");
                string msgJa = OverworldGeography.LandmarkShortTitle(landmarkId, true) + " 制圧！"
                    + " 封印物資庫・ファストトラベル解放"
                    + (newlyDiscovered > 0 ? "（周囲" + newlyDiscovered + "地点を発見）" : "");
                Notify(msgEn, msgJa);
            }
            else
            {
                Notify(
                    "LANDMARK BOSS DEFEATED / " + OverworldGeography.LandmarkBossName(landmarkId, false),
                    "ランドマークボス撃破 / " + OverworldGeography.LandmarkBossName(landmarkId, true));
            }
        }

        public int NearestUndefeatedLandmark(Vector3 from)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
            {
                if (IsLandmarkBossDefeated(i)) continue;
                float d = (WorldGeography.Landmarks[i] - from).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = i; }
            }
            return best;
        }

        // ------------------------------------------------------------------
        // 2. Fog-of-War Survey Rate & Milestone Rewards
        // ------------------------------------------------------------------

        public int OverworldSurveyPercent =>
            Mathf.Clamp(Mathf.FloorToInt(revealedFogCellCount * 100f / OverworldFogFullSurveyCells), 0, 100);

        public int CurrentFogMilestoneTier
        {
            get
            {
                if (cachedFogMilestoneSlot != SaveSlotPrefs.Current || loadedFogMilestoneTier < 0)
                {
                    cachedFogMilestoneSlot = SaveSlotPrefs.Current;
                    loadedFogMilestoneTier = Mathf.Clamp(PlayerPrefs.GetInt(FogMilestoneKey, 0), 0, FogMilestoneThresholds.Length);
                }
                return loadedFogMilestoneTier;
            }
        }

        public int NextFogMilestonePercent
        {
            get
            {
                int tier = CurrentFogMilestoneTier;
                return tier >= FogMilestoneThresholds.Length ? 100 : FogMilestoneThresholds[tier];
            }
        }

        void UpdateRevealedFogStatsAndMilestones(int cellCount)
        {
            revealedFogCellCount = cellCount;
            if (smoke || !WorldStorySeen || !IsOverworld || screen == Screen.Dead || screen == Screen.Menu) return;
            CheckOverworldFogMilestones();
        }

        void CheckOverworldFogMilestones()
        {
            int tier = CurrentFogMilestoneTier;
            int pct = OverworldSurveyPercent;
            bool changed = false;
            while (tier < FogMilestoneThresholds.Length && pct >= FogMilestoneThresholds[tier])
            {
                int threshold = FogMilestoneThresholds[tier];
                int scrapReward = ScaleScrap((tier + 1) * 12000000);
                int skillGain = threshold >= 100 ? 2 : 1;
                AwardCredits(scrapReward);
                ExpeditionTokens += skillGain;
                GrantSurveyMilestoneGear(tier, PlayerPosition + Vector3.right * 1.4f);
                SaveProgression();
                SaveInventory();
                tier++;
                loadedFogMilestoneTier = tier;
                PlayerPrefs.SetInt(FogMilestoneKey, tier);
                PlayerPrefs.Save();
                changed = true;
                Notify(
                    "MAP SURVEY " + threshold + "% / SCRAP +" + scrapReward.ToString("N0") + " · SKILL PT +" + skillGain,
                    "踏破率 " + threshold + "% 達成！ スクラップ +" + scrapReward.ToString("N0") + "・スキルpt +" + skillGain);
            }
            if (changed) PlayLegendaryRewardSound();
        }

        void GrantSurveyMilestoneGear(int tier, Vector3 dropPos)
        {
            var r = new System.Random(88100 + tier * 997 + SaveSlotPrefs.Current * 31);
            Rarity rarity = tier >= 2 ? Rarity.Legendary : Rarity.Epic;
            int level = Mathf.Max(2, DebtLevel + 1);
            switch (tier % 3)
            {
                case 0:
                    var g = Arsenal.CreateForDungeon(r, DungeonIndex, rarity, level);
                    if (!StoreWeapon(g, true)) AddDrop(g, dropPos);
                    break;
                case 1:
                    var a = Armory.CreateForDungeon(r, DungeonIndex, rarity, level);
                    if (!StoreArmor(a, true)) AddArmorDrop(a, dropPos);
                    break;
                default:
                    var m = ArtifactCatalog.Create(r, DungeonIndex, rarity, level);
                    if (!StoreArtifact(m, true)) AddArtifactDrop(m, dropPos);
                    break;
            }
        }

        // ------------------------------------------------------------------
        // 3. Overworld Bounties (賞金首)
        // ------------------------------------------------------------------

        static readonly string[] BountyNamesEn =
        {
            "\"Rustjaw\" Krell", "\"Red-Eye\" Vora", "\"Cinder\" Mako", "\"Scrap-Duke\" Hal",
            "\"Deadbolt\" Torin", "\"Ash-Hound\" Rafe", "\"Spark\" Soren", "\"Leadfoot\" Dane",
            "\"Tollkeeper\" Grigg", "\"Mire-Witch\" Tessa", "\"Chrome-Jaw\" Briggsby", "\"Pyre\" Calder",
            "\"Wiretap\" Lin", "\"Sledge\" Orlov", "\"Soot-Baron\" Vexer", "\"Switchblade\" Nori",
            "\"Iron-Lung\" Kade", "\"Crosswind\" Faye", "\"Zero-Sum\" Dax", "\"Grave-Shift\" Rook"
        };

        static readonly string[] BountyNamesJa =
        {
            "「錆顎」クレル", "「赤目」ヴォラ", "「燃え滓」マコ", "「屑鉄公」ハル",
            "「デッドボルト」トリン", "「灰猟犬」レイフ", "「スパーク」ソレン", "「鉛足」デイン",
            "「関所守」グリッグ", "「泥沼の魔女」テッサ", "「鉄顎」ブリッグスビー", "「火葬場」カルダー",
            "「盗聴屋」リン", "「大槌」オルロフ", "「煤男爵」ヴェクサー", "「飛び出しナイフ」ノリ",
            "「鉄肺」ケイド", "「横風」フェイ", "「ゼロサム」ダックス", "「墓番」ルーク"
        };

        void EnsureBountyLoaded()
        {
            if (bountyLoaded) return;
            bountyLoaded = true;
            activeBountyIndex = 0;
            bountyKillCount = 0;
            if (smoke || !PlayerPrefs.HasKey(BountySaveKey)) return;
            try
            {
                var data = JsonUtility.FromJson<OverworldBountySave>(PlayerPrefs.GetString(BountySaveKey));
                if (data != null)
                {
                    activeBountyIndex = Mathf.Max(0, data.index);
                    bountyKillCount = Mathf.Max(0, data.kills);
                }
            }
            catch { }
        }

        void SaveBountyState()
        {
            if (smoke) return;
            EnsureBountyLoaded();
            PlayerPrefs.SetString(BountySaveKey, JsonUtility.ToJson(new OverworldBountySave { index = activeBountyIndex, kills = bountyKillCount }));
            PlayerPrefs.Save();
        }

        public int ActiveBountyIndex { get { EnsureBountyLoaded(); return activeBountyIndex % OverworldBountyCount; } }
        public int OverworldBountyKillCount { get { EnsureBountyLoaded(); return bountyKillCount; } }
        public int ActiveBountyLevel => Mathf.Max(2, DebtLevel + 1);

        public string ActiveBountyName(bool japanese) =>
            (japanese ? BountyNamesJa : BountyNamesEn)[ActiveBountyIndex];

        public Vector3 ActiveBountyPosition
        {
            get
            {
                int idx = ActiveBountyIndex;
                var random = new System.Random(61903 + idx * 4391);
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    int gx = -42 + random.Next(85);
                    int gz = -42 + random.Next(85);
                    float along = gz * 100f + 20f + (float)random.NextDouble() * 60f;
                    var p = new Vector3(WorldGeography.StreetX(gx, along) + 3f, .65f, along);
                    if (Mathf.Abs(p.x) > 4600f || Mathf.Abs(p.z) > 4600f) continue;
                    if (WorldGeography.River(p) && !WorldGeography.Bridge(p.z)) continue;
                    bool safe = true;
                    foreach (var camp in WorldGeography.Camps)
                        if ((camp - p).sqrMagnitude < 45f * 45f) { safe = false; break; }
                    if (!safe) continue;
                    if (!WorldGeography.Blocked(p, 1.2f) && !WorldEntranceBuildings.Blocked(p, 1.2f, WorldGeography))
                        return p;
                }
                return WorldGeography.Landmarks[(idx * 7) % OverworldGeography.LandmarkCount] + new Vector3(0f, .65f, -18f);
            }
        }

        public bool IsBountyEnemy(Foe foe) =>
            foe != null && worldEnemyInstances.TryGetValue(foe, out var s) && ReferenceEquals(s, activeBountySpawn);

        void AddOverworldBountySpawns()
        {
            EnsureBountyLoaded();
            Vector3 pos = ActiveBountyPosition;
            int faction = (ActiveBountyIndex % 2) + 1;
            int level = ActiveBountyLevel;
            activeBountySpawn = new WorldEnemySpawn
            {
                position = pos,
                kind = 3,
                level = level,
                faction = faction,
                elite = true
            };
            worldEnemySpawns.Add(activeBountySpawn);
            for (int g = 0; g < 2; g++)
            {
                Vector3 gp = pos + new Vector3(g == 0 ? -4.5f : 4.5f, 0f, -2.5f);
                worldEnemySpawns.Add(new WorldEnemySpawn
                {
                    position = gp,
                    kind = 10,
                    level = Mathf.Max(1, level - 1),
                    faction = faction,
                    elite = true
                });
            }
        }

        void OnOverworldBountyKilled(Vector3 pos)
        {
            EnsureBountyLoaded();
            string slainName = ActiveBountyName(VaultLanguage.Japanese);
            int rewardScrap = ScaleScrap(22000000);
            AwardCredits(rewardScrap);

            var r = new System.Random(unchecked(74123 + activeBountyIndex * 911 + bountyKillCount * 37));
            Rarity rarity = r.NextDouble() < .35 ? Rarity.Legendary : Rarity.Epic;
            int level = ActiveBountyLevel;
            switch (r.Next(3))
            {
                case 0: AddDrop(Arsenal.CreateForDungeon(r, DungeonIndex, rarity, level), pos + Vector3.right * .9f); break;
                case 1: AddArmorDrop(Armory.CreateForDungeon(r, DungeonIndex, rarity, level), pos + Vector3.right * .9f); break;
                default: AddArtifactDrop(ArtifactCatalog.Create(r, DungeonIndex, rarity, level), pos + Vector3.right * .9f); break;
            }
            if (rarity == Rarity.Legendary) PlayLegendaryRewardSound();
            else PlayUserSound("transaction", .4f);

            activeBountyIndex++;
            bountyKillCount++;
            SaveBountyState();
            AddOverworldBountySpawns();

            if (OverworldWaypointKind == 4)
                SetOverworldWaypoint(4, ActiveBountyIndex);

            Notify(
                "BOUNTY CLAIMED / " + slainName + " · SCRAP +" + rewardScrap.ToString("N0"),
                "賞金首討伐「" + slainName + "」 / スクラップ +" + rewardScrap.ToString("N0"));
        }

        public void TrackActiveOverworldBounty()
        {
            EnsureBountyLoaded();
            SetOverworldWaypoint(4, ActiveBountyIndex);
            Notify(
                "BOUNTY TRACKED / " + ActiveBountyName(false),
                "賞金首を目的地に設定 / " + ActiveBountyName(true));
        }

        // ------------------------------------------------------------------
        // 4. Field Records (30 Concise In-World Lore Entries)
        // ------------------------------------------------------------------

        static readonly string[] LoreEntriesJa =
        {
            "時計塔の針は午後3時14分で止まったままだ。\n下に詰まった封筒はすべて、開けられる前に焼けている。",
            "凱旋門の銘板は削り取られ、上から別の名前が刻まれている。\n勝った側だけが、石に文字を残せる。",
            "噴水の底にはコインの代わりに空薬莢が溜まっている。\n水は12年前に枯れたが、願い事をする奴はまだいる。",
            "円形神殿の議事録は途中で千切れている。\n「壁の外は自己責任とする」──最後の決議はそれだけだった。",
            "給水塔のタンク内壁に、赤い塗料で「毒なし」と殴り書き。\nその横に、違う筆跡で「水もなし」と書き足されている。",
            "黒曜石の周りに並んだ測量杭は、すべて同じ方向を向いて倒れている。\n衝撃は北西から来た。",
            "自動リピーターは同じ短縮番号を繰り返し吐き出している。\n受信側の周波数帯には、もう誰も応答しない。",
            "出荷伝票の最終行：「第4サイロの鍵はブリッグスに預けた」。\n日付はアッシュフォール封鎖の前夜だ。",
            "古い石祠の賽銭箱に、ドッグタグが一枚だけ入っていた。\n裏面の刻印は擦り切れて「K-9 / R」としか読めない。",
            "ガラスドームの記録盤には、最後の研究員のメモが残る。\n「根がコンクリートを割り始めた。避難先より先に、種が逃げた」",
            "駅構内の路線図には、実在しない分岐駅が鉛筆で描き足されている。\n地下は上の連中が思っているよりずっと広い。",
            "第2貯水タンクの点検ハッチに鎖と南京錠。\n内側から叩き凹ませた跡が、金属にいくつも残っている。",
            "観測日誌の最終ページ：「軌道上のライトが3つ消えた。\n今夜からは地面の光だけを頼りにする」。",
            "慰霊碑の篝火台には、まだ温かい灰が混ざっていた。\n誰かが毎晩ここに油を注ぎに来ている。",
            "旧市庁舎の金庫室は空だった。\n残っていたのは、マルコム・ヴェイル名義の債務買収契約書一通だけだ。",
            "転車台のブレーキレバーに黒い絶縁テープが巻かれている。\n輸送列車が脱線した夜、誰かが意図的にポイントを固定した。",
            "クレーンの運転席に家族写真がテープで貼られている。\n裏に一言：「港が閉まる前に必ず戻る」。",
            "冷却塔の線量計は振り切れたまま針が曲がっている。\nそれでもレッドクロウの連中はここを荷物置き場にしている。",
            "高炉の作業日誌：「今夜溶かすのはスクラップじゃない。\nヴェイルの帳簿に載らない人間の装備一式だ」。",
            "ステージ袖のマイクスタンドに、錆びた首輪が引っ掛かっている。\n爆薬は抜かれているが、型番はノックスのものと同じだった。",
            "レーダー室の壁にピン留めされた航空写真。\n川の西岸にだけ、赤い丸印がいくつも書かれている。",
            "掘削リグの泥受けに落ちていた暗号メモ：\n『首輪の起爆信号は旧軍事バンド 418.2MHz』。",
            "大聖堂の告解室に無線機が隠されていた。\n「神様じゃない、イオナだ。犬を連れた男がいたら通してやれ」",
            "検疫ゲートの通過スタンプ欄は最後のページまで埋まっている。\n一番下の署名欄には『MALCOLM VALE』のスタンプ。",
            "装甲列車の貨物明細：『弾薬箱12、医療キット8、未分類の首輪パーツ24』。\n襲撃された車列と番号が一致している。",
            "鐘楼の鎖は切れておらず、わざと外されて下に降ろされていた。\n鳴らせば街中の感染者がここへ集まる。",
            "風力タービンの保全票：「ブレードが折れても発電は止めるな。\n基地の照明が落ちたら終わりだ」。",
            "バンカー司令室の黒板：『借金10億は飾りだ。\nヴェイルが欲しいのは、死なずに稼ぎ続ける駒の戦闘データ』。 ",
            "パラボラ群の保守ノート：「衛星は死んでいない。応答を返さないだけだ。\nこっちの暗号鍵がとっくに期限切れらしい」。",
            "発射台の整備士メモ：「カウントダウンは3で止まった。\n上へ逃げるはずだった連中は、いま頃この街の下にいる」。"
        };

        static readonly string[] LoreEntriesEn =
        {
            "The clock face stopped at 3:14 PM.\nEvery envelope wedged under the door burned before it was opened.",
            "The arch plaque was chiseled flat and recarved.\nOnly the winners get to keep their names in stone.",
            "Spent brass sits in the dry fountain basin instead of coins.\nThe water ran out twelve years ago; people still toss things in.",
            "The rotunda minutes tear off mid-sentence:\n'Defense beyond the inner wall remains the citizen's sole responsibility.'",
            "Red paint inside the tank reads NO TOXIN.\nSomeone scratched underneath in smaller letters: NO WATER EITHER.",
            "Every survey pylon around the obelisk leans the exact same way.\nThe blast came out of the northwest.",
            "The automated repeater loops the same three-digit call.\nNo one left alive monitors the receiving band.",
            "Last line on the shipping manifest: 'Silo 4 key left with Briggs.'\nDated the night before Ashfall sealed.",
            "Inside the shrine's wooden offering box sits a single tin tag.\nHalf-worn stamp reads only: 'K-9 / R.'",
            "A clipped note inside the glass dome:\n'Roots split the foundation slab today. The seeds got out before we did.'",
            "Someone penciled a phantom branch line onto the transit map.\nMore tunnels run under the city than Vale's ledger admits.",
            "Padlocks hang on the twin cistern hatches.\nThe dents on the steel were hammered from the inside.",
            "Final entry in the observatory log: 'Three orbital markers went dark.\nFrom tonight on, watch the ground instead.'",
            "Fresh lamp oil still clings to the ash in the cenotaph braziers.\nSomeone crosses the ridge after dark to keep them lit.",
            "The municipal vault stands empty.\nOnly a bulk debt-acquisition deed signed by Malcolm Vale remains on the desk.",
            "Black tape binds the turntable switch lever.\nSomeone locked the rail spur the night the supply train derailed.",
            "A faded photograph is taped above the gantry controls.\nWritten on the back: 'Back before the docks close.'",
            "The dosimeter needle bent against the max stop.\nRed Claw still uses the cooling floor to stash hot crates.",
            "Foundry shift sheet: 'Tonight's melt isn't scrap iron.\nIt's every unregistered rig Vale pulled off the road.'",
            "A gutted explosive collar hangs on the stage mic stand.\nCharge removed—same serial batch as Knox's.",
            "Aerial stills pinned across the radar wall.\nEvery red circle sits on the west side of the river.",
            "Greased cipher scrap beside the derrick sump:\n'Collar pulse runs on old military band 418.2 MHz.'",
            "A field transceiver is tucked inside the confessional booth.\n'Not a priest—it's Iona. Any runner with a hound gets clear passage.'",
            "The quarantine logbook is stamped clean to the last margin.\nEvery pass on the bottom sheet carries 'M. VALE' in ink.",
            "Armored train bill of lading: 'Ammo x12, Med x8, Collar cores x24.'\nSame convoy serial as the northern ambush.",
            "The bell chain wasn't snapped—it was unhooked and lowered.\nRing it once and half the infected in the sector converge.",
            "Turbine maintenance ticket: 'Keep rotor #2 spinning even sheared.\nIf the camp floodlights die, we're done.'",
            "Chalked across the bunker board: 'The billion-scrap tab is theater.\nVale farms live combat telemetry from runners who won't die.'",
            "Dish array tech note: 'The bird up top isn't dead—it just won't handshake.\nOur uplink keys expired years ago.'",
            "Gantry checklist: 'Hold called at T-minus three.\nEveryone booked on the upper deck went underground instead.'"
        };

        void EnsureLoreLoaded()
        {
            if (loreLoaded) return;
            loreLoaded = true;
            collectedLoreMask = 0;
            if (smoke || !PlayerPrefs.HasKey(LoreSaveKey)) return;
            if (uint.TryParse(PlayerPrefs.GetString(LoreSaveKey, "0"), out uint mask))
                collectedLoreMask = mask;
        }

        void SaveLoreState()
        {
            if (smoke) return;
            EnsureLoreLoaded();
            PlayerPrefs.SetString(LoreSaveKey, collectedLoreMask.ToString());
            PlayerPrefs.Save();
        }

        public bool IsOverworldLoreCollected(int id)
        {
            EnsureLoreLoaded();
            return id >= 0 && id < OverworldGeography.LandmarkCount && (collectedLoreMask & (1u << id)) != 0;
        }

        public int CollectedLoreCount
        {
            get
            {
                EnsureLoreLoaded();
                int count = 0;
                for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
                    if ((collectedLoreMask & (1u << i)) != 0) count++;
                return count;
            }
        }

        void CollectOverworldLore(int id)
        {
            if (id < 0 || id >= OverworldGeography.LandmarkCount) return;
            EnsureLoreLoaded();
            bool isNew = !IsOverworldLoreCollected(id);
            collectedLoreMask |= (1u << id);
            SaveLoreState();

            if (isNew)
            {
                int scrap = ScaleScrap(3500000);
                AwardCredits(scrap);
                PlayUserSound("transaction", .35f);
                Notify(
                    "FIELD RECORD " + (id + 1).ToString("00") + "/30 · SCRAP +" + scrap.ToString("N0"),
                    "調査記録 " + (id + 1).ToString("00") + "/30 を回収 · スクラップ +" + scrap.ToString("N0"));
            }
            activeLorePopupId = id;
            selectedLoreArchiveId = id;
            Time.timeScale = 0f;
            RefreshOverworldActivities();
        }

        // ------------------------------------------------------------------
        // 5. Integration into Overworld Activities, Caches, Records & Popups
        // ------------------------------------------------------------------

        void AddLandmarkExplorationActivities()
        {
            if (!IsOverworld || player == null) return;
            Vector3 pos = PlayerPosition;
            for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
            {
                Vector3 lmPos = WorldGeography.Landmarks[i];
                if ((lmPos - pos).sqrMagnitude > 165f * 165f) continue;

                if (IsLandmarkBossDefeated(i) && !IsLandmarkCacheOpened(i))
                {
                    nearbyOverworldActivities.Add(new OverworldActivitySite
                    {
                        id = LandmarkCacheSiteBase + i,
                        type = 7,
                        position = LandmarkCachePosition(i)
                    });
                }

                if (!IsOverworldLoreCollected(i))
                {
                    nearbyOverworldActivities.Add(new OverworldActivitySite
                    {
                        id = LandmarkRecordSiteBase + i,
                        type = 6,
                        position = LandmarkRecordPosition(i)
                    });
                }
            }
        }

        bool HandleCustomOverworldSite(OverworldActivitySite site)
        {
            if (site == null) return false;
            if (site.id >= LandmarkCacheSiteBase && site.id < LandmarkCacheSiteBase + OverworldGeography.LandmarkCount)
            {
                int lmId = site.id - LandmarkCacheSiteBase;
                if (!IsLandmarkBossDefeated(lmId) || IsLandmarkCacheOpened(lmId)) return false;
                MarkLandmarkCacheOpened(lmId);
                var visuals = world ? world.GetComponent<OverworldActivityVisuals>() : null;
                if (visuals) visuals.OpenReward(site.id);

                int level = OverworldGeography.LandmarkBossLevel(lmId, DebtLevel);
                var r = new System.Random(unchecked(30011 + lmId * 773 + DebtLevel * 19));
                Rarity rarity = r.NextDouble() < .45 ? Rarity.Legendary : Rarity.Epic;
                Vector3 dropPos = site.position + Vector3.right * 1.1f;
                switch (r.Next(3))
                {
                    case 0: AddDrop(Arsenal.CreateForDungeon(r, DungeonIndex, rarity, level), dropPos); break;
                    case 1: AddArmorDrop(Armory.CreateForDungeon(r, DungeonIndex, rarity, level), dropPos); break;
                    default: AddArtifactDrop(ArtifactCatalog.Create(r, DungeonIndex, rarity, level), dropPos); break;
                }
                int bonusScrap = ScaleScrap(8000000);
                AwardCredits(bonusScrap);
                if (rarity == Rarity.Legendary) PlayLegendaryRewardSound();
                else PlayUserSound("transaction", .4f);
                SaveInventory();
                RefreshOverworldActivities();
                Notify(
                    "SEALED CACHE OPENED / SCRAP +" + bonusScrap.ToString("N0"),
                    "封印物資庫を開封！ スクラップ +" + bonusScrap.ToString("N0"));
                return true;
            }
            if (site.id >= LandmarkRecordSiteBase && site.id < LandmarkRecordSiteBase + OverworldGeography.LandmarkCount)
            {
                int lmId = site.id - LandmarkRecordSiteBase;
                var visuals = world ? world.GetComponent<OverworldActivityVisuals>() : null;
                if (visuals) visuals.OpenReward(site.id);
                CollectOverworldLore(lmId);
                return true;
            }
            return false;
        }

        public bool OverworldExplorationModalOpen =>
            (IsOverworld && activeLorePopupId >= 0) || overworldLoreArchiveOpen;

        public void CloseOverworldExplorationModal()
        {
            activeLorePopupId = -1;
            overworldLoreArchiveOpen = false;
            if (screen == Screen.Playing && selectedOverworldActivity == null && !InventoryOpen)
                Time.timeScale = 1f;
        }

        public void OpenOverworldLoreArchive()
        {
            EnsureLoreLoaded();
            for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
                if (IsOverworldLoreCollected(i)) { selectedLoreArchiveId = i; break; }
            overworldLoreArchiveOpen = true;
        }

        void DrawOverworldLorePopup()
        {
            if (activeLorePopupId < 0 || activeLorePopupId >= OverworldGeography.LandmarkCount) return;
            Box(new Rect(0, 0, 1440, 900), new Color(0f, 0f, 0f, .72f));
            Rect card = new Rect(300, 230, 840, 420);
            Panel(card, Cyan);
            string title = (activeLorePopupId + 1).ToString("00") + " / " + OverworldGeography.LandmarkShortTitle(activeLorePopupId, VaultLanguage.Japanese);
            WorldLabel(new Rect(335, 255, 700, 32), T("FIELD RECORD RECOVERED", "調査記録を回収") + "  ·  " + CollectedLoreCount + "/30", Cyan, 20);
            WorldLabel(new Rect(335, 290, 770, 46), title, Gold, 28);
            Box(new Rect(335, 344, 770, 2), new Color(.35f, .52f, .58f, .55f));
            string body = (VaultLanguage.Japanese ? LoreEntriesJa : LoreEntriesEn)[activeLorePopupId];
            WorldLabel(new Rect(335, 362, 770, 180), body, UiPaper, 24);
            if (WorldButton(new Rect(910, 565, 195, 58), T("CLOSE", "閉じる"), true))
                CloseOverworldExplorationModal();
        }

        public void DrawOverworldLoreArchiveOverlay()
        {
            if (!overworldLoreArchiveOpen) return;
            EnsureLoreLoaded();
            Box(new Rect(0, 0, 1440, 900), new Color(.02f, .035f, .048f, .97f));
            WorldLabel(new Rect(45, 28, 760, 48), T("ASHFALL / FIELD RECORDS (", "アッシュフォール / 調査記録（") + CollectedLoreCount + "/30" + (VaultLanguage.Japanese ? "）" : ")"), Gold, 30);
            if (WorldButton(new Rect(1195, 24, 205, 54), T("CLOSE", "閉じる"), true))
            {
                CloseOverworldExplorationModal();
                return;
            }

            Rect listBox = new Rect(45, 96, 520, 760);
            Panel(listBox, Cyan);
            overworldLoreScroll = GUI.BeginScrollView(new Rect(55, 108, 500, 736), overworldLoreScroll, new Rect(0, 0, 474, OverworldGeography.LandmarkCount * 54f));
            for (int i = 0; i < OverworldGeography.LandmarkCount; i++)
            {
                bool collected = IsOverworldLoreCollected(i);
                string tag = collected ? "✓ " : "· ";
                string rowLabel = (i + 1).ToString("00") + " " + tag + (collected ? OverworldGeography.LandmarkShortTitle(i, VaultLanguage.Japanese) : "？？？");
                float ry = i * 54f;
                if (WorldButton(new Rect(4, ry + 2, 466, 48), rowLabel, true))
                    selectedLoreArchiveId = i;
            }
            GUI.EndScrollView();

            Rect detailBox = new Rect(590, 96, 810, 760);
            Panel(detailBox, Gold);
            int id = Mathf.Clamp(selectedLoreArchiveId, 0, OverworldGeography.LandmarkCount - 1);
            bool hasRecord = IsOverworldLoreCollected(id);
            WorldLabel(new Rect(630, 130, 730, 34), T("LANDMARK ", "ランドマーク ") + (id + 1).ToString("00") + " / 30", Cyan, 20);
            WorldLabel(new Rect(630, 168, 730, 56), hasRecord ? OverworldGeography.LandmarkTitle(id, VaultLanguage.Japanese) : "？？？", Gold, 28);
            Box(new Rect(630, 232, 730, 2), new Color(.45f, .58f, .60f, .45f));
            string detail = hasRecord
                ? (VaultLanguage.Japanese ? LoreEntriesJa : LoreEntriesEn)[id]
                : T("Unrecovered field terminal.\nSearch around this landmark to collect its data log.", "未回収の端末記録。\nこのランドマーク周辺を探索すると記録を回収できます。");
            WorldLabel(new Rect(630, 260, 730, 320), detail, hasRecord ? UiPaper : Mist, 24);
            WorldLabel(new Rect(630, 620, 730, 42), LandmarkBenefitLabel(id, VaultLanguage.Japanese), Cyan, 20);
            if (WorldButton(new Rect(630, 690, 380, 58), T("TRACK THIS LANDMARK", "このランドマークを目的地に指定"), true))
            {
                SetOverworldWaypoint(3, id);
                overworldSelectedLandmark = id;
                overworldLoreArchiveOpen = false;
                if (screen == Screen.DungeonSelect)
                {
                    Vector3 lm = WorldGeography.Landmarks[id];
                    worldMapCenter = new Vector2(lm.x / 10000f + .5f, lm.z / 10000f + .5f);
                    ClampWorldMapCenter();
                }
            }
        }
    }
}
