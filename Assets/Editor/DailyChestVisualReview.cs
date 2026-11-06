using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Requires --gameplay-screenshot (sets smoke before inventory initialization).
// Uses the real hub and OnGUI; confirmation is injected because Editor ads are unavailable.
[InitializeOnLoad]
public static class DailyChestVisualReview
{
    const string Output = "D:/Unity/BuildTemp/DailyChestVisualReview";
    static bool started, spawned;
    static double deadline;
    static DailyChestVisualReview() { if (Has("--daily-chest-visual-review")) EditorApplication.update += Tick; }
    static bool Has(string flag) => Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0;
    public static void Run()
    {
        if (!Has("--daily-chest-visual-review") || !Has("--gameplay-screenshot") || !Has("--mobile-controls"))
            throw new Exception("Required flags: --daily-chest-visual-review --gameplay-screenshot --mobile-controls");
        Directory.CreateDirectory(Output);
        if (File.Exists(Output + "/result.txt")) File.Delete(Output + "/result.txt");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus(); started = true; deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!started) return;
        if (EditorApplication.isPlaying && !spawned) { spawned = true; new GameObject("Daily chest visual review").AddComponent<DailyChestVisualCapture>(); }
        if (File.Exists(Output + "/result.txt")) EditorApplication.Exit(File.ReadAllText(Output + "/result.txt").StartsWith("PASS") ? 0 : 1);
        if (EditorApplication.timeSinceStartup > deadline) { File.WriteAllText(Output + "/result.txt", "FAIL timeout"); EditorApplication.Exit(2); }
    }
}
public sealed class DailyChestVisualCapture : MonoBehaviour
{
    const string Output = "D:/Unity/BuildTemp/DailyChestVisualReview";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static FieldInfo Field(string name) => typeof(VaultGame).GetField(name, Flags) ?? throw new Exception("Missing field " + name);
    static MethodInfo Method(string name) => typeof(VaultGame).GetMethod(name, Flags) ?? throw new Exception("Missing method " + name);
    IEnumerator Start()
    {
        VaultGame game = null; string error = null;
        try {
            game = FindAnyObjectByType<VaultGame>();
            if (!game || !(bool)Field("smoke").GetValue(game)) throw new Exception("Smoke-mode game unavailable");
            game.screen = VaultGame.Screen.Hub;
            Method("BuildHubImmediate").Invoke(game, null);
            var player = (Transform)Field("player").GetValue(game);
            player.position = new Vector3(-4, .65f, -3);
            Method("UpdateCamera").Invoke(game, new object[] { true });
            VaultLanguage.Set(true, false);
        } catch (Exception e) { error = e.ToString(); }
        if (error == null) {
            for (int i = 0; i < 30; i++) yield return null;
            // Wait for an actual bright phase of the pulsing marker.
            while (Mathf.Sin(Time.unscaledTime * 5f) <= .3f) yield return null;
            yield return new WaitForEndOfFrame();
            try { Capture("hub-ja.png"); Field("dailyChestAdConfirm").SetValue(game, true); } catch (Exception e) { error = e.ToString(); }
        }
        if (error == null) {
            for (int i = 0; i < 5; i++) yield return null;
            yield return new WaitForEndOfFrame();
            try { Capture("confirmation-ja.png"); VaultLanguage.Set(false, false); } catch (Exception e) { error = e.ToString(); }
        }
        if (error == null) {
            for (int i = 0; i < 5; i++) yield return null;
            yield return new WaitForEndOfFrame();
            try { Capture("confirmation-en.png"); } catch (Exception e) { error = e.ToString(); }
        }
        File.WriteAllText(Output + "/result.txt", error == null ? "PASS / real hub and UI / smoke / confirmation injected / visual inspection required / no rewarded ad" : "FAIL " + error);
    }
    static void Capture(string name)
    {
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        if (!image) throw new Exception("Screenshot unavailable");
        File.WriteAllBytes(Output + "/" + name, image.EncodeToPNG()); Destroy(image);
    }
}
