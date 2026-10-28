using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor {
 [InitializeOnLoad]
 public sealed class PolyHavenFoliageImporter : IPreprocessBuildWithReport {
  static readonly string[] Names = { "weed_plant_02", "grass_medium_01", "street_lamp_01" };
  static PolyHavenFoliageImporter() { EditorApplication.delayCall += EnsureAssets; }
  public int callbackOrder => 1;
  public void OnPreprocessBuild(BuildReport report) { EnsureAssets(); }
  public static void EnsureAssets() {
   if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += EnsureAssets; return; }
   if (!AssetDatabase.IsValidFolder("Assets/Resources/ImportedWorld/undergrowth")) AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld", "undergrowth");
   foreach (var name in Names) {
    bool lamp = name == "street_lamp_01";
    string output = "Assets/Resources/ImportedWorld/" + (lamp ? "PolyHaven" : "undergrowth");
    if (!AssetDatabase.IsValidFolder(output)) AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld", "PolyHaven");
    string prefab = output + "/" + name + ".prefab";
    if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab)) continue;
    string source = "Assets/ArtSource/PolyHaven/" + name + "/";
    var model = AssetDatabase.LoadAssetAtPath<GameObject>(source + name + "_1k.fbx");
    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source + name + "_rgba.png");
    if (!model || !texture) { Debug.LogWarning("Poly Haven foliage source missing: " + name); continue; }
    var importer = AssetImporter.GetAtPath(source + name + "_rgba.png") as TextureImporter;
    if (importer && (importer.maxTextureSize != 512 || !importer.alphaIsTransparency)) {
     importer.maxTextureSize = 512; importer.alphaIsTransparency = true; importer.SaveAndReimport();
     texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source + name + "_rgba.png");
    }
    string matPath = output + "/" + name + "_cutout.mat";
    var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
    if (!material) {
     material = new Material(Shader.Find("Standard")); material.name = "Poly Haven " + name;
     material.mainTexture = texture; material.SetFloat("_Mode", 1); material.SetFloat("_Cutoff", .4f);
     material.SetOverrideTag("RenderType", "TransparentCutout"); material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
     material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero); material.SetInt("_ZWrite", 1);
     material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
     material.SetFloat("_Glossiness", .1f); AssetDatabase.CreateAsset(material, matPath);
    }
    var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
    if (!instance) continue;
    foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)) {
     var slots = renderer.sharedMaterials;
     for (int i = 0; i < slots.Length; i++) slots[i] = material;
     renderer.sharedMaterials = slots;
    }
    PrefabUtility.SaveAsPrefabAsset(instance, prefab); Object.DestroyImmediate(instance);
   }
   AssetDatabase.SaveAssets();
  }
 }
}
