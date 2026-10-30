using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        int eqCategory;
        string eqSelectedKey;
        object eqPendingDiscard;
        string eqPendingDiscardName;
        bool eqLastMerchant,eqLastSelling,eqShowFullDetails,eqUpgradePage;
        Vector2 eqGridScroll,eqDetailScroll,eqLoadoutScroll;
        GUIStyle eqCopy,eqCaption,eqButtonStyle;
        float eqGridExtent,eqDetailExtent;
        public bool EquipmentMenuVisible=>InventoryOpen||(screen==Screen.Shop&&!debtPage);
        public int EquipmentGestureRegion(Vector2 point)
        {
            if(!EquipmentMenuVisible||eqUpgradePage||!string.IsNullOrEmpty(pendingSaleKey)||eqPendingDiscard!=null)return 0;
            float top=InventoryOpen?122:196,gridTop=top+86;
            if(new Rect(36,gridTop+8,432,766-gridTop).Contains(point))return 1;
            if(new Rect(950,top+248,446,362-top).Contains(point))return 2;
            if(new Rect(502,652,406,126).Contains(point))return 4;
            if(new Rect(494,top+8,422,(InventoryOpen?526:452)-45).Contains(point))return 3;
            return 0;
        }
        public void DragEquipmentMenu(int region,Vector2 delta)
        {
            float top=InventoryOpen?122:196;
            if(region==1)eqGridScroll.y=Mathf.Clamp(eqGridScroll.y-delta.y,0,Mathf.Max(0,eqGridExtent-(680-top)));
            if(region==2)eqDetailScroll.y=Mathf.Clamp(eqDetailScroll.y-delta.y,0,Mathf.Max(0,eqDetailExtent-(362-top)));
            if(region==3&&equipmentHeroPreview)equipmentHeroPreview.Angle=Mathf.Repeat(equipmentHeroPreview.Angle-delta.x*.55f+155,360)-155;
            if(region==4)eqLoadoutScroll.y=Mathf.Max(0,eqLoadoutScroll.y-delta.y);
        }
        sealed class EqCard
        {
            public string key,name,description,fullDescription,summary,comparison,maker; public Color color; public bool equipped;
            public object item; public int offer=-1; public SaleEntry sale;
        }
        void EqText(Rect r,string value,Color color,GUIStyle style=null)
        {GUI.color=color;GUI.Label(r,value,style??eqCopy);GUI.color=Color.white;}
        static readonly Color EqCanvas=new Color(.075f,.080f,.083f);
        static readonly Color EqSurface=new Color(.115f,.121f,.125f);
        static readonly Color EqInk=new Color(.93f,.90f,.84f);
        static readonly Color EqMuted=new Color(.65f,.65f,.62f);
        static readonly Color EqLine=new Color(.25f,.26f,.26f);
        static readonly Color EqAccent=new Color(.77f,.49f,.30f);
        Texture2D junkBinIcon;
        bool EqButton(Rect r,string caption,bool selected=false,bool enabled=true)
        {
            bool hover=enabled&&r.Contains(Event.current.mousePosition);
            Box(r,selected?new Color(.29f,.22f,.17f):hover?new Color(.19f,.19f,.18f):EqSurface);
            FieldUIArt.Plate(r,selected);

            var style=new GUIStyle(eqButtonStyle){fontStyle=FontStyle.Normal,fontSize=r.width<150?Mathf.Min(eqButtonStyle.fontSize,23):eqButtonStyle.fontSize};
            style.normal.textColor=!enabled?new Color(.42f,.43f,.42f):EqInk;
            style.hover.textColor=style.normal.textColor;style.active.textColor=style.normal.textColor;
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool clicked=GUI.Button(r,caption,style);GUI.enabled=previous;if(clicked)PlayUserSound("skill",.09f);return clicked;
        }
        bool EqNavigation(Rect r,string caption,bool selected)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            if(hover)Box(r,new Color(.16f,.17f,.17f));
            Box(new Rect(r.x,r.yMax-2,r.width,selected?3:1),selected?EqAccent:EqLine);
            EqText(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),caption,selected?EqAccent:EqMuted,eqButtonStyle);
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);if(clicked)PlayUserSound("skill",.09f);return clicked;
        }
        EqCard EqMake(object item)
        {
            var c=new EqCard{item=item,color=Mist};bool has=false;int score=0,reference=0,level=1;Rarity rarity=Rarity.Common;string maker=null;
            if(item is Gun g){c.key="g:"+g.id;c.name=VaultLanguage.GunName(g);c.color=g.color;c.equipped=EquippedWeaponSlot(g)>=0;score=GearScoring.Score(g);reference=EqGunComparison(out has);level=g.level;rarity=g.rarity;maker=ManufacturerBranding.Maker(g);
                string damage=T("DMG ","威力 ")+GunDamageMin(g).ToString("0.0")+"-"+GunDamageMax(g).ToString("0.0")+(g.pellets>1?" ×"+g.pellets:"");
                c.description=T("TYPE ","銃種 ")+VaultLanguage.GunClassName(g)+"\n"+damage+T("   RATE ","   連射 ")+(1/Mathf.Max(.001f,g.interval)).ToString("0.0")+T("/s\nMAG ","/秒\n装弾 ")+g.magazine+T("   RELOAD ","   リロード ")+g.reloadSeconds.ToString("0.0")+T("s\nCRIT ","秒\n会心 ")+(GunCriticalChance(g)*100).ToString("0.#")+"%";
                c.description=EqAppend(c.description,GearScoring.AffixSummary(g));
                if(!string.IsNullOrEmpty(g.legendaryKey))c.description=EqAppend(c.description,LegendaryDescription(g,LegendaryCatalog.Description(g,VaultLanguage.Japanese)));
                c.fullDescription=EqAppend(c.description,T("ACCURACY ","命中 ")+Arsenal.Accuracy(g).ToString("0.#")+"%"+T("   SPEED ","   弾速 ")+g.speed.ToString("0.#"));
                }
            else if(item is ArmorItem a){c.key="a:"+a.id;c.name=Armory.Name(a);c.equipped=IsArmorEquipped(a);score=GearScoring.Score(a);reference=MobileArmorComparison(a,out has);level=a.level;rarity=a.rarity;maker=ManufacturerBranding.Maker(a);
                c.description=Armory.SlotName(a.slot)+"\n"+Armory.Stats(a).TrimEnd(' ','·');
                if(!string.IsNullOrEmpty(a.legendaryKey))c.description=EqAppend(c.description,LegendaryDescription(a,LegendaryCatalog.Description(a,VaultLanguage.Japanese)));
                c.fullDescription=string.IsNullOrEmpty(a.legendaryKey)?EqAppend(c.description,LegendaryDescription(a,Armory.Description(a))):c.description;}
            else if(item is ArtifactItem m){c.key="m:"+m.id;c.name=ArtifactCatalog.DisplayName(m);c.equipped=Array.IndexOf(EquippedArtifacts,m)>=0;score=GearScoring.Score(m);reference=MobileModuleComparison(m,out has);level=m.level;rarity=m.rarity;maker=ManufacturerBranding.Maker(m);
                string effect=m.Description;int line=effect.IndexOf('\n');if(line>=0)effect=effect.Substring(line+1);int clause=effect.IndexOf(" / ",StringComparison.Ordinal);string shortEffect=clause>=0?effect.Substring(0,clause):effect;
                c.description=shortEffect;c.description=EqAppend(c.description,GearScoring.AffixSummary(m));if(m.rarity==Rarity.Legendary)c.description=EqAppend(c.description,T("Unique effect in details","固有効果は詳細で確認"));
                c.fullDescription=LegendaryDescription(m,m.Description);c.fullDescription=EqAppend(c.fullDescription,GearScoring.AffixSummary(m));}
            else if(item is MeleeWeaponItem w){c.key="w:"+w.id;c.name=MeleeWeaponCatalog.Name(w);c.color=w.color;c.equipped=w==EquippedMeleeWeapon;score=MeleeWeaponCatalog.Score(w);reference=MobileMeleeComparison(w,out has);level=w.level;rarity=w.rarity;maker=ManufacturerBranding.Maker(w);c.description=T("TYPE ","種別 ")+MeleeWeaponCatalog.Name(w.family)+"\n"+T("DMG ","威力 ")+MeleeDamageMin(w).ToString("0.0")+"-"+MeleeDamageMax(w).ToString("0.0")+T("\nRATE ","\n速度 ")+(1/Mathf.Max(.01f,w.interval)).ToString("0.0")+"/s\n"+T("RANGE ","射程 ")+w.effectiveRange.ToString("0.0")+"m / "+w.arcDegrees.ToString("0")+"°"+MeleeLegendaryDetail(w);c.description=EqAppend(c.description,GearScoring.AffixSummary(w));c.fullDescription=c.description;}
            c.color=Arsenal.Colors[(int)rarity];c.summary="Lv "+level+" · "+VaultLanguage.RarityName(rarity);c.comparison=!c.equipped&&has?EqComparison(score,reference):"";
            string comparison=!c.equipped&&has?EqComparison(score,reference):"";
            string header=comparison;
            c.maker=maker;c.description=EqAppend(header,c.description);c.fullDescription=EqAppend(header,c.fullDescription);return c;
        }
        static string EqComparison(int score,int reference)=>score>reference?" <color=#FF5B50>▲"+(score-reference)+"</color>":score<reference?" <color=#65AAFF>▼"+(reference-score)+"</color>":" ＝";
        static string EqAppend(string source,string extra)=>string.IsNullOrWhiteSpace(extra)?source:source+"\n"+extra;
        int EqGunComparison(out bool found)
        {int lowest=int.MaxValue;for(int i=0;i<AvailableWeaponSlotCount;i++){var equipped=WeaponInSlot(i);if(equipped!=null)lowest=Mathf.Min(lowest,GearScoring.Score(equipped));}found=lowest!=int.MaxValue;return found?lowest:0;}
        void EqPortrait(Rect r,object item,bool detailed=false)
        {
            bool previousBadge=ManufacturerBranding.PortraitBadgeEnabled;
            ManufacturerBranding.PortraitBadgeEnabled=false;
            int score=0;
            if(item is Gun g){ItemPortraits.Draw(r,g);score=GearScoring.Score(g);
                float size=Mathf.Min(Mathf.Min(r.width,r.height)-10,Mathf.Clamp(Mathf.Min(r.width,r.height)*.36f,30,48));
                var icon=new Rect(r.x+5,r.y+5,size,size);Box(icon,new Color(.04f,.045f,.05f,.88f));
                FieldUIArt.GunClassIcon(new Rect(icon.x+2,icon.y+2,size-4,size-4),g,EqInk);}
            else if(item is ArmorItem a){ItemPortraits.Draw(r,a);score=GearScoring.Score(a);}
            else if(item is ArtifactItem m){ManufacturerBranding.ModulePortrait(r,m);score=GearScoring.Score(m);}
            else if(item is MeleeWeaponItem w){
                // Each melee atlas cell is one fifth of the texture width; preserve
                // that aspect ratio when the detail panel is much wider than tall.
                Rect portrait=r;
                if(detailed){var atlas=Resources.Load<Texture2D>("Art/MeleeWeaponIcons-portrait-v2")??Resources.Load<Texture2D>("Art/MeleeWeaponIcons");if(atlas){float aspect=atlas.width/(5f*atlas.height);float portraitWidth=Mathf.Min(r.width,r.height*aspect);float portraitHeight=Mathf.Min(r.height,r.width/aspect);portrait=new Rect(r.center.x-portraitWidth*.5f,r.center.y-portraitHeight*.5f,portraitWidth,portraitHeight);}}
                ManufacturerPortraits.Melee(portrait,w);score=MeleeWeaponCatalog.Score(w);}
            else {ManufacturerBranding.PortraitBadgeEnabled=previousBadge;return;}
            ManufacturerBranding.PortraitBadgeEnabled=previousBadge;
            string maker=item is Gun dg?ManufacturerBranding.Maker(dg):item is ArmorItem da?ManufacturerBranding.Maker(da):item is ArtifactItem dm?ManufacturerBranding.Maker(dm):item is MeleeWeaponItem dw?ManufacturerBranding.Maker(dw):null;
            if(!string.IsNullOrEmpty(maker))ManufacturerBranding.Badge(detailed
                ?new Rect(r.x-22,r.y+53,190,62)
                :new Rect(r.x-10,r.y+50,82,52),maker);
            var style=new GUIStyle(eqCopy){font=this.font,fontStyle=FontStyle.Normal,
                fontSize=r.width>200?29:24,alignment=TextAnchor.LowerRight,wordWrap=false};
            string value=score.ToString("N0");
            while(style.fontSize>16&&style.CalcSize(new GUIContent(value)).x>r.width-18)style.fontSize--;
            float width=Mathf.Min(r.width-8,style.CalcSize(new GUIContent(value)).x+14);
            var badge=new Rect(r.xMax-width-5,r.yMax-style.fontSize-13,width,style.fontSize+9);

            EqText(new Rect(badge.x+1,badge.y+1,badge.width-5,badge.height-2),value,new Color(0,0,0,.9f),style);
            EqText(new Rect(badge.x,badge.y,badge.width-5,badge.height-2),value,EqInk,style);
        }
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
            int copyFontSize=MobileTouchControls.Enabled?31:23;
            eqCopy=new GUIStyle(label){fontSize=copyFontSize,fontStyle=FontStyle.Normal,wordWrap=true,clipping=TextClipping.Clip,richText=true};eqCaption=new GUIStyle(eqCopy){font=this.font,fontSize=MobileTouchControls.Enabled?30:27,fontStyle=FontStyle.Normal};eqButtonStyle=new GUIStyle(centered){fontSize=copyFontSize,fontStyle=FontStyle.Normal,wordWrap=true,clipping=TextClipping.Clip};eqButtonStyle.normal.textColor=Color.white;eqButtonStyle.hover.textColor=Color.white;eqButtonStyle.active.textColor=Color.white;
            if(eqLastMerchant!=merchant||eqLastSelling!=shopSellingPage){eqSelectedKey=null;eqShowFullDetails=false;eqPendingDiscard=null;eqGridScroll=eqDetailScroll=Vector2.zero;eqLastMerchant=merchant;eqLastSelling=shopSellingPage;if(!merchant)eqUpgradePage=false;}
            LoadJunk();bool eqPreviousEnabled=GUI.enabled;GUI.enabled=eqPreviousEnabled&&string.IsNullOrEmpty(pendingSaleKey)&&eqPendingDiscard==null&&!confirmJunkSale;
            Box(new Rect(0,0,1440,900),EqCanvas);
            Box(new Rect(28,12,44,3),EqAccent);
            Box(new Rect(28,103,1384,1),EqLine);
            EqText(new Rect(28,18,950,44),merchant?T("SERA / SALVAGE EXCHANGE","セラ / 回収品交換所"):T("INVENTORY","インベントリ"),EqInk,new GUIStyle(eqCaption){fontSize=34});
            if(merchant)EqText(new Rect(28,66,900,44),T("SCRAP ","所持スクラップ ")+Credits.ToString("N0"),EqMuted);
            if(EqButton(new Rect(1172,16,240,76),T("CLOSE","閉じる"))){if(merchant){screen=Screen.Hub;pendingSaleKey=null;}else ToggleInventory();return true;}
            if(merchant)
            {
                if(EqNavigation(new Rect(28,112,212,72),T("BUY","購入"),!shopSellingPage&&!eqUpgradePage)){shopSellingPage=false;eqUpgradePage=false;pendingSaleKey=null;}
                if(EqNavigation(new Rect(250,112,212,72),T("SELL","売却"),shopSellingPage&&!eqUpgradePage)){shopSellingPage=true;eqUpgradePage=false;pendingSaleKey=null;}
                if(EqNavigation(new Rect(472,112,252,72),T("CAPACITY","所持枠拡張"),eqUpgradePage)){eqUpgradePage=true;pendingSaleKey=null;}
                if(shopSellingPage&&!eqUpgradePage&&EqButton(new Rect(730,112,220,72),T("SELL JUNK","不要品売却")))confirmJunkSale=true;
                if(EqButton(new Rect(958,112,454,72),T("DEBT / SKILLS","返済 / スキル"))){debtPage=true;debtSkillTreePage=false;return true;}
                if(eqUpgradePage){DrawEquipmentCapacityShop();GUI.enabled=eqPreviousEnabled;return true;}
            }
            float top=merchant?196:122;
            string[] tabs={T("GUNS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接")};
            int[] owned={Weapons.Count,ActiveArmorCount,ArtifactInventory.Count,MeleeWeaponInventory.Count};
            int[] limits={WeaponCapacity,ArmorCapacity,ArtifactCapacity,MeleeWeaponCapacity};
            for(int i=0;i<tabs.Length;i++)tabs[i]+="\n"+owned[i]+" / "+limits[i];
            var previousTabStyle=eqButtonStyle;eqButtonStyle=new GUIStyle(eqButtonStyle){fontSize=MobileTouchControls.Enabled?21:19,wordWrap=false};
            for(int i=0;i<4;i++)if(EqNavigation(new Rect(28+i*112,top,106,74),tabs[i],eqCategory==i)){eqCategory=i;saleCategory=i;eqSelectedKey=null;eqShowFullDetails=false;pendingSaleKey=null;eqGridScroll=eqDetailScroll=Vector2.zero;}
            eqButtonStyle=previousTabStyle;
            float gridTop=top+86;
            Box(new Rect(482,top,1,778-top),EqLine);
            Box(new Rect(926,top,1,778-top),EqLine);
            FieldUIArt.Plate(new Rect(940,top+9,466,769-top),false);
            Box(new Rect(950,620,446,1),EqLine);
            DrawEquipmentHeroPreview(new Rect(494,top+8,422,merchant?452:526));
            DrawEquippedCategory(top);
            if(!merchant)
            {
                var previousActionStyle=eqButtonStyle;
                var actionStyle=new GUIStyle(eqButtonStyle){wordWrap=false};
                string actionCaption=T("ACTION SKILL","アクションスキル");
                while(actionStyle.fontSize>14&&actionStyle.CalcSize(new GUIContent(actionCaption)).x>220)actionStyle.fontSize--;
                eqButtonStyle=actionStyle;
                if(EqButton(new Rect(650,24,240,60),actionCaption))actionSkillInventoryOpen=true;
                eqButtonStyle=previousActionStyle;
            }
            if(!merchant&&screen==Screen.Hub&&EqButton(new Rect(900,24,164,60),T("STASH","倉庫")))VaultStorageOpen();
            FieldUIArt.Plate(new Rect(502,652,406,126),false);
            Box(new Rect(518,668,3,92),EqAccent);
            DrawEquippedTotals();
            var cards=EqCards(merchant);var selected=cards.Find(c=>c.key==eqSelectedKey);if(selected==null&&cards.Count>0){selected=cards[0];eqSelectedKey=selected.key;eqDetailScroll=Vector2.zero;}
            float gridHeight=766-gridTop;
            const float rowStep=220;
            eqGridExtent=Mathf.Max(gridHeight,cards.Count*rowStep);
            eqGridScroll=GUI.BeginScrollView(new Rect(36,gridTop+8,432,gridHeight),eqGridScroll,new Rect(0,0,410,eqGridExtent));
            var cardText=new GUIStyle(eqCopy){fontSize=MobileTouchControls.Enabled?30:24};
            for(int i=0;i<cards.Count;i++)
            {
                // IMGUI clips pixels, but does not skip loading offscreen portraits.
                // Only submit visible rows so a full collection cannot load hundreds
                // of large artwork textures on opening this screen.
                if(i*rowStep+210<eqGridScroll.y||i*rowStep>eqGridScroll.y+gridHeight)continue;
                var c=cards[i];Rect r=new Rect(0,i*rowStep,408,210);bool chosen=c.key==eqSelectedKey;
                FieldUIArt.Plate(r,chosen);
                EqPortrait(new Rect(r.x+16,r.y+16,122,122),c.item);
                if(junkKeys.Contains(JunkItemKey(c.item)))
                {
                    if(!junkBinIcon)junkBinIcon=Resources.Load<Texture2D>("Art/UI/junk-bin");
                    if(junkBinIcon)GUI.DrawTexture(new Rect(r.x+108,r.y+6,36,36),junkBinIcon,ScaleMode.ScaleToFit,true);
                }
                Box(new Rect(r.x+16,r.y+150,32,3),c.color);
                string cardName=c.name.Trim().Replace("\n ","\n").Replace("\n　","\n");var nameStyle=new GUIStyle(cardText);
                while(nameStyle.fontSize>20&&nameStyle.CalcHeight(new GUIContent(cardName),244)>118)nameStyle.fontSize--;
                EqText(new Rect(r.x+154,r.y+12,244,118),cardName,EqInk,nameStyle);
                var summaryStyle=new GUIStyle(cardText){fontSize=MobileTouchControls.Enabled?23:24,wordWrap=false};
                while(summaryStyle.fontSize>14&&summaryStyle.CalcSize(new GUIContent(c.summary)).x>244)summaryStyle.fontSize--;
                EqText(new Rect(r.x+154,r.y+139,244,29),c.summary,EqMuted,summaryStyle);
                EqText(new Rect(r.x+154,r.y+170,244,30),c.equipped?"✓ "+T("EQUIPPED","装備中"):c.comparison,EqMuted,new GUIStyle(cardText){fontSize=MobileTouchControls.Enabled?23:24,wordWrap=false});
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)){eqSelectedKey=c.key;eqShowFullDetails=false;eqDetailScroll=Vector2.zero;}
            }
            GUI.EndScrollView();if(cards.Count==0)EqText(new Rect(48,gridTop+30,400,180),T("No items in this category","このカテゴリにはアイテムがありません"),EqMuted);
            if(selected!=null)
            {
                var nameStyle=new GUIStyle(eqCaption);while(nameStyle.fontSize>20&&nameStyle.CalcHeight(new GUIContent(selected.name),440)>84)nameStyle.fontSize--;
                EqText(new Rect(952,top+20,440,94),selected.name,EqInk,nameStyle);
                Box(new Rect(952,top+104,64,3),selected.color);
                EqPortrait(new Rect(978,top+116,390,124),selected.item,true);
                float detailH=362-top;
                string eqDetail=selected.fullDescription;
                float textHeight=eqCopy.CalcHeight(new GUIContent(eqDetail),410)+24;
                eqDetailExtent=Mathf.Max(detailH,textHeight+16);
                eqDetailScroll=GUI.BeginScrollView(new Rect(950,top+248,446,detailH),eqDetailScroll,new Rect(0,0,416,eqDetailExtent));
                EqText(new Rect(2,4,410,textHeight),eqDetail,EqInk);
                GUI.EndScrollView();
                EqActions(selected,merchant);
            }
            for(int i=0;i<ActiveAmmoTypeCount;i++)
            {
                var type=ActiveAmmoTypeAt(i);Rect r=new Rect(28+i*278,790,270,merchant?82:62);
                string shortLabel=AmmoLabel(type);
                string caption=shortLabel+"\n"+AmmoReserve(type)+" / "+AmmoCapacity(type);
                if(merchant)
                {
                    caption+="\n"+T("BUY ","購入 ")+AmmoPackSize(type)+" / "+AmmoPackCost(type).ToString("N0");
                    var previous=eqButtonStyle;
                    var fitted=new GUIStyle(previous){fontSize=MobileTouchControls.Enabled?20:19,wordWrap=false,alignment=TextAnchor.MiddleCenter};
                    string[] lines=caption.Split('\n');
                    while(fitted.fontSize>13)
                    {
                        float widest=0;foreach(string line in lines)widest=Mathf.Max(widest,fitted.CalcSize(new GUIContent(line)).x);
                        if(widest<=r.width-22&&fitted.CalcHeight(new GUIContent(caption),r.width-22)<=r.height-8)break;
                        fitted.fontSize--;
                    }
                    eqButtonStyle=fitted;if(EqButton(r,caption))BuyAmmo(type,1);eqButtonStyle=previous;
                }
                else{Box(new Rect(r.x,r.y,r.width,1),EqLine);EqText(new Rect(r.x+10,r.y+10,r.width-20,r.height-12),caption,EqMuted,new GUIStyle(eqCopy){fontSize=16});}
            }
            if(!merchant)EqText(new Rect(28,864,1384,32),RunBuffSummary(),EqMuted,new GUIStyle(eqCopy){fontSize=18});
            GUI.enabled=eqPreviousEnabled;if(confirmJunkSale)DrawJunkConfirmation();else if(merchant&&!string.IsNullOrEmpty(pendingSaleKey))EqSaleConfirmation();else if(!merchant&&eqPendingDiscard!=null)EqDiscardConfirmation();return true;
        }
        void DrawEquippedTotals()
        {
            var lines=new List<string>{T("CURRENT LOADOUT","現在の装備"),"HP "+maxHp.ToString("0")+T(" / MOVE "," / 移動 ")+EffectiveMoveSpeed.ToString("0.0"),T("DAMAGE REDUCTION ","被ダメージ軽減 ")+(DamageReduction*100).ToString("0")+"%"};
            float health=EquippedWeaponAffixBonus("health")+ArtifactAffixBonus("health");
            float damage=ArtifactAffixBonus("damage"),fire=ArtifactAffixBonus("fireRate"),reload=ArtifactAffixBonus("reload"),accuracy=ArtifactAffixBonus("accuracy");
            float move=EquippedWeaponAffixBonus("moveSpeed")+ArtifactAffixBonus("moveSpeed"),mitigation=EquippedWeaponAffixBonus("mitigation")+ArtifactAffixBonus("mitigation"),regen=EquippedWeaponAffixBonus("regen")+ArtifactAffixBonus("regen");
            float dodge=0,ammoBonus=ArtifactAffixBonus("ammo"),critChance=ArtifactAffixBonus("critChance"),critDamage=ArtifactAffixBonus("critDamage");
            foreach(var armor in EquippedArmor)if(Armory.IsActiveArmor(armor))
            {health+=armor.maxHealth;damage+=armor.damageBonus;fire+=armor.fireBonus;reload+=armor.reloadBonus;accuracy+=armor.accuracyBonus;move+=armor.moveBonus;mitigation+=armor.damageReduction;regen+=armor.regenBonus;dodge+=armor.dodgeBonus;ammoBonus+=armor.ammoBonus;critChance+=GearScoring.AffixTotal(armor.affixes,"critChance");critDamage+=GearScoring.AffixTotal(armor.affixes,"critDamage");}
            void Add(string en,string ja,float value,bool percent=true){if(Mathf.Abs(value)>.0001f)lines.Add(T(en,ja)+" +"+(percent?value*100:value).ToString(percent?"0.#":"0.#")+(percent?"%":""));}
            Add("GEAR HP","装備HP",health,false);Add("DAMAGE","威力",damage);Add("FIRE RATE","連射速度",fire);Add("RELOAD","リロード速度",reload);Add("ACCURACY","命中精度",accuracy);Add("MOVE","移動速度",move);Add("MITIGATION","被ダメージ軽減",mitigation);Add("REGEN","HP回復",regen,false);Add("DODGE","回避",dodge);Add("AMMO","弾数",ammoBonus);Add("CRIT CHANCE","会心率",critChance);Add("CRIT DAMAGE","会心倍率",critDamage);
            var style=new GUIStyle(eqCopy){fontSize=MobileTouchControls.Enabled?19:17};float height=lines.Count*27+12;
            eqLoadoutScroll=GUI.BeginScrollView(new Rect(532,660,364,111),eqLoadoutScroll,new Rect(0,0,336,Mathf.Max(111,height)));
            for(int i=0;i<lines.Count;i++)EqText(new Rect(0,i*27,334,26),lines[i],EqInk,style);
            GUI.EndScrollView();
        }
        void DrawEquippedCategory(float top)
        {
            // Keep the character's central silhouette clear; portraits use the
            // unused margins and always follow the selected equipment tab.
            var items=new List<object>();var labels=new List<string>();
            if(eqCategory==0)for(int i=0;i<Mathf.Min(2,AvailableWeaponSlotCount);i++){items.Add(WeaponInSlot(i));labels.Add(T("SLOT ","銃枠 ")+(i+1));}
            else if(eqCategory==1)for(int i=1;i<EquippedArmor.Length;i++){items.Add(EquippedArmor[i]);labels.Add(Armory.SlotName((ArmorSlot)i));}
            else if(eqCategory==2)for(int i=0;i<EquippedArtifacts.Length;i++){items.Add(EquippedArtifacts[i]);labels.Add(T("MODULE ","モジュール ")+(i+1));}
            else {items.Add(EquippedMeleeWeapon);labels.Add(T("MELEE","近接武器"));}
            for(int i=0;i<items.Count;i++)
            {
                float x=i%2==0?502:814,y=top+82+(i/2)*134;
                var r=new Rect(x,y,94,122);Box(r,new Color(.08f,.085f,.09f,.92f));
                EqText(new Rect(x+2,y+2,90,34),labels[i],EqMuted,new GUIStyle(eqCopy){fontSize=14,alignment=TextAnchor.UpperCenter});
                if(items[i]!=null)EqPortrait(new Rect(x+3,y+36,88,82),items[i]);
                else EqText(new Rect(x+3,y+44,88,60),T("EMPTY","未装備"),EqMuted,new GUIStyle(eqCopy){fontSize=14,alignment=TextAnchor.MiddleCenter});
            }
        }
        void DrawEquipmentCapacityShop()
        {
            EqText(new Rect(28,204,1320,46),T("PERMANENT CAPACITY UPGRADES","恒久的な所持上限の拡張"),EqInk,new GUIStyle(eqCaption){fontSize=29});
            StorageUpgrade[] categories={StorageUpgrade.AmmoHandgun,StorageUpgrade.AmmoShotgun,StorageUpgrade.AmmoSniper,StorageUpgrade.AmmoRifle,StorageUpgrade.AmmoLMG,StorageUpgrade.Weapons,StorageUpgrade.Armor,StorageUpgrade.Modules,StorageUpgrade.Melee,StorageUpgrade.Stash};
            string[] labels={T("HANDGUN AMMO","ハンドガン弾"),T("SHOTGUN AMMO","ショットガン弾"),T("SNIPER RIFLE AMMO","スナイパーライフル弾"),T("ASSAULT RIFLE AMMO","アサルトライフル弾"),T("HEAVY MACHINE GUN AMMO","ヘビーマシンガン弾"),T("FIREARMS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接武器"),T("STASH","倉庫")};
            string[] values={T("Handgun reserve","ハンドガン弾の上限"),T("Shotgun reserve","ショットガン弾の上限"),T("Sniper rifle reserve","スナイパーライフル弾の上限"),T("Assault rifle reserve","アサルトライフル弾の上限"),T("Heavy machine gun reserve","ヘビーマシンガン弾の上限"),T("Weapon inventory","銃の所持枠"),T("Armor inventory","防具の所持枠"),T("Module inventory","モジュールの所持枠"),T("Melee inventory","近接武器の所持枠"),T("Stash capacity","倉庫の所持枠")};
            for(int i=0;i<categories.Length;i++)
            {
                var category=categories[i];int tier=StorageTier(category),cost=StorageUpgradeCost(category);
                int column=i%3,row=i/3;float x=28+column*465,y=310+row*140;
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
            string key=JunkItemKey(c.item);
            if(EqButton(new Rect(1180,710,216,70),junkKeys.Contains(key)?T("UNMARK","不要解除"):T("JUNK","不要")))ToggleJunk(key);
            EqEquipActions(c,630,true);
        }
        void EqEquipActions(EqCard c,float y,bool discard)
        {
            if(c.item is Gun g){for(int slot=0;slot<AvailableWeaponSlotCount;slot++){int index=slot;if(EqButton(new Rect(950+slot*112,y,106,72),T("SLOT ","枠 ")+(slot+1),EquippedWeaponSlot(g)==slot))EquipWeapon(Weapons.IndexOf(g),index);}if(discard&&!c.equipped&&Weapons.Count>1&&EqButton(new Rect(950,y+80,discard?220:446,70),T("DISCARD","破棄")))QueueDiscard(c);}
            else if(c.item is ArmorItem a){if(EqButton(new Rect(950,y,446,72),c.equipped?T("REMOVE","外す"):T("EQUIP","装備"))){if(c.equipped)UnequipArmor((int)a.slot);else EquipArmor(a);}if(discard&&!c.equipped&&EqButton(new Rect(950,y+80,discard?220:446,70),T("DISCARD","破棄")))QueueDiscard(c);}
            else if(c.item is ArtifactItem m){int current=Array.IndexOf(EquippedArtifacts,m);if(!discard&&current>=0){if(EqButton(new Rect(950,y,446,70),T("REMOVE","外す")))UnequipArtifact(current);return;}for(int slot=0;slot<2;slot++)if(EqButton(new Rect(950+slot*226,y,220,72),T("SLOT ","枠 ")+(slot+1),current==slot))EquipArtifact(m,slot);if(current>=0){if(EqButton(new Rect(950,y+80,discard?220:446,70),T("REMOVE","外す")))UnequipArtifact(current);}else if(discard&&EqButton(new Rect(950,y+80,discard?220:446,70),T("DISCARD","破棄")))QueueDiscard(c);}
            else if(c.item is MeleeWeaponItem w){if(!c.equipped&&EqButton(new Rect(950,y,446,72),T("EQUIP","装備")))EquipMeleeWeapon(w);else if(c.equipped)EqText(new Rect(958,y,420,70),T("✓ EQUIPPED","✓ 装備中"),EqAccent);if(discard&&!c.equipped&&MeleeWeaponInventory.Count>1&&EqButton(new Rect(950,y+80,discard?220:446,70),T("DISCARD","破棄"))){QueueDiscard(c);}}
        }
        void EqSaleConfirmation()
        {
            var s=SaleEntries(saleCategory).Find(e=>SaleKey(e)==pendingSaleKey);if(s==null){pendingSaleKey=null;return;}
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.85f));Box(new Rect(310,220,820,450),EqSurface);Box(new Rect(340,228,60,3),EqAccent);
            string copy=T("CONFIRM SALE\n\n","売却の確認\n\n")+s.title+"\n\n"+T("Receive ","売却額 ")+s.price.ToString("N0")+T(" scrap. Cannot be undone."," スクラップ。売却後は戻せません。");EqText(new Rect(340,240,760,295),copy,EqInk);
            if(EqButton(new Rect(340,555,360,86),T("CONFIRM SALE","売却を確定"))){CompleteSale(s);pendingSaleKey=null;}
            if(EqButton(new Rect(730,555,370,86),T("CANCEL","キャンセル")))pendingSaleKey=null;
        }
        void QueueDiscard(EqCard card){eqPendingDiscard=card.item;eqPendingDiscardName=card.name;}
        void EqDiscardConfirmation()
        {
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.85f));Box(new Rect(310,220,820,450),EqSurface);Box(new Rect(340,228,60,3),EqAccent);
            string copy=T("CONFIRM DISCARD\n\n","破棄の確認\n\n")+eqPendingDiscardName+"\n\n"+T("Discard this item? This cannot be undone.","この装備を破棄しますか？ 破棄後は戻せません。");
            EqText(new Rect(340,240,760,295),copy,EqInk);
            if(EqButton(new Rect(340,555,360,86),T("CONFIRM DISCARD","破棄を確定")))
            {
                var item=eqPendingDiscard;eqPendingDiscard=null;eqSelectedKey=null;
                if(item is Gun g){int index=Weapons.IndexOf(g);if(index>=0&&Weapons.Count>1&&EquippedWeaponSlot(g)<0)DiscardWeapon(index);}
                else if(item is ArmorItem a)DiscardArmor(a);
                else if(item is ArtifactItem m)DiscardArtifact(m);
                else if(item is MeleeWeaponItem w&&w!=EquippedMeleeWeapon&&MeleeWeaponInventory.Count>1&&MeleeWeaponInventory.Remove(w))SaveInventory();
            }
            if(EqButton(new Rect(730,555,370,86),T("CANCEL","キャンセル")))eqPendingDiscard=null;
        }
    }
}



