using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Painted, jointed cutouts. Cached motion samples interpolate joint positions,
    // never image opacity: each limb and each item is drawn exactly once.
    [DefaultExecutionOrder(450)]
    public sealed class LayeredPlayer2D : MonoBehaviour
    {
        public enum Motion { Idle, Walk, Shoot, Run }
        public const int WalkSamples=64,IdleSamples=32,ShootSamples=32,RunSamples=64;
        public int DirectionCount=>8;
        public int CurrentDirection {get;private set;}
        public int MovementDirection {get;private set;}=3;
        public Motion CurrentMotion {get;private set;}
        public bool IsAiming=>aiming||shotTime>0||meleeTime>0;
        public bool Ready=>ApprovedPoseActive||body[0]!=null&&body[1]!=null&&body[2]!=null&&body[3]!=null&&body[4]!=null;
        public bool WeaponReady=>ApprovedPoseActive||gunView!=null&&gunView.sprite!=null;
        public bool ApprovedPoseActive=>approvedPose!=null&&approvedPose.Active;
        public ApprovedFieldkitPlayer2D ApprovedPoseRenderer=>approvedPose;
        public int AppliedArmorCount {get;private set;}
        public int WeaponModel {get;private set;}
        public int PoseRevision {get;private set;}
        public Vector3 VisualMuzzlePosition {get;private set;}
        public Vector3 VisualGripPosition {get;private set;}
        public Vector3 VisualSupportGripPosition {get;private set;}
        public Vector3 VisualStockPosition {get;private set;}
        public Vector3 StockContactTarget {get;private set;}
        public float StockContactError=>Vector3.ProjectOnPlane(VisualStockPosition-StockContactTarget,activeCamera!=null?activeCamera.transform.forward:Vector3.forward).magnitude;
        public bool StockOcclusionActive {get;private set;}
        // This painted rig always uses both hands, even when the weapon catalog
        // classifies the model as a pistol or compact SMG.
        public bool UsesTwoHandGrip=>true;
        public bool IsOneHanded=>!UsesTwoHandGrip;
        public bool IsCatalogOneHanded=>Arsenal.IsOneHanded(WeaponModel);
        public float WeaponAimBlend {get;private set;}
        public float RightArmReach {get;private set;}
        public float LeftArmReach {get;private set;}
        public bool RequiresShoulderStock=>!IsCatalogOneHanded&&WeaponAimBlend>.98f;
        public Vector3 CarryGripTarget {get;private set;}
        public bool RightWristBridgeActive=>rightWristBridge!=null&&rightWristBridge.enabled;
        public Vector3 RightHandPosition {get;private set;}
        public Vector3 LeftHandPosition {get;private set;}
        public Vector3 RightElbowPosition {get;private set;}
        public Vector3 LeftElbowPosition {get;private set;}
        public Vector3 RightShoulderPosition {get;private set;}
        public Vector3 LeftShoulderPosition {get;private set;}
        public Vector3 RightFootPosition {get;private set;}
        public Vector3 LeftFootPosition {get;private set;}
        public Vector3 RightKneePosition {get;private set;}
        public Vector3 LeftKneePosition {get;private set;}
        public Vector3 RightHipPosition {get;private set;}
        public Vector3 LeftHipPosition {get;private set;}
        public static float GaitCycleDistance(bool running)=>running?4.0f:2.6f;
        public SpriteRenderer WeaponRenderer=>gunView;
        public SpriteRenderer[] BodyRenderers=>views;
        public SpriteRenderer[] ArmorRenderers=>gearViews;
        public SpriteRenderer[] ArmorHandRenderers=>armoredHandViews;
        public SpriteRenderer[] ArmorUpperArmRenderers=>upperArmorViews;
        public int LoadedBodyDirections {get;private set;}
        public int LoadedArmorDirections {get;private set;}
        public float WalkPhase=>walkPhase;
        public float RaisedBlend=>raised;
        public bool ReviewOverride {get;private set;}
        // Renderer/joint diagnostics are enabled only by the explicit QA review hook.
        public bool ReviewRigDebug {get;private set;}
        static readonly string[] directions={"Front","FrontRight","Right","BackRight","Back"};
        static readonly int[] mirrorParts={0,1,2,6,7,8,3,4,5,12,13,14,9,10,11};
        static readonly int[] mirrorGear={0,1,3,2,5,4,7,6};
        // Side-view painted hull size. The side cell supplies the physical stock span.
        static readonly float[] modelLengths={.60f,.56f,.72f,.60f,.60f,.57f,.70f,.63f,.85f,.87f,.61f,.69f,.64f,.55f,.36f,.38f,.46f,.35f};
        public static int WeaponModelCount=>Arsenal.ModelNames.Length;
        public static bool ExtraArmorReady=>Resources.Load<TextAsset>("Art/LayeredPlayer/TacticalArt/ArmorVariantsReady")!=null;
        public static int ArmorStyleCount=>ExtraArmorReady?4:2;
        public static string ArmorSourcePath(string direction,int variant)=>"Art/LayeredPlayer/"+(ExtraArmorReady&&variant%4>=2?"TacticalArt/ArmorV5_":"ArmorV2_")+direction;
        public static bool TacticalArtReady=>Resources.Load<TextAsset>("Art/LayeredPlayer/TacticalArt/Ready")!=null;
        public static bool InkedArtReady=>Resources.Load<TextAsset>("Art/LayeredPlayer/InkedArt/Ready")!=null;
        public static bool HandgunsReady=>Resources.Load<TextAsset>("Art/LayeredPlayer/HandgunArt/HandgunsReady")!=null;
        public static Color WeaponDisplayTint(int model,Color rarity)=>Color.white;
        public static bool MulticolorArtReady=>Resources.Load<TextAsset>("Art/LayeredPlayer/ColorArt/PaletteReady")!=null;
        public static string ArtMetadataPath(string name)=>"Art/LayeredPlayer/"+(MulticolorArtReady?"ColorArt/":"")+name;
        static readonly string[] names={"head","torso","pelvis","right upper arm","right forearm","right hand","left upper arm","left forearm","left hand","right thigh","right shin","right boot","left thigh","left shin","left boot"};
        readonly Sprite[][] body=new Sprite[5][],armor=new Sprite[5][],extraArmor=new Sprite[5][];
        readonly SpriteRenderer[] views=new SpriteRenderer[15],gearViews=new SpriteRenderer[8];
        readonly SpriteRenderer[] armoredHandViews=new SpriteRenderer[2];
        readonly SpriteRenderer[] upperArmorViews=new SpriteRenderer[2];
        readonly SpriteRenderer[] rigDebugMarkers=new SpriteRenderer[10];
        static readonly Dictionary<Sprite,Sprite> armoredHandSprites=new Dictionary<Sprite,Sprite>();
        public bool HasArmoredGloves=>equipped[2]!=null;
        readonly ArmorItem[] equipped=new ArmorItem[5];
        SpriteRenderer gunView,groundShadow,rightWristBridge,rightBracerBridge;Sprite[] gunSprites;AimedWeapon weapon;
        MaterialPropertyBlock wristBridgeBlock,bracerBridgeBlock;
        static Sprite shadowSprite;
        SortingGroup sortingGroup;
        Vector3 previous;Vector2 travel=new Vector2(.7071f,.4101f);
        float walkPhase,idlePhase,shotTime,raised,movingSpeed,meleeTime;bool aiming,initialized,locomoting;
        int reviewDirection;int reviewMovementDirection=-1;float reviewPhase;Motion reviewMotion;bool reviewAim;
        Color weaponColor=Color.white;
        static Material material;static Sprite rigMarkerSprite;
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        // Efficient cache: 8 directions x 128 articulated motion samples, about 32 KB.
        static Pose[][][] poses;
        struct Pose { public float stride,liftR,liftL,bob,breath,kick,sway,twist; }
        [Serializable] public class WeaponAnchorTable { public WeaponAnchor[] anchors; }
        [Serializable] public class WeaponAnchor { public int model,direction;public Vector2 grip,muzzle,support,stock; }
        WeaponAnchorTable anchors,modernAnchors,handgunAnchors;
        [Serializable] public class BodyRegionTable { public BodyRegion[] regions; }
        [Serializable] public class BodyRegion { public int direction,cell; public float x,y,width,height; }
        static BodyRegionTable bodyRegions;
        static BodyRegionTable inkedBodyRegions,inkedArmorRegions;
        static BodyRegionTable armorV2Regions;
        static BodyRegionTable armorV3Regions;
        static BodyRegionTable armorV4Regions;
        static BodyRegionTable armorV5Regions;
        [Serializable] public class WeaponRegionTable { public WeaponRegion[] regions; }
        [Serializable] public class WeaponRegion { public int model,direction; public float x,y,width,height;public Vector2[] hull; }
        static WeaponRegionTable weaponRegions;
        static WeaponRegionTable modernWeaponRegions;
        static WeaponRegionTable handgunWeaponRegions;
        static readonly Dictionary<Sprite,Vector2> cellOrigins=new Dictionary<Sprite,Vector2>();
        static readonly Dictionary<Sprite,Vector2[]> partJoints=new Dictionary<Sprite,Vector2[]>();
        static readonly Dictionary<(Sprite,float,float),MaterialPropertyBlock> capClips=new Dictionary<(Sprite,float,float),MaterialPropertyBlock>();
        static readonly Dictionary<(Sprite,float),Vector2> armorSockets=new Dictionary<(Sprite,float),Vector2>();
        static readonly Dictionary<Texture2D,Color32[]> armorPixels=new Dictionary<Texture2D,Color32[]>();
        MaterialPropertyBlock torsoCutBlock;
        MaterialPropertyBlock weaponClipBlock;
        readonly Vector4[] weaponClipPoints=new Vector4[64];Sprite clippedWeapon;
        MeshRenderer[] hiddenMeshes;
        ApprovedFieldkitPlayer2D approvedPose;
        string weaponVisualSet=string.Empty;
        public string WeaponVisualSet=>weaponVisualSet;
        public void SetWeaponVisualSet(string value){weaponVisualSet=value??string.Empty;if(initialized)RefreshPose();}

        public void Init()
        {
            if(initialized)return;initialized=true;previous=transform.position;weaponClipBlock=new MaterialPropertyBlock();wristBridgeBlock=new MaterialPropertyBlock();bracerBridgeBlock=new MaterialPropertyBlock();torsoCutBlock=new MaterialPropertyBlock();sortingGroup=GetComponent<SortingGroup>();if(sortingGroup==null)sortingGroup=gameObject.AddComponent<SortingGroup>();
            var json=Resources.Load<TextAsset>(ArtMetadataPath("WeaponAnchors"));if(json!=null)anchors=JsonUtility.FromJson<WeaponAnchorTable>(json.text);
            if(TacticalArtReady){json=Resources.Load<TextAsset>(InkedArtReady?"Art/LayeredPlayer/InkedArt/WeaponAnchors":"Art/LayeredPlayer/TacticalArt/ModernWeaponAnchors");if(json!=null)modernAnchors=JsonUtility.FromJson<WeaponAnchorTable>(json.text);}
            if(HandgunsReady){json=Resources.Load<TextAsset>("Art/LayeredPlayer/HandgunArt/HandgunWeaponAnchors");if(json!=null)handgunAnchors=JsonUtility.FromJson<WeaponAnchorTable>(json.text);}
            BuildPoseCache();
            for(int d=0;d<5;d++)
            {
                body[d]=LoadCells("Art/LayeredPlayer/Body_"+directions[d],true);if(body[d]!=null)LoadedBodyDirections++;
                armor[d]=LoadCells("Art/LayeredPlayer/ArmorV2_"+directions[d],true)??LoadCells("Art/LayeredPlayer/Armor_"+directions[d],true);if(armor[d]!=null)LoadedArmorDirections++;
                if(ExtraArmorReady)extraArmor[d]=LoadCells(ArmorSourcePath(directions[d],2),true);
            }
            for(int i=0;i<15;i++)views[i]=Create("Painted body / "+names[i]);
            for(int i=0;i<8;i++)gearViews[i]=Create("Independent armor / "+i);
            for(int i=0;i<2;i++)armoredHandViews[i]=Create("Independent armor glove / "+i);
            for(int i=0;i<2;i++)upperArmorViews[i]=Create("Articulated chest shoulder / "+i);
            for(int i=0;i<rigDebugMarkers.Length;i++)rigDebugMarkers[i]=Create("QA rig landmark / "+i);
            gunView=Create("Independent painted firearm");
            rightWristBridge=Create("Right distal wrist emerging from torso");rightBracerBridge=Create("Right distal bracer emerging from torso");
            groundShadow=Create("Ground contact shadow");
            if(shadowSprite==null)
            {
                var texture=new Texture2D(48,48,TextureFormat.RGBA32,false);var colors=new Color[48*48];
                for(int y=0;y<48;y++)for(int x=0;x<48;x++){float radius=new Vector2((x-23.5f)/23.5f,(y-23.5f)/23.5f).magnitude;colors[y*48+x]=new Color(0,0,0,Mathf.Pow(Mathf.Clamp01(1-radius),1.7f)*.42f);}
                texture.SetPixels(colors);texture.Apply(false);shadowSprite=Sprite.Create(texture,new Rect(0,0,48,48),new Vector2(.5f,.5f),48);
            }
            if(rigMarkerSprite==null)
            {
                const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.name="QA-only high-contrast rig landmark";texture.filterMode=FilterMode.Point;
                var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){int dx=x-size/2,dy=y-size/2;bool ring=dx*dx+dy*dy>=18*18&&dx*dx+dy*dy<=25*25;bool cross=(Mathf.Abs(dx)<=2&&Mathf.Abs(dy)<=13)||(Mathf.Abs(dy)<=2&&Mathf.Abs(dx)<=13);pixels[y*size+x]=ring||cross?Color.white:Color.clear;}
                texture.SetPixels(pixels);texture.Apply(false);rigMarkerSprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),64);
            }
            groundShadow.sprite=shadowSprite;groundShadow.sortingOrder=1;
            var old=GetComponent<PaintedActor>();if(old!=null){old.enabled=false;foreach(var r in old.GetComponentsInChildren<SpriteRenderer>())if(r!=null&&Array.IndexOf(views,r)<0&&Array.IndexOf(gearViews,r)<0&&r!=gunView)r.enabled=false;}
            SetWeaponModel(0,Color.white);RefreshPose();
        }
        SpriteRenderer Create(string name)
        {
            if(material==null){var shader=Resources.Load<Shader>("LayeredPlayerSprite");if(shader==null)shader=Shader.Find("Sprites/Default");material=new Material(shader){name="Shared layered player paint"};}
            var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;
        }
        public static Sprite[] LoadCells(string path,bool topPivot)
        {
            string paintedPath=path;
            if(MulticolorArtReady){
                if(path.Contains("/ArmorV2_"))paintedPath=path.Replace("/ArmorV2_","/ColorArt/ArmorV3_");
                if(path.Contains("/Weapons_"))paintedPath=path.Replace("/Weapons_","/ColorArt/WeaponsV3_");
            }
            if(TacticalArtReady){if(path.Contains("/ArmorV2_"))paintedPath=path.Replace("/ArmorV2_","/TacticalArt/ArmorV4_");if(path.EndsWith("/Weapons_6",StringComparison.Ordinal))paintedPath=path.Replace("/Weapons_6","/TacticalArt/WeaponsModern_6");}
            if(HandgunsReady&&(path.EndsWith("/Weapons_7",StringComparison.Ordinal)||path.EndsWith("/Weapons_8",StringComparison.Ordinal)))paintedPath=path.Replace("/Weapons_","/HandgunArt/Handguns_");
            if(InkedArtReady)
            {
                if(path.EndsWith("/Weapons_6",StringComparison.Ordinal))paintedPath=Resources.Load<Texture2D>("Art/LayeredPlayer/InkedArt/Menu_Weapons_6")!=null?path.Replace("/Weapons_6","/InkedArt/Menu_Weapons_6"):Resources.Load<Texture2D>("Art/LayeredPlayer/InkedArt/Fieldkit_Weapons_6")!=null?path.Replace("/Weapons_6","/InkedArt/Fieldkit_Weapons_6"):path.Replace("/Weapons_6","/InkedArt/Weapons_6");
                if(path.Contains("/Body_"))paintedPath=path.Replace("/Body_","/InkedArt/Body_");
                if(path.Contains("/ArmorV2_"))paintedPath=path.Replace("/ArmorV2_","/InkedArt/Armor_");
            }
            string key=paintedPath+(topPivot?"/top":"/center");if(cache.TryGetValue(key,out var existing))return existing;
            var tex=Resources.Load<Texture2D>(paintedPath);if(tex==null)return null;
            if(bodyRegions==null){var regionsJson=Resources.Load<TextAsset>("Art/LayeredPlayer/BodyRegions");if(regionsJson!=null)bodyRegions=JsonUtility.FromJson<BodyRegionTable>(regionsJson.text);}
            if(InkedArtReady&&inkedBodyRegions==null){var json=Resources.Load<TextAsset>("Art/LayeredPlayer/InkedArt/BodyRegions");if(json!=null)inkedBodyRegions=JsonUtility.FromJson<BodyRegionTable>(json.text);}
            if(InkedArtReady&&inkedArmorRegions==null){var json=Resources.Load<TextAsset>("Art/LayeredPlayer/InkedArt/ArmorRegions");if(json!=null)inkedArmorRegions=JsonUtility.FromJson<BodyRegionTable>(json.text);}
            if(armorV2Regions==null){var regionsJson=Resources.Load<TextAsset>("Art/LayeredPlayer/ArmorV2Regions");if(regionsJson!=null)armorV2Regions=JsonUtility.FromJson<BodyRegionTable>(regionsJson.text);}
            if(weaponRegions==null){var regionsJson=Resources.Load<TextAsset>(ArtMetadataPath("WeaponRegions"));if(regionsJson!=null)weaponRegions=JsonUtility.FromJson<WeaponRegionTable>(regionsJson.text);}
            if(MulticolorArtReady&&armorV3Regions==null){var regionsJson=Resources.Load<TextAsset>("Art/LayeredPlayer/ColorArt/ArmorV3Regions");if(regionsJson!=null)armorV3Regions=JsonUtility.FromJson<BodyRegionTable>(regionsJson.text);}
            if(TacticalArtReady&&armorV4Regions==null){var json=Resources.Load<TextAsset>("Art/LayeredPlayer/TacticalArt/ArmorV4Regions");if(json!=null)armorV4Regions=JsonUtility.FromJson<BodyRegionTable>(json.text);}
            if(TacticalArtReady&&modernWeaponRegions==null){var json=Resources.Load<TextAsset>(InkedArtReady?"Art/LayeredPlayer/InkedArt/WeaponRegions":"Art/LayeredPlayer/TacticalArt/ModernWeaponRegions");if(json!=null)modernWeaponRegions=JsonUtility.FromJson<WeaponRegionTable>(json.text);}
            if(HandgunsReady&&handgunWeaponRegions==null){var json=Resources.Load<TextAsset>("Art/LayeredPlayer/HandgunArt/HandgunWeaponRegions");if(json!=null)handgunWeaponRegions=JsonUtility.FromJson<WeaponRegionTable>(json.text);}
            if(path.Contains("ArmorV5_")&&armorV5Regions==null){var json=Resources.Load<TextAsset>("Art/LayeredPlayer/TacticalArt/ArmorV5Regions");if(json!=null)armorV5Regions=JsonUtility.FromJson<BodyRegionTable>(json.text);}
            var activeBodyRegions=InkedArtReady?inkedBodyRegions:bodyRegions;
            var activeArmorRegions=path.Contains("ArmorV5_")?armorV5Regions:InkedArtReady?inkedArmorRegions:TacticalArtReady?armorV4Regions:MulticolorArtReady?armorV3Regions:armorV2Regions;
            var activeWeaponRegions=path.EndsWith("/Weapons_7",StringComparison.Ordinal)||path.EndsWith("/Weapons_8",StringComparison.Ordinal)?handgunWeaponRegions:path.EndsWith("/Weapons_6",StringComparison.Ordinal)?modernWeaponRegions:weaponRegions;
            var result=new Sprite[16];Color32[] pixels=tex.isReadable?tex.GetPixels32():null;
            for(int cell=0;cell<16;cell++)
            {
                int x=Mathf.RoundToInt(cell%4*tex.width/4f),y=Mathf.RoundToInt((3-cell/4)*tex.height/4f),w=Mathf.RoundToInt((cell%4+1)*tex.width/4f)-x,h=Mathf.RoundToInt((4-cell/4)*tex.height/4f)-y;
                if(path.Contains("Body_")&&activeBodyRegions?.regions!=null){int direction=Array.IndexOf(directions,path.Substring(path.LastIndexOf("Body_",StringComparison.Ordinal)+5));foreach(var region in activeBodyRegions.regions)if(region.direction==direction&&region.cell==cell){x=Mathf.Clamp(Mathf.FloorToInt(region.x*tex.width),0,tex.width-1);y=Mathf.Clamp(Mathf.FloorToInt((1-region.y-region.height)*tex.height),0,tex.height-1);w=Mathf.Clamp(Mathf.CeilToInt(region.width*tex.width),1,tex.width-x);h=Mathf.Clamp(Mathf.CeilToInt(region.height*tex.height),1,tex.height-y);break;}}
                if((path.Contains("ArmorV2_")||path.Contains("ArmorV5_"))&&activeArmorRegions?.regions!=null){int direction=Array.IndexOf(directions,path.Substring(path.LastIndexOf("ArmorV",StringComparison.Ordinal)+8));foreach(var region in activeArmorRegions.regions)if(region.direction==direction&&region.cell==cell){x=Mathf.Clamp(Mathf.FloorToInt(region.x*tex.width),0,tex.width-1);y=Mathf.Clamp(Mathf.FloorToInt((1-region.y-region.height)*tex.height),0,tex.height-1);w=Mathf.Clamp(Mathf.CeilToInt(region.width*tex.width),1,tex.width-x);h=Mathf.Clamp(Mathf.CeilToInt(region.height*tex.height),1,tex.height-y);break;}}
                Vector2 originalCell=new Vector2(Mathf.RoundToInt(cell%4*tex.width/4f),Mathf.RoundToInt((3-cell/4)*tex.height/4f));
                if(path.Contains("Weapons_")&&activeWeaponRegions?.regions!=null){int sheet=int.Parse(path.Substring(path.LastIndexOf("Weapons_",StringComparison.Ordinal)+8));foreach(var region in activeWeaponRegions.regions)if(region.model==sheet*2+cell/8&&region.direction==cell%8){x=Mathf.Clamp(Mathf.FloorToInt(region.x*tex.width),0,tex.width-1);y=Mathf.Clamp(Mathf.FloorToInt((1-region.y-region.height)*tex.height),0,tex.height-1);w=Mathf.Clamp(Mathf.CeilToInt(region.width*tex.width),1,tex.width-x);h=Mathf.Clamp(Mathf.CeilToInt(region.height*tex.height),1,tex.height-y);break;}}
                int minX=w,minY=h,maxX=-1,maxY=-1;
                if(pixels!=null){for(int py=1;py<h-1;py++)for(int px=1;px<w-1;px++)if(pixels[(y+py)*tex.width+x+px].a>24){minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py);}}
                else {minX=0;minY=0;maxX=w-1;maxY=h-1;}
                if(maxX<minX)continue;
                var rect=new Rect(x+minX,y+minY,maxX-minX+1,maxY-minY+1);
                result[cell]=Sprite.Create(tex,rect,new Vector2(.5f,topPivot?1:.5f),100,0,SpriteMeshType.FullRect);result[cell].name=path+" / "+cell;cellOrigins[result[cell]]=originalCell;
            }
                cache[key]=result;return result;
        }
        public void ApplyArmor(ArmorItem[] items)
        {
            AppliedArmorCount=0;Array.Clear(equipped,0,5);
            if(items!=null)foreach(var item in items)if(item!=null&&(int)item.slot<5){equipped[(int)item.slot]=item;AppliedArmorCount++;}
            if(initialized)RefreshPose();
        }
        public void SetWeaponModel(int model,Color color)
        {
            WeaponModel=Mathf.Clamp(model,0,WeaponModelCount-1);weaponColor=color;hiddenMeshes=null;
            gunSprites=LoadCells("Art/LayeredPlayer/Weapons_"+(WeaponModel/2),false);
            if(initialized&&gunView!=null)RefreshPose();
        }
        public void SetAiming(bool value){aiming=value;if(initialized)RefreshPose();}
        public void Kick(){shotTime=.20f;CurrentMotion=Motion.Shoot;if(initialized)RefreshPose();}
        public void MeleeSwing(){meleeTime=.24f;if(initialized)RefreshPose();}
        public void SetReviewPose(int direction,Motion motion,float phase,bool aim,bool rigDebug=false)
        {ReviewOverride=true;ReviewRigDebug=rigDebug;reviewDirection=((direction%8)+8)%8;reviewMotion=motion;reviewPhase=Mathf.Repeat(phase,1);reviewAim=aim;raised=aim||motion==Motion.Shoot?1:0;RefreshPose();}
        public void SetRigDebug(bool value){ReviewRigDebug=value;RefreshPose();}
        public void SetReviewMovementDirection(int direction){reviewMovementDirection=((direction%8)+8)%8;if(ReviewOverride)RefreshPose();}
        public void ClearReviewPose(){ReviewOverride=false;ReviewRigDebug=false;reviewMovementDirection=-1;previous=transform.position;}
        public static Vector3 DirectionVector(Camera camera,int direction)
        {float a=direction*Mathf.PI/4;var down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;return (camera.transform.right*Mathf.Sin(a)+down*Mathf.Cos(a)).normalized;}
        static int Facing(Camera camera,Vector3 facing)
        {var down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;return Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(Vector3.Dot(facing,camera.transform.right),Vector3.Dot(facing,down))*Mathf.Rad2Deg,360)/45)%8;}
        static void BuildPoseCache()
        {
            if(poses!=null)return;poses=new Pose[8][][];
            for(int d=0;d<8;d++)
            {
                poses[d]=new Pose[4][];
                for(int state=0;state<4;state++)
                {
                    bool moving=state==1||state==3;int count=moving?64:32;float intensity=state==3?1.45f:1;
                    poses[d][state]=new Pose[count];
                    for(int f=0;f<count;f++)
                    {
                        float p=f/(float)count,a=p*Mathf.PI*2;
                        poses[d][state][f]=new Pose{stride=moving?Mathf.Sin(a)*intensity:0,liftR=moving?Mathf.Pow(Mathf.Max(0,Mathf.Cos(a)),1.5f)*intensity:0,liftL=moving?Mathf.Pow(Mathf.Max(0,-Mathf.Cos(a)),1.5f)*intensity:0,bob=moving?Mathf.Cos(a*2)*.032f*intensity:Mathf.Sin(a)*.007f,breath=Mathf.Sin(a)*.006f,sway=Mathf.Cos(a)*(moving?.023f:.011f),twist=moving?Mathf.Sin(a)*intensity:0,kick=state==2?Mathf.Exp(-p*9)*.075f:0};
                    }
                }
            }
        }
        static Pose Sample(int direction,Motion state,float phase)
        {
            var samples=poses[direction][(int)state];float f=Mathf.Repeat(phase,1)*samples.Length;int a=Mathf.FloorToInt(f),b=(a+1)%samples.Length;float t=f-a;
            Pose x=samples[a],y=samples[b];return new Pose{stride=Mathf.Lerp(x.stride,y.stride,t),liftR=Mathf.Lerp(x.liftR,y.liftR,t),liftL=Mathf.Lerp(x.liftL,y.liftL,t),bob=Mathf.Lerp(x.bob,y.bob,t),breath=Mathf.Lerp(x.breath,y.breath,t),sway=Mathf.Lerp(x.sway,y.sway,t),kick=Mathf.Lerp(x.kick,y.kick,t),twist=Mathf.Lerp(x.twist,y.twist,t)};
        }
        void LateUpdate()
        {
            if(!initialized)Init();if(ReviewOverride){RefreshPose();return;}
            var camera=Camera.main;if(camera==null)return;
            Vector3 delta=transform.position-previous;previous=transform.position;delta.y=0;
            float distance=delta.magnitude;locomoting=distance>.00001f;movingSpeed=Mathf.Lerp(movingSpeed,distance/Mathf.Max(.001f,Time.deltaTime),1-Mathf.Exp(-Time.deltaTime*15));
            if(distance>.00001f){MovementDirection=Facing(camera,delta);travel=new Vector2(Vector3.Dot(delta.normalized,camera.transform.right),Vector3.Dot(delta.normalized,camera.transform.up));walkPhase=Mathf.Repeat(walkPhase+distance/GaitCycleDistance(movingSpeed>3.5f),1);CurrentMotion=movingSpeed>3.5f?Motion.Run:Motion.Walk;}
            else {CurrentMotion=Motion.Idle;idlePhase=Mathf.Repeat(idlePhase+Time.deltaTime*.65f,1);}
            shotTime=Mathf.Max(0,shotTime-Time.deltaTime);meleeTime=Mathf.Max(0,meleeTime-Time.deltaTime);if(shotTime>0)CurrentMotion=Motion.Shoot;
            raised=Mathf.MoveTowards(raised,IsAiming?1:0,Time.deltaTime*7);RefreshPose();
        }
        Vector3 origin;Camera activeCamera;
        float rightArmDepth,leftArmDepth;
        Vector3 World(Vector2 p,float depth=0)=>origin+activeCamera.transform.right*p.x+activeCamera.transform.up*p.y+activeCamera.transform.forward*depth;
        public void RefreshPose()
        {
            if(!initialized||Camera.main==null)return;activeCamera=Camera.main;
            if(sortingGroup==null)sortingGroup=GetComponent<SortingGroup>();if(sortingGroup==null)sortingGroup=gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder=Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(transform.position,activeCamera.transform.up)*100),-30000,19000);
            if(weapon==null)weapon=GetComponentInChildren<AimedWeapon>();
            CurrentDirection=ReviewOverride?reviewDirection:IsAiming?Facing(activeCamera,weapon!=null?weapon.Direction:transform.forward):locomoting?MovementDirection:CurrentDirection;
            if(ReviewOverride)CurrentMotion=reviewMotion;
            if(approvedPose==null)approvedPose=gameObject.AddComponent<ApprovedFieldkitPlayer2D>();
            if(approvedPose.Matches(equipped,weaponVisualSet)&&RenderApprovedPose())return;
            approvedPose.Hide();
            int d=CurrentDirection,source=d<=4?d:8-d;bool mirror=d>4;
            var parts=body[source];if(parts==null){foreach(var r in views)r.enabled=false;foreach(var r in gearViews)r.enabled=false;gunView.enabled=false;return;}
            float phase=ReviewOverride?reviewPhase:CurrentMotion==Motion.Walk||CurrentMotion==Motion.Run?walkPhase:CurrentMotion==Motion.Shoot?1-shotTime/.2f:idlePhase;
            Pose p=Sample(d,CurrentMotion,phase);
            if(!ReviewOverride&&locomoting&&CurrentMotion==Motion.Shoot){float kick=p.kick;p=Sample(d,movingSpeed>3.5f?Motion.Run:Motion.Walk,walkPhase);p.kick=kick;}
            // Feet live on the ground; the painted image plane uses the camera's basis.
            origin=transform.position-Vector3.up*.65f-activeCamera.transform.forward*.20f;
            if(groundShadow!=null){groundShadow.transform.SetPositionAndRotation(transform.position-Vector3.up*.635f,Quaternion.Euler(90,0,0));var parentScale=transform.lossyScale;groundShadow.transform.localScale=new Vector3(.82f/parentScale.x,.43f/parentScale.y,1);}
            float angle=d*Mathf.PI/4;if(!ReviewOverride&&IsAiming&&weapon!=null){var down=-Vector3.ProjectOnPlane(activeCamera.transform.up,Vector3.up).normalized;angle=Mathf.Atan2(Vector3.Dot(weapon.Direction,activeCamera.transform.right),Vector3.Dot(weapon.Direction,down));}
            float width=.72f+.28f*Mathf.Abs(Mathf.Cos(angle));
            float side=Mathf.Sin(angle),near=side>=0?1:-1;
            Vector2 anatomicalRight=new Vector2(-Mathf.Cos(angle),-Mathf.Sin(angle)*.58f);
            float aimBlend=ReviewOverride?(reviewAim||reviewMotion==Motion.Shoot?1:0):meleeTime>0?Mathf.Max(raised,.55f):raised;
            bool twoHandCarry=aimBlend<.5f;bool oneHandedPose=false;
            WeaponAimBlend=aimBlend;
            bool moving=ReviewOverride?(CurrentMotion==Motion.Walk||CurrentMotion==Motion.Run):locomoting;
            bool running=ReviewOverride?CurrentMotion==Motion.Run:movingSpeed>3.5f;
            Vector2 forward=new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle)*.58f);
            Vector2 move=ReviewOverride?forward:travel;
            float lean=moving?(running?.34f:.24f):.10f;
            float flightPhase=Mathf.Clamp01((Mathf.Repeat(ReviewOverride?reviewPhase:walkPhase,.5f)-(running?.38f:.46f))/(running?.12f:.04f));
            float flight=moving?Mathf.Sin(flightPhase*Mathf.PI)*(running?.095f:.025f):0;
            Vector2 hip=new Vector2(p.sway*.6f,1.01f+p.bob*.45f+flight)+(moving?Vector2.zero:anatomicalRight*.025f);
            Vector2 neck=hip+new Vector2(0,.72f+p.breath)+move*lean-anatomicalRight*p.twist*.025f;
            Vector2 shoulderCenter=neck+new Vector2(-side*Mathf.Abs(side)*.06f,-.23f)-forward*p.kick;
            Vector2 shoulderR=shoulderCenter+anatomicalRight*.265f-Vector2.up*p.twist*.016f,shoulderL=shoulderCenter-anatomicalRight*.265f+Vector2.up*p.twist*.016f;
            // Separate travel from aim permits backpedalling and strafing without rotating the legs toward the gun.
            float gaitPhase=ReviewOverride?reviewPhase:walkPhase;
            float rightCycle=Mathf.Repeat(gaitPhase,1),leftCycle=Mathf.Repeat(gaitPhase+.5f,1);
            Vector2 legR=hip+anatomicalRight*.14f-Vector2.up*.11f,legL=hip-anatomicalRight*.14f-Vector2.up*.11f;
            SolveLeg(legR,move,rightCycle,moving,running,-.045f,out var kneeR,out var footR,out var swingR);
            SolveLeg(legL,move,leftCycle,moving,running,.045f,out var kneeL,out var footL,out var swingL);
            // Feet plant under the shoulder line; retain the pelvis sockets and let
            // the knees follow the spread instead of widening the whole thigh.
            if(!moving)
            {
                Vector2 plantedSpread=anatomicalRight*.23f;
                footR+=plantedSpread;footL-=plantedSpread;
                kneeR+=plantedSpread*.52f;kneeL-=plantedSpread*.52f;
            }
            float heelR=Mathf.Sin(swingR*Mathf.PI),heelL=Mathf.Sin(swingL*Mathf.PI);
            RightFootPosition=World(footR);LeftFootPosition=World(footL);
            RightKneePosition=World(kneeR);LeftKneePosition=World(kneeL);RightHipPosition=World(legR);LeftHipPosition=World(legL);
            Vector3 shotDirection=ReviewOverride||weapon==null?DirectionVector(activeCamera,d):weapon.Direction;
            Vector2 barrel=new Vector2(Vector3.Dot(shotDirection,activeCamera.transform.right),Vector3.Dot(shotDirection,activeCamera.transform.up));
            Vector2 gunAxis=barrel.normalized;
            float lowerAngle=-24*Mathf.Deg2Rad*(1-aimBlend);float c=Mathf.Cos(lowerAngle),s=Mathf.Sin(lowerAngle);
            if(Mathf.Abs(gunAxis.x)>.2f)gunAxis=new Vector2(gunAxis.x*c-gunAxis.y*s*Mathf.Sign(gunAxis.x),gunAxis.x*s*Mathf.Sign(gunAxis.x)+gunAxis.y*c);
            else gunAxis=(barrel+Vector2.down*.35f*(1-aimBlend)).normalized;
            int gunDirection=d;
            Vector2 gunProjection=barrel;
            if(!oneHandedPose)
            {
                // Across-body low ready is a different gun orientation from aiming.
                Vector2 carryProjection=-anatomicalRight;
                Vector2 carryAxis=new Vector2(carryProjection.x,carryProjection.y*.38f-.42f).normalized;
                int carryDirection=(d+2)%8;
                if(Mathf.Abs(carryProjection.x)<.15f&&carryProjection.y>0){carryAxis=(forward*.35f-Vector2.up*.45f).normalized;carryProjection=new Vector2(.7f,-.41f);carryDirection=1;}
                float axisBlend=!ReviewOverride&&shotTime>0?1:aimBlend;
                gunAxis=Vector2.Lerp(carryAxis,barrel.normalized,axisBlend).normalized;
                gunProjection=Vector2.Lerp(carryProjection,barrel,axisBlend);
                float physicalProjection=Mathf.Sqrt(gunProjection.x*gunProjection.x+gunProjection.y*gunProjection.y/(.5773503f*.5773503f));
                gunProjection/=Mathf.Max(.001f,physicalProjection);
                gunDirection=Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(gunProjection.x,-gunProjection.y/.5773503f)*Mathf.Rad2Deg,360)/45)%8;
            }
            // Both carries stay below the chest; firing braces the elbows without raising the stock to the face.
            // Low ready: compact across-body carry with both hands close to the torso.
            Vector2 carryGrip=hip+Vector2.up*.13f+anatomicalRight*.20f+move*(lean*.08f);
            Vector2 carrySupport=carryGrip+gunAxis*.23f;
            Vector2 aimGrip=hip+Vector2.up*.34f+anatomicalRight*.08f+move*(lean*.08f);
            Vector2 grip=Vector2.Lerp(carryGrip,aimGrip,aimBlend)-gunAxis*p.kick;
            Vector2 support=Vector2.Lerp(carrySupport,grip+gunAxis*.23f,aimBlend);
            if(IsCatalogOneHanded)
            {
                Vector2 extendedGrip=shoulderCenter+barrel*.46f+Vector2.up*.08f;
                grip=Vector2.Lerp(carryGrip,extendedGrip,aimBlend)-gunAxis*p.kick;
                support=grip-anatomicalRight*.055f;
            }
            if(oneHandedPose)
            {
                Vector2 lowHand=hip+anatomicalRight*.27f+Vector2.up*.07f+move*(lean*.15f);
                Vector2 pistolCarry=new Vector2(-anatomicalRight.x,-Mathf.Abs(anatomicalRight.x)*.55f-.12f);
                gunAxis=Vector2.Lerp(pistolCarry.normalized,barrel.normalized,aimBlend).normalized;
                gunProjection=Vector2.Lerp(pistolCarry,barrel,aimBlend);
                gunDirection=aimBlend<.5f?(d+1)%8:d;
                // Ground-plane forward depth is foreshortened in quarter view. Normalizing
                // this vector turned front-facing aim into a full downward arm extension.
                Vector2 fireHand=shoulderR+barrel*.52f+Vector2.up*.055f;
                grip=Vector2.Lerp(lowHand,fireHand,aimBlend)-gunAxis*p.kick;
            }
            if(!ReviewOverride&&meleeTime>0)
            {
                float swing=Mathf.Sin(Mathf.Clamp01(1-meleeTime/.24f)*Mathf.PI);
                Vector2 strike=hip+forward*(.22f+.48f*swing)+Vector2.up*(.36f+.20f*swing);
                grip=Vector2.Lerp(grip,strike,Mathf.Clamp01(swing*.9f));
            }
            CarryGripTarget=World(grip);
            // When the character faces away, hide the weapon and both hands behind
            // the torso together; showing only the gun over the back reads as a spare arm.
            bool rearAim=d>=3&&d<=5;
            int gunOrder=rearAim?88:IsCatalogOneHanded&&WeaponModel==14?122:120;
            UpdateGun(gunDirection,grip,support,gunAxis,gunProjection,shoulderR,hip,neck,anatomicalRight,gunOrder,aimBlend,p.kick);
            if(WeaponReady)
            {
                Vector3 gripDelta=VisualGripPosition-origin,supportDelta=VisualSupportGripPosition-origin;
                grip=new Vector2(Vector3.Dot(gripDelta,activeCamera.transform.right),Vector3.Dot(gripDelta,activeCamera.transform.up));
                support=new Vector2(Vector3.Dot(supportDelta,activeCamera.transform.right),Vector3.Dot(supportDelta,activeCamera.transform.up));
            }
            // Every direction uses the same two-bone solve. Occlusion changes drawing,
            // never the anatomy or the contacts on the rendered weapon.
            RightArmReach=ArmEnd(grip-shoulderR,rightArmDepth).magnitude;LeftArmReach=ArmEnd(support-shoulderL,leftArmDepth).magnitude;
            Vector2 elbowR=ProjectedArmElbow(shoulderR,grip,anatomicalRight,rightArmDepth,false,aimBlend);
            Vector2 elbowL=ProjectedArmElbow(shoulderL,support,-anatomicalRight,leftArmDepth,true,aimBlend);
            int baseOrder=100;bool backFacing=Mathf.Cos(angle)<-.45f;int rightOrder=(near>0||Mathf.Cos(angle)>.45f)&&!backFacing?baseOrder+16:baseOrder-10,leftOrder=(near<0||Mathf.Cos(angle)>.45f)&&!backFacing?baseOrder+14:baseOrder-10;
            if(!oneHandedPose&&!backFacing&&Mathf.Abs(side)>.35f)leftOrder=baseOrder+22;
            Segment(0,parts[mirror?mirrorParts[0]:0],neck+new Vector2(side*.018f,.255f)+forward*aimBlend*.035f,neck+new Vector2(side*.018f,-.075f),baseOrder+11,mirror);
            Segment(1,parts[mirror?mirrorParts[1]:1],neck,hip-Vector2.up*.03f,baseOrder+2,mirror);
            Vector2 pelvisRoll=moving?anatomicalRight*Mathf.Sin(gaitPhase*Mathf.PI*2)*.045f:Vector2.zero;
            Segment(2,parts[mirror?mirrorParts[2]:2],hip+Vector2.up*.16f-pelvisRoll,hip-Vector2.up*.25f+pelvisRoll,baseOrder+3,mirror);
            bool rightHeelBehind=moving&&move.y<-.10f&&swingR>0&&swingR<.75f,leftHeelBehind=moving&&move.y<-.10f&&swingL>0&&swingL<.75f;
            int rightLegOrder=baseOrder-4+(near>0?2:0),leftLegOrder=baseOrder-4+(near<0?2:0);
            // In front-facing recovery the heel folds behind the knee. Drawing
            // the raised boot over the thigh made it look like a shortened peg leg.
            Segment(9,parts[mirror?mirrorParts[9]:9],legR,kneeR,rightLegOrder+(rightHeelBehind?3:0),mirror);
            Segment(10,parts[mirror?mirrorParts[10]:10],kneeR,footR,rightLegOrder+(rightHeelBehind?2:1),mirror);
            Vector2 bootR=SwingBootAxis(move,swingR,heelR,running),bootL=SwingBootAxis(move,swingL,heelL,running);
            Segment(11,parts[mirror?mirrorParts[11]:11],footR-bootR*.035f,footR+bootR*.15f,rightLegOrder+(rightHeelBehind?1:2),mirror);
            Segment(12,parts[mirror?mirrorParts[12]:12],legL,kneeL,leftLegOrder+(leftHeelBehind?3:0),mirror);
            Segment(13,parts[mirror?mirrorParts[13]:13],kneeL,footL,leftLegOrder+(leftHeelBehind?2:1),mirror);
            Segment(14,parts[mirror?mirrorParts[14]:14],footL-bootL*.035f,footL+bootL*.15f,leftLegOrder+(leftHeelBehind?1:2),mirror);
            int rightUpper=rightOrder,leftUpper=side>.35f&&!backFacing?baseOrder-10:leftOrder;
            int rightFore=!oneHandedPose&&!backFacing?baseOrder+21:rightOrder+1;
            int leftFore=!oneHandedPose&&!backFacing?baseOrder+21:leftOrder+1;
            // Compact aimed guns were sorted beneath both forearms, hiding the pistol
            // between crossed front-facing arms. Keep the palms in front while exposing
            // the gun and both arm-to-grip joins.
            if(IsCatalogOneHanded&&!backFacing){rightFore=baseOrder+19;leftFore=baseOrder+19;}
            int rightHandOrder=backFacing?rightOrder+5:baseOrder+23;
            int leftHandOrder=backFacing?leftOrder+5:baseOrder+22;
            if(IsCatalogOneHanded&&WeaponModel==14&&!backFacing){rightHandOrder=gunOrder+1;leftHandOrder=gunOrder+1;}
            Vector2 rightHandAxis=IsCatalogOneHanded&&WeaponModel==14?(grip-elbowR).normalized:gunAxis;
            Segment(3,parts[mirror?mirrorParts[3]:3],shoulderR,elbowR,rightUpper,mirror);Segment(4,parts[mirror?mirrorParts[4]:4],elbowR,grip,rightFore,mirror);Segment(5,parts[mirror?mirrorParts[5]:5],grip-rightHandAxis*.035f,grip+rightHandAxis*.095f,rightHandOrder,mirror);
            Vector2 leftHandAxis=oneHandedPose?(support-elbowL).normalized:gunAxis;
            if(IsCatalogOneHanded&&WeaponModel==14)leftHandAxis=(support-elbowL).normalized;
            Segment(6,parts[mirror?mirrorParts[6]:6],shoulderL,elbowL,leftUpper,mirror);Segment(7,parts[mirror?mirrorParts[7]:7],elbowL,support,leftFore,mirror);Segment(8,parts[mirror?mirrorParts[8]:8],support-leftHandAxis*.035f,support+leftHandAxis*.095f,leftHandOrder,mirror);
            int[] slots={0,1,2,2,3,3,4,4},targets={0,1,4,7,10,13,11,14};
            var armorParts=armor[source];
            for(int i=0;i<8;i++)
            {
                var item=equipped[slots[i]];var r=gearViews[i];var target=views[targets[i]];
                var selectedArmor=item!=null&&ExtraArmorReady&&item.visualVariant%4>=2?extraArmor[source]:armorParts;
                r.sprite=item!=null&&selectedArmor!=null?selectedArmor[(item.visualVariant%2)*8+(mirror?mirrorGear[i]:i)]:null;r.enabled=r.sprite!=null;
                if(!r.enabled)continue;r.flipX=mirror;r.color=Color.white;r.sortingOrder=target.sortingOrder+1;
                r.transform.SetPositionAndRotation(target.transform.position-activeCamera.transform.forward*.0003f,target.transform.rotation);
                float height=target.sprite.bounds.size.y*Mathf.Abs(target.transform.lossyScale.y)*(i==2||i==3?1.20f:1.03f);
                float targetWidth=target.sprite.bounds.size.x*Mathf.Abs(target.transform.lossyScale.x),widthFactor=i==0?1.24f:i==1?1.16f:i==2||i==3?1.22f:1.18f;
                float scale=Mathf.Min(height/r.sprite.bounds.size.y,targetWidth*widthFactor/r.sprite.bounds.size.x);SetScale(r,scale);
                if(i>=4){var local=r.transform.localScale;var parent=r.transform.parent.lossyScale;local.x=targetWidth*1.08f/(r.sprite.bounds.size.x*Mathf.Max(.001f,Mathf.Abs(parent.x)));r.transform.localScale=local;}
                // Keep the equipment's bottom (neck/wrist) attached when a width cap reduces its height.
                if(i==0)r.transform.position+=target.transform.up*(height-r.sprite.bounds.size.y*scale)*-.65f;
                if(i==2||i==3)
                {
                    DrawArmSection(r,r.sprite,i==2?elbowR:elbowL,i==2?grip:support,.56f,.28f,.21f,r.sortingOrder,mirror);
                }
                if(i==1)CutStaticSleeves(r,hip,neck,angle);
                if(i>=6)ApplyCapClip(r,.73f,0);
            }
            for(int arm=0;arm<2;arm++)
            {
                var chest=equipped[1];var r=upperArmorViews[arm];
                var upperParts=chest!=null&&ExtraArmorReady&&chest.visualVariant%4>=2?extraArmor[source]:armorParts;
                r.enabled=chest!=null&&upperParts!=null;if(!r.enabled)continue;
                int part=arm==0?2:3;r.sprite=upperParts[(chest.visualVariant%2)*8+(mirror?mirrorGear[part]:part)];
                Vector2 shoulder=arm==0?shoulderR:shoulderL,elbow=arm==0?elbowR:elbowL;
                Vector2 plateEnd=shoulder+(elbow-shoulder).normalized*Mathf.Min(.17f,(elbow-shoulder).magnitude);
                DrawArmSection(r,r.sprite,shoulder,plateEnd,.96f,.74f,.245f,(arm==0?rightUpper:leftUpper)+1,mirror);
            }
            if(ReviewRigDebug)
            {
                // Isolate the painted body and replace armor with explicit joint/grip landmarks.
                foreach(var r in gearViews)r.enabled=false;
                foreach(var r in upperArmorViews)r.enabled=false;
                foreach(var r in armoredHandViews)r.enabled=false;
                for(int i=0;i<views.Length;i++)if(views[i]!=null&&views[i].enabled)
                    views[i].color=i==1?new Color(.23f,.25f,.29f):i==2?new Color(.52f,.55f,.60f):i==3?new Color(1f,.23f,.08f):i==4?new Color(1f,.58f,.04f):i==5?new Color(1f,.96f,.04f):i==6?new Color(.05f,.80f,1f):i==7?new Color(.18f,.18f,1f):i==8?new Color(.90f,.08f,1f):Color.white;
                Vector2[] points={shoulderR,elbowR,grip,shoulderL,elbowL,support,grip+anatomicalRight*.035f,support-anatomicalRight*.035f,grip,support};
                Color[] colors={new Color(.10f,1f,.12f),new Color(1f,.12f,.04f),new Color(1f,.75f,.02f),new Color(.02f,.96f,1f),new Color(.92f,.03f,1f),new Color(.06f,.50f,1f),Color.yellow,Color.cyan,Color.white,Color.white};
                for(int i=0;i<rigDebugMarkers.Length;i++){var marker=rigDebugMarkers[i];marker.sprite=rigMarkerSprite;marker.enabled=true;marker.color=colors[i];marker.sortingOrder=240;marker.transform.SetPositionAndRotation(World(points[i],-.04f-(i%2)*.001f),activeCamera.transform.rotation);SetScale(marker,i<6?.18f:.11f);}
            }
            else foreach(var marker in rigDebugMarkers)if(marker!=null)marker.enabled=false;
            views[5].enabled=!HasArmoredGloves;views[8].enabled=!HasArmoredGloves;
            for(int hand=0;hand<2;hand++)
            {
                var renderer=armoredHandViews[hand];renderer.enabled=HasArmoredGloves&&gearViews[hand+2].sprite!=null;if(!renderer.enabled)continue;
                var sourceGlove=gearViews[hand+2].sprite;
                if(!armoredHandSprites.TryGetValue(sourceGlove,out var glove))
                {
                    var rect=sourceGlove.rect;rect.height*=.28f;glove=Sprite.Create(sourceGlove.texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);armoredHandSprites[sourceGlove]=glove;
                }
                renderer.sprite=glove;renderer.flipX=mirror;renderer.color=gearViews[hand+2].color;renderer.sortingOrder=hand==0?(backFacing?rightOrder+5:baseOrder+24):(backFacing?leftOrder+5:baseOrder+23);
                Vector2 handPoint=hand==0?grip:support,elbowPoint=hand==0?elbowR:elbowL,handAxis=(handPoint-elbowPoint).normalized;
                renderer.transform.SetPositionAndRotation(World(handPoint,(100-renderer.sortingOrder)*.0003f),activeCamera.transform.rotation*Quaternion.Euler(0,0,Mathf.Atan2(handAxis.y,handAxis.x)*Mathf.Rad2Deg+90));
                float handSize=IsCatalogOneHanded&&WeaponModel==14?Mathf.Lerp(.135f,.112f,WeaponAimBlend):.135f;
                SetScale(renderer,handSize/Mathf.Max(.01f,glove.bounds.size.y));
            }
            RightHandPosition=WeaponReady?VisualGripPosition:World(grip);LeftHandPosition=WeaponReady?VisualSupportGripPosition:World(support);RightElbowPosition=World(elbowR);LeftElbowPosition=World(elbowL);RightShoulderPosition=World(shoulderR);LeftShoulderPosition=World(shoulderL);
            UpdateRightWristBridge(false);
            if(weapon!=null){if(weapon.grip!=null)weapon.grip.position=RightHandPosition;if(weapon.supportGrip!=null)weapon.supportGrip.position=LeftHandPosition;if(weapon.muzzle!=null&&WeaponReady)weapon.muzzle.position=VisualMuzzlePosition;var old=weapon.GetComponent<PaintedWeapon>();if(old!=null){old.enabled=false;if(old.PaintedRenderer!=null)old.PaintedRenderer.enabled=false;}}
            if(hiddenMeshes==null)hiddenMeshes=GetComponentsInChildren<MeshRenderer>(true);foreach(var mesh in hiddenMeshes)if(mesh!=null){mesh.enabled=false;mesh.shadowCastingMode=ShadowCastingMode.Off;}
            PoseRevision++;
        }
        bool RenderApprovedPose()
        {
            var footOrigin=transform.position-Vector3.up*.65f-activeCamera.transform.forward*.20f;
            bool aim=ReviewOverride?reviewAim||reviewMotion==Motion.Shoot:IsAiming;
            bool moving=ReviewOverride?reviewMotion==Motion.Walk||reviewMotion==Motion.Run:locomoting;
            int movementDirection=ReviewOverride?(reviewMovementDirection>=0?reviewMovementDirection:CurrentDirection):MovementDirection;
            if(!approvedPose.Render(activeCamera,footOrigin,CurrentDirection,aim,IsCatalogOneHanded,equipped,moving,movementDirection,ReviewOverride?reviewPhase:walkPhase,weaponVisualSet))return false;
            if(approvedPose.LocomotionActive){RightFootPosition=approvedPose.RightFoot;LeftFootPosition=approvedPose.LeftFoot;}
            foreach(var r in views)if(r!=null)r.enabled=false;
            foreach(var r in gearViews)if(r!=null)r.enabled=false;
            foreach(var r in armoredHandViews)if(r!=null)r.enabled=false;
            foreach(var r in upperArmorViews)if(r!=null)r.enabled=false;
            foreach(var r in rigDebugMarkers)if(r!=null)r.enabled=false;
            if(gunView!=null)gunView.enabled=false;
            if(rightWristBridge!=null)rightWristBridge.enabled=false;
            if(rightBracerBridge!=null)rightBracerBridge.enabled=false;
            if(groundShadow!=null)
            {
                groundShadow.enabled=true;groundShadow.transform.SetPositionAndRotation(transform.position-Vector3.up*.635f,Quaternion.Euler(90,0,0));
                var scale=transform.lossyScale;groundShadow.transform.localScale=new Vector3(.82f/scale.x,.43f/scale.y,1);
            }
            VisualMuzzlePosition=approvedPose.Muzzle;VisualGripPosition=RightHandPosition=approvedPose.Grip;
            VisualSupportGripPosition=LeftHandPosition=approvedPose.Support;VisualStockPosition=StockContactTarget=approvedPose.Stock;
            if(approvedPose.UsesExternalWeapon)RenderApprovedExternalWeapon();
            CarryGripTarget=VisualGripPosition;StockOcclusionActive=false;WeaponAimBlend=aim?1:0;
            if(weapon!=null)
            {
                if(weapon.grip!=null)weapon.grip.position=RightHandPosition;
                if(weapon.supportGrip!=null)weapon.supportGrip.position=LeftHandPosition;
                if(weapon.muzzle!=null)weapon.muzzle.position=VisualMuzzlePosition;
                var old=weapon.GetComponent<PaintedWeapon>();if(old!=null){old.enabled=false;if(old.PaintedRenderer!=null)old.PaintedRenderer.enabled=false;}
            }
            if(hiddenMeshes==null)hiddenMeshes=GetComponentsInChildren<MeshRenderer>(true);
            foreach(var mesh in hiddenMeshes)if(mesh!=null&&!approvedPose.OwnsRenderer(mesh)){mesh.enabled=false;mesh.shadowCastingMode=ShadowCastingMode.Off;}
            PoseRevision++;return true;
        }
        void RenderApprovedExternalWeapon()
        {
            int direction=CurrentDirection;var sprite=gunSprites!=null?gunSprites[(WeaponModel%2)*8+direction]:null;
            if(sprite==null)return;
            var table=WeaponModel>=14?handgunAnchors:WeaponModel>=12?modernAnchors:anchors;WeaponAnchor source=null;
            if(table?.anchors!=null)foreach(var candidate in table.anchors)if(candidate.model==WeaponModel&&candidate.direction==direction){source=candidate;break;}
            if(source==null)return;
            Vector2 grip=AnchorLocal(sprite,source.grip),support=AnchorLocal(sprite,source.support),muzzle=AnchorLocal(sprite,source.muzzle),stock=AnchorLocal(sprite,source.stock);
            Vector2 oldSpan=support-grip;
            Vector3 target=approvedPose.Support-approvedPose.Grip;
            Vector2 newSpan=new Vector2(Vector3.Dot(target,activeCamera.transform.right),Vector3.Dot(target,activeCamera.transform.up));
            if(oldSpan.magnitude<.001f||newSpan.magnitude<.001f)return;
            float scale=newSpan.magnitude/oldSpan.magnitude;
            float rotation=(Mathf.Atan2(newSpan.y,newSpan.x)-Mathf.Atan2(oldSpan.y,oldSpan.x))*Mathf.Rad2Deg;
            gunView.sprite=sprite;gunView.enabled=true;gunView.flipX=false;gunView.flipY=false;gunView.color=Color.white;gunView.SetPropertyBlock(null);
            gunView.sortingOrder=direction>=3&&direction<=5?96:110;
            gunView.transform.SetPositionAndRotation(approvedPose.Grip,activeCamera.transform.rotation*Quaternion.Euler(0,0,rotation));
            var parentScale=gunView.transform.parent.lossyScale;gunView.transform.localScale=new Vector3(scale/parentScale.x,scale/parentScale.y,scale/parentScale.z);
            gunView.transform.position-=gunView.transform.TransformVector(new Vector3(grip.x,grip.y,0));
            VisualGripPosition=gunView.transform.TransformPoint(new Vector3(grip.x,grip.y,0));VisualSupportGripPosition=gunView.transform.TransformPoint(new Vector3(support.x,support.y,0));
            VisualMuzzlePosition=gunView.transform.TransformPoint(new Vector3(muzzle.x,muzzle.y,0));VisualStockPosition=gunView.transform.TransformPoint(new Vector3(stock.x,stock.y,0));
            // Hands remain at their authored source pixels; the weapon uses a
            // single rigid similarity transform fitted to those two contacts.
            RightHandPosition=approvedPose.Grip;LeftHandPosition=approvedPose.Support;
        }
        static Vector2 Elbow(Vector2 shoulder,Vector2 hand,float upper,float fore,Vector2 pole)
        {
            Vector2 axis=hand-shoulder;float distance=Mathf.Max(.001f,axis.magnitude);Vector2 forward=axis/distance;
            float along=(upper*upper-fore*fore+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            Vector2 normal=new Vector2(-forward.y,forward.x);if(Vector2.Dot(normal,pole)<0)normal=-normal;return shoulder+forward*along+normal*height;
        }
        static Vector3 ArmEnd(Vector2 delta,float depth)
        {return new Vector3(delta.x,(delta.y-depth*.5773503f)/.8164966f,depth);}
        Vector2 ProjectedArmElbow(Vector2 shoulder,Vector2 hand,Vector2 outward,float depth,bool supportArm,float aimBlend)
        {
            const float elevation=.8164966f,depthProjection=.5773503f,upper=.40f,fore=.38f;
            Vector3 end=ArmEnd(hand-shoulder,depth);
            float distance=Mathf.Max(.001f,end.magnitude);
            // The complete weapon was constrained before these endpoints were read.
            // An unreachable endpoint is an implementation error, never a longer arm.
            if(distance>upper+fore+.001f)Debug.LogError("Unreachable fixed-length weapon grip: "+distance+" model="+WeaponModel+" direction="+CurrentDirection+" blend="+aimBlend+" support="+supportArm+" instance="+GetEntityId());
            Vector3 axis=end/distance;
            float lateral=supportArm?Mathf.Lerp(.12f,-.65f,aimBlend):.20f;
            float drop=supportArm?Mathf.Lerp(-1f,-.45f,aimBlend):-.8f;
            Vector3 pole=new Vector3(outward.x*lateral,drop,outward.y*lateral/depthProjection);
            Vector3 normal=(pole-axis*Vector3.Dot(pole,axis)).normalized;
            float along=(upper*upper-fore*fore+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            // Projecting a valid 3D bend can hide an entire sleeve along the view
            // axis. Rotate the bend by the smallest angle on the same fixed-bone
            // joint circle; never push the projected elbow off that circle.
            if(!ReadableArm(axis*along+normal*height,end))
            {
                Vector3 best=normal;float bestAngle=181;
                foreach(float sign in new[]{-1f,1f})
                {
                    float previousAngle=0;
                    for(float turn=10;turn<=180;turn+=10)
                    {
                        Vector3 candidate=Quaternion.AngleAxis(sign*turn,axis)*normal;
                        if(ReadableArm(axis*along+candidate*height,end))
                        {
                            float lo=previousAngle,hi=turn;
                            for(int iteration=0;iteration<10;iteration++)
                            {
                                float mid=(lo+hi)*.5f;Vector3 trial=Quaternion.AngleAxis(sign*mid,axis)*normal;
                                if(ReadableArm(axis*along+trial*height,end))hi=mid;else lo=mid;
                            }
                            if(hi<bestAngle){bestAngle=hi;best=Quaternion.AngleAxis(sign*hi,axis)*normal;}break;
                        }
                        previousAngle=turn;
                    }
                }
                normal=best;
            }
            Vector3 joint=axis*along+normal*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            return shoulder+new Vector2(joint.x,joint.y*elevation+joint.z*depthProjection);
        }
        static bool ReadableArm(Vector3 joint,Vector3 end)
        {
            Vector3 fore=end-joint;
            Vector2 upperProjection=new Vector2(joint.x,joint.y*.8164966f+joint.z*.5773503f);
            Vector2 foreProjection=new Vector2(fore.x,fore.y*.8164966f+fore.z*.5773503f);
            return upperProjection.magnitude>=.075f&&foreProjection.magnitude>=.045f;
        }
        static void SolveLeg(Vector2 hip,Vector2 move,float cycle,bool moving,bool running,float idleStep,out Vector2 knee,out Vector2 foot,out float swing)
        {
            const float elevation=.8164966f;
            if(!moving){swing=0;foot=new Vector2(hip.x,.14f)+move*idleStep;knee=Vector2.Lerp(hip,foot,.51f)+move*.015f;return;}
            float stance=running?.38f:.46f;
            swing=moving?Mathf.Clamp01((cycle-stance)/(1-stance)):0;
            // The stance foot travels exactly the distance covered by the actor in
            // this fraction of a cycle. Recovery folds behind before reaching forward.
            float halfStep=GaitCycleDistance(running)*stance*.5f;
            float step=moving?(cycle<stance?Mathf.Lerp(halfStep,-halfStep,cycle/stance):Mathf.Lerp(-halfStep,halfStep,Mathf.SmoothStep(0,1,swing))):idleStep;
            float lift=moving?Mathf.Pow(Mathf.Sin(swing*Mathf.PI),1.5f)*(running?.50f:.38f):0;
            float airborne=Mathf.Sin(Mathf.Clamp01((Mathf.Repeat(cycle,.5f)-stance)/(.5f-stance))*Mathf.PI)*(running?.095f:.025f);
            float height=(hip.y-.14f-airborne)/elevation;
            Vector2 ankle=new Vector2(step,lift-height);
            float thigh=moving?.62f:.53f,shin=moving?.60f:.52f;
            ankle=Vector2.ClampMagnitude(ankle,thigh+shin-.025f);
            Vector2 joint=Elbow(Vector2.zero,ankle,thigh,shin,Vector2.right);
            knee=hip+move*joint.x+Vector2.up*(joint.y*elevation);
            foot=hip+move*ankle.x+Vector2.up*(ankle.y*elevation);
        }
        static Vector2 SwingBootAxis(Vector2 move,float swing,float heel,bool running)
        {
            // Heel recovery points the toe back, then levels it toward the next
            // plant. The planted half never rotates or slides relative to stride.
            float lift=Mathf.Sin(swing*Mathf.PI);
            Vector2 back=new Vector2(-move.x,-move.y*.35f);
            Vector2 axis=Vector2.down*(1-heel*(running?.78f:.52f))+back*heel*(running?.85f:.50f)+new Vector2(move.x,0)*lift*swing*.18f;
            return axis.normalized;
        }
        static float FootStep(float cycle,float halfStep)
        {
            // Ground half-cycle cancels actor travel; the lifted return accelerates into the next plant.
            return cycle<.5f?Mathf.Lerp(halfStep,-halfStep,cycle*2):Mathf.Lerp(-halfStep,halfStep,Mathf.SmoothStep(0,1,(cycle-.5f)*2));
        }
        void UpdateRightWristBridge(bool visible)
        {
            CopyDistal(views[4],rightWristBridge,wristBridgeBlock,visible,.48f,.10f);
            CopyDistal(gearViews[2],rightBracerBridge,bracerBridgeBlock,visible&&HasArmoredGloves,.56f,.28f);
        }
        void CopyDistal(SpriteRenderer source,SpriteRenderer target,MaterialPropertyBlock properties,bool visible,float top,float bottom)
        {
            target.enabled=visible&&source.enabled&&source.sprite!=null;if(!target.enabled)return;
            target.sprite=source.sprite;target.flipX=source.flipX;target.flipY=source.flipY;target.color=source.color;
            target.sortingOrder=target==rightWristBridge&&IsCatalogOneHanded&&WeaponModel==14&&WeaponAimBlend>.5f?gunView.sortingOrder-1:gunView.sortingOrder+1;
            target.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);target.transform.localScale=source.transform.localScale;
            source.GetPropertyBlock(properties);var rect=source.sprite.rect;properties.SetFloat("_PartClipEnabled",1);
            properties.SetVector("_PartClipRect",new Vector4(rect.x/source.sprite.texture.width,(rect.y+rect.height*bottom)/source.sprite.texture.height,rect.xMax/source.sprite.texture.width,(rect.y+rect.height*top)/source.sprite.texture.height));
            target.SetPropertyBlock(properties);
        }
        void Segment(int index,Sprite sprite,Vector2 top,Vector2 bottom,int order,bool mirror)
        {
            var r=views[index];r.sprite=sprite;r.enabled=sprite!=null;r.color=Color.white;if(sprite==null)return;r.flipX=mirror;r.sortingOrder=order;
            Vector2 axis=bottom-top,sourceTop=new Vector2(0,0),sourceBottom=new Vector2(0,-sprite.bounds.size.y);
            bool limb=index>=3;
            if(limb)
            {
                var joints=SourceJoints(sprite,index==11||index==14);sourceTop=joints[0];sourceBottom=joints[1];
                if(mirror){sourceTop.x=-sourceTop.x;sourceBottom.x=-sourceBottom.x;}
            }
            Vector2 sourceAxis=sourceBottom-sourceTop;float angle=(Mathf.Atan2(axis.y,axis.x)-Mathf.Atan2(sourceAxis.y,sourceAxis.x))*Mathf.Rad2Deg;
            float scale=(axis.magnitude+(limb?.008f:.025f))/Mathf.Max(.01f,sourceAxis.magnitude);
            r.transform.rotation=activeCamera.transform.rotation*Quaternion.Euler(0,0,angle);SetScale(r,scale);
            float cap=index==0?.34f:index==1?.66f:index==2?.48f:index==3||index==6?.20f:index==4||index==7?.18f:index==5||index==8?.115f:index==9||index==12?.31f:index==10||index==13?.25f:.265f;
            if((index==5||index==8)&&IsCatalogOneHanded&&WeaponModel==14&&WeaponAimBlend>.5f)cap=.055f;
            float width=sprite.bounds.size.x*scale;float xFactor=Mathf.Min(1,cap/Mathf.Max(.001f,width));if(index==2)xFactor=Mathf.Clamp(.49f/Mathf.Max(.001f,width),.8f,1.6f);
            float viewWidth=.86f+.14f*Mathf.Abs(Mathf.Cos(CurrentDirection*Mathf.PI/4));
            if(index==9||index==12)xFactor=.31f*viewWidth/Mathf.Max(.001f,width);
            if(index==10||index==13)xFactor=.25f*viewWidth/Mathf.Max(.001f,width);
            if(index==11||index==14)xFactor=.265f*viewWidth/Mathf.Max(.001f,width);
            var local=r.transform.localScale;local.x*=xFactor;r.transform.localScale=local;
            if(limb)
            {
                // Re-solve orientation after fixing the transverse width. A
                // nonvertical painted limb otherwise misses its socket when widened.
                float scaleX=scale*xFactor,span=axis.magnitude+.008f;
                float scaleY=Mathf.Sqrt(Mathf.Max(.000001f,span*span-Mathf.Pow(sourceAxis.x*scaleX,2)))/Mathf.Max(.001f,Mathf.Abs(sourceAxis.y));
                Vector2 correctedAxis=new Vector2(sourceAxis.x*scaleX,sourceAxis.y*scaleY);
                angle=(Mathf.Atan2(axis.y,axis.x)-Mathf.Atan2(correctedAxis.y,correctedAxis.x))*Mathf.Rad2Deg;
                r.transform.rotation=activeCamera.transform.rotation*Quaternion.Euler(0,0,angle);
                var parent=transform.lossyScale;r.transform.localScale=new Vector3(scaleX/Mathf.Max(.001f,Mathf.Abs(parent.x)),scaleY/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);
                r.transform.position=World(top,(100-order)*.0003f)-r.transform.right*(sourceTop.x*scaleX)-r.transform.up*(sourceTop.y*scaleY);
            }
            else r.transform.position=World(top,(100-order)*.0003f)-(r.transform.right*sourceTop.x*xFactor+r.transform.up*sourceTop.y)*scale;
            if(index==2)ApplyCapClip(r,.86f,.15f);
            // Preserve the painted shoulder cap above the upper-arm socket so it
            // tucks into the torso instead of ending as a flat, detached edge.
            else if(index==3||index==6)ApplyCapClip(r,.96f,.19f);
            else if(index==4||index==7)ApplyCapClip(r,.81f,.19f);
            else if(limb)ApplyCapClip(r,index==11||index==14?.78f:.87f,index==11||index==14?0:.10f);
        }
        static Vector2[] SourceJoints(Sprite sprite,bool boot)
        {
            if(partJoints.TryGetValue(sprite,out var stored))return stored;
            var rect=sprite.rect;var pixels=sprite.texture.GetPixels32();int textureWidth=sprite.texture.width;
            float[] ys=boot?new[]{.71f,.08f}:new[]{.79f,.21f};var joints=new Vector2[2];
            for(int end=0;end<2;end++)
            {
                double sum=0;int count=0;int middle=Mathf.RoundToInt(rect.y+rect.height*ys[end]),half=Mathf.Max(1,Mathf.RoundToInt(rect.height*.055f));
                for(int y=Mathf.Max((int)rect.y,middle-half);y<Mathf.Min((int)rect.yMax,middle+half+1);y++)for(int x=(int)rect.x;x<(int)rect.xMax;x++)if(pixels[y*textureWidth+x].a>100){sum+=x+.5f;count++;}
                float px=count>0?(float)(sum/count):rect.center.x;
                joints[end]=new Vector2(px-rect.x-sprite.pivot.x,rect.height*ys[end]-sprite.pivot.y)/100f;
            }
            partJoints[sprite]=joints;return joints;
        }
        static void ApplyCapClip(SpriteRenderer renderer,float top,float bottom)
        {
            var sprite=renderer.sprite;if(sprite==null)return;
            var key=(sprite,top,bottom);
            if(!capClips.TryGetValue(key,out var block))
            {
                var rect=sprite.rect;block=new MaterialPropertyBlock();block.SetFloat("_PartClipEnabled",1);
                block.SetVector("_PartClipRect",new Vector4(rect.x/sprite.texture.width,(rect.y+rect.height*bottom)/sprite.texture.height,rect.xMax/sprite.texture.width,(rect.y+rect.height*top)/sprite.texture.height));capClips[key]=block;
            }
            renderer.SetPropertyBlock(block);
        }
        static Vector2 ArmorSocket(Sprite sprite,float fraction)
        {
            var key=(sprite,fraction);if(armorSockets.TryGetValue(key,out var socket))return socket;
            if(!armorPixels.TryGetValue(sprite.texture,out var pixels)){pixels=sprite.texture.GetPixels32();armorPixels[sprite.texture]=pixels;}
            var rect=sprite.rect;int row=Mathf.RoundToInt(rect.y+rect.height*fraction),half=Mathf.Max(1,Mathf.RoundToInt(rect.height*.02f));double total=0;int count=0;
            for(int y=Mathf.Max((int)rect.y,row-half);y<Mathf.Min((int)rect.yMax,row+half+1);y++)for(int x=(int)rect.x;x<(int)rect.xMax;x++)if(pixels[y*sprite.texture.width+x].a>96){total+=x+.5f;count++;}
            float px=count>0?(float)(total/count):rect.center.x;
            socket=new Vector2(px-rect.x-sprite.pivot.x,rect.height*fraction-sprite.pivot.y)/100;armorSockets[key]=socket;return socket;
        }
        void DrawArmSection(SpriteRenderer r,Sprite sprite,Vector2 top,Vector2 bottom,float clipTop,float clipBottom,float width,int order,bool mirror)
        {
            r.sprite=sprite;r.enabled=sprite!=null;if(!r.enabled)return;r.flipX=mirror;r.color=Color.white;r.sortingOrder=order;
            Vector2 sourceTop=ArmorSocket(sprite,clipTop-.025f),sourceBottom=ArmorSocket(sprite,clipBottom+.025f);
            if(mirror){sourceTop.x=-sourceTop.x;sourceBottom.x=-sourceBottom.x;}
            Vector2 sourceAxis=sourceBottom-sourceTop,targetAxis=bottom-top;
            float scaleX=Mathf.Min(targetAxis.magnitude/Mathf.Max(.01f,sourceAxis.magnitude),width/Mathf.Max(.01f,sprite.bounds.size.x));
            float scaleY=Mathf.Sqrt(Mathf.Max(.000001f,targetAxis.sqrMagnitude-Mathf.Pow(sourceAxis.x*scaleX,2)))/Mathf.Max(.001f,Mathf.Abs(sourceAxis.y));
            Vector2 corrected=new Vector2(sourceAxis.x*scaleX,sourceAxis.y*scaleY);
            float rotation=(Mathf.Atan2(targetAxis.y,targetAxis.x)-Mathf.Atan2(corrected.y,corrected.x))*Mathf.Rad2Deg;
            r.transform.rotation=activeCamera.transform.rotation*Quaternion.Euler(0,0,rotation);var parent=r.transform.parent.lossyScale;
            r.transform.localScale=new Vector3(scaleX/Mathf.Max(.001f,Mathf.Abs(parent.x)),scaleY/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);
            r.transform.position=World(top,(100-order)*.0003f)-r.transform.right*sourceTop.x*scaleX-r.transform.up*sourceTop.y*scaleY;
            ApplyCapClip(r,clipTop,clipBottom);
        }
        void CutStaticSleeves(SpriteRenderer r,Vector2 hip,Vector2 neck,float angle)
        {
            r.GetPropertyBlock(torsoCutBlock);torsoCutBlock.SetFloat("_TorsoSleeveCutEnabled",1);
            Vector3 a=activeCamera.WorldToViewportPoint(World(hip+Vector2.up*.06f)),b=activeCamera.WorldToViewportPoint(World(neck-Vector2.up*.035f));
            float radius=.255f*(.8f+.2f*Mathf.Abs(Mathf.Cos(angle)));
            float viewportRadius=Mathf.Abs(activeCamera.WorldToViewportPoint(World(hip+Vector2.right*radius)).x-a.x);
            torsoCutBlock.SetVector("_TorsoSleeveA",a);torsoCutBlock.SetVector("_TorsoSleeveB",b);torsoCutBlock.SetFloat("_TorsoSleeveRadius",viewportRadius);r.SetPropertyBlock(torsoCutBlock);
        }
        void SetScale(SpriteRenderer r,float scale)
        {var parent=r.transform.parent.lossyScale;r.transform.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(parent.x)),scale/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);}
        void UpdateGun(int d,Vector2 grip,Vector2 support,Vector2 axis,Vector2 barrel,Vector2 rightShoulder,Vector2 hip,Vector2 neck,Vector2 anatomicalRight,int order,float aimBlend,float recoil)
        {
            gunView.sprite=gunSprites!=null?gunSprites[(WeaponModel%2)*8+d]:null;
            // Rear poses use ordinary torso occlusion with the same contact geometry.
            gunView.enabled=gunView.sprite!=null;
            gunView.flipX=false;gunView.flipY=false;
            if(clippedWeapon!=gunView.sprite)
            {
                clippedWeapon=gunView.sprite;int count=0;
                var activeRegions=WeaponModel>=14?handgunWeaponRegions:WeaponModel>=12?modernWeaponRegions:weaponRegions;
                if(activeRegions?.regions!=null)foreach(var region in activeRegions.regions)if(region.model==WeaponModel&&region.direction==d&&region.hull!=null)
                {
                    count=Math.Min(64,region.hull.Length);for(int point=0;point<count;point++)weaponClipPoints[point]=new Vector4(region.hull[point].x,1-region.hull[point].y,0,0);break;
                }
                weaponClipBlock.SetInt("_ClipCount",count);weaponClipBlock.SetVectorArray("_ClipPoints",weaponClipPoints);gunView.SetPropertyBlock(weaponClipBlock);
            }
            float length=modelLengths[WeaponModel]*Mathf.Max(.2f,barrel.magnitude);Vector2 localGrip,localMuzzle,localSupport,localStock;
            if(gunView.sprite!=null)
            {
                var sprite=gunView.sprite;Vector2 authored=new Vector2(Mathf.Sin(d*Mathf.PI/4),-Mathf.Cos(d*Mathf.PI/4)*.58f).normalized;
                float sourceLength=Mathf.Abs(authored.x)*sprite.bounds.size.x+Mathf.Abs(authored.y)*sprite.bounds.size.y;
                localGrip=-authored*sourceLength*.18f;localMuzzle=authored*sourceLength*.48f;localSupport=authored*sourceLength*.035f;localStock=-authored*sourceLength*.48f;
                var activeAnchors=WeaponModel>=14?handgunAnchors:WeaponModel>=12?modernAnchors:anchors;
                if(activeAnchors?.anchors!=null)foreach(var a in activeAnchors.anchors)if(a.model==WeaponModel&&a.direction==d){localGrip=AnchorLocal(sprite,a.grip);localMuzzle=AnchorLocal(sprite,a.muzzle);localSupport=AnchorLocal(sprite,a.support);localStock=AnchorLocal(sprite,a.stock);break;}
                // Calibrated against the actual model-1 paintings: leather fore-end
                // midpoint in rear view and the lower handguard in the side view.
                if(WeaponModel==1&&d==4)localSupport=AnchorLocal(sprite,new Vector2(.51f,.32f));
                if(WeaponModel==1&&d==6)localSupport=AnchorLocal(sprite,new Vector2(.35f,.36f));
                // Rear model-6 muzzle was mistakenly on its optic. The menu rifle's
                // old rear anchors came from a different painting. These points are
                // calibrated on the active source artwork, not clamped to arm reach.
                if(WeaponModel==6&&d==4)
                {
                    localMuzzle=AnchorLocal(sprite,new Vector2(.50f,.12f));
                    localSupport=AnchorLocal(sprite,new Vector2(.50f,.30f));
                }
                if(WeaponModel==12&&d==4)
                {
                    localStock=AnchorLocal(sprite,new Vector2(.43f,.94f));
                    localMuzzle=AnchorLocal(sprite,new Vector2(.43f,.12f));
                    localGrip=AnchorLocal(sprite,new Vector2(.43f,.68f));
                    localSupport=AnchorLocal(sprite,new Vector2(.43f,.45f));
                }
                // A pistol support palm wraps the firing grip; a long gun uses its
                // authored fore-end. Both contacts are transformed with the gun.
                if(IsCatalogOneHanded)
                {
                    Vector2 pistolAxis=(localMuzzle-localGrip).normalized;
                    localSupport=localGrip+pistolAxis*.035f+new Vector2(-pistolAxis.y,pistolAxis.x)*.035f;
                }
                Vector2 rawStock=localStock,rawMuzzle=localMuzzle,rawGrip=localGrip;
                // Pistols do not have a shoulder stock; their legacy stock
                // anchor is merely an art alignment point and is not on the
                // barrel axis. Aim their atlas cells from the muzzle to the
                // actual trigger-hand grip so the rendered barrel follows aim.
                bool gripDefinesAxis=IsCatalogOneHanded;
                Vector2 authoredAxis=localMuzzle-(gripDefinesAxis?localGrip:localStock);
                if(!IsCatalogOneHanded)
                {
                    Vector2 nominal=new Vector2(Mathf.Sin(d*Mathf.PI/4),-Mathf.Cos(d*Mathf.PI/4)*.5773503f);
                    float nominalRotation=(Mathf.Atan2(nominal.y,nominal.x)-Mathf.Atan2(authoredAxis.y,authoredAxis.x))*Mathf.Rad2Deg;
                    Vector2 handleOffset=localGrip-localStock;
                    Vector2 handleNormal=handleOffset-authoredAxis*Vector2.Dot(handleOffset,authoredAxis)/Mathf.Max(.00001f,authoredAxis.sqrMagnitude);
                    // The atlas's trigger grip determines its underside. Some rear
                    // three-quarter paintings reverse the receiver roll. Reflect the
                    // painting and every contact together, then align the same axis.
                    if(Rotate(handleNormal,nominalRotation).y>.01f)
                    {
                        gunView.flipY=true;localGrip.y=-localGrip.y;localSupport.y=-localSupport.y;localMuzzle.y=-localMuzzle.y;localStock.y=-localStock.y;
                        authoredAxis=localMuzzle-localStock;
                    }
                }
                float rotation=Mathf.DeltaAngle(0,(Mathf.Atan2(axis.y,axis.x)-Mathf.Atan2(authoredAxis.y,authoredAxis.x))*Mathf.Rad2Deg);
                // Front/rear paintings encode depth through foreshortening. Their
                // projected stock/muzzle can reverse vertically; never roll the
                // whole gun upside down to interpret that as a planar direction.

                // Aim artwork can be authored across the fore/aft axis at a
                // diagonal. Let it complete the rotation to match the real
                // barrel vector; clamping here left front-facing pistols lying
                // sideways across the waist while the arms reached forward.

                float scale=length/Mathf.Max(.01f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));

                gunView.transform.rotation=activeCamera.transform.rotation*Quaternion.Euler(0,0,rotation);
                StockContactTarget=World(rightShoulder-Vector2.up*(.16f*(1-aimBlend)),(100-order)*.0003f);
                Vector3 carryPosition=World(grip,(100-order)*.0003f)-(gunView.transform.right*localGrip.x+gunView.transform.up*localGrip.y)*scale;
                Vector3 bracedPosition=StockContactTarget-(gunView.transform.right*localStock.x+gunView.transform.up*localStock.y)*scale;
                gunView.transform.position=IsCatalogOneHanded?carryPosition:Vector3.Lerp(carryPosition,bracedPosition,aimBlend);
                Vector2 leftShoulder=rightShoulder-anatomicalRight*.53f;
                float sideDepth=anatomicalRight.y/.5773503f;
                float forwardDepth=barrel.y/.5773503f;
                Vector2 stockAxis=localMuzzle-localStock;
                float gripFraction=.30f,supportFraction=.65f,sidePhysicalRatio=.9f;
                // A front-cell vertical grip offset describes handle height, not
                // longitudinal depth. Recover that depth once from the side cell.
                if(activeAnchors?.anchors!=null)foreach(var sideAnchor in activeAnchors.anchors)
                    if(sideAnchor.model==WeaponModel&&sideAnchor.direction==6)
                    {
                        Vector2 sideAxis=sideAnchor.muzzle-sideAnchor.stock;
                        Vector2 sideSupport=WeaponModel==1?new Vector2(.35f,.36f):sideAnchor.support;
                        var sideSprite=gunSprites[(WeaponModel%2)*8+6];
                        Vector2 sideSpan=Vector2.Scale(sideAxis,new Vector2(sprite.texture.width/400f,sprite.texture.height/400f));
                        sidePhysicalRatio=sideSpan.magnitude/Mathf.Max(.01f,Mathf.Max(sideSprite.bounds.size.x,sideSprite.bounds.size.y));
                        float squared=Mathf.Max(.00001f,sideAxis.sqrMagnitude);
                        gripFraction=Vector2.Dot(sideAnchor.grip-sideAnchor.stock,sideAxis)/squared;
                        supportFraction=Vector2.Dot(sideSupport-sideAnchor.stock,sideAxis)/squared;break;
                    }
                // Shoulders, the stock and both wrists share one reconstructed 3D
                // frame. In a side view the far shoulder is .53 units deeper;
                // treating it as a shallow 2D endpoint created the rectangular arm.
                float readyDepth=-sideDepth*.065f+forwardDepth*.08f;
                float physicalLength=modelLengths[WeaponModel]*sidePhysicalRatio;
                rightArmDepth=IsCatalogOneHanded?Mathf.Lerp(readyDepth,forwardDepth*.46f-sideDepth*.265f,aimBlend):Mathf.Lerp(readyDepth,forwardDepth*physicalLength*gripFraction,aimBlend);
                leftArmDepth=IsCatalogOneHanded?rightArmDepth+sideDepth*.53f:Mathf.Lerp(readyDepth+sideDepth*.53f,sideDepth*.53f+forwardDepth*physicalLength*supportFraction,aimBlend);
                // Project the entire rigid weapon into both arm reach volumes.
                // This continuous translation is zero for valid shoulder-braced
                // endpoints. No aim threshold, scale change or detached wrist is used.
                for(int pass=0;pass<32;pass++)
                {
                    bool within=true;
                    for(int hand=0;hand<2;hand++)
                    {
                        Vector2 contact=hand==0?localGrip:localSupport;
                        Vector3 wrist=gunView.transform.position+(gunView.transform.right*contact.x+gunView.transform.up*contact.y)*scale;
                        Vector3 relative=wrist-World(hand==0?rightShoulder:leftShoulder);
                        Vector2 screen=new Vector2(Vector3.Dot(relative,activeCamera.transform.right),Vector3.Dot(relative,activeCamera.transform.up));
                        float depth=hand==0?rightArmDepth:leftArmDepth;
                        Vector3 end=ArmEnd(screen,depth);
                        if(end.magnitude<=.7651f)continue;
                        within=false;
                        float reachRadius=Mathf.Sqrt(Mathf.Max(.0001f,.765f*.765f-depth*depth));
                        Vector2 plane=Vector2.ClampMagnitude(new Vector2(end.x,end.y),reachRadius);
                        Vector2 change=new Vector2(plane.x-screen.x,plane.y*.8164966f+depth*.5773503f-screen.y);
                        gunView.transform.position+=activeCamera.transform.right*change.x+activeCamera.transform.up*change.y;
                    }
                    if(within)break;
                }
                SetScale(gunView,scale);gunView.color=WeaponDisplayTint(WeaponModel,weaponColor);gunView.sortingOrder=order;
                VisualMuzzlePosition=gunView.transform.position+(gunView.transform.right*localMuzzle.x+gunView.transform.up*localMuzzle.y)*scale;
                VisualGripPosition=gunView.transform.position+(gunView.transform.right*localGrip.x+gunView.transform.up*localGrip.y)*scale;
                VisualSupportGripPosition=gunView.transform.position+(gunView.transform.right*localSupport.x+gunView.transform.up*localSupport.y)*scale;
                VisualStockPosition=gunView.transform.position+(gunView.transform.right*localStock.x+gunView.transform.up*localStock.y)*scale;
                if(IsCatalogOneHanded)StockContactTarget=VisualStockPosition;
                StockOcclusionActive=!IsCatalogOneHanded;
                weaponClipBlock.SetFloat("_StockOcclusionEnabled",StockOcclusionActive?1:0);
                Vector2 inverseTexture=new Vector2(1f/sprite.texture.width,1f/sprite.texture.height);
                Vector2 stockUV=Vector2.Scale(sprite.rect.position+sprite.pivot+rawStock*100,inverseTexture);
                Vector2 muzzleUV=Vector2.Scale(sprite.rect.position+sprite.pivot+rawMuzzle*100,inverseTexture);
                Vector2 uvAxis=muzzleUV-stockUV;weaponClipBlock.SetVector("_StockUVOrigin",stockUV);weaponClipBlock.SetVector("_StockUVAxis",uvAxis/Mathf.Max(.0000001f,uvAxis.sqrMagnitude));
                float stockFraction=Vector2.Dot(rawGrip-rawStock,rawMuzzle-rawStock)/Mathf.Max(.00001f,(rawMuzzle-rawStock).sqrMagnitude);
                weaponClipBlock.SetFloat("_StockFraction",Mathf.Clamp(stockFraction+.08f,.12f,.40f));
                Vector3 torsoA=activeCamera.WorldToViewportPoint(World(hip)),torsoB=activeCamera.WorldToViewportPoint(World(neck-Vector2.up*.10f));
                float radius=Mathf.Abs(activeCamera.WorldToViewportPoint(World(hip+Vector2.right*.265f)).x-torsoA.x);
                weaponClipBlock.SetVector("_StockTorsoA",torsoA);weaponClipBlock.SetVector("_StockTorsoB",torsoB);weaponClipBlock.SetFloat("_StockTorsoRadius",radius);
                gunView.SetPropertyBlock(weaponClipBlock);
            }
            else {StockOcclusionActive=false;StockContactTarget=VisualStockPosition=World(rightShoulder);VisualMuzzlePosition=World(grip+axis*length);VisualGripPosition=World(grip);VisualSupportGripPosition=World(support);}
        }
        static Vector2 AnchorLocal(Sprite sprite,Vector2 normalized)
        {
            // Anchors are normalized inside the untrimmed source cell, with y from top.
            var tex=sprite.texture;Vector2 center=sprite.rect.center;Vector2 cellOrigin=cellOrigins.TryGetValue(sprite,out var cell)?cell:new Vector2(Mathf.Floor(center.x/(tex.width/4f))*tex.width/4f,Mathf.Floor(center.y/(tex.height/4f))*tex.height/4f);
            Vector2 point=cellOrigin+new Vector2(normalized.x*tex.width/4f,(1-normalized.y)*tex.height/4f);return (point-sprite.rect.position-sprite.pivot)/100f;
        }
        static Vector2 Rotate(Vector2 value,float degrees)
        {float a=degrees*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(value.x*c-value.y*s,value.x*s+value.y*c);}
        public string AppearanceSignature()
        {var s=WeaponModel+":"+CurrentDirection;foreach(var r in gearViews)s+="/"+(r.sprite==null?"none":r.sprite.name)+"@"+r.color;return s;}
        public Bounds PaintedBounds()
        {Bounds bounds=new Bounds(origin,Vector3.zero);foreach(var r in views)if(r.enabled&&r.sprite!=null)bounds.Encapsulate(r.bounds);foreach(var r in gearViews)if(r.enabled&&r.sprite!=null)bounds.Encapsulate(r.bounds);if(gunView!=null&&gunView.enabled)bounds.Encapsulate(gunView.bounds);return bounds;}
    }
}














