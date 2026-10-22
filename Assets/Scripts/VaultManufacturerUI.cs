using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        bool manufacturerRegistry;int makerSelection,manufacturerKind=-1;Vector2 manufacturerScroll;
        readonly List<object> manufacturerProducts=new List<object>();
        public void SetManufacturerReview(int maker,int kind){manufacturerRegistry=true;makerSelection=Mathf.Clamp(maker,0,5);manufacturerKind=kind;manufacturerScroll=Vector2.zero;BuildManufacturerProducts();}
        void BuildManufacturerProducts()
        {
            manufacturerProducts.Clear();string id=ManufacturerCatalog.Id(makerSelection);
            if(manufacturerKind==0){for(int i=0;i<Arsenal.CatalogCount;i++){var item=Arsenal.Catalog(i);if(ManufacturerBranding.Maker(item)==id)manufacturerProducts.Add(item);}}
            else if(manufacturerKind==1){foreach(var product in ArmorProductCatalog.All)if(product.manufacturerId==id&&Armory.IsActiveSlot(product.slot))manufacturerProducts.Add(Armory.CreateCatalog(product.id,Rarity.Common,1));}
            else if(manufacturerKind==2){foreach(var product in ModuleProductCatalog.All)if(product.manufacturerId==id)manufacturerProducts.Add(ArtifactCatalog.CreateCatalog(product.id,product.legendary?Rarity.Legendary:Rarity.Common,1));}
            else if(manufacturerKind==3){for(int i=0;i<MeleeWeaponCatalog.CatalogCount;i++){var item=MeleeWeaponCatalog.Catalog(i);if(ManufacturerBranding.Maker(item)==id)manufacturerProducts.Add(item);}}

        }
        void DrawManufacturerRegistry()
        {
            Box(new Rect(0,0,1440,900),PanelInk);
            Text(44,29,T("THE SIX / FIELD MANUFACTURERS","荒野の6社 / 装備メーカー"),heading,Gold,1160);
            int armorCount=0,legendaryCount=LegendaryCatalog.GunCount;foreach(var product in ArmorProductCatalog.All)if(Armory.IsActiveSlot(product.slot)){armorCount++;if(DistinctEquipmentDesigns.Find(product.id).legendary)legendaryCount++;}
            Text(46,88,T(Arsenal.CatalogCount+" FIREARMS · "+armorCount+" ARMOR · "+ModuleProductCatalog.Count+" MODULES / INCLUDING "+legendaryCount+" NAMED LEGENDARIES", "銃"+Arsenal.CatalogCount+"・防具"+armorCount+"・モジュール"+ModuleProductCatalog.Count+" / 固有伝説"+legendaryCount+"種を含む"),small,Mist,1250);
            if(Button(new Rect(1230,848,165,42),T("BACK","戻る")))manufacturerRegistry=false;
            for(int maker=0;maker<6;maker++)
            {
                string id=ManufacturerCatalog.Id(maker);float x=44+maker*226;
                Panel(new Rect(x,130,218,76),maker==makerSelection?Gold:ManufacturerCatalog.Color(id));
                ManufacturerBranding.Badge(new Rect(x+6,140,54,54),id);
                if(GUI.Button(new Rect(x+64,140,149,54),ManufacturerCatalog.Name(id,VaultLanguage.Japanese),new GUIStyle(centered){fontSize=14,wordWrap=true})){makerSelection=maker;manufacturerScroll=Vector2.zero;BuildManufacturerProducts();}
            }
            string selected=ManufacturerCatalog.Id(makerSelection);
            for(int tab=-1;tab<4;tab++)if(Button(new Rect(44+(tab+1)*195,220,188,42),tab==-1?T("COMPANIES","6社の特徴"):tab==0?T("FIREARMS","銃製品"):tab==1?T("ARMOR","防具製品"):tab==2?T("MODULES","モジュール製品"):T("MELEE","近接製品"))){manufacturerKind=tab;manufacturerScroll=Vector2.zero;BuildManufacturerProducts();}
            if(manufacturerKind<0)
            {
                for(int i=0;i<6;i++)
                {
                    string id=ManufacturerCatalog.Id(i);float x=44+i%3*456,y=284+i/3*278;
                    Panel(new Rect(x,y,440,260),ManufacturerCatalog.Color(id));
                    ManufacturerBranding.Badge(new Rect(x+14,y+16,78,78),id);
                    Text(x+107,y+18,ManufacturerCatalog.Name(id,VaultLanguage.Japanese),new GUIStyle(label){wordWrap=true},ManufacturerCatalog.Color(id),315);
                    Text(x+16,y+110,ManufacturerCatalog.Description(id,VaultLanguage.Japanese),new GUIStyle(small){wordWrap=true},Mist,405);
                    Text(x+16,y+170,ManufacturerBranding.Design(id,VaultLanguage.Japanese),new GUIStyle(small){wordWrap=true},Cyan,405);
                }
                return;
            }
            Text(1050,227,ManufacturerCatalog.Name(selected,VaultLanguage.Japanese)+" / "+manufacturerProducts.Count,small,ManufacturerCatalog.Color(selected),365);
            manufacturerScroll=GUI.BeginScrollView(new Rect(44,282,1352,560),manufacturerScroll,new Rect(0,0,1327,Mathf.Max(560,Mathf.CeilToInt(manufacturerProducts.Count/3f)*210)));
            for(int i=0;i<manufacturerProducts.Count;i++)
            {
                float x=i%3*442,y=i/3*210;if(y+202<manufacturerScroll.y||y>manufacturerScroll.y+560)continue;
                var item=manufacturerProducts[i];Panel(new Rect(x,y,430,199),ManufacturerCatalog.Color(selected));
                string name,detail,code;int score;
                if(item is Gun gun){name=VaultLanguage.GunName(gun);detail=string.IsNullOrEmpty(gun.legendaryKey)?GunNumbers(gun):LegendaryDescription(gun,VaultLanguage.GunDescription(gun));code=gun.catalogId;score=GearScoring.Score(gun);ItemPortraits.Draw(new Rect(x+9,y+12,118,118),gun);}
                else if(item is ArmorItem armor){name=Armory.Name(armor);detail=string.IsNullOrEmpty(armor.legendaryKey)&&armor.uniqueEffect==0?Armory.Stats(armor):LegendaryDescription(armor,Armory.Description(armor));code=armor.catalogId;score=GearScoring.Score(armor);ItemPortraits.Draw(new Rect(x+9,y+12,118,118),armor);}
                else if(item is ArtifactItem module){name=module.Name;detail=LegendaryDescription(module,module.Description);code=module.catalogId;score=GearScoring.Score(module);ManufacturerBranding.ModulePortrait(new Rect(x+9,y+12,118,118),module);}
                else{var melee=(MeleeWeaponItem)item;name=MeleeWeaponCatalog.Name(melee);detail=T("DMG ","威力 ")+melee.damage+" / "+T("INTERVAL ","間隔 ")+melee.interval.ToString("0.00")+" / "+T("RANGE ","射程 ")+melee.range.ToString("0.00")+"m / "+melee.arcDegrees.ToString("0")+"°";code=melee.catalogId;score=MeleeWeaponCatalog.Score(melee);ManufacturerPortraits.Melee(new Rect(x+9,y+12,118,118),melee);ManufacturerBranding.Badge(new Rect(x+14,y+95,27,27),selected);}
                Text(x+138,y+13,name,new GUIStyle(small){wordWrap=true},Gold,278);
                Text(x+138,y+65,T("SCORE ","評価 ")+score+" / Lv 1",tiny,Cyan,275);
                Text(x+10,y+137,detail,new GUIStyle(tiny){wordWrap=true},Mist,407);
                Text(x+138,y+104,code,tiny,Mist,275);
            }
            GUI.EndScrollView();Text(47,857,T("Product reference / Loot and merchant stock supply these designs. No free equipment is granted here.","製品図鑑 / 各製品は戦利品・商人の在庫から入手できます。"),tiny,Mist,1160);
        }
    }
}
