using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        int missionKills;
        bool missionCompleted;
        readonly HashSet<string> completedMissionClaims = new HashSet<string>();
        public int MissionProgress => missionKills;
        public bool MissionCompleted => missionCompleted;

        void ResetExpeditionMission(){missionKills=0;missionCompleted=false;}

        public void ExpeditionEnemyKilled(Foe foe)
        {
            if(SelectedExpeditionStage==null||missionCompleted||foe==null)return;
            ExpeditionStage stage=SelectedExpeditionStage;
            if(stage.mission==ExpeditionMission.HuntTarget){if(foe.kind!=3)return;missionKills=1;missionCompleted=true;Notify("BOUNTY COMPLETE / "+stage.Target,"賞金首討伐 / "+stage.Target);}
            else if(stage.mission==ExpeditionMission.ClearHostiles){missionKills++;}
            else if(stage.mission==ExpeditionMission.DestroyNest&&foe.kind==9){missionKills++;missionCompleted=true;Notify("INFESTATION DESTROYED","巣を破壊");}
        }

        public void ExpeditionSupplyRecovered(){if(SelectedExpeditionStage==null||SelectedExpeditionStage.mission!=ExpeditionMission.RecoverSupplies||missionCompleted)return;missionKills=1;missionCompleted=true;Notify("SUPPLY CACHE RECOVERED","補給物資を回収");}
        public void ExpeditionRunCompleted(){if(SelectedExpeditionStage==null||missionCompleted)return;if(SelectedExpeditionStage.mission==ExpeditionMission.ClearHostiles){missionCompleted=true;missionKills=kills;Notify("ZONE PURGED","区域制圧");}}

        bool TryClaimMissionReward()
        {
            if(!missionCompleted||SelectedExpeditionStage==null)return false;
            string key="stage:"+SelectedStageId;if(completedMissionClaims.Contains(key))return false;completedMissionClaims.Add(key);
            int amount=ScaleScrap(200000+new System.Random(ExpeditionMap.RewardSeed(SelectedStageId)).Next(200001));AwardCredits(amount);
            Notify("MISSION REWARD / SCRAP +"+amount,"任務報酬 / スクラップ +"+amount);return true;
        }
    }
}
