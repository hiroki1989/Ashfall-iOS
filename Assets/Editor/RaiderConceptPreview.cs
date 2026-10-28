using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonVault.Editor {
 public static class RaiderConceptPreview {
  public static void Run(){
   var source=Resources.Load<GameObject>("ImportedEnemies/Enemy_10");if(!source)throw new System.Exception("Raider source missing");
   var actor=Object.Instantiate(source);actor.name="Raider concept / urban scavenger";
   var head=System.Array.Find(actor.GetComponentsInChildren<Transform>(true),t=>t.name=="Head_M");if(!head)throw new System.Exception("Head bone missing");
   var socket=new GameObject("Respirator socket").transform;socket.SetParent(head,true);socket.position=head.position+Vector3.up*.075f+Vector3.forward*.12f;socket.rotation=Quaternion.identity;
   var helper=typeof(ImportedEnemyVisual).Assembly.GetType("NeonVault.GeneratedWardenPreviewAssets");var method=helper?.GetMethod("CreateFittedMask",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);if(method==null||method.Invoke(null,new object[]{socket})==null)throw new System.Exception("Respirator missing");
   var camera=new GameObject("Concept camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.3f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.10f);camera.nearClipPlane=.01f;camera.farClipPlane=100;
   var light=new GameObject("Concept light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
   Directory.CreateDirectory("D:/Unity/AssetIntegration/RaiderConcept");
   for(int i=0;i<2;i++){camera.transform.position=new Vector3(i==0?0:3,1.45f,i==0?6:5);camera.transform.LookAt(new Vector3(0,1.05f,0));var rt=new RenderTexture(900,1100,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var img=new Texture2D(900,1100,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,900,1100),0,0);img.Apply();File.WriteAllBytes("D:/Unity/AssetIntegration/RaiderConcept/raider-"+i+".png",img.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(img);Object.DestroyImmediate(rt);}
   Object.DestroyImmediate(actor);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);Debug.Log("RAIDER_CONCEPT_CAPTURED");
  }
 }
}
