using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NeonVault
{
    // Finite visual review for the current gameplay revision. It uses an isolated
    // runtime session and restores every persistent key touched during startup.
    public sealed class GameplayRevisionReview : MonoBehaviour
    {
        const string Flag = "--gameplay-revision-review";
        static readonly string[] IntKeys = { "Vault.Language", "Vault.Cores", "Vault.Rank", "Vault.FirstBossLegendaryD0" };
        static readonly string[] StringKeys = { "Vault.Progression.v1", "Vault.Loadout.v1", "Vault.DebtSkills.v1", "Vault.ExpeditionCheckpoint.v1" };
        sealed class SavedInt { public string key; public bool exists; public int value; }
        sealed class SavedString { public string key; public bool exists; public string value; }
        static List<SavedInt> savedInts;
        static List<SavedString> savedStrings;
        readonly List<string> results = new List<string>();
        VaultGame game;
        bool drawInventory,drawCatalog;
        bool inventoryDrawn;
        int checks, failures;
        string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SnapshotPreferences()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) < 0) return;
            savedInts = new List<SavedInt>(); savedStrings = new List<SavedString>();
            foreach (string key in IntKeys) savedInts.Add(new SavedInt { key=key, exists=PlayerPrefs.HasKey(key), value=PlayerPrefs.GetInt(key) });
            foreach (string key in StringKeys) savedStrings.Add(new SavedString { key=key, exists=PlayerPrefs.HasKey(key), value=PlayerPrefs.GetString(key) });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0)
                new GameObject("Gameplay revision review").AddComponent<GameplayRevisionReview>();
        }

        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int at=Array.IndexOf(args,"--qa-output");
            output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/QA-gameplay-revision";
            Directory.CreateDirectory(output);
            IEnumerator review=RunReview();
            while(true)
            {
                bool next;
                try { next=review.MoveNext(); }
                catch(Exception e) { Check(false,"review exception: "+e.GetType().Name+" / "+e.Message); break; }
                if(!next) break;
                yield return review.Current;
            }
            drawInventory=false;
            RestorePreferences();
            results.Add("Finite review only; physical touch, iOS/Metal performance, and long-session play were not exercised.");
            results.Add("Checks="+checks+" Failures="+failures);
            File.WriteAllLines(Path.Combine(output,"gameplay-revision-review-result.txt"),results);
            Debug.Log("ASHFALL_GAMEPLAY_REVISION_REVIEW Checks="+checks+" Failures="+failures);
            Application.Quit(failures==0?0:1);
        }

        IEnumerator RunReview()
        {
            try
            {
                for(int i=0;i<180;i++) { game=FindAnyObjectByType<VaultGame>(); if(game) break; yield return null; }
                Check(game!=null,"VaultGame initialized");
                if(game)
                {
                    SetField(game,"smoke",true);SetField(game,"debtIntroSeen",true);
                    game.StartRun(731904);
                    foreach(var foe in game.Enemies) if(foe) foe.enabled=false;
                    game.invincible=99999;
                    yield return null; yield return null;

                    // Exercise the same city marker hit target used by mouse/touch UI.
                    game.screen=VaultGame.Screen.DungeonSelect;
                    int stageId=37;
                    float zoom=GetField<float>(game,"expeditionCityZoom",.65f);
                    Rect mapView=MobileTouchControls.Enabled?game.MobileCityView:new Rect(42,150,952,663);
                    Vector2 scroll=ExpeditionMap.CityPoint(stageId)*zoom-mapView.size*.5f;SetField(game,"expeditionMapScroll",scroll);
                    Vector2 point=mapView.center;
                    bool selected=game.TrySelectCityStage(point);
                    int selectedField=GetField<int>(game,"expeditionMapSelection",-1);
                    Check(selected&&selectedField==stageId,"city map marker selects the intended stage by its visible hit region");
                    yield return Capture("city-map-stage-selection");

                    // Render the real mobile inventory method in a framebuffer review pass.
                    game.StartRun(731904); foreach(var foe in game.Enemies) if(foe) foe.enabled=false;
                    game.invincible=99999; game.ToggleInventory();
                    SetField(game,"mobileInventoryTab",2);
                    drawInventory=true; inventoryDrawn=false;
                    yield return null; yield return null; yield return new WaitForEndOfFrame();
                    float content=game.MobileInventoryContentHeight;
                    Check(inventoryDrawn&&content>=550,"mobile inventory layout executed and allocated a scrollable content region");
                    yield return Capture("mobile-inventory-modules");
                    for(int tab=0;tab<2;tab++){SetField(game,"mobileInventoryTab",tab);yield return Capture("mobile-inventory-"+(tab==0?"guns":"armor"));}
                    drawInventory=false;drawCatalog=true;for(int kind=0;kind<3;kind++){game.SetManufacturerReview(0,kind);yield return Capture("mobile-catalog-"+kind);}drawCatalog=false;SetField(game,"manufacturerRegistry",false);
                    drawInventory=false; game.ToggleInventory();

                    float groundY=game.PlayerPosition.y;
                    game.screen=VaultGame.Screen.Pause;SetField(game,"smoke",false);
                    bool saved=game.SaveRunCheckpoint();bool resumed=game.ResumeSavedRun();
                    SetField(game,"smoke",true);
                    Check(saved&&resumed&&Mathf.Abs(game.PlayerPosition.y-groundY)<.001f,"save/resume preserves grounded player height");
                    foreach(var foe in game.Enemies)if(foe)foe.enabled=false;
                    game.invincible=99999;
                    Invoke(game,"AddDrop",Arsenal.Model(12,Rarity.Legendary),game.PlayerPosition+Vector3.right);
                    Invoke(game,"SpawnFoe",game.PlayerPosition+Vector3.left*3,0);
                    game.Enemies[game.Enemies.Count-1].enabled=false;
                    typeof(VaultGame).GetProperty("ArtifactShield").SetValue(game,12f);Invoke(game,"UpdateBarrierVisual");
                    game.Layout.Discover(game.PlayerPosition);
                    yield return Capture("resumed-player-legendary-map");
                    // Formula checks cover ranges, reserve caps, legendary sale scaling and floor growth.
                    Gun sample=Arsenal.Model(12,Rarity.Common);
                    Check(Mathf.Approximately(VaultGame.GunDamageMin(sample),sample.damage*.85f)&&Mathf.Approximately(VaultGame.GunDamageMax(sample),sample.damage*1.15f),"weapon damage range retains the midpoint contract");
                    Check(Mathf.Approximately(VaultGame.GunCriticalChance(sample),.05f)&&Mathf.Approximately(VaultGame.GunCriticalMultiplier(sample),1.5f),"base critical values are explicit and stable");
                    MethodInfo legendary=typeof(VaultGame).GetMethod("LegendarySaleValue",BindingFlags.Static|BindingFlags.NonPublic);
                    int legendaryPrice=(int)legendary.Invoke(null,new object[]{Rarity.Legendary,1234});
                    int commonPrice=(int)legendary.Invoke(null,new object[]{Rarity.Rare,1234});
                    Check(legendaryPrice==12340&&commonPrice==1234,"legendary sale multiplier is ten without changing other rarities");
                    SetField(game,"room",3);
                    Check(Mathf.Approximately(game.FloorCombatMultiplier,1.2f*1.2f*1.2f),"floor combat multiplier grows by 1.2 per room");
                    Check(ExpeditionMap.RegularStageCount==200&&ExpeditionMap.StageCount==206&&ExpeditionMap.Get(80).region==4&&ExpeditionMap.Get(180).region==4,"expedition catalog contains five 40-site regions plus 6 faction boss sites");
                }
            }
            finally { drawInventory=false; }
        }

        void OnGUI()
        {
            if((!drawInventory&&!drawCatalog)||!game) return;
            Matrix4x4 old=GUI.matrix;
            GUI.matrix=VaultGame.MobileCanvasMatrix;
            if(drawCatalog)Invoke(game,"DrawMobileManufacturer");else game.DrawMobileInventory();
            GUI.matrix=old;
            inventoryDrawn=true;
        }

        IEnumerator Capture(string name)
        {
            yield return null; yield return new WaitForEndOfFrame();
            Texture2D image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image) { File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG()); Destroy(image); }
            Check(image!=null,"captured framebuffer / "+name);
        }

        static object Invoke(object target,string name,params object[] args)
        {return target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(target,args);}
        void OnApplicationQuit(){RestorePreferences();}
        void Check(bool pass,string label)
        {
            checks++; if(!pass) failures++;
            results.Add((pass?"PASS ":"FAIL ")+label);
        }

        static void SetField(object target,string name,object value)
        { var field=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); if(field!=null)field.SetValue(target,value); }
        static T GetField<T>(object target,string name,T fallback)
        { var field=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); return field==null?fallback:(T)field.GetValue(target); }

        static void RestorePreferences()
        {
            if(savedInts!=null) foreach(var item in savedInts) { if(item.exists)PlayerPrefs.SetInt(item.key,item.value);else PlayerPrefs.DeleteKey(item.key); }
            if(savedStrings!=null) foreach(var item in savedStrings) { if(item.exists)PlayerPrefs.SetString(item.key,item.value);else PlayerPrefs.DeleteKey(item.key); }
            PlayerPrefs.Save();
        }
    }
}
