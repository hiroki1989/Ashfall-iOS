using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Save-free editor check: invokes the actual Foe warning-update and firing paths.
public static class V40AimReview
{
    static readonly BindingFlags BF=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static void Set(object o,string n,object v){var f=o.GetType().GetField(n,BF);if(f==null)throw new MissingFieldException(o.GetType().Name,n);f.SetValue(o,v);}
    static object Get(object o,string n){var f=o.GetType().GetField(n,BF);if(f==null)throw new MissingFieldException(o.GetType().Name,n);return f.GetValue(o);}
    public static void Run()
    {
        GameObject root=null;int checks=0;string result;
        try
        {
            root=new GameObject("V40 isolated aim review");root.SetActive(false);
            var game=root.AddComponent<VaultGame>();game.screen=VaultGame.Screen.Playing;
            var world=new GameObject("world").transform;world.SetParent(root.transform,false);Set(game,"world",world);
            var player=new GameObject("player").transform;player.SetParent(world,false);Set(game,"player",player);
            var open=new bool[30,30];for(int x=0;x<30;x++)for(int y=0;y<30;y++)open[x,y]=true;
            var layout=new SectorLayout(open,new Vector2Int(15,15),new Vector2Int(16,16));Set(game,"<Layout>k__BackingField",layout);
            var actor=new GameObject("human");actor.transform.SetParent(world,false);var foe=actor.AddComponent<Foe>();actor.transform.position=layout.Entry;foe.Init(game,10,0);game.Enemies.Add(foe);
            Set(foe,"alerted",true);Set(foe,"cachedVisible",true);Set(foe,"visibilityNext",float.MaxValue);
            var update=typeof(Foe).GetMethod("Update",BF);var fire=typeof(Foe).GetMethod("FireRangedShot",BF);
            if(update==null||fire==null)throw new Exception("Missing Foe methods");
            foreach(float angle in new[]{0f,30f,90f,180f,270f})
            {
                Vector3 initial=Quaternion.Euler(0,angle,0)*Vector3.forward;player.position=actor.transform.position+initial*8;
                Vector3 muzzle=(Vector3)typeof(Foe).GetMethod("HumanMuzzlePosition",BF).Invoke(foe,null);
                GameObject warning=CombatVisuals.WarningLine(muzzle,initial,10,world);
                Set(foe,"warning",warning);Set(foe,"charging",true);Set(foe,"charge",.8f);Set(foe,"lockedAim",initial);
                // Move the target after warning creation to exercise live tracking.
                player.position=actor.transform.position+Quaternion.Euler(0,angle+22,0)*Vector3.forward*8;
                update.Invoke(foe,null);
                var line=warning.GetComponent<LineRenderer>();Vector3 lineWorld=warning.transform.TransformDirection(line.GetPosition(1)-line.GetPosition(0)).normalized;
                Vector3 locked=((Vector3)Get(foe,"lockedAim")).normalized;
                if(Vector3.Angle(lineWorld,locked)>.05f)throw new Exception("warning/aim mismatch at "+angle+": "+Vector3.Angle(lineWorld,locked));
                fire.Invoke(foe,null);var shots=world.GetComponentsInChildren<Shot>(true);if(shots.Length==0)throw new Exception("no projectile at "+angle);
                Shot shot=shots[shots.Length-1];if(Vector3.Angle(lineWorld,shot.velocity)>.05f)throw new Exception("warning/projectile mismatch at "+angle+": "+Vector3.Angle(lineWorld,shot.velocity));
                checks+=2;UnityEngine.Object.DestroyImmediate(warning);foreach(var s in shots)UnityEngine.Object.DestroyImmediate(s.gameObject);
            }
            result="PASS checks="+checks+"\nActual Foe.Update warning tracking and FireRangedShot projectile direction; five headings and moving target.\nDeath-screen visual coverage is not checked by this review.";
        }
        catch(Exception e){result="FAIL "+e;}
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        const string path="D:/Unity/BuildTemp/V40AimReview/result.txt";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,result);Debug.Log(result);
        if(Application.isBatchMode)EditorApplication.Exit(result.StartsWith("PASS")?0:1);
    }
}
