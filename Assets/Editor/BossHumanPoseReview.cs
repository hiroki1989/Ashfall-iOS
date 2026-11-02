using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class BossHumanPoseReview {
 public static void Run(){try{
  string folder="D:/Unity/BuildTemp/BossHumanPoseReview";Directory.CreateDirectory(folder);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.ambientLight=Color.gray;var l=new GameObject("light").AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.2f;l.transform.rotation=Quaternion.Euler(40,-30,0);
  var c=new GameObject("camera").AddComponent<Camera>();c.orthographic=true;c.orthographicSize=1.65f;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.08f,.09f,.1f);var rt=new RenderTexture(768,768,24);rt.Create();c.targetTexture=rt;
  for(int f=0;f<3;f++){var prefab=Resources.Load<HumanFactionVisual>("FactionPresets/faction_"+f+"_0");if(!prefab||!prefab.Body)throw new Exception("Missing faction prefab "+f);var instance=Object.Instantiate(prefab);instance.transform.position=Vector3.zero;instance.gameObject.SetActive(true);var gun=(DetailedHeroEquipment3D.WeaponSockets)typeof(HumanFactionVisual).GetField("gun",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(instance);
   foreach(var pose in new[]{"idle","walk"}){DetailedHeroPlayer3D.Pose(instance.Body,gun,false,true,Vector3.forward,pose=="idle"?-1:1.1f,pose=="idle"?Vector3.zero:Vector3.forward*2,pose=="idle"?0:1);
    for(int a=0;a<2;a++){c.transform.position=new Vector3(a==0?0:4,1.3f,a==0?4:0);c.transform.LookAt(Vector3.up*.9f);c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(768,768,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,768,768),0,0);t.Apply();File.WriteAllBytes(Path.Combine(folder,"faction-"+f+"-"+pose+"-"+a+".png"),t.EncodeToPNG());Object.DestroyImmediate(t);RenderTexture.active=old;}}
   Object.DestroyImmediate(instance.gameObject);
  } File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: all three baked human faction prefabs render idle and walk poses. Actual gameplay not tested.\n");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
