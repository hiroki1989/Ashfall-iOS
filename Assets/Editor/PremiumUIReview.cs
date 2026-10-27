using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NeonVault.Editor
{
    // Opt-in review only. No player saves are modified: the runtime smoke flag
    // is set before presenting any inventory screen.
    [InitializeOnLoad]
    public static class PremiumUIReview
    {
        const string Output="D:/Unity/QA-premium-ui";
        static double next;
        static int step;
        static PremiumUIReview(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--premium-ui-review")>=0)EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"/result.txt","REVIEW_RUNNING / "+DateTime.UtcNow.ToString("O"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+12;
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+4;
            var game=UnityEngine.Object.FindAnyObjectByType<VaultGame>();if(!game)return;
            try
            {
                if(step==0)
                {
                    typeof(VaultGame).GetField("smoke",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,true);
                    game.CompleteWorldBossConversation();game.screen=VaultGame.Screen.Hub;
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"--equipment-new-review")>=0)
                    {
                        var chosen=Resources.Load<Font>("Fonts/851Gkktt-005");if(!chosen||game.GetType().GetField("font",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game)!=chosen)throw new Exception("Requested font not active");
                        if(!Resources.Load<Texture2D>("Art/UI/FieldKit/gun-class-icons-v1"))throw new Exception("Gun class atlas missing");
                        for(int rarity=0;rarity<5;rarity++){
                            var item=MeleeWeaponCatalog.Create(new System.Random(100+rarity),3,(Rarity)rarity);
                            if(item.affixes==null||item.affixes.Length!=rarity)throw new Exception("Melee rarity affix count");
                            float damage=item.damage,interval=item.interval;MeleeWeaponCatalog.EnsureAffixes(item);
                            if(item.damage!=damage||item.interval!=interval)throw new Exception("Repeated melee affix baking");
                        }
                        File.WriteAllText(Output+"/new-equipment-check.txt","FONT_ACTIVE / CLASS_ATLAS / MELEE_0_TO_4_AFFIXES / IDEMPOTENT_LOAD passed");
                    }
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"--real-city-world-review")>=0)
                    {
                        var roads=game.WorldGeography.GetRoadSegmentsForChunk(-14,-21);
                        Vector3 point=new Vector3(-1375,.65f,-2075);
                        foreach(var road in roads){var middle=(road.A+road.B)*.5f;if(!game.WorldGeography.Blocked(middle,.65f)){point=middle;break;}}
                        point.y=.65f;game.BuildOverworld(point);VaultLanguage.Set(true,false);Time.timeScale=0;
                        var worldView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));worldView.Focus();
                        step=20;next=EditorApplication.timeSinceStartup+15;return;
                    }
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"--premium-map-only")>=0){game.OpenOverworldMap();VaultLanguage.Set(true,false);var mapView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));mapView.Focus();step=5;next=EditorApplication.timeSinceStartup+30;return;}
                    else game.ToggleInventory();VaultLanguage.Set(Array.IndexOf(Environment.GetCommandLineArgs(),"--premium-english")<0,false);
                    var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();
                }
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--equipment-new-review")>=0&&step>=2)
                {
                    var category=typeof(VaultGame).GetField("eqCategory",BindingFlags.NonPublic|BindingFlags.Instance);
                    if(step==2)category.SetValue(game,1);
                    if(step==3)ScreenCapture.CaptureScreenshot(Output+"/new-equipment-armor.png");
                    if(step==4)category.SetValue(game,3);
                    if(step==5)ScreenCapture.CaptureScreenshot(Output+"/new-equipment-melee.png");
                    if(step==6){game.ToggleInventory();game.screen=VaultGame.Screen.Menu;game.enabled=false;game.gameObject.AddComponent<EquipmentHudReviewOverlay>().game=game;}
                    if(step==7)ScreenCapture.CaptureScreenshot(Output+"/new-equipment-hud.png");
                    if(step==8){File.WriteAllText(Output+"/inventory-result.txt","NEW_EQUIPMENT_REVIEW_COMPLETED / guns armor melee actual HUD component");EditorApplication.Exit(0);return;}
                    step++;return;
                }
                if(step==1)ScreenCapture.CaptureScreenshot(Output+(MobileTouchControls.Enabled?"/inventory-mobile.png":"/inventory-ja.png"));
                if(step==20)ScreenCapture.CaptureScreenshot(Output+"/real-city-world.png");
                if(step==21)
                {
                    var stream=UnityEngine.Object.FindAnyObjectByType<OverworldStream>();
                    if(!stream||!stream.Ready||stream.LoadedChunkCount>9||game.WorldGeography.Blocked(game.PlayerPosition,.5f))throw new Exception("World stream / safe spawn failed");
                    File.WriteAllText(Output+"/real-city-world-result.txt","WORLD_REVIEW_COMPLETED / CHUNKS="+stream.LoadedChunkCount+" / PLAYER="+game.PlayerPosition);
                    EditorApplication.Exit(0);return;
                }
                if(step==2&&Array.IndexOf(Environment.GetCommandLineArgs(),"--premium-inventory-only")>=0){File.WriteAllText(Output+"/inventory-result.txt","INVENTORY_REVIEW_COMPLETED");EditorApplication.Exit(0);return;}
                if(step==2)VaultLanguage.Set(false,false);
                if(step==3)ScreenCapture.CaptureScreenshot(Output+"/inventory-en.png");
                if(step==4){game.ToggleInventory();game.CompleteWorldBossConversation();game.OpenOverworldMap();}
                if(step==4)next=EditorApplication.timeSinceStartup+25;
                if(step==5)ScreenCapture.CaptureScreenshot(Output+"/map-en.png");
                                if(step==6&&Array.IndexOf(Environment.GetCommandLineArgs(),"--aerial-review")>=0)
                {
                    const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
                    var center=typeof(VaultGame).GetField("worldMapCenter",flags);
                    var zoom=typeof(VaultGame).GetField("worldMapZoom",flags);
                    Vector2 before=(Vector2)center.GetValue(game);float old=(float)zoom.GetValue(game);
                    var anchor=new Vector2(600,330);var rect=new Rect(30,100,900,650);
                    float aspect=(float)typeof(VaultGame).GetField("worldMapVerticalScale",flags).GetValue(game);
                    var worldBefore=before+new Vector2((anchor.x-rect.center.x)/(rect.width*old),-(anchor.y-rect.center.y)/(rect.width*old*aspect));
                    typeof(VaultGame).GetMethod("SetWorldMapZoom",flags).Invoke(game,new object[]{4.5f,anchor,rect});
                    Vector2 after=(Vector2)center.GetValue(game);float current=(float)zoom.GetValue(game);
                    var worldAfter=after+new Vector2((anchor.x-rect.center.x)/(rect.width*current),-(anchor.y-rect.center.y)/(rect.width*current*aspect));
                    bool aerial=(bool)typeof(VaultGame).GetField("aerialAtlas",flags).GetValue(game);
                    var tiles=(Texture2D[])typeof(VaultGame).GetField("aerialDetailTiles",flags).GetValue(game);
                    if(Array.Exists(tiles,t=>!t||t.width!=4096))throw new Exception("Detailed aerial tile missing / "+string.Join(",",Array.ConvertAll(tiles,t=>t?t.width.ToString():"null")));
                    if(!aerial||Vector2.Distance(worldBefore,worldAfter)>.0001f)throw new Exception("Aerial atlas or zoom anchor check failed");
                    File.WriteAllText(Output+"/aerial-ui-check.txt","BAKED_ATLAS=True / TILES=4x4096 / ZOOM_ANCHOR_ERROR="+Vector2.Distance(worldBefore,worldAfter)+" / AXIS_CORRECTION="+aspect);
                    step=7;return;
                }
                if(step==7)ScreenCapture.CaptureScreenshot(Output+"/map-aerial-close.png");
                if(step==8){VaultLanguage.Set(false,false);}
                if(step==9)ScreenCapture.CaptureScreenshot(Output+"/map-aerial-en.png");
                if(step==10||step==6){File.WriteAllText(Output+"/result.txt","REVIEW_COMPLETED");EditorApplication.Exit(0);}
                step++;
            }
            catch(Exception e){File.WriteAllText(Output+"/result.txt",e.ToString());EditorApplication.Exit(2);}
        }
    }
}

namespace NeonVault.Editor {
 public sealed class EquipmentHudReviewOverlay:UnityEngine.MonoBehaviour {
  public VaultGame game;
  void OnGUI(){if(!game)return;GUI.depth=-1000;GUI.color=Color.white;GUI.matrix=VaultGame.MobileCanvasMatrix;GUI.DrawTexture(new Rect(0,0,1440,900),Texture2D.blackTexture);typeof(VaultGame).GetMethod("DrawHeldWeaponHUD",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,new object[]{new Rect(456,20,370,104)});}
 }
}
