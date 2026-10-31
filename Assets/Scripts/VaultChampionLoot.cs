using UnityEngine;
namespace NeonVault {public partial class VaultGame {
 bool restoringChampionCheckpoint;
 void DropBossRelic(Foe foe,int bossId)
 {
  string key="Vault.BossRelic.Defeated."+bossId;
  bool first=PlayerPrefs.GetInt(key,0)==0;
  if(first||rng.NextDouble()<.475)
  {
   int level=finalDebtBossFight?Mathf.Max(1,DebtLevel):ExpeditionLootLevel(rng);
   var relic=BossRelicFoundation.Create(bossId,rng.Next(BossRelicFoundation.CategoryCount),level,rng);
   var p=foe.transform.position+Vector3.right*.7f;
   if(relic is Gun gun)AddDrop(gun,p);
   else if(relic is ArmorItem armor)AddArmorDrop(armor,p);
   else if(relic is ArtifactItem module)AddArtifactDrop(module,p);
   else if(relic is MeleeWeaponItem melee)AddMeleeDrop(melee,p);
  }
  if(first){PlayerPrefs.SetInt(key,1);PlayerPrefs.Save();}
 }
 void DropChampionReward(Foe foe,bool boss){
  // Each champion guarantees one quality reward; ordinary drops remain separate.
  double roll=rng.NextDouble();Rarity rarity=roll<(boss?.2375:.076)?Rarity.Legendary:roll<(boss?.7625:.4315)?Rarity.Epic:Rarity.Rare;
  int level=ExpeditionLootLevel(rng);var p=foe.transform.position+Vector3.right*.7f;
  switch(rng.Next(4)){case 0:AddDrop(Arsenal.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;case 1:AddArmorDrop(Armory.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;case 2:AddArtifactDrop(ArtifactCatalog.Create(rng,DungeonIndex,rarity,level),p);break;default:AddMeleeDrop(MeleeWeaponCatalog.Create(rng,level,rarity),p);break;}
 }
}}
