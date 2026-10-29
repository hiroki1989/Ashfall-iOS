using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame : MonoBehaviour
    {
        public const int MeleeAttackMouseButton=1;
        public const KeyCode MeleeAttackKey=KeyCode.Mouse1;
        public enum Screen { Menu, Hub, Shop, DungeonSelect, Playing, Reward, Pause, Dead, Victory }
        public Screen screen;
        public bool Active => screen==Screen.Playing&&!InventoryOpen&&!worldTransitionPending&&!actionSkillTutorialVisible;
        public Vector3 PlayerPosition => player==null?Vector3.zero:player.position;
        public Camera GameCamera => cam;
        public Vector3 ScreenMovement(float horizontal,float vertical)
        { return new Vector3(vertical,0,-horizontal).normalized; }
        public readonly List<Foe> Enemies=new List<Foe>();
        public readonly List<Vector2Int> route=new List<Vector2Int>();
        readonly List<Rect> obstacles=new List<Rect>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Drop> drops=new List<Drop>();
        class Drop { public Gun gun; public Vector3 position; public GameObject visual; }
        class MeleeDrop { public MeleeWeaponItem item; public Vector3 position; public GameObject visual; }
        readonly List<MeleeDrop> meleeDrops=new List<MeleeDrop>();
        Transform world, player, barrel;
        public SectorLayout Layout {get;private set;}
        public AimedWeapon PlayerWeapon=>barrel==null?null:barrel.GetComponent<AimedWeapon>();
        public Vector3 ExitPosition=>Layout.Exit;
        float navigationTimer,debtChainTimer;
        int debtChainCount;
        readonly List<Transform> satellites=new List<Transform>();
        Camera cam;
        VaultArt art;
        Vector3 cameraFocus;
        Texture2D menuArt;
        System.Random rng;
        public Gun gun;
        public int room, seed, kills, cores, rank, ammo;
        public int ricochetEvents, splitEvents;
        public float hp, maxHp, damageBonus, fireBonus, dodgeTimer, dodgeCooldown, invincible, reload;
        internal float fireTimer;
        float halfWidth, halfDepth, runTime, shake, toastTimer;
        Vector3 dodgeDirection, aim=Vector3.right, mobileInputAim=Vector3.right;
        Vector3 aimedPoint;Ray aimedRay;bool hasAimedPoint,aimUsesCursor;
        string toast="",toastJapanese="";
        string Toast=>VaultLanguage.T(toast,toastJapanese);
        bool cleared, smoke;
        int[] choices=new int[3];
        public System.Collections.Generic.IReadOnlyList<int> RewardChoices=>choices;
        Font font;
        Font englishHeadingFont;
        AudioSource audioSource;
        VaultMelee meleeAttack;
        AudioClip fireSound, scatterSound, coilSound, hitSound, pickupSound, ammoPickupSound;
        Texture2D pixel;
        GUIStyle label, title, small, button;
        static readonly Color Cyan=new Color(93/255f,226/255f,210/255f), Gold=new Color(1f,209/255f,102/255f), Ink=new Color(21/255f,36/255f,50/255f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() { if(Array.IndexOf(Environment.GetCommandLineArgs(),"--real-city-scenery-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--hero3d-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--enemy-cache-review")>=0)return; if(FindAnyObjectByType<VaultGame>()==null) new GameObject("ASHFALL / runtime").AddComponent<VaultGame>(); }
        void Awake()
        {
            Time.timeScale=1;
            Application.targetFrameRate=MobileTouchControls.Enabled?30:120;
            if(MobileTouchControls.Enabled)QualitySettings.vSyncCount=0;
            cores=PlayerPrefs.GetInt("Vault.Cores",0); rank=PlayerPrefs.GetInt("Vault.Rank",0);
            font=VaultFont.Preferred();
            if(font==null)font=Resources.Load<Font>("Fonts/851Gkktt-005-narrow");
            if(font==null)font=Resources.Load<Font>("Fonts/851Gkktt-005");
            if(font==null)font=Resources.Load<Font>("Fonts/NotoSansJP");
            if(font==null)font=Font.CreateDynamicFontFromOSFont(Application.platform==RuntimePlatform.IPhonePlayer?new[]{"HiraginoSans-W3","Hiragino Sans","Arial"}:new[]{"Yu Gothic UI","Meiryo","Segoe UI","Arial"},20);
            englishHeadingFont=VaultFont.Preferred();
            if(englishHeadingFont==null)englishHeadingFont=Resources.Load<Font>("Fonts/851Gkktt-005");
            if(englishHeadingFont==null)englishHeadingFont=Resources.Load<Font>("Fonts/BarlowCondensed-Bold");
            pixel=new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
            cam=new GameObject("Isometric camera").AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=13.5f; cam.backgroundColor=Ink; cam.clearFlags=CameraClearFlags.SolidColor;
            cam.tag="MainCamera";
            // Runtime-created cameras do not receive Unity's default listener.
            // Without one, all weapon AudioSources are silent on device.
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Ashfall.MasterVolume", 1f));
            cam.transform.rotation=Quaternion.Euler(35.264f,45,0);cam.allowHDR=true;cam.depthTextureMode=DepthTextureMode.Depth|DepthTextureMode.DepthNormals;cam.gameObject.AddComponent<VaultPost>();
            QualitySettings.antiAliasing=4;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=145;QualitySettings.pixelLightCount=4;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.32f,.37f,.43f);RenderSettings.ambientEquatorColor=new Color(.25f,.27f,.23f);RenderSettings.ambientGroundColor=new Color(.13f,.11f,.085f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.11f,.12f,.095f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.009f;
            var light=new GameObject("Ash-filtered sunlight / shadow casting").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.95f,.84f);light.intensity=1.2f;light.shadows=LightShadows.Soft;light.shadowStrength=.78f;light.transform.rotation=Quaternion.Euler(48,-25,0);
            var fill=new GameObject("Cool sky fill").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.57f,.72f,.89f);fill.intensity=.38f;fill.transform.rotation=Quaternion.Euler(28,145,0);
            menuArt=Resources.Load<Texture2D>("Art/WastelandKeyArt");
            audioSource=gameObject.AddComponent<AudioSource>();fireSound=GunReport(false,false);scatterSound=GunReport(true,false);coilSound=GunReport(false,true);hitSound=Resources.Load<AudioClip>("SoundEffects/player-hit");if(!hitSound)hitSound=GunReport(true,true);pickupSound=Resources.Load<AudioClip>("WeaponSFX/Handgun_equip");if(pickupSound==null)pickupSound=Tone(620,.16f);ammoPickupSound=Resources.Load<AudioClip>("WeaponSFX/Ammo_pickup");if(ammoPickupSound==null)ammoPickupSound=pickupSound;
            meleeAttack=gameObject.AddComponent<VaultMelee>();meleeAttack.Initialize(this);
            smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--dungeon-entry-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--live-building-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--premium-ui-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--asset-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--overworld-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--economy-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--equipment-screen-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--mobile-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--manufacturer-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--world-visual-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--hero3d-playcheck")>=0;
            // Debt skill unlocks determine whether slots 3/4 survive loadout repair.
            ProgressionInitialize();LoadInventory();screen=Screen.Menu;seed=Environment.TickCount&int.MaxValue;rng=new System.Random(seed);
            // The title screen uses menuArt. Building a dungeon here loads every
            // environment and character asset before the first UI frame on iOS.
            // BuildRoom is called by a new or resumed expedition instead.
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--economy-review")>=0) gameObject.AddComponent<SmokeRun>();
        }
        AudioClip Tone(float frequency,float duration)
        {
            int count=(int)(22050*duration); var data=new float[count];
            for(int i=0;i<count;i++) data[i]=Mathf.Sin(2*Mathf.PI*frequency*i/22050)*.18f*(1-(float)i/count);
            var clip=AudioClip.Create("Synth",count,1,22050,false); clip.SetData(data,0); return clip;
        }
        AudioClip GunReport(bool heavy,bool electric)
        {
            const int rate=22050;float duration=heavy?.23f:.14f;int count=(int)(rate*duration);
            var data=new float[count];var noise=new System.Random(heavy?277:151);float low=0;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate,raw=(float)(noise.NextDouble()*2-1);low=Mathf.Lerp(low,raw,.2f);
                float envelope=Mathf.Exp(-t*(heavy?18:33));
                float body=Mathf.Sin(2*Mathf.PI*(heavy?72:115)*t)*Mathf.Exp(-t*25);
                float crack=raw*Mathf.Exp(-t*110);
                float coil=electric?Mathf.Sin(2*Mathf.PI*(1250*t-1800*t*t))*.22f*Mathf.Exp(-t*17):0;
                data[i]=Mathf.Clamp((low*.42f+body*.32f)*envelope+crack*.32f+coil,-.85f,.85f);
            }
            var clip=AudioClip.Create(electric?"Vacuum tube discharge":heavy?"Pump shotgun report":"Salvage carbine report",count,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void StartRun(int? fixedSeed=null) {if(QueueWorldTransition(()=>StartRunImmediate(fixedSeed)))return;StartRunImmediate(fixedSeed);}
        void StartRunImmediate(int? fixedSeed=null)
        {
            IsOverworld=false;ClearRunCheckpoint();shake=0;
            ResetGameplayOverlays(); seed=fixedSeed??(Environment.TickCount&int.MaxValue); rng=new System.Random(seed);
            route.Clear(); var cell=Vector2Int.zero; route.Add(cell);
            for(int i=1;i<CurrentDungeon.AreaCount;i++)
            {
                var available=new List<Vector2Int>();
                foreach(var dir in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left}) if(!route.Contains(cell+dir)) available.Add(cell+dir);
                if(available.Count==0) { route.Clear(); cell=Vector2Int.zero; route.Add(cell); i=0; continue; }
                cell=available[rng.Next(available.Count)];
                route.Add(cell);
            }
            room=finalDebtBossFight?CurrentDungeon.AreaCount-1:0;kills=0;ricochetEvents=0;splitEvents=0;runTime=0;baseMaxHp=100*Mathf.Max(.01f,VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.startingHpMultiplier : 1f);RecalculateArmor();hp=maxHp;damageBonus=0;fireBonus=0;
            chestRoomForRun=-1; // Chests now roll once per non-final area; extraction always has one.
            PrepareAmmoCratesForRun();
            ResetArtifactCombat();runDamage=0;runFire=0;runVitality=0;runDash=0;runChain=0;runHarvest=0;debtChainCount=0;debtChainTimer=0;
            gun=WeaponInSlot(ActiveWeaponSlot);if(gun==null)gun=Weapons[SelectedWeapon];ammo=magazineAmmo.TryGetValue(gun.id,out int loaded)?Mathf.Min(EffectiveMagazine,loaded):EffectiveMagazine;StopReloadSound();reload=0;reloadPending=0;dodgeCooldown=0;invincible=2.5f;dodgeTimer=0;fireTimer=0;meleeAttack?.ResetCooldown();lastHurt=-10;screen=Screen.Playing;
            BuildRoom(true); Notify("ZONE 01 / Scavenge. Watch the infected.","区域01 / 感染者を警戒し、物資を回収せよ");
        }
        Material Mat(Color color,bool glow=false,bool trail=false)
        {
            var m=new Material(Resources.Load<Shader>(trail?"VaultTrail":"VaultSurface")); m.color=color;
            if(glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*.75f); }
            materials.Add(m); return m;
        }
        GameObject Shape(string name,PrimitiveType primitive,Vector3 position,Vector3 scale,Color color,Transform parent=null,bool glow=false)
        {
            var obj=GameObject.CreatePrimitive(primitive); obj.name=name; obj.transform.SetParent(parent??world,false); obj.transform.localPosition=position; obj.transform.localScale=scale;
            Destroy(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial=Mat(color,glow); return obj;
        }
        public void BuildRoom(bool populate){if(populate&&QueueWorldTransition(()=>BuildRoomImmediate(populate)))return;BuildRoomImmediate(populate);}
        void BuildRoomImmediate(bool populate)
        {
            ClearActiveActionSkill();
            IsOverworld=false;
            if(world!=null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            foreach(var m in materials) Destroy(m); materials.Clear(); Enemies.Clear(); obstacles.Clear(); drops.Clear();foreach(var d in meleeDrops)if(d.visual!=null)Destroy(d.visual);meleeDrops.Clear();armorDrops.Clear();chests.Clear();supplyDrops.Clear();fieldAmmoCrates.Clear();
            world=new GameObject("Generated sector "+room).transform; satellites.Clear();
            ClearArtifactDrops();Layout=new SectorLayout(new System.Random(seed+room*7919),DungeonIndex);
            halfWidth=Layout.Columns*SectorLayout.Cell/2;halfDepth=Layout.Rows*SectorLayout.Cell/2;cleared=false;
            art=new VaultArt(world,materials,new System.Random(seed+room*7919));art.Floor(halfWidth,halfDepth,room);
            art.DungeonAtmosphere(DungeonIndex,Layout);
            art.FactionHideout(ExpeditionMap.FactionForStage(SelectedStageId),Layout);
            foreach(var rect in Layout.walls)art.Partition(rect);
            foreach(var p in Layout.covers)art.Cover(p);
            for(int i=0;i<Layout.chambers.Count;i++)art.Chamber(Layout.Center(Layout.chambers[i]),i);
            art.Gate(Layout.Exit);ImportedWorldAssets.DressInterior(world,Layout,false);
            player=art.Character(Layout.Entry,true);
            barrel=art.Weapon(player,gun==null?Cyan:gun.color);if(gun!=null)VaultArt.ConfigureWeapon(barrel,gun.effect,gun.color,VaultLanguage.DisplayModel(gun));RefreshWeaponAppearance();
            PlayerAppearance?.ApplyArmor(EquippedArmor);
            aim=Vector3.right;mobileInputAim=aim;aimTarget=null;hasAimedPoint=false;PlayerWeapon.Aim(aim);navigationTimer=0;
            CreateSatellites();
            if(populate)
            {
                if(room==CurrentDungeon.AreaCount-1)
                {
                    SpawnFoe(Layout.Exit,3);
                    int champions=rng.Next(3,6);
                    for(int i=0;i<champions;i++)
                    {
                        float angle=2*Mathf.PI*i/champions;
                        Vector3 aroundBoss=Layout.Exit+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*4f;
                        int kind=ExpeditionMap.FactionForStage(SelectedStageId)!=DungeonFaction.None?10:CurrentDungeon.enemyRoster[rng.Next(CurrentDungeon.enemyRoster.Length)];
                        SpawnFoeInternal(aroundBoss,kind,true);
                    }
                }
                else for(int i=0;i<Mathf.Max(1,Mathf.RoundToInt(CurrentDungeon.EnemyCount(room)*Mathf.Max(.01f,VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.enemyCountMultiplier : 1f)));i++)
                {
                    Vector3 p; int attempts=0;
                    do {var cell=Layout.chambers[rng.Next(Layout.chambers.Count)];p=Layout.Center(cell)+new Vector3((float)rng.NextDouble()*6-3,0,(float)rng.NextDouble()*4-2);attempts++;} while((Blocked(p,.8f)||!Layout.SpawnSafe(p))&&attempts<200);
                    if(Blocked(p,.8f)||!Layout.SpawnSafe(p)) p=FindEnemySpawn();
                    SpawnFoe(p,(ExpeditionMap.FactionForStage(SelectedStageId)!=DungeonFaction.None?10:CurrentDungeon.enemyRoster[rng.Next(CurrentDungeon.enemyRoster.Length)]));
                }
                SpawnRoomChest();
                SpawnAmmoCratesForCurrentRoom();
                ReconcileFloorCompletion();
            }
            cam.orthographicSize=7.5f/CameraZoom;UpdateCamera(true);
        }
        Vector3 FindEnemySpawn()
        {
            // Exhaustive finite fallback: never silently omit an encounter after random misses.
            var candidates=new List<Vector3>();
            for(int x=0;x<Layout.Columns;x++)for(int y=0;y<Layout.Rows;y++)
            {
                var p=Layout.Center(new Vector2Int(x,y));
                if(Layout.SpawnSafe(p)&&!Blocked(p,.8f))candidates.Add(p);
            }
            if(candidates.Count==0)throw new InvalidOperationException("Generated floor has no safe enemy spawn: "+seed+" / "+room);
            return candidates[rng.Next(candidates.Count)];
        }
        void ReconcileFloorCompletion()
        {
            // Called at construction/restore boundaries, never as an exception-hiding frame repair.
            bool alive=false;foreach(var foe in Enemies)if(foe!=null&&!foe.dead){alive=true;break;}
            cleared=!alive;
        }
        void ResetGameplayOverlays()
        {
            InventoryOpen=false;manufacturerRegistry=false;Time.timeScale=1;
        }
        void SpawnFoe(Vector3 position,int kind)=>SpawnFoeInternal(position,kind,false);
        void SpawnFoeInternal(Vector3 position,int kind,bool forceElite)
        {
            position=SeparateEnemySpawn(position,kind);
            if(IsOverworld)position.y=.71f; // Legacy enemy root subtracts .62m; sole plane is root minus .09m.
            var f=art.Character(position,false,kind).gameObject.AddComponent<Foe>();
            f.Init(this,kind,room);
            bool raiderBoss=kind==3&&(finalDebtBossFight||ExpeditionMap.FactionForStage(SelectedStageId)!=DungeonFaction.None);
            f.SetRaiderBoss(raiderBoss);
            var faction=IsOverworld?DungeonFaction.None:ExpeditionMap.FactionForStage(SelectedStageId);
            bool factionHuman=kind==10&&faction!=DungeonFaction.None;
            // The human model supplies its own weapon; all other actors retain the legacy visual path.
            if(kind==5||kind==10&&!factionHuman){f.weapon=art.Weapon(f.transform,new Color(.72f,.48f,.24f)).GetComponent<AimedWeapon>();VaultArt.ConfigureWeapon(f.weapon.transform,GunEffect.Normal,new Color(.72f,.48f,.24f),4);}
            f.GetComponent<FaithfulEnemy3D>().Init(f);
            if(factionHuman)
            {
                foreach(var renderer in f.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                var human=HumanFactionVisual.Create(f.transform,faction==DungeonFaction.Raiders?1:2,f.UseAssaultRifle?0:1);
                // FaithfulEnemy3D's actor origin is 0.03 m above the dungeon floor.
                human.transform.localPosition=Vector3.down*.03f;
            }
            else ImportedEnemyVisual.TryAttach(f.gameObject,raiderBoss?11:kind);
            Enemies.Add(f);
            if(IsOverworld)ConfigureOverworldFoe(f);
            else
            {
                int stageLevel=SelectedExpeditionStage?.level??1;
                float levelHealth=1+(stageLevel-1)*.14f,levelDamage=1+(stageLevel-1)*.065f;
                f.ConfigureDifficulty(CurrentDungeon.healthScale*1.5f*FloorCombatMultiplier*levelHealth*Mathf.Max(.01f,VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.enemyHpMultiplier : 1f),CurrentDungeon.damageScale*FloorCombatMultiplier*levelDamage*Mathf.Max(.01f,VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.enemyDamageMultiplier : 1f),DungeonIndex*.10f);
            }
            if(finalDebtBossFight&&kind==3)f.ConfigureDifficulty(3f,1.7f,.25f);
            bool elite=!restoringChampionCheckpoint&&kind!=3&&(forceElite||(IsOverworld?spawningWorldEnemy!=null&&spawningWorldEnemy.elite:rng.NextDouble()<.0275));
            if(elite){f.elite=true;f.ConfigureDifficulty(3.2f,1.35f,.12f);f.transform.localScale*=1.15f;}
            f.timer=2.5f;
        }
        Vector3 SeparateEnemySpawn(Vector3 origin,int kind)
        {
            float radius=kind==3?1.45f:.78f;
            for(int ring=0;ring<=12;ring++)for(int step=0;step<(ring==0?1:16);step++)
            {
                float angle=step*Mathf.PI/8f;
                Vector3 candidate=origin+(ring==0?Vector3.zero:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*1.15f);
                if(Blocked(candidate,.8f)||!IsOverworld&&!Layout.SpawnSafe(candidate))continue;
                bool free=true;
                foreach(var other in Enemies)
                {
                    if(other==null||other.dead)continue;
                    Vector3 delta=candidate-other.transform.position;delta.y=0;
                    float separation=radius+other.SeparationRadius+.2f;
                    if(delta.sqrMagnitude<separation*separation){free=false;break;}
                }
                if(free)return candidate;
            }
            return origin;
        }
        public bool Blocked(Vector3 p,float radius)
        {
            return Layout==null||Layout.Blocked(p,radius)||(IsOverworld&&WorldEntranceBuildings.Blocked(p,radius,WorldGeography));
        }
        public Vector3 ChaseDirection(Vector3 from)=>Layout.Chase(from,PlayerPosition);
        public bool LineOfSight(Vector3 from,Vector3 to)
        {
            if(!Layout.ClearLine(from,to))return false;
            if(IsOverworld){int steps=Mathf.CeilToInt(Vector3.Distance(from,to));for(int i=1;i<=steps;i++)if(WorldEntranceBuildings.Blocked(Vector3.Lerp(from,to,(float)i/steps),.15f,WorldGeography))return false;}
            return true;
        }
        public Vector3 MoveWithinRoom(Vector3 p,Vector3 delta,float radius)
        {
            // Substeps keep a fast dash from tunnelling across a narrow wall.
            int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.3f));Vector3 step=delta/steps;
            for(int i=0;i<steps;i++)p=MoveStep(p,step,radius);return p;
        }
        Vector3 MoveStep(Vector3 p,Vector3 delta,float radius)
        {
            var next=p+delta; if(!Blocked(next,radius))return next;
            next=p+new Vector3(delta.x,0,0); if(!Blocked(next,radius))p=next;
            next=p+new Vector3(0,0,delta.z); if(!Blocked(next,radius))p=next;
            return p;
        }
        float deathReturnAt;
        int deathScrapLost, deathScrapRemaining;
        void Update()
        {
            if(screen==Screen.Dead)
            {
                if(Time.unscaledTime>=deathReturnAt)
                {
                    EnterHub();
                    Notify("SCRAP LOST: "+deathScrapLost.ToString("N0")+" / REMAINING: "+deathScrapRemaining.ToString("N0"),
                           "失ったスクラップ: "+deathScrapLost.ToString("N0")+" / 残り: "+deathScrapRemaining.ToString("N0"));
                }
                return;
            }
            if(worldTransitionPending)return;
            if(WorldBossConversationVisible||DebtFinalConversationVisible||!smoke&&screen==Screen.Hub&&FirstStepsStage==1)return;
            if(VaultStorageVisible){if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape))VaultStorageClose();return;}
            Cursor.visible=VaultMobileInputBridge.Enabled||!Active;
            if(IsOverworld&&OverworldActivityChoiceOpen)
            {
                if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape))OpenNearbyOverworldActivity();
                return;
            }
            if(VaultMobileInputBridge.Pressed(MobileAction.Inventory,KeyCode.I))ToggleInventory();
            if(InventoryOpen){if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape))ToggleInventory();return;}
            if(MobileTouchControls.Enabled&&manufacturerRegistry)return;
            if(screen==Screen.Hub){UpdateHub();return;}
            if(screen==Screen.DungeonSelect&&VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape)){CloseOverworldMap();return;}
            if(screen==Screen.Shop&&VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape)){screen=Screen.Hub;return;}
            if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape))
            {
                if(screen==Screen.Playing) { screen=Screen.Pause; Time.timeScale=0; }
                else if(screen==Screen.Pause) { screen=Screen.Playing; Time.timeScale=1; }
            }
            player?.GetComponent<LayeredPlayer2D>()?.SetAiming(Active&&VaultMobileInputBridge.Firing&&reload<=0);
            if(worldTransitionPending||!Active)return;
            TickArtifacts(Time.deltaTime);TickActionSkill(Time.deltaTime);
            if(Input.GetKeyDown(KeyCode.V)||MobileTouchControls.Consume(MobileAction.Skill))TryUseActionSkill();
            float wheel=VaultMobileInputBridge.Enabled?0:Input.mouseScrollDelta.y;if(Mathf.Abs(wheel)>.01f)CycleWeapon(wheel>0?-1:1);
            if(VaultMobileInputBridge.Pressed(MobileAction.SwitchWeapon,KeyCode.Q))CycleWeapon(-1);
            if(Time.time-lastHurt>3&&HealthRegen>0)hp=Mathf.Min(maxHp,hp+HealthRegen*Time.deltaTime);
            runTime+=Time.deltaTime; toastTimer-=Time.deltaTime; fireTimer-=Time.deltaTime; invincible-=Time.deltaTime; dodgeCooldown-=Time.deltaTime;debtChainTimer-=Time.deltaTime;if(debtChainTimer<=0)debtChainCount=0;
            Layout.Discover(PlayerPosition,IsOverworld?1.5f:5f);navigationTimer-=Time.deltaTime;if(navigationTimer<=0){if(!IsOverworld||Layout.ToCell(PlayerPosition)!=lastNavigationCell){Layout.BuildDistanceField(PlayerPosition);lastNavigationCell=Layout.ToCell(PlayerPosition); }navigationTimer=IsOverworld?.75f:.25f;}
            if(VaultMobileInputBridge.Enabled)
            {
                var mobileDirection=VaultMobileInputBridge.ToWorldMovement(VaultMobileInputBridge.Aim);
                if(mobileDirection.sqrMagnitude>.0001f)mobileInputAim=mobileDirection.normalized;
                aim=mobileInputAim;aimUsesCursor=false;hasAimedPoint=false;
            }
            else
            {
                var ray=cam.ScreenPointToRay(Input.mousePosition);
                aimedRay=ray;aimUsesCursor=true;
                // Recompute after the painted mount moves so the visible muzzle aims at the cursor.
                for(int pass=0;pass<2;pass++)
                {
                    var plane=new Plane(Vector3.up,new Vector3(0,PlayerWeapon.MuzzlePosition.y,0));
                    if(plane.Raycast(ray,out float distance)){aimedPoint=ray.GetPoint(distance);hasAimedPoint=true;var d=aimedPoint-PlayerWeapon.MuzzlePosition;d.y=0;if(d.sqrMagnitude>.01f)aim=d.normalized;}
                    player.rotation=Quaternion.LookRotation(aim);PlayerWeapon.Aim(aim);
                }
            }
            // Track eligible targets continuously, including between shots and while reloading.
            if(PlayerWeapon!=null)
                aim=AutoAimDirection(PlayerWeapon.MuzzlePosition,aim,EffectiveAccuracy);
            player.rotation=Quaternion.LookRotation(aim);
            PlayerWeapon.Aim(aim);
            for(int i=0;i<satellites.Count;i++) { float a=runTime*4+i*Mathf.PI*2/3; satellites[i].position=player.position+new Vector3(Mathf.Sin(a),.35f,Mathf.Cos(a))*1.65f; }
            Vector3 movement=ReadPlayerMovement();
            if(VaultMobileInputBridge.Pressed(MobileAction.Dodge,KeyCode.Space)&&dodgeCooldown<=0) { dodgeTimer=.19f; dodgeCooldown=EffectiveDodgeCooldown/(1+runDash); invincible=.3f; dodgeDirection=movement.sqrMagnitude>0?movement.normalized:aim; Burst(player.position,Cyan,8);ArtifactOnDash();RunDashPulse(); }
            if(dodgeTimer>0) { dodgeTimer-=Time.deltaTime; movement=dodgeDirection*3.3f; }
            player.position=MoveWithinRoom(player.position,movement*EffectiveMoveSpeed*Time.deltaTime,.45f);
            TickFieldEconomy();
            if(NearbyAmmoCrate)OpenNearbyAmmoCrate(true);
            if(reload>0) { reload-=Time.deltaTime; if(reload<=0)CompleteAmmoReload(); }
            else if(VaultMobileInputBridge.Pressed(MobileAction.Reload,KeyCode.R)||ammo<=0)TryStartReload();
            else if(VaultMobileInputBridge.Firing&&fireTimer<=0)Fire();
            if(VaultMobileInputBridge.MeleePressed)AttackMelee();
            if(VaultMobileInputBridge.Pressed(MobileAction.Pickup,KeyCode.F)&&!OpenNearbyChest())PickupNearest();
            if(IsOverworld)
            {
                TickOverworld();
                if(screen!=Screen.Playing)return;
                if(VaultMobileInputBridge.Pressed(MobileAction.Interact,KeyCode.E)&&HandleOverworldInteraction())return;
            }
            else if(ExpeditionExitOpen&&VaultMobileInputBridge.Pressed(MobileAction.Interact,KeyCode.E)&&Vector3.Distance(player.position,ExitPosition)<3)Advance();
            shake=Mathf.Max(0,shake-Time.deltaTime*4);
            UpdateCamera(false);
        }
        Vector2Int lastNavigationCell=new Vector2Int(int.MinValue,int.MinValue);
        void UpdateCamera(bool instant)
        {
            Vector3 target=screen==Screen.Menu?new Vector3(0,.6f,0):new Vector3(Mathf.Clamp(player.position.x,-halfWidth+5,halfWidth-5),.6f,Mathf.Clamp(player.position.z+1.5f,-halfDepth+4,halfDepth-4));
            cameraFocus=instant?target:Vector3.Lerp(cameraFocus,target,1-Mathf.Exp(-Time.deltaTime*5));
            cam.transform.position=cameraFocus-cam.transform.forward*(IsOverworld?85:35)+UnityEngine.Random.insideUnitSphere*shake*.1f;
        }
        public void Fire()
        {
            if(reload>0||ammo<=0)return;
            // Use the same displayed direction for the shot; recalculating from the
            // cursor here would discard continuous tracking just before firing.
            PlayerWeapon.Aim(aim);PlayerWeapon.Kick();
            LegendaryTrigger("fire",PlayerPosition);
            ammo--;fireTimer=1/EffectiveFireRate;PlayWeaponSound(gun,"fire",.5f);shake=gun.effect==GunEffect.Scatter?.38f:.25f;
            CombatVisuals.Muzzle(PlayerWeapon.MuzzlePosition,aim,gun.color,gun.effect,world,VaultLanguage.DisplayModel(gun));
            for(int i=0;i<gun.pellets;i++)
            {
                float halfAngle=Mathf.Max(0,(100-EffectiveAccuracy)*.25f);
                float spread=gun.pellets==1?UnityEngine.Random.Range(-halfAngle,halfAngle):Mathf.Lerp(-halfAngle,halfAngle,i/(gun.pellets-1f));
                Vector3 direction=Quaternion.Euler(0,spread,0)*aim;
                Vector3 origin=PlayerWeapon.MuzzlePosition;
                if(gun.effect==GunEffect.Orbit)
                {
                    origin=satellites.Count>0?satellites[ammo%satellites.Count].position:PlayerWeapon.MuzzlePosition;
                    Vector3 target=player.position+aim*12;target.y=origin.y;direction=Quaternion.Euler(0,spread,0)*(target-origin).normalized;
                    Burst(origin,Gold,3);
                }
                var shot=SpawnBullet(origin,direction,true,UnityEngine.Random.Range(EffectiveDamageMin,EffectiveDamageMax),gun.speed,gun.effect==GunEffect.Ricochet?Cyan:gun.color,gun.effect,EffectiveAccuracy);
                shot.actionCharge=ActionSkillChargeMax*.01f*7.7f/Mathf.Max(.1f,EffectiveFireRate)/Mathf.Max(1,gun.pellets);
                shot.critical=UnityEngine.Random.value<EffectiveCriticalChance;shot.moduleAmplified=ArtifactDamageMultiplier>1.0001f||ArtifactAffixBonus("damage")>0;
                if(shot.critical)shot.damage*=EffectiveCriticalMultiplier;
                LegendaryModifyShot(shot);
            }
        }
        public bool AttackMelee()=>meleeAttack!=null&&meleeAttack.TryAttack(EquippedMeleeWeapon,aim,PlayerPosition,world);
        public void ResetMeleeCooldown()=>meleeAttack?.ResetCooldown();
        public float MeleeDamage(MeleeWeaponItem item)
        {
            if(item==null)return 0;
            return item.damage*(1+damageBonus+ArmorTotal(a=>a.damageBonus)+ArtifactAffixBonus("damage"))
                *(1+Mathf.Min(.75f,debtChainCount*DebtKillChainBonus))*ArtifactDamageMultiplier
                *(1+DebtBossDamageBonus*(room==CurrentDungeon.AreaCount-1?1:0));
        }
        public Shot SpawnBullet(Vector3 p,Vector3 direction,bool friendly,float damage,float speed,Color color,GunEffect effect,float accuracy=100)
        {
            var obj=new GameObject("Projectile");obj.transform.SetParent(world,false);obj.transform.position=p;
            
            var shot=obj.AddComponent<Shot>(); shot.game=this; shot.velocity=direction*speed; shot.friendly=friendly; shot.damage=damage; shot.effect=effect;shot.hitAccuracy=accuracy; CombatVisuals.ConfigureProjectile(shot,color); return shot;
        }
        public Foe NearestEnemy(Vector3 p,Foe except=null)
        { Foe best=null; float distance=float.MaxValue; foreach(var f in Enemies) if(f!=null&&!f.dead&&f!=except) { float d=(f.transform.position-p).sqrMagnitude; if(d<distance){distance=d;best=f;} } return best; }
        public void GravityWell(Vector3 p,float damage,Color? numberColor=null)
        {
            p.y=.3f;var obj=new GameObject("Gravity well");obj.transform.SetParent(world,false);obj.transform.position=p;
            var w=obj.AddComponent<Well>(); w.game=this; w.damage=damage;w.numberColor=numberColor??Color.white;CombatVisuals.ConfigureWell(w,new Color(.65f,.3f,1));
        }
        public void Burst(Vector3 p,Color color,int count)
        {
            CombatVisuals.Burst(p,color,count,world);
        }
        public void Hurt(float damage)
        {
            if(!Active||invincible>0)return;hp-=AbsorbArtifactDamage(damage*(1-DamageReduction));LegendaryTrigger("hurt",PlayerPosition);lastHurt=Time.time; invincible=.45f; shake=1; audioSource.PlayOneShot(hitSound); Burst(player.position,Color.red,8);
            if(hp<=0) { hp=0; Finish(false); }
        }
        public void FoeKilled(Foe f)
        {
            if(IsOverworld)OnOverworldEnemyKilled(f);
            kills++;ArtifactOnKill(f.transform.position);LegendaryTrigger("kill",f.transform.position);debtChainCount++;debtChainTimer=5;if(DebtHealOnKill>0)hp=Mathf.Min(maxHp,hp+DebtHealOnKill);if(runHarvest>0)hp=Mathf.Min(maxHp,hp+runHarvest);Burst(f.transform.position,Gold,9);
            if(!IsOverworld)ExpeditionEnemyKilled(f);
            bool boss=f.kind==3,finalBoss=boss&&room==CurrentDungeon.AreaCount-1;
            if(finalDebtBossFight&&boss){CompleteDebtBossFight();return;}
            if(!IsOverworld&&!smoke&&finalBoss&&DungeonIndex==0&&PlayerPrefs.GetInt("Vault.FirstBossLegendaryD0",0)==0)
            {AddDrop(Arsenal.Legendary(0,rng,DungeonCatalog.ItemLevel(rng,DungeonIndex)),f.transform.position+Vector3.right*.7f);PlayerPrefs.SetInt("Vault.FirstBossLegendaryD0",1);PlayerPrefs.Save();}
            if(boss||f.elite)DropChampionReward(f,boss);
            if(TryRollScrapDrop(boss,out int scrapAmount))AddScrapDrop(f.transform.position+Vector3.left*.65f,scrapAmount);
            var ammoType=ActiveAmmoTypeAt(rng.Next(ActiveAmmoTypeCount));if(TryRollAmmoDrop(boss,ammoType,out int ammoAmount))AddAmmoDrop(f.transform.position+Vector3.back*.65f,ammoType,ammoAmount);
            float gunDropChance=boss?(VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.weaponDropChanceBoss : 1f/6f)
                :(VaultBalanceProfile.Current != null ? VaultBalanceProfile.Current.weaponDropChanceNormal : .04f);
            gunDropChance=Mathf.Clamp01(gunDropChance);
            if(rng.NextDouble()<gunDropChance)AddDrop(RollExpeditionGun(rng,boss),f.transform.position);
            if(rng.NextDouble()<Mathf.Clamp01(gunDropChance*2f))AddHealthPackDrop(f.transform.position+new Vector3(.55f,0,.55f));
            float armorChance=VaultBalanceProfile.Current != null ? (boss?VaultBalanceProfile.Current.armorDropChanceBoss:VaultBalanceProfile.Current.armorDropChance) : (boss?1f/6f:.0435f);
            float moduleChance=VaultBalanceProfile.Current != null ? (boss?VaultBalanceProfile.Current.moduleDropChanceBoss:VaultBalanceProfile.Current.moduleDropChance) : (boss?.0915f:.02f);
            float meleeChance=VaultBalanceProfile.Current != null ? (boss?VaultBalanceProfile.Current.meleeDropChanceBoss:VaultBalanceProfile.Current.meleeDropChance) : (boss?.125f:.03f);
            if(rng.NextDouble()<Mathf.Clamp01(armorChance))AddArmorDrop(RollExpeditionArmor(rng,boss),f.transform.position+Vector3.right*.8f);
            if(rng.NextDouble()<Mathf.Clamp01(moduleChance))AddArtifactDrop(RollExpeditionArtifact(rng,boss),f.transform.position+Vector3.left*.8f);
            if(rng.NextDouble()<Mathf.Clamp01(meleeChance))AddMeleeDrop(RollExpeditionMelee(rng,boss),f.transform.position+Vector3.forward*.8f);
            if(IsOverworld)return;
            bool alive=false; foreach(var e in Enemies) if(e!=null&&!e.dead)alive=true;
            if(!alive)
            {
                cleared=true; hp=Mathf.Min(maxHp,hp+8);
                if(room==CurrentDungeon.AreaCount-1) {Notify("ALPHA MUTANT DOWN / Loot [F] / Extraction [E]","変異体の主を撃破 / 戦利品 [F] / 脱出 [E]");return;}
                if(rng.Next(3)==0)AddDrop(RollExpeditionGun(rng),player.position+Vector3.forward*.5f);
                if(rng.Next(3)==0)AddArmorDrop(RollExpeditionArmor(rng),player.position+Vector3.left*.8f);
                Notify("ZONE CLEAR / +8 HP / Loot [F] / Exit [E]","区域制圧 / HP +8 / 戦利品 [F] / 出口 [E]");
            }
        }
        void AddDrop(Gun weapon,Vector3 p)
        {
            GearBalance.Migrate(weapon);
            p.y=.3f;
            var obj=CreateLootPresentation(weapon.name,p,new Vector3(.65f,.28f,1.05f),weapon.color,LootPresentationKind.Weapon,rarity:weapon.rarity);
            ApplyProductDropModel(obj,weapon);drops.Add(new Drop {gun=weapon,position=p,visual=obj});
        }
        void AddMeleeDrop(MeleeWeaponItem item,Vector3 p)
        {
            if(item==null)return;p.y=.32f;
            var obj=CreateLootPresentation(VaultLanguage.RarityName(item.rarity)+" "+MeleeWeaponCatalog.Name(item),p,new Vector3(.62f,.24f,.72f),item.color,LootPresentationKind.Melee,rarity:item.rarity);
            meleeDrops.Add(new MeleeDrop{item=item,position=p,visual=obj});
        }
        MeleeDrop NearMeleeDrop()
        {MeleeDrop best=null;float distance=2.6f;foreach(var drop in meleeDrops){float d=Vector3.Distance(PlayerPosition,drop.position);if(d<distance){distance=d;best=drop;}}return best;}
        public MeleeWeaponItem NearbyMeleeWeapon=>NearMeleeDrop()?.item;
        public bool NearbySupplyChest
        {get{foreach(var chest in chests)if(chest!=null&&!chest.opened&&Vector3.Distance(PlayerPosition,chest.position)<2.4f)return true;return false;}}
        public bool NearbyAmmoCrate
        {get{foreach(var crate in fieldAmmoCrates)if(crate!=null&&crate.visual!=null&&Vector3.Distance(PlayerPosition,crate.position)<2.5f)return true;return false;}}
        public AmmoType NearbyAmmoCrateType
        {get{FieldAmmoCrate nearest=null;float best=2.5f;foreach(var crate in fieldAmmoCrates)if(crate!=null&&crate.visual!=null){float d=Vector3.Distance(PlayerPosition,crate.position);if(d<best){best=d;nearest=crate;}}return nearest==null?AmmoType.AssaultRifle:nearest.type;}}
        Drop NearDrop()
        {Drop best=null;float distance=2.6f;foreach(var drop in drops){float d=Vector3.Distance(player.position,drop.position);if(d<distance){distance=d;best=drop;}}var armor=NearArmor();if(armor!=null&&Vector3.Distance(PlayerPosition,armor.position)<distance)return null;return best;}
        public void Equip(Gun weapon)
        {if(StoreWeapon(weapon))SelectWeapon(Weapons.IndexOf(weapon));else{Weapons[SelectedWeapon]=weapon;gun=null;SelectWeapon(SelectedWeapon);}}
        void CreateSatellites()
        {
            foreach(var s in satellites)if(s!=null)Destroy(s.gameObject); satellites.Clear();
            if(gun!=null&&gun.effect==GunEffect.Orbit)for(int i=0;i<3;i++)
            {
                var drone=new GameObject("Isotope escort drone").transform;drone.SetParent(world,false);drone.position=player.position+Vector3.right*(i+1);
                art.Part("Drone steel hull",PrimitiveType.Capsule,Vector3.zero,new Vector3(.29f,.15f,.35f),new Color(.33f,.36f,.27f),drone);
                art.Part("Drone reactor indicator",PrimitiveType.Sphere,new Vector3(0,0,.19f),Vector3.one*.065f,Cyan,drone,true);
                for(int side=-1;side<=1;side+=2){art.Part("Drone stabilizer",PrimitiveType.Cube,new Vector3(side*.20f,0,0),new Vector3(.22f,.045f,.12f),new Color(.35f,.25f,.14f),drone);art.Part("Drone rotor",PrimitiveType.Cylinder,new Vector3(side*.28f,.025f,0),new Vector3(.15f,.015f,.15f),new Color(.16f,.18f,.14f),drone);}
                satellites.Add(drone);
            }
        }
        public void AimAt(Vector3 point)
        {
            aimedPoint=point;hasAimedPoint=true;aimUsesCursor=false;
            for(int pass=0;pass<2;pass++){var direction=point-PlayerWeapon.MuzzlePosition;direction.y=0;if(direction.sqrMagnitude>.0001f)aim=direction.normalized;player.rotation=Quaternion.LookRotation(aim);PlayerWeapon.Aim(aim);}
        }
        public int LootCount => drops.Count;
        void PickupNearest()
        {
            var drop=NearDrop();var armor=NearArmor();var melee=NearMeleeDrop();var module=NearArtifact();
            float weaponDistance=drop==null?float.MaxValue:Vector3.Distance(PlayerPosition,drop.position);
            float armorDistance=armor==null?float.MaxValue:Vector3.Distance(PlayerPosition,armor.position);
            float meleeDistance=melee==null?float.MaxValue:Vector3.Distance(PlayerPosition,melee.position);
            float moduleDistance=module==null?float.MaxValue:Vector3.Distance(PlayerPosition,module.position);
            if(module!=null&&moduleDistance<weaponDistance&&moduleDistance<armorDistance&&moduleDistance<meleeDistance)
            {if(!StoreArtifact(module.item)){Notify("Artifact storage full. Discard unused artifacts in [I].","拡張モジュール枠が満杯です。[I]で整理してください。");return;}artifactDrops.Remove(module);Destroy(module.visual);audioSource.PlayOneShot(pickupSound);Notify(ArtifactCatalog.DisplayName(module.item,false)+" collected.",ArtifactCatalog.DisplayName(module.item,true)+"を入手");return;}
            if(melee!=null&&meleeDistance<weaponDistance&&meleeDistance<armorDistance)
            {if(!StoreMeleeWeapon(melee.item)){Notify("Melee storage full / discard items in [I]","近接武器の所持枠が満杯 / [I]で整理してください");return;}meleeDrops.Remove(melee);Destroy(melee.visual);string rarity=VaultLanguage.RarityName(melee.item.rarity);Notify(rarity+" MELEE: "+MeleeWeaponCatalog.English(melee.item.family)+" / SCORE "+MeleeWeaponCatalog.Score(melee.item),rarity+" 近接武器: "+MeleeWeaponCatalog.Japanese(melee.item.family)+" / 評価 "+MeleeWeaponCatalog.Score(melee.item));audioSource.PlayOneShot(pickupSound);return;}
            if(armor!=null&&armorDistance<weaponDistance)
            {if(!StoreArmor(armor.item)){Notify("Armor storage full / discard items in [I]","防具所持枠が満杯 / [I]で整理してください");return;}string en=Armory.Name(armor.item,false);string ja=Armory.Name(armor.item,true);armorDrops.Remove(armor);Destroy(armor.visual);audioSource.PlayOneShot(pickupSound);Notify("ARMOR: "+en+" / Equip in [I]","防具: "+ja+" / [I]で装備");return;}
            if(drop==null)return;
            if(!StoreWeapon(drop.gun)){Notify("10 weapon slots full / discard a weapon in [I]","銃10枠が満杯 / [I]で所持品を整理してください");return;}drops.Remove(drop);Destroy(drop.visual);
            audioSource.PlayOneShot(pickupSound);Notify("WEAPON: "+VaultLanguage.GunName(drop.gun,false)+" / Assign in [I]","銃: "+VaultLanguage.GunName(drop.gun,true)+" / [I]で装備");
        }
        bool ExpeditionExitOpen
        {
            get
            {
                if(SelectedExpeditionStage==null)return cleared;
                // The last floor must remain escapable after its boss is defeated,
                // even if an optional objective was missed on an earlier floor.
                if(room>=CurrentDungeon.AreaCount-1)return cleared;
                return cleared||(ExpeditionAllowsBypass&&(CurrentMissionFloorVisited||MissionCompleted));
            }
        }
        public void Advance()
        {
            if(IsOverworld||screen!=Screen.Playing||!ExpeditionExitOpen)return;
            if(room>=CurrentDungeon.AreaCount-1||(ExpeditionAllowsBypass&&MissionCompleted)){Finish(true);return;}
            choices=new[]{0,1,2,3,4,5,6,7,8,9,10,11,12};
            for(int i=choices.Length-1;i>0;i--) { int j=rng.Next(i+1); int a=choices[i];choices[i]=choices[j];choices[j]=a; }
            screen=Screen.Reward;
        }
        public void ChooseReward(int index)
        {
            if(screen!=Screen.Reward||index<0||index>=choices.Length)return;
            switch(choices[index]) { case 0:damageBonus+=.25f;runDamage+=.25f;break;case 1:baseMaxHp+=30;runVitality+=30;RecalculateArmor();hp=Mathf.Min(maxHp,hp+35);break;case 2:fireBonus+=.22f;runFire+=.22f;break;case 3:hp=maxHp;break;case 4:runDash+=.35f;break;case 5:runChain+=.30f;break;case 6:runHarvest+=3;break;
            case 7:damageBonus+=.12f;runDamage+=.12f;fireBonus+=.10f;runFire+=.10f;break;
            case 8:baseMaxHp+=20;runVitality+=20;RecalculateArmor();hp=Mathf.Min(maxHp,hp+45);break;
            case 9:runDash+=.20f;runHarvest+=1;break;
            case 10:runChain+=.20f;fireBonus+=.10f;runFire+=.10f;break;
            case 11:damageBonus+=.15f;runDamage+=.15f;runDash+=.15f;break;
            case 12:hp=Mathf.Min(maxHp,hp+maxHp*.5f);foreach(var type in new[]{AmmoType.Handgun,AmmoType.Shotgun,AmmoType.AssaultRifle,AmmoType.SniperRifle,AmmoType.LightMachineGun})AddAmmo(type,AmmoPackSize(type));break; }
            room++; ResetGameplayOverlays();StopReloadSound();reload=0;reloadPending=0;dodgeTimer=0;fireTimer=0; invincible=2.5f; dodgeCooldown=0; screen=Screen.Playing; BuildRoom(true);Notify(room==CurrentDungeon.AreaCount-1?"COLOSSUS / Final encounter":"ZONE "+(room+1).ToString("00"),room==CurrentDungeon.AreaCount-1?"巨骸 / 最終決戦":"区域 "+(room+1).ToString("00"));
        }
        void Finish(bool victory)
        {
            screen=victory?Screen.Victory:Screen.Dead;
            ClearRunCheckpoint();shake=0;
            SaveInventory();
            if(!IsOverworld)OnDungeonFinished(victory);
            if(!victory)
            {
                int before=Math.Max(0,Credits);
                ApplyOverworldDeathPenalty();
                RestoreDeathAmmoMinimum();
                deathScrapRemaining=Credits;deathScrapLost=before-Credits;
                RememberOverworldPosition(WorldGeography.Camps[LastVisitedCamp],false);
                Time.timeScale=1;deathReturnAt=Time.unscaledTime+DeathSequenceDuration;
                return;
            }
            if(!smoke||Array.IndexOf(Environment.GetCommandLineArgs(),"--overworld-review")>=0)ReturnFromWorldDungeon(true);
        }
        void Notify(string message,string japanese=null)
        {
            toast=message;toastJapanese=japanese??message;
            if(MobileTouchControls.Enabled)
            {
                toast=toast.Replace("[E]", "USE").Replace("[F]", "PICK UP").Replace("[I]", "INVENTORY");
                toastJapanese=toastJapanese.Replace("[E]", "調べる").Replace("[F]", "拾う").Replace("[I]", "装備画面");
            }
            toastTimer=4;
        }
        void OnDestroy() { foreach(var m in materials)if(m!=null)Destroy(m); if(pixel!=null)Destroy(pixel);if(overworldAtlasTexture!=null)Destroy(overworldAtlasTexture); }

    }
}
