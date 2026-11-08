using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Boss relics retain their persistent IDs and cooldown. Each item has one
        // visible effect; secondary shots cannot trigger another legendary proc.
        void TryBossRelicEffectV2(string id, Vector3 origin)
        {
            if(!BossRelicFoundation.Decode(id,out int boss,out int category))return;
            if(legendaryReady.TryGetValue(id,out float ready)&&legendaryClock<ready)return;
            legendaryReady[id]=legendaryClock+4f;
            legendaryResolving=true;
            try
            {
                Color color=boss==0?new Color(1f,.3f,.1f):boss==1?new Color(.5f,.8f,1f):new Color(1f,.7f,.2f);
                float power=Mathf.Max(12f,gun!=null?gun.damage*1.1f:20f);
                Vector3 source=origin+Vector3.up*.65f;
                Vector3 forward=aim.sqrMagnitude>.01f?aim.normalized:Vector3.forward;
                Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
                List<Foe> targets=LegendaryGunTargets(origin,12f);
                Foe first=targets.Count>0?targets[0]:null;
                if(boss==3)
                {
                    color=new Color(.64f,.90f,.30f);
                    Burst(origin,color,16);
                    switch(category)
                    {
                        case 0: if(first!=null)for(int i=-1;i<=1;i+=2)LegendaryHomingShot(source+side*i*.4f,first,power*.9f,color,null);break;
                        case 1: ScheduleLegendaryPulse(origin,power*.9f,2.4f,2,4,.25f);break;
                        case 2: for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.85f,color,null);break;
                        case 3: if(targets.Count>0)LegendaryHomingShot(source,targets[targets.Count-1],power*2.3f,color,null);break;
                        case 4: for(int i=0;i<8;i++){float a=i*Mathf.PI/4;LegendaryGunShot(source,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.7f,color);}break;
                        case 5: HealPlayer(maxHp*.10f);break;
                        case 6: ArmorBarrier(20);break;
                        case 7: dodgeCooldown=0;break;
                        case 8: invincible=Mathf.Max(invincible,.8f);break;
                        case 9: if(gun!=null){var type=AmmoForGun(gun);ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+18);}break;
                        case 10: LegendaryGunBlast(origin,3.5f,power*1.6f,color,false);break;
                    }
                    return;
                }
                Burst(origin,color,16);
                switch(category)
                {
                    case 0:
                        if(boss==0){for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.85f,color,null);}
                        else if(boss==1){if(first!=null)for(int i=-1;i<=1;i++)LegendaryHomingShot(source+side*i,first,power*.75f,color,null);}
                        else {for(int i=0;i<8;i++){float a=i*Mathf.PI/4;LegendaryGunShot(source,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.65f,color);}}
                        break;
                    case 1:
                        if(boss==0){for(int i=-2;i<=2;i++)LegendaryGunShot(source,Quaternion.Euler(0,i*18,0)*forward,power*.8f,color);}
                        else if(boss==1){for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+forward*i*1.4f,power*.8f,1.4f,1,2,.2f*i);}
                        else LegendaryGunShot(source,forward,power*2.2f,color,GunEffect.Singularity);
                        break;
                    case 2:
                        if(boss==0){for(int i=0;i<Mathf.Min(4,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.7f,color,null);}
                        else if(boss==1){for(int i=0;i<6;i++){float a=i*Mathf.PI/3;LegendaryGunShot(source,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.55f,color);}}
                        else {for(int i=-2;i<=2;i++)LegendaryGunShot(source+side*i*.5f,forward,power*.7f,color);}
                        break;
                    case 3:
                        if(targets.Count>0){var target=targets[targets.Count-1];if(boss==0)LegendaryHomingShot(source,target,power*2.2f,color,null);else if(boss==1){for(int i=0;i<3;i++)ScheduleLegendaryPulse(target.transform.position+side*(i-1),power*.9f,1.3f,1,2,.2f*i);}else LegendaryGunShot(source,target.transform.position-source,power*2.5f,color,GunEffect.Singularity);}
                        break;
                    case 4:
                        if(boss==0){for(int i=0;i<Mathf.Min(4,targets.Count);i++)ScheduleLegendaryPulse(targets[i].transform.position,power*.9f,1.3f,1,4,.2f);}
                        else if(boss==1){for(int i=-2;i<=2;i++)LegendaryGunShot(source,Quaternion.Euler(0,i*12,0)*forward,power*.8f,color);}
                        else {for(int i=0;i<Mathf.Min(4,targets.Count);i++)LegendaryHomingShot(source+side*(i-1.5f)*.5f,targets[i],power*.9f,color,null);}
                        break;
                    case 5:
                        if(boss==0)ScheduleLegendaryPulse(origin,power*.8f,2.8f,3,4);
                        else if(boss==1)HealPlayer(maxHp*.12f);
                        else ArmorBarrier(18);
                        break;
                    case 6:
                        if(boss==0){for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.85f,color,null);}
                        else if(boss==1)legendaryPrecisionUntil=legendaryClock+4f;
                        else if(first!=null)LegendaryGunShot(source,first.transform.position-source,power*1.8f,color,GunEffect.Singularity);
                        break;
                    case 7:
                        if(boss==0){for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin-forward*i*1.2f,power*.75f,1.6f,2,4,.18f*i);}
                        else if(boss==1)dodgeCooldown=0;
                        else invincible=Mathf.Max(invincible,.6f);
                        break;
                    case 8:
                        if(boss==0){for(int i=0;i<8;i++){float a=i*Mathf.PI/4;LegendaryGunShot(source,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.6f,color);}}
                        else if(boss==1)HealPlayer(maxHp*.15f);
                        else ArmorBarrier(14);
                        break;
                    case 9:
                        if(boss==0){for(int i=-2;i<=2;i++)LegendaryGunShot(source+side*i*.5f,forward,power*.72f,color);}
                        else if(boss==1)HealPlayer(maxHp*.18f);
                        else if(gun!=null){var type=AmmoForGun(gun);ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+12);}
                        break;
                    case 10:
                        if(boss==0){for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+forward*i*1.3f,power*.85f,1.6f,1,4,.18f*i);}
                        else if(boss==1){for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.9f,color,null);}
                        else LegendaryGunBlast(origin,3f,power*1.5f,color,false);
                        break;
                }
            }
            finally{legendaryResolving=false;}
        }
    }
}
