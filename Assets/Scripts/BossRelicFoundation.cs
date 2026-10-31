using System;
using UnityEngine;

namespace NeonVault
{
    // Persistent identity and factory for boss-exclusive equipment. This is a
    // foundation only: reward delivery, discovery entries, art and effects are
    // wired separately and must not silently fall back to a normal reward.
    public static class BossRelicFoundation
    {
        public const int BossCount=3;
        public const int CategoryCount=11; // five guns, four armor slots, module, melee
        static readonly string[,] NamesEn={
            {"CINDERBITE","PYRE BREAKER","SCARLET TIDE","EMBER REACH","RED TEMPEST","ASHEN CITADEL","IRON EMBRACE","SCORIA STRIDE","CINDER TREAD","BLOOD CIRCUIT","WIDOW'S EDGE"},
            {"DUST WHISPER","MOON HOWL","DRIFTER'S WAKE","LONG DUSK","PACK STORM","NOMAD VEST","LONE GRIP","DUNE RUNNER","GHOST TRACK","WILD SIGNAL","DUSK FANG"},
            {"DEBT COLLECTOR","FINAL NOTICE","DEAD LETTER","LAST ACCOUNT","ZERO BALANCE","BLACK LEDGER","IRON CLAUSE","DARK CONTRACT","FINAL STEP","FALSE PROMISE","SEVERANCE"}};
        static readonly string[,] NamesJa={
            {"シンダーバイト","パイアブレイカー","スカーレットタイド","エンバーリーチ","レッドテンペスト","アシェンシタデル","アイアンエンブレイス","スコリアストライド","シンダートレッド","ブラッドサーキット","ウィドウズエッジ"},
            {"ダストウィスパー","ムーンハウル","ドリフターズウェイク","ロングダスク","パックストーム","ノマドベスト","ローングリップ","デューンランナー","ゴーストトラック","ワイルドシグナル","ダスクファング"},
            {"デットコレクター","ファイナルノーティス","デッドレター","ラストアカウント","ゼロバランス","ブラックレジャー","アイアンクローズ","ダークコントラクト","ファイナルステップ","フォルスプロミス","セヴァランス"}};
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
            string[] bosses=ja?new[]{"レイヴン","ヴェックス","ヴェイル"}:new[]{"Raven","Vex","Vale"};
            string[,] effectsJa={
                {"撃破時、近くの敵2体へ爆発弾を連鎖させる。","撃破時、近距離を散弾爆破し敵を押し戻す。","撃破時、最大3体へ電弧を連鎖させる。","撃破時、最も遠い敵へ高威力の貫通弾を放つ。","撃破時、最大4体へ制圧射撃し攻撃を遅らせる。","撃破時、シールドを得て周囲の敵を押し戻す。","撃破時、照準を安定させ近くの敵へ追撃する。","撃破時、回避を即座に整えHPを少し回復する。","撃破時、短時間の無敵とシールドを得る。","撃破時、HPを回復し予備弾を補充する。","撃破時、近くの敵を強打し、ひるませる。"},
                {"撃破時、近くの敵2体を凍結弾で連鎖攻撃する。","撃破時、近距離の敵をひるませ、HPを回復する。","撃破時、最大3体を電撃でひるませる。","撃破時、最も遠い敵を長くひるませる。","撃破時、最大4体の攻撃を遅らせ、自身を回復する。","撃破時、厚いシールドを得て敵を押し返す。","撃破時、照準を安定させ敵をひるませる。","撃破時、回避を整え、HPを大きく回復する。","撃破時、無敵時間を得て敵をひるませる。","撃破時、HPと予備弾を回復する。","撃破時、周囲の敵をひるませHPを回復する。"},
                {"撃破時、近くの敵2体へ高威力の追撃を放つ。","撃破時、近距離を爆破しシールドを得る。","撃破時、最大3体を貫く連鎖弾を放つ。","撃破時、最も遠い敵へ致命的な貫通弾を放つ。","撃破時、最大4体を制圧しシールドを得る。","撃破時、シールドを大きく得て周囲を押し戻す。","撃破時、照準を安定させ、近くの敵へ追撃する。","撃破時、回避を整え、シールドを得る。","撃破時、無敵時間と厚いシールドを得る。","撃破時、予備弾を補充しシールドを得る。","撃破時、周囲の敵を強打し、シールドを得る。"}};
            string[,] effectsEn={
                {"On kill, chain an explosive round through two nearby foes.","On kill, blast and push back close foes.","On kill, arc through up to three foes.","On kill, fire a high-damage piercing round at the farthest foe.","On kill, suppress up to four foes and delay their attacks.","On kill, gain barrier and push nearby foes away.","On kill, steady aim and strike a nearby foe.","On kill, reset dodge and restore a little health.","On kill, gain brief invulnerability and barrier.","On kill, restore health and reserve ammo.","On kill, smash and stagger nearby foes."},
                {"On kill, chain a cryo round through two nearby foes.","On kill, stagger close foes and restore health.","On kill, shock and stagger up to three foes.","On kill, heavily stagger the farthest foe.","On kill, delay up to four foes and restore health.","On kill, gain a heavy barrier and push foes back.","On kill, steady aim and stagger a nearby foe.","On kill, reset dodge and restore substantial health.","On kill, gain brief invulnerability and stagger foes.","On kill, restore health and reserve ammo.","On kill, stagger nearby foes and restore health."},
                {"On kill, chain a heavy follow-up through two nearby foes.","On kill, blast close foes and gain barrier.","On kill, chain piercing rounds through up to three foes.","On kill, fire a lethal piercing round at the farthest foe.","On kill, suppress up to four foes and gain barrier.","On kill, gain a large barrier and push nearby foes away.","On kill, steady aim and strike a nearby foe.","On kill, reset dodge and gain barrier.","On kill, gain brief invulnerability and a heavy barrier.","On kill, restore reserve ammo and gain barrier.","On kill, smash nearby foes and gain barrier."}};
            return (ja?bosses[boss]+"由来。": "Recovered from "+bosses[boss]+". ")+(ja?effectsJa:effectsEn)[boss,category]+(ja?" クールダウン4秒。":" 4s cooldown.");
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
                    int pick=BossCount==1?0:boss*(designs.Count-1)/(BossCount-1);
                    var rifle=Arsenal.RollCatalogStats(Arsenal.FromRecipe(designs[pick],Rarity.Legendary),random,level);
                    rifle.bossRelicId=id;rifle.name=Name(id,false);rifle.subtitle=Description(id,false);
                    rifle.damage*=1.3f;rifle.interval*=.9f;
                    return rifle;
                }
                // The 15 named weapon lines are grouped by class. Select the
                // same class line for each boss while varying its maker index.
                int line=requested==WeaponClass.SniperRifle?0:requested==WeaponClass.Handgun?1:requested==WeaponClass.Shotgun?2:3;
                var gun=LegendaryCatalog.CreateGun(30+line*6+boss,random,level);
                if(Arsenal.Category(gun.modelVariant)!=requested)throw new InvalidOperationException("Legendary weapon catalog class mismatch: "+requested);
                gun.bossRelicId=id;gun.name=Name(id,false);gun.subtitle=Description(id,false);
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
                var product=ModuleProductCatalog.Get(4+boss,0);
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
