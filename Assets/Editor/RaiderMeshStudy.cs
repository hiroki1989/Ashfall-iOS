using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace NeonVault.Editor {
 public static class RaiderMeshStudy {
  static Material cloth,armor,skin,leather,metal;
  static Material Mat(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",.16f);return m;}
  static GameObject Shape(Transform parent,string name,Vector3[] centers,Vector2[] radii,Material material,int n=24){
   var vertices=new List<Vector3>();var tris=new List<int>();for(int y=0;y<centers.Length;y++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;vertices.Add(centers[y]+new Vector3(Mathf.Cos(a)*radii[y].x,0,Mathf.Sin(a)*radii[y].y));}
   for(int y=0;y<centers.Length-1;y++)for(int i=0;i<n;i++){int a=y*n+i,b=y*n+(i+1)%n,c=a+n,d=b+n;tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(b);tris.Add(c);tris.Add(d);}
   var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
  }
  static void Ellipsoid(Transform p,string name,Vector3 pos,Vector3 size,Material mat){var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(p,false);obj.transform.localPosition=pos;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(obj.GetComponent<Collider>());}
  static void Block(Transform p,string name,Vector3 pos,Vector3 size,Material mat,float angle=0){var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(p,false);obj.transform.localPosition=pos;obj.transform.localScale=size;obj.transform.localRotation=Quaternion.Euler(0,0,angle);obj.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(obj.GetComponent<Collider>());}
  public static void Run(){
   cloth=Mat(new Color(.24f,.25f,.18f));armor=Mat(new Color(.19f,.20f,.18f));skin=Mat(new Color(.50f,.35f,.27f));leather=Mat(new Color(.23f,.15f,.10f));metal=Mat(new Color(.39f,.35f,.29f));var root=new GameObject("Original Unity Raider Study");
   Shape(root.transform,"tailored jacket",new[]{new Vector3(0,1.04f,0),new Vector3(0,1.16f,0),new Vector3(0,1.38f,0),new Vector3(0,1.54f,0),new Vector3(0,1.64f,0)},new[]{new Vector2(.28f,.17f),new Vector2(.31f,.18f),new Vector2(.31f,.19f),new Vector2(.37f,.17f),new Vector2(.23f,.12f)},cloth);
   Ellipsoid(root.transform,"head",new Vector3(0,1.77f,0),new Vector3(.22f,.29f,.21f),skin);Ellipsoid(root.transform,"hair",new Vector3(0,1.89f,-.012f),new Vector3(.24f,.12f,.22f),armor);
   for(int s=-1;s<=1;s+=2){float x=s*.17f;Shape(root.transform,"trouser leg",new[]{new Vector3(x,.15f,0),new Vector3(x,.43f,0),new Vector3(x,.67f,.01f),new Vector3(x,.88f,0),new Vector3(x,1.08f,0)},new[]{new Vector2(.10f,.11f),new Vector2(.12f,.13f),new Vector2(.14f,.14f),new Vector2(.16f,.15f),new Vector2(.15f,.14f)},cloth);Ellipsoid(root.transform,"boot",new Vector3(x,.09f,.06f),new Vector3(.22f,.18f,.34f),leather);
   Shape(root.transform,"sleeve and arm",new[]{new Vector3(s*.45f,1.04f,.14f),new Vector3(s*.49f,1.20f,.09f),new Vector3(s*.43f,1.40f,.01f),new Vector3(s*.34f,1.54f,0)},new[]{new Vector2(.09f,.09f),new Vector2(.10f,.10f),new Vector2(.12f,.11f),new Vector2(.13f,.12f)},cloth);Ellipsoid(root.transform,"hand",new Vector3(s*.45f,1.01f,.15f),new Vector3(.11f,.15f,.09f),skin);Ellipsoid(root.transform,"shoulder plate",new Vector3(s*.35f,1.51f,.01f),new Vector3(.25f,.14f,.24f),armor);Ellipsoid(root.transform,"knee plate",new Vector3(x,.58f,.13f),new Vector3(.22f,.20f,.07f),metal);
   for(int i=0;i<3;i++)Block(root.transform,"belt pouch",new Vector3(s*(.08f+i*.09f),1.04f,.18f),new Vector3(.075f,.12f,.07f),leather);}
   Block(root.transform,"chest armor",new Vector3(0,1.37f,.17f),new Vector3(.47f,.32f,.10f),armor);Block(root.transform,"diagonal harness",new Vector3(0,1.38f,.23f),new Vector3(.07f,.52f,.035f),leather,-34);Block(root.transform,"belt",new Vector3(0,1.08f,.02f),new Vector3(.58f,.09f,.35f),leather);
   var helper=typeof(ImportedEnemyVisual).Assembly.GetType("NeonVault.GeneratedWardenPreviewAssets");var method=helper.GetMethod("CreateFittedMask",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);var socket=new GameObject("mask socket").transform;socket.SetParent(root.transform,false);socket.localPosition=new Vector3(0,1.72f,.10f);method.Invoke(null,new object[]{socket});
   var camera=new GameObject("camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.15f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.10f);camera.nearClipPlane=.01f;var light=new GameObject("light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=Color.gray;Directory.CreateDirectory("D:/Unity/AssetIntegration/RaiderMeshStudy");
   for(int i=0;i<2;i++){camera.transform.position=new Vector3(i==0?0:3,1.3f,5);camera.transform.LookAt(new Vector3(0,1.0f,0));var rt=new RenderTexture(900,1100,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var img=new Texture2D(900,1100,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,900,1100),0,0);img.Apply();File.WriteAllBytes("D:/Unity/AssetIntegration/RaiderMeshStudy/raider-"+i+".png",img.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(img);Object.DestroyImmediate(rt);}
   Debug.Log("RAIDER_MESH_STUDY_CAPTURED");Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);
  }
 }
}
