using System;
using UnityEngine;

namespace NeonVault
{
    // Persistent identity and factory for boss-exclusive equipment. This is a
    // foundation only: reward delivery, discovery entries, art and effects are
    // wired separately and must not silently fall back to a normal reward.
    public static class BossRelicFoundation
    {
        public const int BossCount=4;
        public const int CategoryCount=11; // five guns, four armor slots, module, melee
        static readonly string[,] NamesEn={
            {"CINDERBITE","PYRE BREAKER","SCARLET TIDE","EMBER REACH","RED TEMPEST","ASHEN CITADEL","IRON EMBRACE","SCORIA STRIDE","CINDER TREAD","BLOOD CIRCUIT","WIDOW'S EDGE"},
            {"DUST WHISPER","MOON HOWL","DRIFTER'S WAKE","LONG DUSK","PACK STORM","NOMAD VEST","LONE GRIP","DUNE RUNNER","GHOST TRACK","WILD SIGNAL","DUSK FANG"},
            {"DEBT COLLECTOR","FINAL NOTICE","DEAD LETTER","LAST ACCOUNT","ZERO BALANCE","BLACK LEDGER","IRON CLAUSE","DARK CONTRACT","FINAL STEP","FALSE PROMISE","SEVERANCE"},
            {"PALE ECHO","GRAVE BLOOM","CARRION CHOIR","DEAD RECKONING","MORROW STORM","HOLLOW SHELL","GRAVE GRIP","ASH WALKER","PALE TREAD","LAST SIGNAL","SILENT FANG"}};
        static readonly string[,] NamesJa={
            {"シンダーバイト","パイアブレイカー","スカーレットタイド","エンバーリーチ","レッドテンペスト","アシェンシタデル","アイアンエンブレイス","スコリアストライド","シンダートレッド","ブラッドサーキット","ウィドウズエッジ"},
            {"ダストウィスパー","ムーンハウル","ドリフターズウェイク","ロングダスク","パックストーム","ノマドベスト","ローングリップ","デューンランナー","ゴーストトラック","ワイルドシグナル","ダスクファング"},
            {"デットコレクター","ファイナルノーティス","デッドレター","ラストアカウント","ゼロバランス","ブラックレジャー","アイアンクローズ","ダークコントラクト","ファイナルステップ","フォルスプロミス","セヴァランス"},
            {"ペイルエコー","グレイヴブルーム","キャリオン・クワイア","デッドレコニング","モロウストーム","ホロウシェル","グレイヴグリップ","アッシュウォーカー","ペイルトレッド","ラストシグナル","サイレントファング"}};
        public static string Name(string id,bool japanese)
        {
            if(string.IsNullOrEmpty(id))return "";
            var parts=id.Split('-');
            if(parts.Length!=4||parts[0]!="boss"||parts[1]!="relic"||!int.TryParse(parts[2],out int boss)||!int.TryParse(parts[3],out int category)||boss<0||boss>=BossCount||category<0||category>=CategoryCount)return "";
            return japanese?NamesJa[boss,category]:NamesEn[boss,category];
        }
        public static bool Decode(string id,out int boss,out int category)
        {
            boss=-1;category=-1;if(string.IsNullOrEmpty(id))return false;
            var p=id.Split('-');return p.Length==4&&p[0]=="boss"&&p[1]=="relic"&&int.TryParse(p[2],out boss)&&int.TryParse(p[3],out category)&&boss>=0&&boss<BossCount&&category>=0&&category<CategoryCount;
        }
        public static string Description(string id,bool ja)
        {
            if(!Decode(id,out int boss,out int category))return "";
            string[] bosses=ja?new[]{"レイヴン","ヴェックス","ヴェイル","レネゲード"}:new[]{"Raven","Vex","Vale","the Renegade"};
            string[,] effectsJa={
                {"撃破地点から最大3体を追う誘導弾を発射。","前方に5発の弾を扇状に放つ。","最大4体を追う誘導弾を放つ。","最も遠い敵を追う強力な誘導弾を放つ。","最大4体の位置に爆発を起こす。","撃破地点に3回爆発する火炎罠を残す。","最大3体を追う援護弾を放つ。","撃破地点の後方に火炎罠を3つ残す。","撃破地点から8方向へ弾を放つ。","横一列に5発の援護弾を放つ。","前方に3回の爆発を起こす。"},
                {"3発の誘導弾が最寄りの敵に集まる。","前方に3回の爆発が進む。","撃破地点から6方向に弾を放つ。","最も遠い敵の周囲に3回の爆発を起こす。","前方に5発の弾を扇状に放つ。","最大HPの12%を回復。","4秒間照準を安定させる。","回避のクールダウンを即座に回復。","最大HPの15%を回復。","最大HPの18%を回復。","最大3体を追う誘導弾を放つ。"},
                {"撃破地点から8方向に弾を放つ。","重力渦を作る強力な弾を放つ。","横一列に5発の弾を放つ。","最も遠い敵へ重力渦を作る弾を放つ。","最大4体を追う誘導弾を放つ。","シールドを18得る。","最寄りの敵へ重力渦を作る弾を放つ。","0.6秒間無敵になる。","シールドを14得る。","使用中の銃の予備弾を12補充する。","撃破地点の周囲を爆破する。"},
                {"最寄りの敵に2発の誘導弾を放つ。","撃破地点で2回爆発する。","前方に3発の誘導弾を放つ。","最も遠い敵へ強力な誘導弾を放つ。","撃破地点から8方向へ弾を放つ。","最大HPの10%を回復する。","シールドを20得る。","回避のクールダウンを回復する。","0.8秒間無敵になる。","使用中の銃の予備弾を18補充する。","撃破地点の周囲を爆破する。"}};
            string[,] effectsEn={
                {"Fire guided rounds at up to three enemies.","Fire a five-round fan.","Fire guided rounds at up to four enemies.","Fire a powerful guided round at the farthest enemy.","Detonate at the positions of up to four enemies.","Leave a three-pulse explosive trap.","Fire guided cover rounds at up to three enemies.","Leave three explosive traps behind the kill site.","Fire rounds in eight directions.","Fire five parallel cover rounds.","Trigger three explosions ahead."},
                {"Three guided rounds converge on the nearest enemy.","Send three explosions forward.","Fire rounds in six directions.","Detonate three times around the farthest enemy.","Fire a five-round fan.","Restore 12% of maximum health.","Steady aim for four seconds.","Reset the dodge cooldown.","Restore 15% of maximum health.","Restore 18% of maximum health.","Fire guided rounds at up to three enemies."},
                {"Fire rounds in eight directions.","Fire a powerful gravity-well round.","Fire five parallel rounds.","Fire a gravity-well round at the farthest enemy.","Fire guided rounds at up to four enemies.","Gain 18 shield.","Fire a gravity-well round at the nearest enemy.","Gain 0.6 seconds of invulnerability.","Gain 14 shield.","Restore 12 reserve rounds for the active gun.","Blast the area around the kill site."},
                {"Fire two guided rounds at the nearest enemy.","Explode twice at the kill site.","Fire guided rounds at up to three enemies.","Fire a powerful guided round at the farthest enemy.","Fire eight rounds outward from the kill site.","Restore 10% of maximum health.","Gain 20 shield.","Reset the dodge cooldown.","Gain 0.8 seconds of invulnerability.","Restore 18 reserve rounds for the active gun.","Blast the area around the kill site."}};
            return (ja?bosses[boss]+"由来。撃破時：": "Recovered from "+bosses[boss]+". On kill: ")+(ja?effectsJa:effectsEn)[boss,category]+(ja?" クールダウン4秒。":" 4s cooldown.");
        }
        public static string Id(int boss,int category)
        {
            if(boss<0||boss>=BossCount)throw new ArgumentOutOfRangeException(nameof(boss));
            if(category<0||category>=CategoryCount)throw new ArgumentOutOfRangeException(nameof(category));
            return "boss-relic-"+boss+"-"+category.ToString("00");
        }

