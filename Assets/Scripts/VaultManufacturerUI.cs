using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        bool manufacturerRegistry;int makerSelection,manufacturerKind=-1;Vector2 manufacturerScroll;
        bool companyDragging;Vector2 companyLastPointer;
        readonly List<object> manufacturerProducts=new List<object>();
        public void SetManufacturerReview(int maker,int kind){manufacturerRegistry=true;makerSelection=Mathf.Clamp(maker,0,5);manufacturerKind=kind;manufacturerScroll=Vector2.zero;BuildManufacturerProducts();}
        void BuildManufacturerProducts()
        {
            if(!smoke)EquipmentDiscovery.RecordCurrent(Weapons,ArmorInventory,ArtifactInventory,MeleeWeaponInventory);
            manufacturerProducts.Clear();string id=ManufacturerCatalog.Id(makerSelection);
            if(manufacturerKind==0){for(int i=0;i<Arsenal.CatalogCount;i++){var item=Arsenal.Catalog(i);if(ManufacturerBranding.Maker(item)==id)manufacturerProducts.Add(item);}}
            else if(manufacturerKind==1){foreach(var product in ArmorProductCatalog.All)if(product.manufacturerId==id&&Armory.IsActiveSlot(product.slot))manufacturerProducts.Add(Armory.CreateCatalog(product.id,Rarity.Common,1));}
            else if(manufacturerKind==2){foreach(var product in ModuleProductCatalog.All)if(product.manufacturerId==id)manufacturerProducts.Add(ArtifactCatalog.CreateCatalog(product.id,product.legendary?Rarity.Legendary:Rarity.Common,1));}
            else if(manufacturerKind==3){for(int i=0;i<MeleeWeaponCatalog.CatalogCount;i++){var item=MeleeWeaponCatalog.Catalog(i);if(ManufacturerBranding.Maker(item)==id){manufacturerProducts.Add(MeleeWeaponCatalog.Catalog(i,Rarity.Legendary));manufacturerProducts.Add(item);}}}
            var legendary=new List<object>();var regular=new List<object>();
            foreach(var item in manufacturerProducts){if(CatalogIsLegendary(item))legendary.Add(item);else regular.Add(item);}
            manufacturerProducts.Clear();manufacturerProducts.AddRange(legendary);manufacturerProducts.AddRange(regular);
        }
        static bool CatalogIsLegendary(object item)
        {
            if(item is Gun gun)return !string.IsNullOrEmpty(gun.legendaryKey);
            if(item is ArmorItem armor)return !string.IsNullOrEmpty(armor.legendaryKey)||armor.uniqueEffect>0;
            if(item is ArtifactItem module)return module.rarity==Rarity.Legendary||module.uniqueEffect>0;
            return item is MeleeWeaponItem melee&&melee.uniqueEffect;
        }
        string CatalogEffect(object item)
        {
            if(item is Gun gun)return string.IsNullOrEmpty(gun.legendaryKey)
                ?T("A firearm recovered from the ruined city.","廃きょの街で回収された銃器。")
                :LegendaryRedOpen+LegendaryCatalog.Description(gun,VaultLanguage.Japanese)+LegendaryRedClose;
            if(item is ArmorItem armor)return string.IsNullOrEmpty(armor.legendaryKey)
                ?T("Survival gear built for the dust and debris.","粉じんと瓦礫の中を生き抜くための装備。")
                :LegendaryRedOpen+LegendaryCatalog.Description(armor,VaultLanguage.Japanese)+LegendaryRedClose;
            if(item is ArtifactItem module)
            {
                if(!CatalogIsLegendary(module))return T("A portable circuit assembled from salvage.","回収部品から組み上げた携帯回路。");
                string description=module.Description;
                string marker=T("Legendary circuit ","レジェンダリー回路");
                int start=description.IndexOf(marker,System.StringComparison.Ordinal);
                return start<0?"":LegendaryRedOpen+description.Substring(start)+LegendaryRedClose;
            }
            if(item is MeleeWeaponItem melee)return melee.uniqueEffect
                ?LegendaryRedOpen+MeleeWeaponCatalog.LegendaryDescription(melee)+LegendaryRedClose
                :T("A hand weapon shaped for close quarters.","接近戦に備えて作られた手持ちの刃物。");
            return "";
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
            for(int tab=-1;tab<4;tab++)if(Button(new Rect(44+(tab+1)*195,220,188,42),tab==-1?T("COMPANY PROFILE","企業概要"):tab==0?T("FIREARMS","銃製品"):tab==1?T("ARMOR","防具製品"):tab==2?T("MODULES","モジュール製品"):T("MELEE","近接製品"))){manufacturerKind=tab;manufacturerScroll=Vector2.zero;BuildManufacturerProducts();}
            if(manufacturerKind<0)
            {
                DrawCompanyProfile(selected,new Rect(44,282,1352,550),false);
                return;
            }
            Text(1050,227,ManufacturerCatalog.Name(selected,VaultLanguage.Japanese)+" / "+manufacturerProducts.Count,small,ManufacturerCatalog.Color(selected),365);
            manufacturerScroll=GUI.BeginScrollView(new Rect(44,282,1352,560),manufacturerScroll,new Rect(0,0,1327,Mathf.Max(560,Mathf.CeilToInt(manufacturerProducts.Count/3f)*210)));
            for(int i=0;i<manufacturerProducts.Count;i++)
            {
                float x=i%3*442,y=i/3*210;if(y+202<manufacturerScroll.y||y>manufacturerScroll.y+560)continue;
                var item=manufacturerProducts[i];Panel(new Rect(x,y,430,199),ManufacturerCatalog.Color(selected));
                bool known=EquipmentDiscovery.Known(item);string name="?",detail="?",code="?";
                if(known)
                {
                    if(item is Gun gun)name=VaultLanguage.GunName(gun);
                    else if(item is ArmorItem armor)name=Armory.Name(armor);
                    else if(item is ArtifactItem module)name=module.Name;
                    else name=MeleeWeaponCatalog.Name((MeleeWeaponItem)item);
                    detail=CatalogEffect(item);code=EquipmentDiscovery.Key(item);
                    Rect icon=new Rect(x+9,y+12,118,118);
                    if(item is Gun g)ItemPortraits.Draw(icon,g);
                    else if(item is ArmorItem a)ItemPortraits.Draw(icon,a);
                    else if(item is ArtifactItem m)ManufacturerBranding.ModulePortrait(icon,m);
                    else ManufacturerPortraits.Melee(icon,(MeleeWeaponItem)item);
                }
                else Text(x+42,y+39,"?",new GUIStyle(heading){alignment=TextAnchor.MiddleCenter},Mist,55);
                Text(x+138,y+13,name,new GUIStyle(small){wordWrap=true},Gold,278);
                Text(x+10,y+137,detail,new GUIStyle(tiny){wordWrap=true,richText=true},Mist,407);
                if(known)Text(x+138,y+104,code,tiny,Mist,275);
            }
            GUI.EndScrollView();Text(47,857,T("Product reference / Loot and merchant stock supply these designs. No free equipment is granted here.","製品図鑑 / 各製品は戦利品・商人の在庫から入手できます。"),tiny,Mist,1160);
        }
        void DrawCompanyProfile(string maker,Rect viewport,bool mobile)
        {
            Panel(viewport,ManufacturerCatalog.Color(maker));
            float width=viewport.width-58;
            var copy=new GUIStyle(label){fontSize=mobile?27:22,wordWrap=true,fontStyle=FontStyle.Normal};
            var title=new GUIStyle(label){fontSize=mobile?27:22,wordWrap=false,fontStyle=FontStyle.Normal};
            string[] headings={T("ABOUT","企業概要"),T("OUR HISTORY","沿革"),T("OUR PRODUCTS","製品の特徴")};
            float[] heights=new float[3];float total=150;
            for(int i=0;i<3;i++){heights[i]=Mathf.Max(mobile?70:52,copy.CalcHeight(new GUIContent(ManufacturerCatalog.CompanyOverview(maker,i,VaultLanguage.Japanese)),width-48));total+=45+heights[i]+22;}
            if(mobile)
            {
                var e=Event.current;
                if(e.type==EventType.MouseDown&&viewport.Contains(e.mousePosition)){companyDragging=true;companyLastPointer=e.mousePosition;}
                else if(e.type==EventType.MouseDrag&&companyDragging){manufacturerScroll.y=Mathf.Clamp(manufacturerScroll.y-(e.mousePosition.y-companyLastPointer.y),0,Mathf.Max(0,total-viewport.height));companyLastPointer=e.mousePosition;e.Use();}
                else if(e.type==EventType.MouseUp)companyDragging=false;
            }
            manufacturerScroll=GUI.BeginScrollView(viewport,manufacturerScroll,new Rect(0,0,viewport.width-24,Mathf.Max(viewport.height-10,total)));
            ManufacturerBranding.Badge(new Rect(18,12,mobile?170:150,100),maker);
            Text(190,34,ManufacturerCatalog.Name(maker,VaultLanguage.Japanese),new GUIStyle(label){fontSize=mobile?32:29},ManufacturerCatalog.Color(maker),width-200);
            float y=126;
            for(int i=0;i<3;i++)
            {
                Text(20,y,headings[i],title,ManufacturerCatalog.Color(maker),width-40);y+=42;
                GUI.color=UiPaper;GUI.Label(new Rect(20,y,width-48,heights[i]+5),ManufacturerCatalog.CompanyOverview(maker,i,VaultLanguage.Japanese),copy);GUI.color=Color.white;y+=heights[i]+25;
            }
            GUI.EndScrollView();
        }
    }
}
