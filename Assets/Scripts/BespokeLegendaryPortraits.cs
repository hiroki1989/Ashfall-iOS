using UnityEngine;
using System.Collections.Generic;
namespace NeonVault {
 public static class BespokeLegendaryPortraits {
  [System.Serializable] sealed class HullPoint {public float x,y;}
  [System.Serializable] sealed class Cell {public HullPoint[] hull;}
  [System.Serializable] sealed class Table {public Cell[] cells;}
  static readonly Dictionary<string,Table> hulls=new Dictionary<string,Table>();
  static readonly Dictionary<string,RenderTexture> previews=new Dictionary<string,RenderTexture>();static Material material;static readonly Vector4[] clip=new Vector4[64];
  static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
  static readonly Dictionary<string,Rect[]> bounds=new Dictionary<string,Rect[]>();
  public static bool Draw(Rect rect,Gun g){int i=BespokeLegendaryDesigns.Index(g);return i>=0&&Draw(rect,i,false);}
  public static bool Draw(Rect rect,ArmorItem a){int i=BespokeLegendaryDesigns.Index(a);return i>=0&&Draw(rect,i,true);}
  static bool Draw(Rect rect,int index,bool armor){
   bool expanded=!armor&&index>=GunSeriesDesigns.FirstNewLegendary;
   int maker=expanded?LegendaryCatalog.MakerFromCatalogIndex(index):index/5;
   int artCell=expanded?LegendaryCatalog.VariantFromCatalogIndex(index)-5:(armor?5:0)+index%5;
   string path=expanded?"Art/BespokeLegendariesExpanded/"+ManufacturerCatalog.Id(maker)+"-legendaries-v2":"Art/BespokeLegendaries/"+ManufacturerCatalog.Id(maker)+"-legendaries-v1";
   if(!textures.TryGetValue(path,out var t)||!t){t=Resources.Load<Texture2D>(path);if(!t)return false;textures[path]=t;}
   if(!bounds.TryGetValue(path,out var cells)){
    var json=Resources.Load<TextAsset>(path+"-bounds");cells=json?DistinctEquipmentPortraits.ParseBounds(json.text,10):null;
    if(cells==null){cells=new Rect[10];for(int row=0;row<2;row++)for(int col=0;col<5;col++)cells[row*5+col]=new Rect(col/5f,(1-row)/2f,.2f,.5f);}bounds[path]=cells;if(json)hulls[path]=JsonUtility.FromJson<Table>(json.text);
   }
   Color old=GUI.color;GUI.color=new Color(.035f,.043f,.05f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Arsenal.Colors[4];GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.x,rect.yMax-2,rect.width,2),Texture2D.whiteTexture);
   Rect uv=cells[artCell];float ratio=uv.width*t.width/(uv.height*t.height);var target=new Rect(rect.x+5,rect.y+5,rect.width-10,rect.height-10);if(ratio>target.width/target.height){float h=target.width/ratio;target.y+=(target.height-h)*.5f;target.height=h;}else{float w=target.height*ratio;target.x+=(target.width-w)*.5f;target.width=w;}
   GUI.color=Color.white;int cell=artCell;string key=path+":"+cell;if(!previews.TryGetValue(key,out var preview)||!preview){
    var shader=Resources.Load<Shader>("PopItemPreview");if(shader){if(!material)material=new Material(shader);material.SetVector("_Crop",new Vector4(uv.x,uv.y,uv.width,uv.height));int count=0;if(hulls.TryGetValue(path,out var table)&&table?.cells!=null&&cell<table.cells.Length&&table.cells[cell]?.hull!=null){count=Mathf.Min(64,table.cells[cell].hull.Length);for(int i=0;i<count;i++)clip[i]=new Vector4(table.cells[cell].hull[i].x,table.cells[cell].hull[i].y,0,0);}material.SetInt("_ClipCount",count);material.SetVectorArray("_ClipPoints",clip);int width=ratio>=1?512:Mathf.Max(64,Mathf.RoundToInt(512*ratio)),height=ratio>=1?Mathf.Max(64,Mathf.RoundToInt(512/ratio)):512;preview=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);preview.Create();var oldTarget=RenderTexture.active;try{Graphics.Blit(t,preview,material);}finally{RenderTexture.active=oldTarget;}previews[key]=preview;}
   }if(preview)GUI.DrawTexture(target,preview,ScaleMode.ScaleToFit,true);else GUI.DrawTextureWithTexCoords(target,t,uv,true);ManufacturerBranding.PortraitBadge(rect,ManufacturerCatalog.Id(maker));GUI.color=old;return true;
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){foreach(var rt in previews.Values)if(rt){rt.Release();Object.Destroy(rt);}previews.Clear();if(material)Object.Destroy(material);material=null;textures.Clear();bounds.Clear();hulls.Clear();}
 }
}
