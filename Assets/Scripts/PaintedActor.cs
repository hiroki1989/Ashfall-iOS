using UnityEngine;
namespace NeonVault
{
    [DefaultExecutionOrder(300)]
    public sealed class PaintedActor : MonoBehaviour
    {
        public int DirectionCount=>32;
        public int AnimationFrameCount=>4;
        public int CurrentDirection {get;private set;}
        public int CurrentFrame {get;private set;}
        public int LoadedFrameCount {get;private set;}
        public bool UsesPaintedBody=>view!=null&&view.sprite!=null;
        public bool ContinuousGeometry=>false;
        public SpriteRenderer PaintedRenderer=>view;
        readonly Sprite[][] frames=new Sprite[4][];
        readonly Sprite[][] armorFrames=new Sprite[4][];
        Sprite[] fallback;SpriteRenderer view;MaterialPropertyBlock block;
        Vector3 previous;float phase,scale;bool reverseDirections;
        // Measured silhouette yaws, not evenly spaced frame numbers. Generated art
        // spends many cells near front/back; sampling it uniformly misaligns aim.
        static readonly float[] runnerYaw={0,5,10,16,22,28,34,40,48,56,66,78,90,105,120,145,166,177,185,195,210,230,248,263,276,290,305,318,330,340,350,357};
        static readonly float[] sentinelYaw={0,10,20,32,44,56,66,76,85,94,104,114,126,138,150,160,169,177,186,198,214,228,244,258,271,281,293,305,318,333,346,355};
        public float FacingAngle {get;private set;}
        public int AuthoredDirection {get;private set;}
        int nextDirection;float directionBlend;
        AimedWeapon weapon;
        public void Init(bool hero,int kind)
        {
            string actor=hero?"Runner":"Sentinel";
            reverseDirections=!hero;
            float pixelHeight=1;
            for(int f=0;f<4;f++){frames[f]=PaintedSpriteAssets.Load("Art/"+actor+"32Walk"+f,8,4);if(frames[f]!=null){LoadedFrameCount++;foreach(var sprite in frames[f])pixelHeight=Mathf.Max(pixelHeight,sprite.bounds.size.y);}}
            for(int f=0;f<4;f++)armorFrames[f]=PaintedSpriteAssets.Load("Art/"+(hero?"Sentinel":"Runner")+"32Walk"+f,8,4);
            fallback=PaintedSpriteAssets.Load("Art/"+actor+"UnarmedDirections",4,2);
            if(LoadedFrameCount==0&&fallback!=null)foreach(var sprite in fallback)pixelHeight=Mathf.Max(pixelHeight,sprite.bounds.size.y);
            view=PaintedSpriteAssets.Create(transform,"Painted actor body / 32 directions");scale=1.85f/pixelHeight;block=new MaterialPropertyBlock();previous=transform.position;
            if(!hero){Color[] roles={new Color(.94f,.84f,.76f),new Color(.75f,.86f,1),new Color(.92f,.70f,1),new Color(1,.68f,.5f),new Color(1,.7f,.6f),new Color(.65f,.8f,1),new Color(.74f,.56f,1),new Color(1,.78f,.45f),new Color(.8f,.95f,1),new Color(.56f,1,.72f)};view.color=roles[Mathf.Clamp(kind,0,9)];}
            UpdateSprite();if(UsesPaintedBody)PaintedSpriteAssets.HideMeshes(transform);
        }
        public void ApplyArmor(ArmorItem[] items)
        {
            if(block==null)return;
            string[] names={"_HeadTint","_ChestTint","_ArmsTint","_LegsTint","_FeetTint"};foreach(string name in names)block.SetColor(name,Color.white);
            Vector4 partMix=Vector4.zero;float feetMix=0;
            if(items!=null)foreach(var item in items)if(item!=null)block.SetColor(names[(int)item.slot],Color.Lerp(Color.white,item.color*2.2f,.34f));
            if(items!=null)foreach(var item in items)if(item!=null){float mix=item.visualVariant==0?0:item.visualVariant==1?.65f:1;if(item.slot==ArmorSlot.Feet)feetMix=mix;else partMix[(int)item.slot]=mix;}
            block.SetVector("_ArmorParts",partMix);block.SetFloat("_ArmorFeet",feetMix);
            if(view!=null)view.SetPropertyBlock(block);
        }
        void UpdateSprite()
        {
            var set=frames[CurrentFrame];if(set==null)foreach(var frame in frames)if(frame!=null){set=frame;break;}
            view.sprite=set!=null?set[AuthoredDirection]:fallback!=null?fallback[Mathf.RoundToInt(CurrentDirection/4f)%8]:null;
            if(view.sprite!=null){var rect=view.sprite.rect;var tex=view.sprite.texture;block.SetVector("_Crop",new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));
                var next=set!=null?set[nextDirection]:view.sprite;var nr=next.rect;block.SetTexture("_NextTex",next.texture);block.SetVector("_NextCrop",new Vector4(nr.x/next.texture.width,nr.y/next.texture.height,nr.width/next.texture.width,nr.height/next.texture.height));block.SetFloat("_DirectionBlend",set!=null?directionBlend:0);
                var armor=armorFrames[CurrentFrame];if(armor!=null){float target=Mathf.Repeat(reverseDirections?FacingAngle:-FacingAngle,360);var table=reverseDirections?runnerYaw:sentinelYaw;int best=0;float closest=360;for(int d=0;d<32;d++){float delta=Mathf.Abs(Mathf.DeltaAngle(target,table[d]));if(delta<closest){closest=delta;best=d;}}var s=armor[best];var ar=s.rect;block.SetTexture("_ArmorTex",s.texture);block.SetVector("_ArmorCrop",new Vector4(ar.x/s.texture.width,ar.y/s.texture.height,ar.width/s.texture.width,ar.height/s.texture.height));}view.SetPropertyBlock(block);}
        }
        void LateUpdate()
        {
            if(view==null)return;var camera=Camera.main;if(camera==null)return;
            if(weapon==null)weapon=GetComponentInChildren<AimedWeapon>();
            Vector3 facing=weapon!=null?weapon.Direction:transform.forward;
            Vector3 right=camera.transform.right,down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;
            float angle=Mathf.Atan2(-Vector3.Dot(facing,right),Vector3.Dot(facing,down))*Mathf.Rad2Deg;
            FacingAngle=Mathf.Repeat(angle,360);
            CurrentDirection=Mathf.RoundToInt(Mathf.Repeat(angle,360)/11.25f)%32;
            if(reverseDirections)CurrentDirection=(32-CurrentDirection)%32;
            float artAngle=Mathf.Repeat(reverseDirections?-angle:angle,360);var yaws=reverseDirections?sentinelYaw:runnerYaw;
            AuthoredDirection=31;for(int d=0;d<31;d++)if(artAngle<yaws[d+1]){AuthoredDirection=d;break;}
            nextDirection=(AuthoredDirection+1)%32;float end=nextDirection==0?360:yaws[nextDirection];directionBlend=Mathf.InverseLerp(yaws[AuthoredDirection],end,artAngle);
            float distance=Vector3.Distance(transform.position,previous);previous=transform.position;
            if(distance>.0001f){phase+=distance/Mathf.Max(.01f,transform.lossyScale.x)*1.65f;CurrentFrame=Mathf.FloorToInt(phase)%4;}else CurrentFrame=0;
            UpdateSprite();view.transform.rotation=camera.transform.rotation;view.transform.localScale=Vector3.one*(view.sprite!=null?1.85f/view.sprite.bounds.size.y:scale);view.transform.position=transform.position-Vector3.up*.60f;
            if(UsesPaintedBody)PaintedSpriteAssets.HideMeshes(transform);
        }
    }
}

