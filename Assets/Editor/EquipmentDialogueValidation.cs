using System; using System.Reflection; using System.IO; using UnityEngine; using UnityEditor; using NeonVault;
public static class EquipmentDialogueValidation {
 public static void Run(){var obj=new GameObject("Equipment test");try{
 var game=obj.AddComponent<VaultGame>();game.gun=Arsenal.Starter();
 var item=new MeleeWeaponItem{affixes=new[]{new GearAffix{type="damage",value=.2f},new GearAffix{type="fireRate",value=.15f},new GearAffix{type="critChance",value=.08f},new GearAffix{type="critDamage",value=.3f}}};
 var prop=typeof(VaultGame).GetProperty("EquippedMeleeWeapon",BindingFlags.Public|BindingFlags.Instance);float dmg=game.EffectiveDamage,rate=game.EffectiveFireRate,chance=game.EffectiveCriticalChance,mult=game.EffectiveCriticalMultiplier;prop.GetSetMethod(true).Invoke(game,new object[]{item});
 if(!(game.EffectiveDamage>dmg&&game.EffectiveFireRate>rate&&game.EffectiveCriticalChance>chance&&game.EffectiveCriticalMultiplier>mult))throw new Exception("Melee affix did not improve all four firearm properties");
 foreach(var key in new[]{"knox","vale","raven","vex","sera","briggs","iona","rook"}){var path="Assets/Resources/Art/Portraits/portrait-"+key+"-v3.png";if(!AssetDatabase.LoadAssetAtPath<Texture2D>(path))throw new Exception("Portrait missing: "+key);}
 if(!AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/AprilGothicOne-R.ttf"))throw new Exception("April font missing");
 Debug.Log("EQUIPMENT_DIALOGUE_VALIDATION_PASS damage="+dmg+" -> "+game.EffectiveDamage+" fire="+rate+" -> "+game.EffectiveFireRate);
 } finally {UnityEngine.Object.DestroyImmediate(obj);}}
}
