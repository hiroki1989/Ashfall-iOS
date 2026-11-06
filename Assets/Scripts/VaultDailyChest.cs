using System;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string DailyAdsDateKey="Vault.DailyChest.AdDate.v1", DailyAdsCountKey="Vault.DailyChest.AdCount.v1";
        const string DailyChestDateKey="Vault.DailyChest.LastClaimDate.v1";
        readonly Vector3 dailyChestPosition=new Vector3(-4,.65f,-1);
        Transform dailyChestMarker;
        GameObject dailyChestVisual;
        bool dailyChestAdPending, dailyChestAdConfirm;
        object dailyChestLastReward; bool dailyChestRewardVisible;
        bool DailyChestNearby => Vector3.Distance(PlayerPosition,dailyChestPosition)<2.5f;
        bool DailyChestFreeAvailable
        {
            get
            {
                if(smoke)return true;
                string today=DateTime.Now.ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture);
                return DailyFreeAvailable(today,PlayerPrefs.GetString(DailyChestDateKey,""));
            }
        }
        static bool DailyFreeAvailable(string today,string lastClaim) => string.CompareOrdinal(lastClaim,today)<0;
        static bool DailyAdAvailable(string today,string lastAd,int count) =>
            string.CompareOrdinal(today,lastAd)>=0 && (today!=lastAd || count<3);
        void BuildDailyChest()
        {
            var p=dailyChestPosition;
            dailyChestVisual=new GameObject("Daily bonus chest");
            dailyChestVisual.transform.SetParent(world,false); dailyChestVisual.transform.position=new Vector3(p.x,0,p.z);
            EquipmentChestVisual.Create(dailyChestVisual.transform,true);
            dailyChestMarker=art.Ring("Daily bonus / red pulse",new Vector3(p.x,.04f,p.z),1.1f,.1f,new Color(1,.05f,.03f),true);
        }
        void UpdateDailyChestMarker()
        {
            if(dailyChestMarker!=null)dailyChestMarker.gameObject.SetActive(DailyChestFreeAvailable && Mathf.Sin(Time.unscaledTime*5f)>-.15f);
        }
        void DrawDailyChestLabel(string key)
        {
            DrawWorldLabel(dailyChestPosition+Vector3.up*1.7f,DailyChestFreeAvailable
                ?T("DAILY BONUS / FREE","デイリーボーナス / 無料")+key
                :T("DAILY BONUS / AD","デイリーボーナス / 広告")+key,Gold);
        }
        void InteractDailyChest()
        {
            if(dailyChestAdPending)return;
            if(DailyChestFreeAvailable)
            {
                GrantDailyChestReward();
                if(!smoke){PlayerPrefs.SetString(DailyChestDateKey,DateTime.Now.ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture));PlayerPrefs.Save();}
                return;
            }
            string today=DateTime.Now.ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture);
            string adDate=PlayerPrefs.GetString(DailyAdsDateKey,"");
            if(!DailyAdAvailable(today,adDate,PlayerPrefs.GetInt(DailyAdsCountKey,0))){Notify("DAILY AD LIMIT REACHED","本日の広告ボーナスは終了しました");return;}
            if(!VaultRewardedAds.IsReady){Notify("AD NOT AVAILABLE YET","広告をまだ利用できません");return;}
            dailyChestAdConfirm=true;
        }
        void DrawDailyChestAdConfirm()
        {
            if(!dailyChestAdConfirm)return;
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.76f));
            Panel(new Rect(300,260,840,370),Gold);
            Text(340,300,T("WATCH AN AD FOR BONUS GEAR?","広告を見て追加の装備を入手しますか？"),heading,Gold,760);
            Text(340,390,T("Up to 3 extra chests per day. Complete the ad to receive gear.","広告の視聴完了で装備を入手できます。追加は1日3回までです。"),new GUIStyle(label){wordWrap=true},Color.white,760);
            if(WorldButton(new Rect(350,500,340,85),T("WATCH","広告を見る"))) { dailyChestAdConfirm=false; ShowDailyChestAd(); }
            if(WorldButton(new Rect(750,500,340,85),T("CANCEL","やめる"))) dailyChestAdConfirm=false;
        }
        void ShowDailyChestAd()
        {
            dailyChestAdPending=true;
            VaultRewardedAds.Show(rewarded=>{
                dailyChestAdPending=false;
                if(rewarded){ string day=DateTime.Now.ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture); string last=PlayerPrefs.GetString(DailyAdsDateKey,""); int savedCount=PlayerPrefs.GetInt(DailyAdsCountKey,0); if(!DailyAdAvailable(day,last,savedCount))return; int count=day==last?savedCount:0; PlayerPrefs.SetString(DailyAdsDateKey,day); PlayerPrefs.SetInt(DailyAdsCountKey,count+1); GrantDailyChestReward(); PlayerPrefs.Save(); }
                else Notify("AD REWARD NOT EARNED","広告報酬を獲得できませんでした");
            });
        }
        void DrawDailyChestReward()
        {
            if(!dailyChestRewardVisible || dailyChestLastReward==null)return;
            object item=dailyChestLastReward;
            Gun weapon=item as Gun; ArmorItem armor=item as ArmorItem; ArtifactItem module=item as ArtifactItem;
            Rarity rarity=weapon!=null?weapon.rarity:armor!=null?armor.rarity:module.rarity;
            int level=weapon!=null?weapon.level:armor!=null?armor.level:module.level;
            int score=weapon!=null?GearScoring.Score(weapon):armor!=null?GearScoring.Score(armor):GearScoring.Score(module);
            string name=weapon!=null?VaultLanguage.GunName(weapon):armor!=null?Armory.Name(armor):ArtifactCatalog.DisplayName(module);
            string details=weapon!=null?LegendaryDescription(weapon,VaultLanguage.GunDescription(weapon)):armor!=null?LegendaryDescription(armor,Armory.Description(armor)):GearScoring.AffixSummary(module);
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.83f));
            Panel(new Rect(220,170,1000,560),Arsenal.Colors[(int)rarity]);
            Text(260,195,T("DAILY BONUS","デイリーボーナス"),heading,Gold,920);
            Rect picture=new Rect(260,285,300,300);
            if(weapon!=null)ItemPortraits.Draw(picture,weapon);else if(armor!=null)ItemPortraits.Draw(picture,armor);else ManufacturerBranding.ModulePortrait(picture,module);
            var body=new GUIStyle(label){fontSize=29,wordWrap=true};
            Text(600,290,name,body,Color.white,560);
            Text(600,390,VaultLanguage.RarityName(rarity)+"  ·  Lv "+level+"  ·  "+T("SCORE ","評価 ")+score,new GUIStyle(label){fontSize=25},Arsenal.Colors[(int)rarity],560);
            Text(600,450,details,new GUIStyle(small){fontSize=23,wordWrap=true,richText=true},Mist,560);
            if(WorldButton(new Rect(420,625,600,72),T("CONFIRM","確認")))dailyChestRewardVisible=false;
        }
        Rarity RollDailyChestRarity()
        {
            // Conditioning the ordinary chest distribution to the top three tiers;
            // a small additional bias favors Epic and Legendary over Rare.
            Rarity rarity;
            do { rarity=EquipmentChestRarity(true); }
            while(rarity<Rarity.Rare || (rarity==Rarity.Rare && rng.NextDouble()>1.0/1.2));
            return rarity;
        }
        void GrantDailyChestReward()
        {
            Rarity rarity=RollDailyChestRarity();
            int level=Mathf.Max(1,DebtLevel);
            switch(rng.Next(3))
            {
                case 0: var weapon=Arsenal.CreateForDungeon(rng,DungeonIndex,rarity,level); StoreWeapon(weapon,true); dailyChestLastReward=weapon; break;
                case 1: var armor=Armory.CreateForDungeon(rng,DungeonIndex,rarity,level); StoreArmor(armor,true); dailyChestLastReward=armor; break;
                default: var module=ArtifactCatalog.Create(rng,DungeonIndex,rarity,level); StoreArtifact(module,true); dailyChestLastReward=module; break;
            }
            dailyChestRewardVisible=true;
            EquipmentChestVisual.Open(dailyChestVisual);
            PlayUserSound("transaction",.35f);
            Notify("DAILY BONUS / "+rarity+" GEAR","デイリーボーナス / "+VaultLanguage.RarityName(rarity)+"装備");
        }
    }
}
