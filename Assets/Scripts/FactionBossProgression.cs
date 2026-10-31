using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public const int RedClawBoss30=200, RedClawBoss60=201, RedClawHeadquarters=202;
        public const int CoyoteBoss30=203, CoyoteBoss60=204, CoyoteHeadquarters=205;
        bool factionBossConversation; int factionBossConversationBeat;
        public static bool IsFactionHeadquarters(int stage)=>stage>=RedClawBoss30&&stage<=CoyoteHeadquarters;
        public static bool IsRedClawBossStage(int stage)=>stage>=RedClawBoss30&&stage<=RedClawHeadquarters;
        public static bool IsCoyoteBossStage(int stage)=>stage>=CoyoteBoss30&&stage<=CoyoteHeadquarters;
        public static int FactionBossTier(int stage)=>!IsFactionHeadquarters(stage)?-1:(stage-RedClawBoss30)%3;
        public static bool IsFactionBossEscapeStage(int stage)=>IsFactionHeadquarters(stage)&&FactionBossTier(stage)<2;
        public static int RequiredFactionClearPercent(int stage){int tier=FactionBossTier(stage);return tier==0?30:tier==1?60:tier==2?100:0;}
        public static int FactionRegularDungeonCount(DungeonFaction faction)
        {
            if(faction==DungeonFaction.None)return 0;
            int count=0;
            for(int id=0;id<ExpeditionMap.RegularStageCount;id++)if(ExpeditionMap.FactionForStage(id)==faction)count++;
            return count;
        }
        public int ClearedFactionDungeonCount(DungeonFaction faction)
        {
            if(faction==DungeonFaction.None)return 0;
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
        void UpdateFactionBossUnlocks(int paidBefore) { }
        void UpdateFactionBossUnlocks(int raidersBefore,int gangBefore)
        {
            int raidersNow=ClearedFactionDungeonCount(DungeonFaction.Raiders);
            int gangNow=ClearedFactionDungeonCount(DungeonFaction.Gang);
            int[] redStages={RedClawBoss30,RedClawBoss60,RedClawHeadquarters};
            string[] redNamesEn={"RED CLAW OUTPOST (30%)","RED CLAW WAR CAMP (60%)","RED CLAW STRONGHOLD (100%)"};
            string[] redNamesJa={"レッドクロウ前線基地（制圧30%）","レッドクロウ軍事拠点（制圧60%）","レッドクロウ本拠地（制圧100%）"};
            for(int i=0;i<3;i++)
            {
                int req=RequiredFactionDungeonClears(redStages[i]);
                if(raidersBefore<req&&raidersNow>=req)
                {
                    DiscoverOverworldDungeon(redStages[i]);
                    SetOverworldWaypoint(2,redStages[i]);
                    Notify(redNamesEn[i]+" UNLOCKED / Boss battle available",redNamesJa[i]+" 解放 / ボス戦ダンジョンに出撃可能");
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
            string facEn=faction==DungeonFaction.Raiders?"RED CLAW":"COYOTE";
            string facJa=faction==DungeonFaction.Raiders?"レッドクロウ":"コヨーテ";
            return japanese?facJa+"拠点 "+pct+"%制圧で解放（現在 "+curPct+"%）":"REQUIRES "+pct+"% "+facEn+" CLEAR (NOW "+curPct+"%)";
        }
        public void BeginFactionBossConversation()
        {
            if(!IsFactionHeadquarters(SelectedStageId)||PlayerPrefs.GetInt("Vault.FactionBoss.Intro."+SelectedStageId,0)!=0)return;
            factionBossConversation=true;factionBossConversationBeat=0;Time.timeScale=0;
        }
        public void DrawFactionBossConversation()
        {
            bool red=IsRedClawBossStage(SelectedStageId);
            int tier=FactionBossTier(SelectedStageId);
            string boss=red?T("RAVEN","レイヴン"):T("VEX","ヴェックス");
            string[] ja=red?(tier==0?new[]{"ヴェイルの犬が、俺たちの前線基地まで嗅ぎつけてきたか。","3割ばかりアジトを潰した程度で、いい気になるなよ。","少し遊んでやる。生き残れたらな。"}
                :tier==1?new[]{"またお前か、ノックス。俺たちの拠点を半分以上も荒らしやがって。","だが、ここで終わりだ。本拠地に近づく前に始末する。","来い！ レッドクロウの牙を思い知らせてやる！"}
                :new[]{"ここまで来たか。ヴェイルの鎖につながれた犬が。","首輪は外す。だが、その前にお前を倒す。","いいだろう。赤い街の代償を教えてやる。"})
                :(tier==0?new[]{"足音が聞こえた時から、お前だと思ったよ。","コヨーテの縄張りを荒らしてる首輪付きっていうのは、あんたか。","前線基地で迎え撃つ。生きて帰れると思うな。"}
                :tier==1?new[]{"しつこい奴だ。コヨーテの拠点を6割も潰すとはね。","これ以上好き勝手はさせない。ここで足を止めてもらう。","本拠地へは一歩も近づけさせないよ！"}
                :new[]{"足音が聞こえた時から、お前だと思った。","コヨーテの縄張りで好き勝手はさせない。","生きて帰れたら、ヴェイルに伝えろ。次はあいつだ。"});
            string[] en=red?(tier==0?new[]{"So Vale's hound sniffed out our forward outpost.","Clear thirty percent of our hideouts and you think you own the streets?","Let's see how long you last against me."}
                :tier==1?new[]{"You again, Knox. You've wrecked over half our territory.","That ends here. I'll bury you before you ever reach our stronghold.","Come on! Taste Red Claw's fangs!"}
                :new[]{"Vale's chained dog made it this far.","The collar comes off. First, I deal with you.","Then learn what this city costs."})
                :(tier==0?new[]{"I knew it was you by the footsteps.","So you're the collared stray hitting Coyote turf.","This outpost is as far as you go."}
                :tier==1?new[]{"Persistent, aren't you? Taking down sixty percent of Coyote territory.","You won't take another step toward our stronghold.","Let's end this right here!"}
                :new[]{"I knew it was you by the footsteps.","Nobody walks through Coyote territory unchallenged.","If you survive, tell Vale he's next."});
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.8f));
            DrawDialoguePortraits(red?"raven":"vex",false,new Rect(110,425,1220,350),Gold);
            Panel(new Rect(110,425,1220,350),Gold);
            Text(145,455,boss,heading,Gold,1100);
            Text(145,525,VaultLanguage.Japanese?ja[factionBossConversationBeat]:en[factionBossConversationBeat],new GUIStyle(label){fontSize=34,wordWrap=true},Color.white,1100);
            if(WorldButton(new Rect(910,685,370,65),factionBossConversationBeat==2?T("FIGHT","戦う"):T("NEXT","次へ")))
            {if(++factionBossConversationBeat>=3){factionBossConversation=false;Time.timeScale=1;PlayerPrefs.SetInt("Vault.FactionBoss.Intro."+SelectedStageId,1);PlayerPrefs.Save();}}
        }
    }
}
