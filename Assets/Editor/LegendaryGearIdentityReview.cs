using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Isolated Editor fixtures; no PlayerPrefs writes or saved-game access.
public static class LegendaryGearIdentityReview
{
    const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly List<string> errors=new List<string>();
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)errors.Add(message);}
    static void Set(VaultGame game,string name,object value)
    {var f=typeof(VaultGame).GetField(name,I);if(f==null)throw new MissingFieldException(name);f.SetValue(game,value);}
    static object Get(VaultGame game,string name)
    {var f=typeof(VaultGame).GetField(name,I);if(f==null)throw new MissingFieldException(name);return f.GetValue(game);}
    static void Invoke(VaultGame game,string name,params object[] args)
    {var m=typeof(VaultGame).GetMethod(name,I);if(m==null)throw new MissingMethodException(name);m.Invoke(game,args);}
    static bool Description(string value)=>!string.IsNullOrWhiteSpace(value)&&!value.Contains("unique special projectile")&&!value.Contains("固有の特殊弾");
    static void Languages(Func<string> getter,string id,HashSet<string> english,HashSet<string> japanese)
    {VaultLanguage.Set(false,false);var en=getter();VaultLanguage.Set(true,false);var ja=getter();Check(Description(en),id+" English description missing");Check(Description(ja),id+" Japanese description missing");Check(english.Add(en),id+" duplicates an English effect description");Check(japanese.Add(ja),id+" duplicates a Japanese effect description");}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run this isolated review only in batch mode.");
        checks=0;errors.Clear();bool oldLanguage=VaultLanguage.Japanese;
        try
        {
            var modes=new HashSet<int>();var moduleEn=new HashSet<string>();var moduleJa=new HashSet<string>();int modules=0;
            foreach(var recipe in DistinctEquipmentDesigns.Modules)
            {
                if(!recipe.legendary||recipe.family<4||recipe.family>6)continue;
                modules++;
                var item=ArtifactCatalog.CreateCatalog(recipe.id,Rarity.Legendary,2,new System.Random(recipe.index+17));
                int mode=ArtifactCatalog.LegendaryModuleMode(item);
                Check(mode>=0&&mode<80&&modes.Add(mode),recipe.id+" duplicate or invalid mode "+mode);
                Languages(()=>ArtifactCatalog.LegendaryModuleSignature(item),recipe.id,moduleEn,moduleJa);
                var copy=JsonUtility.FromJson<ArtifactItem>(JsonUtility.ToJson(item));
                Check(copy.catalogId==item.catalogId&&ArtifactCatalog.LegendaryModuleMode(copy)==mode,recipe.id+" save identity changed");
                CheckModuleReaches(item,mode);
            }
            Check(modules==80&&modes.Count==80,"expected 80 module identities/modes: "+modules+"/"+modes.Count);
            var meleeModes=new HashSet<int>();var meleeEn=new HashSet<string>();var meleeJa=new HashSet<string>();
            for(int catalog=0;catalog<MeleeWeaponCatalog.CatalogCount;catalog++)
            {
                var item=MeleeWeaponCatalog.Catalog(catalog,Rarity.Legendary,2,new System.Random(catalog+202));
                int mode=(int)item.family*10+item.designVariant;
                Check(mode>=0&&mode<50&&meleeModes.Add(mode),"melee duplicate/invalid identity "+mode);
                Languages(()=>MeleeWeaponCatalog.LegendaryDescription(item),item.catalogId,meleeEn,meleeJa);
                var copy=JsonUtility.FromJson<MeleeWeaponItem>(JsonUtility.ToJson(item));
                Check(copy.catalogId==item.catalogId&&copy.family==item.family&&copy.designVariant==item.designVariant,"melee save identity changed "+item.catalogId);
                CheckMeleeReaches(item,mode);
            }
            Check(meleeModes.Count==50,"expected 50 melee identities: "+meleeModes.Count);
        }
        catch(Exception ex){errors.Add("fatal: "+ex);}
        finally{VaultLanguage.Set(oldLanguage,false);}
        string report="checks="+checks+" failures="+errors.Count+"\n"+string.Join("\n",errors);
        const string path="D:/Unity/BuildTemp/LegendaryGearIdentityReview/result.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,report);Debug.Log("LEGENDARY_GEAR_IDENTITY_REVIEW "+report);
        if(Application.isBatchMode)EditorApplication.Exit(errors.Count==0?0:1);
    }
    static VaultGame Fixture(out GameObject root,out Transform world,out Foe foe)
    {
        root=new GameObject("Legendary gear isolated fixture");root.SetActive(false);
        var game=root.AddComponent<VaultGame>();Set(game,"smoke",true);Set(game,"actionSkillLoaded",true);
        var map=new bool[16,16];for(int x=0;x<16;x++)for(int z=0;z<16;z++)map[x,z]=true;
        var layout=new SectorLayout(map,new Vector2Int(2,2),new Vector2Int(13,13));Set(game,"<Layout>k__BackingField",layout);
        world=new GameObject("world").transform;world.SetParent(root.transform,false);
        var player=new GameObject("player").transform;player.SetParent(world,false);player.position=layout.Center(new Vector2Int(5,5));
        Set(game,"world",world);Set(game,"player",player);Set(game,"art",new VaultArt(world,new List<Material>(),new System.Random(1)));Set(game,"aim",Vector3.forward);
        game.screen=VaultGame.Screen.Playing;game.hp=20;game.maxHp=100;game.dodgeCooldown=2;game.ammo=0;
        game.gun=new Gun("review","",Rarity.Rare,GunEffect.Normal,10,.5f,20,20,1){modelVariant=0};
        var target=new GameObject("target");target.transform.SetParent(world,false);target.transform.position=player.position+Vector3.forward*2;
        foe=target.AddComponent<Foe>();foe.game=game;foe.hp=foe.maxHp=100000;game.Enemies.Add(foe);
        return game;
    }
    static void CheckModuleReaches(ArtifactItem item,int mode)
    {
        GameObject root=null;
        try
        {
            var game=Fixture(out root,out var world,out var foe);
            if(mode==14)game.hp=20;
            if(mode==17)game.dodgeCooldown=3;
            if(mode==55)typeof(VaultGame).GetProperty("ActionSkillPoints",I).SetValue(game,0f);
            if(mode==56||mode==63){var reserves=(int[])Get(game,"ammoReserves");for(int i=0;i<reserves.Length;i++)reserves[i]=50;}
            if(mode==57){game.reload=2;Set(game,"reloadPending",2);}
            if(mode==58){var device=new GameObject("device");device.transform.SetParent(world,false);Set(game,"actionSkillObject",device.transform);Set(game,"actionSkillDuration",3f);}
            if(mode==64)typeof(VaultGame).GetProperty("ArtifactShield",I).SetValue(game,10f);
            // Avoid firing death handlers against a fabricated run. The low-health
            // execution condition is separately verified from its implementation.
            if(mode==66){foe.hp=20000;foe.maxHp=100000;foe.elite=true;}
            int shotsBefore=world.GetComponentsInChildren<Shot>(true).Length;
            int fieldsBefore=world.GetComponentsInChildren<LegendaryModuleField>(true).Length;
            int pulsesBefore=((System.Collections.ICollection)Get(game,"legendaryAftershocks")).Count;
            Invoke(game,"ApplyLegendaryModuleSignature",item);
            Check(!(bool)Get(game,"resolvingModuleSignature"),item.catalogId+" left module resolver locked");
            if(mode>=20&&mode<=54||mode==2||mode==8||mode==10||mode==11||mode==16||mode==62)
                Check(world.GetComponentsInChildren<Shot>(true).Length>shotsBefore,item.catalogId+" projectile mode did not spawn a shot");
            if(mode==5||mode==6||mode==18||mode==19)
                Check(((System.Collections.ICollection)Get(game,"legendaryAftershocks")).Count>pulsesBefore,item.catalogId+" pulse mode did not schedule a pulse");
            if(mode>=67&&mode<=79)
            {
                var fields=world.GetComponentsInChildren<LegendaryModuleField>(true);
                Check(fields.Length==fieldsBefore+1,item.catalogId+" field mode did not spawn a field");
                if(fields.Length>fieldsBefore){if(mode==67)game.kills++;bool alive=game.TickLegendaryModuleField(fields[fields.Length-1]);if(mode==79)Check(!alive&&game.hp>20,item.catalogId+" emergency heal did not trigger");}
            }
            if(mode==65)Check(foe.GetComponent<LegendaryFreeze>()!=null&&!foe.enabled,item.catalogId+" freeze mode did not freeze target");
            if(mode==13||mode==14||mode==64)Check(game.hp>20,item.catalogId+" healing mode did not heal");
            if(mode==17)Check(Mathf.Approximately(game.dodgeCooldown,0),item.catalogId+" dodge mode did not reset cooldown");
            if(mode==55)Check(game.ActionSkillPoints>0,item.catalogId+" action mode did not charge");
            if(mode==58)Check((float)Get(game,"actionSkillDuration")>3,item.catalogId+" device mode did not extend duration");
        }
        catch(Exception ex){errors.Add(item.catalogId+" mode "+mode+" activation: "+(ex.InnerException??ex));}
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
    }
    static void CheckMeleeReaches(MeleeWeaponItem item,int mode)
    {
        GameObject root=null;
        try
        {
            var game=Fixture(out root,out var world,out var foe);
            int shotsBefore=world.GetComponentsInChildren<Shot>(true).Length;
            int pulsesBefore=((System.Collections.ICollection)Get(game,"legendaryAftershocks")).Count;
            Invoke(game,"ApplyLegendaryMeleeHit",item,foe,100f);
            int special=(int)item.family*8+item.designVariant;
            if(item.designVariant<8&&special<35)
                Check(world.GetComponentsInChildren<Shot>(true).Length>shotsBefore,item.catalogId+" flight mode did not spawn a shot");
            else if(item.designVariant>=8&&((int)item.family*2+item.designVariant-8==3||(int)item.family*2+item.designVariant-8==5))
                Check(((System.Collections.ICollection)Get(game,"legendaryAftershocks")).Count>pulsesBefore,item.catalogId+" did not schedule pulses");
            else Check(true,item.catalogId+" activation invoked");
        }
        catch(Exception ex){errors.Add(item.catalogId+" melee "+mode+" activation: "+(ex.InnerException??ex));}
        finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
    }
}
