#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NeonVault
{
    public static class BossWeaponAttachmentReview
    {
        public static void Run()
        {
            var stage = new GameObject("Boss weapon review");
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(40, -45, 0);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 3.0f;
            camera.transform.position = new Vector3(1.1f, 3.0f, 8f);
            camera.transform.LookAt(new Vector3(1.1f, 1.1f, 0));
            camera.backgroundColor = new Color(.35f, .38f, .42f); camera.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientLight = Color.gray;
            try
            {
                var foeObject = new GameObject("Vale actual combat"); foeObject.transform.SetParent(stage.transform);
                foeObject.transform.position = new Vector3(-1.2f, 0, 0);
                var foe = foeObject.AddComponent<Foe>(); foe.Init(null, 3, 1); foe.SetRaiderBoss(true);
                if (!ImportedEnemyVisual.TryAttach(foeObject, 11)) throw new Exception("Vale prefab missing");
                var driver = foeObject.GetComponentInChildren<ImportedEnemyVisual>();
                AssertVisibleHeight(driver, "Veil boss");
                driver.ReviewPose("idle", .1f);
                if (!driver.raiderRifle || !driver.raiderRifle.gameObject.activeInHierarchy) throw new Exception("Vale carry rifle missing");
                var gunField = typeof(Foe).GetField("bossMove", BindingFlags.Instance | BindingFlags.NonPublic);
                gunField.SetValue(foe, Enum.Parse(gunField.FieldType, "Gun"));
                driver.ReviewPose("attack", .1f);
                if (!driver.raiderRifle.gameObject.activeInHierarchy || !driver.rifleGripSocket || !driver.rifleSupportSocket) throw new Exception("Vale aiming rifle or grip missing");
                var ravenRoot = new GameObject("Raven combat"); ravenRoot.transform.SetParent(stage.transform);
                ravenRoot.transform.position = new Vector3(1.2f, 0, 0);
                var raven = HumanFactionVisual.CreateFactionBoss(ravenRoot.transform, 1);
                if (!raven || !raven.Muzzle) throw new Exception("Raven weapon missing");
                raven.PreviewMotion(true, -1, false, false, -1);
                var npc = ImportedEnemyVisual.TryCreateBossNPC(stage.transform, new Vector3(3.3f, 0, 0));
                if (!npc) throw new Exception("Veil NPC prefab missing");
                AssertVisibleHeight(npc.GetComponent<ImportedEnemyVisual>(), "Veil NPC");
                var rt = new RenderTexture(1280, 800, 24); camera.targetTexture = rt; camera.Render(); camera.Render();
                RenderTexture.active = rt; var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
                Directory.CreateDirectory("D:/Unity/AssetIntegration/FactionBossesHQ");
                File.WriteAllBytes("D:/Unity/AssetIntegration/FactionBossesHQ/actual_boss_weapons.png", image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
                Debug.Log("BOSS_WEAPON_ATTACHMENT_REVIEW_PASS");
            }
            finally { UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(light.gameObject); UnityEngine.Object.DestroyImmediate(camera.gameObject); }
        }

        static void AssertVisibleHeight(ImportedEnemyVisual driver, string label)
        {
            var skin = driver.model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (!skin) throw new Exception(label + " skin missing");
            var baked = new Mesh();
            skin.BakeMesh(baked, false);
            float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
            foreach (var vertex in baked.vertices)
            {
                float y = skin.transform.TransformPoint(vertex).y;
                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
            }
            UnityEngine.Object.DestroyImmediate(baked);
            float height = (top - bottom) / Mathf.Max(.001f, Mathf.Abs(skin.transform.lossyScale.y));
            Debug.Log(label + " ACTUAL_VISIBLE_HEIGHT=" + height.ToString("F3")
                + " modelScale=" + driver.model.localScale.ToString("F3")
                + " skinScale=" + skin.transform.lossyScale.ToString("F3"));
            if (height < 2.05f || height > 2.50f)
                throw new Exception(label + " height " + height.ToString("F3") + "m is not player sized");
        }
    }
}
#endif
