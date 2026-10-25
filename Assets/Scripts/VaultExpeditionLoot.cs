using System;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Keep the original dungeon pools outside contracts. Contracts favor a
        // family per site and mission, without removing any item from the pool.
        int ExpeditionLootFocus(int count)
        {
            var stage=SelectedExpeditionStage;
            return stage==null?0:(stage.region+stage.id%20+(int)stage.mission)%count;
        }
        int ExpeditionLootLevel(System.Random random)
        {
            var stage=SelectedExpeditionStage;
            return stage==null?DungeonCatalog.ItemLevel(random,DungeonIndex):Mathf.Max(1,stage.level+random.Next(-1,3));
        }
        Rarity ExpeditionLootRarity(System.Random random,bool boss)
        {
            Rarity rarity=DungeonCatalog.RollRarity(random,boss);
            var stage=SelectedExpeditionStage;
            if(stage!=null&&random.NextDouble()<.10+stage.region*.06+(int)stage.mission*.03)
                rarity=(Rarity)Mathf.Max((int)rarity,(int)DungeonCatalog.RollRarity(random,boss));
            return rarity;
        }
        // Contract equipment has its own rarity table; ordinary enemy/cache drops
        // keep their existing rates. A first-clear cache adds ten percentage points
        // to legendary by taking them from the lowest eligible tier.
        public static Rarity RollExpeditionRewardRarity(System.Random random,bool firstClear=false)
        {
            double roll=random.NextDouble();
            if(roll<(firstClear?.25:.15))return Rarity.Legendary;
            if(roll<(firstClear?.60:.50))return Rarity.Epic;
            return Rarity.Rare;
        }
        public Gun RollExpeditionGun(System.Random random,Rarity rewardRarity)
        {
            if(SelectedExpeditionStage==null)return Arsenal.CreateForDungeon(random,DungeonIndex,rewardRarity,DungeonCatalog.ItemLevel(random,DungeonIndex));
            int level=ExpeditionLootLevel(random),focus=ExpeditionLootFocus(4);
            Gun item=null;
            for(int i=0;i<10;i++)
            {
                item=Arsenal.CreateForDungeon(random,DungeonIndex,rewardRarity,level);
                if((int)Arsenal.Category(item.modelVariant)==focus)break;
            }
            return item;
        }
        public ArmorItem RollExpeditionArmor(System.Random random,Rarity rewardRarity)
        {
            if(SelectedExpeditionStage==null)return Armory.CreateForDungeon(random,DungeonIndex,rewardRarity,DungeonCatalog.ItemLevel(random,DungeonIndex));
            int level=ExpeditionLootLevel(random),focus=1+ExpeditionLootFocus(4);
            ArmorItem item=null;
            for(int i=0;i<10;i++)
            {
                item=Armory.CreateForDungeon(random,DungeonIndex,rewardRarity,level);
                if((int)item.slot==focus)break;
            }
            return item;
        }
        public Gun RollExpeditionGun(System.Random random,bool boss=false)
        {
            if(SelectedExpeditionStage==null)return Arsenal.RollForDungeon(random,DungeonIndex,boss);
            Rarity rarity=ExpeditionLootRarity(random,boss);int level=ExpeditionLootLevel(random);
            bool focus=random.NextDouble()<.70;Gun item=null;
            for(int i=0;i<10;i++)
            {
                item=Arsenal.CreateForDungeon(random,DungeonIndex,rarity,level);
                if(!focus||(int)Arsenal.Category(item.modelVariant)==ExpeditionLootFocus(4))break;
            }
            return item;
        }
        public ArmorItem RollExpeditionArmor(System.Random random,bool boss=false)
        {
            if(SelectedExpeditionStage==null)return Armory.RollForDungeon(random,DungeonIndex,boss);
            Rarity rarity=ExpeditionLootRarity(random,boss);int level=ExpeditionLootLevel(random);
            bool focus=random.NextDouble()<.70;ArmorItem item=null;
            for(int i=0;i<10;i++)
            {
                item=Armory.CreateForDungeon(random,DungeonIndex,rarity,level);
                if(!focus||(int)item.slot==1+ExpeditionLootFocus(4))break;
            }
            return item;
        }
        public ArtifactItem RollExpeditionArtifact(System.Random random,bool boss=false)
        {
            if(SelectedExpeditionStage==null)return ArtifactCatalog.RollForDungeon(random,DungeonIndex,boss);
            Rarity rarity=ExpeditionLootRarity(random,boss);int level=ExpeditionLootLevel(random);
            bool focus=random.NextDouble()<.70;ArtifactItem item=null;
            for(int i=0;i<10;i++)
            {
                item=ArtifactCatalog.Create(random,DungeonIndex,rarity,level);
                if(!focus||(int)item.effect==ExpeditionLootFocus(4))break;
            }
            return item;
        }
        public MeleeWeaponItem RollExpeditionMelee(System.Random random,bool boss=false)
        {
            if(SelectedExpeditionStage==null)return MeleeWeaponCatalog.RollForDungeon(random,DungeonIndex,boss);
            Rarity rarity=ExpeditionLootRarity(random,boss);int level=ExpeditionLootLevel(random);
            bool focus=random.NextDouble()<.70;MeleeWeaponItem item=null;
            for(int i=0;i<10;i++)
            {
                item=MeleeWeaponCatalog.Create(random,level,rarity);
                if(!focus||(int)item.family==ExpeditionLootFocus(5))break;
            }
            return item;
        }
        public string ExpeditionLootDescription(int stageId)
        {
            var stage=ExpeditionMap.Get(stageId);
            int focus=(stage.region+stage.id%20+(int)stage.mission)%4;
            string[] en={"handguns","shotguns","assault rifles","sniper rifles"};
            string[] ja={"ハンドガン","ショットガン","アサルトライフル","スナイパーライフル"};
            return T("LOOT FOCUS: ","戦利品傾向: ")+T(en[focus],ja[focus])+T(" / gear Lv "," / 装備Lv ")+Mathf.Max(1,stage.level-1)+"–"+(stage.level+2);
        }
    }
}
