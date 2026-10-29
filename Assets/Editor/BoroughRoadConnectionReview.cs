using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;
public static class BoroughRoadConnectionReview
{
 static float Distance(Vector3 p,OverworldGeography.RoadSegment road){var d=road.B-road.A;float t=Mathf.Clamp01(Vector3.Dot(p-road.A,d)/d.sqrMagnitude);return Vector3.Distance(p,road.A+d*t);}
 static bool Near(OverworldGeography g,Vector3 p,float minWidth){int x=Mathf.FloorToInt(p.x/100),z=Mathf.FloorToInt(p.z/100);for(int i=x-1;i<=x+1;i++)for(int j=z-1;j<=z+1;j++)foreach(var road in g.GetRoadSegmentsForChunk(i,j))if(road.Width>=minWidth&&Distance(p,road)<.1f)return true;return false;}
 public static void Run(){try{var g=new OverworldGeography();int count=0,samples=0;foreach(var road in g.BoroughConnectors){if(!Near(g,road.A,14)||!Near(g,road.B,19))throw new Exception("Connector endpoints do not meet loop/arterial: "+road.A+" to "+road.B);for(int i=0;i<=8;i++){var p=Vector3.Lerp(road.A,road.B,i/8f);if(g.River(p)||g.Blocked(p,.3f))throw new Exception("Connector blocked at "+p);if(g.RoadClearance(p)>0.1f)throw new Exception("Connector missing from shared road graph at "+p);samples++;}count++;}if(count<15)throw new Exception("Too few borough connectors: "+count);File.WriteAllText("D:/Unity/borough-road-review.txt","PASS connectors="+count+" samples="+samples);Debug.Log("BOROUGH_ROADS_OK connectors="+count+" samples="+samples);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
