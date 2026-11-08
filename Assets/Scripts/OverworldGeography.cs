using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public sealed partial class OverworldGeography
    {
        public const int Seed=741921,Columns=200,Rows=200;
        public const float HalfExtent=5000,ChunkSize=100;
        public const int LandmarkCount=30;
        public SectorLayout Layout {get;private set;}
        public Vector3[] Camps {get;private set;}
        public Vector3[] Entrances {get;private set;}
        public Vector3[] Chests {get;private set;}
        public Vector3[] Landmarks {get;private set;}
        // Compatibility: these contain no full-world prop catalogue. Query GetChunk instead.
        public readonly List<Rect> Buildings=new List<Rect>();
        public readonly List<Vector3> Trees=new List<Vector3>(),Cars=new List<Vector3>();
        readonly List<Vector3> protectedSites=new List<Vector3>();
        readonly Dictionary<Vector2Int,List<Vector3>> siteBuckets=new Dictionary<Vector2Int,List<Vector3>>();
        static int[] dungeonLevels;
        static Vector3[] cachedLandmarkPositions;
        public static int DungeonLevel(int id)
        {
            if(dungeonLevels==null)new OverworldGeography();
            return dungeonLevels[Mathf.Clamp(id,0,dungeonLevels.Length-1)];
        }
        public static Vector3 LandmarkPosition(int id)
        {
            if(cachedLandmarkPositions==null)new OverworldGeography();
            return cachedLandmarkPositions[Mathf.Clamp(id,0,LandmarkCount-1)];
        }
        readonly Dictionary<Vector2Int,ChunkData> cache=new Dictionary<Vector2Int,ChunkData>();
        readonly Queue<Vector2Int> cacheOrder=new Queue<Vector2Int>();
        public sealed class ChunkData
        {
            public readonly List<Rect> Buildings=new List<Rect>();
            public readonly List<Vector3> Trees=new List<Vector3>(),Cars=new List<Vector3>();
        }
        public const int StreetSearchRadius=3;
        public const float BridgeHalfLength=32;
        public int District(Vector3 p)
        {
            if(Park(p))return 4;
            float belt=p.x+320*Mathf.Sin(p.z*.00065f);
            if(p.z<-2200)return 3;
            if(belt>-650&&belt<1250&&p.z>-1700&&p.z<1700)return 0;
            if(belt>1600&&p.z<2300)return 2;
            return belt<-700?3:1;
        }
        // These vectors are also consumed by the water shader: shore shading must
        // follow exactly the same centre line and varying width as navigation.
        public static readonly Vector4 RiverShapeParameters=new Vector4(420,430,140,110);
        public static readonly Vector4 RiverFrequencyParameters=new Vector4(.00067f,.00161f,0,.7f);
        public static readonly Vector4 RiverWidthParameters=new Vector4(110,15,5,0);
        public static readonly Vector4 RiverWidthFrequencyParameters=new Vector4(.00091f,.0023f,1.2f,0);
        public float RiverX(float z)=>RiverShapeParameters.x+RiverShapeParameters.y*Mathf.Sin(z*RiverFrequencyParameters.x+RiverFrequencyParameters.z)+RiverShapeParameters.z*Mathf.Sin(z*RiverFrequencyParameters.y+RiverFrequencyParameters.w);
        public float RiverWidth(float z)=>2*(RiverWidthParameters.x+RiverWidthParameters.y*Mathf.Sin(z*RiverWidthFrequencyParameters.x+RiverWidthFrequencyParameters.z)+RiverWidthParameters.z*Mathf.Sin(z*RiverWidthFrequencyParameters.y+RiverWidthFrequencyParameters.w));
        public bool Bridge(float z)=>Mathf.Abs(z-Mathf.Round(z/1000)*1000)<BridgeHalfLength&&Mathf.Abs(z)<4500;
        public bool River(Vector3 p)=>Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f;
        public bool PocketPark(Vector3 p)
        {
            if(River(p)||SiteProtected(p,25))return false;
            var parks=new[]{new Vector2(-1350,-2100),new Vector2(-1700,-650),new Vector2(-1350,1050),new Vector2(-2350,2300),new Vector2(-200,2400),new Vector2(1500,2500),new Vector2(3650,650),new Vector2(2300,-650),new Vector2(1400,-1950),new Vector2(3000,-2800),new Vector2(800,-3600),new Vector2(700,400),new Vector2(-450,-1350),new Vector2(450,1350),new Vector2(1950,650),new Vector2(-2300,-3300),new Vector2(-3150,350),new Vector2(-850,3150),new Vector2(2750,-1450),new Vector2(-2650,-850),new Vector2(1150,1850),new Vector2(-1650,-3150),new Vector2(3350,1850),new Vector2(-3450,1850)};
            for(int i=0;i<parks.Length;i++)
            {
                float r=78f+(i%3)*18f;
                if((new Vector2(p.x,p.z)-parks[i]).sqrMagnitude<r*r)return true;
            }
            int gx=Mathf.FloorToInt((p.x+5000)/250f),gz=Mathf.FloorToInt((p.z+5000)/250f);
            int hash=Mathf.Abs(unchecked(gx*73856093^gz*19349663));
            if(hash%5!=0)return false;
            Vector2 centre=new Vector2(gx*250-5000+150,gz*250-5000+150);
            return (new Vector2(p.x,p.z)-centre).sqrMagnitude<58f*58f;
        }
        public bool DenseForest(Vector3 p)
        {
            if(River(p)||RoadClearance(p)<4f||SiteProtected(p,18))return false;
            var woods=new[]{new Vector3(-2850,0,-1450),new Vector3(-3100,0,950),new Vector3(-1850,0,3050),new Vector3(-950,0,-3150),new Vector3(450,0,3150),new Vector3(2350,0,1550),new Vector3(3350,0,-1550),new Vector3(1850,0,-3250),new Vector3(-550,0,150),new Vector3(1150,0,-650),new Vector3(-2050,0,-1550),new Vector3(2650,0,3200)};
            for(int i=0;i<woods.Length;i++)
            {
                float rad=210f+(i%4)*45f;
                float dx=p.x-woods[i].x,dz=p.z-woods[i].z;
                if(dx*dx+dz*dz<rad*rad)return true;
            }
            float n=Mathf.PerlinNoise(p.x*.0019f+41.3f,p.z*.0019f+83.7f);
            return Park(p)?n>.36f:n>.66f;
        }

        // 30 Named Overworld Landmarks (3x the original 10)
        static readonly (Vector2Int chunk, int kind, Vector3 pos)[] FixedLandmarks = new[]
        {
            (new Vector2Int(-14,-21), 0, new Vector3(-1348,0,-2048)), // 00: Clock Tower
            (new Vector2Int(-17,-7),  1, new Vector3(-1648,0,-648)),  // 01: Memorial Arch
            (new Vector2Int(-7,-5),   2, new Vector3(-648,0,-448)),   // 02: Fountain Plaza
            (new Vector2Int(7,4),     3, new Vector3(752,0,452)),     // 03: Civic Rotunda
            (new Vector2Int(36,6),    4, new Vector3(3652,0,652)),    // 04: Water Tower
            (new Vector2Int(29,28),   5, new Vector3(2952,0,2852)),   // 05: North Obelisk
            (new Vector2Int(-24,23),  6, new Vector3(-2348,0,2352)),  // 06: Radio Spire
            (new Vector2Int(14,-20),  7, new Vector3(1452,0,-1948)),  // 07: Silo Plaza
            (new Vector2Int(-29,-15), 8, new Vector3(-2848,0,-1448)), // 08: Deepwood Shrine
            (new Vector2Int(24,15),   9, new Vector3(2452,0,1552)),   // 09: Botanical Dome
            (new Vector2Int(-3,-14),  2, new Vector3(-248,0,-1348)),  // 10: Sunken Concourse
            (new Vector2Int(19,7),    4, new Vector3(1952,0,752)),    // 11: Twin Cisterns
            (new Vector2Int(-32,9),   10, new Vector3(-3148,0,952)),  // 12: Observatory Dome
            (new Vector2Int(-19,31),  11, new Vector3(-1848,0,3152)), // 13: War Cenotaph
            (new Vector2Int(-5,24),   3, new Vector3(-448,0,2452)),   // 14: Old City Hall
            (new Vector2Int(15,25),   12, new Vector3(1552,0,2552)),  // 15: Rail Turntable
            (new Vector2Int(34,-16),  13, new Vector3(3452,0,-1548)), // 16: Goliath Crane
            (new Vector2Int(23,-7),   14, new Vector3(2352,0,-648)),  // 17: Cooling Towers
            (new Vector2Int(30,-28),  15, new Vector3(3052,0,-2748)), // 18: Blast Furnace
            (new Vector2Int(8,-36),   16, new Vector3(852,0,-3548)),  // 19: Amphitheater
            (new Vector2Int(-10,-32), 17, new Vector3(-948,0,-3148)), // 20: Radar Array
            (new Vector2Int(-23,-33), 18, new Vector3(-2248,0,-3248)),// 21: Oil Derrick
            (new Vector2Int(-38,-24), 19, new Vector3(-3748,0,-2348)),// 22: Cathedral Spire
            (new Vector2Int(-36,-5),  1, new Vector3(-3548,0,-448)),  // 23: Quarantine Gate
            (new Vector2Int(-12,11),  12, new Vector3(-1148,0,1152)), // 24: Armored Train Wreck
            (new Vector2Int(5,14),    8, new Vector3(552,0,1452)),    // 25: Bell Pavilion
            (new Vector2Int(5,32),    20, new Vector3(552,0,3252)),   // 26: Wind Turbine Array
            (new Vector2Int(27,32),   21, new Vector3(2752,0,3252)),  // 27: Command Bunker
            (new Vector2Int(38,20),   17, new Vector3(3852,0,2052)),  // 28: Dish Network
            (new Vector2Int(19,-33),  22, new Vector3(1952,0,-3248))  // 29: Launch Gantry
        };

        static readonly string[] LandmarkTitlesEn =
        {
            "SOUTH MARKET / CLOCK TOWER", "OLD CITY / MEMORIAL ARCH", "RIVERFRONT PARK / FOUNTAIN", "CIVIC QUARTER / ROTUNDA", "EAST MARKET / WATER TOWER",
            "NORTH COMMON / OBELISK", "WESTWOOD / RADIO SPIRE", "SOUTHWORKS / SILO PLAZA", "DEEPWOOD / ANCIENT SHRINE", "EASTERN WOODS / BOTANICAL DOME",
            "MIDTOWN / SUNKEN CONCOURSE", "RUSTYARDS / TWIN CISTERNS", "HIGHLAND / OBSERVATORY DOME", "NORTHRIDGE / WAR CENOTAPH", "CAPITOL / OLD CITY HALL",
            "JUNCTION / RAIL TURNTABLE", "DRYDOCK / GOLIATH CRANE", "REACTOR / COOLING TOWERS", "FOUNDRY / BLAST FURNACE", "GARDENS / AMPHITHEATER",
            "SALT FLATS / RADAR ARRAY", "SLAG FIELD / OIL DERRICK", "ASH QUARTER / CATHEDRAL SPIRE", "WEST WALL / QUARANTINE GATE", "REDLINE / ARMORED TRAIN",
            "TERRACE / BELL PAVILION", "GALE RIDGE / WIND TURBINES", "CITADEL / COMMAND BUNKER", "SKYLINE / DISH NETWORK", "CRATER / LAUNCH GANTRY"
        };

        static readonly string[] LandmarkTitlesJa =
        {
            "サウス・マーケット／時計塔", "オールド・シティ／記念凱旋門", "リバーフロント／大噴水広場", "シビック・クォーター／円形神殿", "イースト・マーケット／給水塔",
            "ノース・コモン／黒曜オベリスク", "ウェストウッド／電波スパイア", "サウスワークス／サイロ・プラザ", "ディープウッド／古樹の祠", "イースタン・ウッズ／植物園ドーム",
            "ミッドタウン／沈下コンコース", "ラストヤード／双子貯水槽", "ハイランド／天文台ドーム", "ノースリッジ／戦没者慰霊碑", "キャピトル／旧市庁舎廃墟",
            "ジャンクション／鉄道転車台", "ドライドック／巨大クレーン", "リアクター／冷却塔群", "ファウンドリー／高炉プラザ", "ガーデンズ／野外音楽堂",
            "ソルトフラッツ／レーダー基地", "スラグフィールド／石油ヤグラ", "アッシュ・クォーター／大聖堂尖塔", "ウェストウォール／検疫ゲート", "レッドライン／装甲列車残骸",
            "テラス／鐘楼パビリオン", "ゲイルリッジ／風力タービン群", "シタデル／バンカー司令塔", "スカイライン／通信パラボラ群", "クレーター／ロケット発射台"
        };

        static readonly string[] LandmarkShortEn =
        {
            "CLOCK TOWER", "MEMORIAL ARCH", "FOUNTAIN PLAZA", "CIVIC ROTUNDA", "WATER TOWER",
            "NORTH OBELISK", "RADIO SPIRE", "SILO PLAZA", "ANCIENT SHRINE", "BOTANICAL DOME",
            "SUNKEN CONCOURSE", "TWIN CISTERNS", "OBSERVATORY DOME", "WAR CENOTAPH", "OLD CITY HALL",
            "RAIL TURNTABLE", "GOLIATH CRANE", "COOLING TOWERS", "BLAST FURNACE", "AMPHITHEATER",
            "RADAR ARRAY", "OIL DERRICK", "CATHEDRAL SPIRE", "QUARANTINE GATE", "ARMORED TRAIN",
            "BELL PAVILION", "WIND TURBINES", "COMMAND BUNKER", "DISH NETWORK", "LAUNCH GANTRY"
        };

        static readonly string[] LandmarkShortJa =
        {
            "時計塔", "記念凱旋門", "大噴水広場", "円形神殿ロタンダ", "給水塔",
            "黒曜オベリスク", "電波スパイア", "サイロ・プラザ", "古樹の祠", "植物園ドーム",
            "沈下コンコース", "双子貯水槽", "天文台ドーム", "戦没者慰霊碑", "旧市庁舎廃墟",
            "鉄道転車台", "巨大クレーン", "冷却塔群", "高炉プラザ", "野外音楽堂",
            "レーダー基地", "石油ヤグラ", "大聖堂尖塔", "検疫ゲート", "装甲列車残骸",
            "鐘楼パビリオン", "風力タービン群", "バンカー司令塔", "通信パラボラ群", "ロケット発射台"
        };

        static readonly string[] LandmarkDescEn =
        {
            "A tall pre-collapse brick clock tower overlooking the southern bazaar.",
            "A weathered triumphal arch honoring the old city's fallen defenders.",
            "A grand stone plaza built around a dry, overgrown riverside fountain.",
            "A neoclassical colonnade rotunda surrounded by shattered marble pillars.",
            "An elevated steel water tank converted into a sniper lookout post.",
            "A monolithic black stone obelisk ringed by ancient survey pylons.",
            "A high-output lattice radio transmission tower still humming with static.",
            "Twin industrial grain silos fortified with scrap-metal catwalks.",
            "A moss-covered stone sanctuary deep within the western redwood grove.",
            "A geodesic glass conservatory overtaken by mutated flora.",
            "A collapsed transit plaza with tiered stone terraces and dry basins.",
            "Paired rusted chemical storage tanks rising above the eastern yards.",
            "A cracked brass-and-concrete astronomical observatory dome on the ridge.",
            "A solemn stepped stone cenotaph flanked by eternal braziers.",
            "The pillared portico and shattered dome of the pre-war municipal hall.",
            "A circular locomotive turntable surrounded by rusted railcars.",
            "A towering shipyard gantry crane looming over the easternindustrial belt.",
            "Hyperbolic concrete cooling towers venting residual toxic haze.",
            "A massive cast-iron blast furnace and slag pit in the southern foundry.",
            "A semicircular open-air stone theater in the southern parklands.",
            "A rotating military radar dish installation scanning the southern waste.",
            "A skeletal steel drilling derrick pumping black sludge from the basin.",
            "The gothic stone spire and rose-window facade of a ruined cathedral.",
            "A reinforced concrete checkpoint gate that once sealed the western sector.",
            "A derailed armored supply locomotive fortified by wasteland scavengers.",
            "A ceremonial bell pavilion overlooking the central park terrace.",
            "Three towering wind turbines with fractured blades spinning in the gale.",
            "An armored concrete command bunker bristling with periscopes and vents.",
            "A cluster of deep-space parabolic dishes linked by elevated walkways.",
            "A colossal steel launch gantry standing beside a scorched blast pit."
        };

        static readonly string[] LandmarkDescJa =
        {
            "南の市場跡を見下ろす、崩壊前の高層レンガ時計塔。",
            "旧市街の防衛戦没者を讃えて建てられた風化した凱旋門。",
            "枯れた大噴水を中心に石畳と街灯が広がる河岸の記念広場。",
            "砕けた大理石の列柱に囲まれた新古典主義の円形神殿。",
            "狙撃用の見張り台へと改造された高架式の鋼鉄製給水塔。",
            "古い測量パイロンに囲まれた黒曜石の巨大オベリスク。",
            "今も微弱なノイズを発し続ける高出力の鉄骨電波塔。",
            "スクラップのキャットウォークで要塞化された双子の産業サイロ。",
            "西の深い針葉樹林の奥に佇む、苔むした石造りの祠。",
            "変異した植物に覆い尽くされたガラス張りの巨大植物園ドーム。",
            "段状の石テラスと枯れた水盤が残る沈下した駅前広場。",
            "東の操車場跡にそびえる錆びついた双子の大型貯水タンク。",
            "尾根の上に建つ、ひび割れたドーム屋根の旧天文観測所。",
            "消えない篝火台に挟まれた、重厚な段状石造りの戦没者慰霊碑。",
            "大戦前の栄華を伝える列柱ポーチと崩れたドームの旧市庁舎。",
            "錆びた貨車群に囲まれた円形の機関車ターンテーブル。",
            "東の工業地帯にそびえ立つ造船所用ゴライアスクレーン。",
            "残留ガスが立ち昇る双曲面コンクリートの巨大冷却塔。",
            "南の製鉄地区に鎮座する黒鉄の高炉とスラグピット。",
            "南の庭園地区に残る半円形石造りの野外音楽堂。",
            "荒野を走査し続ける旧軍の大型回転レーダー基地。",
            "盆地の底から黒い汚泥を汲み上げる鉄骨の石油掘削ヤグラ。",
            "バラ窓のファサードと鋭い石造尖塔が残る大聖堂の廃墟。",
            "かつて西セクターを封鎖していた強化コンクリートの検疫ゲート。",
            "脱線したまま要塞化された重装甲補給列車の残骸。",
            "中央公園のテラスを見晴らす儀式用の大鐘楼パビリオン。",
            "折れたブレードが強風に軋む3基の巨大風力タービン。",
            "潜望鏡と換気ダクトが突き出す重装甲の軍事バンカー司令塔。",
            "高架通路で結ばれたパラボラアンテナ群の通信施設。",
            "焦げた噴射ピットの傍らにそびえるロケット発射ガントリー。"
        };

        static readonly string[] LandmarkBossEn =
        {
            "CHRONOS WARDEN / KRON", "ARCH EXECUTIONER / VANE", "DROWNED SOVEREIGN / MIRE", "SENATOR HUSK / AURELIUS", "CISTERN SNIPER / HAWK",
            "MONOLITH PROPHET / ZEL", "STATIC OVERLORD / VOLT", "SILO BUTCHER / GRIM", "ELDERWOOD BEHEMOTH / THORN", "SPORE EMPRESS / FLORA",
            "PLAZA TYRANT / REX", "RUST BARON / KORVUS", "STARGAZER ABOMINATION / ORION", "ASHEN CENTURION / MORG", "CHANCELLOR GHOUL / CAIN",
            "RAIL JUGGERNAUT / LOCO", "DOCKYARD TITAN / ATLAS", "MELTDOWN COLOSSUS / RADON", "FORGE OVERSEER / VULCAN", "MAESTRO OF BONE / LYRA",
            "SIGNAL HUNTER / ECHO", "SLUDGE GOLIATH / TAR", "BELLTOWER PHANTOM / VESPERS", "QUARANTINE MARSHAL / WARD", "IRON CONDUCTOR / BRAKE",
            "CHIME STALKER / KNELL", "GALE HARBINGER / ZEPHYR", "BUNKER GENERAL / KRIEG", "UPLINK WARLORD / NEXUS", "IGNITION FIEND / APOGEE"
        };

        static readonly string[] LandmarkBossJa =
        {
            "時計塔の番人 クロン", "凱旋門の処刑人 ヴェイン", "噴水広場の水没王 マイア", "円形神殿の骸議員 アウレリウス", "給水塔の狙撃手 ホーク",
            "黒曜石の預言者 ゼル", "電波塔の覇王 ヴォルト", "サイロの解体屋 グリム", "古樹の巨獣 ソーン", "植物園の胞子姫 フローラ",
            "広場の暴君 レックス", "錆の男爵 コルヴス", "星見の怪異 オリオン", "慰霊碑の百人隊長 モルグ", "旧市庁舎の亡霊宰相 カイン",
            "転車台の重装兵 ロコ", "港湾の巨神 アトラス", "冷却塔の融解巨人 ラドン", "高炉の監督官 ヴルカン", "音楽堂の骨楽師 リラ",
            "レーダーの追跡者 エコー", "油田の泥塊巨人 タール", "尖塔の亡霊 ヴェスパー", "検疫門の元帥 ウォード", "装甲列車の車掌 ブレイク",
            "鐘楼の狩人 ネル", "風車の先触れ ゼファー", "バンカーの将軍 クリーグ", "通信網の督戦官 ネクサス", "発射台の業火魔 アポジー"
        };

        public static string LandmarkTitle(int id, bool japanese) =>
            (japanese ? LandmarkTitlesJa : LandmarkTitlesEn)[Mathf.Clamp(id, 0, LandmarkCount - 1)];

        public static string LandmarkShortTitle(int id, bool japanese) =>
            (japanese ? LandmarkShortJa : LandmarkShortEn)[Mathf.Clamp(id, 0, LandmarkCount - 1)];

        public static string LandmarkDescription(int id, bool japanese) =>
            (japanese ? LandmarkDescJa : LandmarkDescEn)[Mathf.Clamp(id, 0, LandmarkCount - 1)];

        public static string LandmarkBossName(int id, bool japanese) =>
            (japanese ? LandmarkBossJa : LandmarkBossEn)[Mathf.Clamp(id, 0, LandmarkCount - 1)];

        public static int LandmarkBossFaction(int id) =>
            id % 3 == 0 ? 0 : id % 3 == 1 ? 1 : 2; // 0: Infected Colossus/Mutant Boss, 1: Red Claw Commander, 2: Coyote Warlord

        public static int LandmarkBossBaseLevel(int id)
        {
            Vector3 pos = LandmarkPosition(id);
            float dist = Vector3.Distance(new Vector3(3450, 0, 990), pos);
            return Mathf.Clamp(2 + Mathf.FloorToInt(dist / 900f) + (id % 3), 2, 12);
        }

        public static int LandmarkBossLevel(int id, int playerLevel) =>
            Mathf.Max(LandmarkBossBaseLevel(id), playerLevel + 1);

        public int LandmarkIndexAtChunk(Vector2Int chunkKey, out int kind, out Vector3 position)
        {
            for(int i=0;i<FixedLandmarks.Length;i++)
            {
                if(FixedLandmarks[i].chunk==chunkKey)
                {
                    kind=FixedLandmarks[i].kind;
                    position=Landmarks!=null&&i<Landmarks.Length?Landmarks[i]:FixedLandmarks[i].pos;
                    return i;
                }
            }
            kind=-1;position=default;return -1;
        }

        public int LandmarkKind(Vector2Int chunkKey,out Vector3 position)
        {
            if(LandmarkIndexAtChunk(chunkKey,out int fixedKind,out position)>=0)return fixedKind;
            int h=Mathf.Abs(unchecked(chunkKey.x*49999^chunkKey.y*81919));
            if(h%10==0)
            {
                var candidate=new Vector3(chunkKey.x*100+52,0,chunkKey.y*100+52);
                if(!River(candidate)&&RoadClearance(candidate)>6f&&!SiteProtected(candidate,18))
                {
                    position=candidate;
                    return h%16;
                }
            }
            position=default;return -1;
        }
        bool CityPark(Vector3 p,float x,float z,float rx,float rz,float phase)
        {
            float dx=(p.x-x)/rx,dz=(p.z-z)/rz;
            float angle=Mathf.Atan2(dz,dx);
            float edge=1+.12f*Mathf.Sin(angle*3+phase)+.06f*Mathf.Sin(angle*5-phase);
            return dx*dx+dz*dz<edge*edge;
        }
        // The outer green belts and two city parks occupy about 18% of the world.
        public bool Park(Vector3 p)=>p.x<-4000+330*Mathf.Sin(p.z*.0009f)
            ||p.z>4300+240*Mathf.Sin(p.x*.0008f)
            ||CityPark(p,-700,-500,600,350,.8f)
            ||CityPark(p,2900,2800,660,440,2.1f);
        // Legacy street APIs retained for compatibility only. Actual roads are the
        // finite bucketed graph in OverworldRoadNetwork, shared with streamed meshes.
        public float StreetX(int street,float z)=>street*100
            +74*Mathf.Sin(z*.0009f)+36*Mathf.Sin(z*.0021f+street*.27f)
            +13*Mathf.Sin(z*.006f+street*.72f);
        public float StreetZ(int street,float x)
        {
            float bend=70*Mathf.Sin(x*.00082f+.5f)+34*Mathf.Sin(x*.0019f-street*.24f)+12*Mathf.Sin(x*.0058f+street*.8f);
            if(street%10==0)bend*=Mathf.Clamp01((Mathf.Abs(x-RiverX(street*100))-130)/240);
            return street*100+bend;
        }
        public float StreetWidth(int street)=>street%10==0?25:street%5==0?15:7;
        public bool StreetSegmentOpen(int street,int segment,bool northSouth)
        {
            if(street%5==0)return true;
            var p=northSouth?new Vector3(StreetX(street,segment*100+50),0,segment*100+50):new Vector3(segment*100+50,0,StreetZ(street,segment*100+50));
            int district=District(p);
            int period=district==0?2:district==1?3:district==2?4:5;
            if(district==4)return false;
            // Omit entire local corridors coherently; district transitions join arterials.
            return ((street%period)+period)%period==1;
        }
        bool ParcelClear(Rect rect)
        {
            // Sample the whole footprint so curves and river banks cannot cut through a house.
            for(float x=rect.xMin;x<=rect.xMax+4;x+=4)for(float z=rect.yMin;z<=rect.yMax+4;z+=4){var p=new Vector3(Mathf.Min(x,rect.xMax),0,Mathf.Min(z,rect.yMax));if(River(p)||RoadClearance(p)<4.5f||SiteProtected(p,3)||Park(p)||PocketPark(p)||DenseForest(p))return false;}
            if(LandmarkKind(new Vector2Int(Mathf.FloorToInt(rect.center.x/100),Mathf.FloorToInt(rect.center.y/100)),out Vector3 lPos)>=0&&(new Vector2(rect.center.x,rect.center.y)-new Vector2(lPos.x,lPos.z)).sqrMagnitude<24*24)return false;
            return true;
        }
        public OverworldGeography()
        {
            BuildRoadNetwork();
            var random=new System.Random(Seed);var map=new bool[Columns,Rows];
            for(int x=0;x<Columns;x++)for(int y=0;y<Rows;y++){var p=new Vector3((x+.5f)*50-HalfExtent,0,(y+.5f)*50-HalfExtent);map[x,y]=!River(p)||Bridge(p.z);}
            // Every bridge crossing occupies an entire coarse navigation row.
            for(int y=0;y<Rows;y++)if(Bridge((y+.5f)*50-HalfExtent))for(int x=0;x<Columns;x++)map[x,y]=true;
            Camps=new Vector3[14];for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7+.28f;Camps[i]=SafeSite(new Vector3(Mathf.Cos(a)*3600,.65f,Mathf.Sin(a)*3400));}
            Entrances=new Vector3[ExpeditionMap.StageCount];Entrances[0]=SafeSite(Camps[0]+new Vector3(-30,0,0));
            for(int i=1;i<100;i++)
            {Vector3 chosen=Vector3.zero;float best=-1;for(int trial=0;trial<40;trial++){var p=new Vector3(-4700+(float)random.NextDouble()*9400,.65f,-4700+(float)random.NextDouble()*9400);float near=float.MaxValue;foreach(var q in protectedSites)near=Mathf.Min(near,(p-q).sqrMagnitude);if(near>best){best=near;chosen=p;}}Entrances[i]=SafeSite(chosen);}
            Chests=new Vector3[500];for(int i=0;i<36;i++)Chests[i]=SafeSite(new Vector3(-4300+i%6*1700+(float)random.NextDouble()*250,.65f,-4300+i/6*1700+(float)random.NextDouble()*250));
            // Keep ID order stable; every cache reserves an accessible clearing before
            // any streamed parcel is generated, including caches in the widened floodplain.
            for(int i=36;i<180;i++)
            {
                int cell=i-36,cx=-45+cell%12*8,cz=-45+cell/12*8;
                Chests[i]=SafeSite(new Vector3(cx*100+25,.65f,cz*100+25));
            }
            // Append new IDs only after the legacy sites and random rolls are fixed.
            // A second, offset camp ring fills the interior gaps without renumbering saves.
            for(int i=7;i<Camps.Length;i++)
            {
                float a=(i-7)*Mathf.PI*2/7+.28f+Mathf.PI/7;
                Camps[i]=NewSite(new Vector3(Mathf.Cos(a)*2150,.65f,Mathf.Sin(a)*2050),true);
            }
            // 500 caches over the 100 km² world: five per km² on average.
            for(int i=180;i<Chests.Length;i++)
            {
                int slot=i-180,column=slot%20,row=slot/20;
                float jitterX=(float)random.NextDouble()*240-120,jitterZ=(float)random.NextDouble()*300-150;
                Chests[i]=NewSite(new Vector3(-4650+column*490+jitterX,.65f,-4650+row*620+jitterZ));
            }
            // Append only after every legacy random roll and site is fixed.
            // Three additional starter sites are constrained to the 1 km square
            // centred on the first camp; entrance zero is already within it.
            for(int i=100;i<103;i++)
                Entrances[i]=NewSite(Camps[0]+new Vector3((i-101)*180,0,220),false,Camps[0]);
            var extraRandom=new System.Random(Seed^0x5A17);
            for(int i=103;i<ExpeditionMap.RegularStageCount;i++)
            {
                Vector3 chosen=Vector3.zero;float best=-1;
                for(int trial=0;trial<40;trial++)
                {
                    var p=new Vector3(-4700+(float)extraRandom.NextDouble()*9400,.65f,-4700+(float)extraRandom.NextDouble()*9400);
                    float near=float.MaxValue;foreach(var q in protectedSites)near=Mathf.Min(near,(p-q).sqrMagnitude);
                    if(near>best){best=near;chosen=p;}
                }
                Entrances[i]=NewSite(chosen);
            }
            Vector3[] bossDesired={
                new Vector3(-2500,.65f,-2100), new Vector3(-3200,.65f,-2850), new Vector3(-3900,.65f,-3600),
                new Vector3(2500,.65f,2100), new Vector3(3200,.65f,2850), new Vector3(3900,.65f,3600),
                new Vector3(-1000,.65f,2600), new Vector3(250,.65f,3400), new Vector3(1350,.65f,4100)
            };
            for(int i=ExpeditionMap.RegularStageCount;i<Entrances.Length;i++)
                Entrances[i]=NewSite(bossDesired[i-ExpeditionMap.RegularStageCount],true);
            AddResidentialRoads();
            Landmarks=new Vector3[LandmarkCount];
            for(int i=0;i<LandmarkCount;i++)
            {
                Vector3 p=FixedLandmarks[i].pos;p.y=.65f;
                if(River(p))p.x=RiverX(p.z)+RiverWidth(p.z)*.5f+45f;
                Landmarks[i]=p;
            }
            cachedLandmarkPositions=Landmarks;
            var levels=new int[Entrances.Length];
            for(int i=0;i<levels.Length;i++)
            {
                if(i>=ExpeditionMap.RegularStageCount)
                {
                    int tier=(i-ExpeditionMap.RegularStageCount)%3;
                    levels[i]=tier==0?4:tier==1?7:10;
                    continue;
                }
                float distance=Vector3.Distance(Camps[0],Entrances[i]);
                // Nearby expeditions stay gentle; farther rings vary by one level.
                int variation=i==0?0:(i*37%3)-1;
                levels[i]=i>=100&&i<103?1:Mathf.Clamp(1+Mathf.FloorToInt(distance/850)+variation,1,10);
            }
            dungeonLevels=levels;
            var entry=new Vector2Int(Mathf.FloorToInt((Camps[0].x+HalfExtent)/50),Mathf.FloorToInt((Camps[0].z+HalfExtent)/50));
            Layout=new SectorLayout(map,entry,new Vector2Int(10,10),50,Blocked);
        }
        Vector3 SafeSite(Vector3 p)
        {
            p.x=Mathf.Clamp(p.x,-4750,4750);p.z=Mathf.Clamp(p.z,-4750,4750);
            if(Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f+35)p.x=RiverX(p.z)+RiverWidth(p.z)*.5f+45;
            protectedSites.Add(p);
            var key=new Vector2Int(Mathf.FloorToInt(p.x/100),Mathf.FloorToInt(p.z/100));
            if(!siteBuckets.TryGetValue(key,out var bucket)){bucket=new List<Vector3>();siteBuckets.Add(key,bucket);}bucket.Add(p);
            return p;
        }
        Vector3 NewSite(Vector3 desired,bool camp=false,Vector3? squareCenter=null)
        {
            // Bounded radial search reserves walkable land clear of roads and old entrances.
            for(int ring=0;ring<=60;ring++)for(int step=0;step<(ring==0?1:24);step++)
            {
                float angle=step*Mathf.PI*2/24;
                var p=desired+new Vector3(Mathf.Cos(angle)*ring*10,0,Mathf.Sin(angle)*ring*10);
                if(Mathf.Abs(p.x)>4750||Mathf.Abs(p.z)>4750||River(p)||RoadClearance(p)<8||SiteProtected(p,20))continue;
                if(squareCenter.HasValue&&(Mathf.Abs(p.x-squareCenter.Value.x)>500||Mathf.Abs(p.z-squareCenter.Value.z)>500))continue;
                if(Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f+35)continue;
                if(camp&&!CampFootprintClear(p))continue;
                return SafeSite(p);
            }
            throw new InvalidOperationException("No clear overworld site near "+desired);
        }
        bool CampFootprintClear(Vector3 threshold)
        {
            // The facade sits north of the threshold. Reserve its complete shell,
            // canopy and a small shoulder rather than checking only the doorway.
            for(float x=-7;x<=7;x+=2)for(float z=0;z<=12;z+=2)
            {
                var p=threshold+new Vector3(x,0,z);
                if(River(p)||RoadClearance(p)<2)return false;
            }
            return true;
        }
        public bool SiteProtected(Vector3 p,float extra=0)
        {
            float radius=12+extra;int minX=Mathf.FloorToInt((p.x-radius)/100),maxX=Mathf.FloorToInt((p.x+radius)/100);
            int minZ=Mathf.FloorToInt((p.z-radius)/100),maxZ=Mathf.FloorToInt((p.z+radius)/100);
            for(int x=minX;x<=maxX;x++)for(int z=minZ;z<=maxZ;z++)
                if(siteBuckets.TryGetValue(new Vector2Int(x,z),out var bucket))foreach(var site in bucket)if((site-p).sqrMagnitude<radius*radius)return true;
            return false;
        }
        public ChunkData GetChunk(int x,int z)
        {
            var key=new Vector2Int(x,z);if(cache.TryGetValue(key,out var data))return data;
            data=new ChunkData();var rng=new System.Random(unchecked(Seed+x*73856093^z*19349663));
            float left=x*ChunkSize,bottom=z*ChunkSize;
            // Rejection packing makes buildings independent of the 100m streaming grid.
            // A two metre margin at chunk borders prevents neighbouring cache overlap.
            for(int attempt=0;attempt<1800&&data.Buildings.Count<48;attempt++)
            {
                var p=new Vector3(left+8+(float)rng.NextDouble()*84,0,bottom+8+(float)rng.NextDouble()*84);
                int district=District(p);if(district==4)continue;
                bool warehouse=district==2&&rng.NextDouble()<.09;
                float w=warehouse?24+(float)rng.NextDouble()*15:8+(float)rng.NextDouble()*14;
                float h=warehouse?19+(float)rng.NextDouble()*17:8+(float)rng.NextDouble()*14;
                var rect=new Rect(p.x-w*.5f,p.z-h*.5f,w,h);
                if(rect.xMin<left+2||rect.xMax>left+98||rect.yMin<bottom+2||rect.yMax>bottom+98||Mathf.Abs(p.x)>4960||Mathf.Abs(p.z)>4960)continue;
                // Keep coarse navigation cell centres free for the shared 50m graph.
                bool overlaps=false;
                foreach(var other in data.Buildings)if(new Rect(other.xMin-1.5f,other.yMin-1.5f,other.width+3,other.height+3).Overlaps(rect)){overlaps=true;break;}
                // Narrow pedestrian passages connect every 50m navigation centre.
                // Their clear width is independent of the vehicle-road layout.
                for(float spine=25;spine<100;spine+=50)
                    if((rect.xMin<left+spine+1.4f&&rect.xMax>left+spine-1.4f)||(rect.yMin<bottom+spine+1.4f&&rect.yMax>bottom+spine-1.4f))overlaps=true;
                if(!overlaps&&ParcelClear(rect))data.Buildings.Add(rect);
            }
            for(int i=0;i<140;i++){var p=new Vector3(left+10+(float)rng.NextDouble()*80,0,bottom+10+(float)rng.NextDouble()*80);if(RoadClearance(p)>1.5f&&RoadClearance(p)<4&&!River(p)&&!SiteProtected(p,5)&&Mathf.Abs(Mathf.Repeat(p.x,50)-25)>5&&Mathf.Abs(Mathf.Repeat(p.z,50)-25)>7){bool clear=true;foreach(var b in data.Buildings)if(new Rect(b.xMin-2,b.yMin-4,b.width+4,b.height+8).Contains(new Vector2(p.x,p.z))){clear=false;break;}if(clear){foreach(var car in data.Cars)if((car-p).sqrMagnitude<30){clear=false;break;}if(clear)data.Cars.Add(p);}}}
            // Separate RNG preserves existing buildings and cars; navigation spines stay clear.
            var forest=new System.Random(unchecked(Seed+x*92821+z*68917));
            for(int i=0;i<960;i++){
                var p=new Vector3(left+3+(float)forest.NextDouble()*94,0,bottom+3+(float)forest.NextDouble()*94);
                bool park=Park(p)||PocketPark(p),dense=DenseForest(p);
                float clear=RoadClearance(p);
                bool boulevard=!park&&!dense&&clear>=2.1f&&clear<=6.2f;
                int limit=dense?960:park?680:boulevard?380:220;
                if(i>=limit)continue;
                if(River(p)||clear<2.1f||SiteProtected(p,8)||Mathf.Abs(p.x)>4990||Mathf.Abs(p.z)>4990)continue;
                if(Mathf.Abs(Mathf.Repeat(p.x,50)-25)<4||Mathf.Abs(Mathf.Repeat(p.z,50)-25)<4)continue;
                bool occupied=false;foreach(var b in data.Buildings)if(new Rect(b.xMin-2,b.yMin-2,b.width+4,b.height+4).Contains(new Vector2(p.x,p.z))){occupied=true;break;}foreach(var car in data.Cars)if((car-p).sqrMagnitude<16)occupied=true;if(occupied)continue;
                float minSpacingSq=dense?5.76f:park?8.5f:9.6f;
                if(LandmarkKind(key,out Vector3 lPos)>=0&&(p-lPos).sqrMagnitude<17*17)continue;
                bool near=false;foreach(var tree in data.Trees)if((tree-p).sqrMagnitude<minSpacingSq){near=true;break;}
                if(!near)data.Trees.Add(p);
            }
            while(cache.Count>=49){var oldest=cacheOrder.Dequeue();cache.Remove(oldest);}cache.Add(key,data);cacheOrder.Enqueue(key);return data;
        }
        public int CachedChunkCount=>cache.Count;
        public bool Blocked(Vector3 p,float radius)
        {
            if(Mathf.Abs(p.x)+radius>=HalfExtent||Mathf.Abs(p.z)+radius>=HalfExtent)return true;
            if(Mathf.Abs(p.x-RiverX(p.z))<RiverWidth(p.z)*.5f+radius&&!Bridge(p.z))return true;
            int cx=Mathf.FloorToInt(p.x/100),cz=Mathf.FloorToInt(p.z/100);
            for(int x=Mathf.FloorToInt((p.x-radius-2)/100);x<=Mathf.FloorToInt((p.x+radius+2)/100);x++)for(int z=Mathf.FloorToInt((p.z-radius-3)/100);z<=Mathf.FloorToInt((p.z+radius+3)/100);z++)
            {
                var chunk=GetChunk(x,z);foreach(var b in chunk.Buildings)if(p.x+radius>b.xMin&&p.x-radius<b.xMax&&p.z+radius>b.yMin&&p.z-radius<b.yMax)return true;
                foreach(var tree in chunk.Trees){float dx=p.x-tree.x,dz=p.z-tree.z;if(dx*dx+dz*dz<(radius+.32f)*(radius+.32f))return true;}
                foreach(var car in chunk.Cars)if(Mathf.Abs(p.x-car.x)<radius+1&&Mathf.Abs(p.z-car.z)<radius+2.5f)return true;
            }
            return false;
        }
        public string RegionName(Vector3 p,bool ja)
        {
            if(Landmarks!=null)
            {
                for(int i=0;i<Landmarks.Length;i++)
                    if((new Vector2(p.x,p.z)-new Vector2(Landmarks[i].x,Landmarks[i].z)).sqrMagnitude<55f*55f)
                        return LandmarkTitle(i,ja);
            }
            if(!River(p)&&!Park(p)){int district=District(p);if(district==0)return ja?"フォールン・ダウンタウン":"Fallen Downtown";if(district==1)return ja?"オールド・ショッピング通り":"Old Shopping Streets";if(district==2)return ja?"デレリクト・モール地区":"Derelict Mall District";}if(River(p))return ja?"グレイウォーター河岸":"Graywater Riverfront";if(Park(p))return ja?"レッドウッド森林公園":"Redwood Forest Park";if(p.z<-1800)return ja?"サウスワークス":"Southworks";if(p.x>1800)return ja?"オールド・シビック・クォーター":"Old Civic Quarter";return ja?"オールド・レジデンシャル地区":"Old Residential District";
        }
    }
}
