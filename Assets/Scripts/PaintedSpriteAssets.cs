using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonVault
{
    internal static class PaintedSpriteAssets
    {
        static Material material;
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        public static Material Material
        {
            get { if(material==null){var shader=Resources.Load<Shader>("VaultActorSprite");if(shader==null)shader=Shader.Find("Sprites/Default");material=new Material(shader){name="Shared painted cutout"};}return material; }
        }
        public static Sprite[] Load(string path,int columns,int rows,Rect[] windows=null)
        {
            if(cache.TryGetValue(path,out var sprites))return sprites;
            var texture=Resources.Load<Texture2D>(path);if(texture==null)return null;
            sprites=new Sprite[columns*rows];
            for(int n=0;n<sprites.Length;n++)
            {
                int x=Mathf.RoundToInt(n%columns*texture.width/(float)columns),y=Mathf.RoundToInt((rows-1-n/columns)*texture.height/(float)rows);
                int w=Mathf.RoundToInt((n%columns+1)*texture.width/(float)columns)-x,h=Mathf.RoundToInt((rows-n/columns)*texture.height/(float)rows)-y;
                if(windows!=null){var window=windows[n];x=Mathf.RoundToInt(window.x*texture.width);y=Mathf.RoundToInt(window.y*texture.height);w=Mathf.RoundToInt(window.width*texture.width);h=Mathf.RoundToInt(window.height*texture.height);}
                int minX=0,minY=0,maxX=w-1,maxY=h-1;
                if(texture.isReadable)
                {
                    var pixels=texture.GetPixels(x,y,w,h);minX=w;minY=h;maxX=-1;maxY=-1;
                    for(int py=0;py<h;py++)for(int px=0;px<w;px++)if(pixels[py*w+px].a>.12f){minX=Mathf.Min(minX,px);minY=Mathf.Min(minY,py);maxX=Mathf.Max(maxX,px);maxY=Mathf.Max(maxY,py);}
                    if(maxX<minX){minX=0;minY=0;maxX=w-1;maxY=h-1;}
                }
                sprites[n]=Sprite.Create(texture,new Rect(x+minX,y+minY,maxX-minX+1,maxY-minY+1),new Vector2(Mathf.Clamp01((w*.5f-minX)/(maxX-minX+1)),0),100,0,SpriteMeshType.FullRect);
            }
            cache[path]=sprites;return sprites;
        }
        public static SpriteRenderer Create(Transform parent,string name)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);var r=obj.AddComponent<SpriteRenderer>();r.sharedMaterial=Material;r.shadowCastingMode=ShadowCastingMode.Off;r.spriteSortPoint=SpriteSortPoint.Pivot;return r;
        }
        public static void HideMeshes(Transform root)
        {foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>(true)){mesh.enabled=true;mesh.shadowCastingMode=ShadowCastingMode.ShadowsOnly;}}
    }
}