        public static object Create(int boss,int category,int level,System.Random random)
        {
            string id=Id(boss,category);
            if(random==null)throw new ArgumentNullException(nameof(random));
            level=Mathf.Max(1,level);
            if(category<5)
            {
                WeaponClass requested=(WeaponClass)category;
                if(requested==WeaponClass.AssaultRifle)
                {
                    var designs=new System.Collections.Generic.List<EquipmentRecipe>();
                    for(int i=30;i<GunSeriesDesigns.FirstNewLegendary;i++){var recipe=DistinctEquipmentDesigns.Guns[i];if(recipe.gunClass==0)designs.Add(recipe);}
                    if(designs.Count==0)throw new InvalidOperationException("No assault-rifle relic design");
                    int pick=boss==3?designs.Count/2:boss*(designs.Count-1)/2;
                    var rifle=Arsenal.RollCatalogStats(Arsenal.FromRecipe(designs[pick],Rarity.Legendary),random,level);
                    rifle.bossRelicId=id;rifle.name=Name(id,false);rifle.subtitle=Description(id,false);
                    GunElements.RollBossGun(rifle,random);
                    rifle.damage*=1.3f;rifle.interval*=.9f;
                    return rifle;
                }
                // The 15 named weapon lines are grouped by class. Select the
                // same class line for each boss while varying its maker index.
                int line=requested==WeaponClass.SniperRifle?0:requested==WeaponClass.Handgun?1:requested==WeaponClass.Shotgun?2:3;
                var gun=LegendaryCatalog.CreateGun(30+line*6+boss,random,level);
                if(Arsenal.Category(gun.modelVariant)!=requested)throw new InvalidOperationException("Legendary weapon catalog class mismatch: "+requested);
                gun.bossRelicId=id;gun.name=Name(id,false);gun.subtitle=Description(id,false);
                GunElements.RollBossGun(gun,random);
                gun.damage*=1.3f;gun.interval*=.9f;
                return gun;
            }
            if(category<9)
            {
                int slot=category-4;
                var armor=LegendaryCatalog.CreateArmor(boss*5+slot,random,level);
                armor.bossRelicId=id;
                armor.maxHealth*=1.3f;armor.damageReduction*=1.3f;armor.damageBonus*=1.3f;armor.fireBonus*=1.3f;armor.moveBonus*=1.3f;armor.reloadBonus*=1.3f;armor.dodgeBonus*=1.3f;armor.regenBonus*=1.3f;armor.accuracyBonus*=1.3f;armor.ammoBonus*=1.3f;
                return armor;
            }
            if(category==9)
            {
                var product=ModuleProductCatalog.Get(boss==3?6:4+boss,boss==3?1:0);
                var module=ArtifactCatalog.CreateCatalog(product.id,Rarity.Legendary,level,random,boss);
                module.bossRelicId=id;
                module.power*=1.3f;module.cooldown*=.85f;
                return module;
            }
            var melee=MeleeWeaponCatalog.Catalog(boss,Rarity.Legendary,level,random);
            melee.bossRelicId=id;
            melee.damage=Mathf.RoundToInt(melee.damage*1.3f);melee.interval*=.9f;
            return melee;
        }
    }
}
