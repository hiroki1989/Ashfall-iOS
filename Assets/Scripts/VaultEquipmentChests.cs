using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Rejection sampling multiplies each top-three weight by 1.05 before
        // normalization, while retaining the normal drop table and balance profile.
        Rarity EquipmentChestRarity()
        {
            while(true)
            {
                Rarity rarity=ExpeditionLootRarity(rng,false);
                if(rarity>=Rarity.Rare||rng.NextDouble()<1.0/1.05)return rarity;
            }
        }
        void DropEquipmentChestReward(Vector3 position,int level)
        {
            Rarity rarity=EquipmentChestRarity();
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
