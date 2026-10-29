using UnityEngine;

namespace NeonVault
{
    public class Foe : MonoBehaviour
    {
        public VaultGame game;
        public bool elite;
        public Color HealthBarColor=>elite?new Color(.72f,.28f,1f):kind==3?new Color(1f,.58f,.12f):new Color(.92f,.12f,.16f);
        public float hp, maxHp, speed, timer, contact;
        public int kind;
        public bool dead;
        public AimedWeapon weapon;
        public bool IsTelegraphing=>charging;
        public float TelegraphProgress=>charging?Mathf.Clamp01(1-charge/TelegraphSeconds):0;
        public bool IsLunging=>dash>0;
        // Stable visual contract: 0 rush, 1 overhead slam, 2 sweeping strike.
        public int BossAttackStyle=>kind==3?(int)bossMove:-1;
        public bool RaiderBoss { get; private set; }
        public void SetRaiderBoss(bool value){RaiderBoss=kind==3&&value;}
        public bool HeavyMelee=>kind==1||kind==2||kind==6||kind==7||kind==9;
        public bool IsMelee=>kind==0||kind==4||kind==8||HeavyMelee;
        public float HealingRadius=>10f;
        public bool UseAssaultRifle { get; private set; }
        public float SeparationRadius=>kind==3?1.45f:elite?1.0f:.78f;
        public static float TargetDodgeChance(float accuracy)=>Mathf.Clamp((82-Mathf.Clamp(accuracy,1,100))*.006f,0,.30f);
        public float TelegraphSeconds=>kind==3?(bossMove==BossMove.Gun?1.05f:bossMove==BossMove.Slam?1.25f:bossMove==BossMove.Sweep?.82f:.6f):(kind==5||kind==10)?.95f:kind==7?1.2f:kind==4?.65f:kind==8?.42f:kind==0?.7f:.85f;
        public static string Name(int kind)=>kind==10?VaultLanguage.T("RAIDER","レイダー"):VaultLanguage.T(new[]{"SHAMBLER","ACID SPITTER","RAD GUNNER","WASTELAND COLOSSUS","POUNCER","NEEDLE STALKER","RAD CYCLER","BLOATER","PLATED CORPSE","FLESH WEAVER"}[Mathf.Clamp(kind,0,9)],new[]{"シャンブラー","アシッドスピッター","ラッドガンナー","ウェイストランド・コロッサス","パウンサー","ニードルストーカー","ラッドサイクラー","ブロウター","プレートコープス","フレッシュウィーバー"}[Mathf.Clamp(kind,0,9)]);
        enum BossMove { Rush, Slam, Sweep, Gun }
        bool alerted, charging;float charge,dash,damageScale=1,resistance,lockedRadialAngle,burstTimer;int burstShots;Vector3 lockedAim,lockedTarget;GameObject warning;
        BossMove bossMove;int bossAttackCount;
        public void Init(VaultGame g,int k,int depth)
        {
            game=g;kind=k;maxHp=hp=k==3?650:k==10?95:(k==1||k==2||k==6||k==7||k==9)?230:45+k*9;speed=k==3?3.25f:k==4?3.8f:k==0?2.7f:k==8?5.4f:k==1||k==5?2.3f:3.1f;timer=.75f+k*.09f;
            // Choose once per instance without consuming the combat random sequence.
            int x=Mathf.RoundToInt(transform.position.x*10),z=Mathf.RoundToInt(transform.position.z*10);
            UseAssaultRifle=k==10&&unchecked((x*73856093^z*19349663)&1)==0;
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
            if(game==null||dead)return;
            if(!game.Active){ClearWarning();charging=false;dash=0;burstShots=0;timer=Mathf.Max(timer,.8f);return;}
            if(burstShots>0){burstTimer-=Time.deltaTime;if(burstTimer<=0){FireRangedShot();burstShots--;burstTimer=RaiderBoss?.22f:.16f;}}
            Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;float distance=delta.magnitude;Vector3 direction=delta.normalized;
            if(!alerted&&distance>22)return;
            if(Time.time>=visibilityNext){visibilityNext=Time.time+.14f+(kind&7)*.008f;cachedVisible=distance<35&&game.LineOfSight(transform.position,game.PlayerPosition);}
            bool visible=cachedVisible;
            if(distance<17&&visible)alerted=true;if(!alerted)return;
            if(!charging&&dash<=0&&burstShots==0){if(distance>.01f)transform.rotation=Quaternion.LookRotation(direction);if(weapon!=null)weapon.Aim(direction);}
            if(dash>0){dash-=Time.deltaTime;transform.position=game.MoveWithinRoom(transform.position,lockedAim*(kind==3?18:14)*Time.deltaTime,.6f);if(Vector3.Distance(game.PlayerPosition,transform.position)<(kind==3?1.5f:1.25f)&&contact<=0){game.Hurt((kind==3?29:22)*damageScale);contact=1;dash=0;}}
            else if(!charging)
            {
                Vector3 movement=direction;
                bool mobile=HeavyMelee||((kind!=1&&kind!=5&&kind!=10&&kind!=9)||distance>10||!visible);
                if(HeavyMelee&&distance<2.1f)mobile=false;
                if((kind==0||kind==8)&&distance<1.65f)mobile=false;
                if(kind==3&&distance<2.4f)mobile=false;
                if(RaiderBoss&&burstShots>0)mobile=false;
                if(kind==7&&distance<2.2f)mobile=false;
                if((kind==5||kind==10)&&visible&&distance<5){movement=-direction;mobile=true;}
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
                    if((mobile||away.sqrMagnitude>0)&&!(RaiderBoss&&burstShots>0))transform.position=game.MoveWithinRoom(transform.position,movement*speed*Time.deltaTime,.6f);
                }
            }
            contact-=Time.deltaTime; // Damage comes from readable attacks, never passive body contact.
            if(charging)
            {
                charge-=Time.deltaTime;if(charge>0)return;charging=false;ClearWarning();Attack();timer=kind==3?(bossMove==BossMove.Gun?2.65f:bossMove==BossMove.Slam?2.1f:bossMove==BossMove.Sweep?1.45f:1.7f):kind==10?(UseAssaultRifle?3.1f:2.35f):kind==5?2.5f:kind==7?3.2f:kind==0||kind==8?1.1f:kind==4?1.9f:kind==9?3.3f:2f;return;
            }
            timer-=Time.deltaTime;
            if(timer>0||!visible||distance>18)return;
            if(HeavyMelee&&distance>3.1f)return;
            if((kind==0||kind==8)&&distance>2.25f||kind==7&&distance>2.9f||kind==4&&distance>10)return;
            if(kind==3)
            {
                // A distant boss closes with a committed rush. At close range it alternates
                // between a slow, punishing slam and a faster frontal sweep with a safe rear arc.
                bossMove=RaiderBoss&&bossAttackCount%3==2?BossMove.Gun:distance>4.4f||bossAttackCount%4==3?BossMove.Rush:bossAttackCount%2==0?BossMove.Slam:BossMove.Sweep;
                bossAttackCount++;
            }
            lockedTarget=game.PlayerPosition;lockedTarget.y=.08f;lockedAim=direction;charging=true;charge=TelegraphSeconds;
            if(kind==3)
            {
                warning=new GameObject("Colossus attack warning");warning.transform.SetParent(transform.parent,false);
                if(bossMove==BossMove.Gun)
                {
                    CombatVisuals.WarningLine(transform.position+Vector3.up*.05f,lockedAim,Mathf.Min(distance+2,18),warning.transform);
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
                    for(int i=-1;i<=1;i++)CombatVisuals.WarningLine(transform.position,Quaternion.Euler(0,i*42,0)*lockedAim,3.5f,warning.transform);
                }
            }
            else if(HeavyMelee){lockedTarget=transform.position;lockedTarget.y=.08f;warning=CombatVisuals.WarningCircle(lockedTarget,2.9f,transform.parent);}
            else if(kind==0||kind==8)warning=CombatVisuals.WarningCircle(transform.position+lockedAim*1.1f,1.2f,transform.parent);
            else if(kind==9)warning=CombatVisuals.WarningCircle(transform.position,HealingRadius,transform.parent);
            else if(kind==6)
            {
                // Freeze the barrage phase before its warning, so every spoke matches a fired bullet.
                lockedRadialAngle=Time.time*.35f;
                warning=new GameObject("Radial barrage warning");warning.transform.SetParent(transform.parent,false);
                int count=9;
                for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count+lockedRadialAngle;CombatVisuals.WarningLine(transform.position,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),5,warning.transform);}
            }
            else warning=CombatVisuals.WarningLine(transform.position+Vector3.up*.05f,lockedAim,kind==4?9:Mathf.Min(distance+2,20),transform.parent);
        }
        void Attack()
        {
            GetComponent<EnemyPainted2D>()?.NotifyAttack();GetComponent<FaithfulEnemy3D>()?.NotifyAttack();
            if(kind==3)
            {
                if(RaiderBoss&&bossMove==BossMove.Gun){FireRangedShot();burstShots=2;burstTimer=.22f;return;}
                if(bossMove==BossMove.Rush){contact=0;dash=.46f;return;}
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
            if(kind==0||kind==8){Vector3 impact=transform.position+lockedAim*1.1f;Vector3 toPlayer=game.PlayerPosition-impact;toPlayer.y=0;CombatVisuals.Burst(impact,new Color(.75f,.4f,.16f),9,transform.parent);if(toPlayer.magnitude<1.2f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt((kind==8?23:17)*damageScale);return;}
            if(kind==4){dash=.55f;return;}
            if(HeavyMelee){CombatVisuals.Burst(transform.position+lockedAim*1.1f,new Color(.55f,.32f,.18f),12,transform.parent);var delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<2.9f&&Vector3.Dot(delta.normalized,lockedAim)>-.15f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(36*damageScale);return;}
            if(kind==9)
            {
                foreach(var ally in game.Enemies)if(ally!=null&&!ally.dead&&ally!=this&&Vector3.Distance(ally.transform.position,transform.position)<HealingRadius&&game.LineOfSight(transform.position,ally.transform.position))ally.hp=Mathf.Min(ally.maxHp,ally.hp+ally.maxHp*.1f);
                CombatVisuals.Burst(transform.position,new Color(.2f,1,.65f),12,transform.parent);return;
            }
            FireRangedShot();
            if(kind==10&&UseAssaultRifle){burstShots=2;burstTimer=.16f;}
        }
        void FireRangedShot()
        {
            var imported=GetComponentInChildren<ImportedEnemyVisual>();
            if(RaiderBoss&&imported)imported.NotifyShot();
            var painted=GetComponent<EnemyPainted2D>();
            var faithful=GetComponent<FaithfulEnemy3D>();
            Vector3 muzzle=(kind==10||RaiderBoss)&&imported!=null?imported.MuzzlePosition:faithful!=null&&faithful.Ready?faithful.MuzzlePosition:painted!=null&&painted.Ready?painted.MuzzlePosition:weapon!=null?weapon.MuzzlePosition:transform.position+Vector3.up*1.05f;
            if(weapon!=null)weapon.Kick();
            Color flare=kind==1?new Color(.48f,.8f,.12f):(kind==5||kind==10||RaiderBoss)?new Color(.6f,.82f,1):kind==6?new Color(.65f,.85f,.18f):new Color(1,.48f,.12f);
            CombatVisuals.Muzzle(muzzle,lockedAim,flare,kind==6?GunEffect.Orbit:GunEffect.Normal,transform.parent,(kind==5||kind==10)?7:kind==2?8:-1);
            if(kind==6)
            {
                int count=9;for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count+lockedRadialAngle;game.SpawnBullet(muzzle,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),false,8*damageScale,7,new Color(.65f,.83f,.18f),GunEffect.Normal);}
            }
            if(kind!=6)for(int i=kind==2?-1:0;i<=(kind==2?1:0);i++){var shot=game.SpawnBullet(muzzle,Quaternion.Euler(0,i*14,0)*lockedAim,false,(RaiderBoss?12:kind==10?(UseAssaultRifle?9:22):kind==5?22:10)*damageScale,RaiderBoss?25:kind==10?(UseAssaultRifle?26:22):kind==5?22:kind==1?11:13,(kind==1?new Color(.55f,.8f,.12f):kind==2?new Color(.75f,.85f,.25f):new Color(1,.67f,.28f)),GunEffect.Normal);shot.corrosive=kind==1;}
        }
        public bool Hit(float damage,Vector3? hitPoint=null,Color? hitColor=null,float accuracy=100,Color? numberColor=null)
        {
            if(dead)return false;Vector3 point=hitPoint??(transform.position+Vector3.up*.8f);
            // Poorly controlled fire can clip the target without landing a damaging hit.
            float evadeChance=TargetDodgeChance(accuracy);
            if(evadeChance>0&&UnityEngine.Random.value<evadeChance){CombatVisuals.EvadeNumber(point,game.GameCamera,transform.parent);return false;}
            damage*=1-resistance;
            if(kind==8&&hitPoint.HasValue){Vector3 approach=point-transform.position;approach.y=0;float facing=Vector3.Dot(transform.forward,approach.normalized);damage*=facing>.3f?.55f:facing<-.3f?1.35f:1;}
            CombatVisuals.DamageNumber(point,damage,numberColor??hitColor??new Color(1,.84f,.42f),game.GameCamera,transform.parent);
            hp-=damage;var painted=GetComponent<EnemyPainted2D>();painted?.NotifyHit(point);GetComponent<FaithfulEnemy3D>()?.NotifyHit(point);game.Burst(point,hitColor??(kind==3?new Color(.7f,.8f,.2f):new Color(.55f,.16f,.1f)),4);
            if(hp<=0){dead=true;ClearWarning();painted?.BeginDeath();var meshLife=GetComponent<FaithfulEnemy3D>();meshLife?.BeginDeath();game.FoeKilled(this);Destroy(gameObject,meshLife!=null?FaithfulEnemy3D.DeathDuration:painted!=null&&painted.Ready?EnemyPainted2D.DeathDuration+.03f:0);}return true;
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
        public float actionCharge;
        public bool friendly,corrosive,critical,moduleAmplified,legendaryEffect;
        public GunEffect effect;
        public int bounces;
        public Foe last;
        public Color visualColor=Color.cyan;
        public static readonly Color LegendaryDamageColor=new Color(1f,.36f,.04f);
        public Color DamageNumberColor=>legendaryEffect?LegendaryDamageColor:critical?new Color(1,.9f,.12f):moduleAmplified?new Color(1,.22f,.16f):Color.white;
        float visibilityNext;bool cachedVisible;
        void Update()
        {
            if(game==null || !game.Active) return;
            float dt=Time.deltaTime;
            Vector3 old=transform.position;
            transform.position+=velocity*dt;
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
                    transform.position=hitPoint;CombatVisuals.Impact(hitPoint,velocity,visualColor,effect,transform.parent);
                    if(!target.Hit(damage,hitPoint,DamageNumberColor,hitAccuracy)){Destroy(gameObject);return;}
                    if(actionCharge>0){game.ActionSkillOnBulletHit(actionCharge);actionCharge=0;}
                    game.RunHit(target,damage,hitPoint);
                    game.LegendaryBulletHit(target,hitPoint);
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
                        }
                        if(next!=null) { velocity=(next.transform.position-transform.position).normalized*25; life=2; return; }
                    }
                    Destroy(gameObject); return;
                }
            }
            else if(DistanceToSegment(game.PlayerPosition,old,transform.position)<.6f) { CombatVisuals.Impact(ContactPoint(game.PlayerPosition,old,transform.position,.6f),velocity,visualColor,effect,transform.parent);game.Hurt(damage); LeaveAcid();Destroy(gameObject); return; }
            if(blocked || life<0)
            {
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
                foreach(Foe f in game.Enemies.ToArray()) if(f!=null && Vector3.Distance(f.transform.position,transform.position)<4.6f) f.Hit(damage*2.2f,null,numberColor);
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
