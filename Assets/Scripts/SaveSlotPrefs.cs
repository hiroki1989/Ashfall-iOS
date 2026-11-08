using UnityEngine;

namespace NeonVault
{
    // Slot 1 retains the original preference keys so old saves never need rewriting.
    // Device settings and the once-per-day reward limit are shared by all slots.
    public static class SaveSlotPrefs
    {
        public const int Count=3;
        public static int Current { get; private set; } = 1;
        public static bool Selected { get; private set; }
        static bool Shared(string key) => !key.StartsWith("Vault.",System.StringComparison.Ordinal) || key=="Vault.Language" || key.StartsWith("Vault.DailyChest.",System.StringComparison.Ordinal) || key.StartsWith("Vault.Generated",System.StringComparison.Ordinal);
        static string Key(int slot,string key) => slot==1||Shared(key)?key:"Vault.SaveSlot."+slot+"."+key;
        static string Key(string key)=>Key(Current,key);
        public static bool Exists(int slot)
        {
            if(slot<1||slot>Count)return false;
            return UnityEngine.PlayerPrefs.HasKey(Key(slot,"Vault.Loadout.v1")) || UnityEngine.PlayerPrefs.HasKey(Key(slot,"Vault.Progression.v1")) || UnityEngine.PlayerPrefs.HasKey(Key(slot,"Vault.ExpeditionCheckpoint.v1"));
        }
        public static bool SlotHasCheckpoint(int slot)=>slot>=1&&slot<=Count&&UnityEngine.PlayerPrefs.HasKey(Key(slot,"Vault.ExpeditionCheckpoint.v1"));
        public static void DeleteSlot(int slot)
        {
            if(slot<1||slot>Count)return;
            string prefix=slot==1?"":"Vault.SaveSlot."+slot+".";
            string[] knownKeys = {
                "Vault.Progression.v1", "Vault.Loadout.v1", "Vault.DebtSkills.v1", "Vault.ExpeditionCheckpoint.v1",
                "Vault.Overworld.v1", "Vault.ActionSkills.v1", "Vault.EquipmentDiscovery.v1", "Vault.ActionSkillTutorial.v1",
                "Vault.LegendaryWeaponTutorial.v1", "Vault.FirstSteps.v1", "Vault.FirstSteps.target.v1", "Vault.DebtBoss.defeated.v1",
                "Vault.Cores", "Vault.Rank", "Vault.FirstBossLegendaryD0", "Vault.SeraIntro", "Vault.BriggsIntro",
                "Vault.Pet.Adopted.v1", "Vault.Pet.IonaIntro.v1", "Vault.PetSkills.v1.OverworldCooldown",
                "Vault.Junk.v1", "Vault.Stash.v1", "Vault.Stash.Move.v1", "Vault.Stash.Upgrade.v1"
            };
            foreach(var k in knownKeys) UnityEngine.PlayerPrefs.DeleteKey(prefix+k);
            for(int i=0;i<300;i++)
            {
                UnityEngine.PlayerPrefs.DeleteKey(prefix+"Vault.FactionBoss.Intro."+i);
                UnityEngine.PlayerPrefs.DeleteKey(prefix+"Vault.DungeonVisited."+i);
            }
            for(int b=0;b<10;b++) UnityEngine.PlayerPrefs.DeleteKey(prefix+"Vault.BossRelic.Defeated."+b);
            for(int t=0;t<3;t++) for(int s=0;s<10;s++)
            {
                UnityEngine.PlayerPrefs.DeleteKey(prefix+"Vault.PetSkills.v1.T"+t+".S"+s+".Unlocked");
                UnityEngine.PlayerPrefs.DeleteKey(prefix+"Vault.PetSkills.v1.T"+t+".S"+s+".Rank");
            }
            EquipmentDiscovery.ResetSlotCache();
            UnityEngine.PlayerPrefs.Save();
        }
        public static void Select(int slot)
        {
            if(slot<1||slot>Count)throw new System.ArgumentOutOfRangeException(nameof(slot));
            Current=slot;Selected=true;EquipmentDiscovery.ResetSlotCache();
        }
        public static bool HasKey(string key)=>UnityEngine.PlayerPrefs.HasKey(Key(key));
        public static int GetInt(string key,int fallback=0)=>UnityEngine.PlayerPrefs.GetInt(Key(key),fallback);
        public static float GetFloat(string key,float fallback=0)=>UnityEngine.PlayerPrefs.GetFloat(Key(key),fallback);
        public static string GetString(string key,string fallback="")=>UnityEngine.PlayerPrefs.GetString(Key(key),fallback);
        public static void SetInt(string key,int value)=>UnityEngine.PlayerPrefs.SetInt(Key(key),value);
        public static void SetFloat(string key,float value)=>UnityEngine.PlayerPrefs.SetFloat(Key(key),value);
        public static void SetString(string key,string value)=>UnityEngine.PlayerPrefs.SetString(Key(key),value);
        public static void DeleteKey(string key)=>UnityEngine.PlayerPrefs.DeleteKey(Key(key));
        public static void Save()=>UnityEngine.PlayerPrefs.Save();
    }
}
