using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    // Version 1 changes base roles without recreating inventory objects or rerolling retained affixes.
    public static class GearBalance
    {
        public const int Version=1;
        static int Seed(string id){unchecked{uint h=2166136261;foreach(char c in id??""){h^=c;h*=16777619;}return (int)(h&0x7fffffff);}}
        public static void Migrate(Gun g)
        {
            if(g==null)return;
            if(g.rarity!=Rarity.Legendary)
            {
                g.regenBonus=0;
                var used=new HashSet<string>();if(g.affixes!=null)foreach(var x in g.affixes)if(x!=null&&x.type!="regen")used.Add(x.type);
                var random=new System.Random(Seed(g.id));
                string[] types={"damage","health","fireRate","reload","accuracy","mitigation","moveSpeed","ammo","critChance","critDamage"};
                float[] lo={.025f,4,.025f,.025f,.02f,.01f,.02f,.05f,.01f,.04f},hi={.06f,10,.06f,.06f,.05f,.025f,.05f,.12f,.025f,.10f};
                if(g.affixes!=null)for(int i=0;i<g.affixes.Length;i++)if(g.affixes[i]!=null&&g.affixes[i].type=="regen")
                {
                    int p;do{p=random.Next(types.Length);}while(!used.Add(types[p]));
                    var x=new GearAffix{type=types[p],value=Arsenal.Range(random,lo[p],hi[p])*(1+.16f*((int)g.rarity+Mathf.Max(1,g.level)-1))};g.affixes[i]=x;
                    switch(x.type){case "damage":g.damage*=1+x.value;break;case "health":g.bonusHealth+=x.value;break;case "fireRate":g.interval/=1+x.value;break;case "reload":g.reloadSeconds/=1+x.value;break;case "accuracy":g.accuracy=Mathf.Min(100,g.accuracy*(1+x.value));break;case "mitigation":g.damageReductionBonus+=x.value;break;case "moveSpeed":g.movementBonus+=x.value;break;case "ammo":g.magazine=Mathf.Max(1,Mathf.RoundToInt(g.magazine*(1+x.value)));break;}
                }
            }
            g.balanceVersion=Version;
        }
        public static void Migrate(ArmorItem a)
        {
            if(a==null||a.balanceVersion>=Version)return;
            // Serialized affixes are additive: take them off, convert the base once, then restore the same objects/values.
            if(a.affixes!=null)foreach(var x in a.affixes)if(x!=null)Add(a,x,-1);
            RebalanceArmorBase(a,null);
            if(a.affixes!=null)foreach(var x in a.affixes)if(x!=null)Add(a,x,1);
        }
        static void Add(ArmorItem a,GearAffix x,float sign)
        {float v=x.value*sign;switch(x.type){case "damage":a.damageBonus+=v;break;case "health":a.maxHealth+=v;break;case "fireRate":a.fireBonus+=v;break;case "reload":a.reloadBonus+=v;break;case "accuracy":a.accuracyBonus+=v;break;case "mitigation":a.damageReduction+=v;break;case "moveSpeed":a.moveBonus+=v;break;case "regen":a.regenBonus+=v;break;case "ammo":a.ammoBonus+=v;break;}}
        static float Ratio(float value,float expected)=>Mathf.Clamp(expected>0?value/expected:1,.78f,1.25f);
        internal static void RebalanceArmorBase(ArmorItem a,System.Random random)
        {
            if(a.balanceVersion>=Version)return;
            float power=1+.16f*((int)a.rarity+Mathf.Max(1,a.level)-1);
            // Product tuning is deterministic; subtracting it exposes the original serialized base rolls.
            var product=new ArmorItem{slot=a.slot,catalogId=a.catalogId,manufacturerId=a.manufacturerId,designVariant=a.designVariant,visualVariant=a.visualVariant};
            ArmorProductCatalog.Apply(product,power);
            float health=a.maxHealth-product.maxHealth,damage=a.damageBonus-product.damageBonus,fire=a.fireBonus-product.fireBonus,reload=a.reloadBonus-product.reloadBonus,move=a.moveBonus-product.moveBonus;
            float hpRoll=Ratio(health,(a.slot==ArmorSlot.Chest?16:a.slot==ArmorSlot.Legs?9:7)*power);
            float damageRoll=Ratio(damage,(a.slot==ArmorSlot.Head?.025f:.04f)*power),fireRoll=Ratio(fire,.035f*power),reloadRoll=Ratio(reload,.04f*power),moveRoll=Ratio(move,(a.slot==ArmorSlot.Feet?.055f:.035f)*power);
            a.maxHealth=a.damageBonus=a.fireBonus=a.moveBonus=a.reloadBonus=a.accuracyBonus=a.ammoBonus=a.damageReduction=a.regenBonus=a.dodgeBonus=0;
            switch(a.slot)
            {
                case ArmorSlot.Head:a.maxHealth=7*power*hpRoll;a.accuracyBonus=.025f*power*damageRoll;break;
                case ArmorSlot.Chest:a.maxHealth=20*power*hpRoll;break;
                case ArmorSlot.Arms:
                    var choices=new List<int>{0,1,2,3};var roll=random??new System.Random(Seed(a.id));
                    for(int n=0;n<2;n++){int index=roll.Next(choices.Count),choice=choices[index];choices.RemoveAt(index);switch(choice){case 0:a.maxHealth=7*power*damageRoll;break;case 1:a.damageBonus=.04f*power*damageRoll;break;case 2:a.reloadBonus=.04f*power*reloadRoll;break;case 3:a.fireBonus=.035f*power*fireRoll;break;}}break;
                case ArmorSlot.Legs:a.maxHealth=9*power*hpRoll;a.moveBonus=.035f*power*moveRoll;break;
                case ArmorSlot.Feet:a.maxHealth=7*power*moveRoll;a.moveBonus=.065f*power*moveRoll;break;
            }
            a.balanceVersion=Version;
        }
    }
}
