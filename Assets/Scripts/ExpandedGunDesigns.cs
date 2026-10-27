using UnityEngine;
namespace NeonVault {
 // Eight new mechanical series in each class. The four variants are tuned by GunSeriesDesigns.
 public static class ExpandedGunDesigns {
  static readonly string[] roots={"Vigil","Talon","Morrow","Drift","Rook","Nomad","Horizon","Specter"};
  static readonly string[] rootsJa={"不寝番","かぎ爪","暁","漂流","城鳥","遊牧","地平","幻影"};
  static readonly string[,] mechanisms={
   {"Split Receiver","Long Rail","Twin Port","Folding Stock","Heavy Gas","Free Float","Burst Cam","Siege Barrel"},
   {"Needle Slide","Twin Chamber","Pocket Drum","Long Slide","Counterweight","Open Bolt","Gas Seal","Magnum Frame"},
   {"Slug Choke","Rotary Tube","Duckbill","Lever Feed","Triple Barrel","Breach Lock","Compact Pump","Long Cylinder"},
   {"Twin Belt","Open Pan","Cooled Jacket","Compact Box","Anchor Tripod","High Cycle","Long Feed","Siege Rotor"},
   {"Side Feed","Needle Bolt","Fold Stock","Burst Wheel","Twin Stack","Suppressed Tube","Open Drum","Rapid Cam"},
   {"Short Action","Needle Rail","Long Chassis","Scout Bullpup","Twin Lug","Silent Bolt","Heavy Breech","Split Scope"}};
  static readonly string[,] mechanismsJa={
   {"分割機関部","長軌条","双排気口","折畳銃床","重ガス機構","浮動銃身","点射カム","攻城銃身"},
   {"細針スライド","双薬室","小型ドラム","長スライド","釣合錘","開放遊底","気密機構","大口径枠"},
   {"単弾絞り","回転管","扇状銃口","レバー給弾","三連銃身","破扉閉鎖","小型ポンプ","長円筒"},
   {"双弾帯","開放円盤","冷却外套","小型箱","固定三脚","高速循環","長弾帯","攻城回転機構"},
   {"側面給弾","細針遊底","折畳銃床","点射輪","二列弾倉","消音筒","開放ドラム","高速カム"},
   {"短機関","細針軌条","長シャーシ","斥候ブルパップ","双閉鎖突起","消音ボルト","重閉鎖器","分割照準器"}};
  static readonly string[] directions={"exposed split upper, offset magazine well and angled ejector","long top rail with forward cooling slots","paired muzzle ports and floating fore-end","hinged stock and shortened receiver","oversized piston housing and reinforced trunnion","vented free-float shroud and narrow profile","visible burst cam and indexed selector","thick siege barrel, heat jacket and broad shoulder brace"};
  // Columns: impact, shot interval, magazine, reload, aim. Every line has a clear strength and cost.
  static readonly float[,] tuning={
   {1.28f,1.24f,.82f,1.12f,4},{.85f,.78f,.78f,.84f,-7},{.92f,.72f,1.12f,1.22f,-11},
   {.82f,.88f,.70f,.78f,3},{1.55f,1.52f,.64f,1.35f,-8},{1.14f,1.16f,.80f,1.09f,9},
   {.74f,.66f,1.05f,1.18f,-5},{1.72f,1.68f,.55f,1.51f,-12}};
  static readonly string[] traits={"High impact and steady aim; slower cycle and smaller magazine.","Fast reload and cadence; light impact and short reach.","Rapid split discharge; severe spread and slow reload.","Compact handling and quick reload; low impact and capacity.","Heavy punch; slow cycling, weight and recovery.","Accurate long reach; deliberate fire and reduced capacity.","Very fast bursts; poor control and lower impact.","Extreme impact; tiny magazine and long recovery."};
  static readonly string[] traitsJa={"高威力・安定照準、遅い連射と少ない装弾数。","高速装填・連射、低威力で射程が短い。","分割機構の高速射撃、散りやすく装填は遅い。","小型で装填が速いが威力と弾数が少ない。","重い一撃、連射・取り回し・復帰が遅い。","遠距離高精度、慎重な射撃と少ない装弾数。","非常に速い点射、制御しづらく低威力。","最大級の一撃、極小弾倉と長い復帰時間。"};
  public static string SeriesName(int maker,int gunClass,int series,bool ja){string root=(ja?rootsJa:roots)[series];return root+" "+(ja?mechanismsJa:mechanisms)[gunClass,series];}
  public static string Description(int gunClass,int series,bool ja)=> (ja?traitsJa:traits)[series];
  public static string VisualDirection(int maker,int gunClass,int series)=>"Maker "+maker+", class "+gunClass+": "+directions[series]+"; "+mechanisms[gunClass,series]+" silhouette.";
  public static void Apply(Gun g,int gunClass,int series){
   g.damage*=tuning[series,0];g.interval*=tuning[series,1];g.magazine=Mathf.Max(1,Mathf.RoundToInt(g.magazine*tuning[series,2]));
   g.reloadSeconds*=tuning[series,3];g.accuracy+=tuning[series,4];
   if(series==3)g.movementBonus+=.035f;
   if(series==4||series==7)g.movementBonus-=.025f;
   if(series==5)g.speed*=1.2f;
  }
 }
}
