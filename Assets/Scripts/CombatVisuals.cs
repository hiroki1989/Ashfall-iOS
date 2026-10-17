using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonVault
{
    // Code-built effects: one shared additive material, bounded concurrent transient objects.
    public static class CombatVisuals
    {
        static Material glow,smoke;
        static Texture2D smokeTexture;
        static Font numberFont,avoidFont;
        internal static int particles, numbers;
        public static int ActiveDamageNumbers=>numbers;
        public static int TotalDamageNumbers { get; private set; }
        public static Vector3 LastHitPoint { get; private set; }
        public static float LastDamage { get; private set; }
        const int ParticleLimit=280, NumberLimit=96;
        static Material Glow
        {
            get
            {
                if(glow==null)
                {
                    var shader=Resources.Load<Shader>("CombatGlow");
                    if(shader==null)shader=Resources.Load<Shader>("VaultTrail");
                    if(shader==null)shader=Shader.Find("Sprites/Default");
                    glow=new Material(shader){name="Shared combat glow"};
                }
                return glow;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(glow!=null)Object.Destroy(glow);if(smoke!=null)Object.Destroy(smoke);if(smokeTexture!=null)Object.Destroy(smokeTexture);
            glow=smoke=null;smokeTexture=null;numberFont=null;particles=0;numbers=0;TotalDamageNumbers=0;LastDamage=0;LastHitPoint=Vector3.zero;
        }
        static GameObject ObjectAt(string name,Vector3 p,Transform parent)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=p;return obj;
        }
        static LineRenderer Line(GameObject obj,Color color,float width)
        {
            var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=Glow;line.useWorldSpace=false;
            line.sortingOrder=20000;
            line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
            line.numCapVertices=6;line.numCornerVertices=3;line.textureMode=LineTextureMode.Stretch;line.shadowCastingMode=ShadowCastingMode.Off;
            line.receiveShadows=false;return line;
        }
        static LineRenderer Ring(GameObject obj,Color color,float radius,float width)
        {
            var line=Line(obj,color,width);line.loop=true;line.positionCount=32;
            for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
            return line;
        }
        public static GameObject WarningLine(Vector3 p,Vector3 direction,float length,Transform parent)
        {
            p.y=.08f;var obj=ObjectAt("Attack direction warning",p,parent);
            var line=Line(obj,new Color(1,.18f,.12f,.7f),.13f);line.positionCount=2;line.SetPositions(new[]{Vector3.zero,direction*length});
            line.startColor=new Color(1,.42f,.15f,.35f);line.endColor=new Color(1,.12f,.08f,.85f);return obj;
        }
        public static GameObject WarningCircle(Vector3 p,float radius,Transform parent)
        {
            p.y=.08f;var obj=ObjectAt("Artillery danger perimeter",p,parent);Ring(obj,new Color(1,.12f,.05f,.85f),radius,.13f);
            var cross=ObjectAt("Blast center",p,obj.transform);cross.transform.localPosition=Vector3.zero;var line=Line(cross,new Color(1,.35f,.05f,.65f),.09f);line.positionCount=5;
            line.SetPositions(new[]{new Vector3(-radius*.35f,0,0),new Vector3(radius*.35f,0,0),Vector3.zero,new Vector3(0,0,-radius*.35f),new Vector3(0,0,radius*.35f)});return obj;
        }
        public static void ConfigureProjectile(Shot shot,Color color)
        {
            shot.visualColor=color;
            var old=shot.GetComponent<Renderer>();if(old!=null)old.enabled=false;
            // Accept the legacy factory while replacing its temporary trail.
            var oldTrail=shot.GetComponent<TrailRenderer>();if(oldTrail!=null){oldTrail.enabled=false;Object.Destroy(oldTrail);}
            shot.transform.localScale=Vector3.one;
            shot.transform.rotation=Quaternion.LookRotation(shot.velocity.sqrMagnitude>.001f?shot.velocity:Vector3.forward);
            bool singular=shot.effect==GunEffect.Singularity;
            int family=shot.friendly&&shot.game!=null&&shot.game.gun!=null?VaultLanguage.DisplayModel(shot.game.gun):-1;
            bool needle=family==6||family==7;bool heavy=family==2||family==4;
            var core=ObjectAt("Lead tracer core",shot.transform.position,shot.transform);
            core.transform.localPosition=Vector3.zero;
            var coreLine=Line(core,Color.white,singular?.18f:.065f);coreLine.startColor=new Color(1,1,1,.1f);coreLine.endColor=Color.Lerp(color,Color.white,.8f);coreLine.startWidth=singular?.13f:.035f;coreLine.endWidth=singular?.2f:.08f;coreLine.positionCount=2;
            coreLine.SetPositions(new[]{new Vector3(0,0,needle?-.6f:heavy?-.4f:-.28f),new Vector3(0,0,needle?.55f:.3f)});
            var halo=ObjectAt("Hot metal streak",shot.transform.position,shot.transform);halo.transform.localPosition=Vector3.zero;
            var haloLine=Line(halo,color*.8f,singular?.37f:.19f);haloLine.startColor=new Color(color.r,color.g,color.b,.05f);haloLine.endColor=new Color(color.r,color.g,color.b,.5f);haloLine.startWidth=singular?.18f:.08f;haloLine.endWidth=singular?.42f:.22f;haloLine.positionCount=2;
            haloLine.SetPositions(new[]{new Vector3(0,0,-.38f),new Vector3(0,0,.22f)});
            var wake=ObjectAt("Projectile smoke tracer",shot.transform.position,shot.transform);wake.transform.localPosition=Vector3.zero;
            var trail=wake.AddComponent<TrailRenderer>();trail.sharedMaterial=Glow;
            trail.time=singular?.24f:shot.effect==GunEffect.Ricochet?.2f:.12f;trail.numCapVertices=4;trail.numCornerVertices=3;
            trail.minVertexDistance=.08f;trail.startWidth=singular?.35f:.15f;trail.endWidth=0;
            trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);
            trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;
            trail.sortingOrder=20000;
            if(shot.effect==GunEffect.Scatter){coreLine.startWidth=.025f;coreLine.endWidth=.045f;trail.time=.07f;trail.startWidth=.08f;}
            if(shot.corrosive){haloLine.endWidth=.3f;trail.time=.22f;trail.startWidth=.28f;}
            if(needle){trail.time=.055f;trail.startWidth=.065f;coreLine.endWidth=.035f;}
            if(heavy){haloLine.endWidth=.26f;trail.time=.17f;}
            if(shot.effect==GunEffect.Ricochet || shot.effect==GunEffect.Orbit || singular)
            {
                var glyph=ObjectAt("Experimental coil field",shot.transform.position,shot.transform);glyph.transform.localPosition=Vector3.zero;
                Ring(glyph,color,singular?.39f:.22f,.035f);
                if(singular)
                {
                    var inner=ObjectAt("Vacuum distortion",shot.transform.position,glyph.transform);inner.transform.localPosition=Vector3.zero;
                    Ring(inner,new Color(.8f,.7f,.35f),.27f,.04f);inner.transform.localRotation=Quaternion.Euler(65,0,0);
                }
                shot.gameObject.AddComponent<BoltVisualMotion>().Init(shot,glyph.transform);
            }
            else shot.gameObject.AddComponent<BoltVisualMotion>().Init(shot,null);
        }
        public static void Muzzle(Vector3 position,Vector3 direction,Color color,GunEffect effect,Transform parent=null,int model=-1)
        {
            if(particles>=ParticleLimit)return;
            var obj=ObjectAt("Muzzle flare",position,parent);
            obj.transform.rotation=Quaternion.LookRotation(direction.sqrMagnitude>.001f?direction:Vector3.forward);
            float length=effect==GunEffect.Scatter?1.0f:model==1?.4f:model==2||model==6||model==7?.9f:.7f;
            var hot=Color.Lerp(color,Color.white,.82f);
            var line=Line(obj,hot,effect==GunEffect.Scatter?.28f:.14f);line.positionCount=2;
            line.SetPositions(new[]{Vector3.zero,new Vector3(0,0,length)});
            obj.AddComponent<CombatTransient>().Init(line,hot,.065f,Vector3.zero,1.8f);
            Ribbon(position,direction,color,length*.85f,.36f,.11f,parent);
            Vector3 side=Vector3.Cross(direction.normalized,Vector3.up);if(side.sqrMagnitude<.001f)side=Vector3.right;
            int petals=effect==GunEffect.Scatter?5:model==1?2:model==7?1:3;
            for(int i=0;i<petals&&particles<ParticleLimit;i++)Ribbon(position,(direction+side*((i-(petals-1)*.5f)*.32f)).normalized,color,length*.6f,.11f,.075f,parent);
            Puff(position-direction*.08f,direction*.6f+Vector3.up*.4f,model==2||effect==GunEffect.Scatter?.4f:.28f,model==1?.25f:.42f,parent);
            if(effect==GunEffect.Ricochet||effect==GunEffect.Orbit||effect==GunEffect.Singularity||model==8)RingBurst(position,color,effect==GunEffect.Singularity?.55f:.26f,.18f,parent);
            for(int i=0;i<3&&particles<ParticleLimit;i++)Spark(position,new Color(1,.62f,.16f),(direction+Random.insideUnitSphere*.45f)*Random.Range(2f,4f),.12f,parent);
        }
        public static void MeleeSlash(Vector3 position,Vector3 direction,float range,float arcDegrees,MeleeWeaponFamily family,Color color,Transform parent=null)
        {
            if(particles>ParticleLimit-3)return;
            direction.y=0;if(direction.sqrMagnitude<.0001f)direction=Vector3.forward;direction.Normalize();
            range=Mathf.Clamp(range,.25f,4f);float halfArc=Mathf.Clamp(arcDegrees,1,360)*.5f;
            float width=family==MeleeWeaponFamily.Cleaver||family==MeleeWeaponFamily.Hatchet ? .22f : family==MeleeWeaponFamily.Machete ? .17f : .12f;
            var outer=ObjectAt("Melee slash / outer edge",position+Vector3.up*.17f,parent);
            var edge=Line(outer,color,width);SetSlashArc(edge,direction,range,halfArc,.86f);
            outer.AddComponent<CombatTransient>().Init(edge,color,.20f,Vector3.zero,1.05f);
            var inner=ObjectAt("Melee slash / inner edge",position+Vector3.up*.21f,parent);
            var highlight=Line(inner,Color.Lerp(color,Color.white,.72f),width*.38f);SetSlashArc(highlight,direction,range*.76f,halfArc*.92f,.73f);
            inner.AddComponent<CombatTransient>().Init(highlight,Color.Lerp(color,Color.white,.72f),.15f,Vector3.zero,1f);

            // Keep a briefly visible blade at the hand while the edge travels through the arc.
            var camera=Camera.main;Vector3 projected=direction;
            if(camera!=null)
            {
                projected=camera.transform.right*Vector3.Dot(direction,camera.transform.right)+camera.transform.up*Vector3.Dot(direction,camera.transform.up);
                if(projected.sqrMagnitude<.001f)projected=camera.transform.up;
            }
            projected.Normalize();
            Vector3 hand=position+Vector3.up*.82f+direction*.22f+(camera!=null?camera.transform.right*.13f:Vector3.right*.13f);
            var blade=ObjectAt("Melee weapon in hand / "+family,hand,parent);if(camera!=null)blade.transform.rotation=camera.transform.rotation;
            var bladeLine=Line(blade,Color.Lerp(color,Color.white,.86f),width*.72f);bladeLine.useWorldSpace=false;bladeLine.positionCount=3;
            Vector3 axis=camera!=null?camera.transform.InverseTransformDirection(projected).normalized:projected;
            float length=family==MeleeWeaponFamily.Knife||family==MeleeWeaponFamily.Shiv ? .38f : family==MeleeWeaponFamily.Hatchet ? .48f : .62f;
            bladeLine.SetPositions(new[]{Vector3.zero,axis*length*.72f,axis*length});bladeLine.startWidth=width*.58f;bladeLine.endWidth=width*.10f;
            blade.AddComponent<CombatTransient>().Init(bladeLine,Color.Lerp(color,Color.white,.86f),.18f,Vector3.zero,1.12f);
        }
        static void SetSlashArc(LineRenderer line,Vector3 direction,float radius,float halfArc,float y)
        {
            const int segments=18;line.positionCount=segments+1;
            for(int i=0;i<=segments;i++)
            {
                float angle=Mathf.Lerp(-halfArc,halfArc,i/(float)segments);
                Vector3 point=Quaternion.AngleAxis(angle,Vector3.up)*direction*radius;
                line.SetPosition(i,new Vector3(point.x,y,point.z));
            }
        }
        public static void ConfigureWell(Well well,Color color)
        {
            foreach(var renderer in well.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            well.transform.localScale=Vector3.one;
            var outer=ObjectAt("Vacuum pressure boundary",well.transform.position,well.transform);outer.transform.localPosition=Vector3.zero;
            Ring(outer,color,4.45f,.065f);
            var inner=ObjectAt("Dust suction spiral",well.transform.position,well.transform);inner.transform.localPosition=Vector3.up*.06f;
            var spiral=Line(inner,new Color(.65f,.55f,.3f),.11f);spiral.positionCount=100;
            for(int i=0;i<100;i++){float t=i/99f;float a=t*Mathf.PI*6;float r=Mathf.Lerp(.4f,4.25f,t);spiral.SetPosition(i,new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}
            var core=ObjectAt("Vacuum discharge core",well.transform.position,well.transform);core.transform.localPosition=Vector3.up*.08f;
            Ring(core,new Color(1,.8f,.4f),.5f,.13f);
            well.gameObject.AddComponent<WellVisualMotion>().Init(well,inner.transform,outer.transform);
        }
        public static void Impact(Vector3 p,Vector3 incoming,Color color,GunEffect effect,Transform parent=null)
        {
            Ribbon(p,Vector3.up,Color.Lerp(color,Color.white,.85f),.23f,.24f,.07f,parent);
            Puff(p,Vector3.up*.6f,.4f,.55f,parent);
            if(effect!=GunEffect.Normal&&effect!=GunEffect.Scatter)RingBurst(p,color,effect==GunEffect.Singularity?1.1f:.45f,.25f,parent);
            // A backward cone of needle sparks separates the strike from the flying bolt.
            Vector3 back=incoming.sqrMagnitude>.001f?-incoming.normalized:Vector3.forward;
            for(int i=0;i<(effect==GunEffect.Scatter?3:6)&&particles<ParticleLimit;i++)Spark(p,i%2==0?Color.Lerp(color,Color.white,.7f):color,(back+Random.insideUnitSphere*.6f)*Random.Range(2.4f,5.5f),Random.Range(.16f,.32f),parent);
            if(effect==GunEffect.Ricochet)RingBurst(p,Color.white,.72f,.34f,parent);
        }
        public static void Burst(Vector3 p,Color color,int count,Transform parent=null)
        {
            for(int i=0;i<Mathf.Min(count,24)&&particles<ParticleLimit;i++)
                Spark(p,color,Random.insideUnitSphere*Random.Range(2f,5f),Random.Range(.2f,.45f),parent);
            if(count>=8)RingBurst(p,color,count>=20?2f:.85f,.38f,parent);
        }
        static void Spark(Vector3 p,Color color,Vector3 velocity,float life,Transform parent)
        {
            if(particles>=ParticleLimit)return;
            var obj=ObjectAt("Impact needle",p,parent);obj.transform.rotation=Quaternion.LookRotation(velocity.sqrMagnitude>.001f?velocity:Vector3.up);
            var line=Line(obj,color,.045f);line.positionCount=2;line.SetPositions(new[]{Vector3.zero,new Vector3(0,0,-.25f)});
            obj.AddComponent<CombatTransient>().Init(line,color,life,velocity,0);
        }
        static void Ribbon(Vector3 p,Vector3 direction,Color color,float length,float width,float life,Transform parent)
        {
            if(particles>=ParticleLimit)return;
            var obj=ObjectAt("Layered flash petal",p,parent);obj.transform.rotation=Quaternion.LookRotation(direction.sqrMagnitude>.001f?direction:Vector3.forward);
            var line=Line(obj,color,width);line.positionCount=3;line.SetPositions(new[]{Vector3.zero,Vector3.forward*length*.38f,Vector3.forward*length});line.endWidth=0;
            obj.AddComponent<CombatTransient>().Init(line,color,life,Vector3.zero,1.2f);
        }
        static void Puff(Vector3 p,Vector3 velocity,float size,float life,Transform parent)
        {
            if(particles>=ParticleLimit)return;
            if(smoke==null)
            {
                smokeTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Reusable soft smoke falloff",wrapMode=TextureWrapMode.Clamp};var pixels=new Color[1024];
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float radius=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),1.6f));}
                smokeTexture.SetPixels(pixels);smokeTexture.Apply(false,true);smoke=new Material(Shader.Find("Sprites/Default")){name="Shared soft ash smoke",mainTexture=smokeTexture};
            }
            var obj=ObjectAt("Transient gun smoke",p,parent);var line=Line(obj,new Color(.18f,.16f,.13f,.24f),size);line.sharedMaterial=smoke;line.positionCount=2;line.SetPositions(new[]{Vector3.up*(-size*.5f),Vector3.up*(size*.5f)});
            obj.AddComponent<CombatTransient>().Init(line,new Color(.18f,.16f,.13f,.24f),life,velocity,2.1f);
        }
        static void RingBurst(Vector3 p,Color color,float radius,float life,Transform parent)
        {
            if(particles>=ParticleLimit)return;
            var obj=ObjectAt("Expanding impact halo",p,parent);var line=Ring(obj,color,radius,.045f);
            obj.AddComponent<CombatTransient>().Init(line,color,life,Vector3.zero,3.5f);
        }
        public static void DamageNumber(Vector3 p,float damage,Color color,Camera camera,Transform parent=null)
        {
            if(camera==null||damage<=0)return;
            if(numbers>=NumberLimit){DamageFloat oldest=null;foreach(var number in Object.FindObjectsByType<DamageFloat>(FindObjectsSortMode.None))if(oldest==null||number.Age>oldest.Age)oldest=number;oldest?.Retire();}
            TotalDamageNumbers++;LastHitPoint=p;LastDamage=damage;
            var obj=ObjectAt("Hit damage",p,parent);
            var text=obj.AddComponent<TextMesh>();
            if(numberFont==null)numberFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font=numberFont;text.GetComponent<MeshRenderer>().sharedMaterial=numberFont.material;
            text.fontSize=48;text.fontStyle=FontStyle.Bold;text.characterSize=.075f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.GetComponent<MeshRenderer>().sortingOrder=20010;
            text.text=Mathf.Max(1,Mathf.RoundToInt(damage)).ToString(CultureInfo.InvariantCulture);
            text.color=Color.Lerp(color,Color.white,.55f);
            text.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            obj.AddComponent<DamageFloat>().Init(text,camera);
        }
        public static void EvadeNumber(Vector3 p,Camera camera,Transform parent=null)
        {
            if(camera==null)return;
            if(numbers>=NumberLimit){DamageFloat oldest=null;foreach(var number in Object.FindObjectsByType<DamageFloat>(FindObjectsSortMode.None))if(oldest==null||number.Age>oldest.Age)oldest=number;oldest?.Retire();}
            var obj=ObjectAt("Avoided hit",p,parent);var text=obj.AddComponent<TextMesh>();
            if(avoidFont==null)avoidFont=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic UI","Meiryo","Segoe UI"},42);
            text.font=avoidFont;text.GetComponent<MeshRenderer>().sharedMaterial=avoidFont.material;text.fontSize=42;text.fontStyle=FontStyle.Bold;text.characterSize=.075f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.text=VaultLanguage.T("DODGED","回避");text.color=new Color(.4f,.94f,.9f);
            text.GetComponent<MeshRenderer>().sortingOrder=20010;text.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;obj.AddComponent<DamageFloat>().Init(text,camera);
        }
    }
    public sealed class BoltVisualMotion : MonoBehaviour
    {
        Shot shot;Transform glyph;
        public void Init(Shot owner,Transform ring){shot=owner;glyph=ring;}
        void LateUpdate()
        {
            if(shot==null)return;
            if(shot.velocity.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(shot.velocity);
            if(glyph!=null){glyph.Rotate(0,220*Time.deltaTime,100*Time.deltaTime);glyph.localScale=Vector3.one*(1+Mathf.Sin(Time.time*16)*.08f);}
        }
    }
    public sealed class CombatTransient : MonoBehaviour
    {
        LineRenderer line;Color color;Vector3 velocity;float life,age,expansion,startWidth,endWidth;bool counted;
        public void Init(LineRenderer renderer,Color tint,float duration,Vector3 movement,float expand)
        {line=renderer;color=tint;life=duration;velocity=movement;expansion=expand;startWidth=line.startWidth;endWidth=line.endWidth;counted=true;CombatVisuals.particles++;}
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/life);
            transform.position+=velocity*Time.deltaTime;if(expansion==0)velocity+=Vector3.down*3*Time.deltaTime;
            float size=expansion>0?Mathf.Lerp(.35f,expansion,t):Mathf.Lerp(1,.2f,t);
            transform.localScale=Vector3.one*size;line.startWidth=startWidth*size;line.endWidth=endWidth*size;
            Color c=color;c.a=color.a*(1-t)*(1-t);line.startColor=line.endColor=c;
            if(age>=life)Destroy(gameObject);
        }
        void OnDestroy(){if(counted)CombatVisuals.particles=Mathf.Max(0,CombatVisuals.particles-1);}
    }
    public sealed class DamageFloat : MonoBehaviour
    {
        TextMesh text;Camera camera;Vector3 start,right,up;Color color;float age,side,lane;bool counted;
        public float Age=>age;
        public void Retire(){if(counted){CombatVisuals.numbers=Mathf.Max(0,CombatVisuals.numbers-1);counted=false;}gameObject.SetActive(false);Destroy(gameObject);}
        public void Init(TextMesh label,Camera view)
        {text=label;camera=view;start=transform.position;right=view.transform.right;up=view.transform.up;color=label.color;lane=CombatVisuals.TotalDamageNumbers%7-3;side=lane*.5f;counted=true;CombatVisuals.numbers++;}
        void LateUpdate()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/.85f);
            float fan=Mathf.SmoothStep(0,1,t/.15f);
            transform.position=start+right*(lane*.25f*fan+side*t)+up*(.25f+2.5f*t-1.2f*t*t+Mathf.Abs(lane%2)*.16f*fan);
            if(camera!=null)transform.rotation=camera.transform.rotation;
            float pop=t<.12f?Mathf.Lerp(.65f,1.35f,t/.12f):Mathf.Lerp(1.35f,.8f,(t-.12f)/.88f);
            transform.localScale=Vector3.one*pop;Color c=color;c.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,t));text.color=c;
            if(t>=1)Destroy(gameObject);
        }
        void OnDestroy(){if(counted)CombatVisuals.numbers=Mathf.Max(0,CombatVisuals.numbers-1);}
    }
    public sealed class WellVisualMotion : MonoBehaviour
    {
        Well well;Transform spiral,edge;
        public void Init(Well owner,Transform accretion,Transform horizon){well=owner;spiral=accretion;edge=horizon;}
        void Update()
        {
            if(well==null||!well.game.Active)return;
            spiral.Rotate(0,-320*Time.deltaTime,0);
            spiral.localScale=Vector3.one*(.9f+Mathf.Sin(Time.time*14)*.06f);
            edge.localScale=Vector3.one*(.97f+Mathf.Sin(Time.time*9)*.03f);
        }
    }
}

