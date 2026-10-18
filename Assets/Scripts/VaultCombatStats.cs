using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        // The existing serialized damage remains the midpoint, including old saves.
        public static float GunDamageMin(Gun item)=>item==null?0:item.damage*.85f;
        public static float GunDamageMax(Gun item)=>item==null?0:item.damage*1.15f;
        public float EffectiveDamageMin=>EffectiveDamage*.85f;
        public float EffectiveDamageMax=>EffectiveDamage*1.15f;
        public static float GunCriticalChance(Gun item)=>item==null?0:Mathf.Clamp01(.05f+GearScoring.AffixTotal(item.affixes,"critChance"));
        public static float GunCriticalMultiplier(Gun item)=>item==null?1.5f:1.5f+GearScoring.AffixTotal(item.affixes,"critDamage");
        float ArmorCriticalBonus(string type){float value=0;foreach(var a in EquippedArmor)if(a!=null)value+=GearScoring.AffixTotal(a.affixes,type);return value;}
        public float EffectiveCriticalChance=>gun==null?0:Mathf.Clamp(GunCriticalChance(gun)+ArmorCriticalBonus("critChance")+ArtifactAffixBonus("critChance")+DebtCriticalChanceBonus(VaultLanguage.DisplayModel(gun)),0,.75f);
        public float EffectiveCriticalMultiplier=>gun==null?1.5f:Mathf.Max(1.5f,GunCriticalMultiplier(gun)+ArmorCriticalBonus("critDamage")+ArtifactAffixBonus("critDamage")+DebtCriticalDamageBonus(VaultLanguage.DisplayModel(gun)));
        public static float MeleeDamageMin(MeleeWeaponItem item)=>item==null?0:item.damage*.85f;
        public static float MeleeDamageMax(MeleeWeaponItem item)=>item==null?0:item.damage*1.15f;
        public float EffectiveMeleeDamageMin(MeleeWeaponItem item)=>MeleeDamage(item)*.85f;
        public float EffectiveMeleeDamageMax(MeleeWeaponItem item)=>MeleeDamage(item)*1.15f;
        // Melee has no firearm family: all acquired critical training is shared.
        public float MeleeCriticalChance=>Mathf.Clamp(.05f+ArmorCriticalBonus("critChance")+ArtifactAffixBonus("critChance")+SumDebtSkill(DebtSkillKind.CriticalChance),0,.75f);
        public float MeleeCriticalMultiplier=>Mathf.Max(1.5f,1.5f+ArmorCriticalBonus("critDamage")+ArtifactAffixBonus("critDamage")+SumDebtSkill(DebtSkillKind.CriticalDamage));
        public float FloorCombatMultiplier=>Mathf.Pow(1.2f,Mathf.Max(0,room));
        GameObject barrierVisual;
        void UpdateBarrierVisual()
        {
            bool visible=ArtifactShield>0&&Active&&player!=null;
            if(!visible){if(barrierVisual!=null)barrierVisual.SetActive(false);return;}
            if(barrierVisual==null)
            {
                barrierVisual=new GameObject("Active shield perimeter");barrierVisual.transform.SetParent(player,false);
                var line=barrierVisual.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.startWidth=line.endWidth=.055f;
                var shader=Shader.Find("Sprites/Default");line.material=new Material(shader);line.startColor=line.endColor=new Color(.2f,.85f,1,.75f);
                for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(angle)*.95f,.38f,Mathf.Sin(angle)*.95f));}
                for(int plane=0;plane<2;plane++)
                {
                    var arc=new GameObject("Shield field arc").AddComponent<LineRenderer>();arc.transform.SetParent(barrierVisual.transform,false);
                    arc.useWorldSpace=false;arc.loop=true;arc.positionCount=64;arc.startWidth=arc.endWidth=.025f;arc.sharedMaterial=line.sharedMaterial;
                    arc.startColor=arc.endColor=new Color(.2f,.85f,1,.38f);
                    for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;float side=Mathf.Cos(angle)*.98f,height=1.04f+Mathf.Sin(angle)*1.02f;arc.SetPosition(i,plane==0?new Vector3(side,height,0):new Vector3(0,height,side));}
                }
                barrierVisual.AddComponent<BarrierMaterialLifetime>();
            }
            barrierVisual.SetActive(true);barrierVisual.transform.localRotation=Quaternion.Euler(0,Time.time*55,0);
            barrierVisual.transform.localScale=Vector3.one*(1+.035f*Mathf.Sin(Time.time*7));
        }
    }
    public sealed class BarrierMaterialLifetime:MonoBehaviour
    {
        void OnDestroy(){var line=GetComponent<LineRenderer>();if(line!=null&&line.sharedMaterial!=null)Destroy(line.sharedMaterial);}
    }
}
