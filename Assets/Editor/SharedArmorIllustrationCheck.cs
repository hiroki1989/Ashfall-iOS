using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor
{
    public static class SharedArmorIllustrationCheck
    {
        [Serializable] sealed class Entry { public string id, atlas; }
        [Serializable] sealed class Catalog { public Entry[] entries; }
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = Resources.Load<TextAsset>("Art/SharedArmorIllustration/catalog");
            if (!manifest) throw new Exception("Missing shared armor catalog");
            var catalog = JsonUtility.FromJson<Catalog>(manifest.text);
            if (catalog?.entries == null) throw new Exception("Invalid shared armor catalog");
            var lookup = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var entry in catalog.entries)
                if (entry != null && !string.IsNullOrEmpty(entry.id) && !string.IsNullOrEmpty(entry.atlas))
                    lookup.Add(entry.id, entry.atlas);
            int checkedCount = 0;
            foreach (var product in ArmorProductCatalog.All)
            {
                if (product.slot == ArmorSlot.Head) continue;
                if (!lookup.TryGetValue(product.id, out var atlas))
                    throw new Exception("Missing armor mapping: " + product.id);
                var path = Path.Combine(Application.streamingAssetsPath, "SharedArmorIllustration", atlas + ".png");
                if (!File.Exists(path)) throw new Exception("Missing armor atlas: " + path);
                checkedCount++;
            }
            if (checkedCount != 398) throw new Exception("Expected 398 armor atlases, found " + checkedCount);
            if (new System.Collections.Generic.HashSet<string>(lookup.Values).Count != 168)
                throw new Exception("Expected 168 unique illustration atlases");
            foreach (var atlas in new System.Collections.Generic.HashSet<string>(lookup.Values))
            {
                var path = Path.Combine(Application.streamingAssetsPath, "SharedArmorIllustration", atlas + ".png");
                using (var stream = File.OpenRead(path))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, header.Length) != header.Length ||
                        header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71 ||
                        header[16] != 0 || header[17] != 0 || header[18] != 4 || header[19] != 0 ||
                        header[20] != 0 || header[21] != 0 || header[22] != 4 || header[23] != 0)
                        throw new Exception("Invalid 1024x1024 PNG: " + path);
                }
            }
            // Exercise the same material/texture path used by inventory and combat.
            var shader = Resources.Load<Shader>("GeneratedWardenTexture");
            if (!shader) throw new Exception("Missing Warden shader");
            var source = new Material(shader);
            var host = new GameObject("Shared armor atlas check");
            var renderer = host.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMaterial = source;
            var parts = new SkinnedMeshRenderer[5];
            parts[(int)ArmorSlot.Chest] = renderer;
            var equipped = new ArmorItem[5];
            using (var runtime = new SharedArmorTextures())
            {
                equipped[(int)ArmorSlot.Chest] = Armory.CreateCatalog("distinct.armor.001", Rarity.Common, 1);
                runtime.Apply(parts, equipped);
                var first = renderer.sharedMaterial.mainTexture;
                if (!first || renderer.sharedMaterial.shader != shader)
                    throw new Exception("First chest atlas was not applied with the original shader");
                equipped[(int)ArmorSlot.Chest] = Armory.CreateCatalog("distinct.armor.006", Rarity.Common, 1);
                runtime.Apply(parts, equipped);
                var second = renderer.sharedMaterial.mainTexture;
                if (!second || second == first || renderer.sharedMaterial.shader != shader)
                    throw new Exception("Second chest atlas did not replace the first");
            }
            if (renderer.sharedMaterial != source) throw new Exception("Original chest material was not restored");
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(source);
            Directory.CreateDirectory("D:/Unity/AshfallArtValidation/shared-armor/illustration-qa");
            File.WriteAllText("D:/Unity/AshfallArtValidation/shared-armor/illustration-qa/unity-import-result.txt",
                "SHARED_ARMOR_ILLUSTRATION_IMPORT_OK products=" + checkedCount + " atlases=168" + Environment.NewLine);
            Debug.Log("SHARED_ARMOR_ILLUSTRATION_IMPORT_OK products=" + checkedCount + " atlases=168");
        }
    }
}
