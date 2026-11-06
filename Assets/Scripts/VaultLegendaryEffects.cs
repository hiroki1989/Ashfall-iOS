using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        readonly List<LegendaryAftershock> legendaryAftershocks=new List<LegendaryAftershock>();
        sealed class LegendaryAftershock { public Vector3 position;public float next,damage,radius;public int ticks,maker;public GameObject device; }
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
        void ResetLegendaryEffects(){legendaryReady.Clear();foreach(var pulse in legendaryAftershocks)if(pulse.device)Destroy(pulse.device);legendaryAftershocks.Clear();legendaryClock=0;legendaryShots=0;legendaryPrecisionUntil=0;legendaryResolving=false;}
        void TickLegendaryEffects(float dt)
        {
            legendaryClock+=Mathf.Max(0,dt);if(!Active)return;
            for(int i=legendaryAftershocks.Count-1;i>=0;i--)
            {
                var pulse=legendaryAftershocks[i];if(legendaryClock<pulse.next)continue;
                pulse.next=legendaryClock+.5f;pulse.ticks--;legendaryResolving=true;
                try{if(world!=null){if(pulse.maker==3)CombatVisuals.ElementImpact(pulse.position,GunElement.Poison,world);else CombatVisuals.ElementBlast(pulse.position,pulse.radius,world);}Burst(pulse.position,LegendaryEffectColor(pulse.maker),12);foreach(var foe in Enemies.ToArray())if(foe!=null&&!foe.dead&&(foe.transform.position-pulse.position).sqrMagnitude<=pulse.radius*pulse.radius&&LineOfSight(pulse.position,foe.transform.position))LegendaryStrike(foe,pulse.damage,LegendaryEffectColor(pulse.maker));}finally{legendaryResolving=false;}
                if(pulse.ticks<=0){if(pulse.device)Destroy(pulse.device);legendaryAftershocks.RemoveAt(i);}
            }
            LegendaryTrigger("pulse",PlayerPosition);
        }
        void ScheduleLegendaryPulse(Vector3 position,float damage,float radius,int ticks,int maker,float initialDelay=.35f)
        {
            if(legendaryAftershocks.Count>=16){if(legendaryAftershocks[0].device)Destroy(legendaryAftershocks[0].device);legendaryAftershocks.RemoveAt(0);}
            Burst(position,LegendaryEffectColor(maker),9);
            legendaryAftershocks.Add(new LegendaryAftershock{position=position,damage=damage,radius=radius,ticks=ticks,maker=maker,next=legendaryClock+initialDelay,device=LegendaryDeviceVisual.Create(world,position,maker)});
        }
        void LegendaryTrigger(string trigger,Vector3 origin,Foe struck=null)
        {
            if(legendaryResolving||!Active)return;
            if(trigger=="fire")legendaryShots++;
            if(gun!=null&&!BossRelicFoundation.Decode(gun.bossRelicId,out _,out _))TryLegendaryEffect(gun.legendaryKey,false,trigger,origin,struck);
            foreach(var armor in EquippedArmor)if(Armory.IsActiveArmor(armor)&&!BossRelicFoundation.Decode(armor.bossRelicId,out _,out _))TryLegendaryEffect(armor.legendaryKey,true,trigger,origin,struck);
            if(trigger=="kill")
            {
                if(gun!=null)TryBossRelicEffectV2(gun.bossRelicId,origin);
                foreach(var armor in EquippedArmor)if(Armory.IsActiveArmor(armor))TryBossRelicEffectV2(armor.bossRelicId,origin);
                if(EquippedArtifacts!=null)foreach(var module in EquippedArtifacts)if(module!=null)TryBossRelicEffectV2(module.bossRelicId,origin);
                if(EquippedMeleeWeapon!=null)TryBossRelicEffectV2(EquippedMeleeWeapon.bossRelicId,origin);
            }
        }
        void TryBossRelicEffect(string id,Vector3 origin)
        {
            if(!BossRelicFoundation.Decode(id,out int boss,out int category))return;
            if(legendaryReady.TryGetValue(id,out float ready)&&legendaryClock<ready)return;
            legendaryReady[id]=legendaryClock+4f;
            legendaryResolving=true;
            try
            {
                Color color=boss==0?new Color(1f,.3f,.1f):boss==1?new Color(.5f,.8f,1f):new Color(1f,.7f,.2f);
                float power=Mathf.Max(12f,gun!=null?gun.damage*1.1f:20f);
                var nearby=LegendaryGunTargets(origin,category==3?10f:category==2?7f:3.5f);
                switch(category)
                {
                    case 0: // Sidearm: one compact ricochet chain.
                        for(int i=0;i<Mathf.Min(2,nearby.Count);i++)LegendaryStrike(nearby[i],power*(boss==2?.9f:.68f),color);
                        break;
                    case 1: // Shotgun: close blast with a readable knockback.
                        foreach(var foe in LegendaryGunTargets(origin,2.6f))LegendaryStrike(foe,power*.62f,color);
                        ArmorPush(origin,2.7f,.7f,color);
                        break;
                    case 2: // Assault rifle: chained, lower-damage hits.
                        for(int i=0;i<Mathf.Min(3,nearby.Count);i++){LegendaryStrike(nearby[i],power*.57f,color);if(boss==1)ArmorStagger(nearby[i],.45f,color);}
                        break;
                    case 3: // Sniper rifle: one long-range execution shot.
                        if(nearby.Count>0)LegendaryStrike(nearby[nearby.Count-1],power*(boss==2?1.9f:1.45f),color);
                        break;
                    case 4: // Heavy machine gun: short suppression burst.
                        for(int i=0;i<Mathf.Min(4,nearby.Count);i++){LegendaryStrike(nearby[i],power*.48f,color);ArmorStagger(nearby[i],boss==1?.9f:.55f,color);}
                        break;
                    case 5: // Chest armor.
                        ArmorBarrier(boss==2?14:boss==1?10:8);ArmorPush(origin,3f,.85f,color);
                        break;
                    case 6: // Arm armor.
                        legendaryPrecisionUntil=legendaryClock+2.5f;
                        if(nearby.Count>0)LegendaryStrike(nearby[0],power*.9f,color);
                        break;
                    case 7: // Leg armor.
                        dodgeCooldown=0;HealPlayer(maxHp*(boss==1?.09f:.045f));
                        if(nearby.Count>0)ArmorStagger(nearby[0],.65f,color);
                        break;
                    case 8: // Boots.
                        invincible=Mathf.Max(invincible,.28f);ArmorBarrier(boss==2?9:5);
                        break;
                    case 9: // Support module.
                        HealPlayer(maxHp*(boss==1?.12f:.07f));
                        if(gun!=null){var ammoType=AmmoForGun(gun);ammoReserves[(int)ammoType]=Mathf.Min(AmmoCapacity(ammoType),ammoReserves[(int)ammoType]+(boss==2?8:4));}
                        break;
                    case 10: // Melee relic.
                        foreach(var foe in LegendaryGunTargets(origin,2.4f)){LegendaryStrike(foe,power*(boss==2?1.0f:.72f),color);ArmorStagger(foe,.8f,color);}
                        ArmorPush(origin,2.6f,1.05f,color);
                        break;
                }
                // The three sources retain a distinct combat signature in addition
                // to the item's slot-specific proc.
                if(boss==1&&category!=7&&category!=9)HealPlayer(maxHp*.025f);
                if(boss==2&&category!=5&&category!=8)ArmorBarrier(4);
                Burst(origin,color,16);
            }
            finally{legendaryResolving=false;}
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
            LegendarySignature(origin,maker,LegendaryEffectColor(maker),LegendaryGunTargets(origin,8));
            if(armor){ResolveArmorLegendary(maker,variant,origin);return;}
            ResolveDistinctLegendaryGun(maker,variant,origin,struck);
        }
        // The retired head slot (variant 0) is deliberately inert.
        static string ArmorLegendaryDescription(int maker,int variant,bool ja)
        {
            if(variant==0)return ja?"頭装備は一時停止中":"Head equipment is retired";
            string[] triggerEn={"","On kill (4s cooldown): ","On reload (4s cooldown): ","On dash (4s cooldown): ","Every 5s in combat: "};
            string[] triggerJa={"","撃破時（クールダウン4秒）：","リロード時（クールダウン4秒）：","ダッシュ時（クールダウン4秒）：","戦闘中5秒ごと："};
            string[,] en={
                {"","fire three guided rounds at the nearest survivor","intercept up to eight nearby hostile rounds","fire two short cover shots at the nearest foe","call three delayed strikes around the farthest visible foe"},
                {"","detonate a lingering mine at the kill site","launch a blast wave that damages nearby foes","leave an explosive charge at the dash origin","launch a defensive blast at the closest foe"},
                {"","arc electricity to three survivors","intercept nearby hostile projectiles","dash discharges arcs at three nearby foes","call an electrical strike on the nearest foe"},
                {"","plant a three-pulse spore field at the kill site","below half HP, restore 12% of maximum health","when surrounded, release a three-pulse spore burst","convert barrier into health; otherwise grow a small barrier"},
                {"","plant a delayed mine at the kill site","blast foes in a forward cone","leave two delayed fire pockets behind the dash","detonate near the closest visible foe"},
                {"","gain 20 shield","intercept up to ten nearby hostile rounds","leave a two-pulse explosive charge behind","spend barrier to release a defensive counterburst"}};
            string[,] jp={
                {"","最寄の生存敵へ誘導弾を3発放つ","近くの敵弾を最大8発迎撃する","最寄の敵へ短命の援護弾を2発発射","視界内で最も遠い敵の周囲に3回の追撃を落とす"},
                {"","撃破地点に連続爆発する地雷を設置","周囲の敵に衝撃波でダメージ","ダッシュ開始地点に爆発する罠を残す","最寄りの敵へ防衛爆撃を放つ"},
                {"","生存する敵3体へ電撃を放つ","周囲の敵弾を迎撃する","ダッシュ時に近くの敵3体へ電撃を放つ","最寄りの敵へ電撃を落とす"},
                {"","撃破地点に3回波動する胞子領域を設置","HP半分以下なら最大HPの12%を回復","敵に囲まれた時に3回の胞子波動を放つ","シールドをHPに変換。シールドがなければ少量生成"},
                {"","撃破地点に遅延地雷を設置","前方の扇形範囲を爆破","ダッシュ後方に遅延炎域を2つ残す","最寄の敵の周囲を爆破"},
                {"","シールドを20得る","近くの敵弾を最大10発迎撃する","後方に2回爆発する罠を残す","シールドを消費して防衛反撃を放つ"}};
            return (ja?triggerJa:triggerEn)[variant]+(ja?jp:en)[maker,variant];
        }
        void ResolveArmorLegendary(int maker,int variant,Vector3 origin)
        {
            if(variant==0)return;
            int id=maker*5+variant;
            var color=LegendaryEffectColor(maker);
            var nearby=LegendaryGunTargets(PlayerPosition,8);
            Foe closest=nearby.Count>0?nearby[0]:null;
            // Keep legendary counterattacks relevant throughout the level curve.
            float power=gun!=null?Mathf.Max(1f,gun.damage):10f*GearBalance.LevelPower(Rarity.Common,DebtLevel);
            switch(id)
            {
                case 1: if(closest!=null)for(int i=-1;i<=1;i++)LegendaryHomingShot(PlayerPosition+Vector3.up*.7f+Vector3.right*i*.55f,closest,power*.65f,color,null);break;
                case 2: ArmorInterceptShots(PlayerPosition,2.4f,8,color);break;
                case 3: if(closest!=null&&gun!=null){Vector3 dir=closest.transform.position-PlayerPosition;for(int i=-1;i<=1;i+=2)LegendaryGunShot(PlayerPosition+Vector3.up*.7f,Quaternion.Euler(0,i*10,0)*dir,power*.35f,color);}break;
                case 4: if(nearby.Count>0)ScheduleLegendaryPulse(nearby[nearby.Count-1].transform.position,power*.7f,1.8f,3,maker);break;

                case 6: ScheduleLegendaryPulse(origin,power*.65f,2.3f,3,maker);break;
                case 7: CombatVisuals.ElementBlast(PlayerPosition,3f,world);foreach(var foe in LegendaryGunTargets(PlayerPosition,3f))LegendaryStrike(foe,power,color);break;
                case 8: ScheduleLegendaryPulse(PlayerPosition-dodgeDirection*1.4f,power*1.25f,2.5f,1,maker,.6f);break;
                case 9: if(closest!=null)ScheduleLegendaryPulse(closest.transform.position,power*1.1f,2f,1,maker);break;

                case 11: for(int i=0;i<Mathf.Min(3,nearby.Count);i++){CombatVisuals.ElementArc(origin+Vector3.up*.6f,nearby[i].transform.position+Vector3.up*.7f,world);LegendaryStrike(nearby[i],power*.8f,color);}break;
                case 12: ArmorInterceptShots(PlayerPosition,3,12,color);break;
                case 13: for(int i=0;i<Mathf.Min(3,nearby.Count);i++){CombatVisuals.ElementArc(PlayerPosition+Vector3.up*.7f,nearby[i].transform.position+Vector3.up*.7f,world);LegendaryStrike(nearby[i],power*.65f,color);}break;
                case 14: if(closest!=null){CombatVisuals.ElementArc(PlayerPosition+Vector3.up*.7f,closest.transform.position+Vector3.up*.7f,world);LegendaryStrike(closest,power*1.3f,color);}break;

                case 16: ScheduleLegendaryPulse(origin,power*.65f,2f,3,maker);break;
                case 17: if(hp<maxHp*.5f)HealPlayer(maxHp*.12f);break;
                case 18: if(nearby.Count>=2)ScheduleLegendaryPulse(PlayerPosition,power*.7f,2.5f,3,maker);break;
                case 19: if(ArtifactShield>=3){ArtifactShield-=3;HealPlayer(5);}else ArmorBarrier(3);UpdateBarrierVisual();break;

                case 21: ScheduleLegendaryPulse(origin,power*.8f,1.8f,1,maker);break;
                case 22: foreach(var foe in nearby)if(LegendaryGunInCone(PlayerPosition,aim,foe.transform.position,5,.65f))LegendaryStrike(foe,power*.65f,color);break;
                case 23: for(int i=0;i<2;i++)ScheduleLegendaryPulse(PlayerPosition-dodgeDirection*(i+.5f),power*.38f,1.1f,2,maker);break;
                case 24: if(closest!=null)ScheduleLegendaryPulse(closest.transform.position,power*.75f,1.6f,1,maker);break;

                case 26: ArmorBarrier(20);break;
                case 27: ArmorInterceptShots(PlayerPosition,2.8f,10,color);break;
                case 28: ScheduleLegendaryPulse(PlayerPosition-dodgeDirection,power,2f,2,maker);break;
                case 29: if(ArtifactShield>=5){ArtifactShield-=5;UpdateBarrierVisual();foreach(var foe in nearby)if((foe.transform.position-PlayerPosition).sqrMagnitude<6.25f)LegendaryStrike(foe,power*.8f,color);}break;
            }
            Burst(PlayerPosition,color,12);
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
            var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;line.startWidth=.14f;line.endWidth=.06f;
            line.material=new Material(Resources.Load<Shader>("VaultTrail"));line.material.color=color;line.startColor=line.endColor=color;
            if(maker==0||maker==2||maker==3)
            {
                int links=maker==2?Mathf.Min(3,targets.Count):Mathf.Min(2,targets.Count);line.positionCount=1+links*8;
                Vector3 from=origin+Vector3.up*.65f;line.SetPosition(0,from);
                for(int n=0;n<links;n++){Vector3 to=targets[n].transform.position+Vector3.up*.7f;for(int k=1;k<=8;k++){float t=k/8f;Vector3 at=Vector3.Lerp(from,to,t);if(maker==2&&k<8)at+=Vector3.up*Mathf.Sin(k*2.3f)*.38f;line.SetPosition(n*8+k,at);}from=to;}
            }
            else
            {
                line.loop=true;line.positionCount=48;float radius=maker==4?4:maker==1?2.8f:2.5f;
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,origin+new Vector3(Mathf.Cos(a)*radius,.15f,Mathf.Sin(a)*radius));}
            }
            obj.AddComponent<LegendarySignatureLifetime>();Destroy(obj,.38f);
        }
        void LegendaryStrike(Foe foe,float damage,Color color)
        {
            if(foe==null||foe.dead)return;var point=foe.transform.position+Vector3.up*.75f;
            CombatVisuals.Impact(point,Vector3.up,color,GunEffect.Normal,world);
            Burst(point,color,7);
            foe.Hit(damage,point,color,100,Shot.LegendaryDamageColor);
        }
        public void LegendaryBulletHit(Foe foe,Vector3 point)=>LegendaryTrigger("hit",point,foe);
        void LegendaryModifyShot(Shot shot)
        {
            if(shot==null||!shot.friendly)return;
            if(legendaryClock<legendaryPrecisionUntil){shot.damage*=1.25f;shot.hitAccuracy=100;shot.legendaryEffect=true;}
        }
    }
    public sealed class LegendarySignatureLifetime:MonoBehaviour
    {void OnDestroy(){var line=GetComponent<LineRenderer>();if(line!=null&&line.sharedMaterial!=null)Destroy(line.sharedMaterial);}}
}
