using UnityEngine;
using static NeonVault.DetailedHeroArsenal3D;

namespace NeonVault
{
    // Product recipes drive the complete load bearing geometry, shared with item art.
    public static class DistinctEquipmentModels3D
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Vector2 P(float z,float y)=>new Vector2(z,y);
        static Material M(string key)=>DetailedHeroMaterials3D.Get(key=="Steel"?"ProductSteel":key=="Brass"?"ProductCopper":key=="Rust"?"CinderOrange":key);
        static Transform Root(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static readonly string[] skins={"ProductSteel","IronwakeSteel","ProductSteel","ProductSteel","ProductSteel","ProductSteel"};
        static string Skin(int maker)=>skins[Mathf.Clamp(maker,0,5)];
        static readonly string[] trims={"KestrelTeal","IronwakeCopper","VoltarcYellow","MirelineOlive","Wood","AegisWhite"};
        static string Trim(int maker)=>trims[Mathf.Clamp(maker,0,5)];
        static string Fabric(int maker)=>maker==5?"NavyCanvas":maker==1?"ClothDark":"Cloth";
        static void Plate(Transform g,string name,string material,float width,float z,float y,float length,float height,int style)
        {
            float cut=Mathf.Min(style==0?.008f:style==1?.035f:style==4?.065f:.018f,Mathf.Min(length,height)*.28f);
            Profile(name,g,M(material),width,P(z-length*.5f,y-height*.5f),P(z-length*.5f,y+height*.5f-cut),P(z-length*.5f+cut,y+height*.5f),P(z+length*.5f-cut,y+height*.5f),P(z+length*.5f,y+height*.5f-cut),P(z+length*.5f,y-height*.5f));
        }
        static void MakerStructure(Transform g,EquipmentRecipe r,float start,float end,float width,float y=0)
        {
            var skin=M(Skin(r.maker));var trim=M(Trim(r.maker));
            switch(r.maker){
                case 0:
                    for(int s=-1;s<=1;s+=2){Rod("Machined longitudinal edge",g,trim,V(s*width*.48f,y+.035f,start),V(s*width*.48f,y+.035f,end),.004f);for(int i=0;i<4;i++)Tube("Countersunk precision fastener",g,trim,V(s*width*.48f,y+.012f,Mathf.Lerp(start,end,i/3f)),.004f,.006f,.2f,-1,8);}break;
                case 1:
                    for(int i=0;i<3;i++){float z=Mathf.Lerp(start,end,i/2f);Plate(g,"Thick forged locking band","Steel",width*1.25f,z,y,.035f,.115f,1);Tube("Forged lock bolt",g,M("Dark"),V(0,y+.059f,z),.012f,.025f,.1f,-1,8);}break;
                case 2:
                    for(int i=0;i<7;i++)Tube("Copper power winding",g,M("Brass"),V(0,y,Mathf.Lerp(start,end,i/6f)),width*.58f,.009f,.82f,-1,16);
                    for(int s=-1;s<=1;s+=2)Rod("Ceramic conductor",g,M("TealGlass"),V(s*width*.36f,y+.04f,start),V(s*width*.36f,y+.04f,end),.006f);break;
                case 3:
                    for(int s=-1;s<=1;s+=2){Line("Flexible chemical hose",g,M("Leather"),.008f,V(s*width*.5f,y,start),V(s*width*.9f,y-.018f,(start+end)*.5f),V(s*width*.55f,y,end));Tube("Sealed biological fluid vessel",g,M("MirelineFluid"),V(s*width*.55f,y+.025f,start),.014f,(end-start)*.72f,.07f,-1,12);}break;
                case 4:
                    for(int i=0;i<4;i++){float z=Mathf.Lerp(start,end,i/3f);var t=Profile("Welded irregular scrap gusset",g,M(i%2==0?"Rust":"Steel"),width*1.14f,P(z-.026f,y-.045f),P(z-.014f,y+.046f),P(z+.034f,y+.025f),P(z+.016f,y-.035f));t.localRotation=Quaternion.Euler(0,0,(i%2==0?-1:1)*8);Rod("Raised weld seam",g,trim,V(-width*.5f,y+.04f,z),V(width*.5f,y+.04f,z+.009f),.003f);}break;
                case 5:
                    for(int i=0;i<4;i++){float z=Mathf.Lerp(start,end,i/3f);Plate(g,"Overlapping ballistic laminate",Skin(r.maker),width*1.35f,z,y+.035f,.06f,.055f,1);Plate(g,"Shock absorbing underlayer","LeatherDark",width*1.22f,z,y,.067f,.04f,0);}break;
            }
        }
        public static DetailedHeroEquipment3D.WeaponSockets BuildWeapon(Transform parent,Gun gun,EquipmentRecipe r)
        {
            return GunSeriesModels3D.Build(parent,gun,r);
        }
        static void ReceiverSilhouette(Transform g,EquipmentRecipe r,float width)
        {
            var steel=M("Steel");
            switch(r.body){
                case 0:Profile("Rifle upper and dropped magazine well",g,steel,width,P(-.13f,-.04f),P(-.13f,.06f),P(-.105f,.075f),P(.145f,.075f),P(.17f,.045f),P(.17f,-.025f),P(.115f,-.035f),P(.105f,-.09f),P(.035f,-.09f),P(.022f,-.045f));break;
                case 1:Profile("Pistol contoured slide frame",g,steel,width,P(-.115f,-.045f),P(-.125f,.015f),P(-.10f,.065f),P(.105f,.065f),P(.155f,.035f),P(.15f,-.025f),P(.065f,-.045f),P(.035f,-.08f),P(-.008f,-.08f),P(-.03f,-.045f));break;
                case 2:Profile("Drum firearm stepped breech",g,steel,width,P(-.135f,-.04f),P(-.125f,.065f),P(-.075f,.08f),P(.145f,.08f),P(.17f,.045f),P(.155f,-.035f),P(.10f,-.05f),P(.09f,-.09f),P(-.015f,-.09f),P(-.02f,-.045f));
                    var drum=Root("Transverse axis drum feed",g);drum.localPosition=V(-.065f,-.14f,.055f);drum.localRotation=Quaternion.Euler(0,90,0);Tube("Circular ammunition drum",drum,M(Skin(r.maker)),Vector3.zero,.10f,.13f,.04f,-1,32);for(int end=0;end<2;end++){float z=end*.12f;Tube("Drum steel edge rim",drum,steel,V(0,0,z),.104f,.011f,.91f);Tube("Drum central spindle",drum,M("Steel"),V(0,0,z),.022f,.017f,.1f);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Rod("Stamped radial drum groove",drum,M("Dark"),V(Mathf.Cos(a)*.04f,Mathf.Sin(a)*.04f,z+.013f),V(Mathf.Cos(a)*.085f,Mathf.Sin(a)*.085f,z+.013f),.0015f);}}break;
                case 3:Profile("Heavy twin feed breech",g,steel,width*1.1f,P(-.145f,-.045f),P(-.145f,.04f),P(-.11f,.085f),P(.125f,.085f),P(.175f,.045f),P(.17f,-.035f),P(.10f,-.05f),P(.10f,-.095f),P(-.01f,-.095f),P(-.02f,-.045f));
                    for(int sign=-1;sign<=1;sign+=2){var pod=Root("Vertical ammunition pod",g);pod.localPosition=V(sign*.049f,-.245f,.05f);pod.localRotation=Quaternion.Euler(-90,0,0);Tube("Full height pod cylinder",pod,M(Skin(r.maker)),Vector3.zero,.043f,.19f,.04f,-1,24);for(int ring=0;ring<3;ring++)Tube("Pod protective locking collar",pod,steel,V(0,0,ring*.085f),.048f,.016f,.84f);}break;
                case 4:Profile("Open trigger well upper skeleton",g,steel,width,P(-.13f,.015f),P(-.11f,.065f),P(.155f,.065f),P(.17f,.025f),P(.14f,-.025f),P(.075f,-.025f),P(.055f,-.08f),P(.01f,-.08f),P(-.015f,-.025f));
                    for(int sign=-1;sign<=1;sign+=2)Line("Large triangular open receiver loop",g,M(Skin(r.maker)),.011f,V(sign*.037f,-.025f,-.10f),V(sign*.037f,-.15f,-.065f),V(sign*.037f,-.12f,.08f),V(sign*.037f,-.025f,.10f));break;
                case 5:Profile("Bullpup integrated butt breech silhouette",g,M(Skin(r.maker)),width*1.12f,P(-.22f,-.105f),P(-.22f,.07f),P(-.18f,.085f),P(.13f,.08f),P(.17f,.045f),P(.17f,-.035f),P(.04f,-.045f),P(-.005f,-.12f),P(-.055f,-.12f),P(-.08f,-.05f),P(-.14f,-.05f),P(-.15f,-.105f));break;
            }
        }
        static void ManufacturerSideMechanism(Transform g,EquipmentRecipe r,float width)
        {
            for(int sign=-1;sign<=1;sign+=2){var side=Root("Manufacturer mechanical side",g);side.localPosition=V(sign*(width*.5f+.003f),0,0);
                if(r.body==4){Profile("Open frame upper side strip",side,M(Skin(r.maker)),.006f,P(-.12f,.05f),P(.15f,.05f),P(.15f,.015f),P(-.12f,.015f));continue;}
                switch(r.maker){
                    case 0:Profile("Precision ivory split lower frame",side,M(Skin(r.maker)),.006f,P(-.12f,-.035f),P(-.10f,.005f),P(-.04f,.005f),P(-.02f,-.02f),P(.135f,-.02f),P(.135f,-.055f),P(.025f,-.06f),P(.005f,-.035f));Rod("Machined top receiver lip",side,M("Steel"),V(0,.065f,-.1f),V(0,.065f,.14f),.004f);break;
                    case 1:for(int i=0;i<2;i++)Plate(side,"Forged load bearing side straps",Trim(r.maker),.013f,-.075f+i*.17f,.005f,.042f,.125f,1);Plate(side,"Thick central steel plate","Steel",.010f,.02f,.01f,.15f,.085f,0);break;
                    case 2:Profile("Angular ceramic protective side",side,M(Skin(r.maker)),.010f,P(-.13f,-.03f),P(-.13f,.055f),P(-.02f,.055f),P(.01f,.01f),P(.15f,.01f),P(.15f,-.05f),P(.015f,-.05f),P(-.01f,-.025f));for(int i=0;i<7;i++)Rod("Visible horizontal copper winding",side,M("Brass"),V(sign*.009f,.027f,-.09f+i*.014f),V(sign*.009f,.052f,-.09f+i*.014f),.0035f);Plate(side,"Yellow coil service latch",Trim(r.maker),.015f,.1f,-.025f,.07f,.025f,1);break;
                    case 3:Profile("Organic cutout bone side shell",side,M(Skin(r.maker)),.012f,P(-.13f,-.035f),P(-.12f,.05f),P(-.075f,.07f),P(-.04f,.03f),P(.12f,.03f),P(.15f,-.02f),P(.12f,-.055f),P(.02f,-.055f),P(-.02f,-.025f));Line("Biological return hose",side,M("LeatherDark"),.007f,V(sign*.012f,.035f,-.1f),V(sign*.02f,.08f,-.02f),V(sign*.02f,.06f,.11f));Tube("Horizontal green fluid chamber",side,M("MirelineFluid"),V(sign*.012f,.055f,-.015f),.012f,.12f,.05f);break;
                    case 4:for(int i=0;i<3;i++){var plate=Profile("Asymmetric orange welded side plate",side,M(i==1?"Steel":Skin(r.maker)),.009f,P(-.11f+i*.08f,-.045f),P(-.10f+i*.08f,.045f),P(-.04f+i*.08f,.025f),P(-.05f+i*.08f,-.04f));plate.localRotation=Quaternion.Euler(0,0,(i-1)*3);Rod("Exposed weld bead",side,M("Steel"),V(sign*.008f,-.03f,-.10f+i*.08f),V(sign*.008f,.025f,-.09f+i*.08f),.002f);}break;
                    case 5:Plate(side,"Recessed ballistic blue panel",Skin(r.maker),.015f,.015f,.015f,.22f,.085f,1);for(int i=0;i<2;i++){var stripe=Profile("Diagonal white ballistic inlay",side,M(Trim(r.maker)),.003f,P(.025f+i*.022f,-.023f),P(.035f+i*.022f,-.023f),P(.085f+i*.022f,.05f),P(.075f+i*.022f,.05f));stripe.localPosition=V(sign*.01f,0,0);}break;
                }
                for(int i=0;i<5;i++){var bolt=Tube("Visible transverse receiver bolt",side,M("Steel"),Vector3.zero,.004f,.008f,.15f,-1,10);bolt.localPosition=V(sign*.015f,-.025f,-.10f+i*.055f);bolt.localRotation=Quaternion.Euler(0,sign*90,0);}
            }
        }
        static void ProductGrip(Transform g,EquipmentRecipe r)
        {
            Profile("Shaped full depth pistol grip",g,M(r.maker==4?"Wood":"LeatherDark"),.062f,P(-.065f,-.025f),P(.005f,-.04f),P(-.035f,-.185f),P(-.105f,-.17f));
            for(int sign=-1;sign<=1;sign+=2){var panel=Profile("Grip side maker inset",g,M(Skin(r.maker)),.004f,P(-.057f,-.055f),P(-.014f,-.065f),P(-.047f,-.163f),P(-.087f,-.153f));panel.localPosition=V(sign*.032f,0,0);for(int i=0;i<6;i++)Rod("Grip cut checkering",g,M("Dark"),V(sign*.035f,-.078f-i*.012f,-.065f),V(sign*.035f,-.078f-i*.012f,-.035f),.001f);}
        }
        static void Attachment(Transform g,EquipmentRecipe r,Vector3 position,float scale)
        {
            var p=Root("Product attachment / "+r.attachment,g);p.localPosition=position;p.localScale=Vector3.one*scale;var metal=M(Skin(r.maker));
            switch(r.attachment){
                case 0:Tube("Optical housing",p,metal,V(0,0,-.055f),.025f,.14f,.72f);Tube("Optical lens",p,M("TealGlass"),V(0,0,.085f),.018f,.003f,.05f);break;
                case 1:Tube("Auxiliary drum",p,metal,V(0,0,-.025f),.055f,.07f,.1f,-1,16);break;
                case 2:for(int i=0;i<5;i++)Tube("Induction winding",p,M("Brass"),V(0,0,-.045f+i*.02f),.037f,.008f,.8f);Rod("Ceramic core",p,M("TealGlass"),V(0,0,-.05f),V(0,0,.05f),.018f);break;
                case 3:for(int s=-1;s<=1;s+=2)Tube("Chemical vessel pair",p,M("MirelineFluid"),V(s*.025f,0,-.05f),.019f,.10f,.06f);Line("Vessel return hose",p,M("Leather"),.006f,V(-.025f,0,.05f),V(0,.035f,.075f),V(.025f,0,.05f));break;
                case 4:for(int i=0;i<4;i++)Plate(p,"Heat sink blade","Rust",.085f,-.045f+i*.03f,.015f,.009f,.075f,1);break;
                case 5:Shell("Sloped shield housing",p,metal,V(0,0,0),new Vector2(.14f,.11f),.04f);break;
            }
        }
        static void WeaponDetails(Transform g,EquipmentRecipe r,float width,bool pistol)
        {
            // Separate slide, ejection recess, bolt and contrasting receiver panels.
            Plate(g,"Exposed steel slide","Steel",width*.80f,.02f,.048f,.24f,.025f,1);
            for(int sign=-1;sign<=1;sign+=2){var side=Root("Inset side hardware",g);side.localPosition=V(sign*(width*.5f+.001f),0,0);
                Plate(side,"Dark recessed ejection port","Dark",.003f,.045f,.025f,.075f,.019f,1);
                Rod("Extractor along port",side,M("Steel"),V(sign*.003f,.018f,.015f),V(sign*.003f,.018f,.072f),.002f);
                if(r.body!=4)Plate(side,"Broad lower maker receiver inlay",Skin(r.maker),.005f,.0f,-.027f,.23f,.05f,1);
                for(int k=0;k<4;k++){var bolt=Tube("Receiver recessed screw",side,M("Steel"),Vector3.zero,.004f,.004f,.2f,-1,10);bolt.localPosition=V(sign*.005f,-.03f,-.08f+k*.06f);bolt.localRotation=Quaternion.Euler(0,sign*90,0);}
                for(int i=0;i<3;i++)Rod("Slide serration",side,M("Dark"),V(0,.042f,-.078f+i*.009f),V(0,.058f,-.075f+i*.009f),.0018f);
            }
            Rod("Charge lever",g,M("Steel"),V(width*.5f,.015f,-.025f),V(width*.87f,.015f,-.025f),.005f);
            
            Plate(g,"Rubber shoulder butt pad","Dark",.075f,pistol?-.20f:-.36f,-.025f,.015f,.11f,0);
            for(int i=0;i<4;i++)Rod("Butt pad traction rib",g,M("LeatherDark"),V(-.034f,-.065f+i*.023f,pistol?-.209f:-.369f),V(.034f,-.065f+i*.023f,pistol?-.209f:-.369f),.003f);
            if(r.maker==5)for(int i=0;i<2;i++){var stripe=Root("Aegis diagonal white identification stripe",g);stripe.localPosition=V(width*.52f,.015f,.085f+i*.018f);stripe.localRotation=Quaternion.Euler(20,0,0);Plate(stripe,"White stripe",Trim(r.maker),.003f,0,0,.007f,.05f,0);}
        }
        // Solid bevelled polygon plates. The old circular decorative shell is intentionally
        // not used for product armour: all edges have thickness and angular cut corners.
        static void Shell(string name,Transform parent,Material material,Vector3 center,Vector2 size,float depth)
        {
            var p=Root(name,parent);p.localPosition=center;p.localRotation=Quaternion.Euler(0,-90,0);
            float w=size.x*.5f,h=size.y*.5f,c=Mathf.Min(w,h)*.22f;
            Profile(name+" / solid bevel extrusion",p,material,Mathf.Clamp(depth*.7f,.012f,.04f),P(w-c,h),P(-w+c,h),P(-w,h-c),P(-w,-h+c),P(-w+c,-h),P(w-c,-h),P(w,-h+c),P(w,h-c));
        }
        static void HelmetDome(Transform parent,EquipmentRecipe recipe)
        {
            const int rings=12,sides=40;
            var vertices=new System.Collections.Generic.List<Vector3>();var normals=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
            // Continuous ellipsoidal crown, fitted at temples and occiput without ring endcaps.
            for(int j=0;j<=rings;j++){float phi=j/(float)rings*Mathf.PI*.5f;float r=Mathf.Cos(phi);for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(V(Mathf.Cos(a)*.117f*r,.12f+Mathf.Sin(phi)*.145f,Mathf.Sin(a)*.108f*r-.006f));normals.Add(V(Mathf.Cos(a)*r/.117f,Mathf.Sin(phi)/.145f,Mathf.Sin(a)*r/.108f).normalized);uv.Add(new Vector2(i/(float)sides,j/(float)rings));}}
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh{name="Continuous smooth helmet shell"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var t=Root(mesh.name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=M(Skin(recipe.maker));t.gameObject.AddComponent<ManufacturerEquipment3D.MeshLifetime>().meshes.Add(mesh);
            var nape=Root("Fitted nape guard",parent);nape.localPosition=V(0,.07f,-.086f);nape.localRotation=Quaternion.Euler(0,180,0);Shell("Rear helmet nape plate",nape,M(Skin(recipe.maker)),Vector3.zero,new Vector2(.17f,.07f),.025f);
        }
        static void Helmet(Transform g,EquipmentRecipe r)
        {
            if(r.maker!=0){IndustrialHelmet(g,r);return;}
            HelmetDome(g,r);
            Shell("Dark wraparound visor",g,M("Dark"),V(0,.09f,.088f),new Vector2(.205f,.10f),.042f);
            if(r.body!=1){Shell("Shaped lower face guard",g,M(Skin(r.maker)),V(0,.015f,.083f),new Vector2(.17f,.10f),.052f);}
            for(int sign=-1;sign<=1;sign+=2){var ear=Root("Helmet ear and rear guard",g);ear.localPosition=V(sign*.103f,.06f,-.005f);ear.localRotation=Quaternion.Euler(0,sign*90,0);Tube("Earcup locking ring",ear,M("Steel"),Vector3.zero,.044f,.012f,.65f);Tube("Earcup protective disc",ear,M(Skin(r.maker)),V(0,0,.012f),.033f,.012f,.03f);}
            if(r.body==2){Tube("Respirator filter",g,M("Steel"),V(0,.01f,.125f),.042f,.045f,.3f);Tube("Respirator filter end",g,M(Trim(r.maker)),V(0,.01f,.17f),.032f,.005f,.15f);}
            if(r.body==3)for(int sign=-1;sign<=1;sign+=2){var flap=Root("Helmet angular cheek wing",g);flap.localPosition=V(sign*.086f,.015f,.035f);flap.localRotation=Quaternion.Euler(0,sign*35,0);Shell("Cheek wing",flap,M(Skin(r.maker)),Vector3.zero,new Vector2(.08f,.16f),.025f);}
            if(r.body==4)for(int i=0;i<5;i++)Rod("Breathing grille bar",g,M("Dark"),V((i-2)*.022f,-.018f,.132f),V((i-2)*.022f,.045f,.132f),.003f);
            if(r.body==5)for(int sign=-1;sign<=1;sign+=2){Tube("Twin enhanced vision optic",g,M("Steel"),V(sign*.047f,.10f,.12f),.026f,.045f,.72f);Tube("Optic glass",g,M("TealGlass"),V(sign*.047f,.10f,.166f),.018f,.004f,.05f);}
            for(int i=0;i<2;i++){var stripe=Root("Helmet contrast crown stripe",g);stripe.localPosition=V((i-.5f)*.027f,.252f,.015f);stripe.localRotation=Quaternion.Euler(15,0,0);Plate(stripe,"Crown inlay",Trim(r.maker),.014f,0,0,.095f,.004f,0);}
            var accessory=Root("Helmet rear accessory mount",g);accessory.localPosition=V(0,.09f,-.10f);accessory.localRotation=Quaternion.Euler(0,180,0);accessory.localScale=Vector3.one*.38f;ArmorAccessory(accessory,r,.18f,.12f);
        }
        static void IndustrialHelmet(Transform g,EquipmentRecipe r)
        {
            bool cage=r.body==4;
            if(!cage)HelmetDome(g,r);
            if(cage){
                // Deliberately open over the face and scalp: no closed dome or visor behind the rods.
                for(int ring=0;ring<3;ring++){float y=.015f+ring*.085f;var hoop=Tube("Open helmet cage hoop",g,M(Trim(r.maker)),Vector3.zero,.119f,.009f,.91f,-1,24);hoop.localPosition=V(0,y,-.004f);hoop.localRotation=Quaternion.Euler(-90,0,0);hoop.localScale=V(1,.95f,1);}
                for(int i=0;i<6;i++){float a=i*Mathf.PI*2/6;Line("Open helmet vertical brace",g,M("Steel"),.007f,V(Mathf.Cos(a)*.115f,.01f,Mathf.Sin(a)*.11f),V(Mathf.Cos(a)*.117f,.16f,Mathf.Sin(a)*.11f),V(Mathf.Cos(a)*.065f,.25f,Mathf.Sin(a)*.064f),V(0,.27f,0));}
            }else{
                Shell("Industrial dark eye opening",g,M("Dark"),V(0,.09f,.092f),new Vector2(.205f,r.body==1?.115f:.065f),.038f);
                if(r.body!=1)Shell("Industrial jaw protection",g,M(Skin(r.maker)),V(0,.014f,.09f),new Vector2(.17f,.095f),.04f);
                if(r.body==1){Shell("Wide projecting helmet brim",g,M(Skin(r.maker)),V(0,.15f,.10f),new Vector2(.27f,.065f),.085f);}
                if(r.body==2){for(int rib=-1;rib<=1;rib++){Line("Fore to aft helmet reinforcement rib",g,M(Trim(r.maker)),.008f,V(rib*.034f,.13f,.104f),V(rib*.034f,.24f,.055f),V(rib*.034f,.265f,-.012f),V(rib*.034f,.21f,-.088f));}if(r.maker==1)for(int i=0;i<5;i++)Rod("Ironwake face grill",g,M("Dark"),V((i-2)*.020f,-.01f,.132f),V((i-2)*.020f,.047f,.132f),.003f);}
                if(r.body==3||r.body==5){int plates=r.body==3?3:4;for(int i=0;i<plates;i++){float y=.21f-i*.062f;Shell("Overlapping helmet lamella",g,M(Skin(r.maker)),V(0,y,.07f),new Vector2(.235f-i*.012f,.085f),.055f);}for(int sign=-1;sign<=1;sign+=2){var cheek=Root("Layered helmet side protection",g);cheek.localPosition=V(sign*.092f,.045f,0);cheek.localRotation=Quaternion.Euler(0,sign*75,0);Shell("Overlapping cheek skirt",cheek,M(Skin(r.maker)),Vector3.zero,new Vector2(.115f,.14f),.028f);}}
            }
            for(int sign=-1;sign<=1;sign+=2){var ear=Root("Helmet hinge and ear disc",g);ear.localPosition=V(sign*.117f,.082f,-.008f);ear.localRotation=Quaternion.Euler(0,sign*90,0);Tube("Locking ear rim",ear,M(Trim(r.maker)),Vector3.zero,.040f,.014f,.72f);Tube("Ear armour",ear,M(Skin(r.maker)),V(0,0,.012f),.029f,.012f,.03f);}
            var accessory=Root("Helmet accessory support",g);accessory.localPosition=V(0,.09f,-.10f);accessory.localRotation=Quaternion.Euler(0,180,0);accessory.localScale=Vector3.one*.38f;ArmorAccessory(accessory,r,.18f,.12f);
        }
        // Each added part belongs to exactly one animated bone; none bridges knee/hip/elbow joints.
        public static System.Collections.Generic.List<Transform> ApplyArmorSupplement(DetailedHero3D body,ArmorItem item,EquipmentRecipe r)
        {
            var roots=new System.Collections.Generic.List<Transform>();
            if(r.slot==1){
                foreach(var upper in r.body==0?new Transform[0]:new[]{body.leftUpperArm,body.rightUpperArm}){var p=Root("Anatomical shoulder shell",upper);roots.Add(p);p.localPosition=V(0,-.04f,0);ShoulderCap(p,r);Compact(p);}
                var collar=Root("Armour standing padded collar",body.chest);roots.Add(collar);var rim=Tube("Tall neck protective collar",collar,M("LeatherDark"),Vector3.zero,.09f,.10f,.82f,-1,24);rim.localRotation=Quaternion.Euler(-90,0,0);collar.localPosition=V(0,.16f,0);Compact(collar);
                var back=Root("Chest rear protection",body.chest);roots.Add(back);back.localPosition=V(0,-.025f,-.175f);back.localRotation=Quaternion.Euler(0,180,0);ArmorPanel(back,r,.37f,.39f);Compact(back);
            }
            if(r.slot==3){
                foreach(var thigh in new[]{body.leftThigh,body.rightThigh}){var p=Root("Separate thigh and knee armour",thigh);roots.Add(p);p.localPosition=V(0,-.21f,.125f);ArmorPanel(p,r,.19f,.30f);Shell("Independent thigh mounted knee guard",p,M(Skin(r.maker)),V(0,-.19f,.018f),new Vector2(.145f,.13f),.04f);Compact(p);}
                var waist=Root("Pelvis mounted belt and hip guards",body.pelvis);roots.Add(waist);var belt=Tube("Full waist belt",waist,M("LeatherDark"),Vector3.zero,.205f,.045f,.90f,-1,24);belt.localPosition=V(0,.045f,0);belt.localRotation=Quaternion.Euler(-90,0,0);belt.localScale=V(1,.63f,1);for(int sign=-1;sign<=1;sign+=2){var hip=Root("Articulated hip side guard",waist);hip.localPosition=V(sign*.17f,-.05f,0);hip.localRotation=Quaternion.Euler(0,sign*80,0);ArmorPanel(hip,r,.10f,.13f);}Compact(waist);
            }
            return roots;
        }
        static void OwnedSurface(Transform parent,string name,Material material,System.Collections.Generic.List<Vector3> vertices,System.Collections.Generic.List<int> triangles)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var t=Root(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;t.gameObject.AddComponent<ManufacturerEquipment3D.MeshLifetime>().meshes.Add(mesh);
        }
        static void TorsoShell(Transform g,EquipmentRecipe r,float w,float h)
        {
            if(r.body==4){ArmorPanel(g,r,w,h);return;}
            // Torso surface wraps laterally, expands over the chest and tapers at the waist.
            int bands=r.body==3?3:r.body==5?4:1;
            for(int band=0;band<bands;band++){
                const int rows=8,cols=20;var v=new System.Collections.Generic.List<Vector3>();var ix=new System.Collections.Generic.List<int>();
                for(int j=0;j<=rows;j++){float t=(band+j/(float)rows)/bands;float y=(t-.5f)*h;float chest=Mathf.Sin(t*Mathf.PI);float rx=w*(.60f+.11f*chest);float rz=.185f+.025f*chest;for(int i=0;i<=cols;i++){float a=Mathf.Lerp(-1.25f,1.25f,i/(float)cols);float notch=t>.85f?.025f*Mathf.Pow(Mathf.Cos(a),6)*(t-.85f)/.15f:0;v.Add(V(Mathf.Sin(a)*rx,y-notch,Mathf.Cos(a)*rz-.205f+(bands>1?band*.004f:0)));}}
                for(int j=0;j<rows;j++)for(int i=0;i<cols;i++){int a=j*(cols+1)+i,b=a+cols+1;ix.AddRange(new[]{a,a+1,b,a+1,b+1,b});}int count=v.Count;for(int k=0;k<count;k++)v.Add(v[k]-V(0,0,.009f));int faceIndices=ix.Count;for(int k=0;k<faceIndices;k+=3)ix.AddRange(new[]{ix[k]+count,ix[k+2]+count,ix[k+1]+count});for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){int q=row*(cols+1)+col,next=-1;if(row==0&&col<cols)next=q+1;else if(row==rows&&col>0)next=q-1;else if(col==0&&row<rows)next=q+cols+1;else if(col==cols&&row>0)next=q-cols-1;if(next>=0)ix.AddRange(new[]{q,next,q+count,next,next+count,q+count});}OwnedSurface(g,"Fitted curved breastplate band "+band,M(Fabric(r.maker)),v,ix);
            }
            if(r.body==1){for(int sign=-1;sign<=1;sign+=2){var flange=Root("Flared angular breastplate shoulder",g);flange.localPosition=V(sign*w*.36f,h*.31f,-.035f);flange.localRotation=Quaternion.Euler(0,sign*30,sign*-12);Shell("Shoulder wing plate",flange,M(Skin(r.maker)),Vector3.zero,new Vector2(w*.32f,h*.34f),.025f);}}
            if(r.body==2)for(int rib=-2;rib<=2;rib++){float x=rib*w*.14f;Line("Fitted vertical breastplate channel",g,M(Trim(r.maker)),.005f,V(x,-h*.43f,-.01f),V(x,0,.033f),V(x,h*.40f,0));}
            if(r.maker==0){Line("Kestrel angular chest division",g,M("Steel"),.003f,V(-w*.4f,h*.20f,-.01f),V(0,-h*.12f,.041f),V(w*.4f,h*.20f,-.01f));}
            if(r.maker==2)for(int sign=-1;sign<=1;sign+=2)Line("Voltarc chest conductor rim",g,M(Trim(r.maker)),.004f,V(sign*w*.37f,-h*.4f,-.005f),V(sign*w*.44f,0,-.005f),V(sign*w*.36f,h*.42f,-.005f));
            if(r.maker==3)for(int sign=-1;sign<=1;sign+=2)Line("Mireline flexible shell seal",g,M("MirelineOlive"),.006f,V(sign*w*.36f,-h*.4f,-.01f),V(sign*w*.47f,0,-.02f),V(sign*w*.35f,h*.4f,0));
            if(r.maker==4)for(int i=0;i<3;i++){var patch=Root("Cinder welded torso patch",g);patch.localPosition=V((i-1)*w*.22f,(i%2==0?1:-1)*h*.20f,.025f);Shell("Reclaimed angular patch",patch,M(i==1?"Steel":Skin(r.maker)),Vector3.zero,new Vector2(w*.23f,h*.22f),.012f);}
            if(r.maker==5){var stripe=Root("Aegis torso identification stripe",g);stripe.localPosition=V(w*.12f,h*.03f,.035f);stripe.localRotation=Quaternion.Euler(0,0,-30);Shell("White ballistic stripe",stripe,M(Trim(r.maker)),Vector3.zero,new Vector2(.045f,h*.55f),.005f);}
        }
        static void ShoulderCap(Transform parent,EquipmentRecipe r)
        {
            const int rows=9,cols=20;int layers=r.body==3?3:r.body==5?4:1;
            for(int layer=0;layer<layers;layer++){var v=new System.Collections.Generic.List<Vector3>();var ix=new System.Collections.Generic.List<int>();for(int j=0;j<=rows;j++){float phi=Mathf.Lerp(.08f,1.70f,j/(float)rows);for(int i=0;i<=cols;i++){float a=Mathf.Lerp(-Mathf.PI*.93f,Mathf.PI*.93f,i/(float)cols);v.Add(V(Mathf.Sin(a)*Mathf.Sin(phi)*.135f,Mathf.Cos(phi)*.10f-layer*.035f,Mathf.Cos(a)*Mathf.Sin(phi)*.13f));}}for(int j=0;j<rows;j++)for(int i=0;i<cols;i++){int a=j*(cols+1)+i,b=a+cols+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}OwnedSurface(parent,"Curved articulated shoulder cap "+layer,M(Fabric(r.maker)),v,ix);}
        }
        static void ArmorPanel(Transform g,EquipmentRecipe r,float w,float h)
        {
            // Same six columns used by the authored non-Kestrel armour sheets.
            if(r.body==4){for(int sign=-1;sign<=1;sign+=2){Line("Open armour perimeter rail",g,M(Trim(r.maker)),.009f,V(sign*w*.42f,-h*.5f,0),V(sign*w*.5f,0,.025f),V(sign*w*.42f,h*.5f,0));Rod("Open armour transverse brace",g,M("Steel"),V(-w*.45f,sign*h*.30f,.02f),V(w*.45f,sign*h*.30f,.02f),.008f);}return;}
            int count=r.body==3?3:r.body==5?4:1;for(int i=0;i<count;i++)Shell("Authored armour panel",g,M(Skin(r.maker)),V(0,(i-(count-1)*.5f)*h/count,0),new Vector2(w,h/count*1.12f),r.body==3?.045f:.03f);
            if(r.body==1)for(int sign=-1;sign<=1;sign+=2){var fin=Root("Angular side flange",g);fin.localPosition=V(sign*w*.40f,0,0);fin.localRotation=Quaternion.Euler(0,sign*35,0);Shell("Angular guard edge",fin,M(Skin(r.maker)),Vector3.zero,new Vector2(w*.32f,h),.02f);}
            if(r.body==2)for(int i=0;i<3;i++)Rod("Long parallel armour rib",g,M(Trim(r.maker)),V((i-1)*w*.28f,-h*.42f,.035f),V((i-1)*w*.28f,h*.42f,.035f),.006f);
        }
        static void WrapArmor(Transform g,EquipmentRecipe r)
        {
            if(r.slot==4){
                Profile("Complete enclosed leather shoe upper",g,M("LeatherDark"),.145f,P(-.075f,-.065f),P(-.075f,.045f),P(-.035f,.075f),P(.045f,.06f),P(.10f,.015f),P(.205f,-.015f),P(.225f,-.04f),P(.21f,-.072f));
                Profile("Contoured protective shoe shell",g,M(Skin(r.maker)),.151f,P(.025f,-.058f),P(.04f,.015f),P(.11f,-.005f),P(.205f,-.02f),P(.215f,-.045f),P(.20f,-.063f));
                var ankle=Tube("Complete armored boot ankle",g,M("LeatherDark"),Vector3.zero,.075f,.14f,.82f,-1,20);ankle.localPosition=V(0,-.025f,.015f);ankle.localRotation=Quaternion.Euler(-90,0,0);ankle.localScale=V(1,1.08f,1);
                Shell("Rounded steel toe cap",g,M(r.body==4&&r.maker!=0?"Dark":Skin(r.maker)),V(0,-.024f,.16f),new Vector2(.15f,.115f),.055f);
                Plate(g,"Boot rubber outsole","Dark",.155f,.07f,-.072f,.27f,.028f,1);
                for(int i=0;i<3;i++)Rod("Boot instep fastening",g,M("Leather"),V(-.06f,-.005f,.045f+i*.033f),V(.06f,-.005f,.045f+i*.033f),.006f);
            }else{
                float radius=r.slot==2?.073f:.088f,length=r.slot==2?.23f:.31f;
                var sleeve=Tube("Continuous articulated armored cuff",g,M("LeatherDark"),Vector3.zero,radius,length,.90f,-1,20);sleeve.localPosition=V(0,-.025f,0);sleeve.localRotation=Quaternion.Euler(90,0,0);
                for(int i=0;i<2;i++){var strap=Tube("Circumferential retaining strap",g,M(Trim(r.maker)),Vector3.zero,radius+.003f,.025f,.90f,-1,20);strap.localPosition=V(0,-.055f-i*(length-.085f),0);strap.localRotation=Quaternion.Euler(90,0,0);}
                if(r.slot==2){Profile("Closed protective glove palm",g,M("LeatherDark"),.105f,P(-.045f,-.245f),P(.045f,-.245f),P(.065f,-.34f),P(.035f,-.375f),P(-.045f,-.355f));Shell("Armored glove knuckle cover",g,M(Skin(r.maker)),V(0,-.29f,.04f),new Vector2(.11f,.095f),.03f);for(int i=0;i<4;i++)Shell("Individual knuckle plate",g,M("Steel"),V((i-1.5f)*.024f,-.31f,.05f),new Vector2(.02f,.025f),.014f);}
            }
        }
        static void ArmorAccessory(Transform cover,EquipmentRecipe r,float w,float h)
        {
            var a=Root("Slot-specific accessory / "+r.slot+" / "+r.attachment,cover);
            // Helmet callers already supply a small rear mount; orient to its local support.
            if(r.slot==0){
                a.localScale=Vector3.one*2.4f;
                switch(r.attachment){
                    case 0:var filter=Root("Side-mounted breathing filter",a);filter.localPosition=V(.10f,-.025f,-.065f);filter.localRotation=Quaternion.Euler(0,90,0);Tube("Filter housing",filter,M("Steel"),Vector3.zero,.026f,.035f,.5f);Tube("Filter end grille",filter,M(Skin(r.maker)),V(0,0,.035f),.023f,.005f,.4f);break;
                    case 1:for(int sign=-1;sign<=1;sign+=2){Tube("Forehead night vision housing",a,M("Steel"),V(sign*.027f,.065f,-.22f),.019f,.055f,.7f);Tube("Night vision lens",a,M("TealGlass"),V(sign*.027f,.065f,-.225f),.013f,.004f,.05f);}break;
                    case 2:Plate(a,"Helmet radio support",Skin(r.maker),.025f,.01f,.005f,.035f,.065f,1);Rod("Flexible radio aerial",a,M("Dark"),V(.09f,.02f,0),V(.10f,.24f,.025f),.003f);break;
                    case 3:for(int sign=-1;sign<=1;sign+=2){var cheek=Root("Cheek accessory",a);cheek.localPosition=V(sign*.09f,-.045f,-.12f);cheek.localRotation=Quaternion.Euler(0,sign*60,0);Shell("Cheek guard panel",cheek,M(Skin(r.maker)),Vector3.zero,new Vector2(.07f,.09f),.02f);}break;
                    case 4:for(int sign=-1;sign<=1;sign+=2)Rod("Crown accessory rail",a,M("Steel"),V(sign*.035f,.17f,-.03f),V(sign*.035f,.17f,-.17f),.008f);break;
                    case 5:Plate(a,"Rear helmet counterweight",Skin(r.maker),.10f,.035f,0,.045f,.065f,1);Plate(a,"Counterweight backing","Dark",.11f,.015f,0,.012f,.07f,0);break;
                }return;
            }
            a.localPosition=V(0,-h*.20f,.035f);
            if(r.slot==1){switch(r.attachment){
                case 0:for(int i=0;i<3;i++)Pouch(a,r,V((i-1)*.075f,-.005f,0),.065f);break;
                case 1:for(int sign=-1;sign<=1;sign+=2){var plate=Root("Supplementary shoulder plate",a);plate.localPosition=V(sign*.15f,.18f,-.025f);plate.localRotation=Quaternion.Euler(0,sign*30,sign*-18);Shell("Angular shoulder reinforcement",plate,M(Skin(r.maker)),Vector3.zero,new Vector2(.12f,.09f),.025f);}break;
                case 2:Canister(a,r,V(.095f,.015f,.025f),.035f,.14f);Line("Chest tank return hose",a,M("LeatherDark"),.006f,V(.095f,.08f,.025f),V(.16f,.15f,0),V(.12f,.20f,-.035f));break;
                case 3:for(int sign=-1;sign<=1;sign+=2)Line("Crossed chest load strap",a,M("LeatherDark"),.013f,V(sign*.14f,-.11f,0),V(0,.04f,.01f),V(-sign*.14f,.21f,0));break;
                case 4:Shell("Chest load belt",a,M("LeatherDark"),V(0,-.07f,0),new Vector2(.32f,.045f),.018f);Shell("Belt buckle",a,M("Steel"),V(0,-.07f,.018f),new Vector2(.05f,.04f),.02f);break;
                case 5:Shell("Supplementary chest shield",a,M(Skin(r.maker)),V(0,.03f,.012f),new Vector2(.22f,.20f),.04f);break;
            }return;}
            if(r.slot==2){switch(r.attachment){
                case 0:Pouch(a,r,V(.045f,-.07f,.005f),.045f);break;
                case 1:var fin=Root("Forearm side fin",a);fin.localPosition=V(.07f,.02f,0);fin.localRotation=Quaternion.Euler(0,40,0);Shell("Forearm return fin",fin,M(Skin(r.maker)),Vector3.zero,new Vector2(.055f,.14f),.018f);break;
                case 2:Canister(a,r,V(.055f,.015f,.01f),.016f,.075f);break;
                case 3:Shell("Wrist retaining strap",a,M("LeatherDark"),V(0,-.07f,.005f),new Vector2(.13f,.025f),.012f);Shell("Strap buckle",a,M("Steel"),V(.025f,-.07f,.02f),new Vector2(.025f,.025f),.012f);break;
                case 4:Shell("Wrist protective brace",a,M(Skin(r.maker)),V(0,-.075f,.012f),new Vector2(.12f,.07f),.025f);break;
                case 5:Shell("Additional glove knuckle plate",a,M(Skin(r.maker)),V(0,-.13f,.015f),new Vector2(.10f,.045f),.022f);break;
            }return;}
            if(r.slot==3){switch(r.attachment){
                case 0:Pouch(a,r,V(.065f,.10f,0),.055f);break;
                case 1:Shell("Leg side hip plate",a,M(Skin(r.maker)),V(.065f,.12f,0),new Vector2(.10f,.12f),.025f);break;
                case 2:Canister(a,r,V(.075f,.035f,0),.021f,.10f);break;
                case 3:for(int i=0;i<2;i++)Shell("Leg retaining strap",a,M("LeatherDark"),V(0,-.07f+i*.17f,0),new Vector2(.18f,.028f),.015f);break;
                case 4:Shell("Additional knee plate",a,M(Skin(r.maker)),V(0,.13f,.015f),new Vector2(.14f,.10f),.03f);break;
                case 5:Shell("Additional shin protection",a,M(Skin(r.maker)),V(0,-.015f,.015f),new Vector2(.13f,.22f),.025f);break;
            }return;}
            switch(r.attachment){
                case 0:for(int i=0;i<3;i++)Rod("Boot lace fastening",a,M("Leather"),V(-.05f,-.015f+i*.025f,0),V(.05f,-.015f+i*.025f,.005f),.004f);break;
                case 1:Shell("Boot ankle side plate",a,M(Skin(r.maker)),V(.06f,.05f,-.025f),new Vector2(.065f,.08f),.02f);break;
                case 2:Canister(a,r,V(0,0,-.22f),.018f,.055f);break;
                case 3:Shell("Boot instep retaining strap",a,M("LeatherDark"),V(0,-.015f,.025f),new Vector2(.13f,.035f),.015f);break;
                case 4:Shell("Boot upper cuff plate",a,M(Skin(r.maker)),V(0,.08f,-.015f),new Vector2(.13f,.055f),.022f);break;
                case 5:Shell("Boot additional toe cap",a,M(Skin(r.maker)),V(0,-.03f,.035f),new Vector2(.14f,.08f),.025f);break;
            }
        }
        static void Pouch(Transform a,EquipmentRecipe r,Vector3 position,float width){var p=Root("Equipment pouch",a);p.localPosition=position;Shell("Leather pouch body",p,M("LeatherDark"),Vector3.zero,new Vector2(width,width*1.25f),.035f);Shell("Manufacturer pouch flap",p,M(Skin(r.maker)),V(0,width*.4f,.02f),new Vector2(width*1.05f,width*.35f),.015f);}
        static void Canister(Transform a,EquipmentRecipe r,Vector3 position,float radius,float height){var p=Root("Vertical accessory canister",a);p.localPosition=position-V(0,height*.5f,0);p.localRotation=Quaternion.Euler(-90,0,0);Tube("Sealed accessory vessel",p,M(Skin(r.maker)),Vector3.zero,radius,height,.06f);for(int end=0;end<2;end++)Tube("Canister steel seal",p,M("Steel"),V(0,0,end*(height-.008f)),radius*1.12f,.009f,.78f);}
        static void FrameBox(Transform g,EquipmentRecipe r,float width,float height,float length)
        {
            // Thick four-sided chassis, with open front/rear windows and inset metal lining.
            for(int sign=-1;sign<=1;sign+=2){
                PlateTransform(g,"Load bearing case side wall",Skin(r.maker),.025f,0,0,length,height,1).localPosition+=V(sign*width*.5f,0,0);
                var roof=PlateTransform(g,"Chassis roof and floor",Skin(r.maker),width,0,sign*height*.5f,length,.025f,1);
                PlateTransform(g,"Inset steel roof liner","Steel",width*.88f,0,sign*(height*.5f-.014f),length*.91f,.008f,0);
                for(int end=-1;end<=1;end+=2){var corner=Root("Reinforced case corner",g);corner.localPosition=V(sign*width*.5f,0,end*length*.5f);Shell("Corner upright armour",corner,M(Skin(r.maker)),Vector3.zero,new Vector2(.038f,height*1.12f),.035f);for(int ysign=-1;ysign<=1;ysign+=2)Tube("Case corner screw",corner,M("Steel"),V(0,ysign*height*.38f,.024f),.004f,.004f,.1f,-1,10);}
            }
        }
        static Transform PlateTransform(Transform p,string n,string material,float width,float z,float y,float length,float height,int style){var t=Root(n,p);Plate(t,n,material,width,z,y,length,height,style);return t;}
        static void ModuleRingHousing(Transform g,EquipmentRecipe r,float x,float radius)
        {
            for(int end=0;end<2;end++){
                float z=end==0?-.075f:.065f;Tube("Casing end annulus",g,M(Skin(r.maker)),V(x,0,z),radius,.025f,.69f,-1,32);Tube("Contrasting inner annulus",g,M(Trim(r.maker)),V(x,0,z+.006f),radius*.94f,.009f,.80f,-1,32);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Tube("Annulus exposed bolt",g,M("Steel"),V(x+Mathf.Cos(a)*radius*.92f,Mathf.Sin(a)*radius*.92f,z+.02f),.003f,.004f,.1f,-1,8);}
            }
            Tube("Continuous thick open annular casing",g,M(Skin(r.maker)),V(x,0,-.062f),radius*.97f,.132f,.77f,-1,40);
            Tube("Dark interior casing liner",g,M("Steel"),V(x,0,-.06f),radius*.758f,.128f,.97f,-1,40);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Rod("Visible longitudinal casing rib",g,M(Skin(r.maker)),V(x+Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,-.06f),V(x+Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,.07f),.008f);}
        }
        static void ModuleCore(Transform g,EquipmentRecipe r)
        {
            var a=Root("Module power core / "+r.attachment,g);var skin=M(Skin(r.maker));
            switch(r.attachment){
                case 0:for(int i=0;i<3;i++){var cell=Root("Vertical capacitor cell",a);cell.localPosition=V((i-1)*.046f,-.065f,0);cell.localRotation=Quaternion.Euler(-90,0,0);Tube("Parallel capacitor",cell,skin,Vector3.zero,.020f,.13f,.05f);for(int end=0;end<2;end++){Tube("Capacitor brass terminal collar",cell,M("Brass"),V(0,0,end*.12f),.023f,.012f,.75f);Tube("Capacitor contact",cell,M("Brass"),V(0,0,end==0?-.009f:.132f),.008f,.009f,.05f);}}break;
                case 1:Plate(a,"Battery power cell",Skin(r.maker),.11f,0,0,.14f,.085f,1);for(int sign=-1;sign<=1;sign+=2)Tube("Battery terminal",a,M("Brass"),V(sign*.03f,.04f,.05f),.009f,.02f,.05f);break;
                case 2:Tube("Inductor core",a,M("Dark"),V(0,0,-.075f),.04f,.15f,.05f);for(int i=0;i<12;i++)Tube("Fine copper coil turn",a,M("Brass"),V(0,0,-.07f+i*.012f),.045f,.006f,.85f,-1,16);break;
                case 3:Tube("Green chemical reactor vessel",a,M("MirelineFluid"),V(0,0,-.07f),.044f,.14f,.05f);for(int sign=-1;sign<=1;sign+=2)Tube("Reactor seal",a,skin,V(0,0,sign*.07f),.049f,.015f,.05f);break;
                case 4:Tube("Large flywheel disc",a,M("Steel"),V(0,0,-.025f),.085f,.05f,.25f,-1,24);Tube("Flywheel copper hub",a,M("Brass"),V(0,0,.025f),.028f,.04f,.3f);break;
                case 5:Plate(a,"Exposed green circuit board","MirelineOlive",.16f,0,0,.18f,.015f,0);for(int i=0;i<4;i++)for(int sign=-1;sign<=1;sign+=2){Plate(a,"Circuit ceramic chip",Skin(r.maker),.035f,-.06f+i*.04f,.02f,.025f,.02f,0);Rod("Copper printed trace",a,M("Brass"),V(sign*.055f,.013f,-.075f),V(sign*.055f,.013f,.075f),.0015f);}break;
            }
        }
        public static Transform BuildArmor(Transform parent,ArmorItem item,EquipmentRecipe r)
        {
            var g=Root("Distinct armor / "+r.id,parent);string skin=Skin(r.maker);
            if(r.slot==0){Helmet(g,r);Compact(g);return g;}
            if(r.slot==2||r.slot==3||r.slot==4)WrapArmor(g,r);
            float w=r.slot==1?.38f:r.slot==0?.24f:r.slot==3?.16f:.15f;
            float h=r.slot==1?.43f:r.slot==0?.25f:r.slot==3?.29f:r.slot==4?.13f:.23f;
            // Full shaped cover lies over the preserved underlying garment and animated bone.
            var cover=Root("Load bearing shell",g);cover.localPosition=V(0,r.slot==4?.015f:r.slot>=2?-.12f:-.015f,r.slot==1?.205f:r.slot==0?.10f:r.slot==4?.12f:.065f);
            if(r.slot==1){var cloth=Root("Padded chest harness under shell",g);cloth.localPosition=V(0,.02f,.175f);// Base jacket already supplies continuous cloth; a flat second backing intersected the curved shell.
                for(int sign=-1;sign<=1;sign+=2)Line("Shoulder load harness",cloth,M("Leather"),.016f,V(sign*.13f,-.18f,.01f),V(sign*.18f,.18f,.01f),V(sign*.10f,.23f,-.08f));}
            if(r.slot==1)TorsoShell(cover,r,w,h);else ArmorPanel(cover,r,w,h);
            ArmorAccessory(cover,r,w,h);
            Compact(g);return g;
        }
        public static Transform BuildModule(Transform parent,ArtifactItem item)
        {
            var r=DistinctEquipmentDesigns.For(item);if(r==null)return null;var g=Root("Distinct module / "+r.id,parent);
            switch(r.body){
                case 0:FrameBox(g,r,.18f,.12f,.22f);break;
                case 1:ModuleRingHousing(g,r,0,.10f);break;
                case 2:for(int sign=-1;sign<=1;sign+=2)ModuleRingHousing(g,r,sign*.065f,.065f);break;
                case 3:FrameBox(g,r,.22f,.18f,.22f);for(int sign=-1;sign<=1;sign+=2){Rod("Cage diagonal brace",g,M("Steel"),V(sign*.11f,-.09f,-.11f),V(-sign*.11f,.09f,-.11f),.009f);}break;
                case 4:Line("Shield perimeter housing",g,M(Skin(r.maker)),.019f,V(0,-.12f,0),V(-.12f,-.02f,0),V(-.10f,.10f,0),V(.10f,.10f,0),V(.12f,-.02f,0),V(0,-.12f,0));break;
                case 5:for(int sign=-1;sign<=1;sign+=2)Plate(g,"Long module channel rail",Skin(r.maker),.035f,0,sign*.065f,.32f,.035f,1);break;
            }
            ModuleCore(g,r);Compact(g);return g;
        }
    }
}
