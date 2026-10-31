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
  static readonly Dictionary<Material,Material> fadedMaterials=new Dictionary<Material,Material>();
  readonly Dictionary<(Mesh,Material,int),Group> groups=new Dictionary<(Mesh,Material,int),Group>();
  readonly List<Bounds> buildingBounds=new List<Bounds>();readonly List<bool> hidden=new List<bool>();static VaultGame game;
  static int planeFrame=-1;static Camera planeCamera;static readonly Plane[] planes=new Plane[6];
  float nextOcclusion;bool visibleChunk=true;bool chunkBoundsReady;Bounds chunkBounds;
  readonly Dictionary<Renderer,Material[]> originalRendererMaterials=new Dictionary<Renderer,Material[]>();
  readonly Dictionary<Renderer,ShadowCastingMode> originalRendererShadows=new Dictionary<Renderer,ShadowCastingMode>();
  readonly Dictionary<int,Renderer[]> buildingRenderers=new Dictionary<int,Renderer[]>();
  int decorationOwner=-1;
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
      if(mat.shader&&mat.shader.name=="Standard"&&(!mat.HasProperty("_Mode")||mat.GetFloat("_Mode")!=1)){
       mat.shader=ImportedWorldAssets.BuiltinStandard();
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
   int owner=batch.decorationOwner;if(building){owner=batch.buildingBounds.Count;var source=Get(prefab).bounds;var bounds=new Bounds(matrix.MultiplyPoint3x4(source.center),Vector3.zero);for(int corner=0;corner<8;corner++)bounds.Encapsulate(matrix.MultiplyPoint3x4(source.center+Vector3.Scale(source.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));batch.buildingBounds.Add(bounds);batch.hidden.Add(false);}
   foreach(var part in Get(prefab).parts){var key=(part.mesh,part.material,part.sub);if(!batch.groups.TryGetValue(key,out var group)){group=new Group{mesh=part.mesh,material=part.material,sub=part.sub};batch.groups.Add(key,group);}group.matrices.Add(matrix*part.local);group.owners.Add(owner);}
  }
  // Register all ordinary renderers belonging to the same site as its instanced shell.
  public void RegisterSiteRenderers(){
   int owner;if(buildingBounds.Count>0)owner=0;else{owner=buildingBounds.Count;buildingBounds.Add(new Bounds(transform.TransformPoint(new Vector3(0,3.5f,6)),new Vector3(13,8,10)));hidden.Add(false);}
   var renderers=GetComponentsInChildren<Renderer>(true);buildingRenderers[owner]=renderers;
   foreach(var renderer in renderers)if(renderer&&renderer.enabled){var bounds=buildingBounds[owner];bounds.Encapsulate(renderer.bounds);buildingBounds[owner]=bounds;}
  }
  public int RegisterBuilding(Bounds bounds){int owner=buildingBounds.Count;buildingBounds.Add(bounds);hidden.Add(false);return owner;}
  // Nearby foundation debris is instanced separately from the facade but belongs
  // to the same building. Choose the owner at the footprint centre before adding it.
  public void BeginBuildingDecoration(Vector3 point){decorationOwner=-1;float best=float.MaxValue;for(int i=0;i<buildingBounds.Count;i++){var center=buildingBounds[i].center;float dx=center.x-point.x,dz=center.z-point.z;float d=dx*dx+dz*dz;if(d<best){best=d;decorationOwner=i;}}}
  public void EndBuildingDecoration(){decorationOwner=-1;}
  public void AddBuildingPart(int owner,Mesh mesh,Material material,Matrix4x4 matrix){
   var key=(mesh,material,0);if(!groups.TryGetValue(key,out var group)){group=new Group{mesh=mesh,material=material,sub=0};groups.Add(key,group);}group.matrices.Add(matrix);group.owners.Add(owner);
  }
  void LateUpdate(){DrawNow();}
  static bool ChunkVisible(Camera camera,Bounds bounds){if(planeFrame!=Time.frameCount||planeCamera!=camera){GeometryUtility.CalculateFrustumPlanes(camera,planes);planeFrame=Time.frameCount;planeCamera=camera;}return GeometryUtility.TestPlanesAABB(planes,bounds);}
  static bool ForegroundNear(Bounds bounds,Vector3 point,Camera camera){
   Vector3 forward=camera.transform.forward;forward.y=0;if(forward.sqrMagnitude<.0001f)return false;forward.Normalize();
   Vector3 side=new Vector3(-forward.z,0,forward.x);Vector3 offset=point-bounds.center;
   float frontExtent=Mathf.Abs(forward.x)*bounds.extents.x+Mathf.Abs(forward.z)*bounds.extents.z;
   float sideExtent=Mathf.Abs(side.x)*bounds.extents.x+Mathf.Abs(side.z)*bounds.extents.z;
   float depth=Vector3.Dot(offset,forward);
   return depth<=-frontExtent+.15f&&depth>=-frontExtent-6f&&Mathf.Abs(Vector3.Dot(offset,side))<=sideExtent+2f;
  }
  bool Hides(Bounds bounds,Vector3 point,Camera camera){
   // Only geometry between the camera and the actor can obscure them. Avoid
   // treating an inflated building bounds as blocking someone just in front.
   if(ForegroundNear(bounds,point,camera))return false;
   var viewport=camera.WorldToViewportPoint(point);if(viewport.z<=0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)return false;var ray=camera.ViewportPointToRay(viewport);return bounds.IntersectRay(ray,out float distance)&&distance<Vector3.Dot(point-ray.origin,ray.direction)-.4f;
  }
  static Material Faded(Material source){
   if(fadedMaterials.TryGetValue(source,out var result))return result;
   var ghostShader=Resources.Load<Shader>("BuildingGhost");
   if(!ghostShader){Debug.LogError("BuildingGhost shader is missing; building opacity cannot be changed.");return source;}
   result=new Material(source);result.shader=ghostShader;
   // Opaque asset textures may carry unrelated alpha (including all zero).
   // Preserve alpha only for source materials explicitly using cutout/transparency.
   bool alpha=source.renderQueue>=2450||(source.HasProperty("_Mode")&&source.GetFloat("_Mode")>0.5f)||(source.HasProperty("_Surface")&&source.GetFloat("_Surface")>0.5f);
   result.SetFloat("_UseTextureAlpha",alpha?1f:0f);
   if(source.HasProperty("_BaseMap")&&source.GetTexture("_BaseMap")){result.SetTexture("_MainTex",source.GetTexture("_BaseMap"));result.SetTextureScale("_MainTex",source.GetTextureScale("_BaseMap"));result.SetTextureOffset("_MainTex",source.GetTextureOffset("_BaseMap"));}
   // 50% transparent keeps the building visible while revealing actors behind it.
   // BuildingGhost writes depth so walls and floors do not repeatedly compound
   // opacity along the same sightline when viewed through a multi-part building.
   if(result.HasProperty("_Color")){var color=result.color;color.a=.50f;result.color=color;}
   result.renderQueue=3000;result.enableInstancing=true;
   fadedMaterials[source]=result;return result;
  }
  void UpdateRendererFades(){
   foreach(var entry in buildingRenderers){bool fade=hidden[entry.Key];foreach(var renderer in entry.Value){
    if(!renderer)continue;
    if(fade&&!originalRendererMaterials.ContainsKey(renderer)){var original=renderer.sharedMaterials;originalRendererMaterials.Add(renderer,original);originalRendererShadows[renderer]=renderer.shadowCastingMode;renderer.shadowCastingMode=ShadowCastingMode.Off;var faded=new Material[original.Length];for(int i=0;i<original.Length;i++)faded[i]=original[i]?Faded(original[i]):null;renderer.sharedMaterials=faded;}
    else if(!fade&&originalRendererMaterials.TryGetValue(renderer,out var saved)){renderer.sharedMaterials=saved;originalRendererMaterials.Remove(renderer);if(originalRendererShadows.TryGetValue(renderer,out var shadows))renderer.shadowCastingMode=shadows;originalRendererShadows.Remove(renderer);}
   }}
  }
  void OnDisable(){foreach(var entry in originalRendererMaterials)if(entry.Key){entry.Key.sharedMaterials=entry.Value;if(originalRendererShadows.TryGetValue(entry.Key,out var shadows))entry.Key.shadowCastingMode=shadows;}originalRendererMaterials.Clear();originalRendererShadows.Clear();}
  public static bool OcclusionEnabled=true;
  public int HiddenBuildingCount {get;private set;}
  public void DrawNow(){
   if(!game)game=Object.FindAnyObjectByType<VaultGame>();var camera=game?game.GameCamera:Camera.main;if(camera&&(!camera.enabled||camera.cullingMask==0))return;
   if(camera&&gameObject.name.StartsWith("Stream /")){if(!chunkBoundsReady){var centre=transform.position;var parts=gameObject.name.Split('/');if(parts.Length>=3&&int.TryParse(parts[1].Trim(),out int cx)&&int.TryParse(parts[2].Trim(),out int cz))centre=new Vector3(cx*100+50,8,cz*100+50);chunkBounds=new Bounds(centre,new Vector3(135,70,135));chunkBoundsReady=true;}visibleChunk=ChunkVisible(camera,chunkBounds);if(!visibleChunk)return;}
   if(Time.unscaledTime>=nextOcclusion){nextOcclusion=Time.unscaledTime+(Application.isMobilePlatform?.25f:.12f);HiddenBuildingCount=0;float maxEnemyDistSq=Application.isMobilePlatform?400f:2500f;
   for(int i=0;i<buildingBounds.Count;i++){bool hide=false;if(OcclusionEnabled&&camera&&game){var playerPoint=game.PlayerPosition+Vector3.up*1.1f;hide=Hides(buildingBounds[i],playerPoint,camera);if(!hide&&!ForegroundNear(buildingBounds[i],playerPoint,camera))foreach(var enemy in game.Enemies){if(!enemy||enemy.dead||(enemy.transform.position-game.PlayerPosition).sqrMagnitude>maxEnemyDistSq)continue;if(Hides(buildingBounds[i],enemy.transform.position+Vector3.up*1.1f,camera)){hide=true;break;}}}hidden[i]=hide;if(hide)HiddenBuildingCount++;}
   }
   UpdateRendererFades();
   foreach(var group in groups.Values)for(int pass=0;pass<2;pass++){int count=0;for(int index=0;index<group.matrices.Count;index++){int owner=group.owners[index];bool fade=owner>=0&&hidden[owner];if(fade!=(pass==1))continue;scratch[count++]=group.matrices[index];if(count==1023){Graphics.DrawMeshInstanced(group.mesh,group.sub,fade?Faded(group.material):group.material,scratch,count,null,fade||Application.isMobilePlatform?ShadowCastingMode.Off:ShadowCastingMode.On,true,gameObject.layer);count=0;}}if(count>0)Graphics.DrawMeshInstanced(group.mesh,group.sub,pass==1?Faded(group.material):group.material,scratch,count,null,pass==1||Application.isMobilePlatform?ShadowCastingMode.Off:ShadowCastingMode.On,true,gameObject.layer);}
  }
 }
}


