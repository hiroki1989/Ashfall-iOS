using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor {
 public static class CC0PreviewCapture {
  public static void Run() {
   Directory.CreateDirectory("D:/Unity/CC0AssetStaging/previews");
   string[] paths={"Assets/Resources/ImportedWorld/ApocalypticBuildings/building-01.prefab","Assets/Resources/ImportedWorld/CC0Buildings/Structures_1.prefab","Assets/Resources/ImportedWorld/undergrowth/weed_plant_02.prefab"};
   RenderSettings.ambientLight=Color.gray;
   var lightObject=new GameObject("Review light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;lightObject.transform.rotation=Quaternion.Euler(45,-40,0);
   var cameraObject=new GameObject("Review camera");var camera=cameraObject.AddComponent<Camera>();camera.backgroundColor=new Color(.16f,.18f,.18f);camera.clearFlags=CameraClearFlags.SolidColor;camera.orthographic=true;
   foreach(var path in paths){var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)continue;var instance=Object.Instantiate(source);var renderers=instance.GetComponentsInChildren<Renderer>();if(renderers.Length==0){Object.DestroyImmediate(instance);continue;}var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
    float extent=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);camera.orthographicSize=Mathf.Max(.1f,extent*.8f);camera.transform.position=bounds.center+new Vector3(1,.7f,-1).normalized*extent*3;camera.transform.LookAt(bounds.center);camera.nearClipPlane=.01f;camera.farClipPlane=Mathf.Max(100,extent*10);
    var target=new RenderTexture(768,768,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(768,768,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,768,768),0,0);image.Apply();File.WriteAllBytes("D:/Unity/CC0AssetStaging/previews/"+source.name+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(target);Object.DestroyImmediate(instance);
   }
   Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(lightObject);Debug.Log("CC0_PREVIEWS_WRITTEN");
  }
 }
}
