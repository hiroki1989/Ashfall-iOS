using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor {
 public static class CC0AssetReview {
  public static void Run() {
   var props=Resources.LoadAll<GameObject>("ImportedWorld/PolyHaven");
   var buildings=Resources.LoadAll<GameObject>("ImportedWorld/CC0Buildings");
   var ruins=Resources.LoadAll<GameObject>("ImportedWorld/ApocalypticBuildings");
   if(props.Length<9||buildings.Length!=10||ruins.Length!=7)throw new Exception("Unexpected CC0 prefab count: "+props.Length+" / "+buildings.Length+" / "+ruins.Length);
   foreach(var prefab in props) Check(prefab);
   foreach(var prefab in buildings) Check(prefab);
   foreach(var prefab in ruins) Check(prefab);
   foreach(var name in new[]{"control_terminal","tank","tank_capsule","vent"}) {
    var model=Resources.Load<GameObject>("ImportedWorld/Industrial/"+name);
    if(!model||model.GetComponentsInChildren<MeshFilter>(true).Length==0||!Resources.Load<Texture2D>("ImportedWorld/Industrial/"+name+"_diff"))throw new Exception("Missing industrial model or texture: "+name);
   }
   foreach(var name in new[]{"weed_plant_02","grass_medium_01"}){var prefab=Resources.Load<GameObject>("ImportedWorld/undergrowth/"+name);if(!prefab)throw new Exception("Missing foliage: "+name);Check(prefab);}
   Debug.Log("CC0_ASSET_REVIEW_OK props="+props.Length+" buildings="+buildings.Length+" ruins="+ruins.Length+" industrial=4 foliage=2");
  }
  static void Check(GameObject prefab) {
   var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
   if(filters.Length==0)throw new Exception("No meshes: "+prefab.name);
   foreach(var f in filters) {
    var r=f.GetComponent<MeshRenderer>();
    if(!f.sharedMesh||!r||r.sharedMaterials.Length<f.sharedMesh.subMeshCount)throw new Exception("Invalid mesh or slots: "+prefab.name);
    foreach(var m in r.sharedMaterials)if(!m||!m.mainTexture)throw new Exception("Missing material or texture: "+prefab.name);
   }
  }
 }
}
