using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Attack speed is a rate, so interval is its reciprocal.
        public float PetDamageMultiplier()
        {
            float bonus=ActionSkillExtraValue("pet_damage")+PetTreeDamageBonus;
            if(EquippedArtifacts!=null)foreach(var item in EquippedArtifacts)
                if(item!=null) { bonus+=GearScoring.AffixTotal(item.affixes,"petDamage"); if(ArtifactCatalog.LegendaryModuleMode(item)==17)bonus+=.75f; }
            return Mathf.Max(0f,1f+bonus);
        }
        public float PetAttackIntervalMultiplier()
        {
            float bonus=ActionSkillExtraValue("pet_speed")+PetTreeSpeedBonus;
            if(EquippedArtifacts!=null)foreach(var item in EquippedArtifacts)
                if(item!=null) { bonus+=GearScoring.AffixTotal(item.affixes,"petSpeed"); if(ArtifactCatalog.LegendaryModuleMode(item)==17)bonus+=.25f; }
            return 1f/Mathf.Max(1f,1f+bonus);
        }
    }
}
