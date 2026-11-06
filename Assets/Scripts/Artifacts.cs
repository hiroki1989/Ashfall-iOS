using UnityEngine;
namespace NeonVault {
 public enum ArtifactEffect { KillShield, DashPulse, ReloadSurge, ShieldConversion }
 [System.Serializable] public class ArtifactItem {public string id,name,japanese,catalogId,manufacturerId,bossRelicId;public int designVariant;public int level=1,dungeon,uniqueEffect;public Rarity rarity;public ArtifactEffect effect;public float power,cooldown;public GearAffix[] affixes;public string Name=>ArtifactCatalog.DisplayName(this);public string Description=>ArtifactCatalog.Description(this);}
 public static class ArtifactCatalog {
  public static ArtifactItem RollForDungeon(System.Random random,int dungeon,bool boss=false){return Create(random,dungeon,DungeonCatalog.RollRarity(random,boss),DungeonCatalog.ItemLevel(random,dungeon));}
  public static ArtifactItem Create(System.Random random,int dungeon,Rarity rarity,int level){
   if(random==null)throw new System.ArgumentNullException(nameof(random));
   int family=rarity==Rarity.Legendary?4+Mathf.Clamp(dungeon,0,2):random.Next(4);
   var product=rarity==Rarity.Legendary?DungeonBossExclusive.RollNonExclusiveLegendaryModule(random,family):ModuleProductCatalog.Roll(random,family);return CreateCatalog(product.id,rarity,level,random,dungeon);
  }
  public static ArtifactItem CreateCatalog(string catalogId,Rarity rarity,int level,System.Random random=null,int dungeon=0){
   var product=ModuleProductCatalog.Find(catalogId);
   if(product==null)throw new System.ArgumentException("Unknown module catalog ID",nameof(catalogId));
   if(product.legendary)rarity=Rarity.Legendary;
   else if(rarity==Rarity.Legendary)rarity=Rarity.Epic;
   float baseCooldown=product.effect==ArtifactEffect.KillShield?3:product.effect==ArtifactEffect.DashPulse?5:product.effect==ArtifactEffect.ReloadSurge?7:0;
   var a=new ArtifactItem{id=System.Guid.NewGuid().ToString("N"),catalogId=product.id,manufacturerId=product.manufacturerId,designVariant=product.designVariant,dungeon=product.legendary?product.family-4:dungeon,rarity=rarity,level=Mathf.Max(1,level),effect=product.effect,name=product.name,japanese=product.japanese,power=GearBalance.LevelPower(rarity,level)*product.powerScale,cooldown=baseCooldown*product.cooldownScale,uniqueEffect=product.legendary?(int)product.effect+1:0};
   RollAffixes(a,random??new System.Random(31+product.family*10+product.designVariant));return a;
  }
  static void RollAffixes(ArtifactItem a,System.Random r)
  {
   int count=Mathf.Clamp((int)a.rarity,0,4);string[] types={"damage","health","fireRate","reload","accuracy","mitigation","moveSpeed","regen","ammo","critChance","critDamage","elementDamage","handgunDamage","shotgunDamage","rifleDamage","sniperDamage","heavyDamage","petDamage","petSpeed"};
   var used=new System.Collections.Generic.HashSet<int>();a.affixes=new GearAffix[count];
   if(r==null)return;
   for(int i=0;i<count;i++)
   {
    int p;if(i==0&&!string.IsNullOrEmpty(a.manufacturerId)){var product=ModuleProductCatalog.Find(a.catalogId);p=product==null?r.Next(types.Length):System.Array.IndexOf(types,product.preferredAffix);if(p<0)p=r.Next(types.Length);used.Add(p);}else{do{p=r.Next(types.Length);}while(!used.Add(p));}
    float[] ranges={.025f,4,.025f,.025f,.02f,.01f,.02f,.15f,.05f,.015f,.06f,.04f,.04f,.04f,.04f,.04f,.04f,.08f,.06f};
    a.affixes[i]=new GearAffix{type=types[p],value=ranges[p]*(p>=17?Mathf.Clamp(a.power,1f,3f):a.power)*Arsenal.Range(r,.85f,1.15f)};
   }
  }
  public static string DisplayName(ArtifactItem a)=>DisplayName(a,VaultLanguage.Japanese);
  public static string DisplayName(ArtifactItem a,bool japanese){string relic=BossRelicFoundation.Name(a?.bossRelicId,japanese);if(!string.IsNullOrEmpty(relic))return relic;var recipe=DistinctEquipmentDesigns.For(a);return recipe!=null?(japanese?recipe.japanese:recipe.name):LegacyDisplayName(a,japanese);}
  static string LegacyDisplayName(ArtifactItem a,bool japanese)=>!string.IsNullOrEmpty(a.catalogId)&&ModuleProductCatalog.Find(a.catalogId)!=null?(japanese?a.japanese:a.name):japanese?(a.rarity==Rarity.Legendary?new[]{"街道の防壁","炉心放電器","同位体中継器"}[Mathf.Clamp(a.dungeon,0,2)]:new[]{"シールド蓄電器","放電コイル","リロードリレー","変換回路"}[(int)a.effect]):(a.rarity==Rarity.Legendary?new[]{"HIGHWAY BULWARK","FURNACE DISCHARGER","ISOTOPE RELAY"}[Mathf.Clamp(a.dungeon,0,2)]:new[]{"SHIELD CAPACITOR","DISCHARGE COIL","RELOAD RELAY","CONVERSION CIRCUIT"}[(int)a.effect]);
  public static string Description(ArtifactItem a)
  {
   string detail=EffectDescription(a);
   if(a.effect==ArtifactEffect.KillShield||a.effect==ArtifactEffect.ShieldConversion)detail+=" / "+VaultLanguage.T("Active barrier reduces incoming health damage by 25% plus 0.5% per charge (up to 75% with bonuses); hits consume charge, and charge decays 2/s.","シールド中はHP被害を25%＋残量1点につき0.5%軽減（各種補正込み上限75%）。被弾で残量を消費し、毎秒2減衰。");
   if(LegendaryModuleBonus(a,out _,out _,out _))
   {
    string condition=LegendaryModuleMode(a)==17?"":a.effect==ArtifactEffect.ShieldConversion?VaultLanguage.T("while shield is active (then every 5s): ","シールド有効中に発動（以後5秒ごと）："):VaultLanguage.T("when the module activates: ","モジュール発動時：");
    string signature=LegendaryModuleSignature(a);
    if(!string.IsNullOrEmpty(signature))detail+=" / "+VaultLanguage.T("Legendary ","レジェンダリー効果 ")+condition+signature;
   }
   string relic=BossRelicFoundation.Description(a?.bossRelicId,VaultLanguage.Japanese);
   return string.IsNullOrEmpty(relic)?detail:detail+" / "+relic;
  }
  public static bool LegendaryModuleBonus(ArtifactItem a,out int healing,out int rounds,out float dodge)
  {
   // Legendary identity is already saved with the item; no random support bundle is added.
   healing=rounds=0;dodge=0;
   if(a==null||BossRelicFoundation.Decode(a.bossRelicId,out _,out _))return false;
   var recipe=DistinctEquipmentDesigns.Find(a.catalogId);
   if(recipe!=null&&recipe.legendary&&recipe.family>=4&&recipe.family<=6)return true;
   return a.rarity==Rarity.Legendary||a.uniqueEffect!=0;
  }

