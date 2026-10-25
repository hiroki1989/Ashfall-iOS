using UnityEngine;
using static NeonVault.DetailedHeroArsenal3D;

namespace NeonVault
{
    // Complete firearm architectures. +Z is the bore; sockets stay in this root.
    public static class GunSeriesModels3D
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Vector2 P(float z,float y)=>new Vector2(z,y);
        static Material M(string key)=>DetailedHeroMaterials3D.Get(key);
        static Transform Root(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static void Slab(Transform g,string n,string m,float w,float z,float y,float l,float h)
        {Profile(n,g,M(m),w,P(z-l/2,y-h/2),P(z-l/2,y+h*.3f),P(z-l*.4f,y+h/2),P(z+l*.4f,y+h/2),P(z+l/2,y+h*.25f),P(z+l/2,y-h/2));}
        static string Furniture(int maker)=>maker==0||maker==4?"Wood":maker==3?"Cloth":"LeatherDark";
        static void Grip(Transform g,int maker,float z,bool large=false)
        {
            Profile("Angled textured pistol grip",g,M(Furniture(maker)),large?.059f:.048f,P(z-.036f,-.045f),P(z+.025f,-.05f),P(z+.005f,-.183f),P(z-.059f,-.173f));
            for(int i=0;i<5;i++)Rod("Grip checkering",g,M("Dark"),V(-.026f,-.085f-i*.016f,z-.028f),V(.026f,-.085f-i*.016f,z-.028f),.002f);
            Line("Open trigger guard",g,M("ProductSteel"),.005f,V(0,-.05f,z+.025f),V(0,-.12f,z+.038f),V(0,-.12f,z+.091f),V(0,-.049f,z+.10f));
            Rod("Curved trigger",g,M("ProductSteel"),V(0,-.053f,z+.061f),V(0,-.092f,z+.052f),.004f);
        }
        static void Stock(Transform g,int maker,float rear,bool skeleton=false)
        {
            if(skeleton){for(int sign=-1;sign<=1;sign+=2)Rod("Open stock brace",g,M("ProductSteel"),V(sign*.024f,.01f,-.11f),V(sign*.033f,-.025f,rear),.008f);}
            else Profile("Shoulder stock and raised cheek rest",g,M(Furniture(maker)),.068f,P(rear,-.106f),P(rear,.034f),P(rear+.075f,.05f),P(-.10f,-.005f),P(-.11f,-.055f),P(rear+.09f,-.071f));
            Slab(g,"Rubber shoulder pad","Dark",.075f,rear-.008f,-.033f,.018f,.15f);
        }
        static void Magazine(Transform g,float z,float depth=.18f,float width=.055f)
        {
            Profile("Detachable curved box magazine",g,M("ProductSteel"),width,P(z-.032f,-.039f),P(z+.033f,-.039f),P(z+.054f,-depth),P(z-.015f,-depth-.018f));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Rod("Stamped magazine channel",g,M("Dark"),V(side*(width*.5f+.001f),-.075f,z-.018f+i*.018f),V(side*(width*.5f+.001f),-depth+.025f,z+i*.018f),.002f);
        }
        static void Drum(Transform g,float z,float radius=.085f)
        {
            var t=Root("Transverse round ammunition drum",g);t.localPosition=V(-.052f,-.12f,z);t.localRotation=Quaternion.Euler(0,90,0);
            Tube("Pressed steel drum",t,M("ProductSteel"),Vector3.zero,radius,.104f,.03f,-1,24);
            for(int end=0;end<2;end++){float p=end*.104f;Tube("Drum rim",t,M("Dark"),V(0,0,p),radius+.004f,.008f,.88f,-1,24);Tube("Drum spindle",t,M("ProductSteel"),V(0,0,p),.018f,.014f,.05f,-1,12);}
        }
        static void Fore(Transform g,int maker,float a,float b,bool pump=false)
        {
            Slab(g,pump?"Sliding pump fore-end":"Contoured supporting fore-end",Furniture(maker),.076f,(a+b)/2,-.028f,b-a,.078f);
            for(int i=0;i<7;i++){float z=Mathf.Lerp(a+.012f,b-.012f,i/6f);for(int s=-1;s<=1;s+=2)Rod(pump?"Raised pump rib":"Ventilated fore-end slot",g,M("Dark"),V(s*.039f,-.055f,z),V(s*.039f,-.008f,z),.0025f);}
        }
        static void Barrel(Transform g,float start,float end,float radius=.018f,float x=0)
        {Tube("Steel barrel with open bore",g,M("ProductSteel"),V(x,0,start),radius,end-start,.65f,-1,24);Tube("Thick muzzle crown",g,M("ProductSteel"),V(x,0,end-.012f),radius*1.20f,.012f,.70f,-1,24);}
        static void Receiver(Transform g,float rear,float front,float width,float height=.09f)
        {
            Slab(g,"Machined stepped receiver","ProductSteel",width,(rear+front)/2,.008f,front-rear,height);
            var port=Root("Right recessed ejection port",g);port.localPosition=V(width/2+.001f,.025f,.005f);
            Slab(port,"Open dark ejection port","Dark",.003f,.015f,0,.075f,.022f);
            Rod("Exposed charging handle",g,M("ProductSteel"),V(width/2,.027f,.025f),V(width*.90f,.027f,.025f),.006f);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Rod("Receiver cross pin",g,M("Brass"),V(side*width*.48f,-.018f,rear+.025f+i*(front-rear-.05f)/2),V(side*width*.53f,-.018f,rear+.025f+i*(front-rear-.05f)/2),.003f);
        }
        static void Rail(Transform g,float a,float b,float y=.071f)
        {Slab(g,"Raised sight rail","ProductSteel",.031f,(a+b)/2,y,b-a,.016f);for(int i=0;i<8;i++)Rod("Rail transverse tooth",g,M("Dark"),V(-.019f,y+.012f,Mathf.Lerp(a,b,i/7f)),V(.019f,y+.012f,Mathf.Lerp(a,b,i/7f)),.003f);}
        static void Scope(Transform g,float z,float length=.22f)
        {
            for(int i=0;i<2;i++)Slab(g,"Scope ring foot","ProductSteel",.024f,z+i*.11f,.09f,.025f,.06f);
            Tube("Optical sight body",g,M("ProductSteel"),V(0,.133f,z-.025f),.027f,length,.05f,-1,24);
            Tube("Objective bell",g,M("ProductSteel"),V(0,.133f,z+length-.055f),.040f,.05f,.81f,-1,24);
            Tube("Recessed objective glass",g,M("TealGlass"),V(0,.133f,z+length-.009f),.030f,.002f,.05f,-1,24);
        }
        static void Bipod(Transform g,float z){for(int s=-1;s<=1;s+=2)Rod("Deployed supporting bipod",g,M("ProductSteel"),V(s*.025f,-.025f,z),V(s*.12f,-.23f,z+.07f),.007f);}
        static void Maker(Transform g,EquipmentRecipe r,float z,float width,bool pistol)
        {
            float side=width*.5f+.016f;
            switch(r.maker)
            {
                case 0:
                    Slab(g,"Walnut fitted action cover","Wood",width+.005f,z,-.027f,.11f,.037f);
                    var dial=Root("Precision windage dial",g);dial.localPosition=V(side,.04f,z);dial.localRotation=Quaternion.Euler(0,90,0);Tube("Knurled mechanical adjustment wheel",dial,M("ProductSteel"),Vector3.zero,.019f,.014f,.1f,-1,16);
                    break;
                case 1:
                    for(int i=0;i<2;i++){Slab(g,"Forged reinforcement strap","IronwakeSteel",width+.022f,z-.04f+i*.085f,.01f,.026f,.125f);Rod("Hydraulic recoil piston",g,M("ProductSteel"),V(-side,-.018f,z-.07f),V(-side,-.018f,z+.08f),.013f);}
                    for(int i=0;i<3;i++)Rod("Large industrial rivet",g,M("IronwakeCopper"),V(side,.025f,z-.04f+i*.04f),V(side+.012f,.025f,z-.04f+i*.04f),.007f);
                    break;
                case 2:
                    Slab(g,"Serviceable valve power pack","Dark",.030f,z,.003f,.13f,.075f);
                    for(int i=0;i<(pistol?1:2);i++){var valve=Root("Protected upright vacuum tube",g);valve.localPosition=V(-side,.028f,z-.025f+i*.055f);valve.localRotation=Quaternion.Euler(-90,0,0);Tube("Glass valve",valve,M("AmberGlass"),Vector3.zero,.014f,.067f,.06f,-1,16);Tube("Ceramic valve socket",valve,M("AegisWhite"),Vector3.zero,.018f,.018f,.1f,-1,12);}
                    Line("Insulated pack cable",g,M("LeatherDark"),.004f,V(-side,.03f,z-.04f),V(-side-.02f,-.035f,z),V(0,-.038f,z+.09f));break;
                case 3:
                    var tank=Root("Medical glass return reservoir",g);tank.localPosition=V(-side,-.018f,z-.045f);Tube("Protected fluid reservoir",tank,M("MirelineFluid"),Vector3.zero,.020f,.10f,.04f,-1,16);
                    for(int i=0;i<2;i++)Tube("Reservoir retaining collar",tank,M("ProductSteel"),V(0,0,i*.084f),.024f,.014f,.80f,-1,16);
                    Line("Flexible reservoir return hose",g,M("Leather"),.005f,V(-side,-.018f,z+.055f),V(-side-.03f,-.065f,z+.08f),V(-side,-.035f,z+.115f));break;
                case 4:
                    Slab(g,"Reclaimed timber action splint","Wood",width+.015f,z,-.024f,.135f,.048f);
                    for(int i=0;i<2;i++){Slab(g,"Hand welded patch band","ProductSteel",width+.024f,z-.04f+i*.08f,-.01f,.018f,.10f);Rod("Raised repair weld",g,M("CinderOrange"),V(-side,.046f,z-.04f+i*.08f),V(side,.046f,z-.033f+i*.08f),.003f);}
                    Rod("Brass pressure return pipe",g,M("Brass"),V(side,.018f,z-.07f),V(side,.018f,z+.10f),.006f);break;
                case 5:
                    Slab(g,"Convoy ceramic receiver shield","AegisWhite",width+.015f,z,.014f,.10f,.064f);
                    for(int i=0;i<2;i++)Slab(g,"Shield rubber standoff","Dark",width+.022f,z-.045f+i*.09f,.005f,.014f,.085f);
                    Line("Navy canvas retention strap",g,M("NavyCanvas"),.006f,V(-side,-.02f,z-.04f),V(-side,-.06f,z+.015f),V(side,-.06f,z+.015f),V(side,-.02f,z-.04f));break;
            }
        }
        // The eight later lineages have distinct working assemblies, not scaled copies of
        // the first four. Their sockets remain on the original receiver coordinate system.
        static void ExpandedSeries(Transform g,int gunClass,int series,float muzzle,float width,int maker)
        {
            bool pistol=gunClass==1;
            float front=pistol?Mathf.Min(muzzle-.025f,.27f):Mathf.Min(muzzle-.04f,.54f);
            float flank=width*.5f+.012f;
            switch(series)
            {
                case 4: // Exposed reciprocating action and low feed cradle.
                    Slab(g,"Open bolt guide bridge","ProductSteel",width+.019f,.03f,.095f,.27f,.017f);
                    for(int side=-1;side<=1;side+=2)Rod("Exposed recoil spring guide",g,M("ProductSteel"),V(side*flank,.082f,-.105f),V(side*flank,.082f,front),.006f);
                    Magazine(g,pistol?.015f:.095f,pistol?.13f:.21f,.063f);
                    break;
                case 5: // Tandem chamber, visibly split at the muzzle.
                    for(int side=-1;side<=1;side+=2){Barrel(g,.15f,muzzle-.015f,pistol?.014f:.021f,side*.035f);Slab(g,"Separate chamber breech","ProductSteel",.035f,.09f,.016f,.15f,.075f);}
                    Slab(g,"Twin chamber locking yoke","Brass",width+.045f,.17f,.063f,.025f,.08f);
                    break;
                case 6: // Large upper rotary feed and side reduction gears.
                    {var pan=Root("Rotary upper ammunition cassette",g);pan.localPosition=V(0,.105f,.035f);pan.localRotation=Quaternion.Euler(-90,0,0);Tube("Indexed feed pan",pan,M("ProductSteel"),Vector3.zero,pistol?.071f:.115f,.035f,.08f,-1,28);for(int i=0;i<10;i++){float a=i*Mathf.PI/5;Rod("Pan radial rib",pan,M("Brass"),V(0,0,.038f),V(Mathf.Cos(a)*.09f,Mathf.Sin(a)*.09f,.038f),.003f);}}
                    for(int side=-1;side<=1;side+=2){var gear=Root("External feed reduction gear",g);gear.localPosition=V(side*flank,.025f,-.025f);gear.localRotation=Quaternion.Euler(0,90,0);Tube("Gear wheel",gear,M("Brass"),Vector3.zero,.035f,.012f,.08f,-1,16);}
                    break;
                case 7: // Under-barrel pressure cylinder with articulated links.
                    Tube("Long pressure cylinder",g,M("ProductSteel"),V(0,-.075f,.14f),.031f,front-.08f,.07f,-1,24);
                    for(int side=-1;side<=1;side+=2){Rod("Pressure linkage",g,M("Brass"),V(side*.05f,-.035f,.05f),V(side*.05f,-.055f,front-.04f),.008f);Rod("Actuator cross pin",g,M("ProductSteel"),V(side*.05f,-.055f,front-.04f),V(side*.07f,-.055f,front-.04f),.009f);}
                    break;
                case 8: // Deep longitudinal side cassettes.
                    for(int side=-1;side<=1;side+=2){Slab(g,"Removable longitudinal feed cassette",maker==0||maker==4?"Wood":"ProductSteel",.040f,.045f,-.015f,.29f,.13f);var cassette=Root("Side cassette mount",g);cassette.localPosition=V(side*(flank+.026f),-.01f,.035f);Slab(cassette,"Side cassette outer wall","Dark",.023f,0,0,.28f,.105f);for(int i=0;i<4;i++)Rod("Cassette loading notch",cassette,M("Brass"),V(-.013f,.04f,-.105f+i*.065f),V(.013f,.04f,-.105f+i*.065f),.003f);}
                    break;
                case 9: // Jacketed cooling barrel and broad ventilated shroud.
                    for(int i=0;i<7;i++)Tube("Separate barrel cooling ring",g,M("ProductSteel"),V(0,0,.20f+i*(front-.20f)/7),pistol?.034f:.049f,.012f,.78f,-1,20);
                    for(int side=-1;side<=1;side+=2)Rod("Open cooling jacket rail",g,M("ProductSteel"),V(side*(pistol?.035f:.05f),.025f,.18f),V(side*(pistol?.035f:.05f),.025f,front),.006f);
                    Slab(g,"Tall heat shield","Dark",width+.01f,.32f,.075f,Mathf.Max(.09f,front-.22f),.018f);
                    break;
                case 10: // Stepped counterweight and mechanical elevation arm.
                    Slab(g,"Offset balance weight","ProductSteel",width+.055f,-.105f,-.065f,.13f,.12f);
                    for(int side=-1;side<=1;side+=2)Rod("Triangulated elevation arm",g,M("Brass"),V(side*flank,.04f,-.13f),V(side*(flank+.024f),.13f,.14f),.007f);
                    Slab(g,"Raised range ladder","ProductSteel",.026f,.11f,.155f,.13f,.10f);
                    break;
                case 11: // Forward fork, two stabilizers, exposed central bore.
                    for(int side=-1;side<=1;side+=2){Slab(g,"Forked forward action tine","ProductSteel",.026f,.28f,side*.028f,Mathf.Max(.13f,front-.16f),.06f);Rod("Fork tension cable",g,M("Brass"),V(side*.058f,.052f,.10f),V(side*.065f,.052f,front),.006f);}
                    Tube("Floating muzzle sleeve",g,M("ProductSteel"),V(0,0,muzzle-.075f),pistol?.038f:.053f,.075f,.66f,-1,20);
                    break;
            }
            // Class-specific visible mechanics keep rifles, pistols and support guns apart.
            if(gunClass==1){Slab(g,"Compact slide return cap","Dark",width+.025f,-.09f,.063f,.035f,.035f);}
            else if(gunClass==2){Rod("Shotgun shell lifter",g,M("Brass"),V(-flank,-.08f,.055f),V(flank,-.08f,.055f),.008f);}
            else if(gunClass==3){Bipod(g,Mathf.Min(front,.48f));}
            else if(gunClass==4){Slab(g,"Compact charging shroud","Dark",width+.015f,-.04f,.075f,.17f,.025f);}
            else if(gunClass==5){Scope(g,-.10f,.27f);}
            else Rail(g,-.08f,.18f,.093f);
        }
        static void EditionHardware(Transform g,int edition,int gunClass,float muzzle,float width)
        {
            if(edition==0){Slab(g,"Field service latch","Brass",.018f,-.095f,.048f,.028f,.026f);return;}
            if(edition==1){Slab(g,"Short edition folding rear sight","ProductSteel",.032f,-.078f,.097f,.023f,.047f);Rod("Short edition sling swivel",g,M("ProductSteel"),V(-width*.6f,-.045f,-.15f),V(-width*.85f,-.080f,-.15f),.005f);return;}
            if(edition==2){Slab(g,"Reinforced receiver bridge","ProductSteel",width+.02f,-.02f,.101f,.19f,.025f);for(int side=-1;side<=1;side+=2)Rod("Reinforced cross brace",g,M("Brass"),V(side*width*.65f,-.06f,-.105f),V(side*width*.65f,.06f,.12f),.006f);return;}
            if(edition==3){Slab(g,"Range cheek riser","Dark",width+.015f,-.17f,.066f,.11f,.048f);if(gunClass!=1)Bipod(g,Mathf.Min(.48f,muzzle-.12f));}
        }
        public static DetailedHeroEquipment3D.WeaponSockets Build(Transform parent,Gun gun,EquipmentRecipe r)
        {
            if(GunSilhouette3D.TryBuildRegular(parent,r,out var painted))return painted;
            int c=Mathf.Clamp(r.gunClass,0,5),series=Mathf.Clamp(r.series,0,11),s=series%4;
            var g=Root("Distinct product / "+r.id+" / class "+c+" series "+s,parent);
            float rear=-.34f,end=.64f,support=.26f,grip=-.045f,width=.078f,makerZ=-.035f;
            if(c==1)
            {
                rear=-.10f;end=s==0?.23f:s==1?.31f:s==2?.37f:.42f;support=-.025f;width=s==3?.084f:.064f;
                bool revolver=s==2||(s==3&&(r.maker==0||r.maker==1||r.maker==4));
                if(revolver){Slab(g,"Open revolver top strap","ProductSteel",width,-.01f,.071f,.23f,.022f);Slab(g,"Revolver lower frame","ProductSteel",width,-.015f,-.056f,.19f,.022f);Slab(g,"Revolver rear recoil shield","ProductSteel",width,-.048f,.008f,.026f,.13f);Slab(g,"Revolver forward frame bridge","ProductSteel",width,.075f,.008f,.025f,.13f);}else Receiver(g,-.11f,end-.055f,width,s==3?.12f:s==0?.065f:.08f);Grip(g,r.maker,grip,s==3);
                if(revolver){var cylinder=Root("Revolver rotating six chamber cylinder",g);cylinder.localPosition=V(0,.002f,-.025f);Tube("Revolver fluted cylinder",cylinder,M("ProductSteel"),Vector3.zero,.058f,.09f,.12f,-1,24);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Tube("Separate chamber mouth",cylinder,M("Dark"),V(Mathf.Cos(a)*.039f,Mathf.Sin(a)*.039f,.09f),.010f,.003f,.15f,-1,10);Tube("Visible cylinder longitudinal flute",cylinder,M("Dark"),V(Mathf.Cos(a)*.055f,Mathf.Sin(a)*.055f,.005f),.009f,.08f,.07f,-1,10);}Barrel(g,.065f,end,.024f);Rod("Exposed hammer",g,M("ProductSteel"),V(0,.035f,-.09f),V(0,.077f,-.11f),.009f);}
                else {Barrel(g,.05f,end,s==3?.031f:.015f);Slab(g,"Slide rear serration block","Dark",width+.002f,-.075f,.035f,.045f,.025f);Slab(g,"Flush grip magazine base","ProductSteel",.055f,grip-.025f,-.18f,.067f,.016f);}
                if(s==0){Profile("Compact grip shortened heel",g,M("Dark"),.053f,P(grip-.054f,-.15f),P(grip+.005f,-.15f),P(grip+.005f,-.17f),P(grip-.054f,-.17f));}
                if(s==3){Tube("Compensator block",g,M("ProductSteel"),V(0,0,end-.06f),.043f,.06f,.76f,-1,12);for(int i=0;i<3;i++)Rod("Compensator gas vent",g,M("Dark"),V(-.025f,.04f,end-.05f+i*.015f),V(.025f,.04f,end-.05f+i*.015f),.003f);}
            }
            else
            {
                bool bull=(c==0&&s==1)||(c==5&&s==2);grip=bull?.065f:-.045f;rear=bull?-.25f:c==4?-.26f:-.36f;
                width=c==3?.105f:c==2?.087f:.078f;
                Receiver(g,bull?-.22f:-.13f,.17f,width,c==3?.13f:.095f);if(!(c==2&&s==1))Grip(g,r.maker,grip);else{Profile("Break action rounded walnut wrist",g,M(Furniture(r.maker)),.065f,P(-.35f,-.10f),P(-.34f,.025f),P(-.12f,.02f),P(-.02f,-.04f),P(-.075f,-.095f),P(-.17f,-.055f));Line("Break action trigger bow",g,M("ProductSteel"),.004f,V(0,-.055f,-.08f),V(0,-.12f,-.065f),V(0,-.12f,.02f),V(0,-.05f,.04f));}Stock(g,r.maker,rear,(c==0&&s==0)||(c==4&&s==0)||(c==5&&s==1));
                switch(c)
                {
                    case 0:
                        end=s==1?.55f:s==3?.79f:s==2?.71f:.64f;Magazine(g,bull?-.14f:.075f,s==2?.20f:.17f);Fore(g,r.maker,.19f,end-.13f);Barrel(g,.16f,end,s==2?.023f:.019f);Rail(g,-.09f,.16f);
                        if(s==3){Scope(g,-.07f);Bipod(g,end-.17f);}break;
                    case 2:
                        end=s==2?.46f:s==1?.64f:.73f;support=s==2?.21f:.34f;
                        if(s==1){for(int sign=-1;sign<=1;sign+=2)Barrel(g,.12f,end,.030f,sign*.031f);Fore(g,r.maker,.20f,.40f);Rod("Break action hinge cross pin",g,M("Brass"),V(-.06f,-.025f,.13f),V(.06f,-.025f,.13f),.012f);Rod("Breech opening lever",g,M("ProductSteel"),V(0,.065f,-.015f),V(.06f,.065f,.04f),.007f);}
                        else {Barrel(g,.14f,end,.029f);if(s!=3)Tube("Under barrel tubular magazine",g,M("ProductSteel"),V(0,-.066f,.12f),.022f,end-.17f,.1f,-1,20);Fore(g,r.maker,.22f,s==2?.34f:.46f,s==0);if(s==0)for(int sign=-1;sign<=1;sign+=2)Rod("Pump operating action bar",g,M("ProductSteel"),V(sign*.03f,-.055f,.10f),V(sign*.03f,-.055f,.38f),.004f);if(s==2)Drum(g,.075f,.095f);if(s==3){Magazine(g,.075f,.17f);Rail(g,-.10f,.17f);}}
                        break;
                    case 3:
                        end=s==3?.87f:.77f;Fore(g,r.maker,.22f,.45f);Barrel(g,.15f,end,s==3?.033f:.025f);Bipod(g,.58f);Rail(g,-.1f,.17f);
                        if(s==0||s==3){var box=Root("Side hung belt box",g);box.localPosition=V(-.10f,-.09f,.045f);Slab(box,"Rectangular linked ammunition box","Cloth",.13f,0,0,.18f,.18f);for(int i=0;i<7;i++){float x=-.16f+i*.023f;Rod("Exposed brass ammunition belt link",g,M("Brass"),V(x,.005f,.035f),V(x,.005f,.095f),.007f);}Slab(g,"Hinged belt feed cover","ProductSteel",.115f,.015f,.085f,.21f,.027f);}
                        else if(s==1){var disc=Root("Horizontal pan feed disc",g);disc.localPosition=V(0,.10f,.025f);disc.localRotation=Quaternion.Euler(-90,0,0);Tube("Top mounted circular pan magazine",disc,M("ProductSteel"),Vector3.zero,.145f,.036f,.04f,-1,32);}
                        else Drum(g,.06f,.12f);
                        if(s==3)for(int i=0;i<10;i++)Tube("Heavy radial cooling fin",g,M("ProductSteel"),V(0,0,.40f+i*.032f),.044f,.009f,.69f,-1,16);
                        break;
                    case 4:
                        end=s==0?.40f:.48f;support=.20f;Fore(g,r.maker,.16f,.29f);Barrel(g,.15f,end,.016f);Rail(g,-.08f,.14f);
                        if(s==0){Magazine(g,.055f,.20f,.045f);Line("Side folding wire stock",g,M("ProductSteel"),.009f,V(.055f,0,-.11f),V(.09f,-.015f,-.20f),V(.09f,-.06f,.13f));}
                        if(s==1&&r.maker==3){Magazine(g,.05f,.18f,.048f);}else if(s==1){var top=Root("Vertical top feed magazine",g);top.localPosition=V(0,.02f,.04f);top.localRotation=Quaternion.Euler(180,0,0);Magazine(top,0,.17f,.048f);}
                        if(s==2){if(r.maker==0||r.maker==3){var spiral=Root("Vertical cylindrical spiral ammunition casing",g);spiral.localPosition=V(0,-.045f,.06f);spiral.localRotation=Quaternion.Euler(102,0,0);Tube("Round helical feed body",spiral,M(r.maker==3?"MirelineOlive":"ProductSteel"),Vector3.zero,.045f,.215f,.05f,-1,24);for(int i=0;i<8;i++)Tube("Helical retaining rib",spiral,M("ProductSteel"),V(0,0,.010f+i*.026f),.049f,.009f,.88f,-1,24);Line("Continuous spiral locking track",spiral,M("Dark"),.003f,V(-.045f,0,.01f),V(-.045f,.027f,.04f),V(-.045f,-.027f,.07f),V(-.045f,.027f,.10f),V(-.045f,-.027f,.13f),V(-.045f,.027f,.16f),V(-.045f,0,.20f));}else Tube("Long under barrel helical magazine",g,M("ProductSteel"),V(0,-.085f,.08f),.043f,.27f,.1f,-1,24);}
                        if(s==3)Drum(g,.06f,.085f);break;
                    case 5:
                        end=s==2?.82f:s==3?1.02f:.94f;support=.34f;Magazine(g,bull?-.14f:.055f,.115f);Fore(g,r.maker,.20f,s==1?.52f:.44f);Barrel(g,.16f,end,s==3?.031f:.020f);Rail(g,-.11f,.18f);Scope(g,-.09f,.30f);
                        if(s!=2){var bolt=Root("Manual bolt locking handle",g);bolt.localPosition=V(.055f,.025f,-.055f);Line("Bent bolt handle",bolt,M("ProductSteel"),.008f,Vector3.zero,V(.043f,0,0),V(.043f,-.045f,0));Tube("Bolt handle knob",bolt,M("Dark"),V(.043f,-.045f,-.01f),.016f,.024f,.1f,-1,12);}
                        if(s==1||s==3)Bipod(g,.56f);if(s==3)Tube("Heavy rectangular brake collar",g,M("ProductSteel"),V(0,0,end-.06f),.047f,.06f,.74f,-1,8);break;
                }
            }
            if(series>=4)ExpandedSeries(g,c,series,end,width,r.maker);
            Maker(g,r,makerZ,width,c==1);
            if(c==1&&s==0){foreach(Transform piece in g){if(piece.name.Contains("pistol grip")||piece.name.Contains("checkering")||piece.name.Contains("Flush grip magazine")){var pos=piece.localPosition;pos.y+=.015f;piece.localPosition=pos;var scale=piece.localScale;scale.y*=.82f;piece.localScale=scale;}}}
            // Individual variants preserve architecture but visibly change handling and hardware.
            if(r.seriesVariant%2==1){Rod("Offset service sling loop",g,M("ProductSteel"),V(-width*.5f,-.025f,.12f),V(-width*.85f,-.05f,.12f),.004f);}
            if(r.seriesVariant==2)
            {
                for(int i=0;i<2;i++)Slab(g,"Reinforced action locking band","ProductSteel",width+.030f,-.075f+i*.105f,.002f,.028f,c==1?.11f:.14f);
                for(int sign=-1;sign<=1;sign+=2)Rod("External reinforcement tie rod",g,M("ProductSteel"),V(sign*(width*.5f+.010f),-.03f,-.09f),V(sign*(width*.5f+.010f),-.03f,.08f),.007f);
            }
            if(r.seriesVariant==3)
            {
                Slab(g,"Range front sight hood","ProductSteel",.032f,end-.045f,.046f,.030f,.045f);
                Tube("Range barrel stabilizer collar",g,M("ProductSteel"),V(0,0,end-.095f),c==2?.038f:.030f,.035f,.76f,-1,20);
                Slab(g,"Range adjustable rear sight","ProductSteel",.038f,-.075f,.075f,.045f,.025f);
            }
            EditionHardware(g,r.seriesVariant,c,end,width);
            float length=r.seriesVariant==1?.85f:r.seriesVariant==2?1.04f:r.seriesVariant==3?1.17f:1f;
            // Bake the whole construction through an intermediate root. Compact resolves nested
            // rotations correctly, including transverse drums, before the player scales its root.
            var assembly=Root(g.name,parent);g.name="Variant construction";g.SetParent(assembly,false);g.localScale=V(1,1,length);
            return Finish(assembly,V(0,-.095f,grip*length),V(0,-.062f,support*length),V(0,-.025f,rear*length),V(0,0,end*length));
        }
    }
}
