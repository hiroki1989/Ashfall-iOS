using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Per-actor material ownership; geometry, UVs and bind data remain untouched.
    public sealed class SharedArmorTextures : IDisposable
    {
        [Serializable] sealed class AtlasEntry { public string id, atlas; }
        [Serializable] sealed class AtlasCatalog { public AtlasEntry[] entries; }
        static Dictionary<string, string> atlasNames;
        readonly Dictionary<SkinnedMeshRenderer, Material[]> originals = new Dictionary<SkinnedMeshRenderer, Material[]>();
        readonly Dictionary<SkinnedMeshRenderer, string> assigned = new Dictionary<SkinnedMeshRenderer, string>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        // Inventory browsing can touch hundreds of atlases. Release an atlas once
        // every actor/preview has stopped using it, instead of retaining it on iOS.
        static readonly Dictionary<Texture2D, int> textureUsers = new Dictionary<Texture2D, int>();
        static readonly Dictionary<string, Texture2D> loadedAtlases = new Dictionary<string, Texture2D>();
        static readonly Dictionary<Texture2D, string> atlasKeys = new Dictionary<Texture2D, string>();

        readonly bool factionMobile;
        public SharedArmorTextures(bool factionMobile = false) { this.factionMobile = factionMobile; }

        static Texture2D LoadAtlas(string name, bool mobileFaction)
        {
            string key = mobileFaction ? "faction-mobile/" + name : name;
            if (loadedAtlases.TryGetValue(key, out var cached) && cached) return cached;
            string folder = Path.Combine(Application.streamingAssetsPath, "SharedArmorIllustration");
            string path = mobileFaction ? Path.Combine(folder, "FactionMobile", name + ".png") : Path.Combine(folder, name + ".png");
            if (mobileFaction && !File.Exists(path)) path = Path.Combine(folder, name + ".png");
            if (!File.Exists(path)) { Debug.LogError("Missing shared armor atlas: " + path); return null; }
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, true, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
                else UnityEngine.Object.DestroyImmediate(texture);
                Debug.LogError("Invalid shared armor atlas: " + path);
                return null;
            }
            texture.name = "Shared armor / " + name;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.wrapMode = TextureWrapMode.Clamp;
            loadedAtlases[key] = texture;
            atlasKeys[texture] = key;
            return texture;
        }

        static void Retain(Texture2D texture)
        {
            textureUsers.TryGetValue(texture, out int count);
            textureUsers[texture] = count + 1;
        }

        static void Release(Material material)
        {
            if (!material) return;
            var texture = material.mainTexture as Texture2D;
            if (texture && textureUsers.TryGetValue(texture, out int count))
            {
                if (count > 1) textureUsers[texture] = count - 1;
                else
                {
                    textureUsers.Remove(texture);
                    if (atlasKeys.TryGetValue(texture, out var key))
                    { atlasKeys.Remove(texture); loadedAtlases.Remove(key); }
                    if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
                    else UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            if (Application.isPlaying) UnityEngine.Object.Destroy(material);
            else UnityEngine.Object.DestroyImmediate(material);
        }

        static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "." || id == "..") return false;
            foreach (char c in id) if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')) return false;
            return true;
        }

        static string AtlasName(string id)
        {
            if (atlasNames == null)
            {
                atlasNames = new Dictionary<string, string>();
                var manifest = Resources.Load<TextAsset>("Art/SharedArmorIllustration/catalog");
                if (manifest)
                {
                    var catalog = JsonUtility.FromJson<AtlasCatalog>(manifest.text);
                    if (catalog?.entries != null) foreach (var entry in catalog.entries)
                        if (entry != null && ValidId(entry.id) && ValidId(entry.atlas))
                            atlasNames[entry.id] = entry.atlas;
                }
            }
            return atlasNames.TryGetValue(id, out var atlas) ? atlas : id;
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
                {
                    if (assigned.Remove(renderer)) renderer.sharedMaterials = source;
                    continue;
                }
                if (assigned.TryGetValue(renderer, out var previousId) && previousId == item.catalogId)
                    continue;
                // Each item owns an illustrated atlas in the approved Warden UV layout.
                // A palette LUT would retain the starter outfit's wrinkles and markings.
                var texture = LoadAtlas(AtlasName(item.catalogId), factionMobile && Application.isMobilePlatform);
                if (!texture) { renderer.sharedMaterials = source; assigned.Remove(renderer); continue; }
                var next = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    if (!source[i]) continue;
                    string key = item.catalogId + ":" + source[i].GetEntityId();
                    if (!materials.TryGetValue(key, out var material))
                    {
                        material = new Material(source[i]) { name = "Shared armor / " + item.catalogId };
                        material.mainTexture = texture;
                        material.SetTexture("_MainTex", texture);
                        Retain(texture);
                        materials.Add(key, material);
                    }
                    next[i] = material;
                }
                renderer.sharedMaterials = next;
                assigned[renderer] = item.catalogId;
            }
            // Only equipped materials stay owned; browsing many items must not retain
            // hundreds of atlas references on an iOS device.
            var active = new HashSet<Material>();
            for (int slot=1;slot<parts.Length&&slot<5;slot++)
                if(parts[slot]&&parts[slot].enabled)
                    foreach(var m in parts[slot].sharedMaterials)if(m)active.Add(m);
            var expired=new List<string>();
            foreach(var pair in materials)if(!active.Contains(pair.Value))expired.Add(pair.Key);
            foreach(var key in expired){var m=materials[key];materials.Remove(key);Release(m);}
            for (int slot=1;slot<parts.Length&&slot<5;slot++)
                if(parts[slot]&&!parts[slot].enabled)assigned.Remove(parts[slot]);
        }

        public void Dispose()
        {
            foreach (var entry in originals) if (entry.Key) entry.Key.sharedMaterials = entry.Value;
            foreach (var material in materials.Values) Release(material);
            materials.Clear(); originals.Clear(); assigned.Clear();
        }
    }
}



