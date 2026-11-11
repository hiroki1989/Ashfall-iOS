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

            // 6. Cross Examination (gun-0-7) splits 5m from player position
            game.StartRun(12345);
            game.gun = LegendaryCatalog.CreateGun(LegendaryCatalog.Index("gun-0-7"), new System.Random(1), 1);
            Invoke("ResolveDistinctLegendaryGun", 0, 7, game.PlayerPosition + Vector3.forward * 9f, null);
            var flights = UnityEngine.Object.FindObjectsByType<LegendarySpecialFlight>(FindObjectsSortMode.None);
            bool found5m = false;
            foreach (var f in flights)
            {
                if (f.mode == 1 && Mathf.Abs(f.splitDistance - 5f) < .01f && Vector3.Distance(f.transform.position, game.PlayerPosition + Vector3.up * .65f) < 1f)
                    found5m = true;
            }
            if (!found5m)
                throw new Exception("Cross Examination flight must start at player and have splitDistance == 5f");

            // 7. Accuracy-scaled constant reticle radius (Sniper > Shotgun) stays inside camera viewport
            Set("aimTarget", null);
            game.gun = Arsenal.Model(3, Rarity.Common); // Shotgun (~48 accuracy)
            float shotgunR = (float)Invoke("AimReticleRadius", Vector3.forward);
            game.gun = Arsenal.Model(2, Rarity.Common); // Sniper (~97 accuracy)
            float sniperR = (float)Invoke("AimReticleRadius", Vector3.forward);
            if (!(sniperR > shotgunR + 2f))
                throw new Exception("Expected Sniper reticle radius (" + sniperR + ") > Shotgun (" + shotgunR + ")");
            Vector3 vp = game.GameCamera.WorldToViewportPoint(game.AimReticleWorldPosition);
            if (vp.x < 0.05f || vp.x > 0.95f || vp.y < 0.05f || vp.y > 0.95f)
                throw new Exception("Reticle left safe viewport bounds: " + vp);

            // 8. Nearest-camp assignment & camp mission filtering (max 3, camp-owned, level <= DebtLevel)
            var geo = game.WorldGeography;
            for (int s = 0; s < ExpeditionMap.StageCount; s++)
            {
                int assigned = game.AssignedCampForDungeon(s);
                float bestSq = (new Vector2(geo.Entrances[s].x - geo.Camps[assigned].x, geo.Entrances[s].z - geo.Camps[assigned].z)).sqrMagnitude;
                for (int c = 0; c < geo.Camps.Length; c++)
                {
                    float dSq = (new Vector2(geo.Entrances[s].x - geo.Camps[c].x, geo.Entrances[s].z - geo.Camps[c].z)).sqrMagnitude;
                    if (dSq + 0.01f < bestSq)
                        throw new Exception("Stage " + s + " assigned to camp " + assigned + " but camp " + c + " is closer");
                }
            }
            Invoke("ResetMissionRuntimeState");
            int[] camp0Offers = game.GetCampMissionOffers(0, true);
            if (camp0Offers == null || camp0Offers.Length == 0 || camp0Offers.Length > 3)
                throw new Exception("Expected 1..3 offers at Camp 0, got " + (camp0Offers == null ? -1 : camp0Offers.Length));
            foreach (int offer in camp0Offers)
            {
                if (game.AssignedCampForDungeon(offer) != 0)
                    throw new Exception("Camp 0 offered dungeon belonging to camp " + game.AssignedCampForDungeon(offer));
                if (ExpeditionMap.Get(offer, game.DebtLevel).level > game.DebtLevel)
                    throw new Exception("Camp 0 offered dungeon above DebtLevel: " + offer);
            }

            // 9. Level gating blocks entering higher-level dungeons when smoke == false
            int highLevelStage = -1;
            for (int s = 0; s < ExpeditionMap.RegularStageCount; s++)
            {
                if (ExpeditionMap.Get(s, game.DebtLevel).level > game.DebtLevel) { highLevelStage = s; break; }
            }
            if (highLevelStage < 0) throw new Exception("No high-level stage found for level gate test");
            Set("smoke", false);
            if (game.EnterExpeditionStage(highLevelStage))
                throw new Exception("EnterExpeditionStage must reject dungeon above player DebtLevel");
            Set("smoke", true);

            // 10. Clearing dungeon WITHOUT an active Briggs mission records conquest but grants ZERO rewards
            var clearedList = Get<List<int>>("clearedExpeditionStages");
            clearedList.Clear();
            int credBeforeNoJob = game.Credits;
            int tokBeforeNoJob = game.ExpeditionTokens;
            typeof(VaultGame).GetProperty("SelectedStageId", flags).SetValue(game, 0);
            Set("missionCompleted", true);
            game.OnDungeonFinished(true);
            if (game.Credits != credBeforeNoJob || game.ExpeditionTokens != tokBeforeNoJob)
                throw new Exception("Clearing dungeon without active contract must grant ZERO rewards");
            if (!clearedList.Contains(0))
                throw new Exception("Clearing dungeon without active contract must still update conquest clears");

            // 11. Full Briggs Mission flow: accept -> dungeon waypoint -> 1-active limit -> clear gives 0 payout & sets camp waypoint -> turn in pays out
            int jobStage = camp0Offers[0];
            int secondStage = camp0Offers.Length > 1 ? camp0Offers[1] : -1;
            game.VisitOverworldCamp(0);
            if (!game.AcceptBriggsMission(jobStage))
                throw new Exception("AcceptBriggsMission failed for eligible camp 0 stage " + jobStage);
            if (!game.HasActiveBriggsMission || game.ActiveBriggsMissionStage != jobStage || game.OverworldWaypointKind != 2 || game.OverworldWaypointId != jobStage)
                throw new Exception("Accepted mission must set OverworldWaypoint(2, jobStage)");
            if (secondStage >= 0 && game.AcceptBriggsMission(secondStage))
                throw new Exception("Must not accept a second mission while one is active");
            int credBeforeJob = game.Credits;
            int tokBeforeJob = game.ExpeditionTokens;
            int gunsBeforeJob = game.Weapons.Count;
            int armorBeforeJob = game.ArmorInventory.Count;
            typeof(VaultGame).GetProperty("SelectedStageId", flags).SetValue(game, jobStage);
            Set("missionCompleted", true);
            game.OnDungeonFinished(true);
            if (game.Credits != credBeforeJob || game.ExpeditionTokens != tokBeforeJob || game.Weapons.Count != gunsBeforeJob || game.ArmorInventory.Count != armorBeforeJob)
                throw new Exception("Finishing dungeon must NOT pay out before talking to Briggs");
            if (!game.ActiveBriggsMissionReadyToTurnIn || game.OverworldWaypointKind != 1 || game.OverworldWaypointId != 0)
                throw new Exception("Completing active mission must mark ReadyToTurnIn and set OverworldWaypoint(1, camp 0)");
            if (!game.ClaimCompletedBriggsMissionReward())
                throw new Exception("ClaimCompletedBriggsMissionReward failed");
            if (game.HasActiveBriggsMission || game.ActiveBriggsMissionReadyToTurnIn)
                throw new Exception("Turn-in must clear active mission state");
            if (game.Credits <= credBeforeJob && game.ExpeditionTokens <= tokBeforeJob)
                throw new Exception("Briggs turn-in did not grant contract payout");
            if (game.OverworldWaypointKind == 1 && game.OverworldWaypointId == 0)
                throw new Exception("Turn-in should clear return-to-camp waypoint");

            // 12. FirstSteps Stage 7 -> Stage 8 (30% conquest, NO waypoint)
            Set("smoke", false);
            SaveSlotPrefs.Select(3);
            SaveSlotPrefs.DeleteSlot(3);
            SaveSlotPrefs.SetInt("Vault.FirstSteps.v1", 7);
            game.SetOverworldWaypoint(1, 2);
            Invoke("FirstStepsSkillUpgraded");
            if (SaveSlotPrefs.GetInt("Vault.FirstSteps.v1", 0) != 8)
                throw new Exception("FirstStepsSkillUpgraded should move stage 7 -> 8");
            if (game.OverworldWaypointKind == 1)
                throw new Exception("FirstSteps stage 8 must NOT set a camp waypoint");
            clearedList.Clear();
            for (int s = 0, added = 0; s < ExpeditionMap.RegularStageCount && added < 15; s++)
            {
                if (ExpeditionMap.FactionForStage(s) == DungeonFaction.Raiders) { clearedList.Add(s); added++; }
            }
            Invoke("CheckFirstStepsConquestMilestone");
            if (SaveSlotPrefs.GetInt("Vault.FirstSteps.v1", 0) != 5)
                throw new Exception("FirstSteps stage 8 should complete upon reaching 30% faction conquest");
            SaveSlotPrefs.DeleteSlot(3);
            SaveSlotPrefs.Select(1);
            Set("smoke", true);

            // 13. Solo-clearing a dungeon without a mission before talking to Briggs must NOT forfeit first-clear reward
            SaveSlotPrefs.Select(3);
            SaveSlotPrefs.DeleteSlot(3);
            Set("smoke", false);
            Invoke("ResetRuntimeStateForSlot");
            Invoke("ProgressionInitialize");
            typeof(VaultGame).GetProperty("SelectedStageId", flags).SetValue(game, 100);
            Set("missionCompleted", true);
            game.OnDungeonFinished(true);
            if (!Get<List<int>>("clearedExpeditionStages").Contains(100))
                throw new Exception("Solo clear on stage 100 should update conquest");
            if (game.HasFirstClearRewardPaid(100))
                throw new Exception("Solo clear before talking to Briggs must NOT consume first-clear contract reward");
            SaveSlotPrefs.DeleteSlot(3);
            SaveSlotPrefs.Select(1);
            Set("smoke", true);

            // 14. Non-smoke dungeon clear skips Victory panel and returns without stopping at Screen.Victory
            Set("smoke", false);
            game.SetOverworldReturnStage(-1);
            typeof(VaultGame).GetProperty("SelectedStageId", flags).SetValue(game, 100);
            Set("missionCompleted", true);
            Invoke("Finish", true);
            if (game.screen == VaultGame.Screen.Victory || game.screen != VaultGame.Screen.Hub)
                throw new Exception("Non-smoke dungeon finish must skip Victory panel, got " + game.screen);
            Set("smoke", true);

            // 15. Skill tree node jump moves spatially without flipping branch
            Set("actionTreeKind", 0);
            Set("actionTreeBranch", 1);
            Set("actionTreeTier", 0);
            Set("actionTreeSlot", -1);
            game.JumpActionSkillNode(1, 0);
            game.JumpActionSkillNode(-1, 0);
            if (Get<int>("actionTreeBranch") != 1)
                throw new Exception("Horizontal node navigation must never change actionTreeBranch");
            Set("actionTreeTier", 0);
            Set("actionTreeSlot", -1);
            if (!game.JumpActionSkillNode(0, 1))
                throw new Exception("JumpActionSkillNode(0, 1) from root failed");
            if (Get<int>("actionTreeTier") != 0 || Get<int>("actionTreeSlot") != 0)
                throw new Exception($"Expected (tier=0, slot=0) after Down from root, got ({Get<int>("actionTreeTier")}, {Get<int>("actionTreeSlot")})");
            if (!game.JumpActionSkillNode(0, 1) || Get<int>("actionTreeSlot") < 0)
                throw new Exception("Second JumpActionSkillNode(0, 1) should move to lower slot inside tier");

            // 16. Iona catalog claim grants ExpeditionTokens and ActionSkillAvailablePoints
            int availBefore = game.ActionSkillAvailablePoints;
            int tokBeforeIona = game.ExpeditionTokens;
            Set("collectorKnown", 100);
            Set("collectorClaims", 1);
            if (game.CollectorAvailablePoints != 1)
                throw new Exception("Expected CollectorAvailablePoints == 1 for known=100, claims=1");
            int claimed = game.ClaimCollectorCatalogPoints();
            if (claimed != 1 || game.ExpeditionTokens != tokBeforeIona + 1 || game.ActionSkillAvailablePoints != availBefore + 1)
                throw new Exception($"Iona claim failed to grant skill point: claimed={claimed}, tok={game.ExpeditionTokens}, avail={game.ActionSkillAvailablePoints}");

            // 17. Overworld Landmark buildings have collision (players/enemies/bullets); Trees have NO collision
            var owGeo = game.WorldGeography;
            Vector3 clockTower = owGeo.Landmarks[0];
            if (!owGeo.Blocked(clockTower, .45f))
                throw new Exception("Clock Tower center must be blocked for actors");
            if (!owGeo.Blocked(clockTower + new Vector3(2.8f, 0, 2.8f), .45f))
                throw new Exception("Clock Tower stone base plinth must be blocked");
            if (owGeo.Blocked(clockTower + new Vector3(0, 0, -10.5f), .65f))
                throw new Exception("Landmark Boss spawn at z=-10.5 must stay clear of Clock Tower solid");
            if (owGeo.Layout.ClearLine(clockTower + new Vector3(-6f, 0, 0), clockTower + new Vector3(6f, 0, 0), .12f))
                throw new Exception("Bullets must not pass through Clock Tower");
            Vector3 memorialArch = owGeo.Landmarks[1];
            bool archActive = owGeo.IsFixedLandmarkActive(1);
            bool archPierBlocked = owGeo.Blocked(memorialArch + new Vector3(-4.4f, 0, 0), .45f);
            bool archCenterBlocked = owGeo.Blocked(memorialArch, .45f);
            if (archActive && (!archPierBlocked || archCenterBlocked))
                throw new Exception($"Memorial Arch piers must block while center arch stays open (pier={archPierBlocked}, center={archCenterBlocked})");
            if (!LandmarkArchitecture.LandmarkBlocked(memorialArch + new Vector3(-4.4f, 0, 0), .45f, memorialArch, 1) ||
                LandmarkArchitecture.LandmarkBlocked(memorialArch, .45f, memorialArch, 1))
                throw new Exception("LandmarkBlocked(Memorial Arch) geometry check failed");
            if (!owGeo.Blocked(owGeo.Landmarks[9], .45f) || !owGeo.Blocked(owGeo.Landmarks[27], .45f))
                throw new Exception("Botanical Dome and Command Bunker must be blocked");
            bool checkedTree = false;
            for (int cx = -2; cx <= 2 && !checkedTree; cx++)
            {
                for (int cz = -2; cz <= 2 && !checkedTree; cz++)
                {
                    var chunk = owGeo.GetChunk(cx, cz);
                    foreach (var tree in chunk.Trees)
                    {
                        if (owGeo.Blocked(tree, .45f))
                            throw new Exception("Overworld trees must NOT have collision at " + tree);
                        if (!owGeo.Layout.ClearLine(tree - Vector3.right * .35f, tree + Vector3.right * .35f, .12f))
                            throw new Exception("Bullets must pass freely through overworld trees at " + tree);
                        checkedTree = true;
                        break;
                    }
                }
            }
            if (!checkedTree)
                throw new Exception("No overworld tree found to verify no-collision");

            // 18. Active Briggs mission dungeon waypoint stays locked even upon arriving at dungeon entrance until mission completion (or manual clear)
            Set("smoke", false);
            SaveSlotPrefs.Select(3);
            SaveSlotPrefs.DeleteSlot(3);
            Invoke("ResetRuntimeStateForSlot");
            Invoke("ProgressionInitialize");
            var wpJobs = game.GetCampMissionOffers(0, true);
            if (wpJobs == null || wpJobs.Length == 0 || !game.AcceptBriggsMission(wpJobs[0]))
                throw new Exception("Failed to accept Briggs mission for waypoint test");
            int wpStage = wpJobs[0];
            game.BuildOverworld(owGeo.Entrances[wpStage]);
            var pTr = Get<Transform>("player");
            if (pTr != null) pTr.position = new Vector3(owGeo.Entrances[wpStage].x, .65f, owGeo.Entrances[wpStage].z);
            Invoke("TickOverworld");
            if (game.OverworldWaypointKind != 2 || game.OverworldWaypointId != wpStage)
                throw new Exception($"Mission dungeon waypoint must NOT auto-clear upon reaching dungeon (got kind={game.OverworldWaypointKind}, id={game.OverworldWaypointId})");
            game.SetOverworldWaypoint(0, -1);
            if (game.OverworldWaypointKind != 0)
                throw new Exception("Manual waypoint clear must still work");
            game.SetOverworldWaypoint(2, wpStage);
            typeof(VaultGame).GetProperty("SelectedStageId", flags).SetValue(game, wpStage);
            Set("missionCompleted", true);
            game.OnDungeonFinished(true);
            if (game.OverworldWaypointKind != 1 || game.OverworldWaypointId != 0)
                throw new Exception("Mission completion must redirect waypoint back to camp 0");
            SaveSlotPrefs.DeleteSlot(3);
            SaveSlotPrefs.Select(1);
            Set("smoke", true);

            // 19. Dungeon floor advance goes directly to next zone without Screen.Reward or per-floor run upgrades
            game.EnterHub();
            game.EnterDungeon(0);
            Set("cleared", true);
            int roomBeforeAdv = game.room;
            game.Advance();
            if (game.screen != VaultGame.Screen.Playing || game.room != roomBeforeAdv + 1)
                throw new Exception($"Advance() must move directly to next zone (screen={game.screen}, room={game.room})");
            if (game.runDamage != 0f || game.runFire != 0f || game.runVitality != 0f || game.runDash != 0f || game.runChain != 0f || game.runHarvest != 0f)
                throw new Exception("Per-floor run upgrades must remain disabled (all 0)");

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
