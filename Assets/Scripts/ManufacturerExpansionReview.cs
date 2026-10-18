using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace NeonVault
{
    // Explicit finite review; never runs in a normal game session.
    public sealed class ManufacturerExpansionReview:MonoBehaviour
    {
        const string Flag="--manufacturer-expansion-review";
        static readonly string[] keys={"Vault.Progression.v1","Vault.Loadout.v1","Vault.DebtSkills.v1","Vault.ExpeditionCheckpoint.v1"};
        static readonly Dictionary<string,string> prior=new Dictionary<string,string>();
        readonly List<string> results=new List<string>();int checks,failures;VaultGame game;string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Backup(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)<0)return;foreach(var k in keys)prior[k]=PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0)new GameObject("Manufacturer expansion review").AddComponent<ManufacturerExpansionReview>();}
        IEnumerator Start()
        {
            output="D:/Unity/QA-manufacturer-expansion";Directory.CreateDirectory(output);
            var review=Run();while(true){bool next;try{next=review.MoveNext();}catch(Exception e){Check(false,e.ToString());break;}if(!next)break;yield return review.Current;}
            Restore();results.Add("Checks="+checks+" Failures="+failures);File.WriteAllLines(output+"/result.txt",results);Application.Quit(failures==0?0:1);
        }
        IEnumerator Run()
        {
            for(int i=0;i<120;i++){game=FindAnyObjectByType<VaultGame>();if(game)break;yield return null;}
            Check(game!=null,"runtime initialized");if(!game)yield break;
            Set("smoke",true);Set("debtIntroSeen",true);game.StartRun(781031);game.enabled=false;
            var target=game.Enemies[0];foreach(var f in game.Enemies)if(f){f.enabled=false;if(f!=target)Destroy(f.gameObject);}game.Enemies.Clear();game.Enemies.Add(target);target.transform.position=game.PlayerPosition+Vector3.right*2;
            var counts=new int[6];var ids=new HashSet<string>();
            for(int i=0;i<Arsenal.CatalogCount;i++){var item=Arsenal.Catalog(i);counts[ManufacturerCatalog.Index(item.manufacturerId)]++;Check(ids.Add(item.catalogId),"gun ID "+i);Check(item.damage>0&&item.interval>0&&item.magazine>0,"gun stats "+i);}
            Check(Arsenal.CatalogCount==1800,"1800 regular guns");for(int i=0;i<6;i++)Check(counts[i]==300,"gun manufacturer balance "+i);
            Array.Clear(counts,0,6);ids.Clear();foreach(var p in ArmorProductCatalog.All){counts[ManufacturerCatalog.Index(p.manufacturerId)]++;Check(ids.Add(p.id),"armor ID "+p.id);var item=Armory.CreateCatalog(p.id,Rarity.Rare,3);Check(!string.IsNullOrEmpty(Armory.Name(item,true)),"armor localized name");}
            Check(ArmorProductCatalog.Count==2520,"2520 regular armor");for(int i=0;i<6;i++)Check(counts[i]==420,"armor manufacturer balance "+i);
            Array.Clear(counts,0,6);ids.Clear();foreach(var p in ModuleProductCatalog.All){counts[ManufacturerCatalog.Index(p.manufacturerId)]++;Check(ids.Add(p.id),"module ID "+p.id);var item=ArtifactCatalog.CreateCatalog(p.id,Rarity.Rare,3);Check(item.power>0&&item.cooldown>0,"module positive stats");}
            for(int i=0;i<6;i++)Check(counts[i]==120,"module manufacturer balance "+i);
            var names=new HashSet<string>();string[] gunEvents={"fire","hit","kill","reload","dash"},armorEvents={"hurt","kill","reload","dash","pulse"};
            for(int armor=0;armor<2;armor++)for(int i=0;i<30;i++)
            {
                Call("ResetLegendaryEffects");game.EquippedArmor=new ArmorItem[5];game.gun=Arsenal.Model(12,Rarity.Common);target.dead=false;target.hp=target.maxHp=100000;game.hp=game.maxHp*.4f;typeof(VaultGame).GetProperty("ArtifactShield").SetValue(game,0f);
                string key;var random=new System.Random(9000+i);
                if(armor==0){var item=LegendaryCatalog.CreateGun(i,random,4);game.gun=item;key=item.legendaryKey;Check(item.rarity==Rarity.Legendary&&item.affixes.Length==4,"legendary gun rarity and affixes "+i);Check(JsonUtility.FromJson<Gun>(JsonUtility.ToJson(item)).legendaryKey==key,"gun save roundtrip "+i);Check(names.Add(LegendaryCatalog.GunName(item,true)),"unique gun name "+i);}
                else{var item=LegendaryCatalog.CreateArmor(i,random,4);game.EquippedArmor[(int)item.slot]=item;key=item.legendaryKey;Check(item.rarity==Rarity.Legendary&&item.affixes.Length==4,"legendary armor rarity and affixes "+i);Check(JsonUtility.FromJson<ArmorItem>(JsonUtility.ToJson(item)).legendaryKey==key,"armor save roundtrip "+i);Check(names.Add(LegendaryCatalog.ArmorName(item,true)),"unique armor name "+i);}
                Check(LegendaryCatalog.Index(key)==i,"legendary key "+key);Check(LegendaryCatalog.Description(key,true).Length>20&&LegendaryCatalog.Description(key,false).Length>20,"bilingual effect "+key);
                float hp=game.hp;string trigger=(armor==0?gunEvents:armorEvents)[i%5];int times=trigger=="fire"?3:1;
                if(trigger=="pulse")Call("TickLegendaryEffects",5.1f);else for(int n=0;n<times;n++)Call("LegendaryTrigger",trigger,game.PlayerPosition,target);
                for(int t=0;t<4;t++)Call("TickLegendaryEffects",.6f);
                Check(target.hp<100000||game.hp>hp||game.ArtifactShield>0,"actual effect "+key);
            }
            game.Weapons.Clear();game.Weapons.Add(LegendaryCatalog.CreateGun(11,new System.Random(7),4));game.gun=game.Weapons[0];game.ArmorInventory.Clear();game.ArmorInventory.Add(LegendaryCatalog.CreateArmor(21,new System.Random(8),4));game.screen=VaultGame.Screen.Playing;game.ToggleInventory();
            yield return null;yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();if(image){File.WriteAllBytes(output+"/legendary-inventory.png",image.EncodeToPNG());Destroy(image);}Check(image!=null,"inventory framebuffer");
        }
        void Check(bool pass,string label){checks++;if(!pass)failures++;if(!pass||checks%1000==0||label.StartsWith("actual")||label.Contains("balance"))results.Add((pass?"PASS ":"FAIL ")+label);}
        void Set(string name,object value)=>typeof(VaultGame).GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(game,value);
        object Call(string name,params object[] args)=>typeof(VaultGame).GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(game,args);
        static void Restore(){foreach(var p in prior)if(p.Value==null)PlayerPrefs.DeleteKey(p.Key);else PlayerPrefs.SetString(p.Key,p.Value);PlayerPrefs.Save();}
        void OnApplicationQuit(){Restore();}
    }
}
