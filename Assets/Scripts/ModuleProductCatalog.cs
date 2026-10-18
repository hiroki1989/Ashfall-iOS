using UnityEngine;
namespace NeonVault {
 public sealed class ModuleProduct {
  public string id,name,japanese,manufacturerId; public int family,designVariant;
  public ArtifactEffect effect; public bool legendary;
  public float powerScale,cooldownScale; public string preferredAffix;
 }
 public static class ModuleProductCatalog {
  public const int Count=200;
  static readonly string[] Makers={"kestrel","ironwake","voltarc","mireline","cinder","aegis"};
  static readonly string[] Affixes={"accuracy","health","fireRate","regen","damage","mitigation"};
  static readonly string[] Codes={"capacitor","coil","relay","converter","bulwark","discharger","isotope"};
  static readonly string[][] Names={
   new[]{"Needle Guard","Bastion Cell","Flash Reserve","Green Shelter","Ember Cache","Watchkeeper","Hawkeye Buffer","Rampart Bank","Arc Reclaimer","Verdant Battery"},
   new[]{"Surgical Pulse","Siege Coil","Quickstorm","Spore Ring","Backdraft","Safe Perimeter","Falcon Burst","Thunder Drum","Sparkstep","Rootwave"},
   new[]{"Deadeye Relay","Breach Relay","Rapid Sequencer","Bloom Repeater","Overburn Relay","Steady Circuit","Longshot Link","Heavy Feed","Livewire Link","Lifeline Relay"},
   new[]{"Sight Converter","Fortress Bridge","Surge Divider","Sap Circuit","Hazard Engine","Custodian Gate","Vector Lens","Iron Exchange","Hotwire Matrix","Grove Matrix"},
   new[]{"Highway Sentinel","Highway Citadel","Highway Dynamo","Highway Oasis","Highway Pyre","Highway Warden","Highway Lookout","Highway Redoubt","Highway Lightning","Highway Haven"},
   new[]{"Furnace Scalpel","Furnace Hammer","Furnace Cyclone","Furnace Bloom","Furnace Rupture","Furnace Keeper","Furnace Lance","Furnace Anvil","Furnace Tempest","Furnace Spring"},
   new[]{"Isotope Focus","Isotope Bunker","Isotope Cascade","Isotope Renewal","Isotope Meltdown","Isotope Guardian","Isotope Crosshair","Isotope Foundry","Isotope Storm","Isotope Sanctuary"}
  };
  static readonly string[][] Japanese={
   new[]{"針の護り","砦のセル","閃光予備槽","緑の避難所","残火の貯蔵庫","見張り番","鷹眼バッファ","城壁バンク","電弧回収器","新緑電池"},
   new[]{"精密波動","攻城コイル","疾風雷","胞子環","逆火","安全圏","隼の炸裂","雷鳴ドラム","火花の歩み","根の波"},
   new[]{"必中リレー","突破リレー","高速シーケンサ","開花反復器","過燃焼リレー","安定回路","遠射リンク","重装給電器","活線リンク","命綱リレー"},
   new[]{"照準変換器","要塞ブリッジ","突電分配器","樹液回路","危険エンジン","守衛ゲート","方向レンズ","鉄の交換器","直結マトリクス","木立マトリクス"},
   new[]{"街道の哨兵","街道の城塞","街道の発電機","街道のオアシス","街道の火葬塔","街道の守人","街道の見張り台","街道の堡塁","街道の雷光","街道の安息地"},
   new[]{"炉心のメス","炉心の槌","炉心の旋風","炉心の開花","炉心の破裂","炉心の番人","炉心の槍","炉心の金床","炉心の暴風","炉心の泉"},
   new[]{"同位体の焦点","同位体の防空壕","同位体の連鎖","同位体の再生","同位体の融解","同位体の守護者","同位体の照星","同位体の鋳造所","同位体の嵐","同位体の聖域"}
  };
  static readonly ModuleProduct[] Products=Build();
  public static System.Collections.Generic.IReadOnlyList<ModuleProduct> All=>Products;
  static ModuleProduct[] Build(){
   var result=new System.Collections.Generic.List<ModuleProduct>();
   foreach(var r in DistinctEquipmentDesigns.Modules){int f=r.family,maker=r.maker;float[] power={.93f,1.22f,.87f,.91f,1.26f,1.04f},cooldown={.92f,1.24f,.70f,1.06f,1.32f,.96f};
    result.Add(new ModuleProduct{id=r.id,family=f,designVariant=r.index,name=r.name,japanese=r.japanese,manufacturerId=Makers[maker],effect=(ArtifactEffect)(f<4?f:f-4),legendary=r.legendary,powerScale=power[maker]*HousingPower(r.body)*CorePower(r.attachment,f),cooldownScale=cooldown[maker]*HousingCooldown(r.body)*CoreCooldown(r.attachment,f),preferredAffix=Affixes[maker]});
   }return result.ToArray();
  }

  // Housing: compact / armored / twin reservoir / ventilated / sealed / manifold.
  static float HousingPower(int body)=>new[]{.86f,1.18f,1.28f,.94f,1.06f,1.12f}[body];
  static float HousingCooldown(int body)=>new[]{.78f,1.12f,1.34f,.85f,.97f,1.08f}[body];
  // Reserve stores more; Surge peaks harder; Echo cycles faster; Exchange favors conversion;
  // Shelter prioritizes defensive families; Overload trades a long reset for a large pulse.
  static float CorePower(int core,int family)=>new[]{1.12f,1.25f,.82f,family==3?1.24f:.96f,family==0||family==4?1.20f:1.04f,1.48f}[core];
  static float CoreCooldown(int core,int family)=>new[]{1.16f,1.30f,.68f,family==2?.79f:1.04f,1.08f,1.61f}[core];
  public static ModuleProduct Get(int family,int variant){var choices=new System.Collections.Generic.List<ModuleProduct>();foreach(var p in Products)if(p.family==Mathf.Clamp(family,0,6))choices.Add(p);return choices[Mathf.Abs(variant)%choices.Count];}
  public static ModuleProduct Roll(System.Random random,int family){ModuleProduct p;do{p=Products[random.Next(Products.Length)];}while(p.family!=family);return p;}
  public static ModuleProduct Find(string id){if(string.IsNullOrEmpty(id))return null;foreach(var p in Products)if(p.id==id)return p;
   var bits=id.Split('.');if(bits.Length==3&&bits[0]=="module"&&int.TryParse(bits[2],out int v)){int f=System.Array.IndexOf(Codes,bits[1]);if(f>=0){int maker=(v+f)%6;return new ModuleProduct{id=id,family=f,designVariant=v,name=Names[f][v%10]+ProductGeneration.Name(v,false),japanese=Japanese[f][v%10]+ProductGeneration.Name(v,true),manufacturerId=Makers[maker],effect=(ArtifactEffect)(f<4?f:f-4),legendary=f>=4,powerScale=1,cooldownScale=1,preferredAffix=Affixes[maker]};}}return null;}
 }
}
