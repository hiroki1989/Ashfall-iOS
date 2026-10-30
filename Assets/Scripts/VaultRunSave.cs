using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string RunSaveKey = "Vault.ExpeditionCheckpoint.v1";
        [Serializable] sealed class RunCheckpoint
        {
            public int version=1, seed, dungeon, stage, room, kills, ammo, activeWeaponSlot, missionKills, debtChainCount, chestRoomForRun, ammoCratePlanCount;
            public float actionSkillPoints;
            public int[] routeX, routeY, ammoReserves, ammoCrateRooms, upgradeKeys, upgradeValues;
            public float hp, baseMaxHp, damageBonus, fireBonus, runDamage, runFire, runVitality, runDash, runChain, runHarvest, runTime, artifactShield, artifactClock;
            public float playerX, playerZ, aimX, aimZ;
            public bool cleared, missionCompleted;
            public List<FoeCheckpoint> foes=new List<FoeCheckpoint>();
            public List<GunDropCheckpoint> guns=new List<GunDropCheckpoint>();
            public List<ArmorDropCheckpoint> armor=new List<ArmorDropCheckpoint>();
            public List<ArtifactDropCheckpoint> modules=new List<ArtifactDropCheckpoint>();
            public List<MeleeDropCheckpoint> melee=new List<MeleeDropCheckpoint>();
            public List<SupplyCheckpoint> supplies=new List<SupplyCheckpoint>();
            public List<PositionCheckpoint> chests=new List<PositionCheckpoint>();
            public List<AmmoCrateCheckpoint> ammoCrates=new List<AmmoCrateCheckpoint>();
        }
        [Serializable] sealed class FoeCheckpoint { public int kind; public bool elite; public float x,z,hp,maxHp,timer; }
        [Serializable] sealed class GunDropCheckpoint { public Gun item; public Vector3 position; }
        [Serializable] sealed class ArmorDropCheckpoint { public ArmorItem item; public Vector3 position; }
        [Serializable] sealed class ArtifactDropCheckpoint { public ArtifactItem item; public Vector3 position; }
        [Serializable] sealed class MeleeDropCheckpoint { public MeleeWeaponItem item; public Vector3 position; }
        [Serializable] sealed class SupplyCheckpoint { public int credits,ammoType,ammoAmount; public bool healthPack; public Vector3 position; }
        [Serializable] sealed class PositionCheckpoint { public Vector3 position; public bool rare; }
        [Serializable] sealed class AmmoCrateCheckpoint { public Vector3 position; public int type,amount; public bool mission; }

        public bool HasSavedRun => !smoke && PlayerPrefs.HasKey(RunSaveKey);
        void ClearRunCheckpoint(){if(smoke)return;if(PlayerPrefs.HasKey(RunSaveKey)){PlayerPrefs.DeleteKey(RunSaveKey);PlayerPrefs.Save();}}

        public bool SaveRunCheckpoint()
        {
            if(IsOverworld){if(screen!=Screen.Pause||smoke)return false;RememberOverworldPosition(PlayerPosition,true);SaveInventory();SaveProgression();return true;}
            if(smoke||screen!=Screen.Pause||player==null||world==null)return false;
            var save=new RunCheckpoint
            {
                seed=seed,dungeon=DungeonIndex,stage=SelectedStageId,room=room,kills=kills,ammo=ammo,activeWeaponSlot=ActiveWeaponSlot,
                missionKills=missionKills,missionCompleted=missionCompleted,debtChainCount=debtChainCount,chestRoomForRun=chestRoomForRun,actionSkillPoints=ActionSkillPoints,
                ammoCratePlanCount=ammoCratePlanCount,hp=hp,baseMaxHp=baseMaxHp,damageBonus=damageBonus,fireBonus=fireBonus,
                runDamage=runDamage,runFire=runFire,runVitality=runVitality,runDash=runDash,runChain=runChain,runHarvest=runHarvest,
                runTime=runTime,artifactShield=ArtifactShield,artifactClock=artifactCombatClock,playerX=player.position.x,playerZ=player.position.z,
                aimX=aim.x,aimZ=aim.z,cleared=cleared,ammoReserves=SaveAmmoReserves(),ammoCrateRooms=ammoCrateRoomPlan.ToArray()
            };
            save.routeX=new int[route.Count];save.routeY=new int[route.Count];for(int i=0;i<route.Count;i++){save.routeX[i]=route[i].x;save.routeY[i]=route[i].y;}
            var keys=new List<int>();var values=new List<int>();foreach(var pair in runUpgradeCounts){keys.Add(pair.Key);values.Add(pair.Value);}save.upgradeKeys=keys.ToArray();save.upgradeValues=values.ToArray();
            foreach(var foe in Enemies)if(foe!=null&&!foe.dead)save.foes.Add(new FoeCheckpoint{kind=foe.kind,elite=foe.elite,x=foe.transform.position.x,z=foe.transform.position.z,hp=foe.hp,maxHp=foe.maxHp,timer=foe.timer});
            foreach(var d in drops)save.guns.Add(new GunDropCheckpoint{item=d.gun,position=d.position});
            foreach(var d in armorDrops)save.armor.Add(new ArmorDropCheckpoint{item=d.item,position=d.position});
            foreach(var d in artifactDrops)save.modules.Add(new ArtifactDropCheckpoint{item=d.item,position=d.position});
            foreach(var d in meleeDrops)save.melee.Add(new MeleeDropCheckpoint{item=d.item,position=d.position});
            foreach(var d in supplyDrops)save.supplies.Add(new SupplyCheckpoint{credits=d.credits,ammoType=(int)d.ammoType,ammoAmount=d.ammoAmount,healthPack=d.healthPack,position=d.position});
            foreach(var chest in chests)if(chest!=null&&!chest.opened)save.chests.Add(new PositionCheckpoint{position=chest.position,rare=chest.rare});
            foreach(var crate in fieldAmmoCrates)if(crate!=null&&crate.visual!=null)save.ammoCrates.Add(new AmmoCrateCheckpoint{position=crate.position,type=(int)crate.type,amount=crate.amount,mission=crate.mission});
            SaveInventory();SaveProgression();PlayerPrefs.SetString(RunSaveKey,JsonUtility.ToJson(save));PlayerPrefs.Save();return true;
        }

        public bool ResumeSavedRun()
        {
            if(!HasSavedRun)return false;
            RunCheckpoint save;
            try{save=JsonUtility.FromJson<RunCheckpoint>(PlayerPrefs.GetString(RunSaveKey));}
            catch(Exception e){Debug.LogWarning("Expedition checkpoint could not be read: "+e.GetType().Name);return false;}
            if(!ValidateCheckpoint(save))
            {Debug.LogWarning("Expedition checkpoint is invalid; retained without changing equipment or progression.");return false;}
            IsOverworld=false;EnsureOverworldState();if(save.stage>=0)SetOverworldReturnStage(save.stage);
            DungeonIndex=save.dungeon;SelectedStageId=save.stage<0?-1:Mathf.Clamp(save.stage,0,ExpeditionMap.StageCount-1);ResetGameplayOverlays();shake=0;meleeAttack?.ResetCooldown();seed=save.seed;room=Mathf.Clamp(save.room,0,CurrentDungeon.AreaCount-1);rng=new System.Random(seed^unchecked((room+1)*7919)^save.kills);
            if(save.foes==null)save.foes=new List<FoeCheckpoint>();if(save.guns==null)save.guns=new List<GunDropCheckpoint>();if(save.armor==null)save.armor=new List<ArmorDropCheckpoint>();if(save.modules==null)save.modules=new List<ArtifactDropCheckpoint>();if(save.melee==null)save.melee=new List<MeleeDropCheckpoint>();if(save.supplies==null)save.supplies=new List<SupplyCheckpoint>();if(save.chests==null)save.chests=new List<PositionCheckpoint>();if(save.ammoCrates==null)save.ammoCrates=new List<AmmoCrateCheckpoint>();
            route.Clear();for(int i=0;i<save.routeX.Length;i++)route.Add(new Vector2Int(save.routeX[i],save.routeY[i]));
            kills=save.kills;ActionSkillPoints=Mathf.Clamp(save.actionSkillPoints,0,ActionSkillChargeMax);ricochetEvents=0;splitEvents=0;runTime=save.runTime;baseMaxHp=save.baseMaxHp;runDamage=save.runDamage;runFire=save.runFire;runVitality=save.runVitality;runDash=save.runDash;runChain=save.runChain;runHarvest=save.runHarvest;
            damageBonus=save.damageBonus;fireBonus=save.fireBonus;RecalculateArmor();hp=Mathf.Clamp(save.hp,1,maxHp);LoadAmmoReserves(save.ammoReserves);ActiveWeaponSlot=Mathf.Clamp(save.activeWeaponSlot,0,AvailableWeaponSlotCount-1);gun=WeaponInSlot(ActiveWeaponSlot)??WeaponInSlot(0)??Weapons[0];ActiveWeaponSlot=Mathf.Max(0,EquippedWeaponSlot(gun));SelectedWeapon=Weapons.IndexOf(gun);ammo=Mathf.Clamp(save.ammo,0,EffectiveMagazine);reload=0;reloadPending=0;invincible=1;dodgeCooldown=0;dodgeTimer=0;fireTimer=0;debtChainCount=save.debtChainCount;
            chestRoomForRun=save.chestRoomForRun;ammoCratePlanCount=save.ammoCratePlanCount;ammoCrateRoomPlan.Clear();if(save.ammoCrateRooms!=null)ammoCrateRoomPlan.AddRange(save.ammoCrateRooms);
            ResetArtifactCombat();ArtifactShield=save.artifactShield;artifactCombatClock=save.artifactClock;missionKills=save.missionKills;missionCompleted=save.missionCompleted;
            runUpgradeCounts.Clear();if(save.upgradeKeys!=null&&save.upgradeValues!=null)for(int i=0;i<Math.Min(save.upgradeKeys.Length,save.upgradeValues.Length);i++)runUpgradeCounts[save.upgradeKeys[i]]=save.upgradeValues[i];
            screen=Screen.Playing;fieldAmmoCrates.Clear();BuildRoom(false);float groundedPlayerY=player.position.y;var savedPosition=new Vector3(save.playerX,groundedPlayerY,save.playerZ);player.position=Blocked(savedPosition,.45f)?new Vector3(Layout.Entry.x,groundedPlayerY,Layout.Entry.z):savedPosition;aim=new Vector3(save.aimX,0,save.aimZ);if(aim.sqrMagnitude<.01f)aim=Vector3.right;aim.Normalize();player.rotation=Quaternion.LookRotation(aim);PlayerWeapon.Aim(aim);cleared=save.cleared;
            foreach(var state in save.foes){var position=new Vector3(state.x,.65f,state.z);if(Blocked(position,.8f))position=FindEnemySpawn();restoringChampionCheckpoint=true;try{SpawnFoe(position,state.kind);}finally{restoringChampionCheckpoint=false;}var foe=Enemies[Enemies.Count-1];if(state.elite){foe.elite=true;foe.ConfigureDifficulty(1,1.35f,.12f);foe.transform.localScale*=1.15f;}foe.hp=Mathf.Clamp(state.hp,1,state.maxHp);foe.maxHp=state.maxHp;foe.timer=state.timer;}
            restoringLoot=true;
            foreach(var d in save.guns)if(d.item!=null)AddDrop(d.item,d.position);
            foreach(var d in save.armor)if(d.item!=null)AddArmorDrop(d.item,d.position);
            foreach(var d in save.modules)if(d.item!=null)AddArtifactDrop(d.item,d.position);
            foreach(var d in save.melee)if(d.item!=null)AddMeleeDrop(d.item,d.position);
            restoringLoot=false;
            foreach(var d in save.supplies){if(d.healthPack)AddHealthPackDrop(d.position);else if(d.credits>0)AddScrapDrop(d.position,d.credits);else if(d.ammoAmount>0)AddAmmoDrop(d.position,NormalizeAmmoType((AmmoType)Mathf.Clamp(d.ammoType,0,AmmoTypeCount-1)),d.ammoAmount);}
            foreach(var c in save.chests){Vector3 p=c.position;p.y=0;var obj=CreateLootPresentation("EQUIPMENT CHEST",p,new Vector3(1.8f,.88f,1.0f),Gold,LootPresentationKind.Chest);if(c.rare)EquipmentChestVisual.SetRare(obj);chests.Add(new Chest{position=p,visual=obj,rare=c.rare});}
            foreach(var c in save.ammoCrates){
                if(c.mission){var obj=CreateLootPresentation("MISSION SUPPLIES",c.position,new Vector3(1.15f,.55f,.9f),new Color(.9f,.48f,.19f),LootPresentationKind.AmmoCrate);ObjectiveGroundRing.Create(obj.transform,new Vector3(c.position.x,0,c.position.z),1.15f);fieldAmmoCrates.Add(new FieldAmmoCrate{position=c.position,visual=obj,mission=true});}
                else {var type=NormalizeAmmoType((AmmoType)Mathf.Clamp(c.type,0,AmmoTypeCount-1));var obj=CreateLootPresentation(AmmoPickupName(type,c.amount),c.position,new Vector3(.82f,.38f,.72f),AmmoCrateColor(type),LootPresentationKind.AmmoCrate,type,c.amount);fieldAmmoCrates.Add(new FieldAmmoCrate{position=c.position,type=type,amount=c.amount,visual=obj});}
            }
            ReconcileFloorCompletion();Layout.BuildDistanceField(PlayerPosition);Layout.Discover(PlayerPosition);
            UpdateCamera(true);PlayerPrefs.DeleteKey(RunSaveKey);PlayerPrefs.Save();Notify("EXPEDITION RESTORED / Checkpoint consumed","探索を再開 / 保存データを復元しました");return true;
        }

        static bool ValidPosition(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool ValidateCheckpoint(RunCheckpoint save)
        {
            if(save==null||save.version!=1||save.dungeon<0||save.dungeon>2)return false;
            int count=DungeonCatalog.Get(save.dungeon).AreaCount;
            if(save.room<0||save.room>=count||save.routeX==null||save.routeY==null||save.routeX.Length!=count||save.routeY.Length!=count)return false;
            var visited=new HashSet<Vector2Int>();
            for(int i=0;i<count;i++)
            {
                var c=new Vector2Int(save.routeX[i],save.routeY[i]);
                if(Math.Abs((long)c.x)>count||Math.Abs((long)c.y)>count||!visited.Add(c))return false;
                if(i==0&&c!=Vector2Int.zero)return false;
                if(i>0&&Math.Abs(c.x-save.routeX[i-1])+Math.Abs(c.y-save.routeY[i-1])!=1)return false;
            }
            foreach(float value in new[]{save.hp,save.baseMaxHp,save.damageBonus,save.fireBonus,save.runDamage,save.runFire,save.runVitality,save.runDash,save.runChain,save.runHarvest,save.runTime,save.artifactShield,save.artifactClock,save.playerX,save.playerZ,save.aimX,save.aimZ})if(!Finite(value))return false;
            if(save.hp<=0||save.baseMaxHp<=0||save.runDash<0||save.runTime<0)return false;
            if(save.foes!=null&&save.foes.Count>2048)return false;
            if(save.guns!=null)foreach(var d in save.guns)if(d==null||!ValidPosition(d.position))return false;
            if(save.armor!=null)foreach(var d in save.armor)if(d==null||!ValidPosition(d.position))return false;
            if(save.modules!=null)foreach(var d in save.modules)if(d==null||!ValidPosition(d.position))return false;
            if(save.melee!=null)foreach(var d in save.melee)if(d==null||!ValidPosition(d.position))return false;
            if(save.supplies!=null)foreach(var d in save.supplies)if(d==null||!ValidPosition(d.position))return false;
            if(save.chests!=null)foreach(var d in save.chests)if(d==null||!ValidPosition(d.position))return false;
            if(save.ammoCrates!=null)foreach(var d in save.ammoCrates)if(d==null||!ValidPosition(d.position))return false;
            if(save.foes!=null)foreach(var foe in save.foes)
                if(foe==null||foe.kind<0||foe.kind>10||!Finite(foe.x)||!Finite(foe.z)||!Finite(foe.hp)||!Finite(foe.maxHp)||!Finite(foe.timer)||foe.hp<=0||foe.maxHp<=0)return false;
            return true;
        }

        void SaveRunAndReturnToMenu()
        {
            if(!SaveRunCheckpoint())return;
            screen=Screen.Menu;Time.timeScale=1;InventoryOpen=false;shake=0;
        }
    }
}
