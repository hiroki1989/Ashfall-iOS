#if UNITY_EDITOR
using System;
using System.IO;
using NeonVault;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class KnoxBodyReview
{
    public static void Run()
    {
        AssetDatabase.ImportAsset("Assets/Resources/Characters/Knox/Knox.fbx",ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        var root=new GameObject("Knox review"); var hero=DetailedHero3D.Create(root.transform,DetailedHeroMaterials3D.Get);
        foreach(var r in hero.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        var knox=KnoxBodyVisual.Create(hero);if(!knox)throw new Exception("Knox body could not be created");
        var asm=typeof(KnoxBodyVisual).Assembly;
        var assets=asm.GetType("NeonVault.GeneratedWardenPreviewAssets");var rig=asm.GetType("NeonVault.GeneratedWardenRig");
        var armorSource=(GameObject)assets.GetMethod("CreateCharacterPart").Invoke(null,new object[]{"armor",hero});
        var armorSkin=(SkinnedMeshRenderer)rig.GetMethod("Bind").Invoke(null,new object[]{armorSource,hero,false,true,true});
        var armorParts=(SkinnedMeshRenderer[])rig.GetMethod("SplitArmorBySlot").Invoke(null,new object[]{armorSkin,root.transform});
        knox.FitArmorBoundary(armorParts);
        if(armorSkin)armorSkin.enabled=false;
        foreach(var part in armorParts)if(part)part.enabled=false;
        var gun=DetailedHeroEquipment3D.BuildRifle(root.transform,DetailedHeroMaterials3D.Get);
        var light=new GameObject("light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);
        RenderSettings.ambientLight=new Color(.42f,.43f,.45f);
        var cam=new GameObject("camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.08f;cam.backgroundColor=new Color(.16f,.17f,.19f);cam.clearFlags=CameraClearFlags.SolidColor;
        var rt=new RenderTexture(720,960,24);cam.targetTexture=rt;var tex=new Texture2D(720,960,TextureFormat.RGB24,false);
        Directory.CreateDirectory("D:/Unity/AssetIntegration/KnoxCandidate");
        foreach(var mode in new[]{"idle","fire","walk","covered","equipped","chest","legs"})
        {
            DetailedHeroPlayer3D.Pose(hero,gun,false,mode=="fire",Vector3.forward,mode=="walk"?.3f:-1,mode=="walk"?Vector3.forward:Vector3.zero,mode=="walk"?1:0);knox.SendMessage("Follow");
            var covered=mode=="covered"||mode=="equipped"?new[]{false,true,true,true,true}:mode=="chest"?new[]{false,true,false,false,false}:mode=="legs"?new[]{false,false,false,true,false}:new bool[5];
            knox.SetCovered(covered);
            for(int index=0;index<armorParts.Length;index++)if(armorParts[index]){armorParts[index].enabled=false;if(index<covered.Length&&mode!="covered"&&covered[index])armorParts[index].enabled=true;}
            foreach(var part in knox.GetComponentsInChildren<SkinnedMeshRenderer>())if(part.name.StartsWith("Knox slot ")&&part.enabled){part.enabled=false;part.enabled=true;}
            cam.transform.position=new Vector3(-1.2f,1.25f,4);cam.transform.LookAt(new Vector3(0,.94f,0));cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,960),0,0);tex.Apply();
            File.WriteAllBytes("D:/Unity/AssetIntegration/KnoxCandidate/"+mode+".png",tex.EncodeToPNG());
        }
        RenderTexture.active=null;Object.DestroyImmediate(root);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
        Debug.Log("KNOX_BODY_REVIEW_PASS");EditorApplication.Exit(0);
    }
}
#endif
