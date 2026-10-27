using System;
using UnityEngine;

namespace NeonVault
{
    public enum MeleeWeaponFamily { Knife, Shiv, Cleaver, Machete, Hatchet }

    [Serializable]
    public sealed class MeleeWeaponItem
    {
        public string id, catalogId, manufacturerId;
        public int designVariant;
        public MeleeWeaponFamily family;
        public Rarity rarity;
        public bool uniqueEffect;
        public int level = 1;
        public int damage = 18;
        public GearAffix[] affixes;
        public float interval = .7f, range = 1.2f, arcDegrees = 85f;
        public Color color=>Arsenal.Colors[Mathf.Clamp((int)rarity,0,Arsenal.Colors.Length-1)];
    }

    public static class MeleeWeaponCatalog
    {
        public const int FamilyCount=5, DesignsPerFamily=10;
        public static int CatalogCount=>FamilyCount*DesignsPerFamily;
        public static void InferIdentity(MeleeWeaponItem item)
        {
            if(item==null)return;EnsureAffixes(item);item.designVariant=Mathf.Clamp(item.designVariant,0,9);
            if(ManufacturerCatalog.Index(item.manufacturerId)<0)item.manufacturerId=ManufacturerCatalog.ForDesign((int)item.family,item.designVariant);
            if(string.IsNullOrEmpty(item.catalogId))item.catalogId="melee-"+((int)item.family).ToString("00")+"-"+item.designVariant.ToString("00");
        }
        public static MeleeWeaponItem StarterKnife()=>new MeleeWeaponItem
        {id=Guid.NewGuid().ToString("N"),catalogId="melee-00-00",manufacturerId="kestrel",family=MeleeWeaponFamily.Knife,level=1,damage=18,interval=.65f,range=1.15f,arcDegrees=75};

        public static MeleeWeaponItem Create(System.Random random,int level)
        {random=random??new System.Random();return Create(random,level,DungeonCatalog.RollRarity(random));}

