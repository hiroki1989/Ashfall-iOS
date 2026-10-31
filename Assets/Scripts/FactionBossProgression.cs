using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        public const int RedClawHeadquarters=196, CoyoteHeadquarters=197;
        bool factionBossConversation; int factionBossConversationBeat;
        public static bool IsFactionHeadquarters(int stage)=>stage==RedClawHeadquarters||stage==CoyoteHeadquarters;
        public bool FactionHeadquartersUnlocked(int stage)=>!IsFactionHeadquarters(stage)||InitialDebt-DebtRemaining >= (stage==RedClawHeadquarters?300000000:600000000);
        public bool FactionBossConversationVisible=>factionBossConversation;
        void UpdateFactionBossUnlocks(int paidBefore)
        {
            int paid=InitialDebt-DebtRemaining;
            if(paidBefore<300000000&&paid>=300000000){SetOverworldWaypoint(2,RedClawHeadquarters);Notify("RED CLAW STRONGHOLD REVEALED","レッドクロウ本拠地の位置が判明");}
            if(paidBefore<600000000&&paid>=600000000){SetOverworldWaypoint(2,CoyoteHeadquarters);Notify("COYOTE STRONGHOLD REVEALED","コヨーテ本拠地の位置が判明");}
        }
        public void BeginFactionBossConversation()
        {
            if(!IsFactionHeadquarters(SelectedStageId)||PlayerPrefs.GetInt("Vault.FactionBoss.Intro."+SelectedStageId,0)!=0)return;
            factionBossConversation=true;factionBossConversationBeat=0;Time.timeScale=0;
        }
        public void DrawFactionBossConversation()
        {
            bool red=SelectedStageId==RedClawHeadquarters;
            string boss=red?T("RAVEN","レイヴン"):T("VEX","ヴェックス");
            string[] ja=red?new[]{"ここまで来たか。ヴェイルの鎖につながれた犬が。","首輪は外す。だが、その前にお前を倒す。","いいだろう。赤い街の代償を教えてやる。"}:new[]{"足音が聞こえた時から、お前だと思った。","コヨーテの縄張りで好き勝手はさせない。","生きて帰れたら、ヴェイルに伝えろ。次はあいつだ。"};
            string[] en=red?new[]{"Vale's chained dog made it this far.","The collar comes off. First, I deal with you.","Then learn what this city costs."}:new[]{"I knew it was you by the footsteps.","Nobody walks through Coyote territory unchallenged.","If you survive, tell Vale he's next."};
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.8f));Panel(new Rect(110,425,1220,350),Gold);
            Text(145,455,boss,heading,Gold,1100);
            Text(145,525,VaultLanguage.Japanese?ja[factionBossConversationBeat]:en[factionBossConversationBeat],new GUIStyle(label){fontSize=34,wordWrap=true},Color.white,1100);
            if(WorldButton(new Rect(910,685,370,65),factionBossConversationBeat==2?T("FIGHT","戦う"):T("NEXT","次へ")))
            {if(++factionBossConversationBeat>=3){factionBossConversation=false;Time.timeScale=1;PlayerPrefs.SetInt("Vault.FactionBoss.Intro."+SelectedStageId,1);PlayerPrefs.Save();}}
        }
    }
}
