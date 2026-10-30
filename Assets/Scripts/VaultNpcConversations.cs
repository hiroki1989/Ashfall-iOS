using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        int npcConversation, npcBeat;
        bool NpcConversationVisible => npcConversation != 0;
        void OpenMerchantDialogue(){npcConversation=1;npcBeat=PlayerPrefs.GetInt("Vault.SeraIntro",0)==0?0:7;}
        void OpenBriggsDialogue(){npcConversation=2;npcBeat=PlayerPrefs.GetInt("Vault.BriggsIntro",0)==0?0:12;}
        void DrawNpcConversation()
        {
            string[] seraJa={"見ない顔だね。……ああ、首輪付きか。","そんなに珍しいか？","長く生きて帰ってくるやつはね。私はセラ。ここで武器と弾を扱ってる。","外で拾った物も買うのか。","使える物なら。壊れてても部品になる。荷物を整理したい時は持ってきな。弾と所持枠の拡張も売ってるよ。","良心的な値段だといいが。","良心は品切れ。弾ならまだある。","おかえり。今日は買う？ それとも、荷物を軽くしていく？"};
            string[] seraEn={"New face. Oh... another collar.","Are we that rare?","The ones who come back alive are. I'm Sera. I sell guns and ammunition.","Do you buy what I find out there?","If it's useful. Even broken gear has parts. Bring it here when you need room. I sell ammo and storage upgrades too.","I hope your prices are fair.","Fresh out of fair. Still got ammunition, though.","Welcome back. Buying, or looking to lighten your load?"};
            string[] briggsJa={"名前は。","レイド。","知ってる。本人の口から聞く決まりだ。","負債、10億スクラップ。返済窓口はここ。少額でも受け付ける。","その借金、俺が作ったわけじゃない。","そう言ったやつは、お前が初めてじゃない。","信じないのか。","俺の仕事は、真偽を決めることじゃない。払った額を記録することだ。節目まで返せば、訓練に使えるポイントも渡す。","全部払えば首輪は外れるんだな？","帳簿の残高はゼロになる。首輪の鍵はヴェイルが持ってる。それ以上の約束を、俺の口からはできない。","正直だな。","数字が合わなくなるのは嫌いでね。","返済か、訓練か。どちらにする。"};
            string[] briggsEn={"Name.","Reid.","I know. Procedure says I hear it from you.","Debt: one billion scrap. Payments go through me. Small amounts are accepted.","I didn't make that debt.","You're not the first to say so.","You don't believe me?","Truth isn't my department. I record what you pay. Reach a milestone and you'll get points for training.","Once it's paid, the collar comes off?","The ledger reaches zero. Vale has the key. I can't promise anything beyond that.","Honest, aren't you?","I don't like numbers that don't add up.","Payment or training? Which is it?"};
            var lines=npcConversation==1?(VaultLanguage.Japanese?seraJa:seraEn):(VaultLanguage.Japanese?briggsJa:briggsEn);
            int last=lines.Length-1;int firstLast=npcConversation==1?6:11;
            bool playerLine=npcConversation==1?(npcBeat==1||npcBeat==3||npcBeat==5):(npcBeat==1||npcBeat==4||npcBeat==6||npcBeat==8||npcBeat==10);
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.72f));Panel(new Rect(110,425,1220,350),Gold);
            Text(145,455,playerLine?T("REID","レイド"):npcConversation==1?T("SERA","セラ"):T("BRIGGS","ブリッグス"),heading,Gold,1100);
            Text(145,525,lines[Mathf.Clamp(npcBeat,0,last)],new GUIStyle(label){fontSize=34,wordWrap=true},UiPaper,1100);
            if(WorldButton(new Rect(910,685,370,65),npcBeat>=firstLast?T("CONTINUE","続ける"):T("NEXT","次へ")))
            {
                if(npcBeat>=firstLast){int who=npcConversation;npcConversation=0;PlayerPrefs.SetInt(who==1?"Vault.SeraIntro":"Vault.BriggsIntro",1);PlayerPrefs.Save();debtPage=who==2;debtSkillTreePage=false;shopSellingPage=false;pendingSaleKey=null;screen=Screen.Shop;}
                else npcBeat++;
            }
        }
    }
}
