#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEngine;
namespace NeonVault.EditorTools
{
    public static class RealCityGeographyReview
    {
        public static void Run()
        {
            var report=new StringBuilder();int failures=0,sites=0,roofs=0;
            var timer=Stopwatch.StartNew();var geo=new OverworldGeography();
            report.AppendLine("construction_ms="+timer.ElapsedMilliseconds);
            void Check(bool ok,string label){if(!ok){failures++;report.AppendLine("FAIL "+label);}}
            foreach(var group in new[]{geo.Camps,geo.Entrances,geo.Chests})foreach(var site in group){sites++;Check(!geo.Blocked(site,.4f),"site "+sites+" "+site);}
            Check(geo.Camps.Length==14&&geo.Entrances.Length==200&&geo.Chests.Length==500,"stable site array lengths");
            for(int bridge=-4;bridge<=4;bridge++)
            {
                float z=bridge*1000,x=geo.RiverX(z),half=geo.RiverWidth(z)*.5f;
                foreach(float dz in new[]{-25f,0f,25f})for(float a=x-half-30;a<=x+half+30;a+=2)
                    Check(!geo.Blocked(new Vector3(a,0,z+dz),.4f),"bridge corridor "+bridge+" "+a+","+dz);
                var left=geo.Layout.ToCell(new Vector3(x-half-70,0,z));var right=geo.Layout.ToCell(new Vector3(x+half+70,0,z));
                Check(geo.Layout.IsOpen(left.x,left.y)&&geo.Layout.IsOpen(right.x,right.y),"bridge bank nav cells "+bridge);
            }
            timer.Restart();float minWidth=float.MaxValue,maxWidth=0,minDepth=float.MaxValue,maxDepth=0;
            for(int x=-42;x<=42;x+=7)for(int z=-42;z<=42;z+=7)
            {
                var chunk=geo.GetChunk(x,z);
                foreach(var roof in chunk.Buildings)
                {
                    roofs++;minWidth=Mathf.Min(minWidth,roof.width);maxWidth=Mathf.Max(maxWidth,roof.width);minDepth=Mathf.Min(minDepth,roof.height);maxDepth=Mathf.Max(maxDepth,roof.height);
                    for(float a=roof.xMin;a<=roof.xMax+2;a+=2)for(float b=roof.yMin;b<=roof.yMax+2;b+=2)
                    {
                        var p=new Vector3(Mathf.Min(a,roof.xMax),0,Mathf.Min(b,roof.yMax));
                        Check(!geo.River(p)&&geo.RoadClearance(p)>=1.4f,"roof encroachment "+p);
                    }
                }
            }
            Check(roofs>100&&maxWidth-minWidth>8&&maxDepth-minDepth>8,"varied building dimensions");
            int roadSamples=0,diagonalSamples=0;
            for(int x=-45;x<=45;x+=5)for(int z=-45;z<=45;z+=5)
                foreach(var road in geo.GetRoadSegmentsForChunk(x,z))
                {
                    var heading=road.B-road.A;float length=heading.magnitude;
                    Check(length>.01f&&length<(road.BridgeDeck?1000:100),"finite road segment length");
                    Check(geo.RoadClearance((road.A+road.B)*.5f)<=-road.Width*.5f+.1f,"shared road bucket distance");
                    if(Mathf.Abs(heading.x)>length*.2f&&Mathf.Abs(heading.z)>length*.2f)diagonalSamples++;
                    roadSamples++;
                }
            Check(roadSamples>100&&diagonalSamples>roadSamples*.2f,"non-grid finite road graph");
            report.AppendLine("network_segments="+geo.RoadSegmentCount+" road_samples="+roadSamples+" diagonal="+diagonalSamples);
            Check(geo.CachedChunkCount<=49,"bounded chunk cache");
            report.AppendLine("sites="+sites+" roofs="+roofs+" sampled_chunk_ms="+timer.ElapsedMilliseconds);
            report.AppendLine("roof_width_range="+minWidth+".."+maxWidth+" depth="+minDepth+".."+maxDepth);
            report.AppendLine("cache="+geo.CachedChunkCount+" failures="+failures);
            Directory.CreateDirectory("D:/Unity/QA-premium-ui");File.WriteAllText("D:/Unity/QA-premium-ui/geography.txt",report.ToString());
            if(failures>0)throw new InvalidOperationException("Geography review failed: "+failures);
            UnityEngine.Debug.Log("REAL_CITY_GEOGRAPHY_REVIEW_COMPLETED "+report);
        }
    }
}
#endif
