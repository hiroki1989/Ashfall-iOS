using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        sealed class FieldAmmoCrate
        {
            public Vector3 position;
            public AmmoType type;
            public int amount;
            public GameObject visual;
            public bool mission;
        }

        readonly List<int> ammoCrateRoomPlan = new List<int>(12);
        readonly List<FieldAmmoCrate> fieldAmmoCrates = new List<FieldAmmoCrate>(12);
        int ammoCratePlanCount;
        public int AmmoCratesPlannedThisRun => ammoCratePlanCount;
        public int AmmoCratesNearbyCount => fieldAmmoCrates.Count;

        /// <summary>Call once after the run RNG/dungeon have been selected and before the first BuildRoom.</summary>
        public void PrepareAmmoCratesForRun()
        {
            ammoCrateRoomPlan.Clear();
            fieldAmmoCrates.Clear();
            ammoCratePlanCount=0;
            if (rng == null || CurrentDungeon.AreaCount <= 0) return;
            for (int i = 0; i < CurrentDungeon.AreaCount; i++) ammoCrateRoomPlan.Add(i);
            ammoCratePlanCount=CurrentDungeon.AreaCount*AmmoCratesPerFloor;
        }

        /// <summary>Call for each populated combat room after its SectorLayout has been generated.</summary>
        public void SpawnAmmoCratesForCurrentRoom()
        {
            fieldAmmoCrates.RemoveAll(crate => crate == null || crate.visual == null);
            if (!ammoCrateRoomPlan.Remove(room) || Layout == null || Layout.chambers.Count == 0) return;

            for (int placed = 0; placed < AmmoCratesPerFloor; placed++)
            {
                bool found=false;
                for (int attempt = 0; attempt < 96; attempt++)
                {
                    var chamber = Layout.chambers[rng.Next(Layout.chambers.Count)];
                    if (chamber == Layout.chambers[Layout.EntryChamberIndex] || chamber == Layout.chambers[Layout.ExitChamberIndex]) continue;
                    Vector3 p = Layout.Center(chamber) + new Vector3((float)rng.NextDouble()*6f-3f,0,(float)rng.NextDouble()*5f-2.5f);
                    if (TryPlaceAmmoCrate(p)){found=true;break;}
                }
                // Random samples can repeatedly hit walls or the protected entry.
                // Scan the connected floor as a fallback before accepting fewer crates.
                if(!found)
                {
                    int cells=Layout.Columns*Layout.Rows;
                    int start=rng.Next(cells);
                    for(int n=0;n<cells;n++)
                    {
                        int index=(start+n)%cells;
                        var p=Layout.Center(new Vector2Int(index%Layout.Columns,index/Layout.Columns));
                        if(TryPlaceAmmoCrate(p)){found=true;break;}
                    }
                }
                if (!found) { Debug.LogWarning("Could not safely place all ammunition crates in room " + room); break; }
            }
            if(MissionNeedsCaches && room < CurrentDungeon.AreaCount-1)
                for(int i=0;i<2;i++)SpawnMissionSupplyCrate();
        }

        void SpawnMissionSupplyCrate()
        {
            for(int attempt=0;attempt<120;attempt++)
            {
                var chamber=Layout.chambers[rng.Next(Layout.chambers.Count)];
                if(chamber==Layout.chambers[Layout.EntryChamberIndex]||chamber==Layout.chambers[Layout.ExitChamberIndex])continue;
                Vector3 p=Layout.Center(chamber)+new Vector3((float)rng.NextDouble()*5f-2.5f,0,(float)rng.NextDouble()*5f-2.5f);
                if(Blocked(p,.8f)||Vector3.Distance(p,Layout.Entry)<7f||Vector3.Distance(p,Layout.Exit)<3f)continue;
                bool tooClose=false;foreach(var c in fieldAmmoCrates)if(Vector3.Distance(c.position,p)<2.3f){tooClose=true;break;}
                if(tooClose)continue;
                var visual=CreateLootPresentation("MISSION SUPPLIES",new Vector3(p.x,.31f,p.z),new Vector3(1.15f,.55f,.9f),new Color(.9f,.48f,.19f),LootPresentationKind.AmmoCrate);
                ObjectiveGroundRing.Create(visual.transform,p,1.15f);
                fieldAmmoCrates.Add(new FieldAmmoCrate{position=visual.transform.position,visual=visual,mission=true});
                return;
            }
            Debug.LogWarning("Could not place mission supply crate in room "+room);
        }

        bool TryPlaceAmmoCrate(Vector3 p)
        {
            if(Vector3.Distance(p,Layout.Entry)<7f||Vector3.Distance(p,Layout.Exit)<2f||Blocked(p,.75f))return false;
            foreach(var existing in fieldAmmoCrates)if(Vector3.Distance(p,existing.position)<2.3f)return false;
            var type=ActiveAmmoTypeAt(rng.Next(ActiveAmmoTypeCount));
            int amount=AmmoPackSize(type);
            var visual=CreateLootPresentation(AmmoPickupName(type,amount),new Vector3(p.x,.31f,p.z),new Vector3(.82f,.38f,.72f),AmmoCrateColor(type),LootPresentationKind.AmmoCrate,type,amount);
            fieldAmmoCrates.Add(new FieldAmmoCrate{position=visual.transform.position,type=type,amount=amount,visual=visual});
            return true;
        }

        int AmmoCratesPerFloor => Mathf.Clamp(VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.ammoCratesPerFloor : 10,0,30);

        public bool OpenNearbyAmmoCrate(bool automatic=false)
        {
            FieldAmmoCrate nearest=null;float best=2.5f;
            for(int i=0;i<fieldAmmoCrates.Count;i++)
            {
                var crate=fieldAmmoCrates[i];if(crate==null||crate.visual==null)continue;
                if(crate.mission)continue;
                if(automatic&&AmmoReserve(crate.type)>=AmmoCapacity(crate.type))continue;
                float distance=Vector3.Distance(PlayerPosition,crate.position);if(distance<best){best=distance;nearest=crate;}
            }
            if(nearest==null)return false;

            float scale=DebtAmmoPickupBonus+ArmorTotal(a=>a.ammoBonus)+EquippedWeaponAffixBonus("ammo")+ArtifactAffixBonus("ammo");
            int requested=Mathf.Max(1,Mathf.RoundToInt(nearest.amount*(1+scale))+Mathf.RoundToInt(DebtAmmoBonus(AmmoModel(nearest.type))));
            int picked=Mathf.Min(requested,AmmoCapacity(nearest.type)-AmmoReserve(nearest.type));
            if(picked<=0)
            {
                // Contract caches are inspectable even when every reserve is full
                // (new games start with full ammunition). Do not soft-lock surveys.
                if(SelectedExpeditionStage!=null&&!MissionCompleted&&
                   (ExpeditionAllowsBypass||SelectedExpeditionStage.mission==ExpeditionMission.DestroyNest))
                {
                    if(nearest.visual!=null)Destroy(nearest.visual);
                    fieldAmmoCrates.Remove(nearest);
                    Notify("CACHE INSPECTED / AMMO FULL","物資箱を調査 / 弾薬満杯");
                }
                else Notify("AMMO FULL / "+AmmoLabel(nearest.type),"弾薬満杯 / "+AmmoLabel(nearest.type));
                return true;
            }
            AddAmmo(nearest.type,picked);
            if(nearest.visual!=null)Destroy(nearest.visual);
            fieldAmmoCrates.Remove(nearest);
            string en=AmmoPickupEnglish(nearest.type,picked),ja=AmmoPickupJapanese(nearest.type,picked);
            Notify("AMMO CRATE / "+en,"弾薬箱 / "+ja);
            return true;
        }

        public bool OpenNearbyMissionSupplyCrate()
        {
            foreach(var crate in fieldAmmoCrates)
            {
                if(crate==null||!crate.mission||!crate.visual||Vector3.Distance(PlayerPosition,crate.position)>=2.5f)continue;
                fieldAmmoCrates.Remove(crate);Destroy(crate.visual);ExpeditionSupplyRecovered();
                Notify("MISSION SUPPLIES RECOVERED","任務の物資を回収");return true;
            }
            return false;
        }

        public bool NearbyMissionSupplyCrate
        {
            get
            {
                foreach(var crate in fieldAmmoCrates)
                    if(crate!=null&&crate.mission&&crate.visual&&Vector3.Distance(PlayerPosition,crate.position)<2.5f)return true;
                return false;
            }
        }

        static Color AmmoCrateColor(AmmoType type)
        {
            switch(type)
            {
                case AmmoType.Handgun:return new Color(.83f,.68f,.30f);
                case AmmoType.Shotgun:return new Color(1f,.43f,.13f);
                case AmmoType.SniperRifle:return new Color(.40f,.76f,.95f);
                default:return new Color(.25f,.82f,.72f);
            }
        }
    }
}
