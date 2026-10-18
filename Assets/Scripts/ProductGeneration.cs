using UnityEngine;
namespace NeonVault
{
    // Orthogonal engineering generations, applied only to newly created items.
    // Generation zero is intentionally neutral to preserve every legacy product.
    public static class ProductGeneration
    {
        static readonly float[] output={1,.82f,1.30f,.74f,1.16f,.91f,1.07f,.86f,1.23f,.96f,1.12f,1.04f};
        static readonly float[] cycle={1,.78f,1.26f,.91f,1.11f,.83f,1.19f,.95f,1.34f,1.08f,1.18f,.93f};
        public static int Generation(int design)=>Mathf.Clamp(design/10,0,output.Length-1);
        public static float Output(int design)=>output[Generation(design)];
        public static float Cycle(int design)=>cycle[Generation(design)];
        public static string Name(int design,bool japanese)
        {int g=Generation(design);if(g==0)return "";string[] en={"","Feather","Siege","Reservoir","Overdrive","Quickdraw","Bulwark","Survey","Reactor","Medic","Frontier","Relay"};string[] ja={"","軽量","攻城","大容量","過給","速応","重装","測量","炉心","衛生","辺境","中継"};return " / "+(japanese?ja[g]:en[g])+" G"+g;}
        public static void Apply(Gun gun)
        {
            int g=Generation(gun.designVariant);if(g==0)return;
            gun.damage*=Output(gun.designVariant);gun.interval*=Cycle(gun.designVariant);
            gun.reloadSeconds*=1+(g%3-1)*.13f;
            gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*(1+(g%4-1)*.17f)));
            gun.accuracy=Mathf.Clamp(gun.accuracy+(g%5-2)*3,1,100);
            if(g==6){gun.damageReductionBonus+=.015f;gun.movementBonus-=.02f;}
            if(g==9){gun.regenBonus+=.18f;gun.bonusHealth-=3;}
        }
        public static void Apply(ArmorItem item,float power)
        {
            int g=Generation(item.designVariant);if(g==0)return;
            // Every generation exchanges output, handling, mobility or endurance.
            item.damageBonus+=(Output(item.designVariant)-1)*.12f*power;
            item.fireBonus+=(1-Cycle(item.designVariant))*.10f*power;
            item.maxHealth+=(g%4-1)*2*power;
            item.moveBonus-=(g%4-1)*.006f*power;
            item.reloadBonus+=(g%3-1)*.014f*power;
            item.accuracyBonus-=(g%3-1)*.01f*power;
            if(g==9){item.regenBonus+=.18f*power;item.damageReduction-=.008f*power;}
        }
    }
}
