using PlayerPrefs = NeonVault.SaveSlotPrefs;
using UnityEngine;
namespace NeonVault {public partial class VaultGame {
 bool restoringChampionCheckpoint;
 void DropBossRelic(Foe foe,int bossId)
 {
  string key="Vault.BossRelic.Defeated."+bossId;
  bool first=PlayerPrefs.GetInt(key,0)==0;
  // Exclusive relic drops from the existing legendary probability pool rather than 100% guaranteed on first kill.
  Rarity rarity=ExpeditionLootRarity(rng,true);
  if(rarity!=Rarity.Legendary&&rng.NextDouble()<.2375)rarity=Rarity.Legendary;
  if(rarity==Rarity.Legendary&&rng.NextDouble()<.60)
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
  double roll=rng.NextDouble();Rarity rarity=roll<.05?Rarity.Legendary:roll<.30?Rarity.Epic:Rarity.Rare;
  petEnemyLootRoll=true;try{rarity=PetImproveEnemyRarity(rng,rarity,boss);}finally{petEnemyLootRoll=false;}
  int level=ExpeditionLootLevel(rng);var p=foe.transform.position+Vector3.right*.7f;
  if(boss&&rarity==Rarity.Legendary&&IsOverworld&&TryGetLandmarkBossId(foe,out int landmarkId)&&DungeonBossExclusive.TryGetLandmarkExclusive(landmarkId,out var lExcl)&&rng.NextDouble()<.5)
  {
   int lLevel=GetOverworldEnemyLevel(foe);
   var item=DungeonBossExclusive.CreateStageExclusiveItem(lExcl,rng,lLevel);
   if(item is Gun lg)AddDrop(lg,p);
   else if(item is ArmorItem la)AddArmorDrop(la,p);
   else if(item is ArtifactItem lm)AddArtifactDrop(lm,p);
   else if(item is MeleeWeaponItem lw)AddMeleeDrop(lw,p);
   return;
  }
  if(boss&&rarity==Rarity.Legendary&&!IsOverworld&&DungeonBossExclusive.TryGetStageExclusive(SelectedStageId,out var excl)&&rng.NextDouble()<.5)
  {
   var item=DungeonBossExclusive.CreateStageExclusiveItem(excl,rng,level);
   if(item is Gun eg)AddDrop(eg,p);
   else if(item is ArmorItem ea)AddArmorDrop(ea,p);
   else if(item is ArtifactItem em)AddArtifactDrop(em,p);
   else if(item is MeleeWeaponItem ew)AddMeleeDrop(ew,p);
   return;
  }
  if(boss&&rarity==Rarity.Legendary&&!IsOverworld&&(finalDebtBossFight||IsFactionHeadquarters(SelectedStageId))&&rng.NextDouble()<.5)
  {
   int bId=finalDebtBossFight?2:IsInfectedBossStage(SelectedStageId)?3:IsRedClawBossStage(SelectedStageId)?0:1;
   var relic=BossRelicFoundation.Create(bId,rng.Next(BossRelicFoundation.CategoryCount),level,rng);
   if(relic is Gun rg)AddDrop(rg,p);
   else if(relic is ArmorItem ra)AddArmorDrop(ra,p);
   else if(relic is ArtifactItem rm)AddArtifactDrop(rm,p);
   else if(relic is MeleeWeaponItem rw)AddMeleeDrop(rw,p);
   return;
  }
  switch(rng.Next(4)){case 0:AddDrop(Arsenal.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;case 1:AddArmorDrop(Armory.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;case 2:AddArtifactDrop(ArtifactCatalog.Create(rng,DungeonIndex,rarity,level),p);break;default:AddMeleeDrop(MeleeWeaponCatalog.Create(rng,level,rarity),p);break;}
 }
}}
