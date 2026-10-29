using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Shared composition for desktop and touch. Controls stay in the safe canvas;
        // OnGUI separately fills the physical display behind the mobile safe area.
        void DrawTitleMenu(bool mobile)
        {
            if(menuArt) GUI.DrawTexture(new Rect(0,0,1440,900),menuArt,ScaleMode.ScaleAndCrop);
            // A soft shadow behind the title makes it legible without hiding the hero.
            for(int i=0;i<48;i++)
            {
                float t=i/47f;
                Box(new Rect(i*15,0,16,900),new Color(.025f,.025f,.023f,.78f*(1-t)*(1-t)));
            }
            Box(new Rect(0,0,1440,900),new Color(.015f,.017f,.018f,.14f));
            var brand=Resources.Load<Texture2D>("Art/Brand/ApprovedLogoReference");
            if(brand) GUI.DrawTextureWithTexCoords(new Rect(72,90,500,340),brand,new Rect(.07f,.15f,.39f,.70f),true);
            else Text(76,180,"DUST & DEBT",new GUIStyle(title){fontSize=68},UiPaper,620);
            float h=mobile?96:72, y=mobile?490:488, w=mobile?505:440;
            if(TitleMenuButton(new Rect(76,y,w,h),T("START GAME","ゲーム開始"),true,mobile)) ContinueOverworldGame();
            if(HasSavedRun && TitleMenuButton(new Rect(76,y+h+16,w,h),T("RESUME EXPEDITION","探索を再開"),false,mobile)) ResumeSavedRun();
        }
        bool TitleMenuButton(Rect r,string caption,bool primary,bool mobile)
        {
            var fill=primary?new Color(.80f,.35f,.14f,.94f):new Color(.045f,.05f,.05f,.87f);
            Box(r,fill);
            if(!primary) {Box(new Rect(r.x,r.y,r.width,1),new Color(.69f,.67f,.61f,.5f));Box(new Rect(r.x,r.yMax-1,r.width,1),new Color(.69f,.67f,.61f,.5f));}
            var style=new GUIStyle(label){fontSize=mobile?34:25,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(28,16,0,0),clipping=TextClipping.Clip,fontStyle=FontStyle.Normal};
            style.normal.textColor=UiPaper;style.hover.textColor=Color.white;style.active.textColor=Color.white;
            return GUI.Button(r,caption,style);
        }
    }
}
