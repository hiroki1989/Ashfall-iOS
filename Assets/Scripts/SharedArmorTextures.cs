using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Per-actor material ownership; geometry, UVs and bind data remain untouched.
    public sealed class SharedArmorTextures : IDisposable
    {
        readonly Dictionary<SkinnedMeshRenderer, Material[]> originals = new Dictionary<SkinnedMeshRenderer, Material[]>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "." || id == "..") return false;
            foreach (char c in id) if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')) return false;
            return true;
        }

        public void Apply(SkinnedMeshRenderer[] parts, ArmorItem[] equipped)
        {
            if (parts == null) return;
            for (int slot = 1; slot < parts.Length && slot < 5; slot++)
            {
                var renderer = parts[slot];
                if (!renderer) continue;
                if (!originals.TryGetValue(renderer, out var source))
                { source = renderer.sharedMaterials; originals.Add(renderer, source); }
                ArmorItem item = null;
                if (equipped != null) foreach (var candidate in equipped)
                    if (candidate != null && (int)candidate.slot == slot) item = candidate;
                // Starter products keep the approved original atlas exactly.
                if (item == null || item.catalogId == ArmorProductCatalog.Get((ArmorSlot)slot, 4, 0).id || !ValidId(item.catalogId))
                { renderer.sharedMaterials = source; continue; }
                var texture = Resources.Load<Texture2D>("Art/SharedArmorLUT/" + item.catalogId);
                if (!texture) { renderer.sharedMaterials = source; continue; }
                var next = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    if (!source[i]) continue;
                    string key = item.catalogId + ":" + source[i].GetEntityId();
                    if (!materials.TryGetValue(key, out var material))
                    {
                        material = new Material(source[i]) { name = "Shared armor / " + item.catalogId };
                        material.shader=Resources.Load<Shader>("SharedArmorTexture"); material.SetTexture("_GradeLUT",texture); materials.Add(key, material);
                    }
                    next[i] = material;
                }
                renderer.sharedMaterials = next;
            }
            // Only equipped materials stay owned; browsing many items must not retain
            // hundreds of atlas references on an iOS device.
            var active = new HashSet<Material>();
            for (int slot=1;slot<parts.Length&&slot<5;slot++)
                if(parts[slot]&&parts[slot].enabled)
                    foreach(var m in parts[slot].sharedMaterials)if(m)active.Add(m);
            var expired=new List<string>();
            foreach(var pair in materials)if(!active.Contains(pair.Value))expired.Add(pair.Key);
            foreach(var key in expired){var m=materials[key];materials.Remove(key);if(m)UnityEngine.Object.Destroy(m);}
        }

        public void Dispose()
        {
            foreach (var entry in originals) if (entry.Key) entry.Key.sharedMaterials = entry.Value;
            foreach (var material in materials.Values) if (material)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            materials.Clear(); originals.Clear();
        }
    }
}



