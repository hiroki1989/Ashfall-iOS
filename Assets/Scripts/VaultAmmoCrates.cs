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
        }

        readonly List<int> ammoCrateRoomPlan = new List<int>(2);
        readonly List<FieldAmmoCrate> fieldAmmoCrates = new List<FieldAmmoCrate>(2);
        int ammoCratePlanCount;
        public int AmmoCratesPlannedThisRun => ammoCratePlanCount;
        public int AmmoCratesNearbyCount => fieldAmmoCrates.Count;

        /// <summary>Call once after the run RNG/dungeon have been selected and before the first BuildRoom.</summary>
        public void PrepareAmmoCratesForRun()
        {
            ammoCrateRoomPlan.Clear();
            fieldAmmoCrates.Clear();
            ammoCratePlanCount=0;
            if (rng == null || CurrentDungeon.AreaCount <= 1) return;

            var available = new List<int>();
            // Reserve the boss chamber for its guaranteed combat/loot flow.
            for (int i = 0; i < CurrentDungeon.AreaCount - 1; i++) available.Add(i);
            int count = Mathf.Min(rng.Next(1, 3), available.Count);
            ammoCratePlanCount=count;
            for (int i = 0; i < count; i++)
            {
                int pick = rng.Next(available.Count);
                ammoCrateRoomPlan.Add(available[pick]);
                available.RemoveAt(pick);
            }
        }

        /// <summary>Call for each populated combat room after its SectorLayout has been generated.</summary>
        public void SpawnAmmoCratesForCurrentRoom()
        {
            fieldAmmoCrates.RemoveAll(crate => crate == null || crate.visual == null);
            if (room >= CurrentDungeon.AreaCount - 1 || !ammoCrateRoomPlan.Remove(room) || Layout == null || Layout.chambers.Count < 3) return;

            for (int attempt = 0; attempt < 72; attempt++)
            {
                var chamber = Layout.chambers[rng.Next(Layout.chambers.Count)];
                if (chamber == Layout.chambers[Layout.EntryChamberIndex] || chamber == Layout.chambers[Layout.ExitChamberIndex]) continue;
                Vector3 p = Layout.Center(chamber) + new Vector3((float)rng.NextDouble()*3.4f-1.7f,0,(float)rng.NextDouble()*2.8f-1.4f);
                if (Vector3.Distance(p, Layout.Entry) < 8f || Blocked(p,.75f)) continue;
                var type = (AmmoType)rng.Next(4);
                int amount = AmmoPackSize(type); // A crate is a full, legible pack of one ammunition class.
                var visual = CreateLootPresentation(AmmoPickupName(type,amount),new Vector3(p.x,.31f,p.z),new Vector3(.82f,.38f,.72f),AmmoCrateColor(type),LootPresentationKind.AmmoCrate,type,amount);
                fieldAmmoCrates.Add(new FieldAmmoCrate{position=visual.transform.position,type=type,amount=amount,visual=visual});
                return;
            }
            Debug.LogWarning("Could not place this room's planned ammunition crate in a safe chamber.");
        }

        public bool OpenNearbyAmmoCrate()
        {
            FieldAmmoCrate nearest=null;float best=2.5f;
            for(int i=0;i<fieldAmmoCrates.Count;i++)
            {
                var crate=fieldAmmoCrates[i];if(crate==null||crate.visual==null)continue;
                float distance=Vector3.Distance(PlayerPosition,crate.position);if(distance<best){best=distance;nearest=crate;}
            }
            if(nearest==null)return false;

            float scale=DebtAmmoPickupBonus+ArmorTotal(a=>a.ammoBonus)+EquippedWeaponAffixBonus("ammo")+ArtifactAffixBonus("ammo");
            int requested=Mathf.Max(1,Mathf.RoundToInt(nearest.amount*(1+scale))+Mathf.RoundToInt(DebtAmmoBonus(AmmoModel(nearest.type))));
            int picked=Mathf.Min(requested,AmmoCapacity(nearest.type)-AmmoReserve(nearest.type));
            if(picked<=0){Notify("AMMO FULL / "+AmmoLabel(nearest.type),"弾薬満杯 / "+AmmoLabel(nearest.type));return true;}
            AddAmmo(nearest.type,picked);
            ExpeditionSupplyRecovered();
            if(nearest.visual!=null)Destroy(nearest.visual);
            fieldAmmoCrates.Remove(nearest);
            string en=AmmoPickupEnglish(nearest.type,picked),ja=AmmoPickupJapanese(nearest.type,picked);
            Notify("AMMO CRATE / "+en,"弾薬箱 / "+ja);
            return true;
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
