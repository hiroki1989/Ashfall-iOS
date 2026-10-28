using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    // Baked assets contain only model data and this visual driver. AI remains on Foe.
    [DefaultExecutionOrder(600)]
    public sealed class ImportedEnemyVisual : MonoBehaviour
    {
        public AnimationClip idleClip, walkClip, attackClip, deathClip;
        public Transform model;
        public Animator nativeAnimator;
        public float targetHeight=2.16f;
        Transform[] scaleTransforms; Vector3[] authoredScales;
        public Transform raiderRifle;
        public Transform rifleGripSocket,rifleSupportSocket;
        Limb rifleRight,rifleLeft;
        sealed class Finger { public Transform bone; public Quaternion rest; public Vector3 axis; public float curl; }
        readonly List<Finger> fingers=new List<Finger>();
        readonly List<Material> variantMaterials=new List<Material>();
        readonly List<Mesh> fittedMaskMeshes=new List<Mesh>();
        bool handgun;
        Transform handgunMuzzle;
        public Vector3 MuzzlePosition => handgunMuzzle ? handgunMuzzle.position : raiderRifle ? raiderRifle.position+raiderRifle.forward*(handgun?.19f:.72f) : transform.position+Vector3.up*1.2f;
        Foe owner; Transform actor; Vector3 previous, modelPosition; Quaternion modelRotation;
        sealed class Limb
        {
            public Transform upper,lower,end,tip,index,little;
            public Quaternion upperRest,lowerRest,endRest;
            public int side; public bool arm; public Vector3 palmLocal;
        }
        readonly List<Limb> limbs=new List<Limb>();
        float phase, stateTime, attackTime, deathTime=-1; bool wasCharging; string state="";
        public void NotifyShot(){if(deathTime<0)attackTime=.5f;}
        public static bool TryAttach(GameObject actor,int kind)
        {
            if(!actor)return false;
            if(actor.GetComponentInChildren<ImportedEnemyVisual>())return true;
            var prefab=Resources.Load<GameObject>("ImportedEnemies/Enemy_"+kind);
            if(!prefab)return false;
            var instance=Instantiate(prefab,actor.transform,false);
            var driver=instance.GetComponent<ImportedEnemyVisual>();
            if(!driver||!instance.GetComponentInChildren<Renderer>()){Destroy(instance);return false;}
            driver.Bind(actor.transform,actor.GetComponent<Foe>(),-.09f);

            foreach(var renderer in actor.GetComponentsInChildren<Renderer>())
                if(!renderer.transform.IsChildOf(instance.transform))renderer.enabled=false;
            return true;
        }
        public static Transform TryCreateBossNPC(Transform parent,Vector3 position)
        {
            var prefab=Resources.Load<GameObject>("ImportedEnemies/RaiderBossNPC");if(!prefab)return null;
            var instance=Instantiate(prefab,parent,false);instance.transform.position=position;
            var driver=instance.GetComponent<ImportedEnemyVisual>();
            if(!driver){Destroy(instance);return null;}driver.Bind(instance.transform,null,0);return instance.transform;
        }
        void Bind(Transform target,Foe foe,float groundOffset)
        {
            actor=target;owner=foe;previous=actor.position;
            if(transform!=actor)transform.localPosition=Vector3.up*groundOffset;
            if(!model)model=transform.childCount>0?transform.GetChild(0):transform;
            modelPosition=model.localPosition;modelRotation=model.localRotation;
            scaleTransforms=model.GetComponentsInChildren<Transform>(true);
            authoredScales=new Vector3[scaleTransforms.Length];
            for(int i=0;i<scaleTransforms.Length;i++)authoredScales[i]=scaleTransforms[i].localScale;
            foreach(var animator in GetComponentsInChildren<Animator>())animator.enabled=animator==nativeAnimator;
            foreach(var animation in GetComponentsInChildren<Animation>())animation.enabled=false;
            foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(Vector3.zero,Vector3.one*10);}
            if(idleClip&&!idleClip.humanMotion){idleClip.SampleAnimation(model.gameObject,0);RestoreScales();model.localPosition=modelPosition;model.localRotation=modelRotation;}
            CacheLimbs();
            if(owner&&owner.RaiderBoss)BuildRaiderBossRifle();
            if(owner&&owner.kind==10)
            {
                handgun=!owner.UseAssaultRifle;
                ApplyRaiderDesign();
                if(handgun&&raiderRifle)BuildRaiderHandgun();
            }
            if(raiderRifle)
            {
                rifleRight=limbs.Find(l=>l.arm&&l.side>0);rifleLeft=limbs.Find(l=>l.arm&&l.side<0);
                // Keep the rifle independent of IK wrists to avoid a parent/target feedback loop.
                raiderRifle.SetParent(transform,true);
                CacheGripFingers(rifleRight);CacheGripFingers(rifleLeft);
            }
            Animate(0,0);
            NormalizeVisibleModel(targetHeight);
            if((owner&&(owner.kind==10||owner.RaiderBoss))||(!owner&&name.Contains("RaiderBossNPC"))) AttachRaiderMask();
            // Normalization becomes the persistent scale, including during sampled animation.
            for(int i=0;i<scaleTransforms.Length;i++)authoredScales[i]=scaleTransforms[i].localScale;
        }
        void AttachRaiderMask()
        {
            Transform head=null;foreach(var bone in model.GetComponentsInChildren<Transform>(true))if(bone.name=="Head_M"||bone.name=="head"){head=bone;break;}
            if(!head){Debug.LogWarning("Raider head bone missing: "+name);return;}
            var socket=new GameObject("Raider respirator socket").transform;socket.SetParent(head,true);socket.position=head.position;socket.rotation=actor.rotation;socket.localScale=Vector3.one;
            var mask=GeneratedWardenPreviewAssets.CreateFittedMask(socket);if(!mask){Destroy(socket.gameObject);return;}
            bool boss=owner&&owner.RaiderBoss||!owner&&name.Contains("RaiderBossNPC");
            socket.position+=actor.up*(targetHeight*(boss?-.018f:-.012f))+actor.forward*(targetHeight*(boss?.008f:.006f));
            // Stretch upward from the chin so the lenses reach the eyes without uncovering the mouth.
            float chin=float.PositiveInfinity;
            foreach(var f in mask.GetComponentsInChildren<MeshFilter>(true))
                if(f.sharedMesh)foreach(var v in f.sharedMesh.vertices)chin=Mathf.Min(chin,actor.InverseTransformPoint(f.transform.TransformPoint(v)).y);
            // Fit the respirator depth independently of its eye-to-mouth spacing.
            // The source accessory includes long rigid straps authored for another head.
            foreach(var filter in mask.GetComponentsInChildren<MeshFilter>(true)){
                if(!filter.sharedMesh)continue;
                var mesh=Instantiate(filter.sharedMesh);mesh.name="Raider fitted respirator";fittedMaskMeshes.Add(mesh);
                var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++){
                    var point=actor.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));
                    var origin=actor.InverseTransformPoint(head.position);
                    point.y=chin+(point.y-chin)*(boss?1.48f:1.45f);
                    point.x*=.94f;
                    // Wrap rear strap vertices against the sides of the head, retaining
                    // the original mask front and texture coordinates.
                    float back=Mathf.Clamp01((origin.z+.035f-point.z)/.14f);
                    point.x=Mathf.Lerp(point.x,Mathf.Clamp(point.x,-.115f,.115f),back);
                    vertices[i]=filter.transform.InverseTransformPoint(actor.TransformPoint(point));
                }
                mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();filter.sharedMesh=mesh;
            }
        }
        void RestoreScales()
        {
            // Pack clips carry export-unit/root scale curves; they must not undo metre normalization.
            for(int i=0;i<scaleTransforms.Length;i++)if(scaleTransforms[i])scaleTransforms[i].localScale=authoredScales[i];
        }
        void NormalizeVisibleModel(float height)
        {
            // Imported skin bounds can include detached equipment and stale bind poses.
            // Normalize every species from pose-independent limb lengths, after animation initialization.
            var leftLeg=limbs.Find(l=>!l.arm&&l.side<0);var rightLeg=limbs.Find(l=>!l.arm&&l.side>0);
            var leftArm=limbs.Find(l=>l.arm&&l.side<0);var rightArm=limbs.Find(l=>l.arm&&l.side>0);
            if(leftLeg==null||rightLeg==null||leftArm==null||rightArm==null)return;
            Vector3 feet=(leftLeg.end.position+rightLeg.end.position)*.5f;
            float legs=(Vector3.Distance(leftLeg.upper.position,leftLeg.lower.position)+Vector3.Distance(leftLeg.lower.position,leftLeg.end.position)+Vector3.Distance(rightLeg.upper.position,rightLeg.lower.position)+Vector3.Distance(rightLeg.lower.position,rightLeg.end.position))*.5f;
            var hips=(leftLeg.upper.position+rightLeg.upper.position)*.5f;var shoulders=(leftArm.upper.position+rightArm.upper.position)*.5f;float span=legs+Vector3.Distance(hips,shoulders);if(span<.01f)return;
            float scale=height*.72f/span;Vector3 pivot=model.position;
            model.localScale*=scale;feet=pivot+(feet-pivot)*scale;
            model.position+=new Vector3(actor.position.x-feet.x,transform.position.y+height*.055f-feet.y,actor.position.z-feet.z);
            modelPosition=model.localPosition;
        }
        void Start(){if(actor&&!owner)owner=actor.GetComponent<Foe>();}
        float animationAccumulator;
        void LateUpdate()
        {
            if(!actor)return;animationAccumulator+=Time.deltaTime;
            float interval=owner&&owner.game&&(actor.position-owner.game.PlayerPosition).sqrMagnitude>625?.10f:Application.isMobilePlatform?.033f:.016f;
            if(animationAccumulator<interval)return;float dt=animationAccumulator;animationAccumulator=0;
            Vector3 delta=actor.position-previous;previous=actor.position;delta.y=0;
            if(owner&&owner.game&&!owner.game.Active)return;
            if(owner&&owner.dead&&deathTime<0)deathTime=0;
            if(owner){bool charging=owner.IsTelegraphing;if(wasCharging&&!charging&&!owner.dead)attackTime=.5f;wasCharging=charging;}
            if(deathTime>=0)deathTime+=dt;attackTime=Mathf.Max(0,attackTime-dt);
            Animate(dt,dt>0?delta.magnitude/dt:0);
        }
        void Animate(float dt,float speed)
        {
            int kind=owner?owner.kind:11;
            bool heavy=kind==3||kind==8||(owner&&owner.HeavyMelee);
            phase+=dt*Mathf.Max(1,speed*(heavy?1.85f:kind==4?3.6f:2.8f));
            string next=deathTime>=0?"death":attackTime>0?"attack":speed>.12f?"walk":"idle";
            if(state!=next){state=next;stateTime=0;}stateTime+=dt;
            AnimationClip clip=state=="death"?deathClip:state=="attack"?attackClip:state=="walk"?walkClip:idleClip;
            if(clip&&clip.humanMotion&&nativeAnimator&&nativeAnimator.runtimeAnimatorController)
            {
                nativeAnimator.enabled=true;
                float normalized=stateTime/Mathf.Max(.01f,clip.length);
                if(state=="death"||state=="attack")normalized=Mathf.Min(normalized,.999f);
                nativeAnimator.Play(state,0,normalized);nativeAnimator.Update(0);RestoreScales();
                model.localPosition=modelPosition;model.localRotation=modelRotation;ApplySecondaryMotion(speed,kind);PoseFirearm(speed);return;
            }
            if(nativeAnimator)nativeAnimator.enabled=false;
            // Generic clips preserve authored bone paths.
            if(clip&&!clip.humanMotion)
            {
                float time=state=="death"||state=="attack"?Mathf.Min(stateTime,Mathf.Max(0,clip.length-.001f)):Mathf.Repeat(stateTime,Mathf.Max(.01f,clip.length));
                clip.SampleAnimation(model.gameObject,time);RestoreScales();model.localPosition=modelPosition;model.localRotation=modelRotation;ApplySecondaryMotion(speed,kind);PoseFirearm(speed);return;
            }
            model.localPosition=modelPosition;
            model.localRotation=modelRotation;
            foreach(var limb in limbs)
            {
                limb.upper.localRotation=limb.upperRest;
                limb.lower.localRotation=limb.lowerRest;
                if(limb.end)limb.end.localRotation=limb.endRest;
            }
            foreach(var limb in limbs)
            {
                float stride=Mathf.Sin(phase+limb.side*Mathf.PI*.5f)*Mathf.Clamp01(speed/1.5f);
                float strike=attackTime>0?Mathf.Sin((1-attackTime/.5f)*Mathf.PI):0;
                float prepare=owner&&owner.IsTelegraphing?1:0;
                float dying=deathTime>=0?Mathf.SmoothStep(0,1,deathTime/(heavy?1.05f:.8f)):0;
                Vector3 upperDirection,lowerDirection;
                if(limb.arm)
                {
                    upperDirection=new Vector3(limb.side*.16f,-.94f,.22f+stride*.23f+strike*1.4f);
                    lowerDirection=new Vector3(-limb.side*.08f,-.73f,.48f-stride*.12f+strike*1.25f);
                    // Different silhouettes: dangling corpse, low pouncer, broad heavy, and guarded caster.
                    if(kind==0){upperDirection=new Vector3(limb.side*.19f,-.95f,.3f-stride*.25f+strike);lowerDirection=new Vector3(0,-.72f,.58f-stride*.14f+strike*.7f);}
                    else if(kind==4){upperDirection=new Vector3(limb.side*.36f,-.62f,.6f-stride*.25f+strike);lowerDirection=new Vector3(limb.side*.15f,-.45f,.85f);}
                    else if(heavy){upperDirection=new Vector3(limb.side*.32f,-.94f,.12f-stride*.35f+strike*.95f-prepare*.35f);lowerDirection=new Vector3(-limb.side*.12f,-.8f,.4f-stride*.20f+strike*.85f);}
                    else if(kind==5||kind==10||owner&&owner.RaiderBoss&&owner.BossAttackStyle==3){upperDirection=new Vector3(limb.side*.24f,-.82f,.38f+strike*.35f);lowerDirection=new Vector3(-limb.side*.1f,-.25f,.95f+prepare*.2f);}
                    if(kind==3&&owner&&owner.BossAttackStyle==1){upperDirection=Vector3.Lerp(upperDirection,new Vector3(limb.side*.3f,.8f,.4f),prepare);lowerDirection=Vector3.Lerp(lowerDirection,new Vector3(0,.6f,.8f),prepare);}
                    if(kind==3&&owner&&owner.BossAttackStyle==2){upperDirection=new Vector3(limb.side*(.45f+prepare*.6f),-.25f,.4f+strike);lowerDirection=new Vector3(-limb.side*.65f*strike,-.2f,.9f);}
                    upperDirection=Vector3.Lerp(upperDirection,new Vector3(limb.side*.5f,-.75f,.25f),dying);
                    lowerDirection=Vector3.Lerp(lowerDirection,new Vector3(limb.side*.2f,-.5f,.8f),dying);
                }
                else
                {
                    if(speed<.1f&&dying<=0)continue;
                    upperDirection=new Vector3(limb.side*.025f,-1,stride*.35f);
                    lowerDirection=new Vector3(0,-1,-Mathf.Max(0,-stride)*(heavy?.25f:.42f));
                    upperDirection.x=limb.side*(heavy?.11f:kind==4?.08f:.025f);
                    upperDirection.z*=heavy?.65f:kind==4?1.3f:1;
                    upperDirection=Vector3.Lerp(upperDirection,new Vector3(limb.side*.12f,-1,-.1f),dying);
                    lowerDirection=Vector3.Lerp(lowerDirection,new Vector3(0,-1,.16f),dying);
                }
                Aim(limb.upper,limb.lower,actor.TransformDirection(upperDirection));
                Aim(limb.lower,limb.end,actor.TransformDirection(lowerDirection));
                if(limb.arm&&limb.end&&limb.tip)
                    Aim(limb.end,limb.tip,actor.TransformDirection(new Vector3(0,-.8f,.5f)));
            }
            PoseFirearm(speed);
            ApplySecondaryMotion(speed,kind);
            if(deathTime>=0)ApplyFallbackCollapse(heavy,kind);
        }
        void PoseFirearm(float speed)
        {
            if(owner&&owner.RaiderBoss&&raiderRifle)
            {
                bool firing=owner.BossAttackStyle==3&&(owner.IsTelegraphing||attackTime>0);
                raiderRifle.gameObject.SetActive(firing);
                if(!firing)return;
            }
            if(raiderRifle&&deathTime<0)
            {
                if(rifleRight!=null&&rifleLeft!=null)
                {
                    // The weapon stays on its own root; wrist IK follows its two real contacts.
                    // Telegraphing settles the shoulder, while each shot kicks the muzzle briefly.
                    float aiming=owner&&owner.IsTelegraphing?1:0;
                    float recoil=attackTime>0?Mathf.Sin((1-attackTime/.5f)*Mathf.PI):0;
                    float moving=Mathf.Clamp01(speed/2f);
                    float shoulders=(rifleRight.upper.position.y+rifleLeft.upper.position.y)*.5f;
                    Vector3 grip=actor.position+actor.right*(handgun?.19f:.12f)+actor.forward*(handgun?.29f:.12f)-(handgun?actor.forward*.07f:actor.forward*.045f)*recoil;
                    grip.y=shoulders-(handgun?.12f:.23f)+.055f*aiming-.025f*moving;
                    raiderRifle.rotation=Quaternion.LookRotation((actor.forward+actor.up*(.025f*aiming+(handgun?.28f:.16f)*recoil-.12f*moving)).normalized,actor.up);
                    raiderRifle.position=grip;
                    if(rifleGripSocket)raiderRifle.position+=grip-rifleGripSocket.position;
                    Vector3 support=rifleSupportSocket?rifleSupportSocket.position:grip+actor.forward*.30f+actor.up*.06f;
                    HoldRifleHand(rifleRight,grip,false);
                    if(!handgun)HoldRifleHand(rifleLeft,support,true);
                    foreach(var finger in fingers)finger.bone.localRotation=finger.rest*Quaternion.AngleAxis(finger.curl,finger.axis);
                }
            }
        }
        void ApplyRaiderDesign()
        {
            // The supplied Urban Insurgent is the only licensed source design in this project.
            // Three battlefield loadouts vary cloth/armor without modifying the source material.
            int seed=Mathf.Abs(Mathf.RoundToInt(actor.position.x*17+actor.position.z*31));
            int design=seed%3;
            Color[] cloth={new Color(.85f,.87f,.82f),new Color(.64f,.78f,.67f),new Color(.76f,.68f,.58f)};
            Color tint=cloth[design];
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if(raiderRifle&&renderer.transform.IsChildOf(raiderRifle))continue;
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)if(mats[i])
                {
                    var copy=new Material(mats[i]);copy.name=mats[i].name+" / raider loadout "+design;
                    copy.color=Color.Lerp(mats[i].color,tint,design==0?.08f:.42f);
                    variantMaterials.Add(copy);mats[i]=copy;
                }
                renderer.sharedMaterials=mats;
            }
            BuildRaiderLoadout(design);
        }
        void BuildRaiderLoadout(int design)
        {
            // Distinct silhouettes using the shipped character as a common rig: scout cap,
            // armored assault helmet, or heavy breacher pack. Parts are purely visual.
            Material cloth=AccessoryMaterial("Raider canvas",design==0?new Color(.28f,.31f,.25f):design==1?new Color(.20f,.27f,.22f):new Color(.36f,.29f,.20f));
            Material armor=AccessoryMaterial("Raider plates",design==2?new Color(.22f,.21f,.18f):new Color(.18f,.22f,.23f));
            if(design==0)
            {
                Accessory("Soft patrol cap",PrimitiveType.Sphere,new Vector3(0,1.87f,0),new Vector3(.36f,.12f,.35f),cloth);
                Accessory("Cap brim",PrimitiveType.Cube,new Vector3(0,1.83f,.17f),new Vector3(.34f,.035f,.21f),cloth);
                Accessory("Scout chest harness",PrimitiveType.Cube,new Vector3(0,1.30f,.18f),new Vector3(.31f,.42f,.07f),cloth);
                Accessory("Scout side satchel",PrimitiveType.Cube,new Vector3(-.31f,.96f,-.08f),new Vector3(.23f,.31f,.17f),cloth);
            }
            else if(design==1)
            {
                Accessory("Armored helmet",PrimitiveType.Sphere,new Vector3(0,1.91f,0),new Vector3(.39f,.25f,.39f),armor);
                Accessory("Helmet brow",PrimitiveType.Cube,new Vector3(0,1.81f,.19f),new Vector3(.39f,.08f,.13f),armor);
                Accessory("Assault chest plate",PrimitiveType.Cube,new Vector3(0,1.34f,.19f),new Vector3(.48f,.49f,.10f),armor);
                Accessory("Assault left shoulder",PrimitiveType.Sphere,new Vector3(-.31f,1.49f,0),new Vector3(.25f,.17f,.30f),armor);
                Accessory("Assault right shoulder",PrimitiveType.Sphere,new Vector3(.31f,1.49f,0),new Vector3(.25f,.17f,.30f),armor);
            }
            else
            {
                Accessory("Breacher hood",PrimitiveType.Sphere,new Vector3(0,1.83f,-.04f),new Vector3(.42f,.36f,.39f),cloth);
                Accessory("Breacher face plate",PrimitiveType.Cube,new Vector3(0,1.76f,.19f),new Vector3(.33f,.21f,.075f),armor);
                Accessory("Heavy chest rig",PrimitiveType.Cube,new Vector3(0,1.32f,.22f),new Vector3(.53f,.54f,.14f),armor);
                Accessory("Radio backpack",PrimitiveType.Cube,new Vector3(0,1.25f,-.26f),new Vector3(.58f,.69f,.24f),cloth);
                Accessory("Radio aerial",PrimitiveType.Cylinder,new Vector3(.22f,1.75f,-.29f),new Vector3(.025f,.38f,.025f),armor);
                Accessory("Breacher left shoulder",PrimitiveType.Sphere,new Vector3(-.33f,1.51f,-.01f),new Vector3(.29f,.23f,.34f),armor);
                Accessory("Breacher right shoulder",PrimitiveType.Sphere,new Vector3(.33f,1.51f,-.01f),new Vector3(.29f,.23f,.34f),armor);
            }
        }
        Material AccessoryMaterial(string name,Color color)
        {
            var shader=Shader.Find("Standard");
            var material=new Material(shader){name=name,color=color};
            material.SetFloat("_Glossiness",.12f);
            variantMaterials.Add(material);
            return material;
        }
        void Accessory(string name,PrimitiveType shape,Vector3 position,Vector3 size,Material material)
        {
            if(name=="Soft patrol cap"||name=="Cap brim"||name=="Armored helmet"||name=="Helmet brow"||name.Contains("shoulder"))return;
            var part=GameObject.CreatePrimitive(shape);part.name=name;
            part.transform.SetParent(transform,false);part.transform.localPosition=position;
            part.transform.localScale=size;
            var collider=part.GetComponent<Collider>();if(collider){collider.enabled=false;Destroy(collider);}
            part.GetComponent<Renderer>().sharedMaterial=material;
        }
        void BuildRaiderHandgun()
        {
            // Reuse the game's modeled sidearm and its authored trigger/muzzle sockets.
            var rifle=raiderRifle;
            Material steel=null;
            foreach(var renderer in rifle.GetComponentsInChildren<Renderer>(true))
                if(renderer.sharedMaterial){steel=renderer.sharedMaterial;break;}
            if(!steel)return;
            rifle.gameObject.SetActive(false);
            var sockets=DetailedHeroEquipment3D.BuildPistol(transform,key=>steel);
            raiderRifle=sockets.root;rifleGripSocket=sockets.trigger;rifleSupportSocket=sockets.support;handgunMuzzle=sockets.muzzle;
        }
        void BuildRaiderBossRifle()
        {
            // The imported boss has no authored weapon. Use modeled geometry with real
            // trigger, support and muzzle sockets so animation and projectiles agree.
            Material steel=AccessoryMaterial("Boss rifle steel",new Color(.27f,.31f,.33f));
            Material wood=AccessoryMaterial("Boss rifle stock",new Color(.24f,.18f,.13f));
            var sockets=DetailedHeroEquipment3D.BuildRifle(transform,key=>key=="Wood"?wood:steel);
            raiderRifle=sockets.root;rifleGripSocket=sockets.trigger;
            rifleSupportSocket=sockets.support;handgunMuzzle=sockets.muzzle;
        }
        void OnDestroy()
        {
            foreach(var material in variantMaterials)if(material)Destroy(material);
            foreach(var mesh in fittedMaskMeshes)if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
        }
        void ApplySecondaryMotion(float speed,int kind)
        {
            // Native clips retain their authored gait and death. Add only small whole-body weight shifts.
            if(deathTime>=0)return;
            bool heavy=kind==3||kind==8||(owner&&owner.HeavyMelee);
            float moving=Mathf.Clamp01(speed/1.5f),stride=Mathf.Sin(phase);
            float anticipation=owner&&owner.IsTelegraphing?1:0;
            float strike=attackTime>0?Mathf.Sin((1-attackTime/.5f)*Mathf.PI):0;
            float roll=stride*moving*(heavy?2.1f:kind==0?2.8f:1.1f);
            float pitch=kind==4?moving*7:kind==0?3:heavy?1.5f:0;
            pitch+=anticipation*(kind==4?9:heavy?-5:-2)+strike*(heavy?8:4);
            model.localRotation=modelRotation*Quaternion.Euler(pitch,0,roll);
            // No vertical bounce: the normalized foot contact and actor/root separation remain intact.
        }
        void ApplyFallbackCollapse(bool heavy,int kind)
        {
            float duration=heavy?1.05f:.8f;
            float buckle=Mathf.SmoothStep(0,1,deathTime/(duration*.45f));
            float fall=Mathf.SmoothStep(0,1,(deathTime-duration*.18f)/(duration*.82f));
            var left=limbs.Find(l=>!l.arm&&l.side<0);var right=limbs.Find(l=>!l.arm&&l.side>0);
            if(left==null||right==null)return;
            // Pivot at the pelvis so the body loses support before hitting the ground, rather than
            // rolling a rigid standing model around its arbitrary imported origin.
            Vector3 pelvis=(left.upper.position+right.upper.position)*.5f;
            float ground=transform.position.y;
            float side=kind==0||kind==7?-1:1;
            Quaternion tilt=Quaternion.AngleAxis((kind==4?88:100)*fall,actor.right)*Quaternion.AngleAxis(side*16*fall,actor.forward);
            model.position=pelvis+tilt*(model.position-pelvis);
            model.rotation=tilt*model.rotation;
            float hipHeight=Mathf.Lerp(pelvis.y,ground+targetHeight*(heavy?.115f:.09f),buckle);
            model.position+=Vector3.up*(hipHeight-pelvis.y);
        }
        void CacheGripFingers(Limb hand)
        {
            if(hand==null)return;
            Vector3 palmForward=hand.tip?(hand.tip.position-hand.end.position).normalized:hand.end.forward;
            Transform index=null,little=null;
            foreach(var bone in hand.end.GetComponentsInChildren<Transform>())
            {
                string name=bone.name.ToLowerInvariant();
                if(name.Contains("indexfinger1"))index=bone;
                if(name.Contains("pinkyfinger1"))little=bone;
            }
            hand.index=index;hand.little=little;
            Vector3 palm=hand.tip?hand.tip.position:hand.end.position+hand.end.forward*.07f;
            if(index&&little)palm=(palm+index.position+little.position)/3;
            // Centre of the native palm, measured from its actual metacarpal joints.
            hand.palmLocal=hand.end.InverseTransformPoint(Vector3.Lerp(hand.end.position,palm,.72f));
            Vector3 palmAcross=index&&little?(index.position-little.position).normalized:hand.end.right;
            Vector3 palmNormal=Vector3.Cross(palmForward,palmAcross).normalized*hand.side;
            foreach(var bone in hand.end.GetComponentsInChildren<Transform>())
            {
                string name=bone.name.ToLowerInvariant();
                if(!name.Contains("finger")||bone.childCount==0||name.Contains("end"))continue;
                Vector3 direction=bone.GetChild(0).position-bone.position;
                Vector3 curlAxis=Vector3.Cross(direction,-palmNormal).normalized;
                bool thumb=name.Contains("thumb"),indexFinger=name.Contains("index");
                float curl=thumb?32:indexFinger&&hand.side>0?35:62;
                fingers.Add(new Finger{bone=bone,rest=bone.localRotation,axis=bone.InverseTransformDirection(curlAxis),curl=curl});
            }
        }
        void HoldRifleHand(Limb hand,Vector3 contact,bool support)
        {
            Vector3 forward=raiderRifle.forward,up=raiderRifle.up;
            for(int pass=0;pass<3;pass++)
            {
                Vector3 offset=hand.end.TransformPoint(hand.palmLocal)-hand.end.position;
                SolveArm(hand,contact-offset,actor.right*hand.side*.35f-actor.up*.7f);
                if(!hand.tip)continue;
                Vector3 along=support?forward:forward*.15f-up;
                Aim(hand.end,hand.tip,along);
                Transform index=hand.index,little=hand.little;
                if(index&&little)
                {
                    Vector3 axis=along.normalized;
                    Vector3 current=Vector3.ProjectOnPlane(index.position-little.position,axis);
                    Vector3 desired=Vector3.ProjectOnPlane(support?raiderRifle.right:-raiderRifle.right,axis);
                    if(current.sqrMagnitude>.00001f&&desired.sqrMagnitude>.00001f)
                        hand.end.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(current,desired,axis),axis)*hand.end.rotation;
                }
            }
            // Final positional solve uses the finished wrist orientation, not an assumed 7.5cm offset.
            Quaternion handRotation=hand.end.rotation;
            Vector3 finalOffset=hand.end.TransformPoint(hand.palmLocal)-hand.end.position;
            SolveArm(hand,contact-finalOffset,actor.right*hand.side*.35f-actor.up*.7f);
            hand.end.rotation=handRotation;
        }

        void CacheLimbs()
        {
            Animator human=null;
            foreach(var animator in model.GetComponentsInChildren<Animator>())
                if(animator.avatar&&animator.avatar.isValid&&animator.avatar.isHuman){human=animator;break;}
            foreach(int side in new[]{-1,1})
            {
                bool left=side<0;
                AddLimb(side,true,
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm):FindBone(side,"upperarm","arm","shoulder"),
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm):FindBone(side,"lowerarm","forearm","elbow"),
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand):FindBone(side,"hand","wrist"),
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal):FindBone(side,"middlefinger1","middle01","handmiddle1"));
                AddLimb(side,false,
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg):FindBone(side,"thigh","upleg","hip"),
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg):FindBone(side,"calf","leg","knee"),
                    human?human.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot):FindBone(side,"foot","ankle"),null);
            }
        }
        void AddLimb(int side,bool arm,Transform upper,Transform lower,Transform end,Transform tip)
        {
            if(!upper||!lower||!end||!lower.IsChildOf(upper)||!end.IsChildOf(lower))return;
            limbs.Add(new Limb{side=side,arm=arm,upper=upper,lower=lower,end=end,tip=tip,
                upperRest=upper.localRotation,lowerRest=lower.localRotation,endRest=end.localRotation});
        }
        Transform FindBone(int side,params string[] tokens)
        {
            var all=model.GetComponentsInChildren<Transform>();
            foreach(string token in tokens)
                foreach(var candidate in all)
                {
                    string n=candidate.name.ToLowerInvariant();
                    if(n.Contains("ctrl")||n.Contains("effector")||n.Contains("twist")||n.Contains("clavicle")||n.Contains("part"))continue;
                    bool correctSide=side<0?(n.Contains("left")||n.EndsWith("_l")):(n.Contains("right")||n.EndsWith("_r"));
                    if(!correctSide)continue;
                    string canonical=n.Replace("_","").Replace("left","").Replace("right","");
                    if(n.EndsWith("_l")||n.EndsWith("_r"))canonical=canonical.Substring(0,canonical.Length-1);
                    if(token=="leg"&&canonical.Contains("upleg"))continue;
                    if(canonical.EndsWith(token))return candidate;
                }
            return null;
        }
        static void SolveArm(Limb limb,Vector3 target,Vector3 elbowBias)
        {
            Vector3 shoulder=limb.upper.position;
            float upperLength=Vector3.Distance(shoulder,limb.lower.position),lowerLength=Vector3.Distance(limb.lower.position,limb.end.position);
            Vector3 delta=target-shoulder;float distance=Mathf.Clamp(delta.magnitude,.001f,Mathf.Max(.002f,upperLength+lowerLength-.002f));
            Vector3 direction=delta.normalized;
            float along=(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance);
            float away=Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
            Vector3 bend=Vector3.ProjectOnPlane(elbowBias,direction).normalized;
            Vector3 elbow=shoulder+direction*along+bend*away;
            Aim(limb.upper,limb.lower,elbow-shoulder);
            Aim(limb.lower,limb.end,target-limb.lower.position);
            if(limb.tip)Aim(limb.end,limb.tip,direction);
        }
        // Explicit QA snapshot only; never allocates meshes in the frame loop.
        public string DiagnosticReport()
        {
            var report=new System.Text.StringBuilder();
            report.AppendLine("state="+state+" actor="+(actor?actor.position.ToString("F4"):"null")+" root="+transform.position.ToString("F4")+" model="+(model?model.position.ToString("F4"):"null"));
            foreach(var limb in limbs)
                report.AppendLine((limb.arm?"arm":"leg")+limb.side+" "+limb.upper.name+"="+limb.upper.position.ToString("F4")+" "+limb.lower.name+"="+limb.lower.position.ToString("F4")+" "+limb.end.name+"="+limb.end.position.ToString("F4"));
            foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();
                try
                {
                    skin.BakeMesh(mesh);
                    Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),max=-min;
                    foreach(var vertex in mesh.vertices){Vector3 world=skin.transform.TransformPoint(vertex);min=Vector3.Min(min,world);max=Vector3.Max(max,world);}
                    report.AppendLine(skin.name+" enabled="+skin.enabled+" vertices="+mesh.vertexCount+" worldMin="+min.ToString("F4")+" worldMax="+max.ToString("F4")+" lossyScale="+skin.transform.lossyScale.ToString("F4"));
                    foreach(var material in skin.sharedMaterials)if(material)report.AppendLine(" material="+material.name+" shader="+material.shader.name+" texture="+(material.mainTexture?material.mainTexture.name:"null"));
                }
                finally{Destroy(mesh);}
            }
            return report.ToString();
        }
        static void Aim(Transform bone,Transform child,Vector3 worldDirection)
        {
            if(!bone||!child)return;
            Vector3 current=child.position-bone.position;
            if(current.sqrMagnitude>.0000001f&&worldDirection.sqrMagnitude>.0001f)
                bone.rotation=Quaternion.FromToRotation(current,worldDirection)*bone.rotation;

        }
    }
}
