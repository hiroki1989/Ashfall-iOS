using UnityEngine;
namespace NeonVault
{
    // Isolated inspection rig. It never moves, clones, or animates the gameplay actor.
    public sealed class EquipmentHeroPreview : MonoBehaviour
    {
        const int Layer=31;
        GameObject stage; Camera view; RenderTexture image;
        DetailedHero3D hero; DetailedHeroArsenal3D.ArmorVisuals armor;
        DetailedHeroEquipment3D.WeaponSockets weapon;
        GameObject generatedRoot,generatedWeapon,generatedMask;

        SkinnedMeshRenderer generatedArmorSkin,generatedHeadSkin;
        SkinnedMeshRenderer commonBaseSkin;
        Renderer[] oldBaseRenderers;
        SkinnedMeshRenderer[] generatedArmorParts=new SkinnedMeshRenderer[5];
        bool generatedHasArms,generatedHasLegs;
        Renderer[] baseLookRenderers;
        GameObject[] generatedJointBridges;
        Material[][] baseLookMaterials;bool? generatedLimbPalette;
        string generatedWeaponKey;
        GeneratedProductWeapon productWeapon;
        TacticalBackpackVisual backpack;
        readonly SharedArmorTextures sharedArmorTextures=new SharedArmorTextures();
        readonly SharedArmorSleeves sharedArmorSleeves=new SharedArmorSleeves();
        string key; float lastRender=-100; public float Angle=25;
        public float ReviewPhase=-1;public Vector3 ReviewVelocity;public bool ReviewFiring;
        public bool UsesGeneratedWarden=>GeneratedWardenPreviewAssets.Enabled;
        public bool HasCompleteGeneratedWardenSet { get; private set; }
        public bool HasGeneratedWardenSlot { get; private set; }
        public bool IsShowingGeneratedWarden=>UsesGeneratedWarden&&HasGeneratedWardenSlot;
        public int GeneratedLegacyOverlapCount=>GeneratedWardenRig.CountVisibleLegacy(hero,generatedHeadSkin,generatedArmorSkin,generatedMask);
        public int GeneratedVisibleRendererCount
        {
            get
            {
                if(!generatedRoot||!generatedRoot.activeInHierarchy)return 0;
                int count=0;foreach(var renderer in generatedRoot.GetComponentsInChildren<Renderer>(true))if(renderer.enabled&&renderer.gameObject.activeInHierarchy)count++;
                if(generatedArmorSkin&&generatedArmorSkin.enabled)count++;
                if(generatedHeadSkin&&generatedHeadSkin.enabled)count++;
                if(generatedMask&&generatedMask.activeSelf)count++;
                if(generatedArmorParts!=null)foreach(var part in generatedArmorParts)if(part&&part.enabled&&part.gameObject.activeInHierarchy)count++;

                return count;
            }
        }
        public void ToggleGeneratedWarden()
        {
            GeneratedWardenPreviewAssets.SetEnabled(!UsesGeneratedWarden);lastRender=-100;
            foreach(var player in FindObjectsByType<DetailedHeroPlayer3D>(FindObjectsSortMode.None))player.Synchronize();
        }
        public Texture Render(VaultGame game)
        {
            if(!stage)Initialize(game);
            if(Time.realtimeSinceStartup-lastRender<1f/24f)return image;
            lastRender=Time.realtimeSinceStartup;
            bool wardenSet=GeneratedEquipmentAssetCatalog.TryWardenSet(game.EquippedArmor,
                out _,out _,out _);
            HasCompleteGeneratedWardenSet=wardenSet;
            bool[] slots=GeneratedEquipmentAssetCatalog.WardenSlots(game.EquippedArmor);
            HasGeneratedWardenSlot=GeneratedEquipmentAssetCatalog.HasWardenSlot(slots);
            bool scanFeet=GeneratedEquipmentAssetCatalog.ShowWardenScanFeet(game.EquippedArmor,slots);
            if(UsesGeneratedWarden&&HasGeneratedWardenSlot)
            {
                EnsureGeneratedWarden(game);
                var ordinary=GeneratedEquipmentAssetCatalog.WithoutWardenSlots(game.EquippedArmor,slots);
                GeneratedWardenRig.RestoreBaseAnatomy(hero);
                armor.SetItems(ordinary);
                ManufacturerEquipment3D.ApplyArmor(hero,ordinary);
                view.orthographicSize=1.10f;
                view.transform.LookAt(stage.transform.position+Vector3.up*.94f);
                EnsureReferenceWeapon(game);
                DetailedHeroPlayer3D.Pose(hero,weapon,game.gun!=null&&Arsenal.IsOneHanded(VaultLanguage.DisplayModel(game.gun)),ReviewFiring,Quaternion.Euler(0,Angle,0)*Vector3.forward,ReviewPhase,ReviewVelocity,ReviewPhase>=0?1:0);
                GeneratedWardenRig.SetSlotGeometryVisible(hero,slots,generatedHeadSkin,generatedArmorSkin,generatedMask,scanFeet);
                GeneratedWardenRig.SetJointBridges(generatedJointBridges,slots[1]);
                if(generatedLimbPalette!=false){GeneratedWardenRig.SetLimbMaterials(baseLookRenderers,baseLookMaterials,hero,false);generatedLimbPalette=false;}
                // Keep the original whole-outfit scan as a hidden source template;
                // only the separately skinned armor-slot meshes are rendered.
                if(generatedArmorSkin)generatedArmorSkin.enabled=false;
                if(generatedArmorParts!=null)for(int i=1;i<generatedArmorParts.Length;i++)if(generatedArmorParts[i])
                    generatedArmorParts[i].enabled=slots[i]||i==4&&scanFeet;
                sharedArmorTextures.Apply(generatedArmorParts,game.EquippedArmor);
                if(generatedHeadSkin)generatedHeadSkin.enabled=false;
                if(generatedMask)generatedMask.SetActive(slots[0]);

                bool generatedGun=!GeneratedEquipmentAssetCatalog.TryExactGun(game.gun,out _)
                    &&GeneratedEquipmentAssetCatalog.TryGun(game.gun,out _);
                SetRenderers(weapon?.root,!generatedGun);
                generatedRoot.SetActive(true);
                generatedRoot.transform.localRotation=Quaternion.identity;
                string generatedChoice=generatedGun?(game.gun!=null&&Arsenal.IsOneHanded(VaultLanguage.DisplayModel(game.gun))?"sidearm":"rifle"):null;
                if(generatedWeaponKey!=generatedChoice)ReplaceGeneratedWeapon(generatedChoice);
                if(generatedWeapon)GeneratedWardenPreviewAssets.PlaceWeaponOnSockets(generatedWeapon,weapon);
                // The portrait camera renders only the preview layer. The generated shell had
                // that layer, but the legacy articulated arm/leg renderers stayed on Default,
                // so the center body floated with no limbs. Reassign after every preview build
                // (weapon and equipment replacement can add children after initialization).
                foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
                GeneratedProductWeapon.Synchronize(ref productWeapon,game.gun,weapon,stage.transform,UsesGeneratedWarden);
                if(backpack)backpack.Synchronize(game);
                foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
                ShowCommonBase();
                var covered=GeneratedEquipmentAssetCatalog.CoveredBodySlots(game.EquippedArmor);
                GeneratedCommonBodyRig.SetCoveredSlots(commonBaseSkin,covered);
                sharedArmorSleeves.Apply(generatedArmorParts?[1],commonBaseSkin,hero,game.EquippedArmor);
                view.Render();return image;
            }
            view.orthographicSize=1.10f;
            if(generatedRoot)generatedRoot.SetActive(false);
            if(generatedArmorSkin)generatedArmorSkin.enabled=false;
            if(generatedHeadSkin)generatedHeadSkin.enabled=false;
            if(!generatedMask)generatedMask=GeneratedWardenPreviewAssets.CreateFittedMask(hero.head);
            if(generatedMask)generatedMask.SetActive(true);

            hero.gameObject.SetActive(true);
            SetHeroBaseVisible(true);
            GeneratedWardenRig.SetJointBridges(generatedJointBridges,false);
            if(generatedLimbPalette!=false){GeneratedWardenRig.SetLimbMaterials(baseLookRenderers,baseLookMaterials,hero,false);generatedLimbPalette=false;}
            SetRenderers(weapon?.root,true);
            armor.SetItems(GeneratedEquipmentAssetCatalog.WithoutHeadArmor(game.EquippedArmor));ManufacturerEquipment3D.ApplyArmor(hero,GeneratedEquipmentAssetCatalog.WithoutHeadArmor(game.EquippedArmor));
            string next=ManufacturerEquipment3D.WeaponKey(game.gun);
            if(weapon==null||key!=next)
            {
                if(weapon?.root){weapon.root.gameObject.SetActive(false);Destroy(weapon.root.gameObject);}
                weapon=ManufacturerEquipment3D.BuildWeapon(stage.transform,game.gun);key=next;
            }
            DetailedHeroPlayer3D.Pose(hero,weapon,game.gun!=null&&Arsenal.IsOneHanded(VaultLanguage.DisplayModel(game.gun)),ReviewFiring,Quaternion.Euler(0,Angle,0)*Vector3.forward,ReviewPhase,ReviewVelocity,ReviewPhase>=0?1:0);
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            GeneratedProductWeapon.Synchronize(ref productWeapon,game.gun,weapon,stage.transform,UsesGeneratedWarden);
            if(backpack)backpack.Synchronize(game);
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            ShowCommonBase();
            GeneratedCommonBodyRig.SetCoveredSlots(commonBaseSkin,GeneratedEquipmentAssetCatalog.CoveredBodySlots(game.EquippedArmor));
            sharedArmorSleeves.Apply(generatedArmorParts?[1],commonBaseSkin,hero,game.EquippedArmor);
            view.Render();return image;
        }
        void Initialize(VaultGame game)
        {
            stage=new GameObject("Equipment inspection stage");stage.transform.SetParent(transform,false);stage.transform.position=new Vector3(10000,0,0);
            hero=DetailedHero3D.Create(stage.transform,DetailedHeroMaterials3D.Get);
            oldBaseRenderers=hero.GetComponentsInChildren<Renderer>(true);
            commonBaseSkin=GeneratedCommonBodyRig.Create(hero);
            ShowCommonBase();
            backpack=TacticalBackpackVisual.Create(hero);
            armor=DetailedHeroArsenal3D.ApplyArmor(hero,GeneratedEquipmentAssetCatalog.WithoutHeadArmor(game.EquippedArmor));
            var cameraObject=new GameObject("Equipment portrait camera");cameraObject.transform.SetParent(stage.transform,false);
            view=cameraObject.AddComponent<Camera>();view.enabled=false;view.orthographic=true;view.orthographicSize=1.10f;view.nearClipPlane=.05f;view.farClipPlane=20;
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.91f,.925f,.93f);view.cullingMask=1<<Layer;
            view.transform.localPosition=new Vector3(0,1.85f,4.5f);view.transform.LookAt(stage.transform.position+Vector3.up*.94f);
            image=new RenderTexture(768,1024,24){name="Equipment live portrait",antiAliasing=4};image.Create();view.targetTexture=image;
            foreach(var camera in Camera.allCameras)if(camera!=view)camera.cullingMask&=~(1<<Layer);
            AddLight("Portrait warm key",new Vector3(35,-35,0),new Color(1,.91f,.78f),1.1f);
            AddLight("Portrait cool rim",new Vector3(15,145,0),new Color(.53f,.75f,1),.8f);
        }
        void EnsureGeneratedWarden(VaultGame game)
        {
            if(generatedRoot)return;
            generatedRoot=new GameObject("Generated Warden loadout preview");generatedRoot.transform.SetParent(stage.transform,false);
            // Blender exports this set in centimeters; Unity imports it at 1/100 scale.
            // Restore meter-sized proportions and fit each normalized Hunyuan output to its intended slot.
            // The scan-derived base overlaps the complete Warden clothing mesh. Keep it
            // inside the jacket, trousers and gloves so the two dense surfaces do not fight.
            baseLookRenderers=hero.GetComponentsInChildren<Renderer>(true);
            baseLookMaterials=GeneratedWardenRig.CaptureMaterials(baseLookRenderers);
            var generatedHead=GeneratedWardenPreviewAssets.CreateCharacterPart("body",hero);
            var generatedArmor=GeneratedWardenPreviewAssets.CreateCharacterPart("armor",hero);
            generatedHeadSkin=GeneratedWardenRig.Bind(generatedHead,hero,true);
            generatedArmorSkin=GeneratedWardenRig.Bind(generatedArmor,hero,false,true,true);
            generatedArmorParts=GeneratedWardenRig.SplitArmorBySlot(generatedArmorSkin,generatedRoot.transform);
            if(generatedArmorParts!=null&&generatedArmorParts.Length>1&&generatedArmorParts[1])generatedArmorSkin.enabled=false;
            generatedHasArms=GeneratedWardenRig.HasArticulatedArms(generatedArmorSkin);
            generatedHasLegs=GeneratedWardenRig.HasArticulatedLegs(generatedArmorSkin);
            if(generatedHeadSkin)generatedHeadSkin.gameObject.layer=Layer;
            if(generatedArmorSkin)generatedArmorSkin.gameObject.layer=Layer;
            if(generatedArmorParts!=null)foreach(var part in generatedArmorParts)if(part)part.gameObject.layer=Layer;
            // The generated mask shares the same fitted Head socket as gameplay.
            // Its source axes and dimensions are normalized in the shared importer.
            // Legacy head accessories are hidden while it is active.
            if(!generatedMask)generatedMask=GeneratedWardenPreviewAssets.CreateFittedMask(hero.head);
            generatedJointBridges=GeneratedWardenRig.CreateJointBridges(hero);

            foreach(var bridge in generatedJointBridges)if(bridge)bridge.layer=Layer;

            if(generatedMask)foreach(var t in generatedMask.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            foreach(var t in generatedRoot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;

        }
        void ReplaceGeneratedWeapon(string key)
        {
            if(generatedWeapon){generatedWeapon.SetActive(false);Destroy(generatedWeapon);}
            generatedWeaponKey=key;
            generatedWeapon=key==null?null:GeneratedWardenPreviewAssets.CreateRestingWeapon(key,generatedRoot.transform,key=="sidearm",key=="sidearm"?1.5f:1.4f);
            if(generatedWeapon)foreach(var t in generatedWeapon.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        }
        void EnsureReferenceWeapon(VaultGame game)
        {
            string next=ManufacturerEquipment3D.WeaponKey(game.gun);
            if(weapon==null||key!=next){if(weapon?.root){weapon.root.gameObject.SetActive(false);Destroy(weapon.root.gameObject);}weapon=ManufacturerEquipment3D.BuildWeapon(stage.transform,game.gun);key=next;}
            if(weapon?.root)weapon.root.gameObject.SetActive(true);
        }
        static void SetRenderers(Transform root,bool visible){if(!root)return;foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.enabled=visible;}
        void SetHeroBaseVisible(bool visible){if(baseLookRenderers!=null)foreach(var renderer in baseLookRenderers)if(renderer)renderer.enabled=visible||GeneratedWardenRig.IsLimbRenderer(renderer,hero);}
        void ShowCommonBase()
        {
            if(!commonBaseSkin)return;
            bool replacedArms=GeneratedCommonBodyRig.ReplacesLegacyArms(commonBaseSkin);
            if(oldBaseRenderers!=null)foreach(var renderer in oldBaseRenderers)if(renderer&&(!GeneratedCommonBodyRig.IsLegacyArmRenderer(renderer,hero)||replacedArms))renderer.enabled=false;
            GeneratedCommonBodyRig.SetVisible(commonBaseSkin,true);
        }
        GameObject AddGeneratedModel(string modelName,string textureName,Vector3 position,Vector3 scale)
        {
            var instance=GeneratedWardenPreviewAssets.Create(modelName,textureName,generatedRoot.transform,position,scale);
            if(instance)instance.name="Warden / "+modelName;
            return instance;
        }
        void AddLight(string name,Vector3 rotation,Color color,float intensity)
        {var obj=new GameObject(name);obj.transform.SetParent(stage.transform,false);obj.transform.rotation=Quaternion.Euler(rotation);var light=obj.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.cullingMask=1<<Layer;light.shadows=LightShadows.None;}
        void OnDestroy(){sharedArmorSleeves.Dispose();sharedArmorTextures.Dispose();if(image){image.Release();Destroy(image);}if(stage)Destroy(stage);}
    }
    public partial class VaultGame
    {
        EquipmentHeroPreview equipmentHeroPreview;
        void DrawEquipmentHeroPreview(Rect rect)
        {
            if(!equipmentHeroPreview)equipmentHeroPreview=gameObject.AddComponent<EquipmentHeroPreview>();
            if(Event.current.type==EventType.Repaint)GUI.DrawTexture(rect,equipmentHeroPreview.Render(this),ScaleMode.ScaleToFit,false);
            equipmentHeroPreview.Angle=GUI.HorizontalSlider(new Rect(rect.x+50,rect.yMax-25,rect.width-100,22),equipmentHeroPreview.Angle,-155,205);
        }
    }
}







