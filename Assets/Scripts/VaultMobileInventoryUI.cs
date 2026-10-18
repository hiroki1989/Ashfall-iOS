using System;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        int mobileInventoryTab;
        public Vector2 MobileInventoryScroll;
        public Rect MobileInventoryScrollRect => new Rect(40,230,1360,550);
        public float MobileInventoryContentHeight { get; private set; }
        GUIStyle mobileInventoryText,mobileInventoryButton,mobileInventoryDetails;
        const float MobileInventoryRowGap=12;
        void MobileItemText(Rect rect,string value,Color color)
        { GUI.color=color;GUI.Label(rect,value,mobileInventoryText);GUI.color=Color.white; }
        void MobileItemDetails(Rect rect,string value)
        {GUI.color=Mist;GUI.Label(rect,value,mobileInventoryDetails);GUI.color=Color.white;}
        bool MobileItemButton(Rect rect,string caption,bool selected=false)
        { Panel(rect,selected?Gold:Cyan);return GUI.Button(rect,caption,mobileInventoryButton); }
        string MobileItemMeta(int level,Rarity rarity,string maker,int score,bool equipped)
        { return (equipped?T("EQUIPPED · ","装備中 · "):"")+"Lv "+level+" / "+VaultLanguage.RarityName(rarity)+" / "+maker+" / "+T("SCORE ","評価 ")+score; }
        float MobileDetailHeight(string value,float width)
        { return mobileInventoryDetails.CalcHeight(new GUIContent(value??""),width); }
        string MobileComparison(int score,int reference,bool available)
        {
            if(!available)return T("  ·  NO COMPARABLE EQUIPMENT","  ·  比較対象なし");
            int delta=score-reference;
            return "  ·  "+(delta>0?"▲ ":delta<0?"▼ ":"＝ ")+Mathf.Abs(delta)+" "+T("vs equipped","装備中比");
        }
        bool MobileArmorEquipped(ArmorItem item)=>Array.IndexOf(EquippedArmor,item)>=0;
        int MobileArmorComparison(ArmorItem item,out bool found)
        { var current=EquippedArmor!=null?(int)item.slot<EquippedArmor.Length?EquippedArmor[(int)item.slot]:null:null;found=current!=null;return GearScoring.Score(current); }
        int MobileGunComparison(Gun item,out bool found)
        {
            int first=int.MaxValue,second=int.MaxValue;
            for(int slot=0;slot<Mathf.Min(2,AvailableWeaponSlotCount);slot++){var current=WeaponInSlot(slot);if(current==null)continue;int value=GearScoring.Score(current);if(value<first){second=first;first=value;}else if(value<second)second=value;}
            found=first!=int.MaxValue;if(!found)return 0;return second==int.MaxValue?first:Mathf.Min(first,second);
        }
        int MobileModuleComparison(ArtifactItem item,out bool found)
        { int best=int.MaxValue;foreach(var current in EquippedArtifacts)if(current!=null&&current.effect==item.effect)best=Mathf.Min(best,GearScoring.Score(current));found=best!=int.MaxValue;return found?best:0; }
        int MobileMeleeComparison(MeleeWeaponItem item,out bool found)
        { found=EquippedMeleeWeapon!=null&&EquippedMeleeWeapon.family==item.family;return found?MeleeWeaponCatalog.Score(EquippedMeleeWeapon):0; }
        public void DrawMobileInventory()
        {
            if(mobileInventoryText==null)
            {
                mobileInventoryText=new GUIStyle(label){fontSize=31,wordWrap=true,clipping=TextClipping.Clip};
                mobileInventoryDetails=new GUIStyle(label){fontSize=28,wordWrap=true,clipping=TextClipping.Clip};
                mobileInventoryButton=new GUIStyle(centered){fontSize=30,wordWrap=true};
            }
            Box(new Rect(0,0,1440,900),new Color(.027f,.028f,.023f,.99f));
            MobileItemText(new Rect(40,18,1020,48),T("EQUIPMENT / choose an equip slot","装備 / 装備する枠を選択"),Gold);
            MobileItemText(new Rect(40,68,1020,48),T("Drag the list to browse · arrows compare same type","リストをスワイプ · 矢印は同種装備との比較"),Mist);
            if(MobileItemButton(new Rect(1110,5,290,110),T("CLOSE / RETURN","閉じて戻る"))){ToggleInventory();return;}
            string[] tabs={T("FIREARMS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接武器")};
            for(int i=0;i<4;i++)if(MobileItemButton(new Rect(40+i*340,120,320,100),tabs[i],mobileInventoryTab==i)){mobileInventoryTab=i;MobileInventoryScroll=Vector2.zero;}
            int count=mobileInventoryTab==0?Weapons.Count:mobileInventoryTab==1?ArmorInventory.Count:mobileInventoryTab==2?ArtifactInventory.Count:MeleeWeaponInventory.Count;
            // Every row grows with its wrapped description, keeping long item text reachable by scrolling.
            float contentHeight=0;
            if(mobileInventoryTab==0){foreach(var item in EquippedFirst(Weapons,w=>EquippedWeaponSlot(w)>=0)){string d=VaultLanguage.GunDescription(item)+"\n"+T("DMG ","威力 ")+GunDamageMin(item).ToString("0.0")+"–"+GunDamageMax(item).ToString("0.0")+" ×"+item.pellets+T(" / RATE "," / 連射 ")+(1/Mathf.Max(.001f,item.interval)).ToString("0.0")+"/s / MAG "+item.magazine+"\n"+T("RELOAD ","装填 ")+item.reloadSeconds.ToString("0.00")+"s / SPEED "+item.speed.ToString("0.0")+" / ACC "+Arsenal.Accuracy(item).ToString("0.0")+"%\n"+T("CRIT ","会心 ")+(GunCriticalChance(item)*100).ToString("0.#")+"% ×"+GunCriticalMultiplier(item).ToString("0.00")+"\n"+GearScoring.AffixSummary(item);contentHeight+=Mathf.Max(300,MobileDetailHeight(d,700)+320)+MobileInventoryRowGap;}}
            else if(mobileInventoryTab==1){foreach(var item in EquippedFirst(ArmorInventory,MobileArmorEquipped)){string d=Armory.Description(item)+"\n"+Armory.Stats(item)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item);contentHeight+=Mathf.Max(300,MobileDetailHeight(d,700)+320)+MobileInventoryRowGap;}}
            else if(mobileInventoryTab==2){foreach(var item in EquippedFirst(ArtifactInventory,a=>Array.IndexOf(EquippedArtifacts,a)>=0)){string d=item.Description+"\n"+GearScoring.AffixSummary(item);contentHeight+=Mathf.Max(300,MobileDetailHeight(d,700)+320)+MobileInventoryRowGap;}}
            else {foreach(var item in EquippedFirst(MeleeWeaponInventory,a=>a==EquippedMeleeWeapon)){string d=T("DMG ","威力 ")+MeleeDamageMin(item).ToString("0.0")+"–"+MeleeDamageMax(item).ToString("0.0")+T(" / RATE "," / 速度 ")+(1/Mathf.Max(.01f,item.interval)).ToString("0.0")+"/s / RANGE "+item.range.ToString("0.0")+"m / "+item.arcDegrees.ToString("0")+"°"+(item.uniqueEffect?T(" / UNIQUE EFFECT"," / 固有効果"):"");contentHeight+=Mathf.Max(300,MobileDetailHeight(d,700)+320)+MobileInventoryRowGap;}}
            MobileInventoryContentHeight=Mathf.Max(550,contentHeight);
            MobileInventoryScroll=GUI.BeginScrollView(MobileInventoryScrollRect,MobileInventoryScroll,new Rect(0,0,1320,MobileInventoryContentHeight));
            if(count==0)MobileItemText(new Rect(30,30,1200,90),T("No equipment in this category","このカテゴリの装備はありません"),Mist);
            float y=0;
            if(mobileInventoryTab==0)
            {
                var ordered=EquippedFirst(Weapons,w=>EquippedWeaponSlot(w)>=0);
                foreach(var item in ordered)
                {
                    int slotIndex=EquippedWeaponSlot(item),score=GearScoring.Score(item);bool chosen=slotIndex>=0,has;int reference=MobileGunComparison(item,out has);
                    string details=VaultLanguage.GunDescription(item)+"\n"+T("DMG ","威力 ")+GunDamageMin(item).ToString("0.0")+"–"+GunDamageMax(item).ToString("0.0")+" ×"+item.pellets+T(" / RATE "," / 連射 ")+(1/Mathf.Max(.001f,item.interval)).ToString("0.0")+"/s / MAG "+item.magazine+"\n"+T("RELOAD ","装填 ")+item.reloadSeconds.ToString("0.00")+"s / SPEED "+item.speed.ToString("0.0")+" / ACC "+Arsenal.Accuracy(item).ToString("0.0")+"%\n"+T("CRIT ","会心 ")+(GunCriticalChance(item)*100).ToString("0.#")+"% ×"+GunCriticalMultiplier(item).ToString("0.00")+"\n"+GearScoring.AffixSummary(item)+MobileComparison(score,reference,has);
                    float h=Mathf.Max(300,MobileDetailHeight(details,700)+260);string meta=MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),score,chosen);
                    Panel(new Rect(0,y,1320,h),Arsenal.Colors[(int)item.rarity]);ItemPortraits.Draw(new Rect(12,y+25,150,150),item);MobileItemText(new Rect(180,y+8,700,76),VaultLanguage.GunName(item),item.color);MobileItemText(new Rect(180,y+88,700,130),meta,Gold);MobileItemDetails(new Rect(180,y+222,700,h-230),details);
                    for(int slot=0;slot<Mathf.Min(2,AvailableWeaponSlotCount);slot++)if(MobileItemButton(new Rect(930+slot*165,y+35,150,100),T("SLOT ","枠 ")+(slot+1),slot==slotIndex))EquipWeapon(Weapons.IndexOf(item),slot);
                    if(!chosen&&Weapons.Count>1&&MobileItemButton(new Rect(930,y+150,315,90),T("DISCARD","破棄")))DiscardWeapon(Weapons.IndexOf(item));
                    y+=h+MobileInventoryRowGap;
                }
            }
            else if(mobileInventoryTab==1)
            {
                var ordered=EquippedFirst(ArmorInventory,MobileArmorEquipped);
                foreach(var item in ordered)
                {
                    bool chosen=MobileArmorEquipped(item),has;int score=GearScoring.Score(item),reference=MobileArmorComparison(item,out has);
                    string details=Armory.Description(item)+"\n"+Armory.SlotName(item.slot)+" / "+Armory.Stats(item)+"\n"+T("SCORE ","評価 ")+score+MobileComparison(score,reference,has);
                    float h=Mathf.Max(300,MobileDetailHeight(details,700)+260);string meta=MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),score,chosen);
                    Panel(new Rect(0,y,1320,h),Arsenal.Colors[(int)item.rarity]);ItemPortraits.Draw(new Rect(12,y+25,150,150),item);MobileItemText(new Rect(180,y+8,700,76),Armory.Name(item),Arsenal.Colors[(int)item.rarity]);MobileItemText(new Rect(180,y+88,700,130),meta,Gold);MobileItemDetails(new Rect(180,y+222,700,h-230),details);
                    if(chosen){if(MobileItemButton(new Rect(930,y+35,315,100),T("REMOVE","外す"),true))UnequipArmor((int)item.slot);}
                    else {if(MobileItemButton(new Rect(930,y+35,315,100),T("EQUIP","装備")))EquipArmor(item);if(MobileItemButton(new Rect(930,y+150,315,90),T("DISCARD","破棄")))DiscardArmor(item);}
                    y+=h+MobileInventoryRowGap;
                }
            }
            else if(mobileInventoryTab==2)
            {
                var ordered=EquippedFirst(ArtifactInventory,a=>Array.IndexOf(EquippedArtifacts,a)>=0);
                foreach(var item in ordered)
                {
                    int equipped=Array.IndexOf(EquippedArtifacts,item),score=GearScoring.Score(item);bool has;int reference=MobileModuleComparison(item,out has);
                    string details=item.Description+"\n"+GearScoring.AffixSummary(item)+MobileComparison(score,reference,has);
                    float h=Mathf.Max(300,MobileDetailHeight(details,700)+260);string meta=MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),score,equipped>=0);
                    Panel(new Rect(0,y,1320,h),Arsenal.Colors[(int)item.rarity]);ManufacturerBranding.ModulePortrait(new Rect(12,y+25,150,150),item);MobileItemText(new Rect(180,y+8,700,76),item.Name,Arsenal.Colors[(int)item.rarity]);MobileItemText(new Rect(180,y+88,700,130),meta,Gold);MobileItemDetails(new Rect(180,y+222,700,h-230),details);
                    for(int slot=0;slot<2;slot++)if(MobileItemButton(new Rect(930+slot*165,y+35,150,100),T("SLOT ","枠 ")+(slot+1),equipped==slot))EquipArtifact(item,slot);
                    if(equipped<0&&MobileItemButton(new Rect(930,y+150,315,90),T("DISCARD","破棄")))DiscardArtifact(item);
                    y+=h+MobileInventoryRowGap;
                }
            }
            else
            {
                var ordered=EquippedFirst(MeleeWeaponInventory,a=>a==EquippedMeleeWeapon);
                foreach(var item in ordered)
                {
                    bool chosen=item==EquippedMeleeWeapon,has;int score=MeleeWeaponCatalog.Score(item),reference=MobileMeleeComparison(item,out has);
                    string details=T("DMG ","威力 ")+MeleeDamageMin(item).ToString("0.0")+"–"+MeleeDamageMax(item).ToString("0.0")+T(" / RATE "," / 速度 ")+(1/Mathf.Max(.01f,item.interval)).ToString("0.0")+"/s / RANGE "+item.range.ToString("0.0")+"m / "+item.arcDegrees.ToString("0")+"°"+(item.uniqueEffect?T(" / UNIQUE EFFECT"," / 固有効果"):"")+MobileComparison(score,reference,has);
                    float h=Mathf.Max(300,MobileDetailHeight(details,700)+260);Panel(new Rect(0,y,1320,h),item.color);DrawMeleeIcon(new Rect(12,y+25,150,150),item.family);MobileItemText(new Rect(180,y+8,700,76),MeleeWeaponCatalog.Name(item),item.color);MobileItemText(new Rect(180,y+88,700,130),MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),score,chosen),Gold);MobileItemDetails(new Rect(180,y+222,700,h-230),details);
                    if(chosen){if(MeleeWeaponInventory.Count>1&&MobileItemButton(new Rect(930,y+35,315,100),T("EQUIPPED","装備中"),true)){}}else if(MobileItemButton(new Rect(930,y+35,315,100),T("EQUIP","装備")))EquipMeleeWeapon(item);
                    if(!chosen&&MeleeWeaponInventory.Count>1&&MobileItemButton(new Rect(930,y+150,315,90),T("DISCARD","破棄"))){MeleeWeaponInventory.Remove(item);SaveInventory();}
                    y+=h+MobileInventoryRowGap;
                }
            }
            GUI.EndScrollView();
            for(int i=0;i<4;i++)MobileItemText(new Rect(40+i*340,787,330,48),AmmoLabel((AmmoType)i)+" "+AmmoReserve((AmmoType)i)+" / "+AmmoCapacity((AmmoType)i),Mist);
            MobileItemText(new Rect(40,833,1360,65),RunBuffSummary(),Cyan);
        }
    }
}
