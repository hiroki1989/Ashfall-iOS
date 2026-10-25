using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public enum AmmoType { Handgun, Shotgun, AssaultRifle, SniperRifle, LightMachineGun }

    public partial class VaultGame
    {
        public const int AmmoTypeCount=5;
        public int ScrapLevelMultiplier=>Mathf.Clamp(1+Mathf.Max(0,DebtLevel-1)/2,1,20);
        public int ScaleScrap(int baseHundredths)=>Mathf.Max(1,Mathf.RoundToInt(baseHundredths/100f*ScrapLevelMultiplier*Mathf.Max(0f,VaultBalanceProfile.Current!=null?VaultBalanceProfile.Current.scrapGainMultiplier:1f)));
        static readonly int[] AmmoPackSizes={30,12,30,6,60};
        static readonly int[] BaseAmmoCapacities={240,96,360,60,480};
        public int AmmoCapacitySkillBonus(AmmoType type)=>ValidAmmo(type)?Mathf.RoundToInt(BaseAmmoCapacities[(int)type]*(DebtAmmoCapacityBonus(AmmoModel(type)))):0;
        public int AmmoCapacity(AmmoType type)=>ValidAmmo(type)?BaseAmmoCapacities[(int)type]+AmmoCapacitySkillBonus(type)+Mathf.RoundToInt(BaseAmmoCapacities[(int)type]*.1f*AmmoCapacityTierFor(type)):0;
        // Match the salvage-credit economy used by the safehouse vendors.
        static readonly int[] AmmoPackCosts={1800,2400,2000,2200,2800};
        readonly int[] ammoReserves=new int[AmmoTypeCount];
        readonly List<Chest> chests=new List<Chest>();
        readonly List<SupplyDrop> supplyDrops=new List<SupplyDrop>();
        int chestRoomForRun=-1;
        class Chest { public Vector3 position; public GameObject visual; public bool opened; }
        class SupplyDrop { public int credits; public AmmoType ammoType; public int ammoAmount; public bool healthPack; public Vector3 position; public GameObject visual; }

        public static AmmoType AmmoForGun(Gun item)
        {
            int model=VaultLanguage.DisplayModel(item);
            var recipe=DistinctEquipmentDesigns.For(item);
            if(model==6||(recipe!=null&&recipe.gunClass==3))return AmmoType.LightMachineGun;
            switch(Arsenal.Category(model))
            {
                case WeaponClass.Handgun:return AmmoType.Handgun;
                case WeaponClass.Shotgun:return AmmoType.Shotgun;
                case WeaponClass.SniperRifle:return AmmoType.SniperRifle;
                default:return AmmoType.AssaultRifle;
            }
        }
        public int AmmoReserve(AmmoType type)=>ValidAmmo(type)?ammoReserves[(int)type]:0;
        public int AmmoPackSize(AmmoType type)=>ValidAmmo(type)?AmmoPackSizes[(int)type]:0;
        public int AmmoPackCost(AmmoType type)=>ValidAmmo(type)?PriceAfterDiscount(AmmoPackCosts[(int)type]):0;
        public string AmmoLabel(AmmoType type)=>VaultLanguage.T(AmmoEnglish(type),AmmoJapanese(type));
        public int EffectiveMagazine=>gun==null?0:Mathf.Max(1,Mathf.RoundToInt(gun.magazine*(1+ArmorTotal(a=>a.ammoBonus)+ArtifactAffixBonus("ammo"))));
        static bool ValidAmmo(AmmoType type)=>(int)type>=0&&(int)type<AmmoTypeCount;
        static string AmmoEnglish(AmmoType type)=>type==AmmoType.Handgun?"Handgun rounds":type==AmmoType.Shotgun?"Shotgun shells":type==AmmoType.SniperRifle?"Sniper rounds":type==AmmoType.LightMachineGun?"LMG rounds":"Rifle rounds";
        static string AmmoJapanese(AmmoType type)=>type==AmmoType.Handgun?"拳銃弾":type==AmmoType.Shotgun?"散弾":type==AmmoType.SniperRifle?"狙撃弾":type==AmmoType.LightMachineGun?"軽機関銃弾":"ライフル弾";
        int[] SaveAmmoReserves()=>(int[])ammoReserves.Clone();
        void LoadAmmoReserves(int[] saved){Array.Clear(ammoReserves,0,ammoReserves.Length);if(saved!=null)for(int i=0;i<Math.Min(saved.Length,ammoReserves.Length);i++)ammoReserves[i]=Mathf.Clamp(saved[i],0,AmmoCapacity((AmmoType)i));}
        void SeedAmmoReserves(){for(int i=0;i<AmmoTypeCount;i++)ammoReserves[i]=AmmoCapacity((AmmoType)i);}
        public bool BuyAmmo(AmmoType type,int packs)
        {
            if(!ValidAmmo(type)||packs<=0||packs>20)return false;
            int amount=Mathf.Min(AmmoPackSizes[(int)type]*packs,AmmoCapacity(type)-AmmoReserve(type));
            if(amount<=0)return false;
            int cost=PriceAfterDiscount(Mathf.CeilToInt(AmmoPackCosts[(int)type]*(amount/(float)AmmoPackSizes[(int)type])));
            if(!TrySpendCredits(cost))return false;
            ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+amount);SaveInventory();return true;
        }
        public void AddAmmo(AmmoType type,int amount)
        {if(!ValidAmmo(type)||amount<=0)return;ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+amount);SaveInventory();}
        bool TryStartReload()
        {
            if(gun==null||reload>0||ammo>=EffectiveMagazine||AmmoReserve(AmmoForGun(gun))<=0)return false;
            reloadPending=Mathf.Min(EffectiveMagazine-ammo,AmmoReserve(AmmoForGun(gun)));
            if(reloadPending<=0)return false;reload=EffectiveReload;return true;
        }
        int reloadPending;
        void CompleteAmmoReload()
        {
            if(gun==null){reloadPending=0;return;}
            var type=AmmoForGun(gun);int amount=Mathf.Min(reloadPending,ammoReserves[(int)type]);ammoReserves[(int)type]-=amount;ammo=Mathf.Min(EffectiveMagazine,ammo+amount);reloadPending=0;ArtifactOnReload();SaveInventory();
        }
        void TickFieldEconomy()
        {
            if(player==null)return;
            for(int i=supplyDrops.Count-1;i>=0;i--)
            {
                var d=supplyDrops[i];if(Vector3.Distance(PlayerPosition,d.position)>1.25f)continue;
                if(d.healthPack)
                {
                    if(hp>=maxHp-.01f)continue;
                    float restored=Mathf.Min(maxHp-hp,maxHp*.05f);
                    hp=Mathf.Min(maxHp,hp+restored);
                    if(d.visual!=null)Destroy(d.visual);supplyDrops.RemoveAt(i);audioSource.PlayOneShot(pickupSound);
                    Notify("HEALTH PACK +"+Mathf.CeilToInt(restored)+" HP","回復パック HP +"+Mathf.CeilToInt(restored));
                    continue;
                }
                float ammoScale=DebtAmmoPickupBonus+ArmorTotal(a=>a.ammoBonus)+EquippedWeaponAffixBonus("ammo")+ArtifactAffixBonus("ammo");
                if(d.credits>0)AwardCredits(d.credits);int reserveBefore=AmmoReserve(d.ammoType);int requestedAmmo=d.ammoAmount>0?Mathf.Max(1,Mathf.RoundToInt(d.ammoAmount*(1+ammoScale))+Mathf.RoundToInt(DebtAmmoBonus(AmmoModel(d.ammoType)))):0;int pickedAmmo=Mathf.Min(requestedAmmo,AmmoCapacity(d.ammoType)-reserveBefore);if(d.ammoAmount>0&&pickedAmmo<=0)continue;if(pickedAmmo>0)AddAmmo(d.ammoType,pickedAmmo);
                if(d.visual!=null)Destroy(d.visual);supplyDrops.RemoveAt(i);audioSource.PlayOneShot(pickupSound);
                if(d.credits>0)Notify("SCRAP +"+d.credits,"スクラップ +"+d.credits);
                else Notify(AmmoPickupEnglish(d.ammoType,pickedAmmo),AmmoPickupJapanese(d.ammoType,pickedAmmo));
            }
        }
        static int AmmoModel(AmmoType type)=>type==AmmoType.Handgun?14:type==AmmoType.Shotgun?3:type==AmmoType.SniperRifle?2:type==AmmoType.LightMachineGun?6:12;
        void AddScrapDrop(Vector3 p,int credits)
        {
            p.y=.22f;var visual=CreateLootPresentation("Scrap",p,new Vector3(.40f,.20f,.42f),Gold,LootPresentationKind.Scrap);supplyDrops.Add(new SupplyDrop{credits=credits,position=p,visual=visual});
        }
        void AddAmmoDrop(Vector3 p,AmmoType type,int amount)
        {
            p.y=.22f;Color color=type==AmmoType.Shotgun?new Color(1,.64f,.22f):type==AmmoType.SniperRifle?new Color(.45f,.8f,1):type==AmmoType.Handgun?new Color(.86f,.82f,.56f):Cyan;
            var visual=CreateLootPresentation(AmmoPickupName(type,amount),p,new Vector3(.62f,.24f,.58f),color,LootPresentationKind.Ammo,type,amount);supplyDrops.Add(new SupplyDrop{ammoType=type,ammoAmount=amount,position=p,visual=visual});
        }
        void AddHealthPackDrop(Vector3 p)
        {
            p.y=.24f;
            var visual=CreateLootPresentation(VaultLanguage.T("Health pack","回復パック"),p,new Vector3(.50f,.25f,.42f),new Color(.89f,.22f,.19f),LootPresentationKind.HealthPack);
            supplyDrops.Add(new SupplyDrop{healthPack=true,position=p,visual=visual});
        }
        void SpawnRoomChest()
        {
            if(room!=chestRoomForRun||Layout.chambers.Count<3)return;
            chestRoomForRun=-1;
            int cell=rng.Next(Layout.chambers.Count);for(int i=0;i<Layout.chambers.Count;i++){var c=Layout.chambers[(cell+i)%Layout.chambers.Count];Vector3 p=Layout.Center(c);if(c==Layout.chambers[Layout.EntryChamberIndex]||c==Layout.chambers[Layout.ExitChamberIndex]||Vector3.Distance(p,Layout.Entry)<12||Blocked(p,.7f))continue;
                var obj=CreateLootPresentation("SUPPLY CHEST / [F] OPEN",new Vector3(p.x,.3f,p.z),new Vector3(1.4f,.88f,1.25f),new Color(1f,.53f,.12f),LootPresentationKind.Chest);chests.Add(new Chest{position=obj.transform.position,visual=obj});return;}
        }
        bool OpenNearbyChest()
        {
            Chest nearest=null;float distance=2.4f;foreach(var chest in chests)if(!chest.opened){float d=Vector3.Distance(PlayerPosition,chest.position);if(d<distance){distance=d;nearest=chest;}}
            if(nearest==null)return false;var roll=rng.Next(100);
            if(roll<22)
            {
                var item=RollExpeditionGun(rng);if(StoreWeapon(item)){Notify("CACHE: "+VaultLanguage.GunName(item),"物資箱: "+VaultLanguage.GunName(item));Destroy(nearest.visual);}else{Notify("CACHE: weapon storage full","物資箱: 銃の所持枠が満杯");return true;}
            }
            else if(roll<42)
            {
                var item=RollExpeditionArmor(rng);if(StoreArmor(item)){Notify("CACHE: "+Armory.Name(item),"物資箱: "+Armory.Name(item));Destroy(nearest.visual);}else{Notify("CACHE: armor storage full","物資箱: 防具の所持枠が満杯");return true;}
            }
            else if(roll<57)
            {
                var item=RollExpeditionArtifact(rng);if(StoreArtifact(item)){Notify("CACHE: "+item.Name,"物資箱: "+item.Name);Destroy(nearest.visual);}else{Notify("CACHE: module storage full","物資箱: モジュールの所持枠が満杯");return true;}
            }
            else if(roll<77){int amount=ScaleScrap(35000+DungeonIndex*15000+rng.Next(25001));AwardCredits(amount);Destroy(nearest.visual);Notify("CACHE: SCRAP +"+amount,"物資箱: スクラップ +"+amount);}
            else {var type=(AmmoType)rng.Next(AmmoTypeCount);int amount=AmmoPackSizes[(int)type];AddAmmo(type,amount);Destroy(nearest.visual);Notify("CACHE: "+AmmoLabel(type)+" +"+amount,"物資箱: "+AmmoLabel(type)+" +"+amount);}
            ExpeditionSupplyRecovered();nearest.opened=true;return true;
        }
    }
}
