using UnityEngine;
namespace NeonVault {public partial class VaultGame {
 bool restoringChampionCheckpoint;
 void DropChampionReward(Foe foe,bool boss){
  // Each champion guarantees one quality reward; ordinary drops remain separate.
  double roll=rng.NextDouble();Rarity rarity=roll<(boss?.25:.08)?Rarity.Legendary:roll<(boss?.80:.45)?Rarity.Epic:Rarity.Rare;
  int level=ExpeditionLootLevel(rng);var p=foe.transform.position+Vector3.right*.7f;
  switch(rng.Next(4)){case 0:AddDrop(Arsenal.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;case 1:AddArmorDrop(Armory.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;case 2:AddArtifactDrop(ArtifactCatalog.Create(rng,DungeonIndex,rarity,level),p);break;default:AddMeleeDrop(MeleeWeaponCatalog.Create(rng,level,rarity),p);break;}
 }
}}
