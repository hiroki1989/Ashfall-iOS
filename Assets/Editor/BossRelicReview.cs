using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonVault.Editor
{
    public static class BossRelicReview
    {
        [MenuItem("Neon Vault/QA/Review Boss Relics")]
        public static void Run()
        {
            var names=new HashSet<string>(StringComparer.Ordinal);
            var models=new HashSet<string>(StringComparer.Ordinal);
            int checks=0;
            int bossMissions=0;
            for(int stage=0;stage<ExpeditionMap.StageCount;stage++)
                if(ExpeditionMap.Get(stage).mission==ExpeditionMission.HuntTarget)bossMissions++;
            if(bossMissions!=ExpeditionMap.StageCount/2)throw new InvalidOperationException("Boss mission share is not exactly 50%: "+bossMissions+"/"+ExpeditionMap.StageCount);
            checks++;
            if(ExpeditionMap.Get(0).mission!=ExpeditionMission.HuntTarget)throw new InvalidOperationException("Introductory stage 01 must be a boss hunt");
            checks++;
            if(ExpeditionMap.Get(VaultGame.RedClawHeadquarters).mission!=ExpeditionMission.HuntTarget||
               ExpeditionMap.Get(VaultGame.CoyoteHeadquarters).mission!=ExpeditionMission.HuntTarget)
                throw new InvalidOperationException("Both faction headquarters must be boss hunts");
            checks++;
            if(ExpeditionMap.FactionForStage(VaultGame.RedClawHeadquarters)!=DungeonFaction.Raiders||
               ExpeditionMap.FactionForStage(VaultGame.CoyoteHeadquarters)!=DungeonFaction.Gang)
                throw new InvalidOperationException("Faction headquarters ownership does not match the faction roster");
            checks++;
            for(int boss=0;boss<BossRelicFoundation.BossCount;boss++)
            for(int category=0;category<BossRelicFoundation.CategoryCount;category++)
            {
                string id=BossRelicFoundation.Id(boss,category);
                var item=BossRelicFoundation.Create(boss,category,7,new System.Random(9100+boss*37+category));
                if(item==null)throw new InvalidOperationException("Missing boss relic: "+id);checks++;
                string name=item is Gun g?LegendaryCatalog.GunName(g,false):item is ArmorItem a?Armory.Name(a,false):item is ArtifactItem m?ArtifactCatalog.DisplayName(m,false):MeleeWeaponCatalog.Name((MeleeWeaponItem)item);
                if(string.IsNullOrWhiteSpace(name)||!names.Add(name))throw new InvalidOperationException("Empty or duplicate boss relic name: "+id+" / "+name);checks++;
                string description=BossRelicFoundation.Description(id,false);
                if(string.IsNullOrWhiteSpace(description)||!description.Contains(boss==0?"Raven":boss==1?"Vex":"Vale")||!description.Contains("4s cooldown"))throw new InvalidOperationException("Boss source/effect missing: "+id);checks++;
                if(item is Gun gun)
                {
                    if(gun.rarity!=Rarity.Legendary||!BossRelicFoundation.Decode(gun.bossRelicId,out _,out _))throw new InvalidOperationException("Gun relic identity/rarity missing: "+id);
                    if(Arsenal.Category(gun.modelVariant)!=(WeaponClass)category)throw new InvalidOperationException("Wrong firearm class: "+id);
                    string design="gun/"+category+"/"+(string.IsNullOrEmpty(gun.legendaryKey)?gun.catalogId:gun.legendaryKey);
                    if(!models.Add(design))throw new InvalidOperationException("Duplicate firearm design: "+design);
                    if(string.IsNullOrEmpty(LegendaryCatalog.Description(gun,false))||!LegendaryCatalog.Description(gun,false).Contains(description))throw new InvalidOperationException("Gun details omit boss effect: "+id);
                }
                else if(item is ArmorItem armor)
                {
                    if(!Armory.IsActiveSlot(armor.slot)||(int)armor.slot!=category-4||armor.rarity!=Rarity.Legendary)throw new InvalidOperationException("Wrong armor slot/rarity: "+id);
                    if(!models.Add("armor/"+(int)armor.slot+"/"+armor.legendaryKey))throw new InvalidOperationException("Duplicate armor design: "+id);
                    if(!Armory.Description(armor,false).Contains(description))throw new InvalidOperationException("Armor details omit boss effect: "+id);
                }
                else if(item is ArtifactItem module)
                {
                    string moduleDescription=BossRelicFoundation.Description(id,VaultLanguage.Japanese);
                    if(module.rarity!=Rarity.Legendary||module.catalogId==null||!module.Description.Contains(moduleDescription))throw new InvalidOperationException("Module effect/identity missing: "+id);
                    if(!models.Add("module/"+module.catalogId))throw new InvalidOperationException("Duplicate module design: "+id);
                }
                else if(item is MeleeWeaponItem melee)
                {
                    string meleeDescription=BossRelicFoundation.Description(id,VaultLanguage.Japanese);
                    if(melee.rarity!=Rarity.Legendary||!melee.uniqueEffect||!MeleeWeaponCatalog.LegendaryDescription(melee).Contains(meleeDescription))throw new InvalidOperationException("Melee effect/identity missing: "+id);
                    if(!models.Add("melee/"+melee.family))throw new InvalidOperationException("Duplicate melee design: "+id);
                }
                else throw new InvalidOperationException("Unexpected relic type: "+id);
                checks++;
            }
            string output="D:/Unity/BuildTemp/boss-relic-review.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));
            Debug.Log("BOSS_RELIC_REVIEW_OK / "+checks+" / "+output);
        }
    }
}
