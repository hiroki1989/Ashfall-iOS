using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  bool freeWorldBoundaryPrompt; float freeWorldPromptCooldown, freeWorldPriorTimeScale;
  Vector3 RestrictFreeWorldPosition(Vector3 position,bool notify) {
   if(!FreeVersionBlocksProgress || (notify && !IsOverworld))return position;
   Vector3 center=WorldGeography.Camps[0]; Vector3 delta=position-center; delta.y=0;
   if(delta.sqrMagnitude<=600f*600f)return position;
   Vector3 clamped=notify ? center+delta.normalized*599.5f : center+Vector3.back*2.2f; clamped.y=position.y;
   if(notify) ShowFreeWorldBoundary(); return clamped;
  }
  bool FreeWorldDestinationBlocked(Vector3 position) {
   if(!FreeVersionBlocksProgress)return false;
   Vector3 delta=position-WorldGeography.Camps[0];delta.y=0;
   if(delta.sqrMagnitude<=600f*600f)return false;
   ShowFreeWorldBoundary();return true;
  }
  void ShowFreeWorldBoundary() {
   if(freeWorldBoundaryPrompt || Time.unscaledTime<freeWorldPromptCooldown)return;
   freeWorldPriorTimeScale=Time.timeScale; freeWorldBoundaryPrompt=true; Time.timeScale=0;
  }
  void CloseFreeWorldBoundaryPrompt() {freeWorldBoundaryPrompt=false;freeWorldPromptCooldown=Time.unscaledTime+2f;Time.timeScale=freeWorldPriorTimeScale;}
  void DrawFreeWorldBoundaryPrompt() {
   if(IosPremiumAccess.IsUnlocked){freeWorldBoundaryPrompt=false;Time.timeScale=freeWorldPriorTimeScale;return;}
   Box(new Rect(0,0,1440,900),new Color(0,0,0,.85f));Panel(new Rect(250,180,940,520),Gold);
   Text(290,220,T("CONTINUE YOUR JOURNEY","冒険の続きを"),heading,Gold,860);
   Text(290,310,T("Explore beyond this area in the full game.","この先のエリアは製品版で探索できます。"),new GUIStyle(label){wordWrap=true,fontSize=28},Color.white,850);
   Text(320,405,IosPremiumAccess.PriceText+"  "+IosPremiumAccess.StatusMessage,small,Mist,800);
   if(Button(new Rect(320,500,400,70),T("UNLOCK FULL GAME","製品版を購入"),true))IosPremiumAccess.Purchase();
            if(Button(new Rect(730,500,390,70),T("RESTORE PURCHASE","購入を復元")))IosPremiumAccess.Restore();
   if(Button(new Rect(490,600,460,65),T("CLOSE","閉じる"))) {CloseFreeWorldBoundaryPrompt();}
  }
 }
}
