using System;
using UnityEngine;

namespace NeonVault
{
    public enum ActionSkillKind { Mechanic, Bomber, Medic }

    public partial class VaultGame
    {
        const string ActionSkillSaveKey = "Vault.ActionSkills.v1";
        const int ActionSkillSlots = 45;
        [Serializable] sealed class ActionSkillSave { public int version=1,selected; public int[] ranks; }
        readonly int[] actionSkillRanks = new int[ActionSkillSlots];
        bool actionSkillLoaded;
        ActionSkillKind actionSkillSelected;
        public float ActionSkillPoints { get; private set; }
        public int ActionSkillChargeMax => 15;
        public ActionSkillKind ActionSkillSelected { get { LoadActionSkills(); return actionSkillSelected; } }
        public static int ActionSkillMaxRank(int tier) => tier==4?1:6;
        public bool ActionSkillTierAvailable(ActionSkillKind kind,int branch,int tier)
        {return ValidActionSkill(kind,branch,tier)&&(tier==0||ActionSkillRank(kind,branch,tier-1)>=(tier==4?6:3));}
        static int ActionSkillIndex(ActionSkillKind kind,int branch,int tier)=>(int)kind*15+branch*5+tier;
        static bool ValidActionSkill(ActionSkillKind kind,int branch,int tier)=>(int)kind>=0&&(int)kind<3&&branch>=0&&branch<3&&tier>=0&&tier<5;
        void LoadActionSkills()
        {
            if(actionSkillLoaded)return;actionSkillLoaded=true;
            if(smoke||!PlayerPrefs.HasKey(ActionSkillSaveKey))return;
            try
            {
                var data=JsonUtility.FromJson<ActionSkillSave>(PlayerPrefs.GetString(ActionSkillSaveKey));
                if(data==null||data.version!=1)return;
                actionSkillSelected=(ActionSkillKind)Mathf.Clamp(data.selected,0,2);
                if(data.ranks!=null)for(int i=0;i<Mathf.Min(data.ranks.Length,ActionSkillSlots);i++)actionSkillRanks[i]=Mathf.Clamp(data.ranks[i],0,ActionSkillMaxRank(i%5));
            }
            catch(Exception e){Debug.LogWarning("Action skill save load: "+e.GetType().Name);}
        }
        void SaveActionSkills()
        {
            if(smoke)return;
            PlayerPrefs.SetString(ActionSkillSaveKey,JsonUtility.ToJson(new ActionSkillSave{selected=(int)actionSkillSelected,ranks=actionSkillRanks}));PlayerPrefs.Save();
        }
        public int ActionSkillRank(ActionSkillKind kind,int branch,int tier)
        {LoadActionSkills();return ValidActionSkill(kind,branch,tier)?actionSkillRanks[ActionSkillIndex(kind,branch,tier)]:0;}
        public int ActionSkillSpentPoints { get { LoadActionSkills();int total=0;foreach(int rank in actionSkillRanks)total+=rank;return total; } }
        public int ActionSkillAvailablePoints => Mathf.Max(0,DebtLevel-1+ExpeditionTokens-ActionSkillSpentPoints);
        public bool TryUpgradeActionSkill(ActionSkillKind kind,int branch,int tier)
        {
            LoadActionSkills();if(!ValidActionSkill(kind,branch,tier)||ActionSkillAvailablePoints<=0)return false;
            int index=ActionSkillIndex(kind,branch,tier);if(actionSkillRanks[index]>=ActionSkillMaxRank(tier))return false;
            if(!ActionSkillTierAvailable(kind,branch,tier))return false;
            actionSkillRanks[index]++;SaveActionSkills();RefreshPermanentStats();return true;
        }
        public void SelectActionSkill(ActionSkillKind kind)
        {
            LoadActionSkills();if((int)kind<0||(int)kind>2||kind==actionSkillSelected)return;
            ClearActiveActionSkill();actionSkillSelected=kind;ActionSkillPoints=0;SaveActionSkills();
        }
        public void ResetActionSkills()
        {LoadActionSkills();Array.Clear(actionSkillRanks,0,actionSkillRanks.Length);ActionSkillPoints=0;ClearActiveActionSkill();SaveActionSkills();RefreshPermanentStats();}

