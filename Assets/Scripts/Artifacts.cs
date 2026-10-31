using UnityEngine;
namespace NeonVault {
 public enum ArtifactEffect { KillShield, DashPulse, ReloadSurge, ShieldConversion }
 [System.Serializable] public class ArtifactItem {public string id,name,japanese,catalogId,manufacturerId,bossRelicId;public int designVariant;public int level=1,dungeon,uniqueEffect;public Rarity rarity;public ArtifactEffect effect;public float power,cooldown;public GearAffix[] affixes;public string Name=>ArtifactCatalog.DisplayName(this);public string Description=>ArtifactCatalog.Description(this);}
 public static class ArtifactCatalog {
  public static ArtifactItem RollForDungeon(System.Random random,int dungeon,bool boss=false){return Create(random,dungeon,DungeonCatalog.RollRarity(random,boss),DungeonCatalog.ItemLevel(random,dungeon));}
  public static ArtifactItem Create(System.Random random,int dungeon,Rarity rarity,int level){
   if(random==null)throw new System.ArgumentNullException(nameof(random));
   int family=rarity==Rarity.Legendary?4+Mathf.Clamp(dungeon,0,2):random.Next(4);
   return CreateCatalog(ModuleProductCatalog.Roll(random,family).id,rarity,level,random,dungeon);
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
   int count=Mathf.Clamp((int)a.rarity,0,4);string[] types={"damage","health","fireRate","reload","accuracy","mitigation","moveSpeed","regen","ammo","critChance","critDamage"};
   var used=new System.Collections.Generic.HashSet<int>();a.affixes=new GearAffix[count];
   if(r==null)return;
   for(int i=0;i<count;i++)
   {
    int p;if(i==0&&!string.IsNullOrEmpty(a.manufacturerId)){var product=ModuleProductCatalog.Find(a.catalogId);p=product==null?r.Next(types.Length):System.Array.IndexOf(types,product.preferredAffix);if(p<0)p=r.Next(types.Length);used.Add(p);}else{do{p=r.Next(types.Length);}while(!used.Add(p));}
    float[] ranges={.025f,4,.025f,.025f,.02f,.01f,.02f,.15f,.05f,.015f,.06f};
    a.affixes[i]=new GearAffix{type=types[p],value=ranges[p]*a.power*Arsenal.Range(r,.85f,1.15f)};
   }
  }
  public static string DisplayName(ArtifactItem a)=>DisplayName(a,VaultLanguage.Japanese);
  public static string DisplayName(ArtifactItem a,bool japanese){string relic=BossRelicFoundation.Name(a?.bossRelicId,japanese);if(!string.IsNullOrEmpty(relic))return relic;var recipe=DistinctEquipmentDesigns.For(a);return recipe!=null?(japanese?recipe.japanese:recipe.name):LegacyDisplayName(a,japanese);}
  static string LegacyDisplayName(ArtifactItem a,bool japanese)=>!string.IsNullOrEmpty(a.catalogId)&&ModuleProductCatalog.Find(a.catalogId)!=null?(japanese?a.japanese:a.name):japanese?(a.rarity==Rarity.Legendary?new[]{"街道の防壁","炉心放電器","同位体中継器"}[Mathf.Clamp(a.dungeon,0,2)]:new[]{"シールド蓄電器","放電コイル","リロードリレー","変換回路"}[(int)a.effect]):(a.rarity==Rarity.Legendary?new[]{"HIGHWAY BULWARK","FURNACE DISCHARGER","ISOTOPE RELAY"}[Mathf.Clamp(a.dungeon,0,2)]:new[]{"SHIELD CAPACITOR","DISCHARGE COIL","RELOAD RELAY","CONVERSION CIRCUIT"}[(int)a.effect]);
  public static string Description(ArtifactItem a)
  {
   string detail=EffectDescription(a);
   if(a.effect==ArtifactEffect.KillShield||a.effect==ArtifactEffect.ShieldConversion)detail+=" / "+VaultLanguage.T("Active barrier reduces incoming health damage by 25% plus 0.5% per charge (up to 75% with bonuses); hits consume charge, and charge decays 2/s.","シールド中はHP被害を25%＋残量1点につき0.5%軽減（各種補正込み上限75%）。被弾で残量を消費し、毎秒2減衰。");
   if(LegendaryModuleBonus(a,out int healing,out int rounds,out float dodge))
   {
    var parts=new System.Collections.Generic.List<string>();
    if(healing>0)parts.Add(VaultLanguage.T("heal ","HP ")+healing+VaultLanguage.T(" HP","回復"));
    if(rounds>0)parts.Add(VaultLanguage.T("restore ","予備弾 ")+rounds+VaultLanguage.T(" reserve rounds","発回復"));
    if(dodge>0)parts.Add(VaultLanguage.T("shorten dodge cooldown by ","回避クールダウンを ")+dodge.ToString("0.00")+VaultLanguage.T("s","秒短縮"));
    string condition=a.effect==ArtifactEffect.ShieldConversion?VaultLanguage.T("while barrier is active (then every 5s): ","シールド有効中に発動（以後5秒ごと）："):VaultLanguage.T("when the module activates: ","モジュール発動時：");
    detail+=" / "+VaultLanguage.T("Legendary circuit ","レジェンダリー回路")+condition+string.Join(VaultLanguage.T(", ","、"),parts);
    string signature=LegendaryModuleSignature(a);
    if(!string.IsNullOrEmpty(signature))detail+=" / "+VaultLanguage.T("On the same activation: ","同じ発動時：")+signature;
   }
   string relic=BossRelicFoundation.Description(a?.bossRelicId,VaultLanguage.Japanese);
   return string.IsNullOrEmpty(relic)?detail:detail+" / "+relic;
  }
  public static bool LegendaryModuleBonus(ArtifactItem a,out int healing,out int rounds,out float dodge)
  {
   healing=rounds=0;dodge=0;
   var recipe=a==null?null:DistinctEquipmentDesigns.Find(a.catalogId);
   if(recipe==null||!recipe.legendary||recipe.family<4||recipe.family>6)
   {
    // Old saves predate distinct recipes but still contain legendary modules.
    if(a==null||(a.rarity!=Rarity.Legendary&&a.uniqueEffect==0))return false;
    int tier=Mathf.Clamp(a.dungeon,0,2);
    healing=2+tier;rounds=1;dodge=.5f+tier*.1f;
    return true;
   }
   // Housing and core determine the circuit's support mix. The recipe index
   // determines its exact output, so saved items retain a stable signature.
   int charge=1+recipe.index%4;
   if(recipe.maker==1||recipe.maker==3||recipe.maker==5)healing=charge+recipe.body;
   if(recipe.maker==0||recipe.maker==4||recipe.maker==5)rounds=charge+recipe.attachment/2;
   dodge=.01f*(recipe.index+1);
   return true;
  }
  public static int LegendaryModuleMode(ArtifactItem a)
  {
   var recipe=a==null?null:DistinctEquipmentDesigns.Find(a.catalogId);
   return recipe!=null&&recipe.legendary&&recipe.family>=4&&recipe.family<=6?(recipe.attachment+recipe.body*2)%10:-1;
  }
  public static string LegendaryModuleSignature(ArtifactItem a)
  {
   switch(LegendaryModuleMode(a))
   {
    case 0:return VaultLanguage.T("gain 3 barrier (cap 30)","シールドを3得る（上限30）");
    case 1:return VaultLanguage.T("strike up to 3 visible enemies within 2.6m for ","2.6m以内の見える敵最大3体に")+(6*a.power).ToString("0.0")+VaultLanguage.T(" damage","ダメージ");
    case 2:return VaultLanguage.T("shorten dodge cooldown by another 0.45s","回避クールダウンをさらに0.45秒短縮");
    case 3:return VaultLanguage.T("delay visible enemies within 3m for at least 0.6s","3m以内の見える敵の次の攻撃を最低0.6秒遅らせる");
    case 4:return VaultLanguage.T("grant at least 0.2s of invulnerability","最低0.2秒の無敵を得る");
    case 5:return VaultLanguage.T("push visible enemies within 3m back 1.2m","3m以内の見える敵を1.2m押し返す");
    case 6:return VaultLanguage.T("pull visible enemies within 3m toward you by 0.9m","3m以内の見える敵を自分へ0.9m引き寄せる");
    case 7:return VaultLanguage.T("pierce up to 3 visible enemies ahead in a 4m by 0.8m line for ","前方4mの直線上0.8m幅の見える敵最大3体へ")+(5*a.power).ToString("0.0")+VaultLanguage.T(" damage","ダメージ");
    case 8:return VaultLanguage.T("ready the next melee attack immediately","次の近接攻撃を即座に使用可能にする");
    case 9:return VaultLanguage.T("strike up to 2 visible enemies within 2.5m for ","2.5m以内の見える敵最大2体へ")+(4*a.power).ToString("0.0")+VaultLanguage.T(" damage and delay their next attack to at least 0.7s","ダメージを与え、次の攻撃を最低0.7秒遅らせる");
    default:return "";
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
