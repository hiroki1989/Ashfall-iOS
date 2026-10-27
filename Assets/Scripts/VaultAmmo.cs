using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public enum AmmoType { Handgun, Shotgun, AssaultRifle, SniperRifle, LightMachineGun, HeavyMachineGun=LightMachineGun }

    public partial class VaultGame
    {
        // Preserve serialized ammo indices: the fifth slot holds heavy machine gun rounds.
        public const int AmmoTypeCount=5;
        public const int ActiveAmmoTypeCount=5;
        static readonly AmmoType[] ActiveAmmoTypes={AmmoType.Handgun,AmmoType.Shotgun,AmmoType.SniperRifle,AmmoType.AssaultRifle,AmmoType.LightMachineGun};
        public static AmmoType ActiveAmmoTypeAt(int index)=>ActiveAmmoTypes[Mathf.Clamp(index,0,ActiveAmmoTypeCount-1)];
        public static AmmoType NormalizeAmmoType(AmmoType type)=>type;
        public int ScrapLevelMultiplier=>Mathf.Clamp(1+Mathf.Max(0,DebtLevel-1)/2,1,20);
        public int ScaleScrap(int baseHundredths)=>Mathf.Max(1,Mathf.RoundToInt(baseHundredths/100f*ScrapLevelMultiplier*Mathf.Max(0f,VaultBalanceProfile.Current!=null?VaultBalanceProfile.Current.scrapGainMultiplier:1f)));
        static readonly int[] AmmoPackSizes={30,12,30,6,60};
        static readonly int[] BaseAmmoCapacities={240,96,360,60,480};
        public int AmmoCapacitySkillBonus(AmmoType type){type=NormalizeAmmoType(type);return ValidAmmo(type)?Mathf.RoundToInt(BaseAmmoCapacities[(int)type]*DebtAmmoCapacityBonus(AmmoModel(type))):0;}
        public int AmmoCapacity(AmmoType type){type=NormalizeAmmoType(type);return ValidAmmo(type)?BaseAmmoCapacities[(int)type]+AmmoCapacitySkillBonus(type)+Mathf.RoundToInt(BaseAmmoCapacities[(int)type]*.1f*AmmoCapacityTierFor(type)):0;}
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
            switch(Arsenal.Category(model))
            {
                case WeaponClass.Handgun:return AmmoType.Handgun;
                case WeaponClass.Shotgun:return AmmoType.Shotgun;
                case WeaponClass.SniperRifle:return AmmoType.SniperRifle;
                case WeaponClass.HeavyMachineGun:return AmmoType.LightMachineGun;
                default:return AmmoType.AssaultRifle;
            }
        }
        public int AmmoReserve(AmmoType type){type=NormalizeAmmoType(type);return ValidAmmo(type)?ammoReserves[(int)type]:0;}
        public int AmmoPackSize(AmmoType type){type=NormalizeAmmoType(type);return ValidAmmo(type)?AmmoPackSizes[(int)type]:0;}
        public int AmmoPackCost(AmmoType type){type=NormalizeAmmoType(type);return ValidAmmo(type)?PriceAfterDiscount(AmmoPackCosts[(int)type]):0;}
        public string AmmoLabel(AmmoType type)=>VaultLanguage.T(AmmoEnglish(NormalizeAmmoType(type)),AmmoJapanese(NormalizeAmmoType(type)));
        public int EffectiveMagazine=>gun==null?0:Mathf.Max(1,Mathf.RoundToInt(gun.magazine*(1+ArmorTotal(a=>a.ammoBonus)+ArtifactAffixBonus("ammo"))));
        static bool ValidAmmo(AmmoType type)=>(int)type>=0&&(int)type<AmmoTypeCount;
        static string AmmoEnglish(AmmoType type)=>type==AmmoType.Handgun?"Handgun rounds":type==AmmoType.Shotgun?"Shotgun shells":type==AmmoType.SniperRifle?"Sniper rifle rounds":type==AmmoType.AssaultRifle?"Assault rifle rounds":"Heavy machine gun rounds";
        static string AmmoJapanese(AmmoType type)=>type==AmmoType.Handgun?"ハンドガン弾":type==AmmoType.Shotgun?"ショットガン弾":type==AmmoType.SniperRifle?"スナイパーライフル弾":type==AmmoType.AssaultRifle?"アサルトライフル弾":"ヘビーマシンガン弾";
        int[] SaveAmmoReserves()=>(int[])ammoReserves.Clone();
        void LoadAmmoReserves(int[] saved)
        {
            Array.Clear(ammoReserves,0,ammoReserves.Length);
            if(saved==null)return;
            for(int i=0;i<Math.Min(saved.Length,ammoReserves.Length);i++)
            {
                ammoReserves[i]=Mathf.Clamp(saved[i],0,AmmoCapacity((AmmoType)i));
            }
        }
        void SeedAmmoReserves(){Array.Clear(ammoReserves,0,ammoReserves.Length);for(int i=0;i<ActiveAmmoTypeCount;i++){var type=ActiveAmmoTypeAt(i);ammoReserves[(int)type]=AmmoCapacity(type);}}
        public bool BuyAmmo(AmmoType type,int packs)
        {
            type=NormalizeAmmoType(type);
            if(!ValidAmmo(type)||packs<=0||packs>20)return false;
            int amount=Mathf.Min(AmmoPackSizes[(int)type]*packs,AmmoCapacity(type)-AmmoReserve(type));
            if(amount<=0)return false;
            int cost=PriceAfterDiscount(Mathf.CeilToInt(AmmoPackCosts[(int)type]*(amount/(float)AmmoPackSizes[(int)type])));
            if(!TrySpendCredits(cost))return false;
            ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+amount);SaveInventory();return true;
        }
        public void AddAmmo(AmmoType type,int amount)
        {type=NormalizeAmmoType(type);if(!ValidAmmo(type)||amount<=0)return;ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+amount);SaveInventory();}
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
            type=NormalizeAmmoType(type);
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
            bool final=room==CurrentDungeon.AreaCount-1;
            if(!final&&rng.NextDouble()>=.5)return;
            Vector3 p=Layout.Exit;
            if(final)
            {
                // Prefer a clear spot beside extraction, never a sealed chamber.
                bool found=false;
                for(int ring=0;ring<5&&!found;ring++)for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8;
                    Vector3 candidate=Layout.Exit+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(2.6f+ring*1.2f);
                    if(Blocked(candidate,.85f))continue;p=candidate;found=true;break;
                }
            }
            else
            {
                int cell=Layout.chambers.Count>0?rng.Next(Layout.chambers.Count):0;bool found=false;
                for(int i=0;i<Layout.chambers.Count;i++)
                {
                    var c=Layout.chambers[(cell+i)%Layout.chambers.Count];Vector3 candidate=Layout.Center(c);
                    if(c==Layout.chambers[Layout.EntryChamberIndex]||Blocked(candidate,.85f))continue;
                    p=candidate;found=true;break;
                }
                if(!found)p=Layout.Exit;
            }
            p.y=0;
            var obj=CreateLootPresentation("EQUIPMENT CHEST",p,new Vector3(1.8f,.88f,1.0f),Gold,LootPresentationKind.Chest);
            chests.Add(new Chest{position=p,visual=obj});
        }
        bool OpenNearbyChest()
        {
            if(IsOverworld)return OpenOverworldChest();
            Chest nearest=null;float distance=2.4f;
            foreach(var chest in chests)if(!chest.opened){float d=Vector3.Distance(PlayerPosition,chest.position);if(d<distance){distance=d;nearest=chest;}}
            if(nearest==null)return false;
            DropEquipmentChestReward(nearest.position,ExpeditionLootLevel(rng));
            nearest.opened=true;EquipmentChestVisual.Open(nearest.visual);
            ExpeditionSupplyRecovered();SaveInventory();
            Notify("EQUIPMENT CHEST / Equipment recovered","装備チェスト / 装備を回収しました");return true;
        }
    }
}
