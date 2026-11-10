using PlayerPrefs = NeonVault.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string BriggsMissionSaveKey = "Vault.Missions.v1";

        [Serializable]
        class CampMissionOfferSave
        {
            public int camp;
            public int level;
            public int cycle;
            public List<int> stages;
        }

        [Serializable]
        class BriggsMissionSave
        {
            public int version = 1;
            public int activeStage = -1;
            public int activeCamp = -1;
            public bool readyToTurnIn;
            public int offerCycle;
            public List<int> paidFirstClear;
            public List<CampMissionOfferSave> campOffers;
        }

        bool missionsLoaded;
        int activeBriggsMissionStage = -1;
        int activeBriggsMissionCamp = -1;
        bool activeBriggsMissionReadyToTurnIn;
        int briggsMissionOfferCycle;
        readonly Dictionary<int, int[]> briggsCampOffers = new Dictionary<int, int[]>();
        readonly Dictionary<int, int> briggsCampOfferLevels = new Dictionary<int, int>();
        readonly Dictionary<int, int> briggsCampOfferCycles = new Dictionary<int, int>();
        readonly HashSet<int> briggsFirstClearPaid = new HashSet<int>();
        int[] dungeonAssignedCampCache;

        bool briggsMissionPage;
        int briggsMobileOfferPage;
        int briggsReceiptStage = -1;
        int briggsReceiptScrap;
        int briggsReceiptSkillPoints;
        bool briggsReceiptFirstClear;
        Gun briggsReceiptGun;
        ArmorItem briggsReceiptArmor;

        public bool HasActiveBriggsMission { get { EnsureMissionsLoaded(); return activeBriggsMissionStage >= 0; } }
        public int ActiveBriggsMissionStage { get { EnsureMissionsLoaded(); return activeBriggsMissionStage; } }
        public int ActiveBriggsMissionCamp { get { EnsureMissionsLoaded(); return activeBriggsMissionCamp; } }
        public bool ActiveBriggsMissionReadyToTurnIn { get { EnsureMissionsLoaded(); return activeBriggsMissionReadyToTurnIn; } }
        public bool HasFirstClearRewardPaid(int stageId) { EnsureMissionsLoaded(); return briggsFirstClearPaid.Contains(stageId); }

        public int AssignedCampForDungeon(int stageId)
        {
            stageId = Mathf.Clamp(stageId, 0, ExpeditionMap.StageCount - 1);
            if (dungeonAssignedCampCache == null || dungeonAssignedCampCache.Length != ExpeditionMap.StageCount)
            {
                dungeonAssignedCampCache = new int[ExpeditionMap.StageCount];
                var camps = WorldGeography.Camps;
                var entrances = WorldGeography.Entrances;
                for (int s = 0; s < ExpeditionMap.StageCount; s++)
                {
                    Vector3 e = entrances[s];
                    int bestCamp = 0;
                    float bestDistSq = float.MaxValue;
                    for (int c = 0; c < camps.Length; c++)
                    {
                        float dx = e.x - camps[c].x;
                        float dz = e.z - camps[c].z;
                        float dSq = dx * dx + dz * dz;
                        if (dSq < bestDistSq)
                        {
                            bestDistSq = dSq;
                            bestCamp = c;
                        }
                    }
                    dungeonAssignedCampCache[s] = bestCamp;
                }
            }
            return dungeonAssignedCampCache[stageId];
        }

        void ResetMissionRuntimeState()
        {
            missionsLoaded = false;
            activeBriggsMissionStage = -1;
            activeBriggsMissionCamp = -1;
            activeBriggsMissionReadyToTurnIn = false;
            briggsMissionOfferCycle = 0;
            briggsCampOffers.Clear();
            briggsCampOfferLevels.Clear();
            briggsCampOfferCycles.Clear();
            briggsFirstClearPaid.Clear();
            briggsMissionPage = false;
            briggsMobileOfferPage = 0;
            ClearBriggsReceipt();
        }

        void ClearBriggsReceipt()
        {
            briggsReceiptStage = -1;
            briggsReceiptScrap = 0;
            briggsReceiptSkillPoints = 0;
            briggsReceiptFirstClear = false;
            briggsReceiptGun = null;
            briggsReceiptArmor = null;
        }

        void EnsureMissionsLoaded()
        {
            if (missionsLoaded) return;
            missionsLoaded = true;
            activeBriggsMissionStage = -1;
            activeBriggsMissionCamp = -1;
            activeBriggsMissionReadyToTurnIn = false;
            briggsMissionOfferCycle = 0;
            briggsCampOffers.Clear();
            briggsCampOfferLevels.Clear();
            briggsCampOfferCycles.Clear();
            briggsFirstClearPaid.Clear();

            if (!smoke && PlayerPrefs.HasKey(BriggsMissionSaveKey))
            {
                try
                {
                    var save = JsonUtility.FromJson<BriggsMissionSave>(PlayerPrefs.GetString(BriggsMissionSaveKey));
                    if (save != null)
                    {
                        activeBriggsMissionStage = save.activeStage >= 0 && save.activeStage < ExpeditionMap.StageCount ? save.activeStage : -1;
                        activeBriggsMissionCamp = activeBriggsMissionStage >= 0 ? Mathf.Clamp(save.activeCamp, 0, WorldGeography.Camps.Length - 1) : -1;
                        activeBriggsMissionReadyToTurnIn = activeBriggsMissionStage >= 0 && save.readyToTurnIn;
                        briggsMissionOfferCycle = Mathf.Max(0, save.offerCycle);
                        if (save.paidFirstClear != null)
                        {
                            foreach (int id in save.paidFirstClear)
                                if (id >= 0 && id < ExpeditionMap.StageCount) briggsFirstClearPaid.Add(id);
                        }
                        if (save.campOffers != null)
                        {
                            foreach (var entry in save.campOffers)
                            {
                                if (entry == null || entry.stages == null) continue;
                                int camp = Mathf.Clamp(entry.camp, 0, WorldGeography.Camps.Length - 1);
                                briggsCampOffers[camp] = entry.stages.ToArray();
                                briggsCampOfferLevels[camp] = entry.level;
                                briggsCampOfferCycles[camp] = entry.cycle;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Mission state load failed: " + e.GetType().Name);
                }
            }
            // Legacy migration from pre-mission saves runs explicitly in InitializeMissionsForSave().
        }

        internal void InitializeMissionsForSave(bool existingProgressionSave)
        {
            EnsureMissionsLoaded();
            if (smoke) return;
            if (!PlayerPrefs.HasKey(BriggsMissionSaveKey))
            {
                if (existingProgressionSave && clearedExpeditionStages.Count > 0)
                {
                    foreach (int id in clearedExpeditionStages)
                        if (id >= 0 && id < ExpeditionMap.StageCount) briggsFirstClearPaid.Add(id);
                }
                SaveMissions();
            }
        }

        void SaveMissions()
        {
            if (smoke) return;
            EnsureMissionsLoaded();
            var save = new BriggsMissionSave
            {
                version = 1,
                activeStage = activeBriggsMissionStage,
                activeCamp = activeBriggsMissionCamp,
                readyToTurnIn = activeBriggsMissionReadyToTurnIn,
                offerCycle = briggsMissionOfferCycle,
                paidFirstClear = new List<int>(briggsFirstClearPaid),
                campOffers = new List<CampMissionOfferSave>()
            };
            foreach (var kv in briggsCampOffers)
            {
                if (kv.Value == null) continue;
                save.campOffers.Add(new CampMissionOfferSave
                {
                    camp = kv.Key,
                    level = briggsCampOfferLevels.TryGetValue(kv.Key, out int lvl) ? lvl : 1,
                    cycle = briggsCampOfferCycles.TryGetValue(kv.Key, out int cyc) ? cyc : 0,
                    stages = new List<int>(kv.Value)
                });
            }
            PlayerPrefs.SetString(BriggsMissionSaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        bool CanAccessMissionStage(int stageId)
        {
            if (stageId < 0 || stageId >= ExpeditionMap.StageCount) return false;
            if (!FactionHeadquartersUnlocked(stageId)) return false;
            var stage = ExpeditionMap.Get(stageId, DebtLevel);
            if (!smoke && stage.level > DebtLevel) return false;
            if (FreeVersionBlocksProgress && !FreeVersionAllowsDungeonLevel(stage.level)) return false;
            return true;
        }

        public List<int> EligibleMissionsForCamp(int campIndex, int maxLevel)
        {
            var eligible = new List<int>();
            campIndex = Mathf.Clamp(campIndex, 0, WorldGeography.Camps.Length - 1);
            for (int id = 0; id < ExpeditionMap.StageCount; id++)
            {
                if (AssignedCampForDungeon(id) != campIndex) continue;
                if (!FactionHeadquartersUnlocked(id)) continue;
                var stage = ExpeditionMap.Get(id, maxLevel);
                if (stage.level > maxLevel) continue;
                if (FreeVersionBlocksProgress && !FreeVersionAllowsDungeonLevel(stage.level)) continue;
                eligible.Add(id);
            }
            return eligible;
        }

        public int[] GetCampMissionOffers(int campIndex, bool forceReroll = false)
        {
            EnsureMissionsLoaded();
            campIndex = Mathf.Clamp(campIndex, 0, WorldGeography.Camps.Length - 1);
            bool hasOffers = briggsCampOffers.TryGetValue(campIndex, out int[] cached) && cached != null;
            bool levelMatches = briggsCampOfferLevels.TryGetValue(campIndex, out int cachedLevel) && cachedLevel == DebtLevel;
            bool cycleMatches = briggsCampOfferCycles.TryGetValue(campIndex, out int cachedCycle) && cachedCycle == briggsMissionOfferCycle;

            if (!forceReroll && hasOffers && levelMatches && cycleMatches)
            {
                bool allValid = true;
                for (int i = 0; i < cached.Length; i++)
                {
                    if (!CanAccessMissionStage(cached[i]) || AssignedCampForDungeon(cached[i]) != campIndex)
                    {
                        allValid = false;
                        break;
                    }
                }
                if (allValid) return cached;
            }

            List<int> eligible = EligibleMissionsForCamp(campIndex, DebtLevel);
            var picked = new List<int>(3);
            if (campIndex == 0 && FirstStepsStage <= 3 && eligible.Contains(0) && !clearedExpeditionStages.Contains(0))
            {
                picked.Add(0);
                eligible.Remove(0);
            }

            int seed = unchecked((campIndex + 1) * 73856093 ^ (DebtLevel + 3) * 19349663 ^ (briggsMissionOfferCycle + 7) * 83492791 ^ (forceReroll ? Environment.TickCount : 0));
            var r = new System.Random(seed);
            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                int tmp = eligible[i];
                eligible[i] = eligible[j];
                eligible[j] = tmp;
            }
            for (int i = 0; i < eligible.Count && picked.Count < 3; i++)
                picked.Add(eligible[i]);

            int[] result = picked.ToArray();
            briggsCampOffers[campIndex] = result;
            briggsCampOfferLevels[campIndex] = DebtLevel;
            briggsCampOfferCycles[campIndex] = briggsMissionOfferCycle;
            SaveMissions();
            return result;
        }

        public bool AcceptBriggsMission(int stageId)
        {
            EnsureMissionsLoaded();
            if (HasActiveBriggsMission)
            {
                Notify("CONTRACT LIMIT / Complete or abandon your current contract first.", "受注上限 / 先に現在のミッションを完了または破棄してください");
                return false;
            }
            if (AssignedCampForDungeon(stageId) != LastVisitedCamp)
            {
                Notify("CONTRACT UNAVAILABLE / This contract belongs to another camp.", "受注不可 / 別拠点の管轄ミッションです");
                return false;
            }
            if (!CanAccessMissionStage(stageId))
            {
                var info = ExpeditionMap.Get(stageId, DebtLevel);
                Notify("CONTRACT LOCKED / Requires level " + info.level, "受注不可 / 必要レベル " + info.level);
                return false;
            }

            activeBriggsMissionStage = stageId;
            activeBriggsMissionCamp = LastVisitedCamp;
            activeBriggsMissionReadyToTurnIn = false;
            DiscoverOverworldDungeon(stageId);
            SetOverworldWaypoint(2, stageId);
            SaveMissions();

            var stage = ExpeditionMap.Get(stageId, DebtLevel);
            Notify("CONTRACT ACCEPTED / Destination set: " + stage.Name, "ミッション受注 / 目的地を設定: " + stage.Name);
            return true;
        }

        public bool AbandonBriggsMission()
        {
            EnsureMissionsLoaded();
            if (!HasActiveBriggsMission || activeBriggsMissionReadyToTurnIn) return false;
            if (OverworldWaypointKind == 2 && OverworldWaypointId == activeBriggsMissionStage)
                SetOverworldWaypoint(0, -1);
            activeBriggsMissionStage = -1;
            activeBriggsMissionCamp = -1;
            activeBriggsMissionReadyToTurnIn = false;
            briggsMissionOfferCycle++;
            SaveMissions();
            Notify("CONTRACT ABANDONED", "受注中のミッションを破棄しました");
            return true;
        }

        void OnBriggsMissionDungeonCompleted(int stageId)
        {
            EnsureMissionsLoaded();
            if (!HasActiveBriggsMission || activeBriggsMissionStage != stageId) return;
            activeBriggsMissionReadyToTurnIn = true;
            SetOverworldWaypoint(1, activeBriggsMissionCamp);
            SaveMissions();
            Notify(
                "CONTRACT COMPLETE / Return to Briggs at " + OverworldCampName(activeBriggsMissionCamp, false),
                "ミッション達成 / " + OverworldCampName(activeBriggsMissionCamp, true) + "のブリッグスへ報告しよう");
        }

        public bool ClaimCompletedBriggsMissionReward()
        {
            EnsureMissionsLoaded();
            if (!HasActiveBriggsMission || !activeBriggsMissionReadyToTurnIn) return false;

            int stageId = activeBriggsMissionStage;
            int returnCamp = activeBriggsMissionCamp;
            var stage = ExpeditionMap.Get(stageId, DebtLevel);

            ClearBriggsReceipt();
            completedRewardGun = null;
            completedRewardArmor = null;

            bool firstClear = !briggsFirstClearPaid.Contains(stageId);
            int baseScrap = ScaleScrap(new[] { 5000000, 10000000, 20000000 }[Mathf.Clamp(stage.legacyDungeonIndex, 0, 2)]);
            int creditsBefore = Credits;
            int tokensBefore = ExpeditionTokens;

            AwardCredits(baseScrap);
            if (firstClear)
            {
                briggsFirstClearPaid.Add(stageId);
                GrantFirstClearPayout(stageId);
                int missionBonus = MissionRewardAmount(stageId);
                AwardCredits(missionBonus);
            }
            else
            {
                GrantRepeatExpeditionReward(stageId);
            }

            briggsReceiptStage = stageId;
            briggsReceiptFirstClear = firstClear;
            briggsReceiptScrap = Mathf.Max(0, Credits - creditsBefore);
            briggsReceiptSkillPoints = Mathf.Max(0, ExpeditionTokens - tokensBefore);
            briggsReceiptGun = completedRewardGun;
            briggsReceiptArmor = completedRewardArmor;

            completedScrapReward = briggsReceiptScrap;
            completedSkillPointReward = briggsReceiptSkillPoints;

            if (OverworldWaypointKind == 1 && OverworldWaypointId == returnCamp)
                SetOverworldWaypoint(0, -1);

            activeBriggsMissionStage = -1;
            activeBriggsMissionCamp = -1;
            activeBriggsMissionReadyToTurnIn = false;
            briggsMissionOfferCycle++;

            PlayUserSound("transaction", .35f);
            SaveProgression();
            SaveInventory();
            SaveMissions();

            string payout = T("CONTRACT PAYOUT / SCRAP +", "報酬受取 / スクラップ +") + briggsReceiptScrap.ToString("N0");
            if (briggsReceiptSkillPoints > 0) payout += T(" + SKILL PT", " + スキルpt");
            Notify(payout, payout);
            return true;
        }

        string BriggsMissionRewardSummary(int stageId)
        {
            EnsureMissionsLoaded();
            var stage = ExpeditionMap.Get(stageId, DebtLevel);
            int baseScrap = ScaleScrap(new[] { 5000000, 10000000, 20000000 }[Mathf.Clamp(stage.legacyDungeonIndex, 0, 2)]);
            if (!briggsFirstClearPaid.Contains(stageId))
            {
                int missionBonus = MissionRewardAmount(stageId);
                var r = new System.Random(ExpeditionMap.RewardSeed(stageId));
                int kind = r.Next(4);
                long totalScrap = (long)baseScrap + missionBonus;
                if (kind == 0)
                {
                    totalScrap += ScaleScrap(25000000 + r.Next(250001) * 100);
                    return T("SCRAP +", "スクラップ +") + totalScrap.ToString("N0");
                }
                string bonus = kind == 1
                    ? T("WEAPON CACHE", "銃の報酬箱")
                    : kind == 2
                        ? T("ARMOR CACHE", "防具の報酬箱")
                        : T("SKILL POINT +1", "スキルポイント +1");
                return T("SCRAP +", "スクラップ +") + totalScrap.ToString("N0") + " + " + bonus;
            }
            return T("SCRAP +", "スクラップ +") + baseScrap.ToString("N0") + T(" + REPEAT SCRAP OR GEAR", " + 追加スクラップか装備");
        }

        string BriggsReceiptSummaryLine()
        {
            var parts = new List<string>();
            if (briggsReceiptScrap > 0)
                parts.Add(T("SCRAP +", "スクラップ +") + briggsReceiptScrap.ToString("N0"));
            if (briggsReceiptSkillPoints > 0)
                parts.Add(T("SKILL POINT +", "スキルポイント +") + briggsReceiptSkillPoints);
            if (briggsReceiptGun != null)
                parts.Add(VaultLanguage.GunName(briggsReceiptGun));
            else if (briggsReceiptArmor != null)
                parts.Add(Armory.Name(briggsReceiptArmor));
            return parts.Count > 0 ? string.Join("   ·   ", parts) : T("PAID", "支払完了");
        }

        void DrawBriggsMissions()
        {
            EnsureMissionsLoaded();
            Box(new Rect(0, 0, 1440, 900), PanelInk);
            Text(65, 48, T("BRIGGS / CONTRACTS", "ブリッグス / 仕事斡旋") + " — " + OverworldCampName(LastVisitedCamp, VaultLanguage.Japanese), heading, Gold, 1020);
            Text(68, 106,
                T("WALLET ", "所持金 ") + Credits.ToString("N0") + T(" SCRAP", " スクラップ")
                + "   ·   " + T("YOUR LEVEL ", "現在レベル ") + DebtLevel
                + "   ·   " + T("MAX 1 ACTIVE CONTRACT", "同時に受注できるミッションは1つのみ"),
                small, Cyan, 1280);

            if (Button(new Rect(1110, 48, 265, 48), T("REPAY DEBT", "借金返済へ")))
            {
                briggsMissionPage = false;
                return;
            }

            float contentTop = 152f;
            if (HasActiveBriggsMission)
            {
                var activeStage = ExpeditionMap.Get(activeBriggsMissionStage, DebtLevel);
                Panel(new Rect(65, contentTop, 1310, 88), activeBriggsMissionReadyToTurnIn ? Gold : Cyan);
                string statusText = activeBriggsMissionReadyToTurnIn
                    ? T("COMPLETED — READY TO CLAIM PAYOUT", "達成済み — 報酬を受け取れます")
                    : T("ACTIVE CONTRACT — WAYPOINT SET TO SITE", "受注中 — ダンジョンを目的地に設定済み");
                Text(84, contentTop + 10,
                    statusText + "   (" + OverworldCampName(activeBriggsMissionCamp, VaultLanguage.Japanese) + ")",
                    small, activeBriggsMissionReadyToTurnIn ? Gold : Cyan, 820);
                Text(84, contentTop + 42,
                    (activeStage.id + 1).ToString("00") + " / " + activeStage.Name + "  (Lv." + activeStage.level + ")  ·  " + ExpeditionMap.MissionLabel(activeStage, VaultLanguage.Japanese),
                    label, UiPaper, 880);
                if (activeBriggsMissionReadyToTurnIn)
                {
                    if (Button(new Rect(995, contentTop + 18, 360, 52), T("CLAIM REWARD", "報酬を受け取る"), true))
                        ClaimCompletedBriggsMissionReward();
                }
                else
                {
                    if (Button(new Rect(1065, contentTop + 18, 290, 52), T("ABANDON CONTRACT", "ミッション破棄")))
                        AbandonBriggsMission();
                }
                contentTop = 256f;
            }
            else if (briggsReceiptStage >= 0)
            {
                var doneStage = ExpeditionMap.Get(briggsReceiptStage, DebtLevel);
                Panel(new Rect(65, contentTop, 1310, 80), Gold);
                Text(84, contentTop + 10, T("LAST PAYOUT RECEIVED: ", "報酬受領済み: ") + doneStage.Name, small, Gold, 1240);
                Text(84, contentTop + 40, BriggsReceiptSummaryLine(), label, Color.white, 1240);
                contentTop = 248f;
            }

            int[] offers = GetCampMissionOffers(LastVisitedCamp);
            if (offers.Length == 0)
            {
                Panel(new Rect(65, contentTop + 20, 1310, 170), Mist);
                Text(95, contentTop + 55,
                    T("No contracts at or below level " + DebtLevel + " are available around this camp.\nRepay debt at Briggs's desk to raise your level or check another camp.",
                      "この拠点周辺には現在レベル " + DebtLevel + " 以下で受注できるダンジョンがありません。\n返済窓口でレベルを上げるか、別の拠点を確認してください。"),
                    new GUIStyle(label) { wordWrap = true }, Mist, 1250);
            }
            else
            {
                float cardH = HasActiveBriggsMission || briggsReceiptStage >= 0 ? 158f : 186f;
                float gap = 14f;
                for (int i = 0; i < offers.Length; i++)
                {
                    int stageId = offers[i];
                    var stage = ExpeditionMap.Get(stageId, DebtLevel);
                    float y = contentTop + i * (cardH + gap);
                    bool isActiveThis = HasActiveBriggsMission && activeBriggsMissionStage == stageId;
                    Panel(new Rect(65, y, 1310, cardH), isActiveThis ? Gold : Cyan);
                    DrawDungeonMapArt(new Rect(86, y + cardH * .5f - 28f, 56, 56), stageId, isActiveThis ? Gold : UiPaper);
                    Text(162, y + 12,
                        (stage.id + 1).ToString("00") + " / " + stage.Name + "   ·   " + T("LEVEL ", "レベル ") + stage.level,
                        label, Gold, 810);
                    Text(162, y + 48,
                        stage.Location + "   ·   " + ExpeditionMap.MissionLabel(stage, VaultLanguage.Japanese),
                        new GUIStyle(small) { wordWrap = true }, UiPaper, 810);
                    Text(162, y + cardH - 42,
                        T("REWARD: ", "報酬: ") + BriggsMissionRewardSummary(stageId),
                        small, Cyan, 810);

                    bool canAccept = !HasActiveBriggsMission;
                    Rect btnRect = new Rect(1010, y + cardH * .5f - 28f, 340, 56);
                    bool prevEnabled = GUI.enabled;
                    GUI.enabled = prevEnabled && canAccept;
                    string btnText = isActiveThis
                        ? T("ACTIVE", "受注中")
                        : HasActiveBriggsMission
                            ? T("SLOT OCCUPIED", "他ミッション中")
                            : T("ACCEPT CONTRACT", "この仕事を受ける");
                    if (Button(btnRect, btnText, canAccept) && canAccept)
                    {
                        if (AcceptBriggsMission(stageId))
                        {
                            GUI.enabled = prevEnabled;
                            briggsMissionPage = false;
                            debtPage = false;
                            screen = Screen.Hub;
                            return;
                        }
                    }
                    GUI.enabled = prevEnabled;
                }
            }

            if (Button(new Rect(68, 796, 310, 52), T("RETURN TO SAFEHOUSE", "拠点へ戻る")))
            {
                briggsMissionPage = false;
                debtPage = false;
                screen = Screen.Hub;
            }
        }

        void DrawMobileBriggsMissions()
        {
            EnsureMissionsLoaded();
            Box(new Rect(0, 0, 1440, 900), PanelInk);
            MobileCopyAt(40, 24, T("CONTRACTS / ", "仕事斡旋 / ") + OverworldCampName(LastVisitedCamp, VaultLanguage.Japanese), Gold, 1020);
            if (MobileButton(new Rect(1110, 10, 290, 105), T("BACK", "戻る")))
            {
                briggsMissionPage = false;
                debtPage = false;
                screen = Screen.Hub;
                return;
            }
            MobileCopyAt(40, 118, T("LEVEL ", "レベル ") + DebtLevel + "  ·  " + T("WALLET ", "所持金 ") + Credits.ToString("N0"), Cyan, 920);
            if (MobileButton(new Rect(980, 122, 420, 92), T("REPAY DEBT", "借金返済へ")))
            {
                briggsMissionPage = false;
                return;
            }

            float listTop = 228f;
            if (HasActiveBriggsMission)
            {
                var activeStage = ExpeditionMap.Get(activeBriggsMissionStage, DebtLevel);
                Panel(new Rect(40, 224, 1360, 126), activeBriggsMissionReadyToTurnIn ? Gold : Cyan);
                MobileCopyAt(58, 236,
                    (activeBriggsMissionReadyToTurnIn ? T("DONE: ", "達成: ") : T("ACTIVE: ", "受注中: "))
                    + activeStage.Name + " (Lv." + activeStage.level + ")",
                    activeBriggsMissionReadyToTurnIn ? Gold : Cyan, 910);
                if (activeBriggsMissionReadyToTurnIn)
                {
                    if (MobileButton(new Rect(1000, 240, 380, 94), T("CLAIM", "報酬受取")))
                        ClaimCompletedBriggsMissionReward();
                }
                else
                {
                    if (MobileButton(new Rect(1000, 240, 380, 94), T("ABANDON", "破棄")))
                        AbandonBriggsMission();
                }
                listTop = 366f;
            }

            int[] offers = GetCampMissionOffers(LastVisitedCamp);
            if (offers.Length == 0)
            {
                Panel(new Rect(40, listTop + 10, 1360, 210), Mist);
                MobileCopyAt(68, listTop + 42,
                    T("No contracts at or below Lv." + DebtLevel + " near this camp.", "この拠点周辺には現在レベル" + DebtLevel + "以下の仕事がありません。"),
                    Mist, 1280);
                return;
            }

            int pageSize = 2;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(offers.Length / (float)pageSize));
            briggsMobileOfferPage = Mathf.Clamp(briggsMobileOfferPage, 0, pageCount - 1);
            float cardH = HasActiveBriggsMission ? 196f : 228f;
            float gap = 12f;
            var mobileDetailStyle = new GUIStyle(label) { fontSize = 26, wordWrap = true, clipping = TextClipping.Clip };
            for (int r = 0; r < pageSize; r++)
            {
                int idx = briggsMobileOfferPage * pageSize + r;
                if (idx >= offers.Length) break;
                int stageId = offers[idx];
                var stage = ExpeditionMap.Get(stageId, DebtLevel);
                float y = listTop + r * (cardH + gap);
                bool activeThis = HasActiveBriggsMission && activeBriggsMissionStage == stageId;
                Panel(new Rect(40, y, 1360, cardH), activeThis ? Gold : Cyan);
                MobileCopyAt(60, y + 10, (stage.id + 1).ToString("00") + " / " + stage.Name + "  Lv." + stage.level, Gold, 900);
                Text(60, y + 54, ExpeditionMap.MissionLabel(stage, VaultLanguage.Japanese), mobileDetailStyle, UiPaper, 900);
                Text(60, y + 106, T("REWARD: ", "報酬: ") + BriggsMissionRewardSummary(stageId), mobileDetailStyle, Cyan, 900);
                bool canAccept = !HasActiveBriggsMission;
                bool prev = GUI.enabled;
                GUI.enabled = prev && canAccept;
                if (MobileButton(new Rect(990, y + cardH * .5f - 52f, 385, 104), activeThis ? T("ACTIVE", "受注中") : HasActiveBriggsMission ? T("BUSY", "受注不可") : T("ACCEPT", "受注")) && canAccept)
                {
                    if (AcceptBriggsMission(stageId))
                    {
                        GUI.enabled = prev;
                        briggsMissionPage = false;
                        debtPage = false;
                        screen = Screen.Hub;
                        return;
                    }
                }
                GUI.enabled = prev;
            }

            if (pageCount > 1)
            {
                if (MobileButton(new Rect(40, 780, 360, 105), T("PREV", "前へ")))
                    briggsMobileOfferPage = Mathf.Max(0, briggsMobileOfferPage - 1);
                if (MobileButton(new Rect(1040, 780, 360, 105), T("NEXT", "次へ")))
                    briggsMobileOfferPage = Mathf.Min(pageCount - 1, briggsMobileOfferPage + 1);
            }
        }
    }
}
