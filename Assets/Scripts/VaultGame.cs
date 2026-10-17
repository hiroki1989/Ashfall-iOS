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
        public bool Active => screen==Screen.Playing&&!InventoryOpen;
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
        Vector3 dodgeDirection, aim=Vector3.right;
        Vector3 aimedPoint;Ray aimedRay;bool hasAimedPoint,aimUsesCursor;
        string toast="",toastJapanese="";
        string Toast=>VaultLanguage.T(toast,toastJapanese);
        bool cleared, smoke;
        int[] choices=new int[3];
        public System.Collections.Generic.IReadOnlyList<int> RewardChoices=>choices;
        Font font;
        AudioSource audioSource;
        VaultMelee meleeAttack;
        AudioClip fireSound, scatterSound, coilSound, hitSound, pickupSound;
        Texture2D pixel;
        GUIStyle label, title, small, button;
        static readonly Color Cyan=new Color(.25f,.78f,.82f), Gold=new Color(.98f,.66f,.30f), Ink=new Color(.025f,.038f,.052f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() { if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hero3d-review")>=0)return; if(FindAnyObjectByType<VaultGame>()==null) new GameObject("ASHFALL / runtime").AddComponent<VaultGame>(); }
        void Awake()
        {
            Application.targetFrameRate=MobileTouchControls.Enabled?30:120;
            if(MobileTouchControls.Enabled)QualitySettings.vSyncCount=0;
            cores=PlayerPrefs.GetInt("Vault.Cores",0); rank=PlayerPrefs.GetInt("Vault.Rank",0);
            font=Resources.Load<Font>("Fonts/NotoSansJP");
            if(font==null)font=Font.CreateDynamicFontFromOSFont(Application.platform==RuntimePlatform.IPhonePlayer?new[]{"HiraginoSans-W3","Hiragino Sans","Arial"}:new[]{"Yu Gothic UI","Meiryo","Segoe UI","Arial"},20);
            pixel=new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
            cam=new GameObject("Isometric camera").AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=13.5f; cam.backgroundColor=Ink; cam.clearFlags=CameraClearFlags.SolidColor;
            cam.tag="MainCamera";
            cam.transform.rotation=Quaternion.Euler(35.264f,45,0);cam.allowHDR=true;cam.depthTextureMode=DepthTextureMode.DepthNormals;cam.gameObject.AddComponent<VaultPost>();
            QualitySettings.antiAliasing=4;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=80;QualitySettings.pixelLightCount=4;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.32f,.37f,.43f);RenderSettings.ambientEquatorColor=new Color(.25f,.27f,.23f);RenderSettings.ambientGroundColor=new Color(.13f,.11f,.085f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.11f,.12f,.095f);RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.009f;
            var light=new GameObject("Ash-filtered sunlight / shadow casting").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.95f,.84f);light.intensity=1.2f;light.shadows=LightShadows.Soft;light.shadowStrength=.78f;light.transform.rotation=Quaternion.Euler(48,-25,0);
            var fill=new GameObject("Cool sky fill").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.57f,.72f,.89f);fill.intensity=.38f;fill.transform.rotation=Quaternion.Euler(28,145,0);
            menuArt=Resources.Load<Texture2D>("Art/WastelandKeyArt");
            audioSource=gameObject.AddComponent<AudioSource>();fireSound=GunReport(false,false);scatterSound=GunReport(true,false);coilSound=GunReport(false,true);hitSound=GunReport(true,true);pickupSound=Tone(620,.16f);
            meleeAttack=gameObject.AddComponent<VaultMelee>();meleeAttack.Initialize(this);
            smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--mobile-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--manufacturer-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--world-visual-review")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--hero3d-playcheck")>=0;
            // Debt skill unlocks determine whether slots 3/4 survive loadout repair.
            ProgressionInitialize();LoadInventory();screen=Screen.Menu;seed=Environment.TickCount&int.MaxValue;rng=new System.Random(seed);BuildRoom(false);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke")>=0) gameObject.AddComponent<SmokeRun>();
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
        public void StartRun(int? fixedSeed=null)
        {
            ClearRunCheckpoint();shake=0;
            InventoryOpen=false;Time.timeScale=1; seed=fixedSeed??(Environment.TickCount&int.MaxValue); rng=new System.Random(seed);
            route.Clear(); var cell=Vector2Int.zero; route.Add(cell);
            for(int i=1;i<CurrentDungeon.AreaCount;i++)
            {
                var available=new List<Vector2Int>();
                foreach(var dir in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left}) if(!route.Contains(cell+dir)) available.Add(cell+dir);
                if(available.Count==0) { route.Clear(); cell=Vector2Int.zero; route.Add(cell); i=0; continue; }
                cell=available[rng.Next(available.Count)];
                route.Add(cell);
            }
            room=0;kills=0;ricochetEvents=0;splitEvents=0;runTime=0;baseMaxHp=100;RecalculateArmor();hp=maxHp;damageBonus=0;fireBonus=0;
            chestRoomForRun=rng.NextDouble()<.5?rng.Next(CurrentDungeon.AreaCount):-1;
            PrepareAmmoCratesForRun();
            ResetArtifactCombat();runDamage=0;runFire=0;runVitality=0;runDash=0;runChain=0;runHarvest=0;debtChainCount=0;debtChainTimer=0;
            gun=WeaponInSlot(ActiveWeaponSlot);if(gun==null)gun=Weapons[SelectedWeapon];ammo=magazineAmmo.TryGetValue(gun.id,out int loaded)?Mathf.Min(EffectiveMagazine,loaded):EffectiveMagazine;reload=0;reloadPending=0;dodgeCooldown=0;invincible=2.5f;dodgeTimer=0;fireTimer=0;meleeAttack?.ResetCooldown();lastHurt=-10;screen=Screen.Playing;
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
        public void BuildRoom(bool populate)
        {
            if(world!=null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            foreach(var m in materials) Destroy(m); materials.Clear(); Enemies.Clear(); obstacles.Clear(); drops.Clear();foreach(var d in meleeDrops)if(d.visual!=null)Destroy(d.visual);meleeDrops.Clear();armorDrops.Clear();chests.Clear();supplyDrops.Clear();
            world=new GameObject("Generated sector "+room).transform; satellites.Clear();
            ClearArtifactDrops();Layout=new SectorLayout(new System.Random(seed+room*7919),DungeonIndex);
            halfWidth=Layout.Columns*SectorLayout.Cell/2;halfDepth=Layout.Rows*SectorLayout.Cell/2;cleared=false;
            art=new VaultArt(world,materials,new System.Random(seed+room*7919));art.Floor(halfWidth,halfDepth,room);
            art.DungeonAtmosphere(DungeonIndex,Layout);
            foreach(var rect in Layout.walls)art.Partition(rect);
            foreach(var p in Layout.covers)art.Cover(p);
            for(int i=0;i<Layout.chambers.Count;i++)art.Chamber(Layout.Center(Layout.chambers[i]),i);
            art.Gate(Layout.Exit);
            player=art.Character(Layout.Entry,true);
            barrel=art.Weapon(player,gun==null?Cyan:gun.color);if(gun!=null)VaultArt.ConfigureWeapon(barrel,gun.effect,gun.color,VaultLanguage.DisplayModel(gun));RefreshWeaponAppearance();
            PlayerAppearance?.ApplyArmor(EquippedArmor);
            aim=Vector3.right;hasAimedPoint=false;PlayerWeapon.Aim(aim);navigationTimer=0;
            CreateSatellites();
            if(populate)
            {
                if(room==CurrentDungeon.AreaCount-1) SpawnFoe(Layout.Exit,3);
                else for(int i=0;i<CurrentDungeon.EnemyCount(room);i++)
                {
                    Vector3 p; int attempts=0;
                    do {var cell=Layout.chambers[rng.Next(Layout.chambers.Count)];p=Layout.Center(cell)+new Vector3((float)rng.NextDouble()*6-3,0,(float)rng.NextDouble()*4-2);attempts++;} while((Blocked(p,.8f)||!Layout.SpawnSafe(p))&&attempts<200);
                    if(!Blocked(p,.8f)&&Layout.SpawnSafe(p)) SpawnFoe(p,CurrentDungeon.enemyRoster[rng.Next(CurrentDungeon.enemyRoster.Length)]);
                }
                SpawnRoomChest();
                SpawnAmmoCratesForCurrentRoom();
            }
            cam.orthographicSize=7.5f;UpdateCamera(true);
        }
        void SpawnFoe(Vector3 position,int kind)
        {
            var f=art.Character(position,false,kind).gameObject.AddComponent<Foe>();
            f.Init(this,kind,room);
            // Infected attack with their bodies; only the armed revenant carries a firearm.
            if(kind==5){f.weapon=art.Weapon(f.transform,new Color(.72f,.48f,.24f)).GetComponent<AimedWeapon>();VaultArt.ConfigureWeapon(f.weapon.transform,GunEffect.Normal,new Color(.72f,.48f,.24f),4);}
            f.GetComponent<FaithfulEnemy3D>().Init(f);
            Enemies.Add(f);
            f.ConfigureDifficulty(CurrentDungeon.healthScale,CurrentDungeon.damageScale,DungeonIndex*.10f);
            f.timer=2.5f;
        }
        public bool Blocked(Vector3 p,float radius)
        {
            return Layout==null||Layout.Blocked(p,radius);
        }
        public Vector3 ChaseDirection(Vector3 from)=>Layout.Chase(from,PlayerPosition);
        public bool LineOfSight(Vector3 from,Vector3 to)=>Layout.ClearLine(from,to);
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
        void Update()
        {
            Cursor.visible=VaultMobileInputBridge.Enabled||!Active;
            if(VaultMobileInputBridge.Pressed(MobileAction.Inventory,KeyCode.I))ToggleInventory();
            if(InventoryOpen){if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape))ToggleInventory();return;}
            if(MobileTouchControls.Enabled&&manufacturerRegistry)return;
            if(screen==Screen.Hub){UpdateHub();return;}
            if((screen==Screen.Shop||screen==Screen.DungeonSelect)&&VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape)){screen=Screen.Hub;return;}
            if(VaultMobileInputBridge.Pressed(MobileAction.Pause,KeyCode.Escape))
            {
                if(screen==Screen.Playing) { screen=Screen.Pause; Time.timeScale=0; }
                else if(screen==Screen.Pause) { screen=Screen.Playing; Time.timeScale=1; }
            }
            player?.GetComponent<LayeredPlayer2D>()?.SetAiming(Active&&VaultMobileInputBridge.Firing&&reload<=0);
            if(!Active)return;
            TickArtifacts(Time.deltaTime);
            float wheel=VaultMobileInputBridge.Enabled?0:Input.mouseScrollDelta.y;if(Mathf.Abs(wheel)>.01f)CycleWeapon(wheel>0?-1:1);
            if(VaultMobileInputBridge.Pressed(MobileAction.SwitchWeapon,KeyCode.Q))CycleWeapon(-1);
            if(Time.time-lastHurt>3&&HealthRegen>0)hp=Mathf.Min(maxHp,hp+HealthRegen*Time.deltaTime);
            runTime+=Time.deltaTime; toastTimer-=Time.deltaTime; fireTimer-=Time.deltaTime; invincible-=Time.deltaTime; dodgeCooldown-=Time.deltaTime;debtChainTimer-=Time.deltaTime;if(debtChainTimer<=0)debtChainCount=0;
            Layout.Discover(PlayerPosition);navigationTimer-=Time.deltaTime;if(navigationTimer<=0){Layout.BuildDistanceField(PlayerPosition);navigationTimer=.25f;}
            if(VaultMobileInputBridge.Enabled)
            {
                ApplyMobileAim(VaultMobileInputBridge.Aim);
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
            player.rotation=Quaternion.LookRotation(aim);
            PlayerWeapon.Aim(aim);
            for(int i=0;i<satellites.Count;i++) { float a=runTime*4+i*Mathf.PI*2/3; satellites[i].position=player.position+new Vector3(Mathf.Sin(a),.35f,Mathf.Cos(a))*1.65f; }
            Vector3 movement=ReadPlayerMovement();
            if(VaultMobileInputBridge.Pressed(MobileAction.Dodge,KeyCode.Space)&&dodgeCooldown<=0) { dodgeTimer=.19f; dodgeCooldown=EffectiveDodgeCooldown/(1+runDash); invincible=.3f; dodgeDirection=movement.sqrMagnitude>0?movement.normalized:aim; Burst(player.position,Cyan,8);ArtifactOnDash();RunDashPulse(); }
            if(dodgeTimer>0) { dodgeTimer-=Time.deltaTime; movement=dodgeDirection*3.3f; }
            player.position=MoveWithinRoom(player.position,movement*EffectiveMoveSpeed*Time.deltaTime,.45f);
            TickFieldEconomy();
            if(reload>0) { reload-=Time.deltaTime; if(reload<=0)CompleteAmmoReload(); }
            else if(VaultMobileInputBridge.Pressed(MobileAction.Reload,KeyCode.R)||ammo<=0)TryStartReload();
            else if(VaultMobileInputBridge.Firing&&fireTimer<=0)Fire();
            if(VaultMobileInputBridge.MeleePressed)AttackMelee();
            if(VaultMobileInputBridge.Pressed(MobileAction.Pickup,KeyCode.F)&&!OpenNearbyAmmoCrate()&&!OpenNearbyChest())PickupNearest();
            if(cleared&&VaultMobileInputBridge.Pressed(MobileAction.Interact,KeyCode.E)&&Vector3.Distance(player.position,ExitPosition)<3)Advance();
            shake=Mathf.Max(0,shake-Time.deltaTime*4);
            UpdateCamera(false);
        }
        void UpdateCamera(bool instant)
        {
            Vector3 target=screen==Screen.Menu?new Vector3(0,.6f,0):new Vector3(Mathf.Clamp(player.position.x,-halfWidth+5,halfWidth-5),.6f,Mathf.Clamp(player.position.z+1.5f,-halfDepth+4,halfDepth-4));
            cameraFocus=instant?target:Vector3.Lerp(cameraFocus,target,1-Mathf.Exp(-Time.deltaTime*5));
            cam.transform.position=cameraFocus-cam.transform.forward*35+UnityEngine.Random.insideUnitSphere*shake*.1f;
        }
        public void Fire()
        {
            if(reload>0||ammo<=0)return;
            PlayerWeapon.Aim(aim);PlayerWeapon.Kick();
            if(hasAimedPoint)for(int pass=0;pass<2;pass++)
            {
                if(aimUsesCursor){var plane=new Plane(Vector3.up,new Vector3(0,PlayerWeapon.MuzzlePosition.y,0));if(plane.Raycast(aimedRay,out float distance))aimedPoint=aimedRay.GetPoint(distance);}
                var direction=aimedPoint-PlayerWeapon.MuzzlePosition;direction.y=0;if(direction.sqrMagnitude>.0001f)aim=direction.normalized;
                player.rotation=Quaternion.LookRotation(aim);PlayerWeapon.Aim(aim);
            }
            ammo--;fireTimer=1/EffectiveFireRate;audioSource.PlayOneShot(gun.effect==GunEffect.Scatter?scatterSound:VaultLanguage.DisplayModel(gun)>=7?coilSound:fireSound,.5f);shake=gun.effect==GunEffect.Scatter?.38f:.25f;
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
                SpawnBullet(origin,direction,true,EffectiveDamage,gun.speed,gun.effect==GunEffect.Ricochet?Cyan:gun.color,gun.effect,EffectiveAccuracy);
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
        public void GravityWell(Vector3 p,float damage)
        {
            p.y=.3f;var obj=new GameObject("Gravity well");obj.transform.SetParent(world,false);obj.transform.position=p;
            var w=obj.AddComponent<Well>(); w.game=this; w.damage=damage;CombatVisuals.ConfigureWell(w,new Color(.65f,.3f,1));
        }
        public void Burst(Vector3 p,Color color,int count)
        {
            CombatVisuals.Burst(p,color,count,world);
        }
        public void Hurt(float damage)
        {
            if(!Active||invincible>0)return;hp-=AbsorbArtifactDamage(damage*(1-DamageReduction));lastHurt=Time.time; invincible=.45f; shake=1; audioSource.PlayOneShot(hitSound); Burst(player.position,Color.red,8);
            if(hp<=0) { hp=0; Finish(false); }
        }
        public void FoeKilled(Foe f)
        {
            kills++;ArtifactOnKill(f.transform.position);debtChainCount++;debtChainTimer=5;if(DebtHealOnKill>0)hp=Mathf.Min(maxHp,hp+DebtHealOnKill);if(runHarvest>0)hp=Mathf.Min(maxHp,hp+runHarvest);Burst(f.transform.position,Gold,9);
            ExpeditionEnemyKilled(f);
            bool boss=f.kind==3,finalBoss=boss&&room==CurrentDungeon.AreaCount-1;
            if(!smoke&&finalBoss&&DungeonIndex==0&&PlayerPrefs.GetInt("Vault.FirstBossLegendaryD0",0)==0)
            {AddDrop(Arsenal.Legendary(0,rng,DungeonCatalog.ItemLevel(rng,DungeonIndex)),f.transform.position+Vector3.right*.7f);PlayerPrefs.SetInt("Vault.FirstBossLegendaryD0",1);PlayerPrefs.Save();}
            if(TryRollScrapDrop(boss,out int scrapAmount))AddScrapDrop(f.transform.position+Vector3.left*.65f,scrapAmount);
            var ammoType=(AmmoType)rng.Next(4);if(TryRollAmmoDrop(boss,ammoType,out int ammoAmount))AddAmmoDrop(f.transform.position+Vector3.back*.65f,ammoType,ammoAmount);
            if(rng.NextDouble()<(f.kind==3?1:.24))AddDrop(Arsenal.RollForDungeon(rng,DungeonIndex,f.kind==3),f.transform.position);
            if(rng.NextDouble()<(f.kind==3?1:.26))AddArmorDrop(Armory.RollForDungeon(rng,DungeonIndex,f.kind==3),f.transform.position+Vector3.right*.8f);
            if(rng.NextDouble()<(f.kind==3?.55:.12))AddArtifactDrop(ArtifactCatalog.RollForDungeon(rng,DungeonIndex,f.kind==3),f.transform.position+Vector3.left*.8f);
            if(rng.NextDouble()<(f.kind==3?.75:.18))AddMeleeDrop(MeleeWeaponCatalog.RollForDungeon(rng,DungeonIndex,f.kind==3),f.transform.position+Vector3.forward*.8f);
            bool alive=false; foreach(var e in Enemies) if(e!=null&&!e.dead)alive=true;
            if(!alive)
            {
                cleared=true; hp=Mathf.Min(maxHp,hp+8);
                if(room==CurrentDungeon.AreaCount-1) {Notify("ALPHA MUTANT DOWN / Loot [F] / Extraction [E]","変異体の主を撃破 / 戦利品 [F] / 脱出 [E]");return;}
                AddDrop(Arsenal.RollForDungeon(rng,DungeonIndex),player.position+Vector3.forward*.5f);
                AddArmorDrop(Armory.RollForDungeon(rng,DungeonIndex),player.position+Vector3.left*.8f);
                Notify("ZONE CLEAR / +8 HP / Loot [F] / Exit [E]","区域制圧 / HP +8 / 戦利品 [F] / 出口 [E]");
            }
        }
        void AddDrop(Gun weapon,Vector3 p)
        {
            p.y=.3f;
            var obj=CreateLootPresentation(weapon.name,p,new Vector3(.65f,.28f,1.05f),weapon.color,LootPresentationKind.Weapon);
            drops.Add(new Drop {gun=weapon,position=p,visual=obj});
        }
        void AddMeleeDrop(MeleeWeaponItem item,Vector3 p)
        {
            if(item==null)return;p.y=.32f;
            var obj=CreateLootPresentation(VaultLanguage.RarityName(item.rarity)+" "+MeleeWeaponCatalog.Name(item),p,new Vector3(.62f,.24f,.72f),item.color,LootPresentationKind.Melee);
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
            {if(!StoreArtifact(module.item)){Notify("Artifact storage full. Discard unused artifacts in [I].","拡張モジュール枠が満杯です。[I]で整理してください。");return;}artifactDrops.Remove(module);Destroy(module.visual);Notify(ArtifactCatalog.DisplayName(module.item,false)+" collected. Equip in one of two slots [I].",ArtifactCatalog.DisplayName(module.item,true)+"を入手。2枠のいずれかに装備できます [I]");return;}
            if(melee!=null&&meleeDistance<weaponDistance&&meleeDistance<armorDistance)
            {if(!StoreMeleeWeapon(melee.item)){Notify("Melee storage full / discard items in [I]","近接武器の所持枠が満杯 / [I]で整理してください");return;}meleeDrops.Remove(melee);Destroy(melee.visual);string rarity=VaultLanguage.RarityName(melee.item.rarity);Notify(rarity+" MELEE: "+MeleeWeaponCatalog.English(melee.item.family)+" / SCORE "+MeleeWeaponCatalog.Score(melee.item),rarity+" 近接武器: "+MeleeWeaponCatalog.Japanese(melee.item.family)+" / 評価 "+MeleeWeaponCatalog.Score(melee.item));audioSource.PlayOneShot(pickupSound);return;}
            if(armor!=null&&armorDistance<weaponDistance)
            {if(!StoreArmor(armor.item)){Notify("Armor storage full / discard items in [I]","防具所持枠が満杯 / [I]で整理してください");return;}string en=Armory.Name(armor.item,false);string ja=Armory.Name(armor.item,true);armorDrops.Remove(armor);Destroy(armor.visual);Notify("ARMOR: "+en+" / Equip in [I]","防具: "+ja+" / [I]で装備");return;}
            if(drop==null)return;
            if(!StoreWeapon(drop.gun)){Notify("10 weapon slots full / discard a weapon in [I]","銃10枠が満杯 / [I]で所持品を整理してください");return;}drops.Remove(drop);Destroy(drop.visual);
            audioSource.PlayOneShot(pickupSound);Notify("WEAPON: "+VaultLanguage.GunName(drop.gun,false)+" / Assign in [I]","銃: "+VaultLanguage.GunName(drop.gun,true)+" / [I]で装備");
        }
        public void Advance()
        {
            if(!cleared)return;
            if(room>=CurrentDungeon.AreaCount-1){Finish(true);return;}
            choices=new[]{0,1,2,3,4,5,6};
            for(int i=choices.Length-1;i>0;i--) { int j=rng.Next(i+1); int a=choices[i];choices[i]=choices[j];choices[j]=a; }
            screen=Screen.Reward;
        }
        public void ChooseReward(int index)
        {
            switch(choices[index]) { case 0:damageBonus+=.25f;runDamage+=.25f;break;case 1:baseMaxHp+=30;runVitality+=30;RecalculateArmor();hp=Mathf.Min(maxHp,hp+35);break;case 2:fireBonus+=.22f;runFire+=.22f;break;case 3:hp=maxHp;break;case 4:runDash+=.35f;break;case 5:runChain+=.30f;break;case 6:runHarvest+=3;break; }
            room++; reload=0;reloadPending=0; invincible=2.5f; dodgeCooldown=0; screen=Screen.Playing; BuildRoom(true);Notify(room==CurrentDungeon.AreaCount-1?"COLOSSUS / Final encounter":"ZONE "+(room+1).ToString("00"),room==CurrentDungeon.AreaCount-1?"巨骸 / 最終決戦":"区域 "+(room+1).ToString("00"));
        }
        void Finish(bool victory)
        {
            screen=victory?Screen.Victory:Screen.Dead;
            ClearRunCheckpoint();shake=0;
            SaveInventory();
            OnDungeonFinished(victory);
        }
        void Notify(string message,string japanese=null) { toast=message;toastJapanese=japanese??message;toastTimer=4; }
        void OnDestroy() { foreach(var m in materials)if(m!=null)Destroy(m); if(pixel!=null)Destroy(pixel); }

    }
}
