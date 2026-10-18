using UnityEngine;
using System.Collections.Generic;
namespace NeonVault {
 // GUI composition of authored illustrations; this path never creates a camera or 3D renderer.
 public static class DistinctEquipmentPortraits {
  [System.Serializable] public sealed class BoundsCell {public float x,y,width,height; public Rect Rect=>new Rect(x,y,width,height);}
  [System.Serializable] public sealed class BoundsTable {public BoundsCell[] cells;}
  public static Rect[] ParseBounds(string json,int count){
   var data=JsonUtility.FromJson<BoundsTable>(json);if(data?.cells==null||data.cells.Length!=count)return null;var result=new Rect[count];
   for(int i=0;i<count;i++){var c=data.cells[i];if(c==null||!Valid(c.x)||!Valid(c.y)||!Valid(c.width)||!Valid(c.height)||c.width<=0||c.height<=0||c.x<0||c.y<0||c.x+c.width>1.001f||c.y+c.height>1.001f)return null;result[i]=c.Rect;}return result;
  }
  static bool Valid(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
  sealed class Sheet {public Texture2D texture;public Rect[] cells;}
  static readonly Dictionary<string,Sheet> sheets=new Dictionary<string,Sheet>();
  static readonly HashSet<string> warnings=new HashSet<string>();
  static Sheet Load(EquipmentRecipe r,string kind,int rows){
   string path="Art/DistinctEquipment/"+ManufacturerCatalog.Id(r.maker)+"-"+kind+(kind=="armor-accessories"?"-v1":"-parts-v1");
   if(sheets.TryGetValue(path,out var cached)&&cached.texture)return cached;
   string grounded="Art/GroundedEquipment/"+ManufacturerCatalog.Id(r.maker)+"-"+kind+"-parts-v1";if(kind!="armor-accessories"&&Resources.Load<Texture2D>(grounded))path=grounded;
   var texture=Resources.Load<Texture2D>(path);if(!texture){if(warnings.Add(path))Debug.LogWarning("Equipment illustration missing: "+path);return null;}
   Rect[] cells=null;var json=Resources.Load<TextAsset>(path+"-bounds");
   if(json){try{cells=ParseBounds(json.text,6*rows);}catch(System.Exception){}}
   if(cells==null||cells.Length!=6*rows){
    cells=new Rect[6*rows];Color32[] pixels=texture.isReadable?texture.GetPixels32():null;
    if(pixels==null&&warnings.Add(path+" bounds"))Debug.LogWarning("Equipment illustration needs bounds metadata or Read/Write: "+path);
    for(int row=0;row<rows;row++)for(int col=0;col<6;col++){
     int x0=col*texture.width/6,x1=(col+1)*texture.width/6,y0=(rows-row-1)*texture.height/rows,y1=(rows-row)*texture.height/rows;
     int left=x1,right=x0,bottom=y1,top=y0;
     if(pixels!=null)for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*texture.width+x].a>24){left=Mathf.Min(left,x);right=Mathf.Max(right,x+1);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y+1);}
     if(left>=right||bottom>=top){left=x0;right=x1;bottom=y0;top=y1;}
     cells[row*6+col]=new Rect(left/(float)texture.width,bottom/(float)texture.height,(right-left)/(float)texture.width,(top-bottom)/(float)texture.height);
    }
   }
   var sheet=new Sheet{texture=texture,cells=cells};sheets[path]=sheet;sheets["Art/DistinctEquipment/"+ManufacturerCatalog.Id(r.maker)+"-"+kind+(kind=="armor-accessories"?"-v1":"-parts-v1")]=sheet;return sheet;
  }
  static float Ratio(Sheet s,int cell){var uv=s.cells[cell];return uv.width*s.texture.width/Mathf.Max(1,uv.height*s.texture.height);}
  static void Part(Sheet sheet,int cell,Rect target){GUI.color=Color.white;GUI.DrawTextureWithTexCoords(target,sheet.texture,sheet.cells[cell],true);}
  static void Fill(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);}
  static void Frame(Rect r,Rarity rarity,bool legendary){Fill(r,new Color(.018f,.024f,.032f));Fill(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),legendary?new Color(.12f,.085f,.035f):new Color(.055f,.075f,.095f));Color c=Arsenal.Colors[(int)rarity];Fill(new Rect(r.x,r.y,r.width,2),c);Fill(new Rect(r.x,r.yMax-2,r.width,2),c);}
  static Rect Map(Rect part,Rect bounds,Rect target){float scale=Mathf.Min(target.width/bounds.width,target.height/bounds.height);return new Rect(target.center.x+(part.x-bounds.center.x)*scale,target.center.y+(part.y-bounds.center.y)*scale,part.width*scale,part.height*scale);}
  static Rect Union(Rect a,Rect b)=>Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin),Mathf.Min(a.yMin,b.yMin),Mathf.Max(a.xMax,b.xMax),Mathf.Max(a.yMax,b.yMax));
  public static bool Draw(Rect card,Gun item){var r=DistinctEquipmentDesigns.For(item);return r!=null&&Draw(card,r,"gun",item.rarity);}
  public static bool Draw(Rect card,ArmorItem item){var r=DistinctEquipmentDesigns.For(item);return r!=null&&Draw(card,r,"armor",item.rarity);}
  public static bool Draw(Rect card,ArtifactItem item){var r=DistinctEquipmentDesigns.For(item);return r!=null&&Draw(card,r,"module",item.rarity);}
  static bool Draw(Rect card,EquipmentRecipe r,string kind,Rarity rarity){
   var s=Load(r,kind,kind=="gun"?3:kind=="armor"?6:2);if(s==null)return false;
   Color old=GUI.color;Frame(card,rarity,r.legendary);var area=new Rect(card.x+7,card.y+7,Mathf.Max(1,card.width-14),Mathf.Max(1,card.height-14));
   if(kind=="gun"){
    int body=r.body,barrel=6+r.barrel,stock=12+r.stock;
    float bh=160/Ratio(s,body),barrelH=115/Ratio(s,barrel),stockH=100/Ratio(s,stock);
    // The front connector lies on the upper receiver; overlap hides assembly seams.
    var b=new Rect(84,-bh*.25f,160,bh);var f=new Rect(b.xMax-16,-barrelH*.5f,115,barrelH);var rear=new Rect(b.x-84,-stockH*.42f,100,stockH);
    Rect bounds=Union(Union(b,f),rear);Part(s,stock,Map(rear,bounds,area));Part(s,barrel,Map(f,bounds,area));Part(s,body,Map(b,bounds,area));
   }else{
    int main=kind=="armor"?r.slot*6+r.body:r.body,extra=(kind=="armor"?30:6)+r.attachment;
    float ratio=Ratio(s,main);var image=area;if(ratio>area.width/area.height){image.height=area.width/ratio;image.y=area.center.y-image.height*.5f;}else{image.width=area.height*ratio;image.x=area.center.x-image.width*.5f;}
    if(kind=="armor")Part(s,main,image);
    var accessory=kind=="armor"?Load(r,"armor-accessories",5):s; if(accessory==null)accessory=s; else if(kind=="armor")extra=r.slot*6+r.attachment;
    float w=image.width*(kind=="armor"?(r.slot==0?.24f:r.slot==1?.48f:.33f):.48f),h=w/Ratio(accessory,extra);h=Mathf.Min(h,image.height*.55f);
    Vector2 mount=image.center;
    if(kind=="armor"){
     // Accessories mount on equipment, leaving the helmet visor and gloves readable.
     float mx=r.slot==0?.80f:r.slot==1?.52f:r.slot==2?.67f:r.slot==3?.50f:.62f;
     float my=r.slot==0?.59f:r.slot==1?.66f:r.slot==2?.33f:r.slot==3?.13f:.57f;
     mount=new Vector2(image.x+image.width*mx,image.y+image.height*my);
    }
    if(kind=="module"){
     // Put the core inside the opening, then the housing rim in front of it.
     if(r.body==2){w=image.width*.29f;h=Mathf.Min(w/Ratio(s,extra),image.height*.60f);for(int side=-1;side<=1;side+=2)Part(s,extra,new Rect(image.center.x+side*image.width*.23f-w*.5f,image.center.y-h*.5f,w,h));}
     else Part(s,extra,new Rect(mount.x-w*.5f,mount.y-h*.5f,w,h));
     Part(s,main,image);
    }else Part(accessory,extra,new Rect(mount.x-w*.5f,mount.y-h*.5f,w,h));
   }
   ManufacturerBranding.Badge(new Rect(card.x+3,card.yMax-Mathf.Min(25,card.height*.3f)-3,Mathf.Min(25,card.width*.3f),Mathf.Min(25,card.height*.3f)),ManufacturerCatalog.Id(r.maker));GUI.color=old;return true;
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){sheets.Clear();warnings.Clear();}
 }
}
