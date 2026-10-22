using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Executed only with --smoke. Exercises the real built player and saves reproducible evidence.
    public class SmokeRun : MonoBehaviour
    {
        string output;
        int checks;
        bool artReview;
        bool playerReview;
        bool layeredReview;
        bool reticleReview;
        string ResultFile=>reticleReview?"reticle-review-result.txt":layeredReview?"layered-player-review-result.txt":playerReview?"player-review-result.txt":artReview?"art-review-result.txt":"smoke-result.txt";
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture;
            if(layeredReview)
            {
                var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1440,900,24);
                texture=new Texture2D(1440,900,TextureFormat.RGB24,false);
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1440,900),0,0);texture.Apply(false);}
                finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Destroy(rt);}
                var colors=texture.GetPixels32();int bright=0;foreach(var color in colors)if(color.r+color.g+color.b>90)bright++;
                Check(bright>10000,"actual camera render contains painted scene / "+name);
            }
            else texture=ScreenCapture.CaptureScreenshotAsTexture();
            Check(texture!=null && texture.width>0,"framebuffer "+name);
            if(name=="gameplay.png")
            {
                float min=1,max=0;
                for(int x=texture.width/3;x<texture.width*2/3;x+=13)for(int y=texture.height/3;y<texture.height*2/3;y+=13)
                {Color c=texture.GetPixel(x,y);float value=Mathf.Max(c.r,Mathf.Max(c.g,c.b));min=Mathf.Min(min,value);max=Mathf.Max(max,value);}
                Check(max-min>.18f,"gameplay rendering contains scene detail after post effects");
            }
            if(name.StartsWith("reticle-accuracy-",StringComparison.Ordinal))
            {
                float uiScale=Mathf.Min(texture.width/1440f,texture.height/900f);int x=Mathf.RoundToInt(840*uiScale),y=texture.height-Mathf.RoundToInt(420*uiScale);bool centerVisible=false;
                for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++){var center=texture.GetPixel(x+dx,y+dy);centerVisible|=center.r>.97f&&center.g>.97f&&center.b>.97f;}
                Check(centerVisible,"reticle evidence includes actual HUD center point / "+name);
            }
            File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());
            Destroy(texture);
        }
        IEnumerator CaptureInkedEquipment(VaultGame game)
        {
            var player=game.PlayerAppearance.GetComponent<LayeredPlayer2D>();
            if(!LayeredPlayer2D.InkedArtReady||player==null)yield break;
            var savedGun=game.gun;int savedAmmo=game.ammo;float savedScale=Time.timeScale;
            var gear=game.EquippedArmor;var variants=new int[gear.Length];
            for(int slot=0;slot<gear.Length;slot++){variants[slot]=gear[slot]?.visualVariant??0;if(gear[slot]!=null)gear[slot].visualVariant=0;}
            try
            {
                Time.timeScale=0;game.gun=Arsenal.Model(12,Rarity.Common);game.ammo=game.gun.magazine;
                player.ApplyArmor(gear);player.SetWeaponModel(12,game.gun.color);player.SetReviewPose(7,LayeredPlayer2D.Motion.Idle,0,false);
                bool signatureCarbineLoaded=false;foreach(var renderer in player.GetComponentsInChildren<SpriteRenderer>())if(renderer.enabled&&renderer.sprite!=null&&renderer.sprite.texture!=null&&renderer.sprite.texture.name=="Menu_Weapons_6")signatureCarbineLoaded=true;
                Check(signatureCarbineLoaded,"menu reference rifle art is loaded for in-game directional weapon art");
                yield return Capture("inked-gameplay.png");
                game.EquippedArmor=new ArmorItem[gear.Length];player.ApplyArmor(game.EquippedArmor);
                string[] armNames={"right upper arm","right forearm","right hand","left upper arm","left forearm","left hand"};
                for(int direction=0;direction<8;direction++)
                {
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);
                    bool[] armVisible=new bool[armNames.Length];
                    foreach(var renderer in player.GetComponentsInChildren<SpriteRenderer>())if(renderer.enabled&&renderer.name.StartsWith("Painted body / ",StringComparison.Ordinal))
                    {string suffix=renderer.name.Substring("Painted body / ".Length);for(int i=0;i<armNames.Length;i++)if(suffix==armNames[i])armVisible[i]=true;}
                    bool connected=true;foreach(bool visible in armVisible)connected&=visible;
                    Check(connected,"unarmored shoulder-to-hand arm segments visible direction "+direction);
                    Check(Vector3.Distance(player.RightShoulderPosition,player.RightHandPosition)<1.0f&&Vector3.Distance(player.LeftShoulderPosition,player.LeftHandPosition)<1.0f,"unarmored arm reach remains anatomically plausible direction "+direction);
                    yield return Capture("unarmored-menu-rifle-"+direction+".png");
                }
                game.EquippedArmor=gear;player.ApplyArmor(gear);
            }
            finally
            {
                for(int slot=0;slot<gear.Length;slot++)if(gear[slot]!=null)gear[slot].visualVariant=variants[slot];
                game.gun=savedGun;game.ammo=savedAmmo;player.ApplyArmor(gear);player.SetWeaponModel(VaultLanguage.DisplayModel(savedGun),savedGun.color);player.ClearReviewPose();Time.timeScale=savedScale;
            }
        }
        void Check(bool condition,string name)
        {
            if(!condition) { File.WriteAllText(Path.Combine(output,ResultFile),"FAIL: "+name);Debug.LogError(name);Application.Quit(1);throw new Exception(name); }
            checks++; Debug.Log("SMOKE PASS: "+name);
        }
        static int VisibleMeshes(Transform root){int count=0;foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>())if(mesh.enabled&&mesh.gameObject.activeInHierarchy&&mesh.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)count++;return count;}
        static Transform FindPart(Transform root,string name){foreach(var part in root.GetComponentsInChildren<Transform>(true))if(part.name==name)return part;return null;}
        static Transform HumanSkull(Transform root)=>FindPart(root,"Player anatomic skull")??FindPart(root,"Helmet skull");
        static Bounds BodyBounds(Transform root)
        {
            Bounds bounds=new Bounds();bool first=true;
            foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(!mesh.enabled||!mesh.gameObject.activeInHierarchy||mesh.GetComponentInParent<AimedWeapon>()!=null)continue;
                if(first){bounds=mesh.bounds;first=false;}else bounds.Encapsulate(mesh.bounds);
            }
            return bounds;
        }
        static bool HandsMatchHandling(ActorEquipmentVisual appearance,float tolerance)
        {
            return appearance.GripError<tolerance&&appearance.SupportGripError<tolerance;
        }
        void CheckCarryPose(VaultGame game,LayeredPlayer2D player,string context,LayeredPlayer2D.Motion motion=LayeredPlayer2D.Motion.Idle,float phase=0)
        {
            player.SetReviewPose(player.CurrentDirection,motion,phase,false);
            Check(player.WeaponAimBlend<.001f,"non-firing weapon uses low ready / "+context);
            Check(HandsMatchHandling(game.PlayerAppearance,.001f),"both arms support the lowered weapon / "+context);
            float carryHeight=Vector3.Dot(player.CarryGripTarget,game.GameCamera.transform.up);
            float shoulderHeight=Vector3.Dot(player.RightShoulderPosition,game.GameCamera.transform.up);
            Check(carryHeight<shoulderHeight-.12f,"low ready keeps the weapon at waist level below the shoulder line / "+context);
        }
        void CheckHandlingPose(VaultGame game,LayeredPlayer2D player,string context)
        {
            Check(player.RightArmReach<=.7801f&&player.LeftArmReach<=.7801f,"true 3D fixed arm reach / "+context+" / "+player.RightArmReach.ToString("F4")+" / "+player.LeftArmReach.ToString("F4"));
            Check(player.UsesTwoHandGrip&&!player.IsOneHanded,"every weapon pose uses both hands / "+context);
            Check(player.IsCatalogOneHanded==Arsenal.IsOneHanded(player.WeaponModel),"weapon model retains its catalog classification / "+context);
            Check(HandsMatchHandling(game.PlayerAppearance,.001f),"right and left hands stay on the gun support grips / "+context);
            if(player.IsCatalogOneHanded){Check(!player.RequiresShoulderStock&&!player.StockOcclusionActive,"pistol or compact SMG has two-hand support without shoulder-stock attachment / "+context);}
            else {float shoulderGap=Vector3.ProjectOnPlane(player.VisualStockPosition-player.RightShoulderPosition,game.GameCamera.transform.forward).magnitude;Check(player.RequiresShoulderStock&&player.StockContactError<.001f&&shoulderGap<.02f,"long gun stock braces actual anatomical right shoulder / "+context);Check(player.StockOcclusionActive,"aimed long gun stock has torso masking / "+context);}
            if(player.HasArmoredGloves)
            {
                Check(player.ArmorHandRenderers[0].enabled&&Vector3.ProjectOnPlane(player.ArmorHandRenderers[0].transform.position-player.VisualGripPosition,game.GameCamera.transform.forward).magnitude<.003f,"actual rendered trigger glove remains on gun grip / "+context);
                Check(player.ArmorHandRenderers[1].enabled&&Vector3.ProjectOnPlane(player.ArmorHandRenderers[1].transform.position-player.LeftHandPosition,game.GameCamera.transform.forward).magnitude<.003f,"actual rendered left glove follows support or free hand / "+context);
            }
            Check(Vector3.Distance(player.RightElbowPosition,player.RightShoulderPosition)>.05f&&Vector3.Distance(player.RightElbowPosition,player.RightHandPosition)>.02f,"right arm has finite shoulder-elbow-wrist segments / "+context);
            Check(Vector3.ProjectOnPlane(player.RightElbowPosition-player.RightHandPosition,game.GameCamera.transform.forward).magnitude>.02f,"trigger elbow stays distinct from wrist in rendered image plane / "+context);
            float leftElbowHand=Vector3.ProjectOnPlane(player.LeftElbowPosition-player.LeftHandPosition,game.GameCamera.transform.forward).magnitude,leftElbowShoulder=Vector3.ProjectOnPlane(player.LeftElbowPosition-player.LeftShoulderPosition,game.GameCamera.transform.forward).magnitude;
            Check(leftElbowHand>.02f&&leftElbowShoulder>.05f,"left arm has finite visible shoulder-elbow-wrist segments / "+context+" / elbow-hand "+leftElbowHand.ToString("F4")+" / elbow-shoulder "+leftElbowShoulder.ToString("F4"));
        }
        void VerifyGaitGeometry(LayeredPlayer2D player,Camera camera)
        {
            float runStepsPerSecond=7f/LayeredPlayer2D.GaitCycleDistance(true)*2,previousStepsPerSecond=7f/2.4f*2;
            Check(runStepsPerSecond<3.8f&&runStepsPerSecond/previousStepsPerSecond<=.65f,"base-speed running cadence stays below 3.8 steps per second and falls at least 35 percent from the former run");
            Check(LayeredPlayer2D.GaitCycleDistance(false)>1.8f&&LayeredPlayer2D.GaitCycleDistance(false)<LayeredPlayer2D.GaitCycleDistance(true),"walking also uses a longer cycle while retaining a shorter stride than running");
            var csv=new System.Text.StringBuilder("direction,phase,rightKneeX,rightKneeY,leftKneeX,leftKneeY,rightFootX,rightFootY,leftFootX,leftFootY,rightHipFootDistance,leftHipFootDistance\n");
            for(int direction=0;direction<8;direction++)
            {
                player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);
                Vector3 idleUpper=player.RightKneePosition-player.RightHipPosition,idleLower=player.RightFootPosition-player.RightKneePosition;
                Check(Vector3.Angle(idleUpper,idleLower)<8,"idle legs remain upright rather than crouched / "+direction);
                var minWidth=new float[6];var maxWidth=new float[6];for(int i=0;i<6;i++)minWidth[i]=float.MaxValue;
                Vector2[] first=null,previous=null;float maxJointStep=0,minKneeOrder=float.MaxValue,minFootOrder=float.MaxValue;
                float minLegLength=float.MaxValue,maxLegLength=0,minKneeHeight=float.MaxValue,maxKneeHeight=float.MinValue;
                for(int sample=0;sample<=64;sample++)
                {
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,(sample%64)/64f,false);
                    var origin=player.transform.position;
                    var joints=new[]{ProjectGait(player.RightKneePosition-origin,camera),ProjectGait(player.LeftKneePosition-origin,camera),ProjectGait(player.RightFootPosition-origin,camera),ProjectGait(player.LeftFootPosition-origin,camera)};
                    if(sample==0)first=joints;
                    if(previous!=null)for(int i=0;i<joints.Length;i++)maxJointStep=Mathf.Max(maxJointStep,Vector2.Distance(previous[i],joints[i]));
                    previous=joints;
                    if(sample==64){for(int i=0;i<joints.Length;i++)Check(Vector2.Distance(first[i],joints[i])<.001f,"run cycle closes continuously / "+direction+" / joint "+i);break;}
                    for(int i=0;i<6;i++){var sprite=player.BodyRenderers[9+i];float width=sprite.sprite.bounds.size.x*Mathf.Abs(sprite.transform.lossyScale.x);minWidth[i]=Mathf.Min(minWidth[i],width);maxWidth[i]=Mathf.Max(maxWidth[i],width);}
                    float rightLength=Vector3.ProjectOnPlane(player.RightHipPosition-player.RightFootPosition,camera.transform.forward).magnitude,leftLength=Vector3.ProjectOnPlane(player.LeftHipPosition-player.LeftFootPosition,camera.transform.forward).magnitude;
                    minLegLength=Mathf.Min(minLegLength,rightLength);maxLegLength=Mathf.Max(maxLegLength,rightLength);minKneeHeight=Mathf.Min(minKneeHeight,joints[0].y);maxKneeHeight=Mathf.Max(maxKneeHeight,joints[0].y);
                    if(direction==0||direction==4){float rightSign=direction==0?-1:1;minKneeOrder=Mathf.Min(minKneeOrder,(joints[0].x-joints[1].x)*rightSign);minFootOrder=Mathf.Min(minFootOrder,(joints[2].x-joints[3].x)*rightSign);}
                    csv.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F6},{2:F6},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6},{11:F6}",direction,sample/64f,joints[0].x,joints[0].y,joints[1].x,joints[1].y,joints[2].x,joints[2].y,joints[3].x,joints[3].y,rightLength,leftLength));
                }
                for(int i=0;i<6;i++)Check(maxWidth[i]-minWidth[i]<.004f,"running thigh shin and boot widths remain stable / "+direction+" / part "+(9+i));
                // At 64 samples this limits any single discontinuity to less than one third of a shin.
                Check(maxJointStep<.12f,"running knees and feet have no sudden position jump including cycle seam / "+direction+" / "+maxJointStep);
                if(direction==0||direction==4)
                {
                    Check(minKneeOrder>.05f&&minFootOrder>.05f,"front and rear runs preserve anatomical left-right knee and foot order / "+direction);
                    Check(maxLegLength-minLegLength>.15f&&maxKneeHeight-minKneeHeight>.10f,"front and rear recovery visibly shortens the leg and raises its knee / "+direction);
                }
            }
            File.WriteAllText(Path.Combine(output,"layered-run-gait-geometry.csv"),csv.ToString());
        }
        static Vector2 ProjectGait(Vector3 vector,Camera camera)=>new Vector2(Vector3.Dot(vector,camera.transform.right),Vector3.Dot(vector,camera.transform.up));
        void VerifyAccuracyFire(VaultGame game)
        {
            var saved=game.gun;int savedAmmo=game.ammo;float savedReload=game.reload;var randomState=UnityEngine.Random.state;UnityEngine.Random.InitState(91501);
            var evidence=new System.Text.StringBuilder("model,accuracy,halfAngle,maxActualAngle,projectiles\n");
            try
            {
                foreach(int model in new[]{12,13,14,15,16,17})
                {
                    var item=Arsenal.Model(model,Rarity.Common);float maxAngle=0;int count=0;game.gun=item;game.PlayerAppearance.GetComponent<LayeredPlayer2D>()?.SetWeaponModel(model,item.color);
                    for(int sample=0;sample<32;sample++)
                    {
                        var previous=new System.Collections.Generic.HashSet<Shot>(FindObjectsByType<Shot>(FindObjectsSortMode.None));game.ammo=item.magazine;game.reload=0;game.AimAt(game.PlayerPosition+Vector3.forward*30);game.Fire();
                        int spawned=0;foreach(var shot in FindObjectsByType<Shot>(FindObjectsSortMode.None))if(!previous.Contains(shot)&&shot.friendly)
                        {
                            float angle=Vector3.Angle(shot.velocity,game.PlayerWeapon.Direction);maxAngle=Mathf.Max(maxAngle,angle);spawned++;count++;
                            Check(angle<=Arsenal.SpreadHalfAngle(item)+.04f,"actual bullet stays within item accuracy cone / "+model+" / "+sample);
                            Check(Vector3.Distance(shot.transform.position,game.PlayerWeapon.MuzzlePosition)<.001f,"actual bullet starts at visible model muzzle / "+model);DestroyImmediate(shot.gameObject);
                        }
                        Check(spawned==item.pellets&&game.ammo==item.magazine-1,"actual shot emits model pellet count and spends one cartridge / "+model);
                    }
                    Check(maxAngle>Arsenal.SpreadHalfAngle(item)*.65f,"actual fire exercises accuracy spread instead of always shooting center / "+model);
                    evidence.AppendLine(model+","+Arsenal.Accuracy(item)+","+Arsenal.SpreadHalfAngle(item)+","+maxAngle+","+count);
                }
                // Change the serialized score on the same model: the real fire path must use it.
                var gun=Arsenal.Model(12,Rarity.Common);game.gun=gun;game.PlayerAppearance.GetComponent<LayeredPlayer2D>()?.SetWeaponModel(12,gun.color);
                foreach(float score in new[]{100f,20f})
                {
                    gun.accuracy=score;float maximum=0;
                    for(int sample=0;sample<32;sample++)
                    {
                        var previous=new System.Collections.Generic.HashSet<Shot>(FindObjectsByType<Shot>(FindObjectsSortMode.None));game.ammo=gun.magazine;game.reload=0;game.AimAt(game.PlayerPosition+Vector3.forward*30);game.Fire();
                        int spawned=0;foreach(var shot in FindObjectsByType<Shot>(FindObjectsSortMode.None))if(!previous.Contains(shot)&&shot.friendly){spawned++;float angle=Vector3.Angle(shot.velocity,game.PlayerWeapon.Direction);maximum=Mathf.Max(maximum,angle);Check(angle<=Arsenal.SpreadHalfAngle(gun)+.04f,"serialized accuracy bounds actual fire / "+score);DestroyImmediate(shot.gameObject);}
                        Check(spawned==1&&game.ammo==gun.magazine-1,"accuracy score test fires exactly one real projectile / "+score);
                    }
                    Check(score==100?maximum<.04f:maximum>10,"same model accuracy score changes real projectile spread / "+score);evidence.AppendLine("12,"+score+","+Arsenal.SpreadHalfAngle(gun)+","+maximum+",32");
                }
                File.WriteAllText(Path.Combine(output,"accuracy-fire.csv"),evidence.ToString());
            }
            finally{UnityEngine.Random.state=randomState;game.gun=saved;game.ammo=savedAmmo;game.reload=savedReload;game.PlayerAppearance.GetComponent<LayeredPlayer2D>()?.SetWeaponModel(VaultLanguage.DisplayModel(saved),saved.color);}
        }
        void VerifyGearQuality()
        {
            var random=new System.Random(2202510);
            for(int tier=0;tier<=4;tier++)
            {
                var rarity=(Rarity)tier;var gun=Arsenal.CreateForDungeon(random,0,rarity,3);var armor=Armory.CreateForDungeon(random,0,rarity,3);var module=ArtifactCatalog.Create(random,0,rarity,3);
                Check(gun.affixes!=null&&gun.affixes.Length==tier&&GearScoring.Score(gun)>0,"weapon rarity controls affix count and displays a performance score / "+rarity);
                Check(armor.affixes!=null&&armor.affixes.Length==tier&&GearScoring.Score(armor)>0,"armor rarity controls affix count and displays a performance score / "+rarity);
                Check(module.affixes!=null&&module.affixes.Length==tier&&GearScoring.Score(module)>0,"module rarity controls affix count and displays a performance score / "+rarity);
                var melee=MeleeWeaponCatalog.Create(new System.Random(6147),3,rarity);
                Check(melee.rarity==rarity&&melee.color==Arsenal.Colors[tier]&&melee.damage>0,"melee tier matches firearm rarity color and has valid stats / "+rarity);
                Check(melee.uniqueEffect==(rarity==Rarity.Legendary),"only legendary melee weapons carry a unique-effect marker / "+rarity);
                if(rarity==Rarity.Legendary)Check(LegendaryCatalog.Index(gun.legendaryKey)>=0&&LegendaryCatalog.Index(armor.legendaryKey)>=0&&module.uniqueEffect>0,"legendary weapon armor and module retain their unique effects");
            }
            var namedArmor=Armory.CreateForDungeon(new System.Random(1844),1,Rarity.Epic,7);
            var restoredArmor=JsonUtility.FromJson<ArmorItem>(JsonUtility.ToJson(namedArmor));
            Check(Armory.Name(namedArmor,true)==Armory.Name(restoredArmor,true)&&Armory.Name(namedArmor,false)==Armory.Name(restoredArmor,false),"individual armor names remain stable through save serialization");
            var uniqueHead=new ArmorItem{id="unique-head-name",slot=ArmorSlot.Head,rarity=Rarity.Legendary,dungeon=0,uniqueEffect=1};
            var uniqueChest=new ArmorItem{id="unique-chest-name",slot=ArmorSlot.Chest,rarity=Rarity.Legendary,dungeon=0,uniqueEffect=1};
            Check(Armory.Name(uniqueHead,false)!=Armory.Name(uniqueChest,false)&&Armory.Name(uniqueHead,true)!=Armory.Name(uniqueChest,true),"legendary armor names identify their individual equipment piece in both languages");
            for(int model=0;model<Arsenal.ModelCount;model++)
            {
                var expected=Arsenal.Category(model)==WeaponClass.Handgun?AmmoType.Handgun:Arsenal.Category(model)==WeaponClass.Shotgun?AmmoType.Shotgun:Arsenal.Category(model)==WeaponClass.SniperRifle?AmmoType.SniperRifle:AmmoType.AssaultRifle;
                Check(VaultGame.AmmoForGun(Arsenal.Model(model,Rarity.Common))==expected,"weapon model uses its own reserve-ammo family / "+model);
            }
        }
        void VerifyFieldEconomy(VaultGame game)
        {
            Check(game.AmmoCratesPlannedThisRun>=1&&game.AmmoCratesPlannedThisRun<=2,"each run plans one or two field ammunition crates");
            bool mapValid=ExpeditionMap.StageCount==100;
            for(int region=0;region<5;region++){int regional=0;for(int id=0;id<ExpeditionMap.StageCount;id++){var stage=ExpeditionMap.Get(id);if(stage.region==region){regional++;mapValid&=stage.level>=1&&stage.level<=10&&stage.legacyDungeonIndex>=0&&stage.legacyDungeonIndex<3;if(stage.mission==ExpeditionMission.ClearHostiles)mapValid&=stage.Target==(VaultLanguage.Japanese?"区域の敵":"Zone hostiles");if(stage.mission==ExpeditionMission.RecoverSupplies)mapValid&=stage.Target==(VaultLanguage.Japanese?"補給物資":"Supply cache");if(stage.mission==ExpeditionMission.DestroyNest)mapValid&=stage.Target==(VaultLanguage.Japanese?"再生母体の巣":"Flesh Weaver nest");}}mapValid&=regional==20;}
            Check(mapValid,"expedition catalog has 100 level-gated sites across five 20-site regions");
            Check(game.ScrapLevelMultiplier>=1&&game.ScaleScrap(10000)>=100,"scrap transactions use the reduced base scale and level growth");
            Check(game.AmmoPickupEnglish(AmmoType.Handgun,9)=="Handgun rounds ×9"&&game.AmmoPickupJapanese(AmmoType.Shotgun,4)=="散弾 ×4","ammo pickup labels identify type and exact quantity in both languages");
            int scrapDrops=0;bool scrapRange=true;for(int i=0;i<1000;i++)if(game.TryRollScrapDrop(false,out int amount)){scrapDrops++;scrapRange&=amount>=game.ScaleScrap(12000+game.DungeonIndex*3000)&&amount<=game.ScaleScrap(32000+game.DungeonIndex*3000);}
            Check(scrapRange,"regular scrap rewards stay in the reduced range");
            Check(scrapDrops>=390&&scrapDrops<=510,"regular scrap drop frequency is moderately below the old 50% rate / "+scrapDrops+" of 1000");
            int ammoDrops=0,ammoAmount=0;bool ammoRange=true;for(int i=0;i<1000;i++){var type=(AmmoType)(i%4);if(game.TryRollAmmoDrop(false,type,out int amount)){ammoDrops++;ammoAmount+=amount;ammoRange&=amount>0&&amount<=game.AmmoPackSize(type);}}
            Check(ammoRange,"regular ammo rewards remain partial packs");
            Check(ammoDrops>=250&&ammoDrops<=400&&ammoAmount>0,"regular enemies favor ammo more often while keeping pickups modest");
            int bossDrops=0;bool bossScrapRange=true;for(int i=0;i<1000;i++)if(game.TryRollScrapDrop(true,out int amount)){bossDrops++;bossScrapRange&=amount>=game.ScaleScrap(550000)&&amount<=game.ScaleScrap(900000);}
            Check(bossScrapRange,"boss scrap rewards stay below their former range");
            Check(bossDrops>=750&&bossDrops<=850,"boss scrap rate is modestly lower than its old 95% rate / "+bossDrops+" of 1000");
            int bossAmmoDrops=0;bool bossAmmoRange=true;for(int i=0;i<100;i++)if(game.TryRollAmmoDrop(true,AmmoType.AssaultRifle,out int amount)){bossAmmoDrops++;bossAmmoRange&=amount>0&&amount<game.AmmoPackSize(AmmoType.AssaultRifle);}
            Check(bossAmmoRange,"boss ammo rewards are a useful partial refill");
            Check(bossAmmoDrops>=75&&bossAmmoDrops<=100,"boss ammunition favors drops without guaranteeing a full crate / "+bossAmmoDrops+" of 100");
        }
        IEnumerator CaptureReticleAccuracy(VaultGame game)
        {
            int width=UnityEngine.Screen.width,height=UnityEngine.Screen.height,savedAmmo=game.ammo;var mode=UnityEngine.Screen.fullScreenMode;var savedGun=game.gun;var savedPoint=game.AimUiReviewPoint;float savedScale=Time.timeScale,savedFireTimer=game.fireTimer,savedReload=game.reload;bool enabled=game.enabled;
            var csv=new System.Text.StringBuilder("image,width,height,model,accuracy,spreadHalfAngle,logicalCenterX,logicalCenterY,uiScale,baseGap\n");
            try
            {
                Time.timeScale=0;game.enabled=true;game.fireTimer=0;game.reload=0;game.AimUiReviewPoint=new Vector2(840,420);
                foreach(var resolution in new[]{new Vector2Int(1440,900),new Vector2Int(960,600)})
                {
                    UnityEngine.Screen.SetResolution(resolution.x,resolution.y,FullScreenMode.Windowed);
                    for(int frame=0;frame<120&&(UnityEngine.Screen.width!=resolution.x||UnityEngine.Screen.height!=resolution.y);frame++)yield return null;
                    yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
                    Check(UnityEngine.Screen.width==resolution.x&&UnityEngine.Screen.height==resolution.y,"accuracy reticle review reaches requested resolution / "+resolution);
                    foreach(int model in new[]{12,13})
                    {
                        game.gun=Arsenal.Model(model,Rarity.Common);game.ammo=game.gun.magazine;game.PlayerAppearance.GetComponent<LayeredPlayer2D>()?.SetWeaponModel(model,game.gun.color);
                        string image="reticle-accuracy-"+model+"-"+resolution.x+"x"+resolution.y+".png";yield return Capture(image);
                        float scale=Mathf.Min(resolution.x/1440f,resolution.y/900f);
                        csv.AppendLine(image+","+resolution.x+","+resolution.y+","+model+","+Arsenal.Accuracy(game.gun)+","+Arsenal.SpreadHalfAngle(game.gun)+",840,420,"+scale+","+(2+(100-Arsenal.Accuracy(game.gun))*.16f));
                    }
                }
                File.WriteAllText(Path.Combine(output,"reticle-accuracy-settings.csv"),csv.ToString());
            }
            finally
            {
                game.gun=savedGun;game.ammo=savedAmmo;game.fireTimer=savedFireTimer;game.reload=savedReload;game.AimUiReviewPoint=savedPoint;game.PlayerAppearance.GetComponent<LayeredPlayer2D>()?.SetWeaponModel(VaultLanguage.DisplayModel(savedGun),savedGun.color);
                UnityEngine.Screen.SetResolution(width,height,mode);Time.timeScale=savedScale;game.enabled=enabled;
            }
            for(int frame=0;frame<120&&(UnityEngine.Screen.width!=width||UnityEngine.Screen.height!=height);frame++)yield return null;
            yield return new WaitForEndOfFrame();Check(UnityEngine.Screen.width==width&&UnityEngine.Screen.height==height,"reticle review restores original resolution");
        }
        readonly System.Text.StringBuilder poseCsv=new System.Text.StringBuilder();
        void RecordDynamicPose(VaultGame game,LayeredPlayer2D player,string label,int frame,float angle)
        {
            var camera=game.GameCamera;
            float ru=Vector3.ProjectOnPlane(player.RightElbowPosition-player.RightShoulderPosition,camera.transform.forward).magnitude;
            float rf=Vector3.ProjectOnPlane(player.RightHandPosition-player.RightElbowPosition,camera.transform.forward).magnitude;
            float lu=Vector3.ProjectOnPlane(player.LeftElbowPosition-player.LeftShoulderPosition,camera.transform.forward).magnitude;
            float lf=Vector3.ProjectOnPlane(player.LeftHandPosition-player.LeftElbowPosition,camera.transform.forward).magnitude;
            Check(player.RightArmReach<=.7801f&&player.LeftArmReach<=.7801f,"live true 3D arm reach / "+label+" / "+frame);
            Check(ru>.05f&&lu>.05f&&rf>.02f&&lf>.02f,"dynamic limbs remain visible / "+label+" / "+frame);
            Check(ru<=.402f&&lu<=.402f&&rf<=.382f&&lf<=.382f,"dynamic fixed arm lengths / "+label+" / "+frame);
            float rightError=Vector3.Distance(player.RightHandPosition,player.VisualGripPosition),leftError=Vector3.Distance(player.LeftHandPosition,player.VisualSupportGripPosition);
            Check(rightError<.001f&&leftError<.001f,"dynamic hands remain on rendered contacts / "+label+" / "+frame);
            if(player.RaisedBlend>.999f&&!player.IsCatalogOneHanded)Check(player.StockContactError<.001f,"live stock remains on shoulder / "+label+" / "+frame);
            if(player.CurrentMotion==LayeredPlayer2D.Motion.Shoot||player.RaisedBlend>.999f)
            {
                Vector3 drawn=player.VisualMuzzlePosition-(player.IsCatalogOneHanded?player.VisualGripPosition:player.VisualStockPosition);
                Vector3 expected=Vector3.ProjectOnPlane(game.PlayerWeapon.Direction,camera.transform.forward).normalized;
                Check(Vector3.Dot(Vector3.ProjectOnPlane(drawn,camera.transform.forward).normalized,expected)>.999f,"live firing axis agrees with actual shot / "+label+" / "+frame);
            }
            var scale=player.WeaponRenderer.transform.lossyScale;
            poseCsv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6},{11:F6},{12:F6},{13:F6},{14:F6}",label,frame,player.WeaponModel,player.ReviewRigDebug,Time.time,angle,player.RaisedBlend,scale.x,scale.y,ru,rf,lu,lf,rightError,leftError);
            foreach(var point in new[]{player.VisualGripPosition,player.VisualSupportGripPosition,player.VisualMuzzlePosition,player.VisualStockPosition,player.RightShoulderPosition,player.LeftShoulderPosition,player.RightElbowPosition,player.LeftElbowPosition})
                poseCsv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,",{0:F6},{1:F6}",Vector3.Dot(point,camera.transform.right),Vector3.Dot(point,camera.transform.up));
            poseCsv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,",{0:F6},{1:F6},{2:F6},{3:F6}\n",player.StockContactError,player.RightArmReach,player.LeftArmReach,Mathf.Max(player.WeaponRenderer.sprite.bounds.size.x*scale.x,player.WeaponRenderer.sprite.bounds.size.y*scale.y));
        }
        void ReadPoseTile(Camera camera,Texture2D strip,RenderTexture rt,int frame)
        {
            var target=camera.targetTexture;var active=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;strip.ReadPixels(new Rect(0,0,450,600),frame*450,0);}
            finally{camera.targetTexture=target;RenderTexture.active=active;}
        }
        IEnumerator CapturePoseFrames(VaultGame game,LayeredPlayer2D player,string label,Action<int> beforeFrame,Func<int,float> angle)
        {
            var strip=new Texture2D(2250,600,TextureFormat.RGB24,false);var rt=new RenderTexture(450,600,24);
            for(int frame=0;frame<5;frame++)
            {
                beforeFrame?.Invoke(frame);yield return null;yield return new WaitForEndOfFrame();
                RecordDynamicPose(game,player,label,frame,angle(frame));File.WriteAllText(Path.Combine(output,"dynamic-contacts.csv"),poseCsv.ToString());
                ReadPoseTile(game.GameCamera,strip,rt,frame);
            }
            strip.Apply(false);File.WriteAllBytes(Path.Combine(output,label+".png"),strip.EncodeToPNG());rt.Release();Destroy(rt);Destroy(strip);
        }
        IEnumerator CaptureBlendSweep(VaultGame game,LayeredPlayer2D player,string label,bool raising)
        {
            var strip=new Texture2D(2250,600,TextureFormat.RGB24,false);var rt=new RenderTexture(450,600,24);
            yield return new WaitForEndOfFrame();
            for(int frame=0;frame<5;frame++)
            {
                float target=raising?frame*.25f:1-frame*.25f;
                while(raising?player.RaisedBlend<target-.0001f:player.RaisedBlend>target+.0001f)yield return new WaitForEndOfFrame();
                Check(Mathf.Abs(player.RaisedBlend-target)<.005f,"live blend reaches matching capture point / "+label+" / "+target);
                RecordDynamicPose(game,player,label,frame,270);File.WriteAllText(Path.Combine(output,"dynamic-contacts.csv"),poseCsv.ToString());
                ReadPoseTile(game.GameCamera,strip,rt,frame);
                if(frame==0)player.SetAiming(raising);
            }
            strip.Apply(false);File.WriteAllBytes(Path.Combine(output,label+".png"),strip.EncodeToPNG());rt.Release();Destroy(rt);Destroy(strip);
        }
        IEnumerator CaptureDynamicPoses(VaultGame game)
        {
            var player=game.PlayerAppearance.GetComponent<LayeredPlayer2D>();var camera=game.GameCamera;
            camera.orthographicSize=1.35f;camera.aspect=.75f;camera.transform.position=game.PlayerPosition+camera.transform.up*.62f-camera.transform.forward*35;
            Time.captureDeltaTime=1f/140;
            var gear=(ArmorItem[])game.EquippedArmor.Clone();
            poseCsv.AppendLine("event,frame,model,debug,time,angle,aimBlend,scaleX,scaleY,rightUpper,rightFore,leftUpper,leftFore,rightContactError,leftContactError,gripX,gripY,supportX,supportY,muzzleX,muzzleY,stockX,stockY,rightShoulderX,rightShoulderY,leftShoulderX,leftShoulderY,rightElbowX,rightElbowY,leftElbowX,leftElbowY,stockContactError,right3DReach,left3DReach,hullMaxSize");
            // Exhaust the final articulated geometry without repeating the full
            // screenshot suite; dynamic images below exercise the live code path.
            for(int model=0;model<Arsenal.ModelCount;model++)for(int direction=0;direction<8;direction++)for(int phase=0;phase<4;phase++)
            {
                player.SetWeaponModel(model,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,phase/4f,true);
                CheckHandlingPose(game,player,"dynamic preflight run "+model+" / "+direction+" / "+phase);
            }
            Time.timeScale=1;
            foreach(bool debug in new[]{false,true})foreach(int model in new[]{1,14})
            {
                player.ApplyArmor(debug?new ArmorItem[5]:gear);player.ClearReviewPose();player.SetRigDebug(debug);player.SetWeaponModel(model,Color.white);player.SetAiming(false);yield return new WaitForSeconds(.5f);
                string prefix=(debug?"debug":"normal")+"-"+model;
                game.PlayerWeapon.Aim(LayeredPlayer2D.DirectionVector(camera,6));player.SetAiming(true);yield return new WaitForSeconds(.2f);player.SetAiming(false);yield return new WaitForSeconds(.2f);
                yield return CaptureBlendSweep(game,player,prefix+"-aim-start",true);
                yield return new WaitForSeconds(.2f);game.gun=Arsenal.Model(model,Rarity.Common);game.ammo=game.gun.magazine;game.reload=0;game.AimAt(game.PlayerPosition+LayeredPlayer2D.DirectionVector(camera,6)*10);game.Fire();yield return CapturePoseFrames(game,player,prefix+"-fire",null,f=>270);
                yield return new WaitForSeconds(.25f);yield return CaptureBlendSweep(game,player,prefix+"-release",false);
                yield return new WaitForSeconds(.25f);player.SetAiming(true);yield return new WaitForSeconds(.2f);
                for(int boundary=0;boundary<8;boundary++)
                {
                    float center=boundary*45+22.5f;
                    Action<int> rotate=frame=>{float a=(center+(frame-2)*.3f)*Mathf.Deg2Rad;Vector3 down=-Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;game.PlayerWeapon.Aim(camera.transform.right*Mathf.Sin(a)+down*Mathf.Cos(a));};
                    yield return CapturePoseFrames(game,player,prefix+"-turn-"+boundary,rotate,f=>center+(f-2)*.3f);
                }
            }
            File.WriteAllText(Path.Combine(output,"dynamic-contacts.csv"),poseCsv.ToString());Time.captureDeltaTime=0;
        }
        IEnumerator CaptureCalibrationLineups(VaultGame game,LayeredPlayer2D player)
        {
            var camera=game.GameCamera;float aspect=camera.aspect;var target=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(450,600,24);camera.aspect=.75f;camera.targetTexture=rt;
            try
            {
                foreach(int direction in new[]{0,6})
                {
                    var sheet=new Texture2D(2700,1800,TextureFormat.RGB24,false);
                    for(int model=0;model<Arsenal.ModelCount;model++)
                    {
                        player.SetWeaponModel(model,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,.05f,true);
                        CheckHandlingPose(game,player,"calibrated normal lineup "+model+" / "+direction);
                        camera.Render();RenderTexture.active=rt;
                        sheet.ReadPixels(new Rect(0,0,450,600),(model%6)*450,(2-model/6)*600);
                    }
                    sheet.Apply(false);File.WriteAllBytes(Path.Combine(output,"calibrated-models-0-to-17-d"+direction+".png"),sheet.EncodeToPNG());Destroy(sheet);
                }
            }
            finally{camera.aspect=aspect;camera.targetTexture=target;RenderTexture.active=active;rt.Release();Destroy(rt);}
            yield return null;
        }
        IEnumerator CaptureFocusedPoses(VaultGame game)
        {
            var player=game.PlayerAppearance.GetComponent<LayeredPlayer2D>();
            var camera=game.GameCamera;var baseGear=(ArmorItem[])game.EquippedArmor.Clone();
            camera.orthographicSize=1.35f;
            camera.transform.position=game.PlayerPosition+camera.transform.up*.62f-camera.transform.forward*35;
            Time.timeScale=0;
            foreach(bool debug in new[]{false,true})
            {
                if(debug)player.ApplyArmor(new ArmorItem[5]);
                foreach(int model in new[]{1,14})foreach(int direction in new[]{0,4,6,7})foreach(bool aim in new[]{false,true})
                {
                    player.SetWeaponModel(model,Color.white);
                    player.SetReviewPose(direction,aim?LayeredPlayer2D.Motion.Shoot:LayeredPlayer2D.Motion.Idle,aim?.05f:0,aim,debug);
                    Check(Vector3.Distance(player.RightHandPosition,player.VisualGripPosition)<.001f&&Vector3.Distance(player.LeftHandPosition,player.VisualSupportGripPosition)<.001f,"rendered hand contacts / "+model+" / "+direction);
                    Vector3 rUpper=Vector3.ProjectOnPlane(player.RightElbowPosition-player.RightShoulderPosition,camera.transform.forward),rFore=Vector3.ProjectOnPlane(player.RightHandPosition-player.RightElbowPosition,camera.transform.forward);
                    Vector3 lUpper=Vector3.ProjectOnPlane(player.LeftElbowPosition-player.LeftShoulderPosition,camera.transform.forward),lFore=Vector3.ProjectOnPlane(player.LeftHandPosition-player.LeftElbowPosition,camera.transform.forward);
                    Check(rUpper.magnitude<=.402f&&lUpper.magnitude<=.402f&&rFore.magnitude<=.382f&&lFore.magnitude<=.382f,"fixed arm lengths survive projection / "+model+" / "+direction);
                    if(aim)
                    {
                        if(!player.IsCatalogOneHanded)Check(player.StockContactError<.001f,"stock retains its shoulder socket after reach constraint / "+model+" / "+direction);
                        Vector3 shot=LayeredPlayer2D.DirectionVector(camera,direction);
                        Vector3 expected=Vector3.ProjectOnPlane(shot,camera.transform.forward).normalized;
                        Vector3 actual=Vector3.ProjectOnPlane(player.VisualMuzzlePosition-(player.IsCatalogOneHanded?player.VisualGripPosition:player.VisualStockPosition),camera.transform.forward).normalized;
                        Check(Vector3.Dot(expected,actual)>.999f,"rendered muzzle axis follows actual shot / "+model+" / "+direction);
                    }
                    yield return Capture((debug?"debug-":"normal-")+model+"-d"+direction+(aim?"-aim.png":"-low.png"));
                }
            }
            player.ApplyArmor(baseGear);
            yield return CaptureCalibrationLineups(game,player);
        }
        IEnumerator CaptureApprovedFieldkit(VaultGame game)
        {
            var player=game.PlayerAppearance.GetComponent<LayeredPlayer2D>();var camera=game.GameCamera;
            var savedGear=(ArmorItem[])game.EquippedArmor.Clone();int savedModel=player.WeaponModel;string savedVisualSet=player.WeaponVisualSet;
            var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize,savedScale=Time.timeScale;
            var approvedGear=new ArmorItem[5];for(int slot=0;slot<5;slot++)approvedGear[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=4};
            try
            {
                Time.timeScale=0;camera.transform.rotation=Quaternion.Euler(35.264f,0,0);camera.orthographicSize=1.5f;
                camera.transform.position=game.PlayerPosition+camera.transform.up*.43f-camera.transform.forward*35;
                player.ApplyArmor(approvedGear);player.SetWeaponVisualSet("trail-warden");player.SetWeaponModel(12,Color.white);
                player.SetReviewPose(0,LayeredPlayer2D.Motion.Idle,0,false);
                var renderer=player.ApprovedPoseRenderer;
                Check(renderer!=null&&renderer.Ready,"approved finished-pose atlas and measured metadata are ready / "+renderer?.ValidationError);
                Check(renderer.LoadedPoseCount>=24,"all three complete poses have eight distinct source cells");
                Check(renderer.HasPose("compact-low"),"the lowered sidearm has its own finished pistol pose rather than the rifle painting");
                var axisFailures=new System.Text.StringBuilder();var axisCsv=new System.Text.StringBuilder("model,direction,angleDegrees\n");
                foreach(int model in new[]{12,15})foreach(bool aim in new[]{false,true})for(int d=0;d<8;d++)
                {
                    player.SetWeaponModel(model,Color.white);player.SetReviewPose(d,aim?LayeredPlayer2D.Motion.Shoot:LayeredPlayer2D.Motion.Idle,0,aim);
                    Check(player.ApprovedPoseActive,"approved set uses the finished source pose / "+model+" / "+d+" / "+aim);
                    Check(renderer.CurrentDirection==d&&renderer.CurrentPose==(aim?(model==15?"compact":"aim"):model==15&&renderer.HasPose("compact-low")?"compact-low":"low"),"pose selection matches catalog and direction");
                    Check(Vector3.Distance(game.PlayerWeapon.MuzzlePosition,player.VisualMuzzlePosition)<.0001f,"actual muzzle equals measured source pixel in the same frame");
                    Check(Vector3.Distance(player.VisualGripPosition,renderer.Grip)<.0001f&&Vector3.Distance(player.VisualSupportGripPosition,renderer.Support)<.0001f,"both contacts use the displayed source pixels");
                    if(aim)
                    {
                        Vector3 expected=Vector3.ProjectOnPlane(LayeredPlayer2D.DirectionVector(camera,d),camera.transform.forward).normalized;
                        Vector3 actual=Vector3.ProjectOnPlane(renderer.Muzzle-renderer.BarrelStart,camera.transform.forward).normalized;
                        float angle=Vector3.Angle(expected,actual);axisCsv.AppendLine(model+","+d+","+angle.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
                        if(angle>20)axisFailures.Append(" model "+model+" / d"+d+" / "+angle.ToString("F1")+"deg;");
                    }
                    foreach(var old in player.BodyRenderers)Check(!old.enabled,"legacy articulated body is hidden for the approved pose");
                    Check(Mathf.Abs(renderer.PaintedRenderer.bounds.size.magnitude)<10,"approved source has bounded world size");
                    yield return Capture("approved-"+(aim?model==15?"compact":"aim":"low")+"-m"+model+"-d"+d+".png");
                }
                File.WriteAllText(Path.Combine(output,"approved-source-axis.csv"),axisCsv.ToString());
                Check(axisFailures.Length==0,"all aimed source barrels agree with actual eight-direction shots within 20 degrees:"+axisFailures);
                player.SetWeaponVisualSet(string.Empty);player.SetReviewPose(0,LayeredPlayer2D.Motion.Idle,0,false);
                Check(!player.ApprovedPoseActive,"legacy weapon selection restores the legacy renderer");
            }
            finally
            {
                player.ApplyArmor(savedGear);player.SetWeaponVisualSet(savedVisualSet);player.SetWeaponModel(savedModel,Color.white);player.ClearReviewPose();
                camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;Time.timeScale=savedScale;
            }
        }
        IEnumerator CaptureApprovedMotion(VaultGame game)
        {
            var player=game.PlayerAppearance.GetComponent<LayeredPlayer2D>();var camera=game.GameCamera;
            var savedGear=(ArmorItem[])game.EquippedArmor.Clone();int savedModel=player.WeaponModel;string savedSet=player.WeaponVisualSet;
            var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize,time=Time.timeScale;
            var target=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(256,320,24);var picture=new Texture2D(256,320,TextureFormat.RGB24,false);
            try
            {
                var gear=new ArmorItem[5];for(int slot=0;slot<5;slot++)gear[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=4};
                Time.timeScale=0;camera.transform.rotation=Quaternion.Euler(35.264f,0,0);camera.orthographicSize=1.5f;camera.transform.position=game.PlayerPosition+camera.transform.up*.43f-camera.transform.forward*35;camera.targetTexture=rt;
                player.ApplyArmor(gear);player.SetWeaponVisualSet("trail-warden");player.SetWeaponModel(12,Color.white);player.SetReviewPose(0,LayeredPlayer2D.Motion.Run,0,false);
                var approved=player.ApprovedPoseRenderer;
                Check(approved!=null&&approved.LocomotionReady,"all eight painted lower gait sequences and unchanged upper masks are available / "+approved?.LocomotionValidationError);
                if(approved.NeutralMeasurementsReady)
                {
                    var comparison=new Texture2D(512,320,TextureFormat.RGB24,false);
                    try
                    {
                        player.SetWeaponModel(12,Color.white);
                        for(int panel=0;panel<2;panel++)
                        {
                            approved.ReviewRigidNeutral=panel==1;player.SetReviewMovementDirection(1);player.SetReviewPose(1,panel==0?LayeredPlayer2D.Motion.Idle:LayeredPlayer2D.Motion.Run,0,false);
                            camera.Render();RenderTexture.active=rt;picture.ReadPixels(new Rect(0,0,256,320),0,0);picture.Apply(false);comparison.SetPixels(panel*256,0,256,320,picture.GetPixels());
                        }
                        comparison.Apply(false);File.WriteAllBytes(Path.Combine(output,"approved-d1-source-left-neutral-right.png"),comparison.EncodeToPNG());
                        File.WriteAllText(Path.Combine(output,"approved-d1-neutral-calibration.csv"),approved.NeutralCalibrationCsv());
                        Check(approved.MeasureNeutralGpu(camera,output),"actual rendered alpha cross-sections match independently measured source within five percent");
                    }
                    finally{approved.ReviewRigidNeutral=false;Destroy(comparison);}
                }
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--approved-neutral-only")>=0)yield break;
                var motionFailures=new System.Text.StringBuilder();var metrics=new System.Text.StringBuilder("model,aim,direction,distinctMeshes,rightTravel,leftTravel,maxFootStep,cycleGap,upperChanged\n");
                foreach(int configuration in new[]{0,1,2})for(int direction=0;direction<8;direction++)
                {
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"--approved-stride-d1")>=0&&direction!=1)continue;
                    int model=configuration==2?15:12;bool aim=configuration!=0;player.SetWeaponModel(model,Color.white);player.SetReviewMovementDirection(direction);
                    if(direction==1)
                    {
                        player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,0,aim);float firstOrder=Vector3.Dot(approved.RightFoot-approved.LeftFoot,camera.transform.right);
                        player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,.5f,aim);float oppositeOrder=Vector3.Dot(approved.RightFoot-approved.LeftFoot,camera.transform.right);
                        Check(firstOrder*oppositeOrder<0,"corrected d1 legs actually exchange front/back positions after half a cycle");
                        Check(approved.WaistJoinError<.001f,"lower hip remains attached to the upper belt seam");
                    }
                    var strip=new Texture2D(256*8,320*4,TextureFormat.RGB24,false);
                    var changes=new System.Collections.Generic.HashSet<string>();Vector3 firstRight=Vector3.zero,firstLeft=Vector3.zero,previousRight=Vector3.zero,previousLeft=Vector3.zero,firstMuzzle=Vector3.zero,firstLocalMuzzle=Vector3.zero;Sprite upper=null;float rightTravel=0,leftTravel=0,maxFootStep=0,cycleGap=0;bool upperChanged=false;
                    try
                    {
                        for(int sample=0;sample<=32;sample++)
                        {
                            player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,(sample%32)/32f,aim);
                            Check(player.ApprovedPoseActive&&approved.LocomotionActive&&(approved.CorrectedStrideActive||approved.LowerRenderer.enabled),"painted run is rendered, including measured lower interpolation");
                            if(sample==0){firstRight=approved.RightFoot;firstLeft=approved.LeftFoot;previousRight=firstRight;previousLeft=firstLeft;firstMuzzle=approved.Muzzle;firstLocalMuzzle=approved.PaintedRenderer.transform.InverseTransformPoint(approved.Muzzle);upper=approved.PaintedRenderer.sprite;}
                            else
                            {
                                upperChanged|=approved.PaintedRenderer.sprite!=upper;
                                // Shared whole-character flight translates the
                                // muzzle. Its offset inside the unchanged source
                                // painting must remain fixed instead.
                                Vector3 localMuzzle=approved.PaintedRenderer.transform.InverseTransformPoint(approved.Muzzle);
                                Check(Vector3.Distance(firstLocalMuzzle,localMuzzle)<.001f,"source muzzle pixel remains fixed during shared flight");
                                Check(approved.WaistJoinError<.001f,"pelvis follows upper flight without relative seam displacement");
                                maxFootStep=Mathf.Max(maxFootStep,Mathf.Max(Vector3.Distance(previousRight,approved.RightFoot),Vector3.Distance(previousLeft,approved.LeftFoot)));
                            }
                            if(sample==32){cycleGap=Mathf.Max(Vector3.Distance(firstRight,approved.RightFoot),Vector3.Distance(firstLeft,approved.LeftFoot));break;}
                            rightTravel=Mathf.Max(rightTravel,Vector3.Distance(firstRight,approved.RightFoot));leftTravel=Mathf.Max(leftTravel,Vector3.Distance(firstLeft,approved.LeftFoot));previousRight=approved.RightFoot;previousLeft=approved.LeftFoot;
                            var mesh=approved.LowerRenderer.GetComponent<MeshFilter>().sharedMesh;var vertices=approved.CorrectedStrideActive?new Vector3[0]:mesh.vertices;var signature=new System.Text.StringBuilder();for(int at=0;at<vertices.Length;at+=Mathf.Max(1,vertices.Length/24))signature.Append(vertices[at].ToString("F3"));if(approved.CorrectedStrideActive)signature.Append(approved.RigidPoseFingerprint);changes.Add(signature.ToString());
                            camera.Render();RenderTexture.active=rt;picture.ReadPixels(new Rect(0,0,256,320),0,0);picture.Apply(false);
                            if(direction==1)
                            {
                                var floor=camera.WorldToViewportPoint(game.PlayerPosition-Vector3.up*.65f);int baseline=Mathf.Clamp(Mathf.RoundToInt(floor.y*320),0,319);
                                for(int x=0;x<256;x++)picture.SetPixel(x,baseline,new Color(.1f,.8f,.9f));
                                foreach(var contact in new[]{approved.RightGroundContact,approved.LeftGroundContact})
                                {
                                    var projected=camera.WorldToViewportPoint(contact);int px=Mathf.RoundToInt(projected.x*256),py=Mathf.RoundToInt(projected.y*320);
                                    for(int offset=-4;offset<=4;offset++){if(px+offset>=0&&px+offset<256&&py>=0&&py<320)picture.SetPixel(px+offset,py,Color.magenta);if(px>=0&&px<256&&py+offset>=0&&py+offset<320)picture.SetPixel(px,py+offset,Color.magenta);}
                                }
                                picture.Apply(false);
                            }
                            strip.SetPixels(sample%8*256,(3-sample/8)*320,256,320,picture.GetPixels());
                        }
                        strip.Apply(false);File.WriteAllBytes(Path.Combine(output,"approved-run-m"+model+(aim?"-aim":"-low")+"-d"+direction+"-32.png"),strip.EncodeToPNG());
                        string context="m"+model+(aim?" aim":" low")+" d"+direction;
                        if(changes.Count<24)motionFailures.Append(context+" distinctMeshes="+changes.Count+"; ");
                        if(rightTravel<=.16f||leftTravel<=.16f)motionFailures.Append(context+" stride R="+rightTravel.ToString("F3")+" L="+leftTravel.ToString("F3")+"; ");
                        if(maxFootStep>=.28f)motionFailures.Append(context+" footStep="+maxFootStep.ToString("F3")+"; ");
                        if(cycleGap>=.0001f||upperChanged)motionFailures.Append(context+" cycle/upper invariant; ");
                        var invariant=System.Globalization.CultureInfo.InvariantCulture;
                        metrics.AppendLine(model+","+aim+","+direction+","+changes.Count+","+rightTravel.ToString("F4",invariant)+","+leftTravel.ToString("F4",invariant)+","+maxFootStep.ToString("F4",invariant)+","+cycleGap.ToString("F4",invariant)+","+upperChanged);
                    }
                    finally{Destroy(strip);}
                    yield return null;
                }
                player.SetReviewMovementDirection(0);player.SetReviewPose(2,LayeredPlayer2D.Motion.Run,.125f,true);
                Check(approved.CurrentLowerDirection==0&&player.CurrentDirection==2&&approved.LocomotionActive,"painted strafe keeps movement independent from aim");
                File.WriteAllText(Path.Combine(output,"approved-motion-metrics.csv"),metrics.ToString());
                Check(motionFailures.Length==0,"all direction motion metrics pass after saving every review strip: "+motionFailures);
            }
            finally
            {
                player.ApplyArmor(savedGear);player.SetWeaponVisualSet(savedSet);player.SetWeaponModel(savedModel,Color.white);player.ClearReviewPose();
                camera.targetTexture=target;RenderTexture.active=active;camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;Time.timeScale=time;rt.Release();Destroy(rt);Destroy(picture);
            }
        }
        IEnumerator CaptureApprovedEquipment(VaultGame game)
        {
            var player=game.PlayerAppearance.GetComponent<LayeredPlayer2D>();var camera=game.GameCamera;
            var savedGear=(ArmorItem[])game.EquippedArmor.Clone();int savedModel=player.WeaponModel;string savedSet=player.WeaponVisualSet;
            var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize,time=Time.timeScale;
            var full=new ArmorItem[5];for(int slot=0;slot<5;slot++)full[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=4};
            try
            {
                Time.timeScale=0;camera.transform.rotation=Quaternion.Euler(35.264f,0,0);camera.orthographicSize=1.5f;camera.transform.position=game.PlayerPosition+camera.transform.up*.43f-camera.transform.forward*35;
                player.ApplyArmor(full);player.SetWeaponVisualSet("trail-warden");player.SetWeaponModel(12,Color.white);player.SetReviewPose(0,LayeredPlayer2D.Motion.Idle,0,false);
                var approved=player.ApprovedPoseRenderer;
                Check(approved!=null&&approved.EquipmentLayersReady,"all approved clothing bases are available for real equipment removal / "+approved?.EquipmentValidationError);
                foreach(int model in new[]{12,15})foreach(bool aim in new[]{false,true})for(int direction=0;direction<8;direction++)for(int mask=0;mask<32;mask++)
                {
                    var subset=new ArmorItem[5];int count=0;for(int slot=0;slot<5;slot++)if((mask&(1<<slot))!=0){subset[slot]=full[slot];count++;}
                    player.ApplyArmor(subset);player.SetWeaponModel(model,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,aim);
                    Check(player.ApprovedPoseActive,"every new armor subset keeps the new painted character / "+model+" / "+direction+" / "+mask+" / "+approved.RenderValidationError);
                    Check(player.AppliedArmorCount==count&&approved.UsesCompositeEquipment==(mask!=31),"equipment subset selects the exact base plus requested overlays");
                    Check(approved.UnsupportedEquipmentCount==0,"every requested new armor slot has real painted coverage");
                    for(int slot=0;slot<5;slot++)Check(approved.EquipmentSlotVisible(slot)==((mask&(1<<slot))!=0&&(mask==31||!approved.EquipmentSlotOccluded(slot))),"removing a slot removes its actual renderer / "+slot+" / "+mask);
                    if(direction==7&&(mask==0||mask==31||mask==1||mask==2||mask==4||mask==8||mask==16))yield return Capture("approved-equipment-m"+model+(aim?"-aim":"-low")+"-mask"+mask+".png");
                }
                for(int removed=-1;removed<5;removed++)
                {
                    player.SetReviewMovementDirection(7);
                    var subset=removed<0?new ArmorItem[5]:(ArmorItem[])full.Clone();if(removed>=0)subset[removed]=null;
                    player.ApplyArmor(subset);player.SetWeaponModel(12,Color.white);player.SetReviewPose(7,LayeredPlayer2D.Motion.Run,.125f,true);
                    Check(player.ApprovedPoseActive&&approved.LocomotionActive,"removing armor keeps real painted running instead of a static fallback / "+removed+" / "+approved.RenderValidationError);
                    for(int slot=0;slot<5;slot++)Check(approved.EquipmentSlotVisible(slot)==(removed>=0&&removed!=slot),"running uses the requested upper/lower slot coverage / "+removed+" / "+slot);
                    yield return Capture("approved-equipment-run-remove"+removed+".png");
                }
                var mixed=(ArmorItem[])full.Clone();mixed[1]=new ArmorItem{slot=ArmorSlot.Chest,visualVariant=0};player.ApplyArmor(mixed);player.SetWeaponModel(12,Color.white);player.SetReviewPose(7,LayeredPlayer2D.Motion.Idle,0,false);
                Check(player.ApprovedPoseActive,"legacy armor item keeps the approved character base");
                Check(approved.UnsupportedEquipmentCount==1&&!approved.EquipmentSlotVisible(1),"legacy armor without matching painted art is explicitly recorded as a visual fallback");
                yield return Capture("approved-equipment-legacy-chest-fallback.png");
                player.ApplyArmor(full);player.SetWeaponVisualSet(string.Empty);
                Check(approved.WeaponlessReady,"legacy weapon compatibility has real weaponless and hand-occlusion paintings");
                var axes=new System.Text.StringBuilder("model,direction,projectedBarrelAngle\n");
                foreach(int model in new[]{0,1,4,12,14,15})foreach(int direction in new[]{0,4,7})
                {
                    player.SetWeaponModel(model,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,true);
                    Check(player.ApprovedPoseActive&&approved.UsesExternalWeapon&&player.WeaponRenderer.enabled,"legacy weapon uses the new painted character and an independent firearm");
                    Check(Vector3.Distance(player.RightHandPosition,player.VisualGripPosition)<.002f&&Vector3.Distance(player.LeftHandPosition,player.VisualSupportGripPosition)<.002f,"legacy firearm is fitted rigidly to both authored hand contacts");
                    Check(Vector3.Distance(game.PlayerWeapon.MuzzlePosition,player.VisualMuzzlePosition)<.0001f,"legacy firearm projectile socket is its actual displayed muzzle");
                    var axis=Vector3.ProjectOnPlane(player.VisualMuzzlePosition-player.VisualStockPosition,camera.transform.forward).normalized;
                    var expected=Vector3.ProjectOnPlane(LayeredPlayer2D.DirectionVector(camera,direction),camera.transform.forward).normalized;
                    axes.AppendLine(model+","+direction+","+Vector3.Angle(axis,expected).ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
                    yield return Capture("approved-legacy-weapon-m"+model+"-d"+direction+".png");
                }
                File.WriteAllText(Path.Combine(output,"approved-legacy-barrel-diagnostics.csv"),axes.ToString());
            }
            finally
            {
                player.ApplyArmor(savedGear);player.SetWeaponVisualSet(savedSet);player.SetWeaponModel(savedModel,Color.white);player.ClearReviewPose();camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;Time.timeScale=time;
            }
        }
        IEnumerator VerifyLayeredVisuals(VaultGame game,bool complete)
        {
            var appearance=game.PlayerAppearance;var player=appearance.GetComponent<LayeredPlayer2D>();
            Check(player!=null&&player.Ready,"all five authored body views load and supply eight facings");
            Check(player.LoadedArmorDirections==5,"all five directional armor sheets load");
            Check(player.AppliedArmorCount==5,"all five equipped slots are forwarded into independent 2D layers");
            Check(VisibleMeshes(appearance.transform)==0,"player body and weapon have no visible 3D mesh");
            Check(player.DirectionCount==8&&LayeredPlayer2D.WalkSamples==64&&LayeredPlayer2D.IdleSamples==32&&LayeredPlayer2D.ShootSamples==32,"dense cached walk idle and shoot sample counts");
            var savedGear=(ArmorItem[])game.EquippedArmor.Clone();int savedModel=VaultLanguage.DisplayModel(game.gun);
            for(int direction=0;direction<8;direction++)
            {
                game.AimAt(game.PlayerPosition+LayeredPlayer2D.DirectionVector(game.GameCamera,direction)*10);
                player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);
                if(direction==3||direction==4||direction==5)
                {
                    int torsoOrder=player.BodyRenderers[1].sortingOrder;
                    Check(player.WeaponRenderer.sortingOrder<torsoOrder,"rear-view low-ready weapon is occluded with the support hands / "+direction);
                    Check(player.BodyRenderers[3].sortingOrder<torsoOrder&&player.BodyRenderers[6].sortingOrder<torsoOrder,"rear-view upper arms are occluded by torso during low carry / "+direction);
                    Check(player.BodyRenderers[4].sortingOrder<torsoOrder&&player.BodyRenderers[7].sortingOrder<torsoOrder,"rear-view forearms are occluded by torso / "+direction);
                    Check(player.BodyRenderers[5].sortingOrder<torsoOrder&&player.BodyRenderers[8].sortingOrder<torsoOrder,"rear-view hands stay with their arms behind the torso / "+direction);
                }
                player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,0,true);
                CheckHandlingPose(game,player,"direction "+direction);
                Check(player.CurrentDirection==direction&&player.WeaponReady,"body and weapon directional images active / "+direction);
                Check(Vector3.Distance(game.PlayerWeapon.MuzzlePosition,player.VisualMuzzlePosition)<.0001f,"visual muzzle equals actual projectile socket / "+direction);
                Check(HandsMatchHandling(appearance,.001f),"painted hands meet both weapon grips / "+direction);
                int visible=0;foreach(var r in player.BodyRenderers)if(r.enabled&&r.sprite!=null)visible++;
                Check(visible==(player.HasArmoredGloves?13:15),"body hands are replaced by independent armor gloves without duplicated hands / "+direction);
                if(player.HasArmoredGloves)
                {
                    Check(player.ArmorHandRenderers[0].enabled&&player.ArmorHandRenderers[1].enabled,"exactly two independent armor gloves visible / "+direction);
                    Check(Vector3.ProjectOnPlane(player.ArmorHandRenderers[0].transform.position-player.VisualGripPosition,game.GameCamera.transform.forward).magnitude<.001f&&Vector3.ProjectOnPlane(player.ArmorHandRenderers[1].transform.position-player.LeftHandPosition,game.GameCamera.transform.forward).magnitude<.001f,"rendered glove centers meet actual trigger and support grips in image plane / "+direction);
                }
                foreach(var r in player.BodyRenderers)Check(r.bounds.size.magnitude>0&&r.bounds.size.magnitude<2,"painted limb has finite compact bounds / "+direction);
                foreach(var r in player.ArmorRenderers)Check(r.enabled&&r.sprite!=null,"equipped directional armor overlay exists / "+direction);
                var samples=new System.Collections.Generic.HashSet<string>();float minimumLegWidth=float.MaxValue,maximumLegWidth=0;
                for(int frame=0;frame<64;frame++){player.SetReviewPose(direction,LayeredPlayer2D.Motion.Walk,frame/64f,false);var leg=player.BodyRenderers[10].transform;samples.Add(leg.position.ToString("F5")+leg.rotation.ToString("F5"));var shin=player.BodyRenderers[10];float width=shin.sprite.bounds.size.x*Mathf.Abs(shin.transform.lossyScale.x);minimumLegWidth=Mathf.Min(minimumLegWidth,width);maximumLegWidth=Mathf.Max(maximumLegWidth,width);}
                Check(samples.Count>=60,"64 walking samples produce distinct articulated leg poses / "+direction);
                Check(maximumLegWidth-minimumLegWidth<.004f,"walking shin width stays stable instead of ballooning / "+direction);
                Check(minimumLegWidth>=.214f,"walking calves retain substantial width in every direction / "+direction);
                float minFoot=float.MaxValue,maxFoot=float.MinValue;
                for(int frame=0;frame<64;frame++){player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,frame/64f,false);float height=Vector3.Dot(player.RightFootPosition,game.GameCamera.transform.up);minFoot=Mathf.Min(minFoot,height);maxFoot=Mathf.Max(maxFoot,height);}
                Check(maxFoot-minFoot>.30f,"running heel clearly lifts off the planted level instead of shuffling / "+direction);
            }
            VerifyGaitGeometry(player,game.GameCamera);
            player.SetReviewPose(1,LayeredPlayer2D.Motion.Idle,0,false);
            var allArmorStyles=new System.Collections.Generic.HashSet<ulong>();
            for(int style=0;style<LayeredPlayer2D.ArmorStyleCount;style++){var styleGear=(ArmorItem[])savedGear.Clone();for(int slot=0;slot<5;slot++)styleGear[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=style};player.ApplyArmor(styleGear);allArmorStyles.Add(LayeredFingerprint(game));}
            Check(allArmorStyles.Count==LayeredPlayer2D.ArmorStyleCount,"all four armor designs produce distinct complete rendered appearances");player.ApplyArmor(savedGear);
            for(int slot=0;slot<5;slot++)
            {
                string before=player.AppearanceSignature();ulong image=complete?LayeredFingerprint(game):0;var gear=(ArmorItem[])savedGear.Clone();var original=gear[slot];gear[slot]=new ArmorItem{slot=(ArmorSlot)slot,visualVariant=original.visualVariant+1,color=new Color(.35f,.8f,.9f)};
                player.ApplyArmor(gear);Check(before!=player.AppearanceSignature(),"changing armor slot changes selected painted geometry / "+slot);if(complete)Check(image!=LayeredFingerprint(game),"armor slot swap changes actual rendered pixels / "+slot);player.ApplyArmor(savedGear);
            }
            var models=new System.Collections.Generic.HashSet<string>();
            var modelImages=new System.Collections.Generic.HashSet<ulong>();
            for(int model=0;model<Arsenal.ModelCount;model++)
            {
                player.SetWeaponModel(model,Color.white);player.SetReviewPose(1,LayeredPlayer2D.Motion.Shoot,.05f,true);Check(player.WeaponReady,"painted weapon model sheet exists / "+model);models.Add(player.WeaponRenderer.sprite.name);if(complete)modelImages.Add(LayeredFingerprint(game));
                for(int direction=0;direction<8;direction++){player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);CheckCarryPose(game,player,model+" idle / "+direction);CheckCarryPose(game,player,model+" moving / "+direction,LayeredPlayer2D.Motion.Run,.5f);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,.05f,true);CheckHandlingPose(game,player,model+" / "+direction);for(int phase=0;phase<4;phase++){player.SetReviewPose(direction,LayeredPlayer2D.Motion.Run,phase/4f,true);CheckHandlingPose(game,player,"run "+model+" / "+direction+" / "+phase);}}
            }
            if(complete)Check(modelImages.Count==Arsenal.ModelCount,"all weapon swaps change actual rendered pixels");
            Check(models.Count==Arsenal.ModelCount,"all weapon models select distinct directional images");player.SetWeaponModel(savedModel,game.gun.color);
            if(complete)
            {
                Time.timeScale=0;var camera=game.GameCamera;float size=camera.orthographicSize;var position=camera.transform.position;
                camera.orthographicSize=1.35f;camera.transform.position=game.PlayerPosition+camera.transform.up*.62f-camera.transform.forward*35;
                for(int direction=0;direction<8;direction++){player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);yield return Capture("layered-low-"+direction+".png");player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,.05f,true);yield return Capture("layered-aim-"+direction+".png");}
                foreach(int compactModel in new[]{1,14})for(int direction=0;direction<8;direction++)
                {
                    player.SetWeaponModel(compactModel,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false);yield return Capture("twohand-low-model-"+compactModel+"-"+direction+".png");
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,.05f,true);yield return Capture("twohand-aim-model-"+compactModel+"-"+direction+".png");
                }
                // QA-only bare-body rig landmarks make shoulder, elbow, hand and gun contacts inspectable.
                player.ApplyArmor(new ArmorItem[5]);
                foreach(int compactModel in new[]{14,1})for(int direction=0;direction<8;direction++)
                {
                    player.SetWeaponModel(compactModel,Color.white);player.SetReviewPose(direction,LayeredPlayer2D.Motion.Idle,0,false,true);yield return Capture("pose-debug-low-model-"+compactModel+"-"+direction+".png");
                    player.SetReviewPose(direction,LayeredPlayer2D.Motion.Shoot,.05f,true,true);yield return Capture("pose-debug-aim-model-"+compactModel+"-"+direction+".png");
                }
                player.ApplyArmor(savedGear);
                player.SetWeaponModel(savedModel,game.gun.color);
                RenderLayeredStrips(game,player);
                camera.orthographicSize=size;camera.transform.position=position;
            }
            player.ClearReviewPose();player.SetAiming(false);Time.timeScale=1;yield return null;
            if(!complete)yield break;
            // A real input-independent distance-driven walk and strafe with fixed aim.
            var start=appearance.transform.position;var actual=new System.Collections.Generic.HashSet<string>();bool enabled=game.enabled;game.enabled=false;
            game.AimAt(start+LayeredPlayer2D.DirectionVector(game.GameCamera,2)*10);player.SetAiming(true);
            for(int frame=0;frame<64;frame++){appearance.transform.position=start+LayeredPlayer2D.DirectionVector(game.GameCamera,0)*(frame*.015f);yield return null;actual.Add(player.BodyRenderers[10].transform.position.ToString("F4"));}
            Check(actual.Count>50&&player.MovementDirection!=player.CurrentDirection,"actual distance-driven walking stays independent of fixed weapon aim");appearance.transform.position=start;player.ClearReviewPose();player.SetAiming(false);
            Vector3 lane=Vector3.zero;for(int d=0;d<8;d++){var candidate=LayeredPlayer2D.DirectionVector(game.GameCamera,d);if(game.Layout.ClearLine(start,start+candidate*5,.65f)){lane=candidate;break;}}
            Check(lane.sqrMagnitude>.5f,"layered shooting review has clear target lane");
            var targetObject=new GameObject("Layered first shot target");targetObject.transform.SetParent(appearance.transform.parent,false);var target=targetObject.AddComponent<Foe>();target.Init(game,1,0);target.enabled=false;target.hp=target.maxHp=1000;target.transform.position=start+lane*4;game.Enemies.Add(target);
            game.gun=Arsenal.Starter();game.gun.accuracy=100;game.ammo=game.gun.magazine;game.reload=0;player.SetWeaponModel(0,game.gun.color);game.AimAt(target.transform.position);
            int ammo=game.ammo,numbers=CombatVisuals.TotalDamageNumbers;game.Fire();
            Check(game.ammo==ammo-1&&player.IsAiming,"firing consumes cartridge and immediately raises painted weapon");
            Shot shot=null;foreach(var candidate in FindObjectsByType<Shot>(FindObjectsSortMode.None))if(candidate.friendly)shot=candidate;
            Check(shot!=null&&Vector3.Distance(shot.transform.position,player.VisualMuzzlePosition)<.001f,"real first projectile starts at visible recoiling 2D muzzle in same frame");
            var expectedDirection=target.transform.position-shot.transform.position;expectedDirection.y=0;
            Check(Vector3.Dot(shot.velocity.normalized,expectedDirection.normalized)>.997f,"real projectile aims from painted muzzle toward target");
            yield return Capture("layered-first-shot.png");yield return new WaitForSeconds(.45f);
            Check(Mathf.Abs(target.hp-(1000-game.EffectiveDamage))<.01f&&CombatVisuals.TotalDamageNumbers==numbers+1,"real layered-player shot deals expected damage and hit number");
            Check(!player.IsAiming&&player.RaisedBlend<.1f,"shot and recoil finish then return to low carry");
            game.Enemies.Remove(target);Destroy(targetObject);game.enabled=enabled;yield return Capture("layered-gameplay.png");
        }
        void RenderLayeredStrips(VaultGame game,LayeredPlayer2D player)
        {
            const int size=160;var camera=game.GameCamera;var target=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(size,size,24);var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);camera.targetTexture=rt;
            try
            {
                for(int d=0;d<8;d++)for(int state=0;state<4;state++)
                {
                    int count=state==1||state==3?64:32,rows=count/16;var strip=new Texture2D(size*16,size*rows,TextureFormat.RGBA32,false);var images=new System.Collections.Generic.HashSet<ulong>();
                    for(int sample=0;sample<count;sample++){player.SetReviewPose(d,(LayeredPlayer2D.Motion)state,sample/(float)count,state==2);camera.Render();RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply(false);images.Add(PixelFingerprint(frame));strip.SetPixels(sample%16*size,(rows-1-sample/16)*size,size,size,frame.GetPixels());}
                    Check(images.Count>=(state==1||state==3?60:24),"dense rendered sequence contains changing painted pixels / direction "+d+" / "+(LayeredPlayer2D.Motion)state+" / "+images.Count);
                    strip.Apply(false);File.WriteAllBytes(Path.Combine(output,"layered-motion-"+d+"-"+((LayeredPlayer2D.Motion)state).ToString().ToLowerInvariant()+"-"+count+".png"),strip.EncodeToPNG());DestroyImmediate(strip);
                }
            }
            finally {camera.targetTexture=target;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(frame);}
            File.WriteAllText(Path.Combine(output,"layered-motion-manifest.txt"),"1536 rendered articulated poses: 8 directions x (64 walk + 64 run + 32 idle + 32 shoot). Five AI-painted neutral body and armor views mirrored into eight directions, independent armor and whole-weapon images; cached joint interpolation. These are actual composited 2D renders, not 1536 separately AI-authored drawings.");
        }
        static ulong PixelFingerprint(Texture2D texture)
        {ulong hash=1469598103934665603UL;foreach(var p in texture.GetPixels32()){hash^=(ulong)(p.r|(p.g<<8)|(p.b<<16)|(p.a<<24));hash*=1099511628211UL;}return hash;}
        static ulong LayeredFingerprint(VaultGame game)
        {
            var camera=game.GameCamera;var previous=camera.targetTexture;var active=RenderTexture.active;var position=camera.transform.position;float size=camera.orthographicSize;
            var rt=new RenderTexture(192,192,24);var image=new Texture2D(192,192,TextureFormat.RGBA32,false);
            try{camera.orthographicSize=1.35f;camera.transform.position=game.PlayerPosition+camera.transform.up*.95f-camera.transform.forward*35;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,192,192),0,0);image.Apply(false);return PixelFingerprint(image);}
            finally{camera.targetTexture=previous;camera.transform.position=position;camera.orthographicSize=size;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);}
        }
        IEnumerator VerifyMeleeCombat(VaultGame game)
        {
            Check(VaultGame.MeleeAttackMouseButton==1&&VaultGame.MeleeAttackKey==KeyCode.Mouse1,"melee control is exposed as right mouse button");
            var target=game.Enemies.Find(enemy=>enemy!=null&&!enemy.dead&&enemy.kind!=3);
            Check(target!=null,"melee combat has a non-boss target");if(target==null)yield break;
            var oldEquipped=game.EquippedMeleeWeapon;float oldChain=game.runChain;
            Vector3 origin=game.PlayerPosition,axis=Vector3.zero;
            for(int angle=0;angle<360;angle+=45)
            {var candidate=Quaternion.Euler(0,angle,0)*Vector3.forward;if(game.Layout.ClearLine(origin+Vector3.up*.7f,origin+candidate*5+Vector3.up*.7f,.1f)){axis=candidate;break;}}
            Check(axis.sqrMagnitude>.5f,"melee test has a clear attack direction");if(axis.sqrMagnitude<.5f)yield break;
            Vector3 perpendicular=Vector3.Cross(Vector3.up,axis).normalized;
            int oldKind=target.kind;
            var enemies=game.Enemies.ToArray();var positions=new Vector3[enemies.Length];var speeds=new float[enemies.Length];var timers=new float[enemies.Length];var healths=new float[enemies.Length];var maxHealths=new float[enemies.Length];var kinds=new int[enemies.Length];
            for(int i=0;i<enemies.Length;i++)
            {
                var foe=enemies[i];if(foe==null)continue;positions[i]=foe.transform.position;speeds[i]=foe.speed;timers[i]=foe.timer;healths[i]=foe.hp;maxHealths[i]=foe.maxHp;kinds[i]=foe.kind;foe.speed=0;foe.timer=9999;
                if(foe!=target)foe.transform.position=origin+axis*(12+i*.08f);
            }
            target.kind=1;target.hp=target.maxHp=5000;
            var stats=new[]{
                new MeleeWeaponItem{family=MeleeWeaponFamily.Knife,damage=18,interval=.62f,range=1.2f,arcDegrees=75},
                new MeleeWeaponItem{family=MeleeWeaponFamily.Shiv,damage=22,interval=.78f,range=1.0f,arcDegrees=60},
                new MeleeWeaponItem{family=MeleeWeaponFamily.Cleaver,damage=32,interval=.95f,range=1.15f,arcDegrees=105},
                new MeleeWeaponItem{family=MeleeWeaponFamily.Machete,damage=27,interval=.82f,range=1.65f,arcDegrees=90},
                new MeleeWeaponItem{family=MeleeWeaponFamily.Hatchet,damage=36,interval=1.05f,range=1.4f,arcDegrees=115}
            };
            foreach(var item in stats)
            {
                item.id=System.Guid.NewGuid().ToString("N");Check(game.StoreMeleeWeapon(item),"melee family enters inventory / "+item.family);
                game.EquipMeleeWeapon(item);game.ResetMeleeCooldown();target.transform.position=origin+axis*.8f;target.hp=5000;
                // Aim along the test axis from a distant point so the weapon muzzle offset cannot skew narrow melee fans.
                game.AimAt(origin+axis*20);int numbers=CombatVisuals.TotalDamageNumbers;float health=target.hp;
                if(item.family==MeleeWeaponFamily.Knife)game.Enemies.Add(target); // Duplicate list references must still receive one hit.
                bool attacked=game.AttackMelee();if(item.family==MeleeWeaponFamily.Knife)game.Enemies.RemoveAt(game.Enemies.Count-1);
                Check(attacked&&target.hp<health&&CombatVisuals.TotalDamageNumbers==numbers+1,"one right-click melee swing hits once / "+item.family);
                float afterFirst=target.hp;Check(!game.AttackMelee()&&Mathf.Abs(target.hp-afterFirst)<.001f,"cooldown rejects repeated melee input / "+item.family);
                bool visual=false;foreach(var tr in FindObjectsByType<Transform>())if(tr.name=="Melee slash / outer edge"&&tr.GetComponent<LineRenderer>()!=null){visual=true;break;}
                Check(visual,"melee swing draws a visible slash / "+item.family);
                game.ResetMeleeCooldown();target.transform.position=origin+perpendicular*.8f;target.hp=5000;game.AimAt(origin+axis*5);float missHealth=target.hp;
                Check(game.AttackMelee()&&Mathf.Abs(target.hp-missHealth)<.001f,"survivor outside family fan arc / "+item.family);
                game.ResetMeleeCooldown();target.transform.position=origin+axis*(item.range+.80f);target.hp=5000;game.AimAt(target.transform.position);missHealth=target.hp;
                Check(game.AttackMelee()&&Mathf.Abs(target.hp-missHealth)<.001f,"survivor outside family range / "+item.family);
            }

            // A direct melee hit still calls the shared run-chain hit hook.
            var knife=stats[0];
            knife.arcDegrees=150;knife.range=1.0f;game.ResetMeleeCooldown();game.runChain=.35f;
            target.transform.position=origin+axis*.8f;target.hp=5000;
            Foe chained=null;foreach(var foe in enemies)if(foe!=null&&foe!=target&&!foe.dead){chained=foe;break;}
            if(chained!=null)
            {
                chained.transform.position=origin+axis*2.4f;chained.hp=chained.maxHp=5000;game.AimAt(target.transform.position);float chainHealth=chained.hp;
                game.AttackMelee();Check(chained.hp<chainHealth,"melee hit enters existing run-chain synergy");
            }
            game.runChain=oldChain;game.ResetMeleeCooldown();if(oldEquipped!=null)game.EquipMeleeWeapon(oldEquipped);
            for(int i=0;i<enemies.Length;i++)
            {
                var foe=enemies[i];if(foe==null)continue;foe.transform.position=positions[i];foe.speed=speeds[i];foe.timer=timers[i];foe.hp=healths[i];foe.maxHp=maxHealths[i];foe.kind=kinds[i];
            }
            target.kind=oldKind;
            yield return null;
        }
        IEnumerator VerifyMutantBehavior(VaultGame game)
        {
            bool wasEnabled=game.enabled;game.enabled=false;
            foreach(var enemy in game.Enemies)enemy.enabled=false;
            var actor=new GameObject("Isolated mutant behavior smoke");actor.transform.SetParent(game.PlayerAppearance.transform.parent,false);
            var mutant=actor.AddComponent<Foe>();mutant.Init(game,0,0);mutant.speed=0;mutant.timer=0;
            Vector3 origin=game.PlayerPosition,axis=Vector3.zero;
            for(int angle=0;angle<360;angle+=45){var candidate=Quaternion.Euler(0,angle,0)*Vector3.forward;if(game.Layout.ClearLine(origin,origin+candidate*4,.65f)){axis=candidate;break;}}
            Check(axis.sqrMagnitude>.5f,"isolated attack test has clear escape direction");
            actor.transform.position=origin+axis*1.7f;
            float hpBefore=game.hp;yield return null;
            Check(mutant.IsTelegraphing&&!mutant.IsLunging&&game.hp==hpBefore,"shambler warns before melee instead of unavoidable contact damage");
            // Leaving the attack's fixed reach during the warning avoids the hit.
            actor.transform.position=origin+axis*4;game.invincible=0;
            yield return new WaitForSeconds(mutant.TelegraphSeconds+.12f);
            Check(game.hp==hpBefore,"moving out of a telegraphed melee swing prevents damage");
            game.invincible=999;mutant.kind=4;mutant.timer=0;actor.transform.position=origin+axis*3.3f;
            yield return null;Check(mutant.IsTelegraphing&&!mutant.IsLunging,"pouncer exposes a warning before rapid lunge");
            yield return new WaitForSeconds(mutant.TelegraphSeconds+.03f);
            Check(mutant.IsLunging,"pouncer follows warning with rapid committed movement");
            mutant.enabled=false;Destroy(actor);yield return null;
            var bloaterObject=new GameObject("Isolated bloater smoke");bloaterObject.transform.SetParent(game.PlayerAppearance.transform.parent,false);bloaterObject.transform.position=origin+axis*3.6f;
            var bloater=bloaterObject.AddComponent<Foe>();bloater.Init(game,7,0);bloater.speed=0;bloater.timer=0;
            yield return null;Check(bloater.IsTelegraphing,"bloater exposes a readable self-centered explosion warning");
            game.invincible=0;yield return new WaitForSeconds(bloater.TelegraphSeconds+.3f);
            Check(game.hp==hpBefore,"remaining outside bloater warning radius avoids its explosion");
            game.invincible=999;bloater.enabled=false;Destroy(bloaterObject);yield return null;
            var acidObject=new GameObject("Isolated acid residue smoke");acidObject.transform.SetParent(game.PlayerAppearance.transform.parent,false);acidObject.transform.position=new Vector3(origin.x,.08f,origin.z);
            var acid=acidObject.AddComponent<AcidResidue>();acid.game=game;acid.damage=1;
            game.invincible=0;yield return new WaitForSeconds(.2f);Check(game.hp==hpBefore,"acid residue gives an escape window before its first damaging tick");
            yield return new WaitForSeconds(.5f);Check(game.hp<hpBefore,"acid residue deals actual damage after its warning delay");
            game.hp=hpBefore;game.invincible=999;Destroy(acidObject);yield return null;
            var carrier=new GameObject("Isolated regeneration smoke");carrier.transform.SetParent(game.PlayerAppearance.transform.parent,false);carrier.transform.position=origin+axis*3.6f;
            var healer=carrier.AddComponent<Foe>();healer.Init(game,9,0);healer.speed=0;healer.timer=0;
            var allyObject=new GameObject("Wounded mutant smoke");allyObject.transform.SetParent(carrier.transform,false);
            var ally=allyObject.AddComponent<Foe>();ally.Init(game,0,0);ally.enabled=false;ally.hp=10;game.Enemies.Add(ally);
            yield return null;Check(healer.IsTelegraphing,"regeneration mother signals its healing pulse");
            yield return new WaitForSeconds(healer.TelegraphSeconds+.12f);
            Check(ally.hp>10&&ally.hp<=ally.maxHp,"regeneration mother restores nearby mutant health with bounded healing");
            game.Enemies.Remove(ally);Destroy(carrier);yield return null;
            var cyclerObject=new GameObject("Isolated radial warning smoke");cyclerObject.transform.SetParent(game.PlayerAppearance.transform.parent,false);cyclerObject.transform.position=origin+axis*3.6f;
            var cycler=cyclerObject.AddComponent<Foe>();cycler.Init(game,6,0);cycler.speed=0;cycler.timer=0;
            yield return null;
            var barrage=GameObject.Find("Radial barrage warning");
            Check(cycler.IsTelegraphing&&barrage!=null&&barrage.GetComponentsInChildren<LineRenderer>().Length==9,"rad cycler signals all nine barrage directions before firing");
            cycler.enabled=false;Destroy(cyclerObject);Destroy(barrage);yield return null;
            var platedObject=new GameObject("Isolated armored facing smoke");platedObject.transform.SetParent(game.PlayerAppearance.transform.parent,false);platedObject.transform.position=origin+axis*3;
            var plated=platedObject.AddComponent<Foe>();plated.Init(game,8,0);plated.enabled=false;plated.hp=1000;
            plated.Hit(100,platedObject.transform.position+platedObject.transform.forward*.6f);
            float frontDamage=1000-plated.hp;
            plated.hp=1000;plated.Hit(100,platedObject.transform.position-platedObject.transform.forward*.6f);
            float rearDamage=1000-plated.hp;
            Check(Mathf.Abs(frontDamage-55)<.01f&&Mathf.Abs(rearDamage-135)<.01f,"plated corpse actual hit damage rewards flanking its frontal armor");
            Check(Mathf.Abs(CombatVisuals.LastDamage-rearDamage)<.01f,"plated corpse damage number reports actual rear vulnerability damage");
            Destroy(platedObject);
            foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=true;
            game.enabled=wasEnabled;game.invincible=999;
        }
        IEnumerator VerifyEquipment(VaultGame game)
        {
            Check(game.VerifyPersistence(),"loadout saves and restores through actual PlayerPrefs path using isolated temporary key");
            Check(game.VerifyTrailWardenMigration(),"Trail Warden one-time migration preserves full save and restores snapshot");
            game.ToggleInventory();
            Check(game.InventoryOpen&&game.screen==VaultGame.Screen.Menu&&Time.timeScale==0,"menu inventory pauses without starting a run");
            VaultLanguage.Set(true,false);yield return Capture("inventory-ja.png");
            VaultLanguage.Set(false,false);yield return Capture("inventory-en.png");
            game.ToggleInventory();Check(!game.InventoryOpen&&game.screen==VaultGame.Screen.Menu&&Time.timeScale==1,"menu inventory closes back to menu");
            var seedA=new System.Random(8675309);var seedB=new System.Random(8675309);
            bool variation=false;Gun prior=null;
            for(int i=0;i<30;i++)
            {
                var a=Arsenal.Legendary(0,seedA,4);var b=Arsenal.Legendary(0,seedB,4);
                Check(a.level==4&&a.effect==GunEffect.Ricochet&&a.damage>0&&a.interval>0&&a.magazine>0&&a.reloadSeconds>0,"valid leveled legendary roll "+i);
                Check(a.damage==b.damage&&a.interval==b.interval&&a.speed==b.speed&&a.magazine==b.magazine&&a.reloadSeconds==b.reloadSeconds,"seed reproduces weapon stats "+i);
                if(prior!=null)variation|=a.damage!=prior.damage&&a.interval!=prior.interval&&a.speed!=prior.speed&&a.reloadSeconds!=prior.reloadSeconds&&a.id!=prior.id;
                prior=a;
            }
            Check(variation,"same type rarity and level produce distinct weapon stats and IDs");
            var low=Arsenal.Legendary(1,new System.Random(99),1);var high=Arsenal.Legendary(1,new System.Random(99),8);
            Check(high.damage>low.damage&&high.effect==low.effect,"weapon level grows damage while preserving effect");
            seedA=new System.Random(7654);seedB=new System.Random(7654);
            for(int i=0;i<30;i++)
            {
                var a=Armory.Roll(seedA,3);var b=Armory.Roll(seedB,3);
                Check(a.slot==b.slot&&a.rarity==b.rarity&&a.damageReduction==b.damageReduction&&a.maxHealth==b.maxHealth&&a.damageBonus==b.damageBonus&&a.fireBonus==b.fireBonus&&a.moveBonus==b.moveBonus&&a.reloadBonus==b.reloadBonus&&a.dodgeBonus==b.dodgeBonus&&a.regenBonus==b.regenBonus&&a.visualVariant==b.visualVariant,"seed reproduces armor stats "+i);
            }
            var armorSamples=new System.Collections.Generic.Dictionary<string,ArmorItem>();
            var variedSlots=new System.Collections.Generic.HashSet<ArmorSlot>();
            for(int i=0;i<300;i++)
            {
                var item=Armory.Roll(seedA,3);string key=item.slot+"/"+item.rarity;
                if(armorSamples.TryGetValue(key,out var priorArmor)&&item.damageReduction!=priorArmor.damageReduction&&item.id!=priorArmor.id)variedSlots.Add(item.slot);
                armorSamples[key]=item;
            }
            Check(variedSlots.Count==5,"same armor slot rarity and level vary stats and IDs for all five slots");
            var grownSlots=new System.Collections.Generic.HashSet<ArmorSlot>();
            for(int i=0;i<100;i++)
            {
                var a=Armory.Roll(new System.Random(i),1);var b=Armory.Roll(new System.Random(i),6);
                if(a.slot==b.slot&&a.rarity==b.rarity)
                {Check(b.level>a.level&&b.damageReduction>a.damageReduction,"same-seed armor level increases protection "+i);grownSlots.Add(a.slot);}
            }
            Check(grownSlots.Count==5,"armor level growth verified for all five slots");
            var legacy=JsonUtility.FromJson<Gun>("{\"name\":\"PRISM REAPER\",\"id\":\"legacy-preserved\",\"effect\":2,\"damage\":26}");
            Check(legacy.modelVariant==0&&VaultLanguage.DisplayModel(legacy)==9&&legacy.id=="legacy-preserved"&&legacy.name=="PRISM REAPER"&&legacy.damage==26,"old saved guns migrate presentation without rewriting identity or stats");
            string legacyBefore=JsonUtility.ToJson(legacy);Check(Arsenal.Accuracy(legacy)==Arsenal.DefaultAccuracy(9)&&JsonUtility.ToJson(legacy)==legacyBefore,"missing legacy accuracy reads model default without changing serialized identity or stats");
            Check(Arsenal.Accuracy(Arsenal.Model(12,Rarity.Common))>Arsenal.Accuracy(Arsenal.Model(13,Rarity.Common))&&Arsenal.SpreadHalfAngle(Arsenal.Model(12,Rarity.Common))<Arsenal.SpreadHalfAngle(Arsenal.Model(13,Rarity.Common)),"rifles have higher accuracy and tighter shot cones than shotguns");
            var jsonGun=Arsenal.Legendary(2,new System.Random(9001),12);
            string gunJson=JsonUtility.ToJson(jsonGun);var restoredGun=JsonUtility.FromJson<Gun>(gunJson);
            Check(JsonUtility.ToJson(restoredGun)==gunJson&&restoredGun.id==jsonGun.id&&restoredGun.effect==jsonGun.effect,"weapon JSON roundtrip preserves identity level effect and all stats");
            for(int modern=12;modern<Arsenal.ModelCount;modern++)
            {
                var item=Arsenal.Model(modern,Rarity.Rare);var restored=JsonUtility.FromJson<Gun>(JsonUtility.ToJson(item));
                Check(VaultLanguage.DisplayModel(restored)==modern&&restored.id==item.id&&restored.damage==item.damage,"modern gun model and identity survive save roundtrip / "+modern);
                Check(VaultLanguage.GunName(item,true)!=VaultLanguage.GunName(item,false)&&VaultLanguage.GunDescription(item,true)!=VaultLanguage.GunDescription(item,false),"modern gun has Japanese and English name and description / "+modern);
                Check(restored.accuracy==item.accuracy&&item.accuracy>0,"modern gun serialized accuracy preserved / "+modern);
                Check(item.effect==(modern==13?GunEffect.Scatter:GunEffect.Normal),"modern firearms preserve correct ballistic effects / "+modern);
            }
            var modernLoot=new bool[Arsenal.ModelCount-12];for(int roll=0;roll<250;roll++){int model=VaultLanguage.DisplayModel(Arsenal.CreateForDungeon(new System.Random(roll),0,Rarity.Common,1));if(model>=12)modernLoot[model-12]=true;}
            for(int model=12;model<Arsenal.ModelCount;model++)Check(modernLoot[model-12],"each modern firearm and handgun is obtainable through ordinary loot / "+model);
            foreach(ArmorSlot slot in Enum.GetValues(typeof(ArmorSlot)))
            {
                ArmorItem item=null;foreach(var sample in armorSamples.Values)if(sample.slot==slot){item=sample;break;}
                Check(item!=null,"armor serialization sample exists "+slot);string armorJson=JsonUtility.ToJson(item);
                var restoredItem=JsonUtility.FromJson<ArmorItem>(armorJson);
                Check(JsonUtility.ToJson(restoredItem)==armorJson&&restoredItem.id==item.id&&restoredItem.slot==slot,"armor JSON roundtrip preserves all fields "+slot);
            }
            var originalWeapons=game.Weapons.ToArray();int originalSelected=game.SelectedWeapon;
            var originalArmor=(ArmorItem[])game.EquippedArmor.Clone();
            var originalArmorInventory=game.ArmorInventory.ToArray();
            for(int modern=12;modern<Arsenal.ModelCount;modern++)
            {
                var item=Arsenal.Model(modern,Rarity.Rare);Check(game.StoreWeapon(item),"modern firearm fits weapon inventory / "+modern);
                game.SelectWeapon(game.Weapons.IndexOf(item));
                Check(game.PlayerAppearance.GetComponent<LayeredPlayer2D>().WeaponModel==modern,"equipping modern firearm selects its own painted model / "+modern);
                Check(game.VerifyPersistence()&&VaultLanguage.DisplayModel(game.gun)==modern,"actual loadout save retains new weapon, accuracy and active equipment / "+modern);
            }
            game.SelectWeapon(originalSelected);
            game.EquipWeapon(0,0);game.EquipWeapon(1,1);game.ActivateWeaponSlot(0);
            var equippedPrimary=game.WeaponInSlot(0);var equippedSecondary=game.WeaponInSlot(1);
            for(int cycle=0;cycle<6;cycle++){game.CycleWeapon(1);Check(game.gun==(cycle%2==0?equippedSecondary:equippedPrimary),"weapon cycling is limited to the two equipped slots / "+cycle);}
            Check(game.VerifyPersistence(),"both equipped weapon slots and active slot survive actual save reload");
            while(game.Weapons.Count<VaultGame.WeaponCapacity)Check(game.StoreWeapon(Arsenal.Roll(seedA,3)),"weapon stores in available slot "+game.Weapons.Count);
            var overflow=Arsenal.Roll(seedA,3);var selectedGun=game.gun;
            Check(!game.StoreWeapon(overflow)&&game.Weapons.Count==10&&game.gun==selectedGun,"eleventh weapon rejected without replacing equipped gun");
            game.SelectWeapon(0);game.ammo=3;game.CycleWeapon(1);game.ammo=2;game.CycleWeapon(-1);
            Check(game.SelectedWeapon==0&&game.ammo==3,"wheel cycling restores first magazine ammunition");
            game.CycleWeapon(-1);Check(game.SelectedWeapon==1,"weapon cycle wraps backward within two equipped slots");game.CycleWeapon(1);Check(game.SelectedWeapon==0&&game.ammo==3,"weapon cycle wraps forward and preserves ammunition");
            game.ToggleInventory();VaultLanguage.Set(true,false);yield return Capture("inventory-full-ja.png");VaultLanguage.Set(false,false);yield return Capture("inventory-full-en.png");game.ToggleInventory();
            game.StartRun(1729);game.invincible=999;VerifyAccuracyFire(game);
            foreach(var foe in game.Enemies)if(foe!=null){foe.speed=0;foe.timer=999;}
            for(int i=0;i<5;i++)
            {
                var item=new ArmorItem{id="smoke-armor-"+i,slot=(ArmorSlot)i,name="SMOKE ARMOR",rarity=Rarity.Legendary,level=5,color=Arsenal.Colors[4],visualVariant=i,
                    damageReduction=.02f,maxHealth=10,damageBonus=.04f,fireBonus=.03f,moveBonus=.02f,reloadBonus=.05f,dodgeBonus=.04f,regenBonus=.2f};
                Check(game.StoreArmor(item),"armor stores "+item.slot);game.EquipArmor(item);
                Check(game.EquippedArmor[i]==item,"armor equips into correct slot "+item.slot);
                Check(Armory.Name(item,true)!=Armory.Name(item,false)&&Armory.Stats(item,true)!=Armory.Stats(item,false),"armor name and numeric stats localized "+item.slot);
            }
            Check(game.PlayerAppearance.AppliedArmorCount==5,"all five armor slots affect 3D appearance");
            Check(Mathf.Abs(game.DamageReduction-.1f)<.001f,"armor damage reduction aggregates");
            Check(Mathf.Abs(game.maxHp-150)<.001f,"armor health adds to fixed base maximum after training removal");
            Check(Mathf.Abs(game.EffectiveDamage-game.gun.damage*(1+game.damageBonus+.2f))<.001f,"armor damage bonus reaches combat damage");
            Check(Mathf.Abs(game.EffectiveFireRate-(1+game.fireBonus+.15f)/game.gun.interval)<.001f,"armor fire bonus reaches fire interval");
            Check(Mathf.Abs(game.EffectiveMoveSpeed-7.7f)<.001f,"armor movement bonus reaches movement speed");
            Check(Mathf.Abs(game.EffectiveReload-Mathf.Max(.35f,game.gun.reloadSeconds/1.25f))<.001f,"armor reload bonus reaches reload timer");
            Check(game.EffectiveDodgeCooldown<1.1f,"armor dodge bonus reduces actual dodge cooldown");
            game.hp=game.maxHp;game.invincible=0;float hpBefore=game.hp;game.Hurt(10);
            Check(Mathf.Abs(hpBefore-game.hp-9)<.001f,"armor reduces actual incoming damage");
            game.invincible=999;game.StartRun(1730);game.invincible=999;game.hp-=10;
            hpBefore=game.hp;yield return new WaitForSeconds(.25f);
            Check(game.hp>hpBefore&&game.hp<=game.maxHp,"armor regenerates actual health");
            var reloadAmmoType=VaultGame.AmmoForGun(game.gun);game.AddAmmo(reloadAmmoType,game.gun.magazine);int reserveBeforeReload=game.AmmoReserve(reloadAmmoType);
            game.ammo=0;game.reload=0;yield return null;yield return null;
            Check(game.reload>0&&game.reload<=game.EffectiveReload,"empty magazine starts equipment-adjusted reload when matching reserve exists");
            yield return new WaitForSeconds(game.EffectiveReload+.15f);
            Check(game.ammo==game.gun.magazine&&game.AmmoReserve(reloadAmmoType)==reserveBeforeReload-game.gun.magazine,"reload draws matching ammunition from reserve into magazine");
            game.reload=.5f;game.ToggleInventory();game.dodgeCooldown=.8f;float frozenHp=game.hp,frozenTime=game.dodgeCooldown,frozenReload=game.reload;
            Check(game.InventoryOpen&&!game.Active&&Time.timeScale==0,"playing inventory suspends combat");
            yield return new WaitForSecondsRealtime(.15f);
            Check(game.hp==frozenHp&&game.dodgeCooldown==frozenTime&&game.reload==frozenReload,"inventory freezes regeneration dodge cooldown and reload");
            game.ToggleInventory();Check(game.Active&&Time.timeScale==1,"closing inventory resumes gameplay");
            game.screen=VaultGame.Screen.Pause;Time.timeScale=0;game.ToggleInventory();game.ToggleInventory();
            Check(game.screen==VaultGame.Screen.Pause&&!game.Active&&Time.timeScale==0,"inventory returns to paused state");
            game.screen=VaultGame.Screen.Playing;Time.timeScale=1;
            game.ToggleInventory();
            if(game.PlayerAppearance.GetComponent<LayeredPlayer2D>()!=null){yield return VerifyLayeredVisuals(game,false);game.ToggleInventory();}
            else {
            Check(VisibleMeshes(game.PlayerAppearance.transform)>=10,"hero has visible articulated 3D body geometry");
            var skull=HumanSkull(game.PlayerAppearance.transform);
            Check(skull!=null&&skull.GetComponent<MeshRenderer>().bounds.size.y>.3f,"three dimensional human head is present");
            Bounds bodyBounds=new Bounds();bool firstMesh=true;
            foreach(var mesh in game.PlayerAppearance.GetComponentsInChildren<MeshRenderer>())
            {if(!mesh.enabled||!mesh.gameObject.activeInHierarchy||mesh.GetComponentInParent<AimedWeapon>()!=null)continue;if(firstMesh){bodyBounds=mesh.bounds;firstMesh=false;}else bodyBounds.Encapsulate(mesh.bounds);}
            float headRatio=bodyBounds.size.y/skull.GetComponent<MeshRenderer>().bounds.size.y;
            Check(headRatio>=7.2f&&headRatio<=7.5f,"human body has adult human proportions / "+headRatio);
            var motion=game.PlayerAppearance.GetComponent<ActorMotion>();
            Check(motion!=null&&motion.leftLeg!=null&&motion.rightLeg!=null,"hero has separate articulated walking legs");
            for(int angle=0;angle<360;angle+=10)
            {
                Vector3 direction=Quaternion.Euler(0,angle,0)*Vector3.forward;game.AimAt(game.PlayerPosition+direction*10);
                yield return new WaitForEndOfFrame();
                Check(HandsMatchHandling(game.PlayerAppearance,.01f),"both hands remain attached to gun grips at "+angle+" degrees");
                Check(Vector3.Dot(game.PlayerAppearance.transform.forward,direction)>.999f,"3D body continuously follows aim at "+angle+" degrees");
                Vector3 mounted=game.PlayerAppearance.transform.InverseTransformPoint(game.PlayerWeapon.transform.position+game.PlayerWeapon.RecoilDisplacement);
                Check(Mathf.Abs(mounted.x-.25f)<.001f,"weapon stays on actor right shoulder through full aim circle / "+angle);
                Vector3 stock=game.PlayerAppearance.transform.InverseTransformPoint(game.PlayerWeapon.StockContactPosition+game.PlayerWeapon.RecoilDisplacement);
                Check(Mathf.Abs(stock.x-.25f)<.001f&&Mathf.Abs(stock.z+.10f)<.001f,"stock rear meets right shoulder plane / "+angle);
                Check(VisibleMeshes(game.PlayerWeapon.transform)>=4,"mechanical gun remains visible at "+angle+" degrees");
                Check(game.PlayerWeapon.MuzzlePosition.y-game.PlayerPosition.y>1.1f,"gun height aligns with adult human shoulders at "+angle+" degrees");
            }
            game.ToggleInventory();
            var legAngles=new System.Collections.Generic.HashSet<int>();
            var startPosition=game.PlayerPosition;bool savedEnabled=game.enabled;game.enabled=false;
            var right=game.GameCamera.transform.right;right.y=0;right.Normalize();
            for(int step=0;step<24;step++){game.PlayerAppearance.transform.position=startPosition+right*(step*.20f);yield return null;yield return new WaitForEndOfFrame();legAngles.Add(Mathf.RoundToInt(motion.leftLeg.localEulerAngles.x));}
            game.PlayerAppearance.transform.position=startPosition;game.enabled=savedEnabled;
            Check(legAngles.Count>=4,"walking changes articulated leg pose continuously");
            var modelNames=new System.Collections.Generic.HashSet<string>();
            for(int variant=0;variant<Arsenal.ModelCount;variant++)
            {
                var model=Arsenal.Model(variant,variant>=9?Rarity.Legendary:Rarity.Common);
                VaultArt.ConfigureWeapon(game.PlayerWeapon.transform,model.effect,model.color,variant);yield return new WaitForEndOfFrame();
                foreach(var part in game.PlayerWeapon.GetComponentsInChildren<Transform>())if(part.name.StartsWith("Salvaged weapon model ")&&part.gameObject.activeInHierarchy)modelNames.Add(part.name);
                Check(VisibleMeshes(game.PlayerWeapon.transform)>=4,"mechanical weapon variant has visible geometry "+variant);
                Check(model.modelVariant==variant&&model.damage>0&&model.speed>0&&model.magazine>0&&model.reloadSeconds>0,"weapon variant supplies valid distinct model and stats "+variant);
                Check(JsonUtility.FromJson<Gun>(JsonUtility.ToJson(model)).modelVariant==variant,"weapon model survives serialization "+variant);
                Check(HandsMatchHandling(game.PlayerAppearance,.01f),"both hands connect to each mechanical weapon model "+variant);
                Check(Vector3.Dot(Vector3.ProjectOnPlane(game.PlayerWeapon.MuzzlePosition-game.PlayerWeapon.GripPosition,Vector3.up).normalized,game.PlayerWeapon.Direction)>.98f,"each mechanical model muzzle follows its aim "+variant);
                Check(VaultLanguage.GunName(model,true)!=VaultLanguage.GunName(model,false)&&VaultLanguage.GunDescription(model,true)!=VaultLanguage.GunDescription(model,false),"each weapon model has Japanese and English presentation "+variant);
            }
            Check(modelNames.Count==Arsenal.ModelCount,"all distinct mechanical models selected");
            VaultArt.ConfigureWeapon(game.PlayerWeapon.transform,game.gun.effect,game.gun.color,VaultLanguage.DisplayModel(game.gun));
            }
            yield return CaptureActor(game,"actor-front.png",-game.GameCamera.transform.forward);
            yield return CaptureActor(game,"actor-side.png",game.GameCamera.transform.right);
            yield return CaptureActor(game,"actor-front-detail.png",-game.GameCamera.transform.forward,1.8f);
            yield return CaptureActor(game,"actor-side-detail.png",game.GameCamera.transform.right,1.8f);
            // Build a contrasting, reproducible legendary appearance before the close view.
            var visualRandom=new System.Random(81173);
            for(int slot=0;slot<5;slot++)
            {
                ArmorItem visualItem=null;
                for(int attempt=0;attempt<2000;attempt++)
                {var rolled=Armory.CreateForDungeon(visualRandom,0,Rarity.Legendary,4);if((int)rolled.slot==slot){visualItem=rolled;break;}}
                Check(visualItem!=null&&game.StoreArmor(visualItem),"legendary visual set collected "+slot);game.EquipArmor(visualItem);
            }
            yield return CaptureActor(game,"actor-armored.png",-game.GameCamera.transform.forward);
            yield return CaptureActor(game,"actor-armored-detail.png",-game.GameCamera.transform.forward,1.8f);
            while(game.ArmorInventory.Count<VaultGame.ArmorCapacity)Check(game.StoreArmor(Armory.Roll(visualRandom,20)),"armor stores in available slot "+game.ArmorInventory.Count);
            var armorOverflow=Armory.Roll(visualRandom,20);var fullArmor=game.ArmorInventory.ToArray();var fullEquipped=(ArmorItem[])game.EquippedArmor.Clone();
            Check(!game.StoreArmor(armorOverflow)&&game.ArmorInventory.Count==50,"fifty-first armor rejected at capacity");
            bool armorUnchanged=true;for(int i=0;i<fullArmor.Length;i++)armorUnchanged&=game.ArmorInventory[i]==fullArmor[i];
            for(int i=0;i<5;i++)armorUnchanged&=game.EquippedArmor[i]==fullEquipped[i];
            Check(armorUnchanged,"full armor storage rejection preserves inventory and equipped items");
            var retainedArmor=game.EquippedArmor[1];game.UnequipArmor(1);
            Check(game.EquippedArmor[1]==null&&game.ArmorInventory.Contains(retainedArmor),"removing armor leaves an empty slot and keeps the item owned");
            string retainedArmorId=retainedArmor.id;
            Check(game.VerifyPersistence()&&game.EquippedArmor[1]==null&&game.ArmorInventory.Exists(item=>item.id==retainedArmorId),"empty armor slot survives loadout serialization and remains owned");
            game.EquipArmor(game.ArmorInventory.Find(item=>item.id==retainedArmorId));
            Check(game.EquippedArmor[1]!=null&&game.EquippedArmor[1].id==retainedArmorId,"restored armor can be equipped again");
            for(int i=0;i<10;i++)game.Weapons[i]=Arsenal.Legendary(i,visualRandom,99);
            game.SelectWeapon(2);game.ToggleInventory();
            VaultLanguage.Set(true,false);yield return Capture("inventory-full-ja.png");
            VaultLanguage.Set(false,false);yield return Capture("inventory-full-en.png");
            game.ToggleInventory();
            game.Weapons.Clear();game.Weapons.AddRange(originalWeapons);game.SelectWeapon(originalSelected);
            game.ArmorInventory.Clear();game.ArmorInventory.AddRange(originalArmorInventory);
            for(int i=0;i<5;i++)game.EquipArmor(originalArmor[i]);
        }
        IEnumerator CaptureActor(VaultGame game,string filename,Vector3 direction,float framing=3.8f)
        {
            var camera=game.GameCamera;float size=camera.orthographicSize;Vector3 position=camera.transform.position;
            // Hold gameplay Update for the evidence frame so the mouse and camera follow cannot overwrite the controlled view.
            bool enabled=game.enabled;game.enabled=false;direction.y=0;direction.Normalize();
            game.AimAt(game.PlayerPosition+direction*10);camera.orthographicSize=framing;
            camera.transform.position=game.PlayerPosition+Vector3.up*.9f-camera.transform.forward*35;
            yield return Capture(filename);
            camera.orthographicSize=size;camera.transform.position=position;game.enabled=enabled;
        }
        IEnumerator CapturePlayerPortrait(VaultGame game,string filename,Vector3 direction,bool detail=false)
        {
            var camera=game.GameCamera;var rotation=camera.transform.rotation;var position=camera.transform.position;float size=camera.orthographicSize;
            camera.transform.rotation=Quaternion.Euler(detail?18:8,0,0);
            camera.orthographicSize=detail?.88f:1.48f;
            camera.transform.position=game.PlayerPosition+Vector3.up*(detail?1.04f:.53f)-camera.transform.forward*35;
            game.AimAt(game.PlayerPosition+direction*10);
            yield return Capture(filename);
            camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;
        }
        IEnumerator VerifyPlayerReview(VaultGame game)
        {
            game.StartRun(104729);game.invincible=999;game.enabled=false;
            foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
            // Review the player and its starter gun only, without changing saved inventory.
            game.gun=Arsenal.Starter();game.gun.accuracy=100;game.ammo=game.gun.magazine;game.reload=0;
            VaultArt.ConfigureWeapon(game.PlayerWeapon.transform,game.gun.effect,game.gun.color,0);
            Time.timeScale=0;game.AimAt(game.PlayerPosition+Vector3.forward*10);
            yield return new WaitForEndOfFrame();
            foreach(string textureName in new[]{"WastelandCloth","WastelandLeather"})
            {
                var texture=Resources.Load<Texture2D>("Art/"+textureName);
                Check(texture!=null&&texture.wrapMode==TextureWrapMode.Repeat&&texture.mipmapCount>1&&texture.filterMode==FilterMode.Trilinear,"player surface texture loads with repeat mipmaps and trilinear filtering / "+textureName);
            }
            var appearance=game.PlayerAppearance;var weapon=game.PlayerWeapon;
            Check(appearance!=null&&VisibleMeshes(appearance.transform)>=10,"player has visible continuous 3D anatomy and armor");
            var skull=HumanSkull(appearance.transform);
            Check(skull!=null&&skull.GetComponent<MeshRenderer>()!=null,"player adult skull mesh exists");
            float ratio=BodyBounds(appearance.transform).size.y/skull.GetComponent<MeshRenderer>().bounds.size.y;
            Check(ratio>=7.2f&&ratio<=7.5f,"player has 7.2 to 7.5 adult head proportions / "+ratio);
            Check(appearance.AppliedArmorCount==5,"player displays all five equipped armor slots");
            var evidence=new System.Text.StringBuilder();evidence.AppendLine("angle,right-hand-error,left-hand-error,arm-extension,stock-x,stock-z,muzzle-error");
            Transform bore=null;foreach(var part in weapon.GetComponentsInChildren<Transform>())if(part.name=="Machined muzzle brake"&&part.gameObject.activeInHierarchy){bore=part;break;}
            Check(bore!=null&&bore.GetComponent<MeshFilter>()!=null,"starter gun has a hollow physical muzzle brake");
            Vector3 boreFront=new Vector3(0,0,bore.GetComponent<MeshFilter>().sharedMesh.bounds.max.z);
            for(int angle=0;angle<360;angle+=10)
            {
                Vector3 direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
                game.AimAt(game.PlayerPosition+direction*10);
                Check(Vector3.Dot(weapon.Direction,direction)>.999f,"initial aim synchronizes gun immediately / "+angle);
                Check(HandsMatchHandling(appearance,.001f),"initial aim synchronizes both hands immediately / "+angle);
                yield return new WaitForEndOfFrame();
                Vector3 mount=appearance.transform.InverseTransformPoint(weapon.transform.position+weapon.RecoilDisplacement);
                Vector3 stock=appearance.transform.InverseTransformPoint(weapon.StockContactPosition+weapon.RecoilDisplacement);
                float muzzleError=Vector3.Distance(weapon.MuzzlePosition,bore.TransformPoint(boreFront));
                Check(Mathf.Abs(mount.x-.25f)<.001f&&Mathf.Abs(mount.y-1.25f)<.001f,"gun remains mounted on right shoulder / "+angle);
                Check(Mathf.Abs(stock.x-.25f)<.001f&&Mathf.Abs(stock.z+.10f)<.001f,"stock remains seated at right shoulder / "+angle);
                Check(HandsMatchHandling(appearance,.001f),"both hands remain attached after frame / "+angle);
                Check(appearance.ArmExtension<=1.025f,"adult arm lengths reach both grips without stretching / "+angle);
                Check(muzzleError<.03f,"projectile socket matches visible muzzle bore / "+angle);
                evidence.AppendLine(angle+","+appearance.GripError+","+appearance.SupportGripError+","+appearance.ArmExtension+","+stock.x+","+stock.z+","+muzzleError);
            }
            File.WriteAllText(Path.Combine(output,"player-rig-review.csv"),evidence.ToString());
            yield return CapturePlayerPortrait(game,"player-front.png",Vector3.back);
            yield return CapturePlayerPortrait(game,"player-side.png",Vector3.right);
            yield return CapturePlayerPortrait(game,"player-back.png",Vector3.forward);
            yield return CapturePlayerPortrait(game,"player-equipment-detail.png",Vector3.back,true);
            yield return CapturePlayerPortrait(game,"player-weapon-grip-detail.png",Vector3.right,true);
            Vector3 shotDirection=Vector3.zero;
            for(int angle=0;angle<360;angle+=45)
            {
                var candidate=Quaternion.Euler(0,angle,0)*Vector3.forward;
                if(game.Layout.ClearLine(game.PlayerPosition,game.PlayerPosition+candidate*5,.65f)){shotDirection=candidate;break;}
            }
            Check(shotDirection.sqrMagnitude>.5f,"player first-shot review has a clear target lane");
            var targetObject=new GameObject("Isolated player first-shot target");targetObject.transform.SetParent(appearance.transform.parent,false);
            var target=targetObject.AddComponent<Foe>();target.Init(game,1,0);target.enabled=false;target.hp=target.maxHp=1000;
            target.transform.position=game.PlayerPosition+shotDirection*4;game.Enemies.Add(target);
            // Turn and fire in one frame: the bullet must use the new pose, including immediate recoil.
            game.AimAt(game.PlayerPosition-shotDirection*10);game.AimAt(target.transform.position);
            Vector3 beforeRecoil=weapon.transform.position;int beforeAmmo=game.ammo,beforeNumbers=CombatVisuals.TotalDamageNumbers;
            game.Fire();
            Check(game.ammo==beforeAmmo-1,"first shot consumes exactly one cartridge");
            Check(Vector3.Dot(beforeRecoil-weapon.transform.position,shotDirection)>.12f,"first shot immediately moves gun back along aim for recoil");
            Check(HandsMatchHandling(appearance,.001f),"both hands immediately follow recoil displacement");
            Shot firstShot=null;foreach(var shot in FindObjectsByType<Shot>(FindObjectsSortMode.None))if(shot.friendly)firstShot=shot;
            Check(firstShot!=null&&Vector3.Distance(firstShot.transform.position,weapon.MuzzlePosition)<.001f,"first bullet starts at current recoiling muzzle");
            Check(Vector3.Angle(firstShot.velocity,weapon.Direction)<=Arsenal.SpreadHalfAngle(game.gun)+.04f,"first bullet follows newly selected aim within item accuracy cone");
            yield return CaptureActor(game,"player-first-shot.png",-game.GameCamera.transform.forward,1.8f);
            Time.timeScale=1;yield return new WaitForSeconds(.4f);
            Check(Mathf.Abs(target.hp-(1000-game.EffectiveDamage))<.01f,"first fired projectile causes actual target damage");
            Check(CombatVisuals.TotalDamageNumbers==beforeNumbers+1,"first projectile produces one actual hit damage number");
            Check(weapon.RecoilDisplacement.magnitude<.001f,"gun returns to shoulder after recoil");
            Check(HandsMatchHandling(appearance,.001f),"both hands remain connected after recoil recovery");
            game.Enemies.Remove(target);Destroy(targetObject);Time.timeScale=0;
            File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));
            Application.Quit(0);
        }
        IEnumerator CaptureModelShowcases(VaultGame game)
        {
            var camera=game.GameCamera;var world=game.PlayerAppearance.transform.parent.gameObject;
            bool gameEnabled=game.enabled,worldActive=world.activeSelf;
            float timeScale=Time.timeScale,size=camera.orthographicSize;
            Vector3 cameraPosition=camera.transform.position;Quaternion cameraRotation=camera.transform.rotation;
            Color background=camera.backgroundColor;var post=camera.GetComponent<VaultPost>();bool postEnabled=post!=null&&post.enabled;
            var ownedMaterials=new System.Collections.Generic.List<Material>();
            var showcase=new GameObject("Temporary QA model showcases");
            try
            {
                // Isolated render-only geometry never enters the inventory, progression or enemy collections.
                game.enabled=false;world.SetActive(false);Time.timeScale=1;
                showcase.transform.position=new Vector3(1000,0,1000);
                var art=new VaultArt(showcase.transform,ownedMaterials,new System.Random(12012));
                var actors=new GameObject("Ten mutant anatomy lineup");actors.transform.SetParent(showcase.transform,false);
                var actorArt=new VaultArt(actors.transform,ownedMaterials,new System.Random(1010));
                art.Part("Neutral evidence floor",PrimitiveType.Cube,new Vector3(0,-.12f,0),new Vector3(22,.2f,18),new Color(.19f,.20f,.17f));
                for(int kind=0;kind<10;kind++)
                {
                    Vector3 location=new Vector3((kind%5-2)*3.25f,.65f,kind<5?3.8f:-3.8f);
                    var actor=actorArt.Character(location,false,kind);actor.rotation=Quaternion.Euler(0,165,0);
                    if(kind==5)
                    {
                        var weapon=actorArt.Weapon(actor,new Color(.65f,.48f,.25f));
                        VaultArt.ConfigureWeapon(weapon,GunEffect.Normal,new Color(.65f,.48f,.25f),4);
                        weapon.GetComponent<AimedWeapon>().enabled=false;
                    }
                    var foe=actor.gameObject.AddComponent<Foe>();foe.Init(game,kind,0);foe.enabled=false;
                    if(kind==5)foe.weapon=actor.GetComponentInChildren<AimedWeapon>();
                    var painted=actor.gameObject.AddComponent<EnemyPainted2D>();painted.Init(foe);painted.SetReviewPose(1,EnemyPainted2D.Motion.Idle,.12f);
                    Check(painted.Ready&&VisibleMeshes(actor)==0,"finished mutant showcase uses complete painted 2D art / kind "+kind);
                }
                camera.transform.rotation=Quaternion.Euler(35,0,0);camera.orthographicSize=6.6f;
                camera.transform.position=showcase.transform.position+Vector3.up*1.5f-camera.transform.forward*35;
                camera.backgroundColor=new Color(.12f,.13f,.11f);if(post!=null)post.enabled=false;
                yield return null;Time.timeScale=0;
                yield return Capture("mutant-lineup-2d.png");
                actors.SetActive(false);Time.timeScale=1;
                var guns=new GameObject("Mechanical and modern weapon lineup");guns.transform.SetParent(showcase.transform,false);
                var gunArt=new VaultArt(guns.transform,ownedMaterials,new System.Random(1212));
                for(int variant=0;variant<Arsenal.ModelCount;variant++)
                {
                    Vector3 location=new Vector3((variant%4-1.5f)*3.3f,.30f,(variant/4-1)*3.2f);
                    var mount=new GameObject("Weapon display mount "+variant);mount.transform.SetParent(guns.transform,false);mount.transform.localPosition=location;
                    gunArt.Part("Salvage display table",PrimitiveType.Cube,location+Vector3.down*.2f,new Vector3(2.7f,.20f,1.65f),new Color(.24f,.24f,.20f));
                    var model=Arsenal.Model(variant,variant>=9?Rarity.Legendary:Rarity.Common);
                    var image=mount.AddComponent<SpriteRenderer>();image.sprite=LayeredPlayer2D.LoadCells("Art/LayeredPlayer/Weapons_"+(variant/2),false)[(variant%2)*8+1];
                    var material=new Material(Resources.Load<Shader>("LayeredPlayerSprite"));ownedMaterials.Add(material);image.sharedMaterial=material;
                    image.transform.rotation=Quaternion.Euler(55,0,0);image.transform.localScale=Vector3.one*(2.1f/image.sprite.bounds.size.x);image.color=LayeredPlayer2D.WeaponDisplayTint(variant,model.color);
                    var regions=JsonUtility.FromJson<LayeredPlayer2D.WeaponRegionTable>(Resources.Load<TextAsset>(variant>=14?"Art/LayeredPlayer/HandgunArt/HandgunWeaponRegions":variant>=12?"Art/LayeredPlayer/TacticalArt/ModernWeaponRegions":LayeredPlayer2D.ArtMetadataPath("WeaponRegions")).text);
                    foreach(var region in regions.regions)if(region.model==variant&&region.direction==1){var points=new Vector4[64];for(int p=0;p<region.hull.Length;p++)points[p]=new Vector4(region.hull[p].x,1-region.hull[p].y,0,0);var block=new MaterialPropertyBlock();block.SetInt("_ClipCount",region.hull.Length);block.SetVectorArray("_ClipPoints",points);image.SetPropertyBlock(block);break;}
                    Check(image.sprite!=null&&VisibleMeshes(mount.transform)==0,"finished weapon showcase uses painted item art / variant "+variant);
                }
                camera.transform.rotation=Quaternion.Euler(55,0,0);camera.orthographicSize=6.1f;
                camera.transform.position=showcase.transform.position+Vector3.up*.65f-camera.transform.forward*35;
                yield return null;Time.timeScale=0;
                yield return Capture("weapon-lineup-"+Arsenal.ModelCount+"-2d.png");
            }
            finally
            {
                showcase.SetActive(false);Destroy(showcase);
                foreach(var material in ownedMaterials)if(material!=null)Destroy(material);
                camera.orthographicSize=size;camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.backgroundColor=background;
                if(post!=null)post.enabled=postEnabled;
                world.SetActive(worldActive);Time.timeScale=timeScale;game.enabled=gameEnabled;
            }
        }
        IEnumerator VerifyHaven(VaultGame game)
        {
            var weapons=game.Weapons.ToArray();int selected=game.SelectedWeapon;var artifacts=game.ArtifactInventory.ToArray();var equipped=(ArtifactItem[])game.EquippedArtifacts.Clone();
            game.runDamage=.3f;game.runFire=.2f;game.runVitality=20;game.runDash=.5f;game.runChain=.3f;game.runHarvest=3;
            game.EnterHub();yield return null;
            Check(game.screen==VaultGame.Screen.Hub&&!game.Active&&game.Enemies.Count==0,"haven is a safe playable home base without hostiles");
            Check(game.runDamage+game.runFire+game.runVitality+game.runDash+game.runChain+game.runHarvest==0,"all run boons reset on returning to haven");
            Check(!game.EnterDungeon(1),"next dungeon remains locked until previous clear");
            VaultLanguage.Set(true,false);yield return Capture("hub-ja.png");VaultLanguage.Set(false,false);yield return Capture("hub-en.png");
            game.screen=VaultGame.Screen.Shop;int cost=game.ShopOfferPrice(game.ShopStock[0]);game.AwardCredits(cost);int before=game.Credits;
            Check(game.PurchaseShop(0)&&game.Credits==before-cost&&game.Weapons.Count==weapons.Length+1,"shop purchase consumes exact currency and stores equipment");
            before=game.Credits;Check(!game.PurchaseShop(0)&&game.Credits==before,"purchased stock cannot be bought or rerolled by reopening");
            game.screen=VaultGame.Screen.Hub;game.screen=VaultGame.Screen.Shop;Check(game.ShopStock[0].purchased,"shop stock retains sold state when reopened");
            VaultLanguage.Set(true,false);yield return Capture("shop-ja.png");VaultLanguage.Set(false,false);yield return Capture("shop-en.png");
            game.screen=VaultGame.Screen.DungeonSelect;VaultLanguage.Set(true,false);yield return Capture("dungeons-ja.png");VaultLanguage.Set(false,false);
            var roll=new System.Random(6173);int[] ordinary=new int[5];for(int i=0;i<40000;i++)ordinary[(int)DungeonCatalog.RollRarity(roll)]++;
            Check(ordinary[4]>120&&ordinary[4]<280,"ordinary legendary rate is approximately 0.5 percent, not guaranteed");
            Check(ordinary[0]>21000&&ordinary[0]<23000&&ordinary[3]>1100&&ordinary[3]<1700,"common and epic rarity proportions match their documented rates");
            int bossLegends=0;for(int i=0;i<20000;i++)if(DungeonCatalog.RollRarity(roll,true)==Rarity.Legendary)bossLegends++;
            Check(bossLegends>800&&bossLegends<1200,"boss legendary rate is approximately five percent");
            for(int dungeon=0;dungeon<3;dungeon++){
                var profile=DungeonCatalog.Get(dungeon);bool valid=true;for(int i=0;i<400;i++){var g=Arsenal.RollForDungeon(roll,dungeon);var a=Armory.RollForDungeon(roll,dungeon);var relic=ArtifactCatalog.RollForDungeon(roll,dungeon);valid&=g.level>=profile.minLevel&&g.level<=profile.maxLevel&&a.level>=profile.minLevel&&a.level<=profile.maxLevel&&relic.level>=profile.minLevel&&relic.level<=profile.maxLevel;}
                Check(valid,"all weapon armor and artifact rolls stay in dungeon level band "+dungeon);
                var legend=Arsenal.CreateForDungeon(roll,dungeon,Rarity.Legendary,profile.minLevel);Check(legend.effect==(dungeon==0?GunEffect.Ricochet:dungeon==1?GunEffect.Orbit:GunEffect.Singularity),"each dungeon uses its exclusive legendary weapon family "+dungeon);
                for(int map=0;map<40;map++){var layout=new SectorLayout(new System.Random(map),dungeon);Check(layout.Connected()&&layout.WalkableCells>650&&!layout.SpawnSafe(layout.Entry),"themed map remains connected large and entrance safe / "+dungeon+" / "+map);}
            }
            Check(game.EnterDungeon(0),"unlocked first dungeon can be entered from haven");game.invincible=999;
            var ward=equipped[0];game.EquipArtifact(ward,0);game.ResetArtifactCombat();game.ArtifactOnKill(game.PlayerPosition);float shield=game.ArtifactShield;Check(shield>0,"kill shield artifact triggers its actual barrier");
            game.EquipArtifact(ward,0);game.ArtifactOnKill(game.PlayerPosition);Check(game.ArtifactShield==shield,"same-slot re-equip cannot bypass artifact cooldown");
            game.EquipArtifact(ward,1);game.ArtifactOnKill(game.PlayerPosition);Check(game.ArtifactShield==shield,"moving artifact between slots preserves its cooldown");
            float residue=game.AbsorbArtifactDamage(3);
            Check(residue>0&&residue<3&&game.ArtifactShield<shield,
                "artifact barrier reduces incoming damage without granting immunity and expends charge");
            var surge=ArtifactCatalog.Create(new System.Random(8),0,Rarity.Common,1);surge.effect=ArtifactEffect.ReloadSurge;game.StoreArtifact(surge);game.EquipArtifact(surge,0);float baseline=game.EffectiveDamage,raw=game.gun.damage;game.ArtifactOnReload();Check(game.EffectiveDamage>baseline&&game.gun.damage==raw,"reload artifact modifies effective damage without rewriting weapon base stats");
            game.OpenArtifactInventory();VaultLanguage.Set(true,false);yield return Capture("artifacts-ja.png");VaultLanguage.Set(false,false);yield return Capture("artifacts-en.png");game.ToggleInventory();
            game.TickArtifacts(8);Check(Mathf.Abs(game.ArtifactDamageMultiplier-1)<.001f,"reload damage condition expires");
            game.EnterHub();game.Weapons.Clear();game.Weapons.AddRange(weapons);game.SelectWeapon(selected);game.ArtifactInventory.Clear();game.ArtifactInventory.AddRange(artifacts);game.EquippedArtifacts=equipped;game.ResetArtifactCombat();game.screen=VaultGame.Screen.Menu;
        }
        IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();int qa=Array.IndexOf(args,"--qa-output");
            artReview=Array.IndexOf(args,"--art-review")>=0;
            playerReview=Array.IndexOf(args,"--player-review")>=0;
            layeredReview=Array.IndexOf(args,"--layered-player-review")>=0||Array.IndexOf(args,"--pose-focus-review")>=0||Array.IndexOf(args,"--pose-dynamic-review")>=0||Array.IndexOf(args,"--approved-fieldkit-review")>=0||Array.IndexOf(args,"--approved-fieldkit-motion-review")>=0||Array.IndexOf(args,"--approved-fieldkit-equipment-review")>=0;
            reticleReview=Array.IndexOf(args,"--reticle-review")>=0;
            output=qa>=0&&qa+1<args.Length?args[qa+1]:Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA"));Directory.CreateDirectory(output);
            var game=GetComponent<VaultGame>();
            Check(Camera.main==game.GameCamera,"3D animation and weapons use the active quarter-view camera");
            yield return new WaitForSeconds(.5f);
            if(Array.IndexOf(args,"--approved-fieldkit-equipment-review")>=0)
            {
                game.StartRun(104729);game.invincible=999;game.enabled=false;foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
                yield return CaptureApprovedEquipment(game);
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" equipment subset and legacy socket checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            if(Array.IndexOf(args,"--approved-fieldkit-motion-review")>=0)
            {
                game.StartRun(104729);game.invincible=999;game.enabled=false;foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
                yield return CaptureApprovedMotion(game);
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" painted gait checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            if(Array.IndexOf(args,"--approved-fieldkit-review")>=0)
            {
                game.StartRun(104729);game.invincible=999;game.enabled=false;foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
                yield return CaptureApprovedFieldkit(game);
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" source pose checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            if(Array.IndexOf(args,"--debt-intro-review")>=0)
            {
                game.screen=VaultGame.Screen.Menu;VaultLanguage.Set(true,false);yield return Capture("debt-intro-ja.png");VaultLanguage.Set(false,false);yield return Capture("debt-intro-en.png");
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            if(Array.IndexOf(args,"--debt-review")>=0)
            {
                game.screen=VaultGame.Screen.Shop;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(VaultGame).GetField("debtPage",flags).SetValue(game,true);typeof(VaultGame).GetField("debtSkillTreePage",flags).SetValue(game,false);
                VaultLanguage.Set(true,false);yield return Capture("debt-office-ja.png");VaultLanguage.Set(false,false);yield return Capture("debt-office-en.png");
                game.AwardCredits(25000000);Check(game.RepayDebt(25000000)&&game.DebtTotalPaid==25000000&&game.DebtLevel==2&&game.DebtSkillPoints==1,"25 million scrap repayment advances level and awards one skill point");
                typeof(VaultGame).GetField("debtSkillTreePage",flags).SetValue(game,true);typeof(VaultGame).GetField("debtTrack",flags).SetValue(game,0);
                VaultLanguage.Set(true,false);yield return Capture("debt-tree-ja.png");VaultLanguage.Set(false,false);yield return Capture("debt-tree-en.png");
                Check(game.TryUnlockDebtSkill("weapon0_damage1")&&game.DebtSkillPoints==0&&Mathf.Abs(game.DebtDamageBonus(14)-.12f)<.001f,"handgun branch consumes its point and changes live weapon damage");
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            if(Array.IndexOf(args,"--pose-dynamic-review")>=0)
            {
                game.StartRun(104729);game.invincible=999;game.enabled=false;foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
                yield return CaptureDynamicPoses(game);
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" dynamic checks / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            if(Array.IndexOf(args,"--pose-focus-review")>=0)
            {
                game.StartRun(104729);game.invincible=999;game.enabled=false;
                foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
                yield return CaptureFocusedPoses(game);
                File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" contact/render checks / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;
            }
            VerifyGearQuality();
            for(int ammoType=0;ammoType<4;ammoType++)
                Check(game.AmmoReserve((AmmoType)ammoType)==game.AmmoCapacity((AmmoType)ammoType),
                    "fresh game starts with a full reserve for ammunition family "+ammoType);
            if(reticleReview){game.StartRun(130363);game.invincible=999;foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;VaultLanguage.Set(false,false);yield return CaptureReticleAccuracy(game);File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;}
            if(layeredReview){game.StartRun(104729);game.invincible=999;game.enabled=false;foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;yield return VerifyLayeredVisuals(game,true);File.WriteAllText(Path.Combine(output,ResultFile),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(0);yield break;}
            if(playerReview){yield return VerifyPlayerReview(game);yield break;}
            if(artReview)
            {
                game.StartRun(104729);game.invincible=999;game.enabled=false;
                foreach(var enemy in game.Enemies)if(enemy!=null)enemy.enabled=false;
                Time.timeScale=0;
                Check(game.PlayerAppearance!=null&&VisibleMeshes(game.PlayerAppearance.transform)>=10,"finished survivor has visible articulated 3D anatomy");
                var skull=HumanSkull(game.PlayerAppearance.transform);
                Check(skull!=null&&skull.GetComponent<MeshRenderer>()!=null,"finished survivor has a three dimensional human head");
                Bounds bodyBounds=new Bounds();bool firstMesh=true;
                foreach(var mesh in game.PlayerAppearance.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!mesh.enabled||!mesh.gameObject.activeInHierarchy||mesh.GetComponentInParent<AimedWeapon>()!=null)continue;
                    if(firstMesh){bodyBounds=mesh.bounds;firstMesh=false;}else bodyBounds.Encapsulate(mesh.bounds);
                }
                float headRatio=bodyBounds.size.y/skull.GetComponent<MeshRenderer>().bounds.size.y;
                Check(headRatio>=7.2f&&headRatio<=7.5f,"finished survivor retains adult human proportions / "+headRatio);
                yield return CaptureActor(game,"actor-front-detail.png",-game.GameCamera.transform.forward,1.8f);
                yield return CaptureActor(game,"actor-side-detail.png",game.GameCamera.transform.right,1.8f);
                yield return CaptureModelShowcases(game);
                game.GameCamera.transform.position=game.PlayerPosition+Vector3.up*.9f-game.GameCamera.transform.forward*35;
                yield return Capture("art-review-gameplay.png");
                File.WriteAllText(Path.Combine(output,"art-review-result.txt"),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));
                Application.Quit(0);yield break;
            }
            bool savedLanguage=VaultLanguage.Japanese;
            Check(Resources.Load<Shader>("CombatGlow")!=null&&Resources.Load<Shader>("VaultArmor")!=null,"combat glow and sculpted armor shaders bundled");
            VaultLanguage.Set(true,false);Check(VaultLanguage.T("English","日本語")=="日本語","Japanese translation selection");
            Check(VaultLanguage.GunName(Arsenal.Legendary(0))=="レッドスプリッター","Japanese legendary weapon name");
            var japaneseFont=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic UI","Meiryo"},20);japaneseFont.RequestCharactersInTexture("日本語聖域強化",20);Check(japaneseFont.HasCharacter('聖'),"Japanese OS font glyph available");
            yield return Capture("menu-ja.png");VaultLanguage.Set(false,false);
            Check(VaultLanguage.GunDescription(Arsenal.Legendary(0)).Contains("Ricochets"),"English legendary weapon description");
            yield return Capture("menu.png");
            yield return new WaitForSeconds(.3f);
            yield return VerifyEquipment(game);
            yield return VerifyHaven(game);
            game.StartRun(104729);game.invincible=999;
            yield return VerifyMutantBehavior(game);
            yield return VerifyMeleeCombat(game);
            var armorTexture=Resources.Load<Texture2D>("Art/WastelandSurface");
            var clothTexture=Resources.Load<Texture2D>("Art/WastelandCloth");
            var leatherTexture=Resources.Load<Texture2D>("Art/WastelandLeather");
            Check(armorTexture!=null&&clothTexture!=null&&leatherTexture!=null,"distinct metal cloth and leather textures bundled and loadable");
            if(game.PlayerAppearance.GetComponent<LayeredPlayer2D>()==null){
            var materialReport=new System.Text.StringBuilder();
            int clothParts=0,leatherParts=0,metalParts=0,skinParts=0;
            foreach(var renderer in game.PlayerAppearance.GetComponentsInChildren<MeshRenderer>())
            {
                string part=renderer.name;Texture expectedTexture;string expectedShader,role;
                if(part=="Field jacket"||part=="Trouser thigh"||part=="Trouser shin"||part=="Articulated upper arm")
                {expectedTexture=clothTexture;expectedShader="NeonVault/Surface";role="cloth";clothParts++;}
                else if(part=="Boot"||part=="Leather field backpack")
                {expectedTexture=leatherTexture;expectedShader="NeonVault/Surface";role="leather";leatherParts++;}
                else if(part=="Salvaged receiver")
                {expectedTexture=armorTexture;expectedShader="NeonVault/SculptedArmor";role="metal";metalParts++;}
                else if(part=="Helmet skull")
                {expectedTexture=null;expectedShader="NeonVault/Surface";role="skin";skinParts++;}
                else continue;
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var mat=renderer.sharedMaterial;
                Check(mat!=null&&mat.shader!=null,"assigned material and shader exist / "+part);
                materialReport.AppendLine(part+" role="+role+" shader="+mat.shader.name+" texture="+(mat.mainTexture==null?"NULL":mat.mainTexture.name)+" color="+mat.color+" block="+block.GetColor("_Color"));
                Check(mat.shader.name==expectedShader&&mat.mainTexture==expectedTexture,"body and weapon use role-specific shader and texture / "+role+" / "+part);
            }
            Check(clothParts>=7&&leatherParts>=3&&metalParts>=1&&skinParts==1,"material verification covers jacket both trousers both upper arms boots backpack receiver and skin");
            File.WriteAllText(Path.Combine(output,"material-report.txt"),materialReport.ToString());
            }
            string layout=game.Layout.Signature();
            var mapShapes=new System.Collections.Generic.HashSet<string>();var goalLocations=new System.Collections.Generic.HashSet<string>();var entryRooms=new System.Collections.Generic.HashSet<int>();
            for(int mapSeed=0;mapSeed<120;mapSeed++)
            {var map=new SectorLayout(new System.Random(mapSeed));Check(map.Connected(),"all chambers and passages connected seed "+mapSeed);Check(!map.Blocked(map.Entry,.6f)&&!map.Blocked(map.Exit,.6f),"entry and exit clear seed "+mapSeed);mapShapes.Add(map.Columns+"x"+map.Rows);goalLocations.Add(map.Exit.ToString("F1"));entryRooms.Add(map.EntryChamberIndex);Check(map.ExitPathCells>=18&&Vector3.Distance(map.Entry,map.Exit)>=25,"random exit stays far away and reachable / "+mapSeed);}
            Check(mapShapes.Count==4&&goalLocations.Count>=30&&entryRooms.Count>=4,"map proportions entrances and exit positions vary across seeds");
            var fogMap=new SectorLayout(new System.Random(79031));var fogEntry=fogMap.ToCell(fogMap.Entry);var fogExit=fogMap.ToCell(fogMap.Exit);
            Check(!fogMap.IsExplored(fogEntry.x,fogEntry.y)&&!fogMap.IsExplored(fogExit.x,fogExit.y),"new minimap begins completely unexplored");
            fogMap.Discover(fogMap.Entry);Check(fogMap.IsExplored(fogEntry.x,fogEntry.y)&&!fogMap.IsExplored(fogExit.x,fogExit.y),"entry discovery reveals only nearby passages without exposing the exit");
            fogMap.Discover(fogMap.Exit);Check(fogMap.IsExplored(fogEntry.x,fogEntry.y)&&fogMap.IsExplored(fogExit.x,fogExit.y)&&!fogMap.IsVisible(fogMap.Entry),"exploration memory remains while distant enemies stay outside current visibility");
            Check(game.DungeonIndex!=0||game.room!=0||game.Enemies.Count>=30,"first stage contains substantially more hostiles");
            Check(game.Layout.chambers.Count>=12&&game.Layout.WalkableCells>650,"irregular expanded area has twelve or more chambers and wide passages");
            foreach(var enemy in game.Enemies)Check(game.Layout.SpawnSafe(enemy.transform.position)&&Vector3.Distance(enemy.transform.position,game.PlayerPosition)>=12,"hostile spawns outside safe entrance zone");
            for(int chamber=0;chamber<game.Layout.chambers.Count;chamber++)
            {
                Vector3 walker=game.Layout.Center(game.Layout.chambers[chamber]);
                for(int step=0;step<2000&&Vector3.Distance(walker,game.PlayerPosition)>.3f;step++)walker=game.MoveWithinRoom(walker,game.ChaseDirection(walker)*.17f,.6f);
                Check(Vector3.Distance(walker,game.PlayerPosition)<.4f,"AI path reaches entry from chamber "+chamber);
            }
            Rect wall=game.Layout.walls[game.Layout.walls.Count/2];Vector3 wallStart=new Vector3(wall.xMin-1,.65f,wall.center.y),wallEnd=new Vector3(wall.xMax+1,.65f,wall.center.y);
            Check(!game.LineOfSight(wallStart,wallEnd),"solid partitions block line of sight");
            var aimTarget=game.PlayerPosition+Vector3.right*10;game.AimAt(aimTarget);var muzzleAim=aimTarget-game.PlayerWeapon.MuzzlePosition;muzzleAim.y=0;Check(Vector3.Dot(game.PlayerWeapon.Direction,muzzleAim.normalized)>.999f,"player gun continuously aims from visible muzzle toward target");
            if(game.PlayerAppearance.GetComponent<LayeredPlayer2D>()==null)Check(Vector3.Dot(Vector3.ProjectOnPlane(game.PlayerWeapon.MuzzlePosition-game.PlayerWeapon.GripPosition,Vector3.up).normalized,Vector3.right)>.98f,"barrel extends from hand grip along the aim direction");
            Check(game.PlayerAppearance.GetComponent<LayeredPlayer2D>()!=null?game.PlayerAppearance.GetComponent<LayeredPlayer2D>().WeaponReady:VisibleMeshes(game.PlayerWeapon.transform)>=4,"equipped weapon has visible art");
            foreach(var foe in game.Enemies)Check(foe.kind==5?foe.weapon!=null:foe.weapon==null,"zombies fight with mutation anatomy and needle stalkers carry guns");
            foreach(var foe in game.Enemies)
            {
                var painted=foe.GetComponent<EnemyPainted2D>();
                Check(painted!=null&&painted.Ready&&painted.LoadedDirections==5,"enemy has five painted views mapped into eight facings / "+foe.kind);
                Check(VisibleMeshes(foe.transform)==0,"enemy body and equipment use painted 2D rendering / "+foe.kind);
            }
            Check(Mathf.Abs(Mathf.DeltaAngle(game.GameCamera.transform.eulerAngles.y,45))<.1f&&Mathf.Abs(game.GameCamera.transform.eulerAngles.x-35.264f)<.1f,"quarter view camera angles");
            var diagonalForward=game.ScreenMovement(0,1);var diagonalBackward=game.ScreenMovement(0,-1);
            Check(Vector3.Dot(diagonalForward,game.GameCamera.transform.right)>.5f&&Vector3.Dot(diagonalForward,game.GameCamera.transform.up)>.3f,"forward input follows the upper-right quarter-view ground axis");
            Check(Vector3.Dot(diagonalBackward,game.GameCamera.transform.right)<-.5f&&Vector3.Dot(diagonalBackward,game.GameCamera.transform.up)<-.3f,"backward input follows the lower-left quarter-view ground axis");
            Check(Vector3.Distance(game.ScreenMovement(1,0),Vector3.back)<.001f&&Mathf.Abs(game.ScreenMovement(1,1).magnitude-1)<.001f,"lateral input follows the other ground axis and diagonal speed stays normalized");
            Check(game.PlayerAppearance!=null&&(game.PlayerAppearance.GetComponent<LayeredPlayer2D>()!=null?game.PlayerAppearance.GetComponent<LayeredPlayer2D>().Ready:VisibleMeshes(game.PlayerAppearance.transform)>=10),"real equipped survivor art active");
            foreach(var enemy in game.Enemies)
            {
                var visual=enemy.GetComponent<EnemyPainted2D>();int visible=0;
                if(visual!=null)foreach(var sprite in visual.BodyRenderers)if(sprite!=null&&sprite.enabled&&sprite.sprite!=null)visible++;
                Check(visual!=null&&visual.Ready&&visible==3,"mutant has visible upper body and two independently animated painted legs");
            }
            if(game.PlayerAppearance.GetComponent<LayeredPlayer2D>()==null)foreach(var sprite in game.PlayerAppearance.GetComponentsInChildren<SpriteRenderer>())Check(!sprite.enabled||!sprite.gameObject.activeInHierarchy,"hero silhouette comes from 3D geometry");
            Check(game.route.Count==game.CurrentDungeon.AreaCount && new System.Collections.Generic.HashSet<Vector2Int>(game.route).Count==game.CurrentDungeon.AreaCount,"dungeon has a connected distinct four-area route");
            for(int i=1;i<game.route.Count;i++)Check(Mathf.Abs(game.route[i].x-game.route[i-1].x)+Mathf.Abs(game.route[i].y-game.route[i-1].y)==1,"connected route "+i);
            string first=string.Join(",",game.route);
            game.StartRun(104729);game.invincible=999;
            Check(first==string.Join(",",game.route),"seed reproduces route");
            Check(layout==game.Layout.Signature(),"seed reproduces chamber and corridor layout");
            game.StartRun(130363);game.invincible=999;
            Check(layout!=game.Layout.Signature(),"new seed changes internal area layout");
            Check(first!=string.Join(",",game.route),"new seed changes route");
            Check(game.Blocked(new Vector3(100,0,0),.5f),"arena boundary blocks movement");
            game.GameCamera.GetComponent<VaultPost>().enabled=false;
            yield return Capture("gameplay-base.png");
            game.GameCamera.GetComponent<VaultPost>().enabled=true;
            yield return Capture("gameplay.png");
            yield return CaptureInkedEquipment(game);
            game.AimUiReviewPoint=new Vector2(840,420);yield return Capture("reticle-preview.png");game.AimUiReviewPoint=null;yield return CaptureReticleAccuracy(game);
            VaultLanguage.Set(true,false);yield return Capture("gameplay-ja.png");VaultLanguage.Set(false,false);
            var addMeleeDrop=typeof(VaultGame).GetMethod("AddMeleeDrop",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Check(addMeleeDrop!=null,"melee loot presentation factory is available");
            if(addMeleeDrop!=null){addMeleeDrop.Invoke(game,new object[]{MeleeWeaponCatalog.Create(new System.Random(1982),1,Rarity.Legendary),game.PlayerPosition+Vector3.forward*.5f});VaultLanguage.Set(true,false);yield return Capture("melee-loot-legendary-ja.png");VaultLanguage.Set(false,false);}
            yield return new WaitForSeconds(.2f);
            var f=game.NearestEnemy(game.PlayerPosition);f.speed=0;f.timer=999;f.kind=1;
            f.transform.position=game.PlayerPosition+Vector3.forward*3;
            yield return null;Check(Vector3.Dot(f.transform.forward,(game.PlayerPosition-f.transform.position).normalized)>.999f,"enemy body aims toward player");
            float health=f.hp;
            Check(ItemPortraits.AtlasAvailable,"item portraits bundled and loadable");
            Check(ItemPortraits.GunCell(Arsenal.Legendary(0))!=ItemPortraits.GunCell(Arsenal.Legendary(1))&&ItemPortraits.GunCell(Arsenal.Legendary(1))!=ItemPortraits.GunCell(Arsenal.Legendary(2)),"legendary item portraits distinguish all special weapon families");
            int beforeHitNumbers=CombatVisuals.TotalDamageNumbers;
            game.AimAt(f.transform.position);game.Fire();
            yield return new WaitForSeconds(.3f);
            Check(f==null||f.hp<health,"starter bullet damages enemy");
            Check(CombatVisuals.TotalDamageNumbers==beforeHitNumbers+1&&CombatVisuals.ActiveDamageNumbers>0,"one damage number per starter bullet hit");
            Check(Mathf.Abs(CombatVisuals.LastDamage-game.EffectiveDamage)<.01f,"damage number uses actual armor-adjusted damage");
            if(f!=null)Check(Vector3.ProjectOnPlane(CombatVisuals.LastHitPoint-f.transform.position,Vector3.up).magnitude<.7f,"damage number originates on target contact surface");
            yield return CaptureActor(game,"damage-hit.png",-game.GameCamera.transform.forward);
            f=game.NearestEnemy(game.PlayerPosition);f.speed=0;f.timer=999;f.hp=1000;f.transform.position=game.PlayerPosition+Vector3.forward*3;
            beforeHitNumbers=CombatVisuals.TotalDamageNumbers;health=f.hp;
            for(int pellet=0;pellet<6;pellet++)game.SpawnBullet(f.transform.position-Vector3.forward*1.2f+Vector3.up*.9f,Vector3.forward,true,7,12,Color.cyan,GunEffect.Scatter);
            yield return new WaitForSeconds(.1f);
            Check(CombatVisuals.TotalDamageNumbers==beforeHitNumbers+6&&Mathf.Abs(f.hp-(health-42))<.01f,"each of six pellets creates its own damage number and applies damage");
            yield return CaptureActor(game,"damage-pellets.png",-game.GameCamera.transform.forward);
            yield return new WaitForSeconds(.9f);
            Check(CombatVisuals.ActiveDamageNumbers==0,"damage popups expire after their arc and fade");
            var farther=game.Enemies.Find(enemy=>enemy!=null&&enemy!=f&&!enemy.dead);
            if(farther!=null)
            {
                Vector3 savedPosition=farther.transform.position;float savedHealth=farther.hp;
                farther.speed=0;farther.timer=999;farther.hp=1000;farther.transform.position=f.transform.position+Vector3.forward*.8f;
                game.Enemies.Remove(farther);game.Enemies.Insert(0,farther);health=f.hp;
                game.SpawnBullet(f.transform.position-Vector3.forward*2+Vector3.up*1.53f,Vector3.forward,true,11,600,Color.cyan,GunEffect.Normal);
                yield return new WaitForSeconds(.05f);
                Check(Mathf.Abs(f.hp-(health-11))<.01f&&farther.hp==1000,"fast swept bullet hits nearest enemy before farther list entry and wall");
                farther.transform.position=savedPosition;farther.hp=savedHealth;
            }
            beforeHitNumbers=CombatVisuals.TotalDamageNumbers;
            for(int hit=0;hit<120;hit++)CombatVisuals.DamageNumber(f.transform.position+Vector3.up,hit+1,Color.white,game.GameCamera);
            Check(CombatVisuals.TotalDamageNumbers==beforeHitNumbers+120&&CombatVisuals.ActiveDamageNumbers<=96,"dense hits always create new damage labels while recycling oldest display");
            yield return new WaitForSeconds(.9f);
            Check(CombatVisuals.ActiveDamageNumbers==0,"dense damage label burst releases every transient");
            f=game.NearestEnemy(game.PlayerPosition); f.speed=0;f.hp=1000;f.transform.position=game.PlayerPosition+Vector3.forward*3;
            foreach(var other in game.Enemies)if(other!=null&&other!=f){other.speed=0;other.transform.position=game.PlayerPosition+Vector3.forward*6;}
            game.Equip(Arsenal.Legendary(0));game.AimAt(f.transform.position);game.Fire();
            Check(game.ammo==game.gun.magazine-1,"legendary fire consumes ammunition");
            float deadline=Time.time+1;while(game.ricochetEvents==0&&Time.time<deadline)yield return null;
            Check(game.ricochetEvents>0&&game.splitEvents>=2,"prism ricochets and spawns split bullets");
            yield return null;
            int survivingSplits=0;foreach(var shot in FindObjectsByType<Shot>())if(shot.friendly&&shot.effect==GunEffect.Normal&&shot.last==f)survivingSplits++;
            Check(survivingSplits>=2,"split bullets escape the original target");
            yield return new WaitForSeconds(.2f);
            game.Equip(Arsenal.Legendary(1));
            Check(game.gun.effect==GunEffect.Orbit,"orbit weapon equips");
            var drones=new System.Collections.Generic.List<Transform>();
            foreach(var t in FindObjectsByType<Transform>())if(t.name=="Isotope escort drone")drones.Add(t);
            Check(drones.Count==3,"three isotope escort drones exist");
            foreach(var drone in drones)Check(VisibleMeshes(drone)>=6,"isotope escort drone has visible hull reactor rotors and stabilizers");
            yield return null;
            Vector3 dronePosition=drones[0].position;
            yield return new WaitForSeconds(.1f);
            Check((drones[0].position-dronePosition).sqrMagnitude>.01f,"isotope escort drones actually orbit the survivor");
            game.Fire();
            bool launchedFromDrone=false;
            foreach(var shot in FindObjectsByType<Shot>())
                if(shot.friendly&&shot.effect==GunEffect.Orbit)
                    foreach(var drone in drones)if((shot.transform.position-drone.position).sqrMagnitude<.000001f&&Mathf.Abs(shot.velocity.y)<.001f)launchedFromDrone=true;
            Check(launchedFromDrone,"isotope projectile launches horizontally from an actual escort drone position");
            yield return new WaitForSeconds(.3f);
            game.Equip(Arsenal.Legendary(2));
            f=game.NearestEnemy(game.PlayerPosition);f.speed=0; health=f.hp;
            game.AimAt(f.transform.position);game.Fire();
            deadline=Time.time+1;while(FindAnyObjectByType<Well>()==null&&Time.time<deadline)yield return null;
            Check(FindAnyObjectByType<Well>()!=null,"singularity projectile creates gravity well on impact");
            yield return new WaitForSeconds(1.5f);
            Check(f==null||f.dead||f.hp<health,"gravity well detonates and damages");
            game.Equip(Arsenal.Legendary(0));
            yield return new WaitForEndOfFrame();
            yield return Capture("legendary.png");
            yield return new WaitForSeconds(.3f);
            for(int sector=0;sector<game.CurrentDungeon.AreaCount;sector++)
            {
                foreach(var enemy in game.Enemies.ToArray())if(enemy!=null&&!enemy.dead)enemy.Hit(100000);
                yield return null;
                if(sector<game.CurrentDungeon.AreaCount-1)
                {
                    Check(game.LootCount>0,"clear reward loot sector "+sector);
                    game.Advance();Check(game.screen==VaultGame.Screen.Reward,"augment screen sector "+sector);
                    if(sector==0){VaultLanguage.Set(true,false);yield return Capture("reward-ja.png");VaultLanguage.Set(false,false);}
                    game.ChooseReward(0);game.invincible=999;
                    Check(game.room==sector+1&&game.Active,"next sector starts "+sector);
                }
            }
            Check(game.Active,"boss victory keeps field active for loot before return gate");game.Advance();
            Check(game.screen==VaultGame.Screen.Victory,"boss kill completes stage");
            Check(game.HighestUnlocked>=1,"first dungeon clear unlocks the next dungeon");
            yield return Capture("victory.png");
            for(int dungeon=1;dungeon<3;dungeon++){
                game.EnterHub();Check(game.EnterDungeon(dungeon),"cleared progression allows dungeon "+dungeon);game.invincible=999;
                foreach(var enemy in game.Enemies)Check(game.Layout.SpawnSafe(enemy.transform.position),"new dungeon enemy starts outside entry safe area / "+dungeon);
                VaultLanguage.Set(true,false);yield return Capture("dungeon-"+dungeon+"-ja.png");VaultLanguage.Set(false,false);
                for(int area=0;area<game.CurrentDungeon.AreaCount;area++){foreach(var enemy in game.Enemies.ToArray())if(enemy!=null&&!enemy.dead)enemy.Hit(100000);yield return null;game.Advance();if(area<game.CurrentDungeon.AreaCount-1){float baseDamage=game.gun.damage;int choice=0;while(choice<3&&game.RewardChoices[choice]==3)choice++;game.ChooseReward(Mathf.Min(2,choice));game.invincible=999;Check(game.gun.damage==baseDamage,"temporary boons never rewrite item base damage / "+dungeon+" / "+area);}}
                Check(game.screen==VaultGame.Screen.Victory,"themed dungeon boss and return gate finish / "+dungeon);
            }
            yield return new WaitForSeconds(.3f);
            game.StartRun(42);game.invincible=0;game.Hurt(9999);
            Check(game.screen==VaultGame.Screen.Dead,"death ends run");
            game.StartRun(43);Check(game.Active&&game.hp==game.maxHp,"retry resets run");
            yield return CaptureModelShowcases(game);
            VerifyFieldEconomy(game);
            VaultLanguage.Set(savedLanguage,false);
            File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"PASS / "+checks+" checks / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));
            Application.Quit(0);
        }
    }
}






