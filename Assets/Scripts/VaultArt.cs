using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Authored geometry, painted materials and lighting are separate from combat collision.
    public sealed partial class VaultArt
    {
        readonly Transform root;
        readonly List<Material> owned;
        readonly System.Random random;
        readonly Dictionary<string,Material> cache=new Dictionary<string,Material>();
        public static readonly Color Stone=new Color(.34f,.34f,.30f), Dark=new Color(.11f,.12f,.10f), Bronze=new Color(.5f,.31f,.13f), Gold=new Color(.95f,.62f,.23f), Jade=new Color(.55f,.74f,.16f);
        public VaultArt(Transform root,List<Material> owned,System.Random random) {this.root=root;this.owned=owned;this.random=random;}
        Material Material(Color color,bool glow=false)
        {
            string key=color.ToString()+glow;
            if(cache.TryGetValue(key,out var mat))return mat;
            mat=new Material(Resources.Load<Shader>("VaultSurface"));mat.color=color;
            if(!glow)
            {
                mat.mainTexture=Resources.Load<Texture2D>("Art/WastelandFloor");mat.mainTextureScale=new Vector2(1.5f,1.5f);mat.color=Color.Lerp(color,Color.white,.30f);
            }
            mat.SetColor("_EmissionColor",glow?color*1.4f:Color.black);cache.Add(key,mat);owned.Add(mat);return mat;
        }
        Material ArmorMaterial(Color color)
        {
            Color neutral=Color.Lerp(new Color(.30f,.32f,.31f),color,.58f);
            string key="Rust metal/"+neutral.ToString();if(cache.TryGetValue(key,out var mat))return mat;
            mat=new Material(Resources.Load<Shader>("VaultArmor"));mat.name="Rust metal";mat.mainTexture=Resources.Load<Texture2D>("Art/WastelandSurface");mat.color=neutral*1.25f;mat.SetFloat("_PaintMix",.62f);mat.SetColor("_EmissionColor",Color.black);cache.Add(key,mat);owned.Add(mat);return mat;
        }
        Material WastelandMaterial(string name,Color color)
        {
            string n=name.ToLowerInvariant();
            if(n.Contains("isotope glass")||n.Contains("goggle glass reflection"))
            {
                string opticKey=n.Contains("reflection")?"Optical reflection":"Isotope glass";if(cache.TryGetValue(opticKey,out var optic))return optic;
                optic=new Material(Resources.Load<Shader>("VaultArmor"));optic.name=opticKey;optic.color=n.Contains("reflection")?new Color(.48f,.57f,.27f):new Color(.23f,.30f,.075f);optic.SetFloat("_PaintMix",0);optic.SetColor("_EmissionColor",n.Contains("reflection")?new Color(.018f,.025f,.003f):new Color(.075f,.10f,.016f));cache.Add(opticKey,optic);owned.Add(optic);return optic;
            }
            if(n.Contains("lens")||n.Contains("visor glass"))
            {
                string glassKey="Smoked optical glass";if(cache.TryGetValue(glassKey,out var glass))return glass;
                glass=new Material(Resources.Load<Shader>("VaultArmor"));glass.name=glassKey;glass.color=new Color(.11f,.20f,.17f);glass.SetFloat("_PaintMix",0);glass.SetColor("_EmissionColor",Color.black);cache.Add(glassKey,glass);owned.Add(glass);return glass;
            }
            bool skin=n.Contains("skull")||n.Contains("neck")||n.Contains("cheek")||n.Contains("abdomen")||n.Contains("swollen")||n.Contains("tooth");
            bool rubber=n.Contains("respirator")||n.Contains("visor")||n.Contains("filter vent")||n.Contains("gauntlet")||n.Contains("glov")||n.Contains("finger")||n.Contains("elbow")||n.Contains("cuff")||n.Contains("sole")||n.Contains("hose")||n.Contains("lens")||n.Contains("goggle");
            bool cloth=n.Contains("jacket")||n.Contains("trouser")||n.Contains("pocket")||n.Contains("stitch")||n.Contains("upper arm")||n.Contains("sleeve")||n.Contains("collar")||n.Contains("hem")||n.Contains("canvas")||n.Contains("fold")||n.Contains("fabric");
            bool leather=n.Contains("pouch")||n.Contains("flap")||n.Contains("leather")||n.Contains("wooden")||n=="boot"||n=="belt"||n.Contains("harness")||n.Contains("strap")||n.Contains("pack");
            if(n.Contains("buckle")||n.Contains("rivet")||n.Contains("zipper tooth")||n.Contains("stud")){cloth=false;leather=false;rubber=false;skin=false;}
            if(n.Contains("steel")||n.Contains("metal")||n.Contains("copper")){skin=false;rubber=false;cloth=false;leather=false;}
            if(!skin&&!rubber&&!cloth&&!leather)return ArmorMaterial(color);
            string type=skin?"Matte skin":rubber?"Matte rubber":cloth?"Matte cloth":"Worn leather";
            Color tint=skin?color:rubber?new Color(.075f,.085f,.075f):cloth?Color.Lerp(new Color(.26f,.32f,.25f),color,.22f):new Color(.26f,.18f,.12f);
            string key=type+tint.ToString();if(cache.TryGetValue(key,out var mat))return mat;
            mat=new Material(Resources.Load<Shader>("VaultSurface"));mat.name=type;mat.color=tint;
            if(leather){mat.mainTexture=Resources.Load<Texture2D>("Art/WastelandLeather");mat.color=new Color(.70f,.62f,.51f);}
            if(cloth){mat.mainTexture=Resources.Load<Texture2D>("Art/WastelandCloth")??Resources.Load<Texture2D>("Art/WastelandLeather");mat.mainTextureScale=new Vector2(2,2);mat.color=Color.Lerp(new Color(.67f,.73f,.57f),color,.3f);}
            if((cloth||leather)&&mat.mainTexture!=null){mat.mainTexture.wrapMode=TextureWrapMode.Repeat;mat.mainTexture.filterMode=FilterMode.Trilinear;mat.mainTexture.anisoLevel=8;}
            mat.SetColor("_EmissionColor",Color.black);cache.Add(key,mat);owned.Add(mat);return mat;
        }
        Material EnvironmentMaterial(string name,Color color)
        {
            string n=name.ToLowerInvariant();
            bool concrete=n.Contains("concrete")||n.Contains("foundation")||n.Contains("plinth")||n.Contains("pier");
            bool rubber=n.Contains("tire")||n.Contains("window")||n.Contains("windscreen")||n.Contains("grille")||n.Contains("cable")||n.Contains("slit");
            bool glass=n.Contains("lens")||n.Contains("gauge face");
            if(!glass&&!rubber)
            {
                var weathered=DistrictMaterial(concrete?0:1,color);
                if(weathered!=null)return weathered;
            }
            if(!concrete&&!rubber&&!glass)
            {
                string metalKey="World salvage metal/"+color;if(cache.TryGetValue(metalKey,out var metal))return metal;
                metal=new Material(Resources.Load<Shader>("VaultArmor"));metal.name=metalKey;metal.color=Color.Lerp(color,new Color(.27f,.28f,.25f),.25f)*1.15f;metal.mainTexture=Resources.Load<Texture2D>("Art/WastelandSurface");metal.SetFloat("_PaintMix",.54f);metal.SetColor("_EmissionColor",Color.black);cache.Add(metalKey,metal);owned.Add(metal);return metal;
            }
            string key="World/"+(concrete?"concrete":rubber?"rubber":"glass")+color;
            if(cache.TryGetValue(key,out var mat))return mat;
            mat=new Material(Resources.Load<Shader>("VaultSurface"));mat.name=key;mat.color=color;
            if(concrete){mat.mainTexture=Resources.Load<Texture2D>("Art/WastelandFloor");mat.mainTextureScale=new Vector2(2,2);mat.color=Color.Lerp(color,Color.white,.22f);}
            mat.SetColor("_EmissionColor",Color.black);cache.Add(key,mat);owned.Add(mat);return mat;
        }
        static bool ActorParent(Transform parent)
        {
            for(var p=parent;p!=null;p=p.parent)if(p.name.StartsWith("Runner /")||p.name=="Vault sentinel")return true;
            return false;
        }
        public Transform Part(string name,PrimitiveType type,Vector3 p,Vector3 scale,Color color,Transform parent=null,bool glow=false)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;UnityEngine.Object.Destroy(obj.GetComponent<Collider>());
            if(ActorParent(parent)&&type==PrimitiveType.Cube)obj.GetComponent<MeshFilter>().sharedMesh=BeveledPlateMesh();
            obj.transform.SetParent(parent??root,false);obj.transform.localPosition=p;obj.transform.localScale=scale;
            var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=!glow&&ActorParent(parent)&&!name.Contains("Scarf")?WastelandMaterial(name,color):!glow?EnvironmentMaterial(name,color):Material(color,true);renderer.shadowCastingMode=glow?ShadowCastingMode.Off:ShadowCastingMode.On;return obj.transform;
        }
        Transform Group(string name,Vector3 p,Transform parent=null) {var g=new GameObject(name).transform;g.SetParent(parent??root,false);g.localPosition=p;return g;}
        public void Floor(float w,float d,int sector)
        {
 var floor=Part("Wasteland concrete",PrimitiveType.Cube,new Vector3(0,-.26f,0),new Vector3(w*2,.4f,d*2),Color.white);
 var source=DistrictMaterial(sector%3==1?0:3,new Color(.76f,.73f,.65f));
 Material mat;
 if(source!=null){mat=new Material(source);mat.color=source.color*.48f;owned.Add(mat);}
 else{mat=new Material(Resources.Load<Shader>("VaultSurface"));mat.color=new Color(.23f,.24f,.22f);mat.mainTexture=Resources.Load<Texture2D>("Art/WastelandFloor");mat.mainTextureScale=new Vector2(w/3,d/3);owned.Add(mat);}floor.GetComponent<Renderer>().sharedMaterial=mat;
 Wall(new Vector3(0,0,d+.3f),w*2,0,true);Wall(new Vector3(w+.3f,0,0),d*2,90,true);Wall(new Vector3(0,0,-d-.3f),w*2,0,false);Wall(new Vector3(-w-.3f,0,0),d*2,90,false);
 WastelandFence(new Vector3(-w+4,0,d-1),7);
 for(int x=-(int)w;x<w;x+=6){Column(new Vector3(x,0,d),2.8f);Brazier(new Vector3(x+2,0,d-1));}
 }
        void Wall(Vector3 position,float length,float yaw,bool high)
        {
 var wall=Group("Reinforced concrete ruins",position);wall.localRotation=Quaternion.Euler(0,yaw,0);float h=high?2.1f:.38f;
 Part("Concrete wall",PrimitiveType.Cube,new Vector3(0,h/2,0),new Vector3(length,h,.7f),Stone,wall);
 for(int i=0;i<length/2;i++){float x=-length/2+1+i*2;Part("Rusted wall reinforcement",PrimitiveType.Cube,new Vector3(x,h*.5f,-.40f),new Vector3(.08f,h,.08f),Bronze,wall);if(high)Part("Broken pipe",PrimitiveType.Cylinder,new Vector3(x,1.8f,-.5f),new Vector3(.11f,.75f,.11f),Dark,wall).localRotation=Quaternion.Euler(0,0,90);}
 }
        void WastelandFence(Vector3 p,float length)
        {
            var fence=Group("Collapsed rust fence",p);
            for(int s=-1;s<=1;s+=2)Part("Fence post",PrimitiveType.Cylinder,new Vector3(s*length/2,1.0f,0),new Vector3(.085f,1,.085f),Bronze,fence);
            for(int i=0;i<5;i++)Part("Fence horizontal wire",PrimitiveType.Cube,new Vector3(0,.25f+i*.35f,0),new Vector3(length,.018f,.018f),Dark,fence);
            for(int i=0;i<14;i++)Part("Fence woven strand",PrimitiveType.Cube,new Vector3(-length/2+i*length/13,.95f,0),new Vector3(.013f,1.65f,.013f),Bronze,fence).localRotation=Quaternion.Euler(0,0,i%2==0?20:-20);
        }
        public void Column(Vector3 p,float height)
        {
 var c=Group("Reinforced support",p);Part("Concrete pier",PrimitiveType.Cube,new Vector3(0,height/2,0),new Vector3(.65f,height,.65f),Stone,c);
 for(int s=-1;s<=1;s+=2)Part("Exposed rusted rebar",PrimitiveType.Cylinder,new Vector3(s*.27f,height*.55f,-.35f),new Vector3(.045f,height*.5f,.045f),Bronze,c);
 Part("Industrial support cap",PrimitiveType.Cube,new Vector3(0,height,0),new Vector3(.9f,.15f,.8f),Dark,c);
 }
        public void Cover(Vector3 p)
        {
 var c=Group("Wrecked utility vehicle",new Vector3(p.x,0,p.z));
 Part("Truck frame",PrimitiveType.Cube,new Vector3(0,.35f,0),new Vector3(1.66f,.16f,1.78f),Dark,c);
 Part("Rusted lower body",PrimitiveType.Cube,new Vector3(0,.59f,0),new Vector3(1.65f,.41f,1.69f),Bronze*.62f,c);
 Part("Cabin roof",PrimitiveType.Cube,new Vector3(0,1.37f,-.29f),new Vector3(1.45f,.10f,.84f),Stone,c);
 Part("Engine hood",PrimitiveType.Cube,new Vector3(0,.87f,.55f),new Vector3(1.48f,.12f,.57f),Stone*.82f,c).localRotation=Quaternion.Euler(8,0,-3);
 Part("Dark windscreen",PrimitiveType.Cube,new Vector3(0,1.13f,.10f),new Vector3(1.26f,.42f,.03f),new Color(.09f,.14f,.16f),c).localRotation=Quaternion.Euler(-13,0,0);
 for(int sgn=-1;sgn<=1;sgn+=2)
 {
 Part("Cabin front pillar",PrimitiveType.Cube,new Vector3(sgn*.67f,1.12f,.09f),new Vector3(.09f,.46f,.08f),Stone,c).localRotation=Quaternion.Euler(-13,0,0);
 Part("Cabin rear pillar",PrimitiveType.Cube,new Vector3(sgn*.67f,1.10f,-.67f),new Vector3(.11f,.46f,.08f),Stone,c);
 Part("Door window void",PrimitiveType.Cube,new Vector3(sgn*.675f,1.12f,-.29f),new Vector3(.027f,.31f,.54f),Dark,c);
 Part("Door panel",PrimitiveType.Cube,new Vector3(sgn*.82f,.75f,-.23f),new Vector3(.04f,.30f,.63f),Stone*.85f,c);
 Part("Door handle",PrimitiveType.Cube,new Vector3(sgn*.85f,.84f,-.38f),new Vector3(.05f,.035f,.13f),Dark,c);
 Part("Broken side mirror",PrimitiveType.Cube,new Vector3(sgn*.93f,1.03f,.02f),new Vector3(.17f,.12f,.05f),Dark,c);
 Part("Truck headlamp bezel",PrimitiveType.Cylinder,new Vector3(sgn*.53f,.67f,.865f),new Vector3(.21f,.035f,.21f),Dark,c).localRotation=Quaternion.Euler(90,0,0);
 Part("Truck headlamp lens",PrimitiveType.Cylinder,new Vector3(sgn*.53f,.67f,.906f),new Vector3(.16f,.008f,.16f),new Color(.66f,.59f,.36f),c).localRotation=Quaternion.Euler(90,0,0);
 for(int t=-1;t<=1;t+=2)
 {
 Part("Flat tire",PrimitiveType.Cylinder,new Vector3(sgn*.84f,.29f,t*.55f),new Vector3(.54f,.105f,.54f),Dark,c).localRotation=Quaternion.Euler(0,0,90);
 Part("Rusted wheel hub",PrimitiveType.Cylinder,new Vector3(sgn*.955f,.29f,t*.55f),new Vector3(.27f,.014f,.27f),Bronze*.7f,c).localRotation=Quaternion.Euler(0,0,90);
 Part("Wheel arch brow",PrimitiveType.Cube,new Vector3(sgn*.82f,.61f,t*.55f),new Vector3(.12f,.11f,.65f),Stone,c);
 }
 }
 Part("Front bumper",PrimitiveType.Cube,new Vector3(0,.43f,.91f),new Vector3(1.82f,.12f,.15f),Dark,c);
 for(int i=0;i<6;i++)Part("Radiator grille slit",PrimitiveType.Cube,new Vector3(-.29f+i*.115f,.64f,.869f),new Vector3(.047f,.23f,.028f),Dark,c);
 Part("Windscreen wiper",PrimitiveType.Cube,new Vector3(-.30f,.96f,.14f),new Vector3(.48f,.018f,.025f),Dark,c).localRotation=Quaternion.Euler(0,0,12);
 }
        public void Gate(float z)
        {Gate(new Vector3(0,0,z));}
        public void Gate(Vector3 p)
        {
 p.y=0;Column(p+Vector3.left*2,3.2f);Column(p+Vector3.right*2,3.2f);Part("Evacuation gantry",PrimitiveType.Cube,p+Vector3.up*3.25f,new Vector3(4.5f,.35f,.65f),Bronze);
 for(int s=-1;s<=1;s+=2)Part("Evacuation beacon",PrimitiveType.Sphere,p+new Vector3(s*1.95f,2.75f,0),Vector3.one*.22f,Jade,null,true);
 Ring("Extraction field",p+Vector3.up*.025f,1.7f,.075f,Jade,true);
 }
        public void Brazier(Vector3 p)
        {
 var b=Group("Burning oil drum",p);Part("Rusted barrel",PrimitiveType.Cylinder,new Vector3(0,.48f,0),new Vector3(.65f,.48f,.65f),Bronze*.7f,b);
 for(int i=0;i<3;i++)Part("Steel barrel band",PrimitiveType.Cylinder,new Vector3(0,.12f+i*.36f,0),new Vector3(.68f,.025f,.68f),Dark,b);
 for(int tongue=0;tongue<3;tongue++)
 {
 var fire=Part("Burning fuel tongue",PrimitiveType.Sphere,new Vector3((tongue-1)*.11f,.95f+ (tongue==1?.08f:0),tongue==1?.04f:-.06f),new Vector3(.20f,tongue==1?.57f:.39f,.20f),tongue==1?new Color(1,.57f,.13f):new Color(1,.26f,.045f),b,true);
 var mesh=ProfileMesh("Tapered flame tongue",new[]{-.5f,-.29f,-.03f,.22f,.41f,.5f},new[]{.10f,.48f,.37f,.23f,.08f,.003f},new[]{.10f,.40f,.30f,.18f,.06f,.003f},12);fire.GetComponent<MeshFilter>().sharedMesh=mesh;TrackMesh(fire,mesh);fire.gameObject.AddComponent<FlameMotion>().phase=tongue*1.8f;
 }
 }
        public Transform Character(Vector3 position,bool hero,int kind=0)
        {
            if(hero&&kind==0)
            {
                var actor=Group("Runner / layered painted survivor",position);
                actor.gameObject.AddComponent<ActorEquipmentVisual>();
                DetailedHeroPlayer3D.Create(actor);
                return actor;
            }

            if(!hero)return FaithfulEnemy3D.Create(root,position,kind);
            return WastelandCharacter(position,hero,kind);
        }
        // Hand-built chamfered plates have readable front, side and rear planes at every yaw.
        Transform Cuirass(Transform parent,Color color)
        {
            var part=Group("Breastplate",Vector3.zero,parent);
            var rings=new Vector3[32];
            float[] height={.13f,.27f,.63f,.76f};
            float[] width={.27f,.34f,.32f,.23f};
            float[] front={.25f,.31f,.28f,.18f};
            for(int ring=0;ring<4;ring++)
            {
                float w=width[ring],z=front[ring],y=height[ring],b=-.17f;
                Vector3[] outline={new Vector3(-w*.72f,y,z),new Vector3(w*.72f,y,z),new Vector3(w,y,z*.58f),new Vector3(w,y,b*.6f),new Vector3(w*.72f,y,b),new Vector3(-w*.72f,y,b),new Vector3(-w,y,b*.6f),new Vector3(-w,y,z*.58f)};
                for(int i=0;i<8;i++)rings[ring*8+i]=outline[i];
            }
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int ring=0;ring<3;ring++)for(int side=0;side<8;side++)
            {
                int next=(side+1)%8,index=vertices.Count;
                vertices.Add(rings[ring*8+side]);vertices.Add(rings[(ring+1)*8+side]);vertices.Add(rings[(ring+1)*8+next]);vertices.Add(rings[ring*8+next]);
                triangles.AddRange(new[]{index,index+2,index+1,index,index+3,index+2});
            }
            for(int cap=0;cap<2;cap++)for(int side=0;side<8;side++)
            {
                int ring=cap==0?0:3,index=vertices.Count;
                vertices.Add(new Vector3(0,height[ring],0));vertices.Add(rings[ring*8+side]);vertices.Add(rings[ring*8+(side+1)%8]);
                if(cap==0)triangles.AddRange(new[]{index,index+2,index+1});else triangles.AddRange(new[]{index,index+1,index+2});
            }
            var mesh=new Mesh{name="Faceted engraved cuirass"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].x/.7f+.5f,(vertices[i].y-.13f)/.63f);mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();
            part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=ArmorMaterial(color);TrackMesh(part,mesh);
            return part;
        }
        Transform Cape(Transform parent,Color color)
        {
            var g=Group("Hand shaped mantle",new Vector3(0,.65f,-.21f),parent);
            var mesh=new Mesh{name="Folded split mantle"};var vertices=new Vector3[50];var uv=new Vector2[50];var triangles=new List<int>();
            for(int row=0;row<5;row++)for(int col=0;col<5;col++)
            {
                int i=row*5+col;float t=row/4f,u=col/4f;
                float width=Mathf.Lerp(.25f,.4f,t);
                float hem=row==4?(col==2?.09f:col%2==0?0:.045f):0;
                vertices[i]=new Vector3((u-.5f)*width*2,.05f-t*.94f+hem,-t*.25f+Mathf.Cos(u*Mathf.PI*4)*(.015f+t*.04f));
                vertices[i+25]=vertices[i];uv[i]=uv[i+25]=new Vector2(u,t);
            }
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                int i=row*5+col;triangles.AddRange(new[]{i,i+5,i+1,i+1,i+5,i+6});
                int b=i+25;triangles.AddRange(new[]{b,b+1,b+5,b+1,b+6,b+5});
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Material(color);TrackMesh(g,mesh);return g;
        }
        public Transform Weapon(Transform parent,Color color)
        {
            var gun=Group("Aimed weapon",new Vector3(.25f,1.25f,.185f),parent);
            var muzzle=Group("Muzzle origin",new Vector3(0,0,1.28f),gun);
            var aimed=gun.gameObject.AddComponent<AimedWeapon>();aimed.muzzle=muzzle;
            aimed.grip=Group("Trigger hand socket",new Vector3(.10f,-.14f,.22f),gun);
            aimed.supportGrip=Group("Support hand socket",new Vector3(-.08f,-.09f,.42f),gun);
            if(parent.GetComponent<LayeredPlayer2D>()==null&&parent.GetComponent<DetailedHeroPlayer3D>()==null)AddWastelandWeaponVariants(gun);
            return gun;
        }
        public void Partition(Rect rect)
        {
            Vector3 p=new Vector3(rect.center.x,0,rect.center.y);
            Part("Ruined chamber foundation",PrimitiveType.Cube,p+Vector3.up*.33f,new Vector3(rect.width,.7f,rect.height),Dark);
            Part("Concrete partition cap",PrimitiveType.Cube,p+Vector3.up*.72f,new Vector3(rect.width,.16f,rect.height),Stone);
            Part("Concrete recessed cap panel",PrimitiveType.Cube,p+Vector3.up*.806f,new Vector3(Mathf.Max(.1f,rect.width-.10f),.012f,Mathf.Max(.1f,rect.height-.10f)),Stone*.88f);
            Part("Partition brass seam",PrimitiveType.Cube,p+new Vector3(0,.8f,-rect.height*.45f),new Vector3(rect.width,.025f,.055f),Bronze);
        }
        public void Chamber(Vector3 p,int index)
        {
            // SectorLayout.Center is at actor height (0.65 m), not floor height.
            // Chamber props and painted ground marks use floor-relative offsets.
            p.y=0;
            var corner=p+new Vector3(-4.5f,0,3);Brazier(corner);
            var marks=Group("Painted service bay markings",p+new Vector3(2,-.043f,2));
            Color paint=index%3==0?new Color(.75f,.66f,.35f):new Color(.56f,.64f,.65f);
            for(int i=0;i<3;i++)Part("Concrete service stripe",PrimitiveType.Cube,new Vector3(i*.4f,0,0),new Vector3(.14f,.01f,1.4f),paint,marks);
            Part("Service bay lower rail",PrimitiveType.Cube,new Vector3(.4f,0,-.8f),new Vector3(1.2f,.01f,.06f),paint,marks);
        }
        public Transform Ring(string name,Vector3 position,float radius,float width,Color color,bool glow=false)
        {
            var g=Group(name,position);var vertices=new Vector3[128];var triangles=new int[64*6];
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64;vertices[i*2]=new Vector3(Mathf.Sin(a)*(radius-width),0,Mathf.Cos(a)*(radius-width));vertices[i*2+1]=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);
                int j=(i+1)%64;int t=i*6;triangles[t]=i*2;triangles[t+1]=j*2;triangles[t+2]=i*2+1;triangles[t+3]=i*2+1;triangles[t+4]=j*2;triangles[t+5]=j*2+1;
            }
            for(int t=0;t<triangles.Length;t+=3){int swap=triangles[t+1];triangles[t+1]=triangles[t+2];triangles[t+2]=swap;}
            var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();g.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Material(color,glow);TrackMesh(g,mesh);return g;
        }
    }
    public class FlameMotion : MonoBehaviour
    {
        public float phase;
        Vector3 scale;
        void Start(){scale=transform.localScale;}
        void Update(){transform.localScale=Vector3.Scale(scale,new Vector3(1+Mathf.Sin(Time.time*8+phase)*.13f,1+Mathf.Sin(Time.time*11+phase)*.18f,1));}
    }
    public class ActorMotion : MonoBehaviour
    {
        public VaultGame game;
        public Transform leftLeg,rightLeg,leftKnee,rightKnee,body,cape;
        Vector3 previous;
        float phase, movementBlend;
        Vector3 localVelocity;
        void Start(){previous=transform.position;}
        void LateUpdate()
        {
            if(Time.timeScale==0){previous=transform.position;return;}
            float dt=Mathf.Max(.001f,Time.deltaTime);
            Vector3 velocity=(transform.position-previous)/dt;previous=transform.position;
            float speed=velocity.magnitude;
            localVelocity=Vector3.Lerp(localVelocity,transform.InverseTransformDirection(velocity),1-Mathf.Exp(-dt*12));
            movementBlend=Mathf.Lerp(movementBlend,Mathf.Clamp01(speed/3),1-Mathf.Exp(-dt*10));
            phase+=dt*(2+Mathf.Min(speed,8)*2.7f);
            float stride=Mathf.Sin(phase), blend=movementBlend;
            float forward=localVelocity.z/Mathf.Max(1,localVelocity.magnitude);
            float lateral=localVelocity.x/Mathf.Max(1,localVelocity.magnitude);
            // Keep feet stepping along travel even while the torso aims sideways/backwards.
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(stride*29*blend*forward,0,-stride*17*blend*lateral);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-stride*29*blend*forward,0,stride*17*blend*lateral);
            if(leftKnee!=null)leftKnee.localRotation=Quaternion.Euler(Mathf.Max(0,-stride)*38*blend,0,0);
            if(rightKnee!=null)rightKnee.localRotation=Quaternion.Euler(Mathf.Max(0,stride)*38*blend,0,0);
            if(body!=null){body.localPosition=Vector3.up*(Mathf.Cos(phase*2)*.025f*blend+Mathf.Sin(Time.time*2.2f)*.006f);body.localRotation=Quaternion.Euler(Mathf.Clamp(localVelocity.z*1.0f,-5,7),stride*2.2f*blend,Mathf.Clamp(-localVelocity.x*1.2f,-6,6));}
            if(cape!=null)cape.localRotation=Quaternion.Euler(8+Mathf.Sin(phase)*3+blend*16,Mathf.Sin(phase*.6f)*3-lateral*blend*8,0);
        }
    }
}






