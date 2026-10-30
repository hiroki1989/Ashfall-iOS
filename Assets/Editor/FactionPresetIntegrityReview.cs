using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NeonVault;

[InitializeOnLoad]
public static class FactionPresetIntegrityReview
{
    const string Flag="FactionPresetIntegrityReview.pending";
    static readonly List<GameObject> actors=new List<GameObject>();
    static int stage;
    static FactionPresetIntegrityReview(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
    public static void Run(){SessionState.SetBool(Flag,true);EditorApplication.update+=Tick;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            if(stage==0)
            {
                for(int f=0;f<3;f++)for(int v=0;v<2;v++)
                {
                    var prefab=Resources.Load<HumanFactionVisual>("FactionPresets/faction_"+f+"_"+v);
                    if(!prefab)throw new Exception("Missing preset "+f+"/"+v);
                    for(int n=0;n<2;n++)
                    {
                        var actor=new GameObject("preset QA "+f+"/"+v+"/"+n);actors.Add(actor);
                        var visual=HumanFactionVisual.Create(actor.transform,f,v);
                        Check(visual);
                        actor.transform.position=new Vector3(1,0,1);
                    }
                }
                for(int i=0;i<actors.Count;i+=2)UnityEngine.Object.Destroy(actors[i]);
                stage=1; return;
            }
            for(int i=1;i<actors.Count;i+=2) Check(actors[i].GetComponentInChildren<HumanFactionVisual>());
            Debug.Log("FACTION_PRESET_INTEGRITY_OK: six presets, bones, weapons and surviving shared resources");
            SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
    }
    static void Check(HumanFactionVisual visual)
    {
        if(!visual||!visual.Body||!visual.Muzzle)throw new Exception("Missing body or muzzle");
        if(!visual.Body.transform.IsChildOf(visual.transform)||!visual.Muzzle.IsChildOf(visual.transform))throw new Exception("Cross-instance body or muzzle");
        var body=visual.Body;
        foreach(var bone in new[]{body.head,body.chest,body.pelvis,body.rightUpperArm,body.rightForearm,body.rightHand,body.leftUpperArm,body.leftForearm,body.leftHand,body.rightThigh,body.rightShin,body.rightFoot,body.leftThigh,body.leftShin,body.leftFoot})
            if(!bone||!bone.IsChildOf(visual.transform))throw new Exception("Missing or cross-instance pose bone");
        int visible=0;
        foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
            visible++;
            foreach(var material in renderer.sharedMaterials)if(!material)throw new Exception("Missing material: "+renderer.name);
            if(renderer is SkinnedMeshRenderer skin)
            {
                if(!skin.sharedMesh)throw new Exception("Missing skin mesh");
                foreach(var bone in skin.bones)if(!bone||!bone.IsChildOf(visual.transform))throw new Exception("Missing or cross-instance skin bone: "+renderer.name);
            }
            else if(renderer is MeshRenderer) {var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)throw new Exception("Missing mesh: "+renderer.name);}
        }
        if(visible==0)throw new Exception("Invisible preset");
    }
}
