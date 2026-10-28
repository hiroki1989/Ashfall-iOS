using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // The selected skill and ranks are owned by the progression system. These
        // transient objects belong to the current world and never enter a save file.
        Transform actionSkillObject, actionSkillHead;
        Material actionSkillRingMaterial;
        float actionSkillDuration, actionSkillFireTimer, actionSkillPulseTimer;
        float actionSkillPower, actionSkillRadius;
        bool actionSkillFollows, actionSkillMultitarget;
        readonly List<ActionGrenade> actionGrenades=new List<ActionGrenade>();

        sealed class ActionGrenade
        {
            public Transform visual;
            public Vector3 start, end;
            public float age, travel, damage, radius;
            public bool cluster;
        }

        int ASRank(ActionSkillKind kind,int branch,int tier)=>Mathf.Clamp(ActionSkillRank(kind,branch,tier),0,1);
        float ASPath(ActionSkillKind kind,int branch)
        {float n=0;for(int tier=0;tier<4;tier++)n+=Mathf.Clamp(ActionSkillRank(kind,branch,tier),0,6)/6f;return n;}

        public void ActionSkillOnKill(Foe foe)
        {
            if(foe==null)return;
            ActionSkillPoints=Mathf.Min(ActionSkillChargeMax,ActionSkillPoints+(foe.kind==3?4:foe.elite?2:1));
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
            return true;
        }

        void DeployActionTurret()
        {
            var k=ActionSkillKind.Mechanic;
            float fire=ASPath(k,0),movement=ASPath(k,1),area=ASPath(k,2);
            actionSkillDuration=12+movement*1.5f+ASRank(k,1,4)*5;
            actionSkillPower=Mathf.Max(12,EffectiveDamage)*(.65f+fire*.13f+ASRank(k,0,4)*.55f);
            actionSkillRadius=14+area*1.5f;
            actionSkillFollows=ASRank(k,1,4)>0;
            actionSkillMultitarget=ASRank(k,2,4)>0;
            actionSkillFireTimer=.2f;
            actionSkillObject=new GameObject("Mechanic / field sentry").transform;
            actionSkillObject.SetParent(world,false);
            Vector3 origin=MoveWithinRoom(PlayerPosition,aim.normalized*1.5f,.6f);origin.y=.08f;actionSkillObject.position=origin;
            var dark=new Color(.19f,.21f,.19f);var steel=new Color(.39f,.37f,.30f);var brass=new Color(.67f,.39f,.14f);
            art.Part("Sentry mounting foot",PrimitiveType.Cylinder,new Vector3(0,.10f,0),new Vector3(.55f,.11f,.55f),dark,actionSkillObject);
            for(int i=0;i<3;i++)
            {float a=i*Mathf.PI*2/3;var support=art.Part("Sentry support leg",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.27f,.10f,Mathf.Cos(a)*.27f),new Vector3(.13f,.12f,.54f),steel,actionSkillObject);support.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);}
            art.Part("Sentry rotating post",PrimitiveType.Cylinder,new Vector3(0,.36f,0),new Vector3(.24f,.26f,.24f),steel,actionSkillObject);
            actionSkillHead=new GameObject("Sentry aiming head").transform;actionSkillHead.SetParent(actionSkillObject,false);actionSkillHead.localPosition=new Vector3(0,.66f,0);
            art.Part("Receiver casing",PrimitiveType.Cube,new Vector3(0,0,0),new Vector3(.47f,.30f,.48f),dark,actionSkillHead);
            art.Part("Feed box",PrimitiveType.Cube,new Vector3(-.31f,-.06f,-.02f),new Vector3(.17f,.25f,.30f),steel,actionSkillHead);
            art.Part("Barrel jacket",PrimitiveType.Cylinder,new Vector3(0,0,.36f),new Vector3(.12f,.28f,.12f),steel,actionSkillHead).localRotation=Quaternion.Euler(90,0,0);
            art.Part("Sentry sight",PrimitiveType.Cube,new Vector3(0,.19f,.02f),new Vector3(.14f,.08f,.18f),brass,actionSkillHead);
            CombatVisuals.Burst(origin+Vector3.up*.3f,new Color(.8f,.66f,.35f),9,world);
        }

        void ThrowActionGrenades()
        {
            var k=ActionSkillKind.Bomber;
            int count=1+Mathf.FloorToInt(ASPath(k,0))+3*ASRank(k,0,4);
            float radius=2.7f+ASPath(k,1)*.35f+ASRank(k,1,4)*1.25f;
            float damage=Mathf.Max(18,EffectiveDamage)*(1.8f+ASPath(k,2)*.3f+ASRank(k,2,4)*.8f);
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
                actionGrenades.Add(new ActionGrenade{visual=root,start=start,end=end,travel=.6f+i*.045f,damage=damage,radius=radius,cluster=ASRank(k,2,4)>0});
            }
        }

        void DeployActionHealingField()
        {
            var k=ActionSkillKind.Medic;
            float recovery=ASPath(k,0),radius=ASPath(k,1),duration=ASPath(k,2);
            actionSkillPower=.028f+recovery*.006f+ASRank(k,0,4)*.025f;
            actionSkillRadius=3.0f+radius*.45f+ASRank(k,1,4)*1.6f;
            actionSkillDuration=12+duration*1.5f+ASRank(k,2,4)*5;
            actionSkillFollows=ASRank(k,2,4)>0;
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
            if(actionSkillHead)TickActionTurret(delta);
            else
            {
                Vector3 separation=PlayerPosition-actionSkillObject.position;separation.y=0;
                if(separation.sqrMagnitude<=actionSkillRadius*actionSkillRadius&&LineOfSight(actionSkillObject.position,PlayerPosition))hp=Mathf.Min(maxHp,hp+maxHp*actionSkillPower*delta);
                actionSkillPulseTimer-=delta;if(actionSkillPulseTimer<=0){actionSkillPulseTimer=1.5f;CombatVisuals.Burst(actionSkillObject.position+Vector3.up*.1f,new Color(.28f,.72f,.44f),3,world);}
            }
        }

        void TickActionTurret(float delta)
        {
            actionSkillFireTimer-=delta;if(actionSkillFireTimer>0)return;
            Foe target=null;float best=actionSkillRadius*actionSkillRadius;
            foreach(var foe in Enemies)if(foe&&!foe.dead)
            {Vector3 d=foe.transform.position-actionSkillHead.position;d.y=0;float distance=d.sqrMagnitude;if(distance<best&&LineOfSight(actionSkillHead.position,foe.transform.position)){best=distance;target=foe;}}
            if(!target){actionSkillFireTimer=.25f;return;}
            Vector3 direction=target.transform.position-actionSkillHead.position;direction.y=0;if(direction.sqrMagnitude<.001f)return;
            direction.Normalize();actionSkillHead.rotation=Quaternion.LookRotation(direction);
            Vector3 muzzle=actionSkillHead.position+direction*.64f;
            SpawnBullet(muzzle,direction,true,actionSkillPower,25,new Color(.9f,.67f,.32f),GunEffect.Normal);
            CombatVisuals.Muzzle(muzzle,direction,new Color(.9f,.67f,.32f),GunEffect.Normal,world);
            if(actionSkillMultitarget)
            {
                Foe second=null;float next=actionSkillRadius*actionSkillRadius;
                foreach(var foe in Enemies)if(foe&&!foe.dead&&foe!=target){Vector3 d=foe.transform.position-actionSkillHead.position;d.y=0;if(d.sqrMagnitude<next&&LineOfSight(actionSkillHead.position,foe.transform.position)){next=d.sqrMagnitude;second=foe;}}
                if(second){Vector3 d=second.transform.position-muzzle;d.y=0;if(d.sqrMagnitude>.001f)SpawnBullet(muzzle,d.normalized,true,actionSkillPower*.8f,25,new Color(.9f,.67f,.32f),GunEffect.Normal);}
            }
            actionSkillFireTimer=Mathf.Max(.2f,.65f-ASPath(ActionSkillKind.Mechanic,2)*.06f);
        }

        void ExplodeActionGrenade(ActionGrenade grenade)
        {
            Vector3 center=grenade.end;CombatVisuals.Burst(center+Vector3.up*.25f,new Color(1,.47f,.13f),20,world);
            CombatVisuals.Impact(center+Vector3.up*.25f,Vector3.up,new Color(1,.55f,.16f),GunEffect.Scatter,world);
            // Copy targets because damage can synchronously run kill/reward logic.
            var targets=new List<Foe>(Enemies);
            foreach(var foe in targets)if(foe&&!foe.dead)
            {
                Vector3 d=foe.transform.position-center;d.y=0;
                if(d.sqrMagnitude>grenade.radius*grenade.radius||!LineOfSight(center,foe.transform.position))continue;
                float falloff=Mathf.Lerp(1,.55f,Mathf.Clamp01(d.magnitude/grenade.radius));
                foe.Hit(grenade.damage*falloff,foe.transform.position+Vector3.up*.8f,new Color(1,.55f,.16f));
                if(grenade.cluster&&!foe.dead)foe.Hit(grenade.damage*.25f,foe.transform.position+Vector3.up*.8f,new Color(1,.55f,.16f));
            }
        }

        void ClearActionDevice()
        {
            if(actionSkillObject)Destroy(actionSkillObject.gameObject);
            if(actionSkillRingMaterial)Destroy(actionSkillRingMaterial);
            actionSkillObject=null;actionSkillHead=null;actionSkillRingMaterial=null;actionSkillDuration=0;
        }
        public void ClearActiveActionSkill()
        {
            ClearActionDevice();
            foreach(var grenade in actionGrenades)if(grenade.visual)Destroy(grenade.visual.gameObject);
            actionGrenades.Clear();
        }
    }
}
