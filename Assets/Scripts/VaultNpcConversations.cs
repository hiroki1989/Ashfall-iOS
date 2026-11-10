using PlayerPrefs = NeonVault.SaveSlotPrefs;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        int npcConversation, npcBeat, briggsChoiceSelection;
        bool NpcConversationVisible => npcConversation != 0;
        void OpenMerchantDialogue(){npcConversation=1;npcBeat=PlayerPrefs.GetInt("Vault.SeraIntro",0)==0?0:7;}
        void OpenBriggsDialogue()
        {
            EnsureMissionsLoaded();
            if(activeBriggsMissionReadyToTurnIn)
            {
                ClaimCompletedBriggsMissionReward();
                npcConversation=2;
                npcBeat=13;
                briggsChoiceSelection=0;
                return;
            }
            if(DebtLevel>=2&&PlayerPrefs.GetInt(PetAdoptedKey,0)==0)
            {
                npcConversation=3;npcBeat=0;return;
            }
            npcConversation=2;
            npcBeat=PlayerPrefs.GetInt("Vault.BriggsIntro",0)==0?0:12;
            briggsChoiceSelection=0;
        }
        void DrawNpcConversation()
        {
            if(npcConversation==3){DrawPetAdoptionConversation();return;}
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
                "元本10億スクラップ。ここは返済窓口だ。小銭だろうと血塗れのスクラップだろうと、耳を揃えりゃ受け取る。",
                "その借金、そもそも俺が背負う筋合いのものじゃない。",
                "その台詞、このカウンターで百回は聞いたよ。……もっとも、ただ座っていても数字は減らん。",
                "稼ぎ口を用意してくれるとでも言うのか。",
                "俺は取り立て屋だが、同時に仕事の斡旋もしてる。周辺の巣や連中の拠点の掃討——俺から仕事を受けて片付けてくれば、きっちり報酬を払ってやる。だが勝手に潜って暴れてきた分には一スクラップも出さん。仕事が欲しけりゃ俺を通せ。",
                "10億きっちり払えば、この首輪は外れるんだな。",
                "帳簿の上では完済になる。だがロックを解除する親鍵はヴェイルの懐だ。俺が保証できるのは数字までだ。",
                "ずいぶん正直に言うんだな。",
                "計算の合わない仕事は性に合わないんでね。……さて、借金を返しに来たか、それとも仕事を取りに来たか。"
            };
            string[] briggsEn={
                "State your name.",
                "Knox.",
                "Matches the file. Protocol says I still make you say it out loud.",
                "Principal balance: one billion scrap. All payments clear through this desk. Pocket change or blood money, it all counts the same.",
                "Funny thing is, I never signed for that debt.",
                "If I had a scrap for every man who told me that, I'd buy out Vale myself. Still—sitting here won't shrink your ledger.",
                "So what, you're offering me a way to pay it off?",
                "I collect the debt, and I hand out the contracts to pay it. Clear nearby nests or rival strongholds on my ledger, report back to me, and you'll get paid. Wander in on your own, though, and you won't see a single scrap from my drawer.",
                "And when the balance hits zero, the collar comes off?",
                "My ledger closes at zero. The master key stays in Vale's coat pocket. I only guarantee the math, not the man.",
                "Appreciate the honesty.",
                "Cooked books give me a migraine. ...Now then, you here to pay down your tab, or you looking for a job?"
            };

            Box(new Rect(0,0,1440,900),new Color(0,0,0,.76f));

            if(npcConversation==2 && npcBeat==13)
            {
                string doneTitle=briggsReceiptStage>=0?ExpeditionMap.Get(briggsReceiptStage,DebtLevel).Name:T("CONTRACT","依頼");
                string turnInBody=T("Clean work on ","きっちり片付けたな、")+doneTitle+T(". Here's your cut—logged cleanly on the ledger.","。これが報酬だ。帳簿にも記録しておいた。");
                DrawUnifiedDialogueScene("briggs",false,T("BRIGGS","ブリッグス"),turnInBody,Gold);
                Panel(new Rect(470,152,500,300),Gold);
                Text(492,168,T("CONTRACT PAYOUT RECEIVED","ミッション報酬を受け取りました"),label,Gold,456);
                Text(492,208,doneTitle,small,Cyan,456);
                if(briggsReceiptGun!=null||briggsReceiptArmor!=null)
                {
                    Rect p=new Rect(492,248,92,92);
                    if(briggsReceiptGun!=null)ItemPortraits.Draw(p,briggsReceiptGun);else ItemPortraits.Draw(p,briggsReceiptArmor);
                    string gearName=briggsReceiptGun!=null?VaultLanguage.GunName(briggsReceiptGun):Armory.Name(briggsReceiptArmor);
                    Rarity gearRarity=briggsReceiptGun!=null?briggsReceiptGun.rarity:briggsReceiptArmor.rarity;
                    Text(598,252,gearName,small,Arsenal.Colors[(int)gearRarity],352);
                    Text(598,288,VaultLanguage.RarityName(gearRarity),tiny,Mist,352);
                }
                Text(492,356,BriggsReceiptSummaryLine(),new GUIStyle(small){wordWrap=true},UiPaper,456);
                if(Button(UnifiedDialoguePrimaryButton,T("CONTINUE","続ける"),true)||(Event.current.type==EventType.Layout&&Input.GetKeyDown(KeyCode.Return)))
                {
                    ClearBriggsReceipt();
                    if(DebtLevel>=2&&PlayerPrefs.GetInt(PetAdoptedKey,0)==0){npcConversation=3;npcBeat=0;}
                    else {npcBeat=12;briggsChoiceSelection=0;}
                }
                return;
            }

            if(npcConversation==2 && npcBeat==12)
            {
                if(Event.current.type==EventType.Layout)
                {
                    if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.JoystickButton1)){npcConversation=0;return;}
                    if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A))briggsChoiceSelection=(briggsChoiceSelection+2)%3;
                    if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D))briggsChoiceSelection=(briggsChoiceSelection+1)%3;
                    if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))
                    {
                        if(briggsChoiceSelection==0){npcConversation=0;debtPage=true;briggsMissionPage=false;debtSkillTreePage=false;screen=Screen.Shop;return;}
                        if(briggsChoiceSelection==1){npcConversation=0;debtPage=true;briggsMissionPage=true;debtSkillTreePage=false;GetCampMissionOffers(LastVisitedCamp);screen=Screen.Shop;return;}
                        npcConversation=0;return;
                    }
                }
                string choicePrompt=T("Back in one piece. Are you making a repayment today, or do you want a job?","また来たか。今日は借金の返済か？ それとも仕事がほしいか？");
                string activeHint=HasActiveBriggsMission?T("ACTIVE JOB: ","受注中の仕事: ")+ExpeditionMap.Get(activeBriggsMissionStage,DebtLevel).Name:T("CONTRACT LIMIT: 1 JOB AT A TIME","受注中のミッションは1件まで");
                DrawUnifiedDialogueScene("briggs",false,T("BRIGGS","ブリッグス"),choicePrompt,Gold);
                Text(UnifiedDialogueBox.x+35f,UnifiedDialogueBox.y+210f,activeHint,small,HasActiveBriggsMission?Gold:Cyan,UnifiedDialogueBox.width-70f);
                if(Button(new Rect(145,725,360,65),T("REPAY DEBT","返済する"),briggsChoiceSelection==0))
                {
                    npcConversation=0;debtPage=true;briggsMissionPage=false;debtSkillTreePage=false;screen=Screen.Shop;
                }
                if(Button(new Rect(530,725,360,65),T("TAKE A JOB","仕事がほしい"),briggsChoiceSelection==1))
                {
                    npcConversation=0;debtPage=true;briggsMissionPage=true;debtSkillTreePage=false;GetCampMissionOffers(LastVisitedCamp);screen=Screen.Shop;
                }
                if(Button(new Rect(920,725,370,65),T("CLOSE","立ち去る"),briggsChoiceSelection==2))
                {
                    npcConversation=0;
                }
                return;
            }

            var lines=npcConversation==1?(VaultLanguage.Japanese?seraJa:seraEn):(VaultLanguage.Japanese?briggsJa:briggsEn);
            int last=lines.Length-1;int firstLast=npcConversation==1?6:11;
            bool playerLine=npcConversation==1?(npcBeat==1||npcBeat==3||npcBeat==5):(npcBeat==1||npcBeat==4||npcBeat==6||npcBeat==8||npcBeat==10);
            string speaker=playerLine?T("KNOX","ノックス"):npcConversation==1?T("SERA","セラ"):T("BRIGGS","ブリッグス");
            DrawUnifiedDialogueScene(npcConversation==1?"sera":"briggs",playerLine,speaker,lines[Mathf.Clamp(npcBeat,0,last)],Gold);
            if(WorldButton(UnifiedDialoguePrimaryButton,npcBeat>=firstLast?T("CONTINUE","続ける"):T("NEXT","次へ")))
            {
                if(npcBeat>=firstLast)
                {
                    int who=npcConversation;
                    PlayerPrefs.SetInt(who==1?"Vault.SeraIntro":"Vault.BriggsIntro",1);
                    PlayerPrefs.Save();
                    if(who==1)
                    {
                        npcConversation=0;debtPage=false;briggsMissionPage=false;debtSkillTreePage=false;shopSellingPage=false;pendingSaleKey=null;screen=Screen.Shop;
                    }
                    else
                    {
                        npcConversation=2;npcBeat=12;briggsChoiceSelection=0;
                    }
                }
                else npcBeat++;
            }
        }
    }
}