  public static int LegendaryModuleMode(ArtifactItem a)
  {
   if(a==null||BossRelicFoundation.Decode(a.bossRelicId,out _,out _))return -1;
   var recipe=DistinctEquipmentDesigns.Find(a.catalogId);
   if(recipe!=null&&recipe.legendary&&recipe.family>=4&&recipe.family<=6){int rank=0;for(int i=0;i<recipe.index;i++)if((i/6)%7>=4)rank++;return rank;}
   return a.rarity==Rarity.Legendary||a.uniqueEffect!=0?Mathf.Clamp(a.dungeon,0,2):-1;
  }
  public static string LegendaryModuleSignature(ArtifactItem a)
  {
   switch(LegendaryModuleMode(a))
   {
    case 0:return VaultLanguage.T("gain 20 shield (cap 30)","シールドを20得る（上限30）");
    case 1:return VaultLanguage.T("strike up to 3 visible enemies within 2.6m for ","2.6m以内の見える敵最大3体に")+(12*a.power).ToString("0.0")+VaultLanguage.T(" damage","ダメージ");
    case 2:return VaultLanguage.T("fire five rounds in a forward fan","前方に弾を5発扇状に放つ");
    case 3:return VaultLanguage.T("blast up to 5 visible enemies within 3m for ","3m以内の見える敵最大5体に")+(12*a.power).ToString("0.0")+VaultLanguage.T(" damage","の爆発ダメージ");
    case 4:return VaultLanguage.T("grant at least 1.25s of invulnerability","最低1.25秒の無敵を得る");
    case 5:return VaultLanguage.T("create three explosive pulses within 3m","周囲3mに爆発を3回起こす");
    case 6:return VaultLanguage.T("create three spore pulses within 3m","周囲3mに胞子の波動を3回起こす");
    case 7:return VaultLanguage.T("pierce up to 3 visible enemies ahead in a 4m by 0.8m line for ","前方4mの直線上0.8m幅の見える敵最大3体へ")+(5*a.power).ToString("0.0")+VaultLanguage.T(" damage","ダメージ");
    case 8:return VaultLanguage.T("fire eight rounds in all directions","全方向へ弾を8発放つ");
    case 9:return VaultLanguage.T("strike up to 2 visible enemies within 2.5m for ","2.5m以内の見える敵最大2体へ")+(10*a.power).ToString("0.0")+VaultLanguage.T(" damage with electrical arcs","の電撃ダメージを与える");
    case 10:return VaultLanguage.T("launch guided rounds at up to three nearby enemies","近くの敵最大3体へ誘導弾を放つ");
    case 11:return VaultLanguage.T("fire a gravity-well round forward","前方に重力渦を作る弾を放つ");
    case 12:return VaultLanguage.T("detonate a large blast within 4m","周囲4mに大爆発を起こす");
    case 13:return VaultLanguage.T("restore 8% of maximum health","最大HPの8%を回復する");
    case 14:return VaultLanguage.T("below half health, restore 15% of maximum health","HP半分未満なら最大HPの15%を回復する");
    case 15:return VaultLanguage.T("restore 10% of the active weapon reserve capacity","現在の銃の予備弾を所持上限の10%補充する");
    case 16:return VaultLanguage.T("launch six rounds outward in a circle","周囲6方向に弾を放つ");
    case 17:return VaultLanguage.T("while equipped, companion bite damage +75% and attack speed +25%","装備中、犬のかみつき威力 +75%、攻撃速度 +25%");
    case 18:return VaultLanguage.T("place a three-pulse blast at the nearest enemy","最も近い敵の位置で3回爆発を起こす");
    case 19:return VaultLanguage.T("release three explosions in a line ahead","前方に3回の爆発を連続して起こす");
    case 20:return VaultLanguage.T("fire a round that divides into three while flying","飛行中に3発に分かれる弾を放つ");
    case 21:return VaultLanguage.T("fire a round that splits sideways while flying","飛行中に左右へ分裂する弾を放つ");
    case 22:return VaultLanguage.T("fire a round that curves into a circle","円を描いて飛ぶ弾を放つ");
    case 23:return VaultLanguage.T("fire a round that bursts into a five-way radial volley","飛行中に5方向へ分裂する弾を放つ");
    case 24:return VaultLanguage.T("fire a round that bursts into a five-shot fan","飛行中に扇状5発に分裂する弾を放つ");
    case 25:return VaultLanguage.T("fire a round that weaves from side to side","左右に蛇行する弾を放つ");
    case 26:return VaultLanguage.T("fire an accelerating round that sheds rounds sideways","加速しながら左右へ弾を放つ");
    case 27:return VaultLanguage.T("fire a round that sheds rounds in successive directions","飛行中に周囲へ弾を放つ");
    case 28:return VaultLanguage.T("fire a spiraling round","らせんを描く弾を放つ");
    case 29:return VaultLanguage.T("fire an electric round that chains to enemies","敵へ連鎖する電撃弾を放つ");
    case 30:return VaultLanguage.T("fire a rotating round that releases rounds behind it","回転しながら後方へ弾を放つ");
    case 31:return VaultLanguage.T("fire a flame round","炎属性の弾を放つ");
    case 32:return VaultLanguage.T("fire a toxic round","毒属性の弾を放つ");
    case 33:return VaultLanguage.T("fire an explosive round","爆発属性の弾を放つ");
    case 34:return VaultLanguage.T("fire a slowing electric round that chains to five enemies","減速しながら最大5体に連鎖する電撃弾を放つ");
    default:
     int mode=LegendaryModuleMode(a);
     if(mode>=35&&mode<55){string[] en={"reverse and return","curve sharply left","curve sharply right","orbit before continuing","weave in a wider arc","accelerate over its flight","decelerate over its flight","release a blast along its path","release a fiery blast along its path","release a spore blast along its path","burst forward after a delay","release rounds while orbiting","release rounds to both sides","release rounds behind itself","curve toward the nearest enemy","sweep its direction back and forth","form a gravity well at the end of its flight","drop an explosive charge at the end of its flight","release guided rounds at the end of its flight","release a radial volley at the end of its flight"};string[] ja={"反転して戻る","左へ大きく曲がる","右へ大きく曲がる","円を描いてから進む","大きく蛇行する","飛行中に加速する","飛行中に減速する","飛行中に爆発を起こす","飛行中に炎の爆発を起こす","飛行中に胞子の爆発を起こす","一定時間後に加速する","周回しながら弾を放つ","飛行中に左右へ弾を放つ","飛行中に後方へ弾を放つ","近くの敵を追う","左右に大きく方向を振る","飛行の終点に重力渦を作る","飛行の終点に爆発する罠を残す","飛行の終点から誘導弾を放つ","飛行の終点から全方向に弾を放つ"};return VaultLanguage.T("fire a round that will ","弾を放ち、")+VaultLanguage.T(en[mode-35],ja[mode-35]);}
     switch(mode)
     {
      case 55:return VaultLanguage.T("restore 20% of the action skill gauge","アクションスキルのゲージを20%回復する");
      case 56:return VaultLanguage.T("move up to a quarter of the magazine from reserves into the current magazine","予備弾からマガジン容量の4分の1を装填する");
      case 57:return VaultLanguage.T("finish the current reload instantly","リロード中なら即座に完了する");
      case 58:return VaultLanguage.T("extend an active action device by four seconds","発動中の装置の持続時間を4秒延長する");
      case 59:return VaultLanguage.T("intercept up to 12 nearby hostile projectiles","近くの敵弾を最大12発迎撃する");
      case 60:return VaultLanguage.T("strike every visible enemy in a narrow forward lane","前方の細い直線上にいる敵全員を攻撃する");
      case 61:return VaultLanguage.T("blast the most distant visible enemy","視界内の最も遠い敵の位置を爆破する");
      case 62:return VaultLanguage.T("fire a bouncing round that ricochets among enemies","敵の間を跳ねる弾を放つ");
      case 63:return VaultLanguage.T("restore 5% of each ammunition reserve capacity","全銃種の予備弾をそれぞれ所持上限の5%補充する");
      case 64:return VaultLanguage.T("recover health by spending the active shield","シールドを消費してHPを回復する");
      case 65:return VaultLanguage.T("freeze the nearest enemy for two seconds","最寄りの敵を2秒間凍結する");
      case 66:return VaultLanguage.T("execute the weakest visible non-elite enemy below 25% health","視界内でHP25%未満の最も弱い通常敵を処刑する");
      case 67:return VaultLanguage.T("for six seconds, each kill restores 4% maximum health","6秒間、敵を倒すたび最大HPの4%を回復する");
      case 68:return VaultLanguage.T("deploy a six-second healing field","6秒間の回復エリアを設置する");
      case 69:return VaultLanguage.T("deploy a six-second ammunition supply field","6秒間の弾薬補給エリアを設置する");
      case 70:return VaultLanguage.T("deploy a six-second damage field","6秒間の攻撃エリアを設置する");
      case 71:return VaultLanguage.T("deploy a six-second electrical field","6秒間の電撃エリアを設置する");
      case 72:return VaultLanguage.T("deploy a six-second projectile interception field","6秒間の敵弾迎撃エリアを設置する");
      case 73:return VaultLanguage.T("deploy an autonomous sentry for six seconds","6秒間、自動射撃する迎撃機を設置する");
      case 74:return VaultLanguage.T("deploy a six-second freezing field","6秒間、敵を凍結させるエリアを設置する");
      case 75:return VaultLanguage.T("place a proximity mine that detonates when enemies approach","敵が近づくと爆発する地雷を設置する");
      case 76:return VaultLanguage.T("deploy a rotating projectile emitter for six seconds","6秒間、回転しながら弾を放つ装置を設置する");
      case 77:return VaultLanguage.T("deploy a six-second action gauge charging field","6秒間、アクションゲージを回復するエリアを設置する");
      case 78:return VaultLanguage.T("deploy a six-second field that extends the active action device","6秒間、発動中の装置を延長するエリアを設置する");
      case 79:return VaultLanguage.T("for six seconds, heal 25% of maximum HP when health falls below 25%","6秒間、HPが25%未満になったら最大HPの25%を回復する");
      default:return "";
     }
   }
  }
  static string ModuleTendency(ArtifactItem a){
   string[] en={"Precision affixes; modest output, shorter cooldown.","Heavy reserve affixes; stronger bursts, longer cooldown.","Fire-rate affixes; rapid cooldown, weaker bursts.","Regeneration affixes; lower direct output.","Damage affixes; risky long cooldown or weaker conversion.","Protection affixes; consistent output and cooldown."};
   string[] ja={"精度Affix傾向。出力控えめ・クールダウン短縮。","体力Affix傾向。強い瞬発力・長いクールダウン。","連射Affix傾向。クールダウン短縮・瞬発力低下。","再生Affix傾向。直接出力は控えめ。","威力Affix傾向。長いクールダウン、または変換出力低下。","防護Affix傾向。安定した出力とクールダウン。"};
   int i=ManufacturerCatalog.Index(a.manufacturerId);return i<0?"":(VaultLanguage.Japanese?ja:en)[i];
  }
  static string EffectDescription(ArtifactItem a){switch(a.effect){case ArtifactEffect.KillShield:return VaultLanguage.T("On enemy kill: gain ","敵撃破時：シールド +")+(8*a.power).ToString("0.0")+VaultLanguage.T(" shield (cap 30), ","（上限30）、")+a.cooldown.ToString("0.00")+VaultLanguage.T("s cooldown. Shield decays 2/s.","秒クールダウン。毎秒2減衰。");case ArtifactEffect.DashPulse:return VaultLanguage.T("On dash: pulse ","ダッシュ時：周囲波動 ")+(12*a.power).ToString("0.0")+VaultLanguage.T(" damage in 3.5m, ","ダメージ、半径3.5m、")+a.cooldown.ToString("0.00")+VaultLanguage.T("s cooldown.","秒クールダウン。");case ArtifactEffect.ReloadSurge:return VaultLanguage.T("On reload: +","リロード完了時：威力 +")+(15*a.power).ToString("0")+VaultLanguage.T("% damage for 3s; ","%を3秒、クールダウン")+a.cooldown.ToString("0.00")+VaultLanguage.T("s cooldown.","秒。");default:return VaultLanguage.T("While barrier is active: +","シールドがある間：威力 +")+(12*a.power).ToString("0")+VaultLanguage.T("% damage while a barrier is active.","%。シールドが有効な間のみ発動。");}}
 }
}
