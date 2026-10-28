using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor {
 [InitializeOnLoad]
 public sealed class ApocalypticImporter : IPreprocessBuildWithReport {
  const string Source="Assets/ArtSource/ApocalypticMajadroid/";
  const string Output="Assets/Resources/ImportedWorld/ApocalypticBuildings";
  static ApocalypticImporter(){EditorApplication.delayCall+=EnsureAssets;}
  public int callbackOrder=>0;
  public void OnPreprocessBuild(BuildReport report){EnsureAssets();}
  public static void EnsureAssets(){
   if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureAssets;return;}
   if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld","ApocalypticBuildings");
   string[] names={"Concrete031_1K_Color","Concrete026_1K_Color","Rocks007_1K_Color","Facade001_1K_Color","Rust006_1K_Color"};
   var mats=new Material[names.Length];
   for(int i=0;i<names.Length;i++){
    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Textures/"+names[i]+".jpg");
    if(!texture){Debug.LogWarning("Apocalyptic texture missing: "+names[i]);return;}
    var path=Output+"/"+names[i]+".mat";mats[i]=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!mats[i]){mats[i]=new Material(Shader.Find("Standard")){name=names[i],mainTexture=texture,enableInstancing=true};mats[i].SetFloat("_Glossiness",.08f);AssetDatabase.CreateAsset(mats[i],path);}
   }
   for(int i=1;i<=7;i++){
    var path=Output+"/building-"+i.ToString("00")+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Models/building-"+i.ToString("00")+".fbx");if(!model){Debug.LogWarning("Apocalyptic building missing: "+i);continue;}
    var instance=PrefabUtility.InstantiatePrefab(model) as GameObject;if(!instance)continue;
    int parts=0;bool valid=true;
    foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)){
     var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||filter.sharedMesh.uv.Length!=filter.sharedMesh.vertexCount){valid=false;break;}
     var slots=renderer.sharedMaterials;
     for(int s=0;s<slots.Length;s++){
      if(!slots[s]){valid=false;break;}
      var name=slots[s].name.ToLowerInvariant();
      int material=name.Contains("rust")?4:name.Contains("rock")?2:name.Contains("moss")?1:name.Contains("concrete")?0:3;
      slots[s]=mats[material];
     }
     renderer.sharedMaterials=slots;parts++;
    }
    if(valid&&parts>0)PrefabUtility.SaveAsPrefabAsset(instance,path);else Debug.LogWarning("Apocalyptic building has unsupported meshes: "+i);
    Object.DestroyImmediate(instance);
   }
   AssetDatabase.SaveAssets();
  }
 }
}
