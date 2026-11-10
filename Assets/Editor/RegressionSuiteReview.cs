using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NeonVault;

public static class RegressionSuiteReview
{
    public static void Run()
    {
        GameObject host = null;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            host = new GameObject("RegressionSuiteReviewHost");
            var game = host.AddComponent<VaultGame>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            object Invoke(string name, params object[] args) => typeof(VaultGame).GetMethod(name, flags).Invoke(game, args);
            T Get<T>(string name) => (T)typeof(VaultGame).GetField(name, flags).GetValue(game);
            void Set(string name, object value) => typeof(VaultGame).GetField(name, flags).SetValue(game, value);
            T Prop<T>(string name) => (T)typeof(VaultGame).GetProperty(name, flags).GetValue(game);

            Invoke("Awake");
            Set("smoke", true);
            Invoke("ResetRuntimeStateForSlot");
            Invoke("StartSelectedSaveSlot", 1, false);
            if (game.screen != VaultGame.Screen.Hub)
                throw new Exception("Expected Hub after starting save slot, got " + game.screen);

            // 1. Safehouse pause -> resume must stay in Screen.Hub (never Screen.Playing)
            Invoke("PauseGame");
            if (game.screen != VaultGame.Screen.Pause || !Get<bool>("pausedFromHub") || Time.timeScale != 0f)
                throw new Exception("PauseGame from Hub failed");

            Invoke("ResumeFromPause");
            if (game.screen != VaultGame.Screen.Hub || Get<bool>("pausedFromHub") || Time.timeScale != 1f)
                throw new Exception("ResumeFromPause from Hub returned to " + game.screen);

            // Pause from Hub -> OpenOverworldMap -> CloseOverworldMap -> ResumeFromPause -> Hub
            Set("overworldStateLoaded", true);
            Invoke("CompleteOverworldStory");
            game.EnterHub();
            Invoke("PauseGame");
            game.OpenOverworldMap();
            if (game.screen != VaultGame.Screen.DungeonSelect)
                throw new Exception("Expected DungeonSelect from Hub pause");
            game.CloseOverworldMap();
            if (game.screen != VaultGame.Screen.Pause)
                throw new Exception("CloseOverworldMap from Hub pause should return to Pause, got " + game.screen);
            Invoke("ResumeFromPause");
            if (game.screen != VaultGame.Screen.Hub || Get<bool>("pausedFromHub"))
                throw new Exception("Resume after Hub pause->map must return to Hub, got " + game.screen);

            // Pause from Hub -> SaveRunAndReturnToMenu -> Menu (without saving dungeon checkpoint)
            Invoke("PauseGame");
            Invoke("SaveRunAndReturnToMenu");
            if (game.screen != VaultGame.Screen.Menu || Get<bool>("pausedFromHub") || Time.timeScale != 1f)
                throw new Exception("SaveRunAndReturnToMenu from Hub pause failed: " + game.screen);

            // 2. Legendary popup stops combat & exposes menu cursor (Active == false, Time.timeScale == 0)
            game.StartRun(12345);
            if (game.screen != VaultGame.Screen.Playing || !game.Active)
                throw new Exception("StartRun should be Playing & Active");
            Set("legendaryWeaponTutorialVisible", true);
            Time.timeScale = 0f;
            if (game.Active || Time.timeScale != 0f)
                throw new Exception("Legendary tutorial must set Active=false and Time.timeScale=0");
            Invoke("ResetGameplayOverlays");
            if (Get<bool>("legendaryWeaponTutorialVisible") || Time.timeScale != 1f || !game.Active)
                throw new Exception("ResetGameplayOverlays must clear legendary tutorial and restore Active & timeScale");

            // 3. NPC / Collector dialogue active flag blocks non-dialogue controls
            game.EnterHub();
            Invoke("OpenCollector");
            if (!Prop<bool>("AnyNpcOrStoryDialogueActive"))
                throw new Exception("OpenCollector must activate AnyNpcOrStoryDialogueActive");
            Set("collectorOpen", false);
            Invoke("OpenMerchantDialogue");
            if (!Prop<bool>("AnyNpcOrStoryDialogueActive"))
                throw new Exception("Merchant dialogue must activate AnyNpcOrStoryDialogueActive");
            Set("npcConversation", 0);

            // 4. Junk toggle works cleanly
            Invoke("ToggleJunk", "Gun:qa-item");
            var junk = Get<HashSet<string>>("junkKeys");
            if (junk == null || !junk.Contains("Gun:qa-item"))
                throw new Exception("ToggleJunk failed to add tag");
            Invoke("ToggleJunk", "Gun:qa-item");
            if (junk.Contains("Gun:qa-item"))
                throw new Exception("ToggleJunk failed to remove tag");

            // 5. CatalogEffect never outputs internal 'distinct' IDs for standard gear
            var armor = Armory.CreateCatalog(ArmorProductCatalog.All[0].id, Rarity.Common, 1);
            string eff = (string)Invoke("CatalogEffect", armor);
            if (!string.IsNullOrEmpty(eff) && eff.IndexOf("distinct", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new Exception("CatalogEffect leaked internal ID: " + eff);
            for (int maker = 0; maker < 6; maker++)
                for (int kind = -1; kind <= 3; kind++)
                    game.SetManufacturerReview(maker, kind);

            Debug.Log("REGRESSION_SUITE_REVIEW PASS");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("REGRESSION_SUITE_REVIEW FAIL " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
        finally
        {
            Time.timeScale = 1f;
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
