using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor {
 // Converts the separately licensed source files into Resources prefabs on first import.
 [InitializeOnLoad]
 public sealed class PolyHavenWorldImporter : IPreprocessBuildWithReport {
  const string Source = "Assets/ArtSource/PolyHaven";
  const string Output = "Assets/Resources/ImportedWorld/PolyHaven";
  static readonly string[] Names = {"ammo_box","barrel_stove","concrete_road_barrier","covered_car","fire_hydrant","metal_toolbox","metal_trash_can","old_tyre","tree_stump_01"};
  static PolyHavenWorldImporter() { EditorApplication.delayCall += EnsureAssets; }
  public int callbackOrder => 0;
  public void OnPreprocessBuild(BuildReport report) { EnsureAssets(); }
  public static void EnsureAssets() {
   if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += EnsureAssets; return; }
   if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets/Resources/ImportedWorld", "PolyHaven");
   foreach (string name in Names) {
    string prefabPath = Output + "/" + name + ".prefab";
    if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) continue;
    string dir = Source + "/" + name + "/";
    var model = AssetDatabase.LoadAssetAtPath<GameObject>(dir + name + "_1k.fbx");
    if (!model) { Debug.LogWarning("Poly Haven source is not yet imported: " + name); continue; }
    var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
    if (!instance) continue;
    bool supported = true;
    foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)) {
     var slots = renderer.sharedMaterials;
     for (int i = 0; i < slots.Length; i++) {
      // FBX material names were checked against its diffuse texture connections.
      string slot = slots[i] ? slots[i].name : "";
      string texture = slot == "tire_r13_01" && name == "old_tyre" ? "old_tyre" : slot;
      bool known = texture == name || (name == "fire_hydrant" && texture == "fire_hydrant_aged") ||
       (name == "metal_trash_can" && texture == "metal_trash_can_rust");
      if (!known) { supported = false; Debug.LogWarning("Unmapped Poly Haven material: " + name + "/" + slot); break; }
      string extension = texture == "fire_hydrant_aged" || texture == "metal_trash_can_rust" ? ".png" : ".jpg";
      string normalExtension = extension == ".png" ? ".png" : ".exr";
      var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + texture + "_diff_1k" + extension);
      string normalPath = dir + texture + "_nor_gl_1k" + normalExtension;
      var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
      if (!diffuse || !normalImporter) { supported = false; Debug.LogWarning("Missing Poly Haven texture: " + texture); break; }
      if (normalImporter.textureType != TextureImporterType.NormalMap) { normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.SaveAndReimport(); }
      string materialPath = Output + "/" + texture + ".mat";
      var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
      if (!material) {
       material = new Material(Shader.Find("Standard")); material.name = "Poly Haven " + texture;
       material.mainTexture = diffuse; material.SetFloat("_Glossiness", .12f);
       material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
       material.EnableKeyword("_NORMALMAP"); AssetDatabase.CreateAsset(material, materialPath);
      }
      slots[i] = material;
     }
     renderer.sharedMaterials = slots;
     if (!supported) break;
    }
    if (!supported) { UnityEngine.Object.DestroyImmediate(instance); continue; }
    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
    UnityEngine.Object.DestroyImmediate(instance);
   }
   AssetDatabase.SaveAssets();
  }
 }
}
