using System;
using System.Collections.Generic;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

public static class CollectorNamesCheck
{
    public static void Run()
    {
        try
        {
            var owners=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach(var recipe in DistinctEquipmentDesigns.Guns)
            {
                if(recipe.legendary)continue;
                string name=recipe.name;
                if(owners.TryGetValue(name,out int owner)&&owner!=recipe.maker)throw new Exception("Cross-maker gun name: "+name);
                owners[name]=recipe.maker;
            }
            foreach(var recipe in DistinctEquipmentDesigns.Armor)
            {
                if(recipe.legendary)continue;
                if(owners.TryGetValue(recipe.name,out int owner)&&owner!=recipe.maker)throw new Exception("Cross-maker armor name: "+recipe.name);
                owners[recipe.name]=recipe.maker;
            }
            var progress=typeof(VaultGame).GetNestedType("ProgressSave",BindingFlags.NonPublic);
            var save=Activator.CreateInstance(progress,true);
            progress.GetField("tokens").SetValue(save,8);
            progress.GetField("collectorClaims").SetValue(save,3);
            progress.GetField("collectorIntroSeen").SetValue(save,true);
            var restored=JsonUtility.FromJson(JsonUtility.ToJson(save),progress);
            if((int)progress.GetField("tokens").GetValue(restored)!=8||(int)progress.GetField("collectorClaims").GetValue(restored)!=3||!(bool)progress.GetField("collectorIntroSeen").GetValue(restored))throw new Exception("Collector save round trip failed");
            Debug.Log("COLLECTOR_NAMES_CHECK_OK names="+owners.Count);
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
