using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public const int WeaponSlotCount=4;
        public int WeaponCapacity=>10+2*WeaponCapacityTier;
        public int ArmorCapacity=>20+10*ArmorCapacityTier;
        public int MeleeWeaponCapacity=>12+3*MeleeCapacityTier;
        public int WeaponCapacityTier{get;private set;}
        public int ArmorCapacityTier{get;private set;}
        public int MeleeCapacityTier{get;private set;}
        readonly int[] ammoCapacityTiers=new int[AmmoTypeCount];
        public int AmmoCapacityTierFor(AmmoType type)=>((int)type>=0&&(int)type<AmmoTypeCount)?ammoCapacityTiers[(int)type]:0;
        public readonly List<Gun> Weapons=new List<Gun>();
        public readonly List<ArmorItem> ArmorInventory=new List<ArmorItem>();
        public ArmorItem[] EquippedArmor=new ArmorItem[5];
        public int ActiveArmorCount {get {int count=0;foreach(var item in ArmorInventory)if(Armory.IsActiveArmor(item))count++;return count;}}
        public readonly List<MeleeWeaponItem> MeleeWeaponInventory=new List<MeleeWeaponItem>();
        public MeleeWeaponItem EquippedMeleeWeapon {get;private set;}
        public int SelectedWeapon {get;private set;}
        public int ActiveWeaponSlot {get;private set;}
        public int AvailableWeaponSlotCount=>Mathf.Clamp(2+DebtExtraWeaponSlots,2,WeaponSlotCount);
        readonly string[] weaponSlotIds=new string[WeaponSlotCount];
        public Gun WeaponInSlot(int slot)=>slot>=0&&slot<WeaponSlotCount?Weapons.Find(w=>w.id==weaponSlotIds[slot]):null;
        public int EquippedWeaponSlot(Gun item){if(item==null)return -1;for(int i=0;i<WeaponSlotCount;i++)if(WeaponInSlot(i)==item)return i;return -1;}
        void RepairWeaponSlots(bool fillEmpty=true)
        {
            var repair=new bool[WeaponSlotCount];for(int i=0;i<WeaponSlotCount;i++)repair[i]=i<AvailableWeaponSlotCount&&(fillEmpty||!string.IsNullOrEmpty(weaponSlotIds[i]));
            for(int i=0;i<WeaponSlotCount;i++)if(i>=AvailableWeaponSlotCount||WeaponInSlot(i)==null)weaponSlotIds[i]=null;
            for(int i=0;i<WeaponSlotCount;i++)if(weaponSlotIds[i]!=null)for(int j=0;j<i;j++)if(weaponSlotIds[i]==weaponSlotIds[j])weaponSlotIds[i]=null;
            for(int i=0;i<WeaponSlotCount;i++)if(weaponSlotIds[i]==null&&repair[i]){var candidate=Weapons.Find(w=>EquippedWeaponSlot(w)<0);if(candidate!=null)weaponSlotIds[i]=candidate.id;}
            if(WeaponInSlot(0)==null&&Weapons.Count>0)weaponSlotIds[0]=Weapons[0].id;
            if(WeaponInSlot(ActiveWeaponSlot)==null)ActiveWeaponSlot=WeaponInSlot(0)!=null?0:1;
        }
        public bool InventoryOpen {get;private set;}
        Screen inventoryReturn;
        float baseMaxHp=100,lastHurt;
        readonly Dictionary<string,int> magazineAmmo=new Dictionary<string,int>();
        readonly List<ArmorDrop> armorDrops=new List<ArmorDrop>();
        class ArmorDrop {public ArmorItem item;public Vector3 position;public GameObject visual;}
        int trailWardenGrantVersion,trailWardenDelivered;
        // One-time complimentary items may exceed capacity; ordinary acquisition keeps its existing limit.
        [Serializable] class LoadoutSave {public int trailWardenGrantVersion,trailWardenDelivered;public int weaponCapacityTier,armorCapacityTier,meleeCapacityTier,ammoCapacityTier;public int[] ammoCapacityTiers;public List<Gun> weapons;public List<ArmorItem> armor;public List<MeleeWeaponItem> meleeWeapons;public string equippedMelee;public string[] equipped;public int selected;public string[] weaponSlots;public int activeWeaponSlot;public int[] ammoReserves;public string[] magazineIds;public int[] magazines;}
        float ArmorTotal(Func<ArmorItem,float> value){float total=0;foreach(var item in EquippedArmor)if(Armory.IsActiveArmor(item))total+=value(item);return total;}
        public float EffectiveDamage=>gun==null?0:gun.damage*(1+damageBonus+DebtDamageBonus(VaultLanguage.DisplayModel(gun))+ArmorTotal(a=>a.damageBonus)+ArtifactAffixBonus("damage"))*(1+Mathf.Min(.75f,debtChainCount*DebtKillChainBonus))*ArtifactDamageMultiplier*(1+DebtBossDamageBonus*(room==CurrentDungeon.AreaCount-1?1:0));
        public float EffectiveFireRate=>gun==null?0:(1+fireBonus+DebtFireRateBonus(VaultLanguage.DisplayModel(gun))+ArmorTotal(a=>a.fireBonus)+ArtifactAffixBonus("fireRate"))/gun.interval;
        public float EffectiveReload=>gun==null?1:Mathf.Max(.35f,gun.reloadSeconds/(1+DebtReloadBonus(VaultLanguage.DisplayModel(gun))+ArmorTotal(a=>a.reloadBonus)+ArtifactAffixBonus("reload")));
        public float EffectiveAccuracy=>gun==null?0:Mathf.Clamp(Arsenal.Accuracy(gun)+100*DebtAccuracyBonus(VaultLanguage.DisplayModel(gun))+100*ArmorTotal(a=>a.accuracyBonus)+100*ArtifactAffixBonus("accuracy"),1,100);
        public float EffectiveMoveSpeed=>7*(1+Mathf.Min(.65f,ArmorTotal(a=>a.moveBonus)+EquippedWeaponAffixBonus("moveSpeed")+ArtifactAffixBonus("moveSpeed")));
        public float DamageReduction=>Mathf.Clamp(ArmorTotal(a=>a.damageReduction)+EquippedWeaponAffixBonus("mitigation")+ArtifactAffixBonus("mitigation")+DebtDamageReductionBonus,0,.65f);
        public float HealthRegen=>ArmorTotal(a=>a.regenBonus)+EquippedWeaponAffixBonus("regen")+ArtifactAffixBonus("regen")+DebtRecoveryBonus;
        public float EffectiveDodgeCooldown=>Mathf.Max(.4f,1.1f/(1+ArmorTotal(a=>a.dodgeBonus)));
        public ArmorItem NearbyArmor {get {var d=NearArmor();return d==null?null:d.item;}}
        public ActorEquipmentVisual PlayerAppearance=>player==null?null:player.GetComponent<ActorEquipmentVisual>();
        void LoadInventory(string storageKey="Vault.Loadout.v1",bool reviewTrailWardenMigration=false)
        {
            MeleeWeaponInventory.Clear();EquippedMeleeWeapon=null;trailWardenGrantVersion=0;trailWardenDelivered=0;WeaponCapacityTier=ArmorCapacityTier=MeleeCapacityTier=0;Array.Clear(ammoCapacityTiers,0,ammoCapacityTiers.Length);
            bool legacySlots=true,loadedAmmoReserves=false,hasSavedArmorSlots=false;
            if((!smoke||storageKey!="Vault.Loadout.v1")&&PlayerPrefs.HasKey(storageKey))
            {
                try
                {
                    var save=JsonUtility.FromJson<LoadoutSave>(PlayerPrefs.GetString(storageKey));
                    if(save!=null){trailWardenGrantVersion=save.trailWardenGrantVersion;trailWardenDelivered=save.trailWardenDelivered;WeaponCapacityTier=Mathf.Clamp(save.weaponCapacityTier,0,10);ArmorCapacityTier=Mathf.Clamp(save.armorCapacityTier,0,10);MeleeCapacityTier=Mathf.Clamp(save.meleeCapacityTier,0,10);for(int i=0;i<AmmoTypeCount;i++)ammoCapacityTiers[i]=save.ammoCapacityTiers!=null&&i<save.ammoCapacityTiers.Length?Mathf.Clamp(save.ammoCapacityTiers[i],0,10):Mathf.Clamp(save.ammoCapacityTier,0,10);}
                    if(save?.weapons!=null)foreach(var item in save.weapons)if(item!=null&&item.damage>0&&item.interval>0&&(Weapons.Count<WeaponCapacity||save.trailWardenGrantVersion>0)){if(string.IsNullOrEmpty(item.id))item.id=Guid.NewGuid().ToString("N");Weapons.Add(item);}
                    if(save?.armor!=null)foreach(var item in save.armor)if(item!=null&&(int)item.slot>=0&&(int)item.slot<5){if(string.IsNullOrEmpty(item.id))item.id=Guid.NewGuid().ToString("N");ArmorInventory.Add(item);}
                    if(save?.meleeWeapons!=null)foreach(var item in save.meleeWeapons)if(item!=null&&item.damage>0&&item.interval>0&&MeleeWeaponInventory.Count<MeleeWeaponCapacity){if(string.IsNullOrEmpty(item.id))item.id=Guid.NewGuid().ToString("N");MeleeWeaponCatalog.EnsureAffixes(item);if(!MeleeWeaponInventory.Exists(m=>m.id==item.id))MeleeWeaponInventory.Add(item);}
                    if(save!=null&&!string.IsNullOrEmpty(save.equippedMelee))EquippedMeleeWeapon=MeleeWeaponInventory.Find(m=>m.id==save.equippedMelee);
                    if(save?.equipped!=null){hasSavedArmorSlots=true;for(int i=1;i<Math.Min(5,save.equipped.Length);i++)EquippedArmor[i]=string.IsNullOrEmpty(save.equipped[i])?null:ArmorInventory.Find(a=>a.id==save.equipped[i]&&(int)a.slot==i);}
                    if(save?.ammoReserves!=null){LoadAmmoReserves(save.ammoReserves);loadedAmmoReserves=true;}
                    if(save?.magazineIds!=null&&save.magazines!=null)for(int i=0;i<Math.Min(save.magazineIds.Length,save.magazines.Length);i++)if(!string.IsNullOrEmpty(save.magazineIds[i]))magazineAmmo[save.magazineIds[i]]=Mathf.Max(0,save.magazines[i]);
                    SelectedWeapon=save==null?0:Mathf.Clamp(save.selected,0,Mathf.Max(0,Weapons.Count-1));
                    Array.Clear(weaponSlotIds,0,WeaponSlotCount);
                    if(save?.weaponSlots!=null){legacySlots=false;for(int i=0;i<Math.Min(WeaponSlotCount,save.weaponSlots.Length);i++)weaponSlotIds[i]=save.weaponSlots[i];ActiveWeaponSlot=Mathf.Clamp(save.activeWeaponSlot,0,WeaponSlotCount-1);}
                    else if(Weapons.Count>0){weaponSlotIds[0]=Weapons[SelectedWeapon].id;ActiveWeaponSlot=0;}
                }catch(Exception e){Debug.LogWarning("Loadout load failed: "+e.GetType().Name);}
            }
            foreach(var item in Weapons)GearBalance.Migrate(item);
            foreach(var item in ArmorInventory)if(Armory.IsActiveArmor(item))GearBalance.Migrate(item);
            EquippedArmor[0]=null;
            if(!loadedAmmoReserves)SeedAmmoReserves();
            if(MeleeWeaponInventory.Count==0)MeleeWeaponInventory.Add(MeleeWeaponCatalog.StarterKnife());
            if(EquippedMeleeWeapon==null)EquippedMeleeWeapon=MeleeWeaponInventory[0];
            if(Weapons.Count==0){Weapons.Add(smoke&&!reviewTrailWardenMigration?Arsenal.Starter():Arsenal.TrailWarden());Weapons.Add(smoke&&!reviewTrailWardenMigration?Arsenal.Model(3,Rarity.Common):Arsenal.TrailWarden(true));}
            for(int i=1;i<5;i++)if(EquippedArmor[i]==null&&!hasSavedArmorSlots)
            {var armor=ArmorInventory.Find(a=>(int)a.slot==i);if(armor==null){armor=smoke&&!reviewTrailWardenMigration?Armory.Starter((ArmorSlot)i):Armory.TrailWarden((ArmorSlot)i);if(ActiveArmorCount<ArmorCapacity)ArmorInventory.Add(armor);else continue;}EquippedArmor[i]=armor;}
            if(!smoke||reviewTrailWardenMigration){if(trailWardenGrantVersion==0)trailWardenGrantVersion=1;DeliverTrailWardenGifts(reviewTrailWardenMigration);}
            RepairWeaponSlots(legacySlots);gun=WeaponInSlot(ActiveWeaponSlot);SelectedWeapon=Weapons.IndexOf(gun);RefreshPermanentStats();ammo=gun!=null&&magazineAmmo.TryGetValue(gun.id,out int loaded)?Mathf.Min(EffectiveMagazine,loaded):gun==null?0:EffectiveMagazine;hp=maxHp;SaveInventory(storageKey);if(storageKey=="Vault.Loadout.v1")LoadStash();
        }
        bool DeliverTrailWardenGifts(bool review=false)
        {
            if((smoke&&!review)||trailWardenGrantVersion==0)return false;bool changed=false;
            for(int i=0;i<7;i++)
            {
                if((trailWardenDelivered&(1<<i))!=0)continue;
                if(i<2)
                {
                    int model=i==0?12:15;var item=Weapons.Find(w=>w.visualSet==Arsenal.TrailWardenVisualSet&&w.modelVariant==model);
                    if(item==null){item=Arsenal.TrailWarden(i==1);Weapons.Add(item);}
                    weaponSlotIds[i]=item.id;ActiveWeaponSlot=0;
                }
                else
                {
                    int slot=i-2;if(slot==0){trailWardenDelivered|=1<<i;changed=true;continue;}var item=ArmorInventory.Find(a=>a.visualVariant==4&&(int)a.slot==slot);
                    if(item==null){item=Armory.TrailWarden((ArmorSlot)slot);ArmorInventory.Add(item);}
                    EquippedArmor[slot]=item;
                }
                trailWardenDelivered|=1<<i;changed=true;
            }
            return changed;
        }
        public void RefreshWeaponAppearance()=>PlayerAppearance?.ApplyWeaponVisualSet(gun?.visualSet);
        void RefreshPermanentStats(){baseMaxHp=100;damageBonus=0;fireBonus=0;RecalculateArmor();}
        void SaveInventory(string storageKey="Vault.Loadout.v1")
        {
            EquippedArmor[0]=null;
            if(smoke&&storageKey=="Vault.Loadout.v1")return;var ids=new string[5];for(int i=0;i<5;i++)ids[i]=EquippedArmor[i]?.id;
            if(!smoke&&storageKey=="Vault.Loadout.v1")EquipmentDiscovery.RecordCurrent(Weapons,ArmorInventory,null,MeleeWeaponInventory);
            if(gun!=null)magazineAmmo[gun.id]=ammo;
            var magIds=new string[Weapons.Count];var mags=new int[Weapons.Count];for(int i=0;i<Weapons.Count;i++){magIds[i]=Weapons[i].id;mags[i]=magazineAmmo.TryGetValue(Weapons[i].id,out int value)?value:Weapons[i].magazine;}
            PlayerPrefs.SetString(storageKey,JsonUtility.ToJson(new LoadoutSave{trailWardenGrantVersion=trailWardenGrantVersion,trailWardenDelivered=trailWardenDelivered,weaponCapacityTier=WeaponCapacityTier,armorCapacityTier=ArmorCapacityTier,meleeCapacityTier=MeleeCapacityTier,ammoCapacityTiers=(int[])ammoCapacityTiers.Clone(),weapons=Weapons,armor=ArmorInventory,meleeWeapons=MeleeWeaponInventory,equippedMelee=EquippedMeleeWeapon?.id,equipped=ids,selected=SelectedWeapon,weaponSlots=(string[])weaponSlotIds.Clone(),activeWeaponSlot=ActiveWeaponSlot,ammoReserves=SaveAmmoReserves(),magazineIds=magIds,magazines=mags}));PlayerPrefs.Save();
        }
        public bool VerifyPersistence()
        {
            if(!smoke)return false;string key="Vault.QA."+Guid.NewGuid().ToString("N");
            string gunId=gun.id;int savedMagazineAmmo=ammo;var savedReserves=SaveAmmoReserves();var savedWeaponSlots=(string[])weaponSlotIds.Clone();int savedActiveSlot=ActiveWeaponSlot;int weaponCount=Weapons.Count,armorCount=ArmorInventory.Count;var equippedIds=new string[5];for(int i=0;i<5;i++)equippedIds[i]=EquippedArmor[i]?.id;
            SaveInventory(key);Weapons.Clear();ArmorInventory.Clear();EquippedArmor=new ArmorItem[5];LoadInventory(key);
            bool valid=Weapons.Count==weaponCount&&ArmorInventory.Count==armorCount&&gun.id==gunId&&ammo==savedMagazineAmmo&&ActiveWeaponSlot==savedActiveSlot;for(int i=0;i<WeaponSlotCount;i++)valid&=weaponSlotIds[i]==savedWeaponSlots[i];for(int i=0;i<AmmoTypeCount;i++)valid&=ammoReserves[i]==savedReserves[i];
            for(int i=0;i<5;i++)valid&=equippedIds[i]==null?EquippedArmor[i]==null:EquippedArmor[i]?.id==equippedIds[i]&&ArmorInventory.Contains(EquippedArmor[i]);
            PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();PlayerAppearance?.ApplyArmor(EquippedArmor);return valid;
        }
        public bool VerifyTrailWardenMigration()
        {
            if(!smoke)return false;
            string prefix="Vault.QA.TrailWarden."+Guid.NewGuid().ToString("N"),snapshot=prefix+".snapshot",test=prefix+".test";
            float priorHp=hp;SaveInventory(snapshot);string original=PlayerPrefs.GetString(snapshot);bool valid=true;
            try
            {
                // Explicit test storage is the only smoke path allowed to exercise the migration.
                Weapons.Clear();ArmorInventory.Clear();EquippedArmor=new ArmorItem[5];magazineAmmo.Clear();
                LoadInventory(test,true);
                valid&=Weapons.Count==2&&ArmorInventory.Count==4&&EquippedArmor[0]==null&&trailWardenDelivered==127;
                valid&=WeaponInSlot(0)?.visualSet==Arsenal.TrailWardenVisualSet&&WeaponInSlot(1)?.visualSet==Arsenal.TrailWardenVisualSet;
                valid&=EquippedArmor[0]==null;for(int i=1;i<5;i++)valid&=EquippedArmor[i]?.visualVariant==4;
                var full=new LoadoutSave{weapons=new List<Gun>(),armor=new List<ArmorItem>()};
                for(int i=0;i<WeaponCapacity;i++)full.weapons.Add(Arsenal.Model(i%Arsenal.ModelCount,Rarity.Common));
                for(int i=0;i<ArmorCapacity;i++)full.armor.Add(Armory.Starter((ArmorSlot)(i%5)));
                PlayerPrefs.SetString(test,JsonUtility.ToJson(full));
                Weapons.Clear();ArmorInventory.Clear();EquippedArmor=new ArmorItem[5];magazineAmmo.Clear();LoadInventory(test,true);
                valid&=Weapons.Count==WeaponCapacity+2&&ArmorInventory.Count==ArmorCapacity+4&&trailWardenDelivered==127;
                foreach(var item in full.weapons)valid&=Weapons.Exists(w=>w.id==item.id);
                foreach(var item in full.armor)valid&=ArmorInventory.Exists(a=>a.id==item.id);
                valid&=WeaponInSlot(0)?.modelVariant==12&&WeaponInSlot(1)?.modelVariant==15&&gun==WeaponInSlot(0);
                valid&=EquippedArmor[0]==null;for(int i=1;i<5;i++)valid&=EquippedArmor[i]?.visualVariant==4;
                string once=PlayerPrefs.GetString(test);
                Weapons.Clear();ArmorInventory.Clear();EquippedArmor=new ArmorItem[5];magazineAmmo.Clear();LoadInventory(test,true);
                valid&=Weapons.Count==WeaponCapacity+2&&ArmorInventory.Count==ArmorCapacity+4;
                valid&=once==PlayerPrefs.GetString(test);
            }
            finally
            {
                Weapons.Clear();ArmorInventory.Clear();EquippedArmor=new ArmorItem[5];magazineAmmo.Clear();LoadInventory(snapshot);hp=priorHp;
                valid&=original==PlayerPrefs.GetString(snapshot);
                PlayerPrefs.DeleteKey(snapshot);PlayerPrefs.DeleteKey(test);PlayerPrefs.Save();
                PlayerAppearance?.ApplyArmor(EquippedArmor);RefreshWeaponAppearance();
            }
            return valid;
        }
        public void SelectWeapon(int index)
        {
            if(index<0||index>=Weapons.Count)return;
            int slot=EquippedWeaponSlot(Weapons[index]);
            if(slot<0){EquipWeapon(index,ActiveWeaponSlot);return;}
            ActivateWeaponSlot(slot);
        }
        public void EquipWeapon(int index,int slot)
        {
            if(index<0||index>=Weapons.Count||slot<0||slot>=AvailableWeaponSlotCount)return;
            var item=Weapons[index];int previousSlot=EquippedWeaponSlot(item);
            if(previousSlot>=0&&previousSlot!=slot)weaponSlotIds[previousSlot]=weaponSlotIds[slot];
            weaponSlotIds[slot]=item.id;RepairWeaponSlots();ActivateWeaponSlot(slot);
        }
        public void ActivateWeaponSlot(int slot)
        {
            var next=WeaponInSlot(slot);if(next==null)return;
            bool changed=gun!=null&&gun!=next;
            if(gun!=null)magazineAmmo[gun.id]=ammo;
            ActiveWeaponSlot=slot;SelectedWeapon=Weapons.IndexOf(next);gun=next;ammo=magazineAmmo.TryGetValue(gun.id,out int remaining)?Mathf.Min(EffectiveMagazine,remaining):EffectiveMagazine;
            StopReloadSound();reload=0;reloadPending=0;fireTimer=Mathf.Max(fireTimer,.16f);if(changed)PlayWeaponSound(gun,"equip",.65f);PlayerWeapon?.Recolor(gun.color);if(PlayerWeapon!=null)VaultArt.ConfigureWeapon(PlayerWeapon.transform,gun.effect,gun.color,VaultLanguage.DisplayModel(gun));RefreshWeaponAppearance();CreateSatellites();RecalculateArmor();SaveInventory();
        }
        public void CycleWeapon(int direction)
        {
            for(int step=1;step<=AvailableWeaponSlotCount;step++){int slot=((ActiveWeaponSlot+direction*step)%AvailableWeaponSlotCount+AvailableWeaponSlotCount)%AvailableWeaponSlotCount;if(WeaponInSlot(slot)!=null){ActivateWeaponSlot(slot);return;}}
        }
        public bool StoreWeapon(Gun item)
        {
            GearBalance.Migrate(item);
            if(item==null)return false;if(Weapons.Contains(item))return true;
            if(Weapons.Count>=WeaponCapacity)return false;
            if(string.IsNullOrEmpty(item.id))item.id=Guid.NewGuid().ToString("N");
            Weapons.Add(item);SaveInventory();return true;
        }
        public bool StoreMeleeWeapon(MeleeWeaponItem item)
        {
            if(item==null)return false;
            if(string.IsNullOrEmpty(item.id))item.id=Guid.NewGuid().ToString("N");
            if(MeleeWeaponInventory.Exists(m=>m.id==item.id))return true;
            if(MeleeWeaponInventory.Count>=MeleeWeaponCapacity)return false;
            MeleeWeaponCatalog.EnsureAffixes(item);MeleeWeaponInventory.Add(item);
            SaveInventory();return true;
        }
        public void EquipMeleeWeapon(MeleeWeaponItem item)
        {if(item==null||!MeleeWeaponInventory.Contains(item))return;if(EquippedMeleeWeapon!=item)PlayWeaponSound(gun,"equip",.5f);EquippedMeleeWeapon=item;SaveInventory();}
        public bool StoreArmor(ArmorItem item)
        {if(!Armory.IsActiveArmor(item)||ActiveArmorCount>=ArmorCapacity)return false;GearBalance.Migrate(item);if(!ArmorInventory.Contains(item))ArmorInventory.Add(item);SaveInventory();return true;}
        public void EquipArmor(ArmorItem item)
        {if(!Armory.IsActiveArmor(item)||!ArmorInventory.Contains(item))return;if(EquippedArmor[(int)item.slot]!=item)PlayWeaponSound(gun,"equip",.5f);EquippedArmor[(int)item.slot]=item;RecalculateArmor();PlayerAppearance?.ApplyArmor(EquippedArmor);SaveInventory();}
        public void UnequipArmor(int slot)
        {if(slot<0||slot>=EquippedArmor.Length||EquippedArmor[slot]==null)return;EquippedArmor[slot]=null;RecalculateArmor();PlayerAppearance?.ApplyArmor(EquippedArmor);SaveInventory();}
        void RecalculateArmor(){EquippedArmor[0]=null;maxHp=baseMaxHp+DebtHealthBonus+ArmorTotal(a=>a.maxHealth)+EquippedWeaponAffixBonus("health")+ArtifactAffixBonus("health");hp=Mathf.Min(hp,maxHp);}
        public void DiscardWeapon(int index)
        {
            if(Weapons.Count<=1||index<0||index>=Weapons.Count)return;
            var removed=Weapons[index];bool current=removed==gun;Weapons.RemoveAt(index);magazineAmmo.Remove(removed.id);
            RepairWeaponSlots();if(current){gun=null;ActivateWeaponSlot(ActiveWeaponSlot);}else SelectedWeapon=Weapons.IndexOf(gun);SaveInventory();
        }
        public void DiscardArmor(ArmorItem item){if(!Armory.IsActiveArmor(item)||Array.IndexOf(EquippedArmor,item)>=0)return;ArmorInventory.Remove(item);SaveInventory();}
        public void ToggleInventory()
        {
            if(InventoryOpen){actionSkillInventoryTree=false;actionSkillInventoryOpen=false;InventoryOpen=false;screen=inventoryReturn;Time.timeScale=screen==Screen.Pause||screen==Screen.DungeonSelect?0:1;return;}
            if(screen!=Screen.Menu&&screen!=Screen.Playing&&screen!=Screen.Pause&&screen!=Screen.Hub&&screen!=Screen.Shop&&screen!=Screen.DungeonSelect)return;
            inventoryReturn=screen;InventoryOpen=true;Time.timeScale=0;
        }
        ArmorDrop NearArmor(){ArmorDrop nearest=null;float distance=2.6f;foreach(var d in armorDrops){float n=Vector3.Distance(PlayerPosition,d.position);if(n<distance){distance=n;nearest=d;}}return nearest;}
        void AddArmorDrop(ArmorItem item,Vector3 p)
        {
            if(!Armory.IsActiveArmor(item))return;
            GearBalance.Migrate(item);
            p.y=.35f;string displayName=Armory.Name(item);var shape=CreateLootPresentation(displayName,p,new Vector3(.6f,.35f,.65f),item.color,LootPresentationKind.Armor,rarity:item.rarity);

            ApplyProductDropModel(shape,item);armorDrops.Add(new ArmorDrop{item=item,position=p,visual=shape});
        }
    }
}
