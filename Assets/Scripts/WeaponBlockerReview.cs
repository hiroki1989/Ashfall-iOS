using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace NeonVault
{
    // Finite opt-in regression; use --weapon-blocker-review --mobile-controls.
    public sealed class WeaponBlockerReview:MonoBehaviour
    {
        const string Flag="--weapon-blocker-review";
        static readonly string[] Strings={"Vault.Progression.v1","Vault.Loadout.v1","Vault.DebtSkills.v1","Vault.ExpeditionCheckpoint.v1"};
        static readonly string[] Ints={"Vault.Language","Vault.Cores","Vault.Rank","Vault.FirstBossLegendaryD0"};
        static readonly Dictionary<string,string> oldStrings=new Dictionary<string,string>();
        static readonly Dictionary<string,int> oldInts=new Dictionary<string,int>();
        static readonly HashSet<string> existed=new HashSet<string>();
        readonly List<string> log=new List<string>();int checks,failures;VaultGame game;
        static bool Requested()=>Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Snapshot(){if(!Requested())return;foreach(var k in Strings){if(PlayerPrefs.HasKey(k))existed.Add(k);oldStrings[k]=PlayerPrefs.GetString(k);}foreach(var k in Ints){if(PlayerPrefs.HasKey(k))existed.Add(k);oldInts[k]=PlayerPrefs.GetInt(k);}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Requested())new GameObject("Weapon blocker review").AddComponent<WeaponBlockerReview>();}
        IEnumerator Start()
        {
            yield return null;var run=Review();while(true){bool next;try{next=run.MoveNext();}catch(Exception e){Check(false,e.ToString());break;}if(!next)break;yield return run.Current;}
            Restore();var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");string output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/QA-weapon-blocker";Directory.CreateDirectory(output);log.Add("Checks="+checks+" Failures="+failures);File.WriteAllLines(Path.Combine(output,"weapon-blocker-review-result.txt"),log);Debug.Log("ASHFALL_WEAPON_BLOCKER Checks="+checks+" Failures="+failures);Application.Quit(failures==0?0:1);
        }
        IEnumerator Review()
        {
            for(int i=0;i<120&&!game;i++){game=FindAnyObjectByType<VaultGame>();yield return null;}Check(game,"game initialized");if(!game)yield break;
            Field("smoke",true);Field("debtIntroSeen",true);game.StartRun(731904);game.invincible=999999;foreach(var foe in game.Enemies)if(foe)foe.enabled=false;
            yield return null;yield return null;game.enabled=false;
            var control=FindAnyObjectByType<MobileTouchControls>();Check(MobileTouchControls.Enabled&&control,"mobile input boundary available");
            var driver=game.PlayerAppearance.GetComponent<DetailedHeroPlayer3D>();
            var update=typeof(VaultGame).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
            var guns=new List<Gun>();foreach(var recipe in DistinctEquipmentDesigns.Guns)guns.Add(Arsenal.FromRecipe(recipe,Rarity.Rare));for(int i=0;i<Arsenal.ModelCount;i++){var legacy=Arsenal.Model(i,Rarity.Common);legacy.catalogId=null;legacy.manufacturerId=null;guns.Add(legacy);}
            foreach(var gun in guns)
            {
                game.Weapons.Clear();game.Weapons.Add(gun);game.EquipWeapon(0,0);driver.Synchronize();
                var root=driver.EquippedWeaponRoot;Check(root&&root.parent==driver.transform,"equipped root follows player "+gun.id);
                Check(Finite(game.PlayerWeapon.MuzzlePosition)&&Finite(game.PlayerAppearance.RightHandPosition)&&Finite(game.PlayerAppearance.LeftHandPosition),"finite hand/muzzle "+gun.id);
                Check(root&&game.PlayerWeapon.muzzle.IsChildOf(root),"live equipped muzzle "+gun.id);
                if(control)
                {
                    var rect=MobileTouchControls.MoveRect;Vector2 center=new Vector2(rect.center.x,Screen.height-rect.center.y);
                    Vector2 stick=Vector2.up;foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                    {var world=VaultMobileInputBridge.ToWorldMovement(direction);if(Vector3.Distance(game.PlayerPosition,game.MoveWithinRoom(game.PlayerPosition,world*.15f,.45f))>.05f){stick=direction;break;}}
                    control.InjectPointer(9182,center,TouchPhase.Began);control.InjectPointer(9182,center+stick*rect.height*.3f,TouchPhase.Moved);
                    var before=game.PlayerPosition;update.Invoke(game,null);control.InjectPointer(9182,center,TouchPhase.Ended);
                    Check(Vector3.Distance(before,game.PlayerPosition)>.00001f,"actual game update moves after switch "+gun.id);
                    // Return to the same clear starting cell so the finite test cannot walk into a wall.
                    driver.transform.position=before;
                }
                yield return null;
            }
            // A stale/destroyed assembly used to dereference current.root forever.
            var retired=driver.EquippedWeaponRoot;Destroy(retired.gameObject);yield return null;driver.Synchronize();
            Check(driver.EquippedWeaponRoot&&driver.EquippedWeaponRoot!=retired&&Finite(game.PlayerWeapon.MuzzlePosition),"destroyed assembly reconstitutes valid sockets");
            Check(game.VerifyPersistence(),"loadout selected gun and magazines restore");
            if(game.InventoryOpen)game.ToggleInventory();game.ToggleInventory();Check(Time.timeScale==0,"inventory pauses");game.ToggleInventory();Check(Time.timeScale==1&&game.Active,"inventory closes and resumes");
        }
        void Field(string name,object value)=>typeof(VaultGame).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,value);
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
        void Check(bool pass,string label){checks++;if(!pass){failures++;log.Add("FAIL "+label);}else if(checks%100==0)log.Add("PASS "+label);}
        static void Restore(){if(!Requested())return;foreach(var k in Strings)if(existed.Contains(k))PlayerPrefs.SetString(k,oldStrings[k]);else PlayerPrefs.DeleteKey(k);foreach(var k in Ints)if(existed.Contains(k))PlayerPrefs.SetInt(k,oldInts[k]);else PlayerPrefs.DeleteKey(k);PlayerPrefs.Save();}
        void OnApplicationQuit()=>Restore();
    }
}
