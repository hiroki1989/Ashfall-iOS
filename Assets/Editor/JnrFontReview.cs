using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Read-only review: isolated transient objects, no PlayerPrefs or saved games.
public static class JnrFontReview
{
    static readonly List<string> failures=new List<string>();
    static int checks;
    static void Check(bool ok,string label){checks++;if(!ok)failures.Add(label);}
    public static void Run()
    {
        const string output="D:/Unity/BuildTemp/JnrFontReview/result.txt";
        failures.Clear();checks=0;
        GameObject root=null;
        try
        {
            var font=Resources.Load<Font>("Fonts/JNRfont_n");
            Check(font!=null,"JNR font is bundled as Resources/Fonts/JNRfont_n");
            var type=typeof(VaultGame).Assembly.GetType("NeonVault.VaultFont",true);
            var method=type.GetMethod("Preferred",BindingFlags.Static|BindingFlags.NonPublic);
            var preferred=(Font)method.Invoke(null,null);
            Check(preferred!=null&&preferred==font,"game central font resolves the bundled JNR font");
            if(font!=null)
            {
                const string chars="日本語英語開始装備銃防具敵体力回復炎電気毒爆発射撃0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz▲▼✓×−～…・★◆";
                foreach(char c in chars)Check(font.HasCharacter(c),"font contains U+"+((int)c).ToString("X4"));
                font.RequestCharactersInTexture(chars,32,FontStyle.Normal);
                foreach(char c in "日本語装備銃炎電毒爆012ABC")Check(font.GetCharacterInfo(c,out _,32,FontStyle.Normal),"font atlas contains "+c);
            }
            root=new GameObject("JNR font isolated QA");
            var camera=new GameObject("QA camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);
            CombatVisuals.DamageNumber(Vector3.zero,123,Color.white,camera,root.transform);
            CombatVisuals.EvadeNumber(Vector3.zero,camera,root.transform);
            var texts=root.GetComponentsInChildren<TextMesh>(true);
            Check(texts.Length==2,"damage and dodge text are instantiated");
            foreach(var text in texts)Check(text.font==font,"combat TextMesh uses JNR / "+text.name);
        }
        catch(Exception e){failures.Add(e.ToString());}
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string report="checks="+checks+" failures="+failures.Count+"\n"+string.Join("\n",failures);
        File.WriteAllText(output,report);Debug.Log("JNR_FONT_REVIEW "+report);
        if(Application.isBatchMode)EditorApplication.Exit(failures.Count==0?0:1);
    }
}
