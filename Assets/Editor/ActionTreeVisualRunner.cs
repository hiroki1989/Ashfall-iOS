using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ActionTreeVisualRunner
{
    const string Output="D:/Unity/BuildTemp/ActionTreeReview/result.txt";
    static ActionTreeVisualRunner(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--action-tree-review")>=0)EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run in batch mode only.");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--action-tree-review")<0)throw new InvalidOperationException("Add --action-tree-review to prevent game bootstrap and save writes.");
        Directory.CreateDirectory(Path.GetDirectoryName(Output));if(File.Exists(Output))File.Delete(Output);
        SessionState.SetFloat("ActionTreeReviewStart",(float)EditorApplication.timeSinceStartup);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if(type!=null){var view=EditorWindow.GetWindow(type);view.Show();view.Focus();}
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(File.Exists(Output))EditorApplication.Exit(File.ReadAllText(Output).StartsWith("PASS")?0:1);
        else if(EditorApplication.timeSinceStartup-SessionState.GetFloat("ActionTreeReviewStart",(float)EditorApplication.timeSinceStartup)>180)EditorApplication.Exit(2);
    }
}
