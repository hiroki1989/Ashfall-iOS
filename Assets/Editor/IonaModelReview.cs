using System.IO;
using UnityEditor;
using UnityEngine;

public static class IonaModelReview
{
    static Texture2D LoadNormal(string assetPath)
    {
        if (!File.Exists(assetPath)) return null;
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.NormalMap)
        {
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    public static void Run()
    {
        var light = new GameObject("l").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.28f; light.transform.rotation = Quaternion.Euler(42, -28, 0);
        var fill = new GameObject("fill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = 0.55f; fill.color = new Color(.78f, .86f, .96f); fill.transform.rotation = Quaternion.Euler(25, 135, 0);
        RenderSettings.ambientLight = new Color(.42f, .43f, .45f);
        var cam = new GameObject("c").AddComponent<Camera>(); cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.16f, .17f, .19f);
        var rt = new RenderTexture(720, 960, 24); cam.targetTexture = rt;
        var tex = new Texture2D(720, 960, TextureFormat.RGB24, false);
        Directory.CreateDirectory("D:/Unity/AssetIntegration/Iona");
        AssetDatabase.Refresh();

        foreach (var key in new[] { "Iona", "Sera", "Briggs" })
        {
            string fbxPath = "Assets/Resources/Characters/" + key + "/" + key + ".fbx";
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            var model = Resources.Load<GameObject>("Characters/" + key + "/" + key);
            if (!model) { Debug.LogError("HUB_NPC_REVIEW missing " + key); EditorApplication.Exit(1); return; }
            var root = new GameObject(key).transform; root.rotation = Quaternion.LookRotation(Vector3.back);
            var inst = Object.Instantiate(model, root, false);
            var r = inst.GetComponentInChildren<Renderer>();
            foreach (var sm in r.sharedMaterials)
            {
                if (!sm) continue;
                string mName = sm.name.ToLowerInvariant();
                string tFile = mName.Contains("leather") ? "iona_leather"
                             : mName.Contains("brass") ? "iona_brass"
                             : mName.Contains("pants") ? "iona_pants"
                             : mName.Contains("hair") ? "iona_hair"
                             : mName.Contains("helmet") ? "npc_helmet_olive"
                             : mName.Contains("canister") ? (key.ToLowerInvariant() + "_canister_cap")
                             : mName.Contains("vent") ? (key.ToLowerInvariant() + "_vent")
                             : mName.Contains("lens") ? (key.ToLowerInvariant() + "_lens")
                             : mName.Contains("dial") ? (key.ToLowerInvariant() + "_dial")
                             : mName.Contains("steel") ? "sera_apron_metal"
                             : mName.Contains("armor") ? (key == "Sera" ? "sera_apron_metal" : "briggs_vest_navy")
                             : mName.Contains("jacket") ? (key == "Sera" ? "sera_blue_jacket" : key == "Briggs" ? "briggs_coat_brown" : "iona_jacket")
                             : mName.Contains("hood") ? (key == "Sera" ? "npc_helmet_olive" : "briggs_scarf_dark")
                             : null;
                if (tFile != null && sm.mainTexture == null)
                {
                    var t = Resources.Load<Texture2D>("Characters/" + key + "/Textures/" + tFile) ?? Resources.Load<Texture2D>("Characters/Iona/Textures/" + tFile);
                    if (t) { sm.mainTexture = t; sm.color = Color.white; }
                }
                if (tFile != null)
                {
                    var nm = LoadNormal("Assets/Resources/Characters/" + key + "/Textures/" + tFile + "_n.png");
                    if (nm)
                    {
                        sm.EnableKeyword("_NORMALMAP");
                        sm.SetTexture("_BumpMap", nm);
                        sm.SetFloat("_BumpScale", 0.85f);
                    }
                }
                if (mName.Contains("brass") || mName.Contains("canister")) { sm.SetFloat("_Metallic", 0.72f); sm.SetFloat("_Glossiness", 0.62f); }
                else if (mName.Contains("steel") || (key == "Sera" && mName.Contains("armor"))) { sm.SetFloat("_Metallic", 0.58f); sm.SetFloat("_Glossiness", 0.52f); }
                else if (mName.Contains("helmet") || mName.Contains("vent")) { sm.SetFloat("_Metallic", 0.42f); sm.SetFloat("_Glossiness", 0.48f); }
                else if (mName.Contains("lens") || mName.Contains("dial")) { sm.SetFloat("_Metallic", 0.25f); sm.SetFloat("_Glossiness", 0.88f); }
                else if (mName.Contains("leather")) { sm.SetFloat("_Metallic", 0.08f); sm.SetFloat("_Glossiness", 0.42f); }
                else { sm.SetFloat("_Metallic", 0.02f); sm.SetFloat("_Glossiness", 0.18f); }
                EditorUtility.SetDirty(sm);
            }
            var b = r.bounds;
            string pfx = key == "Iona" ? "unity" : key.ToLowerInvariant() + "-unity";
            RenderTexture.active = rt;
            cam.orthographicSize = 1.05f; cam.transform.position = new Vector3(6, 7, -6) + Vector3.up * .9f; cam.transform.LookAt(new Vector3(0, .92f, 0));
            cam.Render(); tex.ReadPixels(new Rect(0, 0, 720, 960), 0, 0);
            File.WriteAllBytes("D:/Unity/AssetIntegration/Iona/" + pfx + "-review.png", tex.EncodeToPNG());
            cam.orthographicSize = .98f; cam.transform.position = new Vector3(1.1f, 1.25f, -4); cam.transform.LookAt(new Vector3(0, .94f, 0));
            cam.Render(); tex.ReadPixels(new Rect(0, 0, 720, 960), 0, 0);
            File.WriteAllBytes("D:/Unity/AssetIntegration/Iona/" + pfx + "-front.png", tex.EncodeToPNG());
            float fh = b.max.y - .18f;
            cam.orthographicSize = .24f; cam.transform.position = new Vector3(.28f, fh + .01f, -3); cam.transform.LookAt(new Vector3(0, fh - .01f, 0));
            cam.Render(); tex.ReadPixels(new Rect(0, 0, 720, 960), 0, 0);
            File.WriteAllBytes("D:/Unity/AssetIntegration/Iona/" + pfx + "-face.png", tex.EncodeToPNG());
            Debug.Log("IONA_REVIEW " + key + " ok bounds=" + b + " mats=" + r.sharedMaterials.Length);
            Object.DestroyImmediate(root.gameObject);
        }
        // Side-by-side scale comparison with Player (DetailedHeroPlayer3D)
        var cmpRoot = new GameObject("ScaleCompare").transform;
        var playerActor = new GameObject("Player").transform;
        playerActor.SetParent(cmpRoot, false);
        playerActor.position = new Vector3(-1.35f, .65f, 0);
        playerActor.rotation = Quaternion.LookRotation(Vector3.back);
        NeonVault.DetailedHeroPlayer3D.Create(playerActor);
        float[] xs = { -0.45f, 0.45f, 1.35f };
        string[] keys = { "Iona", "Sera", "Briggs" };
        for (int i = 0; i < 3; i++)
        {
            var m = Resources.Load<GameObject>("Characters/" + keys[i] + "/" + keys[i]);
            var nRoot = new GameObject(keys[i]).transform;
            nRoot.SetParent(cmpRoot, false);
            nRoot.position = new Vector3(xs[i], 0, 0);
            nRoot.rotation = Quaternion.LookRotation(Vector3.back);
            var npc = Object.Instantiate(m, nRoot, false);
            npc.transform.localScale = Vector3.one * (keys[i] == "Iona" ? 1.20f : 1.09f);
        }
        var rtCmp = new RenderTexture(1200, 800, 24); cam.targetTexture = rtCmp;
        var texCmp = new Texture2D(1200, 800, TextureFormat.RGB24, false);
        RenderTexture.active = rtCmp;
        cam.orthographicSize = 1.45f; cam.transform.position = new Vector3(0, 1.25f, -5); cam.transform.LookAt(new Vector3(0, 1.15f, 0));
        cam.Render(); texCmp.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0);
        File.WriteAllBytes("D:/Unity/AssetIntegration/Iona/hub-npc-scale-compare.png", texCmp.EncodeToPNG());
        Object.DestroyImmediate(cmpRoot.gameObject);
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }
}
