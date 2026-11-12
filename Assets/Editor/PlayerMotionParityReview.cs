#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NeonVault
{
    // Renders the player next to every retargeted human (Raven, Vex, Veil, soldiers)
    // in identical idle / walk / fire states so the motions can be compared directly.
    public static class PlayerMotionParityReview
    {
        const string OutDir = "D:/Unity/AssetIntegration/FactionBossesHQ";

        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            Shoot("idle", false, -1f);
            Shoot("walk_a", false, .10f);
            Shoot("walk_b", false, .60f);
            Shoot("fire", true, -1f);
            Shoot("fire_walk", true, .35f);
            Debug.Log("PLAYER_MOTION_PARITY_REVIEW_PASS");
        }

        static void Shoot(string label, bool firing, float phase)
        {
            var stage = new GameObject("Parity stage");
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(40, -35, 0);
            RenderSettings.ambientLight = new Color(.55f, .57f, .6f);
            var cam = new GameObject("Cam").AddComponent<Camera>();
            try
            {
                float x = -3.25f; const float step = 1.3f;
                Vector3 dir = Quaternion.Euler(0, -60, 0) * Vector3.forward;
                // Player reference (same solver the in-game player uses).
                var p = new GameObject("Player").transform; p.SetParent(stage.transform); p.position = new Vector3(x, 0, 0);
                var hero = DetailedHero3D.Create(p, DetailedHeroMaterials3D.Get);
                hero.transform.localScale = Vector3.one * 1.28f;
                var rifle = DetailedHeroEquipment3D.BuildRifle(hero.transform, DetailedHeroMaterials3D.Get);
                DetailedHeroPlayer3D.Pose(hero, rifle, false, firing, dir, phase, phase >= 0 ? dir * 3f : Vector3.zero, phase >= 0 ? 1f : 0f);
                x += step;
                // Raven, Vex bosses and both soldier factions.
                foreach (var spec in new[] { (1, true), (2, true), (1, false), (2, false) })
                {
                    var a = new GameObject("Human").transform; a.SetParent(stage.transform); a.position = new Vector3(x, 0, 0); a.rotation = Quaternion.LookRotation(dir);
                    var v = spec.Item2 ? HumanFactionVisual.CreateFactionBoss(a, spec.Item1) : HumanFactionVisual.Create(a, spec.Item1, 0);
                    v.transform.localScale = Vector3.one * (spec.Item2 ? (spec.Item1 == 1 ? 1.13f : 1.20f) : 1.12f);
                    v.transform.localPosition = spec.Item2 && spec.Item1 == 1 ? Vector3.up * .10f : Vector3.down * .03f;
                    v.PreviewMotion(firing, phase, false, false, -1f);
                    x += step;
                }
                // Veil (imported Mixamo rig) through the live Foe path.
                var fo = new GameObject("Veil"); fo.transform.SetParent(stage.transform); fo.transform.position = new Vector3(x, 0, 0); fo.transform.rotation = Quaternion.LookRotation(dir);
                var foe = fo.AddComponent<Foe>(); foe.Init(null, 3, 1); foe.SetRaiderBoss(true);
                if (!ImportedEnemyVisual.TryAttach(fo, 11)) throw new Exception("Veil prefab missing");
                var drv = fo.GetComponentInChildren<ImportedEnemyVisual>();
                if (firing)
                {
                    var f = typeof(Foe).GetField("bossMove", BindingFlags.Instance | BindingFlags.NonPublic);
                    f.SetValue(foe, Enum.Parse(f.FieldType, "Gun"));
                }
                drv.ReviewPose(firing && phase >= 0 ? "fire_walk" : firing ? "attack" : phase >= 0 ? "walk" : "idle", phase >= 0 ? phase : .1f);

                cam.orthographic = true; cam.orthographicSize = 1.85f;
                cam.transform.position = new Vector3(-0.2f, 3.6f, -9f); cam.transform.LookAt(new Vector3(0f, 1.0f, 0));
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.30f, .33f, .37f);
                var rt = new RenderTexture(2000, 800, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
                RenderTexture.active = rt; var img = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); img.Apply();
                File.WriteAllBytes(OutDir + "/parity_" + label + ".png", img.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(img); UnityEngine.Object.DestroyImmediate(rt);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(light.gameObject); UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
        }
    }
}
#endif
