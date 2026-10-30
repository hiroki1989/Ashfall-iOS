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
                    LegendaryGunBlast(origin,2.7f,power*.9f,color,true);
                    break;
                case 6: // Debtbreaker: ram the hit foe and damage neighbors at landing.
                    if(first!=null){LegendaryGunPush(first,aim,2);LegendaryGunBlast(first.transform.position,1.6f,power*.65f,color,false);}
                    break;
                case 7: // Funeral Engine: three delayed blasts, increasing radius.
                    for(int i=0;i<3;i++)ScheduleLegendaryPulse(origin,power*.3f,1.1f+i*.7f,1,maker,.25f+i*.2f);
                    break;
                case 8: // Siege Encore: forward cone with outward knockback.
                    foreach(var foe in targets)if(LegendaryGunInCone(origin,aim,foe.transform.position,7,.65f))
                    {LegendaryStrike(foe,power*.7f,color);LegendaryGunPush(foe,foe.transform.position-origin,1.4f);}
                    break;
                case 9: // Hammerfall: three advancing impacts along the dash lane.
                    for(int i=1;i<=3;i++)LegendaryGunBlast(origin+dodgeDirection*(i*1.4f),.8f,power*.5f,color,true);
                    break;

                // Voltarc: electrical paths and interrupted enemy attacks.
                case 10: // Trinity Circuit: jump along three different foes.
                    for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryStrike(targets[i],power*(1-i*.2f),color);
                    break;
                case 11: // Blackout Choir: delay the idle attack timer.
                    if(first!=null&&!first.dead){first.DelayNextAttack(1.3f);LegendaryStrike(first,power*.55f,color);}
                    break;
                case 12: // Dead Battery: leap from the kill site to two survivors.
                    for(int i=0;i<Mathf.Min(2,targets.Count);i++){LegendaryStrike(targets[i],power*.9f,color);targets[i].DelayNextAttack(.7f);}
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
            switch(mechanism)
            {
                case 0: // Prism: three narrow crossing sight lanes.
                    for(int a=-1;a<=1;a++)LegendaryGunShot(source,Quaternion.Euler(0,a*11,0)*forward,power*.36f,color);
                    break;
                case 1: // Twin horizon: two separately guided short-lived rounds.
                    for(int i=0;i<Mathf.Min(2,targets.Count);i++)LegendaryGunShot(source,targets[i].transform.position-source,power*.52f,color);
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
                    if(first!=null){foreach(var foe in targets)if(foe!=first){LegendaryGunShot(first.transform.position+Vector3.up*.5f,foe.transform.position-first.transform.position,power*.68f,color);break;}}
                    break;
                case 8: // Silent trajectory: a precise suppressed follow-up stalls attack.
                    if(first!=null){LegendaryStrike(first,power*.68f,color);first.DelayNextAttack(.65f);}
                    break;
                case 9: // Final bearing: a heavy terminal impact with a small outer ring.
                    LegendaryGunBlast(origin,1.15f,power*.88f,color,true);
                    ScheduleLegendaryPulse(origin,power*.28f,2.0f,1,maker,.28f);
                    break;
            }
            switch(maker)
            {
                case 0:legendaryPrecisionUntil=Mathf.Max(legendaryPrecisionUntil,legendaryClock+1.2f);break;
                case 1:if(first!=null)LegendaryGunPush(first,forward,.55f);break;
                case 2:if(first!=null)first.DelayNextAttack(.4f);break;
                case 3:if(first!=null)LegendaryGunHeal(1.5f);break;
                case 4:if(first!=null)LegendaryGunBlast(first.transform.position,.65f,power*.12f,color,false);break;
                case 5:LegendaryGunBarrier(2);break;
            }
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
            shot.life=.55f;shot.legendaryEffect=true;
        }
        void LegendaryGunPush(Foe foe,Vector3 direction,float distance)
        {if(foe==null||foe.dead)return;direction.y=0;if(direction.sqrMagnitude>.01f)foe.transform.position=MoveWithinRoom(foe.transform.position,direction.normalized*distance,.6f);}
        void LegendaryGunBlast(Vector3 point,float radius,float damage,Color color,bool push)
        {
            CombatVisuals.Impact(point+Vector3.up*.1f,Vector3.up,color,GunEffect.Normal,world);
            foreach(var foe in LegendaryGunTargets(point,radius))
            {LegendaryStrike(foe,damage,color);if(push)LegendaryGunPush(foe,foe.transform.position-point,1.1f);}
        }
        void LegendaryGunHeal(float amount){hp=Mathf.Min(maxHp,hp+amount);}
        void LegendaryGunBarrier(float amount){ArtifactShield=Mathf.Min(60,ArtifactShield+amount);UpdateBarrierVisual();}

        static string DistinctLegendaryGunDescription(int maker,int variant,bool ja)
        {
            if(maker<0||maker>=6||variant<0||variant>=15)return "";
            if(variant>=5){int s=variant-5;string[] mechanismEn={"Three crossing projectiles fan through a narrow prism lane.","Two guided rounds seek separate nearby targets.","Four diagonal rounds form a crossfire.","A delayed impact echoes twice at offset positions.","A thin long lane hits harder at greater distance.","One heavy round seeks the farthest visible enemy.","Two rounds cross in from opposite flanks.","The impact rebounds toward another survivor.","A quiet precision follow-up delays the target attack.","A compact heavy blast ripples outward a moment later."};string[] mechanismJa={"狭い交差射線へ3発の分光弾。","別々の近くの敵を追う誘導弾2発。","斜め4方向へ交差射撃。","命中地点とその先で遅延衝撃が2回発生。","遠い敵ほど強い細い長距離射線。","見えている最遠の敵を狙う重弾1発。","左右から2発の弾が交差。","命中地点から別の生存者へ跳弾。","静かな追撃が敵の攻撃を遅らせる。","小さな重爆発のあと外側へ衝撃が広がる。"};string[] makerEn={" Precise sight for 1.2s."," Push the first target."," Interrupt the first target."," Siphon a little HP."," Scatter fragments at the first target."," Gain a small barrier."};string[] makerJa={" 1.2秒間、照準を強化。"," 最初の敵を押し出す。"," 最初の敵の攻撃を遅らせる。"," 少量のHPを吸収。"," 最初の敵に破片を散らす。"," 少量のシールドを得る。"};return (ja?mechanismJa:mechanismEn)[s]+(ja?makerJa:makerEn)[maker];}
            string[] trigger=ja?new[]{"3射ごと（クールダウン1秒）：","銃弾命中時（クールダウン2秒）：","撃破時（クールダウン2秒）：","リロード時（クールダウン3秒）：","ダッシュ時（クールダウン3秒）："}:new[]{"Every third shot (1s cooldown): ","On bullet hit (2s cooldown): ","On kill (2s cooldown): ","On reload (3s cooldown): ","On dash (3s cooldown): "};
            string[] en={
                "Strike the nearest foe for 150% bonus damage; shots gain 25% damage and perfect accuracy for 3s.",
                "Aim up to three short-lived shots at separate nearby foes, each for 42% bonus damage.",
                "Strike the weakest nearby foe for 220% bonus damage below 40% HP, otherwise 80%.",
                "Pierce up to three foes in an 8m straight lane for 100%, 80%, then 60% bonus damage.",
                "Fire three short-lived rearward shots at -18°, 0°, and 18° for 42% bonus damage each.",
                "Concuss foes within 2.7m for 90% bonus damage and push them back.",
                "Push the hit foe 2m, then blast foes within 1.6m of its landing point for 65% bonus damage.",
                "Leave three delayed shock pulses at the kill site, with radii 1.1m, 1.8m, and 2.5m.",
                "Hit foes in a 7m forward cone for 70% bonus damage and push them outward 1.4m.",
                "Drive three 0.8m impacts forward at 1.4m intervals, each dealing 50% bonus damage and pushing foes back.",
                "Chain strikes through up to three nearby foes for 100%, 80%, then 60% bonus damage.",
                "Strike the hit foe for 55% bonus damage and set its idle attack timer to at least 1.3s.",
                "Strike up to two survivors for 90% bonus damage and set their idle attack timers to at least 0.7s.",
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
                "半径2.7mの敵へ追加威力90%の衝撃と押し出し。",
                "命中した敵を2m押し、その着地点から半径1.6mを追加威力65%で爆破。",
                "撃破地点に半径1.1m・1.8m・2.5mの遅延衝撃波を残す。",
                "前方7mの扇内へ追加威力70%の追撃、外向きに1.4m押す。",
                "前方1.4m間隔で半径0.8mの強打を3回。各追加威力50%と押し出し。",
                "近くの最大3体へ追加威力100%・80%・60%の連鎖追撃。",
                "命中した敵へ追加威力55%の追撃、クールダウン中の攻撃タイマーを最低1.3秒にする。",
                "生存する最大2体へ追加威力90%の追撃、クールダウン中の攻撃タイマーを最低0.7秒にする。",
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
}
