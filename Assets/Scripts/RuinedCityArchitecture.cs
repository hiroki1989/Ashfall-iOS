using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonVault {
 public static class RuinedCityArchitecture {
  static readonly Dictionary<int,GameObject> prototypes=new Dictionary<int,GameObject>();
  static Material[] palette;static Mesh cube;
  static void Prepare(){if(palette!=null)return;
   var shader=ImportedWorldAssets.BuiltinStandard();
   string[] names={"M_YFCM_BrushedWorn","M_YFCM_RevealingBricks","M_YFCM_Spalling",null,null};
   Color[] colors={new Color(.48f,.47f,.43f),new Color(.47f,.32f,.26f),new Color(.34f,.36f,.35f),new Color(.08f,.12f,.13f),new Color(.36f,.18f,.095f)};
   palette=new Material[5];for(int i=0;i<5;i++){palette[i]=new Material(shader);var source=names[i]==null?null:Resources.Load<Material>("ImportedWorld/Surfaces/"+names[i]);if(source)palette[i].CopyPropertiesFromMaterial(source);palette[i].shader=shader;palette[i].color=colors[i];palette[i].SetFloat("_Glossiness",i==3?.3f:.08f);palette[i].enableInstancing=true;}
   var box=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=box.GetComponent<MeshFilter>().sharedMesh;Object.Destroy(box);
  }
  static GameObject Prototype(int variant){Prepare();if(prototypes.TryGetValue(variant,out var existing))return existing;
   bool mall=variant==6,shop=variant==7;int floors=shop?2:mall?3:5+variant%4;
   var root=new GameObject(mall?"Ruined shopping mall":shop?"Abandoned storefronts":"Ruined high-rise / "+variant);root.hideFlags=HideFlags.HideAndDontSave;Object.DontDestroyOnLoad(root);
   var batches=new List<CombineInstance>[5];for(int i=0;i<5;i++)batches[i]=new List<CombineInstance>();
   void Box(Vector3 pos,Vector3 size,int mat,float yaw=0){batches[mat].Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(pos,Quaternion.Euler(0,yaw,0),size)});}
   float floorHeight=3.2f;
   for(int floor=0;floor<floors;floor++){
    float y=floor*floorHeight;float width=12;
    // Torn slabs leave a different roof silhouette and expose the structural frame.
    if(floor<floors-2)Box(new Vector3(0,y+.13f,0),new Vector3(width,.26f,8),0);
    else {Box(new Vector3(-3,y+.13f,0),new Vector3(6,.26f,8),0);Box(new Vector3(3,y+.13f,2),new Vector3(6,.26f,4),0);}
    for(int side=-1;side<=1;side+=2){
     for(int panel=0;panel<4;panel++)if(floor<2||(floor+panel+variant)%3!=0)Box(new Vector3(side*5.8f,y+1.6f,-3+panel*2),new Vector3(.42f,3.2f,1.85f),floor==floors-1?1:2);
     for(int stain=0;stain<3;stain++)Box(new Vector3(-4+stain*3.7f,y+2.1f,side*3.84f),new Vector3(.12f+.09f*stain,1.4f,.05f),4);
     Box(new Vector3(0,y+2.85f,side*3.8f),new Vector3(12,.65f,.38f),variant%2==0?0:1);
     for(int bay=0;bay<5;bay++){
      float x=-5.6f+bay*2.8f;if(!(floor==0&&bay==2))Box(new Vector3(x,y+1.6f,side*3.8f),new Vector3(.32f,3.2f,.42f),0);
      if(bay<4&&(floor+bay+variant)%3==0&&floor>0)Box(new Vector3(x+1.2f,y+1.45f,side*3.86f),new Vector3(2.05f,1.9f,.06f),3);
      if(bay<4&&(floor*7+bay+variant)%5==0)Box(new Vector3(x+.8f,y+1.1f,side*3.95f),new Vector3(1.8f,.2f,.12f),4,12);
     }
    }
    if(floor==0&&(mall||shop)){
     for(int store=0;store<3;store++){float x=-4+store*4;Box(new Vector3(x,1.25f,-3.85f),new Vector3(3.4f,2.4f,.08f),3);Box(new Vector3(x,2.7f,-4.1f),new Vector3(3.6f,.55f,.5f),store%2==0?4:2);Box(new Vector3(x,2.3f,-4.6f),new Vector3(3.5f,.12f,1.4f),4);}
    }
   }
   for(int i=0;i<5;i++)Box(new Vector3(-5+i*2.1f,floors*3.2f-.2f,2.9f),new Vector3(.08f,1.5f,.08f),4,8);
   if(mall){Box(new Vector3(0,10.3f,0),new Vector3(6,.25f,5),2);Box(new Vector3(-4,11,2),new Vector3(2,1.5f,1.5f),2);}
   for(int mat=0;mat<5;mat++){var mesh=new Mesh{name=root.name+" / "+mat,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batches[mat].ToArray(),true,true);mesh.RecalculateBounds();var child=new GameObject("Architecture material "+mat);child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=palette[mat];}
   root.SetActive(false);prototypes[variant]=root;return root;
  }
  public static void Place(Transform parent,Rect footprint,int variant){var source=Prototype(variant);ImportedWorldBatch.Add(parent,source,new Vector3(footprint.center.x,0,footprint.center.y),Quaternion.identity,new Vector3(footprint.width/12,1,footprint.height/8),true);}
 }
}
