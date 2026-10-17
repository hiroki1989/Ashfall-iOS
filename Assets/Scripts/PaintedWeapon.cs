using UnityEngine;
namespace NeonVault
{
    [DefaultExecutionOrder(400)]
    public sealed class PaintedWeapon : MonoBehaviour
    {
        SpriteRenderer view;Sprite[] portraits;AimedWeapon aimed;Transform muzzle;int cell;
        Sprite[] yawViews;int yaw;
        GunEffect visualEffect;
        Vector2[] activeGrips=gripPixels,activeMuzzles=muzzlePixels;float atlasWidth=1774,atlasHeight=887;
        // Raw atlas coordinates measured from the inspected generated original.
        static readonly Vector2[] gripPixels={new Vector2(138,82),new Vector2(708,122),new Vector2(1212,182),new Vector2(1660,283),new Vector2(137,762),new Vector2(407,737),new Vector2(838,692),new Vector2(1370,506)};
        static readonly Vector2[] muzzlePixels={new Vector2(139,331),new Vector2(321,330),new Vector2(834,183),new Vector2(1389,85),new Vector2(137,491),new Vector2(734,495),new Vector2(1276,622),new Vector2(1728,759)};
        public bool UsesPaintedWeapon=>view!=null&&view.sprite!=null;
        public int CurrentCell=>cell;
        public int AuthoredYawCount=>yawViews!=null?yawViews.Length:0;
        public int CurrentYaw=>yaw;
        public GunEffect VisualEffect=>visualEffect;
        public SpriteRenderer PaintedRenderer=>view;
        public Vector3 VisualGripPosition {get;private set;}
        public Vector3 VisualMuzzlePosition {get;private set;}
        public void Init(bool hero,Transform muzzleOrigin)
        {
            muzzle=muzzleOrigin;aimed=GetComponent<AimedWeapon>();portraits=PaintedSpriteAssets.Load("Art/KeyEquipmentAtlas",4,4,PortraitWindows());
            yawViews=PaintedSpriteAssets.Load("Art/WeaponYawAtlas-v1",4,2,YawWindows());
            view=PaintedSpriteAssets.Create(transform,"Painted equipment firearm");cell=hero?0:2;Refresh();
        }
        static Rect[] YawWindows()
        {
            float[] edges={0,277,794,1355,1774};var windows=new Rect[8];
            for(int n=0;n<8;n++){int col=n%4;windows[n]=new Rect(edges[col]/1774f,n<4?.5f:0,(edges[col+1]-edges[col])/1774f,.5f);}return windows;
        }
                static Rect[] PortraitWindows()
        {
            var windows=new Rect[16];
            for(int n=0;n<16;n++)
            {
                float x=n%4*313.5f,y=n<4?24:n<8?322:n<12?595:917,w=313.5f,h=n<4?270:n<8?255:n<12?314:337;
                if(n==8){y=567;h=350;}if(n==12){y=934;h=320;}if(n==5){x=320;w=307;}if(n==6){x=640;w=300;}
                windows[n]=new Rect(x/1254f,1-(y+h)/1254f,w/1254f,h/1254f);
            }
            return windows;
        }
        public void ConfigureEffect(GunEffect effect)
        {cell=effect==GunEffect.Scatter?1:effect==GunEffect.Ricochet?4:effect==GunEffect.Orbit?5:effect==GunEffect.Singularity?6:2;SelectFamily(effect);Refresh();}
        public void SetGun(Gun gun){if(gun==null)return;cell=ItemPortraits.GunCell(gun);SelectFamily(gun.effect);Refresh();SetAccent(gun.color);}
        void SelectFamily(GunEffect effect)
        {
            visualEffect=effect;activeGrips=gripPixels;activeMuzzles=muzzlePixels;atlasWidth=1774;atlasHeight=887;
            string name="WeaponYawAtlas-v1";float[] edges={0,277,794,1355,1774};
            if(effect==GunEffect.Orbit){name="WeaponOrbitYaw-v1";edges=new float[]{0,275,710,1345,1774};activeGrips=Points(155,65,624,149,1160,241,1630,255,155,690,461,714,921,690,1422,549);activeMuzzles=Points(153,286,327,305,749,181,1384,92,155,472,658,505,1324,632,1704,736);}
            else if(effect==GunEffect.Singularity){name="WeaponSingularityYaw-v1";edges=new float[]{0,286,806,1386,1774};activeGrips=Points(162,85,746,148,1192,261,1653,270,162,710,453,737,1007,733,1460,564);activeMuzzles=Points(161,300,459,294,885,238,1448,125,161,518,694,552,1300,675,1665,731);}
            else if(effect==GunEffect.Scatter){name="WeaponScatterYaw-v1";atlasWidth=1536;atlasHeight=1024;edges=new float[]{0,268,733,1152,1536};activeGrips=Points(158,147,562,266,978,296,1341,282,149,740,458,776,889,770,1259,683);activeMuzzles=Points(156,384,347,345,780,253,1190,146,149,560,665,610,1120,734,1447,845);}
            else if(effect==GunEffect.Ricochet){name="WeaponRicochetYaw-v1";edges=new float[]{0,443.5f,887,1330.5f,1774};activeGrips=Points(212,87,745,174,1186,201,1584,273,212,701,630,738,1062,686,1470,577);activeMuzzles=Points(212,294,521,276,927,198,1382,94,212,505,783,545,1297,628,1693,727);}
            var windows=new Rect[8];for(int n=0;n<8;n++){int col=n%4;windows[n]=new Rect(edges[col]/atlasWidth,n<4?.5f:0,(edges[col+1]-edges[col])/atlasWidth,.5f);}
            if(effect==GunEffect.Ricochet)for(int n=0;n<8;n++){var w=windows[n];windows[n]=new Rect(w.x+10/atlasWidth,w.y+10/atlasHeight,w.width-20/atlasWidth,w.height-20/atlasHeight);}
            yawViews=PaintedSpriteAssets.Load("Art/"+name,4,2,windows);
            if(yawViews==null){yawViews=PaintedSpriteAssets.Load("Art/WeaponYawAtlas-v1",4,2,YawWindows());activeGrips=gripPixels;activeMuzzles=muzzlePixels;atlasWidth=1774;atlasHeight=887;}
        }
        static Vector2[] Points(params float[] values){var points=new Vector2[8];for(int n=0;n<8;n++)points[n]=new Vector2(values[n*2],values[n*2+1]);return points;}
        public void SetAccent(Color color){if(view!=null)view.color=Color.Lerp(Color.white,color,.12f);}
        public void SynchronizeGripPosition()
        {
            if(aimed==null||aimed.grip==null)return;var actor=GetComponentInParent<PaintedActor>();var camera=Camera.main;if(actor==null||camera==null||!actor.UsesPaintedBody)return;
            Vector3 facing=aimed.Direction;Vector3 right=camera.transform.right,down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;
            float angle=Mathf.Atan2(-Vector3.Dot(facing,right),Vector3.Dot(facing,down));float size=actor.transform.lossyScale.x;
            Vector3 foot=actor.transform.position-Vector3.up*.60f;
            Vector3 hand=foot+camera.transform.up*(1.85f*.69f*size)+right*(-Mathf.Sin(angle)*.40f*size);
            hand-=aimed.RecoilDisplacement;
            Vector3 delta=hand-aimed.GripPosition;transform.position+=right*Vector3.Dot(delta,right)+camera.transform.up*Vector3.Dot(delta,camera.transform.up);
        }
        void Refresh(){if(view!=null&&portraits!=null)view.sprite=portraits[cell];}
        void LateUpdate()
        {
            if(!UsesPaintedWeapon||muzzle==null)return;var camera=Camera.main;if(camera==null)return;
            Vector3 grip=aimed!=null?aimed.GripPosition:transform.position;
            Vector3 axis=muzzle.position-grip;
            Vector2 projected=new Vector2(Vector3.Dot(axis,camera.transform.right),Vector3.Dot(axis,camera.transform.up));
            if(yawViews!=null){UpdateYaw(camera,grip,projected);PaintedSpriteAssets.HideMeshes(transform);return;}
            float length=Mathf.Max(.15f,projected.magnitude);float angle=Mathf.Atan2(projected.y,projected.x)*Mathf.Rad2Deg;
            // The weapon's right-hand grip is 32% along its painted stock-to-barrel axis.
            // Mapping that axis onto the real grip/muzzle sockets also supplies foreshortening.
            float width=length/.63f;float height=width*view.sprite.bounds.size.y/view.sprite.bounds.size.x;
            view.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,angle);
            bool left=projected.x<0;view.flipY=false;
            Vector3 up=view.transform.up,right=view.transform.right;
            Vector3 pivotOffset=right*((.32f-view.sprite.pivot.x/view.sprite.rect.width)*width)+up*(left?-.55f:.55f)*height;
            var actor=GetComponentInParent<PaintedActor>();
            bool behind=actor!=null&&actor.CurrentDirection>=10&&actor.CurrentDirection<=22;
            Vector3 depth=camera.transform.forward*(behind?.035f:-.035f);
            Vector3 planeGrip=grip;
            if(actor!=null&&actor.PaintedRenderer!=null)planeGrip+=camera.transform.forward*Vector3.Dot(actor.PaintedRenderer.transform.position-grip,camera.transform.forward);
            view.transform.position=planeGrip-pivotOffset+depth;
            // Compensate parent rotation/scale: width is specified in world units.
            Vector3 parentScale=transform.lossyScale;
            view.transform.localScale=new Vector3(width/view.sprite.bounds.size.x/Mathf.Max(.001f,parentScale.x),(left?-1:1)*height/view.sprite.bounds.size.y/Mathf.Max(.001f,parentScale.y),1);
            VisualGripPosition=planeGrip+depth;VisualMuzzlePosition=planeGrip+camera.transform.right*projected.x+camera.transform.up*projected.y+depth;
            PaintedSpriteAssets.HideMeshes(transform);
        }
        void UpdateYaw(Camera camera,Vector3 grip,Vector2 projected)
        {
            var actor=GetComponentInParent<PaintedActor>();
            Vector3 facing=aimed!=null?aimed.Direction:transform.forward;
            Vector3 down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;
            float angle=Mathf.Repeat(Mathf.Atan2(-Vector3.Dot(facing,camera.transform.right),Vector3.Dot(facing,down))*Mathf.Rad2Deg,360);
            yaw=Mathf.RoundToInt(angle/45)%8;view.sprite=yawViews[yaw];
            var tex=view.sprite.texture;var rect=view.sprite.rect;
            // Anchors are in full original-image coordinates and remain correct
            // after alpha trimming and Unity texture resize.
            Vector2 gp=new Vector2(activeGrips[yaw].x/atlasWidth*tex.width,(1-activeGrips[yaw].y/atlasHeight)*tex.height);
            Vector2 mp=new Vector2(activeMuzzles[yaw].x/atlasWidth*tex.width,(1-activeMuzzles[yaw].y/atlasHeight)*tex.height);
            Vector2 sourceAxis=(mp-gp)/100;
            float rotation=(Mathf.Atan2(projected.y,projected.x)-Mathf.Atan2(sourceAxis.y,sourceAxis.x))*Mathf.Rad2Deg;
            float worldScale=Mathf.Max(.08f,projected.magnitude)/Mathf.Max(.01f,sourceAxis.magnitude);
            view.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,rotation);view.flipY=false;
            Vector2 localGrip=(gp-rect.position-view.sprite.pivot)/100;
            Vector3 planeGrip=grip;
            if(actor!=null&&actor.PaintedRenderer!=null)planeGrip+=camera.transform.forward*Vector3.Dot(actor.PaintedRenderer.transform.position-grip,camera.transform.forward);
            bool behind=Vector3.Dot(facing,down)<0;Vector3 depth=camera.transform.forward*(behind?.035f:-.035f);
            view.transform.position=planeGrip+depth-(view.transform.right*localGrip.x+view.transform.up*localGrip.y)*worldScale;
            Vector3 parentScale=transform.lossyScale;view.transform.localScale=new Vector3(worldScale/Mathf.Max(.001f,parentScale.x),worldScale/Mathf.Max(.001f,parentScale.y),1);
            VisualGripPosition=planeGrip+depth;VisualMuzzlePosition=VisualGripPosition+camera.transform.right*projected.x+camera.transform.up*projected.y;
        }
    }
}


