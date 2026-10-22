using UnityEngine;
namespace NeonVault {
 public enum ArtifactEffect { KillShield, DashPulse, ReloadSurge, ShieldConversion }
 [System.Serializable] public class ArtifactItem {public string id,name,japanese,catalogId,manufacturerId;public int designVariant;public int level=1,dungeon,uniqueEffect;public Rarity rarity;public ArtifactEffect effect;public float power,cooldown;public GearAffix[] affixes;public string Name=>ArtifactCatalog.DisplayName(this);public string Description=>ArtifactCatalog.Description(this);}
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
   var a=new ArtifactItem{id=System.Guid.NewGuid().ToString("N"),catalogId=product.id,manufacturerId=product.manufacturerId,designVariant=product.designVariant,dungeon=product.legendary?product.family-4:dungeon,rarity=rarity,level=Mathf.Max(1,level),effect=product.effect,name=product.name,japanese=product.japanese,power=(1+.16f*((int)rarity+Mathf.Max(1,level)-1))*product.powerScale,cooldown=baseCooldown*product.cooldownScale,uniqueEffect=product.legendary?(int)product.effect+1:0};
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
  public static string DisplayName(ArtifactItem a,bool japanese){var recipe=DistinctEquipmentDesigns.For(a);return recipe!=null?(japanese?recipe.japanese:recipe.name):LegacyDisplayName(a,japanese);}
  static string LegacyDisplayName(ArtifactItem a,bool japanese)=>!string.IsNullOrEmpty(a.catalogId)&&ModuleProductCatalog.Find(a.catalogId)!=null?(japanese?a.japanese:a.name):japanese?(a.rarity==Rarity.Legendary?new[]{"街道の防壁","炉心放電器","同位体中継器"}[Mathf.Clamp(a.dungeon,0,2)]:new[]{"障壁蓄電器","放電コイル","リロードリレー","変換回路"}[(int)a.effect]):(a.rarity==Rarity.Legendary?new[]{"HIGHWAY BULWARK","FURNACE DISCHARGER","ISOTOPE RELAY"}[Mathf.Clamp(a.dungeon,0,2)]:new[]{"SHIELD CAPACITOR","DISCHARGE COIL","RELOAD RELAY","CONVERSION CIRCUIT"}[(int)a.effect]);
  public static string Description(ArtifactItem a)
  {
   string detail=EffectDescription(a);
   if(a.effect==ArtifactEffect.KillShield||a.effect==ArtifactEffect.ShieldConversion)detail+=" / "+VaultLanguage.T("Active barrier reduces incoming health damage by 25% plus 0.5% per charge (up to 75% with bonuses); hits consume charge, and charge decays 2/s.","障壁中はHP被害を25%＋残量1点につき0.5%軽減（各種補正込み上限75%）。被弾で残量を消費し、毎秒2減衰。");
   if(LegendaryModuleBonus(a,out int healing,out int rounds,out float dodge))
   {
    var parts=new System.Collections.Generic.List<string>();
    if(healing>0)parts.Add(VaultLanguage.T("heal ","HP ")+healing+VaultLanguage.T(" HP","回復"));
    if(rounds>0)parts.Add(VaultLanguage.T("restore ","予備弾 ")+rounds+VaultLanguage.T(" reserve rounds","発回復"));
    if(dodge>0)parts.Add(VaultLanguage.T("shorten dodge cooldown by ","回避待機を ")+dodge.ToString("0.00")+VaultLanguage.T("s","秒短縮"));
    string condition=a.effect==ArtifactEffect.ShieldConversion?VaultLanguage.T("while barrier is active (then every 5s): ","障壁有効中に発動（以後5秒ごと）："):VaultLanguage.T("when the module activates: ","モジュール発動時：");
    detail+=" / "+VaultLanguage.T("Legendary circuit ","レジェンダリー回路")+condition+string.Join(VaultLanguage.T(", ","、"),parts);
   }
   if(string.IsNullOrEmpty(a.manufacturerId))return detail;
   return ManufacturerCatalog.Name(a.manufacturerId,VaultLanguage.Japanese)+" / "+ModuleTendency(a)+"\n"+detail;
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
  static string ModuleTendency(ArtifactItem a){
   string[] en={"Precision affixes; modest output, shorter cooldown.","Heavy reserve affixes; stronger bursts, longer cooldown.","Fire-rate affixes; rapid cooldown, weaker bursts.","Regeneration affixes; lower direct output.","Damage affixes; risky long cooldown or weaker conversion.","Protection affixes; consistent output and cooldown."};
   string[] ja={"精度Affix傾向。出力控えめ・待機短縮。","体力Affix傾向。強い瞬発力・長い待機。","連射Affix傾向。待機短縮・瞬発力低下。","再生Affix傾向。直接出力は控えめ。","威力Affix傾向。長い待機、または変換出力低下。","防護Affix傾向。安定した出力と待機。"};
   int i=ManufacturerCatalog.Index(a.manufacturerId);return i<0?"":(VaultLanguage.Japanese?ja:en)[i];
  }
  static string EffectDescription(ArtifactItem a){switch(a.effect){case ArtifactEffect.KillShield:return VaultLanguage.T("Equipped + enemy kill + cooldown ready: gain ","装備中＋敵撃破＋待機終了：障壁 +")+(8*a.power).ToString("0.0")+VaultLanguage.T(" shield (cap 30), ","（上限30）、")+a.cooldown.ToString("0.00")+VaultLanguage.T("s cooldown. Shield decays 2/s.","秒待機。毎秒2減衰。");case ArtifactEffect.DashPulse:return VaultLanguage.T("Equipped + dash + cooldown ready: pulse ","装備中＋ダッシュ＋待機終了：周囲波動 ")+(12*a.power).ToString("0.0")+VaultLanguage.T(" damage in 3.5m, ","ダメージ、半径3.5m、")+a.cooldown.ToString("0.00")+VaultLanguage.T("s cooldown.","秒待機。");case ArtifactEffect.ReloadSurge:return VaultLanguage.T("Equipped + reload completed + cooldown ready: +","装備中＋装填完了＋待機終了：威力 +")+(15*a.power).ToString("0")+VaultLanguage.T("% damage for 3s; ","%を3秒、待機")+a.cooldown.ToString("0.00")+VaultLanguage.T("s cooldown.","秒。");default:return VaultLanguage.T("Equipped + shield > 0: +","装備中＋障壁がある間：威力 +")+(12*a.power).ToString("0")+VaultLanguage.T("% damage while a barrier is active.","%。障壁が有効な間のみ発動。");}}
 }
}
