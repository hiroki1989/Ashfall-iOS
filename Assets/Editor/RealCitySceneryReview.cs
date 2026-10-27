using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonVault.Editor
{
    // Isolated review of the production stream. No player, enemies, or saves.
    [InitializeOnLoad]
    public static class RealCitySceneryReview
    {
        static int step;static double next;static OverworldStream stream;static Camera camera;
        static OverworldGeography geography;
        const string Output="D:/Unity/QA-premium-ui/";
        static RealCitySceneryReview(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--real-city-scenery-review")>=0)EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/scenery-result.txt","SCENERY_REVIEW_RUNNING");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+8;
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+4;
            try
            {
                if(step==0)
                {
                    geography=new OverworldGeography();var point=new Vector3(-1375,0,-2075);
                    var root=new GameObject("Production city stream review");
                    stream=root.AddComponent<OverworldStream>();stream.Initialize(geography,new List<Material>());stream.TickFocus(point);
                    camera=new GameObject("Review Camera").AddComponent<Camera>();camera.tag="MainCamera";
                    camera.gameObject.AddComponent<AudioListener>();camera.orthographic=true;camera.orthographicSize=65;
                    camera.transform.rotation=Quaternion.Euler(48,45,0);camera.transform.position=point-camera.transform.forward*230;
                    camera.nearClipPlane=.3f;camera.farClipPlane=500;camera.backgroundColor=new Color(.09f,.12f,.14f);
                    var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.intensity=1.15f;sun.color=new Color(1,.95f,.84f);sun.shadows=LightShadows.Soft;
                    RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.44f,.47f,.46f);
                    QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=350;QualitySettings.antiAliasing=2;
                    EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
                    next=EditorApplication.timeSinceStartup+12;
                }
                if(step==1)
                {
                    if(!stream.Ready||stream.LoadedChunkCount!=9)throw new Exception("Stream did not settle");
                    ScreenCapture.CaptureScreenshot(Output+"real-city-scenery.png");
                }
                if(step==2)
                {
                    var bank=new Vector3(geography.RiverX(0)-geography.RiverWidth(0)*.5f-15,0,0);
                    stream.TickFocus(bank);camera.transform.position=bank-camera.transform.forward*230;
                    next=EditorApplication.timeSinceStartup+12;
                }
                if(step==3)ScreenCapture.CaptureScreenshot(Output+"real-city-river.png");
                if(step==4)
                {
                    File.WriteAllText(Output+"scenery-result.txt","SCENERY_REVIEW_COMPLETED / CHUNKS="+stream.LoadedChunkCount+" / GRAPH_SEGMENTS="+geography.RoadSegmentCount);
                    EditorApplication.Exit(0);
                }
                step++;
            }
            catch(Exception error){File.WriteAllText(Output+"scenery-result.txt",error.ToString());EditorApplication.Exit(2);}
        }
    }
}
