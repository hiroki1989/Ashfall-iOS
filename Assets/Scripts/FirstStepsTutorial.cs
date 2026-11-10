using PlayerPrefs = NeonVault.SaveSlotPrefs;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string FirstStepsKey = "Vault.FirstSteps.v1";
        const string ActionSkillTutorialKey = "Vault.ActionSkillTutorial.v1";
        const string LegendaryWeaponTutorialKey = "Vault.LegendaryWeaponTutorial.v1";
        bool actionSkillTutorialVisible, legendaryWeaponTutorialVisible;
        public void BeginLegendaryWeaponTutorial()
        {
            if(smoke || PlayerPrefs.GetInt(LegendaryWeaponTutorialKey,0)!=0)return;
            legendaryWeaponTutorialVisible=true;Time.timeScale=0;
        }
        void DrawLegendaryWeaponTutorial()
        {
            if(!legendaryWeaponTutorialVisible)return;
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.78f));Panel(new Rect(220,190,1000,520),Gold);
            Text(260,220,T("LEGENDARY WEAPON ACQUIRED","レジェンダリー武器を入手"),heading,Gold,920);
            Text(260,310,T("Legendary weapons are exceedingly rare and possess powerful unique effects.\nOpen the equipment screen [I], equip your new legendary weapon, and try it out in combat.","レジェンダリー武器は非常にレアで、強力な固有の効果を持っています。\n装備画面を開いて早速装備し、その威力を試してみましょう。"),new GUIStyle(label){fontSize=26,wordWrap=true},Color.white,900);
            if(WorldButton(new Rect(500,600,440,70),T("CONTINUE","続ける"))){legendaryWeaponTutorialVisible=false;PlayerPrefs.SetInt(LegendaryWeaponTutorialKey,1);PlayerPrefs.Save();Time.timeScale=1;}
        }
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
        void StartValeRematch()
        {
            if(DebtRemaining!=0||PlayerPrefs.GetInt(DebtBossDefeatedKey,0)==0)return;
            if(FreeVersionBlocksProgress){Notify("FREE VERSION / Unlock the full game to challenge Vale.","無料版 / ヴェイルへの再挑戦は完全版の購入で解放されます。");return;}
            finalDebtBossFight=true;SelectedStageId=-1;DungeonIndex=2;
            ResetExpeditionMission();StartRun();
        }
        int debtFinalBeat;
        bool DebtFinalConversationVisible => !FreeVersionBlocksProgress && !smoke && screen == Screen.Hub && WorldStorySeen && DebtRemaining == 0 && PlayerPrefs.GetInt(DebtBossDefeatedKey, 0) == 0;
        void DrawDebtFinalConversation()
        {
            string[] en={
                "One billion. Down to the last rusted bolt.",
                "So I heard. Briggs said his counting fingers cramped up.",
                "A deal's a deal, Vale. Unlock the collar.",
                "Honestly? I had twenty grand riding on you dying in the first week.",
                "Save the small talk. The key.",
                "Funny thing about keys, Knox. Once you turn 'em, your best dog runs off the porch.",
                "I paid for your missing cargo.",
                "Cargo? Come on. Look around you.",
                "That convoy was hauling empty crates. I didn't lose a damn thing—I just needed one man hungry enough to clear this city for me.",
                "...The ambush on the ridge. That was your crew.",
                "Best investment I ever made. Now the Red Claws and the Coyotes are bleeding out, and Ashfall is mine. Why would I let my top earner retire?",
                "Contract's finished. The only balance left is the one I'm taking out of your hide.",
                "Big talk with five pounds of C4 around your windpipe. Come try your luck.",
                "Keep your finger on that button, Vale. Let's see who's faster."
            };
            string[] ja={
                "10億だ。錆びたボルト一本まで、きっちり払ったぞ。",
                "聞いてるよ。ブリッグスが帳簿をつけながら腱鞘炎だとボヤいてた。",
                "契約通りだ、ヴェイル。首輪の鍵を回せ。",
                "正直に言おうか。お前が最初の一週間で野垂れ死ぬ方に、俺は賭けてたんだ。",
                "世間話はいい。鍵を出せ。",
                "鍵ってのは不思議なもんでな、ノックス。一度回すと、せっかく飼い慣らした最高の猟犬が逃げていく。",
                "消えた積み荷の代金は完済したはずだ。",
                "積み荷？ おいおい、まだそんなおとぎ話を信じてたのか。",
                "あのトラックの荷台は最初から空っぽだったんだよ。俺が欲しかったのは荷じゃない。この街の邪魔者を掃除してくれる、逃げられない働き手だ。",
                "……尾根で俺の車を襲ったのは、てめえの手下か。",
                "最高の投資だったよ。おかげでレッドクロウもコヨーテもボロボロ、街は俺のものだ。そんな稼ぎ頭を、みすみす手放す馬鹿がいるか？",
                "仕事はここまでだ。あとは、俺とてめえの個人的な精算だけが残ってる。",
                "首に爆薬を巻いたままよく吠える。やれるもんなら、その鍵を俺の懐から奪ってみろ。",
                "起爆スイッチから指を離すなよ、ヴェイル。どっちが早いか勝負だ。"
            };
            bool[] reid={true,false,true,false,true,false,true,false,false,true,false,true,false,true};
            debtFinalBeat=Mathf.Clamp(debtFinalBeat,0,en.Length-1);
            Box(new Rect(0,0,1440,900),new Color(.01f,.015f,.02f,.9f));
            bool knoxSpeaking=reid[debtFinalBeat];
            string speaker=knoxSpeaking?T("KNOX","ノックス"):T("MALCOLM VALE","マルコム・ヴェイル");
            DrawUnifiedDialogueScene("vale",knoxSpeaking,speaker,T(en[debtFinalBeat],ja[debtFinalBeat]),Gold,(debtFinalBeat+1)+" / "+en.Length);
            if(WorldButton(UnifiedDialoguePrimaryButton,debtFinalBeat==en.Length-1?T("FIGHT","戦う"):T("CONTINUE","続ける")))
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
            Notify("MALCOLM VALE IS DOWN / The debt is settled.","マルコム・ヴェイルを撃破 / 借りは返した。");
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
            if(FirstStepsStage==4||FirstStepsStage==6||FirstStepsStage==7){target=WorldGeography.Camps[LastVisitedCamp];return true;}
            if(FirstStepsStage==8&&OverworldWaypointKind==1){target=OverworldWaypointPosition;return true;}
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
            int target = 0;
            PlayerPrefs.SetInt(FirstStepsTargetKey, target);
            SetFirstStepsStage(1);
            if (target >= 0) SetOverworldWaypoint(2, target);
        }
        void FirstStepsDungeonCleared(){if(FirstStepsStage==3&&SelectedStageId==0)SetFirstStepsStage(4);}
        void FirstStepsEquipmentSold(){if(FirstStepsStage==4)SetFirstStepsStage(6);}
        void FirstStepsDebtPaid()
        {
            if(FirstStepsStage!=6)return;
            if(DebtLevel>=2)SetFirstStepsStage(7);
            else { SetFirstStepsStage(3);PlayerPrefs.SetInt(FirstStepsTargetKey,0);SetOverworldWaypoint(2,0); }
        }
        void FirstStepsSkillUpgraded(){if(smoke)return;if(FirstStepsStage==7){SetFirstStepsStage(8);PlayerPrefs.DeleteKey(FirstStepsTargetKey);int nearest=-1;float distance=float.PositiveInfinity;var camps=WorldGeography.Camps;var origin=camps[Mathf.Clamp(LastVisitedCamp,0,camps.Length-1)];for(int i=0;i<camps.Length;i++){if(i==LastVisitedCamp)continue;float d=(camps[i]-origin).sqrMagnitude;if(d<distance){distance=d;nearest=i;}}if(nearest>=0)SetOverworldWaypoint(1,nearest);Notify("FIRST STEPS COMPLETE / Visit the nearest camp and repay the billion","チュートリアル完了 / 近くの別の拠点へ向かい、10億スクラップの返済を目指そう");}}
        void DrawFirstStepsTutorial()
        {
            if (smoke || InventoryOpen || manufacturerRegistry || WorldBossConversationVisible || worldTransitionPending) return;
            int stage = FirstStepsStage;
            if (stage == 0 || stage == 5 || screen == Screen.Menu || screen == Screen.Dead) return;
            if (stage == 2 && IsOverworld && screen == Screen.Playing) { SetFirstStepsStage(3); stage = 3; }
            if ((stage == 4 || stage == 6) && DebtLevel >= 2) { SetFirstStepsStage(7); stage = 7; }
            if (stage == 1 && screen == Screen.Hub)
            {
                Box(new Rect(0,0,1440,900),new Color(0,0,0,.72f));
                Panel(new Rect(230,190,980,490),Gold);
                Text(270,225,T("YOUR SAFEHOUSE","拠点について"),heading,Gold,900);
                Text(270,310,T("Talk to the weapons merchant to buy, sell and expand storage.\nBriggs records debt payments and awards levels.\nOpen the daily chest once for free, then watch ads for up to 3 extra opens each day.\nUse the exit to explore the city.\nYour nearest level 1 dungeon has been marked on the map.","武器商人に話しかけると装備の売買や倉庫の拡張ができます。\nブリッグスに返済するとレベルが上がります。\n日次の箱は毎日1回無料で開けられ、広告視聴でさらに最大3回開けられます。\n出口から街へ出ましょう。\n最も近いレベル1のダンジョンを地図に示しました。"),new GUIStyle(label){fontSize=26,wordWrap=true},Color.white,900);
                if(WorldButton(new Rect(510,570,420,75),T("CONTINUE","続ける"))) SetFirstStepsStage(2);
                return;
            }
            if (AnyNpcOrStoryDialogueActive || DebtIntroVisible || actionSkillTutorialVisible || legendaryWeaponTutorialVisible || screen == Screen.Pause) return;
            string en = stage == 2 ? "Leave the safehouse through the exit." : stage == 3 ? (screen == Screen.Hub ? "Leave the safehouse and return to dungeon 01." : IsOverworld ? "Follow the marker and clear dungeon 01." : "Complete the mission and leave the dungeon.") : stage == 4 ? "Return to a safehouse and sell spare equipment to Sera." : stage == 6 ? "Talk to Briggs and repay your debt." : stage == 8 ? "Follow the marker to the nearest other camp." : "Talk to Briggs and spend a skill point to improve an action skill.";
            string ja = stage == 2 ? "出口を調べて街へ出よう。" : stage == 3 ? (screen == Screen.Hub ? "街へ出て、01のダンジョンに再挑戦しよう。" : IsOverworld ? "目的地へ向かい、01のダンジョンを攻略しよう。" : "ミッションを達成してダンジョンから脱出しよう。") : stage == 4 ? "拠点に戻り、セラに不要な装備を売ろう。" : stage == 6 ? "ブリッグスに話しかけ、借金を返済しよう。" : stage == 8 ? "目的地をたどり、最も近い別の拠点へ向かおう。" : "ブリッグスに話しかけ、スキルポイントでアクションスキルを強化しよう。";
            if (screen == Screen.Hub || screen == Screen.Playing)
            {
                bool mobile = MobileTouchControls.Enabled;
                bool leftHanded = mobile && MobileTouchControls.IsLeftHanded;
                float bx, by, bw;
                if (screen == Screen.Hub)
                {
                    bw = 428f;
                    bx = 1440f - 16f - bw;
                    by = !mobile ? 16f : leftHanded ? 114f : 158f;
                }
                else if (!IsOverworld)
                {
                    if (mobile && !leftHanded) { bw = 385f; bx = 700f; by = 16f; }
                    else { bw = 420f; bx = 996f; by = 8f; }
                }
                else
                {
                    if (!mobile) { bw = 262f; bx = 1158f; by = 96f; }
                    else if (leftHanded) { bw = 380f; bx = 1040f; by = 18f; }
                    else if (!OverworldRushActive) { bw = 386f; bx = 630f; by = 90f; }
                    else { bw = 276f; bx = 1140f; by = 354f; }
                }
                int fontSize = bw < 320f ? 14 : 16;
                float bodyOffset = bw < 320f ? 19f : 23f;
                float maxBh = (!mobile && screen == Screen.Playing && !IsOverworld) ? 68f : (!mobile && IsOverworld) ? 65f : 92f;
                var bodyStyle = new GUIStyle(small) { fontSize = fontSize, wordWrap = true };
                float textH = bodyStyle.CalcHeight(new GUIContent(T(en, ja)), bw - 24f);
                while (fontSize > 11 && bodyOffset + textH + 5f > maxBh)
                {
                    fontSize--;
                    bodyStyle.fontSize = fontSize;
                    textH = bodyStyle.CalcHeight(new GUIContent(T(en, ja)), bw - 24f);
                }
                float bh = Mathf.Max(54f, bodyOffset + textH + 5f);
                Box(new Rect(bx, by, bw, bh), new Color(.02f, .03f, .04f, .92f));
                Box(new Rect(bx, by, 3f, bh), Gold);
                Text(bx + 12f, by + 3f, T("FIRST STEPS", "最初の一歩"), tiny, Gold, bw - 20f);
                Text(bx + 12f, by + bodyOffset, T(en, ja), bodyStyle, Color.white, bw - 24f);
            }
        }
    }
}
