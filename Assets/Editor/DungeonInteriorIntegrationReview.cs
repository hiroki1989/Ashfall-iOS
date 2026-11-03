using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;

public static class DungeonInteriorIntegrationReview
{
    const string Output = "D:/Unity/BuildTemp/DungeonInteriorIntegrationReview";
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        try {
            for (int theme=0; theme<3; theme++) for (int seed=0; seed<128; seed++) foreach (bool compact in new[]{false,true}) {
                var l=new SectorLayout(new System.Random(seed*7919+theme*31),theme,compact);
                Check(l.Connected(),"Disconnected layout theme="+theme+" seed="+seed+" compact="+compact);
                Check(l.ExitPathCells>0 && !l.Blocked(l.Entry,.45f) && !l.Blocked(l.Exit,.45f),"Entrance/exit blocked theme="+theme+" seed="+seed);
                foreach(var cover in l.covers) { Check(l.Blocked(cover,.45f),"Cover is not blocked"); }
            }
            foreach(string n in new[]{"office_desk","office_shelf","office_drawer","old_terminal","office_printer","office_simple_desk","sci_shelf","sci_chair"}) {
                var prefab=Resources.Load<GameObject>("ImportedWorld/OfficeSalvage/"+n);
                Check(prefab!=null && prefab.GetComponentsInChildren<Renderer>(true).Length>0,"Missing office prefab "+n);
            }
            for(int style=0;style<3;style++) Capture(style);
            File.WriteAllText(Output+"/result.txt","PASS checks="+checks+" layouts=768 styles=3\n");
            Debug.Log("DUNGEON_INTERIOR_INTEGRATION_PASS checks="+checks);
            EditorApplication.Exit(0);
        } catch(Exception e) { File.WriteAllText(Output+"/result.txt","FAIL checks="+checks+"\n"+e); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Capture(int style)
    {
        var l=new SectorLayout(new System.Random(100+style),style,true);
        var root=new GameObject("Dungeon review"); var mats=new List<Material>(); var art=new VaultArt(root.transform,mats,new System.Random(100+style));
        art.Floor(l.Columns*SectorLayout.Cell/2,l.Rows*SectorLayout.Cell/2,style);
        foreach(var r in l.walls) art.Partition(r);
        foreach(var p in l.covers) art.Cover(p);
        for(int i=0;i<l.chambers.Count;i++) art.Chamber(l.Center(l.chambers[i]),i);
        ImportedWorldAssets.DressInterior(root.transform,l,false,style);
        int count=0;Vector3 focus=l.Center(l.chambers[0]);
        foreach(var t in root.GetComponentsInChildren<Transform>(false)) {
            if(!t.name.StartsWith("Imported cover /")) continue;
            var renderers=t.GetComponentsInChildren<Renderer>(false);Check(renderers.Length>0,"Cover missing renderer");
            var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            Check(b.size.x<=1.93f && b.size.z<=1.93f,"Cover out of blocked cell: "+t.name+" "+b.size);
            Check(Mathf.Abs(b.min.y+.06f)<.03f,"Floating cover: "+t.name+" y="+b.min.y);
            var cell=l.ToCell(b.center);Check(!l.IsOpen(cell.x,cell.y),"Cover in walkable cell: "+t.name);
            count++;if(t.name.Contains("office") || t.name.Contains("terminal") || t.name.Contains("sci_"))focus=b.center;
        }
        Check(count>0,"No imported covers style="+style);
        var cameraObject=new GameObject("Dungeon camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=13;camera.backgroundColor=new Color(.07f,.075f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.rotation=Quaternion.Euler(55,35,0);camera.transform.position=focus-camera.transform.forward*45;camera.nearClipPlane=.1f;camera.farClipPlane=150;
        var sunObject=new GameObject("Dungeon review light");var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(45,-30,0);sun.intensity=1.2f;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);
        var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Output+"/style-"+style+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(sunObject);UnityEngine.Object.DestroyImmediate(root);foreach(var m in mats)if(m)UnityEngine.Object.DestroyImmediate(m);
    }
}
