using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonVault
{
    public static class BossModelReview
    {
        const string OutDir = "D:/Unity/AssetIntegration/FactionBossesHQ";

        public static void Run()
        {
            ConfigureTextureAssets();
            Directory.CreateDirectory(OutDir);

            RenderLineup("unity_bosses_front.png", new Vector3(0f, 1.25f, -4.6f), new Vector3(0f, 1.08f, 0f), 1.45f, false, 1.35f);
            RenderLineup("unity_bosses_combat_iso.png", new Vector3(-3.6f, 4.4f, -3.6f), new Vector3(0f, 0.96f, 0.1f), 1.70f, true, 1.35f);
            RenderLineup("unity_bosses_closeup.png", new Vector3(0f, 1.85f, -2.8f), new Vector3(0f, 1.72f, 0.05f), 0.78f, false, 0.96f);
            RenderMotionVerificationStrip("unity_bosses_motions.png");
            RenderAll4VsPlayer("unity_all4_vs_player_scale.png");

            Debug.Log("BOSS_MODEL_REVIEW_COMPLETE");
        }

        public static void RunAll4()
        {
            Directory.CreateDirectory(OutDir);
            RenderAll4VsPlayer("unity_all4_vs_player_scale.png");
            Debug.Log("BOSS_MODEL_REVIEW_ALL4_COMPLETE");
        }

        public static void RunMotionQA()
        {
            Directory.CreateDirectory(OutDir);
            RenderMotionVerificationStrip("unity_raven_vex_motion.png");
            var root = CreateStudioStage(out var floorMat);
            try
            {
                string[] poses = { "idle", "walk", "attack", "death" };
                float[] times = { .20f, .45f, .20f, .70f };
                for (int i = 0; i < poses.Length; i++)
                {
                    var actor = ImportedEnemyVisual.TryCreateBossNPC(root.transform,
                        new Vector3((i - 1.5f) * 1.45f, 0f, 0f));
                    if (!actor) throw new System.InvalidOperationException("Vale motion prefab missing");
                    var driver = actor.GetComponent<ImportedEnemyVisual>();
                    driver.enabled = false;
                    driver.ReviewPose(poses[i], times[i]);
                    Debug.Log("VALE_MOTION_QA " + poses[i] + " " + driver.DiagnosticReport());
                }
                RenderShot(root, "unity_vale_motion.png", new Vector3(0f, 3.2f, -7f),
                    new Vector3(0f, 1f, 0f), 2f, 1800, 900);
            }
            finally
            {
                Object.DestroyImmediate(floorMat);
                Object.DestroyImmediate(root);
            }
            Debug.Log("BOSS_MODEL_MOTION_QA_COMPLETE");
        }

        static void ConfigureTextureAssets()
        {
            foreach (string charName in new[] { "Raven", "Vex", "ValeProps", "ValeBoss" })
            {
                string fbx = "Assets/Resources/Characters/" + charName + "/" + charName + ".fbx";
                var model = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (model != null)
                {
                    bool dirty = false;
                    if (!model.isReadable) { model.isReadable = true; dirty = true; }
                    var targetType = charName == "ValeProps" ? ModelImporterAnimationType.None : ModelImporterAnimationType.Generic;
                    if (model.animationType != targetType) { model.animationType = targetType; dirty = true; }
                    if (model.optimizeBones) { model.optimizeBones = false; dirty = true; }
                    if (dirty) model.SaveAndReimport();
                }

                string texDir = "Assets/Resources/Characters/" + charName + "/Textures";
                if (!Directory.Exists(texDir)) continue;
                foreach (string file in Directory.GetFiles(texDir, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (imp == null) continue;
                    bool dirty = false;
                    if (assetPath.EndsWith("_n.png"))
                    {
                        if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
                    }
                    else if (assetPath.EndsWith("_m.png") || assetPath.EndsWith("_ao.png"))
                    {
                        if (imp.sRGBTexture) { imp.sRGBTexture = false; dirty = true; }
                    }
                    if (imp.maxTextureSize < 2048) { imp.maxTextureSize = 2048; dirty = true; }
                    if (imp.filterMode != FilterMode.Trilinear) { imp.filterMode = FilterMode.Trilinear; dirty = true; }
                    if (imp.anisoLevel < 8) { imp.anisoLevel = 8; dirty = true; }
                    if (imp.textureCompression != TextureImporterCompression.CompressedHQ) { imp.textureCompression = TextureImporterCompression.CompressedHQ; dirty = true; }
                    if (dirty) imp.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();
        }

        static GameObject CreateStudioStage(out Material floorMat)
        {
            var root = new GameObject("BossModelReviewStage");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(4f, 1f, 3f);
            floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.16f, 0.17f, 0.19f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.transform.SetParent(root.transform, false);
            key.type = LightType.Directional;
            key.intensity = 1.48f;
            key.color = new Color(1f, 0.96f, 0.90f);
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(42f, 150f, 0f);

            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(root.transform, false);
            fill.type = LightType.Directional;
            fill.intensity = 0.74f;
            fill.color = new Color(0.75f, 0.85f, 1f);
            fill.transform.rotation = Quaternion.Euler(28f, -140f, 0f);
            return root;
        }

        static void RenderShot(GameObject root, string fileName, Vector3 camPos, Vector3 lookAt, float orthoSize, int w = 1440, int h = 900)
        {
            var camGo = new GameObject("ReviewCam");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.position = camPos;
            camGo.transform.LookAt(lookAt);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.14f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;

            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, fileName), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        static void RenderLineup(string fileName, Vector3 camPos, Vector3 lookAt, float orthoSize, bool combatPose, float spacing = 1.35f)
        {
            var root = CreateStudioStage(out var floorMat);
            Quaternion actorRot = combatPose ? Quaternion.Euler(0f, 120f, 0f) : Quaternion.Euler(0f, 175f, 0f);

            SpawnBoss(root.transform, "Raven", new Vector3(-spacing, 0f, 0f), actorRot, combatPose, -1f, false, false, -1f, () =>
            {
                var p = new GameObject("RavenActor").transform;
                var v = HumanFactionVisual.CreateFactionBoss(p, 1);
                v.transform.localScale = Vector3.one * 1.20f;
                return (p, v);
            });

            SpawnBoss(root.transform, "Vex", Vector3.zero, actorRot, combatPose, -1f, false, false, -1f, () =>
            {
                var p = new GameObject("VexActor").transform;
                var v = HumanFactionVisual.CreateFactionBoss(p, 2);
                v.transform.localScale = Vector3.one * 1.15f;
                return (p, v);
            });

            var valeNpc = ImportedEnemyVisual.TryCreateBossNPC(root.transform, new Vector3(spacing, 0f, 0f));
            if (valeNpc)
            {
                valeNpc.localRotation = actorRot;
                valeNpc.localScale = Vector3.one;
                var drv = valeNpc.GetComponent<ImportedEnemyVisual>();
                if (drv) drv.enabled = false;
            }

            RenderShot(root, fileName, camPos, lookAt, orthoSize);
            Object.DestroyImmediate(floorMat);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Renders a 4-state motion verification grid for Raven & Vex:
        /// Left-stride walk, Right-stride walk, Combat crouch, and Death collapse.
        /// </summary>
        static void RenderMotionVerificationStrip(string fileName)
        {
            var root = CreateStudioStage(out var floorMat);
            Quaternion sideIso = Quaternion.Euler(0f, 110f, 0f);
            float[] xs = { -2.4f, -0.8f, 0.8f, 2.4f };
            float[] phases = { 0.25f, 0.75f, -1f, -1f };
            bool[] crouches = { false, false, true, false };
            float[] deaths = { -1f, -1f, -1f, 0.85f };

            for (int col = 0; col < 4; col++)
            {
                int cIdx = col;
                SpawnBoss(root.transform, "Raven_M" + col, new Vector3(xs[col], 0f, 1.3f), sideIso,
                    true, phases[cIdx], crouches[cIdx], false, deaths[cIdx], () =>
                    {
                        var p = new GameObject("RavenM").transform;
                        var v = HumanFactionVisual.CreateFactionBoss(p, 1);
                        v.transform.localScale = Vector3.one * 1.15f;
                        return (p, v);
                    });

                SpawnBoss(root.transform, "Vex_M" + col, new Vector3(xs[col], 0f, -1.1f), sideIso,
                    true, phases[cIdx], crouches[cIdx], false, deaths[cIdx], () =>
                    {
                        var p = new GameObject("VexM").transform;
                        var v = HumanFactionVisual.CreateFactionBoss(p, 2);
                        v.transform.localScale = Vector3.one * 1.15f;
                        return (p, v);
                    });
            }

            RenderShot(root, fileName, new Vector3(0f, 5.2f, -6.8f), new Vector3(0f, 0.85f, 0.1f), 2.55f, 1600, 960);
            Object.DestroyImmediate(floorMat);
            Object.DestroyImmediate(root);
        }

        static void RenderAll4VsPlayer(string fileName)
        {
            var root = CreateStudioStage(out var floorMat);
            try
            {
                var player = new GameObject("Player scale reference").transform;
                player.SetParent(root.transform, false);
                player.localPosition = new Vector3(-2.5f, .65f, 0f);
                player.localRotation = Quaternion.identity;
                DetailedHeroPlayer3D.Create(player);
                var keys = new[] { "Iona", "Briggs", "Raven", "ValeBoss" };
                for (int i = 0; i < keys.Length; i++)
                {
                    var position = new Vector3((i - .5f) * 1.25f, 0f, 0f);
                    if (keys[i] == "ValeBoss")
                    {
                        var live = ImportedEnemyVisual.TryCreateBossNPC(root.transform, position);
                        if (!live) throw new System.InvalidOperationException("ValeBoss runtime prefab missing");
                        var driver = live.GetComponent<ImportedEnemyVisual>();
                        if (driver) driver.enabled = false;
                        LogHeight(keys[i] + " live", live);
                        continue;
                    }
                    if (keys[i] == "Raven")
                    {
                        var live = new GameObject("Raven live actor").transform;
                        live.SetParent(root.transform, false);
                        live.localPosition = position;
                        var visual = HumanFactionVisual.CreateFactionBoss(live, 1);
                        visual.transform.localScale = Vector3.one * 1.13f;
                        visual.transform.localPosition = Vector3.up * .10f;
                        visual.enabled = false;
                        LogHeight(keys[i] + " live", live);
                        continue;
                    }
                    var prefab = Resources.Load<GameObject>("Characters/" + keys[i] + "/" + keys[i]);
                    if (!prefab) { Debug.LogWarning("BossModelReview missing model: " + keys[i]); continue; }
                    var actor = new GameObject(keys[i] + " scale review").transform;
                    actor.SetParent(root.transform, false);
                    actor.localPosition = position;
                    actor.localRotation = Quaternion.Euler(0f, 175f, 0f);
                    var model = Object.Instantiate(prefab, actor, false);
                    model.transform.localScale = Vector3.one * (keys[i] == "Iona" ? 1.20f : keys[i] == "Briggs" ? 1.09f : 1.20f);
                    foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
                    LogHeight(keys[i], actor);
                }
                RenderShot(root, fileName, new Vector3(0f, 2.15f, -7.5f),
                    new Vector3(0f, 1.1f, 0f), 1.85f, 2400, 900);
            }
            finally
            {
                Object.DestroyImmediate(floorMat);
                Object.DestroyImmediate(root);
            }
        }

        static void LogHeight(string label, Transform root)
        {
            Bounds bounds = default;
            bool found = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            Debug.Log("ALL4_HEIGHT " + label + "=" + (found ? bounds.size.y.ToString("F3") : "NO_RENDERER")
                + (found ? " minY=" + bounds.min.y.ToString("F3") : ""));
        }

        static void SpawnBoss(Transform parent, string label, Vector3 pos, Quaternion rot,
            bool firingPose, float phase, bool crouch, bool reload, float deathTime,
            System.Func<(Transform, HumanFactionVisual)> builder)
        {
            var (actor, visual) = builder();
            actor.SetParent(parent, false);
            actor.localPosition = pos;
            actor.localRotation = rot;
            if (!visual || !visual.Muzzle)
                Debug.LogWarning("BossModelReview: missing visual/muzzle on " + label);
            if (visual)
                visual.PreviewMotion(firingPose, phase, crouch, reload, deathTime);
        }

        public static void MeasureHeights()
        {
            var root = new GameObject("HeightMeasure");
            try
            {
                var pActor = new GameObject("Player").transform;
                pActor.SetParent(root.transform, false);
                var hero = DetailedHeroPlayer3D.Create(pActor);
                Bounds pb = default;
                bool hasPb = false;
                foreach (var r in pActor.GetComponentsInChildren<Renderer>(true))
                {
                    if (!r.enabled) continue;
                    string rn = r.name.ToLowerInvariant();
                    if (rn.Contains("backpack")) continue;
                    if (!hasPb) { pb = r.bounds; hasPb = true; }
                    else pb.Encapsulate(r.bounds);
                }
                Debug.Log($"HEIGHT_MEASURE Player: min={pb.min.y:F4} max={pb.max.y:F4} height={pb.size.y:F4} fullScale={pActor.localScale.x:F3}");

                var vale = ImportedEnemyVisual.TryCreateBossNPC(root.transform, Vector3.zero);
                if (vale)
                {
                    Bounds vb = default; bool hasVb = false;
                    foreach (var r in vale.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!r.enabled) continue;
                        if (!hasVb) { vb = r.bounds; hasVb = true; }
                        else vb.Encapsulate(r.bounds);
                    }
                    Debug.Log($"HEIGHT_MEASURE ValeNPC: min={vb.min.y:F4} max={vb.max.y:F4} height={vb.size.y:F4}");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
