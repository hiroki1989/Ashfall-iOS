using UnityEngine;
namespace NeonVault {
 public static class FieldUIArt {
  static Texture2D atlas,panel,selectedPanel,map;
  public static void MapSurface(Rect bounds){if(!map)map=Resources.Load<Texture2D>("Art/UI/FieldKit/premium-map-surface-v1");if(!map)return;var old=GUI.color;GUI.color=new Color(1,1,1,.12f);GUI.DrawTexture(bounds,map);GUI.color=old;}
  static Texture2D Atlas=>atlas?atlas:atlas=Resources.Load<Texture2D>("Art/UI/FieldKit/premium-icons-v1");
  static Texture2D Panel=>panel?panel:panel=Resources.Load<Texture2D>("Art/UI/FieldKit/premium-panel-v3");
  static Texture2D SelectedPanel=>selectedPanel?selectedPanel:selectedPanel=Resources.Load<Texture2D>("Art/UI/FieldKit/premium-panel-selected-v3");
  public static bool Icon(Rect bounds,int index,Color tint){if(index<0||index>=16||!Atlas)return false;float side=Mathf.Min(bounds.width,bounds.height);var target=new Rect(bounds.center.x-side*.5f,bounds.center.y-side*.5f,side,side);var old=GUI.color;GUI.color=tint;GUI.DrawTextureWithTexCoords(target,Atlas,new Rect(index%4*.25f,1-(index/4+1)*.25f,.25f,.25f));GUI.color=old;return true;}
  static Texture2D gunClasses;
  public static void GunClassIcon(Rect bounds,Gun gun,Color tint){
   if(!gunClasses)gunClasses=Resources.Load<Texture2D>("Art/UI/FieldKit/gun-class-icons-v1");if(!gunClasses||gun==null)return;
   var type=VaultGame.AmmoForGun(gun);int cell=type==AmmoType.Handgun?0:type==AmmoType.Shotgun?1:type==AmmoType.AssaultRifle?2:type==AmmoType.LightMachineGun?3:4;
   float[] starts={.018f,.153f,.365f,.572f,.78f},ends={.15f,.359f,.563f,.777f,.998f};
   Rect uv=new Rect(starts[cell],.30f,ends[cell]-starts[cell],.38f);float ratio=uv.width*gunClasses.width/(uv.height*gunClasses.height);float w=Mathf.Min(bounds.width,bounds.height*ratio),h=w/ratio;
   var target=new Rect(bounds.center.x-w/2,bounds.center.y-h/2,w,h);var old=GUI.color;GUI.color=tint;GUI.DrawTextureWithTexCoords(target,gunClasses,uv);GUI.color=old;
  }
  // Fixed-size authored corners: no stretching of trim across wide or tall panels.
  public static bool Plate(Rect r,bool selected)
  {
   var texture=selected?SelectedPanel:Panel;
   if(!texture||r.width<=0||r.height<=0)return false;
   float b=Mathf.Min(8,Mathf.Min(r.width,r.height)*.25f),uv=.022f;
   var old=GUI.color;float shade=selected?.68f:.62f;GUI.color=new Color(old.r*shade,old.g*shade,old.b*shade,old.a);
   for(int row=0;row<3;row++)for(int col=0;col<3;col++)
   {
    float x=col==0?r.x:col==1?r.x+b:r.xMax-b,y=row==0?r.y:row==1?r.y+b:r.yMax-b;
    float w=col==1?r.width-2*b:b,h=row==1?r.height-2*b:b;
    float u=col==0?0:col==1?uv:1-uv,v=row==0?1-uv:row==1?uv:0;
    GUI.DrawTextureWithTexCoords(new Rect(x,y,w,h),texture,new Rect(u,v,col==1?1-2*uv:uv,row==1?1-2*uv:uv));
   }
   GUI.color=old;return true;
  }
 }
}
