using UnityEngine;
namespace NeonVault {
 public static class FieldUIArt {
  static Texture2D atlas;static Texture2D Atlas=>atlas?atlas:atlas=Resources.Load<Texture2D>("Art/UI/FieldKit/field-kit-v2");
  public static bool Icon(Rect bounds,int index,Color tint){if(index>=12)return false;if(!Atlas)return false;var old=GUI.color;GUI.color=tint;GUI.DrawTextureWithTexCoords(bounds,Atlas,new Rect(index%4*.25f,1-(index/4+1)*.25f,.25f,.25f));GUI.color=old;return true;}
  public static bool Plate(Rect bounds,bool amber)=>Icon(bounds,amber?12:13,Color.white);
 }
}
