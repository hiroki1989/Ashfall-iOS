using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public enum GunElement { None, Fire, Electric, Poison, Explosive }

    public static class GunElements
    {
        // Apply only to effects with an explicit elemental theme. Other relics
        // keep the same manufacturer-weighted random roll as ordinary guns.
        public static GunElement LegendaryAffinity(Gun gun)
        {
            if(gun==null)return GunElement.None;
            if(BossRelicFoundation.Decode(gun.bossRelicId,out int boss,out int category))
            {
                if(category<5)
                {
                    if(boss==0&&(category==0||category==1))return GunElement.Explosive;
                    if((boss==0||boss==1)&&category==2)return GunElement.Electric;
                    if(boss==2&&category==1)return GunElement.Explosive;
                }
            }
            if(!VaultGame.DecodeLegendaryKey(gun.legendaryKey,out int maker,out int variant)||!gun.legendaryKey.StartsWith("gun-",StringComparison.Ordinal))return GunElement.None;
            // The later series' final mechanism describes a heavy explosion.
            if(variant==14)return GunElement.Explosive;
            if(variant>=5)return GunElement.None;
            if(maker==2)return GunElement.Electric; // Voltarc's five electrical mechanisms.
            if(maker==3&&(variant==2||variant==4))return GunElement.Poison; // spore fields.
            if(maker==1&&variant==1)return GunElement.Explosive; // impact detonation.
            if(maker==4&&variant==2)return GunElement.Explosive;
            if(maker==4&&variant==4)return GunElement.Fire; // ember trail.
            return GunElement.None;
        }

        // Existing save data may predate affinity rules. Preserve matching rolls
        // and all unrelated guns, and deterministically replace only mismatches.
        public static void MigrateLegendaryAffinity(Gun gun)
        {
            if(gun==null||gun.elementAffinityRevision>0)return;
            GunElement affinity=LegendaryAffinity(gun);
            if(affinity==GunElement.None)return;
            gun.elementAffinityRevision=1;
            if(gun.element==affinity&&gun.elementPercent>0){gun.elementVersion=1;return;}
            unchecked
            {
                uint hash=2166136261;
                foreach(char c in (gun.id??"")+"|"+(gun.bossRelicId??"")+"|"+(gun.legendaryKey??"")){hash^=c;hash*=16777619;}
                SetElement(gun,affinity,new System.Random((int)(hash&0x7fffffff)));
            }
        }

        // Boss identity is attached after its base gun was created. Reapply the
        // complete roll after that assignment so its own effect takes priority.
        public static void RollBossGun(Gun gun,System.Random random)
        {
            if(gun==null||random==null)return;
            gun.elementVersion=0;gun.elementAffinityRevision=0;
            Roll(gun,random);
        }

        public static string Description(Gun g,bool ja)
        {
            if(g==null||g.element==GunElement.None)return "";
            string pct=(g.elementPercent*100).ToString("0.#")+"%";
            switch(g.element)
            {
                case GunElement.Fire:return ja?"炎: 追加ダメージ "+pct:"FIRE: bonus damage "+pct;
                case GunElement.Electric:return ja?"電気: 追加ダメージ "+pct+" / 連鎖 "+g.elementChains+"体 / 範囲 "+g.elementRadius.ToString("0.0")+"m":"ELECTRIC: bonus "+pct+" / chains "+g.elementChains+" / radius "+g.elementRadius.ToString("0.0")+"m";
                case GunElement.Poison:
                    pct=(PoisonPercent(g.elementPercent)*100).ToString("0.#")+"%";
                    return ja?"毒: 人間に毎秒 "+pct+" / "+g.elementDuration.ToString("0.0")+"秒、感染者には無効":"POISON: "+pct+" per second against humans / "+g.elementDuration.ToString("0.0")+"s; ineffective against infected";
                default:return ja?"爆発: 追加ダメージ "+pct+" / 範囲 "+g.elementRadius.ToString("0.0")+"m":"EXPLOSIVE: bonus "+pct+" / radius "+g.elementRadius.ToString("0.0")+"m";
            }
        }
        public static void Roll(Gun gun,System.Random random)
        {
            if(gun==null||random==null||gun.elementVersion>0)return;
            gun.elementVersion=1;
            GunElement affinity=LegendaryAffinity(gun);
            if(affinity!=GunElement.None){gun.elementAffinityRevision=1;SetElement(gun,affinity,random);return;}
            int maker=ManufacturerCatalog.Index(gun.manufacturerId);
            // Kestrel favours conventional ammunition; other manufacturers have
            // distinct elemental biases. Rolls are stored on the item.
            int[,] weights={{78,7,6,4,5},{62,8,8,3,19},{54,7,29,4,6},{54,6,6,29,5},{52,24,5,7,12},{72,6,10,5,7}};
            maker=Mathf.Clamp(maker,0,5);int roll=random.Next(100),selected=0;
            for(int i=0;i<5;i++){roll-=weights[maker,i];if(roll<0){selected=i;break;}}
            SetElement(gun,(GunElement)selected,random);
        }
        static void SetElement(Gun gun,GunElement element,System.Random random)
        {
            gun.element=element;gun.elementPercent=0;gun.elementRadius=0;gun.elementDuration=0;gun.elementChains=0;gun.elementVersion=1;
            float Range(float lo,float hi)=>lo+(float)random.NextDouble()*(hi-lo);
            switch(gun.element)
            {
                case GunElement.Fire:gun.elementPercent=Range(.32f,.55f);break;
                case GunElement.Electric:
                    gun.elementPercent=Range(.08f,.16f);gun.elementRadius=Range(2f,4f);
                    int chainRoll=random.Next(100);gun.elementChains=chainRoll<45?1:chainRoll<73?2:chainRoll<90?3:chainRoll<98?4:5;break;
                case GunElement.Poison:gun.elementPercent=Range(.19f,.25f);gun.elementDuration=Range(3f,5f);break;
                case GunElement.Explosive:gun.elementPercent=Range(.17f,.28f);gun.elementRadius=Range(1.4f,2.8f);break;
            }
        }
        public static Color ColorFor(GunElement type)
        {
            switch(type){case GunElement.Fire:return new Color(1f,.16f,.10f);case GunElement.Electric:return new Color(1f,.87f,.12f);case GunElement.Poison:return new Color(.75f,.22f,.95f);case GunElement.Explosive:return new Color(.91f,.31f,.025f);default:return Color.white;}
        }
        // Saves created before the rebalance contain rolls between .055 and .09.
        // Preserve their relative quality without rewriting the stored item.
        public static float PoisonPercent(float value)
        {
            if(value<=0f)return 0f;
            if(value<.1f)return Mathf.Lerp(.19f,.25f,Mathf.InverseLerp(.055f,.09f,value));
            return value;
        }
        public static bool IsPoisonSusceptible(Foe target)=>target!=null&&(target.kind==10||target.RaiderBoss);
        public static void Apply(Shot shot,Foe target,Vector3 point)
        {
            if(shot==null||target==null||shot.element==GunElement.None||shot.elementPercent<=0)return;
            float extra=shot.damage*shot.elementPercent*Mathf.Max(0,shot.elementBonus);Color color=ColorFor(shot.element);
            var game=shot.game;var parent=target.transform.parent;
            if(shot.element==GunElement.Poison)
            {
                if(!target.dead&&IsPoisonSusceptible(target)){CombatVisuals.ElementImpact(point,shot.element,parent);var poison=target.GetComponent<GunPoison>();if(!poison)poison=target.gameObject.AddComponent<GunPoison>();poison.Apply(game,shot.damage*PoisonPercent(shot.elementPercent)*Mathf.Max(0,shot.elementBonus),shot.elementDuration);}
                return;
            }
            CombatVisuals.ElementImpact(point,shot.element,parent);
            if(!target.dead)target.Hit(extra,point,color,100,color);
            if(shot.element==GunElement.Explosive)
            {
                CombatVisuals.ElementBlast(point,shot.elementRadius,parent);
                foreach(var foe in game.Enemies.ToArray())if(foe!=null&&!foe.dead&&foe!=target&&Vector3.Distance(foe.transform.position,target.transform.position)<=shot.elementRadius&&game.LineOfSight(target.transform.position,foe.transform.position))
                {Vector3 p=foe.transform.position+Vector3.up*.8f;CombatVisuals.ElementImpact(p,GunElement.Explosive,parent);foe.Hit(extra,p,color,100,color);}
            }
            else if(shot.element==GunElement.Electric)
            {
                var seen=new HashSet<Foe>{target};Foe from=target;
                for(int i=0;i<Mathf.Clamp(shot.elementChains,1,5);i++)
                {
                    Foe next=null;float best=shot.elementRadius*shot.elementRadius;
                    foreach(var foe in game.Enemies)if(foe!=null&&!foe.dead&&!seen.Contains(foe))
                    {Vector3 d=foe.transform.position-from.transform.position;d.y=0;float dist=d.sqrMagnitude;if(dist<=best&&game.LineOfSight(from.transform.position,foe.transform.position)){best=dist;next=foe;}}
                    if(next==null)break;seen.Add(next);
                    Vector3 a=from.transform.position+Vector3.up*.8f,b=next.transform.position+Vector3.up*.8f;
                    CombatVisuals.ElementArc(a,b,parent);
                    CombatVisuals.ElementImpact(b,GunElement.Electric,parent);next.Hit(extra,b,color,100,color);from=next;
                }
            }
        }
    }
    public sealed class GunPoison : MonoBehaviour
    {
        sealed class Stack { public float damage,remaining,timer; }
        VaultGame game;Foe foe;readonly List<Stack> stacks=new List<Stack>();
        public void Apply(VaultGame owner,float perSecond,float duration)
        {
            game=owner;foe=GetComponent<Foe>();
            if(stacks.Count>=512)
            {
                // In unusually dense fire, merge with the closest expiry without
                // extending existing poison or producing unbounded damage over time.
                var closest=stacks[0];
                for(int i=1;i<stacks.Count;i++)if(Mathf.Abs(stacks[i].remaining-duration)<Mathf.Abs(closest.remaining-duration))closest=stacks[i];
                closest.damage+=perSecond;closest.remaining=Mathf.Min(closest.remaining,duration);return;
            }
            stacks.Add(new Stack{damage=perSecond,remaining=duration});
        }
        void Update()
        {
            if(game==null||foe==null||foe.dead){Destroy(this);return;}
            if(!game.Active)return;
            float dt=Time.deltaTime,total=0;
            for(int i=stacks.Count-1;i>=0;i--)
            {
                var stack=stacks[i];float active=Mathf.Min(dt,Mathf.Max(0,stack.remaining));stack.remaining-=dt;stack.timer+=active;
                while(stack.timer>=1f){stack.timer-=1f;total+=stack.damage;}
                if(stack.remaining<=0)stacks.RemoveAt(i);
            }
            if(total>0&&!foe.dead){Vector3 p=transform.position+Vector3.up*.8f;Color c=GunElements.ColorFor(GunElement.Poison);CombatVisuals.ElementImpact(p,GunElement.Poison,transform.parent);foe.Hit(total,p,c,100,c);}
            if(stacks.Count==0)Destroy(this);
        }
    }
}
