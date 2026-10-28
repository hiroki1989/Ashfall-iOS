using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NeonVault;
[InitializeOnLoad] public static class BuildingFadeFrameReview {
 static GameObject[] prefabs; static GameObject root,camObj;static Camera cam;static ImportedWorldBatch batch;static List<bool> hidden;static int index=-1,phase=0;static int startFrame;static string output="D:/Unity/AssetIntegration/building-fade-frames/";
 static object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static BuildingFadeFrameReview(){if(SessionState.GetBool("BuildingFadeFrameReview",false)){EditorApplication.playModeStateChanged+=OnPlay;if(EditorApplication.isPlaying) EditorApplication.delayCall+=()=>OnPlay(PlayModeStateChange.EnteredPlayMode);}} public static void Run(){Directory.CreateDirectory(output);SessionState.SetBool("BuildingFadeFrameReview",true);EditorApplication.playModeStateChanged+=OnPlay;EditorApplication.isPlaying=true;}
 static void OnPlay(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode)return;try{prefabs=Resources.LoadAll<GameObject>("ImportedWorld/ApocalypticBuildings");if(prefabs.Length!=7)throw new Exception("expected seven prefabs");camObj=new GameObject("review camera");cam=camObj.AddComponent<Camera>();cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.1f,.65f,.85f);cam.orthographic=true;cam.orthographicSize=7;cam.targetTexture=new RenderTexture(512,512,24);cam.transform.position=new Vector3(14,10,-20);cam.transform.LookAt(Vector3.zero);var light=new GameObject("review light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,30,0);EditorApplication.update+=Tick;startFrame=Time.frameCount;}catch(Exception e){Fail(e);}}
 static void Tick(){try{if(Time.frameCount==startFrame)return;startFrame=Time.frameCount;if(index<0||phase==3){if(root)UnityEngine.Object.DestroyImmediate(root);index++;phase=0;if(index>=prefabs.Length){Debug.Log("BUILDING_FADE_FRAMES_OK prefabs=7");EditorApplication.update-=Tick;SessionState.SetBool("BuildingFadeFrameReview",false);EditorApplication.Exit(0);return;}root=new GameObject("review building");var bounds=ImportedWorldBatch.SourceBounds(prefabs[index]);float scale=10f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));ImportedWorldBatch.Add(root.transform,prefabs[index],-bounds.center*scale,Quaternion.identity,Vector3.one*scale,true);batch=root.GetComponent<ImportedWorldBatch>();batch.enabled=false;hidden=(List<bool>)Field(batch,"hidden");Set(batch,"nextOcclusion",float.MaxValue);}
 hidden[0]=phase==1;batch.DrawNow();cam.Render();RenderTexture.active=cam.targetTexture;var tex=new Texture2D(512,512,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,512,512),0,0);tex.Apply();File.WriteAllBytes(output+prefabs[index].name+"-"+new[]{"solid","faded","restored"}[phase]+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=null;phase++;}catch(Exception e){Fail(e);}}
 static void Fail(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
}


