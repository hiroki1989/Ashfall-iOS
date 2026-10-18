using UnityEngine;
namespace NeonVault {
 public sealed class EquipmentRecipe {
  public int maker,index,family,body,barrel,stock,attachment,slot;
  public bool legendary; public string id,name,japanese;
 }
 public static class DistinctEquipmentDesigns {
  public static readonly EquipmentRecipe[] Guns=Build("gun",500), Armor=Build("armor",500), Modules=Build("module",200);
  static readonly System.Collections.Generic.Dictionary<string,EquipmentRecipe> lookup=BuildLookup();
  static System.Collections.Generic.Dictionary<string,EquipmentRecipe> BuildLookup(){var map=new System.Collections.Generic.Dictionary<string,EquipmentRecipe>();foreach(var pool in new[]{Guns,Armor,Modules})foreach(var r in pool)map.Add(r.id,r);return map;}
  static EquipmentRecipe[] Build(string kind,int count){
   var a=new EquipmentRecipe[count];
   for(int i=0;i<count;i++){
    bool legend=kind!="module"&&i<30;int maker=legend?i/5:i%6;
    int local=legend?i%5:kind=="module"?i/6:(i-30)/6+5;
    int family=kind=="gun"?new[]{12,15,13,6,1,2}[local%6]:kind=="armor"?(legend?i%5:local%5):local%7;
    // Mixed radix parts form a unique physical assembly per maker; no tier suffixes.
    int body=local%6,barrel=local/6%6,stock=local/36%6,attachment=kind=="module"?local/6%6:kind=="armor"?local/6%6:(local*5+maker)%6;
    var r=new EquipmentRecipe{index=i,maker=maker,family=family,slot=kind=="armor"?family:0,body=body,barrel=barrel,stock=stock,attachment=attachment,legendary=legend||(kind=="module"&&family>=4),id="distinct."+kind+"."+i.ToString("000")};
    if(kind=="gun"&&legend){r.family=new[]{2,15,13,6,1}[i%5];r.barrel=(maker+i%5)%6;r.stock=5;}
    Name(r,kind);
    a[i]=r;
   }return a;
  }
  static void Name(EquipmentRecipe r,string kind){
   string[][] en={new[]{"Harrier","Swift","Kite","Peregrine","Merlin","Osprey"},new[]{"Anvil","Rivet","Foundry","Hammer","Crucible","Ironclad"},new[]{"Dynamo","Spark","Thunder","Cyclotron","Livewire","Lightning"},new[]{"Briar","Fern","Orchard","Mycelium","Willow","Hemlock"},new[]{"Clinker","Cinder","Ashfall","Furnace","Firebrand","Bonfire"},new[]{"Convoy","Warden","Bastion","Citadel","Custodian","Bulwark"}};
   string[][] ja={new[]{"鷹","燕","鳶","隼","小隼","鶚"},new[]{"金床","鋲","鋳造炉","鉄槌","坩堝","鉄壁"},new[]{"発電機","火花","雷鳴","加速器","活線","稲妻"},new[]{"茨","羊歯","果樹園","菌糸","柳","毒芹"},new[]{"焼滓","残火","降灰","溶鉱炉","火種","篝火"},new[]{"輸送隊","守衛","防塁","城塞","番人","防壁"}};
   string title=en[r.maker][r.body],jp=ja[r.maker][r.body];
   if(kind=="gun"){
    string[] purpose={"Longwatch","Doorbreaker","Doubletap","Tempest","Deadeye","Broadside"},purposeJa={"遠望","破門","双撃","疾風","必中","斉射"};
    string[] role={"Patrol","Ranger","Raider","Sentinel","Siege","Escort"},roleJa={"哨戒銃","遊撃銃","強襲銃","監視銃","攻城銃","護衛銃"};
    r.name=purpose[r.barrel]+" "+title+" "+role[r.stock];r.japanese=purposeJa[r.barrel]+"の"+jp+"・"+roleJa[r.stock];
   }else if(kind=="armor"){
    string[] role={"Pathfinder","Breacher","Sharpshooter","Assault","Courier","Survivor"},roleJa={"先導兵用","突破兵用","狙撃兵用","突撃兵用","伝令兵用","生存者用"};
    string[] slot={"Helm","Cuirass","Gauntlets","Greaves","Boots"},slotJa={"兜","胸鎧","腕甲","脚鎧","装甲靴"};
    r.name=title+" "+role[r.attachment]+" "+slot[r.slot];r.japanese=jp+"の"+slotJa[r.slot]+"・"+roleJa[r.attachment];
   }else{
    string[] core={"Reserve","Surge","Echo","Exchange","Shelter","Overload"},coreJa={"予備槽","衝撃炉","反復器","交換炉","避難核","過負荷炉"};
    string[] purpose={"Shield","Pulse","Reload","Conversion","Highway","Furnace","Isotope"},purposeJa={"障壁","波動","装填","変換","街道","炉心","同位体"};
    r.name=title+" "+purpose[r.family]+" "+core[r.attachment];r.japanese=jp+"の"+purposeJa[r.family]+coreJa[r.attachment];
   }
  }
  public static EquipmentRecipe Find(string id){if(string.IsNullOrEmpty(id))return null;return lookup.TryGetValue(id,out var r)?r:null;}
  // Presentation-only migration. Never write catalog IDs, item IDs or saved effective stats.
  static readonly System.Collections.Generic.Dictionary<string,EquipmentRecipe> legacyPresentation=new System.Collections.Generic.Dictionary<string,EquipmentRecipe>();
  static int Hash(string key){unchecked{uint h=2166136261;for(int i=0;i<key.Length;i++){h^=key[i];h*=16777619;}return (int)(h&0x7fffffff);}}
  static EquipmentRecipe Legacy(string kind,EquipmentRecipe[] pool,int maker,int filter,string identity){
   string key=kind+":"+maker+":"+filter+":"+identity;
   if(legacyPresentation.TryGetValue(key,out var cached))return cached;
   var candidates=new System.Collections.Generic.List<EquipmentRecipe>();
   foreach(var r in pool)if(r.maker==maker&&(kind=="module"?r.family==filter:!r.legendary&&(kind=="armor"?r.slot==filter:r.body==filter)))candidates.Add(r);
   // Every supported maker/category/slot/effect has a compatible authored recipe.
   if(candidates.Count==0)return null;var chosen=candidates[Hash(key)%candidates.Count];legacyPresentation[key]=chosen;return chosen;
  }
  public static EquipmentRecipe For(Gun item){
   if(item==null)return null;var exact=Find(item.catalogId);if(exact!=null&&item.catalogId.StartsWith("distinct.gun."))return exact;
   int legend=LegendaryCatalog.Index(item.legendaryKey);if(legend>=0)return Guns[legend];
   int model=Mathf.Clamp(item.modelVariant,0,Arsenal.ModelCount-1);
   int body=model>=14?1:model==3||model==4||model==5||model==13?2:model==2||model==7||model==8?5:model==1?4:model==6||model==10||model==11?3:0;
   if(model==0&&item.effect==GunEffect.Scatter&&string.IsNullOrEmpty(item.catalogId))body=2;
   int maker=ManufacturerCatalog.Index(item.manufacturerId);if(maker<0)maker=(model+Mathf.Max(0,item.designVariant))%6;
   string identity=string.IsNullOrEmpty(item.catalogId)?model+":"+item.designVariant+":"+item.visualSet:item.catalogId;
   return Legacy("gun",Guns,maker,body,identity);
  }
  public static EquipmentRecipe For(ArmorItem item){
   if(item==null)return null;var exact=Find(item.catalogId);if(exact!=null&&item.catalogId.StartsWith("distinct.armor."))return exact;
   int legend=LegendaryCatalog.Index(item.legendaryKey);if(legend>=0)return Armor[legend];
   int slot=Mathf.Clamp((int)item.slot,0,4),maker=ManufacturerCatalog.Index(item.manufacturerId);if(maker<0)maker=(Mathf.Max(0,item.visualVariant)+slot)%6;
   string identity=string.IsNullOrEmpty(item.catalogId)?slot+":"+item.visualVariant+":"+item.designVariant:item.catalogId;
   return Legacy("armor",Armor,maker,slot,identity);
  }
  public static EquipmentRecipe For(ArtifactItem item){
   if(item==null)return null;var exact=Find(item.catalogId);if(exact!=null&&item.catalogId.StartsWith("distinct.module."))return exact;
   int effect=Mathf.Clamp((int)item.effect,0,3),family=item.rarity==Rarity.Legendary&&effect<3?effect+4:effect;
   int maker=ManufacturerCatalog.Index(item.manufacturerId);if(maker<0)maker=(effect+Mathf.Clamp(item.dungeon,0,2))%6;
   string identity=string.IsNullOrEmpty(item.catalogId)?family+":"+item.designVariant+":"+item.dungeon:item.catalogId;
   return Legacy("module",Modules,maker,family,identity);
  }

 }
}
