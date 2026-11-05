using System;
using NeonVault;
using UnityEditor;
using UnityEngine;
public static class MapLevelPortraitQA {
 public static void Run(){try{
  for(int level=1;level<=50;level++) for(int id=0;id<ExpeditionMap.StageCount;id++){var s=ExpeditionMap.Get(id,level);if(s.level<Math.Max(OverworldGeography.DungeonLevel(id),level-1))throw new Exception("Dungeon level floor");}
  var geo=new OverworldGeography();int trees=0,buildings=0;for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++){var c=geo.GetChunk(x,z);trees+=c.Trees.Count;buildings+=c.Buildings.Count;}if(trees==0||buildings==0)throw new Exception("No geography samples");
  foreach(string k in new[]{"raven","vex","sera","knox","vale","briggs","iona","rook"}) if(VaultGame.LoadDialoguePortrait(k)==null)throw new Exception("Missing portrait "+k);
  Debug.Log("MAP_LEVEL_PORTRAIT_QA_PASS trees="+trees+" buildings="+buildings);EditorApplication.Exit(0);
 }catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
}
