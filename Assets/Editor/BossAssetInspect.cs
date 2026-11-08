using System.IO;
using UnityEditor;
using UnityEngine;
public static class BossAssetInspect {
 public static void Run(){var path="Assets/Sci_Fi_Character_08/Mesh/Sci_Fi_Character_08.FBX"; var lines=new System.Text.StringBuilder(); foreach(var o in AssetDatabase.LoadAllAssetsAtPath(path))lines.AppendLine(o.GetType().Name+" "+o.name+(o is AnimationClip a?" length="+a.length:"")); var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci_Fi_Character_08/Prefabs/Sci_Fi_Character_08_01.prefab"); foreach(var a in prefab.GetComponentsInChildren<Animator>(true))lines.AppendLine("Animator controller="+(a.runtimeAnimatorController?a.runtimeAnimatorController.name:"none")+" avatar="+(a.avatar?a.avatar.name:"none"));foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))lines.AppendLine("Renderer "+r.name+" materials="+r.sharedMaterials.Length);Directory.CreateDirectory("D:/Unity/BuildTools");File.WriteAllText("D:/Unity/BuildTools/boss_asset_inspect.txt",lines.ToString());EditorApplication.Exit(0); }
}
