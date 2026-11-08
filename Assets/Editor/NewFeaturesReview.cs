using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NeonVault;

public static class NewFeaturesReview
{
    public static void Run()
    {
        string logPath = "D:/Unity/BuildTools/new_features_review.log";
        using (var sw = new StreamWriter(logPath, false))
        {
            sw.WriteLine("=== New Features Review ===");

            // 1. SectorLayout boss vs normal floor tests
            var rnd = new System.Random(12345);
            var normalLayout = new SectorLayout(rnd, 0, compact: true, arena: false);
            sw.WriteLine($"Normal floor layout: {normalLayout.Columns}x{normalLayout.Rows}, chambers: {normalLayout.chambers.Count}, walkable: {normalLayout.WalkableCells}");
            if (normalLayout.chambers.Count < 15) throw new Exception("Normal floor chambers should be intricate/numerous");

            var arenaLayout = new SectorLayout(rnd, 0, compact: false, arena: true);
            sw.WriteLine($"Arena floor layout: {arenaLayout.Columns}x{arenaLayout.Rows}, chambers: {arenaLayout.chambers.Count}, walkable: {arenaLayout.WalkableCells}");

            // 2. YOU DIED logo texture load test
            var logo = Resources.Load<Texture2D>("Art/UI/you_died_logo");
            if (logo == null) throw new Exception("Art/UI/you_died_logo failed to load");
            sw.WriteLine($"YOU DIED logo loaded: {logo.width}x{logo.height}");

            // 3. SaveSlot delete test
            int testSlot = 2;
            SaveSlotPrefs.Select(testSlot);
            SaveSlotPrefs.SetInt("Vault.Cores", 9999);
            SaveSlotPrefs.SetString("Vault.Progression.v1", "{\"test\":1}");
            SaveSlotPrefs.SetInt("Vault.DungeonVisited.0", 1);
            SaveSlotPrefs.Save();
            if (!SaveSlotPrefs.Exists(testSlot)) throw new Exception("Test slot 2 should exist");
            if (SaveSlotPrefs.GetInt("Vault.Cores", 0) != 9999) throw new Exception("Test slot 2 Cores mismatch");
            if (PlayerPrefs.GetInt("Vault.SaveSlot.2.Vault.DungeonVisited.0", 0) != 1) throw new Exception("DungeonVisited flag mismatch");

            SaveSlotPrefs.DeleteSlot(testSlot);
            if (SaveSlotPrefs.Exists(testSlot)) throw new Exception("Test slot 2 should NOT exist after DeleteSlot");
            if (SaveSlotPrefs.GetInt("Vault.Cores", 0) != 0) throw new Exception("Test slot 2 Cores should be 0 after DeleteSlot");
            if (PlayerPrefs.GetInt("Vault.SaveSlot.2.Vault.DungeonVisited.0", 0) != 0) throw new Exception("DungeonVisited should be deleted");
            sw.WriteLine("SaveSlot Delete test passed!");

            // 4. GameCenterSyncManager test
            GameCenterSyncManager.Initialize();
            sw.WriteLine("GameCenterSyncManager initialized OK");

            sw.WriteLine("ALL CHECKS PASSED!");
        }
        Debug.Log("NewFeaturesReview completed successfully.");
    }
}
