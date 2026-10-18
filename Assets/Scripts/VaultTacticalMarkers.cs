using UnityEngine;
namespace NeonVault
{
    public partial class VaultGame
    {
        bool MapRevealed(Vector3 position)
        {var cell=Layout.ToCell(position);return Layout.IsExplored(cell.x,cell.y);}
        Vector2 MapLocation(Vector3 position,Vector2 origin,float tile)
        {var cell=Layout.ToCell(position);return origin+new Vector2((cell.x+.5f)*tile,(Layout.Rows-.5f-cell.y)*tile);}
        void DrawTacticalMapMarkers(Vector2 origin,float tile,float size)
        {
            foreach(var foe in Enemies)if(foe!=null&&!foe.dead&&MapRevealed(foe.transform.position))
            {var p=MapLocation(foe.transform.position,origin,tile);Box(new Rect(p.x-size*.4f,p.y-size*.4f,size*.8f,size*.8f),new Color(1,.12f,.12f));}
            foreach(var d in drops)if(d.gun.rarity==Rarity.Legendary)DrawLegendaryMapStar(d.position,origin,tile,size);
            foreach(var d in armorDrops)if(d.item.rarity==Rarity.Legendary)DrawLegendaryMapStar(d.position,origin,tile,size);
            foreach(var d in artifactDrops)if(d.item.rarity==Rarity.Legendary)DrawLegendaryMapStar(d.position,origin,tile,size);
            foreach(var d in meleeDrops)if(d.item.rarity==Rarity.Legendary)DrawLegendaryMapStar(d.position,origin,tile,size);
        }
        void DrawLegendaryMapStar(Vector3 position,Vector2 origin,float tile,float size)
        {
            if(!MapRevealed(position))return;
            Vector2 center=MapLocation(position,origin,tile);
            if(legendaryMapIcon==null)legendaryMapIcon=CreateLegendaryMapIcon();
            GUI.color=Color.white;GUI.DrawTexture(new Rect(center.x-size-2,center.y-size-2,size*2+4,size*2+4),legendaryMapIcon);
        }
        static Texture2D legendaryMapIcon;
        static Texture2D CreateLegendaryMapIcon()
        {
            const int width=48;var mask=new bool[width*width];var points=new Vector2[10];
            for(int i=0;i<10;i++){float a=(-90+i*36)*Mathf.Deg2Rad;points[i]=new Vector2(24,24)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?21:9);}
            for(int y=0;y<width;y++)for(int x=0;x<width;x++)
            {bool inside=false;for(int i=0,j=9;i<10;j=i++)if((points[i].y>y)!=(points[j].y>y)&&x<(points[j].x-points[i].x)*(y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;mask[y*width+x]=inside;}
            var colors=new Color[width*width];
            for(int y=0;y<width;y++)for(int x=0;x<width;x++)
            {bool edge=false;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int px=x+dx,py=y+dy;if(px>=0&&py>=0&&px<width&&py<width&&mask[py*width+px])edge=true;}colors[(width-1-y)*width+x]=mask[y*width+x]?new Color(1,.46f,.035f):edge?Color.black:Color.clear;}
            var texture=new Texture2D(width,width,TextureFormat.RGBA32,false){name="Legendary minimap star",filterMode=FilterMode.Bilinear};texture.SetPixels(colors);texture.Apply(false,true);return texture;
        }
    }
}
