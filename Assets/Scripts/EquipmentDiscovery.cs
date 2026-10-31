using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Product identity survives selling, discarding and moving an item to storage.
    internal static class EquipmentDiscovery
    {
        const string SaveKey = "Vault.EquipmentDiscovery.v1";
        [Serializable] sealed class Save { public string[] ids; }
        static readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        static bool loaded;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            try
            {
                var save = JsonUtility.FromJson<Save>(PlayerPrefs.GetString(SaveKey));
                if (save?.ids != null) foreach (var id in save.ids) if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
            catch (Exception e) { Debug.LogWarning("Equipment discovery load failed: " + e.GetType().Name); }
        }

        internal static string Key(object item)
        {
            string relic=item is Gun rg?rg.bossRelicId:item is ArmorItem ra?ra.bossRelicId:item is ArtifactItem rm?rm.bossRelicId:item is MeleeWeaponItem rw?rw.bossRelicId:null;
            if(BossRelicFoundation.Decode(relic,out _,out _))return relic;
            if (item is Gun gun)
            {
                int index = LegendaryCatalog.Index(gun.legendaryKey);
                return index >= 0 && index < DistinctEquipmentDesigns.Guns.Length
                    ? DistinctEquipmentDesigns.Guns[index].id : gun.catalogId;
            }
            if (item is ArmorItem armor)
            {
                int index = LegendaryCatalog.Index(armor.legendaryKey);
                return index >= 0 && index < DistinctEquipmentDesigns.Armor.Length
                    ? DistinctEquipmentDesigns.Armor[index].id : armor.catalogId;
            }
            if (item is ArtifactItem module) return module.catalogId;
            if (item is MeleeWeaponItem melee)
            {
                MeleeWeaponCatalog.InferIdentity(melee);
                return melee.catalogId + (melee.uniqueEffect ? ":legendary" : "");
            }
            return null;
        }

        internal static bool Known(object item)
        {
            Load(); string key = Key(item);
            return !string.IsNullOrEmpty(key) && ids.Contains(key);
        }
        internal static int CatalogCount()
        {
            Load();var catalog=new HashSet<string>(StringComparer.Ordinal);
            void Add(object item){string key=Key(item);if(!string.IsNullOrEmpty(key)&&ids.Contains(key))catalog.Add(key);}
            for(int i=0;i<Arsenal.CatalogCount;i++)Add(Arsenal.Catalog(i));
            foreach(var product in ArmorProductCatalog.All)if(Armory.IsActiveSlot(product.slot))Add(Armory.CreateCatalog(product.id,Rarity.Common,1));
            foreach(var product in ModuleProductCatalog.All)Add(ArtifactCatalog.CreateCatalog(product.id,product.legendary?Rarity.Legendary:Rarity.Common,1));
            for(int i=0;i<MeleeWeaponCatalog.CatalogCount;i++){Add(MeleeWeaponCatalog.Catalog(i));Add(MeleeWeaponCatalog.Catalog(i,Rarity.Legendary));}
            for(int boss=0;boss<BossRelicFoundation.BossCount;boss++)for(int category=0;category<BossRelicFoundation.CategoryCount;category++)if(ids.Contains(BossRelicFoundation.Id(boss,category)))catalog.Add(BossRelicFoundation.Id(boss,category));
            return catalog.Count;
        }

        internal static void Record(object item)
        {
            Load(); string key = Key(item);
            if (string.IsNullOrEmpty(key)) return;
            bool changed = ids.Add(key);
            if (!changed) return;
            var sorted = new List<string>(ids); sorted.Sort(StringComparer.Ordinal);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new Save { ids = sorted.ToArray() }));
            PlayerPrefs.Save();
        }

        internal static void RecordCurrent(IEnumerable<Gun> guns, IEnumerable<ArmorItem> armor,
            IEnumerable<ArtifactItem> modules, IEnumerable<MeleeWeaponItem> melee)
        {
            if (guns != null) foreach (var item in guns) Record(item);
            if (armor != null) foreach (var item in armor) Record(item);
            if (modules != null) foreach (var item in modules) Record(item);
            if (melee != null) foreach (var item in melee) Record(item);
        }
    }
}
