using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class JnrFontVisualRunner
{
    const string Output="D:/Unity/BuildTemp/JnrFontReview/result.txt";
    static JnrFontVisualRunner(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--jnr-font-review")>=0)EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Run in batch mode only.");
        Directory.CreateDirectory(Path.GetDirectoryName(Output));if(File.Exists(Output))File.Delete(Output);
        SessionState.SetFloat("JnrFontReviewStart",(float)EditorApplication.timeSinceStartup);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var viewType=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if(viewType!=null){var view=EditorWindow.GetWindow(viewType);view.Show();view.Focus();}
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(File.Exists(Output))EditorApplication.Exit(File.ReadAllText(Output).StartsWith("PASS")?0:1);
        else if(EditorApplication.timeSinceStartup-SessionState.GetFloat("JnrFontReviewStart",(float)EditorApplication.timeSinceStartup)>120)EditorApplication.Exit(2);
    }
}
