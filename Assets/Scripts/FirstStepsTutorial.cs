using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string FirstStepsKey = "Vault.FirstSteps.v1";
        const string ActionSkillTutorialKey = "Vault.ActionSkillTutorial.v1";
        bool actionSkillTutorialVisible;
        void BeginActionSkillTutorial()
        {
            if(smoke || PlayerPrefs.GetInt(ActionSkillTutorialKey,0)!=0)return;
            actionSkillTutorialVisible=true;Time.timeScale=0;
        }
        void DrawActionSkillTutorial()
        {
            if(!actionSkillTutorialVisible)return;
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.78f));Panel(new Rect(220,190,1000,520),Gold);
            Text(260,220,T("ACTION SKILL READY","アクションスキル発動可能"),heading,Gold,920);
            Text(260,310,T("Hit enemies to charge your action skill. Activate it when the gauge is full.\nSwitch skills in the inventory, and spend skill points earned from leveling up to improve them.","敵に弾を当てるとポイントがたまり、満タンになるとアクションスキルを使えます。\nスキルは装備画面で切り替えられます。\nレベルアップで得たスキルポイントを使うと強化できます。"),new GUIStyle(label){fontSize=26,wordWrap=true},Color.white,900);
            if(WorldButton(new Rect(500,600,440,70),T("CONTINUE","続ける"))){actionSkillTutorialVisible=false;PlayerPrefs.SetInt(ActionSkillTutorialKey,1);PlayerPrefs.Save();Time.timeScale=1;}
        }
        const string FirstStepsTargetKey = "Vault.FirstSteps.target.v1";
        const string DebtBossDefeatedKey = "Vault.DebtBoss.defeated.v1";
        bool finalDebtBossFight;
        int debtFinalBeat;
        bool DebtFinalConversationVisible => !smoke && screen == Screen.Hub && WorldStorySeen && DebtRemaining == 0 && PlayerPrefs.GetInt(DebtBossDefeatedKey, 0) == 0;
        void DrawDebtFinalConversation()
        {
            string[] en={"A billion. Every last scrap.","I know. Briggs keeps a meticulous ledger.","We had a deal. Take it off.","I never thought you'd make it. I bet you'd die in the first week.","Did you hear me? Take the collar off.","I heard you. I just can't see a reason to do it.","I paid for the cargo.","The cargo? Oh, that.","It was never stolen. I needed a worker who couldn't walk away. That's all.","...You set me up?","You exceeded expectations. Shame to waste that. I have another job for you.","The work is over. The only debt left is between us.","Is that so? Come and collect it.","Let's see what a billion bought me."};
            string[] ja={"10億。最後の一枚まで払った。","知ってる。ブリッグスの帳簿は几帳面だ。","約束だ。外せ。","お前が本当にやり遂げるとはな。最初の一週間で死ぬ方に賭けてた。","聞こえなかったか。首輪を外せ。","聞こえたよ。ただ、外す理由が見当たらない。","積み荷の代金は払った。","積み荷？ ああ、あれか。","盗まれちゃいない。最初からな。逃げられない働き手が欲しかった。それだけだ。","……俺をはめたのは、お前か。","お前は期待以上に働いた。惜しいよ。だから次の仕事も用意した。","仕事は終わりだ。残ってるのは、お前との借りだけだ。","そうか。なら取り立てに来い。","10億で何を買えたのか、見せてもらおう。"};
            bool[] reid={true,false,true,false,true,false,true,false,false,true,false,true,false,false};
            debtFinalBeat=Mathf.Clamp(debtFinalBeat,0,en.Length-1);
            Box(new Rect(0,0,1440,900),new Color(.01f,.015f,.02f,.9f));
            Panel(new Rect(100,470,1240,315),Gold);
            Text(135,500,reid[debtFinalBeat]?T("REID","レイド"):T("MALCOLM VALE","マルコム・ヴェイル"),heading,Gold,1100);
            Text(135,570,T(en[debtFinalBeat],ja[debtFinalBeat]),new GUIStyle(label){fontSize=34,wordWrap=true},Color.white,1100);
            if(WorldButton(new Rect(920,700,370,65),debtFinalBeat==en.Length-1?T("FIGHT","戦う"):T("CONTINUE","続ける")))
            {
                if(++debtFinalBeat>=en.Length)
                {
                    debtFinalBeat=0; finalDebtBossFight=true; SelectedStageId=-1; DungeonIndex=2;
                    ResetExpeditionMission(); StartRun();
                }
            }
        }
        void CompleteDebtBossFight()
        {
            finalDebtBossFight=false;
            PlayerPrefs.SetInt(DebtBossDefeatedKey,1);PlayerPrefs.Save();
            ClearRunCheckpoint();SaveInventory();screen=Screen.Victory;
            Notify("THE MALCOLM VALE IS DOWN / The debt is settled.","マルコム・ヴェイルを撃破 / 借りは返した。");
        }
        int FirstStepsStage => PlayerPrefs.GetInt(FirstStepsKey, 0);
        bool TryFirstStepsWaypoint(out Vector3 target)
        {
            target=Vector3.zero;
            if(smoke || !IsOverworld || screen!=Screen.Playing)return false;
            if(FirstStepsStage==3)
            {
                int id=PlayerPrefs.GetInt(FirstStepsTargetKey,-1);
                if(id>=0&&id<ExpeditionMap.StageCount){target=WorldGeography.Entrances[id];return true;}
            }
            if(FirstStepsStage==4){target=WorldGeography.Camps[LastVisitedCamp];return true;}
            return false;
        }
        void SetFirstStepsStage(int stage)
        {
            if (smoke) return;
            PlayerPrefs.SetInt(FirstStepsKey, stage);
            PlayerPrefs.Save();
        }
        void BeginFirstStepsTutorial()
        {
            if (smoke || FirstStepsStage != 0) return;
            int target = -1; float closest = float.MaxValue;
            Vector3 camp = WorldGeography.Camps[LastVisitedCamp];
            for (int i = 0; i < ExpeditionMap.StageCount; i++)
            {
                if (ExpeditionMap.Get(i, 1).level != 1) continue;
                float distance = (WorldGeography.Entrances[i] - camp).sqrMagnitude;
                if (distance < closest) { closest = distance; target = i; }
            }
            PlayerPrefs.SetInt(FirstStepsTargetKey, target);
            SetFirstStepsStage(1);
            if (target >= 0) SetOverworldWaypoint(2, target);
        }
        void DrawFirstStepsTutorial()
        {
            if (smoke || InventoryOpen || manufacturerRegistry || WorldBossConversationVisible || worldTransitionPending) return;
            int stage = FirstStepsStage;
            if (stage == 0 || stage >= 5 || screen == Screen.Menu || screen == Screen.Dead) return;
            if (stage == 2 && IsOverworld && screen == Screen.Playing) { SetFirstStepsStage(3); stage = 3; }
            if (stage == 3 && OverworldMissionClears > 0) { SetFirstStepsStage(4); stage = 4; }
            if (stage == 4 && DebtLevel >= 2) { SetFirstStepsStage(5); Notify("FIRST STEPS COMPLETE / Repay the billion", "チュートリアル完了 / 10億スクラップの返済を目指そう"); return; }
            if (stage == 1 && screen == Screen.Hub)
            {
                Box(new Rect(0,0,1440,900),new Color(0,0,0,.72f));
                Panel(new Rect(230,190,980,490),Gold);
                Text(270,225,T("YOUR SAFEHOUSE","拠点について"),heading,Gold,900);
                Text(270,310,T("Talk to the weapons merchant to buy, sell and expand storage.\nBriggs records debt payments and awards levels.\nUse the exit to explore the city.\nYour nearest level 1 dungeon has been marked on the map.","武器商人に話しかけると装備の売買や倉庫の拡張ができます。\nブリッグスに返済するとレベルが上がります。\n出口から街へ出ましょう。\n最も近いレベル1のダンジョンを地図に示しました。"),new GUIStyle(label){fontSize=26,wordWrap=true},Color.white,900);
                if(WorldButton(new Rect(510,570,420,75),T("CONTINUE","続ける"))) SetFirstStepsStage(2);
                return;
            }
            string en = stage == 2 ? "Leave the safehouse through the exit." : stage == 3 ? (IsOverworld ? "Follow the marked destination and clear the level 1 dungeon." : "Complete the mission and leave the dungeon.") : "Return to a safehouse. Sell spare gear, then pay Briggs until you reach level 2.";
            string ja = stage == 2 ? "出口を調べて街へ出よう。" : stage == 3 ? (IsOverworld ? "地図の目的地へ向かい、レベル1のダンジョンを攻略しよう。" : "ミッションを達成してダンジョンから脱出しよう。") : "拠点に戻り、不要な装備を売ってブリッグスに返済し、レベル2を目指そう。";
            if (screen == Screen.Hub || screen == Screen.Playing)
            {
                Box(new Rect(410,76,620,82),new Color(.02f,.03f,.04f,.88f));
                Text(430,83,T("FIRST STEPS","最初の一歩"),small,Gold,580);
                Text(430,109,T(en,ja),new GUIStyle(small){fontSize=20,wordWrap=true},Color.white,580);
            }
        }
    }
}
