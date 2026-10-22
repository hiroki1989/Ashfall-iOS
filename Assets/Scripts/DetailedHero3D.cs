using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Entirely geometric character: all silhouettes, tailoring and hair locks are meshes.
    public sealed partial class DetailedHero3D : MonoBehaviour
    {
        public Transform chest, head, pelvis, rightUpperArm, rightForearm, rightHand, leftUpperArm, leftForearm, leftHand;
        public Transform rightThigh, rightShin, rightFoot, leftThigh, leftShin, leftFoot;
        public Transform leftToe,rightToe;
        public Vector3 LeftHeelContact=>leftFoot.TransformPoint(V(0,-.089f,-.055f));
        public Vector3 RightHeelContact=>rightFoot.TransformPoint(V(0,-.089f,-.055f));
        public Vector3 LeftToeContact=>leftToe.TransformPoint(V(0,0,.075f));
        public Vector3 RightToeContact=>rightToe.TransformPoint(V(0,0,.075f));
        public const float UpperArmLength=.285f, ForearmLength=.265f;
        Func<string,Material> palette; Transform rig; readonly List<Mesh> meshes=new List<Mesh>(); readonly List<Material> materials=new List<Material>(); readonly List<Texture2D> textures=new List<Texture2D>();
        readonly HashSet<Renderer> anatomyRenderers=new HashSet<Renderer>();
        public bool IsAnatomyRenderer(Renderer renderer)=>renderer&&anatomyRenderers.Contains(renderer);
        Material cloth,pants,skin,hair,hairHighlight,leather,seam,sole,metal;
        MeshRenderer seatRenderer;
        public void SetSeatFabric(Material value)
        {
            if(!seatRenderer)for(int i=0;i<pelvis.childCount;i++){var r=pelvis.GetChild(i).GetComponent<MeshRenderer>();if(r&&r.sharedMaterial==pants){seatRenderer=r;break;}}
            if(seatRenderer)seatRenderer.sharedMaterial=value?value:pants;
        }
        public void SetBaseJacketVisible(bool torso,bool upperSleeves,bool foreSleeves)
        {
            SetGarment(chest,torso);SetGarment(leftUpperArm,upperSleeves);SetGarment(rightUpperArm,upperSleeves);SetGarment(leftForearm,foreSleeves);SetGarment(rightForearm,foreSleeves);
        }
        void SetGarment(Transform joint,bool visible)
        {
            // Only immediate base-mesh children. Equipment roots and skin are independent.
            for(int i=0;i<joint.childCount;i++){var r=joint.GetChild(i).GetComponent<MeshRenderer>();if(r&&(r.sharedMaterial==cloth||r.sharedMaterial==seam))r.enabled=visible;}
        }
        public void SetBaseTrouserVisible(bool visible)
        {
            foreach(var joint in new[]{leftThigh,rightThigh,leftShin,rightShin})for(int i=0;i<joint.childCount;i++){var r=joint.GetChild(i).GetComponent<MeshRenderer>();if(r&&(r.sharedMaterial==pants||r.sharedMaterial==seam))r.enabled=visible;}
        }
        static Vector3 V(float x,float y,float z){return new Vector3(x,y,z);}
        public static DetailedHero3D Create(Transform parent,Func<string,Material> palette=null)
        {
            var g=new GameObject("Detailed survivor / true 3D anatomy");g.transform.SetParent(parent,false);
            var c=g.AddComponent<DetailedHero3D>();c.palette=palette;c.Build();return c;
        }
        Material Mat(string name,Color color,float smooth=0.15f,float metallic=0)
        {
            if(palette!=null){string key=name.StartsWith("Faded")?"Cloth":name.StartsWith("Dust")?"ClothDark":name.StartsWith("Warm")?"Skin":name.StartsWith("Layered")?"Hair":name.StartsWith("Waxed")?"Leather":name.StartsWith("Canvas")?"Stitch":name.StartsWith("Rubber")?"Dark":"Brass";var shared=palette(key);if(shared!=null)return shared;}
            var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");
            var m=new Material(shader){name=name,color=color};m.SetFloat("_Smoothness",smooth);m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",metallic);materials.Add(m);
            var tex=new Texture2D(128,128,TextureFormat.RGBA32,true){name=name+" / procedural grain",wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float noise=Mathf.PerlinNoise(x*.61f+13,y*.61f+7);float weave=((x%4==0||y%4==0)?-.055f:.018f);float value=.88f+noise*.19f+weave;pixels[y*128+x]=new Color(value,value,value,1);}
            tex.SetPixels(pixels);tex.Apply(true,true);textures.Add(tex);m.mainTexture=tex;m.mainTextureScale=new Vector2(5,5);return m;
        }
        Transform Joint(string name,Transform p,Vector3 pos){var g=new GameObject(name).transform;g.SetParent(p,false);g.localPosition=pos;return g;}
        void Build()
        {
            cloth=Mat("Faded olive woven canvas",new Color(.265f,.279f,.177f));pants=Mat("Dust umber twill",new Color(.32f,.25f,.17f));
            skin=Mat("Warm weathered skin",new Color(.58f,.39f,.25f),.24f);hair=Mat("Layered dark brown hair",new Color(.055f,.038f,.027f),.26f);
            hairHighlight=palette!=null?palette("HairHighlight"):hair;
            leather=Mat("Waxed leather boot",new Color(.17f,.12f,.077f),.24f);seam=Mat("Canvas folded seam",new Color(.15f,.16f,.10f));sole=Mat("Rubber tread",new Color(.055f,.048f,.038f));metal=Mat("Tarnished brass eyelet",new Color(.37f,.29f,.16f),.38f,.65f);
            rig=Joint("Continuous body rig",transform,Vector3.zero);pelvis=Joint("Pelvis",rig,V(0,.98f,0));chest=Joint("Chest",rig,V(0,1.25f,0));head=Joint("Head",chest,V(0,.30f,0));
            Loft("Tailored jacket torso",chest,V(0,-.29f,0),new[]{0f,.05f,.15f,.26f,.36f,.45f,.49f},new[]{.205f,.226f,.192f,.19f,.232f,.246f,.152f},new[]{.129f,.137f,.123f,.133f,.145f,.128f,.085f},cloth,.009f,48,44);
            Loft("Trouser seat",pelvis,V(0,-.10f,0),new[]{0f,.04f,.10f,.17f},new[]{.135f,.197f,.201f,.175f},new[]{.092f,.126f,.123f,.105f},pants,.005f);
            Loft("Sternomastoid neck",head,V(0,-.15f,-.014f),new[]{0f,.045f,.10f,.145f,.17f},new[]{.066f,.055f,.049f,.048f,.047f},new[]{.055f,.049f,.047f,.043f,.040f},skin,0);
            BuildFace();BuildTailoring();
            for(int s=-1;s<=1;s+=2)BuildLimb(s);
            BuildFineDetails();
            SetPose("lowrifle",Vector3.forward,-1);
            CombineRigidMeshes(rig);
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))anatomyRenderers.Add(renderer);
        }
        void BuildFineDetails()
        {
            for(int side=-1;side<=1;side+=2)
            {
                var arm=side<0?leftUpperArm:rightUpperArm;var thigh=side<0?leftThigh:rightThigh;
                for(int crease=0;crease<7;crease++)
                {
                    float y=-.11f-crease*.022f;
                    Curve("Sleeve stitched pressure valley",arm,new[]{V(-.058f,y,.041f),V(-.012f,y+.014f,.076f),V(.046f,y-.004f,.061f)},.0011f,seam);
                    float ky=-.25f-crease*.021f;
                    Curve("Trouser directional wrinkle lip",thigh,new[]{V(-.070f,ky,.057f),V(-.018f,ky+.015f,.093f),V(.060f,ky-.005f,.075f)},.0012f,pants);
                }
                for(int stitch=0;stitch<23;stitch++)
                {
                    float y=-.25f+stitch*.018f;
                    Curve("Jacket placket saddle stitch",chest,new[]{V(side*.026f,y,.146f),V(side*.027f,y+.006f,.147f)},.00065f,seam);
                }
                for(int stitch=0;stitch<15;stitch++)
                {float x=side*(.035f+stitch*.010f);Curve("Hem double topstitch",chest,new[]{V(x,-.272f,.133f),V(x+side*.004f,-.271f,.131f)},.0007f,seam);}
            }
        }
        void BuildFace()
        {
            Loft("Anatomic jaw cheek temples and cranium",head,V(0,-.025f,0),new[]{0f,.021f,.048f,.081f,.12f,.16f,.20f,.223f,.237f},new[]{.036f,.060f,.077f,.088f,.090f,.089f,.078f,.051f,.004f},new[]{.046f,.059f,.067f,.072f,.078f,.080f,.078f,.055f,.004f},skin,0,48,44);
            for(int s=-1;s<=1;s+=2)
            {
                Ellipsoid("Ear pinna",head,V(s*.092f,.083f,-.006f),V(.018f,.036f,.021f),skin);
                Curve("Ear antihelix",head,new[]{V(s*.102f,.105f,.005f),V(s*.107f,.087f,.013f),V(s*.102f,.065f,.009f)},.004f,skin);
            }
            BuildWeatheredFace();
            // Closely fitted cap under individually curved clumps, never a spherical helmet.
            Loft("Scalp undercut",head,V(0,.125f,-.008f),new[]{0f,.04f,.085f,.116f,.13f},new[]{.089f,.095f,.091f,.062f,.005f},new[]{.078f,.087f,.082f,.060f,.005f},hair,0);
            BuildNapeHair();
            var random=new System.Random(9071);
            var scalpY=new[]{0f,.04f,.085f,.116f,.13f};var scalpX=new[]{.089f,.095f,.091f,.062f,.005f};var scalpZ=new[]{.078f,.087f,.082f,.060f,.005f};
            for(int i=0;i<310;i++)
            {
                float a=i*2.399963f,u=.06f+(float)random.NextDouble()*.89f;
                bool fringe=Mathf.Sin(a)>.3f;
                if(fringe)u=Mathf.Max(.26f,u);
                // Roots must follow the very same scalp profile; a spherical approximation buried locks.
                Vector3 p=V(Mathf.Cos(a)*(Profile(scalpX,u)+.0015f),.125f+Profile(scalpY,u)+Mathf.Max(0,Mathf.Sin(a))*.030f*(1-u)*(1-u),Mathf.Sin(a)*(Profile(scalpZ,u)+.0015f)-.008f);
                float loose=(float)random.NextDouble(),sweep=Mathf.Sin(a*2.3f+u*7);
                Vector3 flow=V(.012f+Mathf.Cos(a)*.024f+sweep*.012f,fringe?-.021f:-.014f,fringe?.025f:-.024f);
                if(fringe)flow.y=Mathf.Max(flow.y,.172f-p.y);
                Vector3 lift=V(-.009f+sweep*.012f,.018f+loose*.026f,Mathf.Sin(a*3)*.013f);
                // Overlapping S-curved locks break the regular fringe and crown silhouette.
                HairLock(p,p+lift,p+flow*.65f+lift*.75f+V(sweep*.009f,0,-.006f),p+flow,.005f+(float)random.NextDouble()*.004f,i);
            }
        }
        // Flattened sculpted locks overlap like hair, instead of circular noodles.
        void HairLock(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,int seed)
        {
            const int rows=48,sides=16;var vv=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int j=0;j<=rows;j++)
            {
                float t=j/(float)rows,q=1-t;Vector3 p=q*q*q*a+3*q*q*t*b+3*q*t*t*c+t*t*t*d;
                Vector3 tangent=(3*q*q*(b-a)+6*q*t*(c-b)+3*t*t*(d-c)).normalized;
                Vector3 outward=(p-V(0,.13f,-.01f)).normalized;
                Vector3 side=Vector3.Cross(tangent,outward).normalized;Vector3 normal=Vector3.Cross(side,tangent).normalized;
                float taper=Mathf.Pow(1-t,.68f)*Mathf.Lerp(.6f,1,Mathf.Sin(t*Mathf.PI));
                for(int i=0;i<=sides;i++)
                {
                    float angle=i*Mathf.PI*2/sides;
                    float ridge=1+.12f*Mathf.Cos(angle*5+seed*.7f+t*3);
                    vv.Add(p+side*(Mathf.Cos(angle)*width*taper)+normal*(Mathf.Sin(angle)*width*.32f*taper*ridge));
                    uv.Add(new Vector2(i/(float)sides,t));
                    if(j<rows&&i<sides){int k=j*(sides+1)+i;tr.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
                }
            }
            MeshObject("Layered swept hair / tapered strand ridges",head,Vector3.zero,vv,uv,tr,seed%7==0?hairHighlight:hair);
        }
        void BuildNapeHair()
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();const int sides=28,rows=12;
            for(int j=0;j<=rows;j++)for(int i=0;i<=sides;i++)
            {
                float u=(float)i/sides,t=(float)j/rows,a=Mathf.PI+u*Mathf.PI;
                float y=Mathf.Lerp(.025f+Mathf.Abs(Mathf.Cos(a))*.038f,.165f,t);
                float width=Mathf.Lerp(.064f,.094f,t),depth=Mathf.Lerp(.058f,.087f,t);
                vertices.Add(V(Mathf.Cos(a)*width,y,Mathf.Sin(a)*depth-.006f));uv.Add(new Vector2(u,t));
                if(j<rows&&i<sides){int k=j*(sides+1)+i;indices.Add(k);indices.Add(k+sides+1);indices.Add(k+1);indices.Add(k+1);indices.Add(k+sides+1);indices.Add(k+sides+2);}
            }
            MeshObject("Tapered natural nape and sideburn undercut",head,Vector3.zero,vertices,uv,indices,hair);
            for(int i=0;i<25;i++)
            {
                float a=Mathf.PI+(i+.5f)/25*Mathf.PI;
                Vector3 top=V(Mathf.Cos(a)*.091f,.137f,Mathf.Sin(a)*.084f-.008f);
                Vector3 tip=V(Mathf.Cos(a)*.072f,.037f+Mathf.Abs(Mathf.Cos(a))*.03f,Mathf.Sin(a)*.066f-.009f);
                Curve("Combed nape hair strand",head,new[]{top,Vector3.Lerp(top,tip,.6f)+V(.003f,0,-.003f),tip},.0035f,hair,true);
            }
        }
        void BuildTailoring()
        {
            for(int s=-1;s<=1;s+=2)
            {
                Curve("Raised shoulder seam",chest,new[]{V(s*.065f,.205f,-.062f),V(s*.17f,.207f,-.054f),V(s*.236f,.16f,.014f)},.003f,seam);
                Curve("Front jacket placket",chest,new[]{V(s*.018f,-.28f,.133f),V(s*.018f,-.08f,.14f),V(s*.024f,.10f,.149f)},.006f,cloth);
                var lapel=new List<Vector3>{V(s*.064f,.231f,.050f),V(s*.112f,.180f,.135f),V(s*.047f,.075f,.157f),V(s*.035f,.177f,.123f)};
                var lapelTriangles=new List<int>{0,2,1,0,3,2};if(s<0)lapelTriangles=new List<int>{0,1,2,0,2,3};
                int count=lapel.Count;for(int k=0;k<count;k++)lapel.Add(lapel[k]-Vector3.forward*.003f);
                for(int k=0;k<6;k+=3)lapelTriangles.AddRange(new[]{lapelTriangles[k]+count,lapelTriangles[k+2]+count,lapelTriangles[k+1]+count});
                for(int k=0;k<count;k++){int next=(k+1)%count;lapelTriangles.AddRange(new[]{k,next,k+count,next,next+count,k+count});}
                MeshObject("Folded tailored collar / thin doubled canvas",chest,Vector3.zero,lapel,new List<Vector2>{Vector2.zero,Vector2.right,Vector2.one,Vector2.up,Vector2.zero,Vector2.right,Vector2.one,Vector2.up},lapelTriangles,cloth);
                Curve("Collar folded stitched outer edge",chest,new[]{lapel[0],lapel[1],lapel[2]},.0017f,seam);
                Curve("Hem piping",chest,new[]{V(s*.016f,-.285f,.139f),V(s*.13f,-.276f,.133f),V(s*.208f,-.25f,.058f)},.003f,seam);
                Curve("Back princess seam",chest,new[]{V(s*.15f,.17f,-.10f),V(s*.115f,-.02f,-.137f),V(s*.16f,-.26f,-.127f)},.0025f,seam);
            }
            for(int i=0;i<6;i++)Ellipsoid("Jacket button",chest,V(.024f,-.23f+i*.057f,.148f),V(.007f,.007f,.003f),metal);
            BuildFieldJacketHem();
            // Hood with an open, folded rim, resting on the back of the neck.
        }
        void BuildFieldJacketHem()
        {
            // Four overlapping tailored panels, open at the front and rear vent.
            // Their irregular edge is actual cloth thickness, not a painted fringe.
            var ranges=new[]{new Vector2(.10f,1.34f),new Vector2(1.80f,3.04f),new Vector2(3.18f,4.66f),new Vector2(4.77f,6.18f)};
            foreach(var range in ranges)
            {
                const int rows=20,columns=36;var v=new List<Vector3>();var uv=new List<Vector2>();var ix=new List<int>();
                for(int row=0;row<=rows;row++)for(int col=0;col<=columns;col++)
                {
                    float t=row/(float)rows,u=col/(float)columns,a=Mathf.Lerp(range.x,range.y,u);
                    float tension=.005f*Mathf.Sin(a*13+t*2)*Mathf.Sin(t*Mathf.PI);
                    float torn=.008f*Mathf.Sin(a*19)+.004f*Mathf.Sin(a*37);
                    float end=-.355f+torn+.014f*Mathf.Pow(Mathf.Abs(u*2-1),5);
                    float y=Mathf.Lerp(-.235f,end,t);
                    float radius=.219f+tension+.009f*Mathf.Sin(t*Mathf.PI*.6f);
                    v.Add(V(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*(.145f+tension+.009f*t)));
                    uv.Add(new Vector2(u,t));
                    if(row<rows&&col<columns){int k=row*(columns+1)+col,b=k+columns+1;ix.AddRange(new[]{k,k+1,b,k+1,b+1,b});}
                }
                int count=v.Count,faces=ix.Count;
                for(int i=0;i<count;i++){var q=v[i];v.Add(q-V(q.x,0,q.z).normalized*.0025f);uv.Add(uv[i]);}
                for(int i=0;i<faces;i+=3)ix.AddRange(new[]{ix[i]+count,ix[i+2]+count,ix[i+1]+count});
                // Join the visible bottom cut rather than leaving a paper-thin plane.
                for(int col=0;col<columns;col++){int a=rows*(columns+1)+col,b=a+1;ix.AddRange(new[]{a,a+count,b,b,a+count,b+count});}
                MeshObject("Overlapping worn field jacket hem panel",chest,Vector3.zero,v,uv,ix,cloth);
                for(int col=2;col<columns-2;col+=3)
                {
                    Vector3 q=v[rows*(columns+1)+col];
                    Curve("Frayed canvas hem fibre",chest,new[]{q,q+V(.0015f,-.003f-(col%4)*.001f,.001f)},.0006f,seam);
                }
            }
        }
        void BuildLimb(int s)
        {
            var upper=Joint((s<0?"Left":"Right")+" upper arm",chest,V(s*.18f,.177f,0));
            var fore=Joint("Forearm",upper,V(0,-UpperArmLength,0));var hand=Joint("Hand",fore,V(0,-ForearmLength,0));
            if(s<0){leftUpperArm=upper;leftForearm=fore;leftHand=hand;}else{rightUpperArm=upper;rightForearm=fore;rightHand=hand;}
            Loft("Jacket sleeve deltoid bicep",upper,V(0,-.295f,0),new[]{0f,.025f,.08f,.15f,.22f,.28f,.32f,.35f},new[]{.057f,.064f,.070f,.072f,.078f,.075f,.056f,.002f},new[]{.058f,.064f,.068f,.070f,.075f,.072f,.054f,.002f},cloth,.008f,36,34);
            Loft("Rolled forearm sleeve",fore,V(0,-.16f,0),new[]{0f,.025f,.06f,.11f,.16f,.19f},new[]{.058f,.069f,.072f,.071f,.067f,.06f},new[]{.056f,.068f,.07f,.069f,.066f,.057f},cloth,.009f);
            Loft("Exposed forearm tendons",fore,V(0,-.27f,0),new[]{0f,.035f,.09f,.14f},new[]{.038f,.045f,.056f,.058f},new[]{.033f,.035f,.046f,.049f},skin,.001f);
            Loft("Fingerless glove palm",hand,V(0,-.10f,0),new[]{0f,.025f,.065f,.10f,.12f},new[]{.028f,.042f,.042f,.033f,.027f},new[]{.022f,.026f,.027f,.025f,.023f},leather,.001f);
            for(int f=0;f<4;f++)
            {
                float x=-.028f+f*.018f, len=.047f-Mathf.Abs(f-1.3f)*.005f;
                Curve("Gloved articulated finger",hand,new[]{V(x,-.086f,.013f),V(x,-.115f,.028f),V(x,-.126f,.048f),V(x,-.11f,.057f)},.009f,leather);
                Ellipsoid("Exposed fingertip",hand,V(x,-.11f,.056f),V(.008f,.013f,.008f),skin);
            }
            Curve("Opposed thumb",hand,new[]{V(-s*.035f,-.043f,.005f),V(-s*.050f,-.071f,.035f),V(-s*.027f,-.093f,.049f)},.013f,leather);
            var thigh=Joint((s<0?"Left":"Right")+" thigh",pelvis,V(s*.105f,-.025f,0));var shin=Joint("Shin",thigh,V(0,-.438f,0));var foot=Joint("Foot",shin,V(0,-.416f,0));
            if(s<0){leftThigh=thigh;leftShin=shin;leftFoot=foot;}else{rightThigh=thigh;rightShin=shin;rightFoot=foot;}
            Loft("Anatomic trouser thigh",thigh,V(0,-.45f,0),new[]{0f,.035f,.11f,.23f,.34f,.42f,.48f},new[]{.071f,.083f,.095f,.112f,.115f,.104f,.078f},new[]{.073f,.08f,.095f,.111f,.115f,.109f,.078f},pants,.009f,40,42);
            Loft("Trouser shaped calf",shin,V(0,-.31f,0),new[]{0f,.025f,.065f,.13f,.21f,.29f,.34f},new[]{.061f,.076f,.08f,.09f,.093f,.075f,.068f},new[]{.06f,.072f,.081f,.097f,.093f,.074f,.069f},pants,.009f,40,40);
            Curve("Thigh outer double seam",thigh,new[]{V(s*.091f,-.03f,0),V(s*.119f,-.19f,.003f),V(s*.087f,-.40f,.005f)},.003f,seam);
            Curve("Calf outer seam",shin,new[]{V(s*.077f,-.02f,0),V(s*.098f,-.18f,.005f),V(s*.071f,-.29f,.005f)},.003f,seam);
            AddGarmentCreases(upper,fore,thigh,shin,s);
            Boot(foot);
            FinishBootRig(foot,shin,s);
        }
        void AddGarmentCreases(Transform upper,Transform fore,Transform thigh,Transform shin,int side)
        {
            Curve("Sleeve stitched inseam",upper,new[]{V(-side*.041f,-.025f,.061f),V(-side*.050f,-.14f,.057f),V(-side*.037f,-.265f,.046f)},.0017f,seam);
            for(int i=0;i<3;i++)
            {
                float y=-.247f+i*.045f;
                Curve("Elbow diagonal compression crease",upper,new[]{V(-.051f,y-.012f,.041f),V(-.019f,y+.005f,.068f),V(.028f,y+.024f,.060f)},.0018f,seam);
                float ky=-.40f+i*.047f;
                Curve("Knee trouser compression crease",thigh,new[]{V(-.057f,ky-.012f,.054f),V(-.010f,ky+.004f,.085f),V(.055f,ky+.023f,.057f)},.0016f,seam);
                float cy=-.265f+i*.039f;
                Curve("Gathered cuff cloth crease",shin,new[]{V(-.05f,cy-.007f,.052f),V(0,cy+.008f,.076f),V(.05f,cy+.018f,.051f)},.0015f,seam);
            }
            Curve("Hip diagonal cloth tension",thigh,new[]{V(-side*.060f,-.055f,.080f),V(-side*.014f,-.105f,.112f),V(side*.068f,-.15f,.081f)},.002f,seam);
            Curve("Crotch tailored rise seam",thigh,new[]{V(-side*.055f,.006f,.062f),V(-side*.075f,-.06f,.068f),V(-side*.055f,-.14f,.077f)},.0017f,seam);
        }
        void Boot(Transform foot)
        {
            Loft("Laced boot shaft",foot,V(0,-.007f,0),new[]{0f,.03f,.08f,.14f,.19f},new[]{.066f,.064f,.058f,.064f,.067f},new[]{.068f,.061f,.054f,.059f,.061f},leather,.002f);
            Loft("Combat boot squared vamp and toe",foot,V(0,-.060f,.064f),new[]{0f,.018f,.045f,.071f,.083f},new[]{.069f,.074f,.070f,.060f,.041f},new[]{.132f,.137f,.130f,.102f,.065f},leather,.001f,36,22,.52f);
            Loft("Thick welted outsole",foot,V(0,-.083f,.063f),new[]{0f,.008f,.023f,.030f},new[]{.075f,.078f,.078f,.072f},new[]{.141f,.145f,.145f,.138f},sole,0,36,8,.48f);
            Curve("Toe cap stitched welt",foot,new[]{V(-.065f,-.014f,.126f),V(-.048f,.012f,.147f),V(0,.019f,.155f),V(.048f,.012f,.147f),V(.065f,-.014f,.126f)},.0028f,seam);
            for(int j=0;j<7;j++)
            {
                float y=.015f+j*.024f,z=.073f-j*.002f;
                for(int s=-1;s<=1;s+=2)Ellipsoid("Boot brass eyelet",foot,V(s*.029f,y,z),V(.006f,.006f,.003f),metal);
                Curve("Crossed boot lace",foot,new[]{V(-.028f,y,z+.004f),V(0,y+.011f,z+.010f),V(.028f,y+.024f,z+.002f)},.0026f,pants);
                Curve("Crossed boot lace",foot,new[]{V(.028f,y,z+.004f),V(0,y+.011f,z+.011f),V(-.028f,y+.024f,z+.002f)},.0026f,pants);
            }
            for(int j=0;j<8;j++)for(int s=-1;s<=1;s+=2)Loft("Outsole segmented traction lug",foot,V(s*.064f,-.089f,-.045f+j*.031f),new[]{0f,.012f},new[]{.014f,.014f},new[]{.011f,.011f},sole,0,8,1,.4f);
        }
        void FinishBootRig(Transform foot,Transform shin,int side)
        {
            var toe=Joint("Flexible boot toe / ball hinge",foot,V(0,-.089f,.12f));if(side<0)leftToe=toe;else rightToe=toe;
            var children=new List<Transform>();for(int i=0;i<foot.childCount;i++)if(foot.GetChild(i)!=toe)children.Add(foot.GetChild(i));
            foreach(var child in children)
            {
                // The upper boot is attached to the tibia, so ankle roll cannot poke its shaft through the heel.
                bool shaft=child.name.Contains("boot shaft")||child.name.Contains("eyelet")||child.name.Contains("boot lace");
                if(shaft){child.SetParent(shin,true);continue;}
                var filter=child.GetComponent<MeshFilter>();if(filter==null)continue;
                var source=filter.sharedMesh;var positions=source.vertices;var sourceUV=source.uv;var ix=source.triangles;
                for(int half=0;half<2;half++)
                {
                    var verts=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
                    for(int k=0;k<ix.Length;k+=3)
                    {
                        var polygon=new List<Vector3>();var puv=new List<Vector2>();
                        for(int j=0;j<3;j++){polygon.Add(foot.InverseTransformPoint(child.TransformPoint(positions[ix[k+j]])));puv.Add(sourceUV.Length>ix[k+j]?sourceUV[ix[k+j]]:Vector2.zero);}
                        var clipped=new List<Vector3>();var cuv=new List<Vector2>();
                        for(int j=0;j<polygon.Count;j++)
                        {
                            int next=(j+1)%polygon.Count;Vector3 a=polygon[j],b=polygon[next];bool ia=half==0?a.z<=.12f:a.z>=.12f,ib=half==0?b.z<=.12f:b.z>=.12f;
                            if(ia){clipped.Add(a);cuv.Add(puv[j]);}
                            if(ia!=ib){float t=(.12f-a.z)/(b.z-a.z);clipped.Add(Vector3.Lerp(a,b,t));cuv.Add(Vector2.Lerp(puv[j],puv[next],t));}
                        }
                        if(clipped.Count<3)continue;int start=verts.Count;
                        for(int j=0;j<clipped.Count;j++){verts.Add(clipped[j]-(half==1?toe.localPosition:Vector3.zero));uvs.Add(cuv[j]);}
                        for(int j=1;j<clipped.Count-1;j++){triangles.Add(start);triangles.Add(start+j);triangles.Add(start+j+1);}
                    }
                    if(verts.Count>0)MeshObject(child.name+(half==0?" / heel":" / flexible toe"),half==0?foot:toe,Vector3.zero,verts,uvs,triangles,child.GetComponent<MeshRenderer>().sharedMaterial);
                }
                meshes.Remove(source);Destroy(source);child.gameObject.SetActive(false);Destroy(child.gameObject);
            }
        }
        public void SetPose(string mode,Vector3 direction,float phase)
        {
            if(rig==null)return;
            direction.y=0;if(direction.sqrMagnitude<.001f)direction=Vector3.forward;
            rig.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
            float stride=phase<0?0:Mathf.Sin(phase*Mathf.PI*2), lift=phase<0?0:Mathf.Abs(Mathf.Sin(phase*Mathf.PI*2));
            float air=phase<0?0:Mathf.Pow(lift,3)*.065f;
            pelvis.localPosition=V(0,.98f+air,0);chest.localPosition=V(0,1.25f+air,.018f*lift);
            chest.localRotation=Quaternion.Euler(phase<0?2:9,phase<0?-5:stride*4,0);
            pelvis.localRotation=Quaternion.Euler(0,phase<0?3:-stride*5,0);
            leftThigh.localRotation=Quaternion.Euler(stride*35,0,-4);rightThigh.localRotation=Quaternion.Euler(-stride*35,0,4);
            leftShin.localRotation=Quaternion.Euler(Mathf.Max(0,-stride)*72,0,0);rightShin.localRotation=Quaternion.Euler(Mathf.Max(0,stride)*72,0,0);
            leftFoot.localRotation=Quaternion.Euler(-Mathf.Max(0,-stride)*13,0,-2);rightFoot.localRotation=Quaternion.Euler(-Mathf.Max(0,stride)*13,0,2);
            bool low=mode=="lowrifle",pistol=mode=="aimpistol";
            Vector3 r=low?V(.11f,1.08f,.29f):pistol?V(.075f,1.40f,.48f):V(.105f,1.43f,.32f);
            Vector3 l=low?V(-.065f,1.14f,.46f):pistol?V(.025f,1.39f,.49f):V(-.09f,1.39f,.49f);
            SetHandTargets(rig.TransformPoint(r),rig.TransformPoint(l));
            head.localRotation=Quaternion.Euler(low?3:-5,low?-9:4,0);
        }
        // phase is one full left/right gait cycle. Root heading remains owned by SetPose.
        public void SetLocomotion(Vector3 worldVelocity,float blend,bool aiming,float phase)
        {
            if(rig==null)return;blend=Mathf.Clamp01(blend);
            Vector3 velocity=rig.InverseTransformDirection(worldVelocity);velocity.y=0;
            float speed=velocity.magnitude;Vector3 travel=speed>.01f?velocity/speed:Vector3.forward;
            float cycle=Mathf.Repeat(phase,1),sway=Mathf.Sin(cycle*Mathf.PI*2),bounce=Mathf.Sin(cycle*Mathf.PI*4);
            float settle=1-blend;float run=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,5f,speed))*blend;float flight=Mathf.Pow(Mathf.Abs(sway),3)*.040f*run;
            // Right leg bears idle weight; opposing shoulder angle avoids a symmetric mannequin stance.
            pelvis.localPosition=V(.038f*settle+sway*.035f*blend,.968f-.081f*blend+bounce*.012f*blend+flight,0);
            pelvis.localRotation=Quaternion.Euler(0,sway*9*blend,-3*settle+sway*5*blend);
            chest.localPosition=V(.018f*settle+travel.x*.045f*blend,1.235f+bounce*.047f*blend+flight,travel.z*.125f*blend-.018f*settle);
            chest.localRotation=Quaternion.Euler((aiming?12:3)+(15+travel.z*10)*blend+bounce*3.5f*blend,-sway*11*blend,2.5f*settle-travel.x*6*blend-sway*5*blend);
            float reach=Mathf.Lerp(.18f,.38f,Mathf.Clamp01(speed/4.5f))*blend;
            float duty=speed>3?(aiming?.38f:.32f):.57f;float stepLift=speed>3?(aiming?.17f:.21f):.11f;
            StepLeg(leftThigh,leftShin,leftFoot,-1,cycle,travel,reach,blend,duty,stepLift);
            StepLeg(rightThigh,rightShin,rightFoot,1,Mathf.Repeat(cycle+.5f,1),travel,reach,blend,duty,stepLift);
        }
        void StepLeg(Transform thigh,Transform shin,Transform foot,int side,float phase,Vector3 travel,float reach,float blend,float stance,float stepLift)
        {
            float along,lift,toe;
            if(phase<stance){float u=phase/stance;along=Mathf.Lerp(reach,-reach,u);lift=0;toe=-5*(1-Mathf.SmoothStep(0,1,u/.18f))+Mathf.SmoothStep(0,28,Mathf.InverseLerp(.52f,1,u));}
            else{float u=(phase-stance)/(1-stance);along=Mathf.Lerp(-reach,reach,u*u*(3-2*u));lift=Mathf.Sin(Mathf.Pow(u,.82f)*Mathf.PI)*stepLift*blend;float release=1-Mathf.SmoothStep(0,1,u/.22f);toe=28*release-Mathf.Sin((u*u*(3-2*u))*Mathf.PI)*27-5*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1,u));}
            Vector3 neutral=V(side*(side<0?.168f:.143f),.101f,side<0?.09f:-.045f);
            Vector3 moving=V(side*(.105f+Mathf.Abs(travel.x)*.022f),.101f+lift,side*.055f*Mathf.Abs(travel.x))+travel*along;
            Vector3 target=Vector3.Lerp(neutral,moving,blend);
            // Keep a safe inter-foot corridor while strafing; the legs step rather than scissor through each other.
            // Stagger fore/aft during strafe instead of clamping the planted foot in world travel.
            float roll=Mathf.Max(0,toe)*blend;
            if(roll>0){Vector3 ball=V(0,-.089f,.12f);target+=Quaternion.Euler(0,side*5,0)*(ball-Quaternion.Euler(roll,0,0)*ball); }
            // The knee always hinges toward the character's front, even while retreating or strafing.
            SolveLeg(thigh,shin,foot,rig.TransformPoint(target),rig.TransformDirection(V(side*.08f,0,1)),toe*blend,side);
            var toeJoint=side<0?leftToe:rightToe;if(toeJoint!=null)toeJoint.localRotation=Quaternion.Euler(-roll,0,0);
        }
        void SolveLeg(Transform thigh,Transform shin,Transform foot,Vector3 target,Vector3 kneePole,float toePitch,int side)
        {
            float scale=Mathf.Max(.001f,Mathf.Abs(transform.lossyScale.y));float upper=.438f*scale,lower=.416f*scale;Vector3 hip=thigh.position,delta=target-hip;
            float distance=Mathf.Clamp(delta.magnitude,.08f*scale,upper+lower-.009f*scale);Vector3 axis=delta.normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 bend=Vector3.ProjectOnPlane(kneePole,axis);
            if(bend.sqrMagnitude<.0001f)bend=Vector3.ProjectOnPlane(rig.up,axis);
            bend.Normalize();
            Vector3 knee=hip+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            // Solve the bend in the character's heading frame. A world-space FromToRotation
            // leaves the front of kneepads facing world +Z when the torso turns.
            thigh.rotation=rig.rotation*Quaternion.FromToRotation(Vector3.down,rig.InverseTransformDirection((knee-hip).normalized));
            shin.rotation=rig.rotation*Quaternion.FromToRotation(Vector3.down,rig.InverseTransformDirection((hip+axis*distance-knee).normalized));
            foot.rotation=rig.rotation*Quaternion.Euler(toePitch,side*5,0);
        }
        public void SetHandTargets(Vector3 rightWorld,Vector3 leftWorld)
        {
            SolveArm(rightUpperArm,rightForearm,rightHand,rightWorld,rig.TransformDirection(V(.45f,-.7f,-.2f)));
            SolveArm(leftUpperArm,leftForearm,leftHand,leftWorld,rig.TransformDirection(V(-.55f,-.75f,-.12f)));
        }
        // Called after SetPose / SetHandTargets: equipment supplies its actual world grip frame.
        public void SetGripOrientation(Quaternion rightWorld,Quaternion leftWorld)
        {
            rightHand.rotation=rightWorld;leftHand.rotation=leftWorld;
        }
        void CombineRigidMeshes(Transform joint)
        {
            // Only merge immediate mesh children. Bone transforms and all attachment sockets survive.
            var children=new List<Transform>();for(int i=0;i<joint.childCount;i++)children.Add(joint.GetChild(i));
            var groups=new Dictionary<Material,List<MeshFilter>>();
            foreach(var child in children)
            {
                var filter=child.GetComponent<MeshFilter>();var renderer=child.GetComponent<MeshRenderer>();
                if(filter!=null&&renderer!=null&&filter.sharedMesh!=null&&child.childCount==0)
                {
                    if(!groups.TryGetValue(renderer.sharedMaterial,out var list)){list=new List<MeshFilter>();groups.Add(renderer.sharedMaterial,list);}list.Add(filter);
                }
                else CombineRigidMeshes(child);
            }
            foreach(var pair in groups)
            {
                if(pair.Value.Count<2)continue;
                var combine=new CombineInstance[pair.Value.Count];int vertexCount=0;
                for(int i=0;i<pair.Value.Count;i++)
                {
                    var filter=pair.Value[i];vertexCount+=filter.sharedMesh.vertexCount;
                    combine[i]=new CombineInstance{mesh=filter.sharedMesh,transform=joint.worldToLocalMatrix*filter.transform.localToWorldMatrix};
                }
                var merged=new Mesh{name=joint.name+" / merged "+pair.Key.name};if(vertexCount>65535)merged.indexFormat=IndexFormat.UInt32;
                merged.CombineMeshes(combine,true,true);merged.RecalculateBounds();meshes.Add(merged);
                var node=Joint(merged.name,joint,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=merged;
                var draw=node.gameObject.AddComponent<MeshRenderer>();draw.sharedMaterial=pair.Key;draw.shadowCastingMode=ShadowCastingMode.On;draw.receiveShadows=true;
                foreach(var filter in pair.Value)
                {
                    var original=filter.sharedMesh;meshes.Remove(original);Destroy(original);filter.gameObject.SetActive(false);Destroy(filter.gameObject);
                }
            }
        }
        void SolveArm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 pole)
        {
            Vector3 a=upper.position, delta=target-a;float scale=transform.lossyScale.y,ul=UpperArmLength*scale,fl=ForearmLength*scale;float distance=Mathf.Clamp(delta.magnitude,.045f*scale,ul+fl-.002f*scale);Vector3 axis=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole,axis).normalized;
            float along=(ul*ul-fl*fl+distance*distance)/(2*distance);
            Vector3 elbow=a+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,ul*ul-along*along));
            upper.rotation=rig.rotation*Quaternion.FromToRotation(Vector3.down,rig.InverseTransformDirection((elbow-a).normalized));
            lower.rotation=rig.rotation*Quaternion.FromToRotation(Vector3.down,rig.InverseTransformDirection((a+axis*distance-elbow).normalized));
            hand.rotation=Quaternion.LookRotation(rig.forward,rig.up)*Quaternion.Euler(-85,0,0);
        }
        void Ellipsoid(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)
        {
            const int rows=14;var ys=new float[rows+1];var xs=new float[rows+1];var zs=new float[rows+1];
            for(int j=0;j<=rows;j++){float a=-Mathf.PI/2+j*Mathf.PI/rows;ys[j]=Mathf.Sin(a)*scale.y;xs[j]=Mathf.Max(.0001f,Mathf.Cos(a)*scale.x);zs[j]=Mathf.Max(.0001f,Mathf.Cos(a)*scale.z);}
            Loft(name,parent,pos,ys,xs,zs,mat,0,24,rows);
        }
        static float Profile(float[] values,float t)
        {
            float f=Mathf.Clamp01(t)*(values.Length-1);int i=Mathf.Min(values.Length-2,(int)f);float u=f-i;
            // Monotone cosine interpolation keeps smooth contours without ringing through joints.
            return Mathf.Lerp(values[i],values[i+1],u*u*(3-2*u));
        }
        static float FoldRidge(float t,float a,float center,float slant,float amplitude,float width)
        {
            float d=(t-center-slant*Mathf.Cos(a))/width;
            return amplitude*(Mathf.Exp(-d*d)-.42f*Mathf.Exp(-(d-1.5f)*(d-1.5f)));
        }
        static float TailoredFold(string name,float t,float a)
        {
            float angular=Mathf.Pow(Mathf.Abs(Mathf.Sin(a+.4f)),4);
            float front=Mathf.Pow(Mathf.Max(0,Mathf.Sin(a)),4);
            float back=Mathf.Pow(Mathf.Max(0,-Mathf.Sin(a)),4);
            if(name.Contains("sleeve deltoid"))return front*(FoldRidge(t,a,.13f,.08f,.012f,.030f)+FoldRidge(t,a,.28f,-.12f,.009f,.042f))+back*FoldRidge(t,a,.44f,.12f,.007f,.060f);
            if(name.Contains("forearm sleeve"))return angular*(FoldRidge(t,a,.12f,.09f,.011f,.037f)+FoldRidge(t,a,.33f,-.14f,.008f,.045f))+back*FoldRidge(t,a,.72f,.13f,.006f,.06f);
            if(name.Contains("trouser thigh"))return front*(FoldRidge(t,a,.14f,.11f,.010f,.034f)+FoldRidge(t,a,.32f,-.13f,.007f,.047f))+back*(FoldRidge(t,a,.74f,.16f,.011f,.055f)+FoldRidge(t,a,.88f,-.09f,.008f,.038f));
            if(name.Contains("shaped calf"))return angular*(FoldRidge(t,a,.14f,.085f,.010f,.04f)+FoldRidge(t,a,.31f,-.11f,.008f,.05f)+FoldRidge(t,a,.79f,.12f,.006f,.065f));
            if(name.Contains("jacket torso"))return Mathf.Abs(Mathf.Cos(a))*(FoldRidge(t,a,.17f,.17f,.012f,.05f)+FoldRidge(t,a,.46f,-.20f,.008f,.075f))+back*FoldRidge(t,a,.72f,.10f,.006f,.06f);
            return 0;
        }
        void Loft(string name,Transform parent,Vector3 pos,float[] ys,float[] xs,float[] zs,Material mat,float relief,int sides=48,int rows=44,float roundness=1)
        {
            // Extra samples resolve sculpted fold lips; tiny lugs keep their economical topology.
            if(rows>=8){sides*=2;rows*=2;}
            var vv=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int j=0;j<=rows;j++)
            {
                float t=(float)j/rows,y=Profile(ys,t),x=Profile(xs,t),z=Profile(zs,t);
                if(name!="Scalp undercut")
                {
                    x=SculptProfile3D.RadiusAtHeight(ys,xs,y);
                    z=SculptProfile3D.RadiusAtHeight(ys,zs,y);
                }
                for(int i=0;i<=sides;i++)
                {
                    float a=i*Mathf.PI*2/sides;float compression=Mathf.Pow(Mathf.Sin(t*Mathf.PI),4);
                    float fold=relief*.18f*Mathf.Sin(a*3+t*15)*compression;
                    // Three diagonal pressure folds, shallow enough to retain the tailored silhouette.
                    float r=fold+relief*.28f*Mathf.Sin(t*24+Mathf.Sin(a*2)*1.5f)*compression;
                    if(relief>0.004f)
                    {
                        // Cloth folds originate at joint compression and seams, not regular rings.
                        r=TailoredFold(name,t,a)*1.65f;
                        // Broad hanging folds terminate at seams; narrower opposing folds gather at joints.
                        float envelope=Mathf.Sin(t*Mathf.PI);
                        float diagonal=a+Mathf.Sin(t*4.1f)*.55f;
                        float drape=(Mathf.Cos(diagonal*3+.7f)*.006f+Mathf.Cos(diagonal*5-1.2f)*.003f)*envelope;
                        if(name.Contains("sleeve")||name.Contains("trouser")||name.Contains("calf"))r+=drape;
                        if(name.Contains("jacket torso"))r+=.009f*Mathf.Cos(a*7+t*2)*envelope*Mathf.Pow(1-t,2);
                    }
                    Vector3 vertex=V(Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),roundness)*(x+r),y,Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),roundness)*(z+r));
                    if(name=="Scalp undercut")vertex.y+=Mathf.Max(0,Mathf.Sin(a))*.030f*(1-t)*(1-t);
                    if(name.StartsWith("Anatomic jaw")&&Mathf.Sin(a)>0)
                    {
                        float fy=y+pos.y,fx=vertex.x;
                        vertex.z+=FaceRelief(fx,fy,Mathf.Sin(a));
                    }
                    vv.Add(vertex);uv.Add(new Vector2((float)i/sides,t));
                    if(j<rows&&i<sides){int b=j*(sides+1)+i;tris.Add(b);tris.Add(b+sides+1);tris.Add(b+1);tris.Add(b+1);tris.Add(b+sides+1);tris.Add(b+sides+2);}
                }
            }
            int bottom=vv.Count;vv.Add(V(0,ys[0],0));uv.Add(Vector2.zero);int top=vv.Count;vv.Add(V(0,ys[ys.Length-1],0));uv.Add(Vector2.one);
            for(int i=0;i<sides;i++){tris.Add(bottom);tris.Add(i);tris.Add(i+1);int b=rows*(sides+1)+i;tris.Add(top);tris.Add(b+1);tris.Add(b);}
            MeshObject(name,parent,pos,vv,uv,tris,mat);
        }
        void Curve(string name,Transform parent,Vector3[] points,float radius,Material mat,bool taper=false)
        {
            int count=(points.Length-1)*8;const int sides=7;var vertices=new List<Vector3>();var uvs=new List<Vector2>();var tr=new List<int>();
            for(int j=0;j<=count;j++)
            {
                float t=(float)j/count*(points.Length-1);int seg=Mathf.Min(points.Length-2,(int)t);float u=t-seg;
                Vector3 a=points[Mathf.Max(0,seg-1)],b=points[seg],c=points[seg+1],d=points[Mathf.Min(points.Length-1,seg+2)];
                Vector3 p=.5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
                Vector3 tangent=(c-b).normalized;Vector3 side=Vector3.Cross(tangent,Mathf.Abs(tangent.y)<.9f?Vector3.up:Vector3.forward).normalized;Vector3 bin=Vector3.Cross(tangent,side);
                float r=radius*(taper?Mathf.Lerp(1,.03f,Mathf.Pow((float)j/count,1.6f)):1);
                for(int i=0;i<=sides;i++)
                {
                    float ang=i*Mathf.PI*2/sides;vertices.Add(p+(side*Mathf.Cos(ang)+bin*Mathf.Sin(ang))*r);uvs.Add(new Vector2((float)i/sides,(float)j/count));
                    if(j<count&&i<sides){int k=j*(sides+1)+i;tr.Add(k);tr.Add(k+1);tr.Add(k+sides+1);tr.Add(k+1);tr.Add(k+sides+2);tr.Add(k+sides+1);}
                }
            }
            MeshObject(name,parent,Vector3.zero,vertices,uvs,tr,mat);
        }
        void MeshObject(string name,Transform parent,Vector3 position,List<Vector3> vertices,List<Vector2> uvs,List<int> triangles,Material material)
        {
            var mesh=new Mesh{name=name};if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
            var node=Joint(name,parent,position);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
        }
        void OnDestroy(){foreach(var m in meshes)if(m!=null)Destroy(m);foreach(var m in materials)if(m!=null)Destroy(m);foreach(var t in textures)if(t!=null)Destroy(t);}
    }
}

















