using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        int eqCategory;
        string eqSelectedKey;
        bool eqLastMerchant,eqLastSelling;
        Vector2 eqGridScroll,eqDetailScroll;
        GUIStyle eqCopy,eqCaption,eqButtonStyle;
        float eqGridExtent,eqDetailExtent;
        public bool EquipmentMenuVisible=>InventoryOpen||(screen==Screen.Shop&&!debtPage);
        public int EquipmentGestureRegion(Vector2 point)
        {
            if(!EquipmentMenuVisible||!string.IsNullOrEmpty(pendingSaleKey))return 0;
            float top=InventoryOpen?122:196,gridTop=top+86;
            if(new Rect(36,gridTop+8,432,766-gridTop).Contains(point))return 1;
            if(new Rect(950,top+112,446,500-top).Contains(point))return 2;
            if(new Rect(494,top+8,422,(InventoryOpen?526:452)-45).Contains(point))return 3;
            return 0;
        }
        public void DragEquipmentMenu(int region,Vector2 delta)
        {
            float top=InventoryOpen?122:196;
            if(region==1)eqGridScroll.y=Mathf.Clamp(eqGridScroll.y-delta.y,0,Mathf.Max(0,eqGridExtent-(680-top)));
            if(region==2)eqDetailScroll.y=Mathf.Clamp(eqDetailScroll.y-delta.y,0,Mathf.Max(0,eqDetailExtent-(500-top)));
            if(region==3&&equipmentHeroPreview)equipmentHeroPreview.Angle=Mathf.Repeat(equipmentHeroPreview.Angle-delta.x*.55f+155,360)-155;
        }
        sealed class EqCard
        {
            public string key,name,description,summary; public Color color; public bool equipped;
            public object item; public int offer=-1; public SaleEntry sale;
        }
        void EqText(Rect r,string value,Color color,GUIStyle style=null)
        {GUI.color=color;GUI.Label(r,value,style??eqCopy);GUI.color=Color.white;}
        static readonly Color EqCanvas=new Color(.91f,.925f,.93f);
        static readonly Color EqSurface=new Color(.975f,.972f,.956f);
        static readonly Color EqInk=new Color(.13f,.19f,.23f);
        static readonly Color EqMuted=new Color(.36f,.43f,.46f);
        static readonly Color EqLine=new Color(.73f,.78f,.79f);
        static readonly Color EqAccent=new Color(.16f,.37f,.38f);
        bool EqButton(Rect r,string caption,bool selected=false,bool enabled=true)
        {
            bool hover=enabled&&r.Contains(Event.current.mousePosition);
            Box(r,selected?EqAccent:hover?new Color(.83f,.88f,.87f):EqSurface);
            Box(new Rect(r.x,r.yMax-1,r.width,1),selected?EqAccent:EqLine);
            if(selected)Box(new Rect(r.x+12,r.y+12,3,r.height-24),new Color(.77f,.84f,.65f));
            var style=new GUIStyle(eqButtonStyle){fontStyle=FontStyle.Bold};
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
            if(item is Gun g){c.key="g:"+g.id;c.name=VaultLanguage.GunName(g);c.color=g.color;c.equipped=EquippedWeaponSlot(g)>=0;score=GearScoring.Score(g);reference=EqGunComparison(out has);level=g.level;rarity=g.rarity;maker=ManufacturerBranding.Maker(g);c.description=T("TYPE ","銃種 ")+VaultLanguage.GunClassName(g)+"\n"+GunNumbers(g)+"\n"+LegendaryDescription(g,VaultLanguage.GunDescription(g))+"\n"+T("LOADED ","装填弾数 ")+(g==gun?ammo:magazineAmmo.TryGetValue(g.id,out int loaded)?loaded:g.magazine)+" / "+g.magazine;}
            else if(item is ArmorItem a){c.key="a:"+a.id;c.name=Armory.Name(a);c.equipped=IsArmorEquipped(a);score=GearScoring.Score(a);reference=MobileArmorComparison(a,out has);level=a.level;rarity=a.rarity;maker=ManufacturerBranding.Maker(a);c.description=Armory.SlotName(a.slot)+"\n"+Armory.Stats(a)+"\n"+LegendaryDescription(a,Armory.Description(a))+"\n"+GearScoring.AffixSummary(a);}
            else if(item is ArtifactItem m){c.key="m:"+m.id;c.name=m.Name;c.equipped=Array.IndexOf(EquippedArtifacts,m)>=0;score=GearScoring.Score(m);reference=MobileModuleComparison(m,out has);level=m.level;rarity=m.rarity;maker=ManufacturerBranding.Maker(m);c.description=LegendaryDescription(m,m.Description)+"\n"+GearScoring.AffixSummary(m);}
            else if(item is MeleeWeaponItem w){c.key="w:"+w.id;c.name=MeleeWeaponCatalog.Name(w);c.color=w.color;c.equipped=w==EquippedMeleeWeapon;score=MeleeWeaponCatalog.Score(w);reference=MobileMeleeComparison(w,out has);level=w.level;rarity=w.rarity;maker=ManufacturerBranding.Maker(w);c.description=T("DMG ","威力 ")+MeleeDamageMin(w).ToString("0.0")+"–"+MeleeDamageMax(w).ToString("0.0")+T("\nRATE ","\n速度 ")+(1/Mathf.Max(.01f,w.interval)).ToString("0.0")+"/s\n"+T("RANGE ","射程 ")+w.range.ToString("0.0")+"m / "+w.arcDegrees.ToString("0")+"°"+MeleeLegendaryDetail(w);}
            c.color=Arsenal.Colors[(int)rarity];c.summary="Lv "+level+" · "+VaultLanguage.RarityName(rarity)+"\n"+T("SCORE ","評価 ")+score+(!c.equipped&&has?(score>reference?" ▲":score<reference?" ▼":" ＝"):"");c.description="Lv "+level+" / "+VaultLanguage.RarityName(rarity)+"\n"+ManufacturerCatalog.Name(maker,VaultLanguage.Japanese)+"\n"+T("SCORE ","評価 ")+score+MobileComparison(score,reference,has)+"\n\n"+c.description;return c;
        }
        int EqGunComparison(out bool found)
        {int lowest=int.MaxValue;for(int i=0;i<AvailableWeaponSlotCount;i++){var equipped=WeaponInSlot(i);if(equipped!=null)lowest=Mathf.Min(lowest,GearScoring.Score(equipped));}found=lowest!=int.MaxValue;return found?lowest:0;}
        void EqPortrait(Rect r,object item)
        {if(item is Gun g)ItemPortraits.Draw(r,g);else if(item is ArmorItem a)ItemPortraits.Draw(r,a);else if(item is ArtifactItem m)ManufacturerBranding.ModulePortrait(r,m);else if(item is MeleeWeaponItem w)ManufacturerPortraits.Melee(r,w);}
        List<EqCard> EqCards(bool merchant)
        {
            var cards=new List<EqCard>();
            if(merchant&&!shopSellingPage){for(int i=0;i<shopStock.Count;i++){var o=shopStock[i];object item=o.gun!=null?(object)o.gun:o.armor!=null?(object)o.armor:o.artifact!=null?(object)o.artifact:o.melee;int category=o.gun!=null?0:o.armor!=null?1:o.artifact!=null?2:3;if(category!=eqCategory||item==null||(item is ArmorItem offeredArmor&&!Armory.IsActiveArmor(offeredArmor)))continue;var c=EqMake(item);c.offer=i;c.key="offer:"+i;c.description+="\n\n"+T("PRICE ","価格 ")+ShopOfferPrice(o).ToString("N0");cards.Add(c);}return cards;}
            if(eqCategory==0)foreach(var item in EquippedFirst(Weapons,w=>EquippedWeaponSlot(w)>=0))cards.Add(EqMake(item));
            if(eqCategory==1)foreach(var item in EquippedFirst(ArmorInventory.FindAll(Armory.IsActiveArmor),IsArmorEquipped))cards.Add(EqMake(item));
            if(eqCategory==2)foreach(var item in EquippedFirst(ArtifactInventory,w=>Array.IndexOf(EquippedArtifacts,w)>=0))cards.Add(EqMake(item));
            if(eqCategory==3)foreach(var item in EquippedFirst(MeleeWeaponInventory,w=>w==EquippedMeleeWeapon))cards.Add(EqMake(item));
            if(merchant){var sales=SaleEntries(eqCategory);foreach(var card in cards){string id=card.key.Substring(2);card.sale=sales.Find(s=>s.id==id);if(card.sale!=null)card.description+="\n\n"+T("SALE VALUE ","売却額 ")+card.sale.price.ToString("N0");}}
            return cards;
        }
        // Returns false so the caller can route Briggs' debt office independently.
        bool DrawEquipmentScreen(bool merchant)
        {
            if(merchant&&debtPage)return false;
            int copyFontSize=MobileTouchControls.Enabled?24:20;
            eqCopy=new GUIStyle(label){fontSize=copyFontSize,wordWrap=true,clipping=TextClipping.Clip,richText=true};eqCaption=new GUIStyle(eqCopy){font=VaultLanguage.Japanese||englishHeadingFont==null?this.font:englishHeadingFont,fontSize=MobileTouchControls.Enabled?26:23,fontStyle=FontStyle.Bold};eqButtonStyle=new GUIStyle(centered){fontSize=copyFontSize,wordWrap=true,clipping=TextClipping.Clip};eqButtonStyle.normal.textColor=Ink;eqButtonStyle.hover.textColor=Ink;eqButtonStyle.active.textColor=Ink;
            if(eqLastMerchant!=merchant||eqLastSelling!=shopSellingPage){eqSelectedKey=null;eqGridScroll=eqDetailScroll=Vector2.zero;eqLastMerchant=merchant;eqLastSelling=shopSellingPage;}
            bool eqPreviousEnabled=GUI.enabled;GUI.enabled=eqPreviousEnabled&&string.IsNullOrEmpty(pendingSaleKey);
            Box(new Rect(0,0,1440,900),EqCanvas);
            Box(new Rect(28,12,44,3),EqAccent);
            Box(new Rect(28,103,1384,1),EqLine);
            EqText(new Rect(28,18,950,44),merchant?T("MARA / SALVAGE EXCHANGE","マラ / 回収品交換所"):T("ARSENAL / EQUIPMENT","武器庫 / 装備"),EqInk,new GUIStyle(eqCaption){fontSize=34});
            EqText(new Rect(28,66,900,44),merchant?T("SCRAP ","所持スクラップ ")+Credits.ToString("N0"):T("Select an item to inspect and equip","所持品を選んで詳細確認・装備"),EqMuted);
            if(EqButton(new Rect(1172,16,240,76),T("CLOSE / BACK","閉じて戻る"))){if(merchant){screen=Screen.Hub;pendingSaleKey=null;}else ToggleInventory();return true;}
            if(merchant){if(EqNavigation(new Rect(28,112,212,72),T("BUY","購入"),!shopSellingPage)){shopSellingPage=false;pendingSaleKey=null;}if(EqNavigation(new Rect(250,112,212,72),T("SELL","売却"),shopSellingPage)){shopSellingPage=true;pendingSaleKey=null;}if(EqButton(new Rect(958,112,454,72),T("DEBT / SKILLS","返済 / スキル"))){debtPage=true;debtSkillTreePage=false;return true;}}
            float top=merchant?196:122;
            string[] tabs={T("GUNS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接")};
            var previousTabStyle=eqButtonStyle;eqButtonStyle=new GUIStyle(eqButtonStyle){fontSize=20,wordWrap=false};
            for(int i=0;i<4;i++)if(EqNavigation(new Rect(28+i*112,top,106,74),tabs[i],eqCategory==i)){eqCategory=i;saleCategory=i;eqSelectedKey=null;pendingSaleKey=null;eqGridScroll=eqDetailScroll=Vector2.zero;}
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
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)){eqSelectedKey=c.key;eqDetailScroll=Vector2.zero;}
            }
            GUI.EndScrollView();if(cards.Count==0)EqText(new Rect(48,gridTop+30,400,180),T("No items in this category","このカテゴリにはアイテムがありません"),EqMuted);
            if(selected!=null){EqText(new Rect(952,top+20,440,94),selected.name,EqInk,eqCaption);Box(new Rect(952,top+104,64,3),selected.color);float detailH=500-top;string eqDetail=selected.description+(!merchant?"\n\n"+RunBuffSummary():"");float height=eqCopy.CalcHeight(new GUIContent(eqDetail),410)+24;eqDetailExtent=Mathf.Max(detailH,height);eqDetailScroll=GUI.BeginScrollView(new Rect(950,top+112,446,detailH),eqDetailScroll,new Rect(0,0,416,eqDetailExtent));EqText(new Rect(2,4,410,height),eqDetail,EqInk);GUI.EndScrollView();EqActions(selected,merchant);}
            for(int i=0;i<4;i++){var type=(AmmoType)i;Rect r=new Rect(28+i*348,790,338,merchant?82:48);string caption=AmmoLabel(type)+" "+AmmoReserve(type)+" / "+AmmoCapacity(type);if(merchant){caption+="\n"+T("BUY ","購入 ")+AmmoPackSize(type)+" / "+AmmoPackCost(type).ToString("N0");if(EqButton(r,caption))BuyAmmo(type,1);}else{Box(new Rect(r.x,r.y,r.width,1),EqLine);EqText(new Rect(r.x+14,r.y+10,r.width-28,r.height-12),caption,EqMuted,new GUIStyle(eqCopy){fontSize=MobileTouchControls.Enabled?22:18});}}
            if(!merchant)EqText(new Rect(28,848,1384,50),RunBuffSummary(),EqMuted,new GUIStyle(eqCopy){fontSize=MobileTouchControls.Enabled?24:18});
            GUI.enabled=eqPreviousEnabled;if(merchant&&!string.IsNullOrEmpty(pendingSaleKey))EqSaleConfirmation();return true;
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



