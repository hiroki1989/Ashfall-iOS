#if UNITY_EDITOR
using System;
using System.IO;
using NeonVault;
using UnityEditor;
using UnityEngine;

public static class NewCharacterCombatReview
{
    public static void Run()
    {
        NeonVault.Downloads2CharacterSetup.RunVexSera();
        FactionSoldierImport.Prepare();
        var root = new GameObject("New character review");
        var light = new GameObject("Review light").AddComponent<Light>();
        var camera = new GameObject("Review camera").AddComponent<Camera>();
        try
        {
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(40f, -45f, 0f);
            RenderSettings.ambientLight = Color.gray;
            camera.orthographic = true; camera.orthographicSize = 1.8f;
            camera.transform.position = new Vector3(4.8f, 2.8f, 9f);
            camera.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.24f,.26f,.29f);
            var visuals = new HumanFactionVisual[3];
            for (int i = 0; i < visuals.Length; i++)
            {
                var actor = new GameObject("Actor " + i).transform;
                actor.SetParent(root.transform, false);
                actor.localPosition = new Vector3((i - 1) * 1.35f, 0f, 0f);
                visuals[i] = i == 2 ? HumanFactionVisual.CreateFactionBoss(actor, 2) : HumanFactionVisual.Create(actor, i + 1, i);
                if (!visuals[i] || !visuals[i].Muzzle) throw new Exception("Missing character or weapon " + i);
                var skin = visuals[i].GetComponentInChildren<SkinnedMeshRenderer>();
                if (!skin || !skin.sharedMesh || skin.bones.Length < 10) throw new Exception("Missing rig " + i);
                var model = visuals[i].transform.Find(i == 2 ? "Vex Blender Boss Model" : "Faction soldier model");
                if (!model) throw new Exception("New model not selected " + i);
                Debug.Log("NEW_CHARACTER " + i + " vertices=" + skin.sharedMesh.vertexCount + " bones=" + skin.bones.Length);
            }
            var seraPrefab = Resources.Load<GameObject>("Characters/Sera/Sera");
            if (!seraPrefab) throw new Exception("Sera model missing");
            var sera = UnityEngine.Object.Instantiate(seraPrefab, root.transform, false);
            sera.name = "Sera"; sera.transform.localPosition = new Vector3(2.2f,0f,0f); sera.transform.localScale = Vector3.one * 1.09f;
            var rt = new RenderTexture(1600,900,24); camera.targetTexture = rt;
            var image = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            Directory.CreateDirectory("D:/Unity/AssetIntegration/FactionBossesHQ");
            foreach (var state in new[] { "idle", "fire", "walk", "death" })
            {
                for (int i=0;i<visuals.Length;i++)
                    visuals[i].PreviewMotion(state == "fire", state == "walk" ? .3f : -1f, false, false, state == "death" ? .5f : -1f);
                camera.Render(); camera.Render(); RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply();
                File.WriteAllBytes("D:/Unity/AssetIntegration/FactionBossesHQ/new_characters_"+state+".png",image.EncodeToPNG());
            }
            RenderTexture.active=null; camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("NEW_CHARACTER_REVIEW_PASS");
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
