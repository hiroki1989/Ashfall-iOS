using System;
using UnityEditor;
using UnityEngine;
using NeonVault;
public static class SaveSlotIsolationReview
{
    public static void Run()
    {
        string key="Vault.SaveSlotReview."+Guid.NewGuid().ToString("N");
        try
        {
            for(int i=1;i<=3;i++){SaveSlotPrefs.Select(i);SaveSlotPrefs.SetInt(key,i*17);}
            for(int i=1;i<=3;i++){SaveSlotPrefs.Select(i);if(SaveSlotPrefs.GetInt(key)!=i*17)throw new Exception("Slot isolation failed "+i);}
            SaveSlotPrefs.Select(1);SaveSlotPrefs.DeleteKey(key);
            SaveSlotPrefs.Select(2);SaveSlotPrefs.DeleteKey(key);
            SaveSlotPrefs.Select(3);SaveSlotPrefs.DeleteKey(key);
            SaveSlotPrefs.Select(1);SaveSlotPrefs.Save();
            Debug.Log("SAVE_SLOT_ISOLATION PASS");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogError("SAVE_SLOT_ISOLATION FAIL "+e);EditorApplication.Exit(1);}
    }
}
