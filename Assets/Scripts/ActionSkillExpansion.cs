using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        // Indices are stable across versions: skill, branch, tier, then branch choice.
        readonly int[] actionSkillExtraRanks = new int[180];
        readonly Dictionary<string,float> actionSkillExtraValues = new Dictionary<string,float>();
        bool actionSkillExtraValuesDirty = true;
        static int ExtraIndex(ActionSkillKind k,int b,int t,int s) => (((int)k*3+b)*5+t)*4+s;
        static bool ValidExtra(ActionSkillKind k,int b,int t,int s) => ValidActionSkill(k,b,t)&&s>=0&&s<4;
        public int ActionSkillExtraRank(ActionSkillKind k,int b,int t,int s)
        { LoadActionSkills(); return ValidExtra(k,b,t,s)?actionSkillExtraRanks[ExtraIndex(k,b,t,s)]:0; }
        public int ActionSkillExtraTotal(ActionSkillKind k,int b,int s)
        { int n=0; for(int t=0;t<5;t++)n+=ActionSkillExtraRank(k,b,t,s); return n; }
        public bool ActionSkillExtraAvailable(ActionSkillKind k,int b,int t,int s)
        {
            if(!ValidExtra(k,b,t,s)||ActionSkillExtraRank(k,b,t,s)!=0||ActionSkillAvailablePoints<=0)return false;
            if(ActionSkillRank(k,b,t)<1)return false;
            return s==0 || (s<3?ActionSkillExtraRank(k,b,t,0)>0:ActionSkillExtraRank(k,b,t,1)>0||ActionSkillExtraRank(k,b,t,2)>0);
        }
        public bool TryUpgradeActionSkillExtra(ActionSkillKind k,int b,int t,int s)
        {
            if(!ActionSkillExtraAvailable(k,b,t,s))return false;
            actionSkillExtraRanks[ExtraIndex(k,b,t,s)]=1;actionSkillExtraValuesDirty=true;SaveActionSkills();RecalculateArmor();FirstStepsSkillUpgraded();PlayUserSound("skill",.35f);return true;
        }
        static readonly string[,,] ExtraEffects = {
            {{"turret_damage","handgun_damage","turret_rate","turret_duration"},{"turret_duration","turret_range","sniper_damage","turret_damage"},{"turret_range","electric_damage","turret_damage","turret_rate"}},
            {{"grenade_damage","handgun_damage","grenade_radius","grenade_count"},{"grenade_radius","poison_damage","grenade_damage","grenade_radius"},{"grenade_damage","explosive_damage","sniper_damage","grenade_radius"}},
            {{"medic_heal","handgun_damage","medic_duration","medic_heal"},{"medic_radius","sniper_damage","medic_heal","medic_duration"},{"medic_duration","fire_damage","medic_radius","medic_heal"}}
        };
        static readonly string[,] ExtraPassive = {
            {"assault_damage","reload_speed"},{"heavy_damage","move_speed"},{"accuracy","action_charge"},
            {"shotgun_damage","critical_chance"},{"fire_rate","move_speed"},{"critical_damage","reload_speed"},
            {"max_health","damage_reduction"},{"critical_damage","critical_chance"},{"max_health","action_charge"}
        };
        static string ExtraEffect(ActionSkillKind k,int b,int t,int s)
        {
            if(k==ActionSkillKind.Mechanic&&b==0&&t==1&&s==2)return "pet_damage";
            if(k==ActionSkillKind.Medic&&b==1&&t==2&&s==2)return "pet_speed";
            if(k==ActionSkillKind.Mechanic&&b==1&&t==4&&s==3)return "turret_double";
            if(k==ActionSkillKind.Mechanic&&b==2&&t==2&&s==3)return "turret_element";
            if(k==ActionSkillKind.Bomber&&b==2&&t==2&&s==3)return "grenade_element";
            if(k==ActionSkillKind.Medic&&b==2&&t==2&&s==3)return "medic_debuff";
            if(s==3)
            {
                if(k==ActionSkillKind.Mechanic&&b==0&&t==2)return "turret_focus";
                if(k==ActionSkillKind.Mechanic&&b==1&&t==2)return "turret_follow";
                if(k==ActionSkillKind.Bomber&&b==0&&t==2)return "grenade_heal";
                if(k==ActionSkillKind.Bomber&&b==1&&t==2)return "grenade_suppress";
                if(k==ActionSkillKind.Medic&&b==0&&t==2)return "medic_guard";
                if(k==ActionSkillKind.Medic&&b==1&&t==2)return "medic_damage";
                if(k==ActionSkillKind.Medic&&b==2&&t==1)return "medic_follow";
            }
            // Later choices previously bundled a tactical effect with two damage
            // bonuses. Give that tactical effect its own stable saved node instead.
            string tactical=ExtraTactical(k,b,t,s);
            if(tactical!=null)return tactical;
            if(s==1&&(t==1||t==3))return ExtraPassive[(int)k*3+b,t==1?0:1];
            if(t>0)
            {
                // Each saved position remains unchanged; later tiers offer a wider
                // selection of utility rather than repeating damage on every row.
                string[,] utility={
                    {"reload_speed","accuracy","move_speed","action_charge","critical_chance","turret_range","turret_duration","fire_rate"},
                    {"move_speed","reload_speed","critical_chance","action_charge","grenade_radius","grenade_count","accuracy","critical_damage"},
                    {"max_health","damage_reduction","move_speed","action_charge","medic_radius","medic_duration","reload_speed","critical_chance"}
                };
                string current=ExtraEffects[(int)k,b,s];
                if(current.EndsWith("_damage"))return utility[(int)k,(b*3+t+s)%8];
            }
            return ExtraEffects[(int)k,b,s];
        }
        static float ExtraAmount(string id,int tier)
        {
            float tactical=ExtraTacticalAmount(id);
            if(tactical>0)return tactical;
            switch(id)
            {
                case "pet_damage":return .25f;
                case "pet_speed":return .20f;
                case "turret_double":case "turret_element":case "grenade_element":case "medic_debuff":case "turret_follow":case "medic_follow":return 1;
                case "turret_focus":case "medic_guard":return .05f;
                case "grenade_heal":return .01f;
                case "medic_damage":return .02f;
                case "grenade_suppress":return .25f;
                case "grenade_count":return 1;
                case "turret_range":case "medic_radius":case "grenade_radius":return .25f+.05f*tier;
                case "turret_duration":case "medic_duration":return .5f+.15f*tier;
                default:return .025f+.005f*tier;
            }
        }
        // Late-tree choices use positioning and interaction with the active device.
        static string ExtraTactical(ActionSkillKind k,int b,int t,int slot)
        {
            if(slot!=0)return null;
            if(k==ActionSkillKind.Mechanic && t>=3)
            {
                string[,] ids={{"turret_near_damage","turret_near_reload"},
                    {"turret_near_critical","turret_near_guard"},
                    {"turret_hit_charge","turret_near_element"}};
                return ids[b,t-3];
            }
            if(t!=4)return null;
            if(k==ActionSkillKind.Bomber)
            {
                string[] ids={"grenade_hit_heal","grenade_hit_charge","grenade_slow"};return ids[b];
            }
            if(k==ActionSkillKind.Medic)
            {
                string[] ids={"medic_field_damage","medic_field_reload","medic_field_charge"};return ids[b];
            }
            return null;
        }
        static float ExtraTacticalAmount(string id)
        {
            switch(id)
            {
                case "turret_near_damage":case "turret_hit_charge":case "medic_field_damage":return .10f;
                case "turret_near_reload":case "grenade_hit_charge":case "medic_field_reload":return .15f;
                case "turret_near_critical":return .05f;
                case "turret_near_guard":return .06f;
                case "turret_near_element":case "medic_field_charge":return .12f;
                case "grenade_hit_heal":return .005f;
                case "grenade_slow":return .5f;
                default:return 0;
            }
        }
        static string ExtraTacticalDescription(string id,bool ja)
        {
            switch(id)
            {
                case "turret_near_damage":return ja?"稼働中のタレットから8m以内では銃威力+10%。":"Gun damage +10% within 8m of an active turret.";
                case "turret_near_reload":return ja?"稼働中のタレットから8m以内ではリロード速度+15%。":"Reload speed +15% within 8m of an active turret.";
                case "turret_near_critical":return ja?"稼働中のタレットから8m以内では会心率+5%。":"Critical chance +5% within 8m of an active turret.";
                case "turret_near_guard":return ja?"稼働中のタレットから8m以内では被ダメージを6%軽減。":"Damage taken -6% within 8m of an active turret.";
                case "turret_hit_charge":return ja?"タレット稼働中、銃の命中によるアクションポイント獲得+10%。":"Bullet hits grant 10% more action points while a turret is active.";
                case "turret_near_element":return ja?"稼働中のタレットから8m以内では属性の追加ダメージ+12%。":"Elemental bonus damage +12% within 8m of an active turret.";
                case "grenade_hit_heal":return ja?"爆風が敵に当たると1体ごとに最大HPの0.5%を回復。最大5体。":"Restore 0.5% maximum HP per blast target, up to five targets.";
                case "grenade_hit_charge":return ja?"爆風が敵に当たるとアクションポイントを最大値の15%回復。1回の発動につき1回。":"A blast hit restores 15% of the action gauge once per activation.";
                case "grenade_slow":return ja?"爆風を受けた敵の次の攻撃を0.5秒遅らせる。":"Delay the next attack of blast targets by 0.5 seconds.";
                case "medic_field_damage":return ja?"回復エリア内では銃威力+10%。":"Gun damage +10% inside the healing field.";
                case "medic_field_reload":return ja?"回復エリア内ではリロード速度+15%。":"Reload speed +15% inside the healing field.";
                case "medic_field_charge":return ja?"回復エリア内では銃の命中によるアクションポイント獲得+12%。":"Bullet hits grant 12% more action points inside the healing field.";
                default:return "";
            }
        }
        public float ActionSkillExtraValue(string effectId)
        {
            LoadActionSkills();
            if(actionSkillExtraValuesDirty)
            {
                actionSkillExtraValues.Clear();
                for(int k=0;k<3;k++)for(int b=0;b<3;b++)for(int t=0;t<5;t++)for(int slot=0;slot<4;slot++)
                {
                    if(actionSkillExtraRanks[ExtraIndex((ActionSkillKind)k,b,t,slot)]==0)continue;
                    string id=ExtraEffect((ActionSkillKind)k,b,t,slot);
                    actionSkillExtraValues.TryGetValue(id,out float current);
                    actionSkillExtraValues[id]=current+ExtraAmount(id,t);
                }
                actionSkillExtraValuesDirty=false;
            }
            return effectId!=null&&actionSkillExtraValues.TryGetValue(effectId,out float value)?value:0;
        }
        static string ExtraLabel(string id,bool ja)
        {
            switch(id)
            {
                case "pet_damage":return ja?"犬のかみつき威力":"Companion bite damage";
                case "pet_speed":return ja?"犬の攻撃速度":"Companion attack speed";
                case "turret_near_damage":return ja?"砲台援護":"Turret cover";
                case "turret_near_reload":return ja?"補給支援":"Supply cover";
                case "turret_near_critical":return ja?"狙撃支援":"Targeting cover";
                case "turret_near_guard":return ja?"防護支援":"Defensive cover";
                case "turret_hit_charge":return ja?"連動充電":"Linked charge";
                case "turret_near_element":return ja?"属性支援":"Elemental cover";
                case "grenade_hit_heal":return ja?"爆風回復":"Blast recovery";
                case "grenade_hit_charge":return ja?"爆風充電":"Blast charge";
                case "grenade_slow":return ja?"衝撃遅延":"Concussive delay";
                case "medic_field_damage":return ja?"治療域の援護":"Field fire";
                case "medic_field_reload":return ja?"治療域の補給":"Field supply";
                case "medic_field_charge":return ja?"治療域の充電":"Field charge";
                case "turret_damage":return ja?"タレット威力":"Turret damage";case "turret_rate":return ja?"タレット連射":"Turret fire rate";
                case "turret_range":return ja?"タレット射程":"Turret range";case "turret_duration":return ja?"タレット持続":"Turret duration";
                case "turret_focus":return ja?"集中射撃":"Focused fire";case "turret_follow":return ja?"追尾架台":"Mobile turret";
                case "grenade_suppress":return ja?"爆風の足止め":"Concussive blast";case "grenade_heal":return ja?"戦闘回復":"Combat recovery";
                case "medic_damage":return ja?"危険な治療域":"Hostile field";case "medic_guard":return ja?"治療域の防護":"Field protection";case "medic_follow":return ja?"移動診療":"Mobile clinic";
                case "turret_double":return ja?"二連タレット":"Twin turrets";case "turret_element":return ja?"属性伝達・砲台":"Turret infusion";
                case "grenade_damage":return ja?"グレネード威力":"Grenade damage";case "grenade_radius":return ja?"爆発範囲":"Blast radius";
                case "grenade_count":return ja?"追加グレネード":"Extra grenade";case "grenade_element":return ja?"属性伝達・爆弾":"Grenade infusion";
                case "medic_heal":return ja?"回復量":"Healing";case "medic_radius":return ja?"回復範囲":"Healing radius";
                case "medic_duration":return ja?"回復エリア持続":"Field duration";case "medic_debuff":return ja?"治療域の妨害":"Suppressive field";
                case "shotgun_damage":return ja?"ショットガン威力":"Shotgun damage";
                case "assault_damage":return ja?"アサルトライフル威力":"Assault rifle damage";
                case "heavy_damage":return ja?"ヘビーマシンガン威力":"Heavy machine gun damage";
                case "accuracy":return ja?"命中精度":"Accuracy";
                case "fire_rate":return ja?"連射速度":"Fire rate";
                case "reload_speed":return ja?"リロード速度":"Reload speed";
                case "max_health":return ja?"最大HP":"Maximum HP";
                case "move_speed":return ja?"移動速度":"Movement speed";
                case "critical_chance":return ja?"会心率":"Critical chance";
                case "critical_damage":return ja?"会心威力":"Critical damage";
                case "damage_reduction":return ja?"ダメージ軽減":"Damage reduction";
                case "action_charge":return ja?"アクションポイント獲得量":"Action point gain";
                case "handgun_damage":return ja?"ハンドガン威力":"Handgun damage";case "sniper_damage":return ja?"スナイパー威力":"Sniper damage";
                case "fire_damage":return ja?"炎属性威力":"Fire damage";case "electric_damage":return ja?"電気属性威力":"Shock damage";
                case "poison_damage":return ja?"毒属性威力":"Poison damage";case "explosive_damage":return ja?"爆発属性威力":"Explosive damage";
                default:return id;
            }
        }
        public string ActionSkillExtraTitle(ActionSkillKind k,int b,int t,int s)
        {
            if(!ValidExtra(k,b,t,s))return "";
            string id=ExtraEffect(k,b,t,s);string[] levels={"I","II","III","IV","V"};
            return ExtraLabel(id,VaultLanguage.Japanese)+" "+levels[t];
        }
        public string ActionSkillExtraDescription(ActionSkillKind k,int b,int t,int s)
        {
            if(!ValidExtra(k,b,t,s))return "";
            string id=ExtraEffect(k,b,t,s);bool ja=VaultLanguage.Japanese;
            string tacticalDescription=ExtraTacticalDescription(id,ja);
            if(tacticalDescription.Length>0)return tacticalDescription;
            switch(id)
            {
                case "turret_focus":return ja?"同じ敵への連続射撃ごとに威力+5%、最大5回。威力は武器の95%が上限。":"Each consecutive turret shot at the same target adds 5% damage, up to five times. Damage is capped at 95% of the weapon.";
                case "turret_follow":return ja?"タレットがプレイヤーを追尾する。":"Turret follows the player.";
                case "grenade_suppress":return ja?"爆風に当たった敵の次の攻撃を0.25秒遅らせる。":"The blast delays each hit enemy’s next attack by 0.25 seconds.";
                case "grenade_heal":return ja?"爆風が敵に当たると1体ごとに最大HPの1%を回復。1回につき最大5体。":"Restore 1% maximum HP per enemy hit, up to five per blast.";
                case "medic_damage":return ja?"回復エリア内の敵に毎秒、装備中の銃のDPSの6%のダメージ。":"Enemies in the field take damage per second equal to 6% of equipped weapon DPS.";
                case "medic_guard":return ja?"回復エリア内で受けるダメージを5%軽減。":"Reduce damage taken in the healing field by 5%.";
                case "medic_follow":return ja?"回復エリアがプレイヤーを追尾する。":"The healing field follows the player.";
                case "turret_double":return ja?"タレットを2台設置。2台目の威力は1台目の70%。":"Deploy two turrets; the second deals 70% of the first turret's damage.";
                case "turret_element":return ja?"タレットに装備中の銃の属性を付与。":"Turrets inherit the equipped gun's element.";
                case "grenade_element":return ja?"グレネードに装備中の銃の属性を付与。":"Grenades inherit the equipped gun's element.";
                case "medic_debuff":return ja?"回復エリア内の敵の攻撃を妨害。":"Suppress enemy attacks inside the healing field.";
            }
            float amount=ExtraAmount(id,t);
            string suffix=id=="grenade_count"?(ja?"個":""):id=="turret_range"||id=="medic_radius"||id=="grenade_radius"?"m":id=="turret_duration"||id=="medic_duration"?(ja?"秒":"s"):"%";
            float displayed=suffix=="%"?amount*100:amount;
            string result=ExtraLabel(id,ja)+" +"+displayed.ToString("0.#")+suffix;
            if(id=="turret_damage")result+=ja?"\n上限：武器威力の95%（2台目は70%）。":"\nLimit: 95% of weapon damage (second turret: 70% of the first).";
            if(id=="turret_rate")result+=ja?"\n上限：武器連射速度の135%。":"\nLimit: 135% of weapon fire rate.";
            if(id=="turret_duration")result+=ja?"\n持続時間の上限：22秒。":"\nDuration limit: 22 seconds.";
            if(id=="grenade_radius")result+=ja?"\n爆発範囲の上限：7m。":"\nBlast radius limit: 7m.";
            if(id=="medic_heal")result+=ja?"\n回復量の上限：最大HPの毎秒9%。":"\nHealing limit: 9% maximum HP per second.";
            if(id=="medic_duration")result+=ja?"\n持続時間の上限：24秒。":"\nDuration limit: 24 seconds.";
            return result;
        }
    }
}


