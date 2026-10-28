using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonVault.Editor {
 public sealed class DustDebtBrandSetup : IPreprocessBuildWithReport {
  public int callbackOrder => -100;
  public void OnPreprocessBuild(BuildReport report) { Apply(); }
  [MenuItem("Dust & Debt/Apply app icon")]
  public static void Apply() {
   var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/Brand/DustDebtIcon.png");
   if(!icon)throw new System.Exception("Dust & Debt icon is missing");
   PlayerSettings.productName="DUST & DEBT";
   PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
   var kinds=PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS);
   foreach(var kind in kinds){var slots=PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS,kind);foreach(var slot in slots)slot.SetTexture(icon);PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS,kind,slots);}
   AssetDatabase.SaveAssets();
   Debug.Log("DUST_DEBT_ICON_CONFIGURED");
  }
 }
}
