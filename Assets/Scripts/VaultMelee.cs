using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    /// <summary>Player-facing attack driver for the equipped backup melee weapon.</summary>
    public sealed class VaultMelee : MonoBehaviour
    {
        VaultGame game;
        float cooldown;
        float legendaryReady;
        readonly HashSet<Foe> struckThisSwing=new HashSet<Foe>();
        public float CooldownRemaining=>cooldown;
        public void ShortenCooldown(float seconds){cooldown=Mathf.Max(0,cooldown-Mathf.Max(0,seconds));}

        public void Initialize(VaultGame owner){game=owner;cooldown=0;legendaryReady=0;}
        public void ResetCooldown(){cooldown=0;legendaryReady=0;struckThisSwing.Clear();}
        void Update(){if(game!=null&&game.Active){cooldown=Mathf.Max(0,cooldown-Time.deltaTime);legendaryReady=Mathf.Max(0,legendaryReady-Time.deltaTime);}}

        public bool TryAttack(MeleeWeaponItem weapon,Vector3 direction,Vector3 origin,Transform parent)
        {
            MeleeWeaponCatalog.EnsureAffixes(weapon);
            if(game==null||!game.Active||weapon==null||weapon.damage<=0||weapon.interval<=0||cooldown>0)return false;
            direction.y=0;if(direction.sqrMagnitude<.0001f)direction=Vector3.forward;direction.Normalize();
            cooldown=Mathf.Max(.08f,weapon.interval);
            struckThisSwing.Clear();
            var color=FamilyColor(weapon.family);
            CombatVisuals.MeleeSlash(origin,direction,weapon.range,weapon.arcDegrees,weapon.family,color,parent);
            game.PlayerAppearance?.GetComponent<LayeredPlayer2D>()?.MeleeSwing();
            game.PlayerAppearance?.GetComponent<DetailedHeroPlayer3D>()?.NotifyMelee();


            foreach(var foe in game.Enemies.ToArray())
            {
                if(foe==null||foe.dead||!struckThisSwing.Add(foe))continue;
                Vector3 delta=foe.transform.position-origin;delta.y=0;
                float targetRadius=foe.kind==3?1.2f:.65f;
                if(delta.sqrMagnitude<.0001f||delta.magnitude>weapon.range+targetRadius)continue;
                if(Vector3.Angle(direction,delta)>Mathf.Clamp(weapon.arcDegrees,1,360)*.5f)continue;
                if(!game.LineOfSight(origin+Vector3.up*.7f,foe.transform.position+Vector3.up*.7f))continue;
                Vector3 point=foe.transform.position-direction*(targetRadius*.45f)+Vector3.up*.8f;
                float damage=Random.Range(game.EffectiveMeleeDamageMin(weapon),game.EffectiveMeleeDamageMax(weapon));
                bool critical=Random.value<Mathf.Clamp(game.MeleeCriticalChance+GearScoring.AffixTotal(weapon.affixes,"critChance"),0,.75f);
                if(critical)damage*=game.MeleeCriticalMultiplier+GearScoring.AffixTotal(weapon.affixes,"critDamage");
                Color numberColor=critical?new Color(1,.9f,.12f):(game.ArtifactDamageMultiplier>1.0001f||game.ArtifactAffixBonus("damage")>0)?new Color(1,.22f,.16f):Color.white;
                if(!foe.Hit(damage,point,numberColor,100))continue;
                game.RunHit(foe,damage,point);
                if(weapon.uniqueEffect&&legendaryReady<=0)
                {
                    legendaryReady=3f;
                    game.ApplyLegendaryMeleeHit(weapon,foe,damage);
                }
            }
            return true;
        }

        static Color FamilyColor(MeleeWeaponFamily family)
        {
            switch(family)
            {
                case MeleeWeaponFamily.Shiv:return new Color(.36f,1,.83f);
                case MeleeWeaponFamily.Cleaver:return new Color(1,.48f,.20f);
                case MeleeWeaponFamily.Machete:return new Color(.37f,.85f,1);
                case MeleeWeaponFamily.Hatchet:return new Color(1,.73f,.28f);
                default:return new Color(.76f,.96f,1);
            }
        }
    }
}
