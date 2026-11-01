using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    public sealed class ElementIconVisualReview : MonoBehaviour
    {
        Gun[] guns;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--element-icon-review")>=0)new GameObject("Element icon review").AddComponent<ElementIconVisualReview>();}
        IEnumerator Start()
        {
            guns=new Gun[5];
            for(int i=0;i<5;i++){guns[i]=Arsenal.Model(13,Rarity.Rare);guns[i].element=(GunElement)i;}
            for(int i=0;i<20;i++)yield return null;
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image){Directory.CreateDirectory("D:/Unity/BuildTemp/ElementIconReview");File.WriteAllBytes("D:/Unity/BuildTemp/ElementIconReview/cards.png",image.EncodeToPNG());Destroy(image);}
            File.WriteAllText("D:/Unity/BuildTemp/ElementIconReview/result.txt",image?"PASS":"FAIL screenshot");
        }
        void OnGUI()
        {
            if(guns==null)return;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
            GUI.color=new Color(.055f,.06f,.065f);GUI.DrawTexture(new Rect(0,0,1440,900),Texture2D.whiteTexture);GUI.color=Color.white;
            var label=new GUIStyle(GUI.skin.label){fontSize=22,normal={textColor=Color.white}};
            for(int i=0;i<5;i++)
            {
                float x=35+i*275;
                GUI.Label(new Rect(x,18,250,35),new[]{"NONE","FIRE","ELECTRIC","POISON","EXPLOSIVE"}[i],label);
                ItemPortraits.Draw(new Rect(x,65,240,240),guns[i]);
                ItemPortraits.Draw(new Rect(x,340,122,122),guns[i]);
                ItemPortraits.Draw(new Rect(x,505,240,124),guns[i]);
            }
        }
    }
}
