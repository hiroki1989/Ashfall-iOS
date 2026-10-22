using UnityEngine;

namespace NeonVault
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum GunEffect { Normal, Scatter, Ricochet, Orbit, Singularity }
    public enum GunHandling { OneHanded, TwoHanded }
    public enum WeaponClass { Handgun, Shotgun, AssaultRifle, SniperRifle }
    [System.Serializable] public class GearAffix { public string type; public float value; }
    [System.Serializable]
    public class Gun
    {
        public string name, subtitle, id, visualSet;
        public string catalogId, manufacturerId, legendaryKey;
        public int designVariant;
        public int balanceVersion;
        public int level = 1, modelVariant;
        public Rarity rarity;
        public GunEffect effect;
        public float damage, interval, speed, reloadSeconds = 1.15f;
        // Zero in older saves means use the model default; new instances serialize their score.
        public float accuracy;
        public GearAffix[] affixes;
        // Non-weapon bonuses are retained as rolled values so the loadout layer can
        // apply them without hiding them inside firearm damage or magazine stats.
        public float bonusHealth, damageReductionBonus, movementBonus, regenBonus;
        public int magazine, pellets;
        public Color color;
        public Gun(string n, string s, Rarity r, GunEffect e, float d, float i, float v, int m, int p)
        { accuracy=85; name=n; subtitle=s; rarity=r; effect=e; damage=d; interval=i; speed=v; magazine=m; pellets=p; color=Arsenal.Colors[(int)r]; id=System.Guid.NewGuid().ToString("N"); }
    }
    public static class Arsenal
    {
        public static readonly Color[] Colors = { new Color(.7f,.8f,.85f), new Color(.3f,1,.6f), new Color(.25f,.65f,1), new Color(.8f,.35f,1), new Color(1,.65f,.16f) };
        public static readonly string[] ModelNames={"DUST RUNNER","RIVET SMG","LONG WATCH","SCRAP HAMMER","TRENCH SWEEPER","SAWED THUNDER","IRON BELT","NEEDLE DRIVER","COIL LANCE","LEAD SPLITTER","ISOTOPE SWARM","VACUUM ENGINE","FIELD CARBINE","BREACH PUMP","RELIC SIX","SERVICE NINE","BREAKER .50","QUIET NINE"};
        public static readonly string[] ModelJapanese={"ダストランナー","リベットSMG","ロングウォッチ","スクラップハンマー","トレンチスイーパー","ソードサンダー","アイアンベルト","ニードルドライバー","コイルランス","レッドスプリッター","アイソトープスウォーム","真空エンジン","フィールドカービン","ブリーチポンプ","遺産の六輪","九式サービス","大口径ブレーカー","静音ナイン"};
        public static int ModelCount=>ModelNames.Length;
        public const int DesignsPerModel=100;
        public static int CatalogCount=>DistinctEquipmentDesigns.Guns.Length;
        public static int CatalogIndex(Gun gun)=>DistinctEquipmentDesigns.For(gun)!=null?DistinctEquipmentDesigns.For(gun).index:gun==null?0:Mathf.Clamp(gun.modelVariant,0,ModelCount-1)+Mathf.Clamp(gun.designVariant,0,DesignsPerModel-1)*ModelCount;
        static readonly string[] DesignEnglish={"Standard","Scout","Bastion","Cyclone","Sharpsight","Breach","Expedition","Surge","Vanguard","Prototype"};
        static readonly string[] DesignJapanese={"標準型","斥候型","要塞型","旋風型","照準型","突破型","遠征型","急襲型","先鋒型","試作型"};
        public static string DesignName(int design,bool japanese)=>(japanese?DesignJapanese:DesignEnglish)[Mathf.Clamp(design,0,DesignsPerModel-1)%10]+ProductGeneration.Name(design,japanese);
        public static Gun Catalog(int index,Rarity rarity=Rarity.Common)
        {
            return FromRecipe(DistinctEquipmentDesigns.Guns[Mathf.Clamp(index,0,CatalogCount-1)],rarity);
        }
        public static Gun FromRecipe(EquipmentRecipe r,Rarity rarity)
        {
            if(r.legendary)return BespokeLegendaryDesigns.CreateBase(r.index);
            var g=BaseModel(r.family,rarity);g.catalogId=r.id;g.manufacturerId=ManufacturerCatalog.Id(r.maker);g.designVariant=r.index;g.name=r.name;
            GunSeriesDesigns.Apply(g,r);
            ManufacturerCatalog.Apply(g);GearBalance.Migrate(g);if(r.gunClass==1&&r.series==2)g.magazine=6;if(r.gunClass==2&&r.series==1)g.magazine=2;if(r.legendary){g.legendaryKey=LegendaryCatalog.Key(false,r.index);g.name=LegendaryCatalog.GunName(g,false);g.subtitle=LegendaryCatalog.Description(g,false);}return g;
        }
        public static Gun LegacyCatalog(int index,Rarity rarity=Rarity.Common)
        {
            index=Mathf.Clamp(index,0,ModelCount*DesignsPerModel-1);int model=index%ModelCount,design=index/ModelCount;
            if(design==0)return Model(model,rarity);
            int basic=design%10;
            // Recreate the baseline, then apply one design and one manufacturer's tuning.
            var gun=BaseModel(model,rarity);gun.designVariant=design;
            gun.catalogId="gun-"+model.ToString("00")+"-"+design.ToString("00");
            gun.manufacturerId=ManufacturerCatalog.ForDesign(model,design);
            float[] damage={1,.82f,1.35f,.74f,1.2f,.9f,.94f,1.1f,1.18f,1.28f};
            float[] cadence={1,.83f,1.3f,.68f,1.18f,1.12f,1.07f,.87f,1.15f,1.21f};
            float[] capacity={1,.75f,1.2f,1.45f,.7f,.8f,1.65f,.65f,1.1f,.9f};
            float[] reload={1,.74f,1.28f,1.22f,1.08f,.9f,1.35f,.7f,1.1f,1.18f};
            float[] aim={0,4,-7,-10,10,-13,2,-5,5,-3};
            gun.damage*=damage[basic];gun.interval*=cadence[basic];gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*capacity[basic]));
            gun.reloadSeconds*=reload[basic];gun.accuracy=Mathf.Clamp(gun.accuracy+aim[basic],1,100);
            if(basic==5){gun.pellets+=2;gun.damage*=.72f;gun.effect=gun.effect==GunEffect.Normal?GunEffect.Scatter:gun.effect;}
            if(basic==9){gun.pellets++;gun.damage*=.8f;gun.speed*=1.15f;}
            gun.name=ModelNames[model]+" / "+DesignName(design,false);
            gun.subtitle=DesignName(design,false)+" engineering · "+ModelDescription(model);
            ManufacturerCatalog.Apply(gun);ProductGeneration.Apply(gun);GearBalance.Migrate(gun);return gun;
        }
        // Compatibility lookup is creation-only; loading retained items uses serialized stats.
        public static Gun FindCatalog(string id,Rarity rarity=Rarity.Common){
            var r=DistinctEquipmentDesigns.Find(id);if(r!=null&&id.StartsWith("distinct.gun."))return FromRecipe(r,rarity);
            if(string.IsNullOrEmpty(id))return null;var parts=id.Split('-');
            if(parts.Length==3&&parts[0]=="gun"&&int.TryParse(parts[1],out int model)&&int.TryParse(parts[2],out int design)&&model>=0&&model<ModelCount&&design>=0&&design<DesignsPerModel)return LegacyCatalog(model+design*ModelCount,rarity);
            return null;
        }
        // SMGs keep a two-hand grip; short shotguns and sidearms remain one-handed.
        public static bool IsOneHanded(int model)=>model==5||(model>=14&&model<=17);
        public static WeaponClass Category(int model) { if(model==3||model==4||model==5||model==13)return WeaponClass.Shotgun; if(model==2||model==7||model==8)return WeaponClass.SniperRifle; if(model>=14&&model<=17)return WeaponClass.Handgun; return WeaponClass.AssaultRifle; }
        public static GunHandling Handling(Gun gun)=>IsOneHanded(VaultLanguage.DisplayModel(gun))?GunHandling.OneHanded:GunHandling.TwoHanded;
        public static float DefaultAccuracy(int model)
        {
            switch(model){case 1:return 70;case 2:return 97;case 7:return 96;case 8:return 98;case 12:return 94;case 3:return 48;case 4:return 55;case 5:return 40;case 13:return 52;case 6:return 76;case 14:return 89;case 15:return 86;case 16:return 78;case 17:return 91;default:return 85;}
        }
        public static float Accuracy(Gun gun)=>gun==null?0:Mathf.Clamp(gun.accuracy>0?gun.accuracy:DefaultAccuracy(VaultLanguage.DisplayModel(gun)),1,100);
        // Accuracy is a spread score, rather than a promise of hitting a moving target.
        public static float SpreadHalfAngle(Gun gun)=>gun==null?0:(100-Accuracy(gun))*.25f;
        public static string ModelDescription(int variant)
        {
            switch(Mathf.Clamp(variant,0,ModelCount-1)){case 14:return "Six-shot revolver · measured single fire";case 15:return "Blowback automatic pistol · rapid lead fire";case 16:return "Heavy hand cannon · powerful single-shot lead rounds";case 17:return "Suppressed compact pistol · precise fire";case 12:return "Modern carbine · controlled automatic lead fire";case 13:return "Pump-action shotgun · hard-hitting breach shot";case 9:return "Ricochets between enemies · fragments on impact";case 10:return "Isotope drones launch toward your aim";case 11:return "Vacuum implosion · delayed detonation";case 3:case 4:case 5:return "Lead shot · wide close-range burst";default:return "Mechanical action · lead or coil projectile";}
        }
        public const string TrailWardenVisualSet="trail-warden";
        public static Gun TrailWarden(bool sidearm=false)
        {
            var item=Model(sidearm?15:12,Rarity.Common);item.visualSet=TrailWardenVisualSet;
            item.name=sidearm?"Warden Sidearm":"Warden Woodstock";
            item.subtitle=sidearm?"Trail Warden service sidearm":"Trail Warden wood-stock rifle";return item;
        }
        public static Gun Starter()=>Catalog(30,Rarity.Common);
        public static Gun Model(int variant,Rarity rarity)
        {
            var gun=BaseModel(variant,rarity);gun.catalogId="gun-"+gun.modelVariant.ToString("00")+"-00";
            gun.manufacturerId=ManufacturerCatalog.ForDesign(gun.modelVariant,0);ManufacturerCatalog.Apply(gun);GearBalance.Migrate(gun);return gun;
        }
        internal static Gun BaseModel(int variant,Rarity rarity)
        {
            variant=Mathf.Clamp(variant,0,ModelCount-1);
            float[] damage={15,9,45,10,8,15,12,27,60,26,18,30,16,12,31,14,56,19};
            float[] interval={.15f,.085f,.7f,.6f,.32f,.9f,.11f,.35f,1.05f,.27f,.21f,.65f,.13f,.72f,.38f,.16f,.72f,.23f};
            int[] magazines={24,36,6,8,12,2,60,12,4,16,20,8,30,6,6,15,5,12},pellets={1,1,1,6,4,8,1,1,1,1,1,1,1,7,1,1,1,1};
            float[] speeds={23,21,36,19,18,17,26,32,40,25,19,17,28,21,30,26,33,29},reloads={1.15f,1.45f,1.7f,1.6f,1.8f,1.05f,2.7f,1.55f,2.1f,1.5f,1.65f,1.9f,1.5f,1.9f,1.65f,1.25f,1.8f,1.35f};
            GunEffect effect=variant>=9&&variant<=11?(GunEffect)(variant-7):(variant>=3&&variant<=5)||variant==13?GunEffect.Scatter:GunEffect.Normal;
            string description=ModelDescription(variant);
            var gun=new Gun(ModelNames[variant],description,rarity,effect,damage[variant],interval[variant],speeds[variant],magazines[variant],pellets[variant]);
            gun.modelVariant=variant;gun.accuracy=DefaultAccuracy(variant);gun.reloadSeconds=reloads[variant];return gun;
        }
        public static Gun Legendary(int index)
        {
            return Model(9+(index%3+3)%3,Rarity.Legendary);
        }
        public static Gun Legendary(int index, System.Random random, int level) => LegendaryCatalog.CreateGun(random.Next(30),random,level);
        public static Gun RollCatalogStats(Gun gun,System.Random random,int level)=>Randomize(gun,random,level);
        public static Gun Roll(System.Random random, int depth)
        {
            if(random==null)throw new System.ArgumentNullException(nameof(random));
            return RollForDungeon(random,Mathf.Clamp(depth/4,0,2));
        }
        public static Gun RollForDungeon(System.Random random,int dungeon,bool boss=false)=>CreateForDungeon(random,dungeon,DungeonCatalog.RollRarity(random,boss),DungeonCatalog.ItemLevel(random,dungeon));
        public static Gun CreateForDungeon(System.Random random,int dungeon,Rarity rarity,int level)
        {
            if(rarity==Rarity.Legendary)return Legendary(dungeon,random,level);
            var item=Catalog(30+random.Next(CatalogCount-30),rarity);
            return Randomize(item,random,level);
        }
        static Gun Randomize(Gun gun,System.Random random,int level)
        {
            if(random==null)throw new System.ArgumentNullException(nameof(random));
            gun.level=Mathf.Max(1,level);
            float outputScale=1+.16f*((int)gun.rarity+Mathf.Max(1,gun.level)-1);
            gun.damage*= outputScale*Range(random,.92f,1.08f);
            gun.interval*=Range(random,.93f,1.07f);
            gun.speed*=Range(random,.85f,1.2f);
            gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*Range(random,.76f,1.3f)));
            gun.reloadSeconds*=Range(random,.82f,1.22f);
            float accuracyVariation=gun.pellets>1?10:5;gun.accuracy=Mathf.Clamp(gun.accuracy+Range(random,-accuracyVariation,accuracyVariation),1,100);
            RollAffixes(gun,random);
            GearBalance.Migrate(gun);
            return gun;
        }
        static void RollAffixes(Gun gun,System.Random random)
        {
            int count=Mathf.Clamp((int)gun.rarity,0,4); string[] types={"damage","health","fireRate","reload","accuracy","mitigation","moveSpeed","regen","ammo","critChance","critDamage"};
            gun.affixes=new GearAffix[count]; var used=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<count;i++)
            {
                int pick;do{pick=random.Next(types.Length);}while((gun.rarity!=Rarity.Legendary&&pick==7)||!used.Add(pick));
                float scale=1+.16f*((int)gun.rarity+gun.level-1);
                float[] lo={.025f,4,.025f,.025f,.02f,.01f,.02f,.15f,.05f,.01f,.04f},hi={.06f,10,.06f,.06f,.05f,.025f,.05f,.4f,.12f,.025f,.10f};
                var x=new GearAffix{type=types[pick],value=Range(random,lo[pick],hi[pick])*scale};gun.affixes[i]=x;
                switch(x.type)
                {
                    case "damage":gun.damage*=1+x.value;break;
                    case "fireRate":gun.interval/=1+x.value;break;
                    case "reload":gun.reloadSeconds/=1+x.value;break;
                    case "accuracy":gun.accuracy=Mathf.Min(100,gun.accuracy*(1+x.value));break;
                    case "ammo":gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*(1+x.value)));break;
                    case "health":gun.bonusHealth+=x.value;break;
                    case "mitigation":gun.damageReductionBonus+=x.value;break;
                    case "moveSpeed":gun.movementBonus+=x.value;break;
                    case "regen":gun.regenBonus+=x.value;break;
                }
            }
        }
        internal static float Range(System.Random random,float min,float max)=>min+(float)random.NextDouble()*(max-min);
    }
}
