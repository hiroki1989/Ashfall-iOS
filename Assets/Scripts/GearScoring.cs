using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public static class GearScoring
    {
        static string[] Types={"damage","health","fireRate","reload","accuracy","mitigation","moveSpeed","regen","ammo","critChance","critDamage"};
        static string[] English={"Damage","Max HP","Fire rate","Reload","Accuracy","Mitigation","Move speed","Regen","Ammo capacity","Critical chance","Critical damage"};
        static string[] Japanese={"威力","最大HP","連射速度","リロード","命中精度","ダメージ軽減","移動速度","自動回復","装弾数","クリティカル率","クリティカル倍率"};
        public static int Score(Gun g){if(g==null)return 0;float dps=g.damage*Mathf.Max(1,g.pellets)/Mathf.Max(.05f,g.interval);return Mathf.Max(1,Mathf.RoundToInt((Mathf.Log(1+dps)*12+Mathf.Log(1+g.magazine)*2+Mathf.Log(1+g.speed)*1.5f+g.level*2+(int)g.rarity*5+AffixWeight(g.affixes))*10));}
        public static int Score(ArmorItem a){if(a==null)return 0;float power=a.damageReduction*100+a.maxHealth*.65f+a.damageBonus*70+a.fireBonus*55+a.moveBonus*45+a.reloadBonus*45+a.dodgeBonus*40+a.regenBonus*12+a.accuracyBonus*120+a.ammoBonus*40;return Mathf.Max(1,Mathf.RoundToInt((power+a.level*3+(int)a.rarity*7+AffixWeight(a.affixes))*10));}
        public static int Score(ArtifactItem a){if(a==null)return 0;return Mathf.Max(1,Mathf.RoundToInt((a.power*20+a.level*3+(int)a.rarity*7+AffixWeight(a.affixes))*10));}
        public static float AffixTotal(GearAffix[] affixes,string type){float result=0;if(affixes!=null)foreach(var a in affixes)if(a!=null&&a.type==type)result+=a.value;return result;}
        static float AffixWeight(GearAffix[] affixes){float result=0;if(affixes!=null)foreach(var a in affixes)if(a!=null)result+=Mathf.Abs(a.value)*2;return result;}
        public static string AffixSummary(Gun g)=>Summary(g==null?null:g.affixes);
        public static string AffixSummary(ArmorItem a)=>Summary(a==null?null:a.affixes);
        public static string AffixSummary(ArtifactItem a)=>Summary(a==null?null:a.affixes);
        static string Summary(GearAffix[] affixes){if(affixes==null||affixes.Length==0)return "";bool ja=VaultLanguage.Japanese;var values=new List<string>();foreach(var a in affixes){if(a==null)continue;int i=System.Array.IndexOf(Types,a.type);if(i<0)continue;string label=VaultLanguage.T(English[i],Japanese[i]);bool pct=i==0||i==2||i==3||i==4||i==5||i==6||i==8||i==9||i==10;string amount=(pct?a.value*100:a.value).ToString(pct?"0.#":"0.0");values.Add(label+" +"+amount+(pct?"%":i==7?(ja?" HP/秒":" HP/s"):""));}return values.Count==0?"":VaultLanguage.T("AFFIXES: ","追加特性: ")+string.Join(" · ",values);}
    }
}
