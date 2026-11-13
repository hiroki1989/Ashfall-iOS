using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Rare chests retain their advantage while shifting the baseline common and
        // uncommon rates by 5 and 3 percentage points, respectively.
        // Normal chests use the unmodified expedition drop table.
        Rarity EquipmentChestRarity(bool rare)
        {
            while(true)
            {
                Rarity rarity=ExpeditionLootRarity(rng,false);
                if(!rare||rarity>=Rarity.Rare||rng.NextDouble()<(rarity==Rarity.Common?.2843298271889887:.2844521998029663))return rarity;
            }
        }
        void DropEquipmentChestReward(Vector3 position,int level,bool rare=false)
        {
            Rarity rarity=EquipmentChestRarity(rare);
            Vector3 p=position+Vector3.forward*1.1f;
            if(Blocked(p,.35f))p=position;
            switch(rng.Next(3))
            {
                case 0:AddDrop(Arsenal.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;
                case 1:AddArmorDrop(Armory.CreateForDungeon(rng,DungeonIndex,rarity,level),p);break;
                default:AddArtifactDrop(ArtifactCatalog.Create(rng,DungeonIndex,rarity,level),p);break;
            }
        }
    }
}
