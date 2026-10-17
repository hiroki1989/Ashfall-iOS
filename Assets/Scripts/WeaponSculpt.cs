using UnityEngine;
using System.Collections.Generic;
namespace NeonVault {
public sealed partial class VaultArt {
Transform GunProfile(string name,Transform parent,Color color,float width,params Vector2[] outline)
{
 var t=Part(name,PrimitiveType.Cube,Vector3.zero,Vector3.one,color,parent);
 var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
 int count=outline.Length;float area=0,minZ=float.MaxValue,maxZ=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
 for(int i=0;i<count;i++){var p=outline[i];var q=outline[(i+1)%count];area+=p.x*q.y-q.x*p.y;minZ=Mathf.Min(minZ,p.x);maxZ=Mathf.Max(maxZ,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
 var polygon=new List<int>();for(int i=0;i<count;i++)polygon.Add(area>0?i:count-1-i);
 var caps=new List<int>();int guard=0;
 float Cross(Vector2 a,Vector2 b,Vector2 c)=> (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
 while(polygon.Count>3&&guard++<count*count)
 {
  bool clipped=false;
  for(int i=0;i<polygon.Count;i++)
  {
   int ia=polygon[(i+polygon.Count-1)%polygon.Count],ib=polygon[i],ic=polygon[(i+1)%polygon.Count];var pa=outline[ia];var pb=outline[ib];var pc=outline[ic];
   if(Cross(pa,pb,pc)<=.000001f)continue;bool inside=false;
   foreach(int j in polygon)if(j!=ia&&j!=ib&&j!=ic&&Cross(pa,pb,outline[j])>=0&&Cross(pb,pc,outline[j])>=0&&Cross(pc,pa,outline[j])>=0){inside=true;break;}
   if(inside)continue;caps.AddRange(new[]{ia,ib,ic});polygon.RemoveAt(i);clipped=true;break;
  }
  if(!clipped)break;
 }
 if(polygon.Count==3)caps.AddRange(polygon);
 for(int side=0;side<2;side++)
 {
  foreach(var p in outline){vertices.Add(new Vector3((side==0?-1:1)*width*.5f,p.y,p.x));uv.Add(new Vector2((p.x-minZ)/Mathf.Max(.001f,maxZ-minZ),(p.y-minY)/Mathf.Max(.001f,maxY-minY)));}
  for(int i=0;i<caps.Count;i+=3){int k=side*count;triangles.AddRange(side==0?new[]{k+caps[i],k+caps[i+1],k+caps[i+2]}:new[]{k+caps[i],k+caps[i+2],k+caps[i+1]});}
 }
 for(int i=0;i<count;i++)
 {
  int j=(i+1)%count;int k=vertices.Count;var p=outline[i];var q=outline[j];
  vertices.AddRange(new[]{new Vector3(-width*.5f,p.y,p.x),new Vector3(width*.5f,p.y,p.x),new Vector3(width*.5f,q.y,q.x),new Vector3(-width*.5f,q.y,q.x)});
  uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
  triangles.AddRange(area>0?new[]{k,k+2,k+1,k,k+3,k+2}:new[]{k,k+1,k+2,k,k+2,k+3});
 }
 var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();t.GetComponent<MeshFilter>().sharedMesh=mesh;TrackMesh(t,mesh);return t;
}
Transform GunTube(string name,Transform g,float z,float length,float radius,Color color,float inner=.65f,float endRadius=-1)
{
 var t=Part(name,PrimitiveType.Cylinder,new Vector3(0,0,z),Vector3.one,color,g);const int N=48;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();if(endRadius<0)endRadius=radius;
 for(int surface=0;surface<4;surface++)
 {
  int k=vertices.Count;int ra=surface==0?0:surface==1?1:surface==2?3:2;int rb=surface==0?1:surface==1?3:surface==2?2:0;
  for(int row=0;row<2;row++)for(int i=0;i<=N;i++){int ring=row==0?ra:rb;float a=i*Mathf.PI*2/N;float r=(ring%2==0?radius:endRadius)*(ring<2?1:inner);vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,ring%2==0?0:length));uv.Add(new Vector2(i/(float)N,row));}
  for(int i=0;i<N;i++){int a=k+i,b=a+1,c=b+N+1,d=a+N+1;triangles.AddRange(new[]{a,b,c,a,c,d});}
 }
 var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();t.GetComponent<MeshFilter>().sharedMesh=mesh;TrackMesh(t,mesh);return t;
}
void BuildPlayerCarbine(Transform g)
{
 SculptGun(g,0,1.10f);
 GunTube("Steel barrel",g,.58f,.52f,.045f,Stone,.58f);
 GunTube("Machined muzzle brake",g,1.02f,.08f,.068f,Stone,.46f);
 var foregrip=GunProfile("Wooden wrapped foregrip",g,Bronze,.14f,new Vector2(.35f,-.045f),new Vector2(.65f,-.04f),new Vector2(.66f,-.09f),new Vector2(.39f,-.12f));
 foregrip.localPosition=new Vector3(-.035f,0,0);
 for(int i=0;i<7;i++)
 {
  float z=.40f+i*.034f;
  Filigree("Canvas foregrip binding",g,new Color(.39f,.37f,.25f),.007f,new Vector3(-.10f,-.025f,z),new Vector3(-.105f,-.115f,z+.012f),new Vector3(.045f,-.12f,z+.017f),new Vector3(.05f,-.027f,z+.022f));
 }
 for(int side=-1;side<=1;side+=2)
 {
  Part("Steel receiver inset",PrimitiveType.Cube,new Vector3(side*.079f,.018f,.38f),new Vector3(.007f,.07f,.20f),Dark,g);
  Part("Steel receiver rail",PrimitiveType.Cube,new Vector3(side*.086f,.062f,.38f),new Vector3(.014f,.012f,.22f),Stone,g);
  Part("Steel selector lever",PrimitiveType.Cube,new Vector3(side*.088f,-.028f,.20f),new Vector3(.012f,.012f,.061f),Bronze,g).localRotation=Quaternion.Euler(28,0,0);
  for(int i=0;i<4;i++)Part("Steel flush rivet",PrimitiveType.Sphere,new Vector3(side*.085f,.035f,.20f+i*.087f),Vector3.one*.013f,Bronze,g);
  for(int i=0;i<5;i++)Part("Steel barrel jacket cooling slot",PrimitiveType.Cube,new Vector3(side*.047f,.015f,.72f+i*.054f),new Vector3(.006f,.023f,.025f),Dark,g);
 }
 for(int i=0;i<5;i++)Filigree("Canvas stock binding",g,new Color(.35f,.33f,.23f),.006f,new Vector3(-.067f,.015f,-.24f+i*.032f),new Vector3(-.068f,-.16f,-.23f+i*.032f),new Vector3(.068f,-.16f,-.22f+i*.032f),new Vector3(.068f,.015f,-.21f+i*.032f));
 Part("Rubber shoulder stock pad",PrimitiveType.Cube,new Vector3(0,-.064f,-.285f),new Vector3(.135f,.205f,.024f),Dark,g);
 GunTube("Steel optic housing",g,.22f,.22f,.039f,Dark,.70f).localPosition+=Vector3.up*.14f;
 Part("Scope lens",PrimitiveType.Sphere,new Vector3(0,.14f,.441f),new Vector3(.053f,.053f,.007f),new Color(.12f,.20f,.16f),g);
 Part("Steel optic mount",PrimitiveType.Cube,new Vector3(0,.10f,.30f),new Vector3(.06f,.045f,.13f),Stone,g);
 Filigree("Steel trigger guard",g,Stone,.009f,new Vector3(.10f,-.075f,.15f),new Vector3(.10f,-.19f,.17f),new Vector3(.10f,-.185f,.29f),new Vector3(.10f,-.075f,.31f));
 Part("Steel curved trigger",PrimitiveType.Cube,new Vector3(.10f,-.12f,.25f),new Vector3(.015f,.046f,.012f),Dark,g).localRotation=Quaternion.Euler(-18,0,0);
}
void SculptGun(Transform g,int n,float muzzle){
 GunProfile("Salvaged receiver",g,Stone,n==6?.23f:.15f,new Vector2(.08f,-.06f),new Vector2(.13f,.08f),new Vector2(.37f,.10f),new Vector2(.53f,.06f),new Vector2(.62f,-.04f),new Vector2(.43f,-.105f),new Vector2(.22f,-.09f));
 GunProfile("Wooden carved shoulder stock",g,Bronze*.65f,.12f,new Vector2(-.285f,-.17f),new Vector2(-.285f,.025f),new Vector2(-.20f,.065f),new Vector2(.12f,.025f),new Vector2(.20f,-.035f),new Vector2(-.08f,-.09f),new Vector2(-.20f,-.20f));
 GunProfile("Leather pistol grip",g,Bronze*.5f,.11f,new Vector2(.16f,-.065f),new Vector2(.29f,-.08f),new Vector2(.24f,-.28f),new Vector2(.11f,-.26f)).localPosition=new Vector3(.1f,0,0);
 if(n==0||n==1)GunProfile("Curved ribbed magazine",g,Stone,.10f,new Vector2(.36f,-.08f),new Vector2(.48f,-.09f),new Vector2(.50f,-.22f),new Vector2(.59f,-.34f),new Vector2(.50f,-.40f),new Vector2(.39f,-.28f));
 if(n==3||n==5){var lower=GunTube("Lower shotgun tube",g,.48f,muzzle-.48f,.055f,Stone);lower.localPosition+=Vector3.down*.11f;for(int i=0;i<10;i++){var p=GunTube("Walnut pump ribs",g,.51f+i*.027f,.02f,.085f,Bronze);p.localPosition+=Vector3.down*.105f;}}
 if(n==4){var drum=GunTube("Rotary shell drum",g,.32f,.16f,.19f,Stone);drum.localPosition+=Vector3.down*.17f;for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var shell=GunTube("Drum brass cartridge",g,.32f,.16f,.023f,Gold);shell.localPosition+=new Vector3(Mathf.Cos(a)*.20f,-.17f+Mathf.Sin(a)*.20f,0);}}
 if(n==6){Part("Olive ammunition box",PrimitiveType.Cube,new Vector3(-.18f,-.20f,.40f),new Vector3(.22f,.30f,.25f),new Color(.25f,.27f,.16f),g);for(int i=0;i<11;i++){var shell=GunTube("Linked ammunition belt",g,.34f,.18f,.024f,Gold);shell.localPosition+=new Vector3(-.26f-Mathf.Sin(i*.21f)*.07f,.08f-i*.03f,0);}for(int s=-1;s<=1;s+=2)Filigree("Deployed steel bipod",g,Stone,.022f,new Vector3(s*.07f,-.05f,.82f),new Vector3(s*.21f,-.43f,.98f));}
 if(n==7||n==11||n==9){GunTube("Isotope pressure chamber",g,.46f,.36f,.105f,Jade);for(int i=0;i<4;i++){float a=i*Mathf.PI/2;Filigree("Pressure chamber cage",g,Stone,.018f,new Vector3(Mathf.Cos(a)*.12f,Mathf.Sin(a)*.12f,.43f),new Vector3(Mathf.Cos(a)*.12f,Mathf.Sin(a)*.12f,.85f));}}
 if(n==8){var path=new Vector3[241];for(int i=0;i<path.Length;i++){float a=i*.28f;path[i]=new Vector3(Mathf.Cos(a)*.13f,Mathf.Sin(a)*.13f,.49f+i*.0024f);}Filigree("Continuous copper induction winding",g,Bronze,.015f,path);}
 if(n==9)for(int s=-1;s<=1;s+=2){var jaw=GunProfile("Machined split jaw",g,Bronze,.065f,new Vector2(.73f,-.12f),new Vector2(.88f,-.17f),new Vector2(1.1f,-.13f),new Vector2(1.1f,-.07f),new Vector2(.87f,-.065f),new Vector2(.79f,.07f));jaw.localPosition=new Vector3(s*.13f,0,0);}
 if(n==10){GunTube("Isotope drone cylinder",g,.42f,.64f,.21f,Stone);for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;var tube=GunTube("Drone launch bore",g,1.05f,.05f,.065f,Bronze);tube.localPosition+=new Vector3(Mathf.Cos(a)*.12f,Mathf.Sin(a)*.12f,0);}}
 if(n==11)GunTube("Flared vacuum intake",g,.88f,.30f,.07f,Bronze,.87f,.25f);
 if(n==7)GunTube("Injection needle",g,.88f,.22f,.013f,Stone,.4f,.003f);
}
}}
