using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public sealed partial class VaultArt
    {
        static Mesh bevelMesh;
        static Mesh BeveledPlateMesh()
        {
            if(bevelMesh==null)bevelMesh=ProfileMesh("Chamfered armor plate",new[]{-.5f,-.4f,.4f,.5f},new[]{.42f,.5f,.5f,.42f},new[]{.42f,.5f,.5f,.42f},8,false);
            return bevelMesh;
        }
        // Every face has its own vertices: deliberate sculpted planes, never a smoothed primitive blob.
        static Mesh ProfileMesh(string name,float[] y,float[] w,float[] d,int segments=24,bool smooth=true)
        {
            var v=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();
            Vector3 Point(int r,int s)
            {
                float a=(s%segments)*Mathf.PI*2/segments;
                return new Vector3(Mathf.Sin(a)*w[r],y[r],Mathf.Cos(a)*d[r]);
            }
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 e)
            {
                int n=v.Count;v.AddRange(new[]{a,b,c,e});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
                tri.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            for(int r=0;r<y.Length-1;r++)for(int s=0;s<segments;s++)
            {
                Face(Point(r,s),Point(r+1,s),Point(r+1,s+1),Point(r,s+1));
                int k=uv.Count-4;float low=(y[r]-y[0])/(y[y.Length-1]-y[0]),high=(y[r+1]-y[0])/(y[y.Length-1]-y[0]);
                uv[k]=new Vector2(s/(float)segments,low);uv[k+1]=new Vector2(s/(float)segments,high);
                uv[k+2]=new Vector2((s+1)/(float)segments,high);uv[k+3]=new Vector2((s+1)/(float)segments,low);
            }
            int sideVertexCount=v.Count;
            for(int s=0;s<segments;s++)
            {
                Face(new Vector3(0,y[0],0),Point(0,s),Point(0,s+1),new Vector3(0,y[0],0));
                int r=y.Length-1;Face(new Vector3(0,y[r],0),Point(r,s+1),Point(r,s),new Vector3(0,y[r],0));
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
            if(smooth)
            {
                // Weld the side shading only; independent cap vertices preserve hard metal lips.
                var normal=mesh.normals;var sum=new Dictionary<Vector3,Vector3>();
                for(int i=0;i<sideVertexCount;i++){sum.TryGetValue(v[i],out var n);sum[v[i]]=n+normal[i];}
                for(int i=0;i<sideVertexCount;i++)normal[i]=sum[v[i]].normalized;
                mesh.normals=normal;
            }
            mesh.RecalculateBounds();return mesh;
        }
        Transform Sculpt(string name,Vector3 position,Transform parent,Color color,float[] y,float[] w,float[] d)
        {
            var t=Group(name,position,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=ProfileMesh(name,y,w,d);
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=WastelandMaterial(name,color);TrackMesh(t,t.GetComponent<MeshFilter>().sharedMesh);return t;
        }
        void Filigree(string name,Transform parent,Color color,float radius,params Vector3[] path)
        {
            var t=Group(name,Vector3.zero,parent);var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int p=0;p<path.Length;p++)
            {
                Vector3 forward=(path[Mathf.Min(p+1,path.Length-1)]-path[Mathf.Max(0,p-1)]).normalized;
                var right=Vector3.Cross(forward,Vector3.forward).normalized;if(right.sqrMagnitude<.1f)right=Vector3.right;
                var up=Vector3.Cross(forward,right).normalized;
                for(int s=0;s<8;s++){float a=s*Mathf.PI/4;v.Add(path[p]+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius);uv.Add(new Vector2(s/8f,p/(float)(path.Length-1)));}
            }
            for(int p=0;p<path.Length-1;p++)for(int s=0;s<8;s++){int a=p*8+s,b=p*8+(s+1)%8;tris.AddRange(new[]{a,b,a+8,b,b+8,a+8});}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=WastelandMaterial(name,color);TrackMesh(t,mesh);
        }
        void FinishCharacterSculpt(ActorMotion rig,Color armor,Color accent)
        {
            var body=rig.body;
            foreach(var mf in body.GetComponentsInChildren<MeshFilter>(true))
            {
                string n=mf.name;
                if(n=="Helmet")mf.sharedMesh=ProfileMesh("Forged domed helmet",new[]{-1f,-.8f,-.35f,.1f,.48f,.78f,.97f,1.04f},new[]{.24f,.34f,.48f,.52f,.49f,.38f,.17f,.015f},new[]{.3f,.39f,.51f,.53f,.5f,.4f,.18f,.015f});
                if(n=="Breastplate")mf.sharedMesh=ProfileMesh("Curved fitted cuirass",new[]{.12f,.22f,.36f,.52f,.64f,.72f,.77f},new[]{.23f,.28f,.34f,.345f,.30f,.245f,.19f},new[]{.21f,.24f,.29f,.3f,.26f,.20f,.14f});
                if(n=="Coat")mf.gameObject.SetActive(false);
                if(n=="Layered pauldron"||n=="Equipment shoulder blade")
                {
                    mf.sharedMesh=ProfileMesh("Dished shoulder shell",new[]{-.5f,-.15f,.2f,.45f,.58f},new[]{.52f,.61f,.56f,.36f,.07f},new[]{.5f,.58f,.53f,.34f,.08f});
                    mf.transform.localScale=n=="Equipment shoulder blade"?new Vector3(.28f,.12f,.34f):new Vector3(.23f,.12f,.35f);
                }
                if(n=="Equipment helmet crest")
                {
                    mf.sharedMesh=ProfileMesh("Swept helmet crest",new[]{-.5f,0,.45f,.7f},new[]{.40f,.34f,.14f,.01f},new[]{.44f,.50f,.29f,.015f});mf.transform.localScale=new Vector3(.075f,.20f,.36f);
                }
                if(n=="Equipment chest cuirass")mf.sharedMesh=ProfileMesh("Raised chest medallion",new[]{-.5f,-.25f,.25f,.5f},new[]{.18f,.44f,.48f,.10f},new[]{.2f,.43f,.48f,.10f});
                if(n=="Helmet crown")mf.transform.localScale=new Vector3(.025f,.06f,.34f);
                if(n=="Helmet cheek plate"||n=="Helmet jaw flange"||n=="Helmet breathing ridge"||n=="Chest plate clavicle"||n=="Breastplate bronze inlay"||n=="Chest plate articulated fauld")mf.gameObject.SetActive(false);
            }
            for(int s=-1;s<=1;s+=2)
            {
                var leg=s<0?rig.leftLeg:rig.rightLeg;
                foreach(var mf in leg.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(mf.name=="Boot"||mf.name=="Equipment toe cap"||mf.name=="Boot instep plate")
                        mf.sharedMesh=ProfileMesh("Sculpted armored sabaton",new[]{-.5f,-.3f,0,.3f,.5f},new[]{.40f,.50f,.49f,.41f,.29f},new[]{.43f,.53f,.51f,.38f,.23f});
                    if(mf.name=="Knee guard")mf.transform.localScale=new Vector3(.23f,.17f,.09f);
                    if(mf.name=="Equipment shin plate")mf.sharedMesh=ProfileMesh("Tapered embossed shin",new[]{-.5f,-.35f,.20f,.5f},new[]{.24f,.40f,.5f,.34f},new[]{.25f,.42f,.5f,.32f});
                    if(mf.name=="Greave lamellar overlap")mf.gameObject.SetActive(false);
                }
                Filigree("Chest plate engraved sweep",body,Gold,.011f,new Vector3(s*.04f,.72f,.20f),new Vector3(s*.15f,.67f,.245f),new Vector3(s*.25f,.57f,.25f),new Vector3(s*.26f,.44f,.22f),new Vector3(s*.17f,.30f,.23f),new Vector3(s*.05f,.22f,.24f));
                Filigree("Chest plate curling engraving",body,Bronze*1.5f,.008f,new Vector3(s*.05f,.57f,.30f),new Vector3(s*.11f,.61f,.285f),new Vector3(s*.19f,.57f,.27f),new Vector3(s*.20f,.50f,.267f),new Vector3(s*.14f,.47f,.285f),new Vector3(s*.11f,.51f,.29f));
                Filigree("Helmet crown engraved arc",body,Gold,.012f,new Vector3(s*.03f,1.245f,0),new Vector3(s*.12f,1.225f,.07f),new Vector3(s*.19f,1.17f,.13f),new Vector3(s*.21f,1.105f,.16f),new Vector3(s*.18f,1.015f,.18f));
                Filigree("Greave etched leaf",leg,Gold,.008f,new Vector3(0,-.36f,.16f),new Vector3(s*.065f,-.27f,.16f),new Vector3(s*.07f,-.16f,.15f),new Vector3(0,-.08f,.14f));
                for(int r=0;r<3;r++)
                {
                    var skirt=Sculpt("Chest plate curved waist lame",new Vector3(s*.18f,.04f-r*.065f,.11f),body,r==0?Bronze:armor*1.6f,new[]{-.03f,0,.03f},new[]{.15f,.17f,.15f},new[]{.11f,.14f,.11f});skirt.localRotation=Quaternion.Euler(-12,0,s*8);
                }
            }
            Part("Helmet nasal bridge",PrimitiveType.Cube,new Vector3(0,1.04f,.263f),new Vector3(.022f,.16f,.035f),Gold,body);
            Part("Helmet angular chin",PrimitiveType.Sphere,new Vector3(0,.907f,.19f),new Vector3(.20f,.11f,.14f),armor*1.5f,body);
            foreach(var mf in rig.GetComponentsInChildren<MeshFilter>(true))TrackMesh(mf.transform,mf.sharedMesh);
        }
        static void TrackMesh(Transform owner,Mesh mesh)
        {
            if(mesh==null||mesh==bevelMesh||mesh.name=="Cube"||mesh.name=="Sphere"||mesh.name=="Capsule"||mesh.name=="Cylinder")return;
            var lifetime=owner.GetComponent<GeneratedMeshLifetime>();if(lifetime==null)lifetime=owner.gameObject.AddComponent<GeneratedMeshLifetime>();lifetime.Add(mesh);
        }
        public static void SculptLimb(Transform part,bool forearm)
        {
            var mesh=ProfileMesh(forearm?"Fitted gauntlet body":"Fitted upper arm",new[]{-1f,-.75f,-.25f,.35f,.75f,1f},forearm?new[]{.28f,.37f,.49f,.51f,.43f,.28f}:new[]{.27f,.38f,.48f,.53f,.42f,.25f},new[]{.27f,.38f,.46f,.49f,.40f,.25f});
            part.GetComponent<MeshFilter>().sharedMesh=mesh;TrackMesh(part,mesh);
        }
        void RefineCharacter(Transform actor,ActorMotion rig,bool hero,Color armor,Color accent)
        {
            var body=rig.body;
            // Full cheek/chin silhouette with narrow brow and rounded crown instead of a capsule head.
            var helmet=body.Find("Helmet");helmet.GetComponent<MeshFilter>().sharedMesh=ProfileMesh("Sculpted closed helmet",
                new[]{-1f,-.65f,.05f,.72f,1f},new[]{.25f,.47f,.55f,.48f,.25f},new[]{.3f,.5f,.55f,.46f,.25f});
            helmet.localScale=new Vector3(.45f,.22f,.43f);
            var eyes=body.Find("Eyes");eyes.gameObject.SetActive(false);
            for(int s=-1;s<=1;s+=2)
            {
                Part("Visor lens",PrimitiveType.Cube,new Vector3(s*.095f,1.065f,.263f),new Vector3(.125f,.028f,.024f),accent,body,true);
                var brow=Part("Helmet swept brow",PrimitiveType.Cube,new Vector3(s*.105f,1.105f,.232f),new Vector3(.19f,.045f,.065f),Gold,body);brow.localRotation=Quaternion.Euler(0,s*12,-s*9);
                var cheek=Part("Helmet jaw flange",PrimitiveType.Cube,new Vector3(s*.145f,.915f,.19f),new Vector3(.13f,.15f,.11f),armor*2.4f,body);cheek.localRotation=Quaternion.Euler(0,-s*25,s*18);
                var chest=Part("Chest plate clavicle",PrimitiveType.Cube,new Vector3(s*.17f,.68f,.245f),new Vector3(.32f,.10f,.10f),new Color(.48f,.65f,.63f),body);chest.localRotation=Quaternion.Euler(0,s*15,-s*16);
                for(int r=0;r<3;r++)
                {
                    var rib=Part("Chest plate articulated fauld",PrimitiveType.Cube,new Vector3(s*.19f,.08f-r*.075f,.18f),new Vector3(.29f,.10f,.15f),r%2==0?armor*2.5f:Bronze,body);rib.localRotation=Quaternion.Euler(-12,0,s*12);
                }
                var leg=s<0?rig.leftLeg:rig.rightLeg;
                var greave=leg.Find("Armored greave");greave.GetComponent<MeshFilter>().sharedMesh=ProfileMesh("Anatomic greave",new[]{-1f,-.7f,.1f,.65f,1f},new[]{.32f,.4f,.55f,.48f,.35f},new[]{.35f,.47f,.58f,.48f,.36f});
                for(int r=0;r<3;r++)
                    Part("Greave lamellar overlap",PrimitiveType.Cube,new Vector3(0,-.15f-r*.075f,.115f),new Vector3(.23f-r*.025f,.08f,.09f),armor*2.6f,leg).localRotation=Quaternion.Euler(-10,0,0);
                Part("Greave gold spine",PrimitiveType.Cube,new Vector3(0,-.22f,.168f),new Vector3(.035f,.22f,.028f),Gold,leg);
                Part("Boot sole",PrimitiveType.Cube,new Vector3(0,-.535f,.075f),new Vector3(.31f,.045f,.46f),new Color(.25f,.22f,.21f),leg);
                Part("Boot instep plate",PrimitiveType.Cube,new Vector3(0,-.42f,.15f),new Vector3(.26f,.09f,.24f),armor*2.4f,leg).localRotation=Quaternion.Euler(-18,0,0);
                Part("Scarf mantle clasp",PrimitiveType.Sphere,new Vector3(s*.22f,.79f,.16f),new Vector3(.09f,.09f,.055f),Gold,body);
                Part("Belt leather pouch",PrimitiveType.Cube,new Vector3(s*.3f,.07f,-.09f),new Vector3(.18f,.20f,.18f),new Color(.38f,.21f,.12f),body);
                for(int stud=0;stud<3;stud++)Part("Pauldron rivet",PrimitiveType.Sphere,new Vector3(s*(.34f+stud*.07f),.755f-stud*.045f,.15f),Vector3.one*.036f,Gold,body);
            }
            Part("Helmet breathing ridge",PrimitiveType.Cube,new Vector3(0,.96f,.25f),new Vector3(.07f,.17f,.10f),Bronze,body).localRotation=Quaternion.Euler(12,0,0);
            for(int slit=0;slit<3;slit++)Part("Helmet respirator vent",PrimitiveType.Cube,new Vector3(0,.93f+slit*.027f,.31f),new Vector3(.043f,.009f,.01f),Dark,body);
            Part("Belt heraldic buckle",PrimitiveType.Cube,new Vector3(0,.07f,.252f),new Vector3(.16f,.16f,.065f),Gold,body).localRotation=Quaternion.Euler(0,0,45);
            Part("Chest plate dorsal spine",PrimitiveType.Cube,new Vector3(0,.5f,-.21f),new Vector3(.12f,.44f,.08f),Bronze,body);
            foreach(var mf in rig.GetComponentsInChildren<MeshFilter>(true))TrackMesh(mf.transform,mf.sharedMesh);
        }
        void RefineWeapon(Transform gun,Color color)
        {
            foreach(string name in new[]{"Gun receiver","Engraved housing","Shoulder stock"})
            {
                var t=gun.Find(name);var scale=t.localScale;t.localScale=new Vector3(scale.x,scale.z,scale.y);t.localRotation=Quaternion.Euler(90,0,0);
                t.GetComponent<MeshFilter>().sharedMesh=ProfileMesh("Contoured gun forging",new[]{-.5f,-.37f,-.1f,.24f,.45f,.5f},new[]{.24f,.45f,.53f,.48f,.35f,.24f},new[]{.24f,.44f,.51f,.46f,.35f,.24f},16);TrackMesh(t,t.GetComponent<MeshFilter>().sharedMesh);
            }
            Part("Magazine well",PrimitiveType.Cube,new Vector3(0,-.20f,.43f),new Vector3(.17f,.31f,.22f),Bronze,gun).localRotation=Quaternion.Euler(-12,0,0);
            Part("Stock leather cheek rest",PrimitiveType.Cube,new Vector3(0,.12f,-.13f),new Vector3(.22f,.075f,.3f),new Color(.43f,.18f,.12f),gun);
            Part("Stock shoulder pad",PrimitiveType.Cube,new Vector3(0,0,-.3f),new Vector3(.24f,.27f,.08f),Dark,gun);
            Part("Rear sight",PrimitiveType.Cube,new Vector3(0,.23f,.24f),new Vector3(.12f,.08f,.055f),Gold,gun);
            Part("Front sight",PrimitiveType.Cube,new Vector3(0,.17f,1.14f),new Vector3(.07f,.11f,.055f),Gold,gun);
            for(int r=0;r<3;r++)Part("Machined barrel collar",PrimitiveType.Cylinder,new Vector3(0,0,.87f+r*.13f),new Vector3(.2f,.025f,.2f),Bronze,gun).localRotation=Quaternion.Euler(90,0,0);
            for(int s=-1;s<=1;s+=2)
            {
                Filigree("Gun scroll engraving",gun,Gold,.008f,new Vector3(s*.15f,.10f,.28f),new Vector3(s*.155f,.13f,.38f),new Vector3(s*.155f,.115f,.53f),new Vector3(s*.15f,.065f,.59f),new Vector3(s*.15f,.03f,.49f),new Vector3(s*.15f,.07f,.43f));
                Part("Receiver side energy window",PrimitiveType.Sphere,new Vector3(s*.144f,.04f,.56f),new Vector3(.022f,.07f,.20f),color,gun,true);
            }
            // These assemblies share rigid sockets and are built only once. Inventory selects the silhouette.
            var variants=gun.gameObject.AddComponent<WeaponModelVariants>();
            for(int i=1;i<=4;i++)
            {
                var g=Group("Weapon assembly "+(GunEffect)i,Vector3.zero,gun);variants.assemblies[i-1]=g.gameObject;
                if(i==1)for(int s=-1;s<=1;s+=2)Part("Scatter secondary barrel",PrimitiveType.Cylinder,new Vector3(s*.13f,-.075f,1.03f),new Vector3(.16f,.23f,.16f),Bronze,g).localRotation=Quaternion.Euler(90,0,0);
                if(i==2)for(int s=-1;s<=1;s+=2)Part("Ricochet deflector fin",PrimitiveType.Cube,new Vector3(s*.20f,.05f,.66f),new Vector3(.075f,.34f,.35f),Gold,g).localRotation=Quaternion.Euler(0,0,-s*24);
                if(i==3)for(int r=0;r<3;r++)Part("Orbit induction drum",PrimitiveType.Cylinder,new Vector3(0,0,.78f+r*.13f),new Vector3(.39f,.04f,.39f),Bronze,g).localRotation=Quaternion.Euler(90,0,0);
                if(i==4)
                {
                    var crystal=Sculpt("Singularity energy crystal",new Vector3(0,.12f,.62f),g,color,new[]{-.16f,-.09f,.16f,.30f},new[]{.01f,.10f,.085f,.001f},new[]{.01f,.10f,.085f,.001f});
                    crystal.GetComponent<MeshRenderer>().sharedMaterial=Material(color,true);
                    for(int s=-1;s<=1;s+=2)Sculpt("Singularity fork",new Vector3(s*.19f,.05f,.93f),g,Gold,new[]{-.28f,-.20f,.16f,.28f},new[]{.03f,.045f,.025f,.001f},new[]{.055f,.065f,.035f,.001f}).localRotation=Quaternion.Euler(90,0,0);
                }
                g.gameObject.SetActive(false);
            }
        }
        public static void ConfigureWeapon(Transform weapon,GunEffect effect,Color color,int modelVariant=0)
        {
            var detailed=weapon.parent.GetComponent<DetailedHeroPlayer3D>();if(detailed!=null){detailed.Synchronize();return;}
            var models=weapon.GetComponent<WastelandWeaponModels>();
            int n=Mathf.Clamp(modelVariant,0,11);
            if(models!=null)for(int i=0;i<models.models.Length;i++)models.models[i].SetActive(i==n);
            var aimed=weapon.GetComponent<AimedWeapon>();if(aimed!=null){float length=n==2?.72f:n==1?.16f:n==5?.22f:n==8?.62f:.38f;aimed.muzzle.localPosition=new Vector3(0,0,n==11?1.18f:.72f+length);}
            weapon.parent.GetComponent<LayeredPlayer2D>()?.SetWeaponModel(Mathf.Clamp(modelVariant,0,Arsenal.ModelCount-1),color);

        }
    }
    public sealed class WeaponModelVariants : MonoBehaviour
    {
        public GameObject[] assemblies=new GameObject[4];
    }
    public sealed class GeneratedMeshLifetime : MonoBehaviour
    {
        readonly HashSet<Mesh> meshes=new HashSet<Mesh>();
        public void Add(Mesh mesh){meshes.Add(mesh);}
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
}
