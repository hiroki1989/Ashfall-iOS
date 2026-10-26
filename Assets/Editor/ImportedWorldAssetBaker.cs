using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace NeonVault.Editor {
 public static class ImportedWorldAssetBaker {
  [MenuItem("Ashfall/Art/Bake imported environment assets")]
  public static void Bake(){
   BakeGroup("building",new[]{"house_aband","house_ruined","house_enter","Warehouse"},new Vector3(12,8,8));
   BakeGroup("tree",new[]{"Maple_468Poly","Maple_1001Poly","Maple_1152Poly","PF Conifer Small BOTD","PF Conifer Bare BOTD"},new Vector3(5,8,5));
   BakeGroup("props",new[]{"pbr_box_ammo","pbr_box_med","pbr_box_supply","pallet","OxygenTank","WaterTank","CinderBlock","bench","rack","barrel_open","table","ConstructionCone","SM_fence_stand_small"},new Vector3(1.2f,2.2f,1.2f));
   BakeGroup("car",new[]{"P_RSG_Crossroad_Pickup_Clean_BRP"},new Vector3(2,2.5f,5));
   BakeGroup("undergrowth",new[]{"P_Bush01","P_Bush02","Bitterbrush Sapling","Bitterbrush"},new Vector3(1.5f,1.2f,1.5f));
   BakeGroup("rock",new[]{"rock_set_01","rock_set_02","rock_set_03"},new Vector3(1.5f,1,1.5f));
   BakeSurfaces();AssetDatabase.SaveAssets();Debug.Log("IMPORTED_WORLD_BAKE_OK");
  }
  public static void BakeGroundPalette(){
   string folder="Assets/Resources/ImportedWorld/Surfaces";Directory.CreateDirectory(folder);
   string[] paths={"Assets/road_props/Materials/road_props/M_grnd_asph.mat","Assets/Abandoned buildings/Materials/abandoned_buildings/M_grnd_grass.mat","Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_BrokenRectangles.mat","Assets/YughuesFreeConcreteMaterials/Materials/M_YFCM_BrushedWorn.mat","Assets/YughuesFreeConcreteMaterials/Materials/M_YFCM_SaltFinish.mat"};
   foreach(var path in paths){var source=AssetDatabase.LoadAssetAtPath<Material>(path);if(!source)continue;string name=Path.GetFileNameWithoutExtension(path);var shader=AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader");var mat=new Material(shader);mat.CopyPropertiesFromMaterial(source);mat.shader=shader;mat.name=name;mat.color=Color.white;mat.SetFloat("_Glossiness",.08f);mat.SetFloat("_Metallic",0);var target=folder+"/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(target);if(existing){EditorUtility.CopySerialized(mat,existing);UnityEngine.Object.DestroyImmediate(mat);}else AssetDatabase.CreateAsset(mat,target);}
   AssetDatabase.SaveAssets();Debug.Log("GROUND_PALETTE_OK");
  }
  public static void BakeStreetDetails(){
   BakeGroup("street",new[]{"traf_cone","brl_empty_old","brl_empty","blboard","RoadDivider"},new Vector3(2.2f,2.4f,1.2f));
   BakeGroup("debris",new[]{"CinderBlock","pallet","barrel_open"},new Vector3(1.2f,.45f,1.2f));
   AssetDatabase.SaveAssets();Debug.Log("IMPORTED_STREET_BAKE_OK");
  }
  static void BakeSurfaces(){
   string folder="Assets/Resources/ImportedWorld/Surfaces";Directory.CreateDirectory(folder);
   foreach(string path in AssetDatabase.FindAssets("t:Material").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("YughuesFreeConcreteMaterials/Materials/")||p.Contains("YughuesFreePavementsMaterials/Materials/")).Take(24)){
    var source=AssetDatabase.LoadAssetAtPath<Material>(path);var mat=new Material(Shader.Find("Standard"));mat.CopyPropertiesFromMaterial(source);mat.shader=Shader.Find("Standard");mat.SetFloat("_Glossiness",.15f);var target=folder+"/"+source.name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(target);if(existing){EditorUtility.CopySerialized(mat,existing);UnityEngine.Object.DestroyImmediate(mat);}else AssetDatabase.CreateAsset(mat,target);
   }
  }
  static void BakeGroup(string group,string[] names,Vector3 maximum){
   string directory="Assets/Resources/ImportedWorld/"+group;Directory.CreateDirectory(directory);
   var all=AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.StartsWith("Assets/Resources/ImportedWorld/")).ToArray();
   foreach(string name in names){string path=all.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==name);if(path==null){Debug.LogWarning("Missing imported prefab: "+name);continue;}
    var root=new GameObject(name);var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);model.transform.localPosition=Vector3.zero;
    foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
    foreach(var body in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
    foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
    var renderers=model.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0){UnityEngine.Object.DestroyImmediate(root);continue;}
    Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    float scale=Mathf.Min(maximum.x/Mathf.Max(.01f,bounds.size.x),maximum.y/Mathf.Max(.01f,bounds.size.y),maximum.z/Mathf.Max(.01f,bounds.size.z));
    model.transform.localScale*=scale;model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
    PrefabUtility.SaveAsPrefabAsset(root,directory+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
   }
  }
 }
}
