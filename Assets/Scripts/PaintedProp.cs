using UnityEngine;
using UnityEngine.Rendering;
namespace NeonVault
{
    public class PaintedProp : MonoBehaviour
    {
        static Sprite[] props;
        Material material;
        public static void Attach(Transform parent,int index,float height,bool hideModel=true)
        {
            if(props==null)
            {
                var texture=Resources.Load<Texture2D>("Art/SanctumProps");if(texture==null)return;
                props=new Sprite[8];int w=texture.width/4,h=texture.height/2;
                for(int n=0;n<8;n++)
                {
                    int x=n%4*w,y=(1-n/4)*h,minX=w,minY=h,maxX=0,maxY=0;var pixels=texture.GetPixels(x,y,w,h);
                    for(int py=0;py<h;py++)for(int px=0;px<w;px++)if(pixels[py*w+px].a>.12f){minX=Mathf.Min(minX,px);minY=Mathf.Min(minY,py);maxX=Mathf.Max(maxX,px);maxY=Mathf.Max(maxY,py);}
                    if(maxY<=minY){minX=0;minY=0;maxX=w-1;maxY=h-1;}
                    props[n]=Sprite.Create(texture,new Rect(x+minX,y+minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
                }
            }
            if(hideModel)foreach(var mesh in parent.GetComponentsInChildren<MeshRenderer>())mesh.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            var g=new GameObject("Painted sanctum prop / "+index);g.transform.SetParent(parent,false);g.transform.localPosition=Vector3.up*.02f;
            g.transform.rotation=Quaternion.Euler(35.264f,45,0);g.transform.localScale=Vector3.one*(height/props[index].bounds.size.y);
            var r=g.AddComponent<SpriteRenderer>();r.sprite=props[index];
            r.spriteSortPoint=SpriteSortPoint.Pivot;
            var owner=g.AddComponent<PaintedProp>();owner.material=new Material(Resources.Load<Shader>("VaultSprite"));r.sharedMaterial=owner.material;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
    public class TorchGlow : MonoBehaviour
    {
        Light lamp;
        void Start(){lamp=GetComponent<Light>();}
        void Update(){lamp.intensity=2.5f+Mathf.Sin(Time.time*9+transform.position.x)*.25f+Mathf.Sin(Time.time*17)*.1f;}
    }
}