        public static MeleeWeaponItem Create(System.Random random,int level,Rarity rarity)
        {
            random=random??new System.Random();return Catalog(random.Next(CatalogCount),rarity,level,random);
        }
        public static MeleeWeaponItem Catalog(int index,Rarity rarity=Rarity.Common,int level=1,System.Random random=null)
        {
            index=Mathf.Clamp(index,0,CatalogCount-1);level=Mathf.Max(1,level);rarity=(Rarity)Mathf.Clamp((int)rarity,0,4);
            int i=index%FamilyCount,design=index/FamilyCount;
            float[] damage={18,22,32,27,36},interval={.62f,.78f,.95f,.82f,1.05f},range={1.2f,1.0f,1.15f,1.65f,1.4f},arc={75,60,105,90,115};
            float[] output={1,.82f,1.28f,.76f,1.13f,1.08f,.92f,1.16f,1.05f,1.24f};
            float[] cadence={1,.78f,1.24f,.71f,1.14f,1.12f,.94f,1.02f,1.18f,1.22f};
            float[] reach={1,.91f,1.06f,.88f,1.2f,.97f,1.12f,.91f,1.16f,1.08f};
            float[] sweep={1,.86f,1.12f,1.2f,.78f,1.25f,1.04f,.88f,1.18f,1.08f};
            float d=damage[i]*output[design],t=interval[i]*cadence[design],r=range[i]*reach[design],a=arc[i]*sweep[design];
            string maker=ManufacturerCatalog.ForDesign(i,design);
            switch(ManufacturerCatalog.Index(maker)){
                case 0:d*=.94f;t*=.88f;a*=.92f;break; // Balanced precision and quicker recovery.
                case 1:d*=1.22f;t*=1.23f;r*=1.04f;break;
                case 2:d*=.83f;t*=.78f;r*=.92f;break;
                case 3:d*=.91f;r*=1.13f;a*=1.04f;break; // Flexible living haft rather than an unused healing stat.
                case 4:d*=1.08f;t*=1.16f;a*=1.2f;r*=.94f;break;
                case 5:d*=.94f;t*=.94f;a*=1.08f;break;
            }
            float scale=1+.16f*((int)rarity+level-1);
            var item=new MeleeWeaponItem{id=Guid.NewGuid().ToString("N"),catalogId="melee-"+i.ToString("00")+"-"+design.ToString("00"),manufacturerId=maker,designVariant=design,family=(MeleeWeaponFamily)i,rarity=rarity,uniqueEffect=rarity==Rarity.Legendary,level=level,damage=Mathf.Max(1,Mathf.RoundToInt(d*scale)),interval=t/(1+.025f*(int)rarity),range=r+.025f*(level-1),arcDegrees=Mathf.Clamp(a,30,170)};
            RollAffixes(item,random??new System.Random(index*397+level*17+(int)rarity));return item;
        }
        // Null is the pre-affix save format. Generate once from stable identity;
        // serialized damage/interval already include these bonuses thereafter.
        public static void EnsureAffixes(MeleeWeaponItem item)
        {
            if(item==null||item.affixes!=null)return;
            int seed=23;string identity=item.id??item.catalogId??"melee";
            unchecked{foreach(char c in identity)seed=seed*31+c;}
            RollAffixes(item,new System.Random(seed));
        }
        static void RollAffixes(MeleeWeaponItem item,System.Random random)
        {
            string[] types={"damage","fireRate","critChance","critDamage"};
            float[] low={.025f,.025f,.01f,.04f},high={.06f,.06f,.025f,.10f};
            int count=Mathf.Clamp((int)item.rarity,0,4);
            item.affixes=new GearAffix[count];var used=new System.Collections.Generic.HashSet<int>();
            float scale=1+.16f*((int)item.rarity+Mathf.Max(1,item.level)-1);
            for(int i=0;i<count;i++)
            {
                int pick;do{pick=random.Next(types.Length);}while(!used.Add(pick));
                var x=new GearAffix{type=types[pick],value=Arsenal.Range(random,low[pick],high[pick])*scale};item.affixes[i]=x;
                if(x.type=="damage")item.damage=Mathf.Max(1,Mathf.RoundToInt(item.damage*(1+x.value)));
                else if(x.type=="fireRate")item.interval/=1+x.value;
            }
        }
        public static MeleeWeaponItem RollForDungeon(System.Random random,int dungeon,bool boss=false)
        {random=random??new System.Random();return Create(random,DungeonCatalog.ItemLevel(random,dungeon),DungeonCatalog.RollRarity(random,boss));}
        public static string Name(MeleeWeaponItem item)
        {
            if(item==null)return "";InferIdentity(item);
            string en=ManufacturerCatalog.Name(item.manufacturerId,false)+" "+English(item.family)+" / "+Arsenal.DesignName(item.designVariant,false);
            string ja=ManufacturerCatalog.Name(item.manufacturerId,true)+" "+Japanese(item.family)+" / "+Arsenal.DesignName(item.designVariant,true);
            return VaultLanguage.T(en,ja);
        }
        public static string Name(MeleeWeaponFamily family)=>VaultLanguage.T(English(family),Japanese(family));
        public static int LegendaryMode(MeleeWeaponItem item)=>item==null?-1:(Mathf.Clamp(item.designVariant,0,9)+2*(int)item.family)%4;
        public static string LegendaryDescription(MeleeWeaponItem item)
        {
            if(item==null||!item.uniqueEffect)return "";
            InferIdentity(item);
            int maker=ManufacturerCatalog.Index(item.manufacturerId);
            int mode=LegendaryMode(item);
            string effect;
            switch(maker)
            {
                case 0:effect=mode==0?VaultLanguage.T("strike 1 other visible enemy within 2.8m for 45% hit damage","2.8m以内の見える別の敵1体に命中威力45%の追撃"):mode==1?VaultLanguage.T("shorten melee recovery by 0.25s","次撃までを0.25秒短縮"):mode==2?VaultLanguage.T("pierce up to 2 other visible enemies in a 3.6m by 0.65m line for 30% hit damage","3.6mの直線上0.65m幅の見える別の敵最大2体へ命中威力30%の貫通攻撃"):VaultLanguage.T("delay the target's next attack to at least 0.7s and shorten melee recovery by 0.15s","対象の次の攻撃を最低0.7秒遅らせ、次撃までを0.15秒短縮");break;
                case 1:effect=mode==0?VaultLanguage.T("push the target back 1.4m","対象を1.4m押し返す"):mode==1?VaultLanguage.T("delay the target's next attack to at least 0.8s","対象の次の攻撃を最低0.8秒遅らせる"):mode==2?VaultLanguage.T("strike up to 2 other visible enemies within 1.8m for 40% hit damage","1.8m以内の見える別の敵最大2体へ命中威力40%の追撃"):VaultLanguage.T("push the target 0.8m and delay its next attack to at least 0.55s","対象を0.8m押し返し、次の攻撃を最低0.55秒遅らせる");break;
                case 2:effect=mode==0?VaultLanguage.T("arc to 2 other visible enemies within 3.2m for 32% hit damage each","3.2m以内の見える別の敵2体に各命中威力32%の連鎖攻撃"):mode==1?VaultLanguage.T("shorten dodge cooldown by 0.6s","回避待機を0.6秒短縮"):mode==2?VaultLanguage.T("pierce up to 3 other visible enemies in a 4m by 0.8m line for 28% hit damage","4mの直線上0.8m幅の見える別の敵最大3体へ命中威力28%の貫通攻撃"):VaultLanguage.T("push other visible enemies within 2.5m by 0.9m and shorten dodge cooldown by 0.25s","2.5m以内の見える別の敵を0.9m押し返し、回避待機を0.25秒短縮");break;
                case 3:effect=mode==0?VaultLanguage.T("heal 3 HP","HPを3回復"):mode==1?VaultLanguage.T("delay the target's next attack to at least 1.1s","対象の次の攻撃を最低1.1秒遅らせる"):mode==2?VaultLanguage.T("pull other visible enemies within 2.8m toward the target by 0.8m","2.8m以内の見える別の敵を対象へ0.8m引き寄せる"):VaultLanguage.T("strike up to 2 other visible enemies within 2m for 22% hit damage and heal 1 HP","2m以内の見える別の敵最大2体へ命中威力22%の追撃を行い、HPを1回復");break;
                case 4:effect=mode==0?VaultLanguage.T("strike up to 3 other visible enemies within 2.3m for 38% hit damage","2.3m以内の見える別の敵最大3体に命中威力38%の範囲攻撃"):mode==1?VaultLanguage.T("push other visible enemies within 2.3m back 1m","2.3m以内の見える別の敵を1m押し返す"):mode==2?VaultLanguage.T("pierce up to 3 other visible enemies in a 3.8m by 1m line for 42% hit damage","3.8mの直線上1m幅の見える別の敵最大3体へ命中威力42%の貫通攻撃"):VaultLanguage.T("push the target 1m and strike up to 2 other visible enemies within 1.7m for 25% hit damage","対象を1m押し返し、1.7m以内の見える別の敵最大2体へ命中威力25%の追撃");break;
                case 5:effect=mode==0?VaultLanguage.T("gain 4 barrier (cap 30)","障壁を4得る（上限30）"):mode==1?VaultLanguage.T("grant at least 0.25s of invulnerability","最低0.25秒の無敵を得る"):mode==2?VaultLanguage.T("gain 2 barrier and push other visible enemies within 2m by 0.8m","障壁を2得て、2m以内の見える別の敵を0.8m押し返す"):VaultLanguage.T("gain 2 barrier, grant at least 0.15s of invulnerability, and delay the target's next attack to at least 0.75s","障壁を2得て最低0.15秒の無敵を得て、対象の次の攻撃を最低0.75秒遅らせる");break;
                default:return "";
            }
            return VaultLanguage.T("First landed melee hit when ready: ","待機終了後、最初の近接命中時：")+effect+VaultLanguage.T("; 3s cooldown.","。待機3秒。");
        }
        public static string English(MeleeWeaponFamily family)=>family==MeleeWeaponFamily.Shiv?"Shiv":family==MeleeWeaponFamily.Cleaver?"Cleaver":family==MeleeWeaponFamily.Machete?"Machete":family==MeleeWeaponFamily.Hatchet?"Hatchet":"Field Knife";
        public static string Japanese(MeleeWeaponFamily family)=>family==MeleeWeaponFamily.Shiv?"シヴ":family==MeleeWeaponFamily.Cleaver?"ブッチャーナイフ":family==MeleeWeaponFamily.Machete?"マチェット":family==MeleeWeaponFamily.Hatchet?"ハチェット":"フィールドナイフ";
        public static int Score(MeleeWeaponItem item){EnsureAffixes(item);return item==null?0:Mathf.Max(1,Mathf.RoundToInt((Mathf.Log(1+item.damage/Mathf.Max(.05f,item.interval))*12+Mathf.Log(1+item.range)*4+item.arcDegrees*.06f+item.level*2+(int)item.rarity*5+GearScoring.AffixTotal(item.affixes,"critChance")*200+GearScoring.AffixTotal(item.affixes,"critDamage")*20)*10));}
    }
}
