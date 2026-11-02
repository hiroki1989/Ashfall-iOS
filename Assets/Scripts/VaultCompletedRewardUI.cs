using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  Vector2 completedRewardScroll;
  void DrawCompletedGearReward(Rect area){
   var weapon=completedRewardGun;var armor=completedRewardArmor;
   if(weapon==null&&armor==null)return;
   var rarity=weapon!=null?weapon.rarity:armor.rarity;
   var accent=Arsenal.Colors[(int)rarity];
   Box(area,new Color(.045f,.075f,.09f,.96f));
   Box(new Rect(area.x,area.y,4,area.height),accent);
   float portrait=Mathf.Min(area.height-24,190);
   var picture=new Rect(area.x+14,area.y+12,portrait,portrait);
   if(weapon!=null)ItemPortraits.Draw(picture,weapon);else ItemPortraits.Draw(picture,armor);
   float textX=picture.xMax+16, textWidth=area.xMax-textX-18;
   string name=weapon!=null?VaultLanguage.GunName(weapon):Armory.Name(armor);
   string detail=weapon!=null?LegendaryDescription(weapon,VaultLanguage.GunDescription(weapon)):LegendaryDescription(armor,Armory.Description(armor));
   var nameStyle=new GUIStyle(label){fontSize=26,wordWrap=true};
   var metaStyle=new GUIStyle(small){fontSize=21,wordWrap=false};
   var detailStyle=new GUIStyle(small){fontSize=21,wordWrap=true,richText=true};
   float nameHeight=Mathf.Max(31,nameStyle.CalcHeight(new GUIContent(name),textWidth));
   float detailHeight=string.IsNullOrEmpty(detail)?0:detailStyle.CalcHeight(new GUIContent(detail),textWidth);
   float contentHeight=nameHeight+30+detailHeight+14;
   var viewport=new Rect(textX,area.y+9,textWidth,area.height-18);
   var content=new Rect(0,0,textWidth-16,Mathf.Max(viewport.height,contentHeight));
   completedRewardScroll=GUI.BeginScrollView(viewport,completedRewardScroll,content);
   GUI.color=accent;GUI.Label(new Rect(0,0,content.width,nameHeight),name,nameStyle);
   GUI.color=Mist;GUI.Label(new Rect(0,nameHeight+2,content.width,28),VaultLanguage.RarityName(rarity)+"  ·  Lv "+(weapon!=null?weapon.level:armor.level),metaStyle);
   if(!string.IsNullOrEmpty(detail)){GUI.color=Color.white;GUI.Label(new Rect(0,nameHeight+32,content.width,detailHeight),detail,detailStyle);}
   GUI.color=Color.white;GUI.EndScrollView();
  }

 }
}
