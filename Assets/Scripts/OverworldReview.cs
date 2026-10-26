using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NeonVault
{
    // Finite QA: requires bootstrap smoke mode and never writes player save data.
    public sealed class OverworldReview : MonoBehaviour
    {
        VaultGame game;
        string output;
        int checks, failures;
        readonly List<string> results = new List<string>();
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--overworld-review") >= 0)
                new GameObject("Overworld completed implementation review").AddComponent<OverworldReview>();
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--qa-output");
            output = at >= 0 && at + 1 < args.Length ? args[at + 1] : "D:/Unity/QA-overworld-20261025";
            Directory.CreateDirectory(output);
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                object current = null; bool more = false;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception e) { Check(false, "Unhandled QA exception " + e); break; }
                if (!more) { stack.Pop(); continue; }
                if (current is IEnumerator iterator) stack.Push(iterator); else yield return current;
            }
            Time.timeScale = 1;
            results.Add("Automated API/reflection checks and framebuffer captures only. Physical input, iOS device/Metal/IL2CPP and disk restart persistence remain unverified.");
            results.Add("Checks=" + checks + " Failures=" + failures);
            File.WriteAllLines(Path.Combine(output, "result.txt"), results);
            Debug.Log("ASHFALL_OVERWORLD_REVIEW Checks=" + checks + " Failures=" + failures);
            Application.Quit(failures == 0 ? 0 : 1);
        }
        void Check(bool pass, string message) { checks++; if (!pass) failures++; results.Add((pass ? "PASS " : "FAIL ") + message); }
        object Get(string name) => typeof(VaultGame).GetField(name, Flags).GetValue(game);
        void Set(string name, object value) => typeof(VaultGame).GetField(name, Flags).SetValue(game, value);
        void Call(string name, params object[] values) => typeof(VaultGame).GetMethod(name, Flags).Invoke(game, values);
        void Move(Vector3 position)
        {
            var layout = game.WorldGeography.Layout;
            if (layout.Blocked(position, .85f))
            {
                Vector3 nearest = game.PlayerPosition; float best = float.MaxValue;
                for (int x = 0; x < layout.Columns; x++) for (int y = 0; y < layout.Rows; y++)
                {
                    Vector3 candidate = new Vector3((x + .5f - layout.Columns * .5f) * layout.CellSize, .65f, (y + .5f - layout.Rows * .5f) * layout.CellSize);
                    float distance = (candidate - position).sqrMagnitude;
                    if (distance < best && !layout.Blocked(candidate, .85f)) { best = distance; nearest = candidate; }
                }
                position = nearest;
            }
            position.y = .65f;
            ((Transform)Get("player")).position = position; OverworldScenery.UpdateFocus((Transform)Get("world"),position);WorldEntranceBuildings.UpdateFocus((Transform)Get("world"),position);Call("StreamOverworldEnemies"); Call("UpdateCamera", true);
            Vector3 viewport = game.GameCamera.WorldToViewportPoint(game.PlayerPosition);
            Check(viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1, "Moved player inside camera viewport");
        }
        IEnumerator Capture(string name)
        {
            bool wasEnabled = game.enabled; game.enabled = true; game.invincible = 99999;
            yield return null; yield return new WaitForEndOfFrame();
            string file = Path.Combine(output, name + ".png"); var picture = ScreenCapture.CaptureScreenshotAsTexture();
            bool visible=false;var pixels=picture.GetPixels32();for(int i=0;i<pixels.Length;i+=113)if(pixels[i].r+pixels[i].g+pixels[i].b>70){visible=true;break;}
            File.WriteAllBytes(file, picture.EncodeToPNG());Destroy(picture);game.enabled=wasEnabled;
            results.Add((visible?"CAPTURE ":"UNVERIFIED black hidden-window framebuffer ")+name);
            var camera=game.GameCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1440,900,24);var scene=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;scene.ReadPixels(new Rect(0,0,1440,900),0,0);scene.Apply();File.WriteAllBytes(Path.Combine(output,name+"-camera.png"),scene.EncodeToPNG());float min=1,max=0;for(int x=300;x<1140;x+=47)for(int y=200;y<700;y+=43){float v=scene.GetPixel(x,y).grayscale;min=Mathf.Min(min,v);max=Mathf.Max(max,v);}Check(max-min>.08f,"Camera scene contains visible detail "+name);}
            finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(scene);}
        }
        IEnumerator Run()
        {
            for (int i = 0; i < 120 && !game; i++) { game = FindAnyObjectByType<VaultGame>(); yield return null; }
            Check(game != null, "VaultGame exists"); if (!game) yield break;
            Check((bool)Get("smoke"), "Bootstrap smoke prevents save writes"); if (!(bool)Get("smoke")) yield break;
            Application.runInBackground=true;
            Screen.SetResolution(1441,900,FullScreenMode.Windowed);yield return null;yield return null;
            Screen.SetResolution(1440,900,FullScreenMode.Windowed);yield return null;yield return null;
            game.enabled = false;
            game.EnsureOverworldState();
            Check(!game.WorldStorySeen && game.IsCampVisited(0) && !game.IsCampVisited(1), "Fresh smoke story and camp state");
            game.ContinueOverworldGame(); yield return null; Check(game.WorldBossConversationVisible, "First continue opens boss conversation");
            foreach (bool ja in new[] { false, true }) { VaultLanguage.Set(ja, false); yield return Capture("story-" + (ja ? "ja" : "en")); }
            game.CompleteWorldBossConversation(); Check(game.WorldStorySeen && !game.WorldBossConversationVisible, "Conversation completes once");
            game.ExitCampToOverworld(); yield return null; Check(game.IsOverworld, "Camp exit starts continuous outdoors");
            var geo = game.WorldGeography;
            Check(geo.Camps.Length == 7 && geo.Entrances.Length == 100 && geo.Chests.Length == 36, "7 camps / 100 entrances / 36 chests");
            Check(geo.Layout.Columns*geo.Layout.CellSize==10000 && geo.Layout.Rows*geo.Layout.CellSize==10000,"10km by 10km physical dimensions");
            Check(game.OverworldEnemySpawnCount>1800,"Load-time population generated across world");
            Check(geo.Layout.Connected(), "All open geography cells connected");
            foreach (var group in new[] { geo.Camps, geo.Entrances, geo.Chests })
                for (int i = 0; i < group.Length; i++) Check(!geo.Layout.Blocked(group[i], .85f), "Site safe and connected " + group.Length + ":" + i);
            Check(!game.EnterNearbyWorldCamp(1),"Remote camp entry rejected");
            Move(geo.Camps[0]);Check(game.EnterNearbyWorldCamp(0)&&game.screen==VaultGame.Screen.Hub,"Nearby camp door enters hub");game.ExitCampToOverworld();yield return null;
            Check(game.Blocked(geo.Entrances[0]+Vector3.forward*6,.5f),"Dungeon building blocks passage through walls");
            Check(!game.Blocked(geo.Entrances[0],.5f),"Dungeon threshold remains accessible");
            var population=game.OverworldEnemySpawnCount;
            var live=game.Enemies.Find(e=>e!=null&&!e.dead);
            if(live!=null){int defeated=game.OverworldEnemyDefeatedCount;live.dead=true;game.OnOverworldEnemyKilled(live);Call("StreamOverworldEnemies");Check(game.OverworldEnemyDefeatedCount==defeated+1&&game.OverworldEnemySpawnCount==population,"Kill marks existing spawn defeated without replacement");Call("StreamOverworldEnemies");Check(game.OverworldEnemyDefeatedCount==defeated+1,"Repeated stream does not revive defeated spawn");}
            yield return Capture("entrance-and-enemies");
            Call("SpawnFoe",game.PlayerPosition+Vector3.right*4,0);var grounded=game.Enemies[game.Enemies.Count-1];grounded.enabled=false;
            Check(Mathf.Abs(grounded.transform.position.y-.09f)<.001f,"Outdoor enemy root matches zero-height sole plane");
            float previousZoom=game.GameCamera.orthographicSize;game.GameCamera.orthographicSize=4;yield return Capture("enemy-ground-contact");game.GameCamera.orthographicSize=previousZoom;

            Check(!game.FastTravelToCamp(1), "Unvisited camp travel rejected");
            game.VisitOverworldCamp(1); Check(game.FastTravelToCamp(1) && game.screen == VaultGame.Screen.Hub && game.LastVisitedCamp == 1, "Visited camp fast travel enters camp hub"); yield return null;
            game.ExitCampToOverworld(); yield return null;
            int stage = -1; for (int i = 0; i < 100; i++) if (ExpeditionMap.Get(i).level <= game.DebtLevel) { stage = i; break; }
            Check(stage >= 0, "An eligible dungeon exists"); if (stage < 0) yield break;
            Move(geo.Camps[1]); Check(!game.EnterNearbyWorldDungeon(stage), "Remote dungeon entry rejected");
            Move(geo.Entrances[stage]); Check(game.EnterNearbyWorldDungeon(stage) && !game.IsOverworld && game.PendingWorldStage == stage, "Nearby eligible dungeon entered"); yield return null;
            Check(!game.FastTravelToCamp(0), "Fast travel prohibited inside dungeon");
            Set("missionCompleted", true); int clears = game.OverworldMissionClears;
            Call("Finish", true); yield return null;
            Check(game.IsOverworld && game.OverworldMissionClears == clears + 1 && Vector3.Distance(game.PlayerPosition, geo.Entrances[stage]) < 4, "Successful mission returns to same entrance and increments clear");
            Move(geo.Entrances[stage]); game.EnterNearbyWorldDungeon(stage); yield return null; game.VisitOverworldCamp(2);
            game.AwardCredits(101); int before = game.Credits; Call("Finish", false); yield return null;
            Check(game.Credits == before / 2 && game.LastVisitedCamp == 2 && game.screen == VaultGame.Screen.Hub && !game.SavedOutside, "Death halves scrap and returns to last camp");
            game.ExitCampToOverworld(); yield return null; Check(Vector3.Distance(game.PlayerPosition, geo.Camps[2]) < 4, "Exit after death uses last camp");
            Check(game.MarkOverworldChestOpened(0) && !game.MarkOverworldChestOpened(0), "Chest opens once");
            game.RegisterOverworldMissionClear(); Check(game.MarkOverworldChestOpened(1), "Second chest opens one clear later");
            game.RegisterOverworldMissionClear(); Check(!game.IsOverworldChestAvailable(0), "Chest unavailable after two clears");
            game.RegisterOverworldMissionClear(); Check(game.IsOverworldChestAvailable(0) && !game.IsOverworldChestAvailable(1), "Chest cooldown measured per opening");
            game.RegisterOverworldMissionClear(); Check(game.IsOverworldChestAvailable(1), "Second chest returns after own three clears");
            Vector3 remembered = geo.Camps[3]; game.RememberOverworldPosition(remembered, true); game.ContinueOverworldGame(); yield return null;
            Check(game.IsOverworld && Vector3.Distance(game.PlayerPosition, remembered) < 1, "Continue restores remembered outdoor position");
            game.invincible = 99999;
            Call("SpawnFoe", game.PlayerPosition + Vector3.right * 12, 0);
            var foe = game.Enemies[game.Enemies.Count - 1];
            Check(Mathf.Abs(game.GetOverworldEnemyLevel(foe) - game.DebtLevel) <= 1, "Outdoor enemy level within player plus/minus one");
            Set("cleared", false); int oldRoom = (int)Get("room"); game.FoeKilled(foe);
            Check(!(bool)Get("cleared") && (int)Get("room") == oldRoom, "Outdoor enemy kill cannot clear or advance floor");
            foreach (var enemy in game.Enemies) if (enemy) enemy.enabled = false;
            var views = new[] { geo.Camps[2], new Vector3(110,.65f,-50), new Vector3(-105,.65f,48), new Vector3(geo.RiverX(5),.65f,5) };
            var names = new[] { "camp", "town", "park", "bridge" };
            foreach (bool ja in new[] { false, true })
            {
                VaultLanguage.Set(ja, false); string lang = ja ? "ja" : "en";
                for (int i = 0; i < views.Length; i++) { Move(views[i]); yield return Capture(names[i] + "-" + lang); }
                game.OpenOverworldMap(); Check(game.screen == VaultGame.Screen.DungeonSelect, "World map opens " + lang); yield return Capture("map-" + lang); game.CloseOverworldMap();
            }
            if (MobileTouchControls.Enabled)
            {
                var control = FindAnyObjectByType<MobileTouchControls>();
                foreach (var size in new[] { new Vector2Int(2532,1170), new Vector2Int(1600,1200) })
                {
                    Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                    for (int i=0; i<90 && (Screen.width!=size.x || Screen.height!=size.y); i++) yield return null;
                    if (control) control.ReviewSafeArea = new Rect(0,24,Screen.width,Screen.height-24);
                    Check(Screen.width==size.x && Screen.height==size.y,"Mobile framebuffer " + size);
                    foreach (bool ja in new[] {false,true}) { VaultLanguage.Set(ja,false); Move(geo.Camps[2]); yield return Capture("mobile-"+size.x+"-"+(ja?"ja":"en")); game.OpenOverworldMap();yield return Capture("mobile-map-"+size.x+"-"+(ja?"ja":"en"));game.CloseOverworldMap(); }
                }
                if (control) control.ReviewSafeArea=null;
            }
            else results.Add("Mobile captures require --mobile-controls; desktop captures completed.");
        }
    }
}



