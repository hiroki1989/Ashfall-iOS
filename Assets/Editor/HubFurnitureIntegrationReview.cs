using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;
public static class HubFurnitureIntegrationReview {
 public static void Run(){
  const string path="D:/Unity/BuildTemp/HubFurnitureIntegrationReview.txt";
  try {
   var layout=new SectorLayout(new System.Random(17017));Array.Clear(layout.open,0,layout.open.Length);
   for(int x=0;x<layout.Columns;x++)for(int y=0;y<layout.Rows;y++){var p=layout.Center(new Vector2Int(x,y));if(new Vector2(p.x,p.z).magnitude<13.5f)layout.open[x,y]=true;}
   layout.walls.Clear();layout.covers.Clear();var root=new GameObject("Hub integration review");var owned=new List<Material>();var art=new VaultArt(root.transform,owned,new System.Random(17017));art.Floor(16,15,0);
   ImportedWorldAssets.DressInterior(root.transform,layout,true);
   int offices=0,walls=0,bad=0;foreach(var tr in root.GetComponentsInChildren<Transform>()){
    if(tr.name.StartsWith("Safehouse station / ")&&(tr.name.Contains("office_")||tr.name.Contains("sci_chair")))offices++;
    if(tr.name=="Safehouse perimeter wall")walls++;
    if(tr.name.Contains("Warehouse")||tr.name.Contains("house_ruined"))bad++;
   }
   if(offices<2||walls!=3||bad!=0)throw new Exception("offices="+offices+" walls="+walls+" bad="+bad);
   foreach(var p in new[]{new Vector3(-8,0,6),new Vector3(8,0,5),new Vector3(-3,0,8),new Vector3(0,0,11)}){
    var c=layout.ToCell(p);if(!layout.IsOpen(c.x,c.y))throw new Exception("Important hub position blocked: "+p);
   }
   File.WriteAllText(path,"PASS offices="+offices+" walls="+walls+" interaction points open");UnityEngine.Object.DestroyImmediate(root);foreach(var mat in owned)if(mat)UnityEngine.Object.DestroyImmediate(mat);EditorApplication.Exit(0);
  }catch(Exception e){File.WriteAllText(path,"FAIL "+e);Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
