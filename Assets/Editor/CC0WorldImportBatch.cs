using UnityEditor;

namespace NeonVault.Editor {
 public static class CC0WorldImportBatch {
  public static void Run() {
   PolyHavenWorldImporter.EnsureAssets();
   CC0BuildingImporter.EnsureAssets();
   ApocalypticImporter.EnsureAssets();
   PolyHavenFoliageImporter.EnsureAssets();
   AssetDatabase.SaveAssets();
   AssetDatabase.Refresh();
  }
 }
}
