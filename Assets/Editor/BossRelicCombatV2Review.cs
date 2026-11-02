using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Temporary objects only: no user PlayerPrefs are read or written.
public static class BossRelicCombatV2Review
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly List<string> failures=new List<string>();
    static int checks;
    static FieldInfo Field(string name)=>typeof(VaultGame).GetField(name,Flags)??throw new MissingFieldException(name);
    static void Set(VaultGame g,string name,object value)=>Field(name).SetValue(g,value);
    static object Get(VaultGame g,string name)=>Field(name).GetValue(g);
    static void Call(VaultGame g,string method,params object[] args)=>(typeof(VaultGame).GetMethod(method,Flags)??throw new MissingMethodException(method)).Invoke(g,args);
    static void Check(bool ok,string message){checks++;if(!ok)failures.Add(message);}
    static int Shots(Transform world)=>world.GetComponentsInChildren<Shot>(true).Length;
    static int Pulses(VaultGame g)=>((IList)Get(g,"legendaryAftershocks")).Count;
    static float Precision(VaultGame g)=>(float)Get(g,"legendaryPrecisionUntil");
    static readonly string[,] En={
        {"guided","five-round","guided","guided","Detonate","trap","guided","traps","eight directions","parallel","explosions"},
        {"guided","explosions","six directions","Detonate","five-round","12%","aim","dodge","15%","18%","guided"},
        {"eight directions","gravity","parallel","gravity","guided","18 shield","gravity","0.6","14 shield","12 reserve","Blast"}};
    static readonly string[,] Ja={
        {"誘導弾","5発","誘導弾","誘導弾","爆発","火炎罠","援護弾","火炎罠","8方向","援護弾","爆発"},
        {"誘導弾","爆発","6方向","爆発","5発","12%","照準","回避","15%","18%","誘導弾"},
        {"8方向","重力渦","5発","重力渦","誘導弾","18","重力渦","0.6","14","12","爆破"}};
    public static void Run()
    {
        checks=0;failures.Clear();
        for(int boss=0;boss<3;boss++)for(int category=0;category<11;category++)
        {
            GameObject root=null;
            string id=BossRelicFoundation.Id(boss,category);
            try
            {
                var en=BossRelicFoundation.Description(id,false);var ja=BossRelicFoundation.Description(id,true);
                Check(en.Contains("On kill:")&&en.Contains("4s cooldown")&&en.Contains(En[boss,category]),id+" English description");
                Check(ja.Contains("撃破時")&&ja.Contains("クールダウン4秒")&&ja.Contains(Ja[boss,category]),id+" Japanese description");
                Check(BossRelicFoundation.Decode(id,out int b,out int c)&&b==boss&&c==category,id+" persistent identity");
                root=new GameObject("BossRelic review "+id);root.SetActive(false);
                var game=root.AddComponent<VaultGame>();Set(game,"smoke",true);Set(game,"actionSkillLoaded",true);
                var world=new GameObject("world").transform;world.SetParent(root.transform,false);Set(game,"world",world);
                var player=new GameObject("player").transform;player.SetParent(world,false);Set(game,"player",player);
                var map=new bool[16,16];for(int x=0;x<16;x++)for(int y=0;y<16;y++)map[x,y]=true;
                var layout=new SectorLayout(map,new Vector2Int(2,2),new Vector2Int(13,13));Set(game,"<Layout>k__BackingField",layout);
                player.position=layout.Center(new Vector2Int(5,5));Set(game,"aim",Vector3.forward);game.screen=VaultGame.Screen.Playing;
                Set(game,"art",new VaultArt(world,new List<Material>(),new System.Random(2)));
                game.gun=new Gun("review","",Rarity.Common,GunEffect.Normal,40,.5f,20,20,1);
                game.hp=25;game.maxHp=100;game.dodgeCooldown=3;game.invincible=0;
                for(int i=0;i<4;i++){var go=new GameObject("target "+i);go.transform.SetParent(world,false);go.transform.position=player.position+new Vector3((i-1.5f)*.8f,0,2+i*.6f);var foe=go.AddComponent<Foe>();foe.game=game;foe.hp=foe.maxHp=10000;game.Enemies.Add(foe);}
                var ready=(Dictionary<string,float>)Get(game,"legendaryReady");
                float beforeHp=game.hp,beforeShield=game.ArtifactShield;int ammo=game.AmmoReserve(VaultGame.AmmoForGun(game.gun));
                Call(game,"TryBossRelicEffectV2",id,player.position);
                int shots=Shots(world),pulses=Pulses(game);float hp=game.hp,shield=game.ArtifactShield,precision=Precision(game),dodge=game.dodgeCooldown,invincible=game.invincible;int afterAmmo=game.AmmoReserve(VaultGame.AmmoForGun(game.gun));
                Check(ready.ContainsKey(id)&&Mathf.Approximately(ready[id],4f),id+" trigger cooldown stored");
                bool effect=shots>0||pulses>0||hp>beforeHp||shield>beforeShield||precision>0||dodge<3||invincible>0||afterAmmo>ammo;
                if(boss==2&&category==10)effect|=game.Enemies.Exists(f=>f.hp<10000);
                Check(effect,id+" produces actual projectile, pulse, damage, or player effect");
                Call(game,"TryBossRelicEffectV2",id,player.position);
                Check(Shots(world)==shots&&Pulses(game)==pulses&&Mathf.Approximately(game.hp,hp)&&Mathf.Approximately(game.ArtifactShield,shield)&&Mathf.Approximately(Precision(game),precision)&&Mathf.Approximately(game.dodgeCooldown,dodge)&&Mathf.Approximately(game.invincible,invincible)&&game.AmmoReserve(VaultGame.AmmoForGun(game.gun))==afterAmmo,id+" cooldown suppresses repeat");
                Set(game,"legendaryClock",4.01f);Call(game,"TryBossRelicEffectV2",id,player.position);
                Check(ready.TryGetValue(id,out float next)&&next>8f,id+" can trigger after cooldown");
                if(category<5)
                {
                    var relic=(Gun)BossRelicFoundation.Create(boss,category,7,new System.Random(9100+boss*37+category));
                    var restored=JsonUtility.FromJson<Gun>(JsonUtility.ToJson(relic));Check(restored.bossRelicId==id,id+" gun save identity");
                    game.gun=restored;ready.Clear();Set(game,"legendaryClock",0f);Set(game,"legendaryShots",2);
                    Call(game,"LegendaryTrigger","fire",player.position,null);
                    Check(string.IsNullOrEmpty(restored.legendaryKey)||!ready.ContainsKey(restored.legendaryKey),id+" ordinary gun proc suppressed");
                    Call(game,"LegendaryTrigger","kill",player.position,null);
                    Check(ready.ContainsKey(id),id+" kill routes to relic effect");
                }
            }
            catch(Exception e){failures.Add(id+" exception: "+e);}
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        }
        string report="checks="+checks+" failures="+failures.Count+"\n"+string.Join("\n",failures);
        const string path="D:/Unity/BuildTemp/BossRelicCombatV2Review/result.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,report);Debug.Log("BOSS_RELIC_COMBAT_V2_REVIEW "+report);
        if(Application.isBatchMode)EditorApplication.Exit(failures.Count==0?0:1);
    }
}
