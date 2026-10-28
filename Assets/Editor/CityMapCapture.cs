using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonVault.Editor {
 [InitializeOnLoad] public static class CityMapCapture {
  static double next; static int step;
  static CityMapCapture(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--city-map-capture")>=0)EditorApplication.update+=Tick;}
  public static void Run(){Directory.CreateDirectory("D:/Unity/AssetIntegration/CityMap20261028");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+12;}
  static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;var game=UnityEngine.Object.FindAnyObjectByType<VaultGame>();if(!game)return;
   if(step==0){typeof(VaultGame).GetField("smoke",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,true);game.CompleteWorldBossConversation();game.screen=VaultGame.Screen.Hub;game.OpenOverworldMap();VaultLanguage.Set(true,false);EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();next=EditorApplication.timeSinceStartup+30;step=1;return;}
   if(step==1){ScreenCapture.CaptureScreenshot("D:/Unity/AssetIntegration/CityMap20261028/map.png");next=EditorApplication.timeSinceStartup+8;step=2;return;}
   if(File.Exists("D:/Unity/AssetIntegration/CityMap20261028/map.png")){Debug.Log("CITY_MAP_CAPTURE_OK");EditorApplication.Exit(0);}else EditorApplication.Exit(2);
  }
 }
}
