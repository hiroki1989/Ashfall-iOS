using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Authored against Enemy_0..9: true geometry, never camera-facing cutouts.
    [DefaultExecutionOrder(480)]
    public sealed partial class FaithfulEnemy3D : MonoBehaviour
    {
        public const float DeathDuration=.85f;
        public bool Ready {get;private set;}
        public Vector3 MuzzlePosition=>muzzle?muzzle.position:head?head.TransformPoint(V(0,.02f,.15f)):transform.position+Vector3.up;
        public Transform Head=>head;
        public int Kind=>kind;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Material> materials=new List<Material>();
        Transform rig,pelvis,chest,head,muzzle;
        readonly Transform[] thighs=new Transform[2],shins=new Transform[2],feet=new Transform[2],arms=new Transform[2],forearms=new Transform[2];
        Material flesh,scar,bone,cloth,leather,steel,dark,green,eye;
        Foe owner;int kind,bossAttackStyle=-1;Vector3 previous;float phase,attack,hit,death=-1;Vector3 hitLocal;
        float girth,armLength,legLength,headHeight,baseLean;
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static readonly float[] heights={2.05f,2.02f,2.16f,3.5f,1.9f,2.22f,2.08f,2.5f,2.26f,2.65f};
        public static Transform Create(Transform parent,Vector3 worldPos,int kind)
        {
            var actor=new GameObject("Faithful enemy 3D / "+Foe.Name(kind)).transform;
            actor.SetParent(parent,false);actor.position=worldPos-Vector3.up*.62f;
            var visual=actor.gameObject.AddComponent<FaithfulEnemy3D>();visual.kind=Mathf.Clamp(kind,0,9);visual.Build();visual.previous=worldPos;
            return actor;
        }
        public void Init(Foe foe)
        {
            owner=foe;previous=transform.position;
            // Legacy weapon is gameplay targeting only; this model owns the visible gun.
            if(owner&&owner.weapon)foreach(var r in owner.weapon.GetComponentsInChildren<Renderer>())r.enabled=false;
        }
        public void NotifyAttack(){if(death<0){attack=.38f;bossAttackStyle=owner?owner.BossAttackStyle:-1;}}
        public void NotifyHit(Vector3 point){if(death>=0)return;hit=.23f;hitLocal=transform.InverseTransformDirection(transform.position-point).normalized;}
        public void BeginDeath(){if(death>=0)return;death=0;attack=hit=0;}
        Material Pigment(string name,string surface,Color color)
        {
            var m=new Material(DetailedHeroMaterials3D.Get(name=="ivory bone"||name=="torn tissue"?"Skin":surface)){name="Enemy "+kind+" / "+name,hideFlags=HideFlags.DontSave};m.color=color;
            if(name=="diseased anatomy"||name=="ivory bone"||name=="torn tissue"||name=="acid sacs")
            {
                var atlas=Resources.Load<Texture2D>("Art/SurvivorMaterials/CreatureSurfaceAtlas-v1");
                if(atlas&&m.HasProperty("_CreatureAtlas"))
                {
                    m.SetTexture("_CreatureAtlas",atlas);m.SetFloat("_CreatureSurface",name=="ivory bone"?2:name=="torn tissue"||name=="acid sacs"?3:kind==3?1:0);
                    m.SetFloat("_AtlasStrength",0);m.SetFloat("_Glossiness",name=="acid sacs"?.48f:name=="ivory bone"?.15f:.22f);
                }
            }
            materials.Add(m);return m;
        }
        Transform Joint(string name,Transform parent,Vector3 p)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
        void Build()
        {
            // The authored Enemy_8 mesh and its four-view paint replace the
            // earlier primitive rig in both gameplay and visual review.
            if(TryBuildImportedEnemy())return;
            if(TryBuildSharedGeometry())return;
            bool brute=kind==3,gaunt=kind==4||kind==9,soldier=kind==2||kind==5||kind==8;
            girth=brute?.39f:kind==7?.35f:kind==6?.31f:kind==8?.28f:kind==2?.26f:gaunt?.175f:.215f;
            armLength=kind==4?.40f:brute?.36f:.30f;legLength=brute?.40f:.43f;headHeight=kind==4?1.62f:1.78f;baseLean=kind==4?26:kind==9?13:brute?8:kind==7?3:8;
            Color skin=kind==3?new Color(.27f,.28f,.265f):kind==4?new Color(.40f,.265f,.25f):kind==7?new Color(.48f,.41f,.34f):kind==9?new Color(.46f,.405f,.38f):kind==6?new Color(.43f,.43f,.31f):new Color(.43f,.365f,.27f);
            if(kind==0)skin=new Color(.53f,.46f,.39f);
            if(kind==4)skin=new Color(.49f,.34f,.32f);
            if(kind==7)skin=new Color(.59f,.52f,.44f);
            if(kind==8)skin=new Color(.16f,.13f,.10f);
            Color fabric=kind==1?new Color(.40f,.30f,.085f):kind==2?new Color(.10f,.205f,.22f):kind==4||kind==5?new Color(.105f,.15f,.20f):kind==7?new Color(.36f,.10f,.065f):kind==9?new Color(.22f,.135f,.18f):new Color(.24f,.205f,.12f);
            flesh=Pigment("diseased anatomy","Skin",skin);scar=Pigment("torn tissue","Leather",kind==9?new Color(.28f,.105f,.20f):new Color(.28f,.09f,.06f));
            bone=Pigment("ivory bone","Stitch",new Color(.56f,.50f,.36f));cloth=Pigment("ragged fabric","Cloth",fabric);leather=Pigment("old straps","LeatherDark",new Color(.18f,.12f,.065f));
            steel=Pigment("battered armor","Steel",new Color(.36f,.38f,.355f));dark=Pigment("cavities","Dark",new Color(.035f,.025f,.023f));green=Pigment("acid sacs","TealGlass",kind==1?new Color(.20f,.30f,.035f):kind==9?new Color(.16f,.23f,.035f):new Color(.32f,.42f,.045f));
            eye=Pigment("eyes","AmberGlass",kind==2||kind==8?new Color(.61f,.07f,.025f):kind==5?new Color(.67f,.32f,.035f):kind==6?new Color(.43f,.56f,.07f):new Color(.60f,.48f,.24f));
            rig=Joint("Complete articulated enemy",transform,Vector3.zero);rig.localScale=Vector3.one*(heights[kind]/2.12f);
            pelvis=Joint("Pelvis",rig,V(0,.96f,0));chest=Joint("Chest",rig,V(0,1.34f,0));head=Joint("Head",chest,V(0,headHeight-1.34f,.02f));head.localScale=V(1,.88f,1);
            Loft("Pelvic anatomy",pelvis,V(0,-.11f,0),flesh,new[]{0f,.09f,.23f},new[]{girth*.68f,girth*.86f,girth*.72f},new[]{.12f,.145f,.13f},24,.003f);
            Loft("Tapered thorax",chest,V(0,-.33f,0),flesh,new[]{0f,.12f,.28f,.48f,.61f,.70f},new[]{girth*.66f,girth*.75f,girth,girth*1.04f,girth*.90f,girth*.28f},new[]{.13f,.145f,.17f,.18f,.145f,.07f},32,.003f);
            Ellipsoid("Neck tendons",chest,V(0,.40f,0),V(.065f,.15f,.068f),flesh);
            Face(brute||kind==8);
            for(int i=0;i<2;i++)Limbs(i,i==0?-1:1,gaunt,brute,soldier);
            if(kind==0||kind==1||kind==4||kind==7||kind==9)RaggedJacket(kind==4||kind==9?false:true);
            else if(kind==2||kind==5)Uniform();
            if(kind==0||kind==4)Ribs(chest,false);
            if(kind==3)BoneArmor();
            if(kind==1)AcidSpitter();
            if(kind==2){Mask(false);MilitaryKit();}
            if(kind==5){Hood();Mask(true);BoneArmor();MilitaryKit();}
            if(kind==6)Cycler();
            if(kind==7)Bloater();
            if(kind==8)Plated();
            if(kind==9)Weaver();
            if(kind==0)Hair();
            if(kind==4)for(int i=0;i<9;i++){float y=-.24f+i*.075f;Cone("Exposed vertebra",chest,V(0,y,-.15f),V(0,y+.02f,-.25f),.032f,bone);}
            Belt();
            if(kind==2||kind==5)Firearm();
            else muzzle=Joint("Mouth attack origin",head,V(0,-.01f,.14f));
            SculptDetails();ApplyEnemyShapeRebuild();Batch(rig);ApplyAuthoredEnemyPaint();CacheSharedGeometry();Ready=true;previous=transform.position;LateUpdate();
        }
        void SculptDetails()
        {
            for(int side=-1;side<=1;side+=2)
            {
                int limb=side<0?0:1;float muscle=kind==3?.16f:kind==6?.145f:kind==2?.12f:kind==7?.13f:kind==4||kind==9?.059f:.084f;
                for(int wound=0;wound<4;wound++)
                {
                    float y=-.05f-wound*.058f;Vector3 a=V(side*muscle*.63f,y,muscle*.60f),b=a+V(-side*.018f,-.035f,.012f);
                    Curve("Recessed torn skin cut",arms[limb],new[]{a,(a+b)*.5f+V(.004f,0,.002f),b},.002f,scar);
                    for(int stitch=0;stitch<3;stitch++){Vector3 q=Vector3.Lerp(a,b,(stitch+1)/4f);Rod("Raised torn wound lip",arms[limb],q+V(-.004f,0,.002f),q+V(.004f,-.003f,.002f),.0012f,flesh);}
                }
                for(int crease=0;crease<4;crease++)Curve("Elbow flexion crease",forearms[limb],new[]{V(-muscle*.5f,-.018f-crease*.008f,muscle*.57f),V(0,-.024f-crease*.008f,muscle*.67f),V(muscle*.5f,-.018f-crease*.008f,muscle*.57f)},.0014f,scar);
            }
            if(kind==3||kind==5||kind==8)
                for(int side=-1;side<=1;side+=2)for(int cut=0;cut<3;cut++)
                {float y=-.06f-cut*.075f;Curve("Carapace fine fracture",forearms[side<0?0:1],new[]{V(-.055f,y,.105f),V(-.014f,y+.010f,.115f),V(.012f,y-.002f,.119f),V(.045f,y+.012f,.108f)},.0008f,dark);}
        }
        void Face(bool skull){SculptFace(skull);}
        void Limbs(int i,int s,bool gaunt,bool brute,bool soldier)
        {
            float hip=girth*.54f,thick=brute?.15f:kind==7?.13f:gaunt?.075f:.10f;
            thighs[i]=Joint((s<0?"Left":"Right")+" thigh",pelvis,V(s*hip,-.025f,0));shins[i]=Joint("Shin",thighs[i],V(0,-legLength,0));feet[i]=Joint("Foot",shins[i],V(0,-legLength,0));
            Loft("Thigh muscle",thighs[i],V(0,-legLength,0),flesh,new[]{0f,legLength*.30f,legLength*.73f,legLength},new[]{thick*.66f,thick,thick*1.08f,thick*.80f},new[]{thick*.70f,thick*.86f,thick*.93f,thick*.78f},24,.003f);
            Loft("Calf and tibia",shins[i],V(0,-legLength,0),flesh,new[]{0f,legLength*.30f,legLength*.68f,legLength},new[]{thick*.48f,thick*.65f,thick*.84f,thick*.67f},new[]{thick*.46f,thick*.70f,thick*.80f,thick*.64f},24,.002f);
            Ellipsoid("Exposed kneecap",shins[i],V(0,.005f,.055f),V(thick*.73f,.073f,.045f),flesh);
            float shoe=brute?.12f:.075f;
            Ellipsoid("Foot heel and instep",feet[i],V(0,-.008f,.038f),V(shoe,.067f,.145f),kind==3||kind==4?flesh:kind==8?steel:leather);
            if(kind==3||kind==4){for(int f=0;f<5;f++)Ellipsoid("Bare individual toe",feet[i],V((f-2)*shoe*.35f,-.025f,.15f-Mathf.Abs(f-1)*.011f),V(shoe*.19f,.028f,.041f),flesh,12,8);}
            else
            {
                Loft("Laced combat boot",shins[i],V(0,-legLength-.01f,0),leather,new[]{0f,.07f,.16f,.23f},new[]{.075f,.073f,.06f,.064f},new[]{.072f,.064f,.055f,.054f},24,.001f);
                for(int j=0;j<6;j++){float y=-legLength+.035f+j*.028f;Curve("Boot crossing lace",shins[i],new[]{V(-.029f,y,.066f),V(.029f,y+.017f,.062f)},.0025f,bone);}
                Ellipsoid("Boot rubber sole",feet[i],V(0,-.060f,.04f),V(.079f,.014f,.145f),dark);
            }
            // Separate rag strips leave the damaged knees and tibial ridges visible.
            if(kind!=3)SculptTrousers(thighs[i],shins[i],thick);
            if(soldier){Ellipsoid("Forged knee plate",shins[i],V(0,-.01f,.09f),V(.077f,.085f,.026f),steel);for(int j=0;j<3;j++)Ring("Calf buckle strap",shins[i],-.15f-j*.085f,.075f,.068f,.018f,leather);}
            arms[i]=Joint((s<0?"Left":"Right")+" upper arm",chest,V(s*(girth+.025f),.30f,0));forearms[i]=Joint("Elbow and forearm",arms[i],V(0,-armLength,0));
            float muscle=brute?.16f:kind==6?.145f:kind==2?.12f:kind==7?.13f:gaunt?.059f:.084f;

            Loft("Deltoid biceps triceps",arms[i],V(0,-armLength,0),flesh,new[]{0f,armLength*.24f,armLength*.65f,armLength,armLength+.055f},new[]{muscle*.6f,muscle*.92f,muscle,muscle*.84f,.004f},new[]{muscle*.6f,muscle*.9f,muscle*.94f,muscle*.82f,.004f},28,.0025f);
            Loft("Ulna flexor musculature",forearms[i],V(0,-armLength,0),flesh,new[]{0f,armLength*.2f,armLength*.68f,armLength},new[]{muscle*.43f,muscle*.55f,muscle*.88f,muscle*.65f},new[]{muscle*.4f,muscle*.56f,muscle*.80f,muscle*.66f},24,.002f);
            for(int t=0;t<3;t++)Curve("Forearm tendons",forearms[i],new[]{V((t-1)*.018f,-armLength+.03f,muscle*.40f),V((t-1)*.025f,-armLength*.5f,muscle*.71f),V((t-1)*.028f,-.035f,muscle*.63f)},.0035f,kind==4?scar:flesh);
            var hand=Joint("Palm and fingers",forearms[i],V(0,-armLength,0));Ellipsoid("Palm",hand,V(0,-.045f,.012f),V(muscle*.65f,.052f,.036f),soldier?leather:flesh);
            SculptDigits(hand,muscle,s,soldier);
            if(kind==0||kind==1||kind==5)for(int j=0;j<5;j++)Rag("Shredded sleeve",arms[i],V(Mathf.Sin(j*1.25f)*muscle,-.02f,Mathf.Cos(j*1.25f)*muscle),.07f,kind==5?.28f:.15f,cloth,j+s);
            if(kind==0||kind==4||kind==7||kind==9)for(int j=0;j<7;j++)
            {float y=-.04f-j*.035f;Ellipsoid("Infected skin lesion",arms[i],V(s*muscle*.72f,y,muscle*.56f),V(.012f,.018f,.005f),scar,12,8);}
            if(kind==2||kind==5||kind==8)for(int j=0;j<4;j++)Ring("Forearm wraps",forearms[i],-.06f-j*.055f,muscle*.8f,muscle*.75f,.016f,kind==8?steel:leather);
        }
        void Ribs(Transform p,bool luminous)
        {
            for(int s=-1;s<=1;s+=2)for(int j=0;j<6;j++)
            {
                float y=.23f-j*.051f;float w=girth*(.80f-j*.055f);
                Curve("Exposed curved rib",p,new[]{V(s*.012f,y,.16f),V(s*w*.55f,y-.025f,.18f),V(s*w,y+.008f,.115f)},.013f,luminous?green:bone);
                if(!luminous)Curve("Intercostal wound",p,new[]{V(s*.025f,y-.018f,.161f),V(s*w*.6f,y-.041f,.165f)},.010f,scar);
            }
            Curve("Sternum",p,new[]{V(0,.27f,.166f),V(0,.13f,.172f),V(0,-.04f,.153f)},.014f,bone);
        }
        void RaggedJacket(bool sleeves)
        {
            // Open front panels wrap the back; no cylinder hides the distinctive exposed anatomy.
            for(int s=-1;s<=1;s+=2)
            {
                Panel("Open ragged jacket front",chest,s,cloth);
                FoldedLapel(s);
                for(int j=0;j<4;j++)Ellipsoid("Worn jacket button",chest,V(s*.16f,.19f-j*.095f,.162f),V(.008f,.008f,.004f),bone,10,6);
            }
        }
        void Panel(string name,Transform p,int s,Material m)
        {
            var v=new List<Vector3>();var ix=new List<int>();const int rows=12,cols=10;
            for(int j=0;j<=rows;j++)for(int k=0;k<=cols;k++)
            {
                float t=j/(float)rows,a=Mathf.Lerp(.50f,3.25f,k/(float)cols);float y=Mathf.Lerp(.37f,-.30f,t);
                if(j==rows)y-=.025f+Mathf.Abs(Mathf.Sin(k*2.3f))*.07f;
                                float height=y+.33f;
                float w=SculptProfile3D.RadiusAtHeight(new[]{0f,.12f,.28f,.48f,.61f,.70f},new[]{girth*.66f,girth*.75f,girth,girth*1.04f,girth*.90f,girth*.28f},height)+.014f;
                float depth=SculptProfile3D.RadiusAtHeight(new[]{0f,.12f,.28f,.48f,.61f,.70f},new[]{.13f,.145f,.17f,.18f,.145f,.07f},height)+.012f;
                v.Add(V(s*Mathf.Sin(a)*w,y,Mathf.Cos(a)*(depth+.005f*Mathf.Sin(k*1.2f+t*5)*Mathf.Sin(t*Mathf.PI))));
            }
            for(int j=0;j<rows;j++)for(int k=0;k<cols;k++){if(j>6&&(j+k*3)%19==0)continue;int a=j*(cols+1)+k,b=a+cols+1;Quad(ix,a,b,a+1,b+1,s<0);}
            MeshPart(name,p,m,v,ix,true);
        }
        void Uniform()
        {
            Loft("Uniform tunic",chest,V(0,-.31f,0),cloth,new[]{0f,.12f,.28f,.48f,.61f,.70f},new[]{girth*.72f,girth*.81f,girth*1.07f,girth*1.09f,girth*.96f,girth*.33f},new[]{.155f,.17f,.195f,.218f,.17f,.09f},32,.005f);
            for(int j=0;j<10;j++){float a=j*Mathf.PI*.2f;Rag("Torn tunic hem",chest,V(Mathf.Sin(a)*girth*.74f,-.24f,Mathf.Cos(a)*.15f),.09f,.16f,cloth,j);}
            Curve("Tunic center opening",chest,new[]{V(0,.31f,.11f),V(0,.1f,.214f),V(0,-.27f,.165f)},.005f,dark);
        }
        void MilitaryKit()
        {
            for(int s=-1;s<=1;s+=2)
            {
                HarnessRibbon(chest,new[]{V(s*.15f,.24f,-.155f),V(s*.18f,.34f,0),V(s*.175f,.29f,.17f),V(s*.165f,.19f,.244f),V(s*.15f,.03f,.248f),V(s*.14f,-.16f,.225f),V(s*.15f,-.29f,.17f)},.033f);
                for(int j=0;j<3;j++)Pouch(chest,V(s*(.05f+j*.063f),.015f,.275f),V(.052f,.11f,.060f));
                Plate("Military shoulder plate",arms[s<0?0:1],V(s*.025f,0,.02f),.14f,.115f,steel);
                Loft("Torn uniform shoulder sleeve",arms[s<0?0:1],V(0,-.15f,0),cloth,new[]{0f,.04f,.10f,.15f,.20f},new[]{.125f,.130f,.132f,.115f,.002f},new[]{.12f,.126f,.13f,.11f,.002f},48,.003f);
            }
        }
        void AcidSpitter()
        {
            for(int s=-1;s<=1;s+=2)Ellipsoid("Swollen acid cheek sac",head,V(s*.11f,-.02f,.01f),V(s<0?.108f:.067f,.115f,.076f),green);
            for(int j=0;j<6;j++){float a=j*2.4f;Ellipsoid("Acid dermal blister",chest,V(Mathf.Sin(a)*.14f,.18f-j*.045f,.16f),V(.035f,.043f,.025f),j%2==0?green:flesh);}
            for(int s=-1;s<=1;s+=2)Curve("Throat sac vein",head,new[]{V(s*.09f,.04f,.078f),V(s*.135f,-.015f,.071f),V(s*.10f,-.10f,.065f)},.004f,bone);
        }
        void Mask(bool hooded)
        {
            Ellipsoid("Respirator face cup",head,V(0,.025f,.104f),V(.074f,.074f,.05f),steel);
            for(int s=-1;s<=1;s+=2)
            {
                Ellipsoid("Mask lens rim",head,V(s*.044f,.13f,.097f),V(.038f,.031f,.018f),leather);
                Ellipsoid("Amber or red eye lens",head,V(s*.044f,.13f,.113f),V(.024f,.018f,.008f),eye);
                Ellipsoid("Respirator charcoal filter",head,V(s*.061f,.004f,.131f),V(.033f,.035f,.025f),dark);
                for(int j=0;j<4;j++)Curve("Respirator filter grille",head,new[]{V(s*.061f-.022f+j*.014f,-.016f,.152f),V(s*.061f-.022f+j*.014f,.023f,.152f)},.002f,steel);
            }
            Curve("Cranial mask strap",head,new[]{V(-.10f,.12f,.04f),V(-.097f,.13f,-.065f),V(0,.14f,-.086f),V(.097f,.13f,-.065f),V(.10f,.12f,.04f)},.012f,leather);
        }
        void Hood()
        {
            var v=new List<Vector3>();var ix=new List<int>();const int n=26,r=14;
            for(int j=0;j<=r;j++)for(int i=0;i<=n;i++)
            {float a=Mathf.Lerp(.57f,Mathf.PI*2-.57f,i/(float)n),t=j/(float)r;float rad=Mathf.Sin(t*Mathf.PI*.76f);v.Add(V(Mathf.Sin(a)*.14f*rad,.33f-t*.43f,Mathf.Cos(a)*.12f*rad-.018f));}
            for(int j=0;j<r;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i,b=a+n+1;Quad(ix,a,b,a+1,b+1,false);}MeshPart("Navy hood open face",head,cloth,v,ix,true);
            for(int s=-1;s<=1;s+=2)Curve("Hood bone trimmed aperture",head,new[]{V(0,.33f,-.018f),V(s*.105f,.23f,.076f),V(s*.14f,.10f,.07f),V(s*.09f,-.10f,.06f)},.013f,leather);
        }
        void BoneArmor()
        {
            for(int s=-1;s<=1;s+=2)
            {
                var shoulder=arms[s<0?0:1];Plate("Organic bony shoulder carapace",shoulder,V(s*.015f,.035f,0),kind==3?.235f:.15f,kind==3?.18f:.12f,bone);
                for(int j=0;j<6;j++){float a=j*2.399963f+s*.31f;Vector3 a0=V(Mathf.Sin(a)*.12f,.045f,Mathf.Cos(a)*.10f);Cone("Broken organic shoulder bone spine",shoulder,a0,a0+V(Mathf.Sin(a)*.09f,.09f+(j%3)*.04f,Mathf.Cos(a)*.075f),.022f,bone);}
                if(kind==3){Plate("Forearm bone plate",forearms[s<0?0:1],V(0,-.14f,.06f),.16f,.19f,bone);for(int j=0;j<4;j++)Cone("Ulna spine",forearms[s<0?0:1],V(s*.09f,-.06f-j*.07f,0),V(s*.20f,-.02f-j*.07f,-.02f),.025f,bone);}
            }
            for(int j=0;j<7;j++){float y=-.24f+j*.095f;Ellipsoid("Vertebral bone plate",chest,V(0,y,-.17f),V(.065f,.05f,.026f),bone);Cone("Spine crest",chest,V(0,y,-.18f),V(0,y+.07f,-.29f),.024f,bone);}
            if(kind==3)for(int j=0;j<12;j++){float a=j*Mathf.PI/6;Rag("Giant leather loincloth",pelvis,V(Mathf.Sin(a)*.27f,-.025f,Mathf.Cos(a)*.16f),.11f,.31f,leather,j);}
        }
        void Cycler()
        {
            // The source has glowing canisters in a metal harness, not luminous exposed ribs.
            for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++){
                var tube=Joint("Chest reagent canister",chest,V(side*.12f,.18f-row*.11f,.165f));
                Ellipsoid("Green reagent behind glass",tube,Vector3.zero,V(.060f,.046f,.042f),green,40,28);
                Ring("Canister upper cage band",tube,.033f,.063f,.045f,.012f,steel);
                Ring("Canister lower cage band",tube,-.033f,.063f,.045f,.012f,steel);
                Rod("Canister retaining bar",tube,V(side*.05f,-.045f,.035f),V(side*.05f,.045f,.035f),.007f,leather);
            }
            for(int s=-1;s<=1;s+=2)
            {
                Ellipsoid("Pressure tank green contents",chest,V(s*.12f,.10f,-.24f),V(.085f,.28f,.085f),green);
                for(int j=0;j<6;j++)Ring("Tank iron cage band",chest,-.12f+j*.075f,.095f,.095f,.018f,leather,V(s*.12f,0,-.24f));
                Curve("Tank shoulder hose",chest,new[]{V(s*.12f,.36f,-.23f),V(s*.25f,.40f,-.10f),V(s*.27f,.25f,.10f),V(s*.20f,-.13f,.16f)},.025f,leather);
                for(int j=0;j<7;j++)Ellipsoid("Radiation lesions",arms[s<0?0:1],V(s*.10f,-j*.035f,.075f),V(.017f,.011f,.007f),green,12,8);
            }
        }
        void Bloater()
        {
            Ellipsoid("Pendulous distended belly",chest,V(0,-.06f,.13f),V(.40f,.37f,.295f),flesh,40,28);
            Ellipsoid("Navel cavity",chest,V(0,-.19f,.416f),V(.016f,.014f,.005f),scar,12,8);
            for(int j=0;j<3;j++)Curve("Abdominal stretched fold",chest,new[]{V(-.18f,-.26f+j*.035f,.34f),V(0,-.28f+j*.035f,.42f),V(.18f,-.26f+j*.035f,.34f)},.003f,flesh);
        }
        void Plated()
        {
            for(int row=0;row<3;row++)Plate("Layered steel cuirass",chest,V(0,.25f-row*.15f,.20f-row*.02f),girth*(1.0f-row*.07f),.115f,steel);
            var rear=Joint("Continuous rear armor coverage",chest,Vector3.zero);rear.localRotation=Quaternion.Euler(0,180,0);
            for(int row=0;row<3;row++)Plate("Overlapping rear steel cuirass",rear,V(0,.25f-row*.15f,.18f-row*.01f),girth*(1.0f-row*.08f),.13f,steel);
            for(int s=-1;s<=1;s+=2)
            {
                Plate("Rust steel pauldron",arms[s<0?0:1],V(s*.015f,.025f,.025f),.175f,.14f,steel);
                for(int j=0;j<4;j++)Cone("Armor shoulder thorn",arms[s<0?0:1],V(s*.11f,.06f,-.07f+j*.05f),V(s*.20f,.18f,-.08f+j*.05f),.025f,bone);
                for(int j=0;j<3;j++)Plate("Layered arm armor",forearms[s<0?0:1],V(0,-.05f-j*.08f,.055f),.10f,.06f,steel);
                Plate("Steel thigh guard",thighs[s<0?0:1],V(0,-.19f,.08f),.105f,.18f,steel);
                Plate("Steel shin guard",shins[s<0?0:1],V(0,-.19f,.075f),.085f,.18f,steel);
                Cone("Crown horn",head,V(s*.085f,.23f,0),V(s*.14f,.39f,.025f),.035f,bone);
            }
            Cone("Crown central spike",head,V(0,.26f,.02f),V(0,.42f,.04f),.033f,bone);
        }
        void Weaver()
        {
            var olive=Pigment("stained olive mantle bindings","Cloth",new Color(.22f,.24f,.12f));
            for(int j=0;j<9;j++)
            {
                float a=j*2.399f;Vector3 p=j<2?V((j==0?-1:1)*.19f,.28f,.02f):V(Mathf.Sin(a)*.19f,.27f+(j%3)*.085f,-.15f-Mathf.Abs(Mathf.Cos(a))*.08f);
                Ellipsoid("Purple regenerative tumor",chest,p,V(.12f,.13f,.105f),scar,28,18);
                for(int n=0;n<4;n++){float t=n*Mathf.PI*.5f,face=j<2?1:-1;Curve("Green tumor vessels",chest,new[]{p+V(Mathf.Cos(t)*.10f,.06f,face*.03f),p+V(Mathf.Cos(t+.6f)*.08f,0,face*.09f),p+V(Mathf.Cos(t+.8f)*.05f,-.09f,face*.04f)},.005f,green);}
            }
            for(int j=0;j<8;j++){float a=j*Mathf.PI*.25f;Rag("Long purple green mantle",pelvis,V(Mathf.Sin(a)*.17f,-.035f,Mathf.Cos(a)*.13f),.13f,.34f,j%2==0?cloth:olive,j);}
        }
        void Hair()
        {
            var hair=Pigment("matted ash grey hair","Hair",new Color(.18f,.17f,.15f));
            var ys=new[]{-.074f,-.050f,-.014f,.040f,.095f,.145f,.192f,.224f,.238f};
            var xs=new[]{.030f,.054f,.070f,.087f,.088f,.084f,.071f,.045f,.003f};
            var zs=new[]{.036f,.053f,.063f,.073f,.078f,.077f,.069f,.047f,.003f};
            for(int j=0;j<180;j++)
            {
                float a=j*2.399963f,u=.64f+(j%17)/17f*.29f;
                bool front=Mathf.Cos(a)>.28f;if(front)u=Mathf.Max(.82f,u);
                float y=SoftProfile(ys,u);Vector3 p=V(Mathf.Sin(a)*(SculptProfile3D.RadiusAtHeight(ys,xs,y)+.001f),y,Mathf.Cos(a)*(SculptProfile3D.RadiusAtHeight(ys,zs,y)+.001f));
                float drop=front?.018f:.045f+(j%5)*.006f;
                Curve("Sparse swept grey hair filament",head,new[]{p,p+V(Mathf.Sin(a)*.009f,.009f,-.008f),p+V(Mathf.Sin(a)*.013f,-drop,Mathf.Cos(a)*.012f)},.00145f,hair);
            }
        }
        void Belt()
        {
            Ring("Weathered waist belt",pelvis,.065f,girth*.90f,.155f,.042f,leather);
            for(int side=-1;side<=1;side+=2)Pouch(pelvis,V(side*girth*.68f,.018f,.137f),V(.085f,.115f,.050f));
            for(int side=-1;side<=1;side+=2)Rod("Belt buckle side",pelvis,V(side*.026f,.045f,.163f),V(side*.026f,.086f,.163f),.003f,steel);
            Rod("Belt buckle upper",pelvis,V(-.026f,.086f,.163f),V(.026f,.086f,.163f),.003f,steel);
            Rod("Belt buckle lower",pelvis,V(-.026f,.045f,.163f),V(.026f,.045f,.163f),.003f,steel);
        }
        void Pouch(Transform parent,Vector3 pos,Vector3 size)
        {
            Loft("Seamed leather ammunition pouch",parent,pos-V(0,size.y*.5f,0),leather,new[]{0f,size.y*.08f,size.y*.83f,size.y},new[]{size.x*.35f,size.x*.50f,size.x*.49f,size.x*.40f},new[]{size.z*.37f,size.z*.50f,size.z*.50f,size.z*.36f},32,.001f);
            Curve("Pouch flap seam",parent,new[]{pos+V(-size.x*.4f,size.y*.25f,size.z*.52f),pos+V(0,size.y*.12f,size.z*.58f),pos+V(size.x*.4f,size.y*.25f,size.z*.52f)},.0016f,bone);
            Ellipsoid("Pouch closure stud",parent,pos+V(0,size.y*.10f,size.z*.60f),V(.005f,.006f,.003f),steel,12,8);
        }
        void Firearm()
        {
            var gun=Joint("Integrated enemy firearm",chest,V(.05f,.06f,.26f));
            Ellipsoid("Carved receiver",gun,Vector3.zero,V(.055f,.065f,.20f),steel);
            Rod("Rifled barrel",gun,V(0,.02f,.15f),V(0,.02f,.57f),.019f,steel);
            Rod("Wood stock",gun,V(0,-.02f,-.15f),V(.035f,-.04f,-.39f),.055f,leather);
            Rod("Pistol grip",gun,V(0,-.03f,-.07f),V(0,-.17f,-.10f),.025f,leather);
            Rod("Magazine",gun,V(0,-.03f,.07f),V(0,-.23f,.12f),.032f,steel);
            Rod("Optical scope",gun,V(0,.10f,-.07f),V(0,.10f,.13f),.023f,dark);
            muzzle=Joint("Visible enemy muzzle",gun,V(0,.02f,.59f));
        }
        static readonly float[] GaitStride={1.42f,1.18f,1.65f,2.65f,1.08f,1.36f,1.95f,1.17f,2.05f,1.22f};
        static readonly float[] GaitHunch={18,26,11,13,46,23,18,8,16,27};
        static readonly float[] GaitRoll={4,9,3,5,7,3,6,12,4,8};
        float nextAnimAt;
        void LateUpdate()
        {
            if(!Ready)return;float dt=Time.deltaTime;
            if(death>=0){if(death<=DeathDuration+.1f)AnimateDeath(dt);return;}
            if(owner&&owner.dead&&!owner.Escaping){BeginDeath();return;}
            if(owner&&owner.game&&!owner.game.Active){previous=transform.position;return;}
            Vector3 delta=transform.position-previous;previous=transform.position;delta.y=0;
            float speed=dt>.0001f?delta.magnitude/dt:0,moving=Mathf.Clamp01(speed/.7f);
            if(owner&&owner.game&&attack<=0&&hit<=0&&!owner.IsTelegraphing)
            {
                float distSq=(transform.position-owner.game.PlayerPosition).sqrMagnitude;
                if(distSq>30f*30f)return;
                if((Application.isMobilePlatform||distSq>16f*16f)&&Time.time<nextAnimAt)return;
                if(Application.isMobilePlatform||distSq>16f*16f)nextAnimAt=Time.time+(distSq>16f*16f?.08f:moving<.05f?.08f:.033f);
            }

            phase+=delta.magnitude/GaitStride[kind];float wave=Mathf.Sin(phase*Mathf.PI*2),beat=Mathf.Sin(phase*Mathf.PI*4);
            float breath=Mathf.Sin(Time.time*(kind==7?.9f:kind==4?3.3f:1.4f)+kind);
            attack=Mathf.Max(0,attack-dt);hit=Mathf.Max(0,hit-dt);
            float strike=attack>0?Mathf.Sin((1-attack/.38f)*Mathf.PI):0;
            float anticipate=owner&&owner.IsTelegraphing?owner.TelegraphProgress:0;
            Vector3 travel=transform.InverseTransformDirection(delta);float forward=travel.z<-.001f?-1:1;
            float lateral=delta.sqrMagnitude>.00001f?travel.normalized.x:0;


            // Distinct silhouettes: spitters rear back, gunners brace, chargers coil,
            // and the healer opens its chest. Keep the pelvis above the grounded feet.
            float crouch=anticipate*(kind==4?19:kind==8?10:kind==7?7:kind==0?8:0);
            float rear=anticipate*(kind==1?-13:kind==2?-5:kind==3?-10:kind==5?-7:kind==6?-8:kind==9?-9:0);
            float lean=(kind==4?12:GaitHunch[kind])+crouch+rear;
            float pitchAttack=kind==1?-24:kind==3?28:kind==4?-23:kind==7?-12:kind==9?-7:-17;
            float hitPulse=hit>0?Mathf.Sin((1-hit/.23f)*Mathf.PI)*Mathf.Clamp01(hit/.12f):0;
            float recoil=kind==2||kind==5?strike*7:0;
            rig.localRotation=Quaternion.Euler(lean+strike*pitchAttack+recoil+hitLocal.z*hitPulse*11,0,wave*GaitRoll[kind]*moving+breath*(kind==9?2:0)+hitLocal.x*hitPulse*9);
            float bob=kind==3?-.035f*Mathf.Pow(Mathf.Abs(wave),.5f):kind==4?.035f*Mathf.Abs(wave):kind==7?-.018f*Mathf.Abs(wave):.018f*Mathf.Abs(wave);
            rig.localPosition=V(wave*(kind==7?.025f:kind==1?.017f:.008f)*moving,bob*moving+(kind==4?-.10f:0),strike*(kind==4?.16f:kind==3?.06f:0));
            chest.localPosition=V(0,1.34f+breath*(kind==7?.018f:.007f),0);
            chest.localRotation=Quaternion.Euler((kind==4?30:0)+anticipate*(kind==1?-16:kind==3?-9:kind==9?-12:kind==7?8:kind==6?-6:0)+strike*(kind==3?18:kind==7?15:9)+breath*(kind==1?2:0),wave*(kind==3?6:kind==7?3:kind==4?10:5)*moving,breath*(kind==9?3:0));
            pelvis.localRotation=Quaternion.Euler(kind==4?12:0,-wave*(kind==3?7:4)*moving,wave*(kind==7?5:1)*moving);
            for(int i=0;i<2;i++)
            {
                int side=i==0?-1:1;float w=i==0?wave:-wave;
                float legSwing=kind==0?27:kind==1?22:kind==2?24:kind==3?19:kind==4?39:kind==5?27:kind==6?30:kind==7?14:kind==8?18:15;
                float spread=kind==3?side*8:kind==4?side*12:kind==7?side*7:kind==8?side*5:0;
                float lift=Mathf.Max(0,-w)*moving;
                thighs[i].localRotation=Quaternion.Euler(w*legSwing*moving*forward+(kind==4?-10:0)+strike*(kind==3?-13:kind==4?20:0),side*(kind==7?9:0),spread-lateral*w*10*moving);
                shins[i].localRotation=Quaternion.Euler((kind==4?18:0)+lift*(kind==4?57:kind==3?29:kind==7?20:kind==8?28:37)+strike*(kind==3?12:0),0,0);
                feet[i].localRotation=Quaternion.Euler(-lift*(kind==4?25:13),0,side*(kind==7?9:4));
                bool gun=kind==2||kind==5;
                float armPitch=gun?-62:-12-w*(kind==3?9:kind==4?31:kind==7?7:19)*moving;
                float elbow=gun?-60:kind==4?-44:kind==9?-68:-18;
                float abduction=side*(kind==3?22:kind==4?29:kind==7?27:kind==9?36:10);
                if(kind==0){armPitch=-24-w*15*moving-anticipate*36-strike*72;elbow=-35-anticipate*18-strike*22;}
                else if(kind==1){armPitch=-28-w*12*moving-anticipate*34-strike*25;elbow=-55-anticipate*19-strike*16;}
                else if(kind==2){armPitch=-62-anticipate*8+strike*10;elbow=-60-anticipate*12;}
                else if(kind==3){armPitch-=anticipate*105;armPitch+=strike*65;elbow=-32-anticipate*36+strike*15;}
                else if(kind==4){armPitch=-35-w*25*moving-anticipate*55+strike*85;elbow=-15+strike*18;}
                else if(kind==5){armPitch=-71-anticipate*13+strike*13;elbow=-66-anticipate*9;}
                else if(kind==6){armPitch=-32-w*11*moving-anticipate*42+strike*32;elbow=-42-anticipate*23;abduction+=side*(anticipate*13+strike*8);}
                else if(kind==7){armPitch=-22-anticipate*32-strike*52;elbow=-35-anticipate*18;abduction+=side*anticipate*8;}
                else if(kind==8){armPitch=-30-w*8*moving-anticipate*27-strike*75;elbow=-25-strike*35;}
                else if(kind==9){armPitch=-48+Mathf.Sin(Time.time*1.7f+side)*5-anticipate*42-strike*35;elbow=-70+breath*8-anticipate*15;abduction+=side*(anticipate*24+strike*16);}
                if(ImportedShapeActive&&kind==8){armPitch=-5-w*8*moving-anticipate*27-strike*75;elbow=-10-strike*35;abduction=side*(38+anticipate*8);}
                if(kind!=3&&kind!=5&&moving>.1f&&anticipate<.01f&&strike<.01f&&death<0)
                {armPitch=-57-w*5*moving;elbow=24;abduction=side*13;}
                arms[i].localRotation=Quaternion.Euler(armPitch,gun?side*-18:side*(kind==4?12:0),abduction);
                forearms[i].localRotation=Quaternion.Euler(elbow,0,side*strike*(kind==4?15:0));
            }
            head.localRotation=Quaternion.Euler((kind==4?-58:-lean*.48f)+anticipate*(kind==1?-18:kind==7?12:kind==9?-10:kind==0?8:0)+strike*(kind==1?-30:kind==3?20:kind==9?-12:8)-hitLocal.z*hitPulse*7,breath*(kind==4?5:kind==0?4:2),breath*(kind==0?3:kind==9?4:0)-hitLocal.x*hitPulse*7);
            if(kind==3) AnimateColossusAttack(anticipate,strike);
            GroundContact(strike);
        }
        void AnimateColossusAttack(float windup,float strike)
        {
            int style=owner&&owner.IsTelegraphing?owner.BossAttackStyle:bossAttackStyle;
            if(windup<=0&&strike<=0&&!(owner&&owner.IsLunging))return;
            float motion=Mathf.Max(strike,owner&&owner.IsLunging?.8f:0);
            if(style==0)
            {
                // Low, committed shoulder charge. Both arms guard the head.
                rig.localRotation*=Quaternion.Euler(-24*windup+34*motion,0,0);
                rig.localPosition+=V(0,-.16f*windup,-.12f*windup+.18f*motion);
                for(int i=0;i<2;i++)
                {
                    int side=i==0?-1:1;
                    arms[i].localRotation=Quaternion.Euler(-18-18*windup-35*motion,side*10,side*(10+8*motion));
                    forearms[i].localRotation=Quaternion.Euler(-45-20*motion,0,0);
                    thighs[i].localRotation*=Quaternion.Euler((i==0?-1:1)*(26*windup+36*motion),0,0);
                }
            }
            else if(style==1)
            {
                // Raised fists remain legible through the long warning, then drive down.
                chest.localRotation*=Quaternion.Euler(-17*windup+48*strike,0,0);
                for(int i=0;i<2;i++)
                {
                    int side=i==0?-1:1;
                    arms[i].localRotation=Quaternion.Euler(-148*windup+46*strike,0,side*(18+12*windup));
                    forearms[i].localRotation=Quaternion.Euler(-46-22*windup+38*strike,0,0);
                }
                rig.localPosition+=V(0,-.10f*windup-.18f*strike,.12f*strike);
            }
            else if(style==2)
            {
                // One-sided wind-up and horizontal sweep; the rear arc stays readable.
                chest.localRotation*=Quaternion.Euler(0,-46*windup+88*strike,0);
                arms[0].localRotation=Quaternion.Euler(-34-30*windup,0,-32-18*windup);
                forearms[0].localRotation=Quaternion.Euler(-62+20*strike,0,0);
                arms[1].localRotation=Quaternion.Euler(-36-38*windup+24*strike,0,46-20*strike);
                forearms[1].localRotation=Quaternion.Euler(-95+68*strike,0,0);
            }
        }
        void AnimateDeath(float dt)
        {
            death+=dt;float t=Mathf.Clamp01(death/DeathDuration),fall=Mathf.SmoothStep(0,1,t);
            float kneel=Mathf.Sin(Mathf.Min(1,t*2)*Mathf.PI*.5f);
            float pitch=kind==0?80:kind==1?78:kind==2?86:kind==3?45:kind==4?100:kind==5?83:kind==6?76:kind==7?-72:kind==8?72:68;
            float roll=kind==0?-15:kind==1?18:kind==2?-18:kind==3?-47:kind==4?12:kind==5?21:kind==6?-25:kind==7?38:kind==8?-19:26;
            rig.localRotation=Quaternion.Euler(Mathf.Lerp(baseLean,pitch,fall),0,roll*fall);
            rig.localPosition=V(0,-(kind==3?.36f:kind==7?.18f:.15f)*kneel,.15f*fall);
            chest.localRotation=Quaternion.Euler(20*kneel,0,roll*.25f*fall);head.localRotation=Quaternion.Euler(20*fall,18*fall,0);
            for(int i=0;i<2;i++){float side=i==0?-1:1;thighs[i].localRotation=Quaternion.Euler(-32*kneel,0,side*(kind==3?14:9)*fall);shins[i].localRotation=Quaternion.Euler((kind==4?65:85)*kneel,0,0);feet[i].localRotation=Quaternion.Euler(-12*kneel,0,0);arms[i].localRotation=Quaternion.Euler(-25+(kind==9?85:65)*fall,0,side*(kind==7?52:35)*fall);forearms[i].localRotation=Quaternion.Euler(-18-24*fall,0,0);}
        }
        // Geometry helpers: independent joint batches preserve animation and reduce draw calls.
        void MeshPart(string name,Transform p,Material mat,List<Vector3> verts,List<int> ix,bool twoSided=false)
        {
            if(twoSided)
            {
                // Separate back vertices keep RecalculateNormals from cancelling front normals.
                int count=ix.Count,offset=verts.Count;verts.AddRange(verts.ToArray());
                for(int i=0;i<count;i+=3){ix.Add(ix[i]+offset);ix.Add(ix[i+2]+offset);ix.Add(ix[i+1]+offset);}
            }
            var mesh=new Mesh{name=name};if(verts.Count>65535)mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(ix,0);
            var uv=new Vector2[verts.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(verts[i].x*4,verts[i].y*4);mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
            var t=Joint(name,p,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=Application.isMobilePlatform&&kind!=3?ShadowCastingMode.Off:ShadowCastingMode.On;if(Application.isMobilePlatform)r.receiveShadows=false;
        }
        static void Quad(List<int> ix,int a,int b,int c,int d,bool reverse)
        {if(reverse)ix.AddRange(new[]{a,c,b,c,d,b});else ix.AddRange(new[]{a,b,c,c,b,d});}
        void Ellipsoid(string n,Transform p,Vector3 pos,Vector3 scale,Material m,int sides=80,int rows=48)
        {
            var v=new List<Vector3>();var ix=new List<int>();
            for(int j=0;j<=rows;j++)for(int i=0;i<=sides;i++){
                float a=i*Mathf.PI*2/sides,t=j*Mathf.PI/rows;
                Vector3 radial=V(Mathf.Cos(a)*Mathf.Sin(t),Mathf.Cos(t),Mathf.Sin(a)*Mathf.Sin(t));
                Vector3 point=Vector3.Scale(radial,scale);
                if(n.Contains("belly")||n.Contains("cheek sac")||n.Contains("tumor"))
                {
                    float tissue=(.007f*Mathf.Sin(a*3+t*2)+.003f*Mathf.Sin(a*7-t*5))*Mathf.Sin(t)*Mathf.Sin(t);
                    point+=radial*tissue;
                    if(n.Contains("belly"))point.y-=.023f*Mathf.Sin(t)*Mathf.Sin(t)*Mathf.Max(0,radial.z);
                }
                v.Add(pos+point);
            }
            for(int j=0;j<rows;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;Quad(ix,a,b,a+1,b+1,true);}MeshPart(n,p,m,v,ix);
        }
        void Loft(string n,Transform p,Vector3 pos,Material m,float[] ys,float[] xs,float[] zs,int sides,float wrinkle)
        {
            var v=new List<Vector3>();var ix=new List<int>();sides=Mathf.Max(80,sides);const int between=18;int rows=(ys.Length-1)*between;
            for(int j=0;j<=rows;j++){int ring=Mathf.Min(ys.Length-2,j/between);float t=(j-ring*between)/(float)between;
                float y=Mathf.Lerp(ys[ring],ys[ring+1],t);
                float rx=SculptProfile3D.RadiusAtHeight(ys,xs,y),rz=SculptProfile3D.RadiusAtHeight(ys,zs,y);
                for(int i=0;i<=sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,u=j/(float)rows;
                    float w=wrinkle*Mathf.Sin(a*3+u*rows*.32f);
                    // Longitudinal separation follows the arm's muscles, with taper near joints.
                    if(n.Contains("biceps")||n.Contains("flexor"))
                    {float belly=Mathf.Pow(Mathf.Sin(u*Mathf.PI),2);w+=.0035f*belly*(Mathf.Sin(a*5+u*2)+.28f*Mathf.Sin(a*11-u*8));}
                    if(n.Contains("tunic")||n.Contains("boot"))w+=wrinkle*.8f*Mathf.Sin(u*51+Mathf.Sin(a*2)*2)*Mathf.Sin(u*Mathf.PI);
                    v.Add(AnatomicSurface(n,pos+V(Mathf.Cos(a)*(rx+w),y,Mathf.Sin(a)*(rz+w)),a,u));
                }}
            for(int j=0;j<rows;j++)for(int i=0;i<sides;i++){if(n.Contains("Torn trouser")&&j/(float)rows<.22f&&Mathf.Sin((i+.5f)*Mathf.PI*2/sides)>.38f)continue;int a=j*(sides+1)+i,b=a+sides+1;Quad(ix,a,b,a+1,b+1,false);}
            int bottom=v.Count;v.Add(pos+V(0,ys[0],0));int top=v.Count;v.Add(pos+V(0,ys[ys.Length-1],0));for(int i=0;i<sides;i++){ix.AddRange(new[]{bottom,i,i+1,top,rows*(sides+1)+i+1,rows*(sides+1)+i});}MeshPart(n,p,m,v,ix);
        }
        void Curve(string n,Transform p,Vector3[] points,float radius,Material mat)
        {
            if(points.Length<3){for(int j=1;j<points.Length;j++)Rod(n,p,points[j-1],points[j],radius,mat);return;}
            int count=(points.Length-1)*10;const int sides=12;var v=new List<Vector3>();var ix=new List<int>();
            for(int j=0;j<=count;j++)
            {
                float t=j/(float)count*(points.Length-1);int seg=Mathf.Min(points.Length-2,(int)t);float u=t-seg;
                Vector3 a=points[Mathf.Max(0,seg-1)],b=points[seg],c=points[seg+1],d=points[Mathf.Min(points.Length-1,seg+2)];
                Vector3 q=.5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
                Vector3 tangent=(.5f*((-a+c)+2*(2*a-5*b+4*c-d)*u+3*(-a+3*b-3*c+d)*u*u)).normalized;
                Vector3 side=Vector3.Cross(tangent,Mathf.Abs(tangent.y)<.9f?Vector3.up:Vector3.forward).normalized,normal=Vector3.Cross(tangent,side);
                float taper=Mathf.Lerp(.68f,1,Mathf.Sin(j/(float)count*Mathf.PI));
                for(int i=0;i<=sides;i++){float angle=i*Mathf.PI*2/sides;v.Add(q+(side*Mathf.Cos(angle)+normal*Mathf.Sin(angle))*radius*taper);if(j<count&&i<sides){int k=j*(sides+1)+i;Quad(ix,k,k+sides+1,k+1,k+sides+2,true);}}
            }
            MeshPart(n,p,mat,v,ix);
        }
        void Rod(string n,Transform p,Vector3 start,Vector3 end,float radius,Material mat)
        {Taper(n,p,start,end,radius,radius,mat);}
        void Cone(string n,Transform p,Vector3 start,Vector3 end,float radius,Material mat)
        {Taper(n,p,start,end,radius,.001f,mat);}
        void Taper(string n,Transform p,Vector3 start,Vector3 end,float r0,float r1,Material mat)
        {
            Vector3 axis=(end-start).normalized,side=Vector3.Cross(axis,Mathf.Abs(axis.y)<.9f?Vector3.up:Vector3.right).normalized,up=Vector3.Cross(axis,side);
            var v=new List<Vector3>();var ix=new List<int>();const int N=20;
            for(int j=0;j<2;j++)for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N;v.Add((j==0?start:end)+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*(j==0?r0:r1));}
            for(int i=0;i<N;i++)Quad(ix,i,N+1+i,i+1,N+2+i,true);int b=v.Count;v.Add(start);int t=v.Count;v.Add(end);for(int i=0;i<N;i++)ix.AddRange(new[]{b,i+1,i,t,N+1+i,N+2+i});MeshPart(n,p,mat,v,ix);
        }
        void Ring(string n,Transform p,float y,float rx,float rz,float width,Material m,Vector3 offset=default)
        {
            var v=new List<Vector3>();var ix=new List<int>();const int N=32;
            for(int j=0;j<2;j++)for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N;v.Add(offset+V(Mathf.Sin(a)*rx,y+(j-.5f)*width,Mathf.Cos(a)*rz));}
            for(int i=0;i<N;i++)Quad(ix,i,N+1+i,i+1,N+2+i,true);MeshPart(n,p,m,v,ix,true);
        }
        void Rag(string n,Transform p,Vector3 pos,float width,float length,Material m,int seed)
        {
            var v=new List<Vector3>();var ix=new List<int>();const int R=16,C=12;
            float a=Mathf.Atan2(pos.x,pos.z);Vector3 tangent=V(Mathf.Cos(a),0,-Mathf.Sin(a)),normal=V(Mathf.Sin(a),0,Mathf.Cos(a));
            for(int j=0;j<=R;j++)for(int i=0;i<=C;i++){float t=j/(float)R,x=(i/(float)C-.5f)*width;float tip=j==R?.02f+Mathf.Abs(Mathf.Sin(i*3.2f+seed))*.075f:0;v.Add(pos+tangent*x+V(0,-t*length-tip,0)+normal*(Mathf.Sin(j*.45f+i*.8f+seed)*.011f+t*.015f));}
            for(int j=0;j<R;j++)for(int i=0;i<C;i++){if(j>10&&(i+j+seed)%23==0)continue;int k=j*(C+1)+i;Quad(ix,k,k+C+1,k+1,k+C+2,false);}MeshPart(n,p,m,v,ix,true);
        }
        void Plate(string n,Transform p,Vector3 pos,float rx,float ry,Material m)
        {
            if(n.ToLowerInvariant().Contains("shoulder")||n.Contains("pauldron")){var mount=Joint(n+" dome mount",p,pos);mount.localRotation=Quaternion.Euler(-65,0,pos.x>=0?-15:15);p=mount;pos=Vector3.zero;rx*=.87f;ry*=.85f;}
            // Forged curved shield with rolled perimeter and individually inset rivets.
            var v=new List<Vector3>{pos+V(0,0,.055f)};var ix=new List<int>();const int N=96,R=24;
            for(int j=1;j<=R;j++)for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N,r=j/(float)R;float edge=m==bone?1+.13f*Mathf.Sin(a*5)+.075f*Mathf.Sin(a*9):1+.045f*Mathf.Sin(a*7)+.025f*Mathf.Sin(a*13);v.Add(pos+V(Mathf.Cos(a)*rx*r*edge,Mathf.Sin(a)*ry*r*edge,.055f*Mathf.Sqrt(1-r*r*.9f)+.005f*Mathf.Sin(a*8+r*17)*r));}
            for(int i=0;i<N;i++)ix.AddRange(new[]{0,i+1,i+2});for(int j=0;j<R-1;j++)for(int i=0;i<N;i++){if(m==bone&&BoneAperture((j+1.5f)/R,(i+.5f)*Mathf.PI*2/N))continue;int a=1+j*(N+1)+i,b=a+N+1;Quad(ix,a,b,a+1,b+1,false);}SolidPlate(n,p,m,v,ix);
            if(m==bone)return;
            Vector3 last=pos+V(rx,0,.018f);for(int i=1;i<=N;i++){float a=i*Mathf.PI*2/N;Vector3 next=pos+V(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry,.018f);Rod("Rolled plate edge",p,last,next,.004f,leather);if(i%4==0)Ellipsoid("Plate rivet",p,next,V(.007f,.007f,.006f),bone,8,6);last=next;}
        }
        void Batch(Transform joint)
        {
            var children=new List<Transform>();for(int i=0;i<joint.childCount;i++)children.Add(joint.GetChild(i));
            var groups=new Dictionary<Material,List<MeshFilter>>();
            foreach(var c in children){var f=c.GetComponent<MeshFilter>();var r=c.GetComponent<MeshRenderer>();if(f&&r&&c.childCount==0){if(!groups.TryGetValue(r.sharedMaterial,out var list)){list=new List<MeshFilter>();groups.Add(r.sharedMaterial,list);}list.Add(f);}else Batch(c);}
            foreach(var pair in groups)
            {
                if(pair.Value.Count<2)continue;var instances=new CombineInstance[pair.Value.Count];int count=0;
                for(int j=0;j<instances.Length;j++){var f=pair.Value[j];count+=f.sharedMesh.vertexCount;instances[j]=new CombineInstance{mesh=f.sharedMesh,transform=joint.worldToLocalMatrix*f.transform.localToWorldMatrix};}
                var merged=new Mesh{name=joint.name+" / "+pair.Key.name};if(count>65535)merged.indexFormat=IndexFormat.UInt32;merged.CombineMeshes(instances,true,true);meshes.Add(merged);
                var node=Joint(merged.name,joint,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=merged;var mr=node.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterial=pair.Key;mr.shadowCastingMode=Application.isMobilePlatform&&kind!=3?ShadowCastingMode.Off:ShadowCastingMode.On;if(Application.isMobilePlatform)mr.receiveShadows=false;
                foreach(var f in pair.Value){meshes.Remove(f.sharedMesh);Destroy(f.sharedMesh);f.gameObject.SetActive(false);Destroy(f.gameObject);}
            }
        }
        void OnDestroy(){foreach(var m in meshes)if(m)Destroy(m);foreach(var m in materials)if(m)Destroy(m);}
    }
}










