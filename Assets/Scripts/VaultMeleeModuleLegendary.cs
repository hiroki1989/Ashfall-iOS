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
            if(!Active||weapon==null||!weapon.uniqueEffect||struck==null||BossRelicFoundation.Decode(weapon.bossRelicId,out _,out _))return;
            MeleeWeaponCatalog.InferIdentity(weapon);
            int maker=ManufacturerCatalog.Index(weapon.manufacturerId);
            if(maker<0)return;
            Color color=LegendaryEffectColor(maker);
            Vector3 center=struck.transform.position;
            Burst(center,color,5);
            if(weapon.designVariant>=8){ResolveExtendedMeleeSignature(weapon,struck,hitDamage,color,center);return;}
            ResolveIndividualMeleeSignature(weapon,struck,hitDamage,color,center);return;
        }


        void ResolveIndividualMeleeSignature(MeleeWeaponItem item,Foe struck,float damage,Color color,Vector3 center)
        {
            int id=(int)item.family*8+item.designVariant;
            Vector3 source=PlayerPosition+Vector3.up*.7f;Vector3 direction=center-PlayerPosition;direction.y=0;
            if(direction.sqrMagnitude<.001f)direction=aim;
            if(id<15){LegendarySpecialFlight(source,direction,damage*.8f,color,id);return;}
            if(id==17){foreach(var target in LegendaryGunTargets(center,8f))if(target!=struck){LegendaryHomingShot(center+Vector3.up*.7f,target,damage*.8f,color,struck);break;}return;}
            if(id<35){var shot=SpawnBullet(source,direction.normalized,true,damage*.8f,20,color,GunEffect.Normal,100);shot.life=1.2f;shot.legendaryEffect=true;shot.skillProjectile=true;shot.last=struck;var flight=shot.gameObject.AddComponent<LegendaryModuleFlight>();flight.shot=shot;flight.mode=id-15;return;}
            switch(id)
            {
                case 35:{var targets=LegendaryGunTargets(center,8);int n=0;foreach(var target in targets)if(target!=struck){LegendaryHomingShot(center+Vector3.up*.7f,target,damage*.8f,color,struck);if(++n>=4)break;}break;}
                case 36:CombatVisuals.ElementBlast(center,4,world);StrikeNearby(center,struck,4,8,damage,color);break;
                case 37:ArtifactShield=Mathf.Min(30,ArtifactShield+20);UpdateBarrierVisual();break;
                case 38:invincible=Mathf.Max(invincible,1.25f);break;
                case 39:HealPlayer(maxHp*.12f);break;
            }
        }

        // Extra signatures use the saved family/design identity and spawn bounded effects.
        void ResolveExtendedMeleeSignature(MeleeWeaponItem item,Foe struck,float damage,Color color,Vector3 center)
        {
            int id=(int)item.family*2+(item.designVariant-8);
            Vector3 source=PlayerPosition+Vector3.up*.7f;
            Vector3 forward=center-PlayerPosition;forward.y=0;if(forward.sqrMagnitude<.001f)forward=aim;forward.Normalize();
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            int maker=Mathf.Max(0,ManufacturerCatalog.Index(item.manufacturerId));
            switch(id)
            {
                case 0:{var targets=LegendaryGunTargets(center,8f);int n=0;foreach(var foe in targets)if(foe!=struck){LegendaryHomingShot(center+Vector3.up*.7f,foe,damage*.8f,color,struck);if(++n>=4)break;}break;}
                case 1:LegendaryGunShot(source,forward,damage*1.4f,color,GunEffect.Singularity);break;
                case 2:for(int i=0;i<8;i++){float a=i*Mathf.PI/4;LegendaryGunShot(center+Vector3.up*.7f,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),damage*.6f,color);}break;
                case 3:for(int i=1;i<=3;i++)ScheduleLegendaryPulse(center+forward*i*1.25f,damage*.8f,1.4f,1,maker,i*.2f);break;
                case 4:CombatVisuals.ElementBlast(center,4f,world);StrikeNearby(center,struck,4f,8,damage*1.1f,color);break;
                case 5:ScheduleLegendaryPulse(center,damage*.8f,2.5f,3,maker);break;
                case 6:for(int i=-2;i<=2;i++)LegendaryGunShot(source,Quaternion.Euler(0,i*13,0)*forward,damage*.65f,color);break;
                case 7:for(int i=-1;i<=1;i+=2)LegendaryGunShot(source+side*i,Quaternion.Euler(0,-i*20,0)*forward,damage,color);break;
                case 8:{var targets=LegendaryGunTargets(center,10f);for(int i=targets.Count-1;i>=0;i--)if(targets[i]!=struck){LegendaryHomingShot(center+Vector3.up*.7f,targets[i],damage*1.6f,color,struck);break;}break;}
                case 9:HealPlayer(maxHp*.16f);break;
            }
        }

        void CreateLegendaryModuleField(int mode,float power,Color color)
        {
            if(world==null)return;
            var go=new GameObject("Legendary module field");go.transform.SetParent(world,false);go.transform.position=PlayerPosition;
            var field=go.AddComponent<LegendaryModuleField>();field.game=this;field.mode=mode;field.power=power;field.color=color;field.lastKills=kills;
            ObjectiveGroundRing.Create(go.transform,go.transform.position,3f);
            Burst(go.transform.position,color,12);
        }
        public bool TickLegendaryModuleField(LegendaryModuleField field)
        {
            if(field==null||!Active)return false;
            Vector3 center=field.transform.position;bool inside=(PlayerPosition-center).sqrMagnitude<=9f;
            switch(field.mode)
            {
                case 67:if(kills>field.lastKills){HealPlayer(maxHp*.04f*(kills-field.lastKills));field.lastKills=kills;}break;
                case 68:if(inside)HealPlayer(maxHp*.02f);break;
                case 69:if(inside&&gun!=null){var type=AmmoForGun(gun);ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+Mathf.Max(1,Mathf.RoundToInt(AmmoCapacity(type)*.015f)));}break;
                case 70:CombatVisuals.ElementBlast(center,3f,world);StrikeNearby(center,null,3f,8,8*field.power,field.color);break;
                case 71:{var foes=LegendaryGunTargets(center,5f);for(int i=0;i<Mathf.Min(3,foes.Count);i++){CombatVisuals.ElementArc(center+Vector3.up*.5f,foes[i].transform.position+Vector3.up*.7f,world);LegendaryStrike(foes[i],8*field.power,field.color);}break;}
                case 72:ArmorInterceptShots(center,3f,4,field.color);break;
                case 73:{var foes=LegendaryGunTargets(center,10f);if(foes.Count>0)LegendaryGunShot(center+Vector3.up*.7f,foes[0].transform.position-center,8*field.power,field.color);break;}
                case 74:{var foes=LegendaryGunTargets(center,3f);foreach(var foe in foes)LegendaryFreeze.Apply(foe,1.2f);break;}
                case 75:{var foes=LegendaryGunTargets(center,2f);if(foes.Count>0){CombatVisuals.ElementBlast(center,3f,world);StrikeNearby(center,null,3f,8,25*field.power,field.color);return false;}break;}
                case 76:{float angle=field.elapsed*160f*Mathf.Deg2Rad;LegendaryGunShot(center+Vector3.up*.7f,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),8*field.power,field.color);break;}
                case 77:if(inside)ActionSkillOnBulletHit(ActionSkillChargeMax*.035f);break;
                case 78:if(inside&&actionSkillObject&&actionSkillDuration>0)actionSkillDuration+=.4f;break;
                case 79:if(hp<maxHp*.25f){HealPlayer(maxHp*.25f);return false;}break;
            }
            return true;
        }

        void StrikeNearby(Vector3 center,Foe exclude,float radius,int limit,float damage,Color color)
        {
            int hit=0;
            foreach(var foe in Enemies.ToArray())
            {
                if(foe==null||foe.dead||foe==exclude||(foe.transform.position-center).sqrMagnitude>radius*radius||!LineOfSight(center,foe.transform.position))continue;
                foe.Hit(damage,null,color,100,Shot.LegendaryDamageColor);
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
                foe.Hit(damage,null,color,100,Shot.LegendaryDamageColor);if(++hit>=limit)break;
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
                    case 0: ArtifactShield=Mathf.Min(30,ArtifactShield+20);UpdateBarrierVisual();break;
                    case 1: CombatVisuals.ElementBlast(center,2.6f,world);StrikeNearby(center,null,2.6f,3,12*item.power,color);break;
                    case 2: for(int i=-2;i<=2;i++)LegendaryGunShot(center+Vector3.up*.7f,Quaternion.Euler(0,i*12,0)*aim,10*item.power,color);break;
                    case 3:
                        CombatVisuals.ElementBlast(center,3f,world);StrikeNearby(center,null,3f,5,12*item.power,color);
                        break;
                    case 4: invincible=Mathf.Max(invincible,1.25f);break;
                    case 5: ScheduleLegendaryPulse(center,10*item.power,3f,3,4);break;
                    case 6: CombatVisuals.ElementImpact(center,GunElement.Poison,world);ScheduleLegendaryPulse(center,7*item.power,3f,3,3);break;
                    case 7: StrikeLine(center,aim,null,4f,.8f,3,5*item.power,color);break;
                    case 8: for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;LegendaryGunShot(center+Vector3.up*.7f,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),8*item.power,color);}break;
                    case 9:
                        int staggered=0;
                        foreach(var foe in Enemies.ToArray())
                        {
                            if(foe==null||foe.dead||(foe.transform.position-center).sqrMagnitude>6.25f||!LineOfSight(center,foe.transform.position))continue;
                            CombatVisuals.ElementArc(center+Vector3.up*.7f,foe.transform.position+Vector3.up*.7f,world);foe.Hit(10*item.power,null,color,100,Shot.LegendaryDamageColor);
                            if(++staggered>=2)break;
                        }
                        break;
                    case 10: {var targets=LegendaryGunTargets(center,9f);for(int i=0;i<Mathf.Min(3,targets.Count);i++)LegendaryHomingShot(center+Vector3.up*.7f,targets[i],12*item.power,color,null);break;}
                    case 11: LegendaryGunShot(center+Vector3.up*.7f,aim,18*item.power,color,GunEffect.Singularity);break;
                    case 12: CombatVisuals.ElementBlast(center,4f,world);StrikeNearby(center,null,4f,8,16*item.power,color);break;
                    case 13: HealPlayer(maxHp*.08f);break;
                    case 14: if(hp<maxHp*.5f)HealPlayer(maxHp*.15f);break;
                    case 15: if(gun!=null){var type=AmmoForGun(gun);ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+Mathf.Max(1,Mathf.RoundToInt(AmmoCapacity(type)*.1f)));}break;
                    case 16: for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;LegendaryGunShot(center+Vector3.up*.7f,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),12*item.power,color);}break;
                    case 17: break; // Companion bonus is passive while this module is equipped.
                    case 18: {var targets=LegendaryGunTargets(center,9f);if(targets.Count>0)ScheduleLegendaryPulse(targets[0].transform.position,12*item.power,2f,3,Mathf.Max(0,ManufacturerCatalog.Index(item.manufacturerId)));break;}
                    case 19: {Vector3 forward=aim.sqrMagnitude>.001f?aim.normalized:transform.forward;for(int i=1;i<=3;i++)ScheduleLegendaryPulse(center+forward*i*1.5f,12*item.power,1.5f,1,Mathf.Max(0,ManufacturerCatalog.Index(item.manufacturerId)),i*.2f);break;}
                    case 55: ActionSkillOnBulletHit(ActionSkillChargeMax*.2f);break;
                    case 56: if(gun!=null){var type=AmmoForGun(gun);int take=Mathf.Min(ammoReserves[(int)type],Mathf.Min(EffectiveMagazine-ammo,Mathf.Max(1,EffectiveMagazine/4)));if(take>0){ammo+=take;ammoReserves[(int)type]-=take;}}break;
                    case 57: if(reload>0){reload=0;CompleteAmmoReload();}break;
                    case 58: if(actionSkillObject&&actionSkillDuration>0)actionSkillDuration+=4f;break;
                    case 59: ArmorInterceptShots(center,3f,12,color);break;
                    case 60: {Vector3 forward=aim;forward.y=0;if(forward.sqrMagnitude>.01f){forward.Normalize();foreach(var foe in LegendaryGunTargets(center,10f))if(LegendaryGunInLane(center,forward,foe.transform.position,10f,.65f))LegendaryStrike(foe,12*item.power,color);}break;}
                    case 61: {var targets=LegendaryGunTargets(center,10f);if(targets.Count>0){Vector3 pos=targets[targets.Count-1].transform.position;CombatVisuals.ElementBlast(pos,2f,world);foreach(var foe in LegendaryGunTargets(pos,2f))LegendaryStrike(foe,14*item.power,color);}break;}
                    case 62: {var shot=SpawnBullet(center+Vector3.up*.7f,aim,true,12*item.power,22,color,GunEffect.Ricochet,100);shot.life=1.5f;shot.legendaryEffect=true;shot.skillProjectile=true;break;}
                    case 63: for(int i=0;i<ActiveAmmoTypeCount;i++){var type=ActiveAmmoTypeAt(i);ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+Mathf.Max(1,Mathf.RoundToInt(AmmoCapacity(type)*.05f)));}break;
                    case 64: if(ArtifactShield>0){float charge=ArtifactShield;ArtifactShield=0;HealPlayer(charge);UpdateBarrierVisual();}break;
                    case 65: {var targets=LegendaryGunTargets(center,8f);if(targets.Count>0)LegendaryFreeze.Apply(targets[0],2f);break;}
                    case 66: {Foe weakest=null;float ratio=.25f;foreach(var foe in LegendaryGunTargets(center,10f)){if(foe.kind==3||foe.elite)continue;float r=foe.hp/Mathf.Max(1,foe.maxHp);if(r<ratio){ratio=r;weakest=foe;}}if(weakest!=null)LegendaryStrike(weakest,(weakest.hp+1)*2,color);break;}
                    default:
                        if(mode>=67&&mode<=79)CreateLegendaryModuleField(mode,item.power,color);
                        else if(mode>=20&&mode<=34)LegendarySpecialFlight(center+Vector3.up*.7f,aim,12*item.power,color,mode-20);
                        else if(mode>=35&&mode<55){var shot=SpawnBullet(center+Vector3.up*.7f,aim,true,12*item.power,20,color,GunEffect.Normal,100);shot.life=1.2f;shot.legendaryEffect=true;shot.skillProjectile=true;var flight=shot.gameObject.AddComponent<LegendaryModuleFlight>();flight.shot=shot;flight.mode=mode-35;}
                        break;
                }
                Burst(center,color,4);
            }
            finally{resolvingModuleSignature=false;}
        }
    }
    // Short-lived, bounded projectile behavior. Child rounds never trigger another legendary.
    public sealed class LegendaryModuleFlight : MonoBehaviour
    {
        public Shot shot; public int mode;
        float elapsed; bool fired, finished; Vector3 initial;
        void Start(){if(shot)initial=shot.velocity.normalized;}
        void Update()
        {
            if(!shot||!shot.game||!shot.game.Active)return;
            elapsed+=Time.deltaTime;float dt=Time.deltaTime;
            if(mode==0&&elapsed>.32f&&!fired){shot.velocity=-shot.velocity;fired=true;}
            if(mode==1||mode==2)shot.velocity=Quaternion.Euler(0,(mode==1?-100:100)*dt,0)*shot.velocity;
            if(mode==3&&elapsed<.7f)shot.velocity=Quaternion.Euler(0,480*dt,0)*shot.velocity;
            if(mode==4)shot.velocity=Quaternion.Euler(0,65*Mathf.Sin(elapsed*8),0)*initial*20;
            if(mode==5)shot.velocity=initial*(10+Mathf.Min(25,elapsed*35));
            if(mode==6)shot.velocity=initial*Mathf.Max(5,30-elapsed*22);
            if(mode==10&&elapsed>.5f)shot.velocity=initial*36;
            if(mode==11&&elapsed<.9f)shot.velocity=Quaternion.Euler(0,260*dt,0)*shot.velocity;
            if(mode==14){var foes=shot.game.Enemies;Foe near=null;float dist=36;foreach(var f in foes){if(!f||f.dead)continue;float d=(f.transform.position-transform.position).sqrMagnitude;if(d<dist){dist=d;near=f;}}if(near){Vector3 d=near.transform.position-transform.position;d.y=0;if(d.sqrMagnitude>.01f)shot.velocity=Vector3.RotateTowards(shot.velocity,d.normalized*20,3*dt,0);}}
            if(mode==15)shot.velocity=Quaternion.Euler(0,110*Mathf.Sin(elapsed*5),0)*initial*20;
            if(!fired&&elapsed>.35f&&mode>=7&&mode<=9){Pulse(shot.game,transform.position,shot.damage*.8f,mode==9);fired=true;}
            if(!fired&&elapsed>.25f&&mode>=11&&mode<=13){for(int i=0;i<4;i++){float a=mode==12?(i%2==0?90:-90):mode==13?180:i*90;Fire(Quaternion.Euler(0,a,0)*initial,.4f);}fired=true;}
            if(!finished&&elapsed>.95f&&mode>=16){finished=true;var game=shot.game;Vector3 pos=transform.position;if(mode==16)game.GravityWell(pos,shot.damage,Shot.LegendaryDamageColor);else if(mode==17)Pulse(game,pos,shot.damage,false);else if(mode==18){int n=0;foreach(var f in game.Enemies){if(!f||f.dead)continue;Vector3 d=f.transform.position-pos;d.y=0;if(d.sqrMagnitude>.01f&&d.sqrMagnitude<81){FireGuided(d,f,.7f);if(++n>=3)break;}}}else for(int i=0;i<8;i++)Fire(Quaternion.Euler(0,i*45,0)*initial,.5f);}
        }
        void Fire(Vector3 direction,float multiplier){if(!shot||direction.sqrMagnitude<.001f)return;var child=shot.game.SpawnBullet(transform.position,direction.normalized,true,shot.damage*multiplier,18,shot.visualColor,GunEffect.Normal,100);child.life=.7f;child.legendaryEffect=true;child.skillProjectile=true;}
        void FireGuided(Vector3 direction,Foe target,float multiplier){if(!shot||direction.sqrMagnitude<.001f)return;var child=shot.game.SpawnBullet(transform.position,direction.normalized,true,shot.damage*multiplier,18,shot.visualColor,GunEffect.Normal,100);child.life=1.2f;child.legendaryEffect=true;child.skillProjectile=true;var guide=child.gameObject.AddComponent<LegendaryGuidance>();guide.shot=child;guide.target=target;}
        static void Pulse(VaultGame game,Vector3 pos,float damage,bool spore){if(spore)CombatVisuals.ElementImpact(pos,GunElement.Poison,null);else CombatVisuals.ElementBlast(pos,2f,null);foreach(var f in game.Enemies.ToArray())if(f&&!f.dead&&(f.transform.position-pos).sqrMagnitude<=4&&game.LineOfSight(pos,f.transform.position))f.Hit(damage,null,Shot.LegendaryDamageColor,100,Shot.LegendaryDamageColor);}
    }

    public sealed class LegendaryModuleField : MonoBehaviour
    {
        public VaultGame game;public int mode,lastKills;public float power,elapsed;public Color color;
        float next;
        void Update()
        {
            if(!game){Destroy(gameObject);return;}
            if(!game.Active)return;
            elapsed+=Time.deltaTime;if(elapsed>6f){Destroy(gameObject);return;}
            next-=Time.deltaTime;if(next>0)return;next=mode==76?.2f:.75f;
            if(!game.TickLegendaryModuleField(this))Destroy(gameObject);
        }
    }
    public sealed class LegendaryFreeze : MonoBehaviour
    {
        Foe foe;float remaining,nextVisual;bool originallyEnabled;
        public static void Apply(Foe target,float seconds)
        {
            if(!target||target.dead)return;
            var freeze=target.GetComponent<LegendaryFreeze>();if(!freeze){freeze=target.gameObject.AddComponent<LegendaryFreeze>();freeze.foe=target;freeze.originallyEnabled=target.enabled;}
            freeze.remaining=Mathf.Max(freeze.remaining,seconds);target.enabled=false;
        }
        void Update()
        {
            if(!foe||foe.dead){Destroy(this);return;}if(!foe.game||!foe.game.Active)return;
            remaining-=Time.deltaTime;nextVisual-=Time.deltaTime;
            if(nextVisual<=0){CombatVisuals.Burst(transform.position+Vector3.up*.7f,new Color(.55f,.85f,1),8,transform.parent);nextVisual=.4f;}
            if(remaining<=0)Destroy(this);
        }
        void OnDestroy(){if(foe&&!foe.dead)foe.enabled=originallyEnabled;}
    }

}
