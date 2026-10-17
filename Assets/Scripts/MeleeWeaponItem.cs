using System;
using UnityEngine;

namespace NeonVault
{
    public enum MeleeWeaponFamily { Knife, Shiv, Cleaver, Machete, Hatchet }

    [Serializable]
    public sealed class MeleeWeaponItem
    {
        public string id, catalogId, manufacturerId;
        public int designVariant;
        public MeleeWeaponFamily family;
        public Rarity rarity;
        public bool uniqueEffect;
        public int level = 1;
        public int damage = 18;
        public float interval = .7f, range = 1.2f, arcDegrees = 85f;
        public Color color=>Arsenal.Colors[Mathf.Clamp((int)rarity,0,Arsenal.Colors.Length-1)];
    }

    public static class MeleeWeaponCatalog
    {
        public const int FamilyCount=5, DesignsPerFamily=10;
        public static int CatalogCount=>FamilyCount*DesignsPerFamily;
        public static void InferIdentity(MeleeWeaponItem item)
        {
            if(item==null)return;item.designVariant=Mathf.Clamp(item.designVariant,0,9);
            if(ManufacturerCatalog.Index(item.manufacturerId)<0)item.manufacturerId=ManufacturerCatalog.ForDesign((int)item.family,item.designVariant);
            if(string.IsNullOrEmpty(item.catalogId))item.catalogId="melee-"+((int)item.family).ToString("00")+"-"+item.designVariant.ToString("00");
        }
        public static MeleeWeaponItem StarterKnife()=>new MeleeWeaponItem
        {id=Guid.NewGuid().ToString("N"),catalogId="melee-00-00",manufacturerId="kestrel",family=MeleeWeaponFamily.Knife,level=1,damage=18,interval=.65f,range=1.15f,arcDegrees=75};

        public static MeleeWeaponItem Create(System.Random random,int level)
        {random=random??new System.Random();return Create(random,level,DungeonCatalog.RollRarity(random));}

        public static MeleeWeaponItem Create(System.Random random,int level,Rarity rarity)
        {
            random=random??new System.Random();return Catalog(random.Next(CatalogCount),rarity,level);
        }
        public static MeleeWeaponItem Catalog(int index,Rarity rarity=Rarity.Common,int level=1)
        {
            index=Mathf.Clamp(index,0,CatalogCount-1);level=Mathf.Max(1,level);rarity=(Rarity)Mathf.Clamp((int)rarity,0,4);
            int i=index%FamilyCount,design=index/FamilyCount;
            float[] damage={18,22,32,27,36},interval={.62f,.78f,.95f,.82f,1.05f},range={1.2f,1.0f,1.15f,1.65f,1.4f},arc={75,60,105,90,115};
            float[] output={1,.82f,1.28f,.76f,1.13f,1.08f,.92f,1.16f,1.05f,1.24f};
            float[] cadence={1,.78f,1.24f,.71f,1.14f,1.12f,.94f,1.02f,1.18f,1.22f};
            float[] reach={1,.91f,1.06f,.88f,1.2f,.97f,1.12f,.91f,1.16f,1.08f};
            float[] sweep={1,.86f,1.12f,1.2f,.78f,1.25f,1.04f,.88f,1.18f,1.08f};
            float d=damage[i]*output[design],t=interval[i]*cadence[design],r=range[i]*reach[design],a=arc[i]*sweep[design];
            string maker=ManufacturerCatalog.ForDesign(i,design);
            switch(ManufacturerCatalog.Index(maker)){
                case 0:d*=.94f;t*=.88f;a*=.92f;break; // Balanced precision and quicker recovery.
                case 1:d*=1.22f;t*=1.23f;r*=1.04f;break;
                case 2:d*=.83f;t*=.78f;r*=.92f;break;
                case 3:d*=.91f;r*=1.13f;a*=1.04f;break; // Flexible living haft rather than an unused healing stat.
                case 4:d*=1.08f;t*=1.16f;a*=1.2f;r*=.94f;break;
                case 5:d*=.94f;t*=.94f;a*=1.08f;break;
            }
            float scale=1+.16f*((int)rarity+level-1);
            return new MeleeWeaponItem{id=Guid.NewGuid().ToString("N"),catalogId="melee-"+i.ToString("00")+"-"+design.ToString("00"),manufacturerId=maker,designVariant=design,family=(MeleeWeaponFamily)i,rarity=rarity,uniqueEffect=rarity==Rarity.Legendary,level=level,damage=Mathf.Max(1,Mathf.RoundToInt(d*scale)),interval=t/(1+.025f*(int)rarity),range=r+.025f*(level-1),arcDegrees=Mathf.Clamp(a,30,170)};
        }
        public static MeleeWeaponItem RollForDungeon(System.Random random,int dungeon,bool boss=false)
        {random=random??new System.Random();return Create(random,DungeonCatalog.ItemLevel(random,dungeon),DungeonCatalog.RollRarity(random,boss));}
        public static string Name(MeleeWeaponItem item)
        {
            if(item==null)return "";InferIdentity(item);
            string en=ManufacturerCatalog.Name(item.manufacturerId,false)+" "+English(item.family)+" / "+Arsenal.DesignName(item.designVariant,false);
            string ja=ManufacturerCatalog.Name(item.manufacturerId,true)+" "+Japanese(item.family)+" / "+Arsenal.DesignName(item.designVariant,true);
            return VaultLanguage.T(en,ja);
        }
        public static string Name(MeleeWeaponFamily family)=>VaultLanguage.T(English(family),Japanese(family));
        public static string English(MeleeWeaponFamily family)=>family==MeleeWeaponFamily.Shiv?"Shiv":family==MeleeWeaponFamily.Cleaver?"Cleaver":family==MeleeWeaponFamily.Machete?"Machete":family==MeleeWeaponFamily.Hatchet?"Hatchet":"Field Knife";
        public static string Japanese(MeleeWeaponFamily family)=>family==MeleeWeaponFamily.Shiv?"シヴ":family==MeleeWeaponFamily.Cleaver?"ブッチャーナイフ":family==MeleeWeaponFamily.Machete?"マチェット":family==MeleeWeaponFamily.Hatchet?"ハチェット":"フィールドナイフ";
        public static int Score(MeleeWeaponItem item)=>item==null?0:Mathf.Max(1,Mathf.RoundToInt((Mathf.Log(1+item.damage/Mathf.Max(.05f,item.interval))*12+Mathf.Log(1+item.range)*4+item.arcDegrees*.06f+item.level*2)*10));
    }
}
