using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // All surfaces are modeled geometry. No imported model, billboard or Unity primitive is used.
    public static class DetailedHeroEquipment3D
    {
        public sealed class WeaponSockets
        {
            public Transform root, trigger, support, stock, muzzle;
        }
        public sealed class EquipmentRig
        {
            public readonly List<Transform>[] slots = { new List<Transform>(), new List<Transform>(), new List<Transform>(), new List<Transform>(), new List<Transform>() };
            public void SetSlot(ArmorSlot slot, bool visible) { foreach (var t in slots[(int)slot]) if(t) t.gameObject.SetActive(visible); }
        }
        public static EquipmentRig Build(DetailedHero3D body,Func<string,Material> palette)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            if(palette==null)throw new ArgumentNullException(nameof(palette));
            var rig=new EquipmentRig();
            rig.slots[0].Add(BuildHead(body.head,palette));rig.slots[1].Add(BuildChest(body.chest,palette));
            rig.slots[1].Add(BuildShoulder(body.leftUpperArm,palette));
            rig.slots[2].Add(BuildForearm(body.leftForearm,palette));rig.slots[2].Add(BuildForearm(body.rightForearm,palette));
            rig.slots[2].Add(BuildGlove(body.leftHand,palette));rig.slots[2].Add(BuildGlove(body.rightHand,palette));
            rig.slots[3].Add(BuildKnee(body.leftShin,palette));rig.slots[3].Add(BuildKnee(body.rightShin,palette));
            rig.slots[3].Add(BuildThigh(body.leftThigh,palette,-1));rig.slots[3].Add(BuildThigh(body.rightThigh,palette,1));
            var leftBoot=BuildBootStraps(body.leftFoot,palette);leftBoot.SetParent(body.leftShin,true);rig.slots[4].Add(leftBoot);
            var rightBoot=BuildBootStraps(body.rightFoot,palette);rightBoot.SetParent(body.rightShin,true);rig.slots[4].Add(rightBoot);
            foreach(var slot in rig.slots)foreach(var t in slot)Compact(t);
            return rig;
        }
        static void Compact(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();var old=new List<GameObject>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.sharedMaterial||!f.sharedMesh)continue;
                if(!groups.TryGetValue(r.sharedMaterial,out var group)){group=new List<CombineInstance>();groups.Add(r.sharedMaterial,group);}
                group.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});old.Add(f.gameObject);
            }
            foreach(var item in groups)
            {
                var t=Group("Combined equipment / "+item.Key.name,root,Vector3.zero);var mesh=new Mesh{name=t.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(item.Value.ToArray(),true,true);
                t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=item.Key;t.gameObject.AddComponent<MeshOwner>().mesh=mesh;
            }
            foreach(var go in old){go.SetActive(false);UnityEngine.Object.Destroy(go);}
        }
        // Mesh ownership is attached to each renderer so repeated equipment changes release native meshes.
        sealed class MeshOwner : MonoBehaviour
        {
            public Mesh mesh;
            void OnDestroy() { if(mesh) UnityEngine.Object.Destroy(mesh); }
        }
        static Transform Group(string n, Transform p, Vector3 v)
        { var t=new GameObject(n).transform; t.SetParent(p,false);t.localPosition=v;return t; }
        static Transform Surface(string n,Transform p,List<Vector3> v,List<int> ix,Func<string,Material> m,string key)
        {
            var t=Group(n,p,Vector3.zero);var mesh=new Mesh{name=n};
            if(v.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);mesh.SetTriangles(ix,0);var uv=new Vector2[v.Count];
            for(int i=0;i<v.Count;i++)uv[i]=new Vector2(v[i].x*4+v[i].z*2,v[i].y*4+v[i].z*2);
            mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m(key);
            t.gameObject.AddComponent<MeshOwner>().mesh=mesh;return t;
        }
        // Elliptic cross-section loft, closed at both ends, with authored radial contour perturbation.
        static Transform Loft(string n,Transform p,Func<string,Material> m,string key,Vector3 center,Vector3 size,int rings=12,int sides=32,float irregular=0)
        {
            if(rings>=8){rings*=2;sides*=2;}
            var v=new List<Vector3>();var ix=new List<int>();
            for(int j=0;j<=rings;j++)
            {
                float u=j/(float)rings, y=(u-.5f)*size.y;
                float r=Mathf.Sqrt(Mathf.Max(.002f,1-Mathf.Pow((u-.5f)*2,4)));
                for(int i=0;i<=sides;i++) {float a=i*Mathf.PI*2/sides;float d=1+irregular*Mathf.Sin(a*7+u*13)*Mathf.Sin(u*Mathf.PI);v.Add(center+new Vector3(Mathf.Cos(a)*size.x*.5f*r*d,y,Mathf.Sin(a)*size.z*.5f*r*d));}
            }
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            int lo=v.Count;v.Add(center+Vector3.down*size.y*.5f);int hi=v.Count;v.Add(center+Vector3.up*size.y*.5f);
            for(int i=0;i<sides;i++){ix.AddRange(new[]{lo,i,i+1,hi,rings*(sides+1)+i+1,rings*(sides+1)+i});}
            return Surface(n,p,v,ix,m,key);
        }
        // Hollow machined tube around local Z. Front/back rims are geometry, never a black painted cap.
        static Transform Tube(string n,Transform p,Func<string,Material> m,string key,Vector3 c,float radius,float length,float bore=.65f,int sides=32)
        {
            var v=new List<Vector3>();var ix=new List<int>();
            for(int ring=0;ring<4;ring++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides,r=radius*(ring>=2?bore:1);v.Add(c+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,(ring%2)*length));}
            for(int surface=0;surface<4;surface++){int ra=surface==0?0:surface==1?3:surface==2?2:1,rb=surface==0?1:surface==1?2:surface==2?0:3;for(int i=0;i<sides;i++){int a=ra*(sides+1)+i,b=rb*(sides+1)+i;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}}
            for(int i=0;i<ix.Count;i+=3){int swap=ix[i+1];ix[i+1]=ix[i+2];ix[i+2]=swap;}
            return Surface(n,p,v,ix,m,key);
        }
        static Transform Rod(string n,Transform p,Func<string,Material> m,string key,Vector3 a,Vector3 b,float r)
        { var t=Tube(n,p,m,key,Vector3.zero,r,Vector3.Distance(a,b),.02f,12);t.localPosition=a;t.localRotation=Quaternion.LookRotation(b-a);return t; }
        static void FittedBand(string name,Transform parent,Func<string,Material> m,float y,float rx,float rz,float width)
        {
            // Author directly about Y: rotating a mesh with baked-in center rotates the attachment too.
            const int N=48;var v=new List<Vector3>();var ix=new List<int>();
            for(int row=0;row<2;row++)for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N;v.Add(new Vector3(Mathf.Cos(a)*rx,y+(row-.5f)*width,Mathf.Sin(a)*rz));}
            for(int i=0;i<N;i++){int a=i,b=i+N+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}Surface(name,parent,v,ix,m,"LeatherDark");
        }
        static void Ribbon(string n,Transform p,Func<string,Material> m,string key,Vector3 a,Vector3 b,float width,float thick=.008f)
        {
            var d=(b-a).normalized;var side=Vector3.Cross(d,Vector3.forward).normalized*width*.5f;if(side.sqrMagnitude<.0001f)side=Vector3.right*width*.5f;
            var v=new List<Vector3>();foreach(float z in new[]{-thick*.5f,thick*.5f}){v.Add(a-side+Vector3.forward*z);v.Add(b-side+Vector3.forward*z);v.Add(b+side+Vector3.forward*z);v.Add(a+side+Vector3.forward*z);}
            Surface(n,p,v,new List<int>{0,1,2,0,2,3,4,6,5,4,7,6,0,5,1,0,4,5,1,6,2,1,5,6,2,7,3,2,6,7,3,4,0,3,7,4},m,key);
        }
        static void Seam(Transform p,Func<string,Material> m,Vector3 a,Vector3 b,int count)
        {for(int i=0;i<count;i++){float u=(i+.25f)/count;Rod("Individual saddle stitch",p,m,"Stitch",Vector3.Lerp(a,b,u),Vector3.Lerp(a,b,u+.38f/count),.0013f);}}
        static void Buckle(Transform p,Func<string,Material> m,Vector3 c,float w,float h)
        {Vector3 a=c+new Vector3(-w/2,-h/2,0),b=c+new Vector3(w/2,-h/2,0),d=c+new Vector3(-w/2,h/2,0),e=c+new Vector3(w/2,h/2,0);Rod("Buckle lower",p,m,"Brass",a,b,.004f);Rod("Buckle upper",p,m,"Brass",d,e,.004f);Rod("Buckle left",p,m,"Brass",a,d,.004f);Rod("Buckle right",p,m,"Brass",b,e,.004f);Rod("Buckle tongue",p,m,"Steel",(a+b)/2,(d+e)/2,.0028f);}
        static void Rivet(Transform p,Func<string,Material> m,Vector3 c,float radius=.006f)
        {Loft("Peened rivet",p,m,"Brass",c,new Vector3(radius*2,radius*2,.006f),4,12);}
        static void Pouch(Transform p,Func<string,Material> m,Vector3 c,Vector3 size)
        {
            SewnLeatherVolume("Box sewn pouch / folded side gussets",p,m,"Leather",c,size);
            SewnLeatherVolume("Overlapping formed leather flap",p,m,"LeatherDark",c+new Vector3(0,size.y*.24f,size.z*.48f),new Vector3(size.x*.97f,size.y*.44f,.014f));
            Ribbon("Pouch closing tongue",p,m,"Leather",c+new Vector3(0,size.y*.32f,size.z*.59f),c+new Vector3(0,-size.y*.15f,size.z*.59f),size.x*.2f);
            Buckle(p,m,c+new Vector3(0,-size.y*.05f,size.z*.63f),size.x*.22f,size.y*.16f);
            for(int s=-1;s<=1;s+=2)Seam(p,m,c+new Vector3(s*size.x*.42f,-size.y*.37f,size.z*.47f),c+new Vector3(s*size.x*.42f,size.y*.30f,size.z*.47f),12);
            Rivet(p,m,c+new Vector3(size.x*.34f,-size.y*.34f,size.z*.5f));Rivet(p,m,c+new Vector3(-size.x*.34f,-size.y*.34f,size.z*.5f));
            // Rolled gusset welt and separately sewn flap edge sit on the leather surface.
            for(int s=-1;s<=1;s+=2){Vector3 previous=Vector3.zero;for(int i=0;i<=18;i++){float u=i/18f;Vector3 q=c+new Vector3(s*size.x*(.36f+.07f*Mathf.Sin(u*Mathf.PI)),Mathf.Lerp(-size.y*.39f,size.y*.39f,u),size.z*(.36f+.07f*Mathf.Sin(u*Mathf.PI)));if(i>0)Rod("Pouch rolled gusset piping",p,m,"LeatherDark",previous,q,.0021f);previous=q;}}
            Seam(p,m,c+new Vector3(-size.x*.35f,size.y*.22f,size.z*.56f),c+new Vector3(size.x*.35f,size.y*.22f,size.z*.56f),18);
            for(int i=0;i<3;i++)Loft("Punched closure tongue hole",p,m,"Dark",c+new Vector3(0,(-.17f+i*.07f)*size.y,size.z*.607f),new Vector3(.003f,.004f,.0014f),3,8);
            for(int i=0;i<5;i++){float x=(i-2)*size.x*.14f;Rod("Leather flap edge wear",p,m,"Stitch",c+new Vector3(x,size.y*.065f,size.z*.535f),c+new Vector3(x+.003f,size.y*.068f,size.z*.535f),.0007f);}
            for(int side=-1;side<=1;side+=2)
            {
                // Folded gussets give the pocket a readable side plane and a recessed seam.
                var a=c+new Vector3(side*size.x*.49f,-size.y*.34f,-size.z*.23f);
                var b=c+new Vector3(side*size.x*.51f,0,-size.z*.08f);
                var d=c+new Vector3(side*size.x*.48f,size.y*.34f,-size.z*.24f);
                Rod("Recessed side gusset fold lower",p,m,"LeatherDark",a,b,.0018f);
                Rod("Recessed side gusset fold upper",p,m,"LeatherDark",b,d,.0018f);
            }
        }
        static void SewnLeatherVolume(string name,Transform parent,Func<string,Material> m,string key,Vector3 c,Vector3 size)
        {
            const int Rows=32,N=64;var v=new List<Vector3>();var ix=new List<int>();
            for(int row=0;row<=Rows;row++)for(int i=0;i<=N;i++)
            {
                float t=row/(float)Rows,a=i*Mathf.PI*2/N;
                float bevel=1-.12f*Mathf.Pow(Mathf.Abs(2*t-1),12);
                float x=Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.32f);
                float z=Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.32f);
                float tension=.002f*Mathf.Sin(t*Mathf.PI)*Mathf.Sin(a*3+t*8);
                v.Add(c+new Vector3(x*(size.x*.5f*bevel+tension),(t-.5f)*size.y,z*(size.z*.5f*bevel+tension*.4f)));
                if(row<Rows&&i<N){int k=row*(N+1)+i,b=k+N+1;ix.AddRange(new[]{k,b,k+1,k+1,b,b+1});}
            }
            int bottom=v.Count;v.Add(c-Vector3.up*size.y*.5f);int top=v.Count;v.Add(c+Vector3.up*size.y*.5f);
            for(int i=0;i<N;i++)ix.AddRange(new[]{bottom,i,i+1,top,Rows*(N+1)+i+1,Rows*(N+1)+i});
            Surface(name,parent,v,ix,m,key);
        }
        // Convex oval armor surface with a curved cross-section and irregular hem.
        static void Plate(Transform p,Func<string,Material> m,Vector3 c,Vector3 size)
        {
            const int N=80,Rows=18;var v=new List<Vector3>{c+new Vector3(0,0,size.z)};var ix=new List<int>();
            for(int j=1;j<=Rows;j++)for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N,r=j/(float)Rows;float dent=Mathf.Sin(a*5+r*9)*Mathf.Sin(a*3-r*12)*.0016f*r;float impact=-.0028f*Mathf.Exp(-Mathf.Pow((r-.57f)/.12f,2)-Mathf.Pow((a-2.1f)/.23f,2));v.Add(c+new Vector3(Mathf.Cos(a)*size.x*.5f*r,Mathf.Sin(a)*size.y*.5f*r,size.z*Mathf.Sqrt(1-r*r*.88f)+dent+impact));}
            for(int i=0;i<N;i++)ix.AddRange(new[]{0,1+i,2+i});
            for(int j=0;j<Rows-1;j++)for(int i=0;i<N;i++){int a=1+j*(N+1)+i,b=a+N+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            // A forged plate has an actual back and cut edge, not a single floating surface.
            int frontCount=v.Count;int frontIndices=ix.Count;
            for(int i=0;i<frontCount;i++)v.Add(v[i]-Vector3.forward*.005f);
            for(int i=0;i<frontIndices;i+=3)ix.AddRange(new[]{ix[i]+frontCount,ix[i+2]+frontCount,ix[i+1]+frontCount});
            int edge=1+(Rows-1)*(N+1);
            for(int i=0;i<N;i++){int a=edge+i,b=a+1;ix.AddRange(new[]{a,b,a+frontCount,b,b+frontCount,a+frontCount});}
            Surface("Forged steel plate / cut edge and sculpted impact",p,v,ix,m,"Steel");
            Vector3 prev=Vector3.zero;for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N;Vector3 q=c+new Vector3(Mathf.Cos(a)*size.x*.5f,Mathf.Sin(a)*size.y*.5f,size.z*.35f);if(i>0)Rod("Rolled rusted plate rim",p,m,"Rust",prev,q,.004f);prev=q;}
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Rivet(p,m,c+new Vector3(Mathf.Cos(a)*size.x*.43f,Mathf.Sin(a)*size.y*.43f,size.z*.62f),.006f);}
            for(int i=0;i<13;i++){float a=i*2.39996f,r=.18f+.23f*Mathf.Sin(i*7.1f);Loft("Scattered oxidation pit",p,m,"Rust",c+new Vector3(Mathf.Cos(a)*size.x*r,Mathf.Sin(a)*size.y*r,size.z*.95f),new Vector3(.009f+i%3*.003f,.014f,.002f),3,8);}
            for(int i=0;i<10;i++){float a=i*2.39996f;Vector3 q=c+new Vector3(Mathf.Cos(a)*size.x*.46f,Mathf.Sin(a)*size.y*.46f,size.z*.48f);Rod("Exposed steel edge abrasion",p,m,"Steel",q,q+new Vector3(.005f,.003f,.0003f),.0010f);}
        }
        static Transform BuildShoulder(Transform arm,Func<string,Material> m)
        {
            var g=Group("Trail Warden / conforming deltoid cap",arm,Vector3.zero);
            var v=new List<Vector3>();var ix=new List<int>();const int Rows=30,Columns=64;
            Vector3 Point(int j,int i){float u=j/(float)Rows,a=Mathf.Lerp(-.48f,1.72f,i/(float)Columns);float r=.064f+Mathf.Sin(u*Mathf.PI*.69f)*.040f;return new Vector3(-.006f-Mathf.Cos(a)*r,.014f-u*.185f,Mathf.Sin(a)*r);}
            for(int j=0;j<=Rows;j++)for(int i=0;i<=Columns;i++)v.Add(Point(j,i));
            for(int j=0;j<Rows;j++)for(int i=0;i<Columns;i++){int a=j*(Columns+1)+i,b=a+Columns+1;ix.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            Surface("Thin curved steel over deltoid",g,v,ix,m,"Steel");
            for(int edge=0;edge<2;edge++)for(int i=1;i<=Columns;i++)Rod("Shoulder rolled lower and upper edge",g,m,"Rust",Point(edge*Rows,i-1),Point(edge*Rows,i),.003f);
            for(int edge=0;edge<2;edge++)for(int j=1;j<=Rows;j++)Rod("Shoulder side rolled edge",g,m,"Rust",Point(j-1,edge*Columns),Point(j,edge*Columns),.003f);
            for(int i=2;i<Columns;i+=5)Rivet(g,m,Point(Rows-1,i),.004f);
            for(int i=0;i<14;i++){int row=2+i%8,col=3+(i*7)%20;var q=Point(row,col);Loft("Shoulder oxidation chip",g,m,"Rust",q*1.008f,new Vector3(.007f,.011f,.004f),3,8);}
            for(int i=2;i<Columns-1;i+=3){Vector3 a=Point(Rows,i),b=Point(Rows,i+1);Rod("Shoulder edge abrasion",g,m,"Steel",a,b,.0011f);}
            return g;
        }

        public static Transform BuildHead(Transform head,Func<string,Material> m)
        {
            var g=Group("Trail Warden / goggles and respirator",head,new Vector3(0,.105f,-.025f));g.localScale=Vector3.one*.90f;
            // Local origin is head center, facing +Z; head radius .115, head height .26.
            for(int s=-1;s<=1;s+=2){
                var eye=Tube("Dark steel goggle rim",g,m,"Steel",new Vector3(s*.050f,.015f,.102f),.038f,.016f,.88f,40);
                Loft("Deep teal optical lens",g,m,"TealGlass",new Vector3(s*.050f,.015f,.118f),new Vector3(.065f,.063f,.009f),8,32);
                Vector3 previous=Vector3.zero;for(int i=0;i<=20;i++){float a=Mathf.Lerp(-.60f,Mathf.PI*.5f,i/20f);Vector3 q=new Vector3(s*.105f*Mathf.Cos(a),.025f,.025f-.080f*Mathf.Sin(a));if(i>0)Ribbon("Conforming goggle headband",g,m,"LeatherDark",previous,q,.020f,.004f);previous=q;}
                var filter=Group("Dual replaceable filter",g,new Vector3(s*.069f,-.090f,.108f));filter.localRotation=Quaternion.Euler(0,s*22,0);
                Tube("Filter dark steel lip",filter,m,"Steel",Vector3.zero,.030f,.021f,.87f,32);
                Loft("Recessed charcoal filter",filter,m,"Dark",new Vector3(0,0,.004f),new Vector3(.055f,.055f,.008f),6,24);
                for(int row=-3;row<=3;row++)for(int col=-3;col<=3;col++){float x=col*.007f,y=row*.007f;if(x*x+y*y>.00061f)continue;Tube("Actual perforated filter grille",filter,m,"Brass",new Vector3(x,y,.022f),.0026f,.004f,.67f,8);}
                Ribbon("Respirator cheek seal",g,m,"LeatherDark",new Vector3(s*.05f,-.075f,.11f),new Vector3(s*.12f,-.015f,-.02f),.026f);
            }
            Loft("Anatomical respirator shell",g,m,"LeatherDark",new Vector3(0,-.063f,.097f),new Vector3(.135f,.125f,.090f),12,32,.04f);
            Tube("Central exhalation valve",g,m,"Steel",new Vector3(0,-.105f,.134f),.020f,.016f,.72f,24);
            Rod("Nose bridge",g,m,"Steel",new Vector3(-.016f,.018f,.114f),new Vector3(.016f,.018f,.114f),.003f);
            return g;
        }
        public static Transform BuildChest(Transform chest,Func<string,Material> m)
        {
            var g=Group("Trail Warden / load-bearing harness",chest,Vector3.zero);
            float JacketDepth(float x,float y){
                float[] heights={-.29f,-.24f,-.14f,-.03f,.07f,.16f,.20f};float[] widths={.205f,.226f,.192f,.19f,.232f,.246f,.152f};float[] depths={.129f,.137f,.123f,.133f,.145f,.128f,.085f};
                int row=0;while(row<5&&y>heights[row+1])row++;float t=Mathf.InverseLerp(heights[row],heights[row+1],y);float rx=Mathf.Lerp(widths[row],widths[row+1],t),rz=Mathf.Lerp(depths[row],depths[row+1],t);float u=Mathf.Clamp(x/rx,-.98f,.98f);return rz*Mathf.Sqrt(1-u*u)+.014f;
            }
                        // Collapsed field hood: a broad draped shell with a recessed opening and an actual rolled edge.
            var hood=new List<Vector3>();var hoodIndices=new List<int>();
            const int hn=64,hr=28;
            for(int row=0;row<=hr;row++)for(int col=0;col<=hn;col++){
                float t=row/(float)hr,a=Mathf.PI*col/hn;
                float spread=Mathf.Lerp(.104f,.172f,Mathf.Sin(t*Mathf.PI*.75f));
                float pleat=.008f*Mathf.Sin(a*7+t*4)*Mathf.Sin(t*Mathf.PI);
                hood.Add(new Vector3(Mathf.Cos(a)*(spread+pleat),.235f-t*.24f+Mathf.Sin(a)*.015f,-.035f-Mathf.Sin(a)*(.085f+.075f*Mathf.Sin(t*Mathf.PI))));
                if(row<hr&&col<hn){int k=row*(hn+1)+col,b=k+hn+1;hoodIndices.AddRange(new[]{k,b,k+1,k+1,b,b+1});}
            }
            // The hood can be seen from either side around the neck opening.
            int faceCount=hoodIndices.Count,hoodVertexCount=hood.Count;
            // Separate lining vertices: reversed triangles sharing vertices cancel their normals.
            for(int i=0;i<hoodVertexCount;i++)hood.Add(hood[i]+Vector3.forward*.002f);
            for(int i=0;i<faceCount;i+=3)hoodIndices.AddRange(new[]{hoodIndices[i]+hoodVertexCount,hoodIndices[i+2]+hoodVertexCount,hoodIndices[i+1]+hoodVertexCount});
            Surface("Collapsed hood / radial cloth drape",g,hood,hoodIndices,m,"Cloth");
            for(int i=1;i<=hn;i++)Rod("Hood folded opening hem",g,m,"ClothDark",hood[i-1],hood[i],.003f);
            for(int row=1;row<=hr;row++)foreach(int edge in new[]{0,hn})Rod("Hood sewn side edge",g,m,"ClothDark",hood[(row-1)*(hn+1)+edge],hood[row*(hn+1)+edge],.002f);
            var wrap=new List<Vector3>();var wi=new List<int>();
            for(int j=0;j<=10;j++)for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48,u=j/10f;float r=1+Mathf.Sin(a*3+u*8)*.035f+Mathf.Sin(u*Mathf.PI*5+a)*.045f;wrap.Add(new Vector3(Mathf.Cos(a)*.106f*r,.177f+u*.085f+Mathf.Sin(a*2+u*7)*.006f,Mathf.Sin(a)*.094f*r+.007f));}
            for(int j=0;j<10;j++)for(int i=0;i<48;i++){int a=j*49+i,b=a+49;wi.AddRange(new[]{a,b,a+1,a+1,b,b+1});}Surface("Open folded olive cloth neck wrap",g,wrap,wi,m,"Cloth");
            for(int fold=0;fold<4;fold++){
                Vector3 previous=Vector3.zero;
                for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;Vector3 q=new Vector3(Mathf.Cos(a)*.101f,.184f+fold*.021f+Mathf.Sin(a*2+fold)*.006f,Mathf.Sin(a)*.089f+.007f);if(i>0)Rod("Soft scarf wrap fold",g,m,"Cloth",previous,q,.0025f);previous=q;}
            }
            var sv=new List<Vector3>();var si=new List<int>();
            for(int row=0;row<=10;row++)for(int col=0;col<=16;col++){
                float u=col/16f,t=row/10f;
                float drape=Mathf.Sin(t*Mathf.PI)*(.009f*Mathf.Sin(u*7*Mathf.PI+t*2)+.004f*Mathf.Sin(u*13*Mathf.PI));
                sv.Add(new Vector3((u-.5f)*Mathf.Lerp(.23f,.06f,t),Mathf.Lerp(.205f,.045f,t)+Mathf.Sin(u*Mathf.PI)*.017f,Mathf.Lerp(.090f,.151f,t)+drape));
            }
            for(int row=0;row<10;row++)for(int col=0;col<16;col++){int a=row*17+col,b=a+17;si.AddRange(new[]{a,b,a+1,a+1,b,b+1});}Surface("Draped scarf front bib",g,sv,si,m,"Cloth");
            for(int col=1;col<16;col+=2){Vector3 q=sv[10*17+col];Rod("Frayed scarf hem thread",g,m,"Cloth",q,q+new Vector3(.001f,-.004f-(col%3)*.001f,.001f),.0008f);}
            for(int s=-1;s<=1;s+=2){
                Vector3 a=new Vector3(s*.14f,.16f,0),b=new Vector3(s*.125f,-.25f,0);a.z=JacketDepth(a.x,a.y);b.z=JacketDepth(b.x,b.y);
                Vector3 previousFront=a;
                for(int segment=1;segment<=20;segment++){Vector3 nextFront=Vector3.Lerp(a,b,segment/20f);nextFront.z=JacketDepth(nextFront.x,nextFront.y);Ribbon("Conforming shoulder harness",g,m,"Leather",previousFront,nextFront,.040f,.008f);Seam(g,m,previousFront+Vector3.right*.014f,nextFront+Vector3.right*.014f,2);Seam(g,m,previousFront-Vector3.right*.014f,nextFront-Vector3.right*.014f,2);previousFront=nextFront;}
                Vector3 clasp=Vector3.Lerp(a,b,.33f);clasp.z=JacketDepth(clasp.x,clasp.y)+.006f;Buckle(g,m,clasp,.046f,.055f);
                for(int i=0;i<5;i++){Vector3 q=Vector3.Lerp(a,b,.49f+i*.08f);q.z=JacketDepth(q.x,q.y)+.007f;Rivet(g,m,q,.0035f);}
                // A straight rear strap cut through the convex jacket and only its
                // triangular ends remained visible. Follow the garment surface.
                Vector3 lastBack=new Vector3(s*.14f,.16f,-JacketDepth(s*.14f,.16f));
                for(int segment=1;segment<=16;segment++){
                    float t=segment/16f;Vector3 nextBack=new Vector3(Mathf.Lerp(s*.14f,-s*.125f,t),Mathf.Lerp(.16f,-.23f,t),0);nextBack.z=-JacketDepth(nextBack.x,nextBack.y);
                    Ribbon("Rear harness conforming segment",g,m,"Leather",lastBack,nextBack,.039f,.008f);lastBack=nextBack;
                }
                Pouch(g,m,new Vector3(s*.115f,-.265f,.17f),new Vector3(.092f,.15f,.065f));
                Pouch(g,m,new Vector3(s*.235f,-.25f,.015f),new Vector3(.075f,.145f,.07f));
            }
            Ribbon("Diagonal chest sling",g,m,"Leather",new Vector3(.185f,.17f,.145f),new Vector3(-.17f,-.23f,.173f),.032f,.012f);
            Buckle(g,m,new Vector3(-.07f,-.10f,.193f),.043f,.05f);
            // Segmented curved belt follows the actual waist rather than crossing the torso as a box.
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;Ribbon("Waist belt curved segment",g,m,"LeatherDark",new Vector3(Mathf.Cos(a)*.24f,-.25f,Mathf.Sin(a)*.16f),new Vector3(Mathf.Cos(b)*.24f,-.25f,Mathf.Sin(b)*.16f),.048f,.012f);}
            Buckle(g,m,new Vector3(0,-.25f,.178f),.075f,.057f);
            var roll=Group("Rolled blanket and lashings",g,new Vector3(0,-.205f,-.205f));roll.localRotation=Quaternion.Euler(0,90,0);
            Tube("Canvas bedroll body",roll,m,"Cloth",new Vector3(0,0,-.22f),.083f,.44f,.02f,40);
            for(int side=-1;side<=1;side+=2){for(int turn=0;turn<5;turn++){float r=.016f+turn*.014f;Tube("Visible concentric blanket fold",roll,m,"LeatherDark",new Vector3(0,0,side*.222f),r,.004f,.82f,32);}}
            for(float z=-.15f;z<.2f;z+=.3f){Tube("Bedroll wraparound leather strap",roll,m,"Leather",new Vector3(0,0,z),.086f,.021f,.965f,40);var clasp=Group("Bedroll conformal clasp",roll,new Vector3(0,-.086f,z+.01f));clasp.localRotation=Quaternion.Euler(90,0,0);Buckle(clasp,m,Vector3.zero,.026f,.029f);}
            return g;
        }
        public static Transform BuildKnee(Transform knee,Func<string,Material> m)
        {
            var g=Group("Trail Warden / knee protection",knee,Vector3.zero);
            Plate(g,m,new Vector3(0,0,.086f),new Vector3(.135f,.17f,.025f));
            for(int i=-1;i<=1;i+=2){float rx=i<0?.091f:.082f,rz=i<0?.093f:.083f;FittedBand("Conforming knee leather binding",g,m,i*.055f,rx,rz,.019f);Buckle(g,m,new Vector3(.067f,i*.055f,.059f),.020f,.025f);}
            return g;
        }
        public static Transform BuildForearm(Transform forearm,Func<string,Material> m)
        {
            var g=Group("Trail Warden / wrist cuff",forearm,Vector3.zero);
            FittedBand("Fitted leather bracer sleeve",g,m,-.230f,.044f,.038f,.075f);
            for(int i=0;i<2;i++)FittedBand("Bracer narrow retaining wrap",g,m,-.205f-i*.045f,.0455f,.0395f,.011f);
            for(int s=-1;s<=1;s+=2)Seam(g,m,new Vector3(s*.022f,-.262f,.035f),new Vector3(s*.022f,-.198f,.035f),9);
            Buckle(g,m,new Vector3(0,-.230f,.040f),.027f,.030f);return g;
        }
        public static Transform BuildGlove(Transform hand,Func<string,Material> m)
        {
            var g=Group("Trail Warden / articulated glove",hand,new Vector3(0,-.060f,.007f));
            Loft("Glove leather palm and back",g,m,"LeatherDark",Vector3.zero,new Vector3(.085f,.095f,.065f),10,24,.04f);
            for(int i=0;i<4;i++){float x=(i-1.5f)*.018f;Loft("Independent glove knuckle",g,m,"Leather",new Vector3(x,-.026f,.031f),new Vector3(.016f,.026f,.014f),5,12);Seam(g,m,new Vector3(x,-.04f,.038f),new Vector3(x,.026f,.030f),7);}
            return g;
        }
        public static Transform BuildThigh(Transform thigh,Func<string,Material> m,int side)
        {
            var g=Group("Trail Warden / thigh utility holster",thigh,Vector3.zero);
            Pouch(g,m,new Vector3(side*.072f,-.14f,.065f),new Vector3(.10f,.18f,.07f));
            for(int i=0;i<2;i++){FittedBand("Fitted thigh holster binding",g,m,-.10f-i*.10f,i==0?.116f:.114f,i==0?.117f:.113f,.019f);Buckle(g,m,new Vector3(side*.075f,-.10f-i*.10f,.086f),.029f,.025f);}return g;
        }
        public static Transform BuildBootStraps(Transform foot,Func<string,Material> m)
        {var g=Group("Trail Warden / boot buckles",foot,Vector3.zero);for(int i=0;i<2;i++){Ribbon("Boot ankle strap",g,m,"Leather",new Vector3(-.061f,.06f+i*.04f,.02f),new Vector3(.061f,.06f+i*.04f,.02f),.019f);Buckle(g,m,new Vector3(.045f,.06f+i*.04f,.028f),.027f,.023f);}return g;}

        // Authored side silhouette extruded with inset bevels: stock, grip and fore-end keep continuous contours.
        static Transform Profile(string n,Transform p,Func<string,Material> m,string key,float width,params Vector2[] points)
        {
            var v=new List<Vector3>();var ix=new List<int>();Vector2 center=Vector2.zero;foreach(var q in points)center+=q;center/=points.Length;
            int N=points.Length;
            for(int ring=0;ring<4;ring++){float x=(ring==0?-1:1)*width*.5f;if(ring==1)x=-width*.4f;if(ring==2)x=width*.4f;float inset=ring==0||ring==3?.94f:1;foreach(var q in points){var t=center+(q-center)*inset;v.Add(new Vector3(x,t.y,t.x));}}
            for(int ring=0;ring<3;ring++)for(int i=0;i<N;i++){int a=ring*N+i,b=ring*N+(i+1)%N;ix.AddRange(new[]{a,b,b+N,a,b+N,a+N});}
            // Ear clipping preserves concave stock and pistol grip silhouettes without fan artifacts.
            float area=0;for(int i=0;i<N;i++)area+=points[i].x*points[(i+1)%N].y-points[(i+1)%N].x*points[i].y;
            var polygon=new List<int>();for(int i=0;i<N;i++)polygon.Add(area>0?i:N-1-i);var caps=new List<int>();
            float Cross(Vector2 a,Vector2 b,Vector2 c){return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
            int guard=0;while(polygon.Count>3&&guard++<N*N){bool clipped=false;for(int k=0;k<polygon.Count;k++){
                int a=polygon[(k+polygon.Count-1)%polygon.Count],b=polygon[k],c=polygon[(k+1)%polygon.Count];
                if(Cross(points[a],points[b],points[c])<=.0000001f)continue;bool inside=false;
                foreach(int q in polygon)if(q!=a&&q!=b&&q!=c&&Cross(points[a],points[b],points[q])>=0&&Cross(points[b],points[c],points[q])>=0&&Cross(points[c],points[a],points[q])>=0){inside=true;break;}
                if(inside)continue;caps.AddRange(new[]{a,b,c});polygon.RemoveAt(k);clipped=true;break;
            }if(!clipped)break;}
            if(polygon.Count==3)caps.AddRange(polygon);
            for(int k=0;k<caps.Count;k+=3){ix.AddRange(new[]{caps[k],caps[k+1],caps[k+2],3*N+caps[k],3*N+caps[k+2],3*N+caps[k+1]});}
            return Surface(n,p,v,ix,m,key);
        }
        static WeaponSockets Sockets(Transform root,Vector3 trigger,Vector3 support,Vector3 stock,Vector3 muzzle)
        {return new WeaponSockets{root=root,trigger=Group("Right hand / trigger socket",root,trigger),support=Group("Left hand / support socket",root,support),stock=Group("Shoulder / stock socket",root,stock),muzzle=Group("Muzzle / true firing axis",root,muzzle)};}
        public static WeaponSockets BuildRifle(Transform parent,Func<string,Material> m)
        {
            var g=Group("Trail Warden / pressure rifle",parent,Vector3.zero);
            Profile("Carved walnut shoulder stock",g,m,"Wood",.072f,new Vector2(-.40f,-.10f),new Vector2(-.39f,.055f),new Vector2(-.25f,.048f),new Vector2(-.10f,.010f),new Vector2(-.045f,-.018f),new Vector2(-.08f,-.060f),new Vector2(-.22f,-.045f));
            Profile("Shoulder rubber buttplate",g,m,"Dark",.080f,new Vector2(-.409f,-.105f),new Vector2(-.409f,.060f),new Vector2(-.39f,.060f),new Vector2(-.39f,-.10f));
            Profile("Milled receiver and bolt housing",g,m,"Steel",.082f,new Vector2(-.11f,-.040f),new Vector2(-.11f,.030f),new Vector2(.16f,.038f),new Vector2(.20f,.012f),new Vector2(.19f,-.046f));
            Profile("Walnut pistol grip",g,m,"Wood",.054f,new Vector2(-.055f,-.035f),new Vector2(-.075f,-.14f),new Vector2(-.025f,-.15f),new Vector2(.015f,-.055f));
            Profile("Sculpted walnut fore-end",g,m,"Wood",.075f,new Vector2(.17f,-.042f),new Vector2(.20f,.023f),new Vector2(.50f,.018f),new Vector2(.53f,-.025f),new Vector2(.43f,-.057f),new Vector2(.23f,-.063f));
            Tube("Free floating blued barrel",g,m,"Steel",new Vector3(0,.008f,.46f),.016f,.37f,.56f,40);
            Tube("Muzzle crown and actual bore",g,m,"Steel",new Vector3(0,.008f,.82f),.023f,.043f,.44f,40);
            for(float z=.24f;z<.54f;z+=.25f)Tube("Fore-end steel retaining band",g,m,"Steel",new Vector3(0,-.012f,z),.044f,.022f,.89f,32);
            for(int s=-1;s<=1;s+=2){
                for(int i=0;i<7;i++)Rod("Receiver machining groove",g,m,"Dark",new Vector3(s*.042f,.003f,.025f+i*.013f),new Vector3(s*.042f,.026f,.025f+i*.013f),.0017f);
                for(int i=0;i<5;i++)Rivet(g,m,new Vector3(s*.043f,-.018f,-.07f+i*.053f),.004f);
                for(int i=0;i<9;i++){float z=.20f+i*.033f;Rod("Walnut grain cut",g,m,"LeatherDark",new Vector3(s*.037f,-.025f,z),new Vector3(s*.038f,-.020f,z+.021f),.0007f);}
                for(int i=0;i<3;i++){float z=.29f+i*.048f;Rod("Fore-end cooling inlet",g,m,"Dark",new Vector3(s*.038f,-.010f,z),new Vector3(s*.038f,-.010f,z+.022f),.0020f);}
                for(int i=0;i<3;i++){float z=-.30f+i*.065f;var screw=Tube("Recessed stock screw head",g,m,"Steel",Vector3.zero,.0038f,.0015f,.02f,14);screw.localPosition=new Vector3(s*.036f,-.013f,z);screw.localRotation=Quaternion.LookRotation(Vector3.right*s);Rod("Stock screw driver slot",g,m,"Dark",new Vector3(s*.038f,-.016f,z),new Vector3(s*.038f,-.010f,z),.0007f);}
            }
            Profile("Open ejection port recess",g,m,"Dark",.085f,new Vector2(.028f,.012f),new Vector2(.030f,.029f),new Vector2(.105f,.029f),new Vector2(.105f,.012f));
            Rod("Bolt handle stem",g,m,"Steel",new Vector3(.04f,.014f,.01f),new Vector3(.075f,.002f,.01f),.006f);Loft("Bolt handle knob",g,m,"Steel",new Vector3(.075f,.002f,.01f),new Vector3(.018f,.018f,.018f),6,16);
            Rod("Receiver cover seam",g,m,"Dark",new Vector3(.042f,.009f,-.095f),new Vector3(.042f,.009f,.18f),.0010f);
            Rod("Bolt return guide",g,m,"Steel",new Vector3(.043f,.022f,-.08f),new Vector3(.043f,.022f,.01f),.0025f);
            for(int i=0;i<5;i++)Rod("Receiver edge handling wear",g,m,"Steel",new Vector3(.042f,.029f,-.07f+i*.039f),new Vector3(.042f,.029f,-.061f+i*.039f),.0009f);
            Vector3[] guard={new Vector3(0,-.045f,.035f),new Vector3(0,-.102f,.040f),new Vector3(0,-.119f,-.004f),new Vector3(0,-.101f,-.05f),new Vector3(0,-.050f,-.055f)};
            for(int i=1;i<guard.Length;i++)Rod("Open trigger guard",g,m,"Steel",guard[i-1],guard[i],.004f);
            Rod("Curved trigger",g,m,"Steel",new Vector3(0,-.05f,-.025f),new Vector3(0,-.086f,-.013f),.003f);
            Profile("Curved box magazine",g,m,"Steel",.060f,new Vector2(.065f,-.044f),new Vector2(.15f,-.044f),new Vector2(.16f,-.105f),new Vector2(.145f,-.18f),new Vector2(.077f,-.17f),new Vector2(.082f,-.10f));
            for(int i=0;i<4;i++)Rod("Magazine pressed ribs",g,m,"Dark",new Vector3(.031f,-.062f,.08f+i*.016f),new Vector3(.031f,-.153f,.09f+i*.016f),.002f);
            // Two amber pressure vessels are separate glass volumes, with collars and a shared saddle.
            for(int i=0;i<2;i++){
                var t=Group("Amber pressure vessel "+i,g,new Vector3(0,.067f,-.055f+i*.059f));t.localRotation=Quaternion.Euler(0,90,0);t.localScale=Vector3.one*.72f;
                Tube("Amber glass pressure tube",t,m,"AmberGlass",new Vector3(0,0,-.040f),.027f,.080f,.72f,32);
                for(int s=-1;s<=1;s+=2)Tube("Pressure tube steel end collar",t,m,"Steel",new Vector3(0,0,s*.034f),.029f,.009f,.70f,24);
                Rod("Pressure core",t,m,"Brass",new Vector3(0,0,-.035f),new Vector3(0,0,.035f),.007f);
                Rod("Pressure saddle",g,m,"Steel",new Vector3(0,.028f,-.055f+i*.075f),new Vector3(0,.060f,-.055f+i*.075f),.015f);
            }
            Rod("Rear iron sight pedestal",g,m,"Steel",new Vector3(0,.028f,.15f),new Vector3(0,.064f,.15f),.009f);
            for(int s=-1;s<=1;s+=2)Rod("Rear notch ear",g,m,"Steel",new Vector3(s*.010f,.058f,.15f),new Vector3(s*.010f,.074f,.15f),.004f);
            Rod("Front sight post",g,m,"Steel",new Vector3(0,.019f,.76f),new Vector3(0,.073f,.76f),.005f);
            for(int s=-1;s<=1;s+=2)Rod("Front sight protective ear",g,m,"Steel",new Vector3(s*.013f,.020f,.76f),new Vector3(s*.013f,.065f,.76f),.004f);
            Vector3 last=new Vector3(0,-.09f,-.34f);for(int i=1;i<=20;i++){float u=i/20f;Vector3 q=new Vector3(.018f,-.09f-Mathf.Sin(u*Mathf.PI)*.12f,Mathf.Lerp(-.34f,.48f,u));Ribbon("Hanging leather rifle sling",g,m,"Leather",last,q,.018f,.004f);last=q;}
            Compact(g);return Sockets(g,new Vector3(0,-.085f,-.030f),new Vector3(0,-.065f,.25f),new Vector3(0,-.010f,-.405f),new Vector3(0,.008f,.865f));
        }
        public static WeaponSockets BuildPistol(Transform parent,Func<string,Material> m)
        {
            var g=Group("Trail Warden / compact sidearm",parent,Vector3.zero);
            Profile("Forged pistol slide",g,m,"Steel",.045f,new Vector2(-.060f,-.015f),new Vector2(-.060f,.025f),new Vector2(.145f,.025f),new Vector2(.165f,.010f),new Vector2(.165f,-.018f));
            Profile("Angled checkered grip",g,m,"LeatherDark",.039f,new Vector2(-.05f,-.015f),new Vector2(.01f,-.025f),new Vector2(-.005f,-.12f),new Vector2(-.067f,-.112f));
            Tube("Pistol muzzle bore",g,m,"Steel",new Vector3(0,.003f,.160f),.013f,.012f,.58f,24);
            for(int s=-1;s<=1;s+=2){for(int i=0;i<7;i++)Rod("Slide serration",g,m,"Dark",new Vector3(s*.023f,-.009f,-.05f+i*.007f),new Vector3(s*.023f,.018f,-.05f+i*.007f),.0015f);for(int i=0;i<10;i++)Rod("Grip checkering",g,m,"Leather",new Vector3(s*.020f,-.035f-i*.007f,-.048f),new Vector3(s*.020f,-.042f-i*.007f,-.006f),.0012f);}
            Vector3[] guard={new Vector3(0,-.020f,.050f),new Vector3(0,-.061f,.046f),new Vector3(0,-.065f,-.010f),new Vector3(0,-.023f,-.015f)};for(int i=1;i<guard.Length;i++)Rod("Open pistol trigger guard",g,m,"Steel",guard[i-1],guard[i],.003f);
            Rod("Pistol trigger",g,m,"Steel",new Vector3(0,-.023f,.018f),new Vector3(0,-.047f,.014f),.0025f);
            Rod("Front pistol sight",g,m,"Steel",new Vector3(0,.025f,.137f),new Vector3(0,.036f,.137f),.003f);
            for(int s=-1;s<=1;s+=2)Rod("Rear pistol sight",g,m,"Steel",new Vector3(s*.008f,.025f,-.046f),new Vector3(s*.008f,.035f,-.046f),.003f);
            Compact(g);return Sockets(g,new Vector3(0,-.063f,-.025f),new Vector3(-.022f,-.068f,-.012f),new Vector3(0,-.06f,-.045f),new Vector3(0,.003f,.172f));
        }
    }
}


