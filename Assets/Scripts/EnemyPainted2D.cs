using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Three independent painted cutouts per view. Animation is pose composition,
    // not a claim of individually painted animation frames.
    [DefaultExecutionOrder(470)]
    public sealed class EnemyPainted2D : MonoBehaviour
    {
        public enum Motion { Idle, Walk, Anticipation, Attack, Hit, Death }
        public const int DirectionCount=8, WalkSamples=64, IdleSamples=32, AttackSamples=32, HitSamples=16, DeathSamples=32;
        public const float DeathDuration=.85f;
        public bool Ready {get;private set;}
        public int LoadedDirections {get;private set;}
        public int CurrentDirection {get;private set;}
        public int SourceDirection=>CurrentDirection<=4?CurrentDirection:8-CurrentDirection;
        public int LeftLegSourceCell=>SourceDirection*3+LeftSourcePart();
        public int RightLegSourceCell=>SourceDirection*3+3-LeftSourcePart();
        public Motion CurrentMotion {get;private set;}
        public Vector3 MuzzlePosition {get;private set;}
        public SpriteRenderer[] BodyRenderers=>parts;
        public SpriteRenderer WeaponRenderer=>gun;
        public bool WeaponReady=>gun!=null&&gun.sprite!=null;
        public bool ReviewOverride {get;private set;}
        static readonly Dictionary<int,Sprite[]> cache=new Dictionary<int,Sprite[]>();
        static readonly Dictionary<int,Sprite[]> rearCache=new Dictionary<int,Sprite[]>();
        static readonly Dictionary<Sprite,Rect> legCrops=new Dictionary<Sprite,Rect>();
        // Trigger hand measured in each trimmed upper-body painting (bottom-up).
        static readonly Vector2[][] triggerHands={
            new[]{new Vector2(.33f,.29f),new Vector2(.48f,.25f),new Vector2(.81f,.24f),new Vector2(.84f,.37f),new Vector2(.70f,.30f)},
            new[]{new Vector2(.38f,.23f),new Vector2(.53f,.22f),new Vector2(.83f,.26f),new Vector2(.83f,.38f),new Vector2(.68f,.30f)},
            new[]{new Vector2(.41f,.27f),new Vector2(.56f,.24f),new Vector2(.83f,.28f),new Vector2(.85f,.35f),new Vector2(.70f,.25f)}
        };
        static readonly float[] heights={2.05f,2.02f,2.16f,3.5f,1.9f,2.22f,2.08f,2.5f,2.26f,2.65f};
        static readonly float[] breadth={1,1,1,1,1,1,1,1,1,1};
        static readonly float[] waistCenter={.50f,.44f,.38f,.51f,.50f};
        static readonly float[] pouncerWaistCenter={.50f,.43f,.23f,.44f,.50f};
        static readonly float[] pouncerWaistOverlap={.36f,.36f,.40f,.28f,.18f};
        // Measured toe/heel silhouettes, rather than the prompt's left/right
        // labels: these front pairs were generated in camera-left/right order.
        static readonly bool[] frontPairReversed={false,true,true,false,false,true,true,true,true,false};
        int LeftSourcePart(){bool exchange=(CurrentDirection>4)^(SourceDirection==0&&frontPairReversed[owner.kind]);return exchange?2:1;}
        static bool ReverseToeView(int kind,int view,int part)
        {
            if(view==1&&(kind==1||kind==4||kind==6||kind==7||kind==8))return true;
            if(view==2&&(kind==4||kind==7))return true;
            if(view==2&&part==2&&(kind==8||kind==9))return true;
            return view==3&&part==1&&(kind==5||kind==6||kind==7||kind==8||kind==9);
        }
        // The lowest upper-body pixel is often a sleeve or hanging skirt,
        // rather than its hip joint. Overlap these painted hems over the thighs.
        static readonly float[] waistOverlap={.055f,.06f,.10f,.20f,.18f,.09f,.11f,.09f,.09f,.12f};
        static Material material;
        SortingGroup sortingGroup;
        [Serializable] public class RegionTable { public Region[] regions; }
        [Serializable] public class Region { public int kind,cell;public float x,y,width,height; }
        static RegionTable authoredRegions;
        [Serializable] public class RearRegionTable {public RearRegion[] regions;}
        [Serializable] public class RearRegion {public int kind,side;public float x,y,width,height;}
        static RearRegionTable rearRegions;
        Foe owner; Sprite[] sprites,weapons; SpriteRenderer[] parts=new SpriteRenderer[3];SpriteRenderer gun;
        LayeredPlayer2D.WeaponAnchorTable anchors; LayeredPlayer2D.WeaponRegionTable regions;
        Vector3 previous,movement,hitAway;float phase,idle,attack,hit,death=-1;int reviewDir,deathDirection;Motion reviewMotion;float reviewPhase;
        MaterialPropertyBlock block;
        readonly Vector4[] hull=new Vector4[64];
        public void Init(Foe foe)
        {
            if(owner!=null)return;owner=foe;previous=transform.position;block=new MaterialPropertyBlock();
            if(!cache.TryGetValue(owner.kind,out sprites))
            {
                var tex=Resources.Load<Texture2D>("Art/LayeredEnemies/Enemy_"+owner.kind);
                if(tex!=null){sprites=Cut(tex,owner.kind);if(sprites!=null)cache[owner.kind]=sprites;}
            }
            if(sprites==null)return;
            var rear=RearLegs(owner.kind);if(rear!=null){sprites[13]=rear[0];sprites[14]=rear[1];}
            for(int d=0;d<5;d++)if(sprites[d*3]!=null&&sprites[d*3+1]!=null&&sprites[d*3+2]!=null)LoadedDirections++;
            Ready=LoadedDirections==5;if(!Ready)return;sortingGroup=GetComponent<SortingGroup>();if(sortingGroup==null)sortingGroup=gameObject.AddComponent<SortingGroup>();
            foreach(var motion in GetComponentsInChildren<ActorMotion>())motion.enabled=false;
            foreach(var old in GetComponentsInChildren<PaintedActor>())old.enabled=false;
            foreach(var old in GetComponentsInChildren<PaintedWeapon>())old.enabled=false;
            for(int i=0;i<3;i++)parts[i]=Create("Painted enemy / "+(i==0?"upper body":i==1?"left leg":"right leg"));
            gun=Create("Independent enemy firearm");
            int model=owner.kind==5?7:owner.kind==6?10:8;
            weapons=LayeredPlayer2D.LoadCells("Art/LayeredPlayer/Weapons_"+(model/2),false);
            var json=Resources.Load<TextAsset>(LayeredPlayer2D.ArtMetadataPath("WeaponAnchors"));if(json!=null)anchors=JsonUtility.FromJson<LayeredPlayer2D.WeaponAnchorTable>(json.text);
            json=Resources.Load<TextAsset>(LayeredPlayer2D.ArtMetadataPath("WeaponRegions"));if(json!=null)regions=JsonUtility.FromJson<LayeredPlayer2D.WeaponRegionTable>(json.text);
            // Hide retained rig geometry and previous sprites only after the complete art exists.
            foreach(var r in GetComponentsInChildren<Renderer>())if(Array.IndexOf(parts,r)<0&&r!=gun&&!(r is LineRenderer)&&!(r is TrailRenderer))r.enabled=false;
            RefreshPose();
        }
        static Sprite[] Cut(Texture2D tex,int kind)
        {
            if(authoredRegions==null){var json=Resources.Load<TextAsset>("Art/LayeredEnemies/EnemyRegions");if(json!=null)authoredRegions=JsonUtility.FromJson<RegionTable>(json.text);}
            // Actual sheets have measured layouts. Never guess a grid when
            // the authoring contract is incomplete: that would cut up neighbors.
            if(authoredRegions?.regions==null)return null;
            var complete=new bool[15];foreach(var region in authoredRegions.regions)if(region.kind==kind&&region.cell>=0&&region.cell<15&&region.width>0&&region.height>0)complete[region.cell]=true;
            for(int cell=0;cell<15;cell++)if(!complete[cell]){authoredRegions=null;return null;}
            var result=new Sprite[15];var pixels=tex.isReadable?tex.GetPixels32():null;
            for(int i=0;i<15;i++)
            {
                int x=0,y=0,w=0,h=0;
                foreach(var r in authoredRegions.regions)if(r.kind==kind&&r.cell==i&&r.width>0&&r.height>0){x=Mathf.Clamp(Mathf.FloorToInt(r.x*tex.width),0,tex.width-1);y=Mathf.Clamp(Mathf.FloorToInt((1-r.y-r.height)*tex.height),0,tex.height-1);w=Mathf.Clamp(Mathf.CeilToInt(r.width*tex.width),1,tex.width-x);h=Mathf.Clamp(Mathf.CeilToInt(r.height*tex.height),1,tex.height-y);break;}
                if(w<=0||h<=0){authoredRegions=null;return null;}
                int minX=0,minY=0,maxX=w-1,maxY=h-1;
                if(pixels!=null){minX=w;minY=h;maxX=-1;maxY=-1;for(int py=0;py<h;py++)for(int px=0;px<w;px++)if(pixels[(y+py)*tex.width+x+px].a>24){minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py);}}
                if(maxX<minX)continue;
                var crop=new Rect(x+minX,y+minY,maxX-minX+1,maxY-minY+1);var rect=crop;Vector2 pivot=new Vector2(.5f,i%3==0?0:1);
                if(i%3==0)pivot.x=kind==4?pouncerWaistCenter[i/3]:waistCenter[i/3];
                else
                {
                    float margin=crop.width*.25f;float left=Mathf.Max(0,crop.x-margin),right=Mathf.Min(tex.width,crop.xMax+margin);rect=new Rect(left,crop.y,right-left,crop.height);
                    float hipX=crop.center.x;
                    if(pixels!=null){double sum=0,count=0;for(int py=(int)crop.yMax-Mathf.Max(1,(int)(crop.height*.20f));py<(int)crop.yMax;py++)for(int px=(int)crop.x;px<(int)crop.xMax;px++)if(pixels[py*tex.width+px].a>48){sum+=px+.5;count++;}if(count>0)hipX=(float)(sum/count);}
                    pivot.x=(hipX-left)/rect.width;
                }
                result[i]=Sprite.Create(tex,rect,pivot,100,0,SpriteMeshType.FullRect);
                if(i%3!=0)legCrops[result[i]]=crop;
                result[i].name="Enemy cutout / "+i;
            }
            return result;
        }
        SpriteRenderer Create(string label)
        {
            if(material==null){var shader=Resources.Load<Shader>("EnemySprite");if(shader==null)shader=Shader.Find("Sprites/Default");material=new Material(shader);}
            var go=new GameObject(label);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return r;
        }
        static Sprite[] RearLegs(int kind)
        {
            if(rearCache.TryGetValue(kind,out var stored))return stored;
            var texture=Resources.Load<Texture2D>("Art/LayeredEnemies/BackLegsV2");if(texture==null||!texture.isReadable)return null;
            if(rearRegions==null){var json=Resources.Load<TextAsset>("Art/LayeredEnemies/BackLegRegions");if(json==null)return null;rearRegions=JsonUtility.FromJson<RearRegionTable>(json.text);}
            if(rearRegions?.regions==null)return null;
            var pixels=texture.GetPixels32();var result=new Sprite[2];
            for(int side=0;side<2;side++)
            {
                RearRegion region=null;foreach(var candidate in rearRegions.regions)if(candidate.kind==kind&&candidate.side==side){region=candidate;break;}if(region==null)return null;
                int x=Mathf.Clamp(Mathf.FloorToInt(region.x*texture.width),0,texture.width-1),y=Mathf.Clamp(Mathf.FloorToInt((1-region.y-region.height)*texture.height),0,texture.height-1);
                int width=Mathf.Clamp(Mathf.CeilToInt(region.width*texture.width),1,texture.width-x),height=Mathf.Clamp(Mathf.CeilToInt(region.height*texture.height),1,texture.height-y);
                int minX=width,minY=height,maxX=-1,maxY=-1;
                for(int py=0;py<height;py++)for(int px=0;px<width;px++)if(pixels[(y+py)*texture.width+x+px].a>24){minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py);}
                if(maxX<minX)return null;
                var crop=new Rect(x+minX,y+minY,maxX-minX+1,maxY-minY+1);
                double total=0,count=0;for(int py=(int)crop.yMax-Mathf.Max(1,(int)(crop.height*.20f));py<(int)crop.yMax;py++)for(int px=(int)crop.x;px<(int)crop.xMax;px++)if(pixels[py*texture.width+px].a>48){total+=px+.5;count++;}
                float hipX=count>0?(float)(total/count):crop.center.x,left=Mathf.Max(0,crop.x-crop.width*.25f),right=Mathf.Min(texture.width,crop.xMax+crop.width*.25f);
                var rect=new Rect(left,crop.y,right-left,crop.height);
                result[side]=Sprite.Create(texture,rect,new Vector2((hipX-left)/rect.width,1),100,0,SpriteMeshType.FullRect);
                result[side].name="Rear painted leg V2 / "+kind+" / "+side;legCrops[result[side]]=crop;
            }
            rearCache[kind]=result;return result;
        }
        public void NotifyAttack(){attack=.38f;RefreshPose();}
        public void NotifyHit(Vector3? point=null){hit=.17f;if(point.HasValue){hitAway=transform.position-point.Value;hitAway.y=0;}}
        public void BeginDeath(){death=0;attack=hit=0;deathDirection=CurrentDirection;CurrentMotion=Motion.Death;RefreshPose();}
        public void SetReviewPose(int direction,Motion motion,float sample){ReviewOverride=true;reviewDir=(direction%8+8)%8;reviewMotion=motion;reviewPhase=Mathf.Repeat(sample,1);RefreshPose();}
        public void ClearReviewPose(){ReviewOverride=false;previous=transform.position;}
        void LateUpdate()
        {
            if(!Ready)return;
            if(death>=0){death+=Time.deltaTime;CurrentMotion=Motion.Death;}
            bool active=owner.game==null||owner.game.Active;
            if(active){var delta=transform.position-previous;delta.y=0;if(delta.sqrMagnitude>.000001f)movement=delta.normalized;phase=Mathf.Repeat(phase+delta.magnitude/(heights[owner.kind]*.58f),1);idle+=Time.deltaTime;
                attack=Mathf.Max(0,attack-Time.deltaTime);hit=Mathf.Max(0,hit-Time.deltaTime);
                CurrentMotion=death>=0?Motion.Death:hit>0?Motion.Hit:attack>0||owner.IsLunging?Motion.Attack:owner.IsTelegraphing?Motion.Anticipation:delta.sqrMagnitude>.000001f?Motion.Walk:Motion.Idle;}
            previous=transform.position;RefreshPose();
        }
        public void RefreshPose()
        {
            if(!Ready)return;var camera=owner.game!=null?owner.game.GameCamera:Camera.main;if(camera==null)return;
            if(sortingGroup==null)sortingGroup=GetComponent<SortingGroup>();if(sortingGroup==null)sortingGroup=gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder=Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(transform.position,camera.transform.up)*100),-30000,19000);
            var down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;
            CurrentDirection=ReviewOverride?reviewDir:Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(Vector3.Dot(transform.forward,camera.transform.right),Vector3.Dot(transform.forward,down))*Mathf.Rad2Deg,360)/45)%8;
            if(!ReviewOverride&&death>=0)CurrentDirection=deathDirection;
            if(ReviewOverride)CurrentMotion=reviewMotion;
            int source=CurrentDirection<=4?CurrentDirection:8-CurrentDirection;bool mirror=CurrentDirection>4;
            float p=ReviewOverride?reviewPhase:CurrentMotion==Motion.Death?Mathf.Clamp01(death/DeathDuration):CurrentMotion==Motion.Attack?1-attack/.38f:phase;
            float h=heights[owner.kind],legH=h*.47f,upperH=h-legH;
            float walk=CurrentMotion==Motion.Walk?Mathf.Sin(p*Mathf.PI*2):0;
            float prepare=CurrentMotion==Motion.Anticipation?Mathf.SmoothStep(.15f,1,ReviewOverride?p:owner.TelegraphProgress):0;
            float strike=CurrentMotion==Motion.Attack?Mathf.Sin(Mathf.Clamp01(p)*Mathf.PI):0;
            float flinch=CurrentMotion==Motion.Hit?Mathf.Sin((ReviewOverride?p:1-hit/.17f)*Mathf.PI):0;
            float dying=CurrentMotion==Motion.Death?p:0;
            float opacity=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,dying));
            float breathPhase=ReviewOverride?reviewPhase*Mathf.PI*2:idle*2.4f;
            float breath=Mathf.Sin(breathPhase+owner.kind*.4f);
            float bob=(Mathf.Abs(walk)*.025f+breath*.008f-prepare*.025f)*(1-dying);
            float fallSide=Vector3.Dot(hitAway,camera.transform.right);if(Mathf.Abs(fallSide)<.08f)fallSide=Mathf.Sin(CurrentDirection*Mathf.PI/4);if(Mathf.Abs(fallSide)<.08f)fallSide=owner.kind%2==0?1:-1;fallSide=Mathf.Sign(fallSide);
            float buckle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.65f,dying));
            float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.96f,dying));
            float stagger=dying>0?Mathf.Sin(Mathf.Clamp01(dying/.26f)*Mathf.PI)*h*.035f:0;
            Vector2 hips=new Vector2(fallSide*(stagger+settle*h*.16f),legH+bob-buckle*legH*.60f-settle*legH*.18f);
            float lean=(owner.kind==4?8:owner.kind==9?-5:0)+prepare*-8+strike*10-flinch*8;
            float deathTilt=-fallSide*(buckle*13+settle*23);
            Vector3 feet=transform.position-Vector3.up*.635f;
            Vector3 World(Vector2 pos)=>feet+camera.transform.right*pos.x+camera.transform.up*pos.y;
            float directionAngle=CurrentDirection*Mathf.PI/4;
            // Part 1 is anatomical left, part 2 anatomical right. Camera left
            // is anatomical right at the front and left at the back. Mirroring
            // also exchanges the source limbs; their hip offsets must rotate.
            Vector2 anatomicalLeft=new Vector2(Mathf.Cos(directionAngle),Mathf.Sin(directionAngle)*.58f);
            float legWidth=0;for(int leg=1;leg<3;leg++){var legSprite=sprites[source*3+leg];var crop=legCrops[legSprite];legWidth+=crop.width/crop.height*legH*.5f;}
            float side=legWidth*(.12f+.24f*Mathf.Abs(Mathf.Cos(directionAngle)));
            Vector2 travel=new Vector2(Mathf.Sin(directionAngle),-Mathf.Cos(directionAngle)*.42f);
            if(!ReviewOverride&&movement.sqrMagnitude>.001f)travel=new Vector2(Vector3.Dot(movement,camera.transform.right),Vector3.Dot(movement,camera.transform.up));
            for(int i=1;i<3;i++)
            {
                int sourceCell=i==1?LeftLegSourceCell:RightLegSourceCell;
                var sprite=sprites[sourceCell];var r=parts[i];r.sprite=sprite;bool legMirror=mirror^ReverseToeView(owner.kind,source,sourceCell%3);r.flipX=legMirror;
                float sign=i==1?1:-1;
                Vector2 hipOffset=anatomicalLeft*(side*sign);
                // A planted phase followed by an airborne return. Project stride
                // onto the camera basis so front, side and back gait differ.
                float cycle=ReviewOverride?p:phase;float footPhase=Mathf.Repeat(cycle+(i==1?0:.5f),1);
                float strideSize=h*.58f*.3f;
                float stride=CurrentMotion==Motion.Walk?(footPhase<.6f?Mathf.Lerp(strideSize,-strideSize,footPhase/.6f):Mathf.Lerp(-strideSize,strideSize,(footPhase-.6f)/.4f)):0;
                float lift=CurrentMotion==Motion.Walk&&footPhase>=.6f?Mathf.Sin((footPhase-.6f)/.4f*Mathf.PI)*.13f:0;
                Vector2 legHip=hips+hipOffset;
                Vector2 sole=hipOffset+new Vector2(travel.x*stride, lift+travel.y*stride);
                if(dying>0)sole=hipOffset+new Vector2(fallSide*settle*h*.05f,settle*h*.015f);
                Vector2 legAxis=sole-legHip;
                float rotation=Mathf.Atan2(legAxis.y,legAxis.x)*Mathf.Rad2Deg+90;
                r.transform.SetPositionAndRotation(World(legHip),camera.transform.rotation*Quaternion.Euler(0,0,rotation));
                Scale(r,sprite,legAxis.magnitude,legH/Mathf.Max(.01f,legAxis.magnitude));r.sortingOrder=10+Mathf.RoundToInt(-(hipOffset.y+travel.y*stride)*30);r.color=new Color(1,1,1,opacity);
                block.Clear();if(legCrops.TryGetValue(sprite,out var crop))
                {
                    block.SetVector("_LimbRect",new Vector4(crop.x/sprite.texture.width,crop.y/sprite.texture.height,crop.width/sprite.texture.width,crop.height/sprite.texture.height));
                    float bend=CurrentMotion==Motion.Walk&&footPhase>=.6f?Mathf.Sin((footPhase-.6f)/.4f*Mathf.PI)*.22f*travel.x:0;
                    bend+=buckle*.18f*fallSide;
                    block.SetFloat("_LimbBend",legMirror?-bend:bend);
                }
                r.SetPropertyBlock(block);
            }
            var upper=parts[0];upper.sprite=sprites[source*3];upper.flipX=mirror^(owner.kind==9&&source==3);upper.sortingOrder=20;
            float overlap=owner.kind==4?pouncerWaistOverlap[source]:waistOverlap[owner.kind];float paintedHeight=upperH/(1-overlap);
            float upperRotation=(mirror?-lean:lean)+deathTilt;var upperOrientation=camera.transform.rotation*Quaternion.Euler(0,0,upperRotation);
            float upperLength=paintedHeight*Mathf.Lerp(1,.62f,settle);
            Vector2 sharedWaist=hips+new Vector2((mirror?-1:1)*(strike*.035f-flinch*.025f),0);
            upper.transform.SetPositionAndRotation(World(sharedWaist)-upperOrientation*Vector3.up*(upperLength*overlap),upperOrientation);
            Scale(upper,upper.sprite,upperLength,breadth[owner.kind]*paintedHeight/upperLength);upper.color=new Color(1,1,1,opacity);
            block.Clear();block.SetFloat("_Hit",flinch*.65f);block.SetFloat("_Breath",breath*.015f+prepare*.025f);
            block.SetFloat("_BodyHeight",upper.sprite.bounds.size.y);
            block.SetFloat("_TorsoBend",(mirror?-1:1)*(walk*.018f-prepare*.045f+strike*.075f-flinch*.065f));
            block.SetFloat("_TorsoTwist",walk*.025f+strike*.045f+breath*.005f);
            upper.SetPropertyBlock(block);
            MuzzlePosition=World(new Vector2(Mathf.Sin(CurrentDirection*Mathf.PI/4)*.5f,h*.70f));
            DrawGun(camera,dying,model:owner.kind==5?7:owner.kind==6?10:8);
        }
        void Scale(SpriteRenderer r,Sprite sprite,float height,float width)
        {var parent=transform.lossyScale;float factor=height/Mathf.Max(.01f,sprite.bounds.size.y);r.transform.localScale=new Vector3(factor*width/Mathf.Abs(parent.x),factor/Mathf.Abs(parent.y),1);}
        void DrawGun(Camera camera,float dying,int model)
        {
            bool ranged=owner.kind==2||owner.kind==5||owner.kind==6;gun.enabled=ranged&&weapons!=null&&dying<1;if(!gun.enabled)return;
            int d=CurrentDirection;var sprite=weapons[(model%2)*8+d];gun.sprite=sprite;if(sprite==null){gun.enabled=false;return;}
            Vector2 localGrip=Vector2.zero,localMuzzle=Vector2.right*.4f,stock=Vector2.left*.4f;
            Vector2 Local(Vector2 a){var tex=sprite.texture;int cellIndex=(model%2)*8+d;var cell=new Vector2(cellIndex%4*tex.width/4f,(3-cellIndex/4)*tex.height/4f);return (cell+new Vector2(a.x*tex.width/4f,(1-a.y)*tex.height/4f)-sprite.rect.position-sprite.pivot)/100;}
            if(anchors?.anchors!=null)foreach(var a in anchors.anchors)if(a.model==model&&a.direction==d){localGrip=Local(a.grip);localMuzzle=Local(a.muzzle);stock=Local(a.stock);break;}
            Vector3 facing=owner.weapon!=null?owner.weapon.Direction:transform.forward;
            Vector2 aim=new Vector2(Vector3.Dot(facing,camera.transform.right),Vector3.Dot(facing,camera.transform.up));
            if(ReviewOverride)aim=new Vector2(Mathf.Sin(d*Mathf.PI/4),-Mathf.Cos(d*Mathf.PI/4)*.58f);
            float length=owner.kind==6?.55f:owner.kind==5?.72f:.82f;
            float scale=length*Mathf.Lerp(.58f,1,Mathf.Abs(aim.normalized.x))/Mathf.Max(.05f,(localMuzzle-stock).magnitude);
            scale=Mathf.Min(scale,length*1.12f/Mathf.Max(.01f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y)));
            int source=d<=4?d:8-d;bool mirror=d>4;
            Vector2 authoredHand=triggerHands[owner.kind==2?0:owner.kind==5?1:2][source];var upper=parts[0];
            Vector2 handLocal=new Vector2((authoredHand.x-upper.sprite.pivot.x/upper.sprite.rect.width)*upper.sprite.bounds.size.x,authoredHand.y*upper.sprite.bounds.size.y);
            if(mirror)handLocal.x=-handLocal.x;
            // Match the same waist-fixed shader shear used by the body.
            float breath=Mathf.Sin((ReviewOverride?reviewPhase*Mathf.PI*2:idle*2.4f)+owner.kind*.4f);
            float walk=CurrentMotion==Motion.Walk?Mathf.Sin((ReviewOverride?reviewPhase:phase)*Mathf.PI*2):0;
            float prepare=CurrentMotion==Motion.Anticipation?Mathf.SmoothStep(.15f,1,ReviewOverride?reviewPhase:owner.TelegraphProgress):0;
            float strike=CurrentMotion==Motion.Attack?Mathf.Sin((ReviewOverride?reviewPhase:1-attack/.38f)*Mathf.PI):0;
            float flinch=CurrentMotion==Motion.Hit?Mathf.Sin((ReviewOverride?reviewPhase:1-hit/.17f)*Mathf.PI):0;
            float bend=(mirror?-1:1)*(walk*.018f-prepare*.045f+strike*.075f-flinch*.065f),twist=walk*.025f+strike*.045f+breath*.005f,inhale=breath*.015f+prepare*.025f;
            handLocal.x=handLocal.x*(1+inhale*authoredHand.y-Mathf.Abs(twist)*authoredHand.y)+upper.sprite.bounds.size.y*(bend+twist*.35f)*authoredHand.y;
            handLocal.y+=upper.sprite.bounds.size.y*inhale*authoredHand.y*.2f;
            Vector3 grip=upper.transform.TransformPoint(handLocal);
            gun.flipX=false;gun.flipY=false;Vector2 authoredAxis=localMuzzle-localGrip;
            if(Mathf.Abs(aim.normalized.x)>.35f&&authoredAxis.x*aim.x<0){gun.flipX=true;localGrip.x=-localGrip.x;localMuzzle.x=-localMuzzle.x;stock.x=-stock.x;authoredAxis=localMuzzle-localGrip;}
            float rotation=Mathf.DeltaAngle(0,(Mathf.Atan2(aim.y,aim.x)-Mathf.Atan2(authoredAxis.y,authoredAxis.x))*Mathf.Rad2Deg);
            if(Mathf.Abs(aim.normalized.x)<.35f)rotation=Mathf.Clamp(rotation,-22,22);
            rotation=Mathf.Clamp(rotation,-70,70);
            if(dying>0)rotation+=Mathf.DeltaAngle(0,(Quaternion.Inverse(camera.transform.rotation)*upper.transform.rotation).eulerAngles.z);
            float kick=ReviewOverride&&CurrentMotion==Motion.Attack?Mathf.Sin(Mathf.Clamp01(reviewPhase/.24f)*Mathf.PI)*.075f:attack>0?Mathf.Clamp01(attack/.38f)*.075f:0;
            gun.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,rotation);gun.transform.position=grip-(camera.transform.right*aim.normalized.x+camera.transform.up*aim.normalized.y)*kick-(gun.transform.right*localGrip.x+gun.transform.up*localGrip.y)*scale;
            var parent=transform.lossyScale;gun.transform.localScale=new Vector3(scale/Mathf.Abs(parent.x),scale/Mathf.Abs(parent.y),1);gun.sortingOrder=d>=3&&d<=5?15:25;gun.color=parts[0].color;
            block.Clear();int count=0;if(regions?.regions!=null)foreach(var r in regions.regions)if(r.model==model&&r.direction==d&&r.hull!=null){count=Math.Min(64,r.hull.Length);for(int n=0;n<count;n++)hull[n]=new Vector4(r.hull[n].x,1-r.hull[n].y,0,0);break;}
            block.SetInt("_ClipCount",count);block.SetVectorArray("_ClipPoints",hull);gun.SetPropertyBlock(block);
            MuzzlePosition=gun.transform.position+(gun.transform.right*localMuzzle.x+gun.transform.up*localMuzzle.y)*scale;
        }
    }
}

