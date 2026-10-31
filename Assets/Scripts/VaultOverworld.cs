using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        public bool IsOverworld { get; private set; }
        OverworldGeography overworldGeography;
        public OverworldGeography WorldGeography => overworldGeography ?? (overworldGeography=new OverworldGeography());
        readonly Dictionary<Chest,int> worldChestIds=new Dictionary<Chest,int>();
        readonly Dictionary<Foe,int> worldEnemyLevels=new Dictionary<Foe,int>();
        float worldSpawnTimer,worldSaveTimer;
        public static int MaxActiveOverworldEnemies=>Application.isMobilePlatform?18:28;
        sealed class WorldEnemySpawn
        {
            public Vector3 position;
            public int kind,level;
            public float hp=-1;
            public bool defeated,elite;
            public Foe active;
        }
        readonly List<WorldEnemySpawn> worldEnemySpawns=new List<WorldEnemySpawn>();
        readonly Dictionary<Foe,WorldEnemySpawn> worldEnemyInstances=new Dictionary<Foe,WorldEnemySpawn>();
        WorldEnemySpawn spawningWorldEnemy;
        int worldEnemyDefeated;
        public int OverworldEnemySpawnCount=>worldEnemySpawns.Count;
        public int OverworldEnemyDefeatedCount=>worldEnemyDefeated;
        public int ActiveOverworldEnemyCount=>worldEnemyInstances.Count;
        bool worldAtlasFromHub;
        public bool WorldBossConversationVisible => screen==Screen.Hub&&!WorldStorySeen;
        public bool CanWorldFastTravel => WorldStorySeen&&(screen==Screen.Hub||screen==Screen.Shop||IsOverworld&&(screen==Screen.Playing||screen==Screen.Pause||screen==Screen.DungeonSelect)||screen==Screen.DungeonSelect&&worldAtlasFromHub);
        public int NearbyWorldEntrance => IsOverworld?NearestWorldSite(WorldGeography.Entrances,3.5f):-1;
        public int NearbyWorldCamp => IsOverworld?NearestWorldSite(WorldGeography.Camps,3.5f):-1;
        int NearestWorldSite(Vector3[] sites,float distance)
        {int best=-1;float sq=distance*distance;for(int i=0;i<sites.Length;i++){float d=(sites[i]-PlayerPosition).sqrMagnitude;if(d<sq){sq=d;best=i;}}return best;}
        public int GetOverworldEnemyLevel(Foe foe)=>foe!=null&&worldEnemyLevels.TryGetValue(foe,out int level)?level:DebtLevel;

        public void ContinueOverworldGame()
        {
            EnsureOverworldState();
            if(!WorldStorySeen){EnterHub();return;}
            if(SavedOutside){BuildOverworld(SavedOverworldPosition);return;}
            EnterHub();
        }
        public void CompleteWorldBossConversation()
        {CompleteOverworldStory();MarkDebtIntroSeen();bossDeparting=departingRaiderBoss!=null;BeginFirstStepsTutorial();Notify("BOSS: Bring me a billion scrap and crush the faction strongholds. Briggs will collect.","ボス：10億スクラップを稼ぎ、各勢力の拠点も制圧しろ。返済はブリッグスに渡せ。");}
        public void ExitCampToOverworld()
        {
            EnsureOverworldState();if(!WorldStorySeen)return;
            Vector3 exit=WorldGeography.Camps[LastVisitedCamp]+Vector3.back*2.2f;
            BuildOverworld(exit);
            Notify("ASHFALL / Travel on foot to a dungeon entrance. Camps are safe.","アッシュフォール / ダンジョン入口へ歩いて向かおう。拠点は安全地帯です。");
        }
        bool worldAtlasFromDungeon;Screen worldAtlasReturnScreen;
        public void OpenOverworldMap()
        {
            if(InventoryOpen||!WorldStorySeen||!(IsOverworld||screen==Screen.Hub||screen==Screen.Playing||screen==Screen.Pause))return;
            worldAtlasFromDungeon=!IsOverworld&&screen!=Screen.Hub;worldAtlasReturnScreen=screen;
            if(selectedOverworldActivity!=null)CloseOverworldActivityChoice();
            if(IsOverworld)RememberOverworldPosition(PlayerPosition,true);
            worldAtlasFromHub=screen==Screen.Hub;screen=Screen.DungeonSelect;Time.timeScale=0;
        }
        public void CloseOverworldMap()
        {screen=worldAtlasFromDungeon?worldAtlasReturnScreen:worldAtlasFromHub?Screen.Hub:Screen.Playing;Time.timeScale=screen==Screen.Pause?0:1;}
        public void WithdrawFromExploration()
        {
            if(IsOverworld){screen=Screen.Playing;Time.timeScale=1;OpenOverworldMap();return;}
            EnsureOverworldState();int stage=PendingWorldStage;
            if(stage<0){EnterHub();return;}
            SaveInventory();ClearRunCheckpoint();SetOverworldReturnStage(-1);
            BuildOverworld(WorldGeography.Entrances[stage]+Vector3.back*2);
            Notify("WITHDREW / No mission reward","入口へ退却 / ミッション報酬なし");
        }
        public bool FastTravelToCamp(int camp)
        {
            EnsureOverworldState();if(!CanWorldFastTravel||!IsCampVisited(camp))return false;
            VisitOverworldCamp(camp);RememberOverworldPosition(WorldGeography.Camps[camp],false);SetOverworldReturnStage(-1);
            EnterHub();Notify("SAFE CAMP / "+OverworldCampName(camp,false),"拠点へ移動 / "+OverworldCampName(camp,true));return true;
        }
        public bool EnterNearbyWorldDungeon(int stage)
        {
            if(!IsOverworld||stage<0||stage>=ExpeditionMap.StageCount||(WorldGeography.Entrances[stage]-PlayerPosition).sqrMagnitude>3.5f*3.5f)return false;
            if(!FactionHeadquartersUnlocked(stage)){Notify(FactionBossUnlockRequirementLabel(stage,false),FactionBossUnlockRequirementLabel(stage,true));return false;}
            DiscoverOverworldDungeon(stage);
            SaveInventory();RememberOverworldPosition(WorldGeography.Entrances[stage]+Vector3.back*2,true);SetOverworldReturnStage(stage);
            return EnterExpeditionStage(stage);
        }
        void ClearWorldScene()
        {
            ClearActiveActionSkill();
            ResetOverworldRush();
            if(world!=null){world.gameObject.SetActive(false);Destroy(world.gameObject);}
            foreach(var material in materials)if(material!=null)Destroy(material);
            materials.Clear();Enemies.Clear();drops.Clear();meleeDrops.Clear();armorDrops.Clear();chests.Clear();supplyDrops.Clear();fieldAmmoCrates.Clear();ClearArtifactDrops();satellites.Clear();obstacles.Clear();worldChestIds.Clear();worldEnemyLevels.Clear();worldEnemyInstances.Clear();worldEnemySpawns.Clear();worldEnemyDefeated=0;nearbyOverworldActivities.Clear();CloseOverworldActivityChoice();
        }
        public void BuildOverworld(Vector3 position) {if(QueueWorldTransition(()=>BuildOverworldImmediate(position)))return;BuildOverworldImmediate(position);}
        void BuildOverworldImmediate(Vector3 position)
        {
            EnsureOverworldState();SaveInventory();ResetGameplayOverlays();ClearRunCheckpoint();ClearWorldScene();
            IsOverworld=true;SetOverworldReturnStage(-1);SelectedStageId=-1;room=0;kills=0;shake=0;cleared=false;missionCompleted=false;missionKills=0;
            runDamage=runFire=runVitality=runDash=runChain=runHarvest=0;damageBonus=fireBonus=0;
            ClearRunUpgradeHistory();ResetArtifactCombat();RefreshPermanentStats();hp=Mathf.Clamp(hp,1,maxHp);
            reload=0;reloadPending=0;dodgeTimer=0;fireTimer=0;invincible=2.5f;dodgeCooldown=0;meleeAttack?.ResetCooldown();
            world=new GameObject("ASHFALL / continuous overworld").transform;Layout=WorldGeography.Layout;
            world.gameObject.AddComponent<OverworldActivityVisuals>().Initialize(this);
            halfWidth=Layout.Columns*Layout.CellSize*.5f;halfDepth=Layout.Rows*Layout.CellSize*.5f;
            rng=new System.Random(unchecked(Environment.TickCount^OverworldMissionClears));
            art=new VaultArt(world,materials,new System.Random(OverworldGeography.Seed));
            OverworldScenery.Build(world,materials,WorldGeography);OverworldScenery.UpdateFocus(world,position);
            if(Blocked(position,.65f))position=WorldGeography.Camps[LastVisitedCamp];position.y=.65f;
            gun=WeaponInSlot(ActiveWeaponSlot)??Weapons[0];ammo=magazineAmmo.TryGetValue(gun.id,out int loaded)?Mathf.Clamp(loaded,0,EffectiveMagazine):EffectiveMagazine;
            player=art.Character(position,true);barrel=art.Weapon(player,gun.color);RefreshWeaponAppearance();PlayerAppearance?.ApplyArmor(EquippedArmor);
            aim=Vector3.right;hasAimedPoint=false;PlayerWeapon.Aim(aim);CreateSatellites();
            CreateWorldSites();CreateWorldChests();GenerateOverworldEnemies();CreateFactionCamps();StreamOverworldEnemies();RefreshOverworldActivities();
            screen=Screen.Playing;cam.orthographicSize=(MobileTouchControls.Enabled?10:11)/CameraZoom;
            Layout.BuildDistanceField(position);Layout.Discover(position);navigationTimer=.75f;worldSpawnTimer=.5f;worldSaveTimer=0;UpdateCamera(true);
            RememberOverworldPosition(position,true);
        }
        void CreateWorldSites()
        {
            WorldEntranceBuildings.Build(world,materials,WorldGeography);
            WorldEntranceBuildings.UpdateFocus(world,PlayerPosition);
        }
        static bool WorldChestIsRare(int id)
        {
            unchecked { uint hash=(uint)id*747796405u+277803737u; hash=(hash^(hash>>16))*2246822519u; return (hash^(hash>>13))%5u==0; }
        }
        void CreateWorldChests()
        {
            for(int i=0;i<WorldGeography.Chests.Length;i++)if(IsOverworldChestAvailable(i))
            {Vector3 p=WorldGeography.Chests[i];p.y=0;var chest=new Chest{position=p,rare=WorldChestIsRare(i)};chests.Add(chest);worldChestIds.Add(chest,i);}
            StreamWorldChests();
        }
        void StreamWorldChests()
        {
            // Keep all persistent site IDs, but load imported meshes only nearby.
            foreach(var chest in chests)
            {
                float distance=(chest.position-PlayerPosition).sqrMagnitude;
                if(chest.visual&&distance>220f*220f){Destroy(chest.visual);chest.visual=null;}
                else if(!chest.visual&&!chest.opened&&distance<180f*180f)
                    {chest.visual=CreateLootPresentation("WASTELAND EQUIPMENT CHEST",chest.position,new Vector3(1.8f,.88f,1),Gold,LootPresentationKind.Chest);if(chest.rare)EquipmentChestVisual.SetRare(chest.visual);}
            }
        }
        bool OpenOverworldChest()
        {
            Chest nearest=null;float best=2.4f;
            foreach(var chest in chests)if(!chest.opened){float d=Vector3.Distance(PlayerPosition,chest.position);if(d<best){best=d;nearest=chest;}}
            if(nearest==null||!worldChestIds.TryGetValue(nearest,out int id))return false;
            if(!MarkOverworldChestOpened(id))return false;
            DropEquipmentChestReward(nearest.position,Mathf.Max(1,DebtLevel+rng.Next(-1,2)),nearest.rare);
            nearest.opened=true;EquipmentChestVisual.Open(nearest.visual);SaveInventory();
            Notify("EQUIPMENT CACHE / Restocks after three mission clears","装備チェスト / ミッションを3回達成すると復活します");return true;
        }
        void TickOverworld()
        {
            TickOverworldRush();
            if(selectedOverworldActivity!=null)Time.timeScale=0;
            if(Input.GetKeyDown(KeyCode.M)){OpenOverworldMap();return;}
            worldSaveTimer-=Time.deltaTime;if(worldSaveTimer<=0){worldSaveTimer=8;RememberOverworldPosition(PlayerPosition,true);SaveInventory();}
            worldSpawnTimer-=Time.deltaTime;if(worldSpawnTimer>0)return;worldSpawnTimer=.5f;
            int nearbyDungeon=NearbyWorldEntrance;if(nearbyDungeon>=0)DiscoverOverworldDungeon(nearbyDungeon);
            OverworldScenery.UpdateFocus(world,PlayerPosition);WorldEntranceBuildings.UpdateFocus(world,PlayerPosition);StreamOverworldEnemies();TickFactionCamps();StreamWorldChests();RefreshOverworldActivities();
        }
        void GenerateOverworldEnemies()
        {
            worldEnemySpawns.Clear();worldEnemyInstances.Clear();worldEnemyDefeated=0;
            int[] roster={0,0,1,2,4,5,7,8,9};
            var random=new System.Random(rng.Next());
            // Roads exclude buildings, cars and trees during geography generation. Populate
            // their inner lanes without materializing thousands of remote scenery chunks.
            // Each load owns a fixed population; streaming never regenerates defeated packs.
            for(int x=-49;x<50;x++)for(int z=-49;z<50;z++)
            {
                bool vertical=random.Next(2)==0;
                int count=random.NextDouble()<.06?1:random.Next(4,8);
                float along=(vertical?z:x)*100+25+(float)random.NextDouble()*50;
                var positions=new List<Vector3>(count);
                for(int member=0;member<count;member++)
                {
                    float t=along+(member-(count-1)*.5f)*2.7f;
                    float lane=member%2==0?-1.15f:1.15f;
                    var p=vertical?new Vector3(WorldGeography.StreetX(x,t)+lane,.65f,t)
                        :new Vector3(t,.65f,WorldGeography.StreetZ(z,t)+lane);
                    if(Mathf.Abs(p.x)>4980||Mathf.Abs(p.z)>4980||Mathf.Abs(p.x-WorldGeography.RiverX(p.z))<31&&!WorldGeography.Bridge(p.z))continue;
                    bool safe=true;
                    foreach(var camp in WorldGeography.Camps)if((camp-p).sqrMagnitude<24*24){safe=false;break;}
                    if(safe)positions.Add(p);
                }
                // Avoid fragmented pairs/triples at rivers and camp safety boundaries.
                if(positions.Count!=count)continue;
                int level=Mathf.Max(1,DebtLevel+random.Next(-1,2));
                foreach(var p in positions)worldEnemySpawns.Add(new WorldEnemySpawn{position=p,kind=roster[random.Next(roster.Length)],level=level,elite=random.NextDouble()<.0225});
            }
        }

        public void OnOverworldEnemyKilled(Foe foe)
        {
            if(foe==null||!worldEnemyInstances.TryGetValue(foe,out var spawn))return;
            if(!spawn.defeated){spawn.defeated=true;worldEnemyDefeated++;}
            spawn.active=null;worldEnemyInstances.Remove(foe);worldEnemyLevels.Remove(foe);
        }
        void StreamOverworldEnemies()
        {
            for(int i=Enemies.Count-1;i>=0;i--)
            {
                var foe=Enemies[i];
                if(foe==null){Enemies.RemoveAt(i);continue;}
                if(foe.dead){OnOverworldEnemyKilled(foe);Enemies.RemoveAt(i);continue;}
                if((foe.transform.position-PlayerPosition).sqrMagnitude<=130*130)continue;
                if(worldEnemyInstances.TryGetValue(foe,out var spawn))
                {spawn.position=foe.transform.position;spawn.position.y=.65f;spawn.hp=foe.hp;spawn.active=null;worldEnemyInstances.Remove(foe);}
                worldEnemyLevels.Remove(foe);foe.gameObject.SetActive(false);Destroy(foe.gameObject);Enemies.RemoveAt(i);
            }
            int streamLimit=OverworldRushActive?Mathf.Max(1,MaxActiveOverworldEnemies-8):MaxActiveOverworldEnemies;
            if(worldEnemyInstances.Count>=streamLimit)return;
            var nearby=new List<WorldEnemySpawn>();
            Vector3 focus=PlayerPosition;
            foreach(var spawn in worldEnemySpawns)
                if(!spawn.defeated&&spawn.active==null&&(spawn.position-focus).sqrMagnitude<90*90)nearby.Add(spawn);
            nearby.Sort((a,b)=>(a.position-focus).sqrMagnitude.CompareTo((b.position-focus).sqrMagnitude));
            foreach(var nearest in nearby)
            {
                if(worldEnemyInstances.Count>=streamLimit)break;
                // Geography is generated lazily; validate the actual footprint only for
                // nearby spawns so the initial world load does not build every chunk.
                if(WorldGeography.Blocked(nearest.position,.8f)||WorldEntranceBuildings.Blocked(nearest.position,.8f,WorldGeography))
                {
                    Vector3 original=nearest.position; bool found=false;
                    for(int ring=1;ring<=8&&!found;ring++)for(int direction=0;direction<8;direction++)
                    {
                        float angle=direction*Mathf.PI/4f;
                        var candidate=original+new Vector3(Mathf.Cos(angle)*ring*2,0,Mathf.Sin(angle)*ring*2);
                        if(!WorldGeography.Blocked(candidate,.8f)&&!WorldEntranceBuildings.Blocked(candidate,.8f,WorldGeography)){nearest.position=candidate;found=true;break;}
                    }
                    if(!found)continue;
                }
                spawningWorldEnemy=nearest;
                try
                {
                    SpawnFoe(nearest.position,nearest.kind);
                    var foe=Enemies[Enemies.Count-1];nearest.position=foe.transform.position;nearest.position.y=.65f;nearest.active=foe;worldEnemyInstances.Add(foe,nearest);
                    if(nearest.hp>=0)foe.hp=Mathf.Clamp(nearest.hp,.001f,foe.maxHp);
                }
                finally{spawningWorldEnemy=null;}
            }
        }
        public bool EnterNearbyWorldCamp(int camp)
        {
            if(!IsOverworld||camp<0||camp>=WorldGeography.Camps.Length||(WorldGeography.Camps[camp]-PlayerPosition).sqrMagnitude>3.5f*3.5f)return false;
            VisitOverworldCamp(camp);SaveInventory();RememberOverworldPosition(WorldGeography.Camps[camp],false);EnterHub();return true;
        }        bool HandleOverworldInteraction()
        {
            if(!IsOverworld)return false;
            int camp=NearbyWorldCamp;if(camp>=0)return EnterNearbyWorldCamp(camp);
            int entrance=NearbyWorldEntrance;if(entrance>=0)return EnterNearbyWorldDungeon(entrance);
            return OpenNearbyOverworldActivity();
        }
        void ConfigureOverworldFoe(Foe foe)
        {
            int level=spawningWorldEnemy!=null?spawningWorldEnemy.level:Mathf.Max(1,DebtLevel+rng.Next(-1,2));worldEnemyLevels[foe]=level;
            float hpScale=1.5f*Foe.LevelHealthScale(level),damageScale=Foe.LevelDamageScale(level);
            var balance=VaultBalanceProfile.Current;
            foe.ConfigureDifficulty(hpScale*(balance!=null?Mathf.Max(.01f,balance.enemyHpMultiplier):1),damageScale*(balance!=null?Mathf.Max(.01f,balance.enemyDamageMultiplier):1),Mathf.Min(.25f,(level-1)*.005f));
        }
        void ReturnFromWorldDungeon(bool victory)
        {
            bool success=victory&&(SelectedExpeditionStage==null||MissionCompleted);int stage=PendingWorldStage;
            if(!victory)return;
            if(success)RegisterOverworldMissionClear();
            string rewardEn=toast,rewardJa=toastJapanese;
            Vector3 p=stage>=0?WorldGeography.Entrances[stage]+Vector3.back*2:SavedOverworldPosition;
            SetOverworldReturnStage(-1);BuildOverworld(p);SaveProgression();
            Notify(rewardEn+" / BACK AT ENTRANCE",rewardJa+" / ダンジョン入口へ帰還");
        }
        void SaveWorldLocationOnSuspend()
        {if(IsOverworld&&screen!=Screen.Dead&&player!=null){RememberOverworldPosition(PlayerPosition,true);SaveInventory();}}
        void OnApplicationPause(bool paused){if(paused)SaveWorldLocationOnSuspend();}
        void OnApplicationQuit(){SaveWorldLocationOnSuspend();}
    }
}



