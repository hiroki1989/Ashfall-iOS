using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Sleeveless products share one tailored vest mesh. It is sculpted once
    // from the approved chest's skinned surface, retaining the exact bindpose
    // and atlas UVs; the equipped product supplies its own illustrated atlas.
    public sealed class SharedArmorSleeves : IDisposable
    {
        readonly Dictionary<SkinnedMeshRenderer, Mesh> originals = new Dictionary<SkinnedMeshRenderer, Mesh>();
        readonly Dictionary<SkinnedMeshRenderer, Mesh> sleeveless = new Dictionary<SkinnedMeshRenderer, Mesh>();

        public static bool IsSleeveless(ArmorItem item)
        {
            if (item == null || item.slot != ArmorSlot.Chest) return false;
            // Regular illustration families are fixed by maker. Voltarc,
            // Mireline, Cinder and Aegis regular chest art has open armholes
            // or short shoulder caps. Bespoke legendary art uses its own cut.
            // The starter Warden jacket also keeps its approved long sleeves.
            if (string.IsNullOrEmpty(item.catalogId)
                || !item.catalogId.StartsWith("distinct.armor.", StringComparison.Ordinal)) return false;
            var recipe = DistinctEquipmentDesigns.For(item);
            if (recipe == null || recipe.slot != (int)ArmorSlot.Chest || recipe.legendary) return false;
            return recipe.maker >= 2 && recipe.maker <= 5;
        }

        // Call after GeneratedCommonBodyRig.SetCoveredSlots and after the armor
        // slot renderer has been enabled. The caller owns the equipment cycle.
        public bool Apply(SkinnedMeshRenderer chest, SkinnedMeshRenderer body,
            DetailedHero3D hero, ArmorItem[] equipped)
        {
            if (!chest || !chest.sharedMesh) return false;
            ArmorItem item = null;
            if (equipped != null) foreach (var candidate in equipped)
                if (candidate != null && candidate.slot == ArmorSlot.Chest) item = candidate;
            bool open = chest.enabled && IsSleeveless(item);
            if (!originals.TryGetValue(chest, out var source))
            {
                source = chest.sharedMesh;
                originals.Add(chest, source);
            }
            if (open && !sleeveless.TryGetValue(chest, out var bare))
            {
                bare = BuildSleevelessVest(source);
                if (bare) sleeveless.Add(chest, bare);
                else open = false;
            }
            var chosen = open ? sleeveless[chest] : source;
            if (chest.sharedMesh != chosen) chest.sharedMesh = chosen;
            SetBaseArms(body, hero, open);
            return open;
        }

        static Mesh BuildSleevelessVest(Mesh source)
        {
            if (!source.isReadable) { Debug.LogError("Sleeveless vest needs a readable chest mesh"); return null; }
            var weights = source.boneWeights;
            var coords = new List<Vector4>();
            source.GetUVs(2, coords); // Warden source position, retained by SplitArmorBySlot.
            if (weights.Length != source.vertexCount || coords.Count != source.vertexCount)
            { Debug.LogError("Sleeveless vest lacks Warden weights/source coordinates"); return null; }
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = "Common sleeveless vest / fitted to Warden skeleton";
            var points = source.vertices;
            // Re-form the outer garment into a shorter, tapered vest. The
            // character's bare sleeves remain a separate, animated body mesh.
            // Shaping the existing scan surface preserves every product's
            // front/back illustration projection without blurry UV transfer.
            for (int i = 0; i < points.Length; i++)
            {
                Vector4 sourcePoint = coords[i];
                float arm = ArmWeight(weights[i]);
                if (arm > .32f || sourcePoint.y < 1.22f || sourcePoint.y > 1.72f) continue;
                Vector3 p = points[i];
                float upper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.26f, 1.64f, sourcePoint.y));
                // The waist closes against the trouser belt; the upper ribs
                // flare into a practical plate carrier without a coat skirt.
                p.x *= Mathf.Lerp(.94f, 1.035f, upper);
                if (p.z > .015f)
                {
                    float bib = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .05f, Mathf.Abs(sourcePoint.x)))
                        * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.26f, 1.40f, sourcePoint.y))
                        * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.57f, 1.68f, sourcePoint.y)));
                    p.z += .012f + .017f * bib;
                    // Fold the old high neck down into a shallow V rather
                    // than deleting cloth and exposing a hole below the head.
                    float throat = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .025f, Mathf.Abs(sourcePoint.x)))
                        * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.57f, 1.69f, sourcePoint.y));
                    p.y -= .027f * throat;
                    p.z -= .012f * throat;
                }
                else p.z -= .008f;
                points[i] = p;
            }
            mesh.vertices = points;
            int removed = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var input = source.GetTriangles(sub);
                var output = new List<int>(input.Length);
                for (int i = 0; i + 2 < input.Length; i += 3)
                {
                    int a = input[i], b = input[i + 1], c = input[i + 2];
                    Vector3 p = (new Vector3(coords[a].x, coords[a].y, coords[a].z)
                        + new Vector3(coords[b].x, coords[b].y, coords[b].z)
                        + new Vector3(coords[c].x, coords[c].y, coords[c].z)) / 3f;
                    float arm = (ArmWeight(weights[a]) + ArmWeight(weights[b]) + ArmWeight(weights[c])) / 3f;
                    // Cut the shoulder/armhole at the true arm influence, not
                    // at an X coordinate: the opening stays attached to the
                    // same joint while walking and aiming.
                    if (p.y >= .76f && p.y <= 1.72f && arm > .40f)
                    { removed++; continue; }
                    output.Add(a); output.Add(b); output.Add(c);
                }
                mesh.SetTriangles(output, sub, false);
            }
            if (removed == 0)
            {
                DestroyMesh(mesh);
                Debug.LogError("Sleeveless vest had no sleeve triangles to replace");
                return null;
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        static float ArmWeight(BoneWeight w)
        {
            float value = 0f;
            if (w.boneIndex0 >= 3 && w.boneIndex0 <= 8) value += w.weight0;
            if (w.boneIndex1 >= 3 && w.boneIndex1 <= 8) value += w.weight1;
            if (w.boneIndex2 >= 3 && w.boneIndex2 <= 8) value += w.weight2;
            if (w.boneIndex3 >= 3 && w.boneIndex3 <= 8) value += w.weight3;
            return value;
        }

        static void SetBaseArms(SkinnedMeshRenderer body, DetailedHero3D hero, bool visible)
        {
            if (body && GeneratedCommonBodyRig.ReplacesLegacyArms(body))
            {
                // Common-body arms are already partitioned into Chest sleeve
                // and Hands skin. Only restore the portion hidden by the chest.
                foreach (var renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (renderer && renderer.name.EndsWith(" / Chest sleeve", StringComparison.Ordinal))
                        renderer.enabled = visible || renderer.enabled;
            }
            else if (hero)
            {
                foreach (var renderer in hero.GetComponentsInChildren<Renderer>(true))
                    if (GeneratedCommonBodyRig.IsLegacyArmRenderer(renderer, hero)
                        && GeneratedWardenRig.IsBaseSleeve(renderer))
                        renderer.enabled = visible || renderer.enabled;
            }
        }

        static void DestroyMesh(Mesh mesh)
        {
            if (!mesh) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
            else UnityEngine.Object.DestroyImmediate(mesh);
        }

        public void Dispose()
        {
            foreach (var entry in originals) if (entry.Key) entry.Key.sharedMesh = entry.Value;
            foreach (var mesh in sleeveless.Values) DestroyMesh(mesh);
            sleeveless.Clear(); originals.Clear();
        }
    }
}
