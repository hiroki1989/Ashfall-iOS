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
            string[] en={"Modern precision guns: blued steel, oiled walnut, olive canvas and compact mechanical sights. Small analogue range dials support accuracy and quick servicing.","Old military and industrial equipment: forged steel, riveted repair plates, oxblood leather and hydraulic recoil buffers. Heavy impact and protection trade speed for stability.","Modern firearms rebuilt with valve-era electrics: ceramic terminals, compact copper coils and amber vacuum tubes. Burst power and rapid fire demand larger batteries and cooling.","Medical and civil-defence equipment: olive fabric, rubber respirators, glass medicine reservoirs and pressure valves. Siphon and healing systems trade direct impact for sustain.","Reclaimed field equipment: worn timber, repaired steel, leather slings and small furnace chambers. Blast pressure and ammunition recovery use visible pipes and salvage mechanisms.","Security and supply equipment: dark navy ballistic canvas, practical plate carriers, reinforced boots and pale identification marks. Analogue capacitor packs power short protective barriers."};
            string[] jp={"現代精密銃：黒染め鋼・油を含んだ木製銃床・オリーブ布・小型照準器。機械式測距ダイヤルが精度と整備性を支える。","旧軍・重工装備：鍛造鋼・鋲留め修理板・赤褐色の革・油圧緩衝器。重量と動作速度を代償に威力・防護を確保。","現代銃＋真空管時代の電装：碍子・小型銅コイル・琥珀色真空管。連射と瞬間出力を支える電池・冷却部を外観に反映。","医療・民間防衛装備：オリーブ布・ゴム製防毒面・薬液ガラス瓶・圧力弁。直接威力を抑え、吸命と回復で継戦。","再生野戦装備：傷んだ木材・補修鋼・革スリング・小型燃焼室。配管と再利用機構で爆圧・弾薬回収を支える。","警備・兵站装備：濃紺の防弾布・実用プレートキャリア・補強靴・淡い識別帯。アナログ蓄電装置で短時間の障壁を展開。"};
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
            if(DistinctEquipmentPortraits.Draw(r,item))return;
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
            int generation=ProductGeneration.Generation(item.designVariant);
            for(int n=0;n<generation;n++){GUI.color=ManufacturerCatalog.Color(Maker(item));GUI.DrawTexture(new Rect(r.x+r.width*.76f,r.y+5+n*r.height*.065f,r.width*(n%2==0?.14f:.09f),Mathf.Max(1,r.height*.018f)),Texture2D.whiteTexture);}
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
