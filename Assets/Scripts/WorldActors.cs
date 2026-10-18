using UnityEngine;

namespace NeonVault
{
    public class Foe : MonoBehaviour
    {
        public VaultGame game;
        public float hp, maxHp, speed, timer, contact;
        public int kind;
        public bool dead;
        public AimedWeapon weapon;
        public bool IsTelegraphing=>charging;
        public float TelegraphProgress=>charging?Mathf.Clamp01(1-charge/TelegraphSeconds):0;
        public bool IsLunging=>dash>0;
        public bool IsMelee=>kind==0||kind==4||kind==8;
        public float HealingRadius=>10f;
        public static float TargetDodgeChance(float accuracy)=>Mathf.Clamp((82-Mathf.Clamp(accuracy,1,100))*.006f,0,.30f);
        public float TelegraphSeconds=>kind==5?.95f:kind==7?1.2f:kind==4?.65f:kind==0||kind==8?.7f:.85f;
        public static string Name(int kind)=>VaultLanguage.T(new[]{"SHAMBLER","ACID SPITTER","RAD GUNNER","WASTELAND COLOSSUS","POUNCER","NEEDLE STALKER","RAD CYCLER","BLOATER","PLATED CORPSE","FLESH WEAVER"}[Mathf.Clamp(kind,0,9)],new[]{"徘徊屍","酸吐き","放射兵","荒野の巨骸","跳び屍","針撃ち","放射旋回体","膨張屍","装甲屍","再生母体"}[Mathf.Clamp(kind,0,9)]);
        bool alerted, charging;float charge,dash,damageScale=1,resistance,lockedRadialAngle;Vector3 lockedAim,lockedTarget;GameObject warning;
        public void Init(VaultGame g,int k,int depth)
        {
            game=g;kind=k;maxHp=hp=k==3?650:45+k*9;speed=k==3?1.8f:k==4?3.8f:k==0?2.7f:k==8?2.35f:k==1||k==5?2.3f:3.1f;timer=.75f+k*.09f;
            resistance=Mathf.Min(.3f,depth*.025f);damageScale=1;
        }
        public void ConfigureDifficulty(float health,float damage,float resist){maxHp*=health;hp=maxHp;damageScale*=damage;resistance=Mathf.Clamp(resist,0,.45f);}
        void ClearWarning(){if(warning!=null)Destroy(warning);warning=null;}
        void Update()
        {
            if(game==null||dead)return;
            if(!game.Active){ClearWarning();charging=false;dash=0;timer=Mathf.Max(timer,.8f);return;}
            Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;float distance=delta.magnitude;Vector3 direction=delta.normalized;
            bool visible=game.LineOfSight(transform.position,game.PlayerPosition);
            if(distance<17&&visible)alerted=true;if(!alerted)return;
            if(!charging&&dash<=0){if(distance>.01f)transform.rotation=Quaternion.LookRotation(direction);if(weapon!=null)weapon.Aim(direction);}
            if(dash>0){dash-=Time.deltaTime;transform.position=game.MoveWithinRoom(transform.position,lockedAim*14*Time.deltaTime,.6f);if(Vector3.Distance(game.PlayerPosition,transform.position)<1.25f&&contact<=0){game.Hurt(22*damageScale);contact=1;dash=0;}}
            else if(!charging)
            {
                Vector3 movement=direction;
                bool mobile=(kind!=1&&kind!=5&&kind!=9)||distance>10||!visible;
                if((kind==0||kind==8)&&distance<1.65f)mobile=false;
                if(kind==7&&distance<3.5f)mobile=false;
                if((kind==1||kind==5||kind==9)&&visible&&distance<5){movement=-direction;mobile=true;}
                if(kind==6&&visible&&distance<13){movement=(Vector3.Cross(Vector3.up,direction)+direction*(distance>8?.35f:-.35f)).normalized;mobile=true;}
                if(mobile)transform.position=game.MoveWithinRoom(transform.position,(visible?movement:game.ChaseDirection(transform.position))*speed*Time.deltaTime,.6f);
            }
            contact-=Time.deltaTime; // Damage comes from readable attacks, never passive body contact.
            if(charging)
            {
                charge-=Time.deltaTime;if(charge>0)return;charging=false;ClearWarning();Attack();timer=kind==3?1.85f:kind==5?2.5f:kind==7?3.2f:kind==0||kind==8?1.1f:kind==4?1.9f:kind==9?3.3f:2f;return;
            }
            timer-=Time.deltaTime;
            if(timer>0||!visible||distance>18)return;
            if((kind==0||kind==8)&&distance>2.25f||kind==7&&distance>4.5f||kind==4&&distance>10)return;
            lockedTarget=game.PlayerPosition;lockedTarget.y=.08f;lockedAim=direction;charging=true;charge=TelegraphSeconds;
            if(kind==7){lockedTarget=transform.position;lockedTarget.y=.08f;warning=CombatVisuals.WarningCircle(lockedTarget,3.5f,transform.parent);}
            else if(kind==0||kind==8)warning=CombatVisuals.WarningCircle(transform.position+lockedAim*1.1f,1.2f,transform.parent);
            else if(kind==9)warning=CombatVisuals.WarningCircle(transform.position,HealingRadius,transform.parent);
            else if(kind==3||kind==6)
            {
                // Freeze the barrage phase before its warning, so every spoke matches a fired bullet.
                lockedRadialAngle=Time.time*.35f;
                warning=new GameObject("Radial barrage warning");warning.transform.SetParent(transform.parent,false);
                int count=kind==3?16:9;
                for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count+lockedRadialAngle;CombatVisuals.WarningLine(transform.position,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),5,warning.transform);}
                if(kind==3)CombatVisuals.WarningLine(transform.position,lockedAim,Mathf.Min(distance+2,20),warning.transform);
            }
            else warning=CombatVisuals.WarningLine(transform.position+Vector3.up*.05f,lockedAim,kind==4?9:Mathf.Min(distance+2,20),transform.parent);
        }
        void Attack()
        {
            GetComponent<EnemyPainted2D>()?.NotifyAttack();GetComponent<FaithfulEnemy3D>()?.NotifyAttack();
            if(kind==0||kind==8){Vector3 impact=transform.position+lockedAim*1.1f;Vector3 toPlayer=game.PlayerPosition-impact;toPlayer.y=0;CombatVisuals.Burst(impact,new Color(.75f,.4f,.16f),9,transform.parent);if(toPlayer.magnitude<1.2f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt((kind==8?23:17)*damageScale);return;}
            if(kind==4){dash=.55f;return;}
            if(kind==7){var hazard=new GameObject("Bloater chemical blast");hazard.transform.SetParent(transform.parent,false);hazard.transform.position=lockedTarget;var blast=hazard.AddComponent<GroundBlast>();blast.game=game;blast.damage=19*damageScale;blast.radius=3.5f;return;}
            if(kind==9)
            {
                foreach(var ally in game.Enemies)if(ally!=null&&!ally.dead&&ally!=this&&Vector3.Distance(ally.transform.position,transform.position)<HealingRadius&&game.LineOfSight(transform.position,ally.transform.position))ally.hp=Mathf.Min(ally.maxHp,ally.hp+ally.maxHp*.1f);
                CombatVisuals.Burst(transform.position,new Color(.2f,1,.65f),12,transform.parent);return;
            }
            var painted=GetComponent<EnemyPainted2D>();
            var faithful=GetComponent<FaithfulEnemy3D>();
            Vector3 muzzle=faithful!=null&&faithful.Ready?faithful.MuzzlePosition:painted!=null&&painted.Ready?painted.MuzzlePosition:weapon!=null?weapon.MuzzlePosition:transform.position+Vector3.up*1.05f;
            if(weapon!=null)weapon.Kick();
            Color flare=kind==1?new Color(.48f,.8f,.12f):kind==5?new Color(.6f,.82f,1):kind==6?new Color(.65f,.85f,.18f):new Color(1,.48f,.12f);
            CombatVisuals.Muzzle(muzzle,lockedAim,flare,kind==6?GunEffect.Orbit:kind==3?GunEffect.Scatter:GunEffect.Normal,transform.parent,kind==5?7:kind==2?8:-1);
            if(kind==3||kind==6)
            {
                int count=kind==3?16:9;for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count+lockedRadialAngle;game.SpawnBullet(muzzle,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),false,(kind==3?10:8)*damageScale,kind==3?6:7,new Color(.65f,.83f,.18f),GunEffect.Normal);}
            }
            if(kind!=6)for(int i=kind==2||kind==3?-1:0;i<=(kind==2||kind==3?1:0);i++){var shot=game.SpawnBullet(muzzle,Quaternion.Euler(0,i*14,0)*lockedAim,false,(kind==5?22:10)*damageScale,kind==5?22:kind==1?11:13,(kind==1?new Color(.55f,.8f,.12f):kind==2?new Color(.75f,.85f,.25f):new Color(1,.67f,.28f)),GunEffect.Normal);shot.corrosive=kind==1;}
        }
        public bool Hit(float damage,Vector3? hitPoint=null,Color? hitColor=null,float accuracy=100)
        {
            if(dead)return false;Vector3 point=hitPoint??(transform.position+Vector3.up*.8f);
            // Poorly controlled fire can clip the target without landing a damaging hit.
            float evadeChance=TargetDodgeChance(accuracy);
            if(evadeChance>0&&UnityEngine.Random.value<evadeChance){CombatVisuals.EvadeNumber(point,game.GameCamera,transform.parent);return false;}
            damage*=1-resistance;
            if(kind==8&&hitPoint.HasValue){Vector3 approach=point-transform.position;approach.y=0;float facing=Vector3.Dot(transform.forward,approach.normalized);damage*=facing>.3f?.55f:facing<-.3f?1.35f:1;}
            CombatVisuals.DamageNumber(point,damage,hitColor??new Color(1,.84f,.42f),game.GameCamera,transform.parent);
            hp-=damage;var painted=GetComponent<EnemyPainted2D>();painted?.NotifyHit(point);GetComponent<FaithfulEnemy3D>()?.NotifyHit(point);game.Burst(point,hitColor??(kind==3?new Color(.7f,.8f,.2f):new Color(.55f,.16f,.1f)),4);
            if(hp<=0){dead=true;ClearWarning();painted?.BeginDeath();var meshLife=GetComponent<FaithfulEnemy3D>();meshLife?.BeginDeath();game.FoeKilled(this);Destroy(gameObject,meshLife!=null?FaithfulEnemy3D.DeathDuration:painted!=null&&painted.Ready?EnemyPainted2D.DeathDuration+.03f:0);}return true;
        }
        void OnDestroy(){ClearWarning();}
    }
    public sealed class GroundBlast : MonoBehaviour
    {
        public VaultGame game;public float damage,radius;float remaining=.12f;
        void Update(){if(game==null){Destroy(gameObject);return;}if(!game.Active){Destroy(gameObject);return;}remaining-=Time.deltaTime;if(remaining>0)return;CombatVisuals.Burst(transform.position,new Color(1,.25f,.08f),20,transform.parent);CombatVisuals.Impact(transform.position,Vector3.up,Color.red,GunEffect.Normal,transform.parent);Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<radius&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(damage);Destroy(gameObject);}
    }
    public class Shot : MonoBehaviour
    {
        public VaultGame game;
        public Vector3 velocity;
        public float damage, life=3;
        public float hitAccuracy=100;
        public bool friendly,corrosive,critical,moduleAmplified;
        public GunEffect effect;
        public int bounces;
        public Foe last;
        public Color visualColor=Color.cyan;
        public Color DamageNumberColor=>critical?new Color(1,.9f,.12f):moduleAmplified?new Color(1,.22f,.16f):Color.white;
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
                            split.critical=critical;split.moduleAmplified=moduleAmplified;split.last=target; // Leave the struck enemy before looking for a new collision.
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
        void Update(){if(game==null||!game.Active){Destroy(gameObject);return;}life-=Time.deltaTime;tick-=Time.deltaTime;if(tick<=0){tick=.65f;CombatVisuals.Burst(transform.position,new Color(.55f,.75f,.12f),3,transform);Vector3 delta=game.PlayerPosition-transform.position;delta.y=0;if(delta.magnitude<1.5f&&game.LineOfSight(transform.position,game.PlayerPosition))game.Hurt(damage);}if(life<=0)Destroy(gameObject);}
    }
    public class Well : MonoBehaviour
    {
        public VaultGame game;
        public float damage, timer=1.4f;public Color numberColor=Color.white;
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
        void Update() { if(Time.timeScale==0)return; transform.position+=velocity*Time.deltaTime; transform.localScale*=Mathf.Pow(.15f,Time.deltaTime); life-=Time.deltaTime; if(life<=0)Destroy(gameObject); }
    }
}

