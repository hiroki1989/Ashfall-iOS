using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public Rect MobileCityView=>new Rect(40,140,1360,490);
        bool mobileMapEntered;int mobileShopPage,mobileSalePage,mobileCatalogPage,mobileSkillPage;
        GUIStyle MobileCopy=>new GUIStyle(label){fontSize=34,wordWrap=true,clipping=TextClipping.Clip,richText=true};
        void MobileCopyAt(float x,float y,string text,Color color,float width=1000)
        {GUIStyle style=MobileCopy;float height=Mathf.Clamp(style.CalcHeight(new GUIContent(text??""),width)+8,42,145);GUI.color=color;GUI.Label(new Rect(x,y,width,height),text??"",style);GUI.color=Color.white;}
        void DrawMobileCityMap()
        {
            if(!mobileMapEntered){expeditionCityZoom=1.6f;expeditionMapScroll=ExpeditionMap.CityPoint(expeditionMapSelection)*expeditionCityZoom-MobileCityView.size*.5f;mobileMapEntered=true;}
            Box(new Rect(0,0,1440,900),PanelInk);MobileCopyAt(42,25,T("CITY OPERATIONS / drag & pinch","都市作戦図 / ドラッグ・ピンチ"),Gold);
            Rect view=MobileCityView;Vector2 size=ExpeditionMap.CityCanvasSize*expeditionCityZoom;
            expeditionMapScroll=Vector2.Max(Vector2.zero,Vector2.Min(expeditionMapScroll,size-view.size));
            expeditionMapScroll=GUI.BeginScrollView(view,expeditionMapScroll,new Rect(0,0,size.x,size.y));DrawCityAtlas();GUI.EndScrollView();
            var stage=ExpeditionMap.Get(expeditionMapSelection,DebtLevel);bool unlocked=stage.level<=DebtLevel;
            MobileCopyAt(42,640,stage.Name+" / Lv "+stage.level+"  ("+T("YOU ","現在 ")+DebtLevel+")",unlocked?Cyan:Gold);
            MobileCopyAt(42,684,ExpeditionMap.MissionLabel(stage.mission,VaultLanguage.Japanese),Mist,1350);
            bool done=clearedExpeditionStages.Contains(stage.id);
            MobileCopyAt(42,727,(done?T("CLEARED · REPLAY ","クリア済 · 再攻略 "):T("FIRST CLEAR ","初回 "))+ExpeditionRewardDescription(stage.id)+(done?"":" / "+T("MISSION ","任務 ")+MissionRewardDescription(stage.id)),Cyan,1350);
            if(MobileButton(new Rect(40,780,270,110),T("BACK","拠点へ"))){mobileMapEntered=false;screen=Screen.Hub;}
            if(MobileButton(new Rect(330,780,125,110),"−"))SetCityZoom(expeditionCityZoom/1.2f,view.size*.5f);
            if(MobileButton(new Rect(475,780,125,110),"+"))SetCityZoom(expeditionCityZoom*1.2f,view.size*.5f);
            if(MobileButton(new Rect(625,780,260,110),T("FIND SITE","選択地点へ")))expeditionMapScroll=ExpeditionMap.CityPoint(expeditionMapSelection)*expeditionCityZoom-view.size*.5f;
            if(MobileButton(new Rect(910,780,490,110),unlocked?T("DEPLOY","出撃"):T("LEVEL LOCKED","レベル不足"))&&unlocked){mobileMapEntered=false;EnterExpeditionStage(stage.id);}
        }
        void DrawMobileManufacturer()
        {
            Box(new Rect(0,0,1440,900),PanelInk);string maker=ManufacturerCatalog.Id(makerSelection);
            ManufacturerBranding.Badge(new Rect(30,15,105,105),maker);MobileCopyAt(155,35,ManufacturerCatalog.Name(maker,VaultLanguage.Japanese),Gold,660);
            if(MobileButton(new Rect(840,10,125,110),"‹")){makerSelection=(makerSelection+5)%6;mobileCatalogPage=0;BuildManufacturerProducts();}
            if(MobileButton(new Rect(980,10,125,110),"›")){makerSelection=(makerSelection+1)%6;mobileCatalogPage=0;BuildManufacturerProducts();}
            if(MobileButton(new Rect(1120,10,280,110),T("BACK","戻る")))manufacturerRegistry=false;
            string[] tabs={T("COMPANY","会社"),T("GUNS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接")};
            for(int i=0;i<5;i++)if(MobileButton(new Rect(30+i*278,140,265,110),tabs[i])){manufacturerKind=i-1;mobileCatalogPage=0;BuildManufacturerProducts();}
            if(manufacturerKind<0){ManufacturerBranding.Badge(new Rect(70,310,230,230),maker);MobileCopyAt(345,300,ManufacturerCatalog.Description(maker,VaultLanguage.Japanese),Mist,1015);MobileCopyAt(345,460,ManufacturerBranding.Design(maker,VaultLanguage.Japanese),Cyan,1015);return;}
            int pages=Mathf.Max(1,Mathf.CeilToInt(manufacturerProducts.Count/2f));mobileCatalogPage=Mathf.Clamp(mobileCatalogPage,0,pages-1);
            for(int row=0;row<2;row++)
            {
                int index=mobileCatalogPage*2+row;if(index>=manufacturerProducts.Count)break;object item=manufacturerProducts[index];float y=275+row*240;Panel(new Rect(30,y,1370,225),ManufacturerCatalog.Color(maker));
                Rect icon=new Rect(45,y+20,170,170);string name,detail;
                if(item is Gun gun){ItemPortraits.Draw(icon,gun);name=VaultLanguage.GunName(gun);detail=string.IsNullOrEmpty(gun.legendaryKey)?GunNumbers(gun):LegendaryDescription(gun,VaultLanguage.GunDescription(gun));}
                else if(item is ArmorItem armor){ItemPortraits.Draw(icon,armor);name=Armory.Name(armor);detail=string.IsNullOrEmpty(armor.legendaryKey)&&armor.uniqueEffect==0?Armory.Stats(armor):LegendaryDescription(armor,Armory.Description(armor));}
                else if(item is ArtifactItem module){ManufacturerBranding.ModulePortrait(icon,module);name=module.Name;detail=LegendaryDescription(module,module.Description);}
                else{var melee=(MeleeWeaponItem)item;DrawMeleeIcon(icon,melee.family);name=MeleeWeaponCatalog.Name(melee);detail=T("DMG ","威力 ")+melee.damage+" / "+melee.range.ToString("0.0")+"m"+MeleeLegendaryDetail(melee);}
                MobileCopyAt(240,y+15,name,Gold,1125);MobileCopyAt(240,y+83,detail,Mist,1125);
            }
            if(MobileButton(new Rect(30,780,350,110),T("PREVIOUS","前のページ")))mobileCatalogPage=Mathf.Max(0,mobileCatalogPage-1);
            MobileCopyAt(580,810,(mobileCatalogPage+1)+" / "+pages,Cyan,250);
            if(MobileButton(new Rect(1040,780,360,110),T("NEXT","次のページ")))mobileCatalogPage=Mathf.Min(pages-1,mobileCatalogPage+1);
        }
        void DrawMobileShop()
        {
            if(debtPage){DrawMobileDebt();return;}
            Box(new Rect(0,0,1440,900),PanelInk);MobileCopyAt(40,25,T("MARA / SCRAP ","マラ / スクラップ ")+Credits.ToString("N0"),Gold,950);
            if(MobileButton(new Rect(1110,10,290,110),T("SAFEHOUSE","拠点へ"))){screen=Screen.Hub;pendingSaleKey=null;}
            if(MobileButton(new Rect(40,135,410,110),T("BUY EQUIPMENT","装備を購入")))shopSellingPage=false;
            if(MobileButton(new Rect(465,135,410,110),T("SELL EQUIPMENT","装備を売却")))shopSellingPage=true;
            if(MobileButton(new Rect(890,135,510,110),T("DEBT / SKILLS","借金返済 / スキル"))){debtPage=true;debtSkillTreePage=false;return;}
            if(shopSellingPage)
            {
                string[] tabs={T("GUNS","銃"),T("ARMOR","防具"),T("MODULES","モジュール"),T("MELEE","近接")};
                for(int i=0;i<4;i++)if(MobileButton(new Rect(40+i*340,260,325,105),tabs[i])){saleCategory=i;mobileSalePage=0;pendingSaleKey=null;}
                var entries=SaleEntries(saleCategory);int pages=Mathf.Max(1,Mathf.CeilToInt(entries.Count/2f));mobileSalePage=Mathf.Clamp(mobileSalePage,0,pages-1);
                for(int row=0;row<2;row++)
                {int index=mobileSalePage*2+row;if(index>=entries.Count)break;var entry=entries[index];float y=380+row*177;Color rarity=MobileSaleRarity(entry);Panel(new Rect(40,y,1360,165),rarity);DrawMobileSalePortrait(entry,new Rect(56,y+13,138,138));MobileCopyAt(215,y+10,entry.title,rarity,770);MobileCopyAt(215,y+51,T("SCORE ","評価 ")+MobileSaleScore(entry)+" / "+MobileSaleRarityName(entry),Mist,770);MobileCopyAt(215,y+85,MobileSaleLegendaryDetail(entry),Mist,770);
                    if(!entry.equipped&&!entry.lastRequired&&MobileButton(new Rect(1010,y+22,360,115),T("SELL ","売却 ")+entry.price))pendingSaleKey=SaleKey(entry);
                    else if(entry.equipped||entry.lastRequired)MobileCopyAt(1025,y+50,T("PROTECTED","売却不可"),Cyan,300);
                }
                if(MobileButton(new Rect(40,780,370,110),T("PREVIOUS","前へ")))mobileSalePage=Mathf.Max(0,mobileSalePage-1);
                if(MobileButton(new Rect(1030,780,370,110),T("NEXT","次へ")))mobileSalePage=Mathf.Min(pages-1,mobileSalePage+1);
                if(!string.IsNullOrEmpty(pendingSaleKey))
                {
                    var entry=entries.Find(e=>SaleKey(e)==pendingSaleKey);Box(new Rect(0,0,1440,900),new Color(0,0,0,.9f));MobileCopyAt(200,260,entry==null?T("Unavailable","対象なし"):entry.title,Gold,1000);
                    if(MobileButton(new Rect(180,450,500,130),T("CONFIRM SALE","売却を確定"))){if(entry!=null)CompleteSale(entry);pendingSaleKey=null;}
                    if(MobileButton(new Rect(740,450,500,130),T("CANCEL","キャンセル")))pendingSaleKey=null;
                }
                return;
            }
            var visibleOffers=new System.Collections.Generic.List<int>();for(int i=0;i<shopStock.Count;i++)if(shopStock[i].armor==null||Armory.IsActiveArmor(shopStock[i].armor))visibleOffers.Add(i);
            mobileShopPage=Mathf.Clamp(mobileShopPage,0,Mathf.Max(0,(visibleOffers.Count-1)/2));
            for(int row=0;row<2;row++)
            {
                int visibleIndex=mobileShopPage*2+row;if(visibleIndex>=visibleOffers.Count)break;int index=visibleOffers[visibleIndex];var o=shopStock[index];float y=270+row*190;Panel(new Rect(40,y,1360,175),Cyan);Rect icon=new Rect(52,y+12,150,150);
                string name=o.gun!=null?VaultLanguage.GunName(o.gun):o.armor!=null?Armory.Name(o.armor):o.artifact!=null?o.artifact.Name:MeleeWeaponCatalog.Name(o.melee);
                if(o.gun!=null)ItemPortraits.Draw(icon,o.gun);else if(o.armor!=null)ItemPortraits.Draw(icon,o.armor);else if(o.artifact!=null)ManufacturerBranding.ModulePortrait(icon,o.artifact);else DrawMeleeIcon(icon,o.melee.family);
                MobileCopyAt(225,y+18,name,Gold,760);MobileCopyAt(225,y+91,T("PRICE ","価格 ")+ShopOfferPrice(o),Cyan,760);
                if(MobileButton(new Rect(1040,y+34,320,110),o.purchased?T("SOLD","購入済"):T("BUY","購入"))&&!o.purchased)PurchaseShop(index);
            }
            if(MobileButton(new Rect(40,650,240,110),T("PREV","前へ")))mobileShopPage=Mathf.Max(0,mobileShopPage-1);
            if(MobileButton(new Rect(300,650,240,110),T("NEXT","次へ")))mobileShopPage=Mathf.Min(Mathf.Max(0,(visibleOffers.Count-1)/2),mobileShopPage+1);
            for(int i=0;i<ActiveAmmoTypeCount;i++){var ammoType=ActiveAmmoTypeAt(i);if(MobileButton(new Rect(40+i*274,780,260,110),AmmoLabel(ammoType)+"\n"+AmmoPackCost(ammoType)))BuyAmmo(ammoType,1);}
        }
        string MobileSaleLegendaryDetail(SaleEntry entry)
        {
            if(entry.kind==SaleKind.Gun){var item=Weapons.Find(x=>x.id==entry.id);if(item!=null&&!string.IsNullOrEmpty(item.legendaryKey))return LegendaryDescription(item,VaultLanguage.GunDescription(item));}
            if(entry.kind==SaleKind.Armor){var item=ArmorInventory.Find(x=>x.id==entry.id);if(item!=null&&(!string.IsNullOrEmpty(item.legendaryKey)||item.uniqueEffect>0))return LegendaryDescription(item,Armory.Description(item));}
            if(entry.kind==SaleKind.Module){var item=ArtifactInventory.Find(x=>x.id==entry.id);if(item!=null&&(item.rarity==Rarity.Legendary||item.uniqueEffect>0))return LegendaryDescription(item,item.Description);}
            return entry.detail;
        }
        Color MobileSaleRarity(SaleEntry entry)
        {
            int tier=0;
            switch(entry.kind)
            {
                case SaleKind.Gun: var gun=Weapons.Find(x=>x.id==entry.id);if(gun!=null)tier=(int)gun.rarity;break;
                case SaleKind.Armor: var armor=ArmorInventory.Find(x=>x.id==entry.id);if(armor!=null)tier=(int)armor.rarity;break;
                case SaleKind.Module: var module=ArtifactInventory.Find(x=>x.id==entry.id);if(module!=null)tier=(int)module.rarity;break;
                case SaleKind.Melee: var melee=MeleeWeaponInventory.Find(x=>x.id==entry.id);if(melee!=null)tier=(int)melee.rarity;break;
            }
            return Arsenal.Colors[Mathf.Clamp(tier,0,Arsenal.Colors.Length-1)];
        }
        string MobileSaleRarityName(SaleEntry entry)
        {
            switch(entry.kind)
            {
                case SaleKind.Gun: var gun=Weapons.Find(x=>x.id==entry.id);return VaultLanguage.RarityName(gun==null?Rarity.Common:gun.rarity);
                case SaleKind.Armor: var armor=ArmorInventory.Find(x=>x.id==entry.id);return VaultLanguage.RarityName(armor==null?Rarity.Common:armor.rarity);
                case SaleKind.Module: var module=ArtifactInventory.Find(x=>x.id==entry.id);return VaultLanguage.RarityName(module==null?Rarity.Common:module.rarity);
                default: var melee=MeleeWeaponInventory.Find(x=>x.id==entry.id);return VaultLanguage.RarityName(melee==null?Rarity.Common:melee.rarity);
            }
        }
        int MobileSaleScore(SaleEntry entry)
        {
            switch(entry.kind)
            {
                case SaleKind.Gun: var gun=Weapons.Find(x=>x.id==entry.id);return gun==null?0:GearScoring.Score(gun);
                case SaleKind.Armor: var armor=ArmorInventory.Find(x=>x.id==entry.id);return armor==null?0:GearScoring.Score(armor);
                case SaleKind.Module: var module=ArtifactInventory.Find(x=>x.id==entry.id);return module==null?0:GearScoring.Score(module);
                default: var melee=MeleeWeaponInventory.Find(x=>x.id==entry.id);return melee==null?0:MeleeWeaponCatalog.Score(melee);
            }
        }
        void DrawMobileSalePortrait(SaleEntry entry,Rect rect)
        {
            switch(entry.kind)
            {
                case SaleKind.Gun: var gun=Weapons.Find(x=>x.id==entry.id);if(gun!=null)ItemPortraits.Draw(rect,gun);break;
                case SaleKind.Armor: var armor=ArmorInventory.Find(x=>x.id==entry.id);if(armor!=null)ItemPortraits.Draw(rect,armor);break;
                case SaleKind.Module: var module=ArtifactInventory.Find(x=>x.id==entry.id);if(module!=null)ManufacturerBranding.ModulePortrait(rect,module);break;
                case SaleKind.Melee: var melee=MeleeWeaponInventory.Find(x=>x.id==entry.id);if(melee!=null)DrawMeleeIcon(rect,melee.family);break;
            }
        }
        void DrawMobileDebt()
        {
            Box(new Rect(0,0,1440,900),PanelInk);MobileCopyAt(40,25,T("DEBT ","残債 ")+DebtRemaining.ToString("N0")+" / Lv "+DebtLevel,Gold,1050);
            if(MobileButton(new Rect(1110,10,290,110),T("BACK","戻る"))){debtPage=false;debtSkillTreePage=false;return;}
            MobileCopyAt(40,155,T("WALLET ","所持金 ")+Credits.ToString("N0")+T(" / POINTS "," / スキルポイント ")+DebtSkillPoints,Cyan,1300);
            if(!debtSkillTreePage)
            {
                int levelCost=DebtNextLevelRequirement;int remainingToNext=DebtNextLevelRemaining;
                MobileCopyAt(40,205,T("NEXT LEVEL ","次のレベルまで ")+remainingToNext.ToString("N0")+" / "+levelCost.ToString("N0"),Cyan,1000);
                Rect progress=new Rect(40,245,1360,22);Box(progress,new Color(.12f,.16f,.17f));Box(new Rect(progress.x,progress.y,progress.width*DebtProgressRatio,progress.height),new Color(1,.34f,.23f));
                int limit=DebtPaymentLimit;debtPaymentAmount=Mathf.Clamp(debtPaymentAmount,0,limit);
                MobileCopyAt(40,310,T("SELECT PAYMENT  /  ","返済額を選択  /  ")+debtPaymentAmount.ToString("N0")+" / "+limit.ToString("N0"),Cyan,1320);
                if(limit>0)SetDebtPaymentFraction(GUI.HorizontalSlider(new Rect(55,408,1330,60),debtPaymentAmount/(float)limit,0f,1f));
                if(MobileButton(new Rect(40,485,655,110),T("MAX AMOUNT","全額を選択")))debtPaymentAmount=limit;
                if(MobileButton(new Rect(720,485,680,110),T("REPAY SELECTED","指定額を返済"))&&debtPaymentAmount>0)RepaySelectedDebt();
                if(MobileButton(new Rect(40,650,1360,130),T("SKILLS","スキルを確認")))debtSkillTreePage=true;
                return;
            }
            string[] en={"HANDGUN","SHOTGUN","ASSAULT","PRECISION","HEAVY MG"},ja={"ハンドガン","ショットガン","アサルトライフル","スナイパーライフル","ヘビーマシンガン"};
            for(int i=0;i<en.Length;i++)if(MobileButton(new Rect(40+i*277,220,265,110),T(en[i],ja[i]))){debtTrack=i;mobileSkillPage=0;}
            var nodes=debtSkillNodes.FindAll(n=>n.track==en[debtTrack]);mobileSkillPage=Mathf.Clamp(mobileSkillPage,0,Mathf.Max(0,(nodes.Count-1)/2));
            for(int row=0;row<2;row++){int index=mobileSkillPage*2+row;if(index>=nodes.Count)break;var node=nodes[index];float y=350+row*185;Panel(new Rect(40,y,1360,170),Gold);MobileCopyAt(60,y+12,VaultLanguage.Japanese?node.ja:node.en,Gold,930);MobileCopyAt(60,y+65,DebtSkillDescription(node),Mist,930);bool owned=unlockedDebtSkills.Contains(node.id);if(MobileButton(new Rect(1035,y+30,325,110),owned?T("OWNED","取得済"):DebtSkillAvailable(node)?T("UNLOCK","習得"):T("LOCKED","条件不足"))&&!owned)TryUnlockDebtSkill(node.id);}
            if(MobileButton(new Rect(40,780,370,110),T("PREV","前へ")))mobileSkillPage=Mathf.Max(0,mobileSkillPage-1);
            if(MobileButton(new Rect(1030,780,370,110),T("NEXT","次へ")))mobileSkillPage=Mathf.Min((nodes.Count-1)/2,mobileSkillPage+1);
        }
    }
}
