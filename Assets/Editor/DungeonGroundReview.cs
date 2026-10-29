using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NeonVault;
public static class DungeonGroundReview {
 public static void Run(){int checks=0;try{
 for(int theme=0;theme<3;theme++)for(int seed=0;seed<4;seed++){
  var root=new GameObject("Ground review");var materials=new List<Material>();var layout=new SectorLayout(new System.Random(seed+31),theme);var art=new VaultArt(root.transform,materials,new System.Random(seed+31));
  art.DungeonAtmosphere(theme,layout);foreach(var cell in layout.chambers)art.Chamber(layout.Center(cell),0);
  foreach(Transform t in root.GetComponentsInChildren<Transform>(true)){
   if(t.name.StartsWith("Salvage landmark /")){if(Mathf.Abs(t.position.y+.06f)>.001f)throw new Exception("Floating landmark: "+t.position);checks++;}
   if(t.name=="Burning oil drum"&&t.parent==root.transform){if(Mathf.Abs(t.position.y)>.001f)throw new Exception("Floating drum: "+t.position);checks++;}
   if(t.name=="Painted service bay markings"){if(Mathf.Abs(t.position.y+.043f)>.001f)throw new Exception("Floating marks: "+t.position);checks++;}
  }
  UnityEngine.Object.DestroyImmediate(root);foreach(var material in materials)if(material)UnityEngine.Object.DestroyImmediate(material);
 }
 if(checks==0)throw new Exception("No chamber props checked");File.WriteAllText("D:/Unity/dungeon-ground-result.txt","PASS checks="+checks+" themes=3 seeds=4");Debug.Log("DUNGEON_GROUND_REVIEW_OK checks="+checks);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
