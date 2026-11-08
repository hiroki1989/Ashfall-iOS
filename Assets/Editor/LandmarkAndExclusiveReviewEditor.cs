using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using NeonVault;

public static class LandmarkAndExclusiveReviewEditor
{
    public static void Run()
    {
        string logPath = "D:/Unity/BuildTools/landmark_exclusive_review.log";
        string outDir = "D:/Unity/BuildTools/LandmarkReview";
        Directory.CreateDirectory(outDir);

        using (var sw = new StreamWriter(logPath, false))
        {
            sw.WriteLine("=== Landmark 3x Expansion, Scenery Quality & Boss Exclusive Drops Review ===");

            // 1. Verify OverworldGeography 30 Landmarks
            var geo = new OverworldGeography();
            if (OverworldGeography.LandmarkCount != 30)
                throw new Exception($"Expected LandmarkCount=30, got {OverworldGeography.LandmarkCount}");
            if (geo.Landmarks == null || geo.Landmarks.Length != 30)
                throw new Exception($"Expected geo.Landmarks.Length=30, got {geo.Landmarks?.Length ?? -1}");

            for (int i = 0; i < 30; i++)
            {
                Vector3 pos = geo.Landmarks[i];
                string titleJa = OverworldGeography.LandmarkTitle(i, true);
                string titleEn = OverworldGeography.LandmarkTitle(i, false);
                string bossJa = OverworldGeography.LandmarkBossName(i, true);
                string bossEn = OverworldGeography.LandmarkBossName(i, false);
                int bossLv = OverworldGeography.LandmarkBossLevel(i, 1);
                int faction = OverworldGeography.LandmarkBossFaction(i);
                if (string.IsNullOrEmpty(titleJa) || string.IsNullOrEmpty(titleEn))
                    throw new Exception($"Landmark {i} missing title");
                if (string.IsNullOrEmpty(bossJa) || string.IsNullOrEmpty(bossEn))
                    throw new Exception($"Landmark {i} missing boss name");
                if (bossLv < 1 || bossLv > 20)
                    throw new Exception($"Landmark {i} invalid boss level {bossLv}");
                if (faction < 0 || faction > 2)
                    throw new Exception($"Landmark {i} invalid faction {faction}");
                sw.WriteLine($"Landmark #{i:D2}: {titleJa} ({titleEn}) @ {pos} | Boss: {bossJa} (Lv.{bossLv}, Faction {faction})");
            }
            sw.WriteLine("PASS: All 30 Landmarks verified in OverworldGeography.");

            // 2. Verify DungeonBossExclusive: 83 Dungeon Boss exclusives + 30 Landmark Boss exclusives = 113 total
            var rng = new System.Random(12345);
            for (int lm = 0; lm < 30; lm++)
            {
                if (!DungeonBossExclusive.TryGetLandmarkExclusive(lm, out var drop))
                    throw new Exception($"Landmark {lm} does not have an assigned ExclusiveLegendaryDrop!");

                object item = DungeonBossExclusive.CreateStageExclusiveItem(drop, rng, 10);
                if (item == null)
                    throw new Exception($"Landmark {lm} failed to create exclusive item for {drop.catalogId}");

                if (!DungeonBossExclusive.TryGetExclusiveLandmarkForItem(item, out int mappedLm) || mappedLm != lm)
                    throw new Exception($"Landmark {lm} exclusive item reverse lookup mismatch: got {mappedLm}");

                if (drop.category == ExclusiveLegendaryCategory.Gun && !DungeonBossExclusive.IsExclusiveGun(drop.catalogId))
                    throw new Exception($"Landmark {lm} gun {drop.catalogId} not marked exclusive!");
                if (drop.category == ExclusiveLegendaryCategory.Armor && !DungeonBossExclusive.IsExclusiveArmor(drop.catalogId))
                    throw new Exception($"Landmark {lm} armor {drop.catalogId} not marked exclusive!");
                if (drop.category == ExclusiveLegendaryCategory.Module && !DungeonBossExclusive.IsExclusiveModule(drop.catalogId))
                    throw new Exception($"Landmark {lm} module {drop.catalogId} not marked exclusive!");
                if (drop.category == ExclusiveLegendaryCategory.Melee && !DungeonBossExclusive.IsExclusiveMelee(drop.index))
                    throw new Exception($"Landmark {lm} melee {drop.index} not marked exclusive!");

                sw.WriteLine($"Landmark #{lm:D2} Exclusive: Category={drop.category}, CatalogId={drop.catalogId}, Index={drop.index}");
            }
            sw.WriteLine("PASS: All 30 Landmark Boss Exclusive drops verified and reversible.");

            // Verify non-exclusive pools never roll any exclusive item (1000 rolls each)
            for (int r = 0; r < 1000; r++)
            {
                int gIdx = DungeonBossExclusive.RollNonExclusiveGunCatalogIndex(rng);
                string gKey = LegendaryCatalog.Key(false, gIdx);
                if (DungeonBossExclusive.IsExclusiveGun(gKey))
                    throw new Exception($"Non-exclusive gun roll returned exclusive gun {gKey}!");

                int aIdx = DungeonBossExclusive.RollNonExclusiveArmorIndex(rng);
                string aKey = LegendaryCatalog.Key(true, aIdx);
                if (DungeonBossExclusive.IsExclusiveArmor(aKey))
                    throw new Exception($"Non-exclusive armor roll returned exclusive armor {aKey}!");

                var mod = DungeonBossExclusive.RollNonExclusiveLegendaryModule(rng, 4 + (r % 3));
                if (DungeonBossExclusive.IsExclusiveModule(mod.id))
                    throw new Exception($"Non-exclusive module roll returned exclusive module {mod.id}!");

                int mIdx = DungeonBossExclusive.RollNonExclusiveMeleeIndex(rng);
                if (DungeonBossExclusive.IsExclusiveMelee(mIdx))
                    throw new Exception($"Non-exclusive melee roll returned exclusive melee {mIdx}!");
            }
            sw.WriteLine("PASS: 4,000 non-exclusive legendary rolls confirmed 0% leak of Dungeon or Landmark boss exclusives.");

            int dungeonExCount = 0;
            for (int st = 0; st < ExpeditionMap.StageCount; st++)
            {
                if (DungeonBossExclusive.TryGetStageExclusive(st, out _))
                    dungeonExCount++;
            }
            if (dungeonExCount != 83)
                throw new Exception($"Expected 83 stages with exclusive drops, got {dungeonExCount}");
            sw.WriteLine($"PASS: All {dungeonExCount} Dungeon Boss Exclusive drops verified.");

            // 3. Test "???" mystery state before fighting boss vs after fighting boss
            var holder = new GameObject("ReviewVaultGameHolder");
            holder.SetActive(false);
            var game = holder.AddComponent<VaultGame>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
            typeof(VaultGame).GetField("smoke", flags).SetValue(game, true);
            typeof(VaultGame).GetField("overworldGeography", flags).SetValue(game, geo);

            SaveSlotPrefs.DeleteKey("Vault.LandmarkBossFought.29");
            if (game.IsLandmarkBossFought(29)) throw new Exception("Landmark 29 should be unfought initially");
            game.MarkLandmarkBossFought(29);
            if (!game.IsLandmarkBossFought(29)) throw new Exception("Landmark 29 should be fought after MarkLandmarkBossFought");
            SaveSlotPrefs.DeleteKey("Vault.LandmarkBossFought.29");

            SaveSlotPrefs.DeleteKey("Vault.DungeonBossFought.50");
            if (game.IsDungeonBossFought(50)) throw new Exception("Dungeon 50 should be unfought initially");
            game.MarkDungeonBossFought(50);
            if (!game.IsDungeonBossFought(50)) throw new Exception("Dungeon 50 should be fought after MarkDungeonBossFought");
            SaveSlotPrefs.DeleteKey("Vault.DungeonBossFought.50");
            sw.WriteLine("PASS: Fought state tracking (??? before fight -> revealed after fight) verified for both Landmark and Dungeon bosses.");

            // 4. Test LandmarkArchitecture 3D generation & render screenshots
            typeof(LandmarkArchitecture).GetMethod("Prepare", flags).Invoke(null, null);
            var getLmProto = typeof(LandmarkArchitecture).GetMethod("GetLandmarkPrototype", flags);
            var getParkProto = typeof(LandmarkArchitecture).GetMethod("GetParkPrototype", flags);

            for (int kind = 0; kind < 23; kind++)
            {
                var proto = (GameObject)getLmProto.Invoke(null, new object[] { kind });
                if (proto == null || proto.transform.childCount == 0)
                    throw new Exception($"Landmark kind {kind} produced invalid prototype!");
            }
            sw.WriteLine("PASS: All 23 LandmarkArchitecture 3D monument prototypes built cleanly.");

            for (int v = 0; v < 4; v++)
            {
                var proto = (GameObject)getParkProto.Invoke(null, new object[] { v });
                if (proto == null || proto.transform.childCount == 0)
                    throw new Exception($"Park variant {v} produced invalid prototype!");
            }
            sw.WriteLine("PASS: All 4 LandmarkArchitecture Park prototypes built cleanly.");

            // Render showcase images
            var renderRoot = new GameObject("LandmarkRenderRoot");
            var camObj = new GameObject("RenderCam");
            camObj.transform.SetParent(renderRoot.transform);
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 400f;

            var sunObj = new GameObject("SunLight");
            sunObj.transform.SetParent(renderRoot.transform);
            sunObj.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.45f;

            var fillObj = new GameObject("FillLight");
            fillObj.transform.SetParent(renderRoot.transform);
            fillObj.transform.rotation = Quaternion.Euler(28f, 145f, 0f);
            var fill = fillObj.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.52f, 0.66f, 0.84f);
            fill.intensity = 0.70f;

            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

            Vector3[] offsets = {
                new Vector3(-16f, 0f, 12f),
                new Vector3(16f, 0f, 12f),
                new Vector3(-16f, 0f, -12f),
                new Vector3(16f, 0f, -12f)
            };

            // Showcase 1: Clock Tower (0), Botanical Dome (9), Goliath Crane (13), Rocket Launchpad (22)
            var grid1 = new GameObject("Grid1");
            grid1.transform.SetParent(renderRoot.transform);
            int[] kinds1 = { 0, 9, 13, 22 };
            for (int i = 0; i < 4; i++)
            {
                var proto = (GameObject)getLmProto.Invoke(null, new object[] { kinds1[i] });
                var inst = UnityEngine.Object.Instantiate(proto, offsets[i], Quaternion.Euler(0, -25f, 0), grid1.transform);
                inst.SetActive(true);
            }
            cam.transform.position = new Vector3(0f, 32f, -44f);
            cam.transform.LookAt(new Vector3(0f, 6.5f, 0f));
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "landmarks_showcase_1.png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(grid1);

            // Showcase 2: Memorial Arch (1), Deepwood Shrine (8), Cathedral Spire (19), Gazebo Park
            var grid2 = new GameObject("Grid2");
            grid2.transform.SetParent(renderRoot.transform);
            int[] kinds2 = { 1, 8, 19 };
            for (int i = 0; i < 3; i++)
            {
                var proto = (GameObject)getLmProto.Invoke(null, new object[] { kinds2[i] });
                var inst = UnityEngine.Object.Instantiate(proto, offsets[i], Quaternion.Euler(0, -25f, 0), grid2.transform);
                inst.SetActive(true);
            }
            var parkProto0 = (GameObject)getParkProto.Invoke(null, new object[] { 0 });
            var parkInst0 = UnityEngine.Object.Instantiate(parkProto0, offsets[3], Quaternion.Euler(0, -25f, 0), grid2.transform);
            parkInst0.SetActive(true);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(outDir, "landmarks_showcase_2.png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(grid2);

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(renderRoot);
            UnityEngine.Object.DestroyImmediate(holder);

            sw.WriteLine("ALL LANDMARK & EXCLUSIVE DROP CHECKS PASSED!");
        }
        Debug.Log("LandmarkAndExclusiveReviewEditor completed successfully.");
    }
}
