using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // The selected skill and ranks are owned by the progression system. These
        // transient objects belong to the current world and never enter a save file.
        Transform actionSkillObject, actionSkillHead, extraTurretObject, extraTurretHead;
        float extraTurretFireTimer;
        Foe primaryTurretFocus,secondaryTurretFocus;
        int primaryTurretShots,secondaryTurretShots;
        ActionElementSnapshot turretElement;
        Material actionSkillRingMaterial;
        float actionSkillDuration, actionSkillFireTimer, actionSkillPulseTimer;
        float actionSkillPower, actionSkillRadius;
        bool actionSkillFollows, actionSkillMultitarget;
        AudioClip actionTurretSound, actionGrenadeSound, actionActivationSound;
        bool actionAudioLoaded;
        readonly List<ActionGrenade> actionGrenades=new List<ActionGrenade>();
        readonly Dictionary<Foe,int> actionGrenadeHits=new Dictionary<Foe,int>();

        void PlayActionSound(bool grenade)
        {
            if(!actionAudioLoaded)
            {
                actionTurretSound=Resources.Load<AudioClip>("ActionSkillAudio/turret");
                actionGrenadeSound=Resources.Load<AudioClip>("ActionSkillAudio/grenade");
                actionAudioLoaded=true;
            }
            AudioClip clip=grenade?actionGrenadeSound:actionTurretSound;
            if(clip&&audioSource)audioSource.PlayOneShot(clip,grenade ? .7f : .4f);
        }

        sealed class ActionGrenade
        {
            public Transform visual;
            public Vector3 start, end;
            public float age, travel, damage, radius;
            public bool cluster;
            public ActionElementSnapshot element;
        }

        int ASRank(ActionSkillKind kind,int branch,int tier)=>Mathf.Clamp(ActionSkillRank(kind,branch,tier),0,1);
        float ASPath(ActionSkillKind kind,int branch)
        {
            float n=0;
            for(int tier=0;tier<4;tier++)
            {
                // These ranks already grant a passive player stat. They must not
                // also add an action-device bonus to the same node.
                if(tier==1 && !(branch==1 && kind!=ActionSkillKind.Medic))continue;
                if(tier==3 && branch==1)continue;
                n+=Mathf.Clamp(ActionSkillRank(kind,branch,tier),0,6)/6f;
            }
            return n;
        }

        public void ActionSkillOnBulletHit(float charge)
        {
            if(charge<=0)return;
            bool wasReady=ActionSkillPoints>=ActionSkillChargeMax;
            ActionSkillPoints=Mathf.Min(ActionSkillChargeMax,ActionSkillPoints+charge);
            if(!wasReady&&ActionSkillPoints>=ActionSkillChargeMax)BeginActionSkillTutorial();
        }

        public bool TryUseActionSkill()
        {
            if(!Active||player==null||world==null||art==null||ActionSkillPoints<ActionSkillChargeMax)return false;
            ClearActiveActionSkill();
            ActionSkillPoints=0;
            switch(ActionSkillSelected)
            {
                case ActionSkillKind.Mechanic: DeployActionTurret();break;
                case ActionSkillKind.Bomber: ThrowActionGrenades();break;
                case ActionSkillKind.Medic: DeployActionHealingField();break;
            }
            if(!actionActivationSound)actionActivationSound=Resources.Load<AudioClip>("ActionSkillAudio/activation");
            if(actionActivationSound&&audioSource)audioSource.PlayOneShot(actionActivationSound,.48f);
            return true;
        }

        void DeployActionTurret()
        {
            var k=ActionSkillKind.Mechanic;
            float fire=ASPath(k,0),movement=ASPath(k,1);
            actionSkillDuration=Mathf.Min(22,12+movement*1.5f+ASRank(k,1,4)*5+ActionSkillExtraValue("turret_duration"));
            actionSkillPower=EffectiveDamage*(.5f+fire*.07f+ASRank(k,0,4)*.12f);
            actionSkillRadius=14+ActionSkillExtraValue("turret_range");
            turretElement=CaptureActionElement(ActionSkillExtraValue("turret_element")>0);
            primaryTurretFocus=null;secondaryTurretFocus=null;primaryTurretShots=0;secondaryTurretShots=0;
            // The mobile platform becomes useful during progression rather than
            // requiring the final node. Retain the previous capstone path too.
            actionSkillFollows=ActionSkillExtraValue("turret_follow")>0;
            actionSkillMultitarget=ASRank(k,2,4)>0;
            actionSkillFireTimer=.2f;
            actionSkillObject=new GameObject("Mechanic / field sentry").transform;
            actionSkillObject.SetParent(world,false);
            Vector3 origin=MoveWithinRoom(PlayerPosition,aim.normalized*1.5f,.6f);origin.y=.08f;actionSkillObject.position=origin;
            var dark=new Color(.19f,.21f,.19f);var steel=new Color(.39f,.37f,.30f);var brass=new Color(.67f,.39f,.14f);
            art.Part("Sentry mounting foot",PrimitiveType.Cylinder,new Vector3(0,.10f,0),new Vector3(.55f,.11f,.55f),dark,actionSkillObject);
            art.Part("Sentry turntable rim",PrimitiveType.Cylinder,new Vector3(0,.23f,0),new Vector3(.46f,.035f,.46f),brass,actionSkillObject);
            for(int i=0;i<3;i++)
            {float a=i*Mathf.PI*2/3;var support=art.Part("Sentry support leg",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.27f,.10f,Mathf.Cos(a)*.27f),new Vector3(.13f,.12f,.54f),steel,actionSkillObject);support.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);art.Part("Sentry rubber foot",PrimitiveType.Cylinder,new Vector3(Mathf.Sin(a)*.53f,.055f,Mathf.Cos(a)*.53f),new Vector3(.16f,.045f,.16f),dark,actionSkillObject);}
            art.Part("Sentry rotating post",PrimitiveType.Cylinder,new Vector3(0,.36f,0),new Vector3(.24f,.26f,.24f),steel,actionSkillObject);
            actionSkillHead=new GameObject("Sentry aiming head").transform;actionSkillHead.SetParent(actionSkillObject,false);actionSkillHead.localPosition=new Vector3(0,.66f,0);
            art.Part("Receiver casing",PrimitiveType.Cube,new Vector3(0,0,0),new Vector3(.47f,.30f,.48f),dark,actionSkillHead);
            art.Part("Feed box",PrimitiveType.Cube,new Vector3(-.31f,-.06f,-.02f),new Vector3(.17f,.25f,.30f),steel,actionSkillHead);
            art.Part("Barrel jacket",PrimitiveType.Cylinder,new Vector3(0,0,.36f),new Vector3(.12f,.28f,.12f),steel,actionSkillHead).localRotation=Quaternion.Euler(90,0,0);
            art.Part("Sentry sight",PrimitiveType.Cube,new Vector3(0,.19f,.02f),new Vector3(.14f,.08f,.18f),brass,actionSkillHead);
            art.Part("Receiver upper rail",PrimitiveType.Cube,new Vector3(0,.17f,-.08f),new Vector3(.25f,.035f,.34f),steel,actionSkillHead);
            art.Part("Feed box lid",PrimitiveType.Cube,new Vector3(-.31f,.075f,-.02f),new Vector3(.19f,.035f,.31f),dark,actionSkillHead);
            art.Part("Feed box latch",PrimitiveType.Cube,new Vector3(-.405f,-.01f,.07f),new Vector3(.025f,.07f,.06f),brass,actionSkillHead);
            art.Part("Optics lens",PrimitiveType.Cylinder,new Vector3(0,.19f,.125f),new Vector3(.075f,.012f,.075f),new Color(.48f,.72f,.66f),actionSkillHead).localRotation=Quaternion.Euler(90,0,0);
            art.Part("Barrel muzzle",PrimitiveType.Cylinder,new Vector3(0,0,.66f),new Vector3(.075f,.08f,.075f),dark,actionSkillHead).localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<4;i++)art.Part("Barrel cooling collar",PrimitiveType.Cylinder,new Vector3(0,0,.20f+i*.105f),new Vector3(.14f,.018f,.14f),dark,actionSkillHead).localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<3;i++)art.Part("Receiver fastener",PrimitiveType.Cylinder,new Vector3(.24f,-.08f+i*.08f,-.16f),new Vector3(.035f,.012f,.035f),brass,actionSkillHead).localRotation=Quaternion.Euler(0,0,90);
            if(ActionSkillExtraValue("turret_double")>0)
            {
                extraTurretObject=Instantiate(actionSkillObject.gameObject,world).transform;
                extraTurretObject.name="Mechanic / second sentry";
                extraTurretHead=extraTurretObject.Find("Sentry aiming head");
                Vector3 side=Vector3.Cross(Vector3.up,aim.normalized)*1.8f;
                var pos=MoveWithinRoom(PlayerPosition,side,.6f);extraTurretObject.position=new Vector3(pos.x,origin.y,pos.z);
                extraTurretFireTimer=.35f;
            }
            CombatVisuals.Burst(origin+Vector3.up*.3f,new Color(.8f,.66f,.35f),9,world);
        }

        void ThrowActionGrenades()
        {
            grenadeChargeAwarded=false;
            actionGrenadeHits.Clear();
            var k=ActionSkillKind.Bomber;
            int count=Mathf.Clamp(1+Mathf.FloorToInt(ASPath(k,0))+3*ASRank(k,0,4)+Mathf.RoundToInt(ActionSkillExtraValue("grenade_count")),1,12);
            float radius=Mathf.Min(7f,2.7f+ASPath(k,1)*.35f+ASRank(k,1,4)*1.25f+ActionSkillExtraValue("grenade_radius"));
            float weaponDps=gun!=null?EffectiveDamage*Mathf.Max(1,gun.pellets)*EffectiveFireRate:EffectiveDamage*7.7f;
            float damage=Mathf.Max(18,weaponDps/7.7f)*(20.0f+ASPath(k,2)*1.0f)*(1+ActionSkillExtraValue("grenade_damage"));
            var grenadeElement=CaptureActionElement(ActionSkillExtraValue("grenade_element")>0);
            Vector3 forward=aim.sqrMagnitude>.01f?aim.normalized:player.forward;forward.y=0;
            for(int i=0;i<count;i++)
            {
                float spread=count==1?0:Mathf.Lerp(-40,40,(float)i/(count-1));
                Vector3 direction=Quaternion.Euler(0,spread,0)*forward;
                Vector3 start=PlayerPosition+Vector3.up*.6f;
                Vector3 end=MoveWithinRoom(PlayerPosition,direction*(5.3f+(i%2)*1.1f),.22f);end.y=.16f;
                var root=new GameObject("Bomber / thrown grenade").transform;root.SetParent(world,false);root.position=start;
                art.Part("Grenade body",PrimitiveType.Capsule,Vector3.zero,new Vector3(.19f,.16f,.19f),new Color(.32f,.34f,.22f),root);
                art.Part("Grenade cap",PrimitiveType.Cylinder,new Vector3(0,.16f,0),new Vector3(.12f,.04f,.12f),new Color(.42f,.39f,.28f),root);
                actionGrenades.Add(new ActionGrenade{visual=root,start=start,end=end,travel=.6f+i*.045f,damage=damage,radius=radius,cluster=ASRank(k,2,4)>0,element=grenadeElement});
            }
        }

        void DeployActionHealingField()
        {
            var k=ActionSkillKind.Medic;
            float recovery=ASPath(k,0),radius=ASPath(k,1),duration=ASPath(k,2);
            actionSkillPower=Mathf.Min(.09f,(.028f+recovery*.006f+ASRank(k,0,4)*.025f)*(1+ActionSkillExtraValue("medic_heal")));
            actionSkillRadius=3.0f+radius*.45f+ASRank(k,1,4)*1.6f+ActionSkillExtraValue("medic_radius");
            actionSkillDuration=Mathf.Min(24,12+duration*1.5f+ASRank(k,2,4)*5+ActionSkillExtraValue("medic_duration"));
            actionSkillFollows=ActionSkillExtraValue("medic_follow")>0;
            actionSkillPulseTimer=0;
            actionSkillObject=new GameObject("Medic / healing field").transform;
            actionSkillObject.SetParent(world,false);var p=PlayerPosition;p.y=.10f;actionSkillObject.position=p;
            var shader=Resources.Load<Shader>("CombatGlow")??Shader.Find("Sprites/Default");
            if(shader!=null)
            {
                actionSkillRingMaterial=new Material(shader){name="Medic field glow"};
                var ring=actionSkillObject.gameObject.AddComponent<LineRenderer>();ring.sharedMaterial=actionSkillRingMaterial;ring.useWorldSpace=false;ring.loop=true;ring.positionCount=64;
                ring.startWidth=ring.endWidth=.085f;ring.startColor=ring.endColor=new Color(.25f,.9f,.64f,.65f);ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
                for(int i=0;i<ring.positionCount;i++){float a=i*Mathf.PI*2/ring.positionCount;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*actionSkillRadius,0,Mathf.Sin(a)*actionSkillRadius));}
            }
            art.Part("Medical field beacon",PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(.22f,.12f,.22f),new Color(.36f,.45f,.39f),actionSkillObject);
            CombatVisuals.Burst(actionSkillObject.position,new Color(.34f,.85f,.58f),8,world);
        }

        public void TickActionSkill(float delta)
        {
            if(!Active||delta<=0)return;
            for(int i=actionGrenades.Count-1;i>=0;i--)
            {
                var grenade=actionGrenades[i];if(!grenade.visual){actionGrenades.RemoveAt(i);continue;}
                grenade.age+=delta;float t=Mathf.Clamp01(grenade.age/grenade.travel);
                grenade.visual.position=Vector3.Lerp(grenade.start,grenade.end,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.5f);
                grenade.visual.Rotate(260*delta,130*delta,0);
                if(t<1)continue;
                ExplodeActionGrenade(grenade);Destroy(grenade.visual.gameObject);actionGrenades.RemoveAt(i);
            }
            if(!actionSkillObject)return;
            actionSkillDuration-=delta;
            if(actionSkillDuration<=0){ClearActionDevice();return;}
            if(actionSkillFollows)
            {
                var current=actionSkillObject.position;Vector3 target=PlayerPosition-aim.normalized*1.4f;target.y=current.y;
                Vector3 offset=target-current;offset.y=0;
                if(offset.magnitude>1.8f){var moved=MoveWithinRoom(new Vector3(current.x,PlayerPosition.y,current.z),Vector3.ClampMagnitude(offset,delta*3.5f),.45f);actionSkillObject.position=new Vector3(moved.x,current.y,moved.z);}
            }
            if(extraTurretObject&&actionSkillFollows)
            {
                Vector3 target=PlayerPosition+Vector3.Cross(Vector3.up,aim.normalized)*1.8f;
                var offset=target-extraTurretObject.position;offset.y=0;
                if(offset.sqrMagnitude>3.24f){var moved=MoveWithinRoom(new Vector3(extraTurretObject.position.x,PlayerPosition.y,extraTurretObject.position.z),Vector3.ClampMagnitude(offset,delta*3.5f),.45f);extraTurretObject.position=new Vector3(moved.x,extraTurretObject.position.y,moved.z);}
            }
            if(actionSkillHead){TickActionTurret(delta,actionSkillHead,ref actionSkillFireTimer);if(extraTurretHead)TickActionTurret(delta,extraTurretHead,ref extraTurretFireTimer);}
            else
            {
                Vector3 separation=PlayerPosition-actionSkillObject.position;separation.y=0;
                if(separation.sqrMagnitude<=actionSkillRadius*actionSkillRadius&&LineOfSight(actionSkillObject.position,PlayerPosition))
                {
                    HealPlayer(maxHp*actionSkillPower*delta);
                }
                actionSkillPulseTimer-=delta;if(actionSkillPulseTimer<=0){actionSkillPulseTimer=1.5f;ApplyMedicDebuff();CombatVisuals.Burst(actionSkillObject.position+Vector3.up*.1f,new Color(.28f,.72f,.44f),3,world);}
            }
        }

        void TickActionTurret(float delta,Transform head,ref float fireTimer)
        {
            fireTimer-=delta;if(fireTimer>0)return;
            Foe target=null;float best=actionSkillRadius*actionSkillRadius;
            foreach(var foe in Enemies)if(foe&&!foe.dead)
            {Vector3 d=foe.transform.position-head.position;d.y=0;float distance=d.sqrMagnitude;if(distance<best&&LineOfSight(head.position,foe.transform.position)){best=distance;target=foe;}}
            if(!target){fireTimer=.25f;return;}
            Vector3 direction=target.transform.position-head.position;direction.y=0;if(direction.sqrMagnitude<.001f)return;
            actionSkillPower=EffectiveDamage*(.5f+ASPath(ActionSkillKind.Mechanic,0)*.07f+ASRank(ActionSkillKind.Mechanic,0,4)*.12f)*(1+ActionSkillExtraValue("turret_damage"))*(head==extraTurretHead?.7f:1f);
            if(head==actionSkillHead){if(primaryTurretFocus==target)primaryTurretShots=Mathf.Min(5,primaryTurretShots+1);else{primaryTurretFocus=target;primaryTurretShots=0;}actionSkillPower*=1+primaryTurretShots*ActionSkillExtraValue("turret_focus");}
            else {if(secondaryTurretFocus==target)secondaryTurretShots=Mathf.Min(5,secondaryTurretShots+1);else{secondaryTurretFocus=target;secondaryTurretShots=0;}actionSkillPower*=1+secondaryTurretShots*ActionSkillExtraValue("turret_focus");}
            actionSkillPower=Mathf.Min(actionSkillPower,EffectiveDamage*(head==extraTurretHead?.665f:.95f));
            direction.Normalize();head.rotation=Quaternion.LookRotation(direction);
            Vector3 muzzle=head.position+direction*.64f;
            int pellets=gun!=null?Mathf.Max(1,gun.pellets):1;float speed=gun!=null?gun.speed:25;
            for(int i=0;i<pellets;i++){float spread=pellets==1?0:Mathf.Lerp(-8,8,(float)i/(pellets-1));var shot=SpawnBullet(muzzle,Quaternion.Euler(0,spread,0)*direction,true,actionSkillPower,speed,new Color(.9f,.67f,.32f),GunEffect.Normal);shot.skillProjectile=true;ApplyActionElement(shot,turretElement);}
            PlayActionSound(false);
            CombatVisuals.Muzzle(muzzle,direction,new Color(.9f,.67f,.32f),GunEffect.Normal,world);
            if(actionSkillMultitarget)
            {
                Foe second=null;float next=actionSkillRadius*actionSkillRadius;
                foreach(var foe in Enemies)if(foe&&!foe.dead&&foe!=target){Vector3 d=foe.transform.position-head.position;d.y=0;if(d.sqrMagnitude<next&&LineOfSight(head.position,foe.transform.position)){next=d.sqrMagnitude;second=foe;}}
                if(second){Vector3 d=second.transform.position-muzzle;d.y=0;if(d.sqrMagnitude>.001f){var shot=SpawnBullet(muzzle,d.normalized,true,actionSkillPower*.8f,speed,new Color(.9f,.67f,.32f),GunEffect.Normal);shot.skillProjectile=true;ApplyActionElement(shot,turretElement);}}
            }
            fireTimer=Mathf.Max(.04f,(gun!=null?1/Mathf.Max(.01f,EffectiveFireRate):.65f)/Mathf.Min(1.35f,1+ASPath(ActionSkillKind.Mechanic,2)*.06f+ActionSkillExtraValue("turret_rate")));
        }

        void ExplodeActionGrenade(ActionGrenade grenade)
        {
            PlayActionSound(true);
            Vector3 center=grenade.end;CombatVisuals.Burst(center+Vector3.up*.25f,new Color(1,.47f,.13f),20,world);
            CombatVisuals.Impact(center+Vector3.up*.25f,Vector3.up,new Color(1,.55f,.16f),GunEffect.Scatter,world);
            // Copy targets because damage can synchronously run kill/reward logic.
            var targets=new List<Foe>(Enemies);
            Foe elementTarget=null;float elementDistance=float.MaxValue,elementFalloff=0;int hitCount=0;
            foreach(var foe in targets)if(foe&&!foe.dead)
            {
                Vector3 d=foe.transform.position-center;d.y=0;
                if(d.sqrMagnitude>grenade.radius*grenade.radius||!LineOfSight(center,foe.transform.position))continue;
                float falloff=Mathf.Lerp(1,.55f,Mathf.Clamp01(d.magnitude/grenade.radius));
                actionGrenadeHits.TryGetValue(foe,out int previousHits);actionGrenadeHits[foe]=previousHits+1;
                // Repeated blasts in a single salvo retain crowd coverage but
                // yield diminishing damage against the same enemy.
                falloff/=1+.4f*previousHits;
                hitCount++;float delay=Mathf.Max(ActionSkillExtraValue("grenade_suppress"),ActionSkillExtraValue("grenade_slow"));if(delay>0)foe.DelayNextAttack(delay);
                if(foe.Hit(grenade.damage*falloff,foe.transform.position+Vector3.up*.8f,new Color(1,.55f,.16f))&&d.sqrMagnitude<elementDistance){elementTarget=foe;elementDistance=d.sqrMagnitude;elementFalloff=falloff;}
                if(grenade.cluster&&!foe.dead)foe.Hit(grenade.damage*falloff*.25f,foe.transform.position+Vector3.up*.8f,new Color(1,.55f,.16f));
            }
            ApplyGrenadeElement(grenade,elementTarget,elementFalloff);
            float heal=ActionSkillExtraValue("grenade_heal")+ActionSkillExtraValue("grenade_hit_heal");if(heal>0&&hitCount>0)HealPlayer(maxHp*heal*Mathf.Min(hitCount,5));
            if(hitCount>0&&!grenadeChargeAwarded&&ActionSkillExtraValue("grenade_hit_charge")>0){grenadeChargeAwarded=true;ActionSkillOnBulletHit(ActionSkillChargeMax*ActionSkillExtraValue("grenade_hit_charge"));}
        }

        void ClearActionDevice()
        {
            if(actionSkillObject)Destroy(actionSkillObject.gameObject);
            if(extraTurretObject)Destroy(extraTurretObject.gameObject);
            extraTurretObject=null;extraTurretHead=null;
            if(actionSkillRingMaterial)Destroy(actionSkillRingMaterial);
            actionSkillObject=null;actionSkillHead=null;actionSkillRingMaterial=null;actionSkillDuration=0;
        }
        public void ClearActiveActionSkill()
        {
            ClearActionDevice();
            foreach(var grenade in actionGrenades)if(grenade.visual)Destroy(grenade.visual.gameObject);
            actionGrenades.Clear();actionGrenadeHits.Clear();
        }
    }
}
