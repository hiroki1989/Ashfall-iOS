using System;
using UnityEngine;

namespace NeonVault
{
    public enum MeleeWeaponFamily { Knife, Shiv, Cleaver, Machete, Hatchet }

    [Serializable]
    public sealed class MeleeWeaponItem
    {
        public string id, catalogId, manufacturerId, bossRelicId;
        public int designVariant;
        public MeleeWeaponFamily family;
        public Rarity rarity;
        public bool uniqueEffect;
        public int level = 1;
        public int damage = 18;
        public GearAffix[] affixes;
        public float interval = .7f, range = 1.2f, arcDegrees = 85f;
        // Keep serialized range in its original units so old and new saves share the same scaling.
        public float effectiveRange=>range*2f;
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
            random=random??new System.Random();int index=rarity==Rarity.Legendary?DungeonBossExclusive.RollNonExclusiveMeleeIndex(random):random.Next(CatalogCount);return Catalog(index,rarity,level,random);
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
            float scale=GearBalance.LevelPower(rarity,level);
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
            float scale=GearBalance.LevelPower(item.rarity,item.level);
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
            if(item==null)return "";string relic=BossRelicFoundation.Name(item.bossRelicId,VaultLanguage.Japanese);if(!string.IsNullOrEmpty(relic))return relic;InferIdentity(item);
            return DistinctEquipmentDesigns.ManufacturerProductName("melee",(int)item.family*DesignsPerFamily+item.designVariant,Mathf.Max(0,ManufacturerCatalog.Index(item.manufacturerId)),VaultLanguage.Japanese);
        }
        public static string Name(MeleeWeaponFamily family)=>VaultLanguage.T(English(family),Japanese(family));
        public static int LegendaryMode(MeleeWeaponItem item)=>item==null?-1:(Mathf.Clamp(item.designVariant,0,9)+2*(int)item.family)%4;
        public static string LegendaryDescription(MeleeWeaponItem item)
        {
            if(item==null||!item.uniqueEffect)return "";
            string bossRelic=BossRelicFoundation.Description(item.bossRelicId,VaultLanguage.Japanese);
            if(!string.IsNullOrEmpty(bossRelic))return bossRelic;
            InferIdentity(item);
            if(item.designVariant>=8)
            {
                string[] en={"release guided rounds toward up to four other enemies","fire a gravity-well round","release eight rounds around the target","trigger three explosions ahead","blast up to eight nearby enemies","trigger three pulses at the target","fire five rounds in a fan","fire two crossing rounds","fire a heavy guided round at the farthest enemy","restore 16% of maximum HP"};
                string[] ja={"別の敵最大4体へ誘導弾を放つ","重力渦を作る弾を放つ","対象から全方向に弾を8発放つ","前方へ3回の爆発を起こす","対象の周囲の敵最大8体を爆破","対象の周囲に3回の波動を起こす","前方へ弾を5発扇状に放つ","左右から交差する弾を2発放つ","最も遠い敵へ高威力の誘導弾を放つ","最大HPの16%を回復する"};
                int i=(int)item.family*2+item.designVariant-8;
                return VaultLanguage.T("First landed melee hit when ready: ","クールダウン終了後、最初の近接命中時：")+VaultLanguage.T(en[i],ja[i])+VaultLanguage.T("; 3s cooldown.","。クールダウン3秒。");
            }
            int individual=(int)item.family*8+item.designVariant;
            string[] effectEn={"a round splits into three","a round splits sideways","a round circles","a round divides into a radial volley","a round divides into a five-shot fan","a round weaves from side to side","an accelerating round fires sideways","a round fires in successive directions","a round spirals","an electric round chains between enemies","a rotating round fires backward","a fire round","a poison round","an explosive round","a slowing electric round chains to five enemies","a round reverses course","a round curves left","a round curves right","a round circles and continues","a round weaves widely","a round accelerates","a round decelerates","a round releases a blast","a round releases a fiery blast","a round releases a spore burst","a round accelerates after a delay","a circling round releases shots","a round fires to both sides","a round fires behind itself","a round homes toward the nearest enemy","a round sweeps from side to side","a round creates a gravity well","a round releases a blast near the end of its flight","a round releases guided rounds","a round releases a radial volley","guided rounds chase four other enemies","a large blast hits nearby enemies","gain 20 shield","gain 1.25 seconds of invulnerability","restore 12% of maximum HP"};
            string[] effectJa={"弾が飛行中に3発へ分裂する","弾が飛行中に左右へ分裂する","弾が円を描いて飛ぶ","弾が全方向へ分裂する","弾が扇状の5発へ分裂する","弾が左右に蛇行する","弾が加速しながら左右へ弾を放つ","弾が周囲へ順次弾を放つ","弾がらせんを描いて飛ぶ","電撃弾が敵へ連鎖する","回転する弾が後方へ弾を放つ","炎属性の弾を放つ","毒属性の弾を放つ","爆発属性の弾を放つ","減速する電撃弾が最大5体に連鎖する","弾が反転して戻る","弾が左へ曲がる","弾が右へ曲がる","弾が円を描いてから進む","弾が大きく蛇行する","弾が加速する","弾が減速する","弾が飛行中に爆発する","弾が飛行中に炎の爆発を起こす","弾が飛行中に胞子の爆発を起こす","弾が一定時間後に加速する","弾が周回しながら弾を放つ","弾が左右へ弾を放つ","弾が後方へ弾を放つ","弾が近くの敵を追う","弾が左右に方向を振る","弾が重力渦を作る","弾が終点近くで爆発する","弾から誘導弾が放たれる","弾から全方向に弾が放たれる","別の敵最大4体を誘導弾が追う","大爆発で周囲の敵を攻撃する","シールドを20得る","1.25秒の無敵を得る","最大HPの12%を回復する"};
            return VaultLanguage.T("First landed melee hit when ready: ","クールダウン終了後、最初の近接命中時：")+VaultLanguage.T(effectEn[individual],effectJa[individual])+VaultLanguage.T("; 3s cooldown.","。クールダウン3秒。");

        }
        public static string English(MeleeWeaponFamily family)=>family==MeleeWeaponFamily.Shiv?"Shiv":family==MeleeWeaponFamily.Cleaver?"Cleaver":family==MeleeWeaponFamily.Machete?"Machete":family==MeleeWeaponFamily.Hatchet?"Hatchet":"Field Knife";
        public static string Japanese(MeleeWeaponFamily family)=>family==MeleeWeaponFamily.Shiv?"シヴ":family==MeleeWeaponFamily.Cleaver?"ブッチャーナイフ":family==MeleeWeaponFamily.Machete?"マチェット":family==MeleeWeaponFamily.Hatchet?"ハチェット":"フィールドナイフ";
        public static int Score(MeleeWeaponItem item){EnsureAffixes(item);return item==null?0:Mathf.Max(1,Mathf.RoundToInt((Mathf.Log(1+item.damage/Mathf.Max(.05f,item.interval))*12+Mathf.Log(1+item.effectiveRange)*4+item.arcDegrees*.06f+item.level*2+(int)item.rarity*5+GearScoring.AffixTotal(item.affixes,"critChance")*200+GearScoring.AffixTotal(item.affixes,"critDamage")*20)*10));}
    }
}
