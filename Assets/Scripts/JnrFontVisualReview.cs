using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Only runs for the explicit batch review flag; no game save is modified.
    public sealed class JnrFontVisualReview : MonoBehaviour
    {
        const string Output="D:/Unity/BuildTemp/JnrFontReview";
        Font font;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--jnr-font-review")>=0)new GameObject("JNR font review").AddComponent<JnrFontVisualReview>();}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);
            var game=FindAnyObjectByType<VaultGame>();if(game)game.enabled=false;
            font=VaultFont.Preferred();
            if(!font){File.WriteAllText(Output+"/result.txt","FAIL font asset missing");yield break;}
            for(int i=0;i<30;i++)yield return null;
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(!image){File.WriteAllText(Output+"/result.txt","FAIL screenshot unavailable");yield break;}
            File.WriteAllBytes(Output+"/font-ui.png",image.EncodeToPNG());
            Destroy(image);File.WriteAllText(Output+"/result.txt","PASS image captured; visual inspection required");
        }
        void Panel(Rect r){GUI.color=new Color(.085f,.105f,.115f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(.70f,.40f,.22f);GUI.DrawTexture(new Rect(r.x,r.y,3,r.height),Texture2D.whiteTexture);GUI.color=Color.white;}
        void Label(Rect r,string value,int size,Color color,bool wrap=false)
        {var s=new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=FontStyle.Normal,wordWrap=wrap,clipping=TextClipping.Clip};s.normal.textColor=color;GUI.Label(r,value,s);}
        void OnGUI()
        {
            if(!font)return;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
            GUI.color=new Color(.035f,.043f,.048f);GUI.DrawTexture(new Rect(0,0,1440,900),Texture2D.whiteTexture);GUI.color=Color.white;
            var white=new Color(.95f,.93f,.88f);var muted=new Color(.72f,.73f,.70f);var orange=new Color(.94f,.58f,.28f);
            Label(new Rect(42,18,1000,58),"インベントリ / INVENTORY",40,white);
            Label(new Rect(42,75,1000,42),"銃　防具　モジュール　近接　　GUNS  ARMOR  MODULES  MELEE",25,muted);
            Panel(new Rect(42,138,420,292));
            Label(new Rect(65,155,365,72),"シンダー・レイン\nCINDER RAIN",30,white,true);
            Label(new Rect(65,245,355,38),"Lv 36 · レジェンダリー",25,orange);
            Label(new Rect(65,290,355,110),"威力 12,345-14,678 ×7\n連射 4.8/秒　命中精度 92.5%\n装弾 32　リロード 1.8秒",23,white,true);
            Panel(new Rect(485,138,455,292));
            Label(new Rect(507,155,400,43),"ACTION SKILL / スキル",29,white);
            Label(new Rect(507,215,400,174),"タレットを召喚し、近くの敵を攻撃。\n命中した敵から、周囲の敵へ電撃が連鎖する。\nHPを回復し、シールドを強化する。",25,white,true);
            Panel(new Rect(964,138,435,292));
            Label(new Rect(987,155,380,42),"MISSION / ミッション",29,white);
            Label(new Rect(987,218,370,140),"物資を2つ回収する\n敵を40体倒す\n報酬：レジェンダリー装備",25,white,true);
            Panel(new Rect(42,453,1357,202));
            Label(new Rect(65,470,1290,40),"日本語 / English   見出し 36px ・本文 23px ・小文字 16px",25,orange);
            Label(new Rect(65,522,1280,34),"HP 149 / 180　レベル 36　評価 12,345　所持 18 / 20　炎・電気・毒・爆発",23,white);
            Label(new Rect(65,575,1280,30),"ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789 + - / % : .",20,muted);
            Label(new Rect(65,615,1280,24),"取得・装備・回復・会心・攻撃・射程・範囲・連鎖・最終報酬 / small text",16,white);
            Panel(new Rect(42,680,1357,175));
            Label(new Rect(65,698,1290,40),"ダイアログ / Dialogue",28,orange);
            Label(new Rect(65,744,1290,87),"ここでは誰も、ただでは生きられない。帰ってきたなら、まず話を聞こう。\nNothing comes free out here. If you made it back, tell me what happened.",25,white,true);
        }
    }
}
