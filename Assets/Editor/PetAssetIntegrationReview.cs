using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class PetAssetIntegrationReview {
 public static void Run() {
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PetDog/GermanShepherd.prefab");
  if(!prefab)throw new Exception("Pet prefab missing");
  var dog=UnityEngine.Object.Instantiate(prefab);
  try {
   var anim=dog.GetComponentInChildren<Animator>();
   if(!anim||!anim.runtimeAnimatorController)throw new Exception("Pet animator/controller missing");
   foreach(var state in new[]{"1 type_Run Loop","1 type_Walk_Loop","1 type_Idle Breathing","1 type_Idle_Playing"}) if(!anim.HasState(0,Animator.StringToHash(state)))throw new Exception("Pet state missing: "+state);
   foreach(var bone in new[]{"DEF-jaw","DEF-thigh.L","DEF-thigh.R","DEF-shin.L","DEF-shin.R"}) if(Array.Find(dog.GetComponentsInChildren<Transform>(true),t=>t.name==bone)==null)throw new Exception("Pet bone missing: "+bone);
   if(!dog.GetComponentInChildren<SkinnedMeshRenderer>())throw new Exception("Pet skin missing");
   if(!Resources.Load<Texture2D>("PetDog/GermanShepherdBase"))throw new Exception("Pet texture missing");
   if(!Resources.Load<GameObject>("ImportedWorld/RustyVehicles/rusty_sedan")||!Resources.Load<GameObject>("ImportedWorld/RustyVehicles/rusty_pickup"))throw new Exception("vehicle prefab missing");
   File.WriteAllText(@"D:\Unity\BuildTemp\PetAssetIntegrationReview.txt","PASS: dog prefab, animator states, five bones, skin, texture, two vehicle prefabs"); Debug.Log("PET_ASSET_INTEGRATION_PASS");
  } finally {UnityEngine.Object.DestroyImmediate(dog);}
 }
}
