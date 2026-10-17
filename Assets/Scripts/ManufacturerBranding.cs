using UnityEngine;
namespace NeonVault
{
    // Original geometric maker marks: no third-party logos, text remains localizable UI.
    public static class ManufacturerBranding
    {
        static readonly Texture2D[] logos=new Texture2D[6];
        public static string Maker(Gun g){ManufacturerCatalog.InferGunIdentity(g);return g.manufacturerId;}
        public static string Maker(ArmorItem a)=>ManufacturerCatalog.Index(a.manufacturerId)>=0?a.manufacturerId:ManufacturerCatalog.Id(a.visualVariant+(int)a.slot);
        public static string Maker(ArtifactItem a)=>ManufacturerCatalog.Index(a.manufacturerId)>=0?a.manufacturerId:ManufacturerCatalog.Id((int)a.effect+a.dungeon);
        public static string Maker(MeleeWeaponItem a){MeleeWeaponCatalog.InferIdentity(a);return a.manufacturerId;}
        public static string GunDescription(Gun g,bool ja)=>ManufacturerCatalog.Name(Maker(g),ja)+" · "+ManufacturerCatalog.Description(Maker(g),ja);
        public static string Design(string id,bool ja)
        {
            string[] en={"Machined rails, narrow vents, blue enamel datum marks, ivory grip inserts.","Forged ribbed steel, exposed rivets, oxblood leather and copper braces.","Ceramic coil banks, brass conductors, yellow warning tabs and navy housings.","Olive fabric, translucent green reservoirs, bone-white fittings and woven hoses.","Welded patchwork, scratched orange plates, reclaimed timber and mismatched fasteners.","Layered ballistic plates, blue cloth panels, pale serial bands and reinforced buckles."};
            string[] jp={"切削レール・細い放熱孔。青い計測印と象牙色グリップ。","鍛造鋼の補強リブと鋲。赤褐色の革、銅の支柱。","セラミックコイルと真鍮導体。黄の警告片、紺の筐体。","オリーブ布地と緑の薬液槽。骨白の継手、編組ホース。","溶接した継ぎ当てと傷んだ橙の鋼板。再生木材、不揃いな留め具。","積層防弾板と青い布地。白い識別帯、補強バックル。"};
            return (ja?jp:en)[Mathf.Max(0,ManufacturerCatalog.Index(id))];
        }
        static float Segment(Vector2 p,Vector2 a,Vector2 b){Vector2 d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
        static bool Line(Vector2 p,float ax,float ay,float bx,float by,float width=.027f)=>Segment(p,new Vector2(ax,ay),new Vector2(bx,by))<width;
        public static Texture2D Logo(string id)
        {
            int index=Mathf.Max(0,ManufacturerCatalog.Index(id));if(logos[index]!=null)return logos[index];
            const int size=160;var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Manufacturer emblem / "+id,filterMode=FilterMode.Trilinear};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Vector2 p=new Vector2((x+.5f)/size,(y+.5f)/size);bool ink=false;float r=Vector2.Distance(p,Vector2.one*.5f);
                switch(index)
                {
                    case 0: // Kestrel wing over a precise sight.
                        ink=Line(p,.16f,.70f,.48f,.57f)||Line(p,.84f,.70f,.52f,.57f)||Line(p,.22f,.57f,.48f,.47f)||Line(p,.78f,.57f,.52f,.47f)||Line(p,.5f,.35f,.5f,.18f)||Mathf.Abs(r-.115f)<.023f;break;
                    case 1: // Forging anvil and wake.
                        ink=p.y>.48f&&p.y<.65f&&p.x>.17f&&p.x<.83f||p.x>.36f&&p.x<.64f&&p.y>.28f&&p.y<.51f||Line(p,.25f,.27f,.75f,.27f)||Line(p,.16f,.77f,.48f,.72f)||Line(p,.48f,.72f,.84f,.77f);break;
                    case 2: // Six-sided electric housing with a lightning break.
                        for(int s=0;s<6;s++){float a=s*Mathf.PI/3,b=(s+1)*Mathf.PI/3;ink|=Segment(p,Vector2.one*.5f+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.34f,Vector2.one*.5f+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.34f)<.022f;}
                        ink|=Line(p,.58f,.77f,.40f,.49f,.036f)||Line(p,.40f,.49f,.60f,.51f,.036f)||Line(p,.60f,.51f,.43f,.23f,.036f);break;
                    case 3: // Living double helix enclosed by leaf ribs.
                        for(int s=0;s<20;s++){float a=s/20f,b=(s+1)/20f;ink|=Segment(p,new Vector2(.5f+Mathf.Sin(a*Mathf.PI*2)*.16f,.2f+a*.6f),new Vector2(.5f+Mathf.Sin(b*Mathf.PI*2)*.16f,.2f+b*.6f))<.023f;ink|=Segment(p,new Vector2(.5f-Mathf.Sin(a*Mathf.PI*2)*.16f,.2f+a*.6f),new Vector2(.5f-Mathf.Sin(b*Mathf.PI*2)*.16f,.2f+b*.6f))<.023f;}
                        ink|=Line(p,.34f,.38f,.66f,.38f,.015f)||Line(p,.34f,.62f,.66f,.62f,.015f);break;
                    case 4: // Open reclamation gear, furnace spark.
                        ink=Mathf.Abs(r-.27f)<.048f&&!(p.x>.55f&&p.y>.55f);
                        for(int s=0;s<7;s++){float a=s*Mathf.PI*2/7;ink|=Segment(p,Vector2.one*.5f+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.25f,Vector2.one*.5f+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.35f)<.039f;}
                        ink|=Line(p,.48f,.35f,.58f,.55f,.035f)||Line(p,.58f,.55f,.47f,.53f,.035f)||Line(p,.47f,.53f,.57f,.76f,.035f);break;
                    default: // Shield, supply chevrons.
                        ink=Line(p,.22f,.79f,.78f,.79f)||Line(p,.22f,.79f,.27f,.39f)||Line(p,.78f,.79f,.73f,.39f)||Line(p,.27f,.39f,.5f,.19f)||Line(p,.73f,.39f,.5f,.19f)||Line(p,.35f,.62f,.5f,.52f)||Line(p,.65f,.62f,.5f,.52f)||Line(p,.35f,.48f,.5f,.38f)||Line(p,.65f,.48f,.5f,.38f);break;
                }
                pixels[y*size+x]=ink?Color.white:Color.clear;
            }
            texture.SetPixels(pixels);texture.Apply(true,false);logos[index]=texture;return texture;
        }
        public static void Badge(Rect rect,string maker)
        {
            var old=GUI.color;GUI.color=new Color(.02f,.025f,.03f,.94f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=ManufacturerCatalog.Color(maker);GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),Logo(maker),ScaleMode.ScaleToFit,true);GUI.color=old;
        }
        static Texture2D moduleIllustrations;
        public static bool ModuleIllustrationAvailable=>Resources.Load<Texture2D>("Art/Manufacturers/ModuleIllustrations-v1")!=null;
        public static void ModulePortrait(Rect r,ArtifactItem item)
        {
            if(!moduleIllustrations)moduleIllustrations=Resources.Load<Texture2D>("Art/Manufacturers/ModuleIllustrations-v1");
            if(!moduleIllustrations){ModulePortraitFallback(r,item);return;}
            var old=GUI.color;GUI.color=new Color(.025f,.04f,.05f);GUI.DrawTexture(r,Texture2D.whiteTexture);
            int column=Mathf.Max(0,ManufacturerCatalog.Index(Maker(item))),row=Mathf.Clamp((int)item.effect,0,3);
            Rect uv=new Rect(column/6f+.008f,1-(row+1)/4f+.015f,1/6f-.016f,1/4f-.030f);
            float ratio=(moduleIllustrations.width/6f)/(moduleIllustrations.height/4f);
            Rect image=new Rect(r.x+4,r.y+4,r.width-8,r.height-8);
            if(ratio>image.width/image.height){float h=image.width/ratio;image.y+=(image.height-h)*.5f;image.height=h;}
            else{float w=image.height*ratio;image.x+=(image.width-w)*.5f;image.width=w;}
            GUI.color=Color.white;GUI.DrawTextureWithTexCoords(image,moduleIllustrations,uv,true);
            Badge(new Rect(r.x+3,r.yMax-Mathf.Min(25,r.height*.3f)-3,Mathf.Min(25,r.width*.3f),Mathf.Min(25,r.height*.3f)),Maker(item));
            GUI.color=Arsenal.Colors[(int)item.rarity];GUI.DrawTexture(new Rect(r.x,r.yMax-3,r.width,3),Texture2D.whiteTexture);GUI.color=old;
        }
        static void ModulePortraitFallback(Rect r,ArtifactItem item)
        {
            string maker=Maker(item);Color old=GUI.color;
            GUI.color=new Color(.025f,.04f,.05f);GUI.DrawTexture(r,Texture2D.whiteTexture);
            GUI.color=new Color(.24f,.29f,.28f);GUI.DrawTexture(new Rect(r.x+r.width*.18f,r.y+r.height*.16f,r.width*.64f,r.height*.67f),Texture2D.whiteTexture);
            Color wire=ManufacturerCatalog.Color(maker);int count=3+item.designVariant%5;
            for(int i=0;i<count;i++)
            {
                float x=r.x+r.width*(.23f+i*.54f/Mathf.Max(1,count-1));GUI.color=new Color(.60f,.44f,.21f);
                GUI.DrawTexture(new Rect(x,r.y+r.height*.11f,r.width*.035f,r.height*.78f),Texture2D.whiteTexture);
                GUI.color=wire;GUI.DrawTexture(new Rect(x-r.width*.014f,r.y+r.height*(.29f+(i%2)*.23f),r.width*.07f,r.height*.10f),Texture2D.whiteTexture);
            }
            Badge(new Rect(r.x+r.width*.34f,r.y+r.height*.31f,r.width*.32f,r.height*.32f),maker);
            GUI.color=Arsenal.Colors[(int)item.rarity];GUI.DrawTexture(new Rect(r.x,r.yMax-3,r.width,3),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
