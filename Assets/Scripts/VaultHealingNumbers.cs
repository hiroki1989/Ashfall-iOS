using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        float pendingHealNumber,healNumberAt;
        Transform healNumberWorld;
        bool healNumberCarryFloor;
        static readonly Color HealNumberColor=new Color(.22f,.9f,.47f);

        // Apply first, then accumulate only the effective increase. Fractional
        // regeneration shares one number with other healing in the same interval.
        public float HealPlayer(float amount,bool carryFloor=false)
        {
            if(float.IsNaN(amount)||amount<=0)return 0;
            float before=hp;
            hp=Mathf.Min(maxHp,hp+amount);
            float restored=Mathf.Max(0,hp-before);
            if(restored<=0||!player||!world||(screen!=Screen.Playing&&!(carryFloor&&screen==Screen.Reward)))return restored;
            if(healNumberWorld!=world){pendingHealNumber=0;healNumberWorld=world;}
            healNumberCarryFloor|=carryFloor;
            if(pendingHealNumber<=0)healNumberAt=Time.time+.6f;
            pendingHealNumber+=restored;
            return restored;
        }

        void TickHealNumber()
        {
            if(pendingHealNumber<=0)return;
            if(healNumberCarryFloor&&(screen==Screen.Reward||worldTransitionPending))return;
            if(healNumberCarryFloor&&screen==Screen.Playing&&world&&player)
            {healNumberWorld=world;healNumberCarryFloor=false;}
            if(!world||healNumberWorld!=world||screen!=Screen.Playing||!player)
            {pendingHealNumber=0;healNumberWorld=null;healNumberCarryFloor=false;return;}
            if(!Active||!cam||Time.time<healNumberAt||pendingHealNumber<.05f)return;
            CombatVisuals.DamageNumber(PlayerPosition+Vector3.up*1.7f,pendingHealNumber,HealNumberColor,cam,world,true);
            pendingHealNumber=0;
        }
    }
}
