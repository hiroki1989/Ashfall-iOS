using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Finished painted poses are kept intact. No limb IK or source recolouring is
    // applied here: every socket is measured on the same displayed source cell.
    public sealed class ApprovedFieldkitPlayer2D : MonoBehaviour
    {
        public const string ResourceRoot="Art/ApprovedFieldkit/";
        [Serializable] public sealed class PoseTable
        {
            public int version=1,visualVariant=4;
            public string visualSet="trail-warden";
            public float height=2.2f;
            public PoseCell[] cells;
            public LowerCell[] lowerCells;
        }
        [Serializable] public class PoseCell
        {
            public string pose;
            public int direction;
            public string texture;
            public int columns=4,rows=2,cellIndex=-1;
            public int sourceWidth,sourceHeight;
            public SourceRect rect;
            public Vector2 pivot,muzzle,grip,support,stock,barrelStart;
            public float bodyHeightPixels,beltY;
            public Vector2 beltCenter;
            public Vector2 hemAttach;
            public NeutralMeasurements neutralMeasurements;
            public Vector2[] upperPolygon;
            public string upperTexture,upperBaseTexture;
            public string weaponlessTexture,baseWeaponlessTexture,handsTexture;
            public Vector2[] weaponPolygon;
            public Polygon[] weaponPolygons,handPolygons;
            public bool weaponOccluded,handsOccluded;
            public int[] occludedSlots;
            public string baseTexture;
            public EquipmentLayer[] overlays;
        }
        [Serializable] public sealed class LowerCell : PoseCell
        {
            public int frame;
            public Vector2 hip,rightKnee,leftKnee,rightFoot,leftFoot;
            public Vector2[] rightLegPolygon,leftLegPolygon,pelvisPolygon;
            public string rightLegTexture,leftLegTexture;
            public Vector2 waistAttach;
            public RigidPart[] rigidParts;
            public RigidPart pelvisPart;
        }
        [Serializable] public sealed class RigidPart
        {
            public string name,texture;public SourceRect rect;
            public Vector2 start,end,pivot;
            public Vector2 rightHip,leftHip;
            public Vector2 sole;
            public float bodyHeightPixels;
            public float visibleWidthPixels;
            public float midCrossSectionPixels;
        }
        [Serializable] public sealed class NeutralMeasurements
        {
            public Vector2 rightHip,rightKnee,rightAnkle,rightToe,rightSole,leftHip,leftKnee,leftAnkle,leftToe,leftSole;
            public float rightBootWidth,leftBootWidth;
            public float rightBootWidthPixels,leftBootWidthPixels,rightThighWidthPixels,leftThighWidthPixels,rightShinWidthPixels,leftShinWidthPixels;
        }
        sealed class RigidPiece {public Sprite sprite;public Vector2 start,end,rightHip,leftHip,sole;public float pixelScale,crossScale,visibleWidthPixels,midCrossSectionPixels;public SpriteRenderer view;public Transform mount;public Mesh mesh;public MeshRenderer renderer;}
        [Serializable] public sealed class SourceRect {public int x,y,width,height;}
        [Serializable] public sealed class EquipmentLayer
        {
            public int slot,visualVariant;
            public string texture;
            public string upperTexture;
            public int columns=4,rows=2,cellIndex=-1;
            public Vector2[] polygon;
            public Polygon[] polygons;
        }
        [Serializable] public sealed class Polygon {public Vector2[] points;}
        sealed class Frame
        {
            public Sprite sprite,baseSprite,upperSprite,upperBaseSprite,weaponlessSprite,baseWeaponlessSprite,handsSprite,upperWeaponlessSprite,upperBaseWeaponlessSprite,upperHandsSprite;public PoseCell cell;public float pixelsToWorld;
            public Sprite basePatchSprite,upperBasePatchSprite,weaponlessPatchSprite,upperWeaponlessPatchSprite;
            public Sprite externalBasePatchSprite,upperExternalBasePatchSprite,bareWeaponlessPatchSprite,upperBareWeaponlessPatchSprite,bareHandsSprite,upperBareHandsSprite;
            public Sprite rightLegSprite,leftLegSprite,pelvisSprite;
            public readonly Dictionary<string,RigidPiece> rigid=new Dictionary<string,RigidPiece>();
            public readonly Dictionary<int,Sprite> equipment=new Dictionary<int,Sprite>();
            public readonly Dictionary<int,Sprite> upperEquipment=new Dictionary<int,Sprite>();
            public readonly Dictionary<int,Vector2[][]> equipmentPolygons=new Dictionary<int,Vector2[][]>();
            public readonly Dictionary<int,Sprite> externalEquipment=new Dictionary<int,Sprite>(),upperExternalEquipment=new Dictionary<int,Sprite>();
            public readonly Dictionary<int,string> equipmentErrors=new Dictionary<int,string>();
        }
        // The upper painting never enters this mesh. Lower-body source drawings
        // supply real gait poses; measured knees and feet interpolate the lower
        // silhouette between them without cross-fading two sets of legs.
        sealed class LowerSurface
        {
            const int Subdivisions=10;
            public readonly MeshRenderer renderer;
            readonly Mesh mesh;
            Vector3[] vertices,restVertices;
            Sprite previousSprite;
            public LowerSurface(Transform parent,Material material,string name,int order)
            {
                var go=new GameObject(name);go.transform.SetParent(parent,false);
                var filter=go.AddComponent<MeshFilter>();renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
                mesh=new Mesh{name=name+" interpolation mesh"};mesh.MarkDynamic();filter.sharedMesh=mesh;
            }
            void SetSource(Sprite sprite)
            {
                var source=sprite.vertices;var sourceUv=sprite.uv;var triangles=sprite.triangles;
                var points=new List<Vector3>();var uv=new List<Vector2>();
                for(int at=0;at<triangles.Length;at+=3)
                {
                    int a=triangles[at],b=triangles[at+1],c=triangles[at+2];
                    for(int u=0;u<Subdivisions;u++)for(int v=0;v<Subdivisions-u;v++)
                    {
                        AddVertex(source,sourceUv,a,b,c,u,v,points,uv);AddVertex(source,sourceUv,a,b,c,u+1,v,points,uv);AddVertex(source,sourceUv,a,b,c,u,v+1,points,uv);
                        if(u+v<Subdivisions-1){AddVertex(source,sourceUv,a,b,c,u+1,v,points,uv);AddVertex(source,sourceUv,a,b,c,u+1,v+1,points,uv);AddVertex(source,sourceUv,a,b,c,u,v+1,points,uv);}
                    }
                }
                restVertices=points.ToArray();vertices=new Vector3[restVertices.Length];var indices=new int[vertices.Length];var colours=new Color[vertices.Length];
                for(int i=0;i<indices.Length;i++){indices[i]=i;colours[i]=Color.white;}
                mesh.Clear();mesh.indexFormat=vertices.Length>ushort.MaxValue?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.vertices=restVertices;mesh.SetUVs(0,uv);mesh.colors=colours;mesh.triangles=indices;
                var block=new MaterialPropertyBlock();block.SetTexture("_MainTex",sprite.texture);block.SetColor("_Color",Color.white);renderer.SetPropertyBlock(block);previousSprite=sprite;
            }
            static void AddVertex(Vector2[] points,Vector2[] uv,int a,int b,int c,int u,int v,List<Vector3> output,List<Vector2> outputUv)
            {
                float x=u/(float)Subdivisions,y=v/(float)Subdivisions,z=1-x-y;
                output.Add(points[a]*z+points[b]*x+points[c]*y);outputUv.Add(uv[a]*z+uv[b]*x+uv[c]*y);
            }
            public void Render(Frame frame,Frame next,float blend,Sprite sprite,Transform anchor,Vector2[] corrected=null,int fixedLeg=-1)
            {
                var cell=(LowerCell)frame.cell;var following=(LowerCell)next.cell;
                float ratio=next.pixelsToWorld/frame.pixelsToWorld;
                Vector2[] current={cell.rightKnee,cell.leftKnee,cell.rightFoot,cell.leftFoot};
                Vector2[] target={following.rightKnee,following.leftKnee,following.rightFoot,following.leftFoot};
                bool measured=cell.rightKnee!=Vector2.zero&&cell.leftKnee!=Vector2.zero&&following.rightKnee!=Vector2.zero&&following.leftKnee!=Vector2.zero;
                if(previousSprite!=sprite)SetSource(sprite);
                float height=frame.sprite.rect.height;
                for(int index=0;index<vertices.Length;index++)
                {
                    Vector2 pixel=new Vector2(restVertices[index].x+frame.cell.pivot.x*frame.sprite.rect.width,height-restVertices[index].y-frame.cell.pivot.y*height),offset=Vector2.zero;
                    if(corrected!=null)
                    {
                        vertices[index]=LocalPoint(frame,WarpLegPixel(cell,pixel,corrected,fixedLeg));continue;
                    }
                    if(measured&&(blend>0||corrected!=null))
                    {
                        // Hip remains fixed at the belt seam. Inverse-distance
                        // skinning is restricted to the completed leg painting.
                        float hipWeight=4/(16+(pixel-cell.hip).sqrMagnitude),total=hipWeight;
                        for(int joint=0;joint<4;joint++)
                        {
                            float weight=1/(16+(pixel-current[joint]).sqrMagnitude);
                            Vector2 delta=corrected!=null?corrected[joint]-current[joint]:(target[joint]-following.hip)*ratio+cell.hip-current[joint];
                            offset+=delta*weight;total+=weight;
                        }
                        float belowHip=Mathf.Clamp01((pixel.y-cell.hip.y)/Mathf.Max(1,height*.16f));
                        offset=offset/Mathf.Max(.000001f,total)*((corrected!=null?1:blend)*Mathf.SmoothStep(0,1,belowHip));
                    }
                    vertices[index]=LocalPoint(frame,pixel+offset);
                }
                mesh.vertices=vertices;
                mesh.RecalculateBounds();renderer.transform.SetPositionAndRotation(anchor.position,anchor.rotation);renderer.transform.localScale=anchor.localScale;renderer.enabled=true;
            }
            public void Dispose(){if(mesh!=null)UnityEngine.Object.Destroy(mesh);}
            static Vector2 WarpLegPixel(LowerCell cell,Vector2 pixel,Vector2[] target,int fixedLeg)
            {
                Vector2[] knees={cell.rightKnee,cell.leftKnee},feet={cell.rightFoot,cell.leftFoot};
                float best=float.MaxValue;Vector2 result=pixel;
                for(int leg=0;leg<2;leg++)
                {
                    if(fixedLeg>=0&&leg!=fixedLeg)continue;
                    Vector2 hip=LegHip(cell,leg);
                    Vector2[] source={hip,knees[leg],feet[leg]},destination={hip,target[leg],target[leg+2]};
                    for(int bone=0;bone<2;bone++)
                    {
                        Vector2 a=source[bone],b=source[bone+1],axis=b-a;
                        float t=Mathf.Clamp01(Vector2.Dot(pixel-a,axis)/Mathf.Max(.001f,axis.sqrMagnitude));
                        float distance=(pixel-(a+axis*t)).sqrMagnitude;
                        if(distance>=best)continue;best=distance;
                        Vector2 newAxis=destination[bone+1]-destination[bone];
                        float angle=Mathf.Atan2(newAxis.y,newAxis.x)-Mathf.Atan2(axis.y,axis.x),co=Mathf.Cos(angle),si=Mathf.Sin(angle);
                        Vector2 delta=pixel-a;float scale=newAxis.magnitude/Mathf.Max(1,axis.magnitude);
                        result=destination[bone]+new Vector2(delta.x*co-delta.y*si,delta.x*si+delta.y*co)*scale;
                    }
                }
                // Preserve the original waistband and pelvis pixels as one
                // continuous bridge; only pixels below the crotch enter bones.
                float transition=Mathf.SmoothStep(0,1,Mathf.Clamp01((pixel.y-cell.hip.y)/Mathf.Max(1,cell.bodyHeightPixels*.15f)));
                return Vector2.Lerp(pixel,result,transition);
            }
        }
        readonly Dictionary<string,Frame> frames=new Dictionary<string,Frame>();
        readonly List<Sprite> ownedSprites=new List<Sprite>();
        PoseTable table;
        SpriteRenderer painted;
        SpriteRenderer lowerPainted;
        SpriteRenderer paintedHands;
        SpriteRenderer clothingPatch,weaponPatch;
        readonly SpriteRenderer[] equipmentViews=new SpriteRenderer[5];
        readonly SpriteRenderer[] lowerEquipmentViews=new SpriteRenderer[5];
        LowerSurface lowerSurface;
        LowerSurface lowerBasePatchSurface;
        LowerSurface rightLegSurface,leftLegSurface,pelvisSurface;
        readonly LowerSurface[] lowerEquipmentSurfaces=new LowerSurface[5];
        readonly List<Frame>[] lowerFrames={new List<Frame>(),new List<Frame>(),new List<Frame>(),new List<Frame>(),new List<Frame>(),new List<Frame>(),new List<Frame>(),new List<Frame>()};
        Material material;
        bool attempted;
        public bool Ready {get;private set;}
        public bool Active=>painted!=null&&painted.enabled;
        public int VisualVariant=>table!=null?table.visualVariant:4;
        public string CurrentPose {get;private set;}
        public int CurrentDirection {get;private set;}
        public SpriteRenderer PaintedRenderer=>painted;
        public Vector3 Muzzle {get;private set;}
        public Vector3 Grip {get;private set;}
        public Vector3 Support {get;private set;}
        public Vector3 Stock {get;private set;}
        public Vector3 BarrelStart {get;private set;}
        public Vector3 SourceBarrelAxis=>(Muzzle-BarrelStart).normalized;
        public int LoadedPoseCount=>frames.Count;
        public bool EquipmentLayersReady {get;private set;}
        public bool WeaponlessReady {get;private set;}
        public bool UsesExternalWeapon {get;private set;}
        public int UnsupportedEquipmentCount {get;private set;}
        public bool UsesCompositeEquipment {get;private set;}
        public bool EquipmentSlotVisible(int slot)=>slot>=0&&slot<5&&(!UsesCompositeEquipment||equipmentViews[slot]!=null&&equipmentViews[slot].enabled||lowerEquipmentSurfaces[slot]!=null&&lowerEquipmentSurfaces[slot].renderer.enabled);
        public bool EquipmentSlotOccluded(int slot)=>frames.TryGetValue(Key(CurrentPose,CurrentDirection),out var frame)&&SlotOccluded(frame,slot);
        static bool SlotOccluded(Frame frame,int slot)=>frame.cell.occludedSlots!=null&&Array.IndexOf(frame.cell.occludedSlots,slot)>=0;
        public bool LocomotionReady {get;private set;}
        public bool LocomotionActive {get;private set;}
        public int CurrentLowerFrame {get;private set;}
        public int CurrentLowerDirection {get;private set;}
        public MeshRenderer LowerRenderer=>pelvisSurface!=null&&pelvisSurface.renderer.enabled?pelvisSurface.renderer:rightLegSurface!=null&&rightLegSurface.renderer.enabled?rightLegSurface.renderer:lowerSurface!=null?lowerSurface.renderer:null;
        public Vector3 RightFoot {get;private set;}
        public Vector3 LeftFoot {get;private set;}
        public float LowerInterpolation {get;private set;}
        public bool CorrectedStrideActive {get;private set;}
        public bool ReviewRigidNeutral {get;set;}
        public Vector3 RightGroundContact {get;private set;}
        public Vector3 LeftGroundContact {get;private set;}
        public Vector3 GroundReference {get;private set;}
        public string RigidPoseFingerprint {get;private set;}
        public bool NeutralMeasurementsReady=>frames.TryGetValue(Key("low",1),out var source)&&source.cell.neutralMeasurements!=null;
        public string NeutralCalibrationCsv()
        {
            var source=frames[Key("low",1)];var measured=source.cell.neutralMeasurements;var frame=lowerFrames[1][0];var csv=new System.Text.StringBuilder("part,targetAxisMetres,actualAxisMetres,targetWidthMetres,actualWidthMetres,widthRatio\n");
            foreach(string side in new[]{"right","left"})foreach(string segment in new[]{"Thigh","Shin","Boot"})
            {
                bool right=side=="right";var part=frame.rigid[side+segment];Vector2 a=segment=="Thigh"?(right?measured.rightHip:measured.leftHip):segment=="Shin"?(right?measured.rightKnee:measured.leftKnee):(right?measured.rightAnkle:measured.leftAnkle);
                Vector2 b=segment=="Thigh"?(right?measured.rightKnee:measured.leftKnee):segment=="Shin"?(right?measured.rightAnkle:measured.leftAnkle):(right?measured.rightSole:measured.leftSole);
                float target=Vector2.Distance(a,b)*source.pixelsToWorld,actual=Vector2.Distance(part.start,segment=="Boot"?part.sole:part.end)*part.pixelScale;
                float widthPixels=segment=="Thigh"?(right?measured.rightThighWidthPixels:measured.leftThighWidthPixels):segment=="Shin"?(right?measured.rightShinWidthPixels:measured.leftShinWidthPixels):(right?measured.rightBootWidthPixels:measured.leftBootWidthPixels);
                Vector2 normal=new Vector2(-(part.end-part.start).y,(part.end-part.start).x).normalized;float min=float.MaxValue,max=float.MinValue;foreach(var vertex in part.sprite.vertices){float p=Vector2.Dot(vertex,normal);min=Mathf.Min(min,p);max=Mathf.Max(max,p);}
                float expectedWidth=widthPixels*source.pixelsToWorld,actualWidth=part.visibleWidthPixels>0?part.visibleWidthPixels*(part.crossScale>0?part.crossScale:part.pixelScale):float.NaN;var culture=System.Globalization.CultureInfo.InvariantCulture;
                csv.AppendLine(side+segment+","+target.ToString("F5",culture)+","+actual.ToString("F5",culture)+","+expectedWidth.ToString("F5",culture)+","+actualWidth.ToString("F5",culture)+","+(actualWidth/Mathf.Max(.0001f,expectedWidth)).ToString("F4",culture));
            }
            return csv.ToString();
        }
        public bool NeutralWidthsMatch()
        {
            var source=frames[Key("low",1)];var m=source.cell.neutralMeasurements;var frame=lowerFrames[1][0];
            foreach(string side in new[]{"right","left"})foreach(string segment in new[]{"Thigh","Shin","Boot"})
            {
                bool right=side=="right";var part=frame.rigid[side+segment];float target=segment=="Thigh"?(right?m.rightThighWidthPixels:m.leftThighWidthPixels):segment=="Shin"?(right?m.rightShinWidthPixels:m.leftShinWidthPixels):(right?m.rightBootWidthPixels:m.leftBootWidthPixels);
                float expected=target*source.pixelsToWorld,actual=part.visibleWidthPixels*part.crossScale;
                if(expected<=0||part.visibleWidthPixels<=0||Mathf.Abs(actual/expected-1)>.05f)return false;
            }
            return true;
        }
        public bool MeasureNeutralGpu(Camera camera,string folder)
        {
            var source=frames[Key("low",1)];var m=source.cell.neutralMeasurements;var frame=lowerFrames[1][0];bool valid=true;
            var csv=new System.Text.StringBuilder("part,targetCrossSectionPixels,renderedCrossSectionPixels,ratio\n");
            var obj=new GameObject("Neutral isolated alpha measurement camera");var probe=obj.AddComponent<Camera>();probe.CopyFrom(camera);probe.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);probe.clearFlags=CameraClearFlags.SolidColor;probe.backgroundColor=Color.clear;probe.cullingMask=1<<31;probe.enabled=false;
            var target=new RenderTexture(512,640,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(512,640,TextureFormat.RGBA32,false);var active=RenderTexture.active;probe.targetTexture=target;
            try
            {
                foreach(string side in new[]{"right","left"})foreach(string segment in new[]{"Thigh","Shin","Boot"})
                {
                    bool right=side=="right";var part=frame.rigid[side+segment];int layer=part.renderer.gameObject.layer;part.renderer.gameObject.layer=31;
                    try
                    {
                        probe.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,512,640),0,0);pixels.Apply(false);
                        Vector3 center=part.view.transform.TransformPoint(Vector2.Lerp(part.start,segment=="Boot"?part.sole:part.end,.5f));var screen=probe.WorldToViewportPoint(center);int y=Mathf.Clamp(Mathf.RoundToInt(screen.y*640),0,639);int min=512,max=-1;
                        for(int x=0;x<512;x++)if(pixels.GetPixel(x,y).a>.5f){min=Mathf.Min(min,x);max=Mathf.Max(max,x);}
                        float actual=max>=min?max-min+1:0;
                        float width=segment=="Thigh"?(right?m.rightThighWidthPixels:m.leftThighWidthPixels):segment=="Shin"?(right?m.rightShinWidthPixels:m.leftShinWidthPixels):(right?m.rightBootWidthPixels:m.leftBootWidthPixels);
                        float expected=width*source.pixelsToWorld*(640/(camera.orthographicSize*2)),ratio=actual/Mathf.Max(.0001f,expected);valid&=Mathf.Abs(ratio-1)<=.05f;
                        var culture=System.Globalization.CultureInfo.InvariantCulture;csv.AppendLine(side+segment+","+expected.ToString("F3",culture)+","+actual.ToString("F3",culture)+","+ratio.ToString("F4",culture));
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"neutral-alpha-"+side+segment+".png"),pixels.EncodeToPNG());
                    }
                    finally{part.renderer.gameObject.layer=layer;}
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"approved-d1-neutral-gpu-width.csv"),csv.ToString());
            }
            finally{RenderTexture.active=active;target.Release();Destroy(target);Destroy(pixels);Destroy(obj);}
            return valid;
        }
        public float WaistJoinError {get;private set;}
        public bool OwnsRenderer(Renderer renderer)
        {
            foreach(var sequence in lowerFrames)foreach(var frame in sequence)foreach(var part in frame.rigid.Values)if(part.renderer==renderer)return true;
            if(lowerSurface!=null&&renderer==lowerSurface.renderer)return true;
            if(lowerBasePatchSurface!=null&&renderer==lowerBasePatchSurface.renderer)return true;
            if(rightLegSurface!=null&&renderer==rightLegSurface.renderer||leftLegSurface!=null&&renderer==leftLegSurface.renderer||pelvisSurface!=null&&renderer==pelvisSurface.renderer)return true;
            foreach(var surface in lowerEquipmentSurfaces)if(surface!=null&&renderer==surface.renderer)return true;return false;
        }
        public string ValidationError {get;private set;}
        public string LocomotionValidationError {get;private set;}
        public string GeometryValidationError {get;private set;}
        public string EquipmentValidationError {get;private set;}
        public string RenderValidationError {get;private set;}

        public bool Initialize()
        {
            if(attempted)return Ready;attempted=true;
            var json=Resources.Load<TextAsset>(ResourceRoot+"Poses-v1");
            if(json==null){ValidationError="Pose metadata is not installed";return false;}
            try {table=JsonUtility.FromJson<PoseTable>(json.text);}
            catch(Exception e){ValidationError="Invalid approved pose metadata: "+e.Message;return false;}
            if(table==null||table.version!=1||table.height<=0||table.cells==null){ValidationError="Unsupported approved pose metadata";return false;}
            if(string.IsNullOrEmpty(table.visualSet))table.visualSet="trail-warden";
            var neutralJson=Resources.Load<TextAsset>(ResourceRoot+"NeutralD1-v1");
            if(neutralJson!=null)foreach(var cell in table.cells)if(cell.pose=="low"&&cell.direction==1)cell.neutralMeasurements=JsonUtility.FromJson<NeutralMeasurements>(neutralJson.text);
            string[] poses={"low","aim","compact","compact-low"};
            string[] paths={"LowReady8-v1","Aim8-v1","CompactAim8-v1","CompactLowReady8-v1"};
            for(int p=0;p<poses.Length;p++)
            {
                for(int d=0;d<8;d++)
                {
                    PoseCell cell=null;
                    foreach(var candidate in table.cells)if(candidate!=null&&candidate.pose==poses[p]&&candidate.direction==d){cell=candidate;break;}
                    if(cell==null&&p==3)continue;
                    if(cell==null){ValidationError="Missing pose cell: "+poses[p]+" / "+d;return false;}
                    string path=string.IsNullOrEmpty(cell.texture)?paths[p]:cell.texture;
                    var texture=Resources.Load<Texture2D>(ResourceRoot+path);
                    if(texture!=null&&cell.sourceWidth>0&&cell.sourceHeight>0&&(texture.width!=cell.sourceWidth||texture.height!=cell.sourceHeight))
                    {ValidationError="Imported texture dimensions differ from measured source: "+path+" actual="+texture.width+"x"+texture.height+" source="+cell.sourceWidth+"x"+cell.sourceHeight;return false;}
                    int cols=cell.columns>0?cell.columns:4,rows=cell.rows>0?cell.rows:2,index=cell.cellIndex<0?d:cell.cellIndex;
                    if(texture==null||texture.width%cols!=0||texture.height%rows!=0||index<0||index>=cols*rows){ValidationError="Missing or invalid pose atlas layout: "+path;return false;}
                    int w=texture.width/cols,h=texture.height/rows;
                    Rect sourceRect=new Rect((index%cols)*w,(rows-1-index/cols)*h,w,h);
                    if(cell.rect!=null&&cell.rect.width>0&&cell.rect.height>0)
                    {
                        var crop=cell.rect;
                        if(crop.x<0||crop.y<0||crop.x+crop.width>texture.width||crop.y+crop.height>texture.height){ValidationError="Pose rectangle exceeds texture: "+path+" / "+d;return false;}
                        w=crop.width;h=crop.height;sourceRect=new Rect(crop.x,texture.height-crop.y-crop.height,w,h);
                    }
                    if(cell.bodyHeightPixels<=0||cell.bodyHeightPixels>h||cell.pivot.x<0||cell.pivot.x>1||cell.pivot.y<0||cell.pivot.y>1||!Inside(cell.muzzle,w,h)||!Inside(cell.grip,w,h)||!Inside(cell.support,w,h)||!Inside(cell.stock,w,h)||Vector2.Distance(cell.muzzle,cell.stock)<2)
                    {ValidationError="Missing or invalid source anchors: "+poses[p]+" / "+d;return false;}
                    var sprite=Sprite.Create(texture,sourceRect,cell.pivot,1,0,SpriteMeshType.FullRect);
                    sprite.name="Approved Trail Warden / "+poses[p]+" / "+d;ownedSprites.Add(sprite);
                    var frame=new Frame{sprite=sprite,cell=cell,pixelsToWorld=table.height/cell.bodyHeightPixels};
                    if(!string.IsNullOrEmpty(cell.baseTexture))frame.baseSprite=LoadLayer(cell.baseTexture,cols,rows,index,frame);
                    if(!string.IsNullOrEmpty(cell.upperTexture))frame.upperSprite=LoadLayer(cell.upperTexture,cols,rows,index,frame);
                    if(!string.IsNullOrEmpty(cell.upperBaseTexture))frame.upperBaseSprite=LoadLayer(cell.upperBaseTexture,cols,rows,index,frame);
                    frame.weaponlessSprite=LoadLayer(cell.weaponlessTexture,cols,rows,index,frame);
                    frame.baseWeaponlessSprite=LoadLayer(cell.baseWeaponlessTexture,cols,rows,index,frame);
                    frame.handsSprite=LoadLayer(cell.handsTexture,cols,rows,index,frame);
                    if(cell.overlays!=null)foreach(var layer in cell.overlays)
                    {
                        if(layer==null||layer.slot<0||layer.slot>=5)continue;
                        var overlay=LoadEquipmentLayer(layer,frame,layer.cellIndex<0?d:layer.cellIndex);
                        if(overlay!=null)frame.equipment[layer.visualVariant*5+layer.slot]=overlay;
                        else frame.equipmentErrors[layer.visualVariant*5+layer.slot]=GeometryValidationError??EquipmentValidationError??"mask could not be triangulated or lies outside its source rectangle";
                        var upper=LoadLayer(layer.upperTexture,layer.columns,layer.rows,layer.cellIndex<0?d:layer.cellIndex,frame);
                        if(upper!=null)frame.upperEquipment[layer.visualVariant*5+layer.slot]=upper;
                    }
                    ComposePreservedLayers(frame);
                    if(cell.upperPolygon!=null&&cell.upperPolygon.Length>=3)
                    {
                        var exactUpper=CreateUpperPolygon(frame.sprite,cell.upperPolygon);
                        if(exactUpper!=null)frame.upperSprite=exactUpper;
                        if(frame.baseSprite!=null)frame.upperBaseSprite=CreateUpperPolygon(frame.baseSprite,cell.upperPolygon);
                        if(frame.weaponlessSprite!=null)frame.upperWeaponlessSprite=CreateUpperPolygon(frame.weaponlessSprite,cell.upperPolygon);
                        if(frame.baseWeaponlessSprite!=null)frame.upperBaseWeaponlessSprite=CreateUpperPolygon(frame.baseWeaponlessSprite,cell.upperPolygon);
                        if(frame.handsSprite!=null)frame.upperHandsSprite=CreateUpperPolygon(frame.handsSprite,cell.upperPolygon);
                        if(frame.basePatchSprite!=null)frame.upperBasePatchSprite=CreateUpperPolygon(frame.basePatchSprite,cell.upperPolygon);
                        if(frame.weaponlessPatchSprite!=null)frame.upperWeaponlessPatchSprite=CreateUpperPolygon(frame.weaponlessPatchSprite,cell.upperPolygon);
                        if(frame.externalBasePatchSprite!=null)frame.upperExternalBasePatchSprite=CreateUpperPolygon(frame.externalBasePatchSprite,cell.upperPolygon);
                        if(frame.bareWeaponlessPatchSprite!=null)frame.upperBareWeaponlessPatchSprite=CreateUpperPolygon(frame.bareWeaponlessPatchSprite,cell.upperPolygon);
                        if(frame.bareHandsSprite!=null)frame.upperBareHandsSprite=CreateUpperPolygon(frame.bareHandsSprite,cell.upperPolygon);
                        foreach(var layer in frame.equipment)
                        {
                            var exactLayer=frame.equipmentPolygons.TryGetValue(layer.Key,out var regions)?CreateMaskedSprite(layer.Value,regions,cell.upperPolygon):CreateUpperPolygon(layer.Value,cell.upperPolygon);
                            if(exactLayer!=null)frame.upperEquipment[layer.Key]=exactLayer;
                        }
                        foreach(var layer in frame.externalEquipment){var upper=CreateUpperPolygon(layer.Value,cell.upperPolygon);if(upper!=null)frame.upperExternalEquipment[layer.Key]=upper;}
                    }
                    frames.Add(Key(poses[p],d),frame);
                }
            }
            var go=new GameObject("Approved Trail Warden finished pose");go.transform.SetParent(transform,false);
            painted=go.AddComponent<SpriteRenderer>();
            var shader=Shader.Find("Sprites/Default");
            material=new Material(shader){name="Approved painted source colours"};painted.sharedMaterial=material;painted.sortingOrder=100;painted.enabled=false;
            var handsObject=new GameObject("Approved source hand occlusion");handsObject.transform.SetParent(go.transform,false);
            paintedHands=handsObject.AddComponent<SpriteRenderer>();paintedHands.sharedMaterial=material;paintedHands.sortingOrder=120;paintedHands.enabled=false;
            var clothingObject=new GameObject("Approved original armor-hole clothing patches");clothingObject.transform.SetParent(go.transform,false);clothingPatch=clothingObject.AddComponent<SpriteRenderer>();clothingPatch.sharedMaterial=material;clothingPatch.sortingOrder=98;clothingPatch.enabled=false;
            var patchObject=new GameObject("Approved original weapon-hole clothing patch");patchObject.transform.SetParent(go.transform,false);weaponPatch=patchObject.AddComponent<SpriteRenderer>();weaponPatch.sharedMaterial=material;weaponPatch.sortingOrder=97;weaponPatch.enabled=false;
            var lowerObject=new GameObject("Approved painted locomotion lower body");lowerObject.transform.SetParent(transform,false);
            lowerPainted=lowerObject.AddComponent<SpriteRenderer>();lowerPainted.sharedMaterial=material;lowerPainted.sortingOrder=90;lowerPainted.enabled=false;
            lowerSurface=new LowerSurface(transform,material,"Approved continuous lower painting",90);
            lowerBasePatchSurface=new LowerSurface(transform,material,"Approved continuous lower clothing patch",89);
            rightLegSurface=new LowerSurface(transform,material,"Approved isolated right leg",92);leftLegSurface=new LowerSurface(transform,material,"Approved isolated left leg",91);pelvisSurface=new LowerSurface(transform,material,"Approved fixed pelvis bridge",93);
            for(int slot=0;slot<5;slot++)
            {
                var layerObject=new GameObject("Approved equipment slot / "+slot);layerObject.transform.SetParent(go.transform,false);
                var view=layerObject.AddComponent<SpriteRenderer>();view.sharedMaterial=material;view.sortingOrder=101+slot;view.enabled=false;equipmentViews[slot]=view;
                var lowerLayer=new GameObject("Approved lower equipment slot / "+slot);lowerLayer.transform.SetParent(lowerObject.transform,false);
                var lowerView=lowerLayer.AddComponent<SpriteRenderer>();lowerView.sharedMaterial=material;lowerView.sortingOrder=91+slot;lowerView.enabled=false;lowerEquipmentViews[slot]=lowerView;
                lowerEquipmentSurfaces[slot]=new LowerSurface(transform,material,"Approved continuous lower slot / "+slot,91+slot);
            }
            LoadLocomotion();
            EquipmentLayersReady=true;foreach(var frame in frames.Values)
            {
                bool valid=frame.baseSprite!=null&&frame.basePatchSprite!=null;EquipmentLayersReady&=valid;
                if(!valid&&EquipmentValidationError==null)EquipmentValidationError="Missing base or clothing patch: "+Key(frame.cell.pose,frame.cell.direction)+"; measured slot masks="+frame.equipmentPolygons.Count+"; geometry="+GeometryValidationError;
            }
            WeaponlessReady=true;foreach(var frame in frames.Values)WeaponlessReady&=frame.weaponlessSprite!=null&&(frame.handsSprite!=null||frame.cell.handsOccluded)&&(frame.weaponlessPatchSprite!=null||frame.cell.weaponOccluded);
            ValidationError=null;Ready=true;return true;
        }
        void LoadLocomotion()
        {
            if(table.lowerCells==null){LocomotionValidationError="Painted gait metadata is not installed";return;}
            foreach(var cell in table.lowerCells)
            {
                if(cell==null||cell.direction<0||cell.direction>=8||cell.frame<0||cell.bodyHeightPixels<=0||string.IsNullOrEmpty(cell.texture))continue;
                var texture=Resources.Load<Texture2D>(ResourceRoot+cell.texture);if(texture==null)continue;
                if(cell.sourceWidth>0&&cell.sourceHeight>0&&(texture.width!=cell.sourceWidth||texture.height!=cell.sourceHeight))continue;
                int cols=Mathf.Max(1,cell.columns),rows=Mathf.Max(1,cell.rows),index=cell.cellIndex<0?cell.frame:cell.cellIndex;
                Rect rect;
                if(cell.rect!=null&&cell.rect.width>0&&cell.rect.height>0)rect=new Rect(cell.rect.x,texture.height-cell.rect.y-cell.rect.height,cell.rect.width,cell.rect.height);
                else {int w=texture.width/cols,h=texture.height/rows;if(index>=cols*rows)continue;rect=new Rect(index%cols*w,(rows-1-index/cols)*h,w,h);}
                if(rect.xMin<0||rect.yMin<0||rect.xMax>texture.width||rect.yMax>texture.height)continue;
                if(!Inside(cell.hip,(int)rect.width,(int)rect.height)||!Inside(cell.rightKnee,(int)rect.width,(int)rect.height)||!Inside(cell.leftKnee,(int)rect.width,(int)rect.height)||!Inside(cell.rightFoot,(int)rect.width,(int)rect.height)||!Inside(cell.leftFoot,(int)rect.width,(int)rect.height))
                {LocomotionValidationError="Measured gait joint exceeds its source rectangle: direction="+cell.direction+" frame="+cell.frame;continue;}
                var sprite=Sprite.Create(texture,rect,cell.pivot,1,0,SpriteMeshType.FullRect);sprite.name="Approved run / "+cell.direction+" / "+cell.frame;ownedSprites.Add(sprite);
                var frame=new Frame{sprite=sprite,cell=cell,pixelsToWorld=table.height/cell.bodyHeightPixels};
                frame.baseSprite=LoadLayer(cell.baseTexture,cols,rows,index,frame);
                if(cell.overlays!=null)foreach(var layer in cell.overlays)
                {
                    if(layer==null||layer.slot<0||layer.slot>=5)continue;
                    var overlay=LoadEquipmentLayer(layer,frame,layer.cellIndex<0?cell.frame:layer.cellIndex);
                    if(overlay!=null)frame.equipment[layer.visualVariant*5+layer.slot]=overlay;
                }
                ComposePreservedLayers(frame);
                var rigidDefinitions=new List<RigidPart>();if(cell.rigidParts!=null)rigidDefinitions.AddRange(cell.rigidParts);if(cell.pelvisPart!=null)rigidDefinitions.Add(cell.pelvisPart);foreach(var part in rigidDefinitions)
                {
                    var partTexture=Resources.Load<Texture2D>(ResourceRoot+part.texture);var crop=part.rect;
                    if(partTexture==null||crop==null||crop.width<=0||crop.height<=0||crop.x<0||crop.y<0||crop.x+crop.width>partTexture.width||crop.y+crop.height>partTexture.height)continue;
                    var partSprite=Sprite.Create(partTexture,new Rect(crop.x,partTexture.height-crop.y-crop.height,crop.width,crop.height),part.pivot,1,0,SpriteMeshType.FullRect);ownedSprites.Add(partSprite);
                    var mount=new GameObject("Approved fixed aspect / "+part.name);mount.transform.SetParent(transform,false);var obj=new GameObject("Approved rigid painted / "+part.name);obj.transform.SetParent(mount.transform,false);var view=obj.AddComponent<SpriteRenderer>();view.sharedMaterial=material;view.sortingOrder=part.name.StartsWith("right",StringComparison.Ordinal)?92:91;view.enabled=false;
                    Vector2 shift=new Vector2(part.pivot.x*crop.width,part.pivot.y*crop.height);
                    var geometryObject=new GameObject("Approved explicit affine / "+part.name);geometryObject.transform.SetParent(transform,false);var geometry=geometryObject.AddComponent<MeshFilter>();var geometryRenderer=geometryObject.AddComponent<MeshRenderer>();geometryRenderer.sharedMaterial=material;geometryRenderer.enabled=false;var rigidMesh=new Mesh{name="Rigid fixed affine quad"};geometry.sharedMesh=rigidMesh;
                    frame.rigid[part.name]=new RigidPiece{mesh=rigidMesh,renderer=geometryRenderer,sprite=partSprite,view=view,mount=mount.transform,visibleWidthPixels=part.visibleWidthPixels,midCrossSectionPixels=part.midCrossSectionPixels,sole=new Vector2(part.sole.x,crop.height-part.sole.y)-shift,pixelScale=part.bodyHeightPixels>0?table.height/part.bodyHeightPixels:frame.pixelsToWorld,rightHip=new Vector2(part.rightHip.x,crop.height-part.rightHip.y)-shift,leftHip=new Vector2(part.leftHip.x,crop.height-part.leftHip.y)-shift,start=new Vector2(part.start.x,crop.height-part.start.y)-shift,end=new Vector2(part.end.x,crop.height-part.end.y)-shift};
                }
                if(cell.rightLegPolygon!=null&&cell.leftLegPolygon!=null&&cell.pelvisPolygon!=null)
                {
                    var right=string.IsNullOrEmpty(cell.rightLegTexture)?sprite:LoadLayer(cell.rightLegTexture,cols,rows,index,frame);
                    var left=string.IsNullOrEmpty(cell.leftLegTexture)?sprite:LoadLayer(cell.leftLegTexture,cols,rows,index,frame);
                    if(right!=null)frame.rightLegSprite=CreateMaskedSprite(right,new[]{cell.rightLegPolygon},null);
                    if(left!=null)frame.leftLegSprite=CreateMaskedSprite(left,new[]{cell.leftLegPolygon},null);
                    frame.pelvisSprite=CreateMaskedSprite(sprite,new[]{cell.pelvisPolygon},null);
                }
                lowerFrames[cell.direction].Add(frame);
            }
            LocomotionReady=true;
            foreach(var sequence in lowerFrames)
            {
                sequence.Sort((a,b)=>((LowerCell)a.cell).frame.CompareTo(((LowerCell)b.cell).frame));LocomotionReady&=sequence.Count>=8;
                for(int index=0;index<sequence.Count;index++)
                {
                    var cell=(LowerCell)sequence[index].cell;
                    LocomotionReady&=cell.frame==index&&cell.hip!=Vector2.zero&&cell.rightKnee!=Vector2.zero&&cell.leftKnee!=Vector2.zero&&cell.rightFoot!=Vector2.zero&&cell.leftFoot!=Vector2.zero;
                }
            }
            foreach(var frame in frames.Values)LocomotionReady&=frame.upperSprite!=null;
            if(!LocomotionReady&&string.IsNullOrEmpty(LocomotionValidationError))LocomotionValidationError=GeometryValidationError??("Gait frame counts: "+string.Join(",",System.Array.ConvertAll(lowerFrames,x=>x.Count.ToString()))+"; missing upper: "+string.Join(",",System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(frames.Values,x=>x.upperSprite==null),x=>Key(x.cell.pose,x.cell.direction))));
            if(LocomotionReady)LocomotionValidationError=null;
        }
        static string Key(string pose,int direction)=>pose+"/"+direction;
        void ComposePreservedLayers(Frame frame)
        {
            var originalBase=frame.baseSprite;var originalBareWeaponless=frame.baseWeaponlessSprite;
            var armor=new List<Vector2[]>();foreach(var entry in frame.equipmentPolygons)if(entry.Key/5==VisualVariant)armor.AddRange(entry.Value);
            if(frame.baseSprite!=null&&armor.Count>0)
            {
                var generated=frame.baseSprite;frame.baseSprite=CreateWithoutPolygons(frame.sprite,armor.ToArray());frame.basePatchSprite=CreateMaskedSprite(generated,armor.ToArray(),null);
                if(originalBareWeaponless!=null)frame.externalBasePatchSprite=CreateMaskedSprite(originalBareWeaponless,armor.ToArray(),null);
            }
            var weapons=PolygonRegions(frame.cell.weaponPolygon,frame.cell.weaponPolygons);
            if(frame.weaponlessSprite!=null&&weapons.Length>0)
            {
                var generated=frame.weaponlessSprite;frame.weaponlessSprite=CreateWithoutPolygons(frame.sprite,weapons);frame.weaponlessPatchSprite=CreateMaskedSprite(generated,weapons,null);
                if(originalBareWeaponless!=null)frame.bareWeaponlessPatchSprite=CreateMaskedSprite(originalBareWeaponless,weapons,null);
                if(frame.baseSprite!=null)frame.baseWeaponlessSprite=CreateWithoutPolygons(frame.baseSprite,weapons);
                foreach(var entry in frame.equipment){var external=CreateWithoutPolygons(entry.Value,weapons);if(external!=null)frame.externalEquipment[entry.Key]=external;}
            }
            else if(frame.cell.weaponOccluded)
            {
                frame.weaponlessSprite=frame.sprite;frame.baseWeaponlessSprite=frame.baseSprite;foreach(var entry in frame.equipment)frame.externalEquipment[entry.Key]=entry.Value;
            }
            var hands=PolygonRegions(null,frame.cell.handPolygons);
            if(hands.Length>0){frame.handsSprite=CreateMaskedSprite(frame.sprite,hands,null);if(originalBase!=null)frame.bareHandsSprite=CreateMaskedSprite(originalBase,hands,null);}
        }
        public bool HasPose(string pose)
        {for(int direction=0;direction<8;direction++)if(!frames.ContainsKey(Key(pose,direction)))return false;return true;}
        Sprite CreateUpperPolygon(Sprite source,Vector2[] polygon)
        {return source.name.EndsWith(" / completely masked source",StringComparison.Ordinal)?CreateEmptySourceSprite(source):CreateMaskedSprite(source,SourceRegions(source),polygon);}
        static Vector2[][] SourceRegions(Sprite sprite)
        {
            var vertices=sprite.vertices;var triangles=sprite.triangles;var regions=new Vector2[triangles.Length/3][];
            for(int at=0;at<triangles.Length;at+=3)
            {
                var region=new Vector2[3];for(int point=0;point<3;point++){var v=vertices[triangles[at+point]];region[point]=new Vector2(Mathf.Clamp(v.x+sprite.pivot.x,0,sprite.rect.width),Mathf.Clamp(sprite.rect.height-v.y-sprite.pivot.y,0,sprite.rect.height));}regions[at/3]=region;
            }
            return regions;
        }
        static Vector2[][] PolygonRegions(Vector2[] single,Polygon[] multiple)
        {
            var regions=new List<Vector2[]>();if(single!=null&&single.Length>=3)regions.Add(single);
            if(multiple!=null)foreach(var polygon in multiple)if(polygon?.points!=null&&polygon.points.Length>=3)regions.Add(polygon.points);return regions.ToArray();
        }
        Sprite CreateWithoutPolygons(Sprite source,Vector2[][] holes)
        {
            var pieces=new List<List<Vector2>>();foreach(var region in SourceRegions(source))pieces.Add(new List<Vector2>(region));
            foreach(var hole in holes)
            {
                var triangles=Triangulate(hole);if(triangles==null)return null;
                if(IsConvex(hole))
                {
                    var boundary=new List<Vector2>(hole);float signed=0;for(int i=0;i<boundary.Count;i++)signed+=Cross(boundary[i],boundary[(i+1)%boundary.Count]);if(signed<0)boundary.Reverse();
                    pieces=SubtractConvex(pieces,boundary);continue;
                }
                for(int at=0;at<triangles.Length;at+=3)
                {
                    pieces=SubtractConvex(pieces,new List<Vector2>{hole[triangles[at]],hole[triangles[at+1]],hole[triangles[at+2]]});
                }
            }
            var points=new List<Vector2>();var indices=new List<ushort>();foreach(var piece in pieces)AppendGeometry(piece,points,indices);
            if(indices.Count==0)return CreateEmptySourceSprite(source);
            return CreateGeometrySprite(source,points,indices);
        }
        static bool IsConvex(Vector2[] polygon)
        {
            float sign=0;for(int i=0;i<polygon.Length;i++)
            {
                float turn=Cross(polygon[(i+1)%polygon.Length]-polygon[i],polygon[(i+2)%polygon.Length]-polygon[(i+1)%polygon.Length]);
                if(Mathf.Abs(turn)<.0001f)continue;if(sign==0)sign=turn;else if(sign*turn<0)return false;
            }
            return sign!=0;
        }
        static List<List<Vector2>> SubtractConvex(List<List<Vector2>> pieces,List<Vector2> boundary)
        {
            var output=new List<List<Vector2>>();var cutBounds=PolygonBounds(boundary);
            foreach(var piece in pieces)
            {
                if(!PolygonBounds(piece).Overlaps(cutBounds)){output.Add(piece);continue;}
                var inside=piece;
                for(int side=0;side<boundary.Count&&inside.Count>=3;side++)
                {
                    Vector2 a=boundary[side],b=boundary[(side+1)%boundary.Count];
                    var outside=ClipHalfPlane(inside,b,a);if(PolygonArea(outside)>.001f)output.Add(outside);
                    inside=ClipHalfPlane(inside,a,b);
                }
            }
            return output;
        }
        static Rect PolygonBounds(List<Vector2> polygon)
        {
            if(polygon.Count==0)return Rect.zero;Vector2 min=polygon[0],max=min;foreach(var point in polygon){min=Vector2.Min(min,point);max=Vector2.Max(max,point);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        Sprite CreateEmptySourceSprite(Sprite source)
        {
            // A fully covered source (e.g. all lower-body pixels belong to
            // Legs/Feet) is a valid empty layer, distinct from a failed mask.
            // Degenerate geometry samples no pixels; the clothing patch below
            // supplies the removed equipment's replacement surface.
            var sprite=Sprite.Create(source.texture,source.rect,new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height),1,0,SpriteMeshType.FullRect);
            sprite.name=source.name+" / completely masked source";
            sprite.OverrideGeometry(new[]{Vector2.zero,Vector2.zero,Vector2.zero},new ushort[]{0,1,2});ownedSprites.Add(sprite);return sprite;
        }
        Sprite LoadEquipmentLayer(EquipmentLayer layer,Frame frame,int index)
        {
            GeometryValidationError=null;
            var regions=new List<Vector2[]>();
            if(layer.polygon!=null&&layer.polygon.Length>=3)regions.Add(layer.polygon);
            if(layer.polygons!=null)foreach(var polygon in layer.polygons)if(polygon?.points!=null&&polygon.points.Length>=3)regions.Add(polygon.points);
            if(layer.polygons!=null&&layer.polygons.Length>0&&regions.Count==0)EquipmentValidationError="Invalid slot polygon contract: "+Key(frame.cell.pose,frame.cell.direction)+" slot="+layer.slot+"; use polygons:[{points:[{x,y},...]}], not nested arrays";
            var source=string.IsNullOrEmpty(layer.texture)?frame.sprite:LoadLayer(layer.texture,layer.columns,layer.rows,index,frame);
            if(source==null)return null;
            if(regions.Count==0)return string.IsNullOrEmpty(layer.texture)?null:source;
            var mask=regions.ToArray();var sprite=CreateMaskedSprite(source,mask,null);
            if(sprite!=null)frame.equipmentPolygons[layer.visualVariant*5+layer.slot]=mask;return sprite;
        }
        Sprite CreateMaskedSprite(Sprite source,Vector2[][] regions,Vector2[] clip)
        {
            var points=new List<Vector2>();var indices=new List<ushort>();
            var clipTriangles=clip!=null?Triangulate(clip):null;if(clip!=null&&clipTriangles==null)return null;
            foreach(var region in regions)
            {
                foreach(var point in region)if(!Inside(point,Mathf.RoundToInt(source.rect.width),Mathf.RoundToInt(source.rect.height)))return null;
                var triangles=Triangulate(region);if(triangles==null)return null;
                for(int at=0;at<triangles.Length;at+=3)
                {
                    var triangle=new List<Vector2>{region[triangles[at]],region[triangles[at+1]],region[triangles[at+2]]};
                    if(clip==null){AppendGeometry(triangle,points,indices);continue;}
                    for(int edge=0;edge<clipTriangles.Length;edge+=3)
                    {
                        var intersection=new List<Vector2>(triangle);
                        for(int side=0;side<3&&intersection.Count>=3;side++)intersection=ClipHalfPlane(intersection,clip[clipTriangles[edge+side]],clip[clipTriangles[edge+(side+1)%3]]);
                        if(intersection.Count>=3)AppendGeometry(intersection,points,indices);
                    }
                }
            }
            return CreateGeometrySprite(source,points,indices);
        }
        Sprite CreateGeometrySprite(Sprite source,List<Vector2> points,List<ushort> indices)
        {
            if(indices.Count==0||points.Count>ushort.MaxValue){GeometryValidationError="Polygon geometry empty or too large: "+source.name+" vertices="+points.Count+" indices="+indices.Count;return null;}
            // OverrideGeometry takes pixel positions in rect space. Unity applies
            // the pivot and pixels-per-unit conversion itself.
            var vertices=new Vector2[points.Count];for(int i=0;i<points.Count;i++)vertices[i]=new Vector2(Mathf.Clamp(points[i].x,0,source.rect.width),Mathf.Clamp(source.rect.height-points[i].y,0,source.rect.height));
            var sprite=Sprite.Create(source.texture,source.rect,new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height),1,0,SpriteMeshType.FullRect);
            sprite.name=source.name+" / exact source polygon mask";sprite.OverrideGeometry(vertices,indices.ToArray());
            var actual=sprite.vertices;bool accepted=actual.Length==vertices.Length&&sprite.triangles.Length==indices.Count;
            // Unity may reorder or weld vertices. Compare occupied geometry,
            // rather than requiring an implementation-specific vertex order.
            var expectedPoints=new Dictionary<string,int>();foreach(var vertex in vertices){string key=PointKey(vertex-source.pivot);expectedPoints.TryGetValue(key,out int count);expectedPoints[key]=count+1;}
            for(int i=0;i<actual.Length&&accepted;i++)
            {
                string key=PointKey(actual[i]);if(!expectedPoints.TryGetValue(key,out int count)||count<=0)accepted=false;else expectedPoints[key]=count-1;
            }
            if(accepted)
            {
                var expectedTriangles=new Dictionary<string,int>();
                for(int at=0;at<indices.Count;at+=3)
                {
                    string key=TriangleKey(vertices[indices[at]]-source.pivot,vertices[indices[at+1]]-source.pivot,vertices[indices[at+2]]-source.pivot);
                    expectedTriangles.TryGetValue(key,out int count);expectedTriangles[key]=count+1;
                }
                var actualIndices=sprite.triangles;
                for(int at=0;at<actualIndices.Length&&accepted;at+=3)
                {
                    string key=TriangleKey(actual[actualIndices[at]],actual[actualIndices[at+1]],actual[actualIndices[at+2]]);
                    if(!expectedTriangles.TryGetValue(key,out int count)||count<=0)accepted=false;else expectedTriangles[key]=count-1;
                }
            }
            if(!accepted){GeometryValidationError="Unity rejected the measured source polygon geometry: "+source.name;Destroy(sprite);return null;}
            ownedSprites.Add(sprite);return sprite;
        }
        static string TriangleKey(Vector2 a,Vector2 b,Vector2 c)
        {
            string[] points={PointKey(a),PointKey(b),PointKey(c)};Array.Sort(points,StringComparer.Ordinal);return string.Join(";",points);
        }
        static string PointKey(Vector2 p)=>Mathf.RoundToInt(p.x*100)+","+Mathf.RoundToInt(p.y*100);
        static void AppendGeometry(List<Vector2> polygon,List<Vector2> points,List<ushort> indices)
        {
            if(PolygonArea(polygon)<=.001f)return;
            int offset=points.Count;points.AddRange(polygon);for(int i=1;i<polygon.Count-1;i++){indices.Add((ushort)offset);indices.Add((ushort)(offset+i));indices.Add((ushort)(offset+i+1));}
        }
        static float PolygonArea(List<Vector2> polygon)
        {float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);return Mathf.Abs(area)*.5f;}
        static List<Vector2> ClipHalfPlane(List<Vector2> polygon,Vector2 a,Vector2 b)
        {
            var result=new List<Vector2>();var previous=polygon[polygon.Count-1];float before=Cross(b-a,previous-a);
            foreach(var current in polygon)
            {
                float after=Cross(b-a,current-a);bool oldInside=before>=-.0001f,newInside=after>=-.0001f;
                if(oldInside!=newInside)result.Add(Vector2.LerpUnclamped(previous,current,before/(before-after)));
                if(newInside)result.Add(current);previous=current;before=after;
            }
            for(int i=result.Count-1;i>=0&&result.Count>1;i--)if((result[i]-result[(i+1)%result.Count]).sqrMagnitude<.000001f)result.RemoveAt(i);
            return result;
        }
        static ushort[] Triangulate(Vector2[] points)
        {
            var order=new List<int>();float area=0;
            for(int i=0;i<points.Length;i++){order.Add(i);var a=points[i];var b=points[(i+1)%points.Length];area+=a.x*b.y-b.x*a.y;}
            if(area<0)order.Reverse();var triangles=new List<ushort>();int guard=points.Length*points.Length;
            while(order.Count>2&&guard-->0)
            {
                bool clipped=false;
                for(int i=0;i<order.Count;i++)
                {
                    int a=order[(i+order.Count-1)%order.Count],b=order[i],c=order[(i+1)%order.Count];
                    if(Cross(points[b]-points[a],points[c]-points[b])<=.0001f)continue;
                    bool contains=false;
                    for(int j=0;j<order.Count;j++)
                    {
                        int p=order[j];if(p==a||p==b||p==c)continue;
                        if(Cross(points[b]-points[a],points[p]-points[a])>=-.0001f&&Cross(points[c]-points[b],points[p]-points[b])>=-.0001f&&Cross(points[a]-points[c],points[p]-points[c])>=-.0001f){contains=true;break;}
                    }
                    if(contains)continue;triangles.Add((ushort)a);triangles.Add((ushort)b);triangles.Add((ushort)c);order.RemoveAt(i);clipped=true;break;
                }
                if(!clipped)return null;
            }
            return order.Count<=2?triangles.ToArray():null;
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        Sprite LoadLayer(string path,int columns,int rows,int index,Frame frame)
        {
            if(string.IsNullOrEmpty(path))return null;
            var texture=Resources.Load<Texture2D>(ResourceRoot+path);
            if(texture==null)return null;
            int width=Mathf.RoundToInt(frame.sprite.rect.width),height=Mathf.RoundToInt(frame.sprite.rect.height);Rect rect;
            if(texture.width==frame.sprite.texture.width&&texture.height==frame.sprite.texture.height)rect=frame.sprite.rect;
            else if(texture.width==width&&texture.height==height)rect=new Rect(0,0,width,height);
            else if(columns>0&&rows>0&&index>=0&&index<columns*rows&&texture.width==width*columns&&texture.height==height*rows)rect=new Rect(index%columns*width,(rows-1-index/columns)*height,width,height);
            else return null;
            var sprite=Sprite.Create(texture,rect,frame.cell.pivot,1,0,SpriteMeshType.FullRect);
            sprite.name="Approved equipment / "+path+" / "+index;ownedSprites.Add(sprite);return sprite;
        }
        static bool Inside(Vector2 p,int w,int h)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&p.x>=0&&p.x<=w&&p.y>=0&&p.y<=h;
        public bool Matches(ArmorItem[] equipment,string visualSet)
        {
            if(!Initialize()||equipment==null||equipment.Length<5)return false;
            if(visualSet!=table.visualSet&&!WeaponlessReady)return false;
            bool complete=true;for(int slot=0;slot<5;slot++)complete&=equipment[slot]!=null&&equipment[slot].visualVariant==VisualVariant;
            if(!complete&&!EquipmentLayersReady)return false;
            return true;
        }
        public bool Render(Camera camera,Vector3 footOrigin,int direction,bool aiming,bool compact,ArmorItem[] equipment,bool moving,int movementDirection,float phase,string visualSet)
        {
            if(!Ready||camera==null){RenderValidationError="Renderer not ready or camera missing: ready="+Ready;return false;}
            string pose=aiming?(compact?"compact":"aim"):compact&&HasPose("compact-low")?"compact-low":"low";
            RenderValidationError=null;
            if(!frames.TryGetValue(Key(pose,direction),out var frame)){RenderValidationError="Missing pose "+Key(pose,direction);return false;}
            bool external=visualSet!=table.visualSet;
            bool complete=true;for(int slot=0;slot<5;slot++)complete&=equipment[slot]!=null&&equipment[slot].visualVariant==VisualVariant;
            UnsupportedEquipmentCount=0;
            if(!complete)
            {
                if(frame.baseSprite==null){RenderValidationError="Missing clothing base / "+Key(pose,direction);return false;}
                if(external&&frame.baseWeaponlessSprite==null){RenderValidationError="Missing external clothing base / "+Key(pose,direction);return false;}
                if(external&&frame.externalBasePatchSprite==null){RenderValidationError="Missing external clothing patch / "+Key(pose,direction);return false;}
                for(int slot=0;slot<5;slot++)if(equipment[slot]!=null&&!frame.equipment.ContainsKey(equipment[slot].visualVariant*5+slot))
                {if(SlotOccluded(frame,slot))continue;if(equipment[slot].visualVariant==VisualVariant){frame.equipmentErrors.TryGetValue(equipment[slot].visualVariant*5+slot,out var reason);RenderValidationError="Missing requested new slot "+slot+" / "+Key(pose,direction)+"; reason="+reason;return false;}UnsupportedEquipmentCount++;}
            }
            HideLower();
            Frame lower=null,nextLower=null;float lowerBlend=0;
            if(!moving)RenderValidationError="Gait not requested: moving=false / "+Key(pose,direction);
            else if(!LocomotionReady)RenderValidationError="Gait globally unavailable: "+LocomotionValidationError+" geometry="+GeometryValidationError;
            if(moving&&LocomotionReady)
            {
                var sequence=lowerFrames[Mathf.Clamp(movementDirection,0,7)];
                CurrentLowerDirection=Mathf.Clamp(movementDirection,0,7);
                float cursor=Mathf.Repeat(phase,1)*sequence.Count;
                int sample=Mathf.FloorToInt(cursor)%sequence.Count;lower=sequence[sample];nextLower=sequence[(sample+1)%sequence.Count];lowerBlend=cursor-Mathf.Floor(cursor);CurrentLowerFrame=sample;
                // Limited d1 candidate: keep each leg's original UV identity and
                // use a single completed source frame to avoid irregular source
                // phase swaps. Only measured lower joints receive the gait.
                if(movementDirection==1&&complete&&sequence[0].rigid.Count==7&&sequence[0].rigid.ContainsKey("pelvis")){lower=sequence[0];nextLower=lower;lowerBlend=0;CurrentLowerFrame=0;}
                if(!complete)
                {
                    bool supported=frame.upperBaseSprite!=null&&lower.baseSprite!=null;
                    for(int slot=0;slot<5;slot++)if(equipment[slot]!=null&&equipment[slot].visualVariant==VisualVariant)
                    {
                        int key=equipment[slot].visualVariant*5+slot;
                        supported&=SlotOccluded(frame,slot)||(slot<=2?frame.upperEquipment.ContainsKey(key):lower.equipment.ContainsKey(key));
                    }
                    if(!supported){RenderValidationError="Gait subset source missing: upperBase="+(frame.upperBaseSprite!=null)+" lowerBase="+(lower.baseSprite!=null)+" / "+Key(pose,direction)+" movement="+movementDirection+" frame="+CurrentLowerFrame+" geometry="+GeometryValidationError;lower=null;}
                }
                if(external&&((complete?frame.upperWeaponlessSprite==null:frame.upperBaseWeaponlessSprite==null)||!frame.cell.handsOccluded&&frame.upperHandsSprite==null)){RenderValidationError="External upper/hands gait layer missing / "+Key(pose,direction);lower=null;}
            }
            CurrentPose=pose;CurrentDirection=direction;
            UsesCompositeEquipment=!complete;
            UnsupportedEquipmentCount=0;
            for(int slot=0;slot<5;slot++)if(!complete&&equipment[slot]!=null)
            {
                int key=equipment[slot].visualVariant*5+slot;
                if(SlotOccluded(frame,slot))continue;
                if(lower!=null?(slot<=2?!frame.upperEquipment.ContainsKey(key):!lower.equipment.ContainsKey(key)):!frame.equipment.ContainsKey(key))UnsupportedEquipmentCount++;
            }
            painted.sprite=external?(lower!=null?(complete?frame.upperWeaponlessSprite:frame.upperBaseWeaponlessSprite):(complete?frame.weaponlessSprite:frame.baseWeaponlessSprite)):(lower!=null?(complete?frame.upperSprite:frame.upperBaseSprite):(complete?frame.sprite:frame.baseSprite));painted.enabled=true;painted.color=Color.white;painted.flipX=false;painted.flipY=false;
            bool bareHands=equipment[2]==null||equipment[2].visualVariant!=VisualVariant;
            UsesExternalWeapon=external;paintedHands.sprite=bareHands?(lower!=null?frame.upperBareHandsSprite:frame.bareHandsSprite):(lower!=null?frame.upperHandsSprite:frame.handsSprite);paintedHands.enabled=external&&paintedHands.sprite!=null;
            clothingPatch.sprite=external?(lower!=null?frame.upperExternalBasePatchSprite:frame.externalBasePatchSprite):(lower!=null?frame.upperBasePatchSprite:frame.basePatchSprite);clothingPatch.enabled=!complete&&clothingPatch.sprite!=null;
            weaponPatch.sprite=complete?(lower!=null?frame.upperWeaponlessPatchSprite:frame.weaponlessPatchSprite):(lower!=null?frame.upperBareWeaponlessPatchSprite:frame.bareWeaponlessPatchSprite);weaponPatch.enabled=external&&weaponPatch.sprite!=null;
            for(int slot=0;slot<5;slot++)
            {
                var dictionary=external?(lower!=null?frame.upperExternalEquipment:frame.externalEquipment):(lower!=null?frame.upperEquipment:frame.equipment);
                var view=equipmentViews[slot];view.enabled=!complete&&equipment[slot]!=null&&dictionary.ContainsKey(equipment[slot].visualVariant*5+slot);
                if(view.enabled)view.sprite=dictionary[equipment[slot].visualVariant*5+slot];
            }
            painted.transform.SetPositionAndRotation(footOrigin,camera.transform.rotation);
            if(moving&&movementDirection==1&&lower!=null&&lower.rigid.Count==7)
            {
                // Entire painted upper and pelvis share the flight displacement;
                // no relative separation is introduced at the waist.
                if(!ReviewRigidNeutral)painted.transform.position+=camera.transform.up*RigidFlight(phase);
            }
            var parentScale=transform.lossyScale;
            painted.transform.localScale=new Vector3(frame.pixelsToWorld/parentScale.x,frame.pixelsToWorld/parentScale.y,frame.pixelsToWorld/parentScale.z);
            if(lower!=null)
            {
                var cell=(LowerCell)lower.cell;lowerPainted.sprite=complete?lower.sprite:lower.baseSprite;lowerPainted.enabled=false;
                lowerPainted.transform.SetPositionAndRotation(Vector3.zero,camera.transform.rotation);
                lowerPainted.transform.localScale=new Vector3(lower.pixelsToWorld/parentScale.x,lower.pixelsToWorld/parentScale.y,lower.pixelsToWorld/parentScale.z);
                var belt=frame.cell.beltCenter;
                if(belt==Vector2.zero)belt=new Vector2(frame.cell.pivot.x*frame.sprite.rect.width,frame.cell.beltY);
                var hipLocal=LocalPoint(lower,cell.hip);
                // The upper mask extends below the belt; overlap the waistband
                // into that opaque hem rather than joining two cut edges.
                lowerPainted.transform.position=Point(frame,belt)-lowerPainted.transform.TransformVector(hipLocal)+camera.transform.up*.045f;
                bool rigid=movementDirection==1&&complete&&lower.rigid.Count==7&&lower.rigid.ContainsKey("pelvis");
                if(rigid&&cell.waistAttach!=Vector2.zero&&frame.cell.hemAttach!=Vector2.zero)lowerPainted.transform.position=Point(frame,frame.cell.hemAttach)-lowerPainted.transform.TransformVector(LocalPoint(lower,cell.waistAttach))+camera.transform.up*.02f;
                WaistJoinError=Vector3.Distance(lowerPainted.transform.TransformPoint(hipLocal),Point(frame,belt)+camera.transform.up*.045f);
                var nextCell=(LowerCell)nextLower.cell;float ratio=nextLower.pixelsToWorld/lower.pixelsToWorld;
                bool separated=false;
                Vector2[] corrected=separated?CorrectedStride(cell,lower.pixelsToWorld,phase):null;CorrectedStrideActive=corrected!=null;
                Vector2 rightTarget=(nextCell.rightFoot-nextCell.hip)*ratio+cell.hip,leftTarget=(nextCell.leftFoot-nextCell.hip)*ratio+cell.hip;
                RightFoot=lowerPainted.transform.TransformPoint(LocalPoint(lower,Vector2.Lerp(cell.rightFoot,rightTarget,lowerBlend)));LeftFoot=lowerPainted.transform.TransformPoint(LocalPoint(lower,Vector2.Lerp(cell.leftFoot,leftTarget,lowerBlend)));
                if(corrected!=null){RightFoot=lowerPainted.transform.TransformPoint(LocalPoint(lower,corrected[2]));LeftFoot=lowerPainted.transform.TransformPoint(LocalPoint(lower,corrected[3]));}
                if(rigid)
                {
                    RenderRigidLegs(lower,lowerPainted.transform,phase);
                    CorrectedStrideActive=true;
                }
                else if(separated)
                {
                    rightLegSurface.Render(lower,nextLower,0,lower.rightLegSprite,lowerPainted.transform,corrected,0);
                    leftLegSurface.Render(lower,nextLower,0,lower.leftLegSprite,lowerPainted.transform,corrected,1);
                    pelvisSurface.Render(lower,nextLower,0,lower.pelvisSprite,lowerPainted.transform);
                }
                else lowerSurface.Render(lower,nextLower,lowerBlend,lowerPainted.sprite,lowerPainted.transform);
                if(!complete&&lower.basePatchSprite!=null)lowerBasePatchSurface.Render(lower,nextLower,lowerBlend,lower.basePatchSprite,lowerPainted.transform,corrected);
                for(int slot=0;slot<5;slot++)if(!complete&&equipment[slot]!=null&&lower.equipment.TryGetValue(equipment[slot].visualVariant*5+slot,out var overlay))lowerEquipmentSurfaces[slot].Render(lower,nextLower,lowerBlend,overlay,lowerPainted.transform,corrected);
                LowerInterpolation=lowerBlend;
                LocomotionActive=true;
            }
            Muzzle=Point(frame,frame.cell.muzzle);Grip=Point(frame,frame.cell.grip);Support=Point(frame,frame.cell.support);Stock=Point(frame,frame.cell.stock);
            BarrelStart=Point(frame,frame.cell.barrelStart!=Vector2.zero?frame.cell.barrelStart:frame.cell.stock);
            return true;
        }
        Vector3 Point(Frame frame,Vector2 topLeftPixel)
        {
            return painted.transform.TransformPoint(LocalPoint(frame,topLeftPixel));
        }
        static Vector2[] CorrectedStride(LowerCell cell,float pixelScale,float phase)
        {
            var result=new Vector2[4];float pixels=1/Mathf.Max(.00001f,pixelScale);
            // Anatomical right/left remain attached to their own pixels. The
            // second leg runs exactly half a cycle behind the first.
            for(int leg=0;leg<2;leg++)
            {
                float cycle=Mathf.Repeat(phase+leg*.5f,1),a=cycle*Mathf.PI*2;
                Vector2 hip=LegHip(cell,leg);
                float swing=Mathf.Cos(a),recovery=Mathf.Max(0,Mathf.Sin(a));
                Vector2 foot=hip+new Vector2(.866f*.27f*swing,.90f+.5f*.27f*swing-.25f*recovery)*pixels;
                float upper=.49f*pixels,lower=.49f*pixels;
                Vector2 delta=foot-hip;float distance=Mathf.Min(delta.magnitude,(upper+lower)*.995f);Vector2 axis=delta.normalized;foot=hip+axis*distance;
                float along=(upper*upper-lower*lower+distance*distance)/(2*Mathf.Max(1,distance)),bend=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
                Vector2 pole=new Vector2(axis.y,-axis.x);if(pole.x<0)pole=-pole;
                result[leg]=hip+axis*along+pole*bend;result[leg+2]=foot;
            }
            return result;
        }
        void RenderRigidLegs(Frame frame,Transform anchor,float phase)
        {
            var pelvis=frame.rigid["pelvis"];var camera=painted.transform;
            Vector3 seam=frames[Key(CurrentPose,CurrentDirection)].cell.hemAttach!=Vector2.zero?Point(frames[Key(CurrentPose,CurrentDirection)],frames[Key(CurrentPose,CurrentDirection)].cell.hemAttach):anchor.TransformPoint(LocalPoint(frame,((LowerCell)frame.cell).hip));
            if(!ReviewRigidNeutral)seam+=camera.up*.045f;PlaceRigidWorld(pelvis,seam,Vector2.down,camera.rotation);
            pelvis.view.sortingOrder=99;pelvis.renderer.sortingOrder=99;
            WaistJoinError=Vector3.Distance(pelvis.view.transform.TransformPoint(pelvis.start),seam);
            var fingerprint=new System.Text.StringBuilder();
            for(int leg=0;leg<2;leg++)
            {
                string side=leg==0?"right":"left";var thigh=frame.rigid[side+"Thigh"];var shin=frame.rigid[side+"Shin"];var boot=frame.rigid[side+"Boot"];
                Vector3 hipWorld=pelvis.view.transform.TransformPoint(leg==0?pelvis.rightHip:pelvis.leftHip);
                var original=frames[Key("low",1)];var measured=original.cell.neutralMeasurements;
                if(measured!=null)
                {
                    Vector2 originalHip=leg==0?measured.rightHip:measured.leftHip,originalKnee=leg==0?measured.rightKnee:measured.leftKnee,originalAnkle=leg==0?measured.rightAnkle:measured.leftAnkle,originalToe=leg==0?measured.rightToe:measured.leftToe;
                    Vector2 delta=originalHip-original.cell.hemAttach;
                    hipWorld=seam+camera.right*(delta.x*original.pixelsToWorld)-camera.up*(delta.y*original.pixelsToWorld);
                    thigh.pixelScale=Vector2.Distance(originalHip,originalKnee)*original.pixelsToWorld/Mathf.Max(1,Vector2.Distance(thigh.start,thigh.end));
                    shin.pixelScale=Vector2.Distance(originalKnee,originalAnkle)*original.pixelsToWorld/Mathf.Max(1,Vector2.Distance(shin.start,shin.end));
                    boot.pixelScale=Vector2.Distance(originalAnkle,originalToe)*original.pixelsToWorld/Mathf.Max(1,Vector2.Distance(boot.start,boot.end));
                    thigh.crossScale=(leg==0?measured.rightThighWidthPixels:measured.leftThighWidthPixels)*original.pixelsToWorld/Mathf.Max(1,thigh.midCrossSectionPixels>0?thigh.midCrossSectionPixels:thigh.visibleWidthPixels);
                    shin.crossScale=(leg==0?measured.rightShinWidthPixels:measured.leftShinWidthPixels)*original.pixelsToWorld/Mathf.Max(1,shin.midCrossSectionPixels>0?shin.midCrossSectionPixels:shin.visibleWidthPixels);
                    boot.crossScale=(leg==0?measured.rightBootWidthPixels:measured.leftBootWidthPixels)*original.pixelsToWorld/Mathf.Max(1,boot.midCrossSectionPixels>0?boot.midCrossSectionPixels:boot.visibleWidthPixels);
                    if(ReviewRigidNeutral)
                    {
                        Vector2 originalSole=leg==0?measured.rightSole:measured.leftSole;
                        boot.pixelScale=Vector2.Distance(originalAnkle,originalSole)*original.pixelsToWorld/Mathf.Max(1,Vector2.Distance(boot.start,boot.sole));
                        Vector3 neutralKneeWorld=hipWorld+camera.right*((originalKnee.x-originalHip.x)*original.pixelsToWorld)-camera.up*((originalKnee.y-originalHip.y)*original.pixelsToWorld);
                        Vector3 ankleWorld=hipWorld+camera.right*((originalAnkle.x-originalHip.x)*original.pixelsToWorld)-camera.up*((originalAnkle.y-originalHip.y)*original.pixelsToWorld);
                        Vector2 thighAxis=new Vector2(originalKnee.x-originalHip.x,originalHip.y-originalKnee.y),shinAxis=new Vector2(originalAnkle.x-originalKnee.x,originalKnee.y-originalAnkle.y),bootAxis=new Vector2(originalToe.x-originalAnkle.x,originalAnkle.y-originalToe.y);
                        PlaceRigidWorld(thigh,hipWorld,thighAxis,camera.rotation);PlaceRigidWorld(shin,neutralKneeWorld,shinAxis,camera.rotation);
                        Vector2 originalEnd=boot.end;boot.end=boot.sole;PlaceRigidWorld(boot,ankleWorld,new Vector2(originalSole.x-originalAnkle.x,originalAnkle.y-originalSole.y),camera.rotation);boot.end=originalEnd;
                        if(leg==0){RightFoot=ankleWorld;RightGroundContact=boot.view.transform.TransformPoint(boot.sole);}else{LeftFoot=ankleWorld;LeftGroundContact=boot.view.transform.TransformPoint(boot.sole);}continue;
                    }
                }
                float upper=(thigh.end-thigh.start).magnitude*thigh.pixelScale,lower=(shin.end-shin.start).magnitude*shin.pixelScale;
                float cycle=Mathf.Repeat(phase+leg*.5f,1);
                bool planted=cycle<.42f;float swingProgress=planted?0:(cycle-.42f)/.58f;
                float stride=planted?Mathf.Lerp(.25f,-.25f,cycle/.42f):Mathf.Lerp(-.25f,.25f,Mathf.SmoothStep(0,1,swingProgress));
                float lift=planted?0:Mathf.Sin(swingProgress*Mathf.PI);
                // d1 forward projects right/down at the quarter-view elevation.
                // During support, compensate shared flight so the planted foot
                // follows the ground plane instead of the pelvis's vertical arc.
                Vector2 foot=new Vector2(.866f*stride,-(upper+lower)*.94f-.289f*stride+.27f*lift-(planted?RigidFlight(phase):0));
                Vector2 axis=foot.normalized;float distance=Mathf.Clamp(foot.magnitude,Mathf.Abs(upper-lower)+.001f,(upper+lower)*.99f);foot=axis*distance;
                float along=(upper*upper-lower*lower+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));Vector2 pole=new Vector2(-axis.y,axis.x);if(pole.x<0)pole=-pole;
                Vector2 knee=axis*along+pole*height;
                Vector3 kneeWorld=hipWorld+camera.right*knee.x+camera.up*knee.y,footWorld=hipWorld+camera.right*foot.x+camera.up*foot.y;
                PlaceRigidWorld(thigh,hipWorld,knee,camera.rotation);PlaceRigidWorld(shin,kneeWorld,foot-knee,camera.rotation);
                PlaceRigidWorld(boot,footWorld,new Vector2(Mathf.Cos(lift*.5f),Mathf.Sin(lift*.5f)),camera.rotation);
                int depthOrder=90+Mathf.RoundToInt(-stride*20)+(leg==0?1:0);
                thigh.view.sortingOrder=depthOrder;shin.view.sortingOrder=depthOrder;boot.view.sortingOrder=depthOrder;thigh.renderer.sortingOrder=depthOrder;shin.renderer.sortingOrder=depthOrder;boot.renderer.sortingOrder=depthOrder;
                if(leg==0)RightFoot=footWorld;else LeftFoot=footWorld;
                Vector3 contact=boot.view.transform.TransformPoint(boot.sole);if(leg==0)RightGroundContact=contact;else LeftGroundContact=contact;
                fingerprint.Append(thigh.view.transform.position.ToString("F4")).Append(thigh.view.transform.rotation.ToString("F4")).Append(shin.view.transform.rotation.ToString("F4"));
            }
            RigidPoseFingerprint=fingerprint.ToString();
            GroundReference=painted.transform.position;
        }
        static float RigidFlight(float phase)
        {
            float half=Mathf.Repeat(phase,.5f);
            return half<=.42f?0:Mathf.Sin((half-.42f)/.08f*Mathf.PI)*.045f;
        }
        static void PlaceRigidWorld(RigidPiece piece,Vector3 start,Vector2 direction,Quaternion cameraRotation)
        {
            Vector2 source=piece.end-piece.start;float sourceAngle=Mathf.Atan2(source.y,source.x)*Mathf.Rad2Deg,targetAngle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            var view=piece.view;view.sprite=piece.sprite;view.enabled=true;
            var mount=piece.mount;mount.SetPositionAndRotation(start,cameraRotation*Quaternion.Euler(0,0,targetAngle));
            var parent=mount.parent.lossyScale;float cross=piece.crossScale>0?piece.crossScale:piece.pixelScale;
            mount.localScale=new Vector3(piece.pixelScale/parent.x,cross/parent.y,piece.pixelScale/parent.z);
            view.transform.localRotation=Quaternion.Euler(0,0,-sourceAngle);view.transform.localScale=Vector3.one;
            view.transform.localPosition=-(view.transform.localRotation*(Vector3)piece.start);
            // Explicit affine geometry: Rtarget * diag(axis,cross) * Rsource^-1.
            // The renderer receives final camera-plane metres, with no inherited
            // nonuniform scale/shear decomposition by SpriteRenderer.
            var sourceVertices=piece.sprite.vertices;var vertices=new Vector3[sourceVertices.Length];
            Quaternion toAxis=Quaternion.Euler(0,0,-sourceAngle);
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 local=toAxis*(Vector3)(sourceVertices[i]-piece.start);
                vertices[i]=new Vector3(local.x*piece.pixelScale,local.y*cross,0);
            }
            piece.mesh.Clear();piece.mesh.vertices=vertices;piece.mesh.uv=piece.sprite.uv;piece.mesh.triangles=Array.ConvertAll(piece.sprite.triangles,x=>(int)x);var colors=new Color[vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;piece.mesh.colors=colors;piece.mesh.RecalculateBounds();
            var geometry=piece.renderer.transform;geometry.SetPositionAndRotation(start,cameraRotation*Quaternion.Euler(0,0,targetAngle));var root=geometry.parent.lossyScale;geometry.localScale=new Vector3(1/root.x,1/root.y,1/root.z);
            var block=new MaterialPropertyBlock();block.SetTexture("_MainTex",piece.sprite.texture);block.SetColor("_Color",Color.white);piece.renderer.SetPropertyBlock(block);piece.renderer.enabled=true;view.enabled=false;
        }
        static Vector2 LegHip(LowerCell cell,int leg)
        {
            float spread=cell.bodyHeightPixels*.055f;
            return cell.hip+new Vector2(leg==0?spread:-spread,cell.bodyHeightPixels*.035f);
        }
        static Vector3 LocalPoint(Frame frame,Vector2 pixel)=>new Vector3(pixel.x-frame.cell.pivot.x*frame.sprite.rect.width,frame.sprite.rect.height-pixel.y-frame.cell.pivot.y*frame.sprite.rect.height,0);
        void HideLower(){LocomotionActive=false;foreach(var sequence in lowerFrames)foreach(var frame in sequence)foreach(var piece in frame.rigid.Values){piece.view.enabled=false;piece.renderer.enabled=false;}if(rightLegSurface!=null)rightLegSurface.renderer.enabled=false;if(leftLegSurface!=null)leftLegSurface.renderer.enabled=false;if(pelvisSurface!=null)pelvisSurface.renderer.enabled=false;if(lowerPainted!=null)lowerPainted.enabled=false;if(lowerSurface!=null)lowerSurface.renderer.enabled=false;if(lowerBasePatchSurface!=null)lowerBasePatchSurface.renderer.enabled=false;foreach(var surface in lowerEquipmentSurfaces)if(surface!=null)surface.renderer.enabled=false;foreach(var view in lowerEquipmentViews)if(view!=null)view.enabled=false;}
        public void Hide(){if(painted!=null)painted.enabled=false;if(paintedHands!=null)paintedHands.enabled=false;if(clothingPatch!=null)clothingPatch.enabled=false;if(weaponPatch!=null)weaponPatch.enabled=false;UsesExternalWeapon=false;foreach(var view in equipmentViews)if(view!=null)view.enabled=false;HideLower();}
        void OnDestroy(){foreach(var sequence in lowerFrames)foreach(var frame in sequence)foreach(var part in frame.rigid.Values)if(part.mesh!=null)Destroy(part.mesh);if(rightLegSurface!=null)rightLegSurface.Dispose();if(leftLegSurface!=null)leftLegSurface.Dispose();if(pelvisSurface!=null)pelvisSurface.Dispose();foreach(var sprite in ownedSprites)if(sprite!=null)Destroy(sprite);if(lowerSurface!=null)lowerSurface.Dispose();if(lowerBasePatchSurface!=null)lowerBasePatchSurface.Dispose();foreach(var surface in lowerEquipmentSurfaces)if(surface!=null)surface.Dispose();if(material!=null)Destroy(material);}
    }
}
