using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        Texture2D criticalHealthVignette;
        static readonly Color VitalPanel=new Color(.025f,.042f,.052f,.96f);
        static readonly Color VitalTrack=new Color(.12f,.16f,.17f);
        static readonly Color VitalRed=new Color(.93f,.19f,.16f);

        bool CriticalHealth=>screen==Screen.Playing&&!InventoryOpen&&maxHp>0&&hp>0&&hp/maxHp<=.25f;
        Color VitalColor=>CriticalHealth?VitalRed:Cyan;

        void DrawCriticalHealthVignette()
        {
            if(!CriticalHealth)return;
            if(criticalHealthVignette==null)
            {
                const int size=128;
                var colors=new Color32[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float nx=(x+.5f-size*.5f)/(size*.5f),ny=(y+.5f-size*.5f)/(size*.5f);
                    float distance=Mathf.Sqrt(nx*nx+ny*ny);
                    float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-.48f)/.64f));
                    colors[y*size+x]=new Color32(137,9,14,(byte)Mathf.RoundToInt(edge*120));
                }
                criticalHealthVignette=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Critical health edge vignette",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                criticalHealthVignette.SetPixels32(colors);
                criticalHealthVignette.Apply(false,true);
            }
            float severity=1-Mathf.Clamp01(hp/Mathf.Max(1,maxHp)*4);
            float pulse=.88f+.12f*Mathf.Sin(Time.unscaledTime*3.2f);
            Matrix4x4 previousMatrix=GUI.matrix;
            Color previousColor=GUI.color;
            GUI.matrix=Matrix4x4.identity;
            GUI.color=new Color(1,1,1,(.68f+.32f*severity)*pulse);
            GUI.DrawTexture(new Rect(0,0,UnityEngine.Screen.width,UnityEngine.Screen.height),criticalHealthVignette);
            GUI.color=previousColor;
            GUI.matrix=previousMatrix;
        }

        void DrawHealthPanel(Rect bounds,bool mobile)
        {
            float ratio=Mathf.Clamp01(hp/Mathf.Max(1,maxHp));
            Color accent=VitalColor;
            Box(bounds,VitalPanel);
            Box(new Rect(bounds.x,bounds.y,bounds.width,2),new Color(.51f,.64f,.66f,.65f));
            Box(new Rect(bounds.x,bounds.y,5,bounds.height),accent);
            Box(new Rect(bounds.x+bounds.width-25,bounds.y,25,5),accent);
            Box(new Rect(bounds.x+bounds.width-5,bounds.y,5,28),accent);
            Box(new Rect(bounds.x+14,bounds.y+11,2,bounds.height-25),new Color(accent.r,accent.g,accent.b,.43f));
            if(mobile)
            {
                Text(bounds.x+26,bounds.y+5,Mathf.CeilToInt(hp).ToString(),new GUIStyle(number){fontSize=43},Color.white,132);
                Text(bounds.x+153,bounds.y+25,"/ "+Mathf.CeilToInt(maxHp)+" HP",new GUIStyle(label){fontSize=23},Mist,185);
                Text(bounds.x+320,bounds.y+25,CriticalHealth?T("CRITICAL","瀕死"):T("VITALS","HP"),new GUIStyle(tiny){fontSize=16,fontStyle=FontStyle.Bold},accent,86);
                DrawHealthFill(new Rect(bounds.x+26,bounds.y+65,bounds.width-50,11),ratio,accent);
            }
            else
            {
                Text(bounds.x+25,bounds.y+10,CriticalHealth?T("CRITICAL / VITALS","瀕死 / 生存状態"):T("VITALS","生存状態"),new GUIStyle(tiny){fontSize=14,fontStyle=FontStyle.Bold},accent,310);
                Text(bounds.x+25,bounds.y+35,Mathf.CeilToInt(hp).ToString(),new GUIStyle(number){fontSize=43},Color.white,145);
                Text(bounds.x+150,bounds.y+56,"/ "+Mathf.CeilToInt(maxHp)+" HP",new GUIStyle(small){fontSize=19},Mist,170);
                DrawHealthFill(new Rect(bounds.x+25,bounds.y+91,bounds.width-50,15),ratio,accent);
            }
        }

        void DrawHealthFill(Rect track,float ratio,Color accent)
        {
            Box(new Rect(track.x-2,track.y-3,track.width+4,track.height+6),new Color(.24f,.32f,.34f));
            Box(track,VitalTrack);
            float width=track.width*ratio;
            if(width>0)
            {
                Box(new Rect(track.x,track.y,width,track.height),accent);
                Box(new Rect(track.x,track.y,width,2),new Color(1,1,1,.34f));
                if(width>6)Box(new Rect(track.x+width-4,track.y-2,4,track.height+4),Color.white);
            }
            for(int i=1;i<8;i++)Box(new Rect(track.x+track.width*i/8f,track.y+2,2,track.height-4),new Color(.025f,.042f,.052f,.74f));
        }
    }
}
