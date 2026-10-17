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
        GUIStyle mobileInventoryText,mobileInventoryButton;
        void MobileItemText(Rect rect,string value,Color color)
        {
            GUI.color=color;GUI.Label(rect,value,mobileInventoryText);GUI.color=Color.white;
        }
        bool MobileItemButton(Rect rect,string caption,bool selected=false)
        {
            Panel(rect,selected?Gold:Cyan);
            return GUI.Button(rect,caption,mobileInventoryButton);
        }
        string MobileItemMeta(int level,Rarity rarity,string maker,int score,bool equipped)
        {
            return (equipped?T("EQUIPPED · ","装備中 · "):"")+"Lv "+level+" / "+VaultLanguage.RarityName(rarity)+" / "+maker+" / "+T("SCORE ","評価 ")+score;
        }
        void MobileItemFrame(float y,Color color,string name,string meta,string details)
        {
            Panel(new Rect(0,y,1320,220),color);
            MobileItemText(new Rect(180,y+8,660,59),name,color);
            MobileItemText(new Rect(180,y+66,660,59),meta,Gold);
            MobileItemText(new Rect(180,y+126,660,88),details,Mist);
        }
        public void DrawMobileInventory()
        {
            if(mobileInventoryText==null)
            {
                mobileInventoryText=new GUIStyle(label){fontSize=24,wordWrap=true,clipping=TextClipping.Clip};
                mobileInventoryButton=new GUIStyle(centered){fontSize=24,wordWrap=true};
            }
            Box(new Rect(0,0,1440,900),new Color(.027f,.028f,.023f,.99f));
            MobileItemText(new Rect(40,18,1020,48),T("EQUIPMENT / choose an equip slot","装備 / 装備する枠を選択"),Gold);
            MobileItemText(new Rect(40,68,1020,48),T("Drag the list to browse","リストをスワイプして閲覧"),Mist);
            if(MobileItemButton(new Rect(1110,5,290,110),T("CLOSE / RETURN","閉じて戻る"))){ToggleInventory();return;}
            string[] tabs={T("FIREARMS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接武器")};
            for(int i=0;i<4;i++)if(MobileItemButton(new Rect(40+i*340,120,320,100),tabs[i],mobileInventoryTab==i)){mobileInventoryTab=i;MobileInventoryScroll=Vector2.zero;}
            int count=mobileInventoryTab==0?Weapons.Count:mobileInventoryTab==1?ArmorInventory.Count:mobileInventoryTab==2?ArtifactInventory.Count:MeleeWeaponInventory.Count;
            MobileInventoryContentHeight=Mathf.Max(550,count*230);
            MobileInventoryScroll=GUI.BeginScrollView(MobileInventoryScrollRect,MobileInventoryScroll,new Rect(0,0,1320,MobileInventoryContentHeight));
            if(count==0)MobileItemText(new Rect(30,30,1200,90),T("No equipment in this category","このカテゴリの装備はありません"),Mist);
            if(mobileInventoryTab==0)
            {
                var ordered=EquippedFirst(Weapons,w=>EquippedWeaponSlot(w)>=0);
                for(int row=0;row<ordered.Count;row++)
                {
                    var item=ordered[row];float y=row*230;int slotIndex=EquippedWeaponSlot(item);
                    MobileItemFrame(y,item.color,VaultLanguage.GunName(item),MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),GearScoring.Score(item),slotIndex>=0),T("DMG ","威力 ")+item.damage.ToString("0.0")+" ×"+item.pellets+T(" / RATE "," / 連射 ")+(1/Mathf.Max(.001f,item.interval)).ToString("0.0")+"/s"+T(" / MAG "," / 弾数 ")+item.magazine+"\n"+T("RELOAD ","装填 ")+item.reloadSeconds.ToString("0.00")+T("s / SPEED ","秒 / 弾速 ")+item.speed.ToString("0.0")+T(" / ACC "," / 命中 ")+Arsenal.Accuracy(item).ToString("0.0")+"%");
                    ItemPortraits.Draw(new Rect(12,y+25,150,150),item);
                    for(int slot=0;slot<Mathf.Min(4,AvailableWeaponSlotCount);slot++)
                        if(MobileItemButton(new Rect(850+slot*116,y+56,110,110),T("SLOT ","枠 ")+(slot+1),slot==slotIndex))EquipWeapon(Weapons.IndexOf(item),slot);
                }
            }
            else if(mobileInventoryTab==1)
            {
                var ordered=EquippedFirst(ArmorInventory,a=>Array.IndexOf(EquippedArmor,a)>=0);
                for(int row=0;row<ordered.Count;row++)
                {
                    var item=ordered[row];float y=row*230;bool chosen=Array.IndexOf(EquippedArmor,item)>=0;
                    MobileItemFrame(y,Arsenal.Colors[(int)item.rarity],Armory.Name(item),MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),GearScoring.Score(item),chosen),Armory.SlotName(item.slot)+" / "+Armory.Stats(item));
                    ItemPortraits.Draw(new Rect(12,y+25,150,150),item);
                    if(MobileItemButton(new Rect(920,y+56,360,110),chosen?T("REMOVE","外す"):T("EQUIP","装備"),chosen)){if(chosen)UnequipArmor((int)item.slot);else EquipArmor(item);}
                }
            }
            else if(mobileInventoryTab==2)
            {
                var ordered=EquippedFirst(ArtifactInventory,a=>Array.IndexOf(EquippedArtifacts,a)>=0);
                for(int row=0;row<ordered.Count;row++)
                {
                    var item=ordered[row];float y=row*230;int equipped=Array.IndexOf(EquippedArtifacts,item);
                    MobileItemFrame(y,Arsenal.Colors[(int)item.rarity],item.Name,MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),GearScoring.Score(item),equipped>=0),item.Description+" / "+GearScoring.AffixSummary(item));
                    ManufacturerBranding.ModulePortrait(new Rect(12,y+25,150,150),item);
                    for(int slot=0;slot<2;slot++)if(MobileItemButton(new Rect(850+slot*150,y+56,140,110),T("SLOT ","枠 ")+(slot+1),equipped==slot))EquipArtifact(item,slot);
                    if(equipped>=0&&MobileItemButton(new Rect(1150,y+56,150,110),T("REMOVE","外す")))UnequipArtifact(equipped);
                }
            }
            else
            {
                var ordered=EquippedFirst(MeleeWeaponInventory,a=>a==EquippedMeleeWeapon);
                for(int row=0;row<ordered.Count;row++)
                {
                    var item=ordered[row];float y=row*230;bool chosen=item==EquippedMeleeWeapon;
                    MobileItemFrame(y,item.color,MeleeWeaponCatalog.Name(item),MobileItemMeta(item.level,item.rarity,ManufacturerCatalog.Name(ManufacturerBranding.Maker(item),VaultLanguage.Japanese),MeleeWeaponCatalog.Score(item),chosen),T("DMG ","威力 ")+item.damage+T(" / RATE "," / 速度 ")+(1/Mathf.Max(.01f,item.interval)).ToString("0.0")+T("/s / RANGE ","/秒 / 射程 ")+item.range.ToString("0.0")+"m / "+item.arcDegrees.ToString("0")+"°"+(item.uniqueEffect?T(" / UNIQUE EFFECT"," / 固有効果"):""));
                    DrawMeleeIcon(new Rect(12,y+25,150,150),item.family);
                    if(MobileItemButton(new Rect(920,y+56,360,110),chosen?T("EQUIPPED","装備中"):T("EQUIP","装備"),chosen))EquipMeleeWeapon(item);
                }
            }
            GUI.EndScrollView();
            for(int i=0;i<4;i++)MobileItemText(new Rect(40+i*340,787,330,48),AmmoLabel((AmmoType)i)+" "+AmmoReserve((AmmoType)i),Mist);
            MobileItemText(new Rect(40,833,1360,65),RunBuffSummary(),Cyan);
        }
    }
}
