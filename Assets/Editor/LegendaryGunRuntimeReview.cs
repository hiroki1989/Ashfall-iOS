using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NeonVault;
using UnityEditor;
using UnityEngine;
public static class LegendaryGunRuntimeReview {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static void Set(VaultGame g,string n,object v)=>typeof(VaultGame).GetField(n,flags).SetValue(g,v);
 public static void Run(){var errors=new List<string>();int checks=0;for(int maker=0;maker<6;maker++)for(int variant=0;variant<15;variant++){GameObject root=null;try{
  var gun=LegendaryCatalog.CreateGun(LegendaryCatalog.CatalogIndex(maker,variant),new System.Random(maker*100+variant),10);string id=gun.legendaryKey;checks++;if(!VaultGame.DecodeLegendaryKey(id,out int m,out int v)||m!=maker||v!=variant)errors.Add(id+" identity");
  var copy=JsonUtility.FromJson<Gun>(JsonUtility.ToJson(gun));checks++;if(copy.legendaryKey!=id)errors.Add(id+" save");
  foreach(bool ja in new[]{false,true}){string d=LegendaryCatalog.Description(gun,ja);checks++;if(string.IsNullOrWhiteSpace(d))errors.Add(id+" description "+ja);}
  root=new GameObject("gun review");root.SetActive(false);var g=root.AddComponent<VaultGame>();Set(g,"smoke",true);Set(g,"actionSkillLoaded",true);var world=new GameObject("world").transform;world.SetParent(root.transform,false);var player=new GameObject("player").transform;player.SetParent(world,false);var map=new bool[16,16];for(int x=0;x<16;x++)for(int y=0;y<16;y++)map[x,y]=true;var layout=new SectorLayout(map,new Vector2Int(2,2),new Vector2Int(13,13));Set(g,"<Layout>k__BackingField",layout);player.position=layout.Center(new Vector2Int(5,5));Set(g,"world",world);Set(g,"player",player);Set(g,"aim",Vector3.forward);Set(g,"art",new VaultArt(world,new List<Material>(),new System.Random(1)));g.screen=VaultGame.Screen.Playing;g.gun=copy;g.hp=25;g.maxHp=100;
  Foe first=null;for(int i=0;i<4;i++){var obj=new GameObject("enemy");obj.transform.SetParent(world,false);obj.transform.position=player.position+new Vector3(i*.5f,0,2+i*.5f);var f=obj.AddComponent<Foe>();f.game=g;f.hp=f.maxHp=100000;g.Enemies.Add(f);if(first==null)first=f;}
  var method=typeof(VaultGame).GetMethod("ResolveDistinctLegendaryGun",flags);method.Invoke(g,new object[]{maker,variant,player.position,first});checks++;
 }catch(Exception e){errors.Add(maker+"/"+variant+" "+e);}finally{if(root)UnityEngine.Object.DestroyImmediate(root);}}
 string report="checks="+checks+" failures="+errors.Count+"\n"+string.Join("\n",errors);Directory.CreateDirectory("D:/Unity/BuildTemp/LegendaryGunRuntimeReview");File.WriteAllText("D:/Unity/BuildTemp/LegendaryGunRuntimeReview/result.txt",report);Debug.Log("LEGENDARY_GUN_RUNTIME_REVIEW "+report);if(Application.isBatchMode)EditorApplication.Exit(errors.Count==0?0:1);}
}
