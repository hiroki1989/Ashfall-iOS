using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        readonly List<LegendaryAftershock> legendaryAftershocks=new List<LegendaryAftershock>();
        sealed class LegendaryAftershock { public Vector3 position;public float next,damage,radius;public int ticks,maker; }
        readonly Dictionary<string,float> legendaryReady=new Dictionary<string,float>();
        float legendaryClock,legendaryPrecisionUntil;int legendaryShots;bool legendaryResolving;
        public static bool DecodeLegendaryKey(string key,out int maker,out int variant)
        {
            maker=variant=-1;if(string.IsNullOrEmpty(key))return false;
            var parts=key.Split('-');return parts.Length==3&&(parts[0]=="gun"||parts[0]=="armor")&&int.TryParse(parts[1],out maker)&&int.TryParse(parts[2],out variant)&&maker>=0&&maker<6&&variant>=0&&variant<5;
        }
        public static string LegendaryRuntimeDescription(string key,bool japanese)
        {
            if(!DecodeLegendaryKey(key,out int maker,out int variant))return "";
            bool armor=key.StartsWith("armor-");
            string[] triggerEn=armor?new[]{"On damage taken (4s cooldown)","On kill (4s cooldown)","On reload (4s cooldown)","On dash (4s cooldown)","Every 5s in combat"}:new[]{"Every third shot (1s cooldown)","On bullet hit (2s cooldown)","On kill (2s cooldown)","On reload (3s cooldown)","On dash (3s cooldown)"};
            string[] triggerJa=armor?new[]{"被弾時（待機4秒）","撃破時（待機4秒）","装填時（待機4秒）","ダッシュ時（待機4秒）","戦闘中5秒ごと"}:new[]{"3射ごと（待機1秒）","銃弾命中時（待機2秒）","撃破時（待機2秒）","装填時（待機3秒）","ダッシュ時（待機3秒）"};
            string[] coreEn={"nearest-target precision strike + 3s of +25% shot damage and perfect spread score","2.8m heavy shockwave + barrier","up to 3 chain-lightning targets + first-target echo","single-target siphon + healing","4m blast + reserve ammunition","barrier charge + 2.5m retaliation"};
            string[] coreJa={"最寄への精密追撃＋3秒間の銃弾威力25%増幅・精度100","半径2.8m重衝撃＋障壁","最大3体への連鎖雷撃＋先頭への再撃","単体吸命＋HP回復","半径4m爆発＋予備弾還元","障壁充填＋半径2.5m反撃"};
            string[] variantEn={"extra focused strike","3m radial strike","three lingering 2.2m pulses","delayed strikes at up to three targets","three forward trail zones pulsing twice"};
            string[] variantJa={"単体集中追撃","半径3m全周追撃","半径2.2mの残留波3回","最大3体の位置へ遅延追撃","前方3地点に各2回の軌跡波"};
            return (japanese?triggerJa:triggerEn)[variant]+": "+(japanese?coreJa:coreEn)[maker]+" / "+(japanese?variantJa:variantEn)[variant]+(maker==1&&variant==1?(japanese?"＋ノックバック":" + knockback"):"");
        }
        public static Color LegendaryEffectColor(int maker)=>new[]{new Color(.65f,.9f,1),new Color(1,.55f,.18f),new Color(.5f,.4f,1),new Color(.3f,1,.4f),new Color(1,.25f,.08f),new Color(.1f,.95f,1)}[Mathf.Clamp(maker,0,5)];
        void ResetLegendaryEffects(){legendaryReady.Clear();legendaryAftershocks.Clear();legendaryClock=0;legendaryShots=0;legendaryPrecisionUntil=0;legendaryResolving=false;}
        void TickLegendaryEffects(float dt)
        {
            legendaryClock+=Mathf.Max(0,dt);if(!Active)return;
            for(int i=legendaryAftershocks.Count-1;i>=0;i--)
            {
                var pulse=legendaryAftershocks[i];if(legendaryClock<pulse.next)continue;
                pulse.next=legendaryClock+.5f;pulse.ticks--;legendaryResolving=true;
                try{Burst(pulse.position,LegendaryEffectColor(pulse.maker),6);foreach(var foe in Enemies.ToArray())if(foe!=null&&!foe.dead&&(foe.transform.position-pulse.position).sqrMagnitude<=pulse.radius*pulse.radius&&LineOfSight(pulse.position,foe.transform.position))LegendaryStrike(foe,pulse.damage,LegendaryEffectColor(pulse.maker));}finally{legendaryResolving=false;}
                if(pulse.ticks<=0)legendaryAftershocks.RemoveAt(i);
            }
            LegendaryTrigger("pulse",PlayerPosition);
        }
        void ScheduleLegendaryPulse(Vector3 position,float damage,float radius,int ticks,int maker)
        {
            if(legendaryAftershocks.Count>=16)legendaryAftershocks.RemoveAt(0);
            Burst(position,LegendaryEffectColor(maker),4);
            legendaryAftershocks.Add(new LegendaryAftershock{position=position,damage=damage,radius=radius,ticks=ticks,maker=maker,next=legendaryClock+.35f});
        }
        void LegendaryTrigger(string trigger,Vector3 origin,Foe struck=null)
        {
            if(legendaryResolving||!Active)return;
            if(trigger=="fire")legendaryShots++;
            if(gun!=null)TryLegendaryEffect(gun.legendaryKey,false,trigger,origin,struck);
            foreach(var armor in EquippedArmor)if(armor!=null)TryLegendaryEffect(armor.legendaryKey,true,trigger,origin,struck);
        }
        void TryLegendaryEffect(string key,bool armor,string trigger,Vector3 origin,Foe struck)
        {
            if(!DecodeLegendaryKey(key,out int maker,out int variant)||armor!=key.StartsWith("armor-"))return;
            string expected=armor?new[]{"hurt","kill","reload","dash","pulse"}[variant]:new[]{"fire","hit","kill","reload","dash"}[variant];
            if(trigger!=expected||(!armor&&variant==0&&legendaryShots%3!=0)||(armor&&variant==4&&legendaryClock<5))return;
            // One timer per catalog effect prevents stacked armor duplicates and recursive procs.
            if(legendaryReady.TryGetValue(key,out float ready)&&legendaryClock<ready)return;
            float cooldown=armor?(variant==4?5:4):variant==0?1:variant<3?2:3;
            legendaryReady[key]=legendaryClock+cooldown;legendaryResolving=true;
            try{ResolveLegendaryEffect(maker,variant,armor,origin,struck);}finally{legendaryResolving=false;}
        }
        void ResolveLegendaryEffect(int maker,int variant,bool armor,Vector3 origin,Foe struck)
        {
            float scale=1+variant*.25f;
            float damage=(armor?12:Mathf.Max(8,gun==null?8:gun.damage*.65f))*scale;
            var color=LegendaryEffectColor(maker);Burst(origin,color,10);
            var candidates=new List<Foe>();foreach(var foe in Enemies)if(foe!=null&&!foe.dead&&(foe.transform.position-origin).sqrMagnitude<=100&&LineOfSight(origin+Vector3.up*.5f,foe.transform.position+Vector3.up*.5f))candidates.Add(foe);
            candidates.Sort((a,b)=>(a.transform.position-origin).sqrMagnitude.CompareTo((b.transform.position-origin).sqrMagnitude));
            LegendarySignature(origin,maker,color,candidates);
            // Each variant changes targeting and timing in addition to its trigger.
            if(variant==0&&candidates.Count>0)LegendaryStrike(candidates[0],damage*.5f,color);
            if(variant==1)foreach(var foe in candidates)if((foe.transform.position-origin).sqrMagnitude<9){LegendaryStrike(foe,damage*.35f,color);if(maker==1){var push=foe.transform.position-origin;push.y=0;foe.transform.position=MoveWithinRoom(foe.transform.position,push.normalized*1.4f,.6f);}}
            if(variant==2)ScheduleLegendaryPulse(origin,damage*.35f,2.2f,3,maker);
            if(variant==3)for(int i=0;i<Mathf.Min(3,candidates.Count);i++)ScheduleLegendaryPulse(candidates[i].transform.position,damage*.4f,1.1f,1,maker);
            if(variant==4)for(int i=0;i<3;i++)ScheduleLegendaryPulse(PlayerPosition+aim*(i*1.5f),damage*.35f,1.25f,2,maker);
            if(maker==0)
            {
                legendaryPrecisionUntil=legendaryClock+3;
                if(candidates.Count>0)LegendaryStrike(candidates[0],damage*1.5f,color);
            }
            else if(maker==1)
            {
                foreach(var foe in candidates)if((foe.transform.position-origin).sqrMagnitude<=2.8f*2.8f)LegendaryStrike(foe,damage*1.4f,color);
                ArtifactShield=Mathf.Min(60,ArtifactShield+3*scale);
            }
            else if(maker==2)
            {
                for(int i=0;i<Mathf.Min(3,candidates.Count);i++)LegendaryStrike(candidates[i],damage*(1-i*.18f),color);
                if(candidates.Count>0)LegendaryStrike(candidates[0],damage*.25f,color);
            }
            else if(maker==3)
            {
                if(candidates.Count>0)LegendaryStrike(candidates[0],damage*.8f,color);
                hp=Mathf.Min(maxHp,hp+4*scale);
            }
            else if(maker==4)
            {
                foreach(var foe in candidates)if((foe.transform.position-origin).sqrMagnitude<=16)LegendaryStrike(foe,damage,color);
                if(gun!=null){var type=AmmoForGun(gun);ammoReserves[(int)type]=Mathf.Min(AmmoCapacity(type),ammoReserves[(int)type]+Mathf.CeilToInt(scale));}
            }
            else
            {
                ArtifactShield=Mathf.Min(60,ArtifactShield+8*scale);
                foreach(var foe in candidates)if((foe.transform.position-origin).sqrMagnitude<=6.25f)LegendaryStrike(foe,damage*.75f,color);
                UpdateBarrierVisual();
            }
        }
        void LegendarySignature(Vector3 origin,int maker,Color color,List<Foe> targets)
        {
            var obj=new GameObject("Legendary signature / "+maker);obj.transform.SetParent(world,false);
            var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;line.startWidth=.065f;line.endWidth=.025f;
            line.material=new Material(Resources.Load<Shader>("VaultTrail"));line.material.color=color;line.startColor=line.endColor=color;
            if(maker==0||maker==2||maker==3)
            {
                int links=maker==2?Mathf.Min(3,targets.Count):Mathf.Min(1,targets.Count);line.positionCount=1+links*8;
                Vector3 from=origin+Vector3.up*.65f;line.SetPosition(0,from);
                for(int n=0;n<links;n++){Vector3 to=targets[n].transform.position+Vector3.up*.7f;for(int k=1;k<=8;k++){float t=k/8f;Vector3 at=Vector3.Lerp(from,to,t);if(maker==2&&k<8)at+=Vector3.up*Mathf.Sin(k*2.3f)*.18f;line.SetPosition(n*8+k,at);}from=to;}
            }
            else
            {
                line.loop=true;line.positionCount=48;float radius=maker==4?4:maker==1?2.8f:2.5f;
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,origin+new Vector3(Mathf.Cos(a)*radius,.15f,Mathf.Sin(a)*radius));}
            }
            obj.AddComponent<LegendarySignatureLifetime>();Destroy(obj,.24f);
        }
        void LegendaryStrike(Foe foe,float damage,Color color)
        {
            if(foe==null||foe.dead)return;var point=foe.transform.position+Vector3.up*.75f;
            CombatVisuals.Impact(point,Vector3.up,color,GunEffect.Normal,world);
            foe.Hit(damage,point,color,100);
        }
        public void LegendaryBulletHit(Foe foe,Vector3 point)=>LegendaryTrigger("hit",point,foe);
        void LegendaryModifyShot(Shot shot)
        {
            if(shot==null||!shot.friendly)return;
            if(legendaryClock<legendaryPrecisionUntil){shot.damage*=1.25f;shot.hitAccuracy=100;shot.moduleAmplified=true;}
        }
    }
    public sealed class LegendarySignatureLifetime:MonoBehaviour
    {void OnDestroy(){var line=GetComponent<LineRenderer>();if(line!=null&&line.sharedMaterial!=null)Destroy(line.sharedMaterial);}}
}
