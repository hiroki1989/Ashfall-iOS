using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EnemyLabelFontReview
{
    const string Output="D:/Unity/BuildTemp/EnemyLabelFontReview";
    static double started;
    static bool spawned;
    static EnemyLabelFontReview(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--enemy-label-font-review")>=0)EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--enemy-label-font-review")<0)throw new Exception("Missing review flag");
        Directory.CreateDirectory(Output);if(File.Exists(Output+"/result.txt"))File.Delete(Output+"/result.txt");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
        started=EditorApplication.timeSinceStartup;EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.isPlaying&&!spawned){spawned=true;new GameObject("Isolated label review").AddComponent<EnemyLabelFontOverlay>();}
        if(File.Exists(Output+"/result.txt"))EditorApplication.Exit(File.ReadAllText(Output+"/result.txt").StartsWith("PASS")?0:1);
        if(started>0&&EditorApplication.timeSinceStartup-started>120)EditorApplication.Exit(2);
    }
}
public sealed class EnemyLabelFontOverlay:MonoBehaviour
{
    const string Output="D:/Unity/BuildTemp/EnemyLabelFontReview";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    readonly string[] samples={"Lv.999 WASTELAND COLOSSUS","Lv.999 ウェイストランド・コロッサス","Lv.999 NEEDLE STALKER","Lv.999 ニードルストーカー","Lv.999 RED CLAW","Lv.999 レッドクロウ","Lv.999 RAVEN","Lv.999 レイヴン","Lv.999 VEX","Lv.999 ヴェックス"};
    VaultGame game;MethodInfo draw,styles,button;Font jnr,legacy,damage;bool ready;string failure;string measurements="";
    IEnumerator Start()
    {
        var holder=new GameObject("Inactive save-free game");holder.SetActive(false);game=holder.AddComponent<VaultGame>();
        jnr=Resources.Load<Font>("Fonts/JNRfont_n");legacy=Resources.Load<Font>("Fonts/GenShinGothic-P-Regular");damage=Resources.Load<Font>("Fonts/AprilGothicOne-R");
        if(!jnr||!legacy||!damage){failure="Missing font asset";}
        else
        {
            typeof(VaultGame).GetField("font",Flags).SetValue(game,jnr);typeof(VaultGame).GetField("englishHeadingFont",Flags).SetValue(game,jnr);
            var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();typeof(VaultGame).GetField("pixel",Flags).SetValue(game,pixel);
            styles=typeof(VaultGame).GetMethod("Styles",Flags);draw=typeof(VaultGame).GetMethod("DrawEnemyHealthLabel",Flags);button=typeof(VaultGame).GetMethod("MobileButton",Flags);
            var camera=new GameObject("Review camera").AddComponent<Camera>();CombatVisuals.DamageNumber(Vector3.zero,12345,Color.white,camera);
            var mesh=FindAnyObjectByType<TextMesh>();if(!mesh||mesh.font!=damage)failure="Damage TextMesh does not use April Gothic";
            ready=true;
        }
        for(int i=0;i<30;i++)yield return null;yield return new WaitForEndOfFrame();
        if(failure==null){var image=ScreenCapture.CaptureScreenshotAsTexture();if(!image)failure="Screenshot unavailable";else{File.WriteAllBytes(Output+"/review.png",image.EncodeToPNG());Destroy(image);}}
        File.WriteAllText(Output+"/result.txt",failure==null?"PASS / screenshot requires visual inspection / no saves\n"+measurements:"FAIL "+failure+"\n"+measurements);ready=false;
    }
    void OnGUI()
    {
        if(!ready||failure!=null)return;
        try
        {
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));GUI.color=new Color(.04f,.05f,.06f);GUI.DrawTexture(new Rect(0,0,1440,900),Texture2D.whiteTexture);GUI.color=Color.white;
            styles.Invoke(game,null);var style=new GUIStyle(GUI.skin.label){font=jnr,fontSize=17,wordWrap=false};measurements="";
            for(int i=0;i<samples.Length;i++)
            {
                style.fontSize=17;var content=new GUIContent(samples[i]);while(style.fontSize>1&&style.CalcSize(content).x>210)style.fontSize--;
                float width=style.CalcSize(content).x;measurements+=samples[i]+" size="+style.fontSize+" width="+width+"\n";if(width>210)failure="Name exceeds width: "+samples[i];
                float x=i%2==0?250:850,y=105+(i/2)*105;draw.Invoke(game,new object[]{x,y,samples[i],Color.white});GUI.color=Color.red;GUI.DrawTexture(new Rect(x-28,y,56,5),Texture2D.whiteTexture);GUI.color=Color.white;
            }
            button.Invoke(game,new object[]{new Rect(170,680,125,110),"‹",38,legacy});button.Invoke(game,new object[]{new Rect(310,680,125,110),"›",38,legacy});
            var digits=new GUIStyle(GUI.skin.label){font=damage,fontSize=50};digits.normal.textColor=Color.yellow;GUI.Label(new Rect(600,680,600,90),"12.3K  456  +78.9",digits);
        }
        catch(Exception e){failure=e.ToString();}
    }
}
