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
            string[] seraJa={
                "見ないツラだね。……ああ、その首元の鉄輪、ヴェイルの新入りか。",
                "そんなに目立つか。",
                "そりゃね、それを巻かれて二日目を拝める奴は滅多にいないからさ。私はセラ。ここで鉄砲と弾を転がしてる。",
                "外で拾ったガラクタも買い取るのか。",
                "引き金が引けりゃ値段はつけるよ。潰れてても部品くらいにはなる。袋が詰まったらここへ放り込みな。実弾と荷袋の拡張も扱ってる。",
                "足元を見るような真似はしないだろうな。",
                "あいにく慈善事業は廃業してね。だが粗悪な弾は売らない、そこだけは信用しな。",
                "生きて戻ったかい。今日は仕入れ？ それとも戦利品の処分かい？"
            };
            string[] seraEn={
                "New face. ...Ah, the iron choker. One of Vale's fresh recruits.",
                "Does it stand out that much?",
                "Only because most guys wearing one don't live to see day two. I'm Sera. I move guns and lead out of this counter.",
                "Do you buy salvage off the street?",
                "If it cycles, I'll price it. Even bent iron's good for spare parts. Dump your excess here when your pack gets heavy. I also stock ammo and rig expansions.",
                "Tell me you don't gouge the collared guys.",
                "Honey, charity went extinct before the ash fell. What I don't sell is bad powder—that's the best deal you'll get in this town.",
                "Still breathing, huh? Looking to stock up, or unloading dead men's gear?"
            };
            string[] briggsJa={
                "名前を言え。",
                "ノックスだ。",
                "顔と帳簿は一致してる。だが本人の口から言わせるのがここの手順でね。",
                "元本10億スクラップ。窓口は俺だ。小銭だろうと血塗れのスクラップだろうと、耳を揃えりゃ受け取る。",
                "その借金、そもそも俺が背負う筋合いのものじゃない。",
                "その台詞、このカウンターで百回は聞いたよ。",
                "俺の言うことが嘘だと？",
                "真実かどうかなんてのは、俺の帳簿には関係ない。俺は払われた数字だけを消す。もっとも、一定の額を納めるごとに戦闘訓練の権限は解放してやる。",
                "10億きっちり払えば、この首輪は外れるんだな。",
                "帳簿の上では完済になる。だがロックを解除する親鍵はヴェイルの懐だ。俺が保証できるのは数字までだ。",
                "ずいぶん正直に言うんだな。",
                "計算の合わない仕事は性に合わないんでね。",
                "さて、今日は入金か？ それとも訓練の申請か。"
            };
            string[] briggsEn={
                "State your name.",
                "Knox.",
                "Matches the file. Protocol says I still make you say it out loud.",
                "Principal balance: one billion scrap. All payments clear through this desk. Pocket change or blood money, it all counts the same.",
                "Funny thing is, I never signed for that debt.",
                "If I had a scrap for every man who told me that, I'd buy out Vale myself.",
                "So you think I'm lying.",
                "I'm an accountant, Knox, not a priest. I don't care who's innocent—I care what's paid. Hit your repayment brackets and I'll authorize combat training.",
                "And when the balance hits zero, the collar comes off?",
                "My ledger closes at zero. The master key stays in Vale's coat pocket. I only guarantee the math, not the man.",
                "Appreciate the honesty.",
                "Cooked books give me a migraine.",
                "Now then—making a deposit, or filing for training?"
            };
            var lines=npcConversation==1?(VaultLanguage.Japanese?seraJa:seraEn):(VaultLanguage.Japanese?briggsJa:briggsEn);
            int last=lines.Length-1;int firstLast=npcConversation==1?6:11;
            bool playerLine=npcConversation==1?(npcBeat==1||npcBeat==3||npcBeat==5):(npcBeat==1||npcBeat==4||npcBeat==6||npcBeat==8||npcBeat==10);
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.76f));
            string speaker=playerLine?T("KNOX","ノックス"):npcConversation==1?T("SERA","セラ"):T("BRIGGS","ブリッグス");
            DrawUnifiedDialogueScene(npcConversation==1?"sera":"briggs",playerLine,speaker,lines[Mathf.Clamp(npcBeat,0,last)],Gold);
            if(WorldButton(UnifiedDialoguePrimaryButton,npcBeat>=firstLast?T("CONTINUE","続ける"):T("NEXT","次へ")))
            {
                if(npcBeat>=firstLast){int who=npcConversation;npcConversation=0;PlayerPrefs.SetInt(who==1?"Vault.SeraIntro":"Vault.BriggsIntro",1);PlayerPrefs.Save();debtPage=who==2;debtSkillTreePage=false;shopSellingPage=false;pendingSaleKey=null;screen=Screen.Shop;}
                else npcBeat++;
            }
        }
    }
}
