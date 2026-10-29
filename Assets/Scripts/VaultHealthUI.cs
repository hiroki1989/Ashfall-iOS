using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        Texture2D criticalHealthVignette;
        static readonly Color VitalPanel=new Color(.025f,.042f,.052f,.68f);
        static readonly Color VitalTrack=new Color(.24f,.24f,.23f,.95f);
        static readonly Color VitalRed=new Color(.93f,.19f,.16f);

        bool CriticalHealth=>screen==Screen.Playing&&!InventoryOpen&&maxHp>0&&hp>0&&hp/maxHp<=.25f;
        Color VitalColor=>CriticalHealth?VitalRed:new Color(.84f,.32f,.12f);
        Texture2D healthPanelArt;

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
            if(!healthPanelArt)healthPanelArt=Resources.Load<Texture2D>("Art/UI/health-panel-minimal-v27");
            if(healthPanelArt)GUI.DrawTextureWithTexCoords(bounds,healthPanelArt,new Rect(0,.24f,1,.53f),true);
            else Box(bounds,VitalPanel);
            if(mobile)
            {
                Text(bounds.x+28,bounds.y+20,"HP  "+Mathf.CeilToInt(hp)+" / "+Mathf.CeilToInt(maxHp),new GUIStyle(label){fontSize=25,fontStyle=FontStyle.Normal},Color.white,240);
                Text(bounds.x+bounds.width-112,bounds.y+24,CriticalHealth?T("CRITICAL","瀕死"):"LV. "+DebtLevel,new GUIStyle(tiny){fontSize=18},Color.white,90);
                DrawHealthFill(new Rect(bounds.x+28,bounds.y+64,bounds.width-56,9),ratio,accent);
            }
            else
            {
                Text(bounds.x+28,bounds.y+20,"HP  "+Mathf.CeilToInt(hp)+" / "+Mathf.CeilToInt(maxHp),new GUIStyle(label){fontSize=23,fontStyle=FontStyle.Normal},Color.white,220);
                Text(bounds.x+bounds.width-100,bounds.y+24,CriticalHealth?T("CRITICAL","瀕死"):"LV. "+DebtLevel,new GUIStyle(tiny){fontSize=16},Color.white,78);
                DrawHealthFill(new Rect(bounds.x+28,bounds.y+64,bounds.width-56,9),ratio,accent);
            }
        }

        void DrawHealthFill(Rect track,float ratio,Color accent)
        {
            Box(track,VitalTrack);
            float width=track.width*ratio;
            if(width>0)
            {
                Box(new Rect(track.x,track.y,width,track.height),accent);
                Box(new Rect(track.x,track.y,width,1),new Color(1,1,1,.20f));
            }
        }
    }
}
