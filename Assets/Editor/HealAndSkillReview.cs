using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Isolated EditMode verification. It never reads or writes player saves.
public static class HealAndSkillReview
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> failures=new List<string>();
    static int checks;
    static FieldInfo Field(string name)=>typeof(VaultGame).GetField(name,Flags)??throw new MissingFieldException(name);
    static void Set(VaultGame g,string name,object v)=>Field(name).SetValue(g,v);
    static object Get(VaultGame g,string name)=>Field(name).GetValue(g);
    static void Tick(VaultGame g)=>typeof(VaultGame).GetMethod("TickHealNumber",Flags).Invoke(g,null);
    static void Check(bool ok,string name){checks++;if(!ok)failures.Add(name);}
    static bool Near(float a,float b)=>Mathf.Abs(a-b)<.001f;
    static void Flush(VaultGame g){Set(g,"healNumberAt",-1f);Tick(g);}
    public static void Run()
    {
        failures.Clear();checks=0;
        GameObject root=null;
        try
        {
            root=new GameObject("HealAndSkillReview isolated root");root.SetActive(false);
            var game=root.AddComponent<VaultGame>();Set(game,"smoke",true);
            var world=new GameObject("world").transform;world.SetParent(root.transform,false);
            var player=new GameObject("player").transform;player.SetParent(world,false);
            var camera=new GameObject("camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);
            Set(game,"world",world);Set(game,"player",player);Set(game,"cam",camera);
            game.screen=VaultGame.Screen.Playing;game.maxHp=100;game.hp=93;
            Check(Near(game.HealPlayer(25),7)&&Near(game.hp,100),"healing reports actual capped amount");
            Check(Near((float)Get(game,"pendingHealNumber"),7),"only capped amount queued");
            Check(Near(game.HealPlayer(10),0)&&Near((float)Get(game,"pendingHealNumber"),7),"full HP neither heals nor queues a zero number");
            int before=CombatVisuals.TotalDamageNumbers;Flush(game);
            Check(CombatVisuals.TotalDamageNumbers==before+1,"capped healing renders one number");
            var labels=root.GetComponentsInChildren<TextMesh>(true);
            Check(labels.Length==1&&labels[0].text=="+7"&&labels[0].color.g>labels[0].color.r,"heal number is green and shows effective amount");
            game.hp=50;game.HealPlayer(3.25f);game.hp-=10;game.HealPlayer(1.75f);
            Check(Near(game.hp,45)&&Near((float)Get(game,"pendingHealNumber"),5),"healing aggregates independently of same-frame damage");
            before=CombatVisuals.TotalDamageNumbers;Flush(game);
            Check(CombatVisuals.TotalDamageNumbers==before+1&&Near(CombatVisuals.LastDamage,5),"multiple healing sources emit one combined number");
            game.hp=90;game.screen=VaultGame.Screen.Reward;game.HealPlayer(8,true);
            Tick(game);Check(Near((float)Get(game,"pendingHealNumber"),8),"reward heal waits during floor transition");
            var nextWorld=new GameObject("next world").transform;nextWorld.SetParent(root.transform,false);player.SetParent(nextWorld,false);
            Set(game,"world",nextWorld);game.screen=VaultGame.Screen.Playing;before=CombatVisuals.TotalDamageNumbers;Flush(game);
            Check(CombatVisuals.TotalDamageNumbers==before+1&&Near(CombatVisuals.LastDamage,8),"reward heal follows player to new floor");
            game.hp=60;game.HealPlayer(2);game.screen=VaultGame.Screen.Dead;Tick(game);
            Check(Near((float)Get(game,"pendingHealNumber"),0),"pending heal discarded on death");
            Check(typeof(VaultGame).GetMethod("ShowMedicHealNumber",Flags)==null&&typeof(VaultGame).GetField("medicHealAccumulated",Flags)==null,"old medic-only number queue removed");

            var activation=Resources.Load<AudioClip>("ActionSkillAudio/activation");
            Check(activation!=null&&activation.length>0,"activation SE asset is loadable");
            Set(game,"actionSkillLoaded",true);Set(game,"actionSkillSelected",ActionSkillKind.Medic);
            Set(game,"art",new VaultArt(nextWorld,new List<Material>(),new System.Random(4)));
            game.screen=VaultGame.Screen.Playing;game.hp=50;Set(game,"<ActionSkillPoints>k__BackingField",0f);
            Check(!game.TryUseActionSkill()&&Get(game,"actionActivationSound")==null,"unready skill neither activates nor loads playback clip");
            Set(game,"<ActionSkillPoints>k__BackingField",15f);
            bool used=game.TryUseActionSkill();
            Check(used&&Near(game.ActionSkillPoints,0),"ready medic skill activates and consumes charge");
            Check(Get(game,"actionActivationSound") as AudioClip==activation,"successful activation selects the attached SE clip");
        }
        catch(Exception e){failures.Add(e.ToString());}
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        const string output="D:/Unity/BuildTemp/HealAndSkillReview/result.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string result="checks="+checks+" failures="+failures.Count+"\n"+string.Join("\n",failures);
        File.WriteAllText(output,result);Debug.Log("HEAL_SKILL_REVIEW "+result);
        if(Application.isBatchMode)EditorApplication.Exit(failures.Count==0?0:1);
    }
}
