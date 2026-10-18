using System.Collections.Generic;
using UnityEngine;
using static NeonVault.DetailedHeroArsenal3D;

namespace NeonVault
{
    // Sixty deliberately authored constructions. Shared methods below are fabrication
    // primitives, never recipes or calls into the ordinary equipment generator.
    public static class BespokeLegendaryModels3D
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Material blood,blued,walnut;
        static Material M(string s){
            if(s=="ProductSteel"){if(!blued){blued=new Material(DetailedHeroMaterials3D.Get(s)){name="Legendary worn blued gunmetal",color=new Color(.065f,.071f,.068f),hideFlags=HideFlags.DontSave};if(blued.HasProperty("_AtlasStrength"))blued.SetFloat("_AtlasStrength",.45f);}return blued;}
            if(s=="Wood"){if(!walnut)walnut=new Material(DetailedHeroMaterials3D.Get(s)){name="Legendary oil darkened walnut",color=new Color(.135f,.077f,.039f),hideFlags=HideFlags.DontSave};return walnut;}
            if(s!="BloodGlass")return DetailedHeroMaterials3D.Get(s);if(!blood){blood=new Material(DetailedHeroMaterials3D.Get("AmberGlass")){name="Legendary sealed blood glass",color=new Color(.32f,.035f,.018f),hideFlags=HideFlags.DontSave};}return blood;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ClearMaterials(){if(blood)Object.Destroy(blood);if(blued)Object.Destroy(blued);if(walnut)Object.Destroy(walnut);blood=blued=walnut=null;}
        static Transform G(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static readonly string[] metal={"ProductSteel","IronwakeSteel","ProductSteel","MirelineBone","ProductSteel","ProductSteel"};
        static readonly string[] accent={"ProductCopper","IronwakeCopper","ProductCopper","BloodGlass","Wood","AegisWhite"};
        static void Panel(Transform g,string n,string mat,float x,float y,float z,float w,float h,float l)
        {var t=G(n,g);t.localPosition=V(x,y,z);Profile(n,t,M(mat),w,new Vector2(-l/2,-h/2),new Vector2(-l/2,h*.28f),new Vector2(-l*.32f,h/2),new Vector2(l*.34f,h/2),new Vector2(l/2,h*.26f),new Vector2(l/2,-h*.32f),new Vector2(l*.3f,-h/2));}
        static void Barrel(Transform g,float z,float length,float radius=.02f,float y=0,float x=0){Tube("Steel bore",g,M("ProductSteel"),V(x,y,z),radius,length,.65f);}
        static void Strut(Transform g,string mat,Vector3 a,Vector3 b,float radius=.008f){Rod("Structural "+mat,g,M(mat),a,b,radius);}
        static void Tank(Transform g,string mat,float x,float y,float z,float radius,float height)
        {var p=G("Upright sealed "+mat,g);p.localPosition=V(x,y,z);p.localRotation=Quaternion.Euler(-90,0,0);Tube("Vessel",p,M(mat),Vector3.zero,radius,height,.04f);for(int i=0;i<2;i++)Tube("Brass seal",p,M("ProductCopper"),V(0,0,i*(height-.008f)),radius*1.12f,.009f,.5f);}
        static void Coils(Transform g,float z,int count,float r=.04f,float y=0){for(int i=0;i<count;i++)Tube("Copper induction winding",g,M("ProductCopper"),V(0,y,z+i*.016f),r,.007f,.83f,-1,16);}
        static void Stock(Transform g,string mat,float rear,float drop=.085f)
        {Profile("Open skeletal shoulder stock",g,M(mat),.06f,new Vector2(-.10f,.025f),new Vector2(rear,.035f),new Vector2(rear,-drop),new Vector2(rear+.035f,-drop),new Vector2(rear+.035f,-.005f),new Vector2(-.12f,-.015f));}
        static void Feed(Transform g,string mat,float z,float height,float angle=0)
        {var p=G("Separate feed assembly",g);p.localPosition=V(0,-.055f,z);p.localRotation=Quaternion.Euler(angle,0,0);Panel(p,"Feed box",mat,0,-height/2,0,.052f,height,.065f);}
        static void Drum(Transform g,string mat,float x,float y,float z,float radius)
        {var p=G("Transverse ammunition drum",g);p.localPosition=V(x,y,z);p.localRotation=Quaternion.Euler(0,90,0);Tube("Pressed drum",p,M(mat),V(0,0,-.055f),radius,.11f,.02f,-1,24);Tube("Drum hub",p,M("ProductSteel"),V(0,0,.055f),radius*.25f,.012f,.02f);}
        static void Scope(Transform g,float z,float length){Tube("Modern optic housing",g,M("ProductSteel"),V(0,.10f,z),.024f,length,.7f);Tube("Recessed optic lens",g,M("TealGlass"),V(0,.10f,z+length-.003f),.017f,.003f,.03f);}
        static void Cylinder(Transform g,string mat,float z,float radius,float length,int chambers,bool glass=false)
        {
            Tube("Longitudinal revolver cylinder",g,M(mat),V(0,.035f,z),radius,length,.03f,-1,24);
            for(int i=0;i<chambers;i++){float a=i*Mathf.PI*2/chambers;float x=Mathf.Cos(a)*radius*.78f,y=.035f+Mathf.Sin(a)*radius*.78f;
                Tube(glass?"Exposed chamber ampoule":"Recessed cylinder flute",g,M(glass?"AmberGlass":"Dark"),V(x,y,z+.01f),radius*.22f,length-.02f,.03f,-1,12);
                Tube("Brass chamber end",g,M("ProductCopper"),V(x,y,z+length-.005f),radius*.19f,.006f,.12f,-1,10);}
            for(int i=0;i<2;i++)Tube("Cylinder end band",g,M("ProductSteel"),V(0,.035f,z+i*(length-.008f)),radius*1.03f,.008f,.85f);
        }
        static void Sling(Transform g,float rear,float front){Line("Loose leather weapon sling",g,M("LeatherDark"),.006f,V(.045f,-.04f,rear),V(.06f,-.17f,rear*.45f),V(.065f,-.19f,front*.55f),V(.04f,-.04f,front));}
        static void Belt(Transform g,float x,float z){for(int i=0;i<8;i++){float y=-.02f-i*.023f;Strut(g,"ProductCopper",V(x-.035f,y,z),V(x+.035f,y,z),.007f);Panel(g,"Linked ammunition belt","ProductSteel",x,y,z,.012f,.011f,.027f);}}
        static void Bipod(Transform g,float z){for(int s=-1;s<=1;s+=2){Strut(g,"ProductSteel",V(s*.025f,-.02f,z),V(s*.095f,-.23f,z+.055f),.008f);Strut(g,"ProductSteel",V(s*.075f,-.23f,z+.035f),V(s*.115f,-.23f,z+.075f),.007f);}}
        // Machined vented jackets are built from annular bands and longitudinal ribs:
        // the dark gaps are real openings, not black paint on a solid cylinder.
        static void VentedJacket(Transform g,float z,float length,float radius,int bands)
        {
            for(int i=0;i<=bands;i++)Tube("Separate perforated barrel jacket band",g,M("ProductSteel"),V(0,.025f,z+length*i/bands),radius,.012f,.88f,-1,24);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Strut(g,"ProductSteel",V(Mathf.Cos(a)*radius,.025f+Mathf.Sin(a)*radius,z),V(Mathf.Cos(a)*radius,.025f+Mathf.Sin(a)*radius,z+length+.012f),.005f);}
        }
        static void LeatherGrip(Transform g,float rear,float bottom,float width)
        {
            Profile("Curved bolted leather grip scales",g,M("LeatherDark"),width,new Vector2(rear+.055f,-.025f),new Vector2(rear+.07f,-.075f),new Vector2(rear+.02f,bottom),new Vector2(rear-.045f,bottom-.01f),new Vector2(rear-.06f,bottom+.035f),new Vector2(rear-.015f,-.055f));
            for(int side=-1;side<=1;side+=2){
                for(int i=0;i<7;i++)Strut(g,"Leather",V(side*(width*.5f+.001f),-.077f-i*.013f,rear-.003f-i*.003f),V(side*(width*.5f+.001f),-.082f-i*.013f,rear+.045f-i*.003f),.0017f);
                var screw=G("Grip scale retaining screw",g);screw.localPosition=V(side*(width*.5f+.002f),bottom+.04f,rear-.015f);screw.localRotation=Quaternion.LookRotation(Vector3.right*side);Tube("Slotted brass grip screw",screw,M("Brass"),Vector3.zero,.0045f,.002f,.06f,-1,12);
            }
        }
        public static DetailedHeroEquipment3D.WeaponSockets BuildWeapon(Transform parent,Gun gun,int index)
        {
            index=Mathf.Clamp(index,0,29);int maker=index/5,kind=index%5;var g=G("Bespoke legendary weapon "+index,parent);string m=metal[maker],a=accent[maker];float end=.66f,rear=-.34f;
            // Only the hand contact geometry is common. Each receiver, barrel/feed,
            // stock and signature mechanism is independently placed in its case.
            if(index<21||index>24)Profile("Anatomical grip",g,M(maker==4?"Wood":"LeatherDark"),.056f,new Vector2(-.067f,-.025f),new Vector2(.004f,-.04f),new Vector2(-.036f,-.18f),new Vector2(-.101f,-.16f));
            Line("Open trigger guard",g,M("ProductSteel"),.004f,V(0,-.055f,.005f),V(0,-.12f,.02f),V(0,-.12f,.075f),V(0,-.045f,.09f));
            switch(index)
            {
                case 0:Panel(g,"Monolithic precision action",m,0,.025f,0,.075f,.075f,.29f);Barrel(g,.14f,.53f,.018f);Stock(g,"Wood",-.38f);Scope(g,-.06f,.24f);Feed(g,m,.05f,.15f);Panel(g,"Third round indexed escapement",a,.044f,.025f,-.03f,.012f,.035f,.08f);end=.67f;rear=-.38f;break;
                case 1:Panel(g,"Precision revolver top strap",m,0,.095f,.02f,.075f,.025f,.27f);Cylinder(g,m,-.075f,.070f,.13f,6);Barrel(g,.06f,.25f,.033f,.07f);Panel(g,"Impact recorder",a,.075f,.04f,-.045f,.012f,.045f,.06f);end=.31f;rear=-.15f;break;
                case 2:Panel(g,"Split locked sporting breech",m,0,.02f,-.015f,.09f,.09f,.25f);Barrel(g,.10f,.37f,.027f,.029f);Barrel(g,.10f,.37f,.027f,-.030f);Stock(g,"Wood",-.33f);Panel(g,"Kill tally mechanical slide",a,.052f,.04f,.01f,.014f,.04f,.12f);end=.47f;break;
                case 3:Panel(g,"Balanced precision belt receiver",m,0,.025f,.005f,.10f,.11f,.31f);Barrel(g,.16f,.43f,.025f);Stock(g,"Wood",-.36f);Panel(g,"Low slung ammunition cassette",a,-.09f,-.08f,.02f,.10f,.13f,.15f);Scope(g,-.05f,.15f);Panel(g,"Reload calibrated pressure lever","ProductSteel",.062f,.055f,-.06f,.015f,.025f,.14f);end=.59f;break;
                case 4:Panel(g,"Compact straight line action",m,0,.02f,.03f,.07f,.08f,.29f);Barrel(g,.17f,.20f,.024f);Feed(g,"Dark",.075f,.21f,12);Stock(g,"ProductSteel",-.28f,.05f);Panel(g,"Dash inertial slide",a,0,.08f,.04f,.045f,.018f,.16f);end=.37f;rear=-.28f;break;
                case 5:Panel(g,"Forged hydraulic bolt block",m,0,.02f,0,.115f,.13f,.30f);Barrel(g,.15f,.47f,.031f);Stock(g,"Wood",-.39f);Tank(g,"IronwakeCopper",.075f,-.025f,-.04f,.026f,.14f);Strut(g,"ProductSteel",V(.08f,.08f,-.05f),V(.08f,.08f,.16f),.012f);Feed(g,m,.06f,.16f);end=.62f;rear=-.39f;break;
                case 6:Panel(g,"Hydraulic revolver upper strap",m,0,.115f,.025f,.11f,.025f,.28f);Barrel(g,.17f,.20f,.042f,.075f);Cylinder(g,"IronwakeSteel",-.08f,.078f,.16f,5,true);Tank(g,"ProductSteel",-.075f,-.10f,.04f,.02f,.15f);end=.37f;rear=-.15f;break;
                case 7:Panel(g,"Break open siege gun",m,0,.01f,0,.13f,.13f,.24f);Barrel(g,.11f,.32f,.047f,.035f);Barrel(g,.11f,.32f,.047f,-.06f);Stock(g,"Wood",-.35f,.13f);Strut(g,"IronwakeCopper",V(.085f,-.025f,-.08f),V(.085f,-.025f,.28f),.017f);end=.43f;break;
                case 8:Panel(g,"Riveted fortress belt breech",m,0,.015f,0,.15f,.16f,.35f);Barrel(g,.17f,.47f,.039f);Drum(g,"IronwakeCopper",-.12f,-.10f,.025f,.11f);Stock(g,"ProductSteel",-.41f,.12f);for(int s=-1;s<=1;s+=2)Tank(g,"ProductSteel",s*.10f,.02f,-.06f,.025f,.17f);end=.64f;rear=-.41f;break;
                case 9:Panel(g,"Stamped compact hydraulic shell",m,0,.02f,.025f,.10f,.11f,.29f);Barrel(g,.17f,.19f,.032f);Feed(g,"IronwakeCopper",.08f,.23f);Stock(g,"ProductSteel",-.29f);Strut(g,"IronwakeCopper",V(.066f,.06f,-.09f),V(.066f,.06f,.16f),.019f);end=.36f;rear=-.29f;break;
                case 10:Panel(g,"Ceramic rail action",m,0,.025f,-.005f,.09f,.10f,.31f);Barrel(g,.15f,.49f,.019f);Coils(g,.17f,5,.033f,-.045f);for(int t=-1;t<=1;t+=2)Tank(g,"AmberGlass",0,.075f,-.015f+t*.032f,.018f,.095f);Stock(g,"Wood",-.37f);Tank(g,"AmberGlass",.065f,.02f,-.09f,.023f,.13f);Scope(g,-.07f,.20f);end=.64f;rear=-.37f;break;
                case 11:Panel(g,"Arc revolver upper strap",m,0,.105f,.025f,.08f,.025f,.29f);Barrel(g,.20f,.16f,.025f,.07f);Cylinder(g,m,-.08f,.069f,.13f,6,true);Tank(g,"AmberGlass",-.053f,.04f,-.04f,.018f,.11f);end=.36f;rear=-.15f;break;
                case 12:Panel(g,"Fork lightning breech",m,0,.02f,0,.105f,.10f,.26f);Barrel(g,.13f,.34f,.027f,.03f);Barrel(g,.13f,.34f,.027f,-.028f);for(int i=0;i<2;i++)Tank(g,"AmberGlass",0,.075f,-.035f+i*.055f,.019f,.09f);Stock(g,"Dark",-.32f);Tank(g,"AmberGlass",0,.08f,.03f,.027f,.14f);end=.49f;break;
                case 13:Panel(g,"Induction generator breech",m,0,.015f,-.01f,.13f,.14f,.34f);Barrel(g,.16f,.43f,.034f);for(int t=-1;t<=1;t+=2)Tank(g,"AmberGlass",0,.085f,-.02f+t*.037f,.022f,.115f);Drum(g,"VoltarcYellow",-.10f,-.08f,.02f,.09f);Stock(g,"Wood",-.38f);Tank(g,"AmberGlass",.09f,.04f,-.07f,.022f,.14f);end=.59f;rear=-.38f;break;
                case 14:Panel(g,"Insulated bullpup pulse body",m,0,.02f,-.03f,.09f,.11f,.39f);Barrel(g,.17f,.19f,.025f);for(int t=-1;t<=1;t+=2)Tank(g,"AmberGlass",0,.075f,.04f+t*.031f,.018f,.10f);Feed(g,"VoltarcYellow",-.14f,.15f);Stock(g,m,-.30f);Tank(g,"AmberGlass",.06f,.07f,-.04f,.018f,.10f);end=.36f;rear=-.30f;break;
                case 15:Panel(g,"Sealed surgical needle receiver",m,0,.02f,-.015f,.075f,.09f,.30f);Barrel(g,.14f,.52f,.014f);Stock(g,"Wood",-.38f);Scope(g,-.06f,.21f);Tank(g,a,-.055f,.01f,.04f,.024f,.15f);Line("Siphon return hose",g,M("LeatherDark"),.007f,V(-.06f,.14f,.04f),V(-.08f,.11f,.35f),V(-.025f,.015f,.47f));end=.66f;rear=-.38f;break;
                case 16:Panel(g,"Rescue injector revolver top strap",m,0,.10f,.025f,.075f,.025f,.28f);Cylinder(g,m,-.08f,.072f,.14f,6,true);Barrel(g,.18f,.16f,.022f,.07f);Tank(g,a,-.05f,-.045f,.06f,.023f,.16f);Panel(g,"Medical pressure gauge","ProductSteel",.05f,.06f,.02f,.018f,.045f,.045f);end=.34f;rear=-.15f;break;
                case 17:Panel(g,"Twin fluid dispersal chambers",m,0,.02f,0,.12f,.11f,.24f);Barrel(g,.12f,.29f,.030f,.03f);Barrel(g,.12f,.29f,.030f,-.032f);for(int s=-1;s<=1;s+=2)Tank(g,a,s*.075f,-.08f,-.035f,.025f,.19f);Stock(g,"MirelineOlive",-.34f);end=.41f;break;
                case 18:Panel(g,"Sterile perfusion machine receiver",m,0,.02f,0,.13f,.13f,.33f);Barrel(g,.17f,.41f,.029f);Stock(g,"Wood",-.38f);Tank(g,a,-.105f,-.13f,-.01f,.05f,.24f);Tank(g,a,.09f,-.03f,.04f,.027f,.17f);Line("Fluid feed",g,M("LeatherDark"),.009f,V(-.105f,.11f,-.01f),V(-.10f,.15f,.13f),V(0,.065f,.18f));end=.58f;rear=-.38f;break;
                case 19:Panel(g,"Compact quarantine sealed upper",m,0,.025f,.005f,.09f,.09f,.31f);Barrel(g,.16f,.20f,.025f);Feed(g,"MirelineOlive",.08f,.20f,18);Stock(g,"ProductSteel",-.29f);Tank(g,a,-.06f,.025f,-.04f,.025f,.12f);end=.36f;rear=-.29f;break;
                case 20:Panel(g,"Salvaged narrow steel action",m,0,.015f,0,.082f,.09f,.32f);Barrel(g,.16f,.53f,.023f);Stock(g,"Wood",-.41f,.115f);Scope(g,-.10f,.16f);for(int s=-1;s<=1;s+=2)Tank(g,"AmberGlass",0,.055f,.115f+s*.035f,.021f,.125f);Line("Bent brass pressure pipe",g,M("ProductCopper"),.009f,V(.064f,.07f,-.09f),V(.08f,.13f,.04f),V(.065f,.13f,.24f),V(.027f,.025f,.32f));Tube("Vented muzzle brake",g,M("ProductSteel"),V(0,0,.61f),.039f,.105f,.72f);for(int i=0;i<4;i++)Panel(g,"Brake vent recess","Dark",.038f,0,.625f+i*.02f,.003f,.025f,.009f);Feed(g,"ProductSteel",.03f,.15f);end=.715f;rear=-.41f;break;
                case 21:
                    Profile("Revolver forged continuous frame",g,M(m),.072f,new Vector2(-.14f,-.035f),new Vector2(-.14f,.11f),new Vector2(.115f,.11f),new Vector2(.16f,.07f),new Vector2(.14f,-.05f),new Vector2(.03f,-.065f),new Vector2(-.02f,-.12f),new Vector2(-.11f,-.09f));
                    Cylinder(g,"ProductSteel",-.08f,.069f,.15f,6,true);
                    for(int i=0;i<6;i++){float ca=i*Mathf.PI/3;Tube("Exposed brass cartridge cell",g,M("Brass"),V(Mathf.Cos(ca)*.061f,.035f+Mathf.Sin(ca)*.061f,-.065f),.016f,.125f,.9f,-1,16);}
                    for(int side=-1;side<=1;side+=2){Panel(g,"Revolver cylinder access window","Dark",side*.074f,.039f,-.004f,.004f,.061f,.102f);for(int row=0;row<3;row++)Strut(g,"Brass",V(side*.077f,.016f+row*.022f,-.052f),V(side*.077f,.016f+row*.022f,.041f),.005f);}
                    Barrel(g,.072f,.225f,.025f,.035f);VentedJacket(g,.11f,.17f,.045f,4);
                    LeatherGrip(g,-.085f,-.205f,.071f);
                    Panel(g,"Revolver raised front sight",m,0,.083f,.257f,.012f,.027f,.022f);
                    Strut(g,"ProductSteel",V(-.038f,-.025f,-.08f),V(-.052f,-.008f,.052f),.005f);
                    end=.298f;rear=-.15f;break;
                case 22:
                    Panel(g,"Welded break barrel furnace",m,0,.01f,-.015f,.115f,.12f,.25f);
                    Barrel(g,.10f,.235f,.028f,.025f,-.033f);Barrel(g,.10f,.235f,.028f,.025f,.033f);
                    Profile("Sawed shotgun walnut shoulder stock",g,M("Wood"),.075f,new Vector2(-.34f,-.12f),new Vector2(-.32f,.015f),new Vector2(-.15f,.025f),new Vector2(-.065f,-.035f),new Vector2(-.14f,-.06f),new Vector2(-.25f,-.11f));
                    Panel(g,"Double barrel forward breech block",m,0,.025f,.12f,.14f,.115f,.075f);
                    for(int side=-1;side<=1;side+=2){Tank(g,"AmberGlass",side*.077f,-.035f,-.04f,.022f,.11f);for(int i=0;i<4;i++)Strut(g,"ProductCopper",V(side*.071f,-.025f,.16f+i*.036f),V(side*.071f,.064f,.16f+i*.036f),.004f);}
                    for(int i=0;i<5;i++)Panel(g,"Welded furnace band",m,0,.025f,.16f+i*.032f,.137f,.108f,.008f);
                    LeatherGrip(g,-.073f,-.16f,.064f);end=.335f;rear=-.34f;break;
                case 23:
                    Panel(g,"Reclaimed machine gun engine block",m,0,.02f,0,.14f,.14f,.34f);Barrel(g,.17f,.42f,.024f,.025f);Stock(g,"Wood",-.39f);
                    for(int side=-1;side<=1;side+=2){
                        Panel(g,"Riveted belt feed lid",m,side*.073f,.04f,.0f,.018f,.07f,.19f);
                        var feed=G("Exposed hanging brass ammunition belt",g);feed.localPosition=V(side*.105f,-.025f,.015f);
                        for(int i=0;i<9;i++){float y=-i*.022f;Strut(feed,"Brass",V(-.027f,y,0),V(.027f,y,0),.007f);Strut(feed,"ProductSteel",V(-.006f,y-.009f,0),V(.006f,y-.009f,0),.0025f);}
                    }
                    Panel(g,"Machine gun suspended field ammunition box","LeatherDark",0,-.205f,.055f,.19f,.20f,.16f);
                    for(int side=-1;side<=1;side+=2)Panel(g,"Ammunition box leather retaining strap","Leather",side*.062f,-.205f,.141f,.02f,.20f,.008f);
                    Tank(g,"AmberGlass",.09f,.01f,-.04f,.026f,.18f);VentedJacket(g,.22f,.28f,.047f,8);Scope(g,-.12f,.18f);LeatherGrip(g,-.08f,-.19f,.068f);
                    end=.59f;rear=-.39f;break;
                case 24:
                    Panel(g,"Stamped scrapyard open upper",m,0,.025f,.015f,.085f,.085f,.29f);Barrel(g,.16f,.19f,.023f,.025f);Stock(g,"ProductSteel",-.28f,.055f);Feed(g,"ProductSteel",.07f,.19f,15);
                    for(int side=-1;side<=1;side+=2)Tank(g,"AmberGlass",side*.025f,.07f,.005f,.017f,.09f);
                    VentedJacket(g,.19f,.105f,.041f,3);
                    Profile("Vertical forward leather handgrip",g,M("LeatherDark"),.048f,new Vector2(.19f,-.025f),new Vector2(.225f,-.04f),new Vector2(.215f,-.19f),new Vector2(.18f,-.18f));
                    for(int i=0;i<7;i++)Panel(g,"Forward handgrip texture rib","Leather",0,-.065f-i*.015f,.201f,.051f,.005f,.029f);
                    LeatherGrip(g,-.078f,-.17f,.058f);Strut(g,"ProductCopper",V(.056f,.06f,-.09f),V(.056f,.06f,.18f),.005f);
                    Panel(g,"Rear flip sight",m,0,.094f,-.105f,.018f,.035f,.015f);end=.35f;rear=-.28f;break;
                case 25:Panel(g,"Security long rail laminate action",m,0,.02f,0,.10f,.11f,.32f);Barrel(g,.16f,.49f,.022f);Stock(g,"Wood",-.38f);Scope(g,-.08f,.23f);Feed(g,"Dark",.06f,.16f);Panel(g,"Ballistic counterfire cheek shield",a,-.064f,.02f,.01f,.025f,.16f,.23f);end=.65f;rear=-.38f;break;
                case 26:Panel(g,"Counterassault revolver top strap",m,0,.105f,.025f,.10f,.028f,.30f);Cylinder(g,m,-.08f,.074f,.14f,6);Barrel(g,.19f,.16f,.03f,.07f);Panel(g,"Compact reaction buckler",a,-.07f,-.005f,.04f,.027f,.17f,.19f);end=.35f;rear=-.15f;break;
                case 27:Panel(g,"Door breach laminate breech",m,0,.015f,0,.12f,.14f,.29f);Barrel(g,.14f,.30f,.048f);Stock(g,m,-.34f);Panel(g,"Broad blast deflector",a,0,.06f,.20f,.20f,.035f,.19f);Feed(g,"Dark",.07f,.17f);end=.44f;break;
                case 28:Panel(g,"Riot suppression fortified receiver",m,0,.02f,0,.15f,.15f,.35f);Barrel(g,.17f,.42f,.032f);Stock(g,m,-.40f);Panel(g,"Shield battery box",a,-.12f,-.07f,.01f,.12f,.17f,.18f);Panel(g,"Protective roof shroud",m,0,.12f,.24f,.16f,.025f,.25f);end=.59f;rear=-.40f;break;
                case 29:Panel(g,"Close protection telescopic body",m,0,.025f,.01f,.09f,.10f,.32f);Barrel(g,.17f,.19f,.027f);Feed(g,"Dark",.06f,.21f);Stock(g,"ProductSteel",-.29f,.06f);Panel(g,"Dash charged side shield",a,-.065f,.025f,.025f,.02f,.12f,.23f);end=.36f;rear=-.29f;break;
            }
            // Serviceable mechanisms: separate fasteners, ejection opening and controls,
            // rather than a blank receiver extrusion. These are fabrication details,
            // not interchangeable bodies or signature mechanisms.
            if(kind!=1){
                float width=maker==1?.059f:.047f;
                for(int side=-1;side<=1;side+=2){
                    Panel(g,"Recessed receiver service seam","Dark",side*width,.018f,.025f,.0025f,.022f,.18f);
                    for(int i=0;i<4;i++){
                        var screw=G("Slotted receiver fastening",g);screw.localPosition=V(side*(width+.003f),-.018f,-.09f+i*.065f);screw.localRotation=Quaternion.LookRotation(Vector3.right*side);
                        Tube("Fastener steel rim",screw,M("ProductSteel"),Vector3.zero,.005f,.002f,.08f,-1,12);
                        Strut(screw,"Dark",V(-.003f,0,.0025f),V(.003f,0,.0025f),.0008f);
                    }
                }
                Panel(g,"Open ejection recess","Dark",width+.004f,.032f,.055f,.003f,.025f,.080f);
                Strut(g,"ProductSteel",V(width,.006f,-.035f),V(width+.028f,.002f,-.035f),.004f);
                Panel(g,"Safety selector","ProductSteel",width+.009f,-.015f,-.07f,.005f,.009f,.027f);
            }
            if(index==20){
                // Menu rifle: broad triangular wooden frame, binding, saddle-mounted
                // upright pressure glasses and a long bored steel muzzle brake.
                Profile("Menu rifle triangular walnut stock",g,M("Wood"),.067f,new Vector2(-.41f,.038f),new Vector2(-.11f,.014f),new Vector2(-.18f,-.025f),new Vector2(-.35f,-.09f),new Vector2(-.41f,-.09f));
                Profile("Stock skeleton inset",g,M("Dark"),.069f,new Vector2(-.375f,.009f),new Vector2(-.19f,-.003f),new Vector2(-.35f,-.059f));
                Panel(g,"Worn rubber shoulder pad","LeatherDark",0,-.025f,-.414f,.071f,.14f,.014f);
                for(int i=0;i<5;i++)Panel(g,"Canvas fore-end binding","ClothDark",0,-.012f,.245f+i*.014f,.071f,.067f,.008f);
                Scope(g,-.17f,.16f);
                for(int i=0;i<2;i++){
                    var saddle=G("Pressure vessel retaining saddle",g);saddle.localPosition=V(0,.061f,.080f+i*.07f);
                    for(int side=-1;side<=1;side+=2)Strut(saddle,"ProductSteel",V(side*.025f,0,0),V(side*.025f,.13f,0),.003f);
                }
                for(int i=0;i<4;i++)for(int side=-1;side<=1;side+=2)Panel(g,"Deep muzzle gas port","Dark",side*.039f,.006f,.632f+i*.020f,.003f,.021f,.011f);
            }
            if(kind==0){if(index!=20)Bipod(g,end-.13f);Sling(g,rear+.07f,.31f);}
            if(kind==3){Belt(g,-.075f,.025f);Bipod(g,end-.13f);Panel(g,"Fabric ammunition satchel","Cloth",-.09f,-.15f,.015f,.10f,.18f,.14f);}
            if(kind==2){Sling(g,rear+.06f,.30f);for(int i=0;i<7;i++)Panel(g,"Separate wood pump rib","Wood",0,-.035f,.17f+i*.023f,.075f,.055f,.014f);}
            if(kind==1){
                Profile("Raised revolver hammer spur",g,M("ProductSteel"),.025f,new Vector2(-.14f,.06f),new Vector2(-.17f,.12f),new Vector2(-.145f,.125f),new Vector2(-.11f,.07f));
                for(int i=0;i<3;i++)Panel(g,"Vented revolver barrel port","Dark",.034f,.065f,.16f+i*.035f,.003f,.012f,.018f);
            }
            if(kind!=1)
            {
                // Palm contact is physically supported at the authored socket;
                // the bare barrel alone sat 35 mm above the support hand.
                switch(maker){
                    case 0:Panel(g,"Checkered polymer support bed","Dark",0,-.032f,.23f,.058f,.047f,.13f);break;
                    case 1:Panel(g,"Ribbed army fore-end","Wood",0,-.031f,.23f,.068f,.05f,.14f);for(int i=0;i<4;i++)Panel(g,"Forged fore-end transverse rib",m,0,-.034f,.18f+i*.032f,.075f,.046f,.008f);break;
                    case 2:Panel(g,"Insulated polymer hand cradle","Dark",0,-.032f,.23f,.06f,.048f,.125f);for(int s=-1;s<=1;s+=2)Strut(g,"VoltarcNavy",V(s*.035f,-.02f,.16f),V(s*.035f,-.02f,.30f),.008f);break;
                    case 3:Panel(g,"Sealed medical fore-end","MirelineOlive",0,-.032f,.23f,.065f,.048f,.14f);break;
                    case 4:Profile("Worn shaped wood fore-end",g,M("Wood"),.063f,new Vector2(.16f,-.012f),new Vector2(.31f,-.012f),new Vector2(.29f,-.052f),new Vector2(.18f,-.058f));break;
                    case 5:Panel(g,"Security polymer handguard","Dark",0,-.03f,.23f,.064f,.05f,.14f);for(int i=0;i<3;i++)Panel(g,"Handguard recessed traction stripe",a,0,-.055f,.19f+i*.04f,.056f,.003f,.009f);break;
                }
            }
            else g.localScale=Vector3.one*.68f;
            return Finish(g,V(0,-.095f,-.045f),V(0,-.055f,kind==1?.13f:.23f),V(0,-.015f,rear),V(0,0,end));
        }

        // Continuous fitted surfaces are offset outside the existing garment. No
        // primitive spans an animated joint. Closed back/edge faces prevent holes.
        static void Wrap(Transform p,string name,string mat,float rx,float rz,float top,float bottom,float frontOffset=0,float arc=3.141593f)
        {
            const int rows=8,cols=32;var v=new List<Vector3>();var ix=new List<int>();
            for(int layer=0;layer<2;layer++)for(int y=0;y<=rows;y++)for(int i=0;i<=cols;i++){float t=y/(float)rows,a=-arc+2*arc*i/cols;float flare=1+.045f*Mathf.Sin(t*Mathf.PI);float r=layer==0?1:.95f;v.Add(V(Mathf.Sin(a)*rx*flare*r,Mathf.Lerp(bottom,top,t),Mathf.Cos(a)*rz*flare*r+frontOffset));}
            int count=(rows+1)*(cols+1);for(int y=0;y<rows;y++)for(int i=0;i<cols;i++){int q=y*(cols+1)+i,b=q+cols+1;ix.AddRange(new[]{q,q+1,b,q+1,b+1,b,q+count,b+count,q+1+count,q+1+count,b+count,b+1+count});}
            for(int y=0;y<=rows;y++)for(int i=0;i<=cols;i++){int q=y*(cols+1)+i,n=-1;if(y==0&&i<cols)n=q+1;else if(y==rows&&i>0)n=q-1;else if(i==0&&y<rows)n=q+cols+1;else if(i==cols&&y>0)n=q-cols-1;if(n>=0)ix.AddRange(new[]{q,n,q+count,n,n+count,q+count});}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(ix,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var u=new Vector2[v.Count];for(int i=0;i<u.Length;i++)u[i]=new Vector2(v[i].x*3+v[i].z*2,v[i].y*3);mesh.uv=u;mesh.RecalculateTangents();var g=G(name,p);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(mat);g.gameObject.AddComponent<ManufacturerEquipment3D.MeshLifetime>().meshes.Add(mesh);
        }
        static void FacePlate(Transform g,string name,string mat,float x,float y,float z,float w,float h,float depth=.025f){var p=G(name,g);p.localPosition=V(x,y,z);p.localRotation=Quaternion.Euler(0,90,0);Panel(p,name,mat,0,0,0,depth,h,w);}
        static void Eye(Transform g,float x,string glass="TealGlass"){Tube("Round protective goggle steel rim",g,M("ProductSteel"),V(x,.115f,.105f),.041f,.033f,.78f);Tube("Recessed goggle lens",g,M(glass),V(x,.115f,.135f),.031f,.003f,.02f);}
        static void HeadShell(Transform g,string name,string mat,float rx,float rz,float top,float bottom)
        {var back=G(name,g);back.localRotation=Quaternion.Euler(0,180,0);Wrap(back,name,mat,rx,rz,top,bottom,0,2.08f);}
        static void Crown(Transform p,string mat)
        {
            var v=new List<Vector3>();var ix=new List<int>();const int sides=32,rings=8;
            for(int j=0;j<=rings;j++){float a=j/(float)rings*Mathf.PI*.5f;for(int i=0;i<=sides;i++){float b=i/(float)sides*Mathf.PI*2;v.Add(V(Mathf.Sin(b)*.132f*Mathf.Cos(a),.235f+Mathf.Sin(a)*.055f,Mathf.Cos(b)*.125f*Mathf.Cos(a)));}}
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int q=j*(sides+1)+i,b=q+sides+1;ix.AddRange(new[]{q,q+1,b,q+1,b+1,b});}
            var mesh=new Mesh{name="Continuous domed legendary crown"};mesh.SetVertices(v);mesh.SetTriangles(ix,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var uv=new Vector2[v.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(v[i].x*4,v[i].z*4);mesh.uv=uv;mesh.RecalculateTangents();var g=G(mesh.name,p);g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(mat);g.gameObject.AddComponent<ManufacturerEquipment3D.MeshLifetime>().meshes.Add(mesh);
        }
        static void Mechanism(Transform g,string mat,float x,float y,float z,int count,float spacing=.03f){for(int i=0;i<count;i++)Tank(g,mat,x+(i-(count-1)*.5f)*spacing,y,z,.011f,.06f);}
        static void FieldPouch(Transform g,float x,float y,float z,string mat="LeatherDark")
        {FacePlate(g,"Sewn field pouch",mat,x,y,z,.075f,.105f,.032f);FacePlate(g,"Stitched overlapping pouch flap","Leather",x,y+.027f,z+.019f,.079f,.036f,.008f);FacePlate(g,"Brass pouch clasp","ProductCopper",x,y+.005f,z+.025f,.012f,.020f,.006f);}
        static void Dial(Transform g,float x,float y,float z,float r=.028f)
        {Tube("Circular analog mechanism steel bezel",g,M("ProductSteel"),V(x,y,z),r,.014f,.78f);Tube("Inset analog dial",g,M("Dark"),V(x,y,z+.012f),r*.74f,.002f,.02f);Strut(g,"ProductCopper",V(x,y,z+.017f),V(x+r*.45f,y+r*.37f,z+.017f),.0015f);}
        public static Transform BuildArmor(Transform parent,ArmorItem item,int index)
        {
            index=Mathf.Clamp(index,0,29);int maker=index/5,slot=index%5;string m=metal[maker],a=accent[maker];var g=G("Bespoke legendary armor "+index,parent);
            switch(index)
            {
                case 0:HeadShell(g,"Precision open crown",m,.122f,.115f,.235f,.085f);Eye(g,-.044f);Eye(g,.044f);FacePlate(g,"Impact indexed temple recorder",a,.122f,.12f,0,.045f,.09f);break;
                case 1:/* Tailored base canvas remains visible. */FacePlate(g,"Kill calibrated narrow optical chest unit",a,0,.10f,.16f,.09f,.10f);for(int s=-1;s<=1;s+=2)FacePlate(g,"Separate tapered chest seam","LeatherDark",s*.12f,-.02f,.14f,.018f,.34f);break;
                case 2:Wrap(g,"Reload precision cuff","LeatherDark",.071f,.075f,-.045f,-.28f);FacePlate(g,"Ammunition exchange slide",a,0,-.16f,.085f,.09f,.14f);break;
                case 3:Wrap(g,"Light articulated shin shell","MirelineOlive",.087f,.083f,-.03f,-.31f);FacePlate(g,"Dash inertial stabilizer",a,0,-.15f,.095f,.075f,.18f);break;
                case 4:Wrap(g,"Precision boot gaiter","LeatherDark",.079f,.089f,.08f,-.073f,.025f);FacePlate(g,"Periodic clockwork heel balancer",m,0,-.025f,-.08f,.105f,.10f);Mechanism(g,a,0,.015f,-.10f,3);break;
                case 5:HeadShell(g,"Forged open face helmet",m,.13f,.125f,.25f,.05f);FacePlate(g,"Damage hydraulic forehead ram","IronwakeCopper",0,.16f,.13f,.15f,.08f);FacePlate(g,"Heavy brow","ProductSteel",0,.12f,.15f,.22f,.035f);break;
                case 6:FacePlate(g,"Hammered steel army breastplate",m,0,.01f,.16f,.20f,.29f,.018f);for(int s=-1;s<=1;s+=2)Tank(g,"IronwakeCopper",s*.13f,-.12f,.15f,.022f,.23f);FacePlate(g,"Kill pressure accumulator","ProductSteel",0,.02f,.183f,.14f,.20f);break;
                case 7:Wrap(g,"Hydraulic reload forearm","LeatherDark",.085f,.085f,-.025f,-.29f);for(int s=-1;s<=1;s+=2)Strut(g,"IronwakeCopper",V(s*.07f,-.05f,.065f),V(s*.07f,-.26f,.065f),.012f);FacePlate(g,"Ram locking wrist",m,0,-.25f,.095f,.15f,.07f);break;
                case 8:Wrap(g,"Forged hydraulic shin","MirelineOlive",.10f,.092f,-.015f,-.32f);Tank(g,"IronwakeCopper",.085f,-.29f,.04f,.018f,.25f);FacePlate(g,"Dash shock knee piston",m,0,-.025f,.10f,.17f,.12f);break;
                case 9:Wrap(g,"Foundry boot ankle","LeatherDark",.092f,.105f,.10f,-.073f,.035f);FacePlate(g,"Periodic pressure sole","IronwakeCopper",0,-.064f,.055f,.16f,.035f,.10f);Tank(g,"ProductSteel",.09f,-.05f,-.02f,.02f,.12f);break;
                case 10:HeadShell(g,"Arc insulated helmet",m,.125f,.12f,.245f,.07f);Eye(g,-.044f);Eye(g,.044f);for(int s=-1;s<=1;s+=2)Tank(g,"AmberGlass",s*.12f,.09f,0,.018f,.13f);FacePlate(g,"Damage arrestor brow",a,0,.20f,.10f,.13f,.035f);break;
                case 11:/* Insulated canvas jacket remains visible. */for(int s=-1;s<=1;s+=2)Mechanism(g,"AmberGlass",s*.12f,-.08f,.145f,3);FacePlate(g,"Kill charge distribution bus",a,0,.06f,.16f,.06f,.24f);break;
                case 12:Wrap(g,"Reload induction bracer","LeatherDark",.078f,.08f,-.04f,-.285f);for(int i=0;i<8;i++)Wrap(g,"Copper wrist winding","ProductCopper",.081f,.084f,-.075f-i*.022f,-.082f-i*.022f);Tank(g,"AmberGlass",.06f,-.22f,.075f,.014f,.12f);break;
                case 13:Wrap(g,"Dielectric dash greave","MirelineOlive",.092f,.087f,-.02f,-.32f);for(int s=-1;s<=1;s+=2)Strut(g,"VoltarcYellow",V(s*.07f,-.04f,.07f),V(s*.07f,-.29f,.07f),.008f);Mechanism(g,"AmberGlass",0,-.20f,.098f,2);break;
                case 14:Wrap(g,"Arc grounded boot","LeatherDark",.084f,.099f,.09f,-.073f,.035f);for(int i=0;i<3;i++)Strut(g,"ProductCopper",V(-.065f,-.075f,.055f+i*.018f),V(.065f,-.075f,.055f+i*.018f),.004f);break;
                case 15:HeadShell(g,"Medical sealed hood","MirelineOlive",.126f,.121f,.25f,.015f);Eye(g,-.044f);Eye(g,.044f);FacePlate(g,"Damage triggered respirator",m,0,.025f,.135f,.12f,.105f);for(int s=-1;s<=1;s+=2)Tank(g,a,s*.095f,-.015f,.10f,.025f,.09f);break;
                case 16:/* Medical canvas jacket remains visible. */FacePlate(g,"Sterile sternum tray",m,0,.025f,.16f,.16f,.22f);Tank(g,a,-.155f,-.15f,.145f,.026f,.19f);Line("Kill recovery hose",g,M("LeatherDark"),.009f,V(-.155f,.04f,.145f),V(-.09f,.16f,.16f),V(.05f,.02f,.18f));break;
                case 17:Wrap(g,"Sterile reload glove bracer","Cloth",.075f,.078f,-.035f,-.29f);Mechanism(g,a,0,-.20f,.088f,3);FacePlate(g,"Injector plunger bar","ProductSteel",0,-.11f,.105f,.11f,.025f);break;
                case 18:Wrap(g,"Medical dash compression shin","MirelineOlive",.094f,.089f,-.015f,-.32f);Tank(g,a,.075f,-.28f,.04f,.017f,.22f);FacePlate(g,"Sterile shin brace",m,0,-.14f,.101f,.11f,.22f);break;
                case 19:Wrap(g,"Hazmat sealed overshoe","MirelineOlive",.088f,.105f,.10f,-.073f,.035f);FacePlate(g,"Periodic fluid pump heel",m,0,-.02f,-.085f,.11f,.11f);Tank(g,a,.075f,-.04f,.015f,.016f,.10f);break;
                case 20:Eye(g,-.046f,"Dark");Eye(g,.046f,"Dark");FacePlate(g,"Full face respirator rubber","LeatherDark",0,.022f,.11f,.13f,.11f,.045f);for(int s=-1;s<=1;s+=2){Tube("Round respirator filter",g,M("ProductSteel"),V(s*.092f,.025f,.095f),.035f,.075f,.08f);for(int i=0;i<3;i++)Tube("Filter rim",g,M("ProductCopper"),V(s*.092f,.025f,.11f+i*.018f),.037f,.005f,.85f);}Line("Goggle strap",g,M("LeatherDark"),.012f,V(-.09f,.12f,.10f),V(-.12f,.12f,-.07f),V(.12f,.12f,-.07f),V(.09f,.12f,.10f));break;
                case 21:/* Existing tailored base jacket supplies the continuous torn cloth. */for(int s=-1;s<=1;s+=2){FacePlate(g,"Leather field pouch","LeatherDark",s*.15f,-.13f,.14f,.09f,.12f,.045f);FacePlate(g,"Pouch flap","Leather",s*.15f,-.08f,.165f,.095f,.035f);}FacePlate(g,"Jacket asymmetrical rust chest patch","ProductSteel",-.12f,.09f,.135f,.10f,.13f);break;
                case 22:Wrap(g,"Scavenger leather bracer","LeatherDark",.078f,.079f,-.04f,-.29f);for(int i=0;i<3;i++)FacePlate(g,"Rusted bracer strip","ProductSteel",0,-.095f-i*.06f,.09f,.12f,.038f);FacePlate(g,"Blast ammo pressure dial","ProductCopper",.04f,-.13f,.10f,.04f,.04f);break;
                case 23:Wrap(g,"Olive field trouser shin","MirelineOlive",.096f,.09f,-.01f,-.32f);FacePlate(g,"Rusted faceted knee","ProductSteel",0,-.035f,.102f,.17f,.14f);FacePlate(g,"Leather knee retaining strap","LeatherDark",0,-.12f,.105f,.17f,.027f);break;
                case 24:Wrap(g,"Worn lace boot upper","LeatherDark",.086f,.102f,.10f,-.073f,.038f);for(int i=0;i<6;i++){float y=-.04f+i*.019f;Strut(g,"Leather",V(-.045f,y,.134f),V(.045f,y+.014f,.134f),.003f);Strut(g,"Leather",V(.045f,y,.135f),V(-.045f,y+.014f,.135f),.003f);}FacePlate(g,"Rusted ankle buckle","ProductSteel",.077f,.035f,.02f,.035f,.035f);break;
                case 25:HeadShell(g,"Security ballistic open visor helmet",m,.132f,.122f,.25f,.055f);FacePlate(g,"Transparent reaction visor","TealGlass",0,.12f,.145f,.20f,.105f,.012f);FacePlate(g,"Damage counterfire brow",a,0,.205f,.115f,.18f,.035f);break;
                case 26:/* Security jacket with discrete front laminate. */FacePlate(g,"Kill reaction shield battery",a,0,.02f,.168f,.20f,.26f);for(int s=-1;s<=1;s+=2)FacePlate(g,"Ballistic side laminate",m,s*.19f,0,.105f,.075f,.20f);break;
                case 27:Wrap(g,"Counterfire reload vambrace","LeatherDark",.085f,.085f,-.035f,-.29f);FacePlate(g,"Asymmetrical forearm shield",m,-.025f,-.14f,.10f,.21f,.24f,.035f);FacePlate(g,"White security inset",a,-.025f,-.11f,.122f,.14f,.035f);break;
                case 28:Wrap(g,"Ballistic dash greave","ClothDark",.10f,.092f,-.015f,-.32f);FacePlate(g,"Overlapping shield knee",a,0,-.025f,.108f,.16f,.12f);FacePlate(g,"Shin reaction laminate",m,0,-.20f,.105f,.13f,.20f);break;
                case 29:Wrap(g,"Security reinforced boot","LeatherDark",.092f,.105f,.10f,-.073f,.035f);FacePlate(g,"Heel battery",m,0,.005f,-.085f,.13f,.12f);Mechanism(g,"AmberGlass",0,.015f,-.11f,2);break;
            }
            if(slot==0){
                if(maker!=4)Crown(g,maker==3?"MirelineOlive":m);
                Wrap(g,"Folded lower neck field scarf","Cloth",.094f,.092f,-.025f,-.085f,-.004f);
                if(maker==0){FacePlate(g,"Perforated steel lower face mask",m,0,.022f,.131f,.13f,.095f,.016f);for(int row=0;row<3;row++)for(int col=-2;col<=2;col++)Tube("Mask breathing perforation",g,M("Dark"),V(col*.018f,.0f+row*.019f,.143f),.004f,.002f,.02f,-1,8);Dial(g,-.115f,.09f,.005f,.031f);}
                if(maker==1){FacePlate(g,"Heavy army dark eye opening","Dark",0,.10f,.13f,.19f,.065f,.008f);FacePlate(g,"Hydraulic breathing mask","LeatherDark",0,.025f,.14f,.14f,.10f,.022f);for(int s=-1;s<=1;s+=2)Tube("Army dual filter",g,M("ProductSteel"),V(s*.065f,.01f,.15f),.03f,.035f,.12f);}
                if(maker==2){FacePlate(g,"Arc insulated breathing mask","LeatherDark",0,.025f,.127f,.12f,.10f,.02f);Tube("Central round mask filter",g,M("ProductSteel"),V(0,.025f,.15f),.036f,.045f,.12f);Tank(g,"AmberGlass",.12f,.08f,-.005f,.016f,.105f);}
                if(maker==3)for(int s=-1;s<=1;s+=2)Tube("Medical red filter end",g,M("BloodGlass"),V(s*.085f,.018f,.12f),.03f,.035f,.04f);
                if(maker==5){FacePlate(g,"Security lower face respirator","LeatherDark",0,.02f,.13f,.14f,.09f,.022f);for(int i=-2;i<=2;i++)Strut(g,"ProductSteel",V(i*.018f,-.005f,.148f),V(i*.018f,.045f,.148f),.0025f);}
            }
            if(slot==1){
                for(int s=-1;s<=1;s+=2){Line("Crossed tailored leather load harness",g,M("LeatherDark"),.009f,V(s*.16f,.18f,.10f),V(s*.04f,.03f,.164f),V(-s*.13f,-.20f,.13f));if(maker!=4)FieldPouch(g,s*.145f,-.185f,.135f,maker==5?"ClothDark":"LeatherDark");}
                // Padded standing scarf leaves the face visible and hides the long bare neck.
                Wrap(g,"Folded standing field collar","Cloth",.105f,.10f,.263f,.17f,-.008f);
                if(maker==0){Tank(g,"AmberGlass",.145f,.02f,.13f,.016f,.12f);var shoulderOptic=G("Shoulder mounted range optic",g);shoulderOptic.localPosition=V(.17f,.13f,.06f);Scope(shoulderOptic,0,.07f);}
                if(maker==1)Dial(g,.095f,.09f,.185f);
                if(maker==2){Tank(g,"AmberGlass",-.12f,.05f,.15f,.017f,.10f);Line("Chest insulated return lead",g,M("ProductCopper"),.005f,V(-.12f,.15f,.15f),V(-.17f,.17f,.11f),V(-.20f,.06f,.08f));}
                if(maker==3)Tank(g,"BloodGlass",.12f,-.16f,.15f,.025f,.16f);
                if(maker==4){Tank(g,"AmberGlass",.12f,.035f,.14f,.015f,.10f);FieldPouch(g,.035f,-.21f,.155f);}
                if(maker==5){Tank(g,"AmberGlass",-.12f,-.18f,.15f,.015f,.08f);Tank(g,"AmberGlass",-.08f,-.18f,.15f,.015f,.08f);}
            }
            if(slot==2){
                if(maker==0)Dial(g,-.055f,-.16f,.065f);
                if(maker==1)Tank(g,"AmberGlass",.05f,-.20f,.075f,.017f,.13f);
                if(maker==2||maker==4)Tank(g,"AmberGlass",.05f,-.22f,.075f,.014f,.12f);
                if(maker==3)Tank(g,"BloodGlass",.055f,-.22f,.07f,.014f,.13f);
                if(maker==5)Tank(g,"AmberGlass",.07f,-.22f,.07f,.014f,.12f);
            }
            if(slot==3){if(maker==0)Dial(g,0,-.025f,.11f,.045f);if(maker==2){for(int i=0;i<4;i++)Wrap(g,"Knee copper induction winding","ProductCopper",.088f,.095f,-.015f-i*.012f,-.021f-i*.012f);}}
            if(slot==4){if(maker==0||maker==5)Dial(g,.075f,-.01f,.045f);if(maker==1||maker==2||maker==4)Tank(g,"AmberGlass",.075f,.015f,.015f,.014f,.10f);if(maker==3)Tank(g,"BloodGlass",.075f,.015f,.015f,.014f,.10f);}
            if(slot==2)g.localScale=V(1,.85f,1);
            Compact(g);return g;
        }
        public static List<Transform> ApplyArmorSupplement(DetailedHero3D body,ArmorItem item,int index)
        {
            int maker=Mathf.Clamp(index/5,0,5),slot=index%5;var roots=new List<Transform>();
            if(slot==1){
                int side=0;foreach(var bone in new[]{body.leftUpperArm,body.rightUpperArm}){var shoulder=G("Independent articulated shoulder",bone);roots.Add(shoulder);Wrap(shoulder,"Shaped shoulder cover",maker==4?(side==0?"ProductSteel":"MirelineOlive"):metal[maker],.112f,.111f,.025f,-.13f);if(maker==4&&side==0)for(int j=0;j<3;j++)FacePlate(shoulder,"Rusted shoulder rivet","ProductCopper",-.06f+j*.06f,-.09f,.107f,.012f,.012f);Compact(shoulder);side++;}
            }
            if(slot==3)foreach(var bone in new[]{body.leftThigh,body.rightThigh}){var thigh=G("Separate thigh garment",bone);roots.Add(thigh);Wrap(thigh,"Fitted thigh",maker==4?"MirelineOlive":"LeatherDark",.118f,.12f,-.02f,-.31f);if(maker!=4)FacePlate(thigh,"Thigh manufacturer protection",metal[maker],0,-.13f,.13f,.16f,.19f);else FacePlate(thigh,"Field trouser side pouch","LeatherDark",.085f,-.15f,.09f,.075f,.12f,.035f);Compact(thigh);}
            if(slot==2)foreach(var bone in new[]{body.leftHand,body.rightHand}){
                var hand=G("Independent fitted legendary glove",bone);roots.Add(hand);
                Wrap(hand,"Waxed glove palm backing","LeatherDark",.044f,.031f,.005f,-.10f);
                // The fitted glove follows the hand, never the forearm; fingertips
                // remain articulated fingerless leather as in the menu reference.
                for(int i=0;i<4;i++)FacePlate(hand,"Individual raised knuckle",maker==4?"Leather":metal[maker],(i-1.5f)*.018f,-.087f,.036f,.014f,.023f,.006f);
                switch(maker){case 0:FacePlate(hand,"Precision reload index","KestrelTeal",0,-.045f,.036f,.04f,.025f,.007f);break;case 1:FacePlate(hand,"Hydraulic glove back","IronwakeCopper",0,-.045f,.039f,.06f,.04f,.013f);break;case 2:Strut(hand,"ProductCopper",V(-.03f,-.025f,.039f),V(.03f,-.07f,.039f),.003f);break;case 3:FacePlate(hand,"Sterile injector glove","MirelineBone",0,-.045f,.037f,.04f,.045f,.006f);break;case 4:FacePlate(hand,"Scavenged glove wrist buckle","ProductSteel",.025f,-.012f,.035f,.02f,.02f,.006f);break;case 5:FacePlate(hand,"Security glove laminate","AegisBlue",0,-.045f,.04f,.06f,.05f,.012f);break;}
                Compact(hand);
            }
            if(slot==4)foreach(var bone in new[]{body.leftToe,body.rightToe})if(bone){
                var toe=G("Articulated legendary toe overlay",bone);roots.Add(toe);
                // Toe pivot is (0,-.089,.12) relative to the foot. Keep all
                // added vertices above ground and forward of that ball hinge.
                var top=G("Separate toe cap",toe);top.localPosition=V(0,.105f,.045f);top.localRotation=Quaternion.Euler(-90,0,0);
                FacePlate(top,"Fitted boot toe protection",maker==4?"Leather":metal[maker],0,0,0,.142f,.07f,.012f);
                if(maker==1||maker==5)FacePlate(top,"Reinforced toe inset",accent[maker],0,0,.009f,.10f,.025f,.005f);
                if(maker==2)for(int i=0;i<3;i++)Strut(top,"ProductCopper",V(-.055f,-.022f+i*.022f,.01f),V(.055f,-.022f+i*.022f,.01f),.002f);
                Compact(toe);
            }
            return roots;
        }
    }
}









