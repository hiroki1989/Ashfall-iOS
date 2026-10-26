using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonVault {
 // Static pack geometry is shared, rather than instantiated thousands of times.
 public sealed class ImportedWorldBatch:MonoBehaviour {
  sealed class Part {public Mesh mesh;public Material material;public int sub;public Matrix4x4 local;}
  sealed class Template {public Bounds bounds;public readonly List<Part> parts=new List<Part>();}
  sealed class Group {public Mesh mesh;public Material material;public int sub;public readonly List<Matrix4x4> matrices=new List<Matrix4x4>();public readonly List<int> owners=new List<int>();}
  static readonly Dictionary<GameObject,Template> templates=new Dictionary<GameObject,Template>();
  static readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
  readonly Dictionary<(Mesh,Material,int),Group> groups=new Dictionary<(Mesh,Material,int),Group>();
  readonly List<Bounds> buildingBounds=new List<Bounds>();readonly List<bool> hidden=new List<bool>();static VaultGame game;
  static int planeFrame=-1;static Camera planeCamera;static readonly Plane[] planes=new Plane[6];
  float nextOcclusion;bool visibleChunk=true;bool chunkBoundsReady;Bounds chunkBounds;
  readonly Matrix4x4[] scratch=new Matrix4x4[1023];
  static Template Get(GameObject prefab){
   if(templates.TryGetValue(prefab,out var t))return t;t=new Template();bool first=true;
   foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true)){
    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!filter.sharedMesh)continue;
    var local=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
    var b=filter.sharedMesh.bounds;
    for(int c=0;c<8;c++){var v=local.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));if(first){t.bounds=new Bounds(v,Vector3.zero);first=false;}else t.bounds.Encapsulate(v);}
    var source=renderer.sharedMaterials;
    for(int sub=0;sub<filter.sharedMesh.subMeshCount&&sub<source.Length;sub++){
     if(!source[sub])continue;if(!materials.TryGetValue(source[sub],out var mat)){mat=new Material(source[sub]);
      if(mat.shader&&mat.shader.name=="Standard"){
#if UNITY_EDITOR
       mat.shader=UnityEditor.AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader");
#else
       mat.shader=Resources.GetBuiltinResource<Material>("Default-Material.mat").shader;
#endif
       mat.SetFloat("_ZWrite",1);mat.SetFloat("_Mode",0);mat.SetFloat("_SrcBlend",1);mat.SetFloat("_DstBlend",0);mat.renderQueue=-1;mat.DisableKeyword("_ALPHABLEND_ON");mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
      }
      mat.enableInstancing=true;materials[source[sub]]=mat;}
     t.parts.Add(new Part{mesh=filter.sharedMesh,material=mat,sub=sub,local=local});
    }
   }
   templates[prefab]=t;return t;
  }
  public static Bounds SourceBounds(GameObject prefab)=>Get(prefab).bounds;
  public static void Add(Transform parent,GameObject prefab,Vector3 position,Quaternion rotation,Vector3 scale,bool building=false){
   var batch=parent.GetComponent<ImportedWorldBatch>();if(!batch)batch=parent.gameObject.AddComponent<ImportedWorldBatch>();
   var matrix=parent.localToWorldMatrix*Matrix4x4.TRS(position,rotation,scale);
   int owner=-1;if(building){owner=batch.buildingBounds.Count;var source=Get(prefab).bounds;var bounds=new Bounds(matrix.MultiplyPoint3x4(source.center),Vector3.zero);for(int corner=0;corner<8;corner++)bounds.Encapsulate(matrix.MultiplyPoint3x4(source.center+Vector3.Scale(source.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));batch.buildingBounds.Add(bounds);batch.hidden.Add(false);}
   foreach(var part in Get(prefab).parts){var key=(part.mesh,part.material,part.sub);if(!batch.groups.TryGetValue(key,out var group)){group=new Group{mesh=part.mesh,material=part.material,sub=part.sub};batch.groups.Add(key,group);}group.matrices.Add(matrix*part.local);group.owners.Add(owner);}
  }
  void LateUpdate(){DrawNow();}
  static bool ChunkVisible(Camera camera,Bounds bounds){if(planeFrame!=Time.frameCount||planeCamera!=camera){GeometryUtility.CalculateFrustumPlanes(camera,planes);planeFrame=Time.frameCount;planeCamera=camera;}return GeometryUtility.TestPlanesAABB(planes,bounds);}
  bool Hides(Bounds bounds,Vector3 point,Camera camera){var viewport=camera.WorldToViewportPoint(point);if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)return false;var ray=camera.ViewportPointToRay(viewport);return bounds.IntersectRay(ray,out float distance)&&distance<Vector3.Dot(point-ray.origin,ray.direction)-.4f;}
  public static bool OcclusionEnabled=true;
  public int HiddenBuildingCount {get;private set;}
  public void DrawNow(){
   if(!game)game=Object.FindAnyObjectByType<VaultGame>();var camera=game?game.GameCamera:Camera.main;
   if(camera&&gameObject.name.StartsWith("Stream /")){if(!chunkBoundsReady){var centre=transform.position;var parts=gameObject.name.Split('/');if(parts.Length>=3&&int.TryParse(parts[1].Trim(),out int cx)&&int.TryParse(parts[2].Trim(),out int cz))centre=new Vector3(cx*100+50,8,cz*100+50);chunkBounds=new Bounds(centre,new Vector3(135,70,135));chunkBoundsReady=true;}visibleChunk=ChunkVisible(camera,chunkBounds);if(!visibleChunk)return;}
   if(Time.unscaledTime>=nextOcclusion){nextOcclusion=Time.unscaledTime+.12f;HiddenBuildingCount=0;
   for(int i=0;i<buildingBounds.Count;i++){bool hide=false;if(OcclusionEnabled&&camera&&game){hide=Hides(buildingBounds[i],game.PlayerPosition+Vector3.up*1.1f,camera);if(!hide)foreach(var enemy in game.Enemies){if(!enemy||enemy.dead||(enemy.transform.position-game.PlayerPosition).sqrMagnitude>2500)continue;if(Hides(buildingBounds[i],enemy.transform.position+Vector3.up*1.1f,camera)){hide=true;break;}}}hidden[i]=hide;if(hide)HiddenBuildingCount++;}
   }
   if(gameObject.name.StartsWith("Dungeon building /"))foreach(var renderer in GetComponentsInChildren<MeshRenderer>())renderer.enabled=HiddenBuildingCount==0;
   foreach(var group in groups.Values){int count=0;for(int index=0;index<group.matrices.Count;index++){int owner=group.owners[index];if(owner>=0&&hidden[owner])continue;scratch[count++]=group.matrices[index];if(count==1023){Graphics.DrawMeshInstanced(group.mesh,group.sub,group.material,scratch,count,null,Application.isMobilePlatform?ShadowCastingMode.Off:ShadowCastingMode.On,true,gameObject.layer);count=0;}}if(count>0)Graphics.DrawMeshInstanced(group.mesh,group.sub,group.material,scratch,count,null,Application.isMobilePlatform?ShadowCastingMode.Off:ShadowCastingMode.On,true,gameObject.layer);}
  }
 }
}
