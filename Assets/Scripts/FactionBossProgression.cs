using PlayerPrefs = NeonVault.SaveSlotPrefs;
using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public const int RedClawBoss30=200, RedClawBoss60=201, RedClawHeadquarters=202;
        public const int CoyoteBoss30=203, CoyoteBoss60=204, CoyoteHeadquarters=205;
        public const int InfectedBoss30=206, InfectedBoss60=207, InfectedHeadquarters=208;
        bool factionBossConversation; int factionBossConversationBeat;
        public static bool IsFactionHeadquarters(int stage)=>stage>=RedClawBoss30&&stage<ExpeditionMap.StageCount;
        public static bool IsInfectedBossStage(int stage)=>stage>=InfectedBoss30&&stage<=InfectedHeadquarters;
        public static bool IsRedClawBossStage(int stage)=>stage>=RedClawBoss30&&stage<=RedClawHeadquarters;
        public static bool IsCoyoteBossStage(int stage)=>stage>=CoyoteBoss30&&stage<=CoyoteHeadquarters;
        public static int FactionBossTier(int stage)=>!IsFactionHeadquarters(stage)?-1:(stage-RedClawBoss30)%3;
        public static bool IsFactionBossEscapeStage(int stage)=>IsFactionHeadquarters(stage)&&FactionBossTier(stage)<2;
        public static int RequiredFactionClearPercent(int stage){int tier=FactionBossTier(stage);return tier==0?30:tier==1?60:tier==2?100:0;}
        public static int FactionRegularDungeonCount(DungeonFaction faction)
        {
            int count=0;
            for(int id=0;id<ExpeditionMap.RegularStageCount;id++)if(ExpeditionMap.FactionForStage(id)==faction)count++;
            return count;
        }
        public int ClearedFactionDungeonCount(DungeonFaction faction)
        {
            int count=0;
            for(int i=0;i<clearedExpeditionStages.Count;i++)
            {
                int id=clearedExpeditionStages[i];
                if(id>=0&&id<ExpeditionMap.RegularStageCount&&ExpeditionMap.FactionForStage(id)==faction)count++;
            }
            return count;
        }
        public int FactionClearPercent(DungeonFaction faction)
        {
            int total=FactionRegularDungeonCount(faction);
            return total<=0?0:Mathf.FloorToInt(ClearedFactionDungeonCount(faction)*100f/total);
        }
        public static int RequiredFactionDungeonClears(int stage)
        {
            if(!IsFactionHeadquarters(stage))return 0;
            var faction=ExpeditionMap.FactionForStage(stage);
            int total=FactionRegularDungeonCount(faction);
            int pct=RequiredFactionClearPercent(stage);
            return Mathf.CeilToInt(total*(pct/100f));
        }
        public bool FactionHeadquartersUnlocked(int stage)=>!IsFactionHeadquarters(stage)||ClearedFactionDungeonCount(ExpeditionMap.FactionForStage(stage))>=RequiredFactionDungeonClears(stage);
        public bool FactionBossConversationVisible=>factionBossConversation;
        readonly System.Collections.Generic.Queue<int> factionUnlockDialogs = new System.Collections.Generic.Queue<int>();
        public bool FactionUnlockDialogVisible => factionUnlockDialogs.Count > 0;
        void DrawFactionUnlockDialog()
        {
            int stage = factionUnlockDialogs.Peek();
            int tier = FactionBossTier(stage);
            string faction = IsInfectedBossStage(stage) ? T("INFECTED", "感染者") : IsRedClawBossStage(stage) ? T("RED CLAW", "\u30ec\u30c3\u30c9\u30af\u30ed\u30a6") : T("COYOTE", "\u30b3\u30e8\u30fc\u30c6");
            string location = tier == 0 ? T("OUTPOST", "\u524d\u7dda\u57fa\u5730") : tier == 1 ? T("WAR CAMP", "\u8ecd\u4e8b\u62e0\u70b9") : T("STRONGHOLD", "\u672c\u62e0\u5730");
            Box(new Rect(0, 0, 1440, 900), new Color(0, 0, 0, .76f));
            Panel(new Rect(300, 250, 840, 400), Gold);
            WorldLabel(new Rect(350, 285, 740, 52), T("FACTION BOSS UNLOCKED", "\u52e2\u529b\u30dc\u30b9\u62e0\u70b9\u3092\u89e3\u653e"), Gold, 32);
            WorldLabel(new Rect(350, 360, 740, 60), faction + " / " + location, UiPaper, 30);
            WorldLabel(new Rect(350, 445, 740, 100), T("The entrance is now your destination. Follow the map marker.", "\u5165\u53e3\u3092\u76ee\u7684\u5730\u306b\u8a2d\u5b9a\u3057\u307e\u3057\u305f\u3002\u5730\u56f3\u306e\u30de\u30fc\u30ab\u30fc\u3092\u76ee\u6307\u3057\u307e\u3057\u3087\u3046\u3002"), Mist, 24);
            if (WorldButton(new Rect(860, 555, 230, 60), T("OK", "\u78ba\u8a8d"))) factionUnlockDialogs.Dequeue();
        }
        void UpdateFactionBossUnlocks(int paidBefore) { }
        void UpdateFactionBossUnlocks(int raidersBefore,int gangBefore,int infectedBefore)
        {
            int raidersNow=ClearedFactionDungeonCount(DungeonFaction.Raiders);
            int gangNow=ClearedFactionDungeonCount(DungeonFaction.Gang);
            int infectedNow=ClearedFactionDungeonCount(DungeonFaction.None);
            int[] redStages={RedClawBoss30,RedClawBoss60,RedClawHeadquarters};
            string[] redNamesEn={"RED CLAW OUTPOST (30%)","RED CLAW WAR CAMP (60%)","RED CLAW STRONGHOLD (100%)"};
            string[] redNamesJa={"レッドクロウ前線基地（制圧30%）","レッドクロウ軍事拠点（制圧60%）","レッドクロウ本拠地（制圧100%）"};
            for(int i=0;i<3;i++)
            {
                int req=RequiredFactionDungeonClears(redStages[i]);
                if(raidersBefore<req&&raidersNow>=req)
                {
                    factionUnlockDialogs.Enqueue(redStages[i]);
                    DiscoverOverworldDungeon(redStages[i]);
                    SetOverworldWaypoint(2,redStages[i]);
                    Notify(redNamesEn[i]+" UNLOCKED / Boss battle available",redNamesJa[i]+" 解放 / ボス戦ダンジョンに出撃可能");
                }
            }
            int[] infectedStages={InfectedBoss30,InfectedBoss60,InfectedHeadquarters};
            for(int i=0;i<3;i++)
            {
                int req=RequiredFactionDungeonClears(infectedStages[i]);
                if(infectedStages[i]<ExpeditionMap.StageCount&&infectedBefore<req&&infectedNow>=req)
                {
                    factionUnlockDialogs.Enqueue(infectedStages[i]);
                    DiscoverOverworldDungeon(infectedStages[i]);
                    SetOverworldWaypoint(2,infectedStages[i]);
                    Notify("INFECTED NEST UNLOCKED / Boss battle available","感染者の巣を解放 / ボス戦に出撃可能");
                }
            }
            int[] gangStages={CoyoteBoss30,CoyoteBoss60,CoyoteHeadquarters};
            string[] gangNamesEn={"COYOTE OUTPOST (30%)","COYOTE WAR DEN (60%)","COYOTE STRONGHOLD (100%)"};
            string[] gangNamesJa={"コヨーテ前線基地（制圧30%）","コヨーテ軍事拠点（制圧60%）","コヨーテ本拠地（制圧100%）"};
            for(int i=0;i<3;i++)
            {
                int req=RequiredFactionDungeonClears(gangStages[i]);
                if(gangBefore<req&&gangNow>=req)
                {
                    factionUnlockDialogs.Enqueue(gangStages[i]);
                    DiscoverOverworldDungeon(gangStages[i]);
                    SetOverworldWaypoint(2,gangStages[i]);
                    Notify(gangNamesEn[i]+" UNLOCKED / Boss battle available",gangNamesJa[i]+" 解放 / ボス戦ダンジョンに出撃可能");
                }
            }
        }
        public string FactionBossUnlockRequirementLabel(int stage,bool japanese)
        {
            if(!IsFactionHeadquarters(stage))return "";
            var faction=ExpeditionMap.FactionForStage(stage);
            int pct=RequiredFactionClearPercent(stage);
            int curPct=FactionClearPercent(faction);
            string facEn=faction==DungeonFaction.None?"INFECTED":faction==DungeonFaction.Raiders?"RED CLAW":"COYOTE";
            string facJa=faction==DungeonFaction.None?"感染者":faction==DungeonFaction.Raiders?"レッドクロウ":"コヨーテ";
            return japanese?facJa+"拠点 "+pct+"%制圧で解放（現在 "+curPct+"%）":"REQUIRES "+pct+"% "+facEn+" CLEAR (NOW "+curPct+"%)";
        }
        public void BeginFactionBossConversation()
        {
            if(!IsFactionHeadquarters(SelectedStageId)||PlayerPrefs.GetInt("Vault.FactionBoss.Intro."+SelectedStageId,0)!=0)return;
            factionBossConversation=true;factionBossConversationBeat=0;Time.timeScale=0;
        }
        public void DrawFactionBossConversation()
        {
            if(IsInfectedBossStage(SelectedStageId))
            {
                Box(new Rect(0,0,1440,900),new Color(0,0,0,.8f));
                DrawUnifiedDialogueScene("renegade",false,T("THE RENEGADE","レネゲード"),"・・・",Gold);
                if(WorldButton(UnifiedDialoguePrimaryButton,T("FIGHT","戦う"))){factionBossConversation=false;Time.timeScale=1;PlayerPrefs.SetInt("Vault.FactionBoss.Intro."+SelectedStageId,1);PlayerPrefs.Save();}
                return;
            }
            bool red=IsRedClawBossStage(SelectedStageId);
            int tier=FactionBossTier(SelectedStageId);
            bool knoxSpeaking=factionBossConversationBeat==1;
            string speaker=knoxSpeaking?T("KNOX","ノックス"):red?T("RAVEN","レイヴン"):T("VEX","ヴェックス");
            string[] ja=red?(tier==0?new[]{
                    "臭いで分かったぜ。ヴェイルが新しく飼い始めた首輪付きの猟犬ってのは、お前だな。",
                    "お前たちの縄張りに恨みはない。ただ、あんたの首にいい値がついてる。",
                    "ハッ、3割ばかり前線を荒らした程度で調子に乗るなよ。その首輪ごと頭蓋骨を砕いてやる！"
                }
                :tier==1?new[]{
                    "またお前か、ノックス。俺たちのシマを半分以上も灰にしやがって……いい度胸だ。",
                    "ヴェイルへの返済がまだ残ってる。悪いが、ここも更地にさせてもらう。",
                    "ふざけるなよ、薄汚い犬が！ 本拠地の門は絶対に踏ませねえ……ここで挽き肉にしてやる！"
                }
                :new[]{
                    "……とうとう本丸まで来やがったか。ヴェイルの鎖につながれたまま、よくそこまで吠えられるもんだ。",
                    "今日でレッドクロウは終わりだ。あの男に払う手切れ金の足しになってもらう。",
                    "抜かせ！ 俺たちが血を流して築いた赤い街だ。貴様の骨も残さず溶鉱炉に叩き込んでやる！"
                })
                :(tier==0?new[]{
                    "足音が重すぎるよ、よそ者。コヨーテの狩場を嗅ぎ回ってる首輪付きっていうのは、あんたかい。",
                    "道を開けろ。俺が用があるのは、ここの金庫と物資だけだ。",
                    "威勢がいいじゃないか。だがコヨーテの縄張りに入った獲物は、骨までしゃぶり尽くすのがうちの流儀でね！"
                }
                :tier==1?new[]{
                    "しぶとい野郎だね。うちの拠点を6割も潰してくれた礼は、たっぷり鉛で返させてもらうよ。",
                    "恨むならヴェイルを恨め。俺はただ、首の爆弾を外すために仕事をしてるだけだ。",
                    "他人の都合なんか知るもんかい！ これ以上うちの家族に手を出させはしない……ここで死にな！"
                }
                :new[]{
                    "……最後の隠れ家まで踏み込んでくるとはね。あんた、ヴェイルの駒で終わるには惜しい目をしてるよ。",
                    "駒で終わるつもりはない。ここを片付けたら、次はあいつの番だ。",
                    "気に入ったよ、ノックス。もし私を倒して生き残れたら、ヴェイルの喉笛も噛み千切ってやりな！"
                });
            string[] en=red?(tier==0?new[]{
                    "Smelled the gunpowder a mile off. So you're the new collared hound Vale let off the chain.",
                    "Nothing personal with Red Claw. Your outpost just happens to be worth a lot of scrap.",
                    "Cute. Wreck thirty percent of our front line and you think you're bulletproof? Let's test that collar!"
                }
                :tier==1?new[]{
                    "You again, Knox. Over half our turf is burning because of you.",
                    "Take it up with Vale's ledger. I'm not done collecting yet.",
                    "You don't take another step toward our stronghold! Boys—tear him apart!"
                }
                :new[]{
                    "All the way to the throne room... and still wearing another man's leash.",
                    "Red Claw ends tonight. Your vault's paying off the last of my tab.",
                    "Over my dead body! This city was built in our blood—you're dying on this floor!"
                })
                :(tier==0?new[]{
                    "Heavy boots, stranger. You must be the collared stray tearing up Coyote territory.",
                    "Step aside. I'm only here for the safe and the salvage.",
                    "Wrong answer. Nothing walks into a Coyote den and leaves with its hide intact!"
                }
                :tier==1?new[]{
                    "You're a hard man to kill, Knox. Sixty percent of our dens gone—you've cost me a fortune.",
                    "Blame Vale. I'm just working off a bomb around my neck.",
                    "Your neck isn't my problem! Nobody backs the Coyotes into a corner and walks away!"
                }
                :new[]{
                    "Right to the last den. Shame a killer with your eyes is still running errands for Malcolm Vale.",
                    "Not for much longer. Once your stronghold falls, Vale's next on my list.",
                    "Now that I'd pay to see. Come on then, Knox—earn the right to kill him!"
                });
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.8f));
            DrawUnifiedDialogueScene(red?"raven":"vex",knoxSpeaking,speaker,VaultLanguage.Japanese?ja[factionBossConversationBeat]:en[factionBossConversationBeat],Gold,(factionBossConversationBeat+1)+" / 3");
            if(WorldButton(UnifiedDialoguePrimaryButton,factionBossConversationBeat==2?T("FIGHT","戦う"):T("NEXT","次へ")))
            {if(++factionBossConversationBeat>=3){factionBossConversation=false;Time.timeScale=1;PlayerPrefs.SetInt("Vault.FactionBoss.Intro."+SelectedStageId,1);PlayerPrefs.Save();}}
        }
    }
}
