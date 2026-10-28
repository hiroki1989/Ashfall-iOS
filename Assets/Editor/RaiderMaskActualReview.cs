using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonVault.Editor {
 public static class RaiderMaskActualReview {
  public static void Run(){Directory.CreateDirectory("D:/Unity/AssetIntegration/RaiderMaskActual");RenderSettings.ambientLight=Color.gray;var light=new GameObject("light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(40,-30,0);var camera=new GameObject("camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.36f;camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.14f);
   foreach(var variant in new[]{0,1,2}){bool boss=variant!=0;var actor=new GameObject("review actor");var foe=actor.AddComponent<Foe>();foe.enabled=false;foe.kind=boss?3:10;foe.SetRaiderBoss(boss);var prefab=Resources.Load<GameObject>("ImportedEnemies/"+(variant==2?"RaiderBossNPC":boss?"Enemy_11":"Enemy_10"));var instance=Object.Instantiate(prefab,actor.transform,false);var driver=instance.GetComponent<ImportedEnemyVisual>();typeof(ImportedEnemyVisual).GetMethod("Bind",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(driver,new object[]{actor.transform,variant==2?null:foe,-.09f});var socket=instance.transform.Find("nonexistent");foreach(var t in instance.GetComponentsInChildren<Transform>(true))if(t.name=="Raider respirator socket")socket=t;if(!socket)throw new System.Exception("mask missing");
    for(int i=0;i<2;i++){var center=socket.position;camera.transform.position=center+(i==0?Vector3.forward:Vector3.right)*3;camera.transform.LookAt(center);var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var img=new Texture2D(900,900,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,900,900),0,0);img.Apply();File.WriteAllBytes("D:/Unity/AssetIntegration/RaiderMaskActual/"+(variant==2?"boss-npc":boss?"boss":"raider")+"-"+i+".png",img.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(img);Object.DestroyImmediate(rt);}Object.DestroyImmediate(actor);}
   Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);Debug.Log("RAIDER_MASK_ACTUAL_CAPTURED");
  }
 }
}
