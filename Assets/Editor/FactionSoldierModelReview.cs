#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;

public static class FactionSoldierModelReview
{
    public static void Run()
    {
        FactionSoldierImport.Prepare();
        var root = new GameObject("Faction soldier review");
        var light = new GameObject("Review light").AddComponent<Light>();
        var camera = new GameObject("Review camera").AddComponent<Camera>();
        try
        {
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(40f, -45f, 0f);
            RenderSettings.ambientLight = Color.gray;
            camera.orthographic = true; camera.orthographicSize = 1.65f;
            camera.transform.position = new Vector3(3.4f, 2.5f, 7f);
            camera.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.24f,.26f,.29f);
            for (int faction = 1; faction <= 2; faction++)
            {
                var actor = new GameObject("Faction " + faction).transform;
                actor.SetParent(root.transform, false);
                actor.localPosition = new Vector3(faction == 1 ? -1.1f : 1.1f, 0, 0);
                var visual = HumanFactionVisual.Create(actor, faction, faction == 1 ? 0 : 1);
                if (!visual || !visual.Muzzle || !visual.transform.Find("Faction soldier model"))
                    throw new Exception("Missing imported soldier or weapon: " + faction);
                var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                if (!skin || !skin.sharedMesh || skin.bones.Length < 10)
                    throw new Exception("Missing soldier rig: " + faction);
                visual.PreviewMotion(true, faction == 1 ? -.1f : .8f, false, false, -1f);
                Debug.Log("FACTION_SOLDIER_REVIEW faction=" + faction + " vertices=" + skin.sharedMesh.vertexCount + " bones=" + skin.bones.Length);
            }
            var rt = new RenderTexture(1280,800,24); camera.targetTexture=rt; camera.Render();
            RenderTexture.active=rt;
            var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply();
            Directory.CreateDirectory("D:/Unity/AssetIntegration/FactionBossesHQ");
            File.WriteAllBytes("D:/Unity/AssetIntegration/FactionBossesHQ/faction_soldiers_new.png",image.EncodeToPNG());
            RenderTexture.active=null; camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("FACTION_SOLDIER_REVIEW_PASS");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(light.gameObject);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
    }
}
#endif
