using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        enum SaleKind { Gun, Armor, Module, Melee }
        sealed class SaleEntry
        {
            public SaleKind kind; public string id, title, detail; public int price; public bool equipped, lastRequired;
        }
        bool shopSellingPage;
        int saleCategory;
        string pendingSaleKey;
        const string JunkKey="Vault.Junk.v1";
        [System.Serializable] sealed class JunkSave { public List<string> keys=new List<string>(); }
        HashSet<string> junkKeys; bool confirmJunkSale;
        void LoadJunk(){if(junkKeys!=null)return;junkKeys=new HashSet<string>();if(smoke||!PlayerPrefs.HasKey(JunkKey))return;try{var data=JsonUtility.FromJson<JunkSave>(PlayerPrefs.GetString(JunkKey));if(data?.keys!=null)foreach(var key in data.keys)junkKeys.Add(key);}catch(System.Exception e){Debug.LogWarning("Junk tags: "+e.GetType().Name);}}
        void ToggleJunk(string key){LoadJunk();if(!junkKeys.Add(key))junkKeys.Remove(key);SaveJunk();}
        void SaveJunk(){if(smoke)return;PlayerPrefs.SetString(JunkKey,JsonUtility.ToJson(new JunkSave{keys=new List<string>(junkKeys)}));PlayerPrefs.Save();}
        string JunkItemKey(object item)=>item is Gun g?"Gun:"+g.id:item is ArmorItem a?"Armor:"+a.id:item is ArtifactItem m?"Module:"+m.id:item is MeleeWeaponItem w?"Melee:"+w.id:"";
        void DrawJunkConfirmation()
        {
            LoadJunk();var eligible=new List<SaleEntry>();long total=0;
            for(int category=0;category<4;category++)foreach(var e in SaleEntries(category))if(junkKeys.Contains(SaleKey(e))&&!e.equipped&&!e.lastRequired){eligible.Add(e);total+=e.price;}
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.85f));Panel(new Rect(310,220,820,450),Gold);
            Text(350,260,T("SELL MARKED ITEMS","不要品を一括売却"),heading,Gold,740);
            Text(350,340,T("Items: ","対象：")+eligible.Count+T("  /  Scrap: ","点  /  売却額：")+total.ToString("N0"),label,UiPaper,740);
            if(Button(new Rect(350,520,330,80),T("CONFIRM SALE","売却を確定"))){foreach(var e in eligible)if(CompleteSale(e))junkKeys.Remove(SaleKey(e));SaveJunk();confirmJunkSale=false;}
            if(Button(new Rect(740,520,330,80),T("CANCEL","キャンセル")))confirmJunkSale=false;
        }
        Vector2 saleScroll;

        public int SaleValue(Gun item)=>item==null?0:LegendarySaleValue(item.rarity,ScaleScrap(Mathf.Clamp(90000+item.level*35000+(int)item.rarity*180000+GearScoring.Score(item)*1800,100000,5000000)));
        public int SaleValue(ArmorItem item)=>item==null?0:LegendarySaleValue(item.rarity,ScaleScrap(Mathf.Clamp(85000+item.level*32000+(int)item.rarity*160000+GearScoring.Score(item)*1700,90000,4000000)));
        public int SaleValue(ArtifactItem item)=>item==null?0:LegendarySaleValue(item.rarity,ScaleScrap(Mathf.Clamp(70000+item.level*30000+(int)item.rarity*150000+GearScoring.Score(item)*1700,80000,3500000)));
        public int SaleValue(MeleeWeaponItem item)=>item==null?0:LegendarySaleValue(item.rarity,ScaleScrap(Mathf.Clamp(70000+item.level*25000+MeleeWeaponCatalog.Score(item)*1600,80000,2000000)));

        static int LegendarySaleValue(Rarity rarity,int value)=>rarity==Rarity.Legendary?(int)System.Math.Min(int.MaxValue,(long)value*10):value;
        List<SaleEntry> SaleEntries(int category)
        {
            var result=new List<SaleEntry>();
            if(category==0)foreach(var item in Weapons)result.Add(new SaleEntry{kind=SaleKind.Gun,id=item.id,title=VaultLanguage.GunName(item),detail=string.IsNullOrEmpty(item.legendaryKey)?GunNumbers(item)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item):LegendaryDescription(item,VaultLanguage.GunDescription(item)),price=SaleValue(item),equipped=EquippedWeaponSlot(item)>=0,lastRequired=Weapons.Count<=1});
            if(category==1)foreach(var item in ArmorInventory)if(Armory.IsActiveArmor(item))result.Add(new SaleEntry{kind=SaleKind.Armor,id=item.id,title=Armory.Name(item),detail=string.IsNullOrEmpty(item.legendaryKey)&&item.uniqueEffect==0?Armory.SlotName(item.slot)+" / Lv "+item.level+" / "+VaultLanguage.RarityName(item.rarity)+" · "+Armory.Stats(item)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item):LegendaryDescription(item,Armory.Description(item)),price=SaleValue(item),equipped=IsArmorEquipped(item)});
            if(category==2)foreach(var item in ArtifactInventory)result.Add(new SaleEntry{kind=SaleKind.Module,id=item.id,title=item.Name,detail="Lv "+item.level+" / "+VaultLanguage.RarityName(item.rarity)+"\n"+LegendaryDescription(item,item.Description)+"\n"+T("SCORE ","評価 ")+GearScoring.Score(item),price=SaleValue(item),equipped=System.Array.IndexOf(EquippedArtifacts,item)>=0});
            if(category==3)foreach(var item in MeleeWeaponInventory)result.Add(new SaleEntry{kind=SaleKind.Melee,id=item.id,title=MeleeWeaponCatalog.Name(item)+" / Lv "+item.level,detail=T("DMG ","威力 ")+item.damage+" · "+T("RATE ","速度 ")+(1/Mathf.Max(.01f,item.interval)).ToString("0.0")+T("/s · RANGE ","/秒 · 射程 ")+item.effectiveRange.ToString("0.0")+"m · "+T("SCORE ","評価 ")+MeleeWeaponCatalog.Score(item)+MeleeLegendaryDetail(item),price=SaleValue(item),equipped=EquippedMeleeWeapon==item,lastRequired=MeleeWeaponInventory.Count<=1});
            return result;
        }
        bool IsArmorEquipped(ArmorItem item){if(EquippedArmor!=null)foreach(var equipped in EquippedArmor)if(equipped==item)return true;return false;}
        string SaleKey(SaleEntry entry)=>entry.kind+":"+entry.id;

        void DrawShopSelling()
        {
            Box(new Rect(0,0,1440,900),PanelInk);
            Text(65,52,T("MARA / FIELD RECOVERY DESK","マラ / 戦利品買取所"),heading,Gold,1050);
            Text(68,110,T("Scrap wallet ","所持スクラップ ")+Credits.ToString("N0")+T(" · unequip gear before selling · every sale needs confirmation"," · 装備中は売却不可 · 売却前に確認が必要"),small,Cyan,1200);
            if(Button(new Rect(1110,62,225,48),T("BUY STOCK","商品を見る"))){shopSellingPage=false;pendingSaleKey=null;}
            string[] en={"GUNS","ARMOR","MODULES","MELEE"},ja={"銃","防具","モジュール","近接武器"};
            for(int i=0;i<4;i++)if(Button(new Rect(68+i*235,155,215,46),T(en[i],ja[i]),i==saleCategory))saleCategory=i;
            var entries=SaleEntries(saleCategory);float contentHeight=Mathf.Max(516,entries.Count*132);
            saleScroll=GUI.BeginScrollView(new Rect(68,215,1280,530),saleScroll,new Rect(0,0,1240,contentHeight));
            for(int i=0;i<entries.Count;i++)
            {
                var entry=entries[i];float y=i*132;int score=0;Rarity rarity=Rarity.Common;Color accent=Arsenal.Colors[(int)rarity];
                if(entry.kind==SaleKind.Gun){var item=Weapons.Find(w=>w.id==entry.id);if(item!=null){score=GearScoring.Score(item);rarity=item.rarity;accent=item.color;}}
                else if(entry.kind==SaleKind.Armor){var item=ArmorInventory.Find(a=>a.id==entry.id);if(item!=null){score=GearScoring.Score(item);rarity=item.rarity;accent=Arsenal.Colors[(int)rarity];}}
                else if(entry.kind==SaleKind.Module){var item=ArtifactInventory.Find(a=>a.id==entry.id);if(item!=null){score=GearScoring.Score(item);rarity=item.rarity;accent=Arsenal.Colors[(int)rarity];}}
                else {var item=MeleeWeaponInventory.Find(m=>m.id==entry.id);if(item!=null){score=MeleeWeaponCatalog.Score(item);rarity=item.rarity;accent=item.color;}}
                Panel(new Rect(0,y,1240,122),accent);
                if(entry.kind==SaleKind.Gun)ItemPortraits.Draw(new Rect(10,y+10,88,88),Weapons.Find(w=>w.id==entry.id));
                else if(entry.kind==SaleKind.Armor)ItemPortraits.Draw(new Rect(10,y+10,88,88),ArmorInventory.Find(a=>a.id==entry.id));
                else if(entry.kind==SaleKind.Module){var item=ArtifactInventory.Find(a=>a.id==entry.id);if(item!=null)ManufacturerBranding.ModulePortrait(new Rect(10,y+10,88,88),item);}
                else {var melee=MeleeWeaponInventory.Find(m=>m.id==entry.id);if(melee!=null)DrawMeleeIcon(new Rect(25,y+25,58,58),melee.family);}
                Text(108,y+6,entry.title,label,accent,690);
                Text(108,y+34,VaultLanguage.RarityName(rarity)+" · "+T("SCORE ","評価 ")+score,new GUIStyle(tiny),accent,690);
                Text(108,y+55,entry.detail,new GUIStyle(tiny){wordWrap=true},Mist,790);
                if(entry.equipped)Text(940,y+44,T("EQUIPPED / CANNOT SELL","装備中 / 売却不可"),small,Cyan,250);
                else if(entry.lastRequired)Text(940,y+44,T("KEEP LAST ITEM","最後の1本は保持"),small,Mist,230);
                else if(Button(new Rect(950,y+37,260,48),T("SELL FOR ","売却 ")+entry.price.ToString("N0")))pendingSaleKey=SaleKey(entry);
            }
            GUI.EndScrollView();
            Text(70,758,T("Sale value combines item level, rarity and performance score. Equipped items and the last available weapon stay protected.","売却額は装備レベル・レア度・性能スコアで決定。装備中の品と最後の武器は保護されます。"),tiny,Mist,1200);
            if(Button(new Rect(68,820,300,52),T("RETURN TO SAFEHOUSE","拠点へ戻る"))){shopSellingPage=false;pendingSaleKey=null;screen=Screen.Hub;}
            if(!string.IsNullOrEmpty(pendingSaleKey))DrawSaleConfirmation();
        }
        void DrawSaleConfirmation()
        {
            var entry=SaleEntries(saleCategory).Find(e=>SaleKey(e)==pendingSaleKey);
            if(entry==null){pendingSaleKey=null;return;}
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.64f));Panel(new Rect(390,290,660,300),Gold);
            Text(430,326,T("CONFIRM SALE","売却を確認"),heading,Gold,580);
            Text(432,395,entry.title,new GUIStyle(label){wordWrap=true},Color.white,570);
            Text(432,450,T("You will receive ","売却額 ")+entry.price.ToString("N0")+T(" scrap. This cannot be undone.","スクラップ。売却後は戻せません。"),small,Mist,560);
            if(Button(new Rect(432,510,250,52),T("CONFIRM SALE","売却する"),true)){if(CompleteSale(entry))pendingSaleKey=null;else Notify("Sale canceled: item is equipped or unavailable.","売却中止：装備中、または対象が見つかりません。");}
            if(Button(new Rect(712,510,250,52),T("CANCEL","キャンセル")))pendingSaleKey=null;
        }
        bool CompleteSale(SaleEntry entry)
        {
            if(entry==null||entry.equipped||entry.lastRequired)return false;bool removed=false;
            if(entry.kind==SaleKind.Gun){int index=Weapons.FindIndex(w=>w.id==entry.id);if(index>=0&&EquippedWeaponSlot(Weapons[index])<0&&Weapons.Count>1){DiscardWeapon(index);removed=true;}}
            else if(entry.kind==SaleKind.Armor){var item=ArmorInventory.Find(a=>a.id==entry.id);if(Armory.IsActiveArmor(item)&&!IsArmorEquipped(item)){DiscardArmor(item);removed=true;}}
            else if(entry.kind==SaleKind.Module){var item=ArtifactInventory.Find(a=>a.id==entry.id);if(item!=null&&System.Array.IndexOf(EquippedArtifacts,item)<0){DiscardArtifact(item);removed=true;}}
            else if(entry.kind==SaleKind.Melee){var item=MeleeWeaponInventory.Find(m=>m.id==entry.id);if(item!=null&&item!=EquippedMeleeWeapon&&MeleeWeaponInventory.Count>1){MeleeWeaponInventory.Remove(item);SaveInventory();removed=true;}}
            if(removed){AwardCredits(entry.price);PlayUserSound("transaction",.35f);Notify("Sold "+entry.title+" for "+entry.price.ToString("N0")+" scrap.",entry.title+"を"+entry.price.ToString("N0")+"スクラップで売却。");}
            return removed;
        }
    }
}
