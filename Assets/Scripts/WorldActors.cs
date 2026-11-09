using UnityEngine;

namespace NeonVault
{
    public class Foe : MonoBehaviour
    {
        public const float HealthGrowthPerLevel=.28f;
        public const float DamageGrowthPerLevel=.27f;
        public static float LevelHealthScale(int level)=>1f+HealthGrowthPerLevel*GearBalance.LevelGrowthSteps(level);
        public static float LevelDamageScale(int level)=>1f+DamageGrowthPerLevel*GearBalance.LevelGrowthSteps(level);
        public VaultGame game;
        public bool elite;
        public Color HealthBarColor=>elite?new Color(.72f,.28f,1f):kind==3?new Color(1f,.58f,.12f):new Color(.92f,.12f,.16f);
        public float hp, maxHp, speed, timer, contact;
        public int kind;
        public bool dead;
        public bool Escaping { get; private set; }
        float escapeTimer, escapeSmokeTimer;
        Vector3 escapeDirection;
        public AimedWeapon weapon;
        public bool IsTelegraphing=>charging;
        public float TelegraphProgress=>charging?Mathf.Clamp01(1-charge/TelegraphSeconds):0;
        public bool IsLunging=>dash>0;
        // Stable visual contract: 0 rush, 1 overhead slam, 2 sweeping strike.
        public int BossAttackStyle=>kind==3?(int)bossMove:-1;
        public int AttackVariant=>attackVariant;
        public bool IsSpinning=>spinTimer>0;
        public bool RaiderBoss { get; private set; }
        public bool IsHumanBoss => RaiderBoss || (kind==3 && GetComponentInChildren<HumanFactionVisual>()!=null);
        public bool MajorBoss { get; set; }
        public void SetRaiderBoss(bool value){RaiderBoss=kind==3&&value;}
        public bool HeavyMelee=>kind==1||kind==2||kind==6||kind==7||kind==9;
        public bool IsMelee=>kind==0||kind==4||kind==8||HeavyMelee;
        public float HealingRadius=>10f;
        public bool UseAssaultRifle { get; private set; }
        public float SeparationRadius=>kind==3?1.45f:elite?1.0f:.78f;
        public static float TargetDodgeChance(float accuracy)=>Mathf.Clamp((82-Mathf.Clamp(accuracy,1,100))*.006f,0,.30f);
        public float TelegraphSeconds=>kind==3?(bossMove==BossMove.FlashLunge?.38f:bossMove==BossMove.ComboFrenzy?.50f:bossMove==BossMove.Rush?.60f:bossMove==BossMove.Whirlwind?.68f:bossMove==BossMove.Sweep?.82f:bossMove==BossMove.Gun?1.05f:bossMove==BossMove.Grenade?1.15f:bossMove==BossMove.ShockwaveRing?1.05f:1.25f):attackVariant==1?(kind==4?.38f:kind==0?.44f:kind==5||kind==2?.52f:kind==1||kind==8?.55f:kind==6?.58f:kind==9?.62f:.68f):attackVariant==2?(kind==8?.92f:kind==4||kind==6||kind==9?.95f:kind==0?1.05f:kind==1||kind==7?1.10f:1.15f):(kind==5||kind==10)?.95f:kind==7?1.2f:kind==4?.65f:kind==8?.42f:kind==0?.7f:.85f;
        public static string Name(int kind)=>kind==10?VaultLanguage.T("RAIDER","レイダー"):VaultLanguage.T(new[]{"SHAMBLER","ACID SPITTER","RAD GUNNER","WASTELAND COLOSSUS","POUNCER","NEEDLE STALKER","RAD CYCLER","BLOATER","PLATED CORPSE","FLESH WEAVER"}[Mathf.Clamp(kind,0,9)],new[]{"シャンブラー","アシッドスピッター","ラッドガンナー","ウェイストランド・コロッサス","パウンサー","ニードルストーカー","ラッドサイクラー","ブロウター","プレートコープス","フレッシュウィーバー"}[Mathf.Clamp(kind,0,9)]);
        enum BossMove { Rush, Slam, Sweep, Gun, FlashLunge, ComboFrenzy, ShockwaveRing, Whirlwind, Grenade }
        bool alerted, charging;float charge,dash,damageScale=1,resistance,lockedRadialAngle,burstTimer;int burstShots;Vector3 lockedAim,lockedTarget;GameObject warning;
        BossMove bossMove;int bossAttackCount,attackVariant,enemyAttackCount,comboSteps,humanStrafeSign=1,humanShotsSinceCover,humanTactic;float dashSpeed,spinTimer,spinTick,comboTimer,sprintBoostTimer,humanTacticTimer,humanReloadTimer,humanRollTimer;bool dashLandingBlast,hasCoverTarget;Vector3 coverTarget;
        public bool IsHumanReloading=>humanReloadTimer>0;
        public bool IsHumanRolling=>humanRollTimer>0;
        public bool IsHumanCrouching=>humanReloadTimer>0&&hasCoverTarget&&(transform.position-coverTarget).sqrMagnitude<1.2f;
        public void Init(VaultGame g,int k,int depth)
        {
            game=g;kind=k;maxHp=hp=k==3?650:k==10?95:(k==1||k==2||k==6||k==7||k==9)?230:45+k*9;speed=k==3?3.25f:k==4?3.8f:k==0?2.7f:k==8?5.4f:k==1||k==5?2.3f:3.1f;timer=.75f+k*.09f;
            // Choose once per instance without consuming the combat random sequence.
            int x=Mathf.RoundToInt(transform.position.x*10),z=Mathf.RoundToInt(transform.position.z*10);
            UseAssaultRifle=k==10&&unchecked((x*73856093^z*19349663)&1)==0;
            humanStrafeSign=((x^z)&1)==0?1:-1;humanTactic=Mathf.Abs(x*31+z*17)%4;humanTacticTimer=.8f+(Mathf.Abs(x+z)%5)*.35f;
            resistance=Mathf.Min(.3f,depth*.025f);damageScale=1;
        }
        public void ConfigureDifficulty(float health,float damage,float resist){maxHp*=health;hp=maxHp;damageScale*=damage;resistance=Mathf.Clamp(resist,0,.45f);}
        public void DelayNextAttack(float seconds)
        {
            if(dead)return;
            if(charging){charging=false;charge=0;ClearWarning();}
            timer=Mathf.Max(timer,seconds);
        }
        void ClearWarning(){if(warning!=null)Destroy(warning);warning=null;}
        float visibilityNext;bool cachedVisible;
        void Update()
        {
            if(game==null)return;
            if(Escaping)
            {
                if(!game.Active)return;
                escapeTimer+=Time.deltaTime;escapeSmokeTimer-=Time.deltaTime;
                if(escapeDirection.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(escapeDirection);
                transform.position+=escapeDirection*14f*Time.deltaTime;
                if(escapeSmokeTimer<=0){escapeSmokeTimer=.18f;CombatVisuals.Burst(transform.position+Vector3.up*.8f,new Color(.75f,.78f,.82f),8,transform.parent);}
                return;
            }
            if(dead)return;
            if(!game.Active){ClearWarning();charging=false;dash=0;burstShots=0;spinTimer=0;comboSteps=0;timer=Mathf.Max(timer,.8f);return;}
            if(spinTimer>0)
            {
                spinTimer-=Time.deltaTime;spinTick-=Time.deltaTime;
                transform.Rotate(0,720f*Time.deltaTime,0);
                Vector3 toP=game.PlayerPosition-transform.position;toP.y=0;
                if(toP.sqrMagnitude>.05f)transform.position=game.MoveWithinRoom(transform.position,toP.normalized*speed*1.85f*Time.deltaTime,.6f);
                if(spinTick<=0)
                {
                    spinTick=.16f;
                    CombatVisuals.MeleeSlash(transform.position,transform.forward,3.8f,350f,MeleeWeaponFamily.Cleaver,new Color(1f,.45f,.15f),transform.parent);
                    if(toP.magnitude<3.9f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(19f*damageScale);
                }
                return;
            }
            if(comboSteps>0)
            {
                comboTimer-=Time.deltaTime;
                if(comboTimer<=0)
                {
                    comboSteps--;
                    Vector3 toP=game.PlayerPosition-transform.position;toP.y=0;
                    if(toP.sqrMagnitude>.01f){lockedAim=toP.normalized;transform.rotation=Quaternion.LookRotation(lockedAim);}
                    float stepDist=kind==3?(comboSteps==0?2.4f:1.8f):1.25f;
                    transform.position=game.MoveWithinRoom(transform.position,lockedAim*stepDist,.6f);
                    GetComponent<EnemyPainted2D>()?.NotifyAttack();GetComponent<FaithfulEnemy3D>()?.NotifyAttack();GetComponentInChildren<ImportedEnemyVisual>()?.NotifyShot();
            if(!alerted&&HeavyMelee){CombatVisuals.Burst(transform.position+lockedAim*1.1f,new Color(.55f,.32f,.18f),12,transform.parent);var d0=game.PlayerPosition-transform.position;d0.y=0;if(d0.magnitude<2.9f&&Vector3.Dot(d0.normalized,lockedAim)>-.15f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(36*damageScale);return;}GetComponentInChildren<ImportedEnemyVisual>()?.NotifyShot();
                    float reach=kind==3?(comboSteps==0?3.6f:3.0f):2.1f;
                    CombatVisuals.MeleeSlash(transform.position,lockedAim,reach,comboSteps==0?160f:115f,MeleeWeaponFamily.Machete,kind==3?new Color(1f,.52f,.18f):new Color(.85f,.35f,.16f),transform.parent);
                    CombatVisuals.Burst(transform.position+lockedAim*(reach*.55f),new Color(.95f,.42f,.15f),comboSteps==0?16:10,transform.parent);
                    Vector3 postDelta=game.PlayerPosition-transform.position;postDelta.y=0;
                    if(postDelta.magnitude<reach&&Vector3.Dot(postDelta.normalized,lockedAim)>.15f&&game.LineOfSight(transform.position,game.PlayerPosition))
                        game.Hurt((kind==3?(comboSteps==0?32f:21f):16f)*damageScale);
                    comboTimer=comboSteps==1?(kind==3?.44f:.36f):.24f;
                }
                return;
            }
            if(burstShots>0){burstTimer-=Time.deltaTime;if(burstTimer<=0){FireRangedShot();burstShots--;burstTimer=MajorBoss?.10f:IsHumanBoss?.18f:.16f;}if(kind==10&&lockedAim.sqrMagnitude>.01f){transform.rotation=Quaternion.LookRotation(lockedAim);weapon?.Aim(lockedAim);}}
            Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;float distance=delta.magnitude;Vector3 direction=delta.normalized;
            if(!alerted&&distance>22)return;
            if(Time.time>=visibilityNext){visibilityNext=Time.time+.14f+(kind&7)*.008f;cachedVisible=distance<35&&game.LineOfSight(transform.position,game.PlayerPosition);}
            bool visible=cachedVisible;
            if(distance<17&&visible)alerted=true;if(!alerted)return;
            if(!charging&&dash<=0&&burstShots==0){if(distance>.01f)transform.rotation=Quaternion.LookRotation(direction);if(weapon!=null)weapon.Aim(direction);}
            if(dash>0){dash-=Time.deltaTime;float dSpd=dashSpeed>0?dashSpeed:(kind==3?18f:14f);transform.position=game.MoveWithinRoom(transform.position,lockedAim*dSpd*Time.deltaTime,.6f);if(Vector3.Distance(game.PlayerPosition,transform.position)<(kind==3?1.6f:1.3f)&&contact<=0){game.Hurt((kind==3?29:kind==8?25:22)*damageScale);contact=1;dash=0;}if(dash<=0&&dashLandingBlast){dashLandingBlast=false;CombatVisuals.Burst(transform.position,new Color(.95f,.42f,.15f),14,transform.parent);Vector3 dp=game.PlayerPosition-transform.position;dp.y=0;if(dp.magnitude<(kind==3?3.2f:kind==7?1.9f:2.3f)&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt((kind==3?26f:20f)*damageScale);}}
            else if(!charging)
            {
                Vector3 movement=direction;
                bool mobile=HeavyMelee||((kind!=1&&kind!=5&&kind!=10&&kind!=9)||distance>10||!visible);
                if(HeavyMelee&&distance<2.1f)mobile=false;
                if((kind==0||kind==8)&&distance<1.65f)mobile=false;
                if(kind==3&&distance<2.4f)mobile=false;
                if(IsHumanBoss&&burstShots>0)mobile=false;
                if(kind==7&&distance<2.2f)mobile=false;
                if((kind==5||kind==10)&&visible&&distance<5){movement=-direction;mobile=true;}
                if(kind==10)
                {
                    if(humanRollTimer>0)humanRollTimer-=Time.deltaTime;
                    if(humanReloadTimer>0)
                    {
                        humanReloadTimer-=Time.deltaTime;
                        if(hasCoverTarget)
                        {
                            Vector3 toCover=coverTarget-transform.position;toCover.y=0;
                            if(toCover.sqrMagnitude>.18f){movement=toCover.normalized;mobile=true;}
                            else mobile=false;
                        }
                        else if(visible)
                        {
                            Vector3 perp=Vector3.Cross(Vector3.up,direction)*humanStrafeSign;
                            movement=(-direction*.75f+perp*.65f).normalized;mobile=true;
                        }
                    }
                    else
                    {
                        humanTacticTimer-=Time.deltaTime;
                        if(humanTacticTimer<=0)
                        {
                            humanTactic=(humanTactic+1+Mathf.Abs(Mathf.RoundToInt(transform.position.x*7+transform.position.z*13))%3)%5;
                            if((humanTactic&1)==0)humanStrafeSign=-humanStrafeSign;
                            humanTacticTimer=humanTactic==4?.38f:1.8f+(humanTactic*.40f);
                            if(humanTactic==4&&visible&&distance<13f)humanRollTimer=.38f;
                        }
                        if(visible&&distance>=4.2f&&distance<=14.5f)
                        {
                            Vector3 perp=Vector3.Cross(Vector3.up,direction)*humanStrafeSign;
                            if(game.Blocked(transform.position+perp*1.1f,.55f)){humanStrafeSign=-humanStrafeSign;perp=-perp;}
                            if(humanTactic==0||humanTactic==3){mobile=false;}
                            else if(humanTactic==4){movement=(perp+(distance>9.5f?direction*.35f:distance<6f?-direction*.35f:Vector3.zero)).normalized;mobile=true;}
                            else if(humanTactic==1){movement=(perp*.8f+direction*(distance>7.2f?.6f:-.25f)).normalized;mobile=true;}
                            else if(humanTactic==2){movement=(perp*.75f-direction*.65f).normalized;mobile=true;}
                            else {mobile=distance>10.5f;movement=direction;}
                        }
                    }
                }
                if(kind==6&&!HeavyMelee&&visible&&distance<13){movement=(Vector3.Cross(Vector3.up,direction)+direction*(distance>8?.35f:-.35f)).normalized;mobile=true;}
                {
                    movement=mobile?(visible?movement:game.ChaseDirection(transform.position)):Vector3.zero;
                    Vector3 away=Vector3.zero;
                    foreach(var other in game.Enemies)
                    {
                        if(other==null||other==this||other.dead)continue;
                        Vector3 offset=transform.position-other.transform.position;offset.y=0;
                        float spacing=SeparationRadius+other.SeparationRadius;
                        float sqr=offset.sqrMagnitude;
                        if(sqr>=spacing*spacing)continue;
                        if(sqr<.0001f)
                        {
                            int sign=game.Enemies.IndexOf(this)<game.Enemies.IndexOf(other)?1:-1;
                            offset=new Vector3(sign,0,sign*.37f);sqr=offset.sqrMagnitude;
                        }
                        away+=offset/Mathf.Sqrt(sqr)*(1-Mathf.Sqrt(sqr)/spacing);
                    }
                    if(away.sqrMagnitude>0)movement=(movement+away*2.2f).normalized;
                    if(sprintBoostTimer>0)sprintBoostTimer-=Time.deltaTime;
                    else if(kind!=10&&distance>3.8f&&distance<11f&&timer<.65f&&((enemyAttackCount+kind)&1)==1)sprintBoostTimer=.45f;
                    float moveSpeed=speed*(kind==10?(humanRollTimer>0?1.45f:humanReloadTimer>0&&hasCoverTarget?1.1f:humanTactic==1?.8f:.75f):sprintBoostTimer>0?(kind==3?1.55f:1.45f):kind==3&&MajorBoss?1.32f:1f);
                    if((mobile||away.sqrMagnitude>0)&&!((IsHumanBoss||kind==10)&&burstShots>0))transform.position=game.MoveWithinRoom(transform.position,movement*moveSpeed*Time.deltaTime,.6f);
                }
            }
            contact-=Time.deltaTime; // Damage comes from readable attacks, never passive body contact.
            if(charging)
            {
                if(kind==10&&visible&&charge>.18f){Vector3 muzzle=HumanMuzzlePosition();Vector3 shotDirection=game.PlayerPosition-muzzle;shotDirection.y=0;if(shotDirection.sqrMagnitude>.0001f){lockedAim=shotDirection.normalized;transform.rotation=Quaternion.LookRotation(lockedAim);weapon?.Aim(lockedAim);}if(warning!=null&&!(elite&&enemyAttackCount%4==0)){muzzle=HumanMuzzlePosition();warning.transform.position=new Vector3(muzzle.x,.08f,muzzle.z);var line=warning.GetComponent<LineRenderer>();if(line!=null&&line.positionCount>=2){float length=line.GetPosition(1).magnitude;line.SetPosition(1,lockedAim*length);}}}
                charge-=Time.deltaTime;if(charge>0)return;charging=false;ClearWarning();Attack();float baseWait=kind==3?(bossMove==BossMove.FlashLunge?.95f:bossMove==BossMove.ComboFrenzy?1.85f:bossMove==BossMove.Whirlwind?1.9f:bossMove==BossMove.ShockwaveRing?2.2f:bossMove==BossMove.Gun?2.3f:bossMove==BossMove.Grenade?2.4f:bossMove==BossMove.Slam?1.95f:bossMove==BossMove.Sweep?1.35f:1.55f):kind==10?(UseAssaultRifle?3.1f:2.35f):kind==5?2.2f:kind==7?2.8f:kind==0||kind==8?1.05f:kind==4?1.75f:kind==9?2.8f:1.85f;if(kind!=3&&kind!=10)baseWait*=attackVariant==1?.68f:attackVariant==2?1.22f:1f;if(kind==3&&hp<maxHp*.6f)baseWait*=.75f;if(MajorBoss)baseWait*=.58f;timer=baseWait;return;
            }
            timer-=Time.deltaTime;
            if(timer>0||!visible||distance>18)return;
            if((kind==0||kind==8)&&distance>(enemyAttackCount>0&&enemyAttackCount%3==1?4.2f:2.25f))return;
            if(kind==7&&distance>6.5f||kind==4&&distance>10.5f||(kind==1||kind==2||kind==5||kind==6||kind==9)&&distance>15f)return;
            if(kind!=3&&kind!=10){attackVariant=enemyAttackCount==0?0:SelectEnemyVariant(distance);enemyAttackCount++;}
            else if(kind==10){if(humanReloadTimer>0)return;enemyAttackCount++;}
            if(kind==3)
            {
                // A distant boss closes with a committed rush. At close range it alternates
                // between a slow, punishing slam and a faster frontal sweep with a safe rear arc.
                bossMove=SelectBossMove(distance);
                bossAttackCount++;
            }
            lockedTarget=game.PlayerPosition;lockedTarget.y=.08f;lockedAim=direction;charging=true;charge=kind==3&&MajorBoss?TelegraphSeconds*.78f:TelegraphSeconds;
            if(kind==3)
            {
                warning=new GameObject("Colossus attack warning");warning.transform.SetParent(transform.parent,false);
                if(bossMove==BossMove.Gun)
                {
                    if(IsHumanBoss){for(int i=-1;i<=1;i++)CombatVisuals.WarningLine(HumanMuzzlePosition(),Quaternion.Euler(0,i*11,0)*lockedAim,Mathf.Min(distance+2,18),warning.transform);}else{for(int i=-2;i<=2;i++)CombatVisuals.WarningParabolicArc(transform.position+Vector3.up*1.5f,transform.position+Quaternion.Euler(0,i*16,0)*lockedAim*Mathf.Clamp(distance+i*.4f,3.5f,14f),1.1f,warning.transform);}
                }
                else if(bossMove==BossMove.Grenade)
                {
                    CombatVisuals.WarningParabolicArc(HumanMuzzlePosition(),lockedTarget,MajorBoss?3.3f:2.8f,warning.transform,new Color(1f,.32f,.12f));
                    if(MajorBoss)
                    {
                        for(int i=-1;i<=1;i+=2)
                        {
                            Vector3 sideTarget=lockedTarget+Quaternion.Euler(0,i*38f,0)*(lockedTarget-transform.position).normalized*2.6f;
                            CombatVisuals.WarningParabolicArc(HumanMuzzlePosition(),sideTarget,2.5f,warning.transform,new Color(1f,.45f,.15f));
                        }
                    }
                }
                else if(bossMove==BossMove.Rush)
                {
                    CombatVisuals.WarningLine(transform.position,lockedAim,Mathf.Min(distance+1.5f,9),warning.transform);
                }
                else if(bossMove==BossMove.Slam)
                {
                    lockedTarget=transform.position;lockedTarget.y=.08f;
                    CombatVisuals.WarningCircle(lockedTarget,3.3f,warning.transform);
                }
                else
                {
                    if(bossMove==BossMove.Sweep)CombatVisuals.WarningArc(transform.position,lockedAim,3.6f,120f,warning.transform);
                    else if(bossMove==BossMove.FlashLunge){CombatVisuals.WarningLine(transform.position,lockedAim,Mathf.Min(distance+2.2f,10.5f),warning.transform);CombatVisuals.WarningCircle(transform.position+lockedAim*Mathf.Min(distance,8f),2.8f,warning.transform);}
                    else if(bossMove==BossMove.ComboFrenzy){for(int s=0;s<3;s++)CombatVisuals.WarningArc(transform.position+lockedAim*(s*1.2f),lockedAim,2.8f+s*.35f,110f+s*20f,warning.transform);}
                    else if(bossMove==BossMove.ShockwaveRing){lockedTarget=transform.position;lockedTarget.y=.08f;CombatVisuals.WarningCircle(lockedTarget,2.8f,warning.transform);CombatVisuals.WarningCircle(lockedTarget,5.2f,warning.transform);for(int i=0;i<6;i++){float a=i*Mathf.PI/3f;CombatVisuals.WarningLine(transform.position,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),5.2f,warning.transform);}}
                    else if(bossMove==BossMove.Whirlwind){CombatVisuals.WarningCircle(transform.position,4.0f,warning.transform);CombatVisuals.WarningLine(transform.position,lockedAim,Mathf.Min(distance,5.5f),warning.transform);}
                    else for(int i=-1;i<=1;i++)CombatVisuals.WarningLine(transform.position,Quaternion.Euler(0,i*42,0)*lockedAim,3.5f,warning.transform);
                }
            }
            else if(kind==6&&attackVariant!=2)
            {
                lockedRadialAngle=Time.time*.35f;warning=new GameObject("Radial barrage warning");warning.transform.SetParent(transform.parent,false);int count=9;for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count+lockedRadialAngle;CombatVisuals.WarningLine(transform.position,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),5,warning.transform);}
            }
            else if(kind==9&&attackVariant==0)warning=CombatVisuals.WarningCircle(transform.position,HealingRadius,transform.parent);
            else if(kind==7&&attackVariant!=2){lockedTarget=attackVariant==1?transform.position+lockedAim*3.9f:transform.position;lockedTarget.y=.08f;warning=CombatVisuals.WarningCircle(lockedTarget,attackVariant==1?1.9f:2.3f,transform.parent);}
            else if((kind==1||kind==2||kind==6||kind==9)&&distance<2.8f){lockedTarget=transform.position;lockedTarget.y=.08f;warning=CombatVisuals.WarningCircle(lockedTarget,2.9f,transform.parent);}
            else if(kind==0||kind==8){if(attackVariant==1)warning=CombatVisuals.WarningLine(transform.position,lockedAim,kind==8?4.4f:3.4f,transform.parent);else if(attackVariant==2)warning=CombatVisuals.WarningArc(transform.position,lockedAim,2.2f,135f,transform.parent);else warning=CombatVisuals.WarningCircle(transform.position+lockedAim*1.1f,1.2f,transform.parent);}
            else if(kind==4){warning=CombatVisuals.WarningLine(transform.position+Vector3.up*.05f,lockedAim,attackVariant==1?6.5f:9f,transform.parent);if(attackVariant==2)CombatVisuals.WarningCircle(transform.position+lockedAim*Mathf.Min(distance,8f),2.3f,warning.transform);}
            else if(kind!=10){warning=new GameObject("Spit attack warning");warning.transform.SetParent(transform.parent,false);int ways=kind==2&&attackVariant==2?5:(kind==1&&attackVariant==2||kind==2&&attackVariant==0)?3:1;for(int i=-(ways/2);i<=ways/2;i++)CombatVisuals.WarningParabolicArc(transform.position+Vector3.up*1.3f,transform.position+Quaternion.Euler(0,i*16,0)*lockedAim*Mathf.Clamp(distance,2.5f,14f),kind==7?1.35f:.95f,warning.transform);}
            else if(kind==10)
            {
                if(warning!=null)Destroy(warning);
                if(elite&&enemyAttackCount%4==0&&distance>4f)
                {
                    warning=CombatVisuals.WarningParabolicArc(HumanMuzzlePosition(),lockedTarget,2.5f,transform.parent,new Color(1f,.35f,.15f));
                }
                else
                {
                    Vector3 muzzle=HumanMuzzlePosition();Vector3 shotDirection=game.PlayerPosition-muzzle;shotDirection.y=0;if(shotDirection.sqrMagnitude>.0001f)lockedAim=shotDirection.normalized;
                    warning=CombatVisuals.WarningLine(muzzle,lockedAim,Mathf.Min(distance+2,20),transform.parent);
                }
            }
        }
        bool TryFindHumanCover(out Vector3 bestPos)
        {
            bestPos=transform.position;float bestScore=float.MaxValue;bool found=false;
            Vector3 playerPos=game.PlayerPosition;
            for(int ring=1;ring<=3;ring++)
            {
                float r=ring*1.85f;
                for(int i=0;i<12;i++)
                {
                    float a=i*Mathf.PI/6f;
                    Vector3 cand=transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*r;
                    cand.y=transform.position.y;
                    if(game.Blocked(cand,.6f))continue;
                    bool blocksSight=!game.LineOfSight(cand,playerPos);
                    bool nearWall=blocksSight||game.Blocked(cand+(playerPos-cand).normalized*1.25f,.55f);
                    if(!nearWall)continue;
                    float score=Vector3.Distance(transform.position,cand)+(blocksSight?0f:3.5f);
                    if(score<bestScore){bestScore=score;bestPos=cand;found=true;}
                }
            }
            return found;
        }
        int SelectEnemyVariant(float distance)
        {
            if(kind==9){foreach(var ally in game.Enemies)if(ally!=null&&!ally.dead&&ally!=this&&ally.hp<ally.maxHp*.92f&&Vector3.Distance(ally.transform.position,transform.position)<HealingRadius)return 0;return distance<3.8f?2:1;}
            if(kind==7)return distance>3.9f?1:(enemyAttackCount%2==1?1:0);
            if(kind==6)return distance<3.1f&&enemyAttackCount%3==2?2:(enemyAttackCount%2==1?1:0);
            if(kind==0||kind==8)return distance>2.35f?1:(enemyAttackCount%3);
            return enemyAttackCount%3;
        }
        BossMove SelectBossMove(float distance)
        {
            BossMove chosen;
            bool enraged=hp<maxHp*.6f;
            int cycle=bossAttackCount%10;
            if(IsHumanBoss)
            {
                // Human bosses fight predominantly with guns (70% Gun & Grenades, 30% Melee)
                // MajorBoss (Great Boss / Faction HQ Boss): uses heavy gun barrages, triple grenades, and swift flash lunges more aggressively
                if(MajorBoss)
                {
                    if(distance>5f)
                    {
                        // 7 Gun (barrage), 2 Triple Grenades, 1 FlashLunge
                        chosen=cycle==0||cycle==2||cycle==4||cycle==5||cycle==6||cycle==8||cycle==9?BossMove.Gun:
                               cycle==1||cycle==7?BossMove.Grenade:
                               BossMove.FlashLunge;
                    }
                    else
                    {
                        // Close quarters: 4 Gun bursts, 2 Triple Grenades, 3 Sweeps, 1 FlashLunge
                        chosen=cycle==0||cycle==3||cycle==6||cycle==8?BossMove.Gun:
                               cycle==2||cycle==7?BossMove.Grenade:
                               cycle==1||cycle==4||cycle==9?BossMove.Sweep:
                               BossMove.FlashLunge;
                    }
                }
                else
                {
                    if(distance>5f)
                    {
                        chosen=cycle==0||cycle==2||cycle==4||cycle==6||cycle==8?BossMove.Gun:
                               cycle==1||cycle==7?BossMove.Grenade:
                               cycle==3?BossMove.FlashLunge:BossMove.Rush;
                    }
                    else
                    {
                        chosen=cycle==0||cycle==3||cycle==6||cycle==8?BossMove.Gun:
                               cycle==2||cycle==7?BossMove.Grenade:
                               cycle==1||cycle==4||cycle==9?BossMove.Sweep:BossMove.FlashLunge;
                    }
                }
            }
            else
            {
                if(MajorBoss)
                {
                    // Major mutant bosses (e.g. infected HQ bosses) aggressively employ shockwave rings, whirlwinds, and lethal flash lunges
                    if(distance>6.2f)chosen=cycle%2==0?BossMove.FlashLunge:cycle==1||cycle==5?BossMove.ShockwaveRing:BossMove.Gun;
                    else if(distance>3.8f)chosen=cycle==0||cycle==5?BossMove.ShockwaveRing:cycle==1||cycle==6?BossMove.Whirlwind:cycle==2||cycle==7?BossMove.FlashLunge:cycle==3?BossMove.ComboFrenzy:BossMove.Gun;
                    else chosen=cycle==0||cycle==5?BossMove.ShockwaveRing:cycle==1||cycle==6?BossMove.Whirlwind:cycle==2||cycle==7?BossMove.ComboFrenzy:cycle==3?BossMove.Slam:BossMove.Sweep;
                }
                else
                {
                    int mutantCycle=bossAttackCount%7;
                    if(distance>6.2f)chosen=mutantCycle==1||enraged&&mutantCycle==4?BossMove.FlashLunge:mutantCycle==3?BossMove.Gun:BossMove.Rush;
                    else if(distance>3.8f)chosen=mutantCycle==0?BossMove.FlashLunge:mutantCycle==1?BossMove.Gun:mutantCycle==2?BossMove.Whirlwind:mutantCycle==3?BossMove.Rush:mutantCycle==4?BossMove.ShockwaveRing:mutantCycle==5?BossMove.ComboFrenzy:BossMove.Sweep;
                    else chosen=mutantCycle==0?BossMove.Slam:mutantCycle==1?BossMove.ComboFrenzy:mutantCycle==2?BossMove.Sweep:mutantCycle==3?BossMove.Whirlwind:mutantCycle==4?BossMove.ShockwaveRing:mutantCycle==5?BossMove.FlashLunge:BossMove.ComboFrenzy;
                }
            }
            bossAttackCount++;
            return chosen;
        }
        void Attack()
        {
            GetComponent<EnemyPainted2D>()?.NotifyAttack();GetComponent<FaithfulEnemy3D>()?.NotifyAttack();GetComponentInChildren<ImportedEnemyVisual>()?.NotifyShot();
            if(!alerted&&HeavyMelee){CombatVisuals.Burst(transform.position+lockedAim*1.1f,new Color(.55f,.32f,.18f),12,transform.parent);var d0=game.PlayerPosition-transform.position;d0.y=0;if(d0.magnitude<2.9f&&Vector3.Dot(d0.normalized,lockedAim)>-.15f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(36*damageScale);return;}
            if(kind==3)
            {
                if(bossMove==BossMove.Gun){FireRangedShot();burstShots=IsHumanBoss?(MajorBoss?7:4):(MajorBoss?5:3);burstTimer=IsHumanBoss?(MajorBoss?.09f:.16f):.14f;return;}
                if(bossMove==BossMove.Grenade)
                {
                    FireGrenade(lockedTarget,(MajorBoss?40:32)*damageScale,MajorBoss?3.3f:2.8f);
                    if(MajorBoss)
                    {
                        for(int i=-1;i<=1;i+=2)
                        {
                            Vector3 sideTarget=lockedTarget+Quaternion.Euler(0,i*38f,0)*(lockedTarget-transform.position).normalized*2.6f;
                            FireGrenade(sideTarget,28*damageScale,2.5f);
                        }
                    }
                    return;
                }
                if(bossMove==BossMove.Rush){contact=0;dashSpeed=18f;dashLandingBlast=false;dash=.46f;return;}
                if(bossMove==BossMove.FlashLunge){contact=0;dashSpeed=27f;dashLandingBlast=true;dash=.30f;CombatVisuals.Burst(transform.position,new Color(1f,.72f,.25f),14,transform.parent);return;}
                if(bossMove==BossMove.ComboFrenzy){comboSteps=3;comboTimer=.02f;return;}
                if(bossMove==BossMove.Whirlwind){spinTimer=.68f;spinTick=.02f;return;}
                if(bossMove==BossMove.ShockwaveRing){CombatVisuals.Burst(transform.position,new Color(1f,.38f,.12f),22,transform.parent);var center=new GameObject("Colossus inner shockwave");center.transform.SetParent(transform.parent,false);center.transform.position=lockedTarget;var cb=center.AddComponent<GroundBlast>();cb.game=game;cb.damage=34*damageScale;cb.radius=2.8f;for(int i=0;i<6;i++){float a=i*Mathf.PI/3f;Vector3 ringPos=lockedTarget+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*3.6f;var node=new GameObject("Colossus fissure blast");node.transform.SetParent(transform.parent,false);node.transform.position=ringPos;var gb=node.AddComponent<GroundBlast>();gb.game=game;gb.damage=28*damageScale;gb.radius=2.0f;}return;}
                if(bossMove==BossMove.Slam)
                {
                    CombatVisuals.Burst(transform.position,new Color(.88f,.4f,.17f),18,transform.parent);
                    var hazard=new GameObject("Colossus heavy slam");hazard.transform.SetParent(transform.parent,false);hazard.transform.position=lockedTarget;
                    var blast=hazard.AddComponent<GroundBlast>();blast.game=game;blast.damage=39*damageScale;blast.radius=3.3f;return;
                }
                Vector3 reach=game.PlayerPosition-transform.position;reach.y=0;
                CombatVisuals.Burst(transform.position+lockedAim*1.4f,new Color(.92f,.54f,.22f),12,transform.parent);
                if(reach.magnitude<3.5f&&Vector3.Dot(reach.normalized,lockedAim)>.5f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(27*damageScale);
                return;
            }
            if(kind==0||kind==8)
            {
                if(attackVariant==1){contact=0;dashSpeed=kind==8?16f:12.5f;dashLandingBlast=false;dash=kind==8?.34f:.24f;return;}
                if(attackVariant==2){CombatVisuals.MeleeSlash(transform.position,lockedAim,2.2f,135f,MeleeWeaponFamily.Cleaver,new Color(.88f,.38f,.14f),transform.parent);CombatVisuals.Burst(transform.position+lockedAim*1.3f,new Color(.82f,.38f,.15f),14,transform.parent);Vector3 d2=game.PlayerPosition-transform.position;d2.y=0;if(d2.magnitude<2.25f&&Vector3.Dot(d2.normalized,lockedAim)>.2f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt((kind==8?31f:24f)*damageScale);return;}
                Vector3 impact=transform.position+lockedAim*1.1f;Vector3 toPlayer=game.PlayerPosition-impact;toPlayer.y=0;CombatVisuals.Burst(impact,new Color(.75f,.4f,.16f),9,transform.parent);if(toPlayer.magnitude<1.2f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt((kind==8?23:17)*damageScale);return;
            }
            if(kind==4){contact=0;dashSpeed=attackVariant==1?21f:attackVariant==2?16f:14f;dashLandingBlast=attackVariant==2;dash=attackVariant==1?.36f:attackVariant==2?.58f:.55f;return;}
            if(kind==7&&attackVariant!=2)
            {
                if(attackVariant==1){contact=0;dashSpeed=13f;dashLandingBlast=true;dash=.30f;return;}
                CombatVisuals.Burst(transform.position+lockedAim*1.1f,new Color(.55f,.32f,.18f),16,transform.parent);var delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<2.3f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(36*damageScale);return;
            }
            if((kind==1||kind==2||kind==6||kind==9)&&(Vector3.Distance(game.PlayerPosition,transform.position)<2.8f||(kind==6||kind==9)&&attackVariant==2))
            {
                float rad=(kind==6||kind==9)&&attackVariant==2?3.3f:2.9f;
                CombatVisuals.Burst(transform.position,kind==6?new Color(.65f,.9f,.18f):kind==9?new Color(.6f,.25f,.85f):new Color(.55f,.32f,.18f),16,transform.parent);
                var delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<rad&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(32*damageScale);return;
            }
            if(kind==9&&attackVariant==0)
            {
                foreach(var ally in game.Enemies)if(ally!=null&&!ally.dead&&ally!=this&&Vector3.Distance(ally.transform.position,transform.position)<HealingRadius&&game.LineOfSight(transform.position,ally.transform.position))ally.hp=Mathf.Min(ally.maxHp,ally.hp+ally.maxHp*.1f);
                CombatVisuals.Burst(transform.position,new Color(.2f,1,.65f),12,transform.parent);return;
            }
            if(kind==10&&elite&&enemyAttackCount%4==0&&(game.PlayerPosition-transform.position).magnitude>3.8f)
            {
                FireGrenade(lockedTarget,24*damageScale,2.5f);
            }
            else
            {
                FireRangedShot();
            }
            if(kind==10)
            {
                if(UseAssaultRifle){burstShots=2;burstTimer=.16f;}
                if((enemyAttackCount&1)==0)humanStrafeSign=-humanStrafeSign;
                else {humanStrafeSign=-humanStrafeSign;}
                humanShotsSinceCover++;
                if(humanShotsSinceCover>=(UseAssaultRifle?2:3))
                {
                    humanShotsSinceCover=0;humanReloadTimer=1.65f;
                    hasCoverTarget=TryFindHumanCover(out coverTarget);
                }
            }
            else if(kind==1&&attackVariant==1){burstShots=2;burstTimer=.18f;}
            else if(kind==5&&attackVariant==1){burstShots=1;burstTimer=.15f;}
            else if(kind==6&&attackVariant==1){burstShots=1;burstTimer=.24f;}
        }
        void FireGrenade(Vector3 targetPos,float dmg,float splashRad)
        {
            var imported=GetComponentInChildren<ImportedEnemyVisual>();
            if(imported)imported.NotifyShot();
            Vector3 muzzle=HumanMuzzlePosition();
            if(weapon!=null)weapon.Kick();
            CombatVisuals.Muzzle(muzzle,lockedAim,new Color(1f,.45f,.12f),GunEffect.Normal,transform.parent,7);
            Vector3 flatDir=targetPos-muzzle;flatDir.y=0;
            if(flatDir.sqrMagnitude<.001f)flatDir=lockedAim;flatDir.Normalize();
            var shot=game.SpawnBullet(muzzle,flatDir,false,dmg,15f,new Color(1f,.42f,.12f),GunEffect.Normal);
            shot.isGrenade=true;
            shot.ConfigureParabolic(muzzle,targetPos,14f,19f,splashRad);
        }
        Vector3 HumanMuzzlePosition()
        {
            var human=GetComponentInChildren<HumanFactionVisual>();
            if(human&&human.Muzzle)return human.Muzzle.position;
            var imported=GetComponentInChildren<ImportedEnemyVisual>();
            if(imported!=null)return imported.MuzzlePosition;
            return weapon!=null?weapon.MuzzlePosition:transform.position+Vector3.up*1.05f;
        }
        void FireRangedShot()
        {
            var imported=GetComponentInChildren<ImportedEnemyVisual>();
            if(RaiderBoss&&imported)imported.NotifyShot();
            var painted=GetComponent<EnemyPainted2D>();
            var faithful=GetComponent<FaithfulEnemy3D>();
            var human=GetComponentInChildren<HumanFactionVisual>();
            Vector3 muzzle=human&&human.Muzzle?human.Muzzle.position:(kind==10||RaiderBoss)&&imported!=null?imported.MuzzlePosition:faithful!=null&&faithful.Ready?faithful.MuzzlePosition:painted!=null&&painted.Ready?painted.MuzzlePosition:weapon!=null?weapon.MuzzlePosition:transform.position+Vector3.up*1.05f;
            if(weapon!=null)weapon.Kick();
            Color flare=kind==1?new Color(.48f,.8f,.12f):(kind==5||kind==10||RaiderBoss)?new Color(.6f,.82f,1):kind==6?new Color(.65f,.85f,.18f):new Color(1,.48f,.12f);
            CombatVisuals.Muzzle(muzzle,lockedAim,flare,kind==6?GunEffect.Orbit:GunEffect.Normal,transform.parent,(kind==5||kind==10)?7:kind==2?8:-1);
            if(kind==6)
            {
                int count=9;float phaseOffset=burstShots>0?Mathf.PI/9f:0f;
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/count+lockedRadialAngle+phaseOffset;Vector3 dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                    var shot=game.SpawnBullet(muzzle,dir,false,8*damageScale,7,new Color(.65f,.83f,.18f),GunEffect.Normal);
                    shot.ConfigureParabolic(muzzle,transform.position+dir*(attackVariant==1?6.2f:5.2f),7.5f,17f,.65f);
                }
                return;
            }
            bool humanGun=kind==10||IsHumanBoss;
            if(humanGun)
            {
                game.PlayEnemyGunshot(IsHumanBoss || UseAssaultRifle);
                int spreadCount=IsHumanBoss&&burstShots%2==1?1:0;
                for(int i=-spreadCount;i<=spreadCount;i++)
                    game.SpawnBullet(muzzle,Quaternion.Euler(0,i*11,0)*lockedAim,false,(IsHumanBoss?12:UseAssaultRifle?9:22)*damageScale,IsHumanBoss?25:UseAssaultRifle?26:22,new Color(1,.67f,.28f),GunEffect.Normal);
                return;
            }
            // Zombie and mutant ranged attacks launch parabolic saliva/acid/bio-globs from the mouth.
            Vector3 playerGround=game.PlayerPosition;playerGround.y=.12f;
            float dist=Mathf.Clamp(Vector3.Distance(transform.position,playerGround),2.5f,15f);
            int globs=kind==3?3:kind==2&&attackVariant==2?5:(kind==1&&attackVariant==2||kind==2&&attackVariant==0||kind==7)?3:1;
            float fanStep=kind==3?15f:kind==2&&attackVariant==2?14f:18f;
            Color spitColor=kind==1||kind==3||kind==7?new Color(.52f,.88f,.14f):kind==2?new Color(.75f,.90f,.22f):kind==9?new Color(.65f,.28f,.92f):new Color(.42f,.85f,.32f);
            for(int i=-(globs/2);i<=globs/2;i++)
            {
                Vector3 dir=Quaternion.Euler(0,i*fanStep,0)*lockedAim;
                float rangeJitter=burstShots>0?(burstShots*1.1f-.5f):0f;
                Vector3 landing=transform.position+dir*Mathf.Clamp(dist+rangeJitter,2.2f,15.5f);
                float hSpeed=kind==5&&attackVariant!=2?13.5f:kind==2&&attackVariant==1?14.5f:10.5f;
                float grav=kind==5&&attackVariant==2||kind==7||kind==3?22f:kind==2&&attackVariant==1?14f:18.5f;
                var shot=game.SpawnBullet(muzzle,dir,false,(kind==3?18:kind==7?24:kind==5?19:12)*damageScale,hSpeed,spitColor,GunEffect.Normal);
                shot.corrosive=kind==1||kind==3||kind==7||(kind==5&&attackVariant==2);
                shot.ConfigureParabolic(muzzle,landing,hSpeed,grav,shot.corrosive?1.05f:.75f);
            }
            if(kind==9&&hp<maxHp)hp=Mathf.Min(maxHp,hp+maxHp*.04f);
        }
        public bool Hit(float damage,Vector3? hitPoint=null,Color? hitColor=null,float accuracy=100,Color? numberColor=null)
        {
            if(dead)return false;Vector3 point=hitPoint??(transform.position+Vector3.up*.8f);
            // Poorly controlled fire can clip the target without landing a damaging hit.
            float evadeChance=TargetDodgeChance(accuracy);
            if(evadeChance>0&&UnityEngine.Random.value<evadeChance){CombatVisuals.EvadeNumber(point,game.GameCamera,transform.parent);return false;}
            damage*=1-resistance;
            if(kind==8&&hitPoint.HasValue){Vector3 approach=point-transform.position;approach.y=0;float facing=Vector3.Dot(transform.forward,approach.normalized);damage*=facing>.3f?.55f:facing<-.3f?1.35f:1;}
            CombatVisuals.DamageNumber(point,damage,numberColor??Color.white,game.GameCamera,transform.parent);
            hp-=damage;var painted=GetComponent<EnemyPainted2D>();painted?.NotifyHit(point);GetComponent<FaithfulEnemy3D>()?.NotifyHit(point);game.Burst(point,hitColor??(kind==3?new Color(.7f,.8f,.2f):new Color(.55f,.16f,.1f)),4);
            if(hp<=0)
            {
                dead=true;ClearWarning();charging=false;dash=0;burstShots=0;
                bool escape=kind==3&&MajorBoss&&game!=null&&!game.IsOverworld&&VaultGame.IsFactionBossEscapeStage(game.SelectedStageId);
                if(escape)
                {
                    Escaping=true;
                    Vector3 away=transform.position-game.PlayerPosition;away.y=0;
                    escapeDirection=away.sqrMagnitude>.01f?away.normalized:Vector3.forward;
                    transform.rotation=Quaternion.LookRotation(escapeDirection);
                    CombatVisuals.Burst(transform.position+Vector3.up*.9f,new Color(.85f,.88f,.92f),18,transform.parent);
                    game.FoeKilled(this);
                    Destroy(gameObject,1.25f);
                }
                else
                {
                    painted?.BeginDeath();
                    var meshLife=GetComponent<FaithfulEnemy3D>();meshLife?.BeginDeath();
                    bool humanLife=GetComponentInChildren<HumanFactionVisual>()!=null;
                    game.FoeKilled(this);
                    float deathDuration=meshLife!=null||humanLife?FaithfulEnemy3D.DeathDuration
                        :painted!=null&&painted.Ready?EnemyPainted2D.DeathDuration+.03f:0;
                    Destroy(gameObject,deathDuration);
                }
            }
            return true;
        }
        void OnDestroy(){ClearWarning();}
    }
    public sealed class GroundBlast : MonoBehaviour
    {
        public VaultGame game;public float damage,radius;float remaining=.12f;
        float visibilityNext;bool cachedVisible;
        void Update(){if(game==null){Destroy(gameObject);return;}if(!game.Active){Destroy(gameObject);return;}remaining-=Time.deltaTime;if(remaining>0)return;CombatVisuals.Burst(transform.position,new Color(1,.25f,.08f),20,transform.parent);CombatVisuals.Impact(transform.position,Vector3.up,Color.red,GunEffect.Normal,transform.parent);Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<radius&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(damage);Destroy(gameObject);}
    }
    public class Shot : MonoBehaviour
    {
        public VaultGame game;
        public Vector3 velocity;
        public float damage, life=3;
        public float hitAccuracy=100;
        float falloffRetention=1f,travelDistance,closeBonus;
        public void ConfigureDamageFalloff(Gun gun,float bonus)
        {
            falloffRetention=Mathf.Clamp01(Arsenal.RangeRetention(gun)+Mathf.Max(0f,bonus));
            closeBonus=gun!=null&&Arsenal.Category(VaultLanguage.DisplayModel(gun))==WeaponClass.Shotgun?.25f:0f;
            travelDistance=0f;
        }
        public float actionCharge;
        public GunElement element;public float elementPercent,elementRadius,elementDuration,elementBonus=1f;public int elementChains;
        public bool friendly,corrosive,critical,moduleAmplified,legendaryEffect,parabolic,skillProjectile,isGrenade;
        public float verticalVelocity,gravity=18f,splashRadius;
        public void ConfigureParabolic(Vector3 origin,Vector3 targetPos,float horizSpeed,float arcGravity=18f,float splash=.85f)
        {
            parabolic=true;gravity=arcGravity;splashRadius=splash;
            Vector3 flat=new Vector3(targetPos.x-origin.x,0,targetPos.z-origin.z);
            float dist=Mathf.Max(1.2f,flat.magnitude);
            float flight=Mathf.Clamp(dist/Mathf.Max(4f,horizSpeed),.30f,1.30f);
            velocity=(flat.sqrMagnitude>.001f?flat.normalized:Vector3.forward)*(dist/flight);
            verticalVelocity=(.12f-origin.y+.5f*arcGravity*flight*flight)/flight;
            life=flight+.35f;
            if(isGrenade) CombatVisuals.ConfigureGrenadeProjectile(this,visualColor);
            else CombatVisuals.ConfigureSpitProjectile(this,visualColor);
        }
        public GunEffect effect;
        public int bounces;
        public Foe last;
        public Color visualColor=Color.cyan;
        public static readonly Color LegendaryDamageColor=new Color(.32f,.82f,1f);
        public static readonly Color ModuleDamageColor=new Color(.08f,.72f,1f);
        public Color DamageNumberColor=>legendaryEffect?LegendaryDamageColor:critical?new Color(.55f,.85f,1f):Color.white;
        float visibilityNext;bool cachedVisible;
        void Update()
        {
            if(game==null || !game.Active) return;
            float dt=Time.deltaTime;
            Vector3 old=transform.position;
            if(parabolic){verticalVelocity-=gravity*dt;transform.position+=new Vector3(velocity.x,verticalVelocity,velocity.z)*dt;}else transform.position+=velocity*dt;
            life-=dt;
            bool blocked=!game.Layout.ClearLine(old,transform.position,.12f);
            if(blocked){Vector3 end=transform.position;float clear=0,wall=1;for(int i=0;i<12;i++){float mid=(clear+wall)*.5f;if(game.Layout.ClearLine(old,Vector3.Lerp(old,end,mid),.12f))clear=mid;else wall=mid;}transform.position=Vector3.Lerp(old,end,clear);}
            if(friendly)
            {
                Foe target=null;float nearestContact=float.MaxValue;
                foreach(Foe foe in game.Enemies)
                {
                    if(foe==null || foe.dead || foe==last) continue;
                    if(DistanceToSegment(foe.transform.position,old,transform.position)<(foe.kind==3?1.2f:.65f)) {float contactDistance=(ContactPoint(foe.transform.position,old,transform.position,foe.kind==3?1.2f:.65f)-old).sqrMagnitude;if(contactDistance<nearestContact){nearestContact=contactDistance;target=foe;}}
                }
                if(target!=null)
                {
                    Vector3 hitPoint=ContactPoint(target.transform.position,old,transform.position,target.kind==3?1.2f:.65f);
                    travelDistance+=Vector3.Distance(old,hitPoint);
                    damage*=Arsenal.DamageAtDistance(falloffRetention,travelDistance,closeBonus);
                    travelDistance=0f;
                    closeBonus=0f; // A ricochet is no longer a point-blank shot.
                    transform.position=hitPoint;CombatVisuals.Impact(hitPoint,velocity,visualColor,effect,transform.parent);
                    if(!target.Hit(damage,hitPoint,DamageNumberColor,hitAccuracy,DamageNumberColor)){Destroy(gameObject);return;}
                    GunElements.Apply(this,target,hitPoint);
                    if(actionCharge>0){game.ActionSkillOnBulletHit(actionCharge);actionCharge=0;}
                    if(!skillProjectile){game.RunHit(target,damage,hitPoint);game.LegendaryBulletHit(target,hitPoint);}
                    if(effect==GunEffect.Singularity) { game.GravityWell(transform.position,damage,DamageNumberColor); Destroy(gameObject); return; }
                    if(effect==GunEffect.Ricochet && bounces<3)
                    {
                        last=target; bounces++;
                        game.ricochetEvents++; game.splitEvents+=2;
                        Foe next=game.NearestEnemy(transform.position,last);
                        for(int i=-1;i<=1;i+=2)
                        {
                            var split=game.SpawnBullet(transform.position,Quaternion.Euler(0,i*40,0)*velocity.normalized,true,damage*.3f,18,Color.cyan,GunEffect.Normal,hitAccuracy);
                            split.critical=critical;split.moduleAmplified=moduleAmplified;split.legendaryEffect=legendaryEffect;split.last=target; // Leave the struck enemy before looking for a new collision.
                            split.element=element;split.elementPercent=elementPercent;split.elementRadius=elementRadius;split.elementDuration=elementDuration;split.elementChains=elementChains;split.elementBonus=elementBonus;
                        }
                        if(next!=null) { velocity=(next.transform.position-transform.position).normalized*25; life=2; return; }
                    }
                    Destroy(gameObject); return;
                }
            }
            if(friendly)travelDistance+=Vector3.Distance(old,transform.position);
            else if((!parabolic||transform.position.y<=1.65f)&&DistanceToSegment(game.PlayerPosition,old,transform.position)<.65f) { CombatVisuals.Impact(ContactPoint(game.PlayerPosition,old,transform.position,.6f),velocity,visualColor,effect,transform.parent);if(parabolic)CombatVisuals.Burst(transform.position,visualColor,10,transform.parent);game.Hurt(damage); LeaveAcid();Destroy(gameObject); return; }
            if((parabolic&&transform.position.y<=.12f)||blocked||life<0)
            {
                if(parabolic)
                {
                    transform.position=new Vector3(transform.position.x,.12f,transform.position.z);
                    CombatVisuals.Burst(transform.position,visualColor,isGrenade?22:10,transform.parent);
                    CombatVisuals.Impact(transform.position,new Vector3(velocity.x,verticalVelocity,velocity.z),visualColor,effect,transform.parent);
                    if(isGrenade)
                    {
                        var hazard=new GameObject("Grenade ground blast");hazard.transform.SetParent(transform.parent,false);hazard.transform.position=transform.position;
                        var blast=hazard.AddComponent<GroundBlast>();blast.game=game;blast.damage=damage;blast.radius=splashRadius>0?splashRadius:2.5f;
                    }
                    else if(splashRadius>0){Vector3 d=game.PlayerPosition-transform.position;d.y=0;if(d.magnitude<splashRadius&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(damage*.75f);}
                }
                LeaveAcid();
                if(blocked)CombatVisuals.Impact(transform.position,velocity,visualColor,effect,transform.parent);
                if(effect==GunEffect.Singularity && friendly) game.GravityWell(transform.position,damage,DamageNumberColor);
                Destroy(gameObject);
            }
        }
        void LeaveAcid()
        {
            if(!corrosive||friendly)return;
            var pool=new GameObject("Acid residue");pool.transform.SetParent(transform.parent,false);pool.transform.position=new Vector3(transform.position.x,.08f,transform.position.z);
            var acid=pool.AddComponent<AcidResidue>();acid.game=game;acid.damage=damage*.3f;
        }
        static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b)
        { p.y=0;a.y=0;b.y=0;Vector3 ab=b-a; float t=ab.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude); return Vector3.Distance(p,a+ab*t); }
        static Vector3 ContactPoint(Vector3 p,Vector3 a,Vector3 b,float radius)
        {
            Vector3 ab=b-a,flat=new Vector3(ab.x,0,ab.z),delta=new Vector3(p.x-a.x,0,p.z-a.z);
            if(flat.sqrMagnitude<.0001f)return a;
            float projection=Vector3.Dot(delta,flat)/flat.sqrMagnitude;
            float perpendicular=(delta-flat*projection).sqrMagnitude;
            float entry=projection-Mathf.Sqrt(Mathf.Max(0,radius*radius-perpendicular)/flat.sqrMagnitude);
            return a+ab*Mathf.Clamp01(entry);
        }
    }
    public sealed class AcidResidue : MonoBehaviour
    {
        public VaultGame game;public float damage;float life=2.8f,tick=.55f;GameObject perimeter;
        void Start(){perimeter=CombatVisuals.WarningCircle(transform.position,1.5f,transform);}
        float visibilityNext;bool cachedVisible;
        void Update(){if(game==null||!game.Active){Destroy(gameObject);return;}life-=Time.deltaTime;tick-=Time.deltaTime;if(tick<=0){tick=.65f;CombatVisuals.Burst(transform.position,new Color(.55f,.75f,.12f),3,transform);Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<1.5f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(damage);}if(life<=0)Destroy(gameObject);}
    }
    public class Well : MonoBehaviour
    {
        public VaultGame game;
        public float damage, timer=1.4f;public Color numberColor=Color.white;
        float visibilityNext;bool cachedVisible;
        void Update()
        {
            if(!game.Active) return;
            timer-=Time.deltaTime;
            transform.Rotate(0,170*Time.deltaTime,0);
            foreach(Foe f in game.Enemies)
                if(f!=null && !f.dead && Vector3.Distance(f.transform.position,transform.position)<4.5f)
                { Vector3 pull=transform.position-f.transform.position; pull.y=0; f.transform.position=game.MoveWithinRoom(f.transform.position,pull.normalized*3*Time.deltaTime,.6f); }
            if(timer<=0)
            {
                game.Burst(transform.position,new Color(.75f,.65f,.35f),24);
                foreach(Foe f in game.Enemies.ToArray()) if(f!=null && Vector3.Distance(f.transform.position,transform.position)<4.6f) f.Hit(damage*2.2f,null,numberColor,100,numberColor);
                Destroy(gameObject);
            }
        }
    }
    public class Spark : MonoBehaviour
    {
        public Vector3 velocity;
        public float life=.5f;
        float visibilityNext;bool cachedVisible;
        void Update() { if(Time.timeScale==0)return; transform.position+=velocity*Time.deltaTime; transform.localScale*=Mathf.Pow(.15f,Time.deltaTime); life-=Time.deltaTime; if(life<=0)Destroy(gameObject); }
    }
}
