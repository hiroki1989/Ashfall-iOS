using System;
using UnityEngine;

namespace NeonVault
{
    public enum DungeonFaction { None, Raiders, Gang }

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
        public const int StageCount = 200;
        // Stable per-stage assignment: 50 raider hideouts, 50 gang hideouts, 100 original sites.
        // The first contract is the infected tutorial. Keep its authored mission and
        // low-cost enemy roster; move its Red Claw allocation to site 03 so the
        // faction totals remain 50 / 50 without loading a crowd of full human rigs
        // during the first transition on memory-constrained phones.
        public static DungeonFaction FactionForStage(int id) => id < 0 || id >= StageCount ? DungeonFaction.None
            : id == 0 ? DungeonFaction.None : id == 2 ? DungeonFaction.Raiders
            : id % 4 == 0 ? DungeonFaction.Raiders : id % 4 == 1 ? DungeonFaction.Gang : DungeonFaction.None;
        public static readonly Vector2 CityCanvasSize = new Vector2(1440, 1000);
        // A coprime permutation scatters stable stage IDs
        // across the city without changing progression, rewards, or claimed contracts.
        public static Vector2 CityPoint(int id)
        {
            id = Mathf.Clamp(id, 0, StageCount - 1);
            int slot = ((id % 100) * 37 + 17) % 100;
            int column = slot % 10, row = slot / 10;
            return new Vector2(88 + column * 140 + Mathf.Sin(slot * 2.31f) * 12,
                83 + row * 91 + Mathf.Cos(slot * 1.73f) * 10) + (id >= 100 ? new Vector2(30, 30) : Vector2.zero);
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
            new[]{"崩壊した中心街","粉塵に沈む高層街","陥没地下鉄","ネオン廃きょ","コンクリート峡谷"},
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
        static readonly string[] missionEn = {"Defeat 18 hostiles","Defeat the boss","Recover 3 supplies across 2 floors","Break the infestation: 8 kills and 2 supplies","Recover supplies on each of 2 floors","Recover 5 supplies across 3 floors"};
        static readonly string[] missionJa = {"敵を18体倒す","ボスを倒す","2フロア以上で物資を3つ回収","感染網を断つ：敵8体と物資2つを回収","別々の2フロアで物資を1つずつ回収","3フロア以上で物資を5つ回収"};
        static readonly Color[] colors = {new Color(.31f,.63f,.55f),new Color(.84f,.43f,.23f),new Color(.23f,.62f,.72f),new Color(.48f,.68f,.35f),new Color(.86f,.28f,.20f)};
        public static ExpeditionStage Get(int id,int playerLevel=1)
        {
            id=Mathf.Clamp(id,0,StageCount-1); int region=(id%100)/20, n=id%20, place=n/4, variant=n%4+(id/100)*4;
            string loc=places[region][place], locJa=placesJa[region][place]; int danger=Mathf.Max(OverworldGeography.DungeonLevel(id),playerLevel-3);
            int mission=id==197?1:id==198?0:id%2==0?1:new[]{0,2,3,4,5}[(id/2)%5];
            var faction=FactionForStage(id);
            string target=mission==1?targets[region][(n*3+place)%5]:mission==0?"Zone hostiles":mission==3?"Infestation network":"Supply cache";
            string targetJa=mission==1?targetsJa[region][(n*3+place)%5]:mission==0?"区域の敵":mission==3?"感染網":"補給物資";
            return new ExpeditionStage { id=id,region=region,level=danger,legacyDungeonIndex=region==0?0:region<3?1:2,
                englishName=id==196?"RED CLAW STRONGHOLD":id==197?"COYOTE STRONGHOLD":loc+" / " + (variant+1).ToString("00"), japaneseName=id==196?"レッドクロウ本拠地":id==197?"コヨーテ本拠地":locJa+"・"+(variant+1).ToString("00"),
                englishLocation=regions[region]+" — "+loc,japaneseLocation=regionsJa[region]+" — "+locJa,
                englishBiome=faction==DungeonFaction.Raiders?"RED CLAW HIDEOUT":faction==DungeonFaction.Gang?"DUSK COYOTES HIDEOUT":biomes[region][place],japaneseBiome=faction==DungeonFaction.Raiders?"レッドクロウのアジト":faction==DungeonFaction.Gang?"ダスク・コヨーテのアジト":biomesJa[region][place],mission=(ExpeditionMission)mission,
                englishTarget=id==196?"RAVEN":id==197?"VEX":mission==1&&faction!=DungeonFaction.None?(faction==DungeonFaction.Raiders?"Raider commander":"Gang leader"):target,japaneseTarget=id==196?"レイヴン":id==197?"ヴェックス":mission==1&&faction!=DungeonFaction.None?(faction==DungeonFaction.Raiders?"レイダーの指揮官":"ギャングのリーダー"):targetJa,
                englishReward=RewardLabel(id,false),japaneseReward=RewardLabel(id,true),regionColor=colors[region] };
        }
        public static int MissionCount(int region,ExpeditionMission mission){int count=0;for(int id=0;id<StageCount;id++)if(Get(id).region==Mathf.Clamp(region,0,4)&&Get(id).mission==mission)count++;return count;}
        public static int RewardSeed(int stageId)=>unchecked(0x51A7D00D ^ (stageId+1)*7919);
        // Stable random target per site keeps map, gameplay and checkpoint in agreement.
        public static int KillTarget(int stageId, ExpeditionMission mission)
        {
            var roll=new System.Random(unchecked(0x1F287B ^ (stageId+1)*7919));
            return mission==ExpeditionMission.ClearHostiles ? roll.Next(40,51) : mission==ExpeditionMission.DestroyNest ? roll.Next(30,41) : 0;
        }
        public static string MissionLabel(ExpeditionStage stage,bool japanese)
        {
            if(stage==null)return "";
            int target=KillTarget(stage.id,stage.mission);
            if(stage.mission==ExpeditionMission.ClearHostiles)return japanese?"敵を"+target+"体倒す":"Defeat "+target+" hostiles";
            if(stage.mission==ExpeditionMission.DestroyNest)return japanese?"感染網を断つ：敵"+target+"体と物資2つを回収":"Break the infestation: "+target+" kills and 2 supplies";
            return MissionLabel(stage.mission,japanese);
        }
        public static string MissionLabel(ExpeditionMission mission,bool japanese)=>japanese?missionJa[(int)mission]:missionEn[(int)mission];
        static string RewardLabel(int id,bool ja) { string[] en={"Credits cache","Unusual gear","Armor cache","Skill point"};string[] jp={"クレジット備蓄","異能装備","防具箱","スキルポイント"};int kind=new System.Random(RewardSeed(id)).Next(4);return (ja?jp:en)[kind]; }
    }
}
