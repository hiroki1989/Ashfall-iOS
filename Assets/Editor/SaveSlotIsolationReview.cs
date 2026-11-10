using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NeonVault;
public static class SaveSlotIsolationReview
{
    public static void Run()
    {
        string key="Vault.SaveSlotReview."+Guid.NewGuid().ToString("N");
        GameObject host=null;
        try
        {
            for(int i=1;i<=3;i++){SaveSlotPrefs.Select(i);SaveSlotPrefs.SetInt(key,i*17);}
            for(int i=1;i<=3;i++){SaveSlotPrefs.Select(i);if(SaveSlotPrefs.GetInt(key)!=i*17)throw new Exception("Slot isolation failed "+i);}
            SaveSlotPrefs.Select(1);SaveSlotPrefs.DeleteKey(key);
            SaveSlotPrefs.Select(2);SaveSlotPrefs.DeleteKey(key);
            SaveSlotPrefs.Select(3);SaveSlotPrefs.DeleteKey(key);
            SaveSlotPrefs.Select(1);SaveSlotPrefs.Save();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            host=new GameObject("SaveSlotIsolationReviewHost");
            var game=host.AddComponent<VaultGame>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            typeof(VaultGame).GetMethod("Awake",flags).Invoke(game,null);
            typeof(VaultGame).GetField("smoke",flags).SetValue(game,true);
            var reset=typeof(VaultGame).GetMethod("ResetRuntimeStateForSlot",flags);
            var loadJunk=typeof(VaultGame).GetMethod("LoadJunk",flags);
            var toggleJunk=typeof(VaultGame).GetMethod("ToggleJunk",flags);
            var junkField=typeof(VaultGame).GetField("junkKeys",flags);
            var startSlot=typeof(VaultGame).GetMethod("StartSelectedSaveSlot",flags);
            var initField=typeof(VaultGame).GetField("saveSlotInitialized",flags);
            var menuOpenField=typeof(VaultGame).GetField("saveSlotMenuOpen",flags);

            // 1. Cold start: junkKeys is null before first shop/junk access.
            if(junkField.GetValue(game)!=null)throw new Exception("Expected junkKeys null before LoadJunk");
            reset.Invoke(game,null);

            // 2. Populated state: ResetRuntimeStateForSlot clears junkKeys back to null so a new slot reloads.
            toggleJunk.Invoke(game,new object[]{"Gun:test"});
            if(junkField.GetValue(game)==null)throw new Exception("Expected junkKeys populated after ToggleJunk");
            reset.Invoke(game,null);
            if(junkField.GetValue(game)!=null)throw new Exception("Expected junkKeys reset to null");
            loadJunk.Invoke(game,null);

            // 3. Full StartSelectedSaveSlot flow (same path as clicking Start/Continue on Title Menu).
            reset.Invoke(game,null);
            menuOpenField.SetValue(game,true);
            startSlot.Invoke(game,new object[]{1,false});
            if(!(bool)initField.GetValue(game))throw new Exception("Expected saveSlotInitialized true after StartSelectedSaveSlot");
            if((bool)menuOpenField.GetValue(game))throw new Exception("Expected saveSlotMenuOpen false after StartSelectedSaveSlot");

            Debug.Log("SAVE_SLOT_ISOLATION PASS");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogError("SAVE_SLOT_ISOLATION FAIL "+e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        finally{if(host!=null)UnityEngine.Object.DestroyImmediate(host);}
    }
}
