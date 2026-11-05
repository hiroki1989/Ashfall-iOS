using UnityEngine;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string PetAdoptedKey="Vault.Pet.Adopted.v1";
        GameObject petObject;
        Animator petAnimator;
        VaultPetPose petPose;
        float petAttackClock,petIdleClock,petWanderClock;
        Vector3 petFollowOffset=new Vector3(.8f,0,-2.1f),petLastPlayerPosition;
        string petAnimation;
        AudioClip petBarkClip;
        float petNextBarkAt;
        bool petBarkScheduled;
        void TryPetBark()
        {
            if(!petBarkScheduled){petBarkScheduled=true;petNextBarkAt=Time.time+Random.Range(30f,90f);return;}
            if(Time.time<petNextBarkAt)return;
            if(!petBarkClip)petBarkClip=Resources.Load<AudioClip>("UserSFX/pet-bark");
            if(petBarkClip&&audioSource)audioSource.PlayOneShot(petBarkClip,.16f);
            petNextBarkAt=Time.time+Random.Range(45f,75f);
        }
        bool PetAdopted => !smoke&&PlayerPrefs.GetInt(PetAdoptedKey,0)!=0;

        void DrawPetAdoptionConversation()
        {
            string[] ja={"そうだ、犬は好きか？","急にどうした。","行き場のないやつが一匹いる。ここじゃ落ち着けないらしくてな。お前、引き取ってくれないか。","……こっちへ来い。悪くない顔だ。名前はルークにしよう。","ルークか。お前のそばなら、少しは安心できそうだな。"};
            string[] en={"Say, do you like dogs?","Where did that come from?","There's one with nowhere to go. He can't settle in here. Would you take him with you?","...Come here, boy. You've got a good face. I'll call you Rook.","Rook, huh? Maybe he'll finally feel safe with you."};
            var lines=VaultLanguage.Japanese?ja:en;bool playerLine=npcBeat==1||npcBeat==3;
            Box(new Rect(0,0,1440,900),new Color(0,0,0,.76f));
            DrawUnifiedDialogueScene("briggs",playerLine,playerLine?T("KNOX","ノックス"):T("BRIGGS","ブリッグス"),lines[Mathf.Clamp(npcBeat,0,lines.Length-1)],Gold);
            if(npcBeat>=2){var rookPortrait=LoadDialoguePortrait("rook");if(rookPortrait)GUI.DrawTexture(new Rect(604,176,232,232),rookPortrait,ScaleMode.ScaleToFit);}
            if(WorldButton(UnifiedDialoguePrimaryButton,npcBeat>=lines.Length-1?T("CONTINUE","続ける"):T("NEXT","次へ")))
            {
                if(npcBeat<lines.Length-1){npcBeat++;return;}
                PlayerPrefs.SetInt(PetAdoptedKey,1);PlayerPrefs.Save();npcConversation=0;debtPage=true;debtSkillTreePage=false;screen=Screen.Shop;
            }
        }

        void TickPet(float dt)
        {
            if(!PetAdopted||player==null||world==null||!(screen==Screen.Hub||screen==Screen.Playing)||(screen==Screen.Playing&&!Active))return;
            if(!petObject)
            {
                var prefab=Resources.Load<GameObject>("PetDog/GermanShepherd");
                if(!prefab)return;
                petObject=Instantiate(prefab,world);petObject.name="Rook / German Shepherd";
                petObject.transform.position=new Vector3(PlayerPosition.x,0,PlayerPosition.z);
                foreach(var collider in petObject.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                var texture=Resources.Load<Texture2D>("PetDog/GermanShepherdBase");
                foreach(var renderer in petObject.GetComponentsInChildren<Renderer>(true))
                {
                    var mats=renderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)
                    {
                        var shader=Shader.Find("Standard");if(!shader)continue;
                        var mat=new Material(shader);mat.name="Pet / compatible";mat.mainTexture=texture;
                        mat.SetFloat("_Glossiness",.12f);materials.Add(mat);mats[i]=mat;
                    }
                    renderer.sharedMaterials=mats;
                }
                var renderers=petObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                if(renderers.Length>0){Bounds dogBounds=renderers[0].bounds;foreach(var r in renderers)dogBounds.Encapsulate(r.bounds);float height=dogBounds.size.y;float heroHeight=0;foreach(var r in player.GetComponentsInChildren<SkinnedMeshRenderer>())if(r.enabled)heroHeight=Mathf.Max(heroHeight,r.bounds.size.y);float targetHeight=1.5f*Mathf.Max(1.05f,heroHeight*.5f);if(height>.01f)petObject.transform.localScale*=Mathf.Clamp(targetHeight/height,.01f,10f);}
                petAnimator=petObject.GetComponentInChildren<Animator>();if(petAnimator)petAnimator.applyRootMotion=false;
                petPose=petObject.AddComponent<VaultPetPose>();petLastPlayerPosition=PlayerPosition;petAnimation=null;
            }
            Vector3 position=petObject.transform.position;Vector3 hero=PlayerPosition;hero.y=position.y;
            float heroMovement=(hero-petLastPlayerPosition).magnitude;petLastPlayerPosition=hero;
            petIdleClock=heroMovement<.015f?petIdleClock+dt:0;
            petAttackClock=Mathf.Max(0,petAttackClock-dt);petWanderClock-=dt;
            if(petWanderClock<=0&&petIdleClock<8f){petWanderClock=Random.Range(3f,7f);petFollowOffset=new Vector3(Random.Range(-1.6f,1.6f),0,Random.value<.16f?Random.Range(.4f,1.3f):-Random.Range(1.3f,3f));}
            Foe target=null;float nearest=16f*16f;
            if(screen==Screen.Playing)foreach(var enemy in Enemies)
            {
                if(!enemy||enemy.dead)continue;Vector3 d=enemy.transform.position-position;d.y=0;
                if(d.sqrMagnitude<nearest&&(enemy.transform.position-hero).sqrMagnitude<24f*24f){target=enemy;nearest=d.sqrMagnitude;}
            }
            // After ten seconds of player stillness stay seated until combat or movement.
            bool resting=!target&&petIdleClock>=10;
            Vector3 goal=target?target.transform.position:resting?position:hero+player.rotation*petFollowOffset;goal.y=position.y;
            if((hero-position).sqrMagnitude>35f*35f){position=hero;petObject.transform.position=position;}
            Vector3 delta=goal-position;delta.y=0;float distance=delta.magnitude;
            bool approaching=distance>(target?1.25f:.65f)&&!(target==null&&petIdleClock>=10);float speed=distance>3?5.4f:2.5f;
            if(approaching&&distance>.001f)
            {
                Vector3 direction=delta/distance;Vector3 next=position+direction*Mathf.Min(distance,speed*dt);
                if(Layout==null||!Blocked(next,.25f)){petObject.transform.position=next;petObject.transform.rotation=Quaternion.Slerp(petObject.transform.rotation,Quaternion.LookRotation(direction),Mathf.Min(1,dt*7));}
                else if(Layout!=null){direction=Layout.Chase(position,goal);if(direction.sqrMagnitude>.01f){next=position+direction*speed*dt;if(!Blocked(next,.25f))petObject.transform.position=next;}}
            }
            bool bite=target&&distance<=1.55f&&petAttackClock<=0&&(Layout==null||LineOfSight(position,target.transform.position));
            if(bite)
            {
                // Cooldown upgrades increase attack frequency, while damage upgrades affect each bite.
                float pelletHits=1f;if(gun!=null&&gun.pellets>1){float targetDistance=Mathf.Max(2f,Vector3.Distance(hero,target.transform.position));float targetAngle=2f*Mathf.Atan2(.5f,targetDistance)*Mathf.Rad2Deg;pelletHits=Mathf.Max(1f,gun.pellets*Mathf.Clamp01(targetAngle/Mathf.Max(1f,gun.shotgunSpreadDegrees)));}
                float weaponDps=gun!=null?EffectiveDamage*pelletHits*Mathf.Max(.01f,EffectiveFireRate):0;
                target.Hit(weaponDps*.02f*4f*PetDamageMultiplier(),target.transform.position+Vector3.up*.7f,Color.white,100,Color.white);
                petAttackClock=4f*Mathf.Clamp(PetAttackIntervalMultiplier(),.15f,2f);petPose?.Bite();
                TryPetBark();
            }
            if(!bite&&approaching&&speed>4f)TryPetBark();
            bool sitting=!target&&petIdleClock>=10;
            if(petPose)petPose.sitting=sitting;
            string animation=approaching?(speed>4?"1 type_Run Loop":"1 type_Walk_Loop"):(petWanderClock<1.5f&&!sitting?"1 type_Idle_Playing":"1 type_Idle Breathing");
            if(petAnimator&&animation!=petAnimation){petAnimator.CrossFade(animation,.16f);petAnimation=animation;}
        }
    }

    // Procedural gestures supplement the pack, which supplies locomotion but no sit/bite clips.
    sealed class VaultPetPose : MonoBehaviour
    {
        public bool sitting; float biteUntil,tailSeed;
        Transform jaw,leftThigh,rightThigh,leftShin,rightShin,tail;
        Quaternion jawOffset=Quaternion.identity,thighOffset=Quaternion.identity,shinOffset=Quaternion.identity,tailOffset=Quaternion.identity;
        void Awake(){foreach(var t in GetComponentsInChildren<Transform>(true)){switch(t.name){case "DEF-jaw":jaw=t;break;case "DEF-thigh.L":leftThigh=t;break;case "DEF-thigh.R":rightThigh=t;break;case "DEF-shin.L":leftShin=t;break;case "DEF-shin.R":rightShin=t;break;case "DEF-spine.003":tail=t;break;}}tailSeed=Random.Range(0f,50f);}
        static void Undo(Transform bone,Quaternion offset){if(bone)bone.localRotation*=Quaternion.Inverse(offset);}
        // Remove last frame's additive pose before the Animator evaluates the next frame.
        void Update(){Undo(jaw,jawOffset);Undo(leftThigh,thighOffset);Undo(rightThigh,thighOffset);Undo(leftShin,shinOffset);Undo(rightShin,shinOffset);Undo(tail,tailOffset);jawOffset=thighOffset=shinOffset=tailOffset=Quaternion.identity;}
        public void Bite(){biteUntil=Time.time+.5f;}
        void LateUpdate()
        {
            if(sitting){thighOffset=Quaternion.Euler(0,0,35);shinOffset=Quaternion.Euler(0,0,-55);if(leftThigh)leftThigh.localRotation*=thighOffset;if(rightThigh)rightThigh.localRotation*=thighOffset;if(leftShin)leftShin.localRotation*=shinOffset;if(rightShin)rightShin.localRotation*=shinOffset;}
            if(jaw&&Time.time<biteUntil){jawOffset=Quaternion.Euler(0,0,20*Mathf.Sin((biteUntil-Time.time)*Mathf.PI*2));jaw.localRotation*=jawOffset;}
            // FBX hierarchy confirms spine.003 is the base of the four-bone tail chain.
            if(tail&&Mathf.PerlinNoise(Time.time*.12f,tailSeed)>.5f){tailOffset=Quaternion.Euler(0,0,Mathf.Sin(Time.time*7+tailSeed)*12);tail.localRotation*=tailOffset;}
        }
    }
}
