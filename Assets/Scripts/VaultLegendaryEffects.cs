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
            var parts=key.Split('-');return parts.Length==3&&(parts[0]=="gun"||parts[0]=="armor")&&int.TryParse(parts[1],out maker)&&int.TryParse(parts[2],out variant)&&maker>=0&&maker<6&&variant>=0&&variant<(parts[0]=="gun"?15:5);
        }
        public static string LegendaryRuntimeDescription(string key,bool japanese)
        {
            if(!DecodeLegendaryKey(key,out int maker,out int variant))return "";
            if(key.StartsWith("armor-"))return ArmorLegendaryDescription(maker,variant,japanese);
            return DistinctLegendaryGunDescription(maker,variant,japanese);
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
        void ScheduleLegendaryPulse(Vector3 position,float damage,float radius,int ticks,int maker,float initialDelay=.35f)
        {
            if(legendaryAftershocks.Count>=16)legendaryAftershocks.RemoveAt(0);
            Burst(position,LegendaryEffectColor(maker),4);
            legendaryAftershocks.Add(new LegendaryAftershock{position=position,damage=damage,radius=radius,ticks=ticks,maker=maker,next=legendaryClock+initialDelay});
        }
        void LegendaryTrigger(string trigger,Vector3 origin,Foe struck=null)
        {
            if(legendaryResolving||!Active)return;
            if(trigger=="fire")legendaryShots++;
            if(gun!=null)TryLegendaryEffect(gun.legendaryKey,false,trigger,origin,struck);
            foreach(var armor in EquippedArmor)if(Armory.IsActiveArmor(armor))TryLegendaryEffect(armor.legendaryKey,true,trigger,origin,struck);
        }
        void TryLegendaryEffect(string key,bool armor,string trigger,Vector3 origin,Foe struck)
        {
            if(!DecodeLegendaryKey(key,out int maker,out int variant)||armor!=key.StartsWith("armor-"))return;
            string expected=armor?new[]{"hurt","kill","reload","dash","pulse"}[variant]:new[]{"fire","hit","kill","reload","dash"}[variant%5];
            if(trigger!=expected||(!armor&&variant%5==0&&legendaryShots%3!=0)||(armor&&variant==4&&legendaryClock<5))return;
            // One timer per catalog effect prevents stacked armor duplicates and recursive procs.
            if(legendaryReady.TryGetValue(key,out float ready)&&legendaryClock<ready)return;
            float cooldown=armor?(variant==4?5:4):variant%5==0?1:variant%5<3?2:3;
            legendaryReady[key]=legendaryClock+cooldown;legendaryResolving=true;
            try{ResolveLegendaryEffect(maker,variant,armor,origin,struck);}finally{legendaryResolving=false;}
        }
        void ResolveLegendaryEffect(int maker,int variant,bool armor,Vector3 origin,Foe struck)
        {
            if(armor){ResolveArmorLegendary(maker,variant,origin);return;}
            ResolveDistinctLegendaryGun(maker,variant,origin,struck);
        }
        // The retired head slot (variant 0) is deliberately inert.
        static string ArmorLegendaryDescription(int maker,int variant,bool ja)
        {
            if(variant==0)return ja?"頭装備は一時停止中":"Head equipment is retired";
            string[] triggerEn={"","On kill (4s cooldown): ","On reload (4s cooldown): ","On dash (4s cooldown): ","Every 5s in combat: "};
            string[] triggerJa={"","撃破時（待機4秒）：","装填時（待機4秒）：","ダッシュ時（待機4秒）：","戦闘中5秒ごと："};
            string[,] en={
                {"","tag the nearest survivor with a precision strike; accurate shots for 2s","intercept close hostile rounds and steady shots for 2s","fire two short cover shots at the nearest foe","delay the farthest visible foe's next attack"},
                {"","gain barrier and shove foes away from the fallen enemy","reload shockwave pushes nearby foes away","dash-start impact pushes nearby foes away","stagger a nearby foe and gain barrier"},
                {"","arc to two survivors and delay their attacks","intercept nearby hostile projectiles","dash shocks and interrupts two nearby foes","intercept the nearest hostile projectile"},
                {"","heal 3 HP and plant a damaging spore at the kill site","below half HP, heal and stagger the nearest foe","heal when surrounded and push nearby foes away","convert barrier into health; otherwise grow a small barrier"},
                {"","plant a delayed mine at the kill site","blast foes in a forward cone","leave two delayed fire pockets behind the dash","detonate near the closest visible foe"},
                {"","gain barrier and counterstrike nearby foes","intercept hostile rounds and reinforce barrier","briefly phase through damage and push nearby foes","spend barrier to release a defensive counterburst"}};
            string[,] jp={
                {"","最寄の生存敵に精密追撃し、2秒間射撃を安定化","近くの敵弾を迎撃し、2秒間射撃を安定化","最寄の敵へ短命の援護弾を2発発射","視界内で最も遠い敵の次の攻撃を遅らせる"},
                {"","障壁を得て、撃破地点の周囲の敵を押し出す","装填時の衝撃波で近くの敵を押す","ダッシュ開始地点の敵を押し出す","近くの敵をひるませ、障壁を得る"},
                {"","生存する2体へ電撃を連鎖させ攻撃を遅らせる","周囲の敵弾を迎撃する","ダッシュで近くの2体を感電・妨害する","最寄の敵弾を迎撃する"},
                {"","HPを3回復し、撃破地点に攻撃胞子を設置","HP半分以下なら回復し、最寄の敵をひるませる","敵に囲まれた時に回復し、周囲を押し返す","障壁をHPに変換。障壁がなければ少量生成"},
                {"","撃破地点に遅延地雷を設置","前方の扇形範囲を爆破","ダッシュ後方に遅延炎域を2つ残す","最寄の敵の周囲を爆破"},
                {"","障壁を獲得し近くの敵へ反撃","敵弾を迎撃し障壁を補強","短時間ダメージをかわし近くの敵を押す","障壁を消費して防衛反撃を放つ"}};
            return (ja?triggerJa:triggerEn)[variant]+(ja?jp:en)[maker,variant];
        }
        void ResolveArmorLegendary(int maker,int variant,Vector3 origin)
        {
            if(variant==0)return;
            int id=maker*5+variant;
            var color=LegendaryEffectColor(maker);
            var nearby=LegendaryGunTargets(PlayerPosition,8);
            Foe closest=nearby.Count>0?nearby[0]:null;
            float power=10+room*2;
            switch(id)
            {
                case 1: LegendaryStrike(closest,power*.7f,color);legendaryPrecisionUntil=legendaryClock+2;break;
                case 2: ArmorInterceptShots(PlayerPosition,2.4f,8,color);legendaryPrecisionUntil=legendaryClock+2;break;
                case 3: if(closest!=null&&gun!=null){Vector3 dir=closest.transform.position-PlayerPosition;for(int i=-1;i<=1;i+=2)LegendaryGunShot(PlayerPosition+Vector3.up*.7f,Quaternion.Euler(0,i*10,0)*dir,power*.35f,color);}break;
                case 4: if(nearby.Count>0)ArmorStagger(nearby[nearby.Count-1],1.1f,color);break;

                case 6: ArmorBarrier(5);ArmorPush(origin,2.5f,1.1f,color);break;
                case 7: ArmorPush(PlayerPosition,2.4f,1.5f,color);break;
                case 8: ArmorPush(PlayerPosition,2.1f,2,color);break;
                case 9: ArmorStagger(closest,1.3f,color);ArmorBarrier(3);break;

                case 11: for(int i=0;i<Mathf.Min(2,nearby.Count);i++){LegendaryStrike(nearby[i],power*.5f,color);ArmorStagger(nearby[i],.7f,color);}break;
                case 12: ArmorInterceptShots(PlayerPosition,3,12,color);break;
                case 13: for(int i=0;i<Mathf.Min(2,nearby.Count);i++){LegendaryStrike(nearby[i],power*.4f,color);ArmorStagger(nearby[i],1,color);}break;
                case 14: ArmorInterceptShots(PlayerPosition,5,1,color);break;

                case 16: ScheduleLegendaryPulse(origin,power*.3f,1.4f,2,maker);hp=Mathf.Min(maxHp,hp+3);break;
                case 17: if(hp<maxHp*.5f){hp=Mathf.Min(maxHp,hp+8);ArmorStagger(closest,.8f,color);}break;
                case 18: if(nearby.Count>=2){hp=Mathf.Min(maxHp,hp+5);ArmorPush(PlayerPosition,1.8f,.8f,color);}break;
                case 19: if(ArtifactShield>=3){ArtifactShield-=3;hp=Mathf.Min(maxHp,hp+5);}else ArmorBarrier(3);UpdateBarrierVisual();break;

                case 21: ScheduleLegendaryPulse(origin,power*.8f,1.8f,1,maker);break;
                case 22: foreach(var foe in nearby)if(LegendaryGunInCone(PlayerPosition,aim,foe.transform.position,5,.65f))LegendaryStrike(foe,power*.65f,color);break;
                case 23: for(int i=0;i<2;i++)ScheduleLegendaryPulse(PlayerPosition-dodgeDirection*(i+.5f),power*.38f,1.1f,2,maker);break;
                case 24: if(closest!=null)ScheduleLegendaryPulse(closest.transform.position,power*.75f,1.6f,1,maker);break;

                case 26: ArmorBarrier(5);foreach(var foe in nearby)if((foe.transform.position-PlayerPosition).sqrMagnitude<4)LegendaryStrike(foe,power*.35f,color);break;
                case 27: ArmorInterceptShots(PlayerPosition,2.8f,10,color);ArmorBarrier(3);break;
                case 28: invincible=Mathf.Max(invincible,.22f);ArmorPush(PlayerPosition,1.9f,1.1f,color);break;
                case 29: if(ArtifactShield>=5){ArtifactShield-=5;UpdateBarrierVisual();foreach(var foe in nearby)if((foe.transform.position-PlayerPosition).sqrMagnitude<6.25f)LegendaryStrike(foe,power*.8f,color);}break;
            }
            Burst(PlayerPosition,color,4);
        }
        void ArmorBarrier(float amount){ArtifactShield=Mathf.Min(60,ArtifactShield+amount);UpdateBarrierVisual();}
        void ArmorStagger(Foe foe,float seconds,Color color)
        {if(foe==null||foe.dead)return;foe.DelayNextAttack(seconds);CombatVisuals.Impact(foe.transform.position+Vector3.up*.8f,Vector3.up,color,GunEffect.Normal,world);}
        void ArmorPush(Vector3 center,float radius,float distance,Color color)
        {
            foreach(var foe in LegendaryGunTargets(center,radius))
            {var direction=foe.transform.position-center;direction.y=0;if(direction.sqrMagnitude<.01f)direction=aim;
                foe.transform.position=MoveWithinRoom(foe.transform.position,direction.normalized*distance,.6f);
                ArmorStagger(foe,.45f,color);}
        }
        void ArmorInterceptShots(Vector3 center,float radius,int limit,Color color)
        {
            float limitSq=radius*radius;int intercepted=0;
            foreach(var shot in FindObjectsByType<Shot>(FindObjectsSortMode.None))
            {
                if(shot==null||shot.friendly||(shot.transform.position-center).sqrMagnitude>limitSq)continue;
                CombatVisuals.Impact(shot.transform.position,Vector3.up,color,GunEffect.Normal,world);
                Destroy(shot.gameObject);if(++intercepted>=limit)break;
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
