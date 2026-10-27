using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  int bossIntroBeat;
  void DrawCinematicBossIntro(){
   string[] speakersEn={"","RAIDER BOSS","YOU","RAIDER BOSS","YOU","RAIDER BOSS","RAIDER BOSS"};
   string[] speakersJa={"","レイダーのボス","主人公","レイダーのボス","主人公","レイダーのボス","レイダーのボス"};
   string[] linesEn={
    "A low hum. Cold concrete. You wake with a steel collar locked around your neck.",
    "Look who's back from the dead. Easy there. The collar's got a temper, and I'm holding the switch.",
    "Where am I? What happened to the convoy?",
    "It burned. Someone pinned the missing cargo on you. Convenient story, isn't it? I don't care whether it's true.",
    "I didn't take anything. Take this thing off and let me find who set me up.",
    "Bring me a billion in scrap and I'll unlock it. Briggs keeps the books. Until then, the ruins are your new workplace.",
    "Find the truth if you like. Just make sure you survive long enough to pay me."};
   string[] linesJa={
    "低い機械音。冷たいコンクリート。目を覚ますと、首には鋼鉄の首輪が巻かれていた。",
    "お目覚めか。急に動くなよ。その首輪は気が短い。起爆装置は俺が持ってる。",
    "ここはどこだ？ 輸送車はどうなった？",
    "燃えたよ。積み荷を盗んだ犯人はお前だって話だ。都合のいい話だな。真偽は俺には関係ない。",
    "俺は何も盗んでない。首輪を外せ。誰が俺をはめたのか突き止める。",
    "スクラップを10億持ってこい。返済はブリッグスが記録する。それまでは廃きょがお前の職場だ。",
    "真犯人を探すのは勝手だ。ただし、返済できるまで生き延びろ。"};
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
   Text(94,92,T("ASHFALL  /  THE COLLAR","アッシュフォール / 首輪"),heading,Gold,1200);
   Text(96,158,T("A debt you never made.","身に覚えのない負債。"),small,Mist,900);
   Box(new Rect(88,570,1264,245),new Color(.035f,.06f,.075f,.97f));
   Box(new Rect(88,570,6,245),bossIntroBeat==2||bossIntroBeat==4?Cyan:Gold);
   string speaker=VaultLanguage.Japanese?speakersJa[bossIntroBeat]:speakersEn[bossIntroBeat];
   Text(120,590,string.IsNullOrEmpty(speaker)?T("PROLOGUE","序章"):speaker,new GUIStyle(label){fontSize=28,fontStyle=FontStyle.Bold},bossIntroBeat==2||bossIntroBeat==4?Cyan:Gold,1000);
   Text(120,642,VaultLanguage.Japanese?linesJa[bossIntroBeat]:linesEn[bossIntroBeat],new GUIStyle(label){fontSize=27,wordWrap=true},Color.white,1190);
   Text(112,832,(bossIntroBeat+1)+" / "+linesEn.Length,tiny,Mist,190);
   if(WorldButton(new Rect(960,822,380,64),bossIntroBeat==linesEn.Length-1?T("ENTER ASHFALL","アッシュフォールへ"):T("CONTINUE","続ける"))){
    if(++bossIntroBeat>=linesEn.Length){bossIntroBeat=0;CompleteWorldBossConversation();if(cam){cam.orthographicSize=11/CameraZoom;UpdateCamera(true);}}
   }
  }
 }
}
