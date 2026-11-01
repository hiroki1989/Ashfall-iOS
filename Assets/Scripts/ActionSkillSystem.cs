using System;
using UnityEngine;

namespace NeonVault
{
    public enum ActionSkillKind { Mechanic, Bomber, Medic }

    public partial class VaultGame
    {
        const string ActionSkillSaveKey = "Vault.ActionSkills.v1";
        const int ActionSkillSlots = 45;
        [Serializable] sealed class ActionSkillSave { public int version=1,selected; public int[] ranks,extraRanks; }
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
                if(data.extraRanks!=null)for(int i=0;i<Mathf.Min(data.extraRanks.Length,actionSkillExtraRanks.Length);i++)actionSkillExtraRanks[i]=Mathf.Clamp(data.extraRanks[i],0,1);
            }
            catch(Exception e){Debug.LogWarning("Action skill save load: "+e.GetType().Name);}
        }
        void SaveActionSkills()
        {
            if(smoke)return;
            PlayerPrefs.SetString(ActionSkillSaveKey,JsonUtility.ToJson(new ActionSkillSave{selected=(int)actionSkillSelected,ranks=actionSkillRanks,extraRanks=actionSkillExtraRanks}));PlayerPrefs.Save();
        }
        public int ActionSkillRank(ActionSkillKind kind,int branch,int tier)
        {LoadActionSkills();return ValidActionSkill(kind,branch,tier)?actionSkillRanks[ActionSkillIndex(kind,branch,tier)]:0;}
        public int ActionSkillSpentPoints { get { LoadActionSkills();int total=0;foreach(int rank in actionSkillRanks)total+=rank;foreach(int rank in actionSkillExtraRanks)total+=rank;return total; } }
        // The expanded tree contains 405 ranks.  Spread 96 additional points
        // over the existing 122 level-ups so the full set of level, mission
        // and collector rewards can reach approximately 80% of the tree.
        // This is computed rather than saved, so existing characters retain
        // all spent ranks and receive the same progression retroactively.
        public int ActionSkillLevelBonusPoints => Mathf.Clamp((DebtLevel-1)*96/122,0,96);
        public int ActionSkillAvailablePoints => Mathf.Max(0,DebtLevel-1+ActionSkillLevelBonusPoints+ExpeditionTokens-ActionSkillSpentPoints);
        public bool TryUpgradeActionSkill(ActionSkillKind kind,int branch,int tier)
        {
            LoadActionSkills();if(!ValidActionSkill(kind,branch,tier)||ActionSkillAvailablePoints<=0)return false;
            int index=ActionSkillIndex(kind,branch,tier);if(actionSkillRanks[index]>=ActionSkillMaxRank(tier))return false;
            if(!ActionSkillTierAvailable(kind,branch,tier))return false;
            actionSkillRanks[index]++;SaveActionSkills();RecalculateArmor();FirstStepsSkillUpgraded();PlayUserSound("skill",.35f);return true;
        }
        public void SelectActionSkill(ActionSkillKind kind)
        {
            LoadActionSkills();if((int)kind<0||(int)kind>2||kind==actionSkillSelected)return;
            ClearActiveActionSkill();actionSkillSelected=kind;ActionSkillPoints=0;SaveActionSkills();
        }
        public void ResetActionSkills()
        {LoadActionSkills();Array.Clear(actionSkillRanks,0,actionSkillRanks.Length);Array.Clear(actionSkillExtraRanks,0,actionSkillExtraRanks.Length);actionSkillExtraValuesDirty=true;ActionSkillPoints=0;ClearActiveActionSkill();SaveActionSkills();RecalculateArmor();}

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
            if(kind==ActionSkillKind.Mechanic&&branch==0){en[0,0]="Each rank raises turret damage by about 1.17% of your weapon damage.";ja[0,0]="1ランクごとにタレット威力が武器威力の約1.17%分上昇。";finalEn[0,0]="Turret damage gains another 12% of weapon damage.";finalJa[0,0]="タレット威力が武器威力の12%分さらに上昇。";}
            if(kind==ActionSkillKind.Mechanic&&branch==2){en[0,2]="Each rank adds 0.25 m to range and increases firing speed by 1%.";ja[0,2]="1ランクごとに射程が0.25m、連射速度が1%上昇。";}
            string text=tier==4?T(finalEn[(int)kind,branch],finalJa[(int)kind,branch]):T(en[(int)kind,branch],ja[(int)kind,branch]);
            if(kind==ActionSkillKind.Bomber&&branch==2)text=tier==4?T("Add 3/7.7 of weapon DPS to blast damage and a secondary hit.","爆発威力に武器DPSの3/7.7を加え、追加打撃。"):T("Each rank adds approximately 1/46.2 of weapon DPS to blast damage.","1ランクごとに爆発威力へ武器DPSの約1/46.2を加算。");
            if(kind==ActionSkillKind.Bomber&&branch==0)text+="\n"+T("Repeated blasts against one enemy deal diminishing damage.","同じ敵への連続爆発は徐々に威力が減少。");
            if(kind==ActionSkillKind.Bomber&&branch==1)text+="\n"+T("Blast radius is capped at 7 m.","爆発範囲の上限は7m。");
            if(kind==ActionSkillKind.Mechanic)text+="\n"+T("Turret limits: 95% weapon damage, 135% fire rate, 22 sec.","上限：武器威力の95%、連射速度の135%、持続22秒。");
            if(kind==ActionSkillKind.Medic&&(branch==0||branch==2))text+="\n"+T("Healing is capped at 9% HP/s and duration at 24 s.","回復量は毎秒最大HPの9%、持続は24秒が上限。");
            if(tier==2&&kind==ActionSkillKind.Mechanic&&branch==1)text+="\n"+T("At rank 3, the turret follows you.","ランク3でタレットが追尾する。");
            if(tier==2&&kind==ActionSkillKind.Medic&&branch==2)text+="\n"+T("At rank 3, the healing field follows you.","ランク3で回復エリアが追尾する。");
            if(tier==1){string[,] bonusEn={{"Assault rifle and heavy machine gun damage +1.2% per rank.","","Reload speed +1.2% per rank."},{"Shotgun damage +1.4% per rank.","","Critical chance +0.3% per rank."},{"Maximum HP +3 per rank.","Accuracy +0.6% per rank.","Damage reduction +1% per rank."}};string[,] bonusJa={{"アサルト・重機関銃の威力が1ランクごとに1.2%上昇。","","リロード速度が1ランクごとに1.2%上昇。"},{"ショットガン威力が1ランクごとに1.4%上昇。","","会心率が1ランクごとに0.3%上昇。"},{"最大HPが1ランクごとに3上昇。","命中精度が1ランクごとに0.6%上昇。","被ダメージ軽減が1ランクごとに1%上昇。"}};string bonus=T(bonusEn[(int)kind,branch],bonusJa[(int)kind,branch]);if(!string.IsNullOrEmpty(bonus))text+="\n"+bonus;}
            if(tier==3&&branch==1)text+="\n"+(kind==ActionSkillKind.Mechanic?T("Assault and heavy machine gun fire rate +1% per rank.","アサルト・重機関銃の連射速度が1ランクごとに1%上昇。"):kind==ActionSkillKind.Bomber?T("Shotgun fire rate +1% per rank.","ショットガンの連射速度が1ランクごとに1%上昇。"):T("Health regeneration +0.12 HP/s per rank.","HP自動回復が1ランクごとに毎秒0.12上昇。"));
            return text;
        }
    }
}
