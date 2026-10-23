using UnityEngine;

namespace NeonVault
{
    // Signatures are keyed by saved catalog identity; no extra save fields are needed.
    public partial class VaultGame
    {
        bool resolvingModuleSignature;
        string MeleeLegendaryDetail(MeleeWeaponItem item)=>item!=null&&item.uniqueEffect?"\n"+LegendaryRedOpen+MeleeWeaponCatalog.LegendaryDescription(item)+LegendaryRedClose:"";

        public void ApplyLegendaryMeleeHit(MeleeWeaponItem weapon,Foe struck,float hitDamage)
        {
            if(!Active||weapon==null||!weapon.uniqueEffect||struck==null)return;
            MeleeWeaponCatalog.InferIdentity(weapon);
            int maker=ManufacturerCatalog.Index(weapon.manufacturerId);
            if(maker<0)return;
            int mode=MeleeWeaponCatalog.LegendaryMode(weapon);
            Color color=LegendaryEffectColor(maker);
            Vector3 center=struck.transform.position;
            Burst(center,color,5);
            switch(maker)
            {
                case 0:
                    if(mode==0)StrikeNearby(center,struck,2.8f,1,hitDamage*.45f,color);
                    else if(mode==1)meleeAttack?.ShortenCooldown(.25f);
                    else if(mode==2)StrikeLine(PlayerPosition,center-PlayerPosition,struck,3.6f,.65f,2,hitDamage*.3f,color);
                    else {struck.DelayNextAttack(.7f);meleeAttack?.ShortenCooldown(.15f);}
                    break;
                case 1:
                    if(mode==0)PushFoe(struck,center-PlayerPosition,1.4f);
                    else if(mode==1)struck.DelayNextAttack(.8f);
                    else if(mode==2)StrikeNearby(center,struck,1.8f,2,hitDamage*.4f,color);
                    else {PushFoe(struck,center-PlayerPosition,.8f);struck.DelayNextAttack(.55f);}
                    break;
                case 2:
                    if(mode==0)StrikeNearby(center,struck,3.2f,2,hitDamage*.32f,color);
                    else if(mode==1)dodgeCooldown=Mathf.Max(0,dodgeCooldown-.6f);
                    else if(mode==2)StrikeLine(PlayerPosition,center-PlayerPosition,struck,4f,.8f,3,hitDamage*.28f,color);
                    if(mode==3){PushNearby(center,struck,2.5f,.9f);dodgeCooldown=Mathf.Max(0,dodgeCooldown-.25f);}
                    break;
                case 3:
                    if(mode==0)hp=Mathf.Min(maxHp,hp+3);
                    else if(mode==1)struck.DelayNextAttack(1.1f);
                    else if(mode==2)PullNearby(center,struck,2.8f,.8f);
                    if(mode==3){StrikeNearby(center,struck,2f,2,hitDamage*.22f,color);hp=Mathf.Min(maxHp,hp+1);}
                    break;
                case 4:
                    if(mode==0)StrikeNearby(center,struck,2.3f,3,hitDamage*.38f,color);
                    else if(mode==1)PushNearby(center,struck,2.3f,1f);
                    else if(mode==2)StrikeLine(PlayerPosition,center-PlayerPosition,struck,3.8f,1f,3,hitDamage*.42f,color);
                    if(mode==3){PushFoe(struck,center-PlayerPosition,1f);StrikeNearby(center,struck,1.7f,2,hitDamage*.25f,color);}
                    break;
                case 5:
                    if(mode==0)ArtifactShield=Mathf.Min(30,ArtifactShield+4);
                    else if(mode==1)invincible=Mathf.Max(invincible,.25f);
                    else if(mode==2){ArtifactShield=Mathf.Min(30,ArtifactShield+2);PushNearby(center,struck,2f,.8f);}
                    if(mode==3){ArtifactShield=Mathf.Min(30,ArtifactShield+2);invincible=Mathf.Max(invincible,.15f);struck.DelayNextAttack(.75f);}
                    break;
            }
        }

