using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        int missionKills;
        bool missionCompleted;
        readonly HashSet<string> completedMissionClaims = new HashSet<string>();
        public bool ExpeditionAllowsBypass
        {
            get
            {
                var stage=SelectedExpeditionStage;
                return stage!=null&&(stage.mission==ExpeditionMission.RecoverSupplies||
                    stage.mission==ExpeditionMission.SurveySectors||stage.mission==ExpeditionMission.SalvageSweep);
            }
        }
        // The existing checkpoint persists this one integer. Keep each counter in its own
        // byte so a resumed run retains both halves of a combined objective.
        int MissionKillCount => missionKills & 255;
        int MissionCacheCount => (missionKills >> 8) & 255;
        int MissionSurveyMask => (missionKills >> 16) & 15;
        public int MissionProgress
        {
            get
            {
                var stage=SelectedExpeditionStage;
                if(stage==null)return 0;
                switch(stage.mission)
                {
                    case ExpeditionMission.RecoverSupplies:
                    case ExpeditionMission.SalvageSweep: return MissionCacheCount;
                    case ExpeditionMission.SurveySectors: return SurveyedFloorCount;
                    case ExpeditionMission.DestroyNest: return Mathf.Min(8,MissionKillCount)+Mathf.Min(2,MissionCacheCount);
                    default: return MissionKillCount;
                }
            }
        }
        int SurveyedFloorCount
        {
            get {int mask=MissionSurveyMask,count=0;while(mask!=0){count+=mask&1;mask>>=1;}return count;}
        }
        // Collection and survey missions may move on after interacting with a
        // supply cache on this floor; a new floor never starts with its exit open.
        bool CurrentMissionFloorVisited => (MissionSurveyMask & (1 << Mathf.Clamp(room,0,3))) != 0;
        public bool MissionCompleted => missionCompleted;

        void ResetExpeditionMission(){missionKills=0;missionCompleted=false;}

        public void ExpeditionEnemyKilled(Foe foe)
        {
            if(SelectedExpeditionStage==null||missionCompleted||foe==null)return;
            ExpeditionStage stage=SelectedExpeditionStage;
            if(stage.mission==ExpeditionMission.HuntTarget)
            {
                if(foe.kind!=3)return;
                missionKills=1;CompleteExpeditionMission("BOUNTY COMPLETE / "+stage.Target,"賞金首討伐 / "+stage.Target);
            }
            else if(stage.mission==ExpeditionMission.ClearHostiles||stage.mission==ExpeditionMission.DestroyNest)
            {
                missionKills=(missionKills&~255)|Mathf.Min(255,MissionKillCount+1);
                CheckExpeditionMission();
            }
        }

        public void ExpeditionSupplyRecovered()
        {
            if(SelectedExpeditionStage==null||missionCompleted)return;
            var mission=SelectedExpeditionStage.mission;
            if(mission!=ExpeditionMission.RecoverSupplies&&mission!=ExpeditionMission.DestroyNest&&
               mission!=ExpeditionMission.SurveySectors&&mission!=ExpeditionMission.SalvageSweep)return;
            missionKills=(missionKills&~(255<<8))|(Mathf.Min(255,MissionCacheCount+1)<<8);
            missionKills|=1<<(16+Mathf.Clamp(room,0,3));
            CheckExpeditionMission();
        }

        void CheckExpeditionMission()
        {
            if(missionCompleted||SelectedExpeditionStage==null)return;
            switch(SelectedExpeditionStage.mission)
            {
                case ExpeditionMission.ClearHostiles:
                    if(MissionKillCount>=18)CompleteExpeditionMission("ZONE SECURED","区域制圧");break;
                case ExpeditionMission.RecoverSupplies:
                    if(MissionCacheCount>=3&&SurveyedFloorCount>=2)CompleteExpeditionMission("SUPPLIES RECOVERED","物資回収完了");break;
                case ExpeditionMission.DestroyNest:
                    if(MissionKillCount>=8&&MissionCacheCount>=2)CompleteExpeditionMission("INFESTATION DISRUPTED","感染網を遮断");break;
                case ExpeditionMission.SurveySectors:
                    if(SurveyedFloorCount>=2)CompleteExpeditionMission("SECTOR SURVEY COMPLETE","区域調査完了");break;
                case ExpeditionMission.SalvageSweep:
                    if(MissionCacheCount>=5&&SurveyedFloorCount>=3)CompleteExpeditionMission("SALVAGE COMPLETE","回収任務完了");break;
            }
        }

        void CompleteExpeditionMission(string english,string japanese)
        {
            missionCompleted=true;Notify(english,japanese);
        }

        public void ExpeditionRunCompleted(){CheckExpeditionMission();}

        int MissionRewardAmount(int stageId)
        {
            return ScaleScrap(20000000+new System.Random(ExpeditionMap.RewardSeed(stageId)).Next(200001)*100);
        }

        string MissionRewardDescription(int stageId)
        {
            int amount=MissionRewardAmount(stageId);
            return VaultLanguage.T("SCRAP +","スクラップ +")+amount.ToString("N0");
        }

        int TryClaimMissionReward()
        {
            if(!missionCompleted||SelectedExpeditionStage==null)return 0;
            string key="stage:"+SelectedStageId;if(completedMissionClaims.Contains(key))return 0;completedMissionClaims.Add(key);
            int amount=MissionRewardAmount(SelectedStageId);AwardCredits(amount);
            return amount;
        }
    }
}
