using System;
using UnityEngine;

namespace NeonVault
{
    public enum ExpeditionMission { ClearHostiles, HuntTarget, RecoverSupplies, DestroyNest, SurveySectors, SalvageSweep }

    [Serializable]
    public sealed class ExpeditionStage
    {
        public int id, region, level, legacyDungeonIndex;
        public string englishName, japaneseName, englishLocation, japaneseLocation;
        public string englishBiome, japaneseBiome, englishTarget, japaneseTarget;
        public ExpeditionMission mission;
        public string englishReward, japaneseReward;
        public Color regionColor;
        public string Name => VaultLanguage.T(englishName, japaneseName);
        public string Location => VaultLanguage.T(englishLocation, japaneseLocation);
        public string Biome => VaultLanguage.T(englishBiome, japaneseBiome);
        public string Target => VaultLanguage.T(englishTarget, japaneseTarget);
    }

    /// <summary>Stable, content-only expedition catalog. Stage ids and rolls must not depend on runtime RNG.</summary>
    public static class ExpeditionMap
    {
        public const int StageCount = 100;
        public static readonly Vector2 CityCanvasSize = new Vector2(1440, 1000);
        // A coprime permutation scatters stable stage IDs
        // across the city without changing progression, rewards, or claimed contracts.
        public static Vector2 CityPoint(int id)
        {
            int slot = (Mathf.Clamp(id, 0, StageCount - 1) * 37 + 17) % StageCount;
            int column = slot % 10, row = slot / 10;
            return new Vector2(88 + column * 140 + Mathf.Sin(slot * 2.31f) * 12,
                83 + row * 91 + Mathf.Cos(slot * 1.73f) * 10);
        }
        static readonly string[] regions = { "Ruinbelt", "Iron Veins", "Floodworks", "Green Fringe", "Red Expanse" };
        static readonly string[] regionsJa = { "崩壊都市帯", "鉄鉱脈", "氾濫工業地帯", "森林化郊外", "赤い荒野" };
        static readonly string[][] places = {
            new[]{"Glass Mile","Civic Rotunda","Sunken Market","East Stack","Ash Terminal"},
            new[]{"Warden Pit","Copper Spine","Black Lift","Kiln Gallery","Ore Chapel"},
            new[]{"Silt Foundry","Pump Cathedral","Drowned Rail","Brine Yard","Lock Nine"},
            new[]{"Moss Estates","Canopy Loop","Foxglove Mall","Rootline Depot","Fern Reservoir"},
            new[]{"White Static","Crater Verge","Salt Observatory","Red Choir","Null Meridian"}
        };
        static readonly string[][] placesJa = {
            new[]{"硝子通り","市民広場跡","沈没市場","東棟群","灰の駅"},
            new[]{"監視坑","銅の背骨","黒昇降機","焼成回廊","鉱石礼拝堂"},
            new[]{"泥濘鋳造所","ポンプ大聖堂","水没線路","塩水ヤード","第九水門"},
            new[]{"苔むす団地","樹冠環状路","キツネノテブクロ・モール","根道貨物駅","シダ貯水池"},
            new[]{"白い雑音","クレーター縁","塩の天文台","赤い聖歌隊","無の子午線"}
        };
        static readonly string[][] biomes = {
            new[]{"Collapsed downtown","Dust-choked towers","Subway sinkhole","Neon ruin","Concrete canyon"},
            new[]{"Open-pit mine","Toxic tailings","Ore conveyor","Cinder shaft","Buried refinery"},
            new[]{"Flooded assembly line","Brackish canal","Submerged turbines","Corroded shipyard","Silted lock"},
            new[]{"Overgrown suburb","Canopy rooftops","Vine-choked mall","Root-split rail","Marsh gardens"},
            new[]{"Radiation glassland","Scorched crater","Static dunes","Salt flats","Fallout basin"}
        };
        static readonly string[][] biomesJa = {
            new[]{"崩壊した中心街","粉塵に沈む高層街","陥没地下鉄","ネオン廃墟","コンクリート峡谷"},
            new[]{"露天掘り鉱区","有毒な鉱滓地","鉱石搬送路","燃え殻の坑道","埋没精錬所"},
            new[]{"浸水組立工場","汽水運河","水没タービン","腐食した造船所","泥に埋もれた水門"},
            new[]{"緑に呑まれた郊外","樹冠屋上","蔦のモール","根に割られた線路","湿地庭園"},
            new[]{"放射線ガラス地帯","焦土クレーター","静電砂丘","塩原","降灰盆地"}
        };
        static readonly string[][] targets = {
            new[]{"Warden Husk","Glassback Brute","The Tollkeeper","Spore Matron","Rail Stalker"},
            new[]{"Drilljaw","Slag Behemoth","Foreman Zero","Cinder Witch","Ore Maw"},
            new[]{"Drowned Marshal","Brine Colossus","Pump Mother","Rust Eel","Lockbreaker"},
            new[]{"Rootbound Alpha","Canopy Widow","The Groundsman","Bloom Horror","Foxfire Pack"},
            new[]{"Red Titan","Static Prophet","Crater Maw","Salt Revenant","Null Stalker"}
        };
        static readonly string[][] targetsJa = {
            new[]{"監視屍","硝子背の巨屍","料金所の番人","胞子の母","線路の追跡者"},
            new[]{"ドリルジョー","鉱滓巨獣","フォアマン・ゼロ","燃え殻の魔女","鉱石喰らい"},
            new[]{"水没元帥","塩水巨人","ポンプの母","錆び鰻","水門破り"},
            new[]{"根縛りの王","樹冠の蜘蛛","庭園管理人","開花怪異","狐火の群れ"},
            new[]{"赤い巨人","静電預言者","クレーター喰らい","塩の亡霊","無の追跡者"}
        };
        static readonly string[] missionEn = {"Defeat 18 hostiles","Defeat the alpha mutant","Recover 3 caches across 2 floors","Break the infestation: 8 kills and 2 caches","Open a cache on each of 2 floors","Salvage 5 caches across 3 floors"};
        static readonly string[] missionJa = {"敵を18体倒す","変異体の主を倒す","2フロア以上で物資を計3箱回収","感染網を断つ：敵8体と物資2箱","別々の2フロアで各1箱を開く","3フロア以上で物資を計5箱回収"};
        static readonly Color[] colors = {new Color(.31f,.63f,.55f),new Color(.84f,.43f,.23f),new Color(.23f,.62f,.72f),new Color(.48f,.68f,.35f),new Color(.86f,.28f,.20f)};
        public static ExpeditionStage Get(int id)
        {
            id=Mathf.Clamp(id,0,StageCount-1); int region=id/20, n=id%20, place=n/4, variant=n%4;
            string loc=places[region][place], locJa=placesJa[region][place]; int danger=OverworldGeography.DungeonLevel(id);
            int mission=(id+region+place)%missionEn.Length;
            string target=mission==1?targets[region][(n*3+place)%5]:mission==0?"Zone hostiles":mission==3?"Infestation network":"Supply cache";
            string targetJa=mission==1?targetsJa[region][(n*3+place)%5]:mission==0?"区域の敵":mission==3?"感染網":"補給物資";
            return new ExpeditionStage { id=id,region=region,level=danger,legacyDungeonIndex=region==0?0:region<3?1:2,
                englishName=loc+" / " + (variant+1).ToString("00"), japaneseName=locJa+"・"+(variant+1).ToString("00"),
                englishLocation=regions[region]+" — "+loc,japaneseLocation=regionsJa[region]+" — "+locJa,
                englishBiome=id%5==2?"Raider-occupied depot":biomes[region][place],japaneseBiome=id%5==2?"レイダー占拠拠点":biomesJa[region][place],mission=(ExpeditionMission)mission,
                englishTarget=id%5==2&&mission==1?"Raider commander":target,japaneseTarget=id%5==2&&mission==1?"レイダーの指揮官":targetJa,
                englishReward=RewardLabel(id,false),japaneseReward=RewardLabel(id,true),regionColor=colors[region] };
        }
        public static int MissionCount(int region,ExpeditionMission mission){int count=0;for(int id=Mathf.Clamp(region,0,4)*20;id<Mathf.Clamp(region,0,4)*20+20;id++)if(Get(id).mission==mission)count++;return count;}
        public static int RewardSeed(int stageId)=>unchecked(0x51A7D00D ^ (stageId+1)*7919);
        public static string MissionLabel(ExpeditionMission mission,bool japanese)=>japanese?missionJa[(int)mission]:missionEn[(int)mission];
        static string RewardLabel(int id,bool ja) { string[] en={"Credits cache","Unusual gear","Armor cache","Skill point"};string[] jp={"クレジット備蓄","異能装備","防具箱","スキルポイント"};int kind=new System.Random(RewardSeed(id)).Next(4);return (ja?jp:en)[kind]; }
    }
}
