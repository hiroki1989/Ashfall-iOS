using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Geometry library for the complete inventory; authored in meters, forward is +Z.
    public static class DetailedHeroArsenal3D
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Material M(string key)=>DetailedHeroMaterials3D.Get(key);
        static readonly Dictionary<string,Material> portraitMaterials=new Dictionary<string,Material>();
        static Material WM(string key)
        {
            if(portraitMaterials.TryGetValue(key,out var value)&&value)return value;
            string source=key=="Canvas"?"Cloth":key=="Copper"?"Brass":key=="GreenGlass"?"TealGlass":key=="TanPolymer"?"Cloth":key=="RedMetal"||key=="BlueSteel"?"Steel":key;
            var material=new Material(M(source)){name="Original portrait / "+key,hideFlags=HideFlags.DontSave};
            if(key=="Canvas")material.color=new Color(.62f,.55f,.39f);
            if(key=="RedMetal")material.color=new Color(.38f,.065f,.055f);
            if(key=="BlueSteel")material.color=new Color(.13f,.24f,.27f);
            if(key=="Copper")material.color=new Color(.47f,.21f,.065f);
            if(key=="TanPolymer")material.color=new Color(.38f,.38f,.23f);
            if(key=="GreenGlass")material.color=new Color(.32f,.53f,.025f);
            portraitMaterials[key]=material;return material;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearPortraitMaterials(){foreach(var value in portraitMaterials.Values)if(value)UnityEngine.Object.Destroy(value);portraitMaterials.Clear();}
        sealed class Resources : MonoBehaviour
        {
            public readonly List<Mesh> meshes=new List<Mesh>();
            void OnDestroy(){foreach(var mesh in meshes)if(mesh)UnityEngine.Object.Destroy(mesh);}
        }
        static Transform G(string name,Transform parent,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        static void Release(Transform t){if(t){t.gameObject.SetActive(false);UnityEngine.Object.Destroy(t.gameObject);}}
        static Transform MeshPart(string name,Transform p,Material material,List<Vector3> vertices,List<int> triangles)
        {
            var t=G(name,p,Vector3.zero);var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].z*5+vertices[i].x*3,vertices[i].y*5);mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;t.gameObject.AddComponent<Resources>().meshes.Add(mesh);return t;
        }
        internal static void Compact(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();var remove=new List<Transform>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {var r=f.GetComponent<MeshRenderer>();if(!r||!f.sharedMesh||!r.sharedMaterial)continue;if(!groups.TryGetValue(r.sharedMaterial,out var list)){list=new List<CombineInstance>();groups.Add(r.sharedMaterial,list);}list.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});remove.Add(f.transform);}
            foreach(var pair in groups){var t=G("Batched / "+pair.Key.name,root,Vector3.zero);var mesh=new Mesh{name=t.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;t.gameObject.AddComponent<Resources>().meshes.Add(mesh);}
            foreach(var t in remove)Release(t);
        }
        // Closed bevel extrusion, ear-clipped caps support concave stock/grip/blade polygons.
        internal static Transform Profile(string name,Transform p,Material m,float width,params Vector2[] outline)
        {
            int n=outline.Length;Vector2 center=Vector2.zero;foreach(var q in outline)center+=q;center/=n;
            var v=new List<Vector3>();var ix=new List<int>();
            for(int j=0;j<4;j++){float x=j==0?-width*.5f:j==1?-width*.38f:j==2?width*.38f:width*.5f;float inset=j==0||j==3?.95f:1;foreach(var q in outline){var a=center+(q-center)*inset;v.Add(V(x,a.y,a.x));}}
            float area=0;for(int i=0;i<n;i++)area+=outline[i].x*outline[(i+1)%n].y-outline[(i+1)%n].x*outline[i].y;
            for(int j=0;j<3;j++)for(int i=0;i<n;i++){int a=j*n+i,b=j*n+(i+1)%n;if(area<0)ix.AddRange(new[]{a,b,b+n,a,b+n,a+n});else ix.AddRange(new[]{a,b+n,b,a,a+n,b+n});}
            var poly=new List<int>();for(int i=0;i<n;i++)poly.Add(area>0?i:n-1-i);var cap=new List<int>();
            float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
            int guard=0;while(poly.Count>3&&guard++<n*n){bool cut=false;for(int i=0;i<poly.Count;i++){int a=poly[(i+poly.Count-1)%poly.Count],b=poly[i],c=poly[(i+1)%poly.Count];if(Cross(outline[a],outline[b],outline[c])<.00000001f)continue;bool contains=false;foreach(int q in poly)if(q!=a&&q!=b&&q!=c&&Cross(outline[a],outline[b],outline[q])>=0&&Cross(outline[b],outline[c],outline[q])>=0&&Cross(outline[c],outline[a],outline[q])>=0){contains=true;break;}if(contains)continue;cap.AddRange(new[]{a,b,c});poly.RemoveAt(i);cut=true;break;}if(!cut)break;}
            if(poly.Count==3)cap.AddRange(poly);for(int i=0;i<cap.Count;i+=3)ix.AddRange(new[]{cap[i],cap[i+1],cap[i+2],3*n+cap[i],3*n+cap[i+2],3*n+cap[i+1]});return MeshPart(name,p,m,v,ix);
        }
        static Vector2 P(float z,float y)=>new Vector2(z,y);
        internal static Transform Tube(string name,Transform p,Material m,Vector3 start,float radius,float length,float bore=.55f,float endRadius=-1,int sides=32)
        {
            if(endRadius<0)endRadius=radius;var v=new List<Vector3>();var ix=new List<int>();
            for(int ring=0;ring<4;ring++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides,r=(ring%2==0?radius:endRadius)*(ring<2?1:bore);v.Add(start+V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,ring%2*length));}
            int[] ra={0,3,2,1},rb={1,2,0,3};for(int j=0;j<4;j++)for(int i=0;i<sides;i++){int a=ra[j]*(sides+1)+i,b=rb[j]*(sides+1)+i;ix.AddRange(new[]{a,a+1,b,a+1,b+1,b});}return MeshPart(name,p,m,v,ix);
        }
        internal static Transform Rod(string n,Transform p,Material m,Vector3 a,Vector3 b,float radius)
        {var t=Tube(n,p,m,Vector3.zero,radius,Vector3.Distance(a,b),.015f,-1,12);t.localPosition=a;t.localRotation=Quaternion.LookRotation(b-a);return t;}
        internal static void Line(string n,Transform p,Material m,float radius,params Vector3[] points){for(int i=1;i<points.Length;i++)Rod(n,p,m,points[i-1],points[i],radius);}
        static void Band(string n,Transform p,Material m,float y,float rx,float rz,float width,float start=0,float span=Mathf.PI*2)
        {
            const int N=40;var v=new List<Vector3>();var ix=new List<int>();for(int j=0;j<2;j++)for(int i=0;i<=N;i++){float a=start+span*i/N;v.Add(V(Mathf.Cos(a)*rx,y+(j-.5f)*width,Mathf.Sin(a)*rz));}for(int i=0;i<N;i++){int a=i,b=a+N+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}MeshPart(n,p,m,v,ix);
        }
        internal static void Shell(string n,Transform p,Material m,Vector3 center,Vector2 size,float curve)
        {
            const int R=8,N=36;var v=new List<Vector3>{center+V(0,0,curve)};var ix=new List<int>();for(int j=1;j<=R;j++)for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N,r=j/(float)R;v.Add(center+V(Mathf.Cos(a)*size.x*.5f*r,Mathf.Sin(a)*size.y*.5f*r,curve*Mathf.Sqrt(1-r*r*.9f)+Mathf.Sin(a*5+r*12)*.001f*r));}for(int i=0;i<N;i++)ix.AddRange(new[]{0,i+1,i+2});for(int j=0;j<R-1;j++)for(int i=0;i<N;i++){int a=1+j*(N+1)+i,b=a+N+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}MeshPart(n,p,m,v,ix);
            Vector3 last=Vector3.zero;for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N;Vector3 q=center+V(Mathf.Cos(a)*size.x*.5f,Mathf.Sin(a)*size.y*.5f,curve*.32f);if(i>0)Rod("Rolled steel hem",p,M("Steel"),last,q,.0026f);if(i%6==0)Tube("Plate rivet",p,M("Brass"),q,.0037f,.002f,.03f,-1,12);last=q;}
        }
        static void Fasteners(Transform g,float from,float to,float x,float y,int count)
        {for(int s=-1;s<=1;s+=2)for(int i=0;i<count;i++){var q=V(s*x,y,Mathf.Lerp(from,to,count==1?0:i/(float)(count-1)));var t=Tube("Slotted receiver screw",g,M("Steel"),Vector3.zero,.0033f,.002f,.02f,-1,12);t.localPosition=q;t.localRotation=Quaternion.LookRotation(Vector3.right*s);Rod("Driver slot",g,M("Dark"),q+V(s*.002f,-.002f,0),q+V(s*.002f,.002f,0),.0007f);}}
        static void Sights(Transform g,float rear,float front,float height=.065f)
        {for(int s=-1;s<=1;s+=2)Rod("Notch sight ear",g,M("Steel"),V(s*.009f,.03f,rear),V(s*.009f,height,rear),.003f);Rod("Front blade sight",g,M("Steel"),V(0,.015f,front),V(0,height,front),.0035f);}
        static void Scope(Transform g,float z,float length=.23f)
        {Tube("Scope optical barrel",g,M("Dark"),V(0,.10f,z),.028f,length,.72f);for(int i=0;i<2;i++){float at=z+.035f+i*(length-.07f);Tube("Scope machined clamp",g,M("Steel"),V(0,.10f,at),.032f,.013f,.87f);Rod("Scope mount",g,M("Steel"),V(0,.031f,at),V(0,.085f,at),.012f);}Tube("Teal objective glass",g,M("TealGlass"),V(0,.10f,z+length-.003f),.020f,.003f,.015f);}
        internal static void Grip(Transform g,bool wood=false,float z=-.025f)
        {Profile("Contoured pistol grip",g,M(wood?"Wood":"LeatherDark"),.046f,P(z-.035f,-.02f),P(z+.025f,-.032f),P(z+.004f,-.139f),P(z-.055f,-.13f));for(int i=0;i<7;i++)Rod("Grip checkered rib",g,M("Leather"),V(.024f,-.055f-i*.009f,z-.034f),V(.024f,-.057f-i*.009f,z+.008f),.001f);}
        internal static void Guard(Transform g,float z=0)
        {Line("Open trigger guard",g,M("Steel"),.0035f,V(0,-.035f,z+.045f),V(0,-.093f,z+.041f),V(0,-.104f,z-.013f),V(0,-.047f,z-.035f));Line("Curved trigger",g,M("Steel"),.0025f,V(0,-.036f,z+.005f),V(0,-.067f,z+.011f),V(0,-.079f,z+.002f));}
        static void Stock(Transform g,float rear=-.35f,bool wood=true,bool skeletal=false)
        {
            if(skeletal){Line("Folding steel stock upper rail",g,M("Steel"),.010f,V(0,.014f,-.08f),V(0,.035f,rear),V(0,-.090f,rear));Rod("Folding stock lower rail",g,M("Steel"),V(0,-.06f,rear),V(0,-.02f,-.08f),.007f);return;}
            Profile("Fitted shoulder stock",g,M(wood?"Wood":"Dark"),.061f,P(rear,-.10f),P(rear,.045f),P(rear+.09f,.040f),P(-.075f,.005f),P(-.075f,-.036f),P(rear+.10f,-.063f));
            Profile("Rubber shoulder pad",g,M("Dark"),.065f,P(rear-.010f,-.104f),P(rear-.010f,.048f),P(rear+.005f,.048f),P(rear+.005f,-.102f));
        }
        static void Magazine(Transform g,float z,float length,bool curved=false)
        {Profile(curved?"Curved detachable magazine":"Straight box magazine",g,M("Steel"),.054f,P(z,-.035f),P(z+.065f,-.035f),P(z+.074f,-length*.6f),P(z+(curved?.045f:.070f),-length),P(z-.018f,-length+.006f),P(z+.005f,-length*.6f));for(int i=0;i<3;i++)Rod("Pressed magazine rib",g,M("Dark"),V(.028f,-.061f,z+.01f+i*.016f),V(.028f,-length+.02f,z+.01f+i*.016f),.0015f);}
        static void Receiver(Transform g,float front,float width=.078f)
        {Profile("Beveled steel receiver",g,M("Steel"),width,P(-.10f,-.040f),P(-.10f,.025f),P(front-.035f,.033f),P(front,.010f),P(front,-.042f));Profile("Recessed ejection port",g,M("Dark"),width+.001f,P(.005f,.009f),P(.005f,.026f),P(.078f,.026f),P(.078f,.009f));Fasteners(g,-.07f,front-.02f,width*.51f,-.015f,5);Rod("Charging handle",g,M("Steel"),V(width*.5f,.008f,.01f),V(width*.85f,.008f,.01f),.005f);}
        static void Handguard(Transform g,float start,float end,bool pump=false)
        {Profile(pump?"Ribbed sliding pump":"Sculpted ventilated fore-end",g,M(pump?"Wood":"Dark"),.078f,P(start,-.055f),P(start,.019f),P(end,.015f),P(end,-.044f));for(int i=0;i<8;i++){float z=Mathf.Lerp(start+.015f,end-.015f,i/7f);for(int s=-1;s<=1;s+=2)Rod(pump?"Pump grip rib":"Handguard vent",g,M(pump?"LeatherDark":"Steel"),V(s*.040f,-.036f,z),V(s*.040f,.005f,z),.002f);}}
        internal static DetailedHeroEquipment3D.WeaponSockets Finish(Transform g,Vector3 trigger,Vector3 support,Vector3 stock,Vector3 muzzle)
        {Compact(g);return new DetailedHeroEquipment3D.WeaponSockets{root=g,trigger=G("Trigger grip socket",g,trigger),support=G("Support grip socket",g,support),stock=G("Shoulder stock socket",g,stock),muzzle=G("True +Z firing axis",g,muzzle)};}
        public static DetailedHeroEquipment3D.WeaponSockets BuildWeapon(Transform parent,int model,string visualSet)
        {
            model=Mathf.Clamp(model,0,Arsenal.ModelCount-1);
            if(visualSet==Arsenal.TrailWardenVisualSet&&(model==12||model==15))return model==15?DetailedHeroEquipment3D.BuildPistol(parent,M):DetailedHeroEquipment3D.BuildRifle(parent,M);
            var g=G("Inventory 3D / "+Arsenal.ModelNames[model],parent,Vector3.zero);
            if(model>=14)return Pistol(g,model);
            return PortraitLongGun(g,model);
        }
        static void PerforatedShroud(Transform g,float start,float end,float radius)
        {
            const int Columns=12,Steps=12;int rows=Mathf.Max(2,Mathf.RoundToInt((end-start)/.043f));var v=new List<Vector3>();var ix=new List<int>();
            for(int row=0;row<rows;row++)for(int column=0;column<Columns;column++){
                float ac=(column+.5f)*Mathf.PI*2/Columns,zc=Mathf.Lerp(start,end,(row+.5f)/rows),ha=Mathf.PI/Columns,hz=(end-start)/rows*.5f;
                int begin=v.Count;
                for(int ring=0;ring<2;ring++)for(int j=0;j<=Steps;j++){float phi=j*Mathf.PI*2/Steps,c=Mathf.Cos(phi),sn=Mathf.Sin(phi),scale=ring==0?1/Mathf.Max(Mathf.Abs(c),Mathf.Abs(sn)):.53f;float a=ac+c*ha*scale,z=zc+sn*hz*scale;v.Add(V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,z));}
                for(int j=0;j<Steps;j++){int q=begin+j,b=q+Steps+1;ix.AddRange(new[]{q,q+1,b+1,q,b+1,b});}
            }MeshPart("Actual perforated portrait barrel jacket",g,M("Steel"),v,ix);Tube("Shroud rear rolled collar",g,M("Dark"),V(0,0,start-.005f),radius+.002f,.012f,.86f);Tube("Shroud muzzle rolled collar",g,M("Dark"),V(0,0,end-.005f),radius+.002f,.012f,.86f);
        }
        static void CanvasStock(Transform g,float rear)
        {
            for(int i=0;i<4;i++){var t=Tube("Canvas wrapped wooden buttstock",g,WM("Canvas"),Vector3.zero,.034f,.021f,.96f,.034f,32);t.localPosition=V(0,-.027f,rear+.021f+i*.018f);t.localScale=V(1,1.95f,1);t.localRotation=Quaternion.Euler(0,0,Mathf.Sin(i*2f)*5);}
        }
        static void Gauge(Transform p,Vector3 c,Material glass)
        {
            var g=G("Side pressure gauge",p,c);g.localRotation=Quaternion.Euler(0,90,0);Tube("Gauge rim",g,M("Steel"),Vector3.zero,.026f,.009f,.80f);Tube("Gauge dial face",g,glass,V(0,0,.008f),.020f,.002f,.01f);Rod("Gauge dial needle",g,M("Dark"),V(0,0,.011f),V(.012f,.009f,.011f),.001f);for(int i=0;i<7;i++){float a=i*Mathf.PI/4;Rod("Gauge dial graduation",g,M("Brass"),V(Mathf.Cos(a)*.015f,Mathf.Sin(a)*.015f,.011f),V(Mathf.Cos(a)*.018f,Mathf.Sin(a)*.018f,.011f),.0006f);}
        }
        static DetailedHeroEquipment3D.WeaponSockets PortraitLongGun(Transform g,int model)
        {
            float rear=model==5?-.23f:-.36f,muzzle=model==2?.87f:model==7?.73f:model==8?.79f:model==9?.76f:model==10?.64f:model==11?.72f:model==5?.39f:.66f;
            Receiver(g,.17f,model==6?.096f:.078f);Grip(g,model!=12);Guard(g);Stock(g,rear,model!=12,false);
            if(model!=6&&model!=12)CanvasStock(g,rear);
            if(model==0||model==1||model==3||model==6){for(int side=-1;side<=1;side+=2){var plate=Profile("Original red side receiver panel",g,WM("RedMetal"),.002f,P(-.075f,-.027f),P(-.075f,.018f),P(.14f,.020f),P(.14f,-.029f));plate.localPosition=V(side*.041f,0,0);}}
            float support=.22f;
            switch(model){
                case 0: // Scope, curved magazine, cream wraps, long drilled shroud.
                    Tube("Dust Runner inner barrel",g,M("Dark"),V(0,0,.16f),.018f,.50f);PerforatedShroud(g,.25f,.64f,.031f);Magazine(g,.045f,.21f,true);Scope(g,-.055f,.19f);for(int i=0;i<3;i++)Tube("Cream handguard binding",g,WM("Canvas"),V(0,0,.185f+i*.019f),.038f,.017f,.95f);break;
                case 1: // Same salvaged wooden family, short straight magazine and two orange top tubes.
                    Tube("SMG inner barrel",g,M("Dark"),V(0,0,.16f),.017f,.35f);PerforatedShroud(g,.25f,.49f,.031f);Magazine(g,.044f,.26f);for(int i=0;i<3;i++)Tube("SMG canvas foregrip binding",g,WM("Canvas"),V(0,0,.185f+i*.019f),.036f,.017f,.96f);for(int side=-1;side<=1;side+=2){Tube("Twin orange top pressure pipes",g,M("AmberGlass"),V(side*.019f,.053f,-.015f),.011f,.16f,.65f);for(int end=0;end<2;end++)Tube("SMG top pipe collar",g,M("Steel"),V(side*.019f,.053f,-.015f+end*.145f),.013f,.012f,.80f);}Sights(g,-.065f,.47f,.062f);muzzle=.51f;break;
                case 2:
                    Tube("Long Watch long exposed barrel",g,M("Steel"),V(0,0,.20f),.015f,.67f);Handguard(g,.15f,.36f,true);for(int i=0;i<7;i++)Tube("Sniper cream fore-end wrapping",g,WM("Canvas"),V(0,0,.17f+i*.023f),.034f,.020f,.96f);Scope(g,-.075f,.29f);Tube("Sniper amber objective",g,M("AmberGlass"),V(0,.10f,.214f),.020f,.004f,.02f);Tube("Sniper muzzle brake",g,M("Steel"),V(0,0,.80f),.025f,.07f,.60f);for(int i=0;i<3;i++)for(int side=-1;side<=1;side+=2){var port=Tube("Brake side exhaust port",g,M("Dark"),Vector3.zero,.004f,.002f,.03f,-1,10);port.localPosition=V(side*.025f,0,.815f+i*.017f);port.localRotation=Quaternion.LookRotation(Vector3.right*side);}break;
                case 3:
                case 13:
                    Tube("Pump shotgun upper barrel",g,M("Steel"),V(0,0,.15f),.025f,.51f,.65f);Tube("Pump shotgun lower shell tube",g,M("Steel"),V(0,-.052f,.15f),.022f,.46f,.04f);Handguard(g,.25f,.43f,true);if(model==3)PerforatedShroud(g,.21f,.58f,.032f);Sights(g,-.06f,.61f,.055f);break;
                case 4:
                    Tube("Drum shotgun barrel",g,M("Dark"),V(0,0,.16f),.023f,.50f);PerforatedShroud(g,.24f,.64f,.036f);var drum=Tube("Large olive drum magazine",g,WM("TanPolymer"),Vector3.zero,.10f,.074f,.02f);drum.localPosition=V(-.037f,-.126f,.067f);drum.localRotation=Quaternion.Euler(0,90,0);for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;var shell=G("Red shotgun shell drum belt",g,V(-.042f,Mathf.Cos(a)*.104f-.126f,Mathf.Sin(a)*.104f+.067f));shell.localRotation=Quaternion.Euler(0,90,0);Tube("Red shell hull",shell,WM("RedMetal"),Vector3.zero,.007f,.044f,.04f);Tube("Shell brass base",shell,M("Brass"),V(0,0,.039f),.0075f,.007f,.03f);}Rod("Wood vertical support grip",g,M("Wood"),V(0,-.035f,.29f),V(0,-.14f,.29f),.024f);var fg=G("Drum shotgun wrapped support grip",g,V(0,0,.29f));for(int i=0;i<5;i++)Band("Foregrip cream binding",fg,WM("Canvas"),-.07f-i*.013f,.026f,.027f,.012f);Sights(g,-.06f,.61f);break;
                case 5:
                    for(int side=-1;side<=1;side+=2)Tube("Twin sawed upper barrel",g,M("Steel"),V(side*.025f,0,.045f),.024f,.345f,.72f);Tube("Third lower breakaction barrel",g,M("Steel"),V(0,-.049f,.045f),.022f,.345f,.67f);Handguard(g,.12f,.28f,true);for(int i=0;i<3;i++)Tube("Sawed fore-end cream binding",g,WM("Canvas"),V(0,-.050f,.15f+i*.02f),.026f,.019f,.95f);ReceiverAccent(g,WM("RedMetal"));Sights(g,-.05f,.33f,.045f);support=.17f;break;
                case 6:
                    Tube("Belt-fed machinegun barrel",g,M("Dark"),V(0,0,.16f),.021f,.50f);PerforatedShroud(g,.28f,.64f,.035f);var box=Profile("Olive belt ammunition box",g,WM("TanPolymer"),.12f,P(.015f,-.055f),P(.16f,-.055f),P(.16f,-.225f),P(.015f,-.225f));box.localPosition=V(-.053f,0,0);for(int i=0;i<14;i++){float t=i/13f;Vector3 q=V(-.057f-.095f*Mathf.Sin(t*Mathf.PI),.025f-t*.22f,.065f);Rod("Hanging linked brass cartridge",g,M("Brass"),q,q+V(0,0,.058f),.005f);Rod("Ammunition belt link",g,M("Steel"),q+V(0,0,.02f),q+V(0,-.014f,.02f),.002f);}for(int side=-1;side<=1;side+=2){Line("Splayed bipod",g,M("Steel"),.008f,V(side*.020f,-.01f,.43f),V(side*.10f,-.22f,.50f),V(side*.115f,-.22f,.51f));}Scope(g,-.05f,.13f);break;
                case 7:
                    Tube("Needle large green reagent reservoir",g,WM("GreenGlass"),V(0,0,.20f),.050f,.25f,.62f);for(int end=0;end<2;end++)Tube("Needle reservoir steel endcap",g,M("Steel"),V(0,0,.18f+end*.25f),.059f,.026f,.70f);for(int side=-1;side<=1;side+=2)Rod("Reagent tank copper support rail",g,WM("Copper"),V(side*.047f,.035f,.19f),V(side*.047f,.035f,.47f),.004f);Tube("Needle taper collar",g,M("Steel"),V(0,0,.46f),.035f,.11f,.45f,.012f);Tube("Long injection needle",g,M("Steel"),V(0,0,.55f),.006f,.18f,.25f,.0015f);Gauge(g,V(.055f,.025f,.15f),WM("Canvas"));Rod("Needle under-tank support grip",g,M("Dark"),V(0,-.025f,.15f),V(0,-.13f,.15f),.025f);break;
                case 8:
                case 9:
                case 10:
                case 11:PortraitEnergy(g,model,ref muzzle);break;
                case 12:
                    Tube("Modern field carbine barrel",g,M("Steel"),V(0,0,.26f),.017f,.40f);var hg=Profile("Tan ventilated modern handguard",g,WM("TanPolymer"),.079f,P(.16f,-.045f),P(.16f,.032f),P(.46f,.029f),P(.48f,-.037f));for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)Rod("Modern rectangular handguard vent",g,M("Dark"),V(side*.041f,-.019f,.20f+i*.047f),V(side*.041f,.010f,.20f+i*.047f),.004f);Magazine(g,.045f,.22f,true);Scope(g,-.065f,.13f);for(int i=0;i<2;i++){var cell=G("Carbine upright amber battery",g,V(-.027f+i*.025f,.05f,-.045f));cell.localRotation=Quaternion.Euler(-90,0,0);Tube("Amber battery glass",cell,M("AmberGlass"),Vector3.zero,.010f,.06f,.65f);}var stock=Profile("Tan telescopic shoulder stock cover",g,WM("TanPolymer"),.069f,P(-.36f,-.095f),P(-.36f,.040f),P(-.18f,.035f),P(-.18f,-.045f),P(-.27f,-.09f));for(int i=0;i<9;i++)Rod("Modern upper rail tooth",g,M("Steel"),V(-.018f,.040f,.0f+i*.045f),V(.018f,.040f,.0f+i*.045f),.0028f);Sights(g,-.055f,.60f);break;
            }
            return Finish(g,V(0,-.079f,-.025f),V(0,-.064f,support),V(0,-.01f,rear-.006f),V(0,0,muzzle));
        }
        static void ReceiverAccent(Transform g,Material material)
        {for(int side=-1;side<=1;side+=2){var t=Profile("Portrait painted receiver inset",g,material,.002f,P(-.065f,-.027f),P(-.065f,.020f),P(.12f,.020f),P(.12f,-.027f));t.localPosition=V(side*.0405f,0,0);}}
        static void PortraitEnergy(Transform g,int model,ref float muzzle)
        {
            ReceiverAccent(g,WM("RedMetal"));
            if(model==8){Tube("Continuous copper induction core",g,M("Dark"),V(0,0,.18f),.031f,.52f);for(int i=0;i<11;i++)Tube("Dense copper induction winding",g,WM("Copper"),V(0,0,.20f+i*.027f),.052f,.019f,.78f);Tube("Cyan collimator glass",g,M("TealGlass"),V(0,0,.49f),.047f,.19f,.68f,.029f);for(int z=0;z<3;z++)Tube("Collimator steel retaining cage",g,M("Steel"),V(0,0,.49f+z*.063f),.051f-z*.006f,.014f,.85f);Tube("Coil Lance copper muzzle",g,WM("Copper"),V(0,0,.68f),.026f,.070f,.55f,.019f);Scope(g,-.07f,.16f);Gauge(g,V(.049f,.017f,.045f),M("TealGlass"));muzzle=.75f;}
            if(model==9){Tube("Green split-fork reactor glass",g,WM("GreenGlass"),V(0,0,.18f),.047f,.235f,.65f);for(int end=0;end<2;end++)Tube("Reactor end bearing",g,M("Steel"),V(0,0,.17f+end*.23f),.060f,.024f,.77f);for(int side=-1;side<=1;side+=2){var fork=G("Wide black split induction jaw",g,V(side*.048f,0,0));Profile("Fork rounded long silhouette",fork,M("Steel"),.035f,P(.41f,-.048f),P(.41f,.048f),P(.69f,.064f),P(.76f,.025f),P(.75f,-.015f),P(.68f,-.056f));Profile("Jaw red retaining saddle",fork,WM("RedMetal"),.038f,P(.45f,-.049f),P(.45f,.049f),P(.48f,.049f),P(.48f,-.049f));Rod("Fork external insulation rail",g,M("Dark"),V(side*.045f,.067f,.02f),V(side*.045f,.067f,.53f),.010f);}Scope(g,-.07f,.15f);muzzle=.76f;}
            if(model==10){Tube("Huge isotope drum body",g,WM("BlueSteel"),V(0,0,.20f),.115f,.30f,.89f,-1,48);for(int end=0;end<2;end++)Tube("Drum broad steel band",g,M("Steel"),V(0,0,.21f+end*.24f),.118f,.028f,.92f);Tube("Copper circular launch face",g,WM("Copper"),V(0,0,.49f),.118f,.016f,.90f,-1,48);for(int drone=0;drone<3;drone++){float a=drone*Mathf.PI*2/3+Mathf.PI*.5f;Vector3 q=V(Mathf.Cos(a)*.062f,Mathf.Sin(a)*.062f,.50f);Tube("Three green launch chamber eyes",g,WM("GreenGlass"),q,.031f,.009f,.03f);Tube("Green eye copper ring",g,WM("Copper"),q,.035f,.012f,.87f);var top=G("Top mounted isotope drone",g,V((drone-1)*.075f,.14f,.27f));Tube("Drone round body",top,M("Dark"),Vector3.zero,.035f,.070f,.04f);Tube("Drone green forward sensor",top,WM("GreenGlass"),V(0,0,.069f),.023f,.006f,.04f);var rotor=Tube("Drone top rotor housing",top,WM("Copper"),Vector3.zero,.044f,.006f,.60f);rotor.localPosition=V(0,.039f,.030f);rotor.localRotation=Quaternion.Euler(90,0,0);}for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Rod("Circular launch face cage",g,WM("Copper"),V(Mathf.Cos(a)*.02f,Mathf.Sin(a)*.02f,.517f),V(Mathf.Cos(a)*.111f,Mathf.Sin(a)*.111f,.517f),.002f);}muzzle=.518f;}
            if(model==11){Tube("Vacuum cyan central reactor",g,M("TealGlass"),V(0,0,.18f),.060f,.28f,.68f);for(int end=0;end<2;end++)Tube("Reactor steel retaining collar",g,M("Steel"),V(0,0,.18f+end*.25f),.066f,.030f,.85f);for(int side=-1;side<=1;side+=2){Line("Looped corrugated vacuum hose",g,M("Dark"),.011f,V(side*.06f,.05f,.10f),V(side*.09f,.09f,.22f),V(side*.09f,.08f,.40f),V(side*.055f,.018f,.49f));for(int i=0;i<7;i++)Tube("Hose copper connector bands",g,WM("Copper"),V(side*.09f,.08f,.22f+i*.025f),.013f,.005f,.80f);Rod("Reactor copper tie rod",g,WM("Copper"),V(side*.047f,-.04f,.18f),V(side*.047f,-.04f,.46f),.004f);}Tube("Wide steel vacuum trumpet",g,M("Steel"),V(0,0,.46f),.038f,.24f,.92f,.125f,48);Tube("Rolled vacuum intake lip",g,M("Steel"),V(0,0,.695f),.128f,.011f,.95f,-1,48);Tube("Recessed green vacuum core",g,WM("GreenGlass"),V(0,0,.52f),.023f,.005f,.03f);muzzle=.706f;}
        }
        static DetailedHeroEquipment3D.WeaponSockets Pistol(Transform g,int model)
        {
            float front=model==14?.172f:model==16?.257f:model==17?.30f:.18f;float slideFront=model==17?.17f:front;
            Grip(g,model==14||model==16);Guard(g);
            if(model==14){Receiver(g,.067f,.055f);Tube("Six chamber revolver cylinder",g,M("Steel"),V(0,0,.018f),.038f,.070f,.03f);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Tube("Revolver visible chamber bore",g,M("Dark"),V(Mathf.Cos(a)*.025f,Mathf.Sin(a)*.025f,.087f),.006f,.002f,.03f,-1,12);Rod("Cylinder fluting",g,M("Dark"),V(Mathf.Cos(a)*.037f,Mathf.Sin(a)*.037f,.024f),V(Mathf.Cos(a)*.037f,Mathf.Sin(a)*.037f,.078f),.002f);}Tube("Revolver long barrel",g,M("Steel"),V(0,0,.087f),.017f,.085f,.55f);Line("Exposed revolver hammer",g,M("Steel"),.005f,V(0,.020f,-.05f),V(0,.048f,-.069f),V(0,.051f,-.081f));}
            else {Profile(model==16?"Long steel 1911 slide":"Service blowback slide",g,M("Steel"),model==16?.052f:.046f,P(-.07f,-.015f),P(-.07f,.030f),P(slideFront-.03f,.030f),P(slideFront,.010f),P(slideFront,-.020f));for(int s=-1;s<=1;s+=2)for(int i=0;i<7;i++)Rod("Pistol rear cocking serration",g,M("Dark"),V(s*(model==16?.027f:.024f),-.010f,-.058f+i*.007f),V(s*(model==16?.027f:.024f),.022f,-.058f+i*.007f),.0013f);Magazine(g,-.035f,.13f);Tube("Pistol rifled crown",g,M("Steel"),V(0,0,slideFront-.014f),model==16?.019f:.012f,.014f,.55f);}
            if(model==16){Profile("1911 wooden inset grip scale",g,M("Wood"),.048f,P(-.072f,-.045f),P(-.02f,-.045f),P(-.033f,-.132f),P(-.085f,-.126f));Rod("1911 ivory rear grip strip",g,WM("Canvas"),V(0,-.050f,-.087f),V(0,-.125f,-.094f),.006f);Line("1911 cocked hammer",g,M("Steel"),.004f,V(0,.02f,-.06f),V(0,.044f,-.078f),V(0,.039f,-.091f));Fasteners(g,-.072f,-.040f,.026f,-.074f,2);}
            if(model==17){AngularPlate("Compact reflex optic housing",g,M("Dark"),V(0,.064f,-.035f),new Vector2(.035f,.038f),.006f);Panel("Cyan reflex sight window",g,M("TealGlass"),V(0,.066f,-.027f),new Vector2(.023f,.023f),.002f);Tube("Threaded compact suppressor",g,M("Dark"),V(0,0,.17f),.027f,.13f,.36f);for(int i=0;i<4;i++)Tube("Suppressor machined baffle collar",g,M("Steel"),V(0,0,.18f+i*.030f),.028f,.004f,.94f);}
            Sights(g,-.055f,model==17?.15f:front-.025f,.043f);Fasteners(g,-.05f,.02f,model==16?.028f:.025f,-.020f,3);
            return Finish(g,V(0,-.075f,-.035f),V(-.022f,-.078f,-.020f),V(0,-.075f,-.055f),V(0,0,front));
        }
        public sealed class ArmorVisuals : MonoBehaviour
        {
            DetailedHero3D body;DetailedHeroEquipment3D.EquipmentRig baseline;
            readonly List<Transform>[] roots={new List<Transform>(),new List<Transform>(),new List<Transform>(),new List<Transform>(),new List<Transform>()};
            readonly List<Material>[] owned={new List<Material>(),new List<Material>(),new List<Material>(),new List<Material>(),new List<Material>()};
            readonly string[] keys=new string[5];
            public int AppliedCount {get;private set;}
            internal void Init(DetailedHero3D value){body=value;baseline=DetailedHeroEquipment3D.Build(body,M);for(int i=0;i<5;i++)baseline.SetSlot((ArmorSlot)i,false);}
            public void SetItems(ArmorItem[] items)
            {
                var selected=new ArmorItem[5];if(items!=null)foreach(var a in items)if(a!=null&&(int)a.slot>=0&&(int)a.slot<5)selected[(int)a.slot]=a;
                SetHairVisible(selected[0]==null||BespokeLegendaryDesigns.Index(selected[0])==20||(DistinctEquipmentDesigns.For(selected[0])==null&&selected[0].visualVariant==4));
                bool replacementChest=selected[1]!=null&&DistinctEquipmentDesigns.For(selected[1])==null&&selected[1].visualVariant!=4;
                bool replacementArms=selected[2]!=null&&DistinctEquipmentDesigns.For(selected[2])==null&&selected[2].visualVariant!=4;
                body.SetBaseJacketVisible(!replacementChest,!replacementChest,!replacementArms);
                body.SetBaseTrouserVisible(selected[3]==null||DistinctEquipmentDesigns.For(selected[3])!=null||selected[3].visualVariant==4);
                if(selected[3]==null||DistinctEquipmentDesigns.For(selected[3])!=null||selected[3].visualVariant==4)body.SetSeatFabric(null);
                AppliedCount=0;for(int slot=0;slot<5;slot++){
                    var item=selected[slot];if(item!=null)AppliedCount++;
                    string key=item==null?"empty":item.catalogId+"/"+item.visualVariant;
                    if(keys[slot]==key)continue;keys[slot]=key;foreach(var t in roots[slot])Release(t);roots[slot].Clear();foreach(var m in owned[slot])if(m)UnityEngine.Object.Destroy(m);owned[slot].Clear();baseline.SetSlot((ArmorSlot)slot,false);
                    if(item==null)continue;
                    // The menu-reference set uses its individually authored cloth, harness and respirator rig.
                    if(BespokeLegendaryDesigns.Index(item)>=20&&BespokeLegendaryDesigns.Index(item)<=24){baseline.SetSlot((ArmorSlot)slot,true);continue;}
                    if(DistinctEquipmentDesigns.For(item)!=null)continue;
                    if(Mathf.Clamp(item.visualVariant,0,4)==4){baseline.SetSlot((ArmorSlot)slot,true);continue;}
                    var palette=new Dictionary<string,Material>();
                    Material Local(string materialKey)
                    {
                        if(palette.TryGetValue(materialKey,out var existing))return existing;
                        int style=Mathf.Clamp(item.visualVariant,0,3);bool navy=style==1||style==3;
                        string sourceKey=materialKey=="Accent"?"Cloth":materialKey=="Trim"?"Brass":materialKey=="Pad"?"Dark":materialKey;
                        var source=M(sourceKey);var copy=new Material(source){name="Portrait style "+style+" / "+materialKey};
                        Color color=source.color;
                        if(materialKey=="Cloth")color=navy?new Color(.085f,.115f,.16f):new Color(.30f,.30f,.19f);
                        if(materialKey=="ClothDark")color=navy?new Color(.055f,.075f,.115f):new Color(.23f,.23f,.14f);
                        if(materialKey=="Accent")color=navy?new Color(.39f,.055f,.055f):style==2?new Color(.50f,.43f,.29f):new Color(.30f,.30f,.19f);
                        if(materialKey=="Steel")color=style==0?new Color(.20f,.28f,.33f):style==1?new Color(.19f,.23f,.30f):style==2?new Color(.43f,.35f,.23f):new Color(.15f,.18f,.22f);
                        if(materialKey=="Pad")color=new Color(.11f,.13f,.15f);
                        if(materialKey=="Leather")color=style==3?new Color(.10f,.105f,.115f):new Color(.25f,.17f,.105f);
                        if(materialKey=="Trim")color=style==3?new Color(.37f,.39f,.40f):new Color(.38f,.30f,.17f);
                        copy.color=color;owned[slot].Add(copy);palette.Add(materialKey,copy);return copy;
                    }
                    BuildArmor(body,item,roots[slot],Local);foreach(var t in roots[slot])Compact(t);
                }
            }
            void SetHairVisible(bool visible)
            {
                if(!body||!body.head)return;
                foreach(var renderer in body.head.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string label=renderer.gameObject.name;
                    bool hair=label.IndexOf("hair",StringComparison.OrdinalIgnoreCase)>=0||label.IndexOf("scalp",StringComparison.OrdinalIgnoreCase)>=0;
                    if(!hair)foreach(var material in renderer.sharedMaterials)
                        if(material&&(material==M("Hair")||material.name.IndexOf("hair",StringComparison.OrdinalIgnoreCase)>=0)){hair=true;break;}
                    if(hair)renderer.enabled=visible;
                }
            }
            void OnDestroy(){for(int slot=0;slot<5;slot++){foreach(var t in roots[slot])Release(t);foreach(var m in owned[slot])if(m)UnityEngine.Object.Destroy(m);}if(baseline!=null)foreach(var list in baseline.slots)foreach(var t in list)Release(t);}
        }
        public static ArmorVisuals ApplyArmor(DetailedHero3D body,ArmorItem[] armor)
        {if(!body)throw new ArgumentNullException(nameof(body));var visual=body.GetComponent<ArmorVisuals>();if(!visual){visual=body.gameObject.AddComponent<ArmorVisuals>();visual.Init(body);}visual.SetItems(armor);return visual;}
        static void Strap(Transform p,Func<string,Material> mat,Vector3 a,Vector3 b,float width=.025f)
        {
            Vector3 side=Vector3.Cross((b-a).normalized,Vector3.forward)*width*.5f;var v=new List<Vector3>{a-side,b-side,b+side,a+side};MeshPart("Fitted leather load strap",p,mat("Leather"),v,new List<int>{0,1,2,0,2,3});for(int s=-1;s<=1;s+=2)for(int i=0;i<10;i++){float u=i/10f;Vector3 q=Vector3.Lerp(a,b,u)+side*s*.75f+Vector3.forward*.001f;Rod("Harness saddle stitching",p,M("Stitch"),q,q+(b-a)*.035f,.0009f);}
        }
        static void Buckle(Transform p,Vector3 c,float width=.032f,float height=.037f)
        {Line("Rectangular steel buckle",p,M("Brass"),.0025f,c+V(-width/2,-height/2,0),c+V(width/2,-height/2,0),c+V(width/2,height/2,0),c+V(-width/2,height/2,0),c+V(-width/2,-height/2,0));Rod("Buckle prong",p,M("Steel"),c+V(0,-height/2,0),c+V(0,height/2,0),.0016f);}
        static void PatchPouch(Transform p,Func<string,Material> mat,Vector3 c,float width=.073f,float height=.11f)
        {
            Shell("Convex leather pocket",p,mat("Leather"),c,new Vector2(width,height),.029f);
            Shell("Rounded stitched flap",p,mat("Leather"),c+V(0,height*.25f,.023f),new Vector2(width*.92f,height*.40f),.007f);
            for(int s=-1;s<=1;s+=2)for(int i=0;i<10;i++){Vector3 q=c+V(s*width*.39f,-height*.35f+i*height*.069f,.022f);Rod("Pouch hand stitching",p,M("Stitch"),q,q+V(0,height*.027f,0),.0009f);}
            Strap(p,mat,c+V(0,height*.30f,.033f),c+V(0,-height*.12f,.033f),.014f);Buckle(p,c+V(0,-height*.025f,.035f),.019f,.022f);
        }
        // Portrait contours are authored independently from rarity and item-roll colors.
        static void AngularPlate(string name,Transform p,Material material,Vector3 c,Vector2 size,float depth,bool shield=false)
        {
            Vector2[] outline=shield?new[]{new Vector2(-.35f,.50f),new Vector2(.35f,.50f),new Vector2(.5f,.20f),new Vector2(.35f,-.22f),new Vector2(0,-.50f),new Vector2(-.35f,-.22f),new Vector2(-.5f,.20f)}:new[]{new Vector2(-.33f,.5f),new Vector2(.33f,.5f),new Vector2(.5f,.32f),new Vector2(.45f,-.35f),new Vector2(.25f,-.5f),new Vector2(-.25f,-.5f),new Vector2(-.45f,-.35f),new Vector2(-.5f,.32f)};
            var v=new List<Vector3>{c+V(0,0,depth)};var ix=new List<int>();foreach(var q in outline)v.Add(c+V(q.x*size.x,q.y*size.y,depth*.28f));for(int i=0;i<outline.Length;i++)ix.AddRange(new[]{0,1+(i+1)%outline.Length,1+i});MeshPart(name,p,material,v,ix);
            for(int i=0;i<outline.Length;i++){Vector3 a=v[i+1],b=v[(i+1)%outline.Length+1];Rod("Angular folded plate rim",p,M("Brass"),a,b,.0017f);if(i%2==0)Tube("Flush armor rivet",p,M("Steel"),Vector3.Lerp(a,b,.22f)+V(0,0,.001f),.0025f,.002f,.03f,-1,10);}
        }
        static void ClothLoft(string name,Transform p,Material material,float[] heights,float[] xs,float[] zs,Vector3 origin)
        {
            const int N=80,Steps=12;int rows=(heights.Length-1)*Steps;var v=new List<Vector3>();var ix=new List<int>();
            bool garment=name.Contains("jacket")||name.Contains("sleeve")||name.Contains("trouser");
            for(int row=0;row<=rows;row++)for(int i=0;i<=N;i++)
            {
                int ring=Mathf.Min(heights.Length-2,row/Steps);float t=(row-ring*Steps)/(float)Steps;t=t*t*(3-2*t);
                float y=Mathf.Lerp(heights[ring],heights[ring+1],t),rx=Mathf.Lerp(xs[ring],xs[ring+1],t),rz=Mathf.Lerp(zs[ring],zs[ring+1],t);
                float a=i*Mathf.PI*2/N,u=row/(float)rows,fold=0;
                if(garment)
                {
                    float front=.3f+.7f*Mathf.Pow(Mathf.Sin(a),2);
                    float d=(u-.22f-.11f*Mathf.Cos(a))/.035f;
                    float e=(u-.45f+.13f*Mathf.Cos(a))/.055f;
                    fold=front*(.010f*(Mathf.Exp(-d*d)-.4f*Mathf.Exp(-(d-1.5f)*(d-1.5f)))+.006f*(Mathf.Exp(-e*e)-.4f*Mathf.Exp(-(e-1.5f)*(e-1.5f))));
                }
                v.Add(origin+V(Mathf.Cos(a)*(rx+fold),y,Mathf.Sin(a)*(rz+fold)));
                if(row<rows&&i<N){int k=row*(N+1)+i,b=k+N+1;ix.AddRange(new[]{k,b,k+1,k+1,b,b+1});}
            }
            // Closed garment ends prevent hollow shoulders when the undergarment is hidden.
            if(garment){int bottom=v.Count;v.Add(origin+V(0,heights[0],0));int top=v.Count;v.Add(origin+V(0,heights[heights.Length-1],0));for(int i=0;i<N;i++)ix.AddRange(new[]{bottom,i,i+1,top,rows*(N+1)+i+1,rows*(N+1)+i});}
            MeshPart(name,p,material,v,ix);
        }
        static void ShoulderCrown(Transform p,Material material,float start)
        {
            const int Rows=12,N=40;var v=new List<Vector3>();var ix=new List<int>();
            for(int row=0;row<=Rows;row++)for(int i=0;i<=N;i++)
            {float t=row/(float)Rows,a=start+i/(float)N*Mathf.PI*.85f,r=Mathf.Lerp(0,.097f,t),y=.0075f+.042f*Mathf.Cos(t*Mathf.PI*.5f);v.Add(V(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));if(row<Rows&&i<N){int k=row*(N+1)+i,b=k+N+1;ix.AddRange(new[]{k,k+1,b,k+1,b+1,b});}}
            MeshPart("Sculpted shoulder crown closing upper shell",p,material,v,ix);
        }
        static void PortraitScarf(Transform p,Func<string,Material> mat,bool red)
        {
            Material cloth=mat(red?"Accent":"Cloth");
            ClothLoft("Layered portrait scarf neck",p,cloth,new[]{.17f,.185f,.21f,.235f,.257f},new[]{.108f,.115f,.105f,.109f,.096f},new[]{.096f,.103f,.094f,.098f,.086f},V(0,0,.01f));
            for(int fold=0;fold<4;fold++){Vector3 previous=Vector3.zero;for(int i=0;i<=28;i++){float u=i/28f;Vector3 q=V((u-.5f)*(.21f-fold*.018f),.20f-fold*.031f+Mathf.Abs(u-.5f)*.075f,.119f+fold*.009f+Mathf.Sin(u*Mathf.PI)*.004f);if(i>0)Rod("Scarf overlapping chevron fold",p,cloth,previous,q,.006f);previous=q;}}
            if(red){Strap(p,key=>key=="Leather"?mat("Accent"):mat(key),V(.16f,.13f,.16f),V(-.13f,-.15f,.178f),.052f);Strap(p,key=>key=="Leather"?mat("Accent"):mat(key),V(.17f,.09f,.163f),V(-.10f,-.17f,.18f),.022f);}
        }
        static void PortraitHead(Transform g,Func<string,Material> mat,int style)
        {
            bool riot=style==3,tactical=style>=2;Material glass=M(style==1||style==2?"AmberGlass":"TealGlass");
            if(!tactical){Hood(g,mat("ClothDark"));Helmet(g,mat("Steel"));AngularPlate("Frontal armored hood crown",g,mat("Steel"),V(0,.163f,.075f),new Vector2(.173f,.090f),.023f,true);
                if(style==1)AngularPlate("Crimson crown inset",g,mat("Accent"),V(0,.192f,.091f),new Vector2(.070f,.075f),.011f,true);
            }else {Helmet(g,mat("Steel"));AngularPlate("Helmet frontal accessory mount",g,mat("Pad"),V(0,.173f,.092f),new Vector2(.046f,.043f),.006f);for(int s=-1;s<=1;s+=2){var ear=Tube("Helmet ear communication cup",g,mat("Pad"),Vector3.zero,.035f,.018f,.03f);ear.localPosition=V(s*.094f,.104f,-.007f);ear.localRotation=Quaternion.LookRotation(Vector3.right*s);}}
            if(!riot)for(int side=-1;side<=1;side+=2){AngularPlate("Opaque portrait cheek seal",g,mat("Pad"),V(side*.058f,.068f,.081f),new Vector2(.068f,.10f),.020f,true);Rod("Armored brow nose bridge",g,mat("Steel"),V(0,.084f,.110f),V(0,.137f,.104f),.014f);}
            // Complete opaque face shell covers the original face from brows to jaw.
            if(!riot){AngularPlate("Full anatomical respirator shell",g,mat("Pad"),V(0,.044f,.077f),new Vector2(.158f,.123f),.029f,true);
                for(int s=-1;s<=1;s+=2){
                    var lens=G("Angled portrait goggle",g,V(s*.044f,.111f,.075f));lens.localRotation=Quaternion.Euler(0,s*10,-s*6);
                    Shell("Goggle dark seal",lens,mat("Pad"),Vector3.zero,new Vector2(.092f,.067f),.016f);Shell("Goggle metal surround",lens,mat("Trim"),V(0,0,.010f),new Vector2(.086f,.061f),.013f);Shell("Contoured colored optical lens",lens,glass,V(0,0,.014f),new Vector2(.075f,.050f),.012f);
                    var filter=G("Twin angled respirator filter",g,V(s*.064f,.017f,.082f));filter.localRotation=Quaternion.Euler(0,s*30,0);Tube("Filter armored canister",filter,mat("Steel"),Vector3.zero,.028f,.027f,.72f);Tube("Filter retaining rim",filter,mat("Trim"),V(0,0,.023f),.029f,.005f,.85f);Tube("Inset perforated charcoal screen",filter,M("Dark"),V(0,0,.024f),.023f,.002f,.02f);for(int row=-2;row<=2;row++)for(int col=-2;col<=2;col++)if(row*row+col*col<7)Tube("Breathing screen perforation rim",filter,mat("Trim"),V(col*.007f,row*.007f,.027f),.0022f,.002f,.6f,-1,8);
                }
                AngularPlate("Pointed nose bridge armored respirator",g,mat("Steel"),V(0,.043f,.109f),new Vector2(.070f,.100f),.020f,true);Rod("Respirator raised center seam",g,mat("Trim"),V(0,.080f,.125f),V(0,-.002f,.121f),.0016f);
                Band("Lower face cloth wrap",g,mat(style==1?"Accent":"Cloth"),-.025f,.077f,.075f,.035f);
            } else {
                // Riot visor is a wide curved transparent shield with real side depth.
                var v=new List<Vector3>();var ix=new List<int>();for(int row=0;row<2;row++)for(int i=0;i<=28;i++){float a=Mathf.Lerp(-1.03f,1.03f,i/28f);v.Add(V(Mathf.Sin(a)*.117f,.022f+row*.137f,Mathf.Cos(a)*.105f+.036f));}for(int i=0;i<28;i++){int a=i,b=a+29;ix.AddRange(new[]{a,a+1,b,a+1,b+1,b});}MeshPart("Wide curved cyan riot face shield",g,glass,v,ix);for(int row=0;row<2;row++){Vector3 previous=Vector3.zero;for(int i=0;i<=28;i++){float a=Mathf.Lerp(-1.03f,1.03f,i/28f);Vector3 q=V(Mathf.Sin(a)*.119f,.022f+row*.137f,Mathf.Cos(a)*.107f+.036f);if(i>0)Rod("Riot visor protective frame",g,mat("Trim"),previous,q,.003f);previous=q;}}
                AngularPlate("Riot chin vent guard",g,mat("Steel"),V(0,.006f,.076f),new Vector2(.154f,.040f),.015f);for(int i=-3;i<=3;i++)Rod("Riot chin intake slot",g,M("Dark"),V(i*.016f,-.005f,.091f),V(i*.016f,.012f,.091f),.002f);
            }
        }
        static void BuildArmor(DetailedHero3D b,ArmorItem item,List<Transform> roots,Func<string,Material> mat)
        {
            int style=Mathf.Clamp(item.visualVariant,0,3);bool old=style<2;
            Transform Root(string name,Transform parent){var t=G(name,parent,Vector3.zero);roots.Add(t);return t;}
            switch(item.slot){
                case ArmorSlot.Head:PortraitHead(Root("Portrait exact fullface / "+style,b.head),mat,style);break;
                case ArmorSlot.Chest:{
                    var g=Root("Portrait field jacket harness / "+style,b.chest);
                    ClothLoft("Portrait colored tailored jacket shell",g,mat("Cloth"),new[]{-.289f,-.24f,-.14f,-.03f,.07f,.15f,.194f},new[]{.208f,.229f,.195f,.193f,.235f,.249f,.155f},new[]{.132f,.140f,.126f,.136f,.148f,.131f,.088f},Vector3.zero);
                    PortraitScarf(g,mat,style==1);
                    var back=G("Jacket rear equipment",g,Vector3.zero);back.localRotation=Quaternion.Euler(0,180,0);
                    for(int side=-1;side<=1;side+=2){Strap(g,mat,V(side*.16f,.17f,.123f),V(side*.13f,-.23f,.16f),old?.034f:.045f);Buckle(g,V(side*.15f,.08f,.160f),.038f,.043f);Strap(back,mat,V(side*.16f,.17f,.123f),V(side*.13f,-.23f,.16f),.031f);}
                    Band("Portrait waist belt",g,mat("Leather"),-.238f,.235f,.155f,.044f);Buckle(g,V(0,-.238f,.161f),.057f,.047f);
                    if(old){AngularPlate("Single angled chest steel patch",g,mat("Steel"),V(.065f,.018f,.147f),new Vector2(.13f,.14f),.014f,true);for(int side=-1;side<=1;side+=2){PatchPouch(g,mat,V(side*.14f,-.23f,.158f),.080f,.115f);var can=G("Chest twin amber service ampoules",g,V(.10f,-.015f,.181f));can.localRotation=Quaternion.Euler(90,0,0);Tube("Amber vial",can,M("AmberGlass"),V(side*.016f,0,0),.010f,.079f,.65f);for(int j=0;j<2;j++)Tube("Vial metal end",can,mat("Trim"),V(side*.016f,0,j*.071f),.012f,.009f,.75f);}}
                    else {Panel("Tactical rectangular ballistic chest panel",g,mat("Pad"),V(0,.018f,.151f),new Vector2(.235f,.24f),.012f);for(int row=0;row<4;row++)for(int col=0;col<4;col++)Panel("Sewn PALS webbing rectangle",g,mat("Leather"),V((col-1.5f)*.049f,.094f-row*.044f,.169f),new Vector2(.045f,.022f),.002f);for(int col=0;col<4;col++)PatchPouch(g,mat,V((col-1.5f)*.077f,-.207f,.164f),.073f,.14f);Panel("Chest radio",g,mat("Steel"),V(.105f,.065f,.177f),new Vector2(.062f,.09f),.010f);Rod("Radio aerial",g,M("Dark"),V(.111f,.10f,.18f),V(.111f,.205f,.18f),.002f);}
                    for(int side=0;side<2;side++){
                        var arm=Root("Portrait matching jacket upper sleeve",side==0?b.leftUpperArm:b.rightUpperArm);ClothLoft("Matching upper sleeve cloth",arm,mat("Cloth"),new[]{-.294f,-.25f,-.17f,-.09f,-.015f,.025f},new[]{.067f,.075f,.084f,.085f,.090f,.054f},new[]{.066f,.078f,.082f,.083f,.086f,.050f},Vector3.zero);
                        var cap=G("Conforming portrait shoulder pauldron",arm,Vector3.zero);Band("Curved shoulder shell",cap,mat(old?"Steel":"Pad"),-.050f,.097f,.098f,.115f,side==0?-.45f:Mathf.PI-.45f,Mathf.PI*.85f);AngularPlate("Front shoulder faceted panel",cap,mat(old?"Steel":"Pad"),V(side==0?-.034f:.034f,-.055f,.077f),new Vector2(.12f,.13f),.016f,true);
                        ShoulderCrown(cap,mat(old?"Steel":"Pad"),side==0?-.45f:Mathf.PI-.45f);
                    }
                    break;}
                case ArmorSlot.Arms:{for(int side=0;side<2;side++){
                    var fore=Root("Portrait sleeve and bracer / "+style,side==0?b.leftForearm:b.rightForearm);var hand=Root("Portrait armored full glove / "+style,side==0?b.leftHand:b.rightHand);
                    ClothLoft("Portrait sleeve forearm cloth",fore,mat("Cloth"),new[]{-.17f,-.12f,-.06f,0,.032f},new[]{.059f,.070f,.073f,.068f,.060f},new[]{.057f,.069f,.072f,.067f,.058f},Vector3.zero);
                    Band("Portrait upper bracer strap",fore,mat(style==1||style==3?"Accent":"Leather"),-.17f,.058f,.055f,.021f);Band("Portrait wrist strap",fore,mat("Leather"),-.245f,.044f,.040f,.023f);Buckle(fore,V(.031f,-.245f,.031f),.023f,.025f);
                    if(old){AngularPlate("Upper bracer polygon",fore,mat("Steel"),V(0,-.153f,.056f),new Vector2(.089f,.080f),.008f,true);Panel("Recessed colored bracer viewing window",fore,M(style==1?"AmberGlass":"TealGlass"),V(0,-.185f,.052f),new Vector2(.041f,.057f),.003f);AngularPlate("Wrist beveled plate",fore,mat("Steel"),V(0,-.247f,.039f),new Vector2(.072f,.058f),.009f,true);}
                    else if(style==2){for(int j=0;j<5;j++)Band("Offwhite field bandage wrap",fore,mat("Accent"),-.155f-j*.014f,.060f-j*.0034f,.056f-j*.0032f,.012f);AngularPlate("Dark tactical wrist cuff",fore,mat("Pad"),V(0,-.24f,.040f),new Vector2(.067f,.05f),.006f);}
                    else {for(int j=0;j<2;j++){AngularPlate("Riot double forearm plate",fore,mat("Steel"),V(0,-.17f-j*.062f,.054f-j*.011f),new Vector2(.081f-j*.011f,.067f),.008f);Band("Red riot cuff retaining strap",fore,mat("Accent"),-.178f-j*.062f,.058f-j*.011f,.055f-j*.011f,.016f);}}
                    Panel("Full leather glove back",hand,mat("Pad"),V(0,-.063f,.028f),new Vector2(.070f,.076f),.005f);for(int finger=0;finger<4;finger++)Shell("Glove separate raised knuckle",hand,old?mat("Trim"):M("Steel"),V((finger-1.5f)*.017f,-.09f,.035f),new Vector2(.012f,.019f),.004f);
                    if(style==1||style==3)for(int j=0;j<2;j++){var tube=G("Side mounted service vial",fore,V(.040f,-.188f-j*.022f,.019f));tube.localRotation=Quaternion.Euler(90,0,0);Tube("Bracer service ampoule",tube,M("AmberGlass"),Vector3.zero,.007f,.040f,.6f);}
                }break;}
                case ArmorSlot.Legs:{b.SetSeatFabric(mat("Cloth"));for(int side=0;side<2;side++){
                    var thigh=Root("Portrait matching trousers upper / "+style,side==0?b.leftThigh:b.rightThigh);var shin=Root("Portrait matching trousers greave / "+style,side==0?b.leftShin:b.rightShin);
                    ClothLoft("Portrait trouser thigh cloth",thigh,mat("Cloth"),new[]{-.45f,-.415f,-.34f,-.22f,-.11f,-.03f,.03f},new[]{.073f,.085f,.097f,.114f,.117f,.106f,.080f},new[]{.075f,.082f,.097f,.113f,.117f,.111f,.080f},Vector3.zero);
                    ClothLoft("Portrait trouser calf cloth",shin,mat("Cloth"),new[]{-.31f,-.285f,-.245f,-.18f,-.10f,-.02f,.03f},new[]{.063f,.078f,.082f,.092f,.095f,.077f,.070f},new[]{.062f,.074f,.083f,.099f,.095f,.076f,.071f},Vector3.zero);
                    Band("Knee upper leather binding",shin,mat("Leather"),.034f,.082f,.083f,.019f);Band("Knee lower leather binding",shin,mat("Leather"),-.060f,.092f,.095f,.019f);
                    if(old){AngularPlate("Angular shield knee plate",shin,mat(style==1?"Accent":"Steel"),V(0,-.006f,.084f),new Vector2(.135f,.145f),.028f,true);AngularPlate("Long tapered angular shin plate",shin,mat(style==1?"Accent":"Steel"),V(0,-.180f,.088f),new Vector2(.089f,.205f),.009f,true);}
                    else {Shell("Rounded tactical kneecap",shin,mat("Steel"),V(0,-.008f,.082f),new Vector2(.125f,.135f),.024f);AngularPlate("Short tactical shin plate",shin,mat("Steel"),V(0,-.187f,.095f),new Vector2(.075f,.125f),.007f);}
                    CrossedWrap(shin,mat(style==1||style==3?"Accent":style==2?"Accent":"Leather"),-.100f,-.190f,.094f,.097f,.021f);
                    Band("Greave ankle retaining strap",shin,mat("Leather"),-.268f,.077f,.075f,.018f);Buckle(shin,V(.055f,-.268f,.055f),.025f,.026f);
                }break;}
                case ArmorSlot.Feet:{for(int side=0;side<2;side++){
                    var shin=Root("Portrait boot upper / "+style,side==0?b.leftShin:b.rightShin);var foot=Root("Portrait boot heel / "+style,side==0?b.leftFoot:b.rightFoot);var toeJoint=side==0?b.leftToe:b.rightToe;if(!toeJoint)toeJoint=side==0?b.leftFoot:b.rightFoot;
                    // Calf-fixed upper shell and articulated toe remain independent.
                    ClothLoft("Portrait tall leather boot shaft",shin,mat("Leather"),new[]{-.41f,-.37f,-.33f,-.29f,-.255f},new[]{.065f,.064f,.062f,.065f,.068f},new[]{.068f,.063f,.059f,.063f,.068f},Vector3.zero);
                    Band("Boot ankle cross strap",shin,mat("Leather"),-.31f,.068f,.068f,.026f);Buckle(shin,V(.044f,-.31f,.053f),.027f,.028f);
                    AngularPlate("Boot tall shin front panel",shin,mat("Pad"),V(0,-.33f,.063f),new Vector2(.085f,.14f),.006f);
                    var toe=Root("Portrait articulated armored toe",toeJoint);toe.localPosition=V(0,.072f,.032f);toe.localRotation=Quaternion.Euler(-90,0,0);AngularPlate("Portrait toe armored cap",toe,mat(old?(style==1?"Accent":"Steel"):"Steel"),Vector3.zero,new Vector2(.125f,.105f),.013f);
                    for(int row=0;row<5;row++){float y=-.388f+row*.024f,z=.071f;for(int sign=-1;sign<=1;sign+=2){Tube("Boot brass lace eyelet",shin,mat("Trim"),V(sign*.027f,y,z),.0035f,.002f,.45f,-1,10);Rod("Crossed boot lace",shin,M("Dark"),V(sign*.026f,y,z+.003f),V(-sign*.026f,y+.020f,z+.003f),.0019f);}}
                    foot.localRotation=Quaternion.Euler(0,180,0);Shell("Portrait boot rear heel counter",foot,mat("Leather"),V(0,-.032f,.068f),new Vector2(.12f,.055f),.005f);
                }break;}
            }
        }
        static void Panel(string name,Transform p,Material material,Vector3 c,Vector2 size,float depth)
        {
            var v=new List<Vector3>{c+V(-size.x*.5f,-size.y*.5f,0),c+V(size.x*.5f,-size.y*.5f,0),c+V(size.x*.5f,size.y*.5f,0),c+V(-size.x*.5f,size.y*.5f,0),c+V(0,0,depth)};
            MeshPart(name,p,material,v,new List<int>{0,1,4,1,2,4,2,3,4,3,0,4});
        }
        static void CrossedWrap(Transform p,Material material,float upper,float lower,float rx,float rz,float width)
        {
            for(int crossing=0;crossing<2;crossing++){
                var v=new List<Vector3>();var ix=new List<int>();for(int edge=0;edge<2;edge++)for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;float y=Mathf.Lerp(upper,lower,(Mathf.Sin(a+(crossing==0?0:Mathf.PI))+.98f)*.5f);v.Add(V(Mathf.Cos(a)*rx,y+(edge-.5f)*width,Mathf.Sin(a)*rz));}for(int i=0;i<48;i++){int a=i,b=a+49;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}MeshPart("Conforming crossed portrait bindings",p,material,v,ix);
            }
        }
        static Vector3 HelmetPoint(float u,float angle)
        {
            float front=Mathf.Max(0,Mathf.Sin(angle)),rear=Mathf.Max(0,-Mathf.Sin(angle));
            float side=Mathf.Abs(Mathf.Cos(angle));
            // Follow the skull to the upper nape, leaving the collar and neck free.
            float bottom=.133f-.163f*rear-.075f*side;
            float radius=Mathf.Sin(Mathf.Min(1,u/.60f)*Mathf.PI*.5f)*Mathf.Lerp(1,.90f,Mathf.InverseLerp(.65f,1,u));
            float crown=.286f;float y=Mathf.Lerp(crown,bottom,Mathf.Pow(u,1.48f));
            float x=Mathf.Sign(Mathf.Cos(angle))*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)),.86f)*.119f*radius;
            float z=Mathf.Sin(angle)*.112f*radius-.010f;
            // A shallow sagittal ridge and flatter temple planes produce a protective shell, rather than a bald sphere.
            y+=Mathf.Pow(1-Mathf.Abs(Mathf.Cos(angle)),8)*.008f*Mathf.Sin(u*Mathf.PI);
            return V(x,y,z);
        }
        static void Helmet(Transform p,Material m)
        {
            const int R=28,N=64;var v=new List<Vector3>();var ix=new List<int>();
            for(int row=0;row<=R;row++)for(int i=0;i<=N;i++)v.Add(HelmetPoint(Mathf.Max(.004f,row/(float)R),i*Mathf.PI*2/N));
            for(int row=0;row<R;row++)for(int i=0;i<N;i++){int a=row*(N+1)+i,b=a+N+1;ix.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            MeshPart("Full occipital shell with fitted upper nape",p,m,v,ix);
            for(int i=1;i<=N;i++)Rod("Continuous protected nape rolled rim",p,M("Dark"),HelmetPoint(1,(i-1)*Mathf.PI*2/N),HelmetPoint(1,i*Mathf.PI*2/N),.0045f);
            for(int side=-1;side<=1;side+=2)
            {
                float angle=side<0?Mathf.PI*.87f:Mathf.PI*.13f;
                Vector3 previous=HelmetPoint(.12f,angle);
                for(int row=1;row<=15;row++){Vector3 q=HelmetPoint(.12f+row*.054f,angle)+V(side*.001f,.001f,0);Rod("Recessed shell panel seam",p,M("Dark"),previous,q,.0012f);previous=q;}
                for(int n=0;n<3;n++){Vector3 q=HelmetPoint(.72f+n*.09f,side<0?Mathf.PI*1.13f:-Mathf.PI*.13f);var stud=Tube("Rear shell captive fastener",p,M("Steel"),Vector3.zero,.004f,.003f,.05f,-1,12);stud.localPosition=q;stud.localRotation=Quaternion.LookRotation(V(side*.6f,0,-1));}
            }
            Shell("Angular reinforced frontal brow",p,m,V(0,.139f,.093f),new Vector2(.194f,.035f),.027f);
            var nape=G("Occipital reinforcement mount",p,V(0,.052f,-.119f));nape.localRotation=Quaternion.Euler(0,180,0);AngularPlate("Segmented occipital reinforcement",nape,m,Vector3.zero,new Vector2(.15f,.074f),.008f,true);
            for(int scrape=0;scrape<15;scrape++)
            {float a=.4f+scrape*2.399f,u=.28f+(scrape%5)*.12f;Vector3 q=HelmetPoint(u,a),r=HelmetPoint(u+.016f,a+.065f);Rod("Fine helmet paint edge wear",p,M("Steel"),q,r,.0008f);}
        }
        static void Hood(Transform p,Material m)
        {
            // Rear and side panels leave a real open front aperture.
            const int R=16,N=36;var v=new List<Vector3>();var ix=new List<int>();for(int j=0;j<=R;j++){float u=j/(float)R,y=Mathf.Lerp(-.055f,.265f,u),r=Mathf.Sqrt(Mathf.Max(.04f,1-Mathf.Pow((y-.105f)/.175f,2)));for(int i=0;i<=N;i++){float a=Mathf.Lerp(.73f,Mathf.PI*2-.73f,i/(float)N);v.Add(V(Mathf.Sin(a)*.115f*r,y,Mathf.Cos(a)*.105f*r-.008f));}}
            for(int j=0;j<R;j++)for(int i=0;i<N;i++){int a=j*(N+1)+i,b=a+N+1;ix.AddRange(new[]{a,a+1,b,a+1,b+1,b});}MeshPart("Open canvas nightwatch hood",p,m,v,ix);
            for(int side=0;side<2;side++)for(int j=1;j<=R;j++)Rod("Hood aperture folded piping",p,m,v[(j-1)*(N+1)+side*N],v[j*(N+1)+side*N],.004f);
        }
        public static Transform BuildMelee(Transform parent,MeleeWeaponFamily family)
        {
            var g=G("Inventory melee 3D / "+MeleeWeaponCatalog.English(family),parent,Vector3.zero);
            // Grip spans -.10..+.025; blade points along +Z. Natural single-hand pivot is origin.
            Profile("Full tang contoured handle",g,M(family==MeleeWeaponFamily.Hatchet?"Wood":"LeatherDark"),.028f,P(-.12f,-.025f),P(-.12f,.025f),P(.025f,.023f),P(.037f,0),P(.024f,-.022f));
            for(int i=0;i<8;i++)Rod("Handle wrapped binding",g,M("Leather"),V(-.015f,-.018f,-.10f+i*.014f),V(.015f,.018f,-.093f+i*.014f),.0015f);
            Fasteners(g,-.083f,-.015f,.015f,0,2);
            switch(family){
                case MeleeWeaponFamily.Knife:
                    Profile("Drop point field knife blade",g,M("Steel"),.009f,P(.025f,-.027f),P(.21f,-.026f),P(.30f,.003f),P(.23f,.035f),P(.025f,.036f));Rod("Knife blade fuller",g,M("Dark"),V(.005f,.017f,.06f),V(.005f,.017f,.22f),.0013f);Rod("Knife finger guard",g,M("Steel"),V(0,-.045f,.020f),V(0,.049f,.020f),.005f);break;
                case MeleeWeaponFamily.Shiv:
                    Profile("Uneven sharpened scrap shiv",g,M("Steel"),.007f,P(.025f,-.018f),P(.105f,-.013f),P(.26f,.019f),P(.11f,.032f),P(.025f,.024f));for(int i=0;i<5;i++)Rod("Shiv grinding scratch",g,M("Dark"),V(.004f,-.005f,.05f+i*.023f),V(.004f,.013f,.064f+i*.023f),.0008f);break;
                case MeleeWeaponFamily.Cleaver:
                    Profile("Heavy rectangular cleaver blade",g,M("Steel"),.014f,P(.03f,-.072f),P(.29f,-.075f),P(.34f,-.055f),P(.34f,.073f),P(.055f,.066f),P(.03f,.030f));var eye=Tube("Cleaver hanging eye",g,M("Dark"),Vector3.zero,.010f,.002f,.72f);eye.localPosition=V(.008f,.048f,.305f);eye.localRotation=Quaternion.Euler(0,90,0);Rod("Cleaver honed bevel",g,M("Steel"),V(.008f,-.065f,.055f),V(.008f,-.066f,.30f),.001f);break;
                case MeleeWeaponFamily.Machete:
                    Profile("Swept forward machete blade",g,M("Steel"),.008f,P(.02f,-.028f),P(.30f,-.05f),P(.47f,-.022f),P(.50f,.020f),P(.45f,.067f),P(.26f,.047f),P(.035f,.030f));Rod("Machete long ground bevel",g,M("Dark"),V(.0045f,-.018f,.06f),V(.0045f,-.035f,.36f),.001f);Rod("Machete small guard",g,M("Steel"),V(0,-.039f,.022f),V(0,.040f,.022f),.004f);break;
                case MeleeWeaponFamily.Hatchet:
                    Profile("Curved wooden hatchet haft",g,M("Wood"),.031f,P(-.11f,-.023f),P(.27f,-.020f),P(.32f,-.005f),P(.32f,.028f),P(.20f,.022f),P(-.11f,.023f));Profile("Forged wedge axe head",g,M("Steel"),.046f,P(.20f,-.05f),P(.35f,-.10f),P(.41f,-.070f),P(.41f,.10f),P(.34f,.115f),P(.25f,.046f),P(.20f,.043f));Rod("Hatchet blade sharpened edge",g,M("Steel"),V(.024f,-.068f,.402f),V(.024f,.096f,.402f),.002f);for(int i=0;i<4;i++)Rod("Haft head leather lashing",g,M("LeatherDark"),V(-.02f,-.025f,.18f+i*.015f),V(.02f,.025f,.18f+i*.015f),.003f);break;
            }
            G("Melee hand socket",g,Vector3.zero);Compact(g);return g;
        }
    }
}