        // Passive effects are retained across the three trees when changing the equipped action.
        public float ActionSkillPassive(string kind,int family=-1)
        {
            float mechanic=ActionSkillRank(ActionSkillKind.Mechanic,0,1), bomber=ActionSkillRank(ActionSkillKind.Bomber,0,1), medic=ActionSkillRank(ActionSkillKind.Medic,0,1);
            float m2=ActionSkillRank(ActionSkillKind.Mechanic,1,3), b2=ActionSkillRank(ActionSkillKind.Bomber,1,3), h2=ActionSkillRank(ActionSkillKind.Medic,1,3);
            switch(kind)
            {
                case "Damage": return family==2||family==4?mechanic*.012f:family==1?bomber*.014f:0;
                case "FireRate": return (family==2||family==4?m2*.01f:family==1?b2*.01f:0);
                case "Health": return medic*3f;
                case "Recovery": return h2*.12f;
                case "DamageReduction": return ActionSkillRank(ActionSkillKind.Medic,2,1)*.01f;
                case "Reload": return ActionSkillRank(ActionSkillKind.Mechanic,2,1)*.012f;
                case "CriticalChance": return ActionSkillRank(ActionSkillKind.Bomber,2,1)*.003f;
                case "Accuracy": return ActionSkillRank(ActionSkillKind.Medic,1,1)*.006f;
                default: return 0;
            }
        }
        public string ActionSkillNodeTitle(ActionSkillKind kind,int branch,int tier)
        {
            string[,,] en={{{"CALIBRATED FIRE","FIELD GUNNER","HARDENED BARREL","DEEP MAGAZINE","OVERDRIVE"},{"EXTENDED CELL","MOBILE PLATFORM","LONG WATCH","HEAVY FEED","FOLLOW MODE"},{"LONG SIGHT","FAST HANDS","AREA CONTROL","RAPID CYCLE","CROSS FIRE"}},{{"EXTRA PAYLOAD","BREACHER","RESERVE POUCH","SATURATION","FULL SALVO"},{"WIDE BURST","FRAGMENT FIELD","SHOCK WAVE","QUICK HANDS","WIDE DETONATION"},{"HEAVY CHARGE","WEAK POINT","COMPOUND","DEEP IMPACT","CLUSTER CHARGE"}},{{"FIRST AID","FIELD HEALTH","DEEP RECOVERY","SUSTAINED CARE","LIFE SURGE"},{"WIDE FIELD","STEADY AIM","COVERAGE","TRIAGE","SANCTUARY"},{"LONG SHIFT","PROTECTIVE CARE","SUPPORT LINE","LASTING FIELD","MOBILE CLINIC"}}};
            string[,,] ja={{{"照準調整","野戦射手","強化銃身","リロード支援","過給射撃"},{"予備電池","機動架台","長時間監視","給弾強化","追尾モード"},{"長距離照準","早業","制圧射界","高速循環","交差射撃"}},{{"追加弾薬","突入兵","予備ポーチ","集中投擲","一斉投擲"},{"広域爆破","破片領域","衝撃波","迅速動作","大爆発"},{"重装薬","弱点狙い","複合装薬","深部衝撃","連鎖装薬"}},{{"応急手当","野戦体力","集中回復","持続治療","生命の鼓動"},{"広域治療","精密照準","治療範囲","治療選別","安全地帯"},{"長時間活動","防護治療","支援線","持続領域","移動診療所"}}};
            return VaultLanguage.Japanese?ja[(int)kind,branch,tier]:en[(int)kind,branch,tier];
        }
        public string ActionSkillNodeDescription(ActionSkillKind kind,int branch,int tier)
        {
            string[,] en={{"Each rank raises turret damage by 2.17% of your weapon damage.","Each rank adds 0.25 sec to turret duration.","Each rank adds 0.25 m to range and reduces the firing interval by 0.01 sec."},{"Every 6 ranks in this branch adds one grenade.","Each rank adds approximately 0.06 m to blast radius.","Each rank raises blast damage by 5% of your weapon damage."},{"Each rank adds 0.1% of max HP per second to field healing.","Each rank adds 0.075 m to field radius.","Each rank adds 0.25 sec to field duration."}};
            string[,] ja={{"1ランクごとにタレットの威力が武器威力の約2.17%分上昇。","1ランクごとにタレットの持続時間が0.25秒延長。","1ランクごとに射程が0.25m延長し、射撃間隔が0.01秒短縮。"},{"この枝の合計6ランクごとにグレネードを1個追加。","1ランクごとに爆発範囲が約0.06m拡大。","1ランクごとに爆発威力が武器威力の5%分上昇。"},{"1ランクごとに回復量が最大HPの毎秒0.1%分上昇。","1ランクごとに回復範囲が0.075m拡大。","1ランクごとに回復時間が0.25秒延長。"}};
            string[,] finalEn={{"Turret damage gains another 55% of weapon damage.","Turret follows you and lasts 5 seconds longer.","Turret also fires at a second enemy."},{"Throw 3 additional grenades.","Increase blast radius by 1.25 m.","Increase blast damage by 300% of weapon damage and add a secondary hit."},{"Heal an additional 2.5% of maximum HP each second.","Increase the healing radius by 1.6 m.","The field follows you and lasts 5 seconds longer."}};
            string[,] finalJa={{"タレット威力が武器威力の55%分さらに上昇。","タレットが追尾し、持続時間が5秒延長。","タレットが2体目にも射撃。"},{"グレネードをさらに3個投擲。","爆発範囲が1.25m拡大。","爆発威力が武器威力の80%分上昇し、追加打撃。"},{"回復量が最大HPの毎秒2.5%分さらに上昇。","回復範囲が1.6m拡大。","回復エリアが追尾し、持続時間が5秒延長。"}};
            string text=tier==4?T(finalEn[(int)kind,branch],finalJa[(int)kind,branch]):T(en[(int)kind,branch],ja[(int)kind,branch]);
            if(kind==ActionSkillKind.Bomber&&branch==2)text=tier==4?T("Increase blast damage by 300% of weapon damage and add a secondary hit.","爆発威力が武器威力の300%分上昇し、追加打撃。"):T("Each rank raises blast damage by about 16.7% of weapon damage.","1ランクごとに爆発威力が武器威力の約16.7%分上昇。");
            if(tier==1){string[,] bonusEn={{"Assault rifle and heavy machine gun damage +1.2% per rank.","","Reload speed +1.2% per rank."},{"Shotgun damage +1.4% per rank.","","Critical chance +0.3% per rank."},{"Maximum HP +3 per rank.","Accuracy +0.6% per rank.","Damage reduction +1% per rank."}};string[,] bonusJa={{"アサルト・重機関銃の威力が1ランクごとに1.2%上昇。","","リロード速度が1ランクごとに1.2%上昇。"},{"ショットガン威力が1ランクごとに1.4%上昇。","","会心率が1ランクごとに0.3%上昇。"},{"最大HPが1ランクごとに3上昇。","命中精度が1ランクごとに0.6%上昇。","被ダメージ軽減が1ランクごとに1%上昇。"}};string bonus=T(bonusEn[(int)kind,branch],bonusJa[(int)kind,branch]);if(!string.IsNullOrEmpty(bonus))text+="\n"+bonus;}
            if(tier==3&&branch==1)text+="\n"+(kind==ActionSkillKind.Mechanic?T("Assault and heavy machine gun fire rate +1% per rank.","アサルト・重機関銃の連射速度が1ランクごとに1%上昇。"):kind==ActionSkillKind.Bomber?T("Shotgun fire rate +1% per rank.","ショットガンの連射速度が1ランクごとに1%上昇。"):T("Health regeneration +0.12 HP/s per rank.","HP自動回復が1ランクごとに毎秒0.12上昇。"));
            return text;
        }
    }
}
