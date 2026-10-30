using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  int bossIntroBeat;
  void DrawCinematicBossIntro(){
   string[] speakersEn={"MALCOLM VALE","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","REID","MALCOLM VALE","MALCOLM VALE"};
   string[] speakersJa={"マルコム・ヴェイル","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","レイド","マルコム・ヴェイル","マルコム・ヴェイル"};
   string[] linesEn={"...You awake?","Easy. That collar gets touchy with guests who wake up too fast.","...What is this?","A collar. Surely you can see that.","Take it off.","Impatient, aren't you? We haven't even been introduced.","I'm Vale. I run this place.","Where am I? What happened to the convoy?","Ashfall. The city and the wasteland beyond it all go by that name. Your vehicle burned. Spectacularly.","And the cargo?","Gone. The story is that you ran off with it.","That's nonsense. I was just the driver.","I thought you'd say that. Dead men tell no tales; survivors get the bill. Convenient for somebody.","Who set this up?","Who knows? All I picked up was you and this ledger.","...A billion?","Cargo, trouble, inconvenience. Ask Briggs about the arithmetic.","You expect me to pay a debt I never made?","Want to try skipping out on it?","...One day I'll get this off and get answers from you.","There's that look. Go earn your keep in Ashfall. Briggs takes the payments.","Look for the culprit if you like. First, try living until tomorrow."};
   string[] linesJa={"……起きたか。","おっと。急に動くな。そいつ、寝起きの客には少し敏感でな。","……何だ、これ。","首輪だ。見れば分かるだろ。","外せ。","せっかちなやつだな。まだ自己紹介も済んでない。","俺はヴェイル。ここを仕切ってる。","ここはどこだ。輸送車は？","アッシュフォールだ。街も、その外の荒れ地も、まとめてそう呼んでる。お前の車は……燃えてたよ。派手にな。","積み荷は？","消えた。で、お前が持ち逃げしたことになってる。","馬鹿を言うな。俺は運んでただけだ。","ああ、そう言うと思った。死人に口なし。生き残りには借金あり。誰かにとっては、ずいぶん都合のいい話だ。","誰が仕組んだ？","さあな。俺が拾ったのはお前と、この帳簿だけだ。","……10億？","積み荷の値段に、手間賃やら迷惑料やらを足した。細かい計算はブリッグスに聞け。","身に覚えのない借金を払えってのか。","嫌なら踏み倒してみるか？","……いずれ、これを外して、お前に答えを聞く。","いいね。その顔だ。アッシュフォールで稼いでこい。返済はブリッグスが受け付ける。","真犯人を探したけりゃ好きにしろ。まずは明日まで生き延びることだな。"};
   bossIntroBeat=Mathf.Clamp(bossIntroBeat,0,linesEn.Length-1);
   if(Event.current.type==EventType.Repaint&&cam&&player)
   {
    Vector3 focus=bossIntroBeat==0?player.position+Vector3.up*.7f:new Vector3(-3,1.15f,7);
    float distance=bossIntroBeat==0?27:24;
    cam.orthographicSize=(bossIntroBeat==0?7.2f:6.2f)/CameraZoom;
    cam.transform.position=Vector3.Lerp(cam.transform.position,focus-cam.transform.forward*distance,Mathf.Clamp01(Time.unscaledDeltaTime*2.8f));
   }
   Box(new Rect(0,0,1440,900),new Color(.015f,.025f,.035f,bossIntroBeat==0?.92f:.56f));
   Box(new Rect(0,0,1440,42),new Color(.01f,.015f,.02f,.98f));
   Box(new Rect(0,858,1440,42),new Color(.01f,.015f,.02f,.98f));
   Box(new Rect(88,72,1264,4),Gold);
   Text(94,92,T("ASHFALL REGION  /  THE COLLAR","アッシュフォール地方 / 首輪"),heading,Gold,1200);
   Text(96,158,T("A debt you never made.","身に覚えのない負債。"),small,Mist,900);
   Box(new Rect(88,570,1264,245),new Color(.035f,.06f,.075f,.97f));
   Box(new Rect(88,570,6,245),speakersEn[bossIntroBeat]=="REID"?Cyan:Gold);
   string speaker=VaultLanguage.Japanese?speakersJa[bossIntroBeat]:speakersEn[bossIntroBeat];
   Text(120,590,string.IsNullOrEmpty(speaker)?T("PROLOGUE","序章"):speaker,new GUIStyle(label){fontSize=28,fontStyle=FontStyle.Normal},speakersEn[bossIntroBeat]=="REID"?Cyan:Gold,1000);
   Text(120,642,VaultLanguage.Japanese?linesJa[bossIntroBeat]:linesEn[bossIntroBeat],new GUIStyle(label){fontSize=32,wordWrap=true},Color.white,1190);
   Text(112,832,(bossIntroBeat+1)+" / "+linesEn.Length,tiny,Mist,190);
   if(WorldButton(new Rect(960,822,380,64),bossIntroBeat==linesEn.Length-1?T("ENTER ASHFALL","アッシュフォールへ"):T("CONTINUE","続ける"))){
    if(++bossIntroBeat>=linesEn.Length){bossIntroBeat=0;CompleteWorldBossConversation();if(cam){cam.orthographicSize=11/CameraZoom;UpdateCamera(true);}}
   }
  }
 }
}
