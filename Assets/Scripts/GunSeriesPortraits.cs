using System.Collections.Generic;
using UnityEngine;
namespace NeonVault {
 // Whole painted side profiles: six class rows, four structurally distinct series.
 // No camera or model rendering is used for an inventory illustration.
 public static class GunSeriesPortraits {
  [System.Serializable] sealed class Point {public float x,y;}
  [System.Serializable] sealed class Cell {public Point[] hull;public DistinctEquipmentPortraits.BoundsCell[] exclusions;}
  [System.Serializable] sealed class Table {public Cell[] cells;}
  static Material material;static readonly Vector4[] clip=new Vector4[64];static readonly Vector4[] holes=new Vector4[32];
  static readonly Dictionary<string,RenderTexture> previews=new Dictionary<string,RenderTexture>();
  sealed class Sheet {public Texture2D image;public Rect[] bounds;public Table table;}
  static readonly Dictionary<int,Sheet> cache=new Dictionary<int,Sheet>();
  public static bool Available(int maker){return Load(maker)!=null;}
  static Sheet Load(int maker){
   if(cache.TryGetValue(maker,out var s)&&s.image)return s;
   string path="Art/GunSeries/"+ManufacturerCatalog.Id(maker)+"-series-v1";
   var image=Resources.Load<Texture2D>(path);if(!image)return null;
   var json=Resources.Load<TextAsset>(path+"-bounds");Rect[] bounds=null;
   if(json)try{bounds=DistinctEquipmentPortraits.ParseBounds(json.text,24);}catch(System.Exception){}
   if(bounds==null)return null;
   s=new Sheet{image=image,bounds=bounds,table=JsonUtility.FromJson<Table>(json.text)};cache[maker]=s;return s;
  }
  public static bool Draw(Rect card,Gun gun){
   var r=DistinctEquipmentDesigns.For(gun);if(r==null||r.legendary)return false;
   var s=Load(r.maker);if(s==null)return false;
   Rect uv=s.bounds[Mathf.Clamp(r.gunClass,0,5)*4+Mathf.Clamp(r.series,0,3)];
   Color old=GUI.color;GUI.color=new Color(.025f,.034f,.042f);GUI.DrawTexture(card,Texture2D.whiteTexture);
   GUI.color=Arsenal.Colors[(int)gun.rarity];GUI.DrawTexture(new Rect(card.x,card.y,card.width,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(card.x,card.yMax-2,card.width,2),Texture2D.whiteTexture);
   var target=new Rect(card.x+6,card.y+6,Mathf.Max(1,card.width-12),Mathf.Max(1,card.height-12));float ratio=uv.width*s.image.width/(uv.height*s.image.height);
   if(ratio>target.width/target.height){float h=target.width/ratio;target.y+=(target.height-h)*.5f;target.height=h;}else{float w=target.height*ratio;target.x+=(target.width-w)*.5f;target.width=w;}
   GUI.color=Color.white;int cell=Mathf.Clamp(r.gunClass,0,5)*4+Mathf.Clamp(r.series,0,3);string key=r.maker+":"+cell;
   if(!previews.TryGetValue(key,out var preview)||!preview){var shader=Resources.Load<Shader>("GunSeriesPreview");if(shader){if(!material)material=new Material(shader);material.SetVector("_Crop",new Vector4(uv.x,uv.y,uv.width,uv.height));var hull=s.table.cells[cell].hull;int count=Mathf.Min(64,hull.Length);for(int i=0;i<count;i++)clip[i]=new Vector4(hull[i].x,hull[i].y,0,0);var exclusions=s.table.cells[cell].exclusions;int holeCount=exclusions==null?0:Mathf.Min(32,exclusions.Length);for(int h=0;h<holeCount;h++){var e=exclusions[h];holes[h]=new Vector4(e.x,e.y,e.width,e.height);}material.SetInt("_ExcludeCount",holeCount);material.SetVectorArray("_ExcludeRects",holes);material.SetInt("_ClipCount",count);material.SetVectorArray("_ClipPoints",clip);preview=new RenderTexture(512,Mathf.Max(64,Mathf.RoundToInt(512/ratio)),0,RenderTextureFormat.ARGB32);preview.Create();var previous=RenderTexture.active;try{Graphics.Blit(s.image,preview,material);}finally{RenderTexture.active=previous;}previews[key]=preview;}}
   if(preview)GUI.DrawTexture(target,preview,ScaleMode.ScaleToFit,true);else GUI.DrawTextureWithTexCoords(target,s.image,uv,true);
   ManufacturerBranding.Badge(new Rect(card.x+3,card.yMax-24,21,21),ManufacturerCatalog.Id(r.maker));GUI.color=old;return true;
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){foreach(var rt in previews.Values)if(rt){rt.Release();Object.Destroy(rt);}previews.Clear();if(material)Object.Destroy(material);material=null;cache.Clear();}
 }
}
