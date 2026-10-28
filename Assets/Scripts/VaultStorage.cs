using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string StashKey="Vault.Stash.v1", StashJournalKey="Vault.Stash.Move.v1", StashUpgradeKey="Vault.Stash.Upgrade.v1";
        [Serializable] class StashUpgradeJournal { public int creditsAfter, tierAfter; }
        [Serializable] class StashSave { public int tier; public List<Gun> guns=new List<Gun>(); public List<ArmorItem> armor=new List<ArmorItem>(); public List<ArtifactItem> modules=new List<ArtifactItem>(); public List<MeleeWeaponItem> melee=new List<MeleeWeaponItem>(); }
        [Serializable] class StashMove { public bool deposit; public int category; public Gun gun; public ArmorItem armor; public ArtifactItem module; public MeleeWeaponItem melee; }
        StashSave stash=new StashSave();
        public int StashTier { get=>stash.tier; private set=>stash.tier=value; }
        public int StashCapacity=>50+10*StashTier;
        public int StashCount=>stash.guns.Count+stash.armor.Count+stash.modules.Count+stash.melee.Count;
        public bool VaultStorageVisible { get; private set; }
        int stashCategory; bool stashViewingStored; Vector2 stashScroll; bool stashPointerDown,stashDragging; Vector2 stashPointerStart,stashPointerLast;
        void SaveStash(){if(smoke)return;PlayerPrefs.SetString(StashKey,JsonUtility.ToJson(stash));PlayerPrefs.Save();}
        void LoadStash()
        {
            stash=new StashSave();if(smoke)return;
            try { if(PlayerPrefs.HasKey(StashKey))stash=JsonUtility.FromJson<StashSave>(PlayerPrefs.GetString(StashKey))??new StashSave(); }
            catch(Exception e){Debug.LogWarning("Stash load failed: "+e.GetType().Name);return;}
            stash.tier=Mathf.Clamp(stash.tier,0,10);stash.guns=stash.guns??new List<Gun>();stash.armor=stash.armor??new List<ArmorItem>();stash.modules=stash.modules??new List<ArtifactItem>();stash.melee=stash.melee??new List<MeleeWeaponItem>();
            if(PlayerPrefs.HasKey(StashUpgradeKey))try { var upgrade=JsonUtility.FromJson<StashUpgradeJournal>(PlayerPrefs.GetString(StashUpgradeKey));if(upgrade!=null)FinishStashUpgrade(upgrade); } catch(Exception e){Debug.LogWarning("Stash upgrade recovery failed: "+e.GetType().Name);}
            if(PlayerPrefs.HasKey(StashJournalKey))try { var move=JsonUtility.FromJson<StashMove>(PlayerPrefs.GetString(StashJournalKey)); if(move!=null)ApplyStashMove(move); } catch(Exception e){Debug.LogWarning("Stash recovery failed: "+e.GetType().Name);}
        }
        bool BuyStashCapacity(int cost)
        {
            var journal=new StashUpgradeJournal{creditsAfter=Credits-cost,tierAfter=StashTier+1};
            if(!smoke){PlayerPrefs.SetString(StashUpgradeKey,JsonUtility.ToJson(journal));PlayerPrefs.Save();}
            FinishStashUpgrade(journal);return true;
        }
        void FinishStashUpgrade(StashUpgradeJournal journal)
        {
            Credits=journal.creditsAfter;StashTier=journal.tierAfter;SaveProgression();SaveStash();
            if(!smoke){PlayerPrefs.DeleteKey(StashUpgradeKey);PlayerPrefs.Save();}
        }
        public void VaultStorageOpen(){if(screen!=Screen.Hub)return;VaultStorageVisible=true;stashScroll=Vector2.zero;}
        public void VaultStorageClose(){VaultStorageVisible=false;stashPointerDown=false;stashDragging=false;}
        bool MoveStash(object item,bool deposit)
        {
            if(item==null||screen!=Screen.Hub)return false;
            if(deposit&&StashCount>=StashCapacity)return false;
            var m=new StashMove{deposit=deposit};
            if(item is Gun g){if(deposit&&(!Weapons.Contains(g)||EquippedWeaponSlot(g)>=0||Weapons.Count<=1)||!deposit&&Weapons.Count>=WeaponCapacity)return false;m.category=0;m.gun=g;}
            else if(item is ArmorItem a){if(deposit&&(!ArmorInventory.Contains(a)||Array.IndexOf(EquippedArmor,a)>=0)||!deposit&&ActiveArmorCount>=ArmorCapacity)return false;m.category=1;m.armor=a;}
            else if(item is ArtifactItem module){if(deposit&&(!ArtifactInventory.Contains(module)||Array.IndexOf(EquippedArtifacts,module)>=0)||!deposit&&ArtifactInventory.Count>=ArtifactCapacity)return false;m.category=2;m.module=module;}
            else if(item is MeleeWeaponItem melee){if(deposit&&(!MeleeWeaponInventory.Contains(melee)||EquippedMeleeWeapon==melee)||!deposit&&MeleeWeaponInventory.Count>=MeleeWeaponCapacity)return false;m.category=3;m.melee=melee;}
            else return false;
            // Reject ambiguous IDs before writing a recovery journal.
            if(m.category==0&&(string.IsNullOrEmpty(m.gun.id)||Weapons.FindAll(x=>x.id==m.gun.id).Count+stash.guns.FindAll(x=>x.id==m.gun.id).Count!=1))return false;
            if(m.category==1&&(string.IsNullOrEmpty(m.armor.id)||ArmorInventory.FindAll(x=>x.id==m.armor.id).Count+stash.armor.FindAll(x=>x.id==m.armor.id).Count!=1))return false;
            if(m.category==2&&(string.IsNullOrEmpty(m.module.id)||ArtifactInventory.FindAll(x=>x.id==m.module.id).Count+stash.modules.FindAll(x=>x.id==m.module.id).Count!=1))return false;
            if(m.category==3&&(string.IsNullOrEmpty(m.melee.id)||MeleeWeaponInventory.FindAll(x=>x.id==m.melee.id).Count+stash.melee.FindAll(x=>x.id==m.melee.id).Count!=1))return false;
            if(!smoke){PlayerPrefs.SetString(StashJournalKey,JsonUtility.ToJson(m));PlayerPrefs.Save();}ApplyStashMove(m);return true;
        }
        void ApplyStashMove(StashMove m)
        {
            // Replaying a journal is idempotent; preserve the instance already in the destination.
            if(m.category==0&&m.gun!=null){var a=Weapons.Find(x=>x.id==m.gun.id);var b=stash.guns.Find(x=>x.id==m.gun.id);if(m.deposit){if(b==null)stash.guns.Add(a??m.gun);if(a!=null)Weapons.Remove(a);}else{if(a==null)Weapons.Add(b??m.gun);if(b!=null)stash.guns.Remove(b);}}
            else if(m.category==1&&m.armor!=null){var a=ArmorInventory.Find(x=>x.id==m.armor.id);var b=stash.armor.Find(x=>x.id==m.armor.id);if(m.deposit){if(b==null)stash.armor.Add(a??m.armor);if(a!=null)ArmorInventory.Remove(a);}else{if(a==null)ArmorInventory.Add(b??m.armor);if(b!=null)stash.armor.Remove(b);}}
            else if(m.category==2&&m.module!=null){var a=ArtifactInventory.Find(x=>x.id==m.module.id);var b=stash.modules.Find(x=>x.id==m.module.id);if(m.deposit){if(b==null)stash.modules.Add(a??m.module);if(a!=null)ArtifactInventory.Remove(a);}else{if(a==null)ArtifactInventory.Add(b??m.module);if(b!=null)stash.modules.Remove(b);}}
            else if(m.category==3&&m.melee!=null){var a=MeleeWeaponInventory.Find(x=>x.id==m.melee.id);var b=stash.melee.Find(x=>x.id==m.melee.id);if(m.deposit){if(b==null)stash.melee.Add(a??m.melee);if(a!=null)MeleeWeaponInventory.Remove(a);}else{if(a==null)MeleeWeaponInventory.Add(b??m.melee);if(b!=null)stash.melee.Remove(b);}}
            else return;
            SelectedWeapon=Weapons.IndexOf(gun);SaveInventory();SaveProgression();SaveStash();if(!smoke){PlayerPrefs.DeleteKey(StashJournalKey);PlayerPrefs.Save();}
        }
        void HandleStashTouch(float contentHeight)
        {
            if(!MobileTouchControls.Enabled)return;
            Event e=Event.current;if(e==null)return;
            if(e.type==EventType.MouseDown&&new Rect(40,265,1360,565).Contains(e.mousePosition))
            {stashPointerDown=true;stashDragging=false;stashPointerStart=stashPointerLast=e.mousePosition;}
            else if(e.type==EventType.MouseDrag&&stashPointerDown)
            {
                if(!stashDragging&&Vector2.Distance(e.mousePosition,stashPointerStart)>=12)stashDragging=true;
                if(stashDragging){stashScroll.y=Mathf.Clamp(stashScroll.y-(e.mousePosition.y-stashPointerLast.y),0,Mathf.Max(0,contentHeight-565));GUIUtility.hotControl=0;e.Use();}
                stashPointerLast=e.mousePosition;
            }
            else if(e.type==EventType.MouseUp&&stashPointerDown)
            {stashPointerDown=false;if(stashDragging){GUIUtility.hotControl=0;e.Use();}stashDragging=false;}
        }
        public void DrawVaultStorage()
        {
            Box(new Rect(0,0,1440,900),PanelInk);Text(40,25,T("SAFEHOUSE STORAGE","拠点倉庫"),heading,Gold,1000);
            Text(45,90,StashCount+" / "+StashCapacity+T(" shared slots"," 共通枠"),label,Cyan,800);
            if(Button(new Rect(1160,30,240,60),T("CLOSE","閉じる"))){VaultStorageClose();return;}
            if(Button(new Rect(40,145,300,55),stashViewingStored?T("VIEW CARRIED","所持品を見る"):T("VIEW STORAGE","倉庫を見る"))){stashViewingStored=!stashViewingStored;stashScroll=Vector2.zero;}
            string[] names={T("GUNS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接")};
            for(int i=0;i<4;i++)if(Button(new Rect(365+i*245,145,230,55),names[i])){stashCategory=i;stashScroll=Vector2.zero;}
            Text(45,215,(stashViewingStored?T("STORAGE / WITHDRAW","倉庫 / 取り出す"):T("CARRIED / DEPOSIT","所持品 / 預ける"))+" — "+names[stashCategory],label,Gold,1250);
            var items=new List<object>();
            if(stashCategory==0){if(stashViewingStored)foreach(var x in stash.guns)items.Add(x);else foreach(var x in Weapons)items.Add(x);}
            if(stashCategory==1){if(stashViewingStored)foreach(var x in stash.armor)items.Add(x);else foreach(var x in ArmorInventory)if(Armory.IsActiveArmor(x))items.Add(x);}
            if(stashCategory==2){if(stashViewingStored)foreach(var x in stash.modules)items.Add(x);else foreach(var x in ArtifactInventory)items.Add(x);}
            if(stashCategory==3){if(stashViewingStored)foreach(var x in stash.melee)items.Add(x);else foreach(var x in MeleeWeaponInventory)items.Add(x);}
            HandleStashTouch(Mathf.Max(550,items.Count*88));
            stashScroll=GUI.BeginScrollView(new Rect(40,265,1360,565),stashScroll,new Rect(0,0,1320,Mathf.Max(550,items.Count*88)));
            for(int i=0;i<items.Count;i++){var x=items[i];float y=i*88;Panel(new Rect(0,y,1300,80),Cyan);string name=x is Gun g?VaultLanguage.GunName(g):x is ArmorItem a?Armory.Name(a):x is ArtifactItem m?m.Name:MeleeWeaponCatalog.Name((MeleeWeaponItem)x);Text(20,y+22,name,label,Gold,930);if(Button(new Rect(1020,y+12,250,55),stashViewingStored?T("WITHDRAW","取り出す"):T("DEPOSIT","預ける"))&&!MoveStash(x,!stashViewingStored))Notify("Cannot move: equipped, full, or duplicate item ID.","装備中、移動先が満杯、または装備IDが重複しています。");}
            GUI.EndScrollView();Text(45,845,T("Unequip gear before depositing it.","装備を外してから預けてください。"),small,Mist,1200);
        }
    }
}
