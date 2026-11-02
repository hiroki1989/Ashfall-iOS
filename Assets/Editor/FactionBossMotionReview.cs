using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FactionBossMotionReview
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    public static void Run()
    {
        string folder="D:/Unity/BuildTemp/FactionBossMotionReview";
        try
        {
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=Color.gray;
            var light=new GameObject("review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(40,-30,0);
            var camera=new GameObject("review camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.10f);
            var actor=new GameObject("review boss");var foe=actor.AddComponent<Foe>();foe.enabled=false;foe.kind=3;foe.SetRaiderBoss(true);
            var prefab=Resources.Load<GameObject>("ImportedEnemies/Enemy_11");if(!prefab)throw new Exception("boss prefab unavailable");
            var obj=Object.Instantiate(prefab,actor.transform,false);var visual=obj.GetComponent<ImportedEnemyVisual>();
            typeof(ImportedEnemyVisual).GetMethod("Bind",Private).Invoke(visual,new object[]{actor.transform,foe,-.09f});
            var animate=typeof(ImportedEnemyVisual).GetMethod("Animate",Private);
            var bossMove=typeof(Foe).GetField("bossMove",Private);var charge=typeof(Foe).GetField("charge",Private);var charging=typeof(Foe).GetField("charging",Private);
            var rt=new RenderTexture(768,768,24);rt.Create();camera.targetTexture=rt;
            foreach(var pose in new[]{"idle","walk","gun","slam"})
            {
                charging.SetValue(foe,pose=="gun"||pose=="slam");
                bossMove.SetValue(foe,Enum.ToObject(bossMove.FieldType,pose=="gun"?3:pose=="slam"?1:0));charge.SetValue(foe,.35f);
                for(int i=0;i<8;i++)animate.Invoke(visual,new object[]{.05f,pose=="walk"?2f:0f});
                if(!visual.raiderRifle||!visual.raiderRifle.gameObject.activeInHierarchy)throw new Exception("rifle missing: "+pose);
                for(int angle=0;angle<2;angle++)
                {
                    camera.transform.position=actor.transform.position+new Vector3(angle==0?0:4,1.5f,angle==0?4:0);camera.transform.LookAt(actor.transform.position+Vector3.up*1.15f);camera.Render();
                    var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,pose+"-"+angle+".png"),tex.EncodeToPNG());Object.DestroyImmediate(tex);RenderTexture.active=old;
                }
            }
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS idle, walk, gun, slam: preserved boss prefab, active weapon, two angles; visual inspection required.\n");
            camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(actor);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);EditorApplication.Exit(0);
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL "+ex);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
