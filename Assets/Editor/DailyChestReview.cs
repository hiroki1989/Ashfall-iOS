using System;
using System.IO;
using System.Reflection;
using NeonVault;
using UnityEngine;

// Read-only Editor smoke review; never changes PlayerPrefs or invokes a successful ad callback.
public static class DailyChestReview
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("Daily chest: " + message); }
    static FieldInfo Field(string name) { var f=typeof(VaultGame).GetField(name,Flags); Check(f!=null,"missing field "+name); return f; }
    static MethodInfo StaticMethod(string name) { var m=typeof(VaultGame).GetMethod(name,BindingFlags.Static | BindingFlags.NonPublic); Check(m!=null,"missing method "+name); return m; }
    static MethodInfo Method(string name) { var m=typeof(VaultGame).GetMethod(name,Flags); Check(m!=null,"missing method "+name); return m; }
    public static void Run()
    {
        var obj=new GameObject("Daily chest read-only review"); obj.SetActive(false);
        try
        {
            var game=obj.AddComponent<VaultGame>();
            Field("rng").SetValue(game,new System.Random(20261106));
            var available=typeof(VaultGame).GetProperty("DailyChestFreeAvailable",Flags);
            Check(available!=null,"availability property missing");
            string today=DateTime.Now.ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture);
            string saved=PlayerPrefs.GetString("Vault.DailyChest.LastClaimDate.v1","");
            Check((bool)available.GetValue(game)==(string.CompareOrdinal(saved,today)<0),"saved-date availability");
            Field("smoke").SetValue(game,true);
            Check((bool)available.GetValue(game),"smoke mode availability");
            Field("smoke").SetValue(game,false);
            Field("dailyChestAdPending").SetValue(game,true);
            Method("InteractDailyChest").Invoke(game,null);
            Check(!(bool)Field("dailyChestAdConfirm").GetValue(game),"pending ad prevents interaction");
            Field("dailyChestAdPending").SetValue(game,false);
            if (!VaultRewardedAds.IsReady) {
                Method("ShowDailyChestAd").Invoke(game,null);
                Check(!(bool)Field("dailyChestAdPending").GetValue(game),"failed ad clears pending");
                Check((string)Field("toast").GetValue(game)=="AD REWARD NOT EARNED","failed ad notification");
            }
            var free=StaticMethod("DailyFreeAvailable"); var ad=StaticMethod("DailyAdAvailable");
            Check((bool)free.Invoke(null,new object[]{"20261106",""}),"first free claim");
            Check(!(bool)free.Invoke(null,new object[]{"20261106","20261106"}),"same-day free claim denied");
            Check((bool)free.Invoke(null,new object[]{"20261107","20261106"}),"next-day free claim");
            Check(!(bool)free.Invoke(null,new object[]{"20261105","20261106"}),"clock rollback free claim denied");
            Check((bool)ad.Invoke(null,new object[]{"20261106","20261106",2}),"third ad allowed");
            Check(!(bool)ad.Invoke(null,new object[]{"20261106","20261106",3}),"fourth ad denied");
            Check((bool)ad.Invoke(null,new object[]{"20261107","20261106",3}),"next-day ad reset");
            Check(!(bool)ad.Invoke(null,new object[]{"20261105","20261106",0}),"clock rollback ad denied");
            var ordinary=Method("EquipmentChestRarity"); var daily=Method("RollDailyChestRarity");
            int[] ordinaryCounts=new int[5], dailyCounts=new int[5];
            for(int i=0;i<10000;i++) ordinaryCounts[(int)(Rarity)ordinary.Invoke(game,new object[]{true})]++;
            Field("rng").SetValue(game,new System.Random(20261107));
            for(int i=0;i<10000;i++) dailyCounts[(int)(Rarity)daily.Invoke(game,null)]++;
            Check(dailyCounts[0]==0 && dailyCounts[1]==0,"daily reward must be Rare or above");
            Check(dailyCounts[3]+dailyCounts[4]>ordinaryCounts[3]+ordinaryCounts[4],"daily Epic/Legendary rate exceeds rare chest");
            Check(dailyCounts[3]>ordinaryCounts[3] && dailyCounts[4]>ordinaryCounts[4],"daily Epic and Legendary rates individually exceed rare chest");
            // Source wiring checks are not a substitute for executing a rewarded iOS ad.
            string chest=File.ReadAllText("Assets/Scripts/VaultDailyChest.cs");
            string hub=File.ReadAllText("Assets/Scripts/VaultHubWorld.cs");
            string ui=File.ReadAllText("Assets/Scripts/VaultUI.cs");
            string mobile=File.ReadAllText("Assets/Scripts/VaultMobileUI.cs");
            Check(chest.Contains("DailyAdAvailable(day,last,savedCount)") && chest.Contains("count+1"),"ad limit and increment wiring");
            Check(chest.Contains("rarity<Rarity.Rare") && chest.Contains("rng.Next(3)"),"reward tier and category wiring");
            Check(hub.Contains("BuildDailyChest();") && hub.Contains("InteractDailyChest();") && hub.Contains("DrawDailyChestLabel(key);"),"hub chest wiring");
            Check(ui.Contains("DrawDailyChestAdConfirm();") && mobile.Contains("DrawDailyChestAdConfirm();"),"desktop/mobile confirmation wiring");
            Debug.Log("DAILY_CHEST_REVIEW_PASS rare chest counts="+string.Join(",",ordinaryCounts)+" daily counts="+string.Join(",",dailyCounts)+"; saved preferences unchanged; successful ad requires device testing");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
}
