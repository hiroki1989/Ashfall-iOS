using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // The caller owns the five catalog triggers and their cooldowns. Every
        // secondary projectile has a short lifetime and a strict per-proc cap.
        void ResolveDistinctLegendaryGun(int maker,int variant,Vector3 origin,Foe struck)
        {
            if(gun==null||world==null||maker<0||maker>=6||variant<0||variant>=15)return;
            if(variant>=5){ResolveExpandedLegendaryGun(maker,variant-5,origin,struck);return;}
            int id=maker*5+variant;
            float power=gun.damage;
            Color color=LegendaryEffectColor(maker);
            Vector3 source=origin+Vector3.up*.65f;
            var targets=LegendaryGunTargets(origin,10);
            Foe first=variant==1?struck:struck!=null&&!struck.dead?struck:targets.Count>0?targets[0]:null;
            Burst(origin,color,13);
            switch(id)
            {
                // Kestrel: deliberate geometry and target selection.
                case 0: // Third Witness: focused third-shot mark.
                    legendaryPrecisionUntil=legendaryClock+3;
                    LegendaryStrike(first,power*1.5f,color);
                    break;
                case 1: // Glass Jury: a three-way fan at separate targets.
                    for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryGunShot(source,targets[i].transform.position-source,power*.42f,color);
                    break;
                case 2: // Last Testament: finish the weakest visible survivor.
                    Foe weakest=null;float ratio=float.MaxValue;
                    foreach(var foe in targets){float r=foe.hp/Mathf.Max(1,foe.maxHp);if(r<ratio){ratio=r;weakest=foe;}}
                    LegendaryStrike(weakest,power*(ratio<=.4f?2.2f:.8f),color);
                    break;
                case 3: // Pale Meridian: pierce the nearest three in a straight lane.
                    int pierced=0;
                    foreach(var foe in targets)if(LegendaryGunInLane(origin,aim,foe.transform.position,8,.75f))
                    {LegendaryStrike(foe,power*(1-pierced*.2f),color);if(++pierced==3)break;}
                    break;
                case 4: // Peregrine: rear-facing cover fire during a dash.
                    for(int i=-1;i<=1;i++)LegendaryGunShot(source,Quaternion.Euler(0,i*18,0)*-dodgeDirection,power*.42f,color);
                    break;

                // Ironclad: displacement and concussive control.
                case 5: // Third Bell: expanding shock at the player.
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;LegendaryGunShot(source,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.55f,color);}
                    break;
                case 6: // Debtbreaker: ram the hit foe and damage neighbors at landing.
                    if(first!=null){int count=0;foreach(var foe in targets)if(foe!=first){LegendaryHomingShot(origin+Vector3.up*.5f,foe,power*.65f,color,first);if(++count==4)break;}}
                    break;
                case 7: // Funeral Engine: three delayed blasts, increasing radius.
                    for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin,power*.3f,1.1f+i*.7f,1,maker,.25f+i*.2f);
                    break;
                case 8: // Siege Encore: forward cone with outward knockback.
                    LegendaryGunShot(source,aim,power*1.6f,color,GunEffect.Singularity);
                    break;
                case 9: // Hammerfall: three advancing impacts along the dash lane.
                    for(int i=1;i<=3;i++)ScheduleLegendaryPulse(origin+dodgeDirection*(i*1.4f),power*.8f,1.4f,1,maker,.18f*i);
                    break;

                // Voltarc: electrical paths and interrupted enemy attacks.
                case 10: // Trinity Circuit: jump along three different foes.
                    for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryStrike(targets[i],power*(1-i*.2f),color);
                    break;
                case 11: // Blackout Choir: delay the idle attack timer.
                    if(first!=null&&!first.dead)for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Vector3 offset=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*1.3f;LegendaryHomingShot(origin+offset+Vector3.up*.5f,first,power*.55f,color,null);}
                    break;
                case 12: // Dead Battery: leap from the kill site to two survivors.
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;LegendaryGunShot(origin+Vector3.up*.5f,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.5f,color);}
                    break;
                case 13: // Storm Reprise: rotating six-spoke discharge.
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;LegendaryGunShot(source,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),power*.3f,color);}
                    break;
                case 14: // Railstorm: a narrow rail through the dash direction.
                    foreach(var foe in targets)if(LegendaryGunInLane(origin,dodgeDirection,foe.transform.position,9,.5f))LegendaryStrike(foe,power*.85f,color);
                    break;

                // Verdant: health conversion and persistent spores.
                case 15: // Blood Dividend: third-shot siphon from nearest target.
                    if(first!=null){LegendaryStrike(first,power*.9f,color);LegendaryGunHeal(3);}
                    break;
                case 16: // Green Leech: direct hit grows a healing seed.
                    if(first!=null){LegendaryStrike(first,power*.45f,color);LegendaryGunHeal(4);}
                    break;
                case 17: // Grave Orchard: three separate spores around the kill.
                    for(int i=-1;i<=1;i++)ScheduleLegendaryPulse(origin+Quaternion.Euler(0,i*60,0)*aim*1.25f,power*.3f,1.1f,2,maker);
                    break;
                case 18: // Second Spring: reload heals more at low health.
                    LegendaryGunHeal(hp<maxHp*.35f?9:4);
                    break;
                case 19: // Spore Runner: dash plants a staggered spore trail.
                    for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin-dodgeDirection*(i*.9f),power*.25f,.85f,2,maker);
                    break;

                // Cinder: shrapnel, fire, and ammunition salvage.
                case 20: // Three Strikes: short-range triple slug.
                    for(int i=-1;i<=1;i++)LegendaryGunShot(source,Quaternion.Euler(0,i*9,0)*aim,power*.52f,color);
                    break;
                case 21: // Scrapyard Saint: impact throws two flanking shards.
                    if(first!=null)for(int i=-1;i<=1;i+=2)LegendaryGunShot(source,Quaternion.Euler(0,i*28,0)*(first.transform.position-source),power*.4f,color);
                    break;
                case 22: // Ash Inheritance: corpse-centered detonation.
                    LegendaryGunBlast(origin,3,power*.8f,color,false);
                    break;
                case 23: // Last Cartridge: reload launches one heavy explosive round.
                    LegendaryGunShot(source,aim,power*1.5f,color,GunEffect.Singularity);
                    break;
                case 24: // Firewalk: overlapping embers behind the dash.
                    for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin-dodgeDirection*(i*1.15f),power*.35f,1.05f,2,maker);
                    break;

                // Aegis: barrier charge and protective counterfire.
                case 25: // Third Guardian: charge barrier and clear nearby threats.
                    LegendaryGunBarrier(5);LegendaryGunBlast(PlayerPosition,1.8f,power*.4f,color,false);
                    break;
                case 26: // Blue Reprisal: impact refunds barrier, lashes the hitter.
                    if(first!=null){LegendaryGunBarrier(4);LegendaryStrike(first,power*.65f,color);}
                    break;
                case 27: // Widow's Pension: kill converts to barrier and a ward pulse.
                    LegendaryGunBarrier(7);ScheduleLegendaryPulse(PlayerPosition,power*.35f,1.7f,2,maker);
                    break;
                case 28: // Emergency Protocol: reload grants barrier and two cover shots.
                    LegendaryGunBarrier(6);
                    for(int i=-1;i<=1;i+=2)LegendaryGunShot(source,Quaternion.Euler(0,i*22,0)*aim,power*.45f,color);
                    break;
                case 29: // Escort Duty: dash cover fire from both flanks.
                    LegendaryGunBarrier(3);
                    Vector3 side=Vector3.Cross(Vector3.up,dodgeDirection).normalized*.8f;
                    LegendaryGunShot(source+side,dodgeDirection,power*.45f,color);
                    LegendaryGunShot(source-side,dodgeDirection,power*.45f,color);
                    break;
            }
        }

        // The ten later relic mechanisms have distinct projectile paths or hit
        // consequences. Manufacturer signatures are applied after the mechanism,
        // so no two makers are stat-only reskins of the same relic.
        void ResolveExpandedLegendaryGun(int maker,int mechanism,Vector3 origin,Foe struck)
        {
            float power=gun.damage;Color color=LegendaryEffectColor(maker);
            Vector3 source=origin+Vector3.up*.65f;
            var targets=LegendaryGunTargets(origin,10);
            Foe first=struck!=null&&!struck.dead?struck:targets.Count>0?targets[0]:null;
            Vector3 forward=aim.sqrMagnitude>.01f?aim.normalized:Vector3.forward;
            Burst(origin,color,11);
            if(ResolveIndividualLegendaryGun(maker,mechanism,origin,source,forward,targets,first,struck,power,color))return;
            switch(mechanism)
            {
                case 0: // Prism: three narrow crossing sight lanes.
                    for(int a=-1;a<=1;a++)LegendaryGunShot(source,Quaternion.Euler(0,a*11,0)*forward,power*.36f,color);
                    break;
                case 1: // Twin horizon: two separately guided short-lived rounds.
                    for(int i=0;i<Mathf.Min(2,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.52f,color,struck);
                    break;
                case 2: // Cross examination: four diagonal trajectories.
                    foreach(int a in new[]{-31,-12,12,31})LegendaryGunShot(source,Quaternion.Euler(0,a,0)*forward,power*.27f,color);
                    break;
                case 3: // Ghost measure: an echo at the contact point, then another behind it.
                    ScheduleLegendaryPulse(origin,power*.52f,1.15f,1,maker,.22f);
                    ScheduleLegendaryPulse(origin+forward*1.3f,power*.38f,1.0f,1,maker,.52f);
                    break;
                case 4: // Long vigil: a thin escalating lane, strongest at distance.
                    foreach(var foe in targets)if(LegendaryGunInLane(origin,forward,foe.transform.position,10,.48f))
                        LegendaryStrike(foe,power*(.36f+.045f*Vector3.Distance(origin,foe.transform.position)),color);
                    break;
                case 5: // Needle court: a single distant priority target.
                    if(targets.Count>0)LegendaryGunShot(source,targets[targets.Count-1].transform.position-source,power*.85f,color);
                    break;
                case 6: // White feint: two crossing shots from offset flanks.
                    {Vector3 side=Vector3.Cross(Vector3.up,forward).normalized*.7f;
                     LegendaryGunShot(source+side,Quaternion.Euler(0,-17,0)*forward,power*.48f,color);
                     LegendaryGunShot(source-side,Quaternion.Euler(0,17,0)*forward,power*.48f,color);}
                    break;
                case 7: // Second angle: impact rebounds toward another survivor.
                    {int count=0;foreach(var foe in targets)if(foe!=struck){LegendaryHomingShot(origin+Vector3.up*.5f,foe,power*.42f,color,struck);if(++count==3)break;}}
                    break;
                case 8: // Silent trajectory: a precise suppressed follow-up stalls attack.
                    if(first!=null&&!first.dead)for(int i=-1;i<=1;i++)LegendaryHomingShot(source+Vector3.Cross(Vector3.up,forward)*i*.55f,first,power*.5f,color,null);
                    break;
                case 9: // Final bearing: a heavy terminal impact with a small outer ring.
                    LegendaryGunBlast(origin,1.15f,power*.88f,color,false);
                    ScheduleLegendaryPulse(origin,power*.28f,2.0f,1,maker,.28f);
                    break;
            }

        }

        // Individual models have one signature effect each. Existing maker and
        // variant IDs remain unchanged so previously acquired guns migrate safely.
        bool ResolveIndividualLegendaryGun(int maker,int mechanism,Vector3 origin,Vector3 source,Vector3 forward,List<Foe> targets,Foe first,Foe struck,float power,Color color)
        {
            if(maker!=1&&maker!=2)return ResolveMoreIndividualLegendaryGun(maker,mechanism,origin,source,forward,targets,first,struck,power,color);
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            if(maker==1)
            {
                switch(mechanism)
                {
                    case 0: LegendarySpecialFlight(source,forward,power*.9f,color,6); break;
                    case 1: LegendaryGunShot(source,forward,power*1.8f,color,GunEffect.Singularity); break;
                    case 2: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+forward*i*1.5f,power*.65f,1.2f,1,maker,.18f*i); break;
                    case 3: LegendarySpecialFlight(source,forward,power*.9f,color,7); break;
                    case 4: LegendarySpecialFlight(source,forward,power*.9f,color,8); break;
                    case 5: if(first!=null)ScheduleLegendaryPulse(first.transform.position,power*1.3f,2.3f,1,maker,.35f); break;
                    case 6: for(int i=0;i<3;i++)LegendaryGunShot(source+side*(i-1)*.8f,forward,power*.65f,color); break;
                    case 7: for(int i=0;i<Mathf.Min(4,targets.Count);i++)ScheduleLegendaryPulse(targets[i].transform.position,power*.45f,1.1f,1,maker,.3f); break;
                    case 8: if(first!=null)for(int i=-1;i<=1;i++)LegendaryHomingShot(source+side*i*1.1f,first,power*.62f,color,null); break;
                    case 9: for(int i=0;i<4;i++)ScheduleLegendaryPulse(origin-forward*i*1.2f,power*.55f,1.5f,1,maker,.16f*i); break;
                }
                return true;
            }
            switch(mechanism)
            {
                case 0: for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.48f,color,null); break;
                case 1: LegendarySpecialFlight(source,forward,power*.9f,color,9); break;
                case 2: if(first!=null)for(int i=0;i<4;i++){float a=i*Mathf.PI/2;Vector3 offset=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*1.6f;LegendaryHomingShot(origin+offset+Vector3.up*.5f,first,power*.48f,color,null);} break;
                case 3: LegendarySpecialFlight(source,forward,power*.85f,color,10); break;
                case 4: LegendarySpecialFlight(source,forward,power*.9f,color,11); break;
                case 5: if(targets.Count>0)for(int i=0;i<Mathf.Min(4,targets.Count);i++)LegendaryHomingShot(source+side*(i-1.5f)*.35f,targets[targets.Count-1-i],power*.5f,color,null); break;
                case 6: for(int i=0;i<6;i++)LegendaryGunShot(source,Quaternion.Euler(0,i*60,0)*forward,power*.42f,color); break;
                case 7: {int n=0;foreach(var foe in targets)if(foe!=struck){LegendaryHomingShot(origin+Vector3.up*.5f,foe,power*.5f,color,struck);if(++n==4)break;}} break;
                case 8: if(first!=null)for(int i=-2;i<=2;i++)LegendaryHomingShot(source+side*i*.7f,first,power*.4f,color,null); break;
                case 9: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+side*(i-1)*1.4f,power*.6f,1.15f,1,maker,.2f*i); break;
            }
            return true;
        }

        bool ResolveMoreIndividualLegendaryGun(int maker,int mechanism,Vector3 origin,Vector3 source,Vector3 forward,List<Foe> targets,Foe first,Foe struck,float power,Color color)
        {
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            switch(maker*10+mechanism)
            {
                case 0: LegendarySpecialFlight(source,forward,power*.72f,color,0);break;
                case 1: if(first!=null)for(int i=-1;i<=1;i++)LegendaryHomingShot(source+side*i*1.2f,first,power*.55f,color,null);break;
                case 2: LegendarySpecialFlight(PlayerPosition+Vector3.up*.65f,forward,power*.7f,color,1);break;
                case 3: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+forward*i*2f,power*.65f,.8f,1,maker,.2f*i);break;
                case 4: for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(source,targets[targets.Count-1-i],power*.65f,color,null);break;
                case 5: LegendaryGunShot(source,forward,power*1.75f,color,GunEffect.Singularity);break;
                case 6: LegendarySpecialFlight(source,forward,power*.9f,color,2);break;
                case 7: {int n=0;foreach(var f in targets)if(f!=struck){LegendaryHomingShot(origin+Vector3.up*.5f,f,power*.6f,color,struck);if(++n==3)break;}}break;
                case 8: if(first!=null)for(int i=0;i<4;i++){float a=i*Mathf.PI/2;LegendaryHomingShot(origin+new Vector3(Mathf.Sin(a),.5f,Mathf.Cos(a))*1.5f,first,power*.45f,color,null);}break;
                case 9: for(int i=0;i<6;i++)LegendaryGunShot(source,Quaternion.Euler(0,60*i,0)*forward,power*.5f,color);break;
                case 30: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+side*(i-1)*1.6f,power*.48f,1.2f,2,maker,.2f*i);break;
                case 31: if(first!=null)ScheduleLegendaryPulse(first.transform.position,power*.75f,1.8f,2,maker,.25f);break;
                case 32: for(int i=0;i<4;i++)ScheduleLegendaryPulse(origin+forward*i*1.3f,power*.48f,1.1f,2,maker,.2f*i);break;
                case 33: LegendarySpecialFlight(source,forward,power*.8f,color,3);break;
                case 34: if(first!=null)for(int i=-1;i<=1;i++)LegendaryHomingShot(source+side*i,first,power*.6f,color,null);break;
                case 35: LegendaryGunShot(source,forward,power*1.6f,color,GunEffect.Singularity);break;
                case 36: for(int i=-2;i<=2;i++)LegendaryGunShot(source+side*i*.45f,Quaternion.Euler(0,i*8,0)*forward,power*.45f,color);break;
                case 37: {int n=0;foreach(var f in targets)if(f!=struck){LegendaryHomingShot(origin+Vector3.up*.5f,f,power*.65f,color,struck);if(++n==3)break;}}break;
                case 38: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+forward*i*1.6f,power*.55f,1.35f,2,maker,.25f*i);break;
                case 39: LegendarySpecialFlight(source,forward,power*.85f,color,12);break;
                case 40: LegendarySpecialFlight(source,forward,power*.85f,color,4);break;
                case 41: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+side*(i-1)*1.3f,power*.7f,1.35f,1,maker,.2f*i);break;
                case 42: LegendaryGunBlast(origin,3f,power*1.2f,color,false);break;
                case 43: LegendaryGunShot(source,forward,power*1.9f,color,GunEffect.Singularity);break;
                case 44: for(int i=0;i<4;i++)ScheduleLegendaryPulse(origin-forward*i*1.2f,power*.6f,1.25f,2,maker,.15f*i);break;
                case 45: for(int i=-1;i<=1;i++)LegendaryGunShot(source+side*i*.8f,forward,power*.72f,color);break;
                case 46: if(first!=null)for(int i=-1;i<=1;i++)LegendaryHomingShot(source+side*i,first,power*.65f,color,null);break;
                case 47: {int n=0;foreach(var f in targets)if(f!=struck){LegendaryHomingShot(origin+Vector3.up*.5f,f,power*.7f,color,struck);if(++n==3)break;}}break;
                case 48: for(int i=0;i<5;i++)ScheduleLegendaryPulse(origin+forward*i,power*.55f,1f,1,maker,.12f*i);break;
                case 49: LegendarySpecialFlight(source,forward,power*.9f,color,13);break;
                case 50: LegendarySpecialFlight(source,forward,power*.9f,color,5);break;
                case 51: if(first!=null)for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;LegendaryHomingShot(origin+new Vector3(Mathf.Sin(a),.5f,Mathf.Cos(a))*1.3f,first,power*.65f,color,null);}break;
                case 52: LegendarySpecialFlight(source,forward,power*.9f,color,14);break;
                case 53: for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin+side*(i-1)*1.5f,power*.65f,1.2f,1,maker,.25f*i);break;
                case 54: for(int i=0;i<Mathf.Min(4,targets.Count);i++)LegendaryHomingShot(source,targets[i],power*.48f,color,null);break;
                case 55: LegendaryGunShot(source,forward,power*1.7f,color,GunEffect.Singularity);break;
                case 56: for(int i=-1;i<=1;i++)LegendaryGunShot(source+side*i*.9f,forward,power*.7f,color);break;
                case 57: {int n=0;foreach(var f in targets)if(f!=struck){LegendaryHomingShot(origin+Vector3.up*.5f,f,power*.6f,color,struck);if(++n==4)break;}}break;
                case 58: if(first!=null)for(int i=-2;i<=2;i++)LegendaryHomingShot(source+side*i*.7f,first,power*.4f,color,null);break;
                case 59: for(int i=0;i<4;i++)ScheduleLegendaryPulse(origin+forward*i*1.25f,power*.6f,1.3f,1,maker,.18f*i);break;
                default:return false;
            }
            return true;
        }

        List<Foe> LegendaryGunTargets(Vector3 origin,float radius)
        {
            var result=new List<Foe>();float limit=radius*radius;
            foreach(var foe in Enemies)if(foe!=null&&!foe.dead&&(foe.transform.position-origin).sqrMagnitude<=limit&&LineOfSight(origin+Vector3.up*.5f,foe.transform.position+Vector3.up*.5f))result.Add(foe);
            result.Sort((a,b)=>(a.transform.position-origin).sqrMagnitude.CompareTo((b.transform.position-origin).sqrMagnitude));
            return result;
        }
        static bool LegendaryGunInLane(Vector3 origin,Vector3 direction,Vector3 point,float length,float width)
        {Vector3 delta=point-origin;delta.y=0;direction.y=0;direction.Normalize();float forward=Vector3.Dot(delta,direction);return forward>0&&forward<length&&(delta-direction*forward).sqrMagnitude<width*width;}
        static bool LegendaryGunInCone(Vector3 origin,Vector3 direction,Vector3 point,float length,float cosine)
        {Vector3 delta=point-origin;delta.y=0;direction.y=0;return delta.sqrMagnitude<length*length&&delta.sqrMagnitude>.001f&&Vector3.Dot(delta.normalized,direction.normalized)>cosine;}
        void LegendaryGunShot(Vector3 source,Vector3 direction,float damage,Color color,GunEffect effect=GunEffect.Normal)
        {
            direction.y=0;if(direction.sqrMagnitude<.01f)return;
            var shot=SpawnBullet(source,direction.normalized,true,damage,22,color,effect,100);
            shot.life=.55f;shot.legendaryEffect=true;shot.skillProjectile=true;
        }
        void LegendaryHomingShot(Vector3 source,Foe target,float damage,Color color,Foe ignore)
        {
            if(target==null||target.dead)return;
            Vector3 direction=target.transform.position-source;direction.y=0;
            if(direction.sqrMagnitude<.01f)return;
            var shot=SpawnBullet(source,direction.normalized,true,damage,22,color,GunEffect.Normal,100);
            shot.life=1.5f;shot.legendaryEffect=true;shot.skillProjectile=true;shot.last=ignore;
            var guidance=shot.gameObject.AddComponent<LegendaryGuidance>();guidance.shot=shot;guidance.target=target;
        }
        // Real projectiles with readable changes in trajectory and bounded child count.
        void LegendarySpecialFlight(Vector3 source,Vector3 direction,float damage,Color color,int mode)
        {
            direction.y=0;if(direction.sqrMagnitude<.01f)return;
            var shot=SpawnBullet(source,direction.normalized,true,damage,20,color,GunEffect.Normal,100);
            shot.life=1.35f;shot.legendaryEffect=true;shot.skillProjectile=true;
            var flight=shot.gameObject.AddComponent<LegendarySpecialFlight>();flight.shot=shot;flight.mode=mode;if(mode==1)flight.splitDistance=5f;
        }
        void LegendaryGunPush(Foe foe,Vector3 direction,float distance)
        {if(foe==null||foe.dead)return;direction.y=0;if(direction.sqrMagnitude>.01f)foe.transform.position=MoveWithinRoom(foe.transform.position,direction.normalized*distance,.6f);}
        void LegendaryGunBlast(Vector3 point,float radius,float damage,Color color,bool push)
        {
            CombatVisuals.Impact(point+Vector3.up*.1f,Vector3.up,color,GunEffect.Normal,world);
            foreach(var foe in LegendaryGunTargets(point,radius))
            {LegendaryStrike(foe,damage,color);if(push)LegendaryGunPush(foe,foe.transform.position-point,1.1f);}
        }
        void LegendaryGunHeal(float amount){HealPlayer(amount);}
        void LegendaryGunBarrier(float amount){ArtifactShield=Mathf.Min(60,ArtifactShield+amount);UpdateBarrierVisual();}

        static string DistinctLegendaryGunDescription(int maker,int variant,bool ja)
        {
            if(maker<0||maker>=6||variant<0||variant>=15)return "";
            string[] triggers=ja?new[]{"3射ごと（クールダウン1秒）：","銃弾命中時（クールダウン2秒）：","撃破時（クールダウン2秒）：","リロード時（クールダウン3秒）：","ダッシュ時（クールダウン3秒）："}:new[]{"Every third shot (1s cooldown): ","On bullet hit (2s cooldown): ","On kill (2s cooldown): ","On reload (3s cooldown): ","On dash (3s cooldown): "};
            if(variant>=5 && maker!=1 && maker!=2){
                string[,] jaMore={
                {"飛行中に3発へ分裂する弾を放つ。","3発の誘導弾が1体に集中する。","5m飛行後に左右へ分裂する弾を放つ。","3回の爆発が前方に進む。","遠い敵を最大3発の誘導弾が追う。","重力渦を作る特異弾を発射。","大きく弧を描いて戻る弾を放つ。","撃破地点から最大3発の誘導弾が別の敵を追う。","敵の周りから4発の誘導弾が集中する。","6方向に弾を放つ。"},
                {"横に並んだ3か所に胞子の爆発を残す。","敵の位置に2回脈動する胞子を残す。","前方4か所に胞子の爆発を残す。","飛行中に5方向へ分裂する胞子弾を放つ。","3発の誘導胞子弾が敵に集中する。","重力渦を作る胞子弾を発射。","5発の胞子弾が横に広がる。","撃破地点から最大3発の誘導胞子弾が別の敵を追う。","前方3か所に2回脈動する胞子を残す。","人間の敵に継続ダメージを与える毒弾を放つ。"},
                {"飛行中に5発へ扇状に分裂する弾を放つ。","3回の爆発が横に並んで発生。","撃破地点に大爆発を起こす。","強力な特異弾で重力渦を作る。","後方4か所に2回脈動する爆発を残す。","横一列に重い弾を3発発射。","3発の誘導弾が敵を追う。","撃破地点から最大3発の誘導弾が別の敵を追う。","5回の爆発が前方に進む。","命中地点の周囲にもダメージを与える爆発弾を放つ。"},
                {"左右にジグザグに進む弾を放つ。","敵の周りから3発の誘導弾が集中する。","減速しながら進み最大5体へ連鎖する電撃弾を放つ。","横に並んだ3回の爆発が順に発生。","最大4体の敵を誘導弾が追う。","重力渦を作る特異弾を発射。","横一列に3発の弾を放つ。","撃破地点から最大4発の誘導弾が別の敵を追う。","5発の誘導弾が1体に集中する。","4回の爆発が前方に進む。"}};
                string[,] enMore={
                {"Fire a round that splits into three in flight.","Three guided rounds converge on one target.","Fire a round that splits left and right after 5m.","Three explosions march forward.","Up to three guided rounds chase distant enemies.","Fire a gravity well round.","Fire a round that curves back along a wide arc.","A kill releases up to three guided rounds at other enemies.","Four guided rounds converge from around the target.","Fire in six directions."},
                {"Leave three pulsing spore zones in a row.","Plant a two-pulse spore zone at the enemy.","Leave four pulsing spore zones ahead.","Fire a spore round that splits into five directions in flight.","Three guided spore rounds converge on one target.","Fire a gravity well spore round.","Five spore rounds spread in parallel.","A kill releases up to three guided spores at other enemies.","Leave three two-pulse spore zones ahead.","Fire a poison round that applies damage over time to human enemies."},
                {"Fire a round that splits into a five-round fan in flight.","Three explosions sweep sideways.","The kill erupts in a large explosion.","Fire a powerful gravity well round.","Leave four pulsing explosions behind.","Fire three heavy parallel rounds.","Three guided rounds chase the target.","A kill releases up to three guided rounds at other enemies.","Five explosions march forward.","Fire an explosive round that damages enemies near its target."},
                {"Fire a round that travels in a zigzag.","Three guided rounds converge around the target.","Fire a slowing electric round that chains to up to five enemies.","Three explosions sweep sideways.","Up to four guided rounds seek enemies.","Fire a gravity well round.","Fire three parallel rounds.","A kill releases up to four guided rounds at other enemies.","Five guided rounds converge on one target.","Four explosions march forward."}};
                int group=maker==0?0:maker==3?1:maker==4?2:3;
                return triggers[variant%5]+(ja?jaMore:enMore)[group,variant-5];
            }
            if(variant>=5 && (maker==1||maker==2)){
                string[,] enSpecial={{"Fire an accelerating round that sheds alternating side shots.","Launch a heavy singularity round that creates a gravity well.","Three explosions march forward in sequence.","Fire a traveling round that releases radial shots in sequence.","Fire a sweeping round that curves left and right.","Mark a nearby enemy for a delayed large explosion.","Fire three heavy parallel slugs.","Plant delayed charges at up to four nearby enemy positions.","Three guided slugs converge from widely spaced lanes.","Four explosions travel backwards along the trail."},{"Three guided bolts seek separate enemies.","Fire an electric round that chains to up to three nearby enemies.","Four guided bolts converge from around the target.","Fire an orbiting round that leaves shots behind its path.","Fire an incendiary round with bonus fire damage.","Up to four guided bolts hunt the farthest enemies.","Six electric bolts radiate from the firing direction.","A kill releases up to four guided bolts at survivors.","Five guided bolts converge on one target.","Three blasts sweep sideways across the impact."}};
                string[,] jaSpecial={{"加速しながら左右に交互に弾を放つ。","重力渦を作る強力な特異弾を発射。","3回の爆発が前方へ順に進む。","進みながら放射状に弾を順次放つ。","左右に曲がりながら進む弾を放つ。","近くの敵の位置に遅れて大爆発を起こす。","横に並んだ3発の重い弾を発射。","近くの最大4体の位置に遅れて爆発する爆薬を設置。","離れた3方向から誘導弾が敵に集中。","4回の爆発が後方へ順に進む。"},{"3発の誘導電撃弾が別々の敵を追う。","命中地点から最大3体へ連鎖する電撃弾を放つ。","敵の周囲から4発の誘導電撃弾が集中。","回転しながら後方に弾を残す弾を放つ。","命中時に炎の追加ダメージを与える弾を放つ。","遠い敵から最大4体を誘導弾で追う。","射撃方向を基準に6方向へ電撃弾を放つ。","撃破地点から最大4発の誘導電撃弾が生存する敵を追う。","5発の誘導電撃弾が1体に集中。","3回の爆発が左右に並んで発生。"}};
                return triggers[variant%5]+(ja?jaSpecial:enSpecial)[maker-1,variant-5];
            }
            if(variant>=5){int s=variant-5;string[] mechanismEn={"Three crossing projectiles fan through a narrow prism lane.","Two guided rounds seek separate nearby targets.","Four diagonal rounds form a crossfire.","A delayed impact echoes twice at offset positions.","A thin long lane hits harder at greater distance.","One heavy round seeks the farthest visible enemy.","Two rounds cross in from opposite flanks.","A kill releases up to three homing rounds aimed at survivors.","Three guided rounds converge on the target.","A compact heavy blast ripples outward a moment later."};string[] mechanismJa={"狭い交差射線へ3発の分光弾。","別々の近くの敵を追う誘導弾2発。","斜め4方向へ交差射撃。","命中地点とその先で遅延衝撃が2回発生。","遠い敵ほど強い細い長距離射線。","見えている最遠の敵を狙う重弾1発。","左右から2発の弾が交差。","撃破地点から最大3発の弾が生存する敵を追尾。","3発の誘導弾が敵を追い、集中攻撃。","小さな重爆発のあと外側へ衝撃が広がる。"};string[] makerEn={" Precise sight for 1.2s."," Push the first target."," Interrupt the first target."," Siphon a little HP."," Scatter fragments at the first target."," Gain a small barrier."};string[] makerJa={" 1.2秒間、照準を強化。"," 最初の敵を押し出す。"," 最初の敵の攻撃を遅らせる。"," 少量のHPを吸収。"," 最初の敵に破片を散らす。"," 少量のシールドを得る。"};return (ja?mechanismJa:mechanismEn)[s];}
            string[] trigger=ja?new[]{"3射ごと（クールダウン1秒）：","銃弾命中時（クールダウン2秒）：","撃破時（クールダウン2秒）：","リロード時（クールダウン3秒）：","ダッシュ時（クールダウン3秒）："}:new[]{"Every third shot (1s cooldown): ","On bullet hit (2s cooldown): ","On kill (2s cooldown): ","On reload (3s cooldown): ","On dash (3s cooldown): "};
            string[] en={
                "Strike the nearest foe for 150% bonus damage; shots gain 25% damage and perfect accuracy for 3s.",
                "Aim up to three short-lived shots at separate nearby foes, each for 42% bonus damage.",
                "Strike the weakest nearby foe for 220% bonus damage below 40% HP, otherwise 80%.",
                "Pierce up to three foes in an 8m straight lane for 100%, 80%, then 60% bonus damage.",
                "Fire three short-lived rearward shots at -18°, 0°, and 18° for 42% bonus damage each.",
                "Fire eight radial rounds, each dealing 55% weapon damage.",
                "The hit releases up to four guided rounds toward other enemies, each dealing 65% weapon damage.",
                "Leave three delayed shock pulses at the kill site, with radii 1.1m, 1.8m, and 2.5m.",
                "Launch a singularity round dealing 160% weapon damage and creating a gravity well.",
                "Three staggered explosions advance along the dash path, each dealing 80% weapon damage.",
                "Chain strikes through up to three nearby foes for 100%, 80%, then 60% bonus damage.",
                "Three guided electric rounds converge on the hit enemy, each dealing 55% weapon damage.",
                "Six electric bolts burst outward from the kill, each dealing 50% weapon damage.",
                "Fire six short-lived radial bolts for 30% bonus damage each.",
                "Strike all foes in a narrow 9m lane for 85% bonus damage each.",
                "Siphon the nearest foe for 90% bonus damage and restore 3 HP on contact.",
                "Strike the hit foe for 45% bonus damage and restore 4 HP.",
                "Plant three spore zones around the kill site; each pulses twice.",
                "Restore 9 HP if below 35% health, otherwise restore 4 HP.",
                "Plant three spore zones behind the dash; each pulses twice.",
                "Fire three short-lived forward slugs at -9°, 0°, and 9° for 52% bonus damage each.",
                "Throw two short-lived shards around the hit foe for 40% bonus damage each.",
                "Detonate the kill site, damaging foes within 3m for 80% bonus damage.",
                "Launch one short-lived singularity round for 150% bonus damage; impact or expiry creates a gravity well.",
                "Leave three overlapping ember zones behind the dash; each pulses twice.",
                "Gain 5 barrier and strike foes within 1.8m for 40% bonus damage.",
                "On hit, gain 4 barrier and strike that foe for 65% bonus damage.",
                "Gain 7 barrier and create two ward pulses within 1.7m of the player.",
                "Gain 6 barrier and fire two short-lived cover shots at ±22° for 45% bonus damage each.",
                "Gain 3 barrier and fire two short-lived forward shots from the dash flanks for 45% bonus damage each."};
            string[] jp={
                "最寄の敵へ追加威力150%の追撃。3秒間、銃弾威力25%増・精度100。",
                "近くの別々の敵へ短命弾を最大3発狙って発射。各追加威力42%。",
                "近くの最もHP割合が低い敵へ追撃。HP40%以下なら追加威力220%、それ以外は80%。",
                "前方8mの直線内で最大3体を貫く追撃。追加威力100%・80%・60%。",
                "後方へ-18°・0°・18°の短命弾3発。各追加威力42%。",
                "全周に8発の弾を放つ。各弾は武器威力の55%。",
                "命中地点から別の敵を追う誘導弾を最大4発放つ。各弾は武器威力の65%。",
                "撃破地点に半径1.1m・1.8m・2.5mの遅延衝撃波を残す。",
                "武器威力160%の特異弾を発射し、着弾地点に重力渦を作る。",
                "ダッシュ方向へ3回の爆発が順に進む。各爆発は武器威力の80%。",
                "近くの最大3体へ追加威力100%・80%・60%の連鎖追撃。",
                "命中した敵へ3発の電撃誘導弾が集中する。各弾は武器威力の55%。",
                "撃破地点から6方向へ電撃弾が広がる。各弾は武器威力の50%。",
                "全周6方向へ短命の電撃弾。各追加威力30%。",
                "前方9mの細い直線内の敵すべてへ追加威力85%の追撃。",
                "最寄の敵へ追加威力90%の吸命追撃、命中時HP3回復。",
                "命中した敵へ追加威力45%の追撃、HP4回復。",
                "撃破地点の周囲に胞子領域3つを設置。各2回脈動。",
                "HP35%未満ならHP9、それ以外はHP4回復。",
                "ダッシュ後方へ胞子領域3つを設置。各2回脈動。",
                "前方-9°・0°・9°へ短命弾3発。各追加威力52%。",
                "命中した敵の左右へ短命の破片弾2発。各追加威力40%。",
                "撃破地点から半径3mを追加威力80%で爆破。",
                "追加威力150%の短命特異弾を1発発射。着弾か寿命切れで重力渦を作る。",
                "ダッシュ後方へ重なった火種領域3つを設置。各2回脈動。",
                "シールド5獲得、半径1.8mの敵へ追加威力40%の追撃。",
                "命中時にシールド4獲得、その敵へ追加威力65%の追撃。",
                "シールド7獲得、プレイヤー周囲1.7mへ2回の守護波。",
                "シールド6獲得、左右22°へ短命の援護弾2発。各追加威力45%。",
                "シールド3獲得、ダッシュ両側から短命の前方援護弾2発。各追加威力45%。"};
            return trigger[variant]+(ja?jp:en)[maker*5+variant];
        }
    }
    // Steer a real secondary projectile toward a moving enemy, rather than
    // applying an instantaneous hit. The shot keeps its normal wall collision.
    public sealed class LegendaryGuidance : MonoBehaviour
    {
        public Shot shot;
        public Foe target;
        void Update()
        {
            if(shot==null||target==null||target.dead||shot.game==null||!shot.game.Active)return;
            Vector3 direction=target.transform.position-transform.position;direction.y=0;
            if(direction.sqrMagnitude<.001f)return;
            float speed=shot.velocity.magnitude;
            shot.velocity=Vector3.RotateTowards(shot.velocity,direction.normalized*speed,5f*Time.deltaTime,0f);
        }
    }

    // The initial projectile may hit before it splits. All fragments are
    // secondary damage and cannot trigger another legendary proc on hit.
    public sealed class LegendarySpecialFlight : MonoBehaviour
    {
        public Shot shot;
        public int mode; public float splitDistance; Vector3 startPosition;
        float elapsed;
        float nextRelease;
        int released;
        Vector3 initialDirection;
        void Start(){startPosition=transform.position;if(shot!=null)initialDirection=shot.velocity.normalized;}
        void Update()
        {
            if(shot==null||shot.game==null||!shot.game.Active)return;
            elapsed+=Time.deltaTime;
            if(mode==2){shot.velocity=Quaternion.Euler(0,240f*Time.deltaTime,0)*shot.velocity;return;}
            if(mode==5){shot.velocity=Quaternion.Euler(0,30f*Mathf.Sin(elapsed*14f),0)*initialDirection*20f;return;}
            if(mode>=6)
            {
                if(mode==6)shot.velocity=initialDirection*(12f+Mathf.Min(elapsed*30f,18f));
                if(mode==8)shot.velocity=Quaternion.Euler(0,120f*Mathf.Sin(elapsed*4f),0)*initialDirection*20f;
                if(mode==9){shot.element=GunElement.Electric;shot.elementPercent=.45f;shot.elementRadius=4f;shot.elementChains=3;}
                if(mode==10)shot.velocity=Quaternion.Euler(0,150f*Time.deltaTime,0)*shot.velocity;
                if(mode==11){shot.element=GunElement.Fire;shot.elementPercent=.9f;}
                if(mode==12){shot.element=GunElement.Poison;shot.elementPercent=.25f;shot.elementDuration=5f;}
                if(mode==13){shot.element=GunElement.Explosive;shot.elementPercent=.65f;shot.elementRadius=2.5f;}
                if(mode==14){shot.velocity=initialDirection*(32f-Mathf.Min(elapsed*18f,22f));shot.element=GunElement.Electric;shot.elementPercent=.3f;shot.elementRadius=2.5f;shot.elementChains=5;}
                if((mode==6||mode==7||mode==10)&&elapsed>=nextRelease&&released<5)
                {
                    var emitterGame=shot.game;Vector3 emitterPosition=transform.position;
                    Vector3 d=mode==6?Quaternion.Euler(0,released%2==0?90f:-90f,0)*initialDirection:mode==7?Quaternion.Euler(0,released*72f,0)*initialDirection:-initialDirection;
                    var child=emitterGame.SpawnBullet(emitterPosition,d,true,shot.damage*.4f,15f,shot.visualColor,GunEffect.Normal,100);
                    child.life=.65f;child.legendaryEffect=true;child.skillProjectile=true;released++;nextRelease=elapsed+.18f;
                }
                return;
            }
            if(splitDistance>0f){if((transform.position-startPosition).sqrMagnitude<splitDistance*splitDistance)return;}
            else if(elapsed<(mode==1?.25f:mode==4?.30f:.22f))return;

            var game=shot.game;Vector3 direction=shot.velocity.normalized;Vector3 source=transform.position;
            int count=mode==0?3:mode==1?2:5;
            for(int i=0;i<count;i++)
            {
                float angle=mode==3?i*72f:mode==4?(i-2)*17f:mode==1?(i==0?-35f:35f):(i-1)*18f;
                var fragment=game.SpawnBullet(source,Quaternion.Euler(0,angle,0)*direction,true,shot.damage*.65f,21f,shot.visualColor,GunEffect.Normal,100);
                fragment.life=.65f;fragment.legendaryEffect=true;fragment.skillProjectile=true;fragment.last=shot.last;
            }
            Destroy(gameObject);
        }
    }

}
