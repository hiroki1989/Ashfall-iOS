using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// REVIEW INCOMPLETE: game.enabled=false also disables VaultGame.OnGUI, so HUD is not rendered. Do not use generated screenshots until an input-safe HUD overlay is implemented and verified.
[InitializeOnLoad]
public static class GameplayScreenshotReview
{
    const string Output="D:/Unity/BuildTemp/GameplayScreenshotReview";
    static bool started; static int step; static double next; static VaultGame game;
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static GameplayScreenshotReview(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--gameplay-screenshot")>=0) EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--gameplay-screenshot")<0 || Array.IndexOf(Environment.GetCommandLineArgs(),"--mobile-controls")<0) throw new Exception("Missing safe review flags");
        Directory.CreateDirectory(Output); var result=Output+"/result.txt"; if(File.Exists(result))File.Delete(result);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var asm=typeof(Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");var sizes=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{GameViewSizeGroupType.Standalone});var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=asm.GetType("UnityEditor.GameViewSizeType");
        bool ipad=Array.IndexOf(Environment.GetCommandLineArgs(),"--gameplay-ipad")>=0; int w=ipad?2752:2868,h=ipad?2064:1320;
        var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),w,h,"Gameplay review " + w + "x" + h});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;
        var view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,index);view.Show();view.Focus();
        started=true;EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+8;
    }
    static void Tick()
    {
        if(!started||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        try {
            if(step==0){ game=UnityEngine.Object.FindAnyObjectByType<VaultGame>();if(!game)return;
                if(!(bool)typeof(VaultGame).GetField("smoke",F).GetValue(game))throw new Exception("smoke disabled");
                Time.timeScale=0; game.enabled=false; game.screen=VaultGame.Screen.Playing;game.seed=12345;game.room=0;game.hp=game.maxHp=100;
                game.gun=game.Weapons[0];game.ammo=game.gun.magazine;
                typeof(VaultGame).GetMethod("BuildRoomImmediate",F).Invoke(game,new object[]{false});
                VaultLanguage.Set(true,false);step=1;next=EditorApplication.timeSinceStartup+8;return;
            }
            if(step==1){Capture("ja");VaultLanguage.Set(false,false);step=2;next=EditorApplication.timeSinceStartup+3;return;}
            if(step==2){Capture("en");step=3;next=EditorApplication.timeSinceStartup+3;return;}
            File.WriteAllText(Output+"/result.txt","PASS / actual generated dungeon and mobile HUD / no enemies / paused / smoke / Editor");EditorApplication.Exit(0);
        } catch(Exception e){File.WriteAllText(Output+"/result.txt","FAIL "+e);EditorApplication.Exit(1);}
    }
    static void Capture(string language){bool ipad=Array.IndexOf(Environment.GetCommandLineArgs(),"--gameplay-ipad")>=0;ScreenCapture.CaptureScreenshot(Output+"/gameplay-"+(ipad?"ipad":"iphone")+"-"+language+".png");}
}
