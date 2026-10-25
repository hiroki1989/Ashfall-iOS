using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        int eqCategory;
        string eqSelectedKey;
        bool eqLastMerchant,eqLastSelling,eqShowFullDetails,eqUpgradePage;
        Vector2 eqGridScroll,eqDetailScroll;
        GUIStyle eqCopy,eqCaption,eqButtonStyle;
        float eqGridExtent,eqDetailExtent;
        public bool EquipmentMenuVisible=>InventoryOpen||(screen==Screen.Shop&&!debtPage);
        public int EquipmentGestureRegion(Vector2 point)
        {
            if(!EquipmentMenuVisible||eqUpgradePage||!string.IsNullOrEmpty(pendingSaleKey))return 0;
            float top=InventoryOpen?122:196,gridTop=top+86;
            if(new Rect(36,gridTop+8,432,766-gridTop).Contains(point))return 1;
            if(new Rect(950,top+112,446,448-top).Contains(point))return 2;
            if(new Rect(494,top+8,422,(InventoryOpen?526:452)-45).Contains(point))return 3;
            return 0;
        }
        public void DragEquipmentMenu(int region,Vector2 delta)
        {
            float top=InventoryOpen?122:196;
            if(region==1)eqGridScroll.y=Mathf.Clamp(eqGridScroll.y-delta.y,0,Mathf.Max(0,eqGridExtent-(680-top)));
            if(region==2)eqDetailScroll.y=Mathf.Clamp(eqDetailScroll.y-delta.y,0,Mathf.Max(0,eqDetailExtent-(448-top)));
            if(region==3&&equipmentHeroPreview)equipmentHeroPreview.Angle=Mathf.Repeat(equipmentHeroPreview.Angle-delta.x*.55f+155,360)-155;
        }
        sealed class EqCard
        {
            public string key,name,description,fullDescription,summary; public Color color; public bool equipped;
            public object item; public int offer=-1; public SaleEntry sale;
        }
        void EqText(Rect r,string value,Color color,GUIStyle style=null)
        {GUI.color=color;GUI.Label(r,value,style??eqCopy);GUI.color=Color.white;}
        static readonly Color EqCanvas=new Color(.91f,.925f,.93f);
        static readonly Color EqSurface=new Color(.975f,.972f,.956f);
        static readonly Color EqInk=new Color(.08f,.12f,.15f);
        static readonly Color EqMuted=new Color(.20f,.26f,.29f);
        static readonly Color EqLine=new Color(.73f,.78f,.79f);
        static readonly Color EqAccent=new Color(.16f,.37f,.38f);
        bool EqButton(Rect r,string caption,bool selected=false,bool enabled=true)
        {
            bool hover=enabled&&r.Contains(Event.current.mousePosition);
            Box(r,selected?EqAccent:hover?new Color(.83f,.88f,.87f):EqSurface);
            Box(new Rect(r.x,r.yMax-1,r.width,1),selected?EqAccent:EqLine);
            if(selected)Box(new Rect(r.x+12,r.y+12,3,r.height-24),new Color(.77f,.84f,.65f));
            var style=new GUIStyle(eqButtonStyle){fontStyle=FontStyle.Bold,fontSize=r.width<150?Mathf.Min(eqButtonStyle.fontSize,20):eqButtonStyle.fontSize};
            style.normal.textColor=!enabled?new Color(.57f,.61f,.61f):selected?EqSurface:EqInk;
            style.hover.textColor=style.normal.textColor;style.active.textColor=style.normal.textColor;
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool clicked=GUI.Button(r,caption,style);GUI.enabled=previous;return clicked;
        }
        bool EqNavigation(Rect r,string caption,bool selected)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            if(hover)Box(r,new Color(.86f,.89f,.89f));
            Box(new Rect(r.x,r.yMax-2,r.width,selected?3:1),selected?EqAccent:EqLine);
            EqText(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),caption,selected?EqInk:EqMuted,eqButtonStyle);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        EqCard EqMake(object item)
        {
            var c=new EqCard{item=item,color=Mist};bool has=false;int score=0,reference=0,level=1;Rarity rarity=Rarity.Common;string maker=null;
            if(item is Gun g){c.key="g:"+g.id;c.name=VaultLanguage.GunName(g);c.color=g.color;c.equipped=EquippedWeaponSlot(g)>=0;score=GearScoring.Score(g);reference=EqGunComparison(out has);level=g.level;rarity=g.rarity;maker=ManufacturerBranding.Maker(g);
                string damage=T("DMG ","威力 ")+GunDamageMin(g).ToString("0.0")+"–"+GunDamageMax(g).ToString("0.0")+(g.pellets>1?" ×"+g.pellets:"");
                c.description=T("TYPE ","銃種 ")+VaultLanguage.GunClassName(g)+"\n"+damage+T("   RATE ","   連射 ")+(1/Mathf.Max(.001f,g.interval)).ToString("0.0")+T("/s\nMAG ","/秒\n装弾 ")+g.magazine+T("   RELOAD ","   装填 ")+g.reloadSeconds.ToString("0.0")+T("s\nCRIT ","秒\n会心 ")+(GunCriticalChance(g)*100).ToString("0.#")+"%";
                c.description=EqAppend(c.description,GearScoring.AffixSummary(g));
                if(!string.IsNullOrEmpty(g.legendaryKey))c.description=EqAppend(c.description,LegendaryDescription(g,LegendaryCatalog.Description(g,VaultLanguage.Japanese)));
                c.fullDescription=EqAppend(c.description,T("ACCURACY ","命中 ")+Arsenal.Accuracy(g).ToString("0.#")+"%"+T("   SPEED ","   弾速 ")+g.speed.ToString("0.#"));
                c.fullDescription=EqAppend(c.fullDescription,LegendaryDescription(g,VaultLanguage.GunDescription(g)));}
            else if(item is ArmorItem a){c.key="a:"+a.id;c.name=Armory.Name(a);c.equipped=IsArmorEquipped(a);score=GearScoring.Score(a);reference=MobileArmorComparison(a,out has);level=a.level;rarity=a.rarity;maker=ManufacturerBranding.Maker(a);
                c.description=Armory.SlotName(a.slot)+"\n"+Armory.Stats(a).TrimEnd(' ','·');
                if(!string.IsNullOrEmpty(a.legendaryKey))c.description=EqAppend(c.description,LegendaryDescription(a,LegendaryCatalog.Description(a,VaultLanguage.Japanese)));
                c.fullDescription=EqAppend(c.description,LegendaryDescription(a,Armory.Description(a)));}
            else if(item is ArtifactItem m){c.key="m:"+m.id;c.name=m.Name;c.equipped=Array.IndexOf(EquippedArtifacts,m)>=0;score=GearScoring.Score(m);reference=MobileModuleComparison(m,out has);level=m.level;rarity=m.rarity;maker=ManufacturerBranding.Maker(m);
                string effect=m.Description;int line=effect.IndexOf('\n');if(line>=0)effect=effect.Substring(line+1);int clause=effect.IndexOf(" / ",StringComparison.Ordinal);string shortEffect=clause>=0?effect.Substring(0,clause):effect;
                c.description=shortEffect;c.description=EqAppend(c.description,GearScoring.AffixSummary(m));if(m.rarity==Rarity.Legendary)c.description=EqAppend(c.description,T("Unique effect in details","固有効果は詳細で確認"));
                c.fullDescription=LegendaryDescription(m,m.Description);c.fullDescription=EqAppend(c.fullDescription,GearScoring.AffixSummary(m));}
            else if(item is MeleeWeaponItem w){c.key="w:"+w.id;c.name=MeleeWeaponCatalog.Name(w);c.color=w.color;c.equipped=w==EquippedMeleeWeapon;score=MeleeWeaponCatalog.Score(w);reference=MobileMeleeComparison(w,out has);level=w.level;rarity=w.rarity;maker=ManufacturerBranding.Maker(w);c.description=T("DMG ","威力 ")+MeleeDamageMin(w).ToString("0.0")+"–"+MeleeDamageMax(w).ToString("0.0")+T("\nRATE ","\n速度 ")+(1/Mathf.Max(.01f,w.interval)).ToString("0.0")+"/s\n"+T("RANGE ","射程 ")+w.range.ToString("0.0")+"m / "+w.arcDegrees.ToString("0")+"°"+MeleeLegendaryDetail(w);c.fullDescription=c.description;}
            c.color=Arsenal.Colors[(int)rarity];c.summary="Lv "+level+" · "+VaultLanguage.RarityName(rarity)+"\n"+T("SCORE ","評価 ")+score+(!c.equipped&&has?(score>reference?" ▲"+(score-reference):score<reference?" ▼"+(reference-score):" ＝"):"");
            string comparison=!c.equipped&&has?(score>reference?" ▲"+(score-reference):score<reference?" ▼"+(reference-score):" ＝"):"";
            string header=ManufacturerCatalog.Name(maker,VaultLanguage.Japanese)+"  ·  "+T("SCORE ","評価 ")+score+comparison;
            c.description=EqAppend(header,c.description);c.fullDescription=EqAppend(header,c.fullDescription);return c;
        }
        static string EqAppend(string source,string extra)=>string.IsNullOrWhiteSpace(extra)?source:source+"\n"+extra;
        int EqGunComparison(out bool found)
        {int lowest=int.MaxValue;for(int i=0;i<AvailableWeaponSlotCount;i++){var equipped=WeaponInSlot(i);if(equipped!=null)lowest=Mathf.Min(lowest,GearScoring.Score(equipped));}found=lowest!=int.MaxValue;return found?lowest:0;}
        void EqPortrait(Rect r,object item)
        {if(item is Gun g)ItemPortraits.Draw(r,g);else if(item is ArmorItem a)ItemPortraits.Draw(r,a);else if(item is ArtifactItem m)ManufacturerBranding.ModulePortrait(r,m);else if(item is MeleeWeaponItem w)ManufacturerPortraits.Melee(r,w);}
        List<EqCard> EqCards(bool merchant)
        {
            var cards=new List<EqCard>();
            if(merchant&&!shopSellingPage){for(int i=0;i<shopStock.Count;i++){var o=shopStock[i];object item=o.gun!=null?(object)o.gun:o.armor!=null?(object)o.armor:o.artifact!=null?(object)o.artifact:o.melee;int category=o.gun!=null?0:o.armor!=null?1:o.artifact!=null?2:3;if(category!=eqCategory||item==null||(item is ArmorItem offeredArmor&&!Armory.IsActiveArmor(offeredArmor)))continue;var c=EqMake(item);c.offer=i;c.key="offer:"+i;cards.Add(c);}return cards;}
            if(eqCategory==0)foreach(var item in EquippedFirst(Weapons,w=>EquippedWeaponSlot(w)>=0))cards.Add(EqMake(item));
            if(eqCategory==1)foreach(var item in EquippedFirst(ArmorInventory.FindAll(Armory.IsActiveArmor),IsArmorEquipped))cards.Add(EqMake(item));
            if(eqCategory==2)foreach(var item in EquippedFirst(ArtifactInventory,w=>Array.IndexOf(EquippedArtifacts,w)>=0))cards.Add(EqMake(item));
            if(eqCategory==3)foreach(var item in EquippedFirst(MeleeWeaponInventory,w=>w==EquippedMeleeWeapon))cards.Add(EqMake(item));
            if(merchant){var sales=SaleEntries(eqCategory);foreach(var card in cards){string id=card.key.Substring(2);card.sale=sales.Find(s=>s.id==id);}}
            return cards;
        }
        // Returns false so the caller can route Briggs' debt office independently.
        bool DrawEquipmentScreen(bool merchant)
        {
            if(merchant&&debtPage)return false;
            int copyFontSize=MobileTouchControls.Enabled?24:20;
            eqCopy=new GUIStyle(label){fontSize=copyFontSize,wordWrap=true,clipping=TextClipping.Clip,richText=true};eqCaption=new GUIStyle(eqCopy){font=englishHeadingFont==null?this.font:englishHeadingFont,fontSize=MobileTouchControls.Enabled?26:23,fontStyle=FontStyle.Bold};eqButtonStyle=new GUIStyle(centered){fontSize=copyFontSize,wordWrap=true,clipping=TextClipping.Clip};eqButtonStyle.normal.textColor=Color.white;eqButtonStyle.hover.textColor=Color.white;eqButtonStyle.active.textColor=Color.white;
            if(eqLastMerchant!=merchant||eqLastSelling!=shopSellingPage){eqSelectedKey=null;eqShowFullDetails=false;eqGridScroll=eqDetailScroll=Vector2.zero;eqLastMerchant=merchant;eqLastSelling=shopSellingPage;if(!merchant)eqUpgradePage=false;}
            bool eqPreviousEnabled=GUI.enabled;GUI.enabled=eqPreviousEnabled&&string.IsNullOrEmpty(pendingSaleKey);
            Box(new Rect(0,0,1440,900),EqCanvas);
            Box(new Rect(28,12,44,3),EqAccent);
            Box(new Rect(28,103,1384,1),EqLine);
            EqText(new Rect(28,18,950,44),merchant?T("MARA / SALVAGE EXCHANGE","マラ / 回収品交換所"):T("ARSENAL / EQUIPMENT","武器庫 / 装備"),EqInk,new GUIStyle(eqCaption){fontSize=34});
            EqText(new Rect(28,66,900,44),merchant?T("SCRAP ","所持スクラップ ")+Credits.ToString("N0"):T("Select an item to inspect and equip","所持品を選んで詳細確認・装備"),EqMuted);
            if(EqButton(new Rect(1172,16,240,76),T("CLOSE / BACK","閉じて戻る"))){if(merchant){screen=Screen.Hub;pendingSaleKey=null;}else ToggleInventory();return true;}
            if(merchant)
            {
                if(EqNavigation(new Rect(28,112,212,72),T("BUY","購入"),!shopSellingPage&&!eqUpgradePage)){shopSellingPage=false;eqUpgradePage=false;pendingSaleKey=null;}
                if(EqNavigation(new Rect(250,112,212,72),T("SELL","売却"),shopSellingPage&&!eqUpgradePage)){shopSellingPage=true;eqUpgradePage=false;pendingSaleKey=null;}
                if(EqNavigation(new Rect(472,112,252,72),T("CAPACITY","所持枠拡張"),eqUpgradePage)){eqUpgradePage=true;pendingSaleKey=null;}
                if(EqButton(new Rect(958,112,454,72),T("DEBT / SKILLS","返済 / スキル"))){debtPage=true;debtSkillTreePage=false;return true;}
                if(eqUpgradePage){DrawEquipmentCapacityShop();GUI.enabled=eqPreviousEnabled;return true;}
            }
            float top=merchant?196:122;
            string[] tabs={T("GUNS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接")};
            var previousTabStyle=eqButtonStyle;eqButtonStyle=new GUIStyle(eqButtonStyle){fontSize=20,wordWrap=false};
            for(int i=0;i<4;i++)if(EqNavigation(new Rect(28+i*112,top,106,74),tabs[i],eqCategory==i)){eqCategory=i;saleCategory=i;eqSelectedKey=null;eqShowFullDetails=false;pendingSaleKey=null;eqGridScroll=eqDetailScroll=Vector2.zero;}
            eqButtonStyle=previousTabStyle;
            float gridTop=top+86;
            Box(new Rect(482,top,1,778-top),EqLine);
            Box(new Rect(926,top,1,778-top),EqLine);
            Box(new Rect(940,top+9,466,104),EqSurface);
            Box(new Rect(950,620,446,1),EqLine);
            DrawEquipmentHeroPreview(new Rect(494,top+8,422,merchant?452:526));
            if(equipmentHeroPreview!=null)
            {
                bool hasWardenSlot=equipmentHeroPreview.HasGeneratedWardenSlot;
                string lookLabel=hasWardenSlot
                    ?equipmentHeroPreview.IsShowingGeneratedWarden?T("LOOK: WARDEN / SWITCH","外観: ウォーデン / 切替"):T("LOOK: CURRENT / SWITCH","外観: 現行 / 切替")
                    :T("LOOK: STANDARD","外観: 通常モデル");
                Rect lookRect=new Rect(502,top+12,406,48);
                if(hasWardenSlot)
                {if(EqButton(lookRect,lookLabel))equipmentHeroPreview.ToggleGeneratedWarden();}
                else {Box(lookRect,EqSurface);EqText(lookRect,lookLabel,EqMuted,centered);}
            }
            Box(new Rect(502,652,406,126),EqInk);
            Box(new Rect(518,668,3,92),EqAccent);
            EqText(new Rect(532,660,364,114),T("CURRENT LOADOUT\n","現在の装備\n")+"HP "+maxHp.ToString("0")+T("  MOVE ","  移動 ")+EffectiveMoveSpeed.ToString("0.0")+T("\nDAMAGE REDUCTION ","\n被ダメージ軽減 ")+(DamageReduction*100).ToString("0")+"%",EqSurface);
            var cards=EqCards(merchant);var selected=cards.Find(c=>c.key==eqSelectedKey);if(selected==null&&cards.Count>0){selected=cards[0];eqSelectedKey=selected.key;eqDetailScroll=Vector2.zero;}
            float gridHeight=766-gridTop;
            eqGridExtent=Mathf.Max(gridHeight,cards.Count*200);
            eqGridScroll=GUI.BeginScrollView(new Rect(36,gridTop+8,432,gridHeight),eqGridScroll,new Rect(0,0,410,eqGridExtent));
            var cardText=new GUIStyle(eqCopy){fontSize=20};
            for(int i=0;i<cards.Count;i++)
            {
                var c=cards[i];Rect r=new Rect(0,i*200,408,190);bool chosen=c.key==eqSelectedKey;
                Box(r,chosen?EqSurface:new Color(.94f,.949f,.946f));
                Box(new Rect(r.x,r.yMax-1,r.width,1),EqLine);
                if(chosen)Box(new Rect(r.x,r.y+12,3,r.height-24),EqAccent);
                Box(new Rect(r.x+12,r.y+12,138,138),new Color(.84f,.87f,.87f));
                EqPortrait(new Rect(r.x+16,r.y+16,130,130),c.item);
                Box(new Rect(r.x+16,r.y+160,32,3),c.color);
                EqText(new Rect(r.x+162,r.y+12,236,96),(c.equipped?"✓ ":"")+c.name,EqInk,cardText);
                EqText(new Rect(r.x+162,r.y+112,236,76),c.summary,EqMuted,cardText);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)){eqSelectedKey=c.key;eqShowFullDetails=false;eqDetailScroll=Vector2.zero;}
            }
            GUI.EndScrollView();if(cards.Count==0)EqText(new Rect(48,gridTop+30,400,180),T("No items in this category","このカテゴリにはアイテムがありません"),EqMuted);
            if(selected!=null){EqText(new Rect(952,top+20,440,94),selected.name,EqInk,eqCaption);Box(new Rect(952,top+104,64,3),selected.color);float detailH=448-top;string eqDetail=eqShowFullDetails?selected.fullDescription:selected.description;float height=eqCopy.CalcHeight(new GUIContent(eqDetail),410)+24;eqDetailExtent=Mathf.Max(detailH,height);eqDetailScroll=GUI.BeginScrollView(new Rect(950,top+112,446,detailH),eqDetailScroll,new Rect(0,0,416,eqDetailExtent));EqText(new Rect(2,4,410,height),eqDetail,EqInk);GUI.EndScrollView();if(EqButton(new Rect(950,570,446,44),eqShowFullDetails?T("SHOW SUMMARY","要約に戻す"):T("MORE DETAILS","詳細を表示"))){eqShowFullDetails=!eqShowFullDetails;eqDetailScroll=Vector2.zero;}EqActions(selected,merchant);}
            for(int i=0;i<ActiveAmmoTypeCount;i++){var type=ActiveAmmoTypeAt(i);float width=272;Rect r=new Rect(28+i*278,790,width,merchant?82:48);string shortLabel=type==AmmoType.Handgun?T("HANDGUN","拳銃弾"):type==AmmoType.Shotgun?T("SHOTGUN","散弾"):type==AmmoType.SniperRifle?T("SNIPER","狙撃弾"):T("MACHINE GUN","機関銃弾");string caption=shortLabel+" "+AmmoReserve(type)+"/"+AmmoCapacity(type);if(merchant){caption+="\n"+T("BUY ","購入 ")+AmmoPackSize(type)+" / "+AmmoPackCost(type).ToString("N0");if(EqButton(r,caption))BuyAmmo(type,1);}else{Box(new Rect(r.x,r.y,r.width,1),EqLine);EqText(new Rect(r.x+10,r.y+10,r.width-20,r.height-12),caption,EqMuted,new GUIStyle(eqCopy){fontSize=MobileTouchControls.Enabled?20:17});}}
            if(!merchant)EqText(new Rect(28,848,1384,50),RunBuffSummary(),EqMuted,new GUIStyle(eqCopy){fontSize=MobileTouchControls.Enabled?24:18});
            GUI.enabled=eqPreviousEnabled;if(merchant&&!string.IsNullOrEmpty(pendingSaleKey))EqSaleConfirmation();return true;
        }
        void DrawEquipmentCapacityShop()
        {
            EqText(new Rect(28,204,1320,46),T("PERMANENT CAPACITY UPGRADES","恒久的な所持上限の拡張"),EqInk,new GUIStyle(eqCaption){fontSize=29});
            EqText(new Rect(28,253,1320,42),T("Ten levels per category. Purchases use scrap and persist between runs.","各カテゴリ10段階。スクラップで購入し、周回をまたいで引き継ぎます。"),EqMuted);
            StorageUpgrade[] categories={StorageUpgrade.AmmoHandgun,StorageUpgrade.AmmoShotgun,StorageUpgrade.AmmoRifle,StorageUpgrade.AmmoSniper,StorageUpgrade.Weapons,StorageUpgrade.Armor,StorageUpgrade.Modules,StorageUpgrade.Melee};
            string[] labels={T("HANDGUN AMMO","拳銃弾"),T("SHOTGUN AMMO","散弾"),T("MACHINE GUN AMMO","機関銃弾"),T("SNIPER AMMO","狙撃弾"),T("FIREARMS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接武器")};
            string[] values={T("Handgun reserve","拳銃弾の上限"),T("Shotgun reserve","散弾の上限"),T("Machine gun reserve","機関銃弾の上限"),T("Sniper reserve","狙撃弾の上限"),T("Weapon inventory","銃の所持枠"),T("Armor inventory","防具の所持枠"),T("Module inventory","モジュールの所持枠"),T("Melee inventory","近接武器の所持枠")};
            for(int i=0;i<categories.Length;i++)
            {
                var category=categories[i];int tier=StorageTier(category),cost=StorageUpgradeCost(category);
                int column=i%3,row=i/3;float x=28+column*465,y=310+row*150;
                Box(new Rect(x,y,446,138),EqSurface);Box(new Rect(x,y,4,138),EqAccent);
                EqText(new Rect(x+18,y+10,410,40),labels[i]+"  "+tier+" / 10",EqInk,new GUIStyle(eqCaption){fontSize=23});
                EqText(new Rect(x+18,y+61,205,52),values[i],EqMuted,new GUIStyle(eqCopy){fontSize=18});
                string caption=tier>=10?T("MAXIMUM","最大"):T("UPGRADE / ","拡張 / ")+cost.ToString("N0");
                if(EqButton(new Rect(x+226,y+54,202,70),caption,false,tier<10&&Credits>=cost))BuyStorageUpgrade(category);
            }
        }
        void EqActions(EqCard c,bool merchant)
        {
            if(merchant){if(c.offer>=0){var o=shopStock[c.offer];int cost=ShopOfferPrice(o);if(EqButton(new Rect(950,630,446,76),o.purchased?T("PURCHASED","購入済み"):T("BUY / ","購入 / ")+cost.ToString("N0"),false,!o.purchased&&Credits>=cost))PurchaseShop(c.offer);}else if(c.sale!=null){var s=c.sale;string caption=s.equipped?T("EQUIPPED / CHANGE LOADOUT TO SELL","装備中 / 装備を変更して売却"):s.lastRequired?T("KEEP LAST WEAPON","最後の武器は保持"):T("SELL / ","売却 / ")+s.price.ToString("N0");if(EqButton(new Rect(950,630,446,76),caption,false,!s.equipped&&!s.lastRequired)){saleCategory=eqCategory;pendingSaleKey=SaleKey(s);}if(s.equipped)EqEquipActions(c,712,false);}return;}
            EqEquipActions(c,630,true);
        }
        void EqEquipActions(EqCard c,float y,bool discard)
        {
            if(c.item is Gun g){for(int slot=0;slot<AvailableWeaponSlotCount;slot++){int index=slot;if(EqButton(new Rect(950+slot*112,y,106,72),T("SLOT ","枠 ")+(slot+1),EquippedWeaponSlot(g)==slot))EquipWeapon(Weapons.IndexOf(g),index);}if(discard&&!c.equipped&&Weapons.Count>1&&EqButton(new Rect(950,y+80,446,70),T("DISCARD","破棄")))DiscardWeapon(Weapons.IndexOf(g));}
            else if(c.item is ArmorItem a){if(EqButton(new Rect(950,y,446,72),c.equipped?T("REMOVE","外す"):T("EQUIP","装備"))){if(c.equipped)UnequipArmor((int)a.slot);else EquipArmor(a);}if(discard&&!c.equipped&&EqButton(new Rect(950,y+80,446,70),T("DISCARD","破棄")))DiscardArmor(a);}
            else if(c.item is ArtifactItem m){int current=Array.IndexOf(EquippedArtifacts,m);if(!discard&&current>=0){if(EqButton(new Rect(950,y,446,70),T("REMOVE","外す")))UnequipArtifact(current);return;}for(int slot=0;slot<2;slot++)if(EqButton(new Rect(950+slot*226,y,220,72),T("SLOT ","枠 ")+(slot+1),current==slot))EquipArtifact(m,slot);if(current>=0){if(EqButton(new Rect(950,y+80,446,70),T("REMOVE","外す")))UnequipArtifact(current);}else if(discard&&EqButton(new Rect(950,y+80,446,70),T("DISCARD","破棄")))DiscardArtifact(m);}
            else if(c.item is MeleeWeaponItem w){if(!c.equipped&&EqButton(new Rect(950,y,446,72),T("EQUIP","装備")))EquipMeleeWeapon(w);else if(c.equipped)EqText(new Rect(958,y,420,70),T("✓ EQUIPPED","✓ 装備中"),EqAccent);if(discard&&!c.equipped&&MeleeWeaponInventory.Count>1&&EqButton(new Rect(950,y+80,446,70),T("DISCARD","破棄"))){MeleeWeaponInventory.Remove(w);SaveInventory();}}
        }
        void EqSaleConfirmation()
        {
            var s=SaleEntries(saleCategory).Find(e=>SaleKey(e)==pendingSaleKey);if(s==null){pendingSaleKey=null;return;}
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.85f));Box(new Rect(310,220,820,450),EqSurface);Box(new Rect(340,228,60,3),EqAccent);
            string copy=T("CONFIRM SALE\n\n","売却の確認\n\n")+s.title+"\n\n"+T("Receive ","売却額 ")+s.price.ToString("N0")+T(" scrap. Cannot be undone."," スクラップ。売却後は戻せません。");EqText(new Rect(340,240,760,295),copy,EqInk);
            if(EqButton(new Rect(340,555,360,86),T("CONFIRM SALE","売却を確定"))){CompleteSale(s);pendingSaleKey=null;}
            if(EqButton(new Rect(730,555,370,86),T("CANCEL","キャンセル")))pendingSaleKey=null;
        }
    }
}



