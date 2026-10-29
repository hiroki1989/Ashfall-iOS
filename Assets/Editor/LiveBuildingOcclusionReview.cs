using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NeonVault;

// Opt-in test: --asset-review enables the game's save-free smoke mode.
[InitializeOnLoad]
public static class LiveBuildingOcclusionReview
{
    const string Output = "D:/Unity/AssetIntegration/live-building-occlusion";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static int step, frames;
    static double next;
    static VaultGame game;
    static Transform player;
    static ImportedWorldBatch selected;
    static int owner;
    static Vector3 behind, clear;
    static FieldInfo boundsField = typeof(ImportedWorldBatch).GetField("buildingBounds", Flags);
    static FieldInfo hiddenField = typeof(ImportedWorldBatch).GetField("hidden", Flags);
    static MethodInfo hides = typeof(ImportedWorldBatch).GetMethod("Hides", Flags);
    static MethodInfo updateCamera = typeof(VaultGame).GetMethod("UpdateCamera", Flags);
    static LiveBuildingOcclusionReview() { if (Array.IndexOf(Environment.GetCommandLineArgs(), "--live-building-review") >= 0) EditorApplication.update += Tick; }
    public static void Run()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--live-building-review") < 0) throw new Exception("Requires --live-building-review to avoid modifying save data");
        Directory.CreateDirectory(Output);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true; next = EditorApplication.timeSinceStartup + 10;
    }
    static void Position(Vector3 p)
    {
        player.position = p; updateCamera.Invoke(game, new object[]{true});
        typeof(ImportedWorldBatch).GetField("nextOcclusion", Flags).SetValue(selected, 0f);
    }
    static bool Hidden() => ((List<bool>)hiddenField.GetValue(selected))[owner];
    static void Capture(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(Output, name+".png")); }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (step == 0)
            {
                game = UnityEngine.Object.FindAnyObjectByType<VaultGame>(); if (!game) return;
                game.BuildOverworld(new Vector3(-1375,.65f,-2075)); step=1; next=EditorApplication.timeSinceStartup+14; return;
            }
            if (step == 1)
            {
                player=(Transform)typeof(VaultGame).GetField("player",Flags).GetValue(game);
                if(!player || !game.GameCamera) throw new Exception("Player or camera missing");
                foreach(var foe in game.Enemies) if(foe) UnityEngine.Object.Destroy(foe.gameObject);
                game.Enemies.Clear(); game.enabled=false;
                foreach(var batch in UnityEngine.Object.FindObjectsByType<ImportedWorldBatch>(FindObjectsSortMode.None))
                {
                    var bounds=(List<Bounds>)boundsField.GetValue(batch);
                    for(int i=0;i<bounds.Count;i++)
                    {
                        var b=bounds[i]; if(b.size.y<4 || b.size.x>50 || b.size.z>50)continue;
                        selected=batch;owner=i; bool foundBehind=false,foundClear=false;
                        for(int z=-3;z<=3;z++) for(int x=-3;x<=3;x++)
                        {
                            Vector3 p=new Vector3(b.center.x+x*(b.extents.x+1)/3f,.65f,b.center.z+z*(b.extents.z+1)/3f);
                            if(game.WorldGeography.Blocked(p,.65f))continue;
                            Position(p);
                            bool h=(bool)hides.Invoke(batch,new object[]{b,p+Vector3.up*1.1f,game.GameCamera});
                            if(h&&!foundBehind){behind=p;foundBehind=true;}
                            if(!h&&!foundClear){clear=p;foundClear=true;}
                        }
                        if(foundBehind&&foundClear){Position(clear);step=2;next=EditorApplication.timeSinceStartup+1;return;}
                    }
                }
                throw new Exception("No unobstructed test positions around generated buildings");
            }
            if(step==2){selected.DrawNow();if(Hidden())throw new Exception("Building faded at clear position");Capture("clear");Position(behind);step=3;next=EditorApplication.timeSinceStartup+1;return;}
            if(step==3){selected.DrawNow();if(!Hidden())throw new Exception("Building did not fade behind it");Capture("behind");Position(clear);step=4;next=EditorApplication.timeSinceStartup+1;return;}
            if(step==4){selected.DrawNow();if(Hidden())throw new Exception("Building did not restore");Capture("restored");File.WriteAllText(Path.Combine(Output,"result.txt"),$"PASS actual generated building / clear={clear} behind={behind} owner={owner} batch={selected.name}");step=5;next=EditorApplication.timeSinceStartup+2;return;}
            Debug.Log("LIVE_BUILDING_OCCLUSION_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText(Path.Combine(Output,"failure.txt"),e.ToString());EditorApplication.Exit(1);}
    }
}
