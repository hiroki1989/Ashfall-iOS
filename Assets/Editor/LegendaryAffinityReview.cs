using System;
using System.Collections.Generic;
using System.IO;
using NeonVault;
using UnityEditor;
using UnityEngine;

// Batch-only inspection creates transient objects and does not load or save
// the player's inventory or PlayerPrefs.
public static class LegendaryAffinityReview
{
    static int checks;
    static readonly List<string> failures=new List<string>();
    static void Check(bool condition,string name){checks++;if(!condition)failures.Add(name);}
    static GunElement Expected(int maker,int variant)
    {
        if(variant==14)return GunElement.Explosive;
        if(variant>=5)return GunElement.None;
        if(maker==2)return GunElement.Electric;
        if(maker==3&&(variant==2||variant==4))return GunElement.Poison;
        if((maker==1&&variant==1)||(maker==4&&variant==2))return GunElement.Explosive;
        if(maker==4&&variant==4)return GunElement.Fire;
        return GunElement.None;
    }
    static GunElement ExpectedBoss(Gun gun,int boss,int category)
    {
        if(boss==0&&(category==0||category==1))return GunElement.Explosive;
        if((boss==0||boss==1)&&category==2)return GunElement.Electric;
        if(boss==2&&category==1)return GunElement.Explosive;
        return VaultGame.DecodeLegendaryKey(gun.legendaryKey,out int maker,out int variant)?Expected(maker,variant):GunElement.None;
    }
    public static void Run()
    {
        const string output="D:/Unity/BuildTemp/LegendaryAffinityReview/result.txt";
        failures.Clear();checks=0;
        try
        {
            var random=new System.Random(88317);
            for(int maker=0;maker<6;maker++)for(int variant=0;variant<15;variant++)
            {
                int index=LegendaryCatalog.CatalogIndex(maker,variant);
                var gun=LegendaryCatalog.CreateGun(index,random,16);
                GunElement expected=Expected(maker,variant);
                Check(VaultGame.DecodeLegendaryKey(gun.legendaryKey,out int m,out int v)&&m==maker&&v==variant,"catalog key / "+maker+"/"+variant);
                Check(GunElements.LegendaryAffinity(gun)==expected,"affinity lookup / "+maker+"/"+variant);
                if(expected!=GunElement.None)
                {
                    Check(gun.element==expected&&gun.elementPercent>0,"created gun has fixed element and positive power / "+maker+"/"+variant);
                    string saved=JsonUtility.ToJson(gun);var copy=JsonUtility.FromJson<Gun>(saved);
                    GearBalance.Migrate(copy);Check(copy.element==expected&&copy.elementPercent==gun.elementPercent,"save roundtrip preserves fixed element / "+maker+"/"+variant);
                    string once=JsonUtility.ToJson(copy);GearBalance.Migrate(copy);
                    Check(once==JsonUtility.ToJson(copy),"migration idempotent / "+maker+"/"+variant);
                    copy.element=GunElement.None;copy.elementPercent=0;copy.elementAffinityRevision=0;
                    var same=JsonUtility.FromJson<Gun>(JsonUtility.ToJson(copy));
                    GearBalance.Migrate(copy);GearBalance.Migrate(same);
                    Check(copy.element==expected&&copy.elementPercent>0,"legacy mismatch migrates / "+maker+"/"+variant);
                    Check(copy.elementPercent==same.elementPercent&&copy.elementChains==same.elementChains&&copy.elementRadius==same.elementRadius&&copy.elementDuration==same.elementDuration,"legacy migration deterministic / "+maker+"/"+variant);
                }
            }
            for(int boss=0;boss<3;boss++)for(int category=0;category<5;category++)
            {
                var gun=(Gun)BossRelicFoundation.Create(boss,category,16,random);
                Check(BossRelicFoundation.Decode(gun.bossRelicId,out int b,out int c)&&b==boss&&c==category,"boss identity / "+boss+"/"+category);
                GunElement expected=ExpectedBoss(gun,boss,category);
                Check(GunElements.LegendaryAffinity(gun)==expected,"boss affinity after id assignment / "+boss+"/"+category);
                if(expected!=GunElement.None)
                {
                    Check(gun.element==expected&&gun.elementPercent>0,"boss gun fixed after id assignment / "+boss+"/"+category);
                    var copy=JsonUtility.FromJson<Gun>(JsonUtility.ToJson(gun));copy.elementAffinityRevision=0;copy.element=GunElement.None;copy.elementPercent=0;
                    GearBalance.Migrate(copy);Check(copy.element==expected&&copy.elementPercent>0,"legacy boss gun migrates / "+boss+"/"+category);
                }
            }
            // A relic without a matching theme must continue to use the random
            // manufacturer distribution, rather than becoming a fixed element.
            var seen=new HashSet<GunElement>();
            for(int i=0;i<500;i++)
            {
                var gun=LegendaryCatalog.CreateGun(LegendaryCatalog.CatalogIndex(0,0),random,1);
                seen.Add(gun.element);
                Check(GunElements.LegendaryAffinity(gun)==GunElement.None,"non-themed gun remains random / "+i);
                string json=JsonUtility.ToJson(gun);GearBalance.Migrate(gun);
                Check(json==JsonUtility.ToJson(gun),"non-themed gun is not rerolled / "+i);
            }
            Check(seen.Count==5,"non-themed rolls include no element and all four elements");
        }
        catch(Exception error){failures.Add(error.ToString());}
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string result="checks="+checks+" failures="+failures.Count+"\n"+string.Join("\n",failures);
        File.WriteAllText(output,result);Debug.Log("LEGENDARY_AFFINITY_REVIEW "+result);
        if(Application.isBatchMode)EditorApplication.Exit(failures.Count==0?0:1);
    }
}
