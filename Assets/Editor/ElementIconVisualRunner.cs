using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ElementIconVisualRunner
{
    const string Output="D:/Unity/BuildTemp/ElementIconReview/result.txt";
    static ElementIconVisualRunner(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--element-icon-review")>=0)EditorApplication.update+=Tick;}
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        if(File.Exists(Output))File.Delete(Output);
        SessionState.SetFloat("ElementIconReviewStart",(float)EditorApplication.timeSinceStartup);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var viewType=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if(viewType!=null){var view=EditorWindow.GetWindow(viewType);view.Show();view.Focus();}
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(File.Exists(Output))EditorApplication.Exit(File.ReadAllText(Output)=="PASS"?0:1);
        else if(EditorApplication.timeSinceStartup-SessionState.GetFloat("ElementIconReviewStart",(float)EditorApplication.timeSinceStartup)>120)EditorApplication.Exit(2);
    }
}
