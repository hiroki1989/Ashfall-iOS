using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault {
 public sealed class ImportedSurfaceLifetime:MonoBehaviour {public Material material;void OnDestroy(){if(material)Destroy(material);}}
 public static class ImportedWorldAssets {
  static readonly Dictionary<string,GameObject[]> groups=new Dictionary<string,GameObject[]>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Register(){OverworldScenery.AssetAdapter=Place;WorldEntranceBuildings.AssetAdapter=Place;}
  public static bool Place(string category,Transform parent,Vector3 position,int seed){
   if(!groups.TryGetValue(category,out var prefabs)){prefabs=Resources.LoadAll<GameObject>("ImportedWorld/"+category);groups[category]=prefabs;}
   if(prefabs.Length==0)return false;
   var source=prefabs[(seed&int.MaxValue)%prefabs.Length];
   if(parent.name.StartsWith("Stream /")){ImportedWorldBatch.Add(parent,source,position,Quaternion.Euler(0,category=="building"||category=="car"?0:seed%4*90,0),Vector3.one);return true;}
   var instance=UnityEngine.Object.Instantiate(source,parent,false);foreach(var animator in instance.GetComponentsInChildren<Animator>(true)){animator.Rebind();animator.Update(0);animator.enabled=false;}foreach(var animation in instance.GetComponentsInChildren<Animation>(true)){animation.Stop();animation.enabled=false;}instance.transform.localPosition=position;instance.transform.localRotation=Quaternion.Euler(0,category=="building"||category=="car"?0:seed%4*90,0);return true;
  }
  public static bool PlaceBuilding(Transform parent,Rect footprint,int seed){
   if(parent.name.StartsWith("Stream /")){
    if(!groups.TryGetValue("building",out var sources)){sources=Resources.LoadAll<GameObject>("ImportedWorld/building");groups["building"]=sources;}if(sources.Length==0)return false;
    var source=sources[(seed&int.MaxValue)%sources.Length];var batchBounds=ImportedWorldBatch.SourceBounds(source);
    ImportedWorldBatch.Add(parent,source,new Vector3(footprint.center.x,0,footprint.center.y),Quaternion.identity,new Vector3(footprint.width/Mathf.Max(.1f,batchBounds.size.x),1,footprint.height/Mathf.Max(.1f,batchBounds.size.z)),true);return true;
   }
   int old=parent.childCount;if(!Place("building",parent,new Vector3(footprint.center.x,0,footprint.center.y),seed))return false;
   var placed=parent.GetChild(old);var rs=placed.GetComponentsInChildren<Renderer>();if(rs.Length==0)return true;Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   placed.localScale=Vector3.Scale(placed.localScale,new Vector3(footprint.width/Mathf.Max(.1f,b.size.x),1,footprint.height/Mathf.Max(.1f,b.size.z)));return true;
  }
  public static void PlaceGroundcover(Transform root,Vector3 position,int seed){
   if(root.name.StartsWith("Stream /")){
    if(!groups.TryGetValue("undergrowth",out var sources)){sources=Resources.LoadAll<GameObject>("ImportedWorld/undergrowth");groups["undergrowth"]=sources;}if(sources.Length==0)return;
    var source=sources[(seed&int.MaxValue)%sources.Length];var batchBounds=ImportedWorldBatch.SourceBounds(source);float scale=Mathf.Min(1,.65f/Mathf.Max(.01f,batchBounds.size.y));ImportedWorldBatch.Add(root,source,position,Quaternion.Euler(0,seed%360,0),Vector3.one*scale);return;
   }
   int before=root.childCount;if(!Place("undergrowth",root,position,seed))return;
   var placed=root.GetChild(before);var rs=placed.GetComponentsInChildren<Renderer>();if(rs.Length==0)return;
   Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   placed.localScale*=Mathf.Min(1,.65f/Mathf.Max(.01f,b.size.y));
  }
  static void DressHubPerimeter(Transform root,SectorLayout layout){
   // A visual apron supports every backdrop, without opening navigation cells.
   var apron=GameObject.CreatePrimitive(PrimitiveType.Cube);apron.name="Safehouse exterior ground apron";
   apron.transform.SetParent(root,false);apron.transform.localPosition=new Vector3(0,-.19f,0);apron.transform.localScale=new Vector3(90,.2f,90);
   var collider=apron.GetComponent<Collider>();if(collider){collider.enabled=false;UnityEngine.Object.Destroy(collider);}
   var floor=Array.Find(root.GetComponentsInChildren<MeshRenderer>(),r=>r.name=="Wasteland concrete");
   Material ground=floor!=null?new Material(floor.sharedMaterial):new Material(BuiltinStandard());
   ground.name="Safehouse exterior shared concrete";ground.mainTextureScale=new Vector2(30,30);
   if(floor==null){ground.color=new Color(.36f,.35f,.30f);ground.mainTexture=Resources.Load<Texture2D>("Art/WastelandFloor");}
   apron.GetComponent<MeshRenderer>().sharedMaterial=ground;apron.AddComponent<ImportedSurfaceLifetime>().material=ground;
   // Compact buildings move into the camera composition while staying beyond closed cells.
   PlaceBlockedBackdrop(root,layout,"building","Warehouse",new Vector3(-7,0,17),0,.65f);
   PlaceBlockedBackdrop(root,layout,"building","house_ruined",new Vector3(13,0,13),0,.6f);
   for(int i=0;i<7;i++){
    PlaceBlockedBackdrop(root,layout,"props","SM_fence_stand_small",new Vector3(-18,0,-10+i*3.4f),90);
    PlaceBlockedBackdrop(root,layout,"props","SM_fence_stand_small",new Vector3(18,0,-10+i*3.4f),90);
   }
   for(int i=0;i<5;i++){
    PlaceBlockedBackdrop(root,layout,"props",i%2==0?"pbr_box_supply":"barrel_open",new Vector3(-15-i%2*1.6f,0,-10+i*2),i*90);
    PlaceBlockedBackdrop(root,layout,"props",i%2==0?"pallet":"OxygenTank",new Vector3(15+i%2*1.6f,0,-9+i*2),i*90);
   }
  }
  static void PlaceBlockedBackdrop(Transform root,SectorLayout layout,string category,string name,Vector3 position,float yaw,float scale=1){
   var source=Resources.Load<GameObject>("ImportedWorld/"+category+"/"+name);if(!source)return;
   var instance=UnityEngine.Object.Instantiate(source,root,false);instance.transform.localPosition=position;instance.transform.localRotation=Quaternion.Euler(0,yaw,0);instance.transform.localScale*=scale;
   var rs=instance.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(instance);return;}
   Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   // Every part of the bounds must remain inside already blocked navigation cells.
   for(float x=b.min.x-.5f;x<=b.max.x+.5f;x+=.5f)for(float z=b.min.z-.5f;z<=b.max.z+.5f;z+=.5f)
    if(!layout.Blocked(new Vector3(x,0,z),0)){instance.SetActive(false);UnityEngine.Object.Destroy(instance);return;}
  }
  static Shader BuiltinStandard(){
#if UNITY_EDITOR
   return UnityEditor.AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader");
#else
   // Resolve through Unity's built-in material, not a name potentially shadowed
   // by an imported shader declaring itself "Standard".
   return Resources.GetBuiltinResource<Material>("Default-Material.mat").shader;
#endif
  }
  public static void ApplyOutdoorSurfaces(Material[] materials){
   string[] names={"M_YFPM_BrokenRectangles","M_grnd_grass","M_grnd_asph",null,"M_YFCM_RevealingBricks","M_YFCM_SaltFinish",null,"M_YFPM_BrokenRectangles"};
   for(int i=0;i<materials.Length&&i<names.Length;i++){
    if(names[i]==null)continue;var source=Resources.Load<Material>("ImportedWorld/Surfaces/"+names[i]);if(!source)continue;
    var m=materials[i];m.shader=BuiltinStandard();m.CopyPropertiesFromMaterial(source);m.shader=BuiltinStandard();
    m.color=i==2?new Color(.36f,.37f,.36f):new Color(.72f,.70f,.65f);m.SetFloat("_Glossiness",.08f);m.SetFloat("_Metallic",0);m.mainTextureScale=Vector2.one;
    if(i==0||i==1||i==2||i==5){
     var shader=Resources.Load<Shader>("DistrictGround");var grass=Resources.Load<Material>("ImportedWorld/Surfaces/M_grnd_grass");var dust=Resources.Load<Material>("ImportedWorld/Surfaces/M_YFCM_SaltFinish");
     if(shader&&grass&&dust){m.shader=shader;m.SetTexture("_Generated",Resources.Load<Texture2D>("Art/Environment/Ground/wasteland-materials-v1"));m.SetFloat("_Road",i==2?1:0);m.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));m.SetTexture("_GrassTex",grass.mainTexture);m.SetTexture("_DustTex",dust.mainTexture);m.SetFloat("_Park",i==1?1:0);m.SetColor("_Color",new Color(.72f,.70f,.65f));}
    }
   }
  }
  public static void ApplySurfaces(Transform root){
   var concrete=Resources.LoadAll<Material>("ImportedWorld/Surfaces");if(concrete.Length==0)return;
   var shader=BuiltinStandard();
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
    bool floor=r.name=="Wasteland concrete";
    bool wall=r.name=="Concrete wall"||r.name=="Ruined chamber foundation"||r.name=="Concrete partition cap"||r.name=="Concrete recessed cap panel";
    if(!floor&&!wall)continue;
    var source=Array.Find(concrete,m=>m.name.Contains(wall?"Spalling":"BrushedWorn"))??concrete[0];
    var material=new Material(shader);material.CopyPropertiesFromMaterial(source);material.shader=shader;
    material.SetFloat("_Glossiness",.15f);material.SetFloat("_Metallic",0);
    var size=r.bounds.size;material.mainTextureScale=new Vector2(Mathf.Max(1,size.x/3),Mathf.Max(1,Mathf.Max(size.z,size.y)/3));
    r.sharedMaterial=material;r.gameObject.AddComponent<ImportedSurfaceLifetime>().material=material;
   }
  }
  static void ReplaceLegacyCovers(Transform root){
   // Replace the whole legacy hierarchy, rather than layering a prop over its truck.
   int index=0;foreach(var legacy in root.GetComponentsInChildren<Transform>()){
    if(legacy.name!="Wrecked utility vehicle")continue;
    string[] names={"pbr_box_supply","WaterTank","rack","pbr_box_ammo","barrel_open"};
    var source=Resources.Load<GameObject>("ImportedWorld/props/"+names[index++%names.Length]);if(!source)continue;
    var placed=UnityEngine.Object.Instantiate(source,legacy.parent,false);placed.name="Imported cover / "+source.name;
    placed.transform.localPosition=legacy.localPosition;placed.transform.localRotation=Quaternion.identity;
    var rs=placed.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(placed);continue;}
    Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    // Covers occupy one 2.2m blocked cell; keep a margin within that footprint.
    float scale=Mathf.Min(1.9f/Mathf.Max(.01f,b.size.x),1.9f/Mathf.Max(.01f,b.size.z),1.8f/Mathf.Max(.01f,b.size.y));
    placed.transform.localScale*=scale;
    rs=placed.GetComponentsInChildren<Renderer>();b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    var centre=legacy.position;placed.transform.position+=new Vector3(centre.x-b.center.x,-.06f-b.min.y,centre.z-b.center.z);
    legacy.gameObject.SetActive(false);UnityEngine.Object.Destroy(legacy.gameObject);
   }
  }
  static void DressClosedDungeonBlocks(Transform root,SectorLayout layout){
   int count=0;
   foreach(var rect in layout.walls){
    if(count>=6)break;
    if(rect.width<6||rect.height<6)continue;
    var centre=new Vector3(rect.center.x,0,rect.center.y);
    var entry=layout.Entry;var exit=layout.Exit;entry.y=exit.y=0;
    if((centre-entry).sqrMagnitude<100||(centre-exit).sqrMagnitude<100)continue;
    var source=Resources.Load<GameObject>("ImportedWorld/building/house_ruined")??Resources.Load<GameObject>("ImportedWorld/building/Warehouse");if(!source)break;
    var placed=UnityEngine.Object.Instantiate(source,root,false);placed.name="Closed dungeon ruin / "+count;
    placed.transform.localPosition=centre;placed.transform.localRotation=Quaternion.Euler(0,count%2*90,0);
    var rs=placed.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(placed);continue;}
    Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    float scale=Mathf.Min((rect.width-.8f)/Mathf.Max(.01f,b.size.x),(rect.height-.8f)/Mathf.Max(.01f,b.size.z),3/Mathf.Max(.01f,b.size.y));
    placed.transform.localScale*=scale;
    b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    var worldCentre=root.TransformPoint(centre);placed.transform.position+=new Vector3(worldCentre.x-b.center.x,root.TransformPoint(new Vector3(0,.82f,0)).y-b.min.y,worldCentre.z-b.center.z);
    b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    var min=root.InverseTransformPoint(b.min);var max=root.InverseTransformPoint(b.max);
    bool safe=min.x>=rect.xMin+.2f&&max.x<=rect.xMax-.2f&&min.z>=rect.yMin+.2f&&max.z<=rect.yMax-.2f&&b.size.y<=3.01f;
    // Check all cells touched by the complete visual bounds, including edge cells.
    var first=layout.ToCell(min);var last=layout.ToCell(max);
    for(int x=first.x;x<=last.x&&safe;x++)for(int z=first.y;z<=last.y;z++)
     if(!layout.Blocked(layout.Center(new Vector2Int(x,z)),0)){safe=false;break;}
    if(!safe){placed.SetActive(false);UnityEngine.Object.Destroy(placed);continue;}
    count++;
   }
  }
  public static void DressInterior(Transform root,SectorLayout layout,bool hub){
   ReplaceLegacyCovers(root);
   ApplySurfaces(root);
   if(hub)DressHubPerimeter(root,layout);else DressClosedDungeonBlocks(root,layout);
  }
 }
}
