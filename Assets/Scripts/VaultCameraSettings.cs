using UnityEngine;
namespace NeonVault {
 public partial class VaultGame {
  float CameraZoom=>Mathf.Clamp(PlayerPrefs.GetFloat("Ashfall.CameraZoom",1),.65f,1.45f);
  void DrawCameraZoom(float x,float y,float width,bool mobile){
   float current=CameraZoom;Text(x,y,T("CAMERA ZOOM","カメラズーム")+"  "+Mathf.RoundToInt(current*100)+"%",new GUIStyle(label){fontSize=mobile?28:21},UiPaper,width);
   float btnW=mobile?52f:36f,btnH=mobile?42f:28f,btnY=y+(mobile?34f:33f);
   float value=current;
   if(SliderStepButton(new Rect(x,btnY,btnW,btnH),"−",mobile))value=Mathf.Clamp(current-.08f,.65f,1.45f);
   var previous=GUI.color;GUI.color=Color.white;var slider=new GUIStyle(GUI.skin.horizontalSlider){fixedHeight=mobile?12:8};var thumb=new GUIStyle(GUI.skin.horizontalSliderThumb){fixedWidth=mobile?30:24,fixedHeight=mobile?30:24};
   Rect track=new Rect(x+btnW+6f,y+40f,Mathf.Max(60f,width-(btnW+6f)*2f),28f);
   float sliderVal=ControllerHorizontalSlider(track,current,.65f,1.45f,slider,thumb);GUI.color=previous;
   if(Mathf.Abs(sliderVal-current)>.001f)value=sliderVal;
   if(SliderStepButton(new Rect(x+width-btnW,btnY,btnW,btnH),"+",mobile))value=Mathf.Clamp(current+.08f,.65f,1.45f);
   if(Mathf.Abs(value-current)>.001f){PlayerPrefs.SetFloat("Ashfall.CameraZoom",value);if(cam)cam.orthographicSize=(IsOverworld?(MobileTouchControls.Enabled?10:11):7.5f)/value;}
  }
  void DrawMasterVolume(float x,float y,float width,bool mobile){
   float current=Mathf.Clamp01(PlayerPrefs.GetFloat("Ashfall.MasterVolume",1f));
   Text(x,y,T("SOUND VOLUME","音量")+"  "+Mathf.RoundToInt(current*100)+"%",new GUIStyle(label){fontSize=mobile?28:21},UiPaper,width);
   float btnW=mobile?52f:36f,btnH=mobile?42f:28f,btnY=y+(mobile?34f:33f);
   float value=current;
   if(SliderStepButton(new Rect(x,btnY,btnW,btnH),"−",mobile))value=Mathf.Clamp01(current-.10f);
   var previous=GUI.color;GUI.color=Color.white;
   var slider=new GUIStyle(GUI.skin.horizontalSlider){fixedHeight=mobile?12:8};var thumb=new GUIStyle(GUI.skin.horizontalSliderThumb){fixedWidth=mobile?30:24,fixedHeight=mobile?30:24};
   Rect track=new Rect(x+btnW+6f,y+40f,Mathf.Max(60f,width-(btnW+6f)*2f),28f);
   float sliderVal=ControllerHorizontalSlider(track,current,0f,1f,slider,thumb);GUI.color=previous;
   if(Mathf.Abs(sliderVal-current)>.001f)value=sliderVal;
   if(SliderStepButton(new Rect(x+width-btnW,btnY,btnW,btnH),"+",mobile))value=Mathf.Clamp01(current+.10f);
   if(Mathf.Abs(value-current)>.001f){PlayerPrefs.SetFloat("Ashfall.MasterVolume",value);AudioListener.volume=value;}
  }
 }
}
