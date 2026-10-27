using UnityEngine;
using System.Collections.Generic;

namespace NeonVault
{
    public enum ArmorSlot { Head, Chest, Arms, Legs, Feet }
    [System.Serializable]
    public class ArmorItem
    {
        public string id, name, subtitle;
        public string catalogId, manufacturerId, legendaryKey;
        public int designVariant;
        public int balanceVersion;
        public ArmorSlot slot;
        public Rarity rarity;
        public int level=1, visualVariant, dungeon, uniqueEffect;
        public float damageReduction, maxHealth, damageBonus, fireBonus, moveBonus, reloadBonus, dodgeBonus, regenBonus, accuracyBonus, ammoBonus;
        public GearAffix[] affixes;
        public Color color;
    }
    public static class Armory
    {
        public static bool IsActiveSlot(ArmorSlot slot)=>(int)slot>=1&&(int)slot<=4;
        public static bool IsActiveArmor(ArmorItem item)=>item!=null&&IsActiveSlot(item.slot);
        public static ArmorItem Starter(ArmorSlot slot)=>Create(slot,Rarity.Common,1,null);
        public static ArmorItem TrailWarden(ArmorSlot slot)
        {return CreateCatalog(ArmorProductCatalog.Get(slot,4,0).id,Rarity.Common,1);}
        public static ArmorItem CreateCatalog(string catalogId,Rarity rarity,int level,System.Random random=null)
        {
            var product=ArmorProductCatalog.Find(catalogId);
            if(product==null)throw new System.ArgumentException("Unknown armor catalog ID",nameof(catalogId));
            return Create(product.slot,rarity,level,random,product);
        }
        public static ArmorItem Roll(System.Random random,int depth)
        {
            if(random==null)throw new System.ArgumentNullException(nameof(random));
            return RollForDungeon(random,Mathf.Clamp(depth/4,0,2));
        }
        public static ArmorItem RollForDungeon(System.Random random,int dungeon,bool boss=false)=>CreateForDungeon(random,dungeon,DungeonCatalog.RollRarity(random,boss),DungeonCatalog.ItemLevel(random,dungeon));
        public static ArmorItem CreateForDungeon(System.Random random,int dungeon,Rarity rarity,int level)
        {
            if(rarity==Rarity.Legendary)return LegendaryCatalog.RollArmor(random,level);
            var item=Create((ArmorSlot)random.Next(1,5),rarity,level,random);
            if(rarity==Rarity.Legendary){item.dungeon=dungeon;item.uniqueEffect=dungeon+1;item.name=Name(item,false);}
            return item;
        }
        static ArmorItem Create(ArmorSlot slot,Rarity rarity,int level,System.Random random,ArmorProduct product=null)
        {
            var recipe=product==null?null:DistinctEquipmentDesigns.Find(product.id);if(recipe!=null&&recipe.legendary)rarity=Rarity.Legendary;
            float power=1+.16f*((int)rarity+Mathf.Max(1,level)-1);
            if(product==null)product=random==null?ArmorProductCatalog.Default(slot):ArmorProductCatalog.Roll(random,slot);
            var a=new ArmorItem{ id=System.Guid.NewGuid().ToString("N"),slot=slot,rarity=rarity,level=level,color=ArmorColor(slot,rarity,random),visualVariant=product.visualVariant,catalogId=product.id,manufacturerId=product.manufacturerId,designVariant=product.designVariant };
            if(recipe!=null&&recipe.legendary)a.legendaryKey=LegendaryCatalog.Key(true,recipe.index);
            a.damageReduction=RollValue(random,(slot==ArmorSlot.Chest?.045f:.018f)*power);
            switch(slot)
            {
                case ArmorSlot.Head:a.maxHealth=RollValue(random,7*power);a.damageBonus=RollValue(random,.025f*power);break;
                case ArmorSlot.Chest:a.maxHealth=RollValue(random,16*power);a.regenBonus=RollValue(random,.12f*power);break;
                case ArmorSlot.Arms:a.damageBonus=RollValue(random,.04f*power);a.fireBonus=RollValue(random,.035f*power);a.reloadBonus=RollValue(random,.04f*power);break;
                case ArmorSlot.Legs:a.maxHealth=RollValue(random,9*power);a.moveBonus=RollValue(random,.035f*power);a.dodgeBonus=RollValue(random,.016f*power);break;
                case ArmorSlot.Feet:a.moveBonus=RollValue(random,.055f*power);a.dodgeBonus=RollValue(random,.025f*power);break;
            }
            ArmorProductCatalog.Apply(a,power);
            GearBalance.RebalanceArmorBase(a,random);
            a.damageReduction=Mathf.Clamp(a.damageReduction,0,.3f);
            RollAffixes(a,random);
            a.dodgeBonus=Mathf.Clamp(a.dodgeBonus,0,.25f);
            a.subtitle=Description(a,false);
            a.name=Name(a,false);
            return a;
        }
        static void RollAffixes(ArmorItem item,System.Random random)
        {
            if(random==null){item.affixes=System.Array.Empty<GearAffix>();return;}
            int count=Mathf.Clamp((int)item.rarity,0,4);string[] types={"damage","health","fireRate","reload","accuracy","mitigation","moveSpeed","regen","ammo","critChance","critDamage"};
            float scale=1+.16f*((int)item.rarity+item.level-1);var used=new HashSet<int>();item.affixes=new GearAffix[count];
            for(int i=0;i<count;i++){int p;do{p=random.Next(types.Length);}while((p==7&&random.NextDouble()>=.15)||!used.Add(p));float[] ranges={.03f,5,.03f,.03f,.02f,.01f,.02f,.2f,.06f,.015f,.06f};item.affixes[i]=new GearAffix{type=types[p],value=ranges[p]*scale*Arsenal.Range(random,.8f,1.2f)};ApplyAffix(item,item.affixes[i]);}
        }
        static void ApplyAffix(ArmorItem a,GearAffix x){switch(x.type){case "damage":a.damageBonus+=x.value;break;case "health":a.maxHealth+=x.value;break;case "fireRate":a.fireBonus+=x.value;break;case "reload":a.reloadBonus+=x.value;break;case "accuracy":a.accuracyBonus+=x.value;break;case "ammo":a.ammoBonus+=x.value;break;case "mitigation":a.damageReduction=Mathf.Min(.65f,a.damageReduction+x.value);break;case "moveSpeed":a.moveBonus+=x.value;break;case "regen":a.regenBonus+=x.value;break;}}
        static Color ArmorColor(ArmorSlot slot,Rarity rarity,System.Random random)
        {
            Color[] metal={new Color(.31f,.32f,.24f),new Color(.24f,.25f,.2f),new Color(.36f,.25f,.16f),new Color(.29f,.3f,.25f),new Color(.24f,.2f,.17f)};
            Color tint=Color.Lerp(metal[(int)slot],new Color(.48f,.3f,.16f),.06f+(int)rarity*.07f);
            return tint*(random==null?1:Arsenal.Range(random,.82f,1.12f));
        }
        static float RollValue(System.Random r,float value)=>value*(r==null?1:Arsenal.Range(r,.78f,1.25f));
        public static string SlotName(ArmorSlot slot,bool? japanese=null)=>(japanese??VaultLanguage.Japanese)?new[]{"頭","胴","腕","脚","足"}[(int)slot]:new[]{"HEAD","CHEST","ARMS","LEGS","FEET"}[(int)slot];
        public static string Name(ArmorItem item,bool? japanese=null)
        {
            bool ja=japanese??VaultLanguage.Japanese;
            if(LegendaryCatalog.Index(item.legendaryKey)>=0)return LegendaryCatalog.ArmorName(item,ja);
            var displayRecipe=DistinctEquipmentDesigns.For(item);if(displayRecipe!=null)return ja?displayRecipe.japanese:displayRecipe.name;
            var product=ArmorProductCatalog.Find(item.catalogId);
            if(product!=null)return ArmorProductCatalog.Name(product,ja)+(item.uniqueEffect>0?(ja?" / 固有装備":" / UNIQUE"):"");
            if(item.visualVariant==4)return ja?"トレイルウォーデン・"+new[]{"防毒ヘルメット","野戦ジャケット","装甲グローブ","野戦パンツ","戦闘ブーツ"}[(int)item.slot]:"TRAIL WARDEN / "+new[]{"FILTER HELMET","FIELD JACKET","ARMORED GLOVES","FIELD TROUSERS","COMBAT BOOTS"}[(int)item.slot];
            int seed=StableNameHash(item);
            string[][] en={new[]{"SENTRY VISOR","BREAKER MASK","NIGHTWATCH HOOD","DUSTWARD HELM","RAVEN FILTER","ASHEN CROWN","BONE-LANTERN FACEPLATE","DEADLINE RESPIRATOR"},new[]{"BULWARK HARNESS","BREACHER SHELL","CINDER CUIRASS","LAST-STAND RIG","IRONBARK VEST","GRAVEWARD PLATE","FALLOUT CARAPACE","REDLINE TORSO"},new[]{"RAIDER GAUNTLETS","ARC-WELDER BRACES","GRAVEGRIP MITTS","RUSTFANG CUFFS","SIEGEHAND BRACERS","SPARK-COIL ARMS","SCAVENGER KNUCKLES","WIDOWMAKER SLEEVES"},new[]{"QUAKE-TREAD GREAVES","DUSTRUNNER LEGS","BREACHER KNEEGUARDS","MIREWALK CUISSES","ASHFALL STRIDERS","IRONMARCH LEGS","RIFTSTEP GREAVES","BONEYARD TREADS"},new[]{"CINDERSTEP BOOTS","ROADWORN TREADS","QUIETUS SABATONS","DUSTDEVIL KICKS","DEADLAND MARCHERS","SHOCKSOLED BOOTS","NIGHTHAWK TREADS","LAST-MILE RUNNERS"}};
            string[][] jaNames={new[]{"斥候のろ過面","破砕工の防毒面","夜警の覆面","塵除け兜","烏羽の呼吸器","灰冠の面頬","骨灯りの面甲","終末線の防毒面"},new[]{"防塁の救命胴衣","突破兵の胸甲","熾火の胸当て","不退転の救命具","鉄樹の胴鎧","墓守の板金服","放射殻の胸甲","赤線のハーネス"},new[]{"略奪者の篭手","アーク溶接腕甲","墓掴みの手甲","錆牙の腕輪","攻城兵の腕甲","火花コイル腕甲","回収屋の拳甲","未亡人の袖甲"},new[]{"震脚の脛当て","塵走りの脚甲","突破兵の膝当て","泥沼歩きの腿甲","灰降りの走脚","鉄行軍の脚甲","裂け目渡りの脛当て","骨捨て場の踏破具"},new[]{"熾火走りの靴","街道歩きのブーツ","静殺の具足","砂嵐蹴りの靴","荒地行軍靴","電撃底のブーツ","鷹目の踏破靴","最後の一里のブーツ"}};
            string[] enTier={"SALVAGE","FIELD","VETERAN","RELIC","MYTHIC"};
            string[] jaTier={"回収品","野戦型","熟練兵用","遺物級","神話級"};
            int tier=Mathf.Clamp((int)item.rarity,0,4),slot=Mathf.Clamp((int)item.slot,0,4),variant=(seed+(int)item.rarity*3+(item.level*7))&7;
            if(item.uniqueEffect>0)
            {
                string[] setsEn={"ROADGUARD SET","FURNACE RUNNER SET","ISOTOPE CONVERTER SET"};
                string[] setsJa={"街道守備装備","炉心走破装備","同位体変換装備"};
                return ja?setsJa[Mathf.Clamp(item.dungeon,0,2)]+" / "+jaNames[slot][variant]:setsEn[Mathf.Clamp(item.dungeon,0,2)]+" / "+en[slot][variant];
            }
            return ja?jaTier[tier]+"・"+jaNames[slot][variant]:enTier[tier]+" / "+en[slot][variant];
        }
        static int StableNameHash(ArmorItem item)
        {
            unchecked { uint h=2166136261;string id=item.id??"";for(int i=0;i<id.Length;i++){h^=id[i];h*=16777619;}if(id.Length==0){h=(uint)((int)item.slot*397^(int)item.rarity*7919^item.level*104729);}return (int)(h^(h>>16)); }
        }
        public static string Description(ArmorItem item,bool? japanese=null)
        {
            bool ja=japanese??VaultLanguage.Japanese;
            if(!string.IsNullOrEmpty(item.legendaryKey))return LegendaryCatalog.Description(item,ja)+"\n"+ArmorProductCatalog.Description(item,ja);
            string productDescription=ArmorProductCatalog.Description(item,ja);
            return productDescription+BaseDescription(item,ja);
        }
        static string BaseDescription(ArmorItem item,bool ja)
        {
            if(item.uniqueEffect>0)return ja?new[]{"撃破障壁：撃破時に障壁+4、待機4秒","疾走回復：ダッシュ時にHP+4、待機5秒","変換：モジュール障壁の持続中に威力+15%"}[Mathf.Clamp(item.dungeon,0,2)]:new[]{"Kill shield: +4 shield on kill, 4s cooldown","Dash recovery: regain 4 HP on dash, 5s cooldown","Conversion: +15% damage while module shield is active"}[Mathf.Clamp(item.dungeon,0,2)];

            return ja?new[]{"照準と生命力を支える頭部装甲","大きく生命力を高める胴部装甲","生命力・火力・装填・連射から2効果の籠手","生命力と移動を支える脚甲","生命力と高い移動速度を支えるブーツ"}[(int)item.slot]:new[]{"Head armor supports accuracy and vitality","Chest armor provides greater health","Gauntlets roll two of health, damage, reload and fire rate","Greaves improve health and movement","Boots improve health and greater movement"}[(int)item.slot];
        }
        public static string Stats(ArmorItem item,bool? japanese=null)
        {
            bool ja=japanese??VaultLanguage.Japanese;
            var values=new List<string>();
            Add(values,ja?"軽減":"Reduction",item.damageReduction,true);
            Add(values,ja?"最大HP":"Max HP",item.maxHealth,false);
            Add(values,ja?"威力":"Damage",item.damageBonus,true);
            Add(values,ja?"連射":"Fire rate",item.fireBonus,true);
            Add(values,ja?"移動":"Move",item.moveBonus,true);
            Add(values,ja?"リロード":"Reload",item.reloadBonus,true);
            Add(values,ja?"命中精度":"Accuracy",item.accuracyBonus,true);
            Add(values,ja?"装弾数":"Ammo",item.ammoBonus,true);
            Add(values,ja?"回避":"Dodge",item.dodgeBonus,true);
            if(item.regenBonus>0)values.Add((ja?"回復":"Regen")+" +"+item.regenBonus.ToString("0.0")+(ja?" HP/秒":" HP/s"));
            values.Add(GearScoring.AffixSummary(item));
            return string.Join(" · ",values);
        }
        static void Add(List<string> values,string label,float value,bool percent)
        {if(Mathf.Abs(value)>.00001f)values.Add(label+(value>0?" +":" ")+(percent?value*100:value).ToString("0.#")+(percent?"%":""));}
    }
}
