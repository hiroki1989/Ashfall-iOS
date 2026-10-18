using UnityEngine;
namespace NeonVault {
 // Dedicated named designs. Lookup is presentation-only and never rerolls saved equipment.
 public static class BespokeLegendaryDesigns {
  public static int Index(Gun g){if(g==null)return -1;int key=LegendaryCatalog.Index(g.legendaryKey);if(key>=0)return key;var r=DistinctEquipmentDesigns.Find(g.catalogId);return r!=null&&r.legendary&&g.catalogId.StartsWith("distinct.gun.")?r.index:-1;}
  public static int Index(ArmorItem a){if(a==null)return -1;int key=LegendaryCatalog.Index(a.legendaryKey);if(key>=0)return key;var r=DistinctEquipmentDesigns.Find(a.catalogId);return r!=null&&r.legendary&&a.catalogId.StartsWith("distinct.armor.")?r.index:-1;}
  public static int Model(int index)=>new[]{2,14,13,6,1}[Mathf.Clamp(index,0,29)%5];
  public static Gun CreateBase(int index){
   index=Mathf.Clamp(index,0,29);int maker=index/5,variant=index%5;var g=Arsenal.BaseModel(Model(index),Rarity.Legendary);
   g.catalogId=DistinctEquipmentDesigns.Guns[index].id;g.manufacturerId=ManufacturerCatalog.Id(maker);g.designVariant=index;g.legendaryKey=LegendaryCatalog.Key(false,index);
   // These are dedicated complete mechanisms, not ordinary barrel/stock stat combinations.
   switch(maker){
    case 0:g.accuracy=Mathf.Min(100,g.accuracy+5);g.speed*=1.12f;g.damage*=.97f;break;
    case 1:g.damage*=1.27f;g.interval*=1.24f;g.reloadSeconds*=1.16f;g.bonusHealth+=8;break;
    case 2:g.interval*=.77f;g.damage*=.88f;g.magazine=Mathf.RoundToInt(g.magazine*1.3f);g.reloadSeconds*=1.1f;break;
    case 3:g.regenBonus+=.35f;g.bonusHealth+=10;g.damage*=.91f;break;
    case 4:g.damage*=1.15f;g.reloadSeconds*=1.16f;g.accuracy=Mathf.Max(1,g.accuracy-5);break;
    case 5:g.damageReductionBonus+=.035f;g.reloadSeconds*=.86f;g.damage*=.96f;break;
   }
   g.name=LegendaryCatalog.GunName(g,false);g.subtitle=LegendaryCatalog.Description(g,false);return g;
  }
 }
}
