using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Transient state checks only: no scene, PlayerPrefs or save data are written.
public static class TreeOcclusionReview
{
    const string Output="D:/Unity/BuildTemp/TreeOcclusionReview/result.txt";
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    static readonly List<string> errors=new List<string>();
    static int checks;
    static void Check(bool value,string name){checks++;if(!value)errors.Add(name);}
    static FieldInfo Field(Type type,string name,BindingFlags flags=Instance)=>type.GetField(name,flags)??throw new MissingFieldException(type.FullName,name);
    static MethodInfo Method(Type type,string name,BindingFlags flags=Instance)=>type.GetMethod(name,flags)??throw new MissingMethodException(type.FullName,name);
    static List<bool> Hidden(ImportedWorldBatch b)=>(List<bool>)Field(typeof(ImportedWorldBatch),"hidden").GetValue(b);
    static void Refresh(ImportedWorldBatch b){Field(typeof(ImportedWorldBatch),"nextOcclusion").SetValue(b,-1f);b.DrawNow();}
    public static void Run()
    {
        checks=0;errors.Clear();
        GameObject root=null,source=null,view=null;Material material=null;
        object previousGame=null;bool previousEnabled=ImportedWorldBatch.OcclusionEnabled;
        try
        {
            root=new GameObject("Isolated tree occlusion review");root.SetActive(false);
            var game=root.AddComponent<VaultGame>();
            var player=new GameObject("Player marker");player.transform.SetParent(root.transform,false);
            view=new GameObject("Review camera");var camera=view.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.aspect=2f;camera.transform.position=new Vector3(0,8,-14);camera.transform.LookAt(new Vector3(0,0,0));
            Field(typeof(VaultGame),"player").SetValue(game,player.transform);Field(typeof(VaultGame),"cam").SetValue(game,camera);
            var global=Field(typeof(ImportedWorldBatch),"game",Static);previousGame=global.GetValue(null);global.SetValue(null,game);
            ImportedWorldBatch.OcclusionEnabled=true;
            var parent=new GameObject("Review instances");parent.transform.SetParent(root.transform,false);
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
            var shader=Shader.Find("Standard")??Shader.Find("Sprites/Default");if(!shader)throw new InvalidOperationException("No standard shader");
            material=new Material(shader){enableInstancing=true};
            source=new GameObject("Tree template");
            void Part(string name,Vector3 position,Vector3 scale){var part=new GameObject(name);part.transform.SetParent(source.transform,false);part.transform.localPosition=position;part.transform.localScale=scale;part.AddComponent<MeshFilter>().sharedMesh=cube;part.AddComponent<MeshRenderer>().sharedMaterial=material;}
            Part("trunk",new Vector3(0,1,0),new Vector3(.5f,2,.5f));Part("canopy",new Vector3(0,3,0),new Vector3(3,2,3));
            ImportedWorldBatch.Add(parent.transform,source,new Vector3(0,0,-4),Quaternion.identity,Vector3.one,tree:true);
            var batch=parent.GetComponent<ImportedWorldBatch>();var kinds=(List<bool>)Field(typeof(ImportedWorldBatch),"treeOwner").GetValue(batch);
            var bounds=(List<Bounds>)Field(typeof(ImportedWorldBatch),"buildingBounds").GetValue(batch);
            Check(bounds.Count==1&&kinds.Count==1&&kinds[0],"imported tree has a single tree owner");
            var groups=(IDictionary)Field(typeof(ImportedWorldBatch),"groups").GetValue(batch);int treeParts=0;
            foreach(var group in groups.Values){var owners=(List<int>)group.GetType().GetField("owners").GetValue(group);foreach(int owner in owners)if(owner==0)treeParts++;}
            Check(treeParts==2,"trunk and canopy both use the same fade owner");
            int building=batch.RegisterBuilding(new Bounds(new Vector3(7,2.5f,-4),new Vector3(4,5,4)));
            Check(!kinds[building],"building remains a building owner");
            batch.BeginBuildingDecoration(Vector3.zero);
            Check((int)Field(typeof(ImportedWorldBatch),"decorationOwner").GetValue(batch)==building,"decorations never attach to a nearby tree");batch.EndBuildingDecoration();
            player.transform.position=new Vector3(0,0,2);Refresh(batch);
            Check(Hidden(batch)[0],"tree fades when between camera and player");
            Check(!Hidden(batch)[building],"unrelated building remains opaque");
            var faded=(Material)Method(typeof(ImportedWorldBatch),"Faded",Static).Invoke(null,new object[]{material});
            Check(faded&&faded.shader.name=="NeonVault/BuildingGhost"&&Mathf.Abs(faded.color.a-.4f)<.001f,"shared fade material opacity is 40 percent");
            player.transform.position=new Vector3(0,0,-8);Refresh(batch);
            Check(!Hidden(batch)[0],"tree behind player stays opaque");
            player.transform.position=new Vector3(5,0,2);Refresh(batch);
            Check(!Hidden(batch)[0],"tree restores when player moves clear");
            player.transform.position=new Vector3(7,0,2);Refresh(batch);
            Check(Hidden(batch)[building],"building fades independently when it hides player");
            Check(!Hidden(batch)[0],"tree stays opaque when building hides player");
            ImportedWorldBatch.OcclusionEnabled=false;player.transform.position=new Vector3(0,0,2);Refresh(batch);
            Check(!Hidden(batch)[0]&&!Hidden(batch)[building],"disabling occlusion restores both kinds");
        }
        catch(Exception e){errors.Add(e.ToString());}
        finally
        {
            ImportedWorldBatch.OcclusionEnabled=previousEnabled;
            Field(typeof(ImportedWorldBatch),"game",Static).SetValue(null,previousGame);
            if(root)UnityEngine.Object.DestroyImmediate(root);if(source)UnityEngine.Object.DestroyImmediate(source);if(view)UnityEngine.Object.DestroyImmediate(view);if(material)UnityEngine.Object.DestroyImmediate(material);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Output));string report="checks="+checks+" failures="+errors.Count+"\n"+string.Join("\n",errors);
        File.WriteAllText(Output,report);Debug.Log("TREE_OCCLUSION_REVIEW "+report);
        if(Application.isBatchMode)EditorApplication.Exit(errors.Count==0?0:1);
    }
}
