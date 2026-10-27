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
            new[]{"サード・ウィットネス","グラス・ジュリー","ラスト・テスタメント","ペイル・メリディアン","ペレグリン"},
            new[]{"サード・ベル","デットブレイカー","フューネラル・エンジン","シージ・アンコール","ハンマーフォール"},
            new[]{"トリニティ・サーキット","ブラックアウト・クワイア","デッド・バッテリー","ストーム・リプライズ","レールストーム"},
            new[]{"ブラッド・ディビデンド","グリーン・リーチ","グレイヴ・オーチャード","セカンド・スプリング","スポア・ランナー"},
            new[]{"スリー・ストライクス","スクラップヤード・セイント","アッシュ・インヘリタンス","ラスト・カートリッジ","ファイアウォーク"},
            new[]{"サード・ガーディアン","ブルー・リプライザル","ウィドウズ・ペンション","エマージェンシー・プロトコル","エスコート・デューティー"}};
        static readonly string[][] NewGunsEn={
            new[]{"Prism Verdict","Twin Horizon","Cross Examination","Ghost Measure","Long Vigil","Needle Court","White Feint","Second Angle","Silent Trajectory","Final Bearing"},
            new[]{"Quake Ledger","Rivet Storm","Anvil Echo","Breach Dividend","Iron Wake","Grave Piston","Siege Relay","Foundry Roar","Crater Debt","Last Hammer"},
            new[]{"Arc Fugue","Copper Serpent","Static Crown","Relay Ghost","Storm Spindle","Broken Conductor","Blue Cascade","Live Junction","Pulse Verdict","Overload Hymn"},
            new[]{"Spore Covenant","Root Tithe","Verdant Wake","Briar Lullaby","Seed Funeral","Green Refrain","Thorn Orchard","Leech Bloom","Last Harvest","Mycelium Crown"},
            new[]{"Shard Psalm","Cinder Rain","Kiln Memory","Fuse Saint","Ash Cyclone","Ember Debt","Blast Reliquary","Scorch Trail","Scrap Comet","Final Furnace"},
            new[]{"Ward Beacon","Blue Bulwark","Escort Halo","Guardian Relay","Shield Verdict","Safe Meridian","Bastion Echo","Counterwall","Citadel Line","Last Shelter"}};
        static readonly string[][] NewGunsJa={
            new[]{"プリズム・ヴァーディクト","ツイン・ホライゾン","クロス・エグザミネーション","ゴースト・メジャー","ロング・ヴィジル","ニードル・コート","ホワイト・フェイント","セカンド・アングル","サイレント・トラジェクトリー","ファイナル・ベアリング"},
            new[]{"クエイク・レジャー","リベット・ストーム","アンヴィル・エコー","ブリーチ・ディビデンド","アイアン・ウェイク","グレイヴ・ピストン","シージ・リレー","ファウンドリー・ロア","クレーター・デット","ラスト・ハンマー"},
            new[]{"アーク・フーガ","カッパー・サーペント","スタティック・クラウン","リレー・ゴースト","ストーム・スピンドル","ブロークン・コンダクター","ブルー・カスケード","ライブ・ジャンクション","パルス・ヴァーディクト","オーバーロード・ヒム"},
            new[]{"スポア・コヴナント","ルート・タイス","ヴァーダント・ウェイク","ブライア・ララバイ","シード・フューネラル","グリーン・リフレイン","ソーン・オーチャード","リーチ・ブルーム","ラスト・ハーヴェスト","マイセリウム・クラウン"},
            new[]{"シャード・サーム","シンダー・レイン","キルン・メモリー","フューズ・セイント","アッシュ・サイクロン","エンバー・デット","ブラスト・レリクアリー","スコーチ・トレイル","スクラップ・コメット","ファイナル・ファーネス"},
            new[]{"ウォード・ビーコン","ブルー・ブルワーク","エスコート・ヘイロー","ガーディアン・リレー","シールド・ヴァーディクト","セーフ・メリディアン","バスティオン・エコー","カウンターウォール","シタデル・ライン","ラスト・シェルター"}};
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
            new[]{"ウィットネス・プロテクション","ゴースト・レジャー","サイレント・エクスチェンジ","ヴァニシング・ポイント","ウォッチメーカー"},
            new[]{"デット・コレクター","アイアン・エピタフ","シージ・ハンズ","アースシェイカー","ウォーキング・フォートレス"},
            new[]{"フィードバック・クラウン","ブラックアウト・ジャケット","ライブ・ターミナル","サンダーステップ","パーペチュアル・ストーム"},
            new[]{"リーチ・キング","グレイヴ・ブルーム","トランスフュージョン","マイセリウム・ストライド","アンダイイング・ルート"},
            new[]{"ブラスト・サバイバー","アッシュ・エア","ラスト・マッチ","マインウォーカー","スクラップ・フェニックス"},
            new[]{"リターン・トゥ・センダー","ウィドウズ・ウォール","リザーブ・ガーディアン","セーフ・パッセージ","ラスト・バスティオン"}};
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
