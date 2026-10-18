using System;
using UnityEngine;
namespace NeonVault
{
    // Stable named relics. Catalog identity survives save/load independently of
    // the ordinary mesh design and its randomized level/affixes.
    public static class LegendaryCatalog
    {
        public const int GunCount=30,ArmorCount=30;
        static readonly string[][] GunsEn={
            new[]{"Third Witness","Glass Jury","Last Testament","Pale Meridian","Peregrine"},
            new[]{"Third Bell","Debtbreaker","Funeral Engine","Siege Encore","Hammerfall"},
            new[]{"Trinity Circuit","Blackout Choir","Dead Battery","Storm Reprise","Railstorm"},
            new[]{"Blood Dividend","Green Leech","Grave Orchard","Second Spring","Spore Runner"},
            new[]{"Three Strikes","Scrapyard Saint","Ash Inheritance","Last Cartridge","Firewalk"},
            new[]{"Third Guardian","Blue Reprisal","Widow's Pension","Emergency Protocol","Escort Duty"}};
        static readonly string[][] GunsJa={
            new[]{"第三の証人","硝子の陪審","最後の証言","白い子午線","隼の帰還"},
            new[]{"第三の弔鐘","債務破砕機","葬送機関","包囲のアンコール","鉄槌落下"},
            new[]{"三位回路","停電の聖歌隊","死んだ電池","嵐の再演","レールストーム"},
            new[]{"血の配当","緑の吸血鬼","墓場の果樹園","二度目の春","胞子の疾走"},
            new[]{"三度目の鉄屑","廃材置場の聖人","灰の遺産","最後の薬莢","火渡り"},
            new[]{"第三の守護者","青の報復","未亡人の年金","緊急手順","護衛任務"}};
        static readonly string[][] ArmorEn={
            new[]{"Witness Protection","Ghost Ledger","Silent Exchange","Vanishing Point","Watchmaker"},
            new[]{"Debt Collector","Iron Epitaph","Siege Hands","Earthshaker","Walking Fortress"},
            new[]{"Feedback Crown","Blackout Jacket","Live Terminal","Thunderstep","Perpetual Storm"},
            new[]{"Leech King","Grave Bloom","Transfusion","Mycelium Stride","Undying Root"},
            new[]{"Blast Survivor","Ash Heir","Last Match","Minewalker","Scrap Phoenix"},
            new[]{"Return to Sender","Widow's Wall","Reserve Guardian","Safe Passage","Last Bastion"}};
        static readonly string[][] ArmorJa={
            new[]{"証人保護","亡霊の帳簿","無言の交換","消失点","時計師の歩み"},
            new[]{"借金取り","鉄の墓碑銘","包囲の両手","大地を砕く者","歩く城塞"},
            new[]{"帰還電流の冠","停電の外套","通電端子","雷鳴の一歩","永久嵐"},
            new[]{"吸血王","墓標の開花","輸血装置","菌糸の歩幅","不死の根"},
            new[]{"爆心の生存者","灰の相続人","最後のマッチ","地雷歩き","廃鉄の不死鳥"},
            new[]{"差出人へ返送","未亡人の壁","予備の守護者","安全な通行","最後の砦"}};
        public static string Key(bool armor,int index)
        {index=Mathf.Clamp(index,0,29);return (armor?"armor-":"gun-")+(index/5)+"-"+(index%5);}
        public static int Index(string key)
        {return VaultGame.DecodeLegendaryKey(key,out int maker,out int variant)?maker*5+variant:-1;}
        public static string GunName(Gun item,bool ja)=>Name(item==null?null:item.legendaryKey,false,ja);
        public static string ArmorName(ArmorItem item,bool ja)=>Name(item==null?null:item.legendaryKey,true,ja);
        static string Name(string key,bool armor,bool ja)
        {int index=Index(key);return index<0?"":(armor?(ja?ArmorJa:ArmorEn):(ja?GunsJa:GunsEn))[index/5][index%5];}
        public static string Description(string key,bool ja)=>VaultGame.LegendaryRuntimeDescription(key,ja);
        public static string Description(Gun item,bool ja)=>Description(item==null?null:item.legendaryKey,ja);
        public static string Description(ArmorItem item,bool ja)=>Description(item==null?null:item.legendaryKey,ja);
        public static Gun CreateGun(int index,System.Random random,int level)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));index=Mathf.Clamp(index,0,29);
            int maker=index/5;
            var item=Arsenal.RollCatalogStats(Arsenal.Catalog(index,Rarity.Legendary),random,level);
            item.legendaryKey=Key(false,index);item.manufacturerId=ManufacturerCatalog.Id(maker);
            item.name=GunName(item,false);item.subtitle=Description(item,false);
            item.color=Arsenal.Colors[(int)Rarity.Legendary];return item;
        }
        public static ArmorItem CreateArmor(int index,System.Random random,int level)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));index=Mathf.Clamp(index,0,29);
            int maker=index/5,variant=index%5;string makerId=ManufacturerCatalog.Id(maker);
            ArmorProduct product=null;
            foreach(var p in ArmorProductCatalog.All)if(p.id==DistinctEquipmentDesigns.Armor[index].id){product=p;break;}
            var item=Armory.CreateCatalog(product.id,Rarity.Legendary,level,random);
            item.legendaryKey=Key(true,index);item.manufacturerId=makerId;item.uniqueEffect=0;
            item.name=ArmorName(item,false);item.subtitle=Description(item,false);return item;
        }
    }
}
