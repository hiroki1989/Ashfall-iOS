using System;
using UnityEngine;
namespace NeonVault
{
    // Stable named relics. Catalog identity survives save/load independently of
    // the ordinary mesh design and its randomized level/affixes.
    public static class LegendaryCatalog
    {
        public const int GunCount=90,NewGunCount=60,ArmorCount=30;
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
        static readonly string[][] NewGunsEn={
            new[]{"Prism Verdict","Twin Horizon","Cross Examination","Ghost Measure","Long Vigil","Needle Court","White Feint","Second Angle","Silent Trajectory","Final Bearing"},
            new[]{"Quake Ledger","Rivet Storm","Anvil Echo","Breach Dividend","Iron Wake","Grave Piston","Siege Relay","Foundry Roar","Crater Debt","Last Hammer"},
            new[]{"Arc Fugue","Copper Serpent","Static Crown","Relay Ghost","Storm Spindle","Broken Conductor","Blue Cascade","Live Junction","Pulse Verdict","Overload Hymn"},
            new[]{"Spore Covenant","Root Tithe","Verdant Wake","Briar Lullaby","Seed Funeral","Green Refrain","Thorn Orchard","Leech Bloom","Last Harvest","Mycelium Crown"},
            new[]{"Shard Psalm","Cinder Rain","Kiln Memory","Fuse Saint","Ash Cyclone","Ember Debt","Blast Reliquary","Scorch Trail","Scrap Comet","Final Furnace"},
            new[]{"Ward Beacon","Blue Bulwark","Escort Halo","Guardian Relay","Shield Verdict","Safe Meridian","Bastion Echo","Counterwall","Citadel Line","Last Shelter"}};
        static readonly string[][] NewGunsJa={
            new[]{"多面の評決","双つの地平","十字尋問","亡霊の測量","長い不寝番","針の法廷","白い陽動","第二の角度","無音の軌跡","最後の方位"},
            new[]{"地震の帳簿","鋲の嵐","金床の反響","破門の配当","鉄の航跡","墓場のピストン","攻城連鎖","鋳造炉の咆哮","陥没の負債","最後の鉄槌"},
            new[]{"電弧の遁走","銅の蛇","静電の冠","継電の亡霊","嵐の紡錘","折れた導体","青い瀑布","活線の分岐","脈動の評決","過負荷の聖歌"},
            new[]{"胞子の契約","根の年貢","緑の航跡","茨の子守歌","種子の葬儀","緑の反復","棘の果樹園","吸血の花","最後の収穫","菌糸の冠"},
            new[]{"破片の詩編","燃え殻の雨","窯の記憶","導火線の聖人","灰の旋風","残火の負債","爆風の聖遺物","焦土の道","廃鉄の彗星","最後の溶鉱炉"},
            new[]{"守護の灯","青い防塁","護衛の輪","守護者の連鎖","盾の評決","安全な子午線","防塁の反響","反撃の壁","城塞の射線","最後の避難所"}};
        static readonly string[] newVisuals={"faceted muzzle and prism fins","paired barrels on a split receiver","crossed sight rails","recessed shroud and ghost ring","elongated vented barrel","needle muzzle and narrow stock","offset side port","dual angle sight","suppressed shroud with tracer vents","heavy terminal barrel and bearing dial"};
        public static int CatalogIndex(int maker,int variant)=>variant<5?maker*5+variant:GunSeriesDesigns.FirstNewLegendary+maker*10+variant-5;
        public static int VariantFromCatalogIndex(int index)=>index<30?index%5:5+(index-GunSeriesDesigns.FirstNewLegendary)%10;
        public static int MakerFromCatalogIndex(int index)=>index<30?index/5:(index-GunSeriesDesigns.FirstNewLegendary)/10;
        public static string VisualDirection(int index){int maker=MakerFromCatalogIndex(index),variant=VariantFromCatalogIndex(index);return variant<5?"Legacy bespoke design":"Maker "+maker+": "+newVisuals[variant-5]+", unique silhouette and readable moving mechanism";}
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
        {index=Mathf.Clamp(index,0,armor?29:GunSeriesDesigns.FirstNewLegendary+NewGunCount-1);return (armor?"armor-":"gun-")+MakerFromCatalogIndex(index)+"-"+VariantFromCatalogIndex(index);}
        public static int Index(string key)
        {if(!VaultGame.DecodeLegendaryKey(key,out int maker,out int variant))return -1;return key.StartsWith("armor-")?maker*5+variant:CatalogIndex(maker,variant);}
        public static string GunName(Gun item,bool ja)=>Name(item==null?null:item.legendaryKey,false,ja);
        public static string ArmorName(ArmorItem item,bool ja)=>Name(item==null?null:item.legendaryKey,true,ja);
        static string Name(string key,bool armor,bool ja)
        {int index=Index(key);if(index<0)return "";if(armor)return (ja?ArmorJa:ArmorEn)[index/5][index%5];int maker=MakerFromCatalogIndex(index),variant=VariantFromCatalogIndex(index);return variant<5?(ja?GunsJa:GunsEn)[maker][variant]:(ja?NewGunsJa:NewGunsEn)[maker][variant-5];}
        public static string Description(string key,bool ja)=>VaultGame.LegendaryRuntimeDescription(key,ja);
        public static string Description(Gun item,bool ja)=>Description(item==null?null:item.legendaryKey,ja);
        public static string Description(ArmorItem item,bool ja)=>Description(item==null?null:item.legendaryKey,ja);
        public static Gun CreateGun(int index,System.Random random,int level)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));index=Mathf.Clamp(index,0,GunSeriesDesigns.FirstNewLegendary+NewGunCount-1);
            if(index>=30&&index<GunSeriesDesigns.FirstNewLegendary)index=CatalogIndex((index-30)%6,5+((index-30)/6)%10);
            int maker=MakerFromCatalogIndex(index);
            var item=Arsenal.RollCatalogStats(Arsenal.Catalog(index,Rarity.Legendary),random,level);
            item.legendaryKey=Key(false,index);item.manufacturerId=ManufacturerCatalog.Id(maker);
            item.name=GunName(item,false);item.subtitle=Description(item,false);
            item.color=Arsenal.Colors[(int)Rarity.Legendary];return item;
        }
        public static ArmorItem RollArmor(System.Random random,int level)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));
            int pick=random.Next(24);return CreateArmor((pick/4)*5+1+pick%4,random,level);
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
