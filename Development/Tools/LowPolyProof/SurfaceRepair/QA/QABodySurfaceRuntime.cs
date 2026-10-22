using System;using System.IO;using System.Reflection;using UnityEngine;using NeonVault;
public static class QABodySurfaceRuntime {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--body-surface-final")<0)return;try{Run();Application.Quit(0);}catch(Exception e){Debug.LogException(e);Application.Quit(1);}}
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 public static void Run(){
  Application.SetStackTraceLogType(LogType.Error,StackTraceLogType.None);
  int pref=PlayerPrefs.GetInt("Vault.GeneratedCommonBody.Experimental",0);PlayerPrefs.SetInt("Vault.GeneratedCommonBody.Experimental",1);
  var go=new GameObject("Body surface final QA");var game=go.AddComponent<VaultGame>();game.gun=Arsenal.TrailWarden();var preview=go.AddComponent<EquipmentHeroPreview>();if(!preview.UsesGeneratedWarden)preview.ToggleGeneratedWarden();
  string dir="D:/Unity/QA-lowpoly-rig-20261021/body-surface-runtime";Directory.CreateDirectory(dir);string report="";
  foreach(var state in new[]{"base","chest","full","base-return"}){
   game.EquippedArmor=new ArmorItem[5];if(state=="chest")game.EquippedArmor[1]=Armory.TrailWarden(ArmorSlot.Chest);if(state=="full")for(int i=0;i<5;i++)game.EquippedArmor[i]=Armory.TrailWarden((ArmorSlot)i);
   foreach(var angle in new[]{0f,45f,90f,135f,180f,270f}){
    preview.Angle=angle;preview.ReviewPhase=-1;preview.ReviewFiring=false;preview.ReviewVelocity=Vector3.zero;
    typeof(EquipmentHeroPreview).GetField("lastRender",flags).SetValue(preview,-100f);
    var rt=(RenderTexture)preview.Render(game);Save(rt,dir+"/"+state+"-"+angle+".png");
    var camera=(Camera)typeof(EquipmentHeroPreview).GetField("view",flags).GetValue(preview);
    var hero=(DetailedHero3D)typeof(EquipmentHeroPreview).GetField("hero",flags).GetValue(preview);
    report+=state+" "+angle+" scale="+hero.transform.lossyScale+" camera="+camera.orthographicSize+" head="+hero.head.position.y+" pelvis="+hero.pelvis.position.y+"\n";
   }
  }
  game.EquippedArmor=new ArmorItem[5];preview.Angle=90;preview.ReviewFiring=true;typeof(EquipmentHeroPreview).GetField("lastRender",flags).SetValue(preview,-100f);Save((RenderTexture)preview.Render(game),dir+"/base-fire.png");
  preview.ReviewFiring=false;preview.ReviewPhase=.32f;preview.ReviewVelocity=Vector3.forward*4.5f;typeof(EquipmentHeroPreview).GetField("lastRender",flags).SetValue(preview,-100f);Save((RenderTexture)preview.Render(game),dir+"/base-run.png");
  File.WriteAllText(dir+"/report.txt",report);PlayerPrefs.SetInt("Vault.GeneratedCommonBody.Experimental",pref);Debug.Log("BODY_SURFACE_FINAL_QA_OK");
 }
 static void Save(RenderTexture rt,string path){RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
}
