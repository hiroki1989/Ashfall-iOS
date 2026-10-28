using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault {
 public sealed class ImportedSurfaceLifetime:MonoBehaviour {public Material material;void OnDestroy(){if(material)Destroy(material);}}
 public static class ImportedWorldAssets {
  static readonly Dictionary<string,GameObject[]> groups=new Dictionary<string,GameObject[]>();
  static Shader builtinStandard;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Register(){OverworldScenery.AssetAdapter=Place;WorldEntranceBuildings.AssetAdapter=Place;}
  // Curated props use existing scenery anchors; source geometry is normalized to a
  // bounded footprint so decorative meshes do not block navigation or cover sites.
  static bool PlacePolyHaven(string category,Transform parent,Vector3 position,int seed){
   if(!parent.name.StartsWith("Stream /"))return false;
   string[] names; float maxWidth,maxHeight,maxDepth;
   if(category=="street") { names=new[]{"fire_hydrant","metal_trash_can"}; maxWidth=1.0f;maxHeight=3.5f;maxDepth=1.0f; }
   else if(category=="debris") { names=new[]{"ammo_box","barrel_stove","concrete_road_barrier","metal_toolbox","old_tyre","tree_stump_01"};maxWidth=1.3f;maxHeight=1.2f;maxDepth=1.3f; }
   else if(category=="car") { names=new[]{"covered_car"};maxWidth=1.8f;maxHeight=1.6f;maxDepth=4.2f; }
   else return false;
   // Keep a mix of the existing scenery and the new set.
   if((seed&3)!=0)return false;
   string name=names[((seed&int.MaxValue)/4)%names.Length];
   var source=Resources.Load<GameObject>("ImportedWorld/PolyHaven/"+name);if(!source)return false;
   var bounds=ImportedWorldBatch.SourceBounds(source);
   if(bounds.size.x<.001f||bounds.size.y<.001f||bounds.size.z<.001f)return false;
   float scale=Mathf.Min(maxWidth/bounds.size.x,maxHeight/bounds.size.y,maxDepth/bounds.size.z);
   var rotation=Quaternion.Euler(0,category=="car"?0:(seed%4)*90,0);
   var offset=rotation*new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
   ImportedWorldBatch.Add(parent,source,position+offset,rotation,Vector3.one*scale);return true;
  }
  public static bool Place(string category,Transform parent,Vector3 position,int seed){
   if(PlacePolyHaven(category,parent,position,seed))return true;
   if(!groups.TryGetValue(category,out var prefabs)){prefabs=Array.FindAll(Resources.LoadAll<GameObject>("ImportedWorld/"+category),p=>!EquipmentChestVisual.IsReserved(p));groups[category]=prefabs;}
   if(prefabs.Length==0)return false;
   var source=prefabs[(seed&int.MaxValue)%prefabs.Length];
   if(parent.name.StartsWith("Stream /")){var scale=Vector3.one;var place=position;var name=source.name.ToLowerInvariant();if(name.Contains("barrel")||name.Contains("drum")||name.Contains("cone")){var bounds=ImportedWorldBatch.SourceBounds(source);float height=name.Contains("cone")?.72f:.90f;if(bounds.size.y>.001f){float s=height/bounds.size.y;scale=Vector3.one*s;place.y-=bounds.min.y*s;}}ImportedWorldBatch.Add(parent,source,place,Quaternion.Euler(0,category=="building"||category=="car"?0:seed%4*90,0),scale);return true;}
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
    PlaceBlockedBackdrop(root,layout,"props",i%2==0?"WaterTank":"barrel_open",new Vector3(-15-i%2*1.6f,0,-10+i*2),i*90);
    PlaceBlockedBackdrop(root,layout,"props",i%2==0?"pallet":"OxygenTank",new Vector3(15+i%2*1.6f,0,-9+i*2),i*90);
   }
   // A defended supply yard gives the safehouse a working perimeter. All props
   // are fitted and checked against the existing closed cells before retaining them.
   for(int i=0;i<6;i++){
    float x=-15+i*6;
    PlaceHubSupply(root,layout,"props","SM_fence_stand_small",new Vector3(x,0,-16),0,3.1f,2.2f);
   }
   string[] supplies={"pbr_box_supply","pbr_box_ammo","pbr_box_med","pallet","WaterTank","barrel_open"};
   for(int i=0;i<supplies.Length;i++){
    float side=i<3?-1:1;
    PlaceHubSupply(root,layout,"props",supplies[i],new Vector3(side*(15.8f+(i%2)*1.5f),0,-5+(i%3)*3.2f),i*90,1.5f,1.8f);
   }
   PlaceHubSupply(root,layout,"PolyHaven","concrete_road_barrier",new Vector3(-17,0,12),90,2.6f,1.1f);
   PlaceHubSupply(root,layout,"PolyHaven","concrete_road_barrier",new Vector3(17,0,12),90,2.6f,1.1f);
   // Small furnishing clusters define a market, records corner and supply area.
   // Each occupies a single navigation cell, leaving the central route clear.
   PlaceHubStation(root,layout,"table",new Vector3(-9.9f,0,-4.4f),0,1.25f);
   PlaceHubStation(root,layout,"pbr_box_supply",new Vector3(-12.1f,0,-4.4f),20,.9f);
   PlaceHubStation(root,layout,"rack",new Vector3(9.9f,0,-4.4f),180,1.8f);
   PlaceHubStation(root,layout,"pbr_box_med",new Vector3(12.1f,0,-4.4f),-20,.9f);
   PlaceHubStation(root,layout,"pallet",new Vector3(-4.4f,0,-9.9f),0,.4f);
   PlaceHubStation(root,layout,"pbr_box_ammo",new Vector3(-6.6f,0,-9.9f),15,.9f);
   PlaceHubStation(root,layout,"bench",new Vector3(4.4f,0,-9.9f),0,1.0f);
   PlaceHubStation(root,layout,"pbr_box_supply",new Vector3(6.6f,0,-9.9f),-15,.9f);
   // Stock and workshop details sit just outside the playable circle.  Fit each
   // asset to a modest footprint before checking the entire blocked-cell area.
   PlaceHubDetail(root,layout,"rack",new Vector3(-15.5f,0,0),90,2.2f,2.2f);
   PlaceHubDetail(root,layout,"pbr_box_supply",new Vector3(-15.5f,0,-3),25,1.3f,1.1f);
   PlaceHubDetail(root,layout,"pbr_box_ammo",new Vector3(-15.5f,0,3),-15,1.3f,1.1f);
   PlaceHubDetail(root,layout,"table",new Vector3(15.5f,0,0),90,2.2f,1.3f);
   PlaceHubDetail(root,layout,"pbr_box_med",new Vector3(15.5f,0,-3),20,1.3f,1.1f);
   PlaceHubDetail(root,layout,"pallet",new Vector3(15.5f,0,3),-15,1.7f,.5f);
   PlaceHubDetail(root,layout,"bench",new Vector3(-9,0,-14.5f),0,2.0f,1.1f);
   PlaceHubDetail(root,layout,"pbr_box_supply",new Vector3(9,0,-14.5f),15,1.3f,1.1f);
   // Realistic props stay entirely inside existing blocked cells so navigation is unchanged.
   PlaceBlockedBackdrop(root,layout,"PolyHaven","barrel_stove",new Vector3(-17,0,8),0,.7f);
   PlaceBlockedBackdrop(root,layout,"PolyHaven","metal_trash_can",new Vector3(17,0,7),90,.7f);
  }
  static void PlaceHubSupply(Transform root,SectorLayout layout,string category,string name,Vector3 position,float yaw,float footprint,float height){
   var source=Resources.Load<GameObject>("ImportedWorld/"+category+"/"+name);if(!source)return;
   var instance=UnityEngine.Object.Instantiate(source,root,false);instance.name="Safehouse perimeter / "+name;
   instance.transform.localRotation=Quaternion.Euler(0,yaw,0);
   var rs=instance.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(instance);return;}
   Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   float scale=Mathf.Min(footprint/Mathf.Max(.01f,b.size.x),footprint/Mathf.Max(.01f,b.size.z),height/Mathf.Max(.01f,b.size.y));
   instance.transform.localScale*=scale;
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var target=root.TransformPoint(position);instance.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var min=root.InverseTransformPoint(b.min);var max=root.InverseTransformPoint(b.max);
   var first=layout.ToCell(min);var last=layout.ToCell(max);bool safe=true;
   for(int x=first.x;x<=last.x&&safe;x++)for(int z=first.y;z<=last.y;z++)
    if(!layout.Blocked(layout.Center(new Vector2Int(x,z)),0)){safe=false;break;}
   if(!safe){instance.SetActive(false);UnityEngine.Object.Destroy(instance);return;}
   foreach(var collider in instance.GetComponentsInChildren<Collider>())collider.enabled=false;
  }
  static void PlaceHubStation(Transform root,SectorLayout layout,string name,Vector3 position,float yaw,float height){
   var cell=layout.ToCell(position);if(!layout.IsOpen(cell.x,cell.y))return;
   var source=Resources.Load<GameObject>("ImportedWorld/props/"+name);if(!source)return;
   var instance=UnityEngine.Object.Instantiate(source,root,false);instance.name="Safehouse station / "+name;
   var centre=layout.Center(cell);centre.y=0;instance.transform.localPosition=centre;instance.transform.localRotation=Quaternion.Euler(0,yaw,0);
   var rs=instance.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(instance);return;}
   Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   float limit=layout.CellSize-.45f;
   float scale=Mathf.Min(limit/Mathf.Max(.01f,b.size.x),limit/Mathf.Max(.01f,b.size.z),height/Mathf.Max(.01f,b.size.y));
   instance.transform.localScale*=scale;b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var target=root.TransformPoint(centre);instance.transform.position+=new Vector3(target.x-b.center.x,target.y-.06f-b.min.y,target.z-b.center.z);
   // Never isolate any part of the hub by closing a furnishing cell.
   layout.open[cell.x,cell.y]=false;
   if(!HubOpenCellsConnected(layout)){layout.open[cell.x,cell.y]=true;instance.SetActive(false);UnityEngine.Object.Destroy(instance);return;}
   foreach(var collider in instance.GetComponentsInChildren<Collider>())collider.enabled=false;
  }
  static bool HubOpenCellsConnected(SectorLayout layout){
   var start=layout.ToCell(new Vector3(0,0,-3));if(!layout.IsOpen(start.x,start.y))return false;
   var seen=new bool[layout.Columns,layout.Rows];var pending=new Queue<Vector2Int>();pending.Enqueue(start);seen[start.x,start.y]=true;
   var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
   while(pending.Count>0){var cell=pending.Dequeue();foreach(var d in directions){var n=cell+d;if(layout.IsOpen(n.x,n.y)&&!seen[n.x,n.y]){seen[n.x,n.y]=true;pending.Enqueue(n);}}}
   for(int x=0;x<layout.Columns;x++)for(int z=0;z<layout.Rows;z++)if(layout.IsOpen(x,z)&&!seen[x,z])return false;
   return true;
  }
  static void PlaceHubDetail(Transform root,SectorLayout layout,string name,Vector3 position,float yaw,float footprint,float height){
   var source=Resources.Load<GameObject>("ImportedWorld/props/"+name);if(!source)return;
   var instance=UnityEngine.Object.Instantiate(source,root,false);instance.name="Safehouse interior / "+name;
   instance.transform.localPosition=position;instance.transform.localRotation=Quaternion.Euler(0,yaw,0);
   var rs=instance.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(instance);return;}
   Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   float scale=Mathf.Min(footprint/Mathf.Max(.01f,b.size.x),footprint/Mathf.Max(.01f,b.size.z),height/Mathf.Max(.01f,b.size.y));
   instance.transform.localScale*=scale;
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var target=root.TransformPoint(position);instance.transform.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var min=root.InverseTransformPoint(b.min);var max=root.InverseTransformPoint(b.max);
   var first=layout.ToCell(min);var last=layout.ToCell(max);bool safe=true;
   for(int x=first.x;x<=last.x&&safe;x++)for(int z=first.y;z<=last.y;z++)
    if(!layout.Blocked(layout.Center(new Vector2Int(x,z)),0)){safe=false;break;}
   if(!safe){instance.SetActive(false);UnityEngine.Object.Destroy(instance);return;}
   foreach(var collider in instance.GetComponentsInChildren<Collider>())collider.enabled=false;
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
  public static Shader BuiltinStandard(){
   if(builtinStandard)return builtinStandard;
   // A primitive uses Unity's actual default shader in both Editor and Player.
   // Default-Material.mat is not available through Resources in built Players.
   var probe=GameObject.CreatePrimitive(PrimitiveType.Cube);
   builtinStandard=probe.GetComponent<Renderer>().sharedMaterial.shader;
   probe.SetActive(false);
   if(Application.isPlaying)UnityEngine.Object.Destroy(probe);
   else UnityEngine.Object.DestroyImmediate(probe);
   return builtinStandard;
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
    if(floor)material.color=new Color(.43f,.45f,.44f);
    material.SetFloat("_Glossiness",.15f);material.SetFloat("_Metallic",0);
    var size=r.bounds.size;material.mainTextureScale=new Vector2(Mathf.Max(1,size.x/3),Mathf.Max(1,Mathf.Max(size.z,size.y)/3));
    r.sharedMaterial=material;r.gameObject.AddComponent<ImportedSurfaceLifetime>().material=material;
   }
  }
  static void ReplaceLegacyCovers(Transform root){
   // Replace the whole legacy hierarchy, rather than layering a prop over its truck.
   int index=0;foreach(var legacy in root.GetComponentsInChildren<Transform>()){
    if(legacy.name!="Wrecked utility vehicle")continue;
    string[] names={"PolyHaven/metal_trash_can","PolyHaven/barrel_stove","PolyHaven/metal_toolbox","props/barrel_open"};
    var source=Resources.Load<GameObject>("ImportedWorld/"+names[index++%names.Length]);if(!source)continue;
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
  static readonly Dictionary<string,Material> industrialMaterials=new Dictionary<string,Material>();
  static void SetIndustrialMaterial(GameObject placed,string name){
   if(!industrialMaterials.TryGetValue(name,out var material)){
    var texture=Resources.Load<Texture2D>("ImportedWorld/Industrial/"+name+"_diff");
    if(!texture)return;
    material=new Material(BuiltinStandard());material.name="Industrial / "+name;
    material.mainTexture=texture;material.SetFloat("_Glossiness",.12f);material.SetFloat("_Metallic",0);
    industrialMaterials[name]=material;
   }
   foreach(var renderer in placed.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
   foreach(var collider in placed.GetComponentsInChildren<Collider>())collider.enabled=false;
  }
  static void DressClosedDungeonBlocks(Transform root,SectorLayout layout){
   int count=0;
   foreach(var rect in layout.walls){
    if(count>=6)break;
    if(rect.width<6||rect.height<6)continue;
    var centre=new Vector3(rect.center.x,0,rect.center.y);
    var entry=layout.Entry;var exit=layout.Exit;entry.y=exit.y=0;
    if((centre-entry).sqrMagnitude<100||(centre-exit).sqrMagnitude<100)continue;
    string[] industrial={"control_terminal","tank","tank_capsule","vent"};
    string industrialName=industrial[count%industrial.Length];
    var source=Resources.Load<GameObject>("ImportedWorld/Industrial/"+industrialName);
    if(!source)source=Resources.Load<GameObject>("ImportedWorld/building/house_ruined")??Resources.Load<GameObject>("ImportedWorld/building/Warehouse");
    if(!source)break;
    var placed=UnityEngine.Object.Instantiate(source,root,false);placed.name="Closed industrial detail / "+count;
    if(source.name==industrialName)SetIndustrialMaterial(placed,industrialName);
    placed.transform.localPosition=centre;placed.transform.localRotation=Quaternion.Euler(0,count%2*90,0);
    var rs=placed.GetComponentsInChildren<Renderer>();if(rs.Length==0){UnityEngine.Object.Destroy(placed);continue;}
    Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    float scale=Mathf.Min(1,Mathf.Min((rect.width-.8f)/Mathf.Max(.01f,b.size.x),(rect.height-.8f)/Mathf.Max(.01f,b.size.z),3/Mathf.Max(.01f,b.size.y)));
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
   if(hub)DressHubPerimeter(root,layout);
   DressClosedDungeonBlocks(root,layout);
  }
 }
}
