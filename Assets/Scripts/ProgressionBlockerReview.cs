using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace NeonVault
{
    // Explicit, finite runtime regression. Snapshot precedes VaultGame.Awake.
    public sealed class ProgressionBlockerReview:MonoBehaviour
    {
        const string Flag="--progression-blocker-review",Checkpoint="Vault.ExpeditionCheckpoint.v1";
        static readonly string[] StringKeys={"Vault.Progression.v1","Vault.Loadout.v1","Vault.DebtSkills.v1","Vault.Overworld.v1",Checkpoint};
        static readonly string[] IntKeys={"Vault.Language","Vault.Cores","Vault.Rank","Vault.FirstBossLegendaryD0","Vault.DebtBoss.defeated.v1","Vault.FactionBoss.Intro.196","Vault.FactionBoss.Intro.197","Vault.FactionBoss.Intro.200","Vault.FactionBoss.Intro.201","Vault.FactionBoss.Intro.202","Vault.FactionBoss.Intro.203","Vault.FactionBoss.Intro.204","Vault.FactionBoss.Intro.205","Vault.BossRelic.Defeated.0","Vault.BossRelic.Defeated.1","Vault.BossRelic.Defeated.2"};
        static readonly Dictionary<string,string> strings=new Dictionary<string,string>();
        static readonly Dictionary<string,int> ints=new Dictionary<string,int>();
        readonly List<string> lines=new List<string>();int checks,failures;VaultGame game;bool restored;
        static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Snapshot()
        {
            if(!Requested)return;
            foreach(string key in StringKeys){if(PlayerPrefs.HasKey(key))strings[key]=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);}
            foreach(string key in IntKeys){if(PlayerPrefs.HasKey(key))ints[key]=PlayerPrefs.GetInt(key);PlayerPrefs.DeleteKey(key);}
            Time.timeScale=0; // Regression: a previous paused session must not freeze startup.
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Requested)new GameObject("Progression blocker review").AddComponent<ProgressionBlockerReview>();}
        void Check(bool ok,string name){checks++;if(!ok)failures++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        static FieldInfo Field(string name)=>typeof(VaultGame).GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        void Set(string name,object value)=>Field(name).SetValue(game,value);
        static bool HasBossRelicDrop(VaultGame target,string expected)
        {
            foreach(string listName in new[]{"drops","armorDrops","meleeDrops","artifactDrops"})
            {
                var list=Field(listName)?.GetValue(target) as System.Collections.IEnumerable;if(list==null)continue;
                foreach(object entry in list)
                {
                    if(entry==null)continue;
                    foreach(var member in entry.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                    {
                        object item=member.GetValue(entry);if(item==null)continue;
                        var identity=item.GetType().GetField("bossRelicId",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                        if(identity!=null&&string.Equals(identity.GetValue(item) as string,expected,StringComparison.Ordinal))return true;
                    }
                }
            }
            return false;
        }
        IEnumerator ReviewFactionBoss(int stage,int bossId)
        {
            Check(game.EnterExpeditionStage(stage),"faction headquarters stage enters / "+stage);
            Set("room",game.CurrentDungeon.AreaCount-1);game.BuildRoom(true);
            int bossCount=0,escorts=0;Foe boss=null;
            foreach(var foe in game.Enemies)if(foe!=null){if(foe.kind==3&&foe.MajorBoss){bossCount++;boss=foe;}else if(foe.kind==10&&foe.elite)escorts++;}
            Check(bossCount==1&&escorts==5&&game.Enemies.Count==6,"faction final floor spawns one boss and five elite escorts / "+stage);
            string intro="Vault.FactionBoss.Intro."+stage;PlayerPrefs.DeleteKey(intro);Time.timeScale=1;game.BeginFactionBossConversation();
            Check(game.FactionBossConversationVisible&&Time.timeScale==0,"first faction boss encounter pauses for dialogue / "+stage);
            Set("factionBossConversation",false);Time.timeScale=1;
            string relicFlag="Vault.BossRelic.Defeated."+bossId;PlayerPrefs.DeleteKey(relicFlag);
            if(boss!=null)boss.Hit(float.MaxValue,null,null,1000);
            bool expectedEscape=VaultGame.IsFactionBossEscapeStage(stage);
            Check(boss!=null&&boss.dead&&boss.Escaping==expectedEscape,(expectedEscape?"30%/60% faction boss escapes on defeat / ":"100% faction boss dies on defeat / ")+stage);
            int category=ExtractBossRelicCategory(game,bossId);
            Check(PlayerPrefs.GetInt(relicFlag,0)==1&&category>=0,"first faction boss kill emits a boss-exclusive legendary / "+stage);
            foreach(var foe in new List<Foe>(game.Enemies))if(foe!=null&&!foe.dead)foe.Hit(float.MaxValue,null,null,1000);
            game.Advance();Check(game.screen==VaultGame.Screen.Victory,"faction headquarters can be fully cleared / "+stage);
            game.EnterHub();
            Check(game.EnterExpeditionStage(stage),"faction boss arena can be replayed / "+stage);
            Set("room",game.CurrentDungeon.AreaCount-1);game.BuildRoom(true);
            int replayBosses=0;foreach(var foe in game.Enemies)if(foe!=null&&foe.kind==3&&foe.MajorBoss)replayBosses++;
            Check(replayBosses==1,"faction replay rebuilds its boss encounter / "+stage);
            yield return null;
        }
        int ExtractBossRelicCategory(VaultGame target,int bossId)
        {
            for(int category=0;category<BossRelicFoundation.CategoryCount;category++)if(HasBossRelicDrop(target,BossRelicFoundation.Id(bossId,category)))return category;
            return -1;
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");string output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/QA-progression-blocker";Directory.CreateDirectory(output);
            var test=Run();while(true){bool next;try{next=test.MoveNext();}catch(Exception e){Check(false,e.ToString());break;}if(!next)break;yield return test.Current;}
            Restore();lines.Add("Checks="+checks+" Failures="+failures);File.WriteAllLines(Path.Combine(output,"progression-blocker-review-result.txt"),lines);Debug.Log("ASHFALL_PROGRESSION_BLOCKER_REVIEW Checks="+checks+" Failures="+failures);Application.Quit(failures==0?0:1);
        }
        IEnumerator Run()
        {
            for(int i=0;i<180;i++){game=FindAnyObjectByType<VaultGame>();if(game)break;yield return null;}
            Check(game!=null,"fresh startup initialized");if(!game)yield break;
            Check(Time.timeScale==1,"fresh startup resets inherited pause");game.enabled=false;Set("debtIntroSeen",true);Set("smoke",true);
            for(int seed=0;seed<120;seed++)
            {
                var layout=new SectorLayout(new System.Random(seed*7919),seed%3);bool spawn=false;
                for(int x=0;x<layout.Columns&&!spawn;x++)for(int y=0;y<layout.Rows;y++){var p=layout.Center(new Vector2Int(x,y));if(layout.SpawnSafe(p)&&!layout.Blocked(p,.8f)){spawn=true;break;}}
                Check(spawn&&layout.ExitPathCells>0,"finite layout spawn and reachable exit seed "+seed);
            }
            for(int dungeon=0;dungeon<3;dungeon++)for(int variant=0;variant<2;variant++)
            {
                Set("manufacturerRegistry",true);Time.timeScale=0;typeof(VaultGame).GetProperty("DungeonIndex").SetValue(game,dungeon);game.StartRun(10531+variant*7919);int floors=DungeonCatalog.Get(dungeon).AreaCount;Check(!(bool)Field("manufacturerRegistry").GetValue(game)&&Time.timeScale==1,"run resets stale registry and pause");
                for(int floor=0;floor<floors;floor++)
                {
                    Check(game.room==floor&&game.screen==VaultGame.Screen.Playing,"actual floor initialized "+dungeon+"/"+variant+"/"+floor);
                    Check(game.Enemies.Count>0,"actual floor has enemies");Check(!game.Blocked(game.PlayerPosition,.45f),"spawn player can move");
                    foreach(var enemy in game.Enemies){Check(enemy!=null&&!enemy.dead&&!game.Blocked(enemy.transform.position,.45f),"enemy alive on walkable floor");}
                    if(floor==0&&variant==0)
                    {
                        Set("smoke",false);game.screen=VaultGame.Screen.Pause;Time.timeScale=0;Check(game.SaveRunCheckpoint(),"actual checkpoint written");string checkpoint=PlayerPrefs.GetString(Checkpoint);int expected=game.Enemies.Count;
                        Check(game.ResumeSavedRun(),"actual checkpoint restored");Check(game.Enemies.Count==expected&&game.Active&&Time.timeScale==1,"restored actors and pause state");Check(!game.Blocked(game.PlayerPosition,.45f),"restored player walkable");
                        string empty=checkpoint;var objectType=typeof(VaultGame).GetNestedType("RunCheckpoint",BindingFlags.NonPublic);object state=JsonUtility.FromJson(empty,objectType);objectType.GetField("foes").SetValue(state,Activator.CreateInstance(objectType.GetField("foes").FieldType));objectType.GetField("cleared").SetValue(state,false);PlayerPrefs.SetString(Checkpoint,JsonUtility.ToJson(state));
                        Check(game.ResumeSavedRun()&&(bool)Field("cleared").GetValue(game),"legacy zero-enemy checkpoint opens exit");
                        PlayerPrefs.SetString(Checkpoint,checkpoint);Check(game.ResumeSavedRun(),"restore combat checkpoint after empty case");
                        string loadout=PlayerPrefs.GetString("Vault.Loadout.v1");PlayerPrefs.SetString(Checkpoint,"{}");Check(!game.ResumeSavedRun()&&PlayerPrefs.GetString(Checkpoint)=="{}"&&PlayerPrefs.GetString("Vault.Loadout.v1")==loadout,"invalid checkpoint retained without equipment writes");PlayerPrefs.DeleteKey(Checkpoint);Set("smoke",true);
                    }
                    // Exercise the real kill completion, reward and construction path.
                    var foes=new List<Foe>(game.Enemies);foreach(var enemy in foes){enemy.Hit(float.MaxValue,null,null,1000);}
                    Check((bool)Field("cleared").GetValue(game),"last kill opens exit");game.Advance();
                    if(floor<floors-1){Check(game.screen==VaultGame.Screen.Reward,"advance opens reward");game.ChooseReward(0);int advanced=game.room;game.ChooseReward(0);Check(game.room==advanced,"duplicate reward cannot skip floor");}
                    else Check(game.screen==VaultGame.Screen.Victory,"final floor extracts");
                    yield return null;yield return null;
                }
                game.EnterHub();Check(game.screen==VaultGame.Screen.Hub&&Time.timeScale==1&&!game.Blocked(game.PlayerPosition,.45f),"hub return movable");
                game.StartRun(917+variant);game.invincible=0;game.Hurt(float.MaxValue);Check(game.screen==VaultGame.Screen.Dead,"real death path");game.EnterHub();Check(game.screen==VaultGame.Screen.Hub&&Time.timeScale==1,"death returns to hub");yield return null;
            }
            var clearedStages=Field("clearedExpeditionStages").GetValue(game) as List<int>;
            clearedStages.Clear();
            var raiderStages=new List<int>();var gangStages=new List<int>();
            for(int id=0;id<ExpeditionMap.RegularStageCount;id++)
            {
                if(ExpeditionMap.FactionForStage(id)==DungeonFaction.Raiders)raiderStages.Add(id);
                else if(ExpeditionMap.FactionForStage(id)==DungeonFaction.Gang)gangStages.Add(id);
            }
            Check(raiderStages.Count==50&&gangStages.Count==50,"50 regular Raider and 50 regular Gang dungeons exist");
            Check(!game.FactionHeadquartersUnlocked(VaultGame.RedClawBoss30)&&!game.FactionHeadquartersUnlocked(VaultGame.CoyoteBoss30)&&!game.EnterExpeditionStage(VaultGame.RedClawBoss30),"faction boss dungeons remain locked before 30% faction clear");
            for(int i=0;i<14;i++)clearedStages.Add(raiderStages[i]);
            Check(!game.FactionHeadquartersUnlocked(VaultGame.RedClawBoss30),"Red Claw 30% boss stays locked at 14/50 clears");
            clearedStages.Add(raiderStages[14]);
            Check(game.FactionHeadquartersUnlocked(VaultGame.RedClawBoss30)&&!game.FactionHeadquartersUnlocked(VaultGame.RedClawBoss60),"Red Claw 30% boss unlocks at 15/50 clears (30%)");
            typeof(VaultGame).GetMethod("UpdateFactionBossUnlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{14,0});
            Check(game.OverworldWaypointKind==2&&game.OverworldWaypointId==VaultGame.RedClawBoss30,"30% Red Claw milestone marks stage 200 on the map");
            yield return ReviewFactionBoss(VaultGame.RedClawBoss30,0);
            for(int i=15;i<30;i++)clearedStages.Add(raiderStages[i]);
            Check(game.FactionHeadquartersUnlocked(VaultGame.RedClawBoss60)&&!game.FactionHeadquartersUnlocked(VaultGame.RedClawHeadquarters),"Red Claw 60% boss unlocks at 30/50 clears (60%)");
            typeof(VaultGame).GetMethod("UpdateFactionBossUnlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{29,0});
            Check(game.OverworldWaypointId==VaultGame.RedClawBoss60,"60% Red Claw milestone marks stage 201 on the map");
            yield return ReviewFactionBoss(VaultGame.RedClawBoss60,0);
            for(int i=30;i<50;i++)clearedStages.Add(raiderStages[i]);
            Check(game.FactionHeadquartersUnlocked(VaultGame.RedClawHeadquarters),"Red Claw 100% stronghold unlocks at 50/50 clears (100%)");
            typeof(VaultGame).GetMethod("UpdateFactionBossUnlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{49,0});
            Check(game.OverworldWaypointId==VaultGame.RedClawHeadquarters,"100% Red Claw milestone marks stage 202 on the map");
            yield return ReviewFactionBoss(VaultGame.RedClawHeadquarters,0);
            for(int i=0;i<15;i++)clearedStages.Add(gangStages[i]);
            Check(game.FactionHeadquartersUnlocked(VaultGame.CoyoteBoss30)&&!game.FactionHeadquartersUnlocked(VaultGame.CoyoteBoss60),"Coyote 30% boss unlocks at 15/50 clears (30%)");
            typeof(VaultGame).GetMethod("UpdateFactionBossUnlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{50,14});
            Check(game.OverworldWaypointId==VaultGame.CoyoteBoss30,"30% Coyote milestone marks stage 203 on the map");
            yield return ReviewFactionBoss(VaultGame.CoyoteBoss30,1);
            for(int i=15;i<30;i++)clearedStages.Add(gangStages[i]);
            Check(game.FactionHeadquartersUnlocked(VaultGame.CoyoteBoss60)&&!game.FactionHeadquartersUnlocked(VaultGame.CoyoteHeadquarters),"Coyote 60% boss unlocks at 30/50 clears (60%)");
            typeof(VaultGame).GetMethod("UpdateFactionBossUnlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{50,29});
            Check(game.OverworldWaypointId==VaultGame.CoyoteBoss60,"60% Coyote milestone marks stage 204 on the map");
            yield return ReviewFactionBoss(VaultGame.CoyoteBoss60,1);
            for(int i=30;i<50;i++)clearedStages.Add(gangStages[i]);
            Check(game.FactionHeadquartersUnlocked(VaultGame.CoyoteHeadquarters),"Coyote 100% stronghold unlocks at 50/50 clears (100%)");
            typeof(VaultGame).GetMethod("UpdateFactionBossUnlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{50,49});
            Check(game.OverworldWaypointId==VaultGame.CoyoteHeadquarters,"100% Coyote milestone marks stage 205 on the map");
            yield return ReviewFactionBoss(VaultGame.CoyoteHeadquarters,1);
            Set("debtPaid",1000000000);PlayerPrefs.SetInt("Vault.DebtBoss.defeated.v1",1);
            typeof(VaultGame).GetMethod("StartValeRematch",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            // StartValeRematch marks the encounter before BuildRoom; this flag stays active through the run.
            Check(game.room==game.CurrentDungeon.AreaCount-1&&(bool)Field("finalDebtBossFight").GetValue(game),"Vale rematch path starts on the dedicated final floor");
            var vale=game.Enemies.Find(e=>e!=null&&e.kind==3&&e.MajorBoss);if(vale!=null)vale.Hit(float.MaxValue,null,null,1000);
            Check(PlayerPrefs.GetInt("Vault.BossRelic.Defeated.2",0)==1&&ExtractBossRelicCategory(game,2)>=0,"first Vale kill emits a Vale-exclusive legendary");
            foreach(var foe in new List<Foe>(game.Enemies))if(foe!=null&&!foe.dead)foe.Hit(float.MaxValue,null,null,1000);
            game.Advance();Check(game.screen==VaultGame.Screen.Victory,"Vale defeat reaches the completion screen");
            game.EnterHub();typeof(VaultGame).GetMethod("StartValeRematch",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            Check((bool)Field("finalDebtBossFight").GetValue(game)&&game.room==game.CurrentDungeon.AreaCount-1,"Vale can be challenged again after victory");
            foreach(string key in new[]{"knox","vale","raven","vex","sera","briggs","gideon"})
            {
                var tex=VaultGame.LoadDialoguePortrait(key);
                Check(tex!=null&&tex.width>=512&&tex.height>=512,"dialogue portrait loaded / "+key);
            }
        }
        void Restore(){if(restored)return;restored=true;if(game)game.enabled=false;foreach(string key in StringKeys){if(strings.TryGetValue(key,out string value))PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);}foreach(string key in IntKeys){if(ints.TryGetValue(key,out int value))PlayerPrefs.SetInt(key,value);else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();Time.timeScale=1;}
        void OnApplicationQuit(){Restore();}
        void OnDestroy(){Restore();}
    }
}
