using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // A separate piece of equipment on the common chest bone. The inactive gun is
    // presentation only; the active gun and all combat sockets remain untouched.
    public sealed class TacticalBackpackVisual : MonoBehaviour
    {
        DetailedHero3D hero;
        Transform pack, gunMount;
        DetailedHeroEquipment3D.WeaponSockets stowed;
        string stowedKey;
        readonly List<Mesh> meshes = new List<Mesh>();
        Material canvas, leather, steel;

        public int TriangleCount
        {
            get { int count=0; foreach(var mesh in meshes) if(mesh) count+=mesh.triangles.Length/3; return count; }
        }

        public bool Owns(Renderer renderer) => renderer &&
            ((pack && renderer.transform.IsChildOf(pack)) ||
             (gunMount && renderer.transform.IsChildOf(gunMount)));

        public static TacticalBackpackVisual Create(DetailedHero3D hero)
        {
            if (!hero || !hero.chest || !hero.pelvis) return null;
            var visual = hero.gameObject.AddComponent<TacticalBackpackVisual>();
            visual.hero = hero;
            visual.Build();
            return visual;
        }

        void Build()
        {
            var tex = Resources.Load<Texture2D>("Art/TacticalBackpackAtlas");
            if (!tex) Debug.LogError("Tactical backpack atlas is missing from Resources");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            canvas = MakeMaterial(shader, tex, new Color(.80f,.74f,.64f), "Field pack / worn olive canvas");
            leather = MakeMaterial(shader, tex, new Color(.74f,.64f,.52f), "Field pack / leather reinforcement");
            steel = MakeMaterial(shader, null, new Color(.27f,.29f,.28f), "Field pack / dull steel");
            pack = new GameObject("Tactical pack / chest-following").transform;
            pack.SetParent(hero.chest, false);
            // Chest's pivot is at y=1.25. The pack sits behind the jacket, high
            // enough to clear the belt and below the fixed respirator/neck.
            pack.localPosition = new Vector3(0,.035f,-.225f);
            // Every pack component has its own curved silhouette. The dense main
            // shell carries fabric folds and tapered side walls rather than being
            // a tessellated cube with a photograph on it.
            Loft("Tailored rucksack shell",pack,canvas,.18f,.225f,.118f,.50f,.005f,
                Vector3.zero,64,48);
            Loft("Rolled top with gathered fabric",pack,canvas,.175f,.175f,.102f,.105f,.27f,
                new Vector3(0,0,-.008f),40,20);
            Loft("Lower rolled bedroll",pack,leather,.18f,.18f,.055f,.10f,-.25f,
                new Vector3(0,0,-.04f),36,18);
            for(int side=-1;side<=1;side+=2)
            {
                Loft("Rear cargo pocket",pack,canvas,.078f,.085f,.042f,.18f,-.115f,
                    new Vector3(side*.092f,0,-.126f),32,24);
                Loft("Side radio pouch",pack,canvas,.055f,.06f,.07f,.205f,-.07f,
                    new Vector3(side*.205f,0,-.015f),32,20);
                Ribbon("Padded shoulder harness",pack,leather,
                    new Vector3(side*.125f,.23f,.115f),new Vector3(side*.19f,-.15f,.11f),.035f,26);
                Ribbon("Vertical outer retention",pack,leather,
                    new Vector3(side*.112f,.245f,-.171f),new Vector3(side*.112f,-.2f,-.167f),.029f,26);
                Panel("Compression buckle",pack,steel,new Vector3(side*.112f,-.015f,-.177f),new Vector3(.038f,.045f,.016f));
                Panel("Gun bracket",pack,steel,new Vector3(side*.205f,.02f,-.01f),new Vector3(.025f,.07f,.11f));
            }
            for(int row=0;row<3;row++)
                Ribbon("Horizontal compression webbing",pack,leather,
                    new Vector3(-.208f,-.17f+row*.16f,-.136f),
                    new Vector3(.208f,-.17f+row*.16f,-.136f),.020f,30);
            for(int row=0;row<4;row++)for(int column=-1;column<=1;column++)
                Panel("MOLLE loop",pack,leather,
                    new Vector3(column*.071f,-.18f+row*.085f,-.170f),
                    new Vector3(.047f,.010f,.008f));
            Panel("Center quick-release",pack,steel,new Vector3(0,.23f,-.171f),new Vector3(.055f,.025f,.018f));
        }

        Material MakeMaterial(Shader shader, Texture2D texture, Color tint, string name)
        {
            var m = new Material(shader) { name=name, color=tint };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",tint);
            if (texture)
            {
                m.mainTexture=texture;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap",texture);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",.15f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic",0);
            return m;
        }

        void Loft(string name, Transform parent, Material material,float bottomWidth,float topWidth,
            float depth,float height,float centerY,Vector3 offset,int around,int rows)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3 Point(int row,int side)
            {
                float t=row/(float)(rows-1),width=Mathf.Lerp(bottomWidth,topWidth,t);
                float a=(side+.5f)*Mathf.PI*2/around;
                float seam=Mathf.Sin(t*Mathf.PI),pleat=Mathf.Sin(a*8+t*12)*.006f*seam;
                float shoulder=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1f,t));
                float x=Mathf.Cos(a)*(width*(1-.055f*shoulder)+pleat);
                float z=Mathf.Sin(a)*(depth+pleat*.65f);
                z+=.004f*Mathf.Sin(t*17+a*4)*seam;
                return offset+new Vector3(x,centerY+(t-.5f)*height,z);
            }
            for(int row=0;row<rows-1;row++)for(int side=0;side<around;side++)
            {
                int next=(side+1)%around;
                var p0=Point(row,side);var p1=Point(row,next);
                var p2=Point(row+1,side);var p3=Point(row+1,next);
                int first=vertices.Count;vertices.AddRange(new[]{p0,p1,p2,p3});
                Vector3 middle=(p0+p1+p2+p3)*.25f;
                // Atlas: upper-left is wearer-facing, upper-right outside,
                // lower-left left side, lower-right right side.
                Vector3 local=middle-offset;
                int cell=local.z>depth*.5f?0:local.z<-depth*.5f?1:local.x<0?2:3;
                float tileU=(cell%2)*.5f,tileV=cell<2?.5f:0;
                float v0=row/(float)(rows-1),v1=(row+1)/(float)(rows-1);
                float U(Vector3 p)=>Mathf.Clamp01(.5f+.5f*(cell<2?(p.x-offset.x)/Mathf.Max(.01f,topWidth):(p.z-offset.z)/Mathf.Max(.01f,depth)));
                uv.AddRange(new[]{new Vector2(tileU+.01f+U(p0)*.48f,tileV+.01f+v0*.48f),
                    new Vector2(tileU+.01f+U(p1)*.48f,tileV+.01f+v0*.48f),
                    new Vector2(tileU+.01f+U(p2)*.48f,tileV+.01f+v1*.48f),
                    new Vector2(tileU+.01f+U(p3)*.48f,tileV+.01f+v1*.48f)});
                triangles.AddRange(new[]{first,first+2,first+1,first+1,first+2,first+3});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var part=new GameObject(name);part.transform.SetParent(parent,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        void Ribbon(string name,Transform parent,Material material,Vector3 from,Vector3 to,float width,int steps)
        {
            var points=new List<Vector3>((steps+1)*2);
            var uv=new List<Vector2>((steps+1)*2);
            var triangles=new List<int>(steps*6);
            Vector3 across=Vector3.Cross((to-from).normalized,Vector3.forward).normalized*width*.5f;
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;
                Vector3 point=Vector3.Lerp(from,to,t);
                point.z-=.009f*Mathf.Sin(t*Mathf.PI);
                points.Add(point-across);points.Add(point+across);
                uv.Add(new Vector2(.65f+t*.13f,.67f));uv.Add(new Vector2(.65f+t*.13f,.82f));
                if(i==steps)continue;
                int a=i*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.SetUVs(0,uv);
            mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var part=new GameObject(name);part.transform.SetParent(parent,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        void Panel(string name,Transform parent,Material material,Vector3 center,Vector3 size)
        {
            // Low-poly stitched pads follow the pack's rectangular seams.
            var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=center;
            var mesh=new Mesh{name=name};float x=size.x*.5f,y=size.y*.5f,z=size.z*.5f;
            mesh.vertices=new[]{new Vector3(-x,-y,-z),new Vector3(x,-y,-z),new Vector3(-x,y,-z),new Vector3(x,y,-z),
                new Vector3(-x,-y,z),new Vector3(x,-y,z),new Vector3(-x,y,z),new Vector3(x,y,z)};
            // A small cloth/leather patch; do not shrink the entire four-view
            // rucksack illustration onto each buckle or pouch.
            mesh.uv=new[]{new Vector2(.67f,.68f),new Vector2(.80f,.68f),new Vector2(.67f,.82f),new Vector2(.80f,.82f),
                new Vector2(.67f,.68f),new Vector2(.80f,.68f),new Vector2(.67f,.82f),new Vector2(.80f,.82f)};
            mesh.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        public void Synchronize(VaultGame game)
        {
            if (!game || !hero) return;
            Gun spare=null;
            for(int slot=0;slot<game.AvailableWeaponSlotCount;slot++)
            {
                if(slot==game.ActiveWeaponSlot)continue;
                spare=game.WeaponInSlot(slot);if(spare!=null)break;
            }
            string key=spare==null?null:ManufacturerEquipment3D.WeaponKey(spare);
            if(key==stowedKey && (spare==null || stowed!=null))return;
            if(stowed?.root){stowed.root.gameObject.SetActive(false);Destroy(stowed.root.gameObject);}
            if(gunMount){gunMount.gameObject.SetActive(false);Destroy(gunMount.gameObject);}
            stowed=null;stowedKey=key;
            if(spare==null)return;
            bool pistol=Arsenal.IsOneHanded(VaultLanguage.DisplayModel(spare));
            gunMount=new GameObject(pistol?"Spare handgun / hip holster":"Spare long gun / right pack scabbard").transform;
            gunMount.SetParent(pistol?hero.pelvis:hero.chest,false);
            gunMount.localPosition=pistol?new Vector3(.225f,-.095f,.025f):new Vector3(.245f,.025f,-.255f);
            stowed=ManufacturerEquipment3D.BuildWeapon(gunMount,spare);
            if(!Valid(stowed))return;
            var root=stowed.root;
            root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;
            // The muzzle points upward on the pack, downward inside the hip holster.
            Vector3 forward=(stowed.muzzle.position-stowed.trigger.position).normalized;
            Vector3 target=pistol?Vector3.down:Vector3.up;
            root.rotation=Quaternion.FromToRotation(forward,target)*root.rotation;
            float length=Vector3.Distance(stowed.trigger.position,stowed.muzzle.position);
            float desired=pistol?.22f:.57f;
            root.localScale*=Mathf.Clamp(desired/Mathf.Max(.01f,length),.3f,2f);
            root.position+=gunMount.position-stowed.trigger.position;
            // Product-specific geometry is allowed only when it has a dedicated mesh.
            // The pack/holster never owns gameplay trigger or muzzle sockets.
        }

        static bool Valid(DetailedHeroEquipment3D.WeaponSockets s)=>s!=null&&s.root&&s.trigger&&s.muzzle;
        void OnDestroy()
        {
            if(canvas)Destroy(canvas);if(leather)Destroy(leather);if(steel)Destroy(steel);
            foreach(var mesh in meshes)if(mesh)Destroy(mesh);
        }
    }
}
