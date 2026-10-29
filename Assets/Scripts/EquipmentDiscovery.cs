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

        internal static void Record(object item)
        {
            Load(); string key = Key(item);
            if (string.IsNullOrEmpty(key)) return;
            bool changed = ids.Add(key);
            if (item is MeleeWeaponItem melee && melee.uniqueEffect) changed |= ids.Add(melee.catalogId);
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
