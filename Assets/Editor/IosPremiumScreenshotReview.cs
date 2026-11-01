using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class IosPremiumScreenshotReview
{
    const string Output = "D:/Unity/BuildTemp/IosPremiumScreenshotReview";
    static double started;
    static bool spawned;
    static IosPremiumScreenshotReview() { if (Array.IndexOf(Environment.GetCommandLineArgs(), "--ios-premium-screenshot") >= 0) EditorApplication.update += Tick; }
    public static void Run()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--ios-premium-screenshot") < 0) throw new Exception("Missing isolated review flag");
        Directory.CreateDirectory(Output); if(File.Exists(Output+"/result.txt")) File.Delete(Output+"/result.txt");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var asm=typeof(Editor).Assembly;
        var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var singletonType=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singletonType.GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{GameViewSizeGroupType.Standalone});
        var sizeType=asm.GetType("UnityEditor.GameViewSize");
        var kind=asm.GetType("UnityEditor.GameViewSizeType");
        bool ipad=Array.IndexOf(Environment.GetCommandLineArgs(), "--ios-premium-ipad") >= 0; int width=ipad?2752:2868, height=ipad?2064:1320;
        var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),width,height,"IAP review " + width + "x" + height});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;
        var view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,index);
        view.Show(); view.Focus(); started=EditorApplication.timeSinceStartup; EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.isPlaying && !spawned) { spawned=true; new GameObject("IAP isolated review").AddComponent<IosPremiumScreenshotOverlay>(); }
        if(File.Exists(Output+"/result.txt")) EditorApplication.Exit(File.ReadAllText(Output+"/result.txt").StartsWith("PASS")?0:1);
        if(started>0 && EditorApplication.timeSinceStartup-started>150) EditorApplication.Exit(2);
    }
}
public sealed class IosPremiumScreenshotOverlay : MonoBehaviour
{
    const string Output="D:/Unity/BuildTemp/IosPremiumScreenshotReview";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    VaultGame game; MethodInfo styles,draw; bool ready; string failure;
    static void Set(object o,string field,object value) { typeof(VaultGame).GetField(field,Flags).SetValue(o,value); }
    IEnumerator Start()
    {
        var holder=new GameObject("Inactive save-free game"); holder.SetActive(false); game=holder.AddComponent<VaultGame>();
        Set(game,"font",Resources.Load<Font>("Fonts/JNRfont_n")); Set(game,"englishHeadingFont",Resources.Load<Font>("Fonts/JNRfont_n"));
        var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply(); Set(game,"pixel",pixel); Set(game,"smoke",true);
        var total=typeof(VaultGame).GetMethod("DebtTotalForLevel",Flags); Set(game,"debtPaid",total.Invoke(game,new object[]{3}));
        typeof(IosPremiumAccess).GetField("price",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,"JPY 490");
        VaultLanguage.Set(true,false); styles=typeof(VaultGame).GetMethod("Styles",Flags); draw=typeof(VaultGame).GetMethod("DrawMobileDebt",Flags); ready=true;
        for(int i=0;i<20;i++) yield return null;
        yield return new WaitForEndOfFrame();
        if(failure==null) {
            var tex=ScreenCapture.CaptureScreenshotAsTexture();
            if(!tex) failure="Screenshot unavailable";
            else { bool ipad=Array.IndexOf(Environment.GetCommandLineArgs(), "--ios-premium-ipad") >= 0; int width=ipad?2752:2868, height=ipad?2064:1320; File.WriteAllBytes(Output+(ipad?"/purchase-ja-ipad-editor.png":"/purchase-ja-iphone-editor.png"),tex.EncodeToPNG()); if(tex.width!=width||tex.height!=height) failure="Unexpected size "+tex.width+"x"+tex.height; Destroy(tex); }
        }
        ready=false; File.WriteAllText(Output+"/result.txt",failure==null?"PASS / Unity Editor / simulated level 3 and JPY 490 / no saves / no StoreKit":"FAIL "+failure);
    }
    void OnGUI()
    {
        if(!ready||failure!=null)return;
        try { GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1)); styles.Invoke(game,null);draw.Invoke(game,null); }
        catch(Exception e){failure=e.ToString();}
    }
}
