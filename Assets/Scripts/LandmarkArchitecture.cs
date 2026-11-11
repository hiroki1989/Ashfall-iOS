using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    /// <summary>
    /// High-detail procedural architectural & industrial mesh generator and prototype builder
    /// for Overworld Landmarks (all 23 kinds / 30 sites) and City Pocket Parks.
    /// Built once per archetype and rendered via GPU-instanced ImportedWorldBatch.
    /// </summary>
    public static class LandmarkArchitecture
    {
        static readonly Dictionary<int, GameObject> landmarkPrototypes = new Dictionary<int, GameObject>();
        static readonly Dictionary<int, GameObject> parkPrototypes = new Dictionary<int, GameObject>();
        static Material[] palette;
        static Mesh cubeMesh, sphereMesh, cylinderMesh, coneMesh, octagonPrismMesh, archRingMesh, dishMesh, hyperboloidMesh;

        // Palette indices:
        // 0: Weathered Limestone / Concrete (Light Warm Gray)
        // 1: Aged Red Brick / Terracotta Masonry
        // 2: Dark Structural Steel / Cast Iron
        // 3: Verdigris Patina Copper / Bronze Dome
        // 4: Rusted Corten Steel / Industrial Oxide
        // 5: Polished Obsidian / Dark Basalt Monolith
        // 6: Sandstone / Plaza Travertine Trim
        // 7: Glowing Cyan / Amber Beacon Glass & Clock Dials
        // 8: Mossy Stone / Overgrown Sanctuary Masonry
        // 9: Tinted Conservatory / Cathedral Glass
        static void Prepare()
        {
            if (palette != null && palette[0] != null) return;
            var shader = ImportedWorldAssets.BuiltinStandard();
            string[] surfaceNames =
            {
                "M_YFCM_BrushedWorn",
                "M_YFCM_RevealingBricks",
                "M_YFCM_Spalling",
                "M_YFCM_Plastered",
                "M_YFCM_RevealingBricks",
                "M_YFCM_Slab1x1",
                "M_YFPM_BrokenRectangles",
                null,
                "M_grnd_grass",
                null
            };
            Color[] colors =
            {
                new Color(.62f, .60f, .55f), // 0: Limestone
                new Color(.52f, .31f, .24f), // 1: Brick
                new Color(.20f, .22f, .24f), // 2: Dark Steel
                new Color(.26f, .48f, .42f), // 3: Verdigris Copper
                new Color(.46f, .25f, .16f), // 4: Rusted Steel
                new Color(.12f, .13f, .16f), // 5: Obsidian
                new Color(.70f, .64f, .52f), // 6: Travertine
                new Color(.96f, .88f, .62f), // 7: Luminous Dial / Beacon
                new Color(.34f, .42f, .30f), // 8: Mossy Stone
                new Color(.32f, .54f, .62f)  // 9: Conservatory Glass
            };
            float[] gloss = { .12f, .08f, .32f, .28f, .14f, .48f, .15f, .65f, .08f, .58f };
            float[] metal = { 0f, 0f, .55f, .35f, .40f, .20f, 0f, .10f, 0f, .15f };

            palette = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var mat = new Material(shader) { name = "LandmarkArch / " + i };
                if (surfaceNames[i] != null)
                {
                    var src = Resources.Load<Material>("ImportedWorld/Surfaces/" + surfaceNames[i]);
                    if (src != null) mat.CopyPropertiesFromMaterial(src);
                }
                mat.shader = shader;
                mat.color = colors[i];
                mat.SetFloat("_Glossiness", gloss[i]);
                mat.SetFloat("_Metallic", metal[i]);
                if (i == 7)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(.85f, .72f, .38f));
                }
                else if (i == 9)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(.10f, .22f, .26f));
                }
                mat.enableInstancing = true;
                palette[i] = mat;
            }

            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = b.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(b);

            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = s.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(s);

            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderMesh = c.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(c);

            coneMesh = CreateConeMesh(16, .5f, 1f, 0f);
            octagonPrismMesh = CreateConeMesh(8, .5f, 1f, .5f);
            archRingMesh = CreateHalfTorusMesh(18, 12, 1f, .18f);
            dishMesh = CreateParabolicDishMesh(20, 8, 1f, .38f);
            hyperboloidMesh = CreateHyperboloidMesh(20, 12, .5f, .33f, .40f, 1f);
        }

        static Mesh CreateConeMesh(int sides, float bottomRadius, float height, float topRadius)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            float halfH = height * .5f;
            for (int i = 0; i <= sides; i++)
            {
                float u = i / (float)sides;
                float a = u * Mathf.PI * 2f;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                Vector3 bot = new Vector3(ca * bottomRadius, -halfH, sa * bottomRadius);
                Vector3 top = new Vector3(ca * topRadius, halfH, sa * topRadius);
                Vector3 norm = new Vector3(ca * height, bottomRadius - topRadius, sa * height).normalized;
                v.Add(bot); v.Add(top);
                n.Add(norm); n.Add(norm);
                uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, 1));
                if (i < sides)
                {
                    int b0 = i * 2, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
                    t.Add(b0); t.Add(t0); t.Add(t1);
                    t.Add(b0); t.Add(t1); t.Add(b1);
                }
            }
            // Bottom cap
            int botCenter = v.Count;
            v.Add(new Vector3(0, -halfH, 0)); n.Add(Vector3.down); uv.Add(new Vector2(.5f, .5f));
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                int idx = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * bottomRadius, -halfH, Mathf.Sin(a0) * bottomRadius));
                v.Add(new Vector3(Mathf.Cos(a1) * bottomRadius, -halfH, Mathf.Sin(a1) * bottomRadius));
                n.Add(Vector3.down); n.Add(Vector3.down);
                uv.Add(Vector2.zero); uv.Add(Vector2.one);
                t.Add(botCenter); t.Add(idx); t.Add(idx + 1);
            }
            if (topRadius > .001f)
            {
                int topCenter = v.Count;
                v.Add(new Vector3(0, halfH, 0)); n.Add(Vector3.up); uv.Add(new Vector2(.5f, .5f));
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                    int idx = v.Count;
                    v.Add(new Vector3(Mathf.Cos(a0) * topRadius, halfH, Mathf.Sin(a0) * topRadius));
                    v.Add(new Vector3(Mathf.Cos(a1) * topRadius, halfH, Mathf.Sin(a1) * topRadius));
                    n.Add(Vector3.up); n.Add(Vector3.up);
                    uv.Add(Vector2.zero); uv.Add(Vector2.one);
                    t.Add(topCenter); t.Add(idx + 1); t.Add(idx);
                }
            }
            var m = new Mesh { name = "ProceduralConePrism" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        static Mesh CreateHalfTorusMesh(int majorSegs, int minorSegs, float majorR, float minorR)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int i = 0; i <= majorSegs; i++)
            {
                float u = i / (float)majorSegs;
                float theta = u * Mathf.PI;
                float ct = Mathf.Cos(theta), st = Mathf.Sin(theta);
                Vector3 center = new Vector3(ct * majorR, st * majorR, 0);
                for (int j = 0; j <= minorSegs; j++)
                {
                    float w = j / (float)minorSegs;
                    float phi = w * Mathf.PI * 2f;
                    float cp = Mathf.Cos(phi), sp = Mathf.Sin(phi);
                    Vector3 normal = new Vector3(ct * cp, st * cp, sp).normalized;
                    v.Add(center + normal * minorR);
                    n.Add(normal);
                    uv.Add(new Vector2(u, w));
                }
            }
            int ring = minorSegs + 1;
            for (int i = 0; i < majorSegs; i++)
                for (int j = 0; j < minorSegs; j++)
                {
                    int a = i * ring + j, b = (i + 1) * ring + j, c = b + 1, d = a + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(a); t.Add(c); t.Add(d);
                }
            var m = new Mesh { name = "ProceduralArchTorus" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        static Mesh CreateParabolicDishMesh(int radialSegs, int rings, float radius, float depth)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float rf = r / (float)rings;
                float curR = rf * radius;
                float y = rf * rf * depth;
                for (int s = 0; s <= radialSegs; s++)
                {
                    float sf = s / (float)radialSegs;
                    float a = sf * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * curR, y, Mathf.Sin(a) * curR));
                    uv.Add(new Vector2(sf, rf));
                }
            }
            int stride = radialSegs + 1;
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < radialSegs; s++)
                {
                    int a = r * stride + s, b = (r + 1) * stride + s, c = b + 1, d = a + 1;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(a); t.Add(d); t.Add(c);
                    // Backface
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(a); t.Add(c); t.Add(d);
                }
            var m = new Mesh { name = "ProceduralParabolicDish" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Mesh CreateHyperboloidMesh(int sides, int rings, float rBot, float rWaist, float rTop, float height)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float yf = r / (float)rings;
                float y = (yf - .5f) * height;
                float waistT = (yf - .62f);
                float rad = yf < .62f
                    ? Mathf.Lerp(rBot, rWaist, Mathf.Sin(yf / .62f * Mathf.PI * .5f))
                    : Mathf.Lerp(rWaist, rTop, (yf - .62f) / .38f);
                for (int s = 0; s <= sides; s++)
                {
                    float sf = s / (float)sides;
                    float a = sf * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad));
                    uv.Add(new Vector2(sf * 2f, yf * 2f));
                }
            }
            int stride = sides + 1;
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * stride + s, b = (r + 1) * stride + s, c = b + 1, d = a + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(a); t.Add(c); t.Add(d);
                }
            var m = new Mesh { name = "ProceduralHyperboloid" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        public static void PlaceLandmark(Transform chunkTransform, Vector3 position, int kind, int seed)
        {
            Prepare();
            var proto = GetLandmarkPrototype(kind);
            if (proto != null)
                ImportedWorldBatch.Add(chunkTransform, proto, position, Quaternion.identity, Vector3.one, true);
            DressLandmarkSurroundings(chunkTransform, position, kind, seed);
        }

        public static void PlacePocketPark(Transform chunkTransform, Vector3 centre, int variant)
        {
            Prepare();
            int v = Mathf.Abs(variant) % 4;
            var proto = GetParkPrototype(v);
            if (proto != null)
                ImportedWorldBatch.Add(chunkTransform, proto, centre, Quaternion.identity, Vector3.one, false);
            DressParkSurroundings(chunkTransform, centre, v);
        }

        static GameObject GetParkPrototype(int variant)
        {
            if (parkPrototypes.TryGetValue(variant, out var existing) && existing != null) return existing;
            var root = new GameObject("PocketParkPrototype_" + variant);
            root.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(root);

            var batches = new List<CombineInstance>[palette.Length];
            for (int i = 0; i < batches.Length; i++) batches[i] = new List<CombineInstance>();

            void AddPart(Mesh m, Vector3 pos, Vector3 scale, int mat, Quaternion rot)
            {
                batches[mat].Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, rot, scale) });
            }
            void Box(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(cubeMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Cyl(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(cylinderMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Sph(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(sphereMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Cone(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(coneMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Oct(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(octagonPrismMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));

            // Paved circular stone plaza & radial walkways
            Oct(new Vector3(0, .03f, 0), new Vector3(17.5f, .06f, 17.5f), 6);
            Oct(new Vector3(0, .065f, 0), new Vector3(14.2f, .05f, 14.2f), 0, 22.5f);
            for (int path = 0; path < 4; path++)
            {
                float a = path * 90f;
                Box(new Vector3(0, .025f, 0), new Vector3(26f, .05f, 2.8f), 6, a);
                Box(new Vector3(0, .035f, 0), new Vector3(26f, .05f, 2.1f), 0, a);
            }

            // Corner raised stone planters with ornamental shrubs & low stone walls
            for (int corner = 0; corner < 4; corner++)
            {
                float a = corner * Mathf.PI * .5f + Mathf.PI * .25f;
                Vector3 cp = new Vector3(Mathf.Cos(a) * 8.4f, 0, Mathf.Sin(a) * 8.4f);
                Oct(cp + Vector3.up * .28f, new Vector3(3.8f, .56f, 3.8f), 6, corner * 45f);
                Oct(cp + Vector3.up * .58f, new Vector3(3.3f, .14f, 3.3f), 8, corner * 45f);
                Sph(cp + Vector3.up * .95f, new Vector3(2.4f, 1.1f, 2.4f), 8);
                Sph(cp + Vector3.up * 1.45f, new Vector3(1.5f, .9f, 1.5f), 3);
            }

            // Ornamental perimeter iron bollards & chains
            for (int post = 0; post < 16; post++)
            {
                if (post % 4 == 0) continue; // Keep radial paths open
                float a = post * Mathf.PI * 2f / 16f;
                Vector3 pp = new Vector3(Mathf.Cos(a) * 12.2f, 0, Mathf.Sin(a) * 12.2f);
                Cyl(pp + Vector3.up * .48f, new Vector3(.22f, .48f, .22f), 2);
                Sph(pp + Vector3.up * 1.02f, new Vector3(.28f, .28f, .28f), 3);
            }

            if (variant == 0)
            {
                // Variant 0: Classical Octagonal Gazebo / Bandstand
                Oct(new Vector3(0, .28f, 0), new Vector3(7.4f, .56f, 7.4f), 0);
                Oct(new Vector3(0, .60f, 0), new Vector3(6.8f, .14f, 6.8f), 6);
                for (int col = 0; col < 8; col++)
                {
                    float a = col * Mathf.PI * .25f + Mathf.PI * .125f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * 2.85f, 0, Mathf.Sin(a) * 2.85f);
                    Cyl(p + Vector3.up * 2.15f, new Vector3(.26f, 1.55f, .26f), 0);
                    Box(p + Vector3.up * .75f, new Vector3(.38f, .30f, .38f), 6);
                    Box(p + Vector3.up * 3.65f, new Vector3(.38f, .26f, .38f), 6);
                }
                Oct(new Vector3(0, 3.85f, 0), new Vector3(7.6f, .28f, 7.6f), 2);
                Cone(new Vector3(0, 4.85f, 0), new Vector3(8.0f, 1.85f, 8.0f), 3);
                Cone(new Vector3(0, 6.05f, 0), new Vector3(2.4f, .95f, 2.4f), 3);
                Cyl(new Vector3(0, 6.75f, 0), new Vector3(.12f, .55f, .12f), 2);
                Sph(new Vector3(0, 7.35f, 0), new Vector3(.35f, .35f, .35f), 7);
            }
            else if (variant == 1)
            {
                // Variant 1: Grand Multi-Tiered Civic Fountain
                Oct(new Vector3(0, .35f, 0), new Vector3(7.8f, .70f, 7.8f), 0);
                Oct(new Vector3(0, .48f, 0), new Vector3(6.8f, .55f, 6.8f), 9);
                Cyl(new Vector3(0, 1.65f, 0), new Vector3(1.35f, 1.25f, 1.35f), 0);
                Cyl(new Vector3(0, 2.65f, 0), new Vector3(4.2f, .22f, 4.2f), 6);
                Cyl(new Vector3(0, 2.82f, 0), new Vector3(3.6f, .18f, 3.6f), 9);
                Cyl(new Vector3(0, 3.65f, 0), new Vector3(.75f, .85f, .75f), 0);
                Cyl(new Vector3(0, 4.35f, 0), new Vector3(2.1f, .18f, 2.1f), 6);
                Cone(new Vector3(0, 5.15f, 0), new Vector3(.85f, 1.45f, .85f), 3);
                Sph(new Vector3(0, 5.95f, 0), new Vector3(.48f, .48f, .48f), 7);
            }
            else if (variant == 2)
            {
                // Variant 2: Equestrian / Heroic Bronze Memorial Statue & Pergola Arches
                Box(new Vector3(0, .45f, 0), new Vector3(4.2f, .90f, 4.2f), 6);
                Box(new Vector3(0, 1.45f, 0), new Vector3(2.8f, 1.25f, 2.8f), 0);
                Box(new Vector3(0, 2.15f, 0), new Vector3(3.1f, .24f, 3.1f), 6);
                // Abstract bronze guardian figure on pedestal
                Cyl(new Vector3(0, 3.25f, 0), new Vector3(.85f, 1.05f, .65f), 3);
                Sph(new Vector3(0, 4.55f, 0), new Vector3(.62f, .62f, .62f), 3);
                Box(new Vector3(.55f, 3.95f, 0), new Vector3(.22f, 1.65f, .22f), 2, 18f);
                // 4 surrounding stone pergola arches
                for (int arch = 0; arch < 4; arch++)
                {
                    float a = arch * 90f + 45f;
                    Quaternion r = Quaternion.Euler(0, a, 0);
                    Vector3 dir = r * Vector3.forward;
                    Vector3 right = r * Vector3.right;
                    Vector3 center = dir * 5.2f;
                    Cyl(center - right * 1.4f + Vector3.up * 1.6f, new Vector3(.32f, 1.6f, .32f), 0);
                    Cyl(center + right * 1.4f + Vector3.up * 1.6f, new Vector3(.32f, 1.6f, .32f), 0);
                    AddPart( cubeMesh, center + Vector3.up * 3.35f, new Vector3(3.6f, .28f, .55f), 6, r);
                }
            }
            else
            {
                // Variant 3: Sunken Botanical Conservatory Pavilion & Sundial Obelisk
                Oct(new Vector3(0, .24f, 0), new Vector3(7.2f, .48f, 7.2f), 8);
                Sph(new Vector3(0, 1.65f, 0), new Vector3(5.6f, 3.6f, 5.6f), 9);
                for (int rib = 0; rib < 4; rib++)
                {
                    AddPart(archRingMesh, new Vector3(0, .35f, 0), new Vector3(2.85f, 2.85f, .95f), 2, Quaternion.Euler(0, rib * 45f, 0));
                }
                Cone(new Vector3(0, 4.15f, 0), new Vector3(1.2f, 1.4f, 1.2f), 3);
                Sph(new Vector3(0, 4.95f, 0), new Vector3(.38f, .38f, .38f), 7);
            }

            FinalizePrototype(root, batches);
            parkPrototypes[variant] = root;
            return root;
        }

        static GameObject GetLandmarkPrototype(int kind)
        {
            if (landmarkPrototypes.TryGetValue(kind, out var existing) && existing != null) return existing;
            var root = new GameObject("LandmarkPrototype_Kind_" + kind);
            root.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(root);

            var batches = new List<CombineInstance>[palette.Length];
            for (int i = 0; i < batches.Length; i++) batches[i] = new List<CombineInstance>();

            void AddPart(Mesh m, Vector3 pos, Vector3 scale, int mat, Quaternion rot)
            {
                batches[mat].Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, rot, scale) });
            }
            void Box(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(cubeMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void BoxRot(Vector3 pos, Vector3 scale, int mat, Vector3 euler) => AddPart(cubeMesh, pos, scale, mat, Quaternion.Euler(euler));
            void Cyl(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(cylinderMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void CylRot(Vector3 pos, Vector3 scale, int mat, Vector3 euler) => AddPart(cylinderMesh, pos, scale, mat, Quaternion.Euler(euler));
            void Sph(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(sphereMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Cone(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(coneMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Oct(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(octagonPrismMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));
            void Arch(Vector3 pos, Vector3 scale, int mat, float yaw = 0) => AddPart(archRingMesh, pos, scale, mat, Quaternion.Euler(0, yaw, 0));

            // Grand Monumental Stepped Plaza Foundation for every landmark (radius ~11m)
            Oct(new Vector3(0, .10f, 0), new Vector3(22f, .20f, 22f), 6);
            Oct(new Vector3(0, .26f, 0), new Vector3(18.5f, .22f, 18.5f), 0, 22.5f);
            Oct(new Vector3(0, .42f, 0), new Vector3(15f, .20f, 15f), 6);
            // 8 Corner Plaza Lantern Pillars
            for (int c = 0; c < 8; c++)
            {
                float a = c * Mathf.PI * .25f + Mathf.PI * .125f;
                Vector3 cp = new Vector3(Mathf.Cos(a) * 9.6f, .2f, Mathf.Sin(a) * 9.6f);
                Box(cp + Vector3.up * .45f, new Vector3(.75f, .9f, .75f), 6, a * Mathf.Rad2Deg);
                Cyl(cp + Vector3.up * 2.1f, new Vector3(.18f, 1.35f, .18f), 2);
                Sph(cp + Vector3.up * 3.55f, new Vector3(.46f, .46f, .46f), 7);
            }

            switch (kind)
            {
                case 0: // 00: Clock Tower (Ornate Brick & Limestone Campanile with 4 Glowing Clock Dials, Corner Buttresses, Belfry & Copper Spire)
                    Box(new Vector3(0, 2.2f, 0), new Vector3(6.2f, 3.8f, 6.2f), 0);
                    Box(new Vector3(0, 8.5f, 0), new Vector3(4.8f, 10.5f, 4.8f), 1);
                    for (int c = 0; c < 4; c++)
                    {
                        float sx = (c & 1) == 0 ? -2.35f : 2.35f, sz = (c & 2) == 0 ? -2.35f : 2.35f;
                        Box(new Vector3(sx, 7.8f, sz), new Vector3(.85f, 12.2f, .85f), 0);
                    }
                    for (int tier = 1; tier <= 3; tier++)
                        Box(new Vector3(0, 3.8f + tier * 3.1f, 0), new Vector3(5.4f, .35f, 5.4f), 6);
                    // Clock housing
                    Box(new Vector3(0, 14.8f, 0), new Vector3(5.3f, 2.8f, 5.3f), 0);
                    for (int face = 0; face < 4; face++)
                    {
                        float a = face * 90f;
                        Quaternion r = Quaternion.Euler(0, a, 0);
                        Vector3 fwd = r * Vector3.forward;
                        CylRot(fwd * 2.68f + Vector3.up * 14.8f, new Vector3(2.2f, .12f, 2.2f), 2, new Vector3(90, a, 0));
                        CylRot(fwd * 2.74f + Vector3.up * 14.8f, new Vector3(1.85f, .12f, 1.85f), 7, new Vector3(90, a, 0));
                        // Clock hands
                        BoxRot(fwd * 2.83f + Vector3.up * 15.05f, new Vector3(.12f, .75f, .08f), 2, new Vector3(0, a, 15));
                        BoxRot(fwd * 2.83f + Vector3.up * 14.8f, new Vector3(.65f, .10f, .08f), 2, new Vector3(0, a, -35));
                    }
                    // Belfry & Spire
                    Box(new Vector3(0, 16.5f, 0), new Vector3(5.8f, .45f, 5.8f), 6);
                    for (int c = 0; c < 4; c++)
                    {
                        float sx = (c & 1) == 0 ? -2.1f : 2.1f, sz = (c & 2) == 0 ? -2.1f : 2.1f;
                        Cyl(new Vector3(sx, 17.9f, sz), new Vector3(.42f, 1.3f, .42f), 0);
                    }
                    Sph(new Vector3(0, 17.8f, 0), new Vector3(1.5f, 1.4f, 1.5f), 3); // Bell
                    Cone(new Vector3(0, 21.4f, 0), new Vector3(5.6f, 4.6f, 5.6f), 3);
                    Cyl(new Vector3(0, 24.8f, 0), new Vector3(.18f, 1.6f, .18f), 2);
                    Sph(new Vector3(0, 26.5f, 0), new Vector3(.55f, .55f, .55f), 7);
                    break;

                case 1: // 01 & 23: Memorial Arch / Quarantine Gate (Grand Neoclassical Triumphal Arch with Coffered Vault & Crown Quadriga/Eagle)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Box(new Vector3(side * 4.4f, 1.1f, 0), new Vector3(3.8f, 1.4f, 4.6f), 6);
                        Box(new Vector3(side * 4.4f, 5.6f, 0), new Vector3(3.2f, 7.8f, 3.8f), 0);
                        // Engaged Corinthian columns front & back
                        for (int fb = -1; fb <= 1; fb += 2)
                        {
                            Cyl(new Vector3(side * 3.1f, 5.4f, fb * 2.05f), new Vector3(.55f, 3.8f, .55f), 6);
                            Cyl(new Vector3(side * 5.7f, 5.4f, fb * 2.05f), new Vector3(.55f, 3.8f, .55f), 6);
                            Box(new Vector3(side * 4.4f, 5.8f, fb * 1.98f), new Vector3(1.6f, 2.8f, .18f), 1);
                        }
                    }
                    Arch(new Vector3(0, 5.8f, 0), new Vector3(3.2f, 3.2f, 8.5f), 6);
                    Box(new Vector3(0, 10.2f, 0), new Vector3(12.8f, 1.6f, 4.6f), 0);
                    Box(new Vector3(0, 11.3f, 0), new Vector3(13.4f, .55f, 5.0f), 6);
                    Box(new Vector3(0, 12.5f, 0), new Vector3(10.6f, 1.9f, 3.6f), 1);
                    Box(new Vector3(0, 13.7f, 0), new Vector3(11.2f, .45f, 4.0f), 6);
                    // Bronze crest atop the attic
                    Box(new Vector3(0, 14.5f, 0), new Vector3(3.4f, 1.2f, 2.2f), 5);
                    Cone(new Vector3(0, 16.2f, 0), new Vector3(2.2f, 2.4f, 1.4f), 3);
                    BoxRot(new Vector3(-1.6f, 16.0f, 0), new Vector3(2.8f, .8f, .35f), 3, new Vector3(0, 0, 22));
                    BoxRot(new Vector3(1.6f, 16.0f, 0), new Vector3(2.8f, .8f, .35f), 3, new Vector3(0, 0, -22));
                    Sph(new Vector3(0, 17.6f, 0), new Vector3(.7f, .7f, .7f), 7);
                    break;

                case 2: // 02 & 10: Fountain Plaza / Sunken Concourse (Monumental 3-Tier Cascade Fountain with Obelisk Spout & 4 Sea-Serpent Pedestals)
                    Oct(new Vector3(0, .75f, 0), new Vector3(12.6f, .9f, 12.6f), 0);
                    Oct(new Vector3(0, .92f, 0), new Vector3(11.2f, .7f, 11.2f), 9);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI * .5f + Mathf.PI * .25f;
                        Vector3 kp = new Vector3(Mathf.Cos(a) * 5.4f, 1.2f, Mathf.Sin(a) * 5.4f);
                        Box(kp, new Vector3(1.4f, 1.2f, 1.4f), 6, k * 90 + 45);
                        Sph(kp + Vector3.up * 1.1f, new Vector3(1.0f, 1.3f, 1.0f), 3);
                    }
                    Cyl(new Vector3(0, 2.4f, 0), new Vector3(2.6f, 1.5f, 2.6f), 0);
                    Oct(new Vector3(0, 3.8f, 0), new Vector3(7.4f, .55f, 7.4f), 6);
                    Oct(new Vector3(0, 4.0f, 0), new Vector3(6.4f, .45f, 6.4f), 9);
                    Cyl(new Vector3(0, 5.4f, 0), new Vector3(1.5f, 1.3f, 1.5f), 0);
                    Oct(new Vector3(0, 6.6f, 0), new Vector3(4.2f, .45f, 4.2f), 6);
                    Cone(new Vector3(0, 8.8f, 0), new Vector3(1.6f, 4.2f, 1.6f), 3);
                    Sph(new Vector3(0, 11.2f, 0), new Vector3(.95f, .95f, .95f), 7);
                    break;

                case 3: // 03 & 14: Civic Rotunda / Old City Hall (12-Column Neoclassical Temple Rotunda with Coffered Copper Dome & Lantern)
                    Cyl(new Vector3(0, .85f, 0), new Vector3(13.2f, .65f, 13.2f), 6);
                    Cyl(new Vector3(0, 1.35f, 0), new Vector3(11.8f, .45f, 11.8f), 0);
                    for (int col = 0; col < 12; col++)
                    {
                        float a = col * Mathf.PI * 2f / 12f;
                        Vector3 cp = new Vector3(Mathf.Cos(a) * 5.1f, 0, Mathf.Sin(a) * 5.1f);
                        Box(cp + Vector3.up * 1.75f, new Vector3(.9f, .45f, .9f), 6, a * Mathf.Rad2Deg);
                        Cyl(cp + Vector3.up * 5.2f, new Vector3(.68f, 3.2f, .68f), 0);
                        Box(cp + Vector3.up * 8.55f, new Vector3(.95f, .45f, .95f), 6, a * Mathf.Rad2Deg);
                    }
                    // Inner sanctuary drum
                    Cyl(new Vector3(0, 5.0f, 0), new Vector3(6.8f, 3.4f, 6.8f), 1);
                    // Entablature & Dome
                    Cyl(new Vector3(0, 9.1f, 0), new Vector3(11.6f, .65f, 11.6f), 0);
                    Cyl(new Vector3(0, 10.1f, 0), new Vector3(10.4f, .75f, 10.4f), 6);
                    Sph(new Vector3(0, 10.8f, 0), new Vector3(9.8f, 7.6f, 9.8f), 3);
                    Cyl(new Vector3(0, 15.2f, 0), new Vector3(2.2f, 1.2f, 2.2f), 0);
                    Cone(new Vector3(0, 16.9f, 0), new Vector3(2.6f, 1.8f, 2.6f), 3);
                    Sph(new Vector3(0, 18.1f, 0), new Vector3(.65f, .65f, .65f), 7);
                    break;

                case 4: // 04 & 11: Water Tower / Twin Cisterns (Industrial Riveted Steel Water Tower with Cross-Braced Legs, Catwalk & Conical Roof)
                    for (int leg = 0; leg < 6; leg++)
                    {
                        float a = leg * Mathf.PI / 3f;
                        Vector3 lp = new Vector3(Mathf.Cos(a) * 3.8f, 5.8f, Mathf.Sin(a) * 3.8f);
                        CylRot(lp, new Vector3(.42f, 5.6f, .42f), 2, new Vector3(Mathf.Sin(a) * 8f, 0, -Mathf.Cos(a) * 8f));
                        Box(new Vector3(Mathf.Cos(a) * 4.4f, .8f, Mathf.Sin(a) * 4.4f), new Vector3(1.1f, .9f, 1.1f), 0);
                    }
                    // Standpipe & horizontal cross-braces
                    Cyl(new Vector3(0, 5.8f, 0), new Vector3(1.2f, 5.5f, 1.2f), 4);
                    Oct(new Vector3(0, 4.5f, 0), new Vector3(8.2f, .25f, 8.2f), 2);
                    Oct(new Vector3(0, 8.2f, 0), new Vector3(7.6f, .25f, 7.6f), 2);
                    // Catwalk ring
                    Cyl(new Vector3(0, 10.8f, 0), new Vector3(8.4f, .22f, 8.4f), 2);
                    for (int r = 0; r < 12; r++)
                    {
                        float a = r * Mathf.PI / 6f;
                        Cyl(new Vector3(Mathf.Cos(a) * 4.05f, 11.4f, Mathf.Sin(a) * 4.05f), new Vector3(.08f, .55f, .08f), 2);
                    }
                    // Riveted tank body & conical roof
                    Cyl(new Vector3(0, 13.8f, 0), new Vector3(6.8f, 3.0f, 6.8f), 4);
                    Cyl(new Vector3(0, 12.5f, 0), new Vector3(6.95f, .18f, 6.95f), 2);
                    Cyl(new Vector3(0, 15.1f, 0), new Vector3(6.95f, .18f, 6.95f), 2);
                    Cone(new Vector3(0, 18.0f, 0), new Vector3(7.4f, 2.8f, 7.4f), 2);
                    Cyl(new Vector3(0, 19.8f, 0), new Vector3(.16f, .9f, .16f), 2);
                    Sph(new Vector3(0, 20.8f, 0), new Vector3(.48f, .48f, .48f), 7);
                    break;

                case 5: // 05: North Obelisk (Tall Tapered Obsidian Monolith with Gold Pyramidion & Ring of 6 Ancient Runic Pylons)
                    Box(new Vector3(0, 1.4f, 0), new Vector3(5.6f, 1.8f, 5.6f), 6);
                    Box(new Vector3(0, 2.8f, 0), new Vector3(4.2f, 1.4f, 4.2f), 5);
                    AddPart(CreateConeMesh(4, 1.65f, 16.5f, .85f), new Vector3(0, 11.6f, 0), Vector3.one, 5, Quaternion.Euler(0, 45f, 0));
                    AddPart(CreateConeMesh(4, .88f, 2.4f, 0f), new Vector3(0, 21.0f, 0), Vector3.one, 7, Quaternion.Euler(0, 45f, 0));
                    for (int p = 0; p < 6; p++)
                    {
                        float a = p * Mathf.PI / 3f;
                        Vector3 pp = new Vector3(Mathf.Cos(a) * 6.4f, 0, Mathf.Sin(a) * 6.4f);
                        Box(pp + Vector3.up * 2.4f, new Vector3(1.1f, 4.2f, .75f), 5, a * Mathf.Rad2Deg);
                        Box(pp + Vector3.up * 4.6f, new Vector3(1.3f, .4f, .95f), 6, a * Mathf.Rad2Deg);
                        Sph(pp + Vector3.up * 5.2f, new Vector3(.55f, .55f, .55f), 7);
                    }
                    break;

                case 6: // 06: Radio Spire (Tall Steel Lattice Transmission Tower with Microwave Dishes & Beacon)
                    Box(new Vector3(0, 1.2f, 0), new Vector3(6.8f, 1.6f, 6.8f), 0);
                    for (int leg = 0; leg < 4; leg++)
                    {
                        float sx = (leg & 1) == 0 ? -1 : 1, sz = (leg & 2) == 0 ? -1 : 1;
                        CylRot(new Vector3(sx * 1.8f, 11.5f, sz * 1.8f), new Vector3(.28f, 10.8f, .28f), 2, new Vector3(-sz * 5.5f, 0, sx * 5.5f));
                    }
                    for (int stage = 0; stage < 6; stage++)
                    {
                        float y = 3.5f + stage * 3.2f;
                        float w = Mathf.Lerp(4.6f, 1.8f, stage / 5f);
                        Box(new Vector3(0, y, 0), new Vector3(w, .24f, w), 4);
                        if (stage == 2 || stage == 4)
                        {
                            Cyl(new Vector3(0, y + .15f, 0), new Vector3(w * 1.35f, .12f, w * 1.35f), 2);
                            for (int d = 0; d < 4; d++)
                            {
                                float da = d * 90f + stage * 45f;
                                Quaternion dr = Quaternion.Euler(0, da, 0);
                                AddPart(dishMesh, dr * new Vector3(0, y + .8f, w * .68f), new Vector3(1.1f, 1.1f, 1.1f), 0, Quaternion.Euler(75, da, 0));
                            }
                        }
                    }
                    Cyl(new Vector3(0, 24.5f, 0), new Vector3(.22f, 4.2f, .22f), 2);
                    Sph(new Vector3(0, 28.9f, 0), new Vector3(.68f, .68f, .68f), 7);
                    break;

                case 7: // 07: Silo Plaza (Twin Industrial Concrete & Rust Grain Silos with Overhead Conveyor Bridge & Elevator Headhouse)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 sp = new Vector3(side * 3.8f, 0, 0);
                        Cyl(sp + Vector3.up * 7.2f, new Vector3(5.4f, 6.8f, 5.4f), 0);
                        for (int hoop = 0; hoop < 5; hoop++)
                            Cyl(sp + Vector3.up * (2.2f + hoop * 2.4f), new Vector3(5.55f, .14f, 5.55f), 4);
                        Cone(sp + Vector3.up * 15.1f, new Vector3(5.6f, 2.2f, 5.6f), 4);
                    }
                    // Connecting overhead gallery bridge & headhouse tower
                    Box(new Vector3(0, 14.8f, 0), new Vector3(9.2f, 1.6f, 2.4f), 2);
                    Box(new Vector3(3.8f, 17.2f, 0), new Vector3(3.2f, 3.4f, 3.2f), 4);
                    Box(new Vector3(3.8f, 19.1f, 0), new Vector3(3.6f, .35f, 3.6f), 2);
                    // External elevator chute
                    Box(new Vector3(-6.7f, 7.8f, 0), new Vector3(.85f, 14.8f, .85f), 2);
                    break;

                case 8: // 08 & 25: Deepwood Shrine / Bell Pavilion (Sacred Stone Torii Sanctuary & Ceremonial Bronze Bell Pavilion)
                    Oct(new Vector3(0, .95f, 0), new Vector3(9.6f, .9f, 9.6f), 8);
                    for (int c = 0; c < 4; c++)
                    {
                        float sx = (c & 1) == 0 ? -2.8f : 2.8f, sz = (c & 2) == 0 ? -2.8f : 2.8f;
                        Cyl(new Vector3(sx, 3.8f, sz), new Vector3(.62f, 2.6f, .62f), 1);
                        Box(new Vector3(sx, 1.4f, sz), new Vector3(.95f, .45f, .95f), 5);
                    }
                    // Sweeping two-tiered pagoda pavilion roof
                    AddPart(CreateConeMesh(4, 6.2f, 1.6f, 2.8f), new Vector3(0, 7.1f, 0), Vector3.one, 2, Quaternion.Euler(0, 45f, 0));
                    Box(new Vector3(0, 8.2f, 0), new Vector3(3.6f, 1.2f, 3.6f), 1);
                    AddPart(CreateConeMesh(4, 4.4f, 1.8f, .2f), new Vector3(0, 9.6f, 0), Vector3.one, 3, Quaternion.Euler(0, 45f, 0));
                    // Great bronze sanctuary bell
                    Cyl(new Vector3(0, 4.4f, 0), new Vector3(1.8f, 1.25f, 1.8f), 3);
                    Sph(new Vector3(0, 5.6f, 0), new Vector3(1.6f, .8f, 1.6f), 3);
                    // Front stone Torii gate at z = -6.5
                    for (int s = -1; s <= 1; s += 2)
                        Cyl(new Vector3(s * 2.4f, 2.8f, -6.5f), new Vector3(.48f, 2.5f, .48f), 1);
                    Box(new Vector3(0, 4.6f, -6.5f), new Vector3(6.2f, .35f, .45f), 1);
                    Box(new Vector3(0, 5.35f, -6.5f), new Vector3(7.0f, .48f, .65f), 5);
                    break;

                case 9: // 09: Botanical Dome (Giant Geodesic Glass Conservatory with Steel Rib Arches & Central Mutated World-Tree Core)
                    Cyl(new Vector3(0, 1.2f, 0), new Vector3(13.6f, .95f, 13.6f), 0);
                    Sph(new Vector3(0, 3.8f, 0), new Vector3(12.4f, 9.8f, 12.4f), 9);
                    for (int rib = 0; rib < 6; rib++)
                        Arch(new Vector3(0, 1.6f, 0), new Vector3(6.3f, 7.2f, 1.2f), 2, rib * 30f);
                    // Inner ancient tree trunk & glowing canopy
                    Cyl(new Vector3(0, 4.2f, 0), new Vector3(1.8f, 3.2f, 1.8f), 4);
                    Sph(new Vector3(0, 7.6f, 0), new Vector3(6.4f, 3.8f, 6.4f), 8);
                    Cone(new Vector3(0, 10.2f, 0), new Vector3(2.8f, 2.2f, 2.8f), 3);
                    Sph(new Vector3(0, 11.6f, 0), new Vector3(.75f, .75f, .75f), 7);
                    break;

                case 10: // 12: Observatory Dome (Cylindrical Concrete Drum, Slotted Brass Observatory Dome & Protruding Refracting Telescope)
                    Cyl(new Vector3(0, 3.6f, 0), new Vector3(10.8f, 3.2f, 10.8f), 0);
                    Cyl(new Vector3(0, 6.9f, 0), new Vector3(11.4f, .45f, 11.4f), 6);
                    for (int w = 0; w < 8; w++)
                    {
                        float a = w * Mathf.PI * .25f;
                        Box(new Vector3(Mathf.Cos(a) * 5.35f, 4.0f, Mathf.Sin(a) * 5.35f), new Vector3(1.4f, 2.4f, .25f), 5, a * Mathf.Rad2Deg + 90f);
                    }
                    Sph(new Vector3(0, 7.2f, 0), new Vector3(10.2f, 9.6f, 10.2f), 3);
                    Arch(new Vector3(0, 7.2f, 0), new Vector3(5.2f, 5.0f, 3.8f), 2, 25f);
                    // Giant telescope tube emerging from the dome slot
                    CylRot(new Vector3(1.2f, 10.6f, -2.4f), new Vector3(1.35f, 4.2f, 1.35f), 2, new Vector3(48, -25, 0));
                    CylRot(new Vector3(2.4f, 12.5f, -4.6f), new Vector3(1.55f, .35f, 1.55f), 7, new Vector3(48, -25, 0));
                    break;

                case 11: // 13: War Cenotaph (Solemn Stepped Whitehall-style Stone Cenotaph Flanked by 4 Flaming Braziers)
                    Box(new Vector3(0, 1.2f, 0), new Vector3(7.6f, 1.4f, 5.2f), 6);
                    Box(new Vector3(0, 2.8f, 0), new Vector3(5.8f, 2.0f, 3.8f), 0);
                    Box(new Vector3(0, 6.8f, 0), new Vector3(4.2f, 6.4f, 2.6f), 6);
                    Box(new Vector3(0, 10.8f, 0), new Vector3(3.4f, 2.2f, 2.1f), 0);
                    Box(new Vector3(0, 12.3f, 0), new Vector3(3.8f, .85f, 2.5f), 5); // Sculpted sarcophagus crown
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Box(new Vector3(side * 2.2f, 6.8f, 0), new Vector3(.35f, 5.2f, 1.4f), 1); // Crimson banners
                    }
                    for (int b = 0; b < 4; b++)
                    {
                        float sx = (b & 1) == 0 ? -4.8f : 4.8f, sz = (b & 2) == 0 ? -3.6f : 3.6f;
                        Cyl(new Vector3(sx, 1.4f, sz), new Vector3(1.1f, 1.1f, 1.1f), 5);
                        Cone(new Vector3(sx, 2.8f, sz), new Vector3(1.5f, .9f, 1.5f), 2);
                        Sph(new Vector3(sx, 3.35f, sz), new Vector3(.95f, 1.2f, .95f), 7);
                    }
                    break;

                case 12: // 15 & 24: Rail Turntable / Armored Train Wreck (Sunken Turntable Pit & Heavy Armored Steam/Diesel Locomotive)
                    Cyl(new Vector3(0, .35f, 0), new Vector3(14.5f, .35f, 14.5f), 2);
                    Cyl(new Vector3(0, .55f, 0), new Vector3(12.8f, .25f, 12.8f), 5);
                    // Bridge girder rails
                    Box(new Vector3(0, .75f, 0), new Vector3(13.6f, .45f, 3.2f), 4, 22f);
                    // Heavy Armored Locomotive sitting on the bridge
                    Quaternion trainRot = Quaternion.Euler(0, 22f, 0);
                    AddPart(cubeMesh, new Vector3(0, 2.2f, 0), new Vector3(11.2f, 2.6f, 3.1f), 2, trainRot);
                    AddPart(cylinderMesh, trainRot * new Vector3(1.5f, 3.1f, 0), new Vector3(2.4f, 3.6f, 2.4f), 4, trainRot * Quaternion.Euler(0, 0, 90));
                    AddPart(cubeMesh, trainRot * new Vector3(-3.6f, 3.6f, 0), new Vector3(3.4f, 2.4f, 3.0f), 2, trainRot);
                    AddPart(cylinderMesh, trainRot * new Vector3(3.8f, 4.6f, 0), new Vector3(.75f, .95f, .75f), 5, trainRot);
                    for (int w = -2; w <= 2; w++)
                    {
                        AddPart(cylinderMesh, trainRot * new Vector3(w * 2.1f, 1.15f, 1.55f), new Vector3(1.25f, .18f, 1.25f), 4, trainRot * Quaternion.Euler(90, 0, 0));
                        AddPart(cylinderMesh, trainRot * new Vector3(w * 2.1f, 1.15f, -1.55f), new Vector3(1.25f, .18f, 1.25f), 4, trainRot * Quaternion.Euler(90, 0, 0));
                    }
                    break;

                case 13: // 16: Goliath Crane (Towering Shipyard Gantry Crane with Wide Portal Legs, Overhead Bridge Beam & Trolley Hoist)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        for (int fb = -1; fb <= 1; fb += 2)
                        {
                            CylRot(new Vector3(side * 5.2f, 9.2f, fb * 2.6f), new Vector3(.65f, 9.2f, .65f), 4, new Vector3(-fb * 7f, 0, 0));
                        }
                        Box(new Vector3(side * 5.2f, 5.5f, 0), new Vector3(.75f, .55f, 4.6f), 2);
                        Box(new Vector3(side * 5.2f, 11.5f, 0), new Vector3(.75f, .55f, 3.2f), 2);
                    }
                    // Main overhead double-girder bridge & operator cab
                    Box(new Vector3(0, 18.2f, 0), new Vector3(15.6f, 2.1f, 3.2f), 4);
                    Box(new Vector3(0, 19.6f, 0), new Vector3(16.2f, .45f, 3.8f), 2);
                    Box(new Vector3(2.4f, 16.4f, 1.6f), new Vector3(2.8f, 1.8f, 2.2f), 2);
                    Box(new Vector3(2.4f, 16.4f, 2.72f), new Vector3(2.2f, .95f, .12f), 7);
                    // Suspended hoist & cargo container
                    Cyl(new Vector3(-1.5f, 12.5f, 0), new Vector3(.18f, 5.2f, .18f), 2);
                    Box(new Vector3(-1.5f, 6.8f, 0), new Vector3(1.6f, .95f, 1.6f), 4);
                    Box(new Vector3(-1.5f, 4.2f, 0), new Vector3(5.6f, 2.4f, 2.5f), 1, 12f);
                    break;

                case 14: // 17: Cooling Towers (Twin Hyperbolic Concrete Reactor Cooling Towers with Plume Rims & Turbine Hall)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 tp = new Vector3(side * 4.2f, 0, 1.2f);
                        float h = side < 0 ? 15.5f : 13.2f;
                        float s = side < 0 ? 6.8f : 5.8f;
                        AddPart(hyperboloidMesh, tp + Vector3.up * (h * .5f + .8f), new Vector3(s, h, s), 0, Quaternion.identity);
                        Cyl(tp + Vector3.up * (h + .8f), new Vector3(s * .82f, .28f, s * .82f), 2);
                        Cyl(tp + Vector3.up * (h + .4f), new Vector3(s * .74f, .25f, s * .74f), 9);
                        for (int leg = 0; leg < 8; leg++)
                        {
                            float a = leg * Mathf.PI * .25f;
                            Box(tp + new Vector3(Mathf.Cos(a) * s * .46f, .65f, Mathf.Sin(a) * s * .46f), new Vector3(.35f, 1.2f, .55f), 6, a * Mathf.Rad2Deg);
                        }
                    }
                    // Foreground reactor turbine hall
                    Box(new Vector3(0, 2.4f, -4.2f), new Vector3(8.6f, 3.8f, 4.2f), 2);
                    CylRot(new Vector3(0, 3.2f, -2.1f), new Vector3(1.4f, 3.8f, 1.4f), 4, new Vector3(0, 0, 90));
                    break;

                case 15: // 18: Blast Furnace (Colossal Cast-Iron Foundry Furnace with 3 Hot-Blast Stoves, Downcomer Pipes & Slag Pit)
                    // Main furnace stack
                    Cyl(new Vector3(-1.8f, 5.5f, 0), new Vector3(5.6f, 4.8f, 5.6f), 4);
                    Cyl(new Vector3(-1.8f, 12.8f, 0), new Vector3(4.2f, 4.5f, 4.2f), 2);
                    Box(new Vector3(-1.8f, 18.2f, 0), new Vector3(5.2f, 1.8f, 5.2f), 4);
                    // 3 Hot-blast Cowper stoves
                    for (int s = 0; s < 3; s++)
                    {
                        Vector3 sp = new Vector3(3.8f, 0, -3.2f + s * 3.2f);
                        Cyl(sp + Vector3.up * 6.5f, new Vector3(2.4f, 6.0f, 2.4f), 4);
                        Sph(sp + Vector3.up * 12.5f, new Vector3(2.4f, 2.2f, 2.4f), 2);
                    }
                    // Overhead downcomer gas pipe & skip hoist incline
                    CylRot(new Vector3(1.0f, 13.8f, 0), new Vector3(.95f, 3.4f, .95f), 2, new Vector3(0, 0, 72));
                    BoxRot(new Vector3(-5.2f, 9.5f, 0), new Vector3(1.2f, 16.5f, 1.4f), 2, new Vector3(0, 0, -28));
                    // Molten slag pit glow
                    Cyl(new Vector3(-1.8f, .58f, -4.4f), new Vector3(3.8f, .18f, 3.8f), 7);
                    break;

                case 16: // 19: Amphitheater (Classical Semicircular Acoustic Shell & Stepped Stone Seating Tiers)
                    // Acoustic shell dome at rear (z > 0)
                    Sph(new Vector3(0, 3.8f, 3.6f), new Vector3(10.5f, 9.2f, 6.8f), 0);
                    Arch(new Vector3(0, 1.2f, 1.8f), new Vector3(5.4f, 5.2f, 2.2f), 6);
                    Cyl(new Vector3(0, .75f, 1.2f), new Vector3(9.2f, .45f, 6.4f), 5);
                    // Semicircular stepped stone benches around front
                    for (int tier = 0; tier < 3; tier++)
                    {
                        float r = 5.2f + tier * 1.8f;
                        for (int seg = -3; seg <= 3; seg++)
                        {
                            float a = seg * 22f * Mathf.Deg2Rad;
                            Vector3 bp = new Vector3(Mathf.Sin(a) * r, .55f + tier * .38f, -Mathf.Cos(a) * r);
                            Box(bp, new Vector3(2.1f, .45f, 1.1f), tier % 2 == 0 ? 6 : 0, -seg * 22f);
                        }
                    }
                    break;

                case 17: // 20 & 28: Radar Array / Dish Network (Armored Concrete Pedestal & Giant Rotating Parabolic Dish Antenna)
                    Oct(new Vector3(0, 2.6f, 0), new Vector3(7.2f, 4.2f, 7.2f), 0);
                    Cyl(new Vector3(0, 5.4f, 0), new Vector3(4.8f, .85f, 4.8f), 2);
                    // Yoke arms
                    for (int s = -1; s <= 1; s += 2)
                        Box(new Vector3(s * 1.8f, 7.8f, 0), new Vector3(.75f, 3.8f, 1.4f), 4);
                    // Giant Parabolic Dish tilted toward sky
                    Quaternion dishRot = Quaternion.Euler(52f, 25f, 0);
                    Vector3 dishPivot = new Vector3(0, 9.4f, 0);
                    AddPart(dishMesh, dishPivot, new Vector3(5.8f, 5.8f, 5.8f), 0, dishRot);
                    AddPart(cylinderMesh, dishPivot + dishRot * new Vector3(0, 2.2f, 0), new Vector3(.35f, 2.2f, .35f), 2, dishRot);
                    AddPart(sphereMesh, dishPivot + dishRot * new Vector3(0, 4.5f, 0), new Vector3(.85f, .85f, .85f), 7, Quaternion.identity);
                    break;

                case 18: // 21: Oil Derrick (Tall Tapered Steel Drilling Derrick + Walking-Beam Pumpjack & Sludge Tanks)
                    for (int leg = 0; leg < 4; leg++)
                    {
                        float sx = (leg & 1) == 0 ? -1 : 1, sz = (leg & 2) == 0 ? -1 : 1;
                        CylRot(new Vector3(sx * 1.9f, 9.8f, sz * 1.9f), new Vector3(.26f, 9.4f, .26f), 2, new Vector3(-sz * 6f, 0, sx * 6f));
                    }
                    for (int g = 0; g < 5; g++)
                    {
                        float y = 3.2f + g * 3.4f;
                        float w = Mathf.Lerp(4.8f, 1.9f, g / 4f);
                        Box(new Vector3(0, y, 0), new Vector3(w, .22f, w), 4);
                    }
                    Box(new Vector3(0, 19.2f, 0), new Vector3(2.2f, .85f, 2.2f), 4);
                    // Pumpjack walking beam & horsehead at side
                    Box(new Vector3(4.6f, 2.4f, 0), new Vector3(1.2f, 3.8f, 1.2f), 2);
                    BoxRot(new Vector3(4.6f, 4.5f, 0), new Vector3(.55f, .65f, 5.4f), 4, new Vector3(14f, 0, 0));
                    Box(new Vector3(4.6f, 4.9f, -2.6f), new Vector3(.75f, 1.8f, .85f), 2);
                    break;

                case 19: // 22: Cathedral Spire (Gothic Twin-Buttress Facade, Luminous Rose Window & Soaring Octagonal Stone Spire)
                    Box(new Vector3(0, 5.2f, 0), new Vector3(8.6f, 9.4f, 5.8f), 0);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Box(new Vector3(s * 3.8f, 7.6f, -.4f), new Vector3(2.6f, 14.2f, 4.2f), 6);
                        Cone(new Vector3(s * 3.8f, 16.4f, -.4f), new Vector3(2.8f, 3.4f, 2.8f), 2);
                    }
                    // Rose window & gothic portal
                    CylRot(new Vector3(0, 7.2f, -2.95f), new Vector3(3.4f, .18f, 3.4f), 2, new Vector3(90, 0, 0));
                    CylRot(new Vector3(0, 7.2f, -3.02f), new Vector3(2.8f, .18f, 2.8f), 7, new Vector3(90, 0, 0));
                    Arch(new Vector3(0, 2.2f, -2.85f), new Vector3(1.8f, 2.2f, 1.2f), 6);
                    // Central soaring spire
                    Oct(new Vector3(0, 12.8f, .6f), new Vector3(4.2f, 5.8f, 4.2f), 0);
                    Cone(new Vector3(0, 20.5f, .6f), new Vector3(4.4f, 9.6f, 4.4f), 3);
                    Sph(new Vector3(0, 25.6f, .6f), new Vector3(.6f, .6f, .6f), 7);
                    break;

                case 20: // 26: Wind Turbine Array (Towering Tubular Steel Wind Turbine with Nacelle & 3 11m Rotor Blades)
                    Cyl(new Vector3(0, 1.5f, 0), new Vector3(4.4f, 1.2f, 4.4f), 0);
                    AddPart(CreateConeMesh(16, 1.45f, 18.5f, .75f), new Vector3(0, 10.8f, 0), Vector3.one, 0, Quaternion.identity);
                    Box(new Vector3(0, 20.4f, .4f), new Vector3(2.2f, 2.0f, 4.8f), 6);
                    Cone(new Vector3(0, 20.4f, -2.4f), new Vector3(1.8f, 1.6f, 1.8f), 4);
                    for (int b = 0; b < 3; b++)
                    {
                        float angle = b * 120f + 22f;
                        Quaternion br = Quaternion.Euler(0, 0, angle);
                        Vector3 offset = br * new Vector3(0, 4.8f, -2.2f);
                        AddPart(cubeMesh, new Vector3(0, 20.4f, 0) + offset, new Vector3(.65f, 9.2f, .18f), 0, br * Quaternion.Euler(0, 12f, 0));
                    }
                    Sph(new Vector3(0, 21.6f, 2.2f), new Vector3(.55f, .55f, .55f), 7);
                    break;

                case 21: // 27: Command Bunker (Reinforced Sloped Concrete Citadel Bunker with Blast Doors, Firing Slits, Periscopes & AA Radar)
                    AddPart(CreateConeMesh(8, 7.6f, 4.6f, 5.4f), new Vector3(0, 2.6f, 0), Vector3.one, 0, Quaternion.Euler(0, 22.5f, 0));
                    Oct(new Vector3(0, 5.6f, 0), new Vector3(9.4f, 1.8f, 9.4f), 2, 22.5f);
                    // Armored visor slits
                    for (int s = 0; s < 4; s++)
                    {
                        float a = s * 90f;
                        Quaternion r = Quaternion.Euler(0, a, 0);
                        Box(r * new Vector3(0, 5.6f, -4.65f), new Vector3(3.6f, .38f, .25f), 7, a);
                    }
                    // Heavy blast door at south entrance
                    BoxRot(new Vector3(0, 2.2f, -6.2f), new Vector3(3.4f, 3.2f, .85f), 2, new Vector3(-16, 0, 0));
                    // Roof command cupola, periscopes & twin cannon turret
                    Cyl(new Vector3(0, 7.2f, 0), new Vector3(4.6f, .95f, 4.6f), 4);
                    Sph(new Vector3(0, 8.2f, 0), new Vector3(3.6f, 1.8f, 3.6f), 2);
                    CylRot(new Vector3(-.55f, 8.4f, -2.6f), new Vector3(.28f, 1.8f, .28f), 2, new Vector3(82, 0, 0));
                    CylRot(new Vector3(.55f, 8.4f, -2.6f), new Vector3(.28f, 1.8f, .28f), 2, new Vector3(82, 0, 0));
                    break;

                default: // 22 (29): Launch Gantry (Colossal Launch Pad, Flame Trench, Multi-Stage Rocket & Umbilical Service Gantry Tower)
                    // Launch pad & flame deflector trench
                    Box(new Vector3(0, 1.2f, 0), new Vector3(11.6f, 1.6f, 11.6f), 0);
                    Box(new Vector3(0, 1.6f, 0), new Vector3(4.8f, 1.2f, 8.4f), 5);
                    // Multi-stage ballistic rocket at x = -2.0
                    Vector3 rp = new Vector3(-2.0f, 0, 0);
                    Cyl(rp + Vector3.up * 6.8f, new Vector3(2.4f, 5.2f, 2.4f), 0);
                    Cyl(rp + Vector3.up * 13.8f, new Vector3(2.0f, 2.2f, 2.0f), 2);
                    Cyl(rp + Vector3.up * 17.5f, new Vector3(1.8f, 1.8f, 1.8f), 0);
                    Cone(rp + Vector3.up * 21.0f, new Vector3(1.85f, 3.6f, 1.85f), 1);
                    for (int fin = 0; fin < 4; fin++)
                    {
                        float a = fin * 90f + 45f;
                        Quaternion fr = Quaternion.Euler(0, a, 0);
                        AddPart(cubeMesh, rp + fr * new Vector3(0, 2.8f, 1.45f), new Vector3(.18f, 2.8f, 1.2f), 1, fr);
                    }
                    // Umbilical Gantry Tower at x = +3.2
                    Vector3 gp = new Vector3(3.2f, 0, 0);
                    for (int leg = 0; leg < 4; leg++)
                    {
                        float sx = (leg & 1) == 0 ? -1.4f : 1.4f, sz = (leg & 2) == 0 ? -1.4f : 1.4f;
                        Cyl(gp + new Vector3(sx, 11.2f, sz), new Vector3(.32f, 10.2f, .32f), 4);
                    }
                    for (int lvl = 0; lvl < 6; lvl++)
                    {
                        float y = 3.6f + lvl * 3.2f;
                        Box(gp + Vector3.up * y, new Vector3(3.4f, .32f, 3.4f), 2);
                        if (lvl == 2 || lvl == 4)
                            Box(new Vector3(.6f, y, 0), new Vector3(3.8f, .55f, 1.2f), 4); // Swing arm
                    }
                    break;
            }

            FinalizePrototype(root, batches);
            landmarkPrototypes[kind] = root;
            return root;
        }

        static void FinalizePrototype(GameObject root, List<CombineInstance>[] batches)
        {
            for (int mat = 0; mat < batches.Length; mat++)
            {
                if (batches[mat].Count == 0) continue;
                var mesh = new Mesh { name = root.name + "_Mat_" + mat, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(batches[mat].ToArray(), true, true);
                mesh.RecalculateBounds();
                var child = new GameObject("Part_" + mat);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = palette[mat];
            }
            root.SetActive(false);
        }

        static void DressParkSurroundings(Transform chunkTransform, Vector3 centre, int variant)
        {
            var bench = Resources.Load<GameObject>("ImportedWorld/props/bench");
            var lamp = Resources.Load<GameObject>("ImportedWorld/PolyHaven/street_lamp_01");
            var rock1 = Resources.Load<GameObject>("ImportedWorld/rock/rock_set_01");
            var rock2 = Resources.Load<GameObject>("ImportedWorld/rock/rock_set_02");
            var fence = Resources.Load<GameObject>("ImportedWorld/props/SM_fence_stand_small");

            for (int seat = 0; seat < 8; seat++)
            {
                float a = seat * Mathf.PI * .25f;
                Vector3 bPos = centre + new Vector3(Mathf.Cos(a) * 10.2f, 0, Mathf.Sin(a) * 10.2f);
                if (bench != null)
                    ImportedWorldBatch.Add(chunkTransform, bench, bPos, Quaternion.Euler(0, -a * Mathf.Rad2Deg - 90f, 0), Vector3.one);
                if (lamp != null && seat % 2 == 0)
                {
                    Vector3 lPos = centre + new Vector3(Mathf.Cos(a) * 11.8f, 0, Mathf.Sin(a) * 11.8f);
                    PlaceNormalizedProp(chunkTransform, lamp, lPos, -a * Mathf.Rad2Deg - 90f, StreetLampHeight);
                }
            }
            // Decorative rock clusters and fence segments at outer park corners
            for (int c = 0; c < 4; c++)
            {
                float a = c * Mathf.PI * .5f + Mathf.PI * .25f;
                var rock = (c + variant) % 2 == 0 ? rock1 : rock2;
                if (rock != null)
                    PlaceNormalizedProp(chunkTransform, rock, centre + new Vector3(Mathf.Cos(a) * 13.5f, 0, Mathf.Sin(a) * 13.5f), c * 90f, 1.4f);
                if (fence != null)
                    PlaceNormalizedProp(chunkTransform, fence, centre + new Vector3(Mathf.Cos(a) * 15.2f, 0, Mathf.Sin(a) * 15.2f), -a * Mathf.Rad2Deg + 90f, 1.8f);
            }
        }

        static void DressLandmarkSurroundings(Transform chunkTransform, Vector3 lPos, int kind, int seed)
        {
            var lamp = Resources.Load<GameObject>("ImportedWorld/PolyHaven/street_lamp_01");
            var bench = Resources.Load<GameObject>("ImportedWorld/props/bench");
            var barrier = Resources.Load<GameObject>("ImportedWorld/PolyHaven/concrete_road_barrier");
            var stove = Resources.Load<GameObject>("ImportedWorld/PolyHaven/barrel_stove");
            var fence = Resources.Load<GameObject>("ImportedWorld/props/SM_fence_stand_small");
            var waterTank = Resources.Load<GameObject>("ImportedWorld/props/WaterTank");
            var rock = Resources.Load<GameObject>("ImportedWorld/rock/rock_set_01");

            // 4 Approach street lamps & benches around the 12.5m perimeter ring (leaving boss approach at z = -10.5 open)
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 p = lPos + new Vector3(Mathf.Cos(a) * 12.2f, 0, Mathf.Sin(a) * 12.2f);
                if (lamp != null)
                    PlaceNormalizedProp(chunkTransform, lamp, p, -a * Mathf.Rad2Deg - 90f, StreetLampHeight);
                if (bench != null)
                    ImportedWorldBatch.Add(chunkTransform, bench, lPos + new Vector3(Mathf.Cos(a) * 10.6f, 0, Mathf.Sin(a) * 10.6f), Quaternion.Euler(0, -a * Mathf.Rad2Deg - 90f, 0), Vector3.one);
            }

            // Boss arena flanking fortifications at z = -11.5, x = +-7.5
            for (int side = -1; side <= 1; side += 2)
            {
                if (barrier != null)
                    PlaceNormalizedProp(chunkTransform, barrier, lPos + new Vector3(side * 7.2f, 0, -10.8f), side * 25f, 1.1f);
                if (stove != null)
                    PlaceNormalizedProp(chunkTransform, stove, lPos + new Vector3(side * 8.6f, 0, -9.5f), 0f, 1.25f);
                if (kind == 4 || kind == 7 || kind == 13 || kind == 14 || kind == 15 || kind == 18 || kind == 21 || kind == 22)
                {
                    if (fence != null)
                        PlaceNormalizedProp(chunkTransform, fence, lPos + new Vector3(side * 11.5f, 0, 0), 90f, 2.0f);
                    if (waterTank != null && side > 0)
                        PlaceNormalizedProp(chunkTransform, waterTank, lPos + new Vector3(9.8f, 0, 8.2f), 45f, 2.2f);
                }
                else if (rock != null)
                {
                    PlaceNormalizedProp(chunkTransform, rock, lPos + new Vector3(side * 11.2f, 0, 8.5f), side * 60f, 1.6f);
                }
            }
        }

        static bool CircleBlocked(Vector3 p, float radius, Vector3 center, float solidRadius)
        {
            float dx = p.x - center.x, dz = p.z - center.z;
            float limit = solidRadius + radius;
            return dx * dx + dz * dz < limit * limit;
        }

        static bool BoxBlocked(Vector3 p, float radius, Vector3 center, float halfX, float halfZ)
        {
            return Mathf.Abs(p.x - center.x) < halfX + radius && Mathf.Abs(p.z - center.z) < halfZ + radius;
        }

        static bool RotatedBoxBlocked(Vector3 p, float radius, Vector3 center, float halfX, float halfZ, float yawDeg)
        {
            float rad = -yawDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            float dx = p.x - center.x, dz = p.z - center.z;
            float lx = dx * cos - dz * sin;
            float lz = dx * sin + dz * cos;
            return Mathf.Abs(lx) < halfX + radius && Mathf.Abs(lz) < halfZ + radius;
        }

        public static bool PocketParkBlocked(Vector3 p, float radius, Vector3 center, int variant)
        {
            if (Mathf.Abs(p.x - center.x) > 5.0f + radius || Mathf.Abs(p.z - center.z) > 5.0f + radius) return false;
            switch (Mathf.Abs(variant) % 4)
            {
                case 0: return CircleBlocked(p, radius, center, 3.7f);
                case 1: return CircleBlocked(p, radius, center, 3.9f);
                case 2: return BoxBlocked(p, radius, center, 2.1f, 2.1f);
                default: return CircleBlocked(p, radius, center, 3.5f);
            }
        }

        public static bool LandmarkBlocked(Vector3 p, float radius, Vector3 center, int kind)
        {
            if (Mathf.Abs(p.x - center.x) > 9.0f + radius || Mathf.Abs(p.z - center.z) > 9.0f + radius) return false;
            switch (kind)
            {
                case 0: // Clock Tower
                    return BoxBlocked(p, radius, center, 3.1f, 3.1f);
                case 1: // Memorial Arch (two solid stone piers; center archway remains passable)
                    return BoxBlocked(p, radius, center + new Vector3(-4.4f, 0, 0), 1.9f, 2.3f) ||
                           BoxBlocked(p, radius, center + new Vector3(4.4f, 0, 0), 1.9f, 2.3f);
                case 2: // Fountain Plaza
                    return CircleBlocked(p, radius, center, 6.2f);
                case 3: // Civic Rotunda
                    return CircleBlocked(p, radius, center, 5.6f);
                case 4: // Water Tower (center standpipe + 6 steel support legs)
                    if (CircleBlocked(p, radius, center, 0.7f)) return true;
                    for (int leg = 0; leg < 6; leg++)
                    {
                        float a = leg * Mathf.PI / 3f;
                        if (CircleBlocked(p, radius, center + new Vector3(Mathf.Cos(a) * 4.4f, 0, Mathf.Sin(a) * 4.4f), 0.65f))
                            return true;
                    }
                    return false;
                case 5: // North Obelisk + 6 pylons
                    if (BoxBlocked(p, radius, center, 2.8f, 2.8f)) return true;
                    for (int pylon = 0; pylon < 6; pylon++)
                    {
                        float a = pylon * Mathf.PI / 3f;
                        if (CircleBlocked(p, radius, center + new Vector3(Mathf.Cos(a) * 6.4f, 0, Mathf.Sin(a) * 6.4f), 0.62f))
                            return true;
                    }
                    return false;
                case 6: // Radio Spire
                    return BoxBlocked(p, radius, center, 3.4f, 3.4f);
                case 7: // Silo Plaza
                    return CircleBlocked(p, radius, center + new Vector3(-3.8f, 0, 0), 2.75f) ||
                           CircleBlocked(p, radius, center + new Vector3(3.8f, 0, 0), 2.75f) ||
                           BoxBlocked(p, radius, center + new Vector3(-6.7f, 0, 0), 0.45f, 0.45f);
                case 8: // Deepwood Shrine + Torii gate columns
                    return CircleBlocked(p, radius, center, 4.7f) ||
                           CircleBlocked(p, radius, center + new Vector3(-2.4f, 0, -6.5f), 0.35f) ||
                           CircleBlocked(p, radius, center + new Vector3(2.4f, 0, -6.5f), 0.35f);
                case 9: // Botanical Dome
                    return CircleBlocked(p, radius, center, 6.7f);
                case 10: // Observatory Dome
                    return CircleBlocked(p, radius, center, 5.45f);
                case 11: // War Cenotaph
                    if (BoxBlocked(p, radius, center, 3.8f, 2.6f)) return true;
                    for (int b = 0; b < 4; b++)
                    {
                        float sx = (b & 1) == 0 ? -4.8f : 4.8f, sz = (b & 2) == 0 ? -3.6f : 3.6f;
                        if (CircleBlocked(p, radius, center + new Vector3(sx, 0, sz), 0.65f)) return true;
                    }
                    return false;
                case 12: // Rail Turntable / Armored Locomotive
                    return RotatedBoxBlocked(p, radius, center, 5.65f, 1.65f, 22f);
                case 13: // Goliath Crane (4 portal legs)
                    for (int side = -1; side <= 1; side += 2)
                        for (int fb = -1; fb <= 1; fb += 2)
                            if (CircleBlocked(p, radius, center + new Vector3(side * 5.2f, 0, fb * 2.6f), 0.65f))
                                return true;
                    return false;
                case 14: // Cooling Towers + turbine hall
                    return CircleBlocked(p, radius, center + new Vector3(-4.2f, 0, 1.2f), 3.15f) ||
                           CircleBlocked(p, radius, center + new Vector3(4.2f, 0, 1.2f), 2.70f) ||
                           BoxBlocked(p, radius, center + new Vector3(0, 0, -4.2f), 4.3f, 2.1f);
                case 15: // Blast Furnace + 3 Cowper stoves
                    if (CircleBlocked(p, radius, center + new Vector3(-1.8f, 0, 0), 2.85f)) return true;
                    for (int s = 0; s < 3; s++)
                        if (CircleBlocked(p, radius, center + new Vector3(3.8f, 0, -3.2f + s * 3.2f), 1.22f))
                            return true;
                    return false;
                case 16: // Amphitheater rear shell
                    return BoxBlocked(p, radius, center + new Vector3(0, 0, 3.0f), 5.1f, 3.2f);
                case 17: // Radar Array pedestal
                    return CircleBlocked(p, radius, center, 3.6f);
                case 18: // Oil Derrick + pumpjack
                    return BoxBlocked(p, radius, center, 2.2f, 2.2f) ||
                           BoxBlocked(p, radius, center + new Vector3(4.6f, 0, 0), 0.7f, 0.7f) ||
                           BoxBlocked(p, radius, center + new Vector3(4.6f, 0, -2.6f), 0.5f, 0.5f);
                case 19: // Cathedral Spire
                    return BoxBlocked(p, radius, center, 4.3f, 2.9f) ||
                           BoxBlocked(p, radius, center + new Vector3(-3.8f, 0, -0.4f), 1.3f, 2.1f) ||
                           BoxBlocked(p, radius, center + new Vector3(3.8f, 0, -0.4f), 1.3f, 2.1f);
                case 20: // Wind Turbine pedestal
                    return CircleBlocked(p, radius, center, 2.25f);
                case 21: // Command Bunker
                    return CircleBlocked(p, radius, center, 7.2f);
                default: // Launch Gantry pad & rocket
                    return BoxBlocked(p, radius, center, 5.8f, 5.8f);
            }
        }

        public const float StreetLampHeight = 3.0f;

        public static void PlaceNormalizedProp(Transform chunkTransform, GameObject prefab, Vector3 position, float yaw, float targetHeight)
        {
            if (prefab == null) return;
            var b = ImportedWorldBatch.SourceBounds(prefab);
            if (b.size.y < .001f) return;
            float scale = targetHeight / b.size.y;
            Quaternion rot = Quaternion.Euler(0, yaw, 0);
            Vector3 offset = rot * new Vector3(-b.center.x * scale, -b.min.y * scale, -b.center.z * scale);
            ImportedWorldBatch.Add(chunkTransform, prefab, position + offset, rot, Vector3.one * scale);
        }
    }
}
