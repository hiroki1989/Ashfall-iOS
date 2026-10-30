using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;

// Builds persistent, self-contained prefabs from the approved runtime equipment.
[InitializeOnLoad]
public static class FactionPresetBaker
{
    const string Flag="FactionPresetBaker.pending";
    const string Root="Assets/Resources/FactionPresets";
    static FactionPresetBaker(){if(SessionState.GetBool(Flag,false))EditorApplication.update+=Tick;}
    public static void Run(){SessionState.SetBool(Flag,true);EditorApplication.update+=Tick;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        EditorApplication.update-=Tick;SessionState.SetBool(Flag,false);
        try
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            FactionPresetBakeSettings.Enabled=true;
            for(int faction=0;faction<3;faction++)for(int variant=0;variant<2;variant++) Bake(faction,variant);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("FACTION_PRESET_BAKE_OK");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Bake(int faction,int variant)
    {
        string folder=Root+"/faction_"+faction+"_"+variant;
        Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var holder=new GameObject("Bake holder");
        var visual=HumanFactionVisual.Create(holder.transform,faction,variant);
        visual.transform.SetParent(null,false);
        var meshes=new Dictionary<Mesh,Mesh>();var mats=new Dictionary<Material,Material>();var textures=new Dictionary<Texture,Texture>();
        int meshN=0, matN=0, texN=0;
        Func<Mesh,Mesh> saveMesh = source=>{
            if(!source||AssetDatabase.Contains(source))return source;
            if(meshes.TryGetValue(source,out var found))return found;
            var copy=UnityEngine.Object.Instantiate(source);copy.name=source.name;
            var path=folder+"/mesh_"+(meshN++)+".asset";AssetDatabase.CreateAsset(copy,path);meshes.Add(source,copy);return copy;
        };
        Func<Texture,Texture> saveTexture = source=>{
            if(!source||AssetDatabase.Contains(source))return source;
            if(textures.TryGetValue(source,out var found))return found;
            var tex=source as Texture2D;
            if(!tex || !tex.isReadable)throw new Exception("Non-readable generated texture: "+source.name);
            string path=folder+"/texture_"+(texN++)+".png";File.WriteAllBytes(path,tex.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            importer.wrapMode=tex.wrapMode;importer.filterMode=tex.filterMode;importer.mipmapEnabled=tex.mipmapCount>1;importer.SaveAndReimport();
            var copy=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!copy)throw new Exception("Texture import failed: "+path);
            textures.Add(source,copy);return copy;
        };
        Func<Material,Material> saveMat = source=>{
            if(!source||AssetDatabase.Contains(source))return source;
            if(mats.TryGetValue(source,out var found))return found;
            var copy=new Material(source);copy.name=source.name;
            foreach(string prop in copy.GetTexturePropertyNames()){var tex=copy.GetTexture(prop);if(tex)copy.SetTexture(prop,saveTexture(tex));}
            AssetDatabase.CreateAsset(copy,folder+"/material_"+(matN++)+".mat");mats.Add(source,copy);return copy;
        };
        foreach(var filter in visual.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh)filter.sharedMesh=saveMesh(filter.sharedMesh);
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.sharedMesh)skin.sharedMesh=saveMesh(skin.sharedMesh);
        foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true)){
            var source=renderer.sharedMaterials;
            // Property blocks are runtime-only and are not saved into prefabs.
            // Persist the faction-specific tint on the helmet material itself.
            if(renderer.name=="Human faction / helmet" && source.Length>0 && source[0])
            {
                var tint=new Material(source[0]);
                tint.color=faction==0?new Color(.67f,.76f,.56f):faction==1?new Color(.42f,.075f,.12f):new Color(.52f,.66f,.83f);
                source[0]=tint;renderer.SetPropertyBlock(null);
            }
            for(int i=0;i<source.Length;i++)source[i]=saveMat(source[i]);renderer.sharedMaterials=source;
        }
        DetailedHeroEquipment3D.DisownSharedMeshes(visual.transform);
        string prefab=Root+"/faction_"+faction+"_"+variant+".prefab";
        var saved=PrefabUtility.SaveAsPrefabAsset(visual.gameObject,prefab,out bool success);
        if(!success||!saved)throw new Exception("Prefab save failed: "+prefab);
        // Runtime procedural mesh owners are nested MonoBehaviours and Unity
        // cannot serialize their scripts into prefab assets. The saved meshes
        // are now independent assets, so those owners are no longer needed.
        var contents=PrefabUtility.LoadPrefabContents(prefab);
        int missing=0;
        foreach(var t in contents.GetComponentsInChildren<Transform>(true))
            missing+=GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        PrefabUtility.SaveAsPrefabAsset(contents,prefab,out success);
        PrefabUtility.UnloadPrefabContents(contents);
        if(!success)throw new Exception("Prefab cleanup failed: "+prefab);
        Debug.Log("FACTION_BAKED_REMOVED_RUNTIME_OWNERS "+missing);

        Debug.Log("FACTION_BAKED "+prefab+" meshes="+meshN+" materials="+matN+" textures="+texN);
        UnityEngine.Object.Destroy(visual.gameObject);UnityEngine.Object.Destroy(holder);
    }
}
