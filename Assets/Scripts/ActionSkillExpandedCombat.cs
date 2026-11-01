using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        bool grenadeChargeAwarded;
        bool ActionTurretActive => actionSkillObject && actionSkillHead && actionSkillDuration>0;
        bool PlayerNearActionTurret
        {
            get
            {
                if(!ActionTurretActive || !player)return false;
                Vector3 d=PlayerPosition-actionSkillObject.position;d.y=0;
                if(d.sqrMagnitude<=64f)return true;
                if(!extraTurretObject)return false;
                d=PlayerPosition-extraTurretObject.position;d.y=0;return d.sqrMagnitude<=64f;
            }
        }
        bool PlayerInActionHealingField
        {
            get
            {
                if(!actionSkillObject||actionSkillHead||actionSkillDuration<=0||!player)return false;
                Vector3 d=PlayerPosition-actionSkillObject.position;d.y=0;
                return d.sqrMagnitude<=actionSkillRadius*actionSkillRadius;
            }
        }
        float ActionPositionBonus(string turretId,string medicId)
        {
            return (PlayerNearActionTurret?ActionSkillExtraValue(turretId):0)
                +(PlayerInActionHealingField?ActionSkillExtraValue(medicId):0);
        }
        // A skill keeps the weapon's element at activation; switching weapons
        // while a projectile is in flight cannot change its effect.
        struct ActionElementSnapshot
        {
            public GunElement element;
            public float percent, radius, duration, bonus;
            public int chains;
        }

        ActionElementSnapshot CaptureActionElement(bool enabled)
        {
            if (!enabled || gun == null || gun.element == GunElement.None) return default;
            return new ActionElementSnapshot { element=gun.element, percent=gun.elementPercent,
                radius=gun.elementRadius, duration=gun.elementDuration, chains=gun.elementChains,
                bonus=1f+ArtifactAffixBonus("elementDamage")+ActionSkillElementBonus(gun.element) };
        }

        void ApplyActionElement(Shot shot, ActionElementSnapshot value)
        {
            if (!shot || value.element == GunElement.None) return;
            shot.element=value.element; shot.elementPercent=value.percent;
            shot.elementRadius=value.radius; shot.elementDuration=value.duration;
            shot.elementChains=value.chains; shot.elementBonus=value.bonus;
        }

        void ApplyGrenadeElement(ActionGrenade grenade, Foe primary, float falloff)
        {
            if (primary == null || grenade.element.element == GunElement.None) return;
            // Exactly one elemental impact per grenade: its own arc/AOE handles
            // secondary enemies, rather than re-triggering once per blast victim.
            var holder=new GameObject("Grenade elemental impact");
            var shot=holder.AddComponent<Shot>(); shot.game=this;
            shot.damage=grenade.damage*falloff;
            ApplyActionElement(shot,grenade.element);
            GunElements.Apply(shot,primary,primary.transform.position+Vector3.up*.8f);
            Destroy(holder);
        }

        void ApplyMedicDebuff()
        {
            float debuff=ActionSkillExtraValue("medic_debuff"),damage=ActionSkillExtraValue("medic_damage");
            if ((debuff <= 0 && damage <= 0) || !actionSkillObject) return;
            foreach (var foe in Enemies)
            {
                if (!foe || foe.dead) continue;
                var delta=foe.transform.position-actionSkillObject.position; delta.y=0;
                if (delta.sqrMagnitude>actionSkillRadius*actionSkillRadius || !LineOfSight(actionSkillObject.position,foe.transform.position)) continue;
                // Delay the next attack without repeatedly cancelling a visible wind-up.
                if(debuff>0&&!foe.IsTelegraphing)foe.DelayNextAttack(.6f);
                if(damage>0)foe.Hit(EffectiveDamage*Mathf.Max(.1f,EffectiveFireRate)*Mathf.Max(1,gun!=null?gun.pellets:1)*damage*3f*1.5f,foe.transform.position+Vector3.up*.8f,new Color(.34f,.85f,.58f));
            }
        }
    }
}
