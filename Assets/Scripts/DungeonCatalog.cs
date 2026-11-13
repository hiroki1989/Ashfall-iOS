using UnityEngine;
namespace NeonVault {
 public sealed class DungeonProfile {
  public int AreaCount=>4; public int index,minLevel,maxLevel; public string english,japanese,themeEnglish,themeJapanese,backgroundSkin; public Color palette; public int[] enemyRoster; public float healthScale,damageScale;
  // Final area retains its dedicated boss; normal areas build pressure through population.
  public int EnemyCount(int area)=>area>=AreaCount-1?1:Mathf.RoundToInt((32+(index==0?0:index==1?2:6)+Mathf.Clamp(area,0,AreaCount-2)*6)*(index==0?.6f:1f)*.9f);
  public string Name=>VaultLanguage.T(english,japanese); public string Theme=>VaultLanguage.T(themeEnglish,themeJapanese);
 }
 public static class DungeonCatalog {
  public static readonly DungeonProfile[] All={
   new DungeonProfile{index=0,minLevel=1,maxLevel=4,english="DEAD HIGHWAY",japanese="死の街道",themeEnglish="Shamblers, acid spitters and plated corpses. Evade swings and flank armor.",themeJapanese="徘徊屍・酸吐き・装甲屍。予告殴りをかわし、装甲の側面を突け。",backgroundSkin="Obsidian",palette=new Color(.24f,.66f,.58f),enemyRoster=new[]{0,1,4,8},healthScale=1.12f,damageScale=1.08f},
   new DungeonProfile{index=1,minLevel=5,maxLevel=8,english="CINDER WORKS",japanese="灰灰工場",themeEnglish="Needle shots and bloater blasts. Break away from locked attack warnings.",themeJapanese="針撃ちと膨張屍の爆発。固定された攻撃予告から離脱せよ。",backgroundSkin="Ember",palette=new Color(1,.35f,.16f),enemyRoster=new[]{2,4,5,7},healthScale=2.35f,damageScale=1.5f},
   new DungeonProfile{index=2,minLevel=9,maxLevel=12,english="HOT ZONE",japanese="汚染隔離区",themeEnglish="Radiation barrages and regenerating mutants. Prioritize flesh weavers.",themeJapanese="放射線弾幕と再生変異体。再生母体を優先して倒せ。",backgroundSkin="Astral",palette=new Color(.6f,.38f,1),enemyRoster=new[]{5,6,8,9},healthScale=4.2f,damageScale=2f}
  };
  public static DungeonProfile Get(int index)=>All[Mathf.Clamp(index,0,All.Length-1)];
  public static int ItemLevel(System.Random random,int index){var d=Get(index);return random.Next(d.minLevel,d.maxLevel+1);}
  public static Rarity RollRarity(System.Random random,bool boss=false)
  {
   // Raise the original common/uncommon probabilities by 5/3 percentage points;
   // share the remaining probability among higher rarities in their original ratio.
   double oldLegendary=boss?4.75:.475, oldNonLegendary=100-oldLegendary;
   double common=oldNonLegendary*(boss?56.7:58.8)/99.5+5;
   double uncommon=oldNonLegendary*(boss?31.45:31.4)/99.5+3;
   double rare=oldNonLegendary*(boss?8.35:7.1)/99.5;
   double epic=oldNonLegendary*(boss?3.0:2.2)/99.5;
   double multiplier=VaultBalanceProfile.Current!=null?Mathf.Max(0f,VaultBalanceProfile.Current.legendaryDropMultiplier):1f;
   double legendary=oldLegendary*multiplier;
   double roll=random.NextDouble()*100;
   if(roll<common)return Rarity.Common;
   if(roll<common+uncommon)return Rarity.Uncommon;
   double highRoll=(roll-common-uncommon)*(rare+epic+legendary)/(100-common-uncommon);
   if(highRoll<rare)return Rarity.Rare;
   if(highRoll<rare+epic)return Rarity.Epic;
   return Rarity.Legendary;
  }
 }
}
