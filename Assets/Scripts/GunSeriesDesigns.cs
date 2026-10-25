using UnityEngine;
namespace NeonVault {
 // Catalog metadata has no dependency on DistinctEquipmentDesigns initialization.
 // Class/body: assault=0, handgun=1, shotgun=2, LMG=3, SMG=4, sniper=5.
 // Each maker has four complete series per class, each with four tuned variants.
 public static class GunSeriesDesigns {
  public const int ClassCount=6,SeriesCount=12,VariantsPerSeries=4,RegularCount=1728,FirstNewLegendary=1758;
  static readonly string[] classes={"Machine Gun","Handgun","Shotgun","Machine Gun","Machine Gun","Sniper Rifle"};
  static readonly string[] classesJa={"機関銃","拳銃","ショットガン","機関銃","機関銃","狙撃銃"};
  static readonly string[] makerNames={"Kestrel","Ironwake","Voltarc","Mireline","Cinder","Aegis"};
  static readonly string[] makerNamesJa={"ケストレル","アイアンウェイク","ヴォルトアーク","マイアライン","シンダー","イージス"};
  static readonly string[,] seriesNames={
   {"Patrol","Carbine","Battle","Marksman"},
   {"Compact","Service","Revolver","Hand Cannon"},
   {"Pump","Coach","Short Drum","Semi-Auto"},
   {"Belt Box","Pan","Drum","Heavy"},
   {"Receiver","Top Magazine","Helical","Drum"},
   {"Wood Bolt","Chassis","Bullpup","Anti-Materiel"}};
  static readonly string[,] seriesNamesJa={
   {"哨戒","騎兵","重戦","選抜射手"},
   {"小型","制式","回転弾倉","大口径"},
   {"ポンプ","水平二連","短胴ドラム","半自動"},
   {"弾帯箱","円盤弾倉","ドラム弾倉","重支援"},
   {"箱型機関","上部弾倉","螺旋弾倉","ドラム弾倉"},
   {"木銃床ボルト","シャーシ","ブルパップ","対物"}};
  // A maker-specific series root gives all 144 product lines their own identity.
  static readonly string[,] roots={
   {"Harrier","Swift","Osprey","Peregrine"},
   {"Anvil","Rivet","Foundry","Hammer"},
   {"Dynamo","Spark","Livewire","Thunder"},
   {"Briar","Fern","Willow","Hemlock"},
   {"Clinker","Ashfall","Furnace","Firebrand"},
   {"Convoy","Warden","Bastion","Custodian"}};
  static readonly string[,] rootsJa={
   {"鷹","燕","鶚","隼"},{"金床","鋲","鋳造炉","鉄槌"},
   {"発電機","火花","活線","雷鳴"},{"茨","羊歯","柳","毒芹"},
   {"焼滓","降灰","溶鉱炉","火種"},{"輸送隊","守衛","防塁","番人"}};
  static readonly string[] variants={"Field","Short","Reinforced","Range"};
  static readonly string[] variantsJa={"野戦型","短銃身型","強装型","遠射型"};
  public static string ClassName(int gunClass,bool japanese=false)=>(japanese?classesJa:classes)[Mathf.Clamp(gunClass,0,5)];
  public static string SeriesName(int maker,int gunClass,int series,bool japanese=false){maker=Mathf.Clamp(maker,0,5);gunClass=Mathf.Clamp(gunClass,0,5);series=Mathf.Clamp(series,0,11);if(series>=4)return ExpandedGunDesigns.SeriesName(maker,gunClass,series-4,japanese);string role=maker==3&&gunClass==4&&series==1?(japanese?"密閉箱型給弾":"Sealed Box Feed"):(japanese?seriesNamesJa:seriesNames)[gunClass,series];return (japanese?rootsJa:roots)[maker,series]+" "+role;}
  public static string VariantName(int variant,bool japanese=false)=>(japanese?variantsJa:variants)[Mathf.Clamp(variant,0,3)];
  public static int Model(int gunClass,int series){switch(gunClass){case 1:return new[]{17,15,14,16}[series%4];case 2:return 13;case 3:return 6;case 4:return 1;case 5:return series%4==3?8:2;default:return 12;}}
  public static void Configure(EquipmentRecipe r){
   if(r.legendary){int variant=LegendaryCatalog.VariantFromCatalogIndex(r.index);r.gunClass=new[]{5,1,2,3,4}[variant%5];r.body=r.gunClass;r.series=0;r.seriesVariant=0;r.visualDirection=LegendaryCatalog.VisualDirection(r.index);return;}
   int local=r.index<606?(r.index-30)/6:(r.index-606)/6;
   r.gunClass=local/(r.index<606?16:32);r.series=r.index<606?local/4%4:4+local/4%8;r.seriesVariant=local%4;
   r.body=r.gunClass;r.family=Model(r.gunClass,r.series);
   // These part keys remain useful for procedural detail, but never select a gun class.
   r.barrel=r.series;r.stock=r.seriesVariant;r.attachment=(r.series+r.maker)%6;
   r.name=makerNames[r.maker]+" "+SeriesName(r.maker,r.gunClass,r.series)+" "+ClassName(r.gunClass)+" / "+VariantName(r.seriesVariant);
   r.japanese=makerNamesJa[r.maker]+"・"+SeriesName(r.maker,r.gunClass,r.series,true)+" "+ClassName(r.gunClass,true)+"・"+VariantName(r.seriesVariant,true);
   if(r.series>=4)r.visualDirection=ExpandedGunDesigns.VisualDirection(r.maker,r.gunClass,r.series-4);
  }
  // Series tune damage / interval / accuracy / magazine / reload as a whole.
  // Every compact line sacrifices output or reach; every heavy line pays handling costs.
  static readonly float[,] damage={ {1,.84f,1.48f,1.24f},{.82f,1,1.18f,1.12f},{1,1.23f,.8f,.82f},{1,.94f,.88f,1.3f},{1,1.08f,.88f,.82f},{1,1.1f,.88f,1.3f} };
  static readonly float[,] interval={ {1,.82f,1.46f,1.22f},{.88f,1,1.3f,1.15f},{1,1.08f,.74f,.55f},{1,.92f,.83f,1.3f},{1,1.12f,.88f,.77f},{1,1.18f,.87f,1.4f} };
  static readonly float[,] capacity={ {1,.73f,.67f,.6f},{.67f,1,1,1},{1,.34f,1.67f,1.33f},{1.5f,.78f,1.17f,1.33f},{1,.83f,1.65f,1.85f},{1,1.33f,1.33f,.75f} };
  static readonly float[,] reload={ {1,.78f,1.23f,1.1f},{.82f,1,1.22f,1.18f},{1,.7f,1.3f,1.22f},{1.4f,.9f,1.2f,1.45f},{1,1.08f,1.38f,1.5f},{1,1.16f,.9f,1.3f} };
  static readonly float[,] aim={ {0,-8,-3,5},{-7,0,4,-4},{0,-12,-9,-5},{-3,3,-5,7},{0,4,-5,-8},{0,2,-6,-2} };
  public static void Apply(Gun g,EquipmentRecipe r){
   int c=r.gunClass,s=r.series,v=r.seriesVariant;
   if(s>=4)ExpandedGunDesigns.Apply(g,c,s-4);else {g.damage*=damage[c,s];g.interval*=interval[c,s];g.magazine=Mathf.Max(1,Mathf.RoundToInt(g.magazine*capacity[c,s]));g.reloadSeconds*=reload[c,s];g.accuracy+=aim[c,s];}


   switch(v){case 1:g.damage*=.91f;g.interval*=.9f;g.accuracy-=7;g.reloadSeconds*=.86f;g.magazine=Mathf.Max(1,Mathf.RoundToInt(g.magazine*.85f));g.movementBonus+=.025f;break;
    case 2:g.damage*=1.16f;g.interval*=1.14f;g.reloadSeconds*=1.17f;g.accuracy-=2;g.magazine=Mathf.Max(1,Mathf.RoundToInt(g.magazine*1.15f));g.movementBonus-=.018f;break;
    case 3:g.damage*=1.06f;g.interval*=1.1f;g.accuracy+=6;g.speed*=1.15f;g.reloadSeconds*=1.08f;g.magazine=Mathf.Max(1,Mathf.RoundToInt(g.magazine*.85f));break;}


   if(c==1&&s==2)g.magazine=6;
   if(c==2&&s==1)g.magazine=2;
   g.accuracy=Mathf.Clamp(g.accuracy,1,100);
  }
  static readonly string[,] descriptions={
   {"Balanced patrol rifle", "Short carbine: quicker fire and reload, lower impact and accuracy", "Battle rifle: high impact, slow fire and small magazine", "Marksman rifle: accurate deliberate fire, reduced capacity"},
   {"Compact slide: quick reload, lower impact and capacity", "Service automatic: balanced sidearm", "Six-shot revolver: strong deliberate fire, slow cylinder reload", "Hand cannon: heavy impact, slow fire and reload"},
   {"Tube-fed pump: balanced spread and cycling", "Two-barrel break action: heavy close burst, only two shells", "Short drum action: more shells and faster cycling, reduced impact and accuracy", "Semi-automatic: rapid follow-up spread, reduced impact and longer reload"},
   {"Belt box: deep reserve, slow belt replacement", "Pan feed: accurate lighter support, smaller capacity", "Drum feed: fast sustained fire, reduced impact and accuracy", "Heavy support: powerful stable fire, slow cadence and reload"},
   {"Box receiver: balanced close-range automatic", "Top magazine: controlled accurate fire, smaller capacity and slower cadence", "Helical feed: large reserve and quick fire, reduced impact and slower reload", "Drum feed: maximum reserve and cadence, reduced accuracy and slow reload"},
   {"Wood-stock bolt action: balanced precision", "Chassis action: stable deliberate fire, larger magazine and slower reload", "Bullpup action: compact handling and larger magazine, reduced precision and impact", "Heavy anti-materiel: high impact, slow fire and reload"}};
  static readonly string[,] descriptionsJa={
   {"哨戒機構：均衡の取れた制式銃", "短カービン：連射・装填が速いが威力・精度が低い", "重戦機構：高威力だが連射が遅く弾数が少ない", "選抜射手機構：高精度の慎重な射撃、弾数は少ない"},
   {"小型スライド：高速装填、威力・弾数は少ない", "制式自動拳銃：均衡の取れた副武装", "六連回転弾倉：強い単発、連射・装填は遅い", "大口径機構：高威力だが連射・装填は遅い"},
   {"管状弾倉ポンプ：均衡の取れた散弾射撃", "水平二連折開機構：強い近距離斉射、装弾数は二発", "短胴ドラム：弾数・連射と引き換えに威力・精度が低い", "半自動機構：速い散弾連射、威力が低く装填は長い"},
   {"弾帯箱：大容量だが弾帯交換は遅い", "円盤給弾：正確な支援射撃、装弾数は少ない", "ドラム給弾：高速持続射撃、威力・精度は低い", "重支援機構：強い安定射撃、連射・装填は遅い"},
   {"箱型機関：均衡の取れた近距離連射", "上部弾倉：正確な射撃、弾数・連射速度は控えめ", "螺旋弾倉：大容量・高速連射、威力が低く装填は長い", "ドラム給弾：最大容量と高速連射、精度が低く装填は長い"},
   {"木銃床ボルト：均衡の取れた精密射撃", "シャーシ機構：安定射撃と増弾、連射・装填は遅い", "ブルパップ機構：小型・増弾、威力・精度は控えめ", "重対物機構：高威力だが連射・装填は遅い"}};
  public static string Description(EquipmentRecipe r,bool japanese=false){
   string mechanism=r.series>=4?ExpandedGunDesigns.Description(r.gunClass,r.series-4,japanese):r.maker==3&&r.gunClass==4&&r.series==1?(japanese?"密閉箱型給弾：正確な射撃、弾数・連射速度は控えめ":"Sealed box feed: controlled accurate fire, smaller capacity and slower cadence"):(japanese?descriptionsJa:descriptions)[r.gunClass,r.series];return ClassName(r.gunClass,japanese)+" / "+SeriesName(r.maker,r.gunClass,r.series,japanese)+" / "+mechanism+" / "+VariantName(r.seriesVariant,japanese);
  }
 }
}
