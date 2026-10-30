using UnityEngine;

namespace NeonVault
{
    // Shared identity and presentation. Infer methods deliberately never modify rolled stats.
    public static class ManufacturerCatalog
    {
        public const int Count=6;
        static readonly string[] ids={"kestrel","ironwake","voltarc","mireline","cinder","aegis"};
        static readonly string[] english={"Kestrel","Ironwake","Voltarc","Mireline","Cinder","Aegis"};
        static readonly string[] japanese={"ケストレル","アイアンウェイク","ヴォルトアーク","マイアライン","シンダー","イージス"};
        static readonly string[] descriptions={"Precise action, quick reload; reduced damage.","Heavy impact; slower fire and handling.","Rapid fire, large magazines; reduced impact and accuracy.","Medical recovery systems restore health; reduced direct damage.","Reclaimed scatter systems; broad spread and longer reload.","Reliable protection; lower output and faster reload."};
        static readonly string[] descriptionsJa={"高精度・高速リロード。単発威力は低い。","高威力の重機関。射撃・リロードは遅い。","高速連射・大容量。単発威力と精度は低い。","医療・防疫機構で体力を回復。直接威力は低い。","再生散弾機構。拡散が広くリロードが遅い。","安定した防護と高速リロード。攻撃出力は低い。"};
        public static string Id(int index)=>ids[(index%Count+Count)%Count];
        public static int Index(string id){for(int i=0;i<Count;i++)if(ids[i]==id)return i;return -1;}
        public static string Name(string id,bool japanese=false){int i=Index(id);return i<0?(japanese?"不明メーカー":"Unknown manufacturer"):(japanese?ManufacturerCatalog.japanese:english)[i];}
        public static string Description(string id,bool japanese=false){int i=Index(id);return i<0?"":(japanese?descriptionsJa:descriptions)[i];}
        public static string CompanyOverview(string id,int section,bool ja)
        {
            string[,] jp={
                {"精密機器を原点とする銃器メーカー。現場での扱いやすさと確かな照準を重視し、警備員から荒野の運び屋まで幅広く支持されている。","災害前は測定器と射撃用照準器を製造。工場を失った後も、技師たちは修理工房をつなぎ、部品を共通化した生産体制を築いた。","照準の合わせやすさと素早いリロードが持ち味。軽快な操作性を優先するため、単発の威力は控えめ。"},
                {"重機と産業設備の技術を武器や防具に転用するメーカー。過酷な現場で使い続けられる頑丈さを追求している。","かつては都市の建設現場に機械を供給。崩壊後は残された工場を修復し、重い鋼材から戦闘用の装備を作り始めた。","厚い鋼板と力強い機構による高威力が特徴。重さに見合う打撃力を得る一方、連射やリロードは遅め。"},
                {"電力制御と高速駆動を得意とするメーカー。失われたインフラの部品を生かし、強い出力を生む装備を開発している。","送電設備の保守会社として出発。大停電後は技術者たちが蓄電器と制御回路を回収し、独自の武器工房を開いた。","高速連射と大きな弾倉が特徴。短時間で多くの弾を放てるが、単発の威力と精度には妥協がある。"},
                {"医療と防疫の技術を持つメーカー。傷を負っても現場で生き延びられるよう、回復と継戦を支える製品を作る。","災害前は病院や避難施設に医療機器を供給。崩壊後も各地の診療所と協力し、残された薬品と器具を再利用してきた。","回復や体力の維持を助ける機構が持ち味。直接の攻撃力を抑え、長く戦い続けることを重視する。"},
                {"廃材を使った野戦装備を作る再生メーカー。使い捨てられた部品に手を加え、荒野で修理できる製品として送り出している。","小さな回収工房の集まりから発展。街の工場跡を巡り、古い銃や機械を持ち帰っては新たな用途を見つけてきた。","広い範囲へ弾をばらまく散弾機構が特徴。近距離で力を発揮する一方、拡散が大きくリロードに時間がかかる。"},
                {"警備と物資輸送を支える防護装備メーカー。危険な地域を移動する人々が、装備を確実に使えることを大切にしている。","護送車両と警備用品の供給から事業を開始。崩壊後は輸送隊の護衛と協力し、防具や携行装備の修理網を維持した。","防護と安定した取り回しを重視し、リロードも速い。突出した攻撃力より、生存と安定した運用に向いている。"}};
            string[,] en={
                {"A firearms maker rooted in precision instruments. Kestrel designs for dependable sights and handling, serving guards and wasteland couriers alike.","Once a maker of measuring equipment and optics, its engineers linked surviving repair shops after the collapse and rebuilt production around shared parts.","Accurate sights and quick reloads make its weapons easy to handle; individual shots trade some impact for that speed."},
                {"Ironwake adapts heavy machinery and industrial engineering to weapons and armor built for punishing conditions.","The company supplied construction machinery before the collapse. Its crews restored surviving workshops and turned heavy steel into combat equipment.","Thick plate and powerful mechanisms deliver heavy impact, at the cost of slower firing and reloading."},
                {"Voltarc specializes in power control and high speed mechanisms, using recovered infrastructure parts to build high output equipment.","Originally an electrical maintenance firm, its engineers salvaged capacitors and control circuits after the grid failed and opened their own workshops.","Rapid fire and large magazines put many rounds downrange, with lower precision and less damage per shot."},
                {"Mireline builds on medical and quarantine technology, making equipment that helps injured crews stay alive in the field.","A supplier to hospitals and shelters before the disaster, it now works with scattered clinics to recover and reuse medicines and instruments.","Recovery and endurance are central to its designs; direct damage is secondary to staying in the fight."},
                {"Cinder turns discarded machinery into field equipment that can be repaired with the materials at hand.","A network of small salvage shops grew into a manufacturer as its crews searched old factories and found new uses for abandoned guns and machines.","Wide scatter mechanisms excel at close range, with a broad spread and slower reloads."},
                {"Aegis supplies protective equipment for security crews and transport teams crossing dangerous territory.","Beginning as a supplier of escort vehicles and security gear, it maintained a repair network with the convoys that survived the collapse.","Reliable protection and quick reloads support steady operation, favoring survival over maximum attack output."}};
            int i=Index(id);return i<0||section<0||section>2?"":(ja?jp:en)[i,section];
        }
        public static Color Color(string id){switch(Index(id)){case 0:return new Color(.35f,.8f,.92f);case 1:return new Color(.66f,.35f,.2f);case 2:return new Color(.9f,.8f,.2f);case 3:return new Color(.4f,.8f,.4f);case 4:return new Color(.96f,.4f,.16f);default:return new Color(.4f,.55f,.9f);}}
        public static string ForDesign(int model,int design)=>Id(model+design);
        public static void InferGunIdentity(Gun gun)
        {
            if(gun==null)return;
            // Resolve the legacy scatter presentation before adding its new identity.
            if(string.IsNullOrEmpty(gun.catalogId)&&gun.effect==GunEffect.Scatter&&gun.modelVariant==0)gun.modelVariant=3;
            if(DistinctEquipmentDesigns.Find(gun.catalogId)==null)gun.designVariant=Mathf.Clamp(gun.designVariant,0,Arsenal.DesignsPerModel-1);
            if(Index(gun.manufacturerId)<0)gun.manufacturerId=ForDesign(gun.modelVariant,gun.designVariant);
            if(string.IsNullOrEmpty(gun.catalogId))gun.catalogId="gun-"+Mathf.Clamp(gun.modelVariant,0,Arsenal.ModelCount-1).ToString("00")+"-"+gun.designVariant.ToString("00");
        }
        // Creation only: callers must not invoke this while restoring saves.
        internal static void Apply(Gun gun)
        {
            switch(Index(gun.manufacturerId))
            {
                case 0:gun.accuracy=Mathf.Min(100,gun.accuracy+7);gun.reloadSeconds*=.84f;gun.damage*=.94f;break;
                case 1:gun.damage*=1.22f;gun.interval*=1.2f;gun.reloadSeconds*=1.15f;gun.movementBonus-=.025f;break;
                case 2:gun.interval*=.8f;gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*1.3f));gun.damage*=.86f;gun.accuracy=Mathf.Max(1,gun.accuracy-5);break;
                case 3:gun.bonusHealth+=6;gun.regenBonus+=.25f;gun.damage*=.92f;break;
                case 4:gun.pellets++;gun.damage*=.72f;gun.accuracy=Mathf.Max(1,gun.accuracy-9);gun.reloadSeconds*=1.18f;if(gun.effect==GunEffect.Normal)gun.effect=GunEffect.Scatter;break;
                case 5:gun.damageReductionBonus+=.025f;gun.reloadSeconds*=.9f;gun.damage*=.93f;break;
            }
        }
    }
}
