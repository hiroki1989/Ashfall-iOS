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

            RenderLineup("unity_bosses_front.png", new Vector3(0f, 1.25f, -4.6f), new Vector3(0f, 1.02f, 0f), 1.40f, false, 1.25f);
            RenderLineup("unity_bosses_combat_iso.png", new Vector3(-3.6f, 4.4f, -3.6f), new Vector3(0f, 0.90f, 0.1f), 1.65f, true, 1.35f);
            RenderLineup("unity_bosses_closeup.png", new Vector3(0f, 1.78f, -2.8f), new Vector3(0f, 1.62f, 0.05f), 0.78f, false, 0.92f);

            Debug.Log("BOSS_MODEL_REVIEW_COMPLETE");
        }

        static void ConfigureTextureAssets()
        {
            foreach (string boss in new[] { "Raven", "Vex" })
            {
                string fbx = "Assets/Resources/Characters/" + boss + "/" + boss + ".fbx";
                var model = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (model != null)
                {
                    bool dirty = false;
                    if (!model.isReadable) { model.isReadable = true; dirty = true; }
                    if (model.animationType != ModelImporterAnimationType.Generic) { model.animationType = ModelImporterAnimationType.Generic; dirty = true; }
                    if (model.optimizeBones) { model.optimizeBones = false; dirty = true; }
                    if (dirty) model.SaveAndReimport();
                }

                string texDir = "Assets/Resources/Characters/" + boss + "/Textures";
                if (!Directory.Exists(texDir)) continue;
                foreach (string file in Directory.GetFiles(texDir, "*_n.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (imp != null && imp.textureType != TextureImporterType.NormalMap)
                    {
                        imp.textureType = TextureImporterType.NormalMap;
                        imp.SaveAndReimport();
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        static void RenderLineup(string fileName, Vector3 camPos, Vector3 lookAt, float orthoSize, bool combatPose, float spacing = 1.35f)
        {
            var root = new GameObject("BossModelReviewStage");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            var floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.16f, 0.17f, 0.19f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.transform.SetParent(root.transform, false);
            key.type = LightType.Directional;
            key.intensity = 1.45f;
            key.color = new Color(1f, 0.96f, 0.90f);
            key.transform.rotation = Quaternion.Euler(42f, 150f, 0f);

            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(root.transform, false);
            fill.type = LightType.Directional;
            fill.intensity = 0.72f;
            fill.color = new Color(0.75f, 0.85f, 1f);
            fill.transform.rotation = Quaternion.Euler(28f, -140f, 0f);

            Quaternion actorRot = combatPose ? Quaternion.Euler(0f, 120f, 0f) : Quaternion.Euler(0f, 175f, 0f);

            SpawnBoss(root.transform, "Raven", -spacing, actorRot, combatPose, () =>
            {
                var p = new GameObject("RavenActor").transform;
                var v = HumanFactionVisual.CreateFactionBoss(p, 1);
                v.transform.localScale = Vector3.one * 1.15f;
                return (p, v);
            });

            SpawnBoss(root.transform, "Vex", 0f, actorRot, combatPose, () =>
            {
                var p = new GameObject("VexActor").transform;
                var v = HumanFactionVisual.CreateFactionBoss(p, 2);
                v.transform.localScale = Vector3.one * 1.15f;
                return (p, v);
            });

            SpawnBoss(root.transform, "Vale", spacing, actorRot, combatPose, () =>
            {
                var p = new GameObject("ValeActor").transform;
                var v = HumanFactionVisual.CreateVale(p);
                v.transform.localScale = Vector3.one * 1.20f;
                return (p, v);
            });

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
            cam.farClipPlane = 30f;

            int w = 1440, h = 900;
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
            Object.DestroyImmediate(floorMat);
            Object.DestroyImmediate(root);
        }

        static void SpawnBoss(Transform parent, string label, float x, Quaternion rot, bool firingPose, System.Func<(Transform, HumanFactionVisual)> builder)
        {
            var (actor, visual) = builder();
            actor.SetParent(parent, false);
            actor.localPosition = new Vector3(x, 0f, 0f);
            actor.localRotation = rot;
            if (!visual || !visual.Muzzle)
                Debug.LogWarning("BossModelReview: missing visual/muzzle on " + label);
            if (visual)
            {
                visual.PreviewPose(firingPose);
                var smr = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Debug.Log($"BossModelReview [{label}] smr={(smr ? smr.name : "none")} bones={(smr && smr.bones != null ? smr.bones.Length : -1)}");
            }
        }
    }
}
