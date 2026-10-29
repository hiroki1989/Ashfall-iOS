using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace NeonVault
{
    // Opt-in finite regression check for the introductory contract.
    public sealed class DungeonEntryReview : MonoBehaviour
    {
        const string Flag = "--dungeon-entry-review";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0)
                new GameObject("Dungeon 01 entry review").AddComponent<DungeonEntryReview>();
        }

        IEnumerator Start()
        {
            string path = "D:/Unity/dungeon-01-entry-review.txt";
            for (int i = 0; i < 120 && !FindAnyObjectByType<VaultGame>(); i++) yield return null;
            try
            {
                var game = FindAnyObjectByType<VaultGame>();
                if (!game) throw new InvalidOperationException("VaultGame was not initialized");
                var stage = typeof(VaultGame).GetProperty("SelectedStageId");
                stage.SetValue(game, 0);
                game.StartRun(731904);
                if (game.screen != VaultGame.Screen.Playing) throw new InvalidOperationException("Dungeon did not open");
                if (game.Enemies.Count < 18) throw new InvalidOperationException("Intro mission has too few foes: " + game.Enemies.Count);
                if (ExpeditionMap.FactionForStage(0) != DungeonFaction.None) throw new InvalidOperationException("Intro faction changed");
                if (FindObjectsByType<HumanFactionVisual>(FindObjectsSortMode.None).Length != 0)
                    throw new InvalidOperationException("Heavy human faction rigs spawned in intro mission");
                int raiders = 0, gang = 0;
                for (int id = 0; id < ExpeditionMap.StageCount; id++)
                {
                    if (ExpeditionMap.FactionForStage(id) == DungeonFaction.Raiders) raiders++;
                    if (ExpeditionMap.FactionForStage(id) == DungeonFaction.Gang) gang++;
                }
                if (raiders != 50 || gang != 50) throw new InvalidOperationException("Faction allocation changed: " + raiders + "/" + gang);
                File.WriteAllText(path, "PASS: Dungeon 01 entered; foes=" + game.Enemies.Count +
                    "; faction rigs=0; faction sites=" + raiders + "/" + gang +
                    "; allocated memory=" + Profiler.GetTotalAllocatedMemoryLong());
                Debug.Log("DUNGEON_01_ENTRY_REVIEW_PASS");
                Application.Quit(0);
            }
            catch (Exception e)
            {
                File.WriteAllText(path, "FAIL: " + e);
                Debug.LogException(e);
                Application.Quit(1);
            }
        }
    }
}
