using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  void DrawCompletedGearReward(Rect area){
   var weapon=completedRewardGun;var armor=completedRewardArmor;
   if(weapon==null&&armor==null)return;
   var rarity=weapon!=null?weapon.rarity:armor.rarity;
   var accent=Arsenal.Colors[(int)rarity];
   Box(area,new Color(.045f,.075f,.09f,.96f));
   Box(new Rect(area.x,area.y,4,area.height),accent);
   float portrait=Mathf.Min(area.height-14,96);
   var picture=new Rect(area.x+12,area.y+(area.height-portrait)*.5f,portrait,portrait);
   if(weapon!=null)ItemPortraits.Draw(picture,weapon);else ItemPortraits.Draw(picture,armor);
   float textX=picture.xMax+14;
   string name=weapon!=null?VaultLanguage.GunName(weapon):Armory.Name(armor);
   string detail=weapon!=null?weapon.subtitle:armor.subtitle;
   Text(textX,area.y+8,name,new GUIStyle(label){fontSize=22},accent,area.xMax-textX-10);
   Text(textX,area.y+39,VaultLanguage.RarityName(rarity)+"  ·  Lv "+(weapon!=null?weapon.level:armor.level),tiny,Mist,area.xMax-textX-10);
   if(!string.IsNullOrEmpty(detail))Text(textX,area.y+65,detail,new GUIStyle(tiny){wordWrap=true},Color.white,area.xMax-textX-10);
  }
 }
}
