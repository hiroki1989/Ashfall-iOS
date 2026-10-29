using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public sealed class OverworldActivitySite
    {
        public int id, type;
        public Vector3 position;
        public bool completed;
    }

    public partial class VaultGame
    {
        readonly List<OverworldActivitySite> nearbyOverworldActivities = new List<OverworldActivitySite>();
        public IReadOnlyList<OverworldActivitySite> NearbyOverworldActivitySites => nearbyOverworldActivities;
        OverworldActivitySite selectedOverworldActivity;
        public bool OverworldActivityChoiceOpen => selectedOverworldActivity != null && IsOverworld;
        public int OverworldActivityChoiceCount => 2;
        public int NearbyOverworldActivity
        {
            get
            {
                if (!IsOverworld || player == null) return -1;
                float best = 3.5f * 3.5f; int type = -1;
                foreach (var site in nearbyOverworldActivities)
                {
                    float d = (site.position - PlayerPosition).sqrMagnitude;
                    if (!site.completed && d < best) { best = d; type = site.type; }
                }
                return type;
            }
        }
        public string OverworldActivityTitle
        {
            get
            {
                int type = selectedOverworldActivity != null ? selectedOverworldActivity.type : NearbyOverworldActivity;
                return type == 0 ? VaultLanguage.T("Abandoned salvage", "廃車・店舗の物資") :
                    type == 1 ? VaultLanguage.T("Distress transmission", "無線救難信号") :
                    type == 2 ? VaultLanguage.T("Hidden supply clue", "隠し物資の手掛かり") :
                    type == 3 ? VaultLanguage.T("Buried supply cache", "隠された物資箱") :
                    type == 4 ? VaultLanguage.T("Hostile surge", "敵襲") :
                    type == 5 ? VaultLanguage.T("Challenge reward chest", "挑戦の報酬箱") : "";
            }
        }
        public string OverworldActivityDescription
        {
            get
            {
                int type = selectedOverworldActivity != null ? selectedOverworldActivity.type : NearbyOverworldActivity;
                return type == 0 ? VaultLanguage.T("The salvage is guarded. Clear the ambush or hold the site against attackers to unlock a rare or better equipment chest.", "物資は敵に守られている。敵を倒すか地点を守り抜くと、レア以上の装備箱が解放される。") :
                    type == 1 ? VaultLanguage.T("A survivor needs the area secured. Clear the attackers or defend the signal to unlock a rare or better equipment chest.", "生存者が周辺の安全確保を求めている。敵を倒すか信号を守ると、レア以上の装備箱が解放される。") :
                    type == 2 ? VaultLanguage.T("Follow the marks to a guarded cache, or fight here to secure the coordinates. Both routes lead to a rare or better equipment chest.", "印を辿って守られた物資箱を目指すか、この場で戦い座標を確保する。どちらもレア以上の装備箱に通じる。") :
                    type == 3 ? VaultLanguage.T("The cache is guarded. Defeat the attackers before time runs out to unlock it.", "物資箱は敵に守られている。制限時間内に敵を倒して解放しよう。") :
                    type == 4 ? VaultLanguage.T("Survive the attacking enemies for 45 seconds to unlock a rare or better equipment chest. Leaving the area ends the challenge.", "45秒間、敵の攻撃を生き延びるとレア以上の装備箱が開きます。エリアを離れると挑戦終了。") :
                    type == 5 ? VaultLanguage.T("Open the unlocked equipment chest.", "解放された装備箱を開けよう。") : "";
            }
        }
        public string OverworldActivityChoiceLabel(int choice)
        {
            int type = selectedOverworldActivity != null ? selectedOverworldActivity.type : NearbyOverworldActivity;
            if (type == 4) return choice == 0 ? VaultLanguage.T("Start / survive 45 seconds", "開始 / 45秒間生き延びる") : VaultLanguage.T("Leave", "立ち去る");
            if (type == 0 || type == 1) return choice == 0 ? VaultLanguage.T("Clear / defeat 6 enemies in 60 seconds", "殲滅 / 60秒以内に敵6体を倒す") : VaultLanguage.T("Defend / hold the site for 30 seconds", "防衛 / 地点を30秒間守る");
            if (type == 3) return choice == 0 ? VaultLanguage.T("Clear / defeat 6 enemies in 60 seconds", "殲滅 / 60秒以内に敵6体を倒す") : VaultLanguage.T("Leave", "立ち去る");
            return choice == 0 ? VaultLanguage.T("Follow marks / track the guarded cache", "印を辿る / 守られた物資箱を追跡") : VaultLanguage.T("Defend / secure the coordinates", "防衛 / 座標を確保する");
        }
        // Use a deterministic representative legendary gun at the player's level, so
        // event scrap tracks the same economy as selling real equipment.
        int ActivityScrap(int seed)
        {
            var random = new System.Random(seed);
            return SaleValue(Arsenal.Legendary(0, random, DebtLevel));
        }
        public Gun LastActivityRewardGun { get; private set; }
        public ArmorItem LastActivityRewardArmor { get; private set; }
        public ArtifactItem LastActivityRewardModule { get; private set; }
        public int LastActivityRewardScrap { get; private set; }
        public bool LastActivityRewardOnGround { get; private set; }
        float activityRewardUntil;
        void GrantActivityGear(int seed, Vector3 position)
        {
            activityRewardUntil=Time.unscaledTime+8f;
            LastActivityRewardGun=null; LastActivityRewardArmor=null; LastActivityRewardModule=null;
            LastActivityRewardScrap=0; LastActivityRewardOnGround=false;
            var random=new System.Random(seed);
            int rarityRoll=random.Next(100);
            var rarity=rarityRoll<65?Rarity.Rare:rarityRoll<90?Rarity.Epic:Rarity.Legendary;
            switch(random.Next(3))
            {
                case 0:
                    LastActivityRewardGun=Arsenal.CreateForDungeon(random, DungeonIndex, rarity, DebtLevel);
                    if(!StoreWeapon(LastActivityRewardGun)){AddDrop(LastActivityRewardGun,position);LastActivityRewardOnGround=true;}
                    break;
                case 1:
                    LastActivityRewardArmor=Armory.CreateForDungeon(random, DungeonIndex, rarity, DebtLevel);
                    if(!StoreArmor(LastActivityRewardArmor)){AddArmorDrop(LastActivityRewardArmor,position);LastActivityRewardOnGround=true;}
                    break;
                default:
                    LastActivityRewardModule=ArtifactCatalog.Create(random, DungeonIndex, rarity, DebtLevel);
                    if(!StoreArtifact(LastActivityRewardModule)){AddArtifactDrop(LastActivityRewardModule,position);LastActivityRewardOnGround=true;}
                    break;
            }
        }
        void GrantActivityScrap(int seed)
        {
            activityRewardUntil=Time.unscaledTime+8f;
            LastActivityRewardGun=null; LastActivityRewardArmor=null; LastActivityRewardModule=null;
            LastActivityRewardOnGround=false; LastActivityRewardScrap=ActivityScrap(seed);
            AwardCredits(LastActivityRewardScrap);
        }
        static int ActivityHash(int x, int z, int type)
        { unchecked { int h = (x + 50) * 73856093 ^ (z + 50) * 19349663 ^ (type + 1) * 83492791; return h & int.MaxValue; } }
        Vector3 FindOverworldClueTarget(Vector3 origin)
        {
            int baseX = Mathf.FloorToInt(origin.x / 50f) * 50 + 25;
            int baseZ = Mathf.FloorToInt(origin.z / 50f) * 50 + 25;
            int[,] offsets = { { 50, 0 }, { 0, 50 }, { -50, 0 }, { 0, -50 }, { 50, 50 }, { -50, -50 }, { 100, 0 }, { 0, 100 } };
            int start = ActivityHash(baseX, baseZ, 3) % offsets.GetLength(0);
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int index = (start + i) % offsets.GetLength(0);
                var p = new Vector3(baseX + offsets[index, 0], .65f, baseZ + offsets[index, 1]);
                bool sameRiverBank = (origin.x - WorldGeography.RiverX(origin.z)) * (p.x - WorldGeography.RiverX(p.z)) > 0;
                if (sameRiverBank && (p - origin).sqrMagnitude >= 35f * 35f && !WorldGeography.Blocked(p, .65f)) return p;
            }
            return Vector3.zero;
        }
        void RefreshOverworldActivities()
        {
            nearbyOverworldActivities.Clear();
            if (!IsOverworld || player == null) return;
            int cx = Mathf.FloorToInt(PlayerPosition.x / 100f), cz = Mathf.FloorToInt(PlayerPosition.z / 100f);
            for (int x = cx - 1; x <= cx + 1; x++) for (int z = cz - 1; z <= cz + 1; z++)
            {
                if (x < -50 || x >= 50 || z < -50 || z >= 50) continue;
                // One stable activity per 300 x 300 m district cell.
                if ((x + 51) % 3 != 0 || (z + 51) % 3 != 0) continue;
                var chunk = WorldGeography.GetChunk(x, z);
                int type = ActivityHash(x, z, 7) % 3;
                    int id = ((x + 50) * 100 + (z + 50)) * 3 + type;
                    int hash = ActivityHash(x, z, type);
                    Vector3 p = Vector3.zero;
                    if (type == 0)
                    {
                        if (chunk.Cars.Count == 0) continue;
                        var car = chunk.Cars[hash % chunk.Cars.Count];
                        p = car + new Vector3(2.8f, .65f, 0);
                    }
                    else if (type == 1)
                    {
                        float atX = x * 100 + 25, atZ = z * 100 + 25;
                        p = new Vector3(WorldGeography.StreetX(x, atZ), .65f, atZ);
                    }
                    else
                    {
                        if (chunk.Buildings.Count == 0) continue;
                        var building = chunk.Buildings[hash % chunk.Buildings.Count];
                        p = new Vector3(building.xMax + 2.3f, .65f, building.center.y);
                    }
                    if (WorldGeography.Blocked(p, .55f)) continue;
                    if (ActivityHash(x, z, 11) % 5 == 0) type = 4;
                    if (overworldRushSite != null && overworldRushSite.id == id) continue;
                    nearbyOverworldActivities.Add(new OverworldActivitySite { id = id, type = type, position = p, completed = IsOverworldActivityCompleted(id) });
            }
            if (overworldRushRewardReady && overworldRushSite != null)
                nearbyOverworldActivities.Add(new OverworldActivitySite { id = overworldRushSite.id, type = 5, position = overworldRushSite.position });
            AddFactionCampRewards();
            // Keep the tracked destination on the atlas after fast travel or restart.
            if (HasActiveOverworldClue && (overworldRushSite == null || overworldRushSite.id != 30000 + ActiveOverworldClueId))
                nearbyOverworldActivities.Add(new OverworldActivitySite { id = 30000 + ActiveOverworldClueId, type = 3, position = ActiveOverworldCluePosition });
        }
        public bool OpenNearbyOverworldActivity()
        {
            if (selectedOverworldActivity != null) { CloseOverworldActivityChoice(); return true; }
            float best = 3.5f * 3.5f;
            foreach (var site in nearbyOverworldActivities)
            {
                float d = (site.position - PlayerPosition).sqrMagnitude;
                if (!site.completed && d < best) { best = d; selectedOverworldActivity = site; }
            }
            if (selectedOverworldActivity == null) return false;
            if (selectedOverworldActivity.type == 5)
            {
                var rewardSite = selectedOverworldActivity; selectedOverworldActivity = null;
                if (!CompleteOverworldActivity(rewardSite.id)) return false;
                if (rewardSite.id >= 30000 && HasActiveOverworldClue && ActiveOverworldClueId == rewardSite.id - 30000)
                    RecoverOverworldClue(ActiveOverworldClueId);
                var visuals=world?world.GetComponent<OverworldActivityVisuals>():null;if(visuals)visuals.OpenReward(rewardSite.id);
                GrantActivityGear(rewardSite.id, rewardSite.position + Vector3.right * 2);
                if (!IsFactionCampReward(rewardSite.id)) { overworldRushRewardReady = false; overworldRushSite = null; }
                SaveInventory();
                Notify("CHALLENGE CHEST / equipment recovered", "挑戦の報酬箱 / 装備を回収"); RefreshOverworldActivities();
            }
            else Time.timeScale = 0;
            return true;
        }
        void CloseOverworldActivityChoice()
        { selectedOverworldActivity = null; if (screen == Screen.Playing) Time.timeScale = 1; }
        public void ChooseOverworldActivity(int choice)
        {
            var site = selectedOverworldActivity;
            if (!IsOverworld || site == null || choice < 0 || choice >= 2) return;
            if ((site.position - PlayerPosition).sqrMagnitude > 5f * 5f) { CloseOverworldActivityChoice(); return; }
            if (site.type == 4)
            {
                CloseOverworldActivityChoice();
                if (choice == 0) StartOverworldRush(site);
                return;
            }
            if (site.type == 3)
            {
                CloseOverworldActivityChoice();
                if (choice == 0) StartOverworldChallenge(site, 1);
                return;
            }
            if (site.type == 2 && choice == 1)
            {
                CloseOverworldActivityChoice();
                StartOverworldChallenge(site, 2);
                return;
            }
            if (site.type == 0 || site.type == 1)
            {
                CloseOverworldActivityChoice();
                StartOverworldChallenge(site, choice == 0 ? 1 : 2);
                return;
            }
            if (site.type == 1 && choice == 0 && gun != null)
            {
                var ammoType = AmmoForGun(gun); int cost = Mathf.Min(AmmoPackSize(ammoType), AmmoReserve(ammoType));
                if (cost <= 0) { Notify("No spare ammo to send", "送れる予備弾薬がありません"); return; }
                ammoReserves[(int)ammoType] -= cost;
            }
            Vector3 clueTarget = Vector3.zero;
            if (site.type == 2 && choice == 0)
            {
                if (HasActiveOverworldClue) { Notify("Finish tracking the current cache first", "追跡中の物資を先に回収してください"); return; }
                clueTarget = FindOverworldClueTarget(site.position);
                if (clueTarget == Vector3.zero) { Notify("The trail is blocked here", "この場所の手掛かりは途切れています"); return; }
                if (!BeginOverworldClue(site.id, clueTarget)) { CloseOverworldActivityChoice(); return; }
            }
            else if (!CompleteOverworldActivity(site.id)) { CloseOverworldActivityChoice(); return; }
            site.completed = true; CloseOverworldActivityChoice();
            int roll = ActivityHash(Mathf.FloorToInt(site.position.x), Mathf.FloorToInt(site.position.z), choice);
            if (site.type == 0)
            {
                if (choice == 0) { var type = ActiveAmmoTypeAt(roll % ActiveAmmoTypeCount); AddAmmo(type, AmmoPackSize(type)); GrantActivityGear(roll, PlayerPosition + Vector3.right * 2); Notify("SALVAGE / ammo and gear recovered", "物資回収 / 弾薬と装備を入手"); }
                else { if (roll % 4 == 0) hp = Mathf.Max(1, hp - maxHp * .12f); GrantActivityGear(roll, PlayerPosition + Vector3.right * 2); Notify("LOCKED CASE / gear recovered", "鍵箱 / 装備を回収"); }
            }
            else if (site.type == 1)
            {
                if (choice == 0) { GrantActivityGear(roll, PlayerPosition + Vector3.right * 2); Notify("RESCUE / survivor sent a reward", "救難 / 生存者から謝礼が届いた"); }
                else { hp = Mathf.Min(maxHp, hp + maxHp * .22f); AddHealthPackDrop(site.position + Vector3.right * 2); GrantActivityGear(roll, PlayerPosition + Vector3.right * 2); Notify("SIGNAL TRACED / medical supplies and gear", "無線追跡 / 医療物資と装備を入手"); }
            }
            else
            {
                if (choice == 0) { Notify("CLUE FOUND / follow the map marker to the cache", "手掛かり発見 / 地図の印を辿って物資を回収"); RefreshOverworldActivities(); }
                else { GrantActivityScrap(roll); Notify("COORDINATES SOLD / scrap received", "座標売却 / スクラップを入手"); }
            }
            SaveInventory();
        }
    }
}
