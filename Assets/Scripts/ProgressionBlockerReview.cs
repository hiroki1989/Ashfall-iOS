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
        static readonly string[] StringKeys={"Vault.Progression.v1","Vault.Loadout.v1","Vault.DebtSkills.v1",Checkpoint};
        static readonly string[] IntKeys={"Vault.Language","Vault.Cores","Vault.Rank","Vault.FirstBossLegendaryD0"};
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
        }
        void Restore(){if(restored)return;restored=true;if(game)game.enabled=false;foreach(string key in StringKeys){if(strings.TryGetValue(key,out string value))PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);}foreach(string key in IntKeys){if(ints.TryGetValue(key,out int value))PlayerPrefs.SetInt(key,value);else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();Time.timeScale=1;}
        void OnApplicationQuit(){Restore();}
        void OnDestroy(){Restore();}
    }
}
