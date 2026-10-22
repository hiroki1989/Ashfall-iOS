using UnityEngine;
namespace NeonVault
{
    [DefaultExecutionOrder(300)]
    public sealed class DetailedHeroPlayer3D : MonoBehaviour
    {
        public DetailedHero3D body;
        DetailedHeroArsenal3D.ArmorVisuals equipment;
        DetailedHeroEquipment3D.WeaponSockets current;
        string weaponKey;
        string failedWeaponKey;
        GameObject generatedLookRoot,generatedLookWeapon,generatedLookMask;

        SkinnedMeshRenderer generatedArmorSkin,generatedHeadSkin;
        SkinnedMeshRenderer commonBaseSkin;
        Renderer[] oldBaseRenderers;
        SkinnedMeshRenderer[] generatedArmorParts=new SkinnedMeshRenderer[5];
        bool generatedHasArms,generatedHasLegs;
        Renderer[] baseLookRenderers;
        GameObject[] generatedJointBridges;
        Material[][] baseLookMaterials;bool? generatedLimbPalette;
        bool? filteredWardenArmor;
        string generatedLookWeaponKey;
        GeneratedProductWeapon productWeapon;
        readonly SharedArmorTextures sharedArmorTextures=new SharedArmorTextures();
        public Transform EquippedWeaponRoot=>current?.root;
        public bool IsGeneratedLookActive=>generatedLookRoot&&generatedLookRoot.activeSelf;
        public int GeneratedArmorSlotTriangleCount(ArmorSlot slot)
        {
            int index=(int)slot;
            return generatedArmorParts!=null&&index>=0&&index<generatedArmorParts.Length
                &&generatedArmorParts[index]&&generatedArmorParts[index].sharedMesh
                ?generatedArmorParts[index].sharedMesh.triangles.Length/3:0;
        }
        public int GeneratedLegacyOverlapCount=>GeneratedWardenRig.CountVisibleLegacy(body,generatedHeadSkin,generatedArmorSkin,generatedLookMask);
        Transform meleeVisual; string meleeKey;float meleeUntil;
        public void NotifyMelee(){meleeUntil=Time.time+.28f;}
        AimedWeapon weapon;
        VaultGame game;
        Vector3 previous;
        float phase, moveBlend;
        Vector3 velocity;
        float firingUntil;
        public void NotifyFire(){firingUntil=Time.time+.18f;Synchronize();}
        public static DetailedHeroPlayer3D Create(Transform actor)
        {
            var driver=actor.gameObject.AddComponent<DetailedHeroPlayer3D>();
            actor.localScale=Vector3.one*1.28f;
            actor.localPosition-=Vector3.up*.62f;
            driver.body=DetailedHero3D.Create(actor,DetailedHeroMaterials3D.Get);
            driver.oldBaseRenderers=driver.body.GetComponentsInChildren<Renderer>(true);
            driver.commonBaseSkin=GeneratedCommonBodyRig.Create(driver.body);
            driver.SuppressOldBase();
            driver.equipment=DetailedHeroArsenal3D.ApplyArmor(driver.body,null);
            driver.previous=actor.position;
            return driver;
        }
        public void ApplyArmor(ArmorItem[] armor)
        {
            var slots=GeneratedEquipmentAssetCatalog.WardenSlots(armor);
            bool filter=GeneratedWardenPreviewAssets.Enabled&&GeneratedEquipmentAssetCatalog.HasWardenSlot(slots);
            var ordinary=filter?GeneratedEquipmentAssetCatalog.WithoutWardenSlots(armor,slots):GeneratedEquipmentAssetCatalog.WithoutHeadArmor(armor);
            GeneratedWardenRig.RestoreBaseAnatomy(body);
            equipment.SetItems(ordinary);ManufacturerEquipment3D.ApplyArmor(body,ordinary);
            filteredWardenArmor=filter;
            if(filter&&IsGeneratedLookActive)GeneratedWardenRig.SetSlotGeometryVisible(body,slots,generatedHeadSkin,generatedArmorSkin,
                generatedLookMask,GeneratedEquipmentAssetCatalog.ShowWardenScanFeet(armor,slots));
            SuppressOldBase();
        }
        public void Synchronize()
        {
            if(!body)return;
            if(!game)game=FindAnyObjectByType<VaultGame>();
            if(!weapon)weapon=GetComponentInChildren<AimedWeapon>();
            if(!weapon)return;
            bool handgun=game&&game.gun!=null&&Arsenal.IsOneHanded(VaultLanguage.DisplayModel(game.gun));
            bool firing=Time.time<firingUntil||(game&&game.Active&&VaultMobileInputBridge.Firing&&game.reload<=0);
            string key=ManufacturerEquipment3D.WeaponKey(game?game.gun:null);
            if(!ValidSockets(current)||(weaponKey!=key&&failedWeaponKey!=key))ReplaceWeapon(key);
            if(!ValidSockets(current))return;
            // Bind the scan mesh against the skeleton's idle/rest pose before applying the
            // current aim pose. Binding on the first aimed frame bakes the arm rotation into
            // its bind matrices, which makes the sleeves flip upward on subsequent directions.
            bool[] wardenSlots=GeneratedEquipmentAssetCatalog.WardenSlots(game?game.EquippedArmor:null);
            if(GeneratedWardenPreviewAssets.Enabled&&GeneratedEquipmentAssetCatalog.HasWardenSlot(wardenSlots)&&!generatedLookRoot)BuildGeneratedLook();
            Pose(body,current,handgun,firing,weapon.Direction,phase,velocity,moveBlend);
            if(game&&game.EquippedMeleeWeapon!=null)
            {
                var item=game.EquippedMeleeWeapon;string nextMeleeKey=ManufacturerEquipment3D.MeleeKey(item);
                if(!meleeVisual||nextMeleeKey!=meleeKey){if(meleeVisual){meleeVisual.gameObject.SetActive(false);Destroy(meleeVisual.gameObject);}meleeVisual=ManufacturerEquipment3D.BuildMelee(body.rightHand,item);meleeKey=ManufacturerEquipment3D.MeleeKey(item);}
                bool swinging=Time.time<meleeUntil;meleeVisual.gameObject.SetActive(swinging);current.root.gameObject.SetActive(!swinging);
                if(swinging){float t=1-(meleeUntil-Time.time)/.28f;body.rightUpperArm.rotation=Quaternion.LookRotation(weapon.Direction)*Quaternion.Euler(-70,Mathf.Lerp(-65,65,t),-35);meleeVisual.localRotation=Quaternion.Euler(0,0,-10);meleeVisual.localPosition=new Vector3(0,-.1f,.02f);}
            }
            // Gameplay and visible barrel share the same sockets.
            weapon.muzzle=current.muzzle;weapon.grip=current.trigger;weapon.supportGrip=current.support;
            // Melee changes the held weapon and pose, not the player's appearance.
            ApplyGeneratedLook(handgun,Time.time<meleeUntil);
            GeneratedProductWeapon.Synchronize(ref productWeapon,game?game.gun:null,current,transform,
                GeneratedWardenPreviewAssets.Enabled&&Time.time>=meleeUntil);
            SuppressOldBase();
        }
        void SuppressOldBase()
        {
            if(!commonBaseSkin)return;
            bool replacedArms=GeneratedCommonBodyRig.ReplacesLegacyArms(commonBaseSkin);
            if(oldBaseRenderers!=null)foreach(var renderer in oldBaseRenderers)if(renderer&&(!GeneratedCommonBodyRig.IsLegacyArmRenderer(renderer,body)||replacedArms))renderer.enabled=false;
            GeneratedCommonBodyRig.SetVisible(commonBaseSkin,true);
            var slots=GeneratedEquipmentAssetCatalog.CoveredBodySlots(game?game.EquippedArmor:null);
            GeneratedCommonBodyRig.SetCoveredSlots(commonBaseSkin,slots);
        }
        void ApplyGeneratedLook(bool handgun,bool meleeSwinging)
        {
            bool[] slots=GeneratedEquipmentAssetCatalog.WardenSlots(game?game.EquippedArmor:null);
            bool generated=GeneratedWardenPreviewAssets.Enabled&&GeneratedEquipmentAssetCatalog.HasWardenSlot(slots);
            bool scanFeet=GeneratedEquipmentAssetCatalog.ShowWardenScanFeet(game?game.EquippedArmor:null,slots);
            bool wasGenerated=IsGeneratedLookActive;
            if(filteredWardenArmor!=generated&&game)ApplyArmor(game.EquippedArmor);
            if(!generatedLookRoot&&generated)BuildGeneratedLook();
            if(generatedLookRoot)generatedLookRoot.SetActive(generated);
            GeneratedWardenRig.SetJointBridges(generatedJointBridges,slots[1]);
            body.gameObject.SetActive(true);
            if(generatedLimbPalette!=false){GeneratedWardenRig.SetLimbMaterials(baseLookRenderers,baseLookMaterials,body,false);generatedLimbPalette=false;}
            if(generated)
            {
                GeneratedWardenRig.RestoreBaseAnatomy(body);
                equipment.SetItems(GeneratedEquipmentAssetCatalog.WithoutWardenSlots(game.EquippedArmor,slots));
                GeneratedWardenRig.SetSlotGeometryVisible(body,slots,generatedHeadSkin,generatedArmorSkin,generatedLookMask,scanFeet);
            }
            else if(wasGenerated)
            {
                GeneratedWardenRig.RestoreBaseAnatomy(body);
                equipment.SetItems(GeneratedEquipmentAssetCatalog.WithoutHeadArmor(game?game.EquippedArmor:null));
            }

            // The unpartitioned scan renderer is only a source template. Never show it:
            // live armor uses independent per-slot renderers under generatedLookRoot.
            if(generatedArmorSkin)generatedArmorSkin.enabled=false;
            if(generatedHeadSkin)generatedHeadSkin.enabled=false;
            if(generatedArmorParts!=null)for(int i=1;i<generatedArmorParts.Length;i++)if(generatedArmorParts[i])
                generatedArmorParts[i].enabled=generated&&(slots[i]||i==4&&scanFeet);
            if(generated)sharedArmorTextures.Apply(generatedArmorParts,game?game.EquippedArmor:null);
            if(!generatedLookMask)generatedLookMask=GeneratedWardenPreviewAssets.CreateFittedMask(body.head);
            if(generatedLookMask)generatedLookMask.SetActive(true);
            // The Warden scan supplies its own jacket, trousers, gloves and boots. Hide
            // the simple-clothes underlayer while that outfit is active to prevent the
            // old procedural clothes from showing through as duplicated anatomy.
            bool generatedGun=generated&&game&&!GeneratedEquipmentAssetCatalog.TryExactGun(game.gun,out _)
                &&GeneratedEquipmentAssetCatalog.TryGun(game.gun,out _);
            if(current?.root){current.root.gameObject.SetActive(!meleeSwinging);foreach(var renderer in current.root.GetComponentsInChildren<Renderer>(true))renderer.enabled=!generatedGun&&!meleeSwinging;}
            if(!generated)return;
            string model=generatedGun?(handgun?"sidearm":"rifle"):null;
            if(generatedLookWeaponKey!=model)
            {
                if(generatedLookWeapon)Destroy(generatedLookWeapon);
                generatedLookWeaponKey=model;
                generatedLookWeapon=model==null?null:GeneratedWardenPreviewAssets.CreateRestingWeapon(model,generatedLookRoot.transform,handgun,handgun?1.5f:1.4f);
            }
            Vector3 facing=weapon?weapon.Direction:Vector3.forward;facing.y=0;
            if(facing.sqrMagnitude<.001f)facing=Vector3.forward;
            generatedLookRoot.transform.rotation=Quaternion.LookRotation(facing.normalized,Vector3.up);
            if(generatedLookWeapon){GeneratedWardenPreviewAssets.PlaceWeaponOnSockets(generatedLookWeapon,current);generatedLookWeapon.SetActive(!meleeSwinging);}
        }
        void OnDestroy(){sharedArmorTextures.Dispose();}
        void BuildGeneratedLook()
        {
            generatedLookRoot=new GameObject("Generated Warden fieldkit / active appearance");generatedLookRoot.transform.SetParent(transform,false);
            generatedLookRoot.transform.localPosition=Vector3.zero;
            generatedLookRoot.transform.localScale=Vector3.one;
            baseLookRenderers=body.GetComponentsInChildren<Renderer>(true);
            baseLookMaterials=GeneratedWardenRig.CaptureMaterials(baseLookRenderers);
            var generatedHead=GeneratedWardenPreviewAssets.CreateCharacterPart("body",body);
            var generatedArmor=GeneratedWardenPreviewAssets.CreateCharacterPart("armor",body);
            generatedHeadSkin=GeneratedWardenRig.Bind(generatedHead,body,true);
            generatedArmorSkin=GeneratedWardenRig.Bind(generatedArmor,body,false,true,true);
            generatedArmorParts=GeneratedWardenRig.SplitArmorBySlot(generatedArmorSkin,generatedLookRoot.transform);
            if(generatedArmorParts!=null&&generatedArmorParts.Length>1&&generatedArmorParts[1])generatedArmorSkin.enabled=false;
            generatedHasArms=GeneratedWardenRig.HasArticulatedArms(generatedArmorSkin);
            generatedHasLegs=GeneratedWardenRig.HasArticulatedLegs(generatedArmorSkin);
            // Use the same generated mask and Head socket as the equipment portrait.
            // Legacy head accessories are hidden by the common visibility switch.
            if(!generatedLookMask)generatedLookMask=GeneratedWardenPreviewAssets.CreateFittedMask(body.head);
            generatedJointBridges=GeneratedWardenRig.CreateJointBridges(body);

        }
        static bool ValidSockets(DetailedHeroEquipment3D.WeaponSockets sockets)=>sockets!=null&&sockets.root&&sockets.trigger&&sockets.support&&sockets.stock&&sockets.muzzle;
        void ReplaceWeapon(string key)
        {
            // Build under an isolated identity transform: failed constructors must not leave
            // orphan geometry, invalidate the live sockets, or interrupt movement through Aim.
            var staging=new GameObject("Weapon replacement staging").transform;
            staging.SetParent(transform,false);
            DetailedHeroEquipment3D.WeaponSockets replacement=null;
            try
            {
                replacement=ManufacturerEquipment3D.BuildWeapon(staging,game?game.gun:null);
                if(!ValidSockets(replacement))throw new System.InvalidOperationException("Weapon assembly has missing sockets");
                foreach(var socket in new[]{replacement.trigger,replacement.support,replacement.stock,replacement.muzzle})
                    if(!Finite(socket.position))throw new System.InvalidOperationException("Weapon assembly has non-finite sockets");
                replacement.root.SetParent(transform,false);
                var previousWeapon=current;current=replacement;
                // Building legacy items may infer identity. Cache the resulting identity once.
                weaponKey=ManufacturerEquipment3D.WeaponKey(game?game.gun:null);failedWeaponKey=null;
                if(previousWeapon?.root){previousWeapon.root.gameObject.SetActive(false);Destroy(previousWeapon.root.gameObject);}
            }
            catch(System.Exception error)
            {
                failedWeaponKey=ManufacturerEquipment3D.WeaponKey(game?game.gun:null);
                Debug.LogError("Weapon presentation failed for "+key+". Retaining a usable weapon rig.\n"+error);
                if(!ValidSockets(current))
                {
                    // A saved problematic item must still permit a fresh session to move/fire.
                    current=DetailedHeroEquipment3D.BuildRifle(transform,DetailedHeroMaterials3D.Get);
                    weaponKey=null;
                }
            }
            finally {staging.gameObject.SetActive(false);Destroy(staging.gameObject);}
        }
        static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
        void LateUpdate()
        {
            float dt=Mathf.Max(.001f,Time.deltaTime);
            Vector3 displacement=transform.position-previous;previous=transform.position;displacement.y=0;
            Vector3 actual=displacement/dt;
            velocity=Vector3.Lerp(velocity,actual,1-Mathf.Exp(-dt*14));
            moveBlend=Mathf.MoveTowards(moveBlend,actual.magnitude>.12f?1:0,dt*7);
            if(moveBlend>.001f){if(phase<0)phase=0;bool aiming=Time.time<firingUntil||(game&&game.Active&&VaultMobileInputBridge.Firing&&game.reload<=0);float speed=velocity.magnitude;float duty=speed>3?(aiming?.38f:.32f):.57f;float reach=Mathf.Lerp(.18f,.38f,Mathf.Clamp01(speed/4.5f));float cycleDistance=2*reach*transform.lossyScale.y/duty;phase+=displacement.magnitude/Mathf.Max(.5f,cycleDistance);}
            else phase=-1;
            Synchronize();
        }
        public static void Pose(DetailedHero3D hero,DetailedHeroEquipment3D.WeaponSockets weapon,bool handgun,bool firing,Vector3 direction,float phase,Vector3 velocity=default,float moveBlend=0)
        {
            direction.y=0;if(direction.sqrMagnitude<.001f)direction=Vector3.forward;direction.Normalize();
            Vector3 bodyDirection=!handgun&&firing?Quaternion.Euler(0,30,0)*direction:direction;
            hero.SetPose(handgun?"aimpistol":firing?"aimrifle":"lowrifle",bodyDirection,phase);
            hero.SetLocomotion(velocity,moveBlend,firing,phase);
            if(!handgun&&firing)hero.head.localRotation=Quaternion.Euler(-5,-30,0);
            Quaternion yaw=Quaternion.LookRotation(direction);
            weapon.root.localScale=Vector3.one*.78f;
            float rhythm=phase<0?0:Mathf.Sin(phase*Mathf.PI*2)*moveBlend;
            float beat=phase<0?0:Mathf.Sin(phase*Mathf.PI*4)*moveBlend;
            bool pistolLowReady=handgun&&!firing;
            weapon.root.rotation=yaw*Quaternion.Euler(firing?0:pistolLowReady?32+rhythm*5:25+rhythm*11,
                firing?0:pistolLowReady?-6+rhythm*3:-18+rhythm*8,rhythm*(firing?4f:pistolLowReady?4f:9));
            Vector3 contact;
            Transform socket;
            if(!handgun&&firing){contact=hero.rightUpperArm.position+direction*.025f;socket=weapon.stock;}
            else
            {
                // Relaxed two-handed pistol carry: wrists at the belt, ahead of the
                // abdomen, with bent elbows. Shooting retains the existing aimed pose.
                Vector3 hold=pistolLowReady?new Vector3(.075f,-.245f,.32f)
                    :new Vector3(.08f,firing?.12f:-.10f,firing?.44f:.14f);
                contact=hero.chest.position+yaw*(hold*hero.transform.lossyScale.y);socket=weapon.trigger;
            }
            if(!firing)contact+=yaw*new Vector3(rhythm*.045f,beat*.030f,rhythm*.075f)*hero.transform.lossyScale.y;
            weapon.root.position+=contact-socket.position;
            if(!firing&&!handgun)
            {
                float scale=hero.transform.lossyScale.y;Vector3 target=weapon.support.position-weapon.root.forward*.065f*scale-weapon.root.up*.01f*scale;
                Vector3 arm=target-hero.leftUpperArm.position;float reach=(DetailedHero3D.UpperArmLength+DetailedHero3D.ForearmLength-.012f)*scale;
                if(arm.magnitude>reach)weapon.root.position-=arm.normalized*(arm.magnitude-reach);
            }
            Vector3 wrist=weapon.trigger.position+weapon.root.up*(.065f*hero.transform.lossyScale.y)-weapon.root.forward*(.01f*hero.transform.lossyScale.y);
            Vector3 support=handgun?wrist-weapon.root.right*(.045f*hero.transform.lossyScale.y):weapon.support.position-weapon.root.forward*(.065f*hero.transform.lossyScale.y)-weapon.root.up*(.01f*hero.transform.lossyScale.y);
            hero.SetHandTargets(wrist,support);
            hero.SetGripOrientation(weapon.root.rotation,weapon.root.rotation*(handgun?Quaternion.Euler(0,0,-15):Quaternion.Euler(-90,0,0)));
        }
    }
}
