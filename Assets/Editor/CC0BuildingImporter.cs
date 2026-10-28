using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor {
 [InitializeOnLoad]
 public sealed class CC0BuildingImporter : IPreprocessBuildWithReport {
  const string Source="Assets/ArtSource/CC0UnfinishedBuildings/";
  const string Output="Assets/Resources/ImportedWorld/CC0Buildings";
  static CC0BuildingImporter(){EditorApplication.delayCall+=EnsureAssets;}
  public int callbackOrder=>0;
  public void OnPreprocessBuild(BuildReport report){EnsureAssets();}
  public static void EnsureAssets(){
   if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureAssets;return;}
   if(!AssetDatabase.IsValidFolder(Output))AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld","CC0Buildings");
   var diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"UnfinishedAtlas_1k_Base Color.png");
   var normalPath=Source+"UnfinishedAtlas_1k_NORMAL.png";
   var normalImporter=AssetImporter.GetAtPath(normalPath) as TextureImporter;
   if(!diffuse||!normalImporter){Debug.LogWarning("CC0 building textures are not imported yet.");return;}
   if(normalImporter.textureType!=TextureImporterType.NormalMap){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();}
   var materialPath=Output+"/UnfinishedAtlas.mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(!mat){mat=new Material(Shader.Find("Standard")){name="CC0 unfinished building atlas",mainTexture=diffuse,enableInstancing=true};mat.SetFloat("_Glossiness",.08f);mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.EnableKeyword("_NORMALMAP");AssetDatabase.CreateAsset(mat,materialPath);}
   for(int i=1;i<=10;i++){
    var output=Output+"/Structures_"+i+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(output))continue;
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Structures_"+i+".fbx");if(!model){Debug.LogWarning("Missing CC0 building model "+i);continue;}
    var instance=PrefabUtility.InstantiatePrefab(model) as GameObject;if(!instance)continue;
    bool valid=true;int parts=0;
    foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)){
     var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||filter.sharedMesh.uv.Length!=filter.sharedMesh.vertexCount){valid=false;break;}
     var slots=renderer.sharedMaterials;
     for(int slot=0;slot<slots.Length;slot++){
      if(!slots[slot]||!slots[slot].name.ToLowerInvariant().Contains("unfinishedatlas")){valid=false;break;}
      slots[slot]=mat;
     }
     renderer.sharedMaterials=slots;parts++;
    }
    if(valid&&parts>0)PrefabUtility.SaveAsPrefabAsset(instance,output);
    else Debug.LogWarning("CC0 building has unsupported UVs or material slots: "+i);
    Object.DestroyImmediate(instance);
   }
   AssetDatabase.SaveAssets();
  }
 }
}
