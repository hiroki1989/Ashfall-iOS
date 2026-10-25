using UnityEngine;

namespace NeonVault
{
    public enum StorageUpgrade { AmmoHandgun, Weapons, Armor, Modules, Melee, AmmoShotgun, AmmoRifle, AmmoSniper, AmmoLMG }

    public partial class VaultGame
    {
        static int AmmoIndex(StorageUpgrade category)
        {
            switch(category)
            {
                case StorageUpgrade.AmmoHandgun:return (int)AmmoType.Handgun;
                case StorageUpgrade.AmmoShotgun:return (int)AmmoType.Shotgun;
                case StorageUpgrade.AmmoRifle:return (int)AmmoType.AssaultRifle;
                case StorageUpgrade.AmmoSniper:return (int)AmmoType.SniperRifle;
                case StorageUpgrade.AmmoLMG:return (int)AmmoType.AssaultRifle;
                default:return -1;
            }
        }

        public int StorageTier(StorageUpgrade category)
        {
            int ammoIndex=AmmoIndex(category);
            if(ammoIndex>=0)return ammoCapacityTiers[ammoIndex];
            switch(category)
            {
                case StorageUpgrade.Weapons:return WeaponCapacityTier;
                case StorageUpgrade.Armor:return ArmorCapacityTier;
                case StorageUpgrade.Modules:return ModuleCapacityTier;
                case StorageUpgrade.Melee:return MeleeCapacityTier;
                default:return 0;
            }
        }

        public int StorageUpgradeCost(StorageUpgrade category)
        {
            if((int)category<0||(int)category>8)return 0;
            int tier=StorageTier(category);
            if(tier>=10)return 0;
            int ammoIndex=AmmoIndex(category);
            int baseCost=ammoIndex>=0?2500+ammoIndex*350:category==StorageUpgrade.Weapons?4000:category==StorageUpgrade.Armor?3000:category==StorageUpgrade.Modules?3500:2500;
            return baseCost*(tier+1)*ScrapLevelMultiplier;
        }

        public bool BuyStorageUpgrade(StorageUpgrade category)
        {
            if((int)category<0||(int)category>8||screen!=Screen.Shop)return false;
            int cost=StorageUpgradeCost(category);
            if(cost<=0||!TrySpendCredits(cost))return false;
            int ammoIndex=AmmoIndex(category);
            if(ammoIndex>=0)ammoCapacityTiers[ammoIndex]++;
            else switch(category)
            {
                case StorageUpgrade.Weapons:WeaponCapacityTier++;break;
                case StorageUpgrade.Armor:ArmorCapacityTier++;break;
                case StorageUpgrade.Modules:ModuleCapacityTier++;break;
                case StorageUpgrade.Melee:MeleeCapacityTier++;break;
            }
            SaveInventory();SaveProgression();
            return true;
        }
    }
}
