using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  int bossIntroBeat;
  void DrawCinematicBossIntro(){
   string[] speakersEn={"MALCOLM VALE","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","KNOX","MALCOLM VALE","MALCOLM VALE"};
   string[] speakersJa={"マルコム・ヴェイル","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","ノックス","マルコム・ヴェイル","マルコム・ヴェイル"};
   string[] linesEn={
    "...Eyes open.",
    "Easy on the neck. The detonator's got a hair trigger for guys who jump out of bed.",
    "...What the hell is on my throat?",
    "Five pounds of stamped steel and a shaped charge. Keep your hands off it.",
    "Take it off.",
    "Not even a 'good morning'? You've got terrible manners for a dead man.",
    "Name's Vale. Whatever still breathes in this sector answers to me.",
    "Where's my rig? What happened to the convoy?",
    "You're in Ashfall. As for your rig—she's a smear of black glass on the north highway. Burned for three hours.",
    "And the crates in the back?",
    "Cleaned out. Funny thing is, the client's saying the driver took the loot and lit the match himself.",
    "Bullshit. We were ambushed—heavy caliber from the ridge.",
    "Maybe. Doesn't change the math. Dead men get buried; the guy who crawls out of the wreck gets the tab.",
    "Who sold me out?",
    "Don't know, don't care. All that landed on my desk was you and an unpaid ledger.",
    "...One billion scrap?",
    "Principal, salvage recovery, plus my personal inconvenience. Briggs has the itemized receipt.",
    "You're charging me for a heist I was on the wrong end of.",
    "You're welcome to contest the bill. Just tap that red light under your chin.",
    "...When this thing comes off, you and I are having a different conversation.",
    "Hold onto that spite, Knox—it'll keep you warm out there. Bleed the Red Claw and Coyote strongholds dry, and bring every scrap to Briggs.",
    "Go find your ghost if it helps you sleep. Just make sure you pay your rent first."
   };
   string[] linesJa={
    "……目が覚めたか。",
    "おい、首は急に動かすな。そいつは寝起きの悪い客には、ちっとばかり過敏でな。",
    "……なんだ、この首の鉄塊は。",
    "鉛と成形炸薬だ。指はかけるなよ。お前の首が飛ぶだけならいいが、床が汚れる。",
    "外せ。",
    "つれないな。死に損ないのわりには、挨拶ひとつ知らんらしい。",
    "俺はヴェイルだ。この界隈で息をしてる連中は、だいたい俺の帳簿に載ってる。",
    "俺の輸送車は？ 護衛の連中どうなった。",
    "ここはアッシュフォールだ。お前のトラックなら、北の幹線道路で黒い鉄くずになってるよ。三時間はよく燃えてた。",
    "荷台のコンテナは。",
    "もぬけの殻だ。面白いことに、依頼主は『運転手が積み荷を抜いて車に火をつけた』と触れ回ってる。",
    "ふざけるな。尾根から重火器で待ち伏せされたんだ。",
    "だろうな。だが世間の算盤は単純だ。死んだ奴は墓に入り、這いずり出た奴がツケを払う。",
    "誰が俺を売った。",
    "さあな。俺の手元に転がり込んできたのは、半死にの運び屋と、桁外れの請求書だけだ。",
    "……10億スクラップだと？",
    "消えた荷の代金に、回収の手間賃、それから俺の不機嫌代だ。内訳が知りたけりゃブリッグスに聞け。",
    "襲われた側に、盗まれた荷の弁償をしろってのか。",
    "不服なら踏み倒してもいいんだぜ。顎の下の赤いランプを、指で強く叩いてみな。",
    "……この首輪が外れたら、てめえから先に鉛をくれてやる。",
    "いい目だ、ノックス。その殺気を忘れるな。外でレッドクロウとコヨーテの根城を片っ端から潰し、10億耳を揃えてブリッグスに渡せ。",
    "黒幕探しは趣味でやれ。まずは明日の朝まで、その首が胴体についてることだな。"
   };
   bossIntroBeat=Mathf.Clamp(bossIntroBeat,0,linesEn.Length-1);
   if(Event.current.type==EventType.Repaint&&cam&&player)
   {
    Vector3 focus=bossIntroBeat==0?player.position+Vector3.up*.7f:new Vector3(-3,1.15f,7);
    float distance=bossIntroBeat==0?27:24;
    cam.orthographicSize=(bossIntroBeat==0?7.2f:6.2f)/CameraZoom;
    cam.transform.position=Vector3.Lerp(cam.transform.position,focus-cam.transform.forward*distance,Mathf.Clamp01(Time.unscaledDeltaTime*2.8f));
   }
   Box(new Rect(0,0,1440,900),new Color(.015f,.025f,.035f,bossIntroBeat==0?.92f:.64f));
   Box(new Rect(0,0,1440,42),new Color(.01f,.015f,.02f,.98f));
   Box(new Rect(0,858,1440,42),new Color(.01f,.015f,.02f,.98f));
   Text(110,64,T("ASHFALL REGION  /  THE COLLAR","アッシュフォール地方 / 首輪"),heading,Gold,1200);
   Text(112,116,T("A debt you never made.","身に覚えのない負債。"),small,Mist,900);
   bool knoxSpeaking=speakersEn[bossIntroBeat]=="KNOX";
   string speaker=VaultLanguage.Japanese?speakersJa[bossIntroBeat]:speakersEn[bossIntroBeat];
   DrawUnifiedDialogueScene("vale",knoxSpeaking,speaker,VaultLanguage.Japanese?linesJa[bossIntroBeat]:linesEn[bossIntroBeat],Gold,(bossIntroBeat+1)+" / "+linesEn.Length);
   if(WorldButton(UnifiedDialoguePrimaryButton,bossIntroBeat==linesEn.Length-1?T("ENTER ASHFALL","アッシュフォールへ"):T("CONTINUE","続ける"))){
    if(++bossIntroBeat>=linesEn.Length){bossIntroBeat=0;CompleteWorldBossConversation();if(cam){cam.orthographicSize=11/CameraZoom;UpdateCamera(true);}}
   }
  }
 }
}
