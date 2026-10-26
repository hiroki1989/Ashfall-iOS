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
                    type == 3 ? VaultLanguage.T("Buried supply cache", "隠された物資箱") : "";
            }
        }
        public string OverworldActivityDescription
        {
            get
            {
                int type = selectedOverworldActivity != null ? selectedOverworldActivity.type : NearbyOverworldActivity;
                return type == 0 ? VaultLanguage.T("A wreck and a shuttered shop still hold usable supplies. Search carefully or break open the locked case.", "廃車と閉店した店に物資が残る。慎重に探すか、鍵箱をこじ開けるか。") :
                    type == 1 ? VaultLanguage.T("A survivor calls from a fading radio. Answer and send supplies, or trace the signal to its emergency cache.", "弱い無線から生存者の声。物資を送るか、発信源の緊急備蓄を探るか。") :
                    type == 2 ? VaultLanguage.T("Marks on the pavement point to another location. Follow them to recover a cache, or sell the coordinates.", "路面の印は別の場所を示す。そこへ向かって物資を回収するか、座標を売るか。") :
                    type == 3 ? VaultLanguage.T("The trail ends here. Open the buried cache.", "手掛かりの終点。埋められた物資を開けよう。") : "";
            }
        }
        public string OverworldActivityChoiceLabel(int choice)
        {
            int type = selectedOverworldActivity != null ? selectedOverworldActivity.type : NearbyOverworldActivity;
            if (type == 0) return choice == 0 ? VaultLanguage.T("Search / ammo and scrap", "捜索 / 弾薬とスクラップ") : VaultLanguage.T("Force lock / more scrap, risk injury", "解錠を強行 / 大量のスクラップ、負傷の恐れ");
            if (type == 1) return choice == 0 ? VaultLanguage.T("Send aid / spend one ammo pack for a reward", "救援 / 弾薬を渡して謝礼を受け取る") : VaultLanguage.T("Trace signal / find medical stores", "追跡 / 医療物資を発見");
            return choice == 0 ? VaultLanguage.T("Follow marks / track the cache", "印を辿る / 物資の場所を追跡") : VaultLanguage.T("Sell coordinates / scrap", "座標を売る / スクラップ");
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
                    nearbyOverworldActivities.Add(new OverworldActivitySite { id = id, type = type, position = p, completed = IsOverworldActivityCompleted(id) });
            }
            // Keep the tracked destination on the atlas after fast travel or restart.
            if (HasActiveOverworldClue)
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
            if (selectedOverworldActivity.type == 3)
            {
                int clueId = selectedOverworldActivity.id - 30000;
                selectedOverworldActivity = null;
                if (!RecoverOverworldClue(clueId)) return false;
                var type = ActiveAmmoTypeAt(ActivityHash(clueId, clueId / 3, 4) % ActiveAmmoTypeCount);
                AddAmmo(type, AmmoPackSize(type) * 2); AwardCredits(ScaleScrap(17000)); SaveInventory();
                Notify("HIDDEN CACHE / supplies recovered", "隠し物資 / 弾薬とスクラップを回収");
                RefreshOverworldActivities();
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
                if (choice == 0) { var type = ActiveAmmoTypeAt(roll % ActiveAmmoTypeCount); AddAmmo(type, AmmoPackSize(type)); AwardCredits(ScaleScrap(9000)); Notify("SALVAGE / ammo and scrap recovered", "物資回収 / 弾薬とスクラップを入手"); }
                else { if (roll % 4 == 0) hp = Mathf.Max(1, hp - maxHp * .12f); AwardCredits(ScaleScrap(26000)); Notify("LOCKED CASE / scrap recovered", "鍵箱 / スクラップを回収"); }
            }
            else if (site.type == 1)
            {
                if (choice == 0) { AwardCredits(ScaleScrap(30000)); Notify("RESCUE / survivor sent a reward", "救難 / 生存者から謝礼が届いた"); }
                else { hp = Mathf.Min(maxHp, hp + maxHp * .22f); AddHealthPackDrop(site.position + Vector3.right * 2); Notify("SIGNAL TRACED / medical cache located", "無線追跡 / 医療物資を発見"); }
            }
            else
            {
                if (choice == 0) { Notify("CLUE FOUND / follow the map marker to the cache", "手掛かり発見 / 地図の印を辿って物資を回収"); RefreshOverworldActivities(); }
                else { AwardCredits(ScaleScrap(23000)); Notify("COORDINATES SOLD / scrap received", "座標売却 / スクラップを入手"); }
            }
            SaveInventory();
        }
    }
}
