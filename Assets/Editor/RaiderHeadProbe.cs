using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace NeonVault.Editor {
 public static class RaiderHeadProbe {
  public static void Run(){var text=new StringBuilder();foreach(var name in new[]{"Enemy_10","Enemy_11","RaiderBossNPC"}){var prefab=Resources.Load<GameObject>("ImportedEnemies/"+name);if(!prefab)continue;var instance=Object.Instantiate(prefab);text.AppendLine(name);foreach(var t in instance.GetComponentsInChildren<Transform>(true))if(t.name.ToLowerInvariant().Contains("head")||t.name.ToLowerInvariant().Contains("neck"))text.AppendLine(t.name+" position="+t.position.ToString("F4")+" rotation="+t.eulerAngles.ToString("F2")+" scale="+t.lossyScale.ToString("F4"));Object.DestroyImmediate(instance);}File.WriteAllText("D:/Unity/AssetIntegration/raider-head-probe.txt",text.ToString());}
 }
}
