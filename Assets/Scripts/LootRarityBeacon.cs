using UnityEngine;

namespace NeonVault
{
    // Lightweight additive light columns, shared meshes and materials across drops.
    public static class LootRarityBeacon
    {
        static Mesh columnMesh, groundMesh;
        static Material[] columns, grounds;

        static Mesh Column()
        {
            if (columnMesh) return columnMesh;
            var vertices = new Vector3[16];
            var uv = new Vector2[16];
            var triangles = new int[24];
            for (int plane = 0; plane < 4; plane++)
            {
                float angle = plane * Mathf.PI / 4f;
                Vector3 axis = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                int v = plane * 4, t = plane * 6;
                vertices[v] = -axis; vertices[v + 1] = axis;
                vertices[v + 2] = -axis + Vector3.up; vertices[v + 3] = axis + Vector3.up;
                uv[v] = new Vector2(0, 0); uv[v + 1] = new Vector2(1, 0);
                uv[v + 2] = new Vector2(0, 1); uv[v + 3] = new Vector2(1, 1);
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            columnMesh = new Mesh { name = "Loot rarity light column", vertices = vertices, uv = uv, triangles = triangles };
            columnMesh.RecalculateBounds();
            return columnMesh;
        }

        static Mesh Ground()
        {
            if (groundMesh) return groundMesh;
            const int segments = 24;
            var vertices = new Vector3[segments + 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            uv[0] = new Vector2(.5f, .5f);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                vertices[i + 1] = new Vector3(x, 0, z);
                uv[i + 1] = new Vector2(.5f + x * .5f, .5f + z * .5f);
                if (i == segments) continue;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2;
            }
            groundMesh = new Mesh { name = "Loot rarity ground glow", vertices = vertices, uv = uv, triangles = triangles };
            groundMesh.RecalculateBounds();
            return groundMesh;
        }

        static Material Paint(Rarity rarity, bool ground)
        {
            var cache = ground ? grounds : columns;
            if (cache == null) cache = new Material[Arsenal.Colors.Length];
            if (ground) grounds = cache; else columns = cache;
            int index = Mathf.Clamp((int)rarity, 0, cache.Length - 1);
            if (cache[index]) return cache[index];
            var shader = Resources.Load<Shader>("Shaders/LootRarityLight");
            if (!shader) return null;
            var material = new Material(shader) { name = "Loot rarity " + rarity + (ground ? " ground" : " beam") };
            Color color = Arsenal.Colors[index];
            color.a = index == 0 ? .55f : index == 1 ? .70f : .90f;
            material.SetColor("_Color", color);
            material.SetFloat("_Ground", ground ? 1 : 0);
            cache[index] = material;
            return material;
        }

        public static void Add(Transform root, Rarity rarity)
        {
            if (!root) return;
            var beamPaint = Paint(rarity, false);
            var groundPaint = Paint(rarity, true);
            if (!beamPaint || !groundPaint) return;
            int tier = Mathf.Clamp((int)rarity, 0, Arsenal.Colors.Length - 1);
            var beam = new GameObject("Loot rarity beam");
            beam.transform.SetParent(root, false);
            beam.transform.localPosition = Vector3.up * .14f;
            beam.transform.localScale = new Vector3(.10f + tier * .018f, 1.10f + tier * .28f, .10f + tier * .018f);
            beam.AddComponent<MeshFilter>().sharedMesh = Column();
            beam.AddComponent<MeshRenderer>().sharedMaterial = beamPaint;

            var halo = new GameObject("Loot rarity halo");
            halo.transform.SetParent(root, false);
            halo.transform.localPosition = Vector3.up * (.035f - root.position.y);
            halo.transform.localScale = Vector3.one * (.38f + tier * .055f);
            halo.AddComponent<MeshFilter>().sharedMesh = Ground();
            halo.AddComponent<MeshRenderer>().sharedMaterial = groundPaint;
        }
    }
}