        void StrikeNearby(Vector3 center,Foe exclude,float radius,int limit,float damage,Color color)
        {
            int hit=0;
            foreach(var foe in Enemies.ToArray())
            {
                if(foe==null||foe.dead||foe==exclude||(foe.transform.position-center).sqrMagnitude>radius*radius||!LineOfSight(center,foe.transform.position))continue;
                foe.Hit(damage,null,color,100);
                if(++hit>=limit)break;
            }
        }

        void PushFoe(Foe foe,Vector3 direction,float distance)
        {
            if(foe==null||foe.dead)return;
            direction.y=0;
            if(direction.sqrMagnitude<.001f)return;
            foe.transform.position=MoveWithinRoom(foe.transform.position,direction.normalized*distance,.6f);
        }

        void PushNearby(Vector3 center,Foe exclude,float radius,float distance)
        {
            foreach(var foe in Enemies.ToArray())if(foe!=null&&!foe.dead&&foe!=exclude&&(foe.transform.position-center).sqrMagnitude<=radius*radius&&LineOfSight(center,foe.transform.position))PushFoe(foe,foe.transform.position-center,distance);
        }

        void PullNearby(Vector3 center,Foe exclude,float radius,float distance)
        {
            foreach(var foe in Enemies.ToArray())if(foe!=null&&!foe.dead&&foe!=exclude&&(foe.transform.position-center).sqrMagnitude<=radius*radius&&LineOfSight(center,foe.transform.position))PushFoe(foe,center-foe.transform.position,distance);
        }

        void StrikeLine(Vector3 start,Vector3 direction,Foe exclude,float length,float width,int limit,float damage,Color color)
        {
            direction.y=0;if(direction.sqrMagnitude<.001f)return;direction.Normalize();int hit=0;
            foreach(var foe in Enemies.ToArray())
            {
                if(foe==null||foe.dead||foe==exclude)continue;
                Vector3 delta=foe.transform.position-start;delta.y=0;float forward=Vector3.Dot(delta,direction);
                if(forward<=0||forward>length||Vector3.Cross(direction,delta).sqrMagnitude>width*width||!LineOfSight(start,foe.transform.position))continue;
                foe.Hit(damage,null,color,100);if(++hit>=limit)break;
            }
        }

        void ApplyLegendaryModuleSignature(ArtifactItem item)
        {
            int mode=ArtifactCatalog.LegendaryModuleMode(item);
            if(mode<0||resolvingModuleSignature||!Active)return;
            resolvingModuleSignature=true;
            try
            {
                Vector3 center=PlayerPosition;
                Color color=LegendaryEffectColor(Mathf.Max(0,ManufacturerCatalog.Index(item.manufacturerId)));
                switch(mode)
                {
                    case 0: ArtifactShield=Mathf.Min(30,ArtifactShield+3);break;
                    case 1: StrikeNearby(center,null,2.6f,3,6*item.power,color);break;
                    case 2: dodgeCooldown=Mathf.Max(0,dodgeCooldown-.45f);break;
                    case 3:
                        foreach(var foe in Enemies.ToArray())if(foe!=null&&!foe.dead&&(foe.transform.position-center).sqrMagnitude<=9&&LineOfSight(center,foe.transform.position))foe.DelayNextAttack(.6f);
                        break;
                    case 4: invincible=Mathf.Max(invincible,.2f);break;
                    case 5: PushNearby(center,null,3f,1.2f);break;
                    case 6: PullNearby(center,null,3f,.9f);break;
                    case 7: StrikeLine(center,aim,null,4f,.8f,3,5*item.power,color);break;
                    case 8: if(meleeAttack!=null)meleeAttack.ShortenCooldown(meleeAttack.CooldownRemaining);break;
                    case 9:
                        int staggered=0;
                        foreach(var foe in Enemies.ToArray())
                        {
                            if(foe==null||foe.dead||(foe.transform.position-center).sqrMagnitude>6.25f||!LineOfSight(center,foe.transform.position))continue;
                            if(foe.Hit(4*item.power,null,color,100)&&!foe.dead)foe.DelayNextAttack(.7f);
                            if(++staggered>=2)break;
                        }
                        break;
                }
                Burst(center,color,4);
            }
            finally{resolvingModuleSignature=false;}
        }
    }
}
