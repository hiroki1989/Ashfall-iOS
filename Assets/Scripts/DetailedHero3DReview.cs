using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace NeonVault
{
    public sealed class DetailedHero3DReview:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"--hero3d-review")>=0||Array.IndexOf(args,"--hero3d-playcheck")>=0||Array.IndexOf(args,"--boss-pose-review")>=0)new GameObject("Actual Unity 3D hero review").AddComponent<DetailedHero3DReview>();}
        IEnumerator Start()
        {
            yield return null;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--boss-pose-review")>=0){yield return BossPoseReview();yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hero3d-playcheck")>=0){yield return PlayCheck();yield break;}
            foreach(var game in FindObjectsByType<VaultGame>(FindObjectsSortMode.None))game.enabled=false;
            var stage=new GameObject("Isolated geometric studio").transform;stage.position=new Vector3(1000,0,1000);
            var hero=DetailedHero3D.Create(stage,DetailedHeroMaterials3D.Get);var equipment=DetailedHeroEquipment3D.Build(hero,DetailedHeroMaterials3D.Get);
            var rifle=DetailedHeroEquipment3D.BuildRifle(stage,DetailedHeroMaterials3D.Get);var pistol=DetailedHeroEquipment3D.BuildPistol(stage,DetailedHeroMaterials3D.Get);
            var c=new GameObject("True mesh review camera").AddComponent<Camera>();c.depth=100;c.orthographic=true;c.orthographicSize=1.15f;c.backgroundColor=new Color(.28f,.29f,.28f);c.clearFlags=CameraClearFlags.SolidColor;c.nearClipPlane=.05f;c.farClipPlane=20;
            c.transform.position=stage.position+new Vector3(0,2.5f,4.2f);c.transform.LookAt(stage.position+Vector3.up*.93f);
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.48f);
            foreach(var existing in FindObjectsByType<Light>(FindObjectsSortMode.None))existing.enabled=false;
            var light=new GameObject("Studio key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(42,-35,0);light.shadows=LightShadows.Soft;
            var fill=new GameObject("Studio fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.38f;fill.transform.rotation=Quaternion.Euler(25,140,0);
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");string path=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/Hero3DQA";Directory.CreateDirectory(path);
            try
            {
                foreach(var list in equipment.slots)foreach(var part in list)part.gameObject.SetActive(false);
                rifle.root.gameObject.SetActive(false);pistol.root.gameObject.SetActive(false);
                c.orthographicSize=.25f;c.transform.position=stage.position+new Vector3(0,1.93f,1.1f);c.transform.LookAt(stage.position+Vector3.up*1.71f);
                foreach(int yaw in new[]{0,-35,90,145}){hero.SetPose("lowrifle",Quaternion.Euler(0,yaw,0)*Vector3.forward,-1);Capture(c,Path.Combine(path,"default-hair-"+yaw+".png"));}
                foreach(var list in equipment.slots)foreach(var part in list)part.gameObject.SetActive(true);
                c.orthographicSize=1.15f;c.transform.position=stage.position+new Vector3(0,2.5f,4.2f);c.transform.LookAt(stage.position+Vector3.up*.93f);
                foreach(string mode in new[]{"lowrifle","aimrifle","aimpistol"})foreach(int angle in new[]{-35,90,145})
                {
                    Vector3 direction=Quaternion.Euler(0,angle,0)*Vector3.forward;
                    rifle.root.gameObject.SetActive(mode!="aimpistol");pistol.root.gameObject.SetActive(mode=="aimpistol");
                    DetailedHeroPlayer3D.Pose(hero,mode=="aimpistol"?pistol:rifle,mode=="aimpistol",mode!="lowrifle",direction,-1);
                    yield return null;
                    var rt=new RenderTexture(1440,1200,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,1200),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(path,mode+"-"+angle+".png"),tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=old;Destroy(tex);rt.Release();Destroy(rt);
                }
                var checks=new System.Text.StringBuilder();int errors=0;
                rifle.root.gameObject.SetActive(true);pistol.root.gameObject.SetActive(false);
                foreach(bool aiming in new[]{false,true})foreach(int heading in new[]{0,45,90,135,180,225,270,315})
                {
                    var sheet=new Texture2D(960,720,TextureFormat.RGB24,false);
                    for(int frame=0;frame<6;frame++)
                    {
                        float phase=frame/6f;Vector3 facing=Quaternion.Euler(0,-35,0)*Vector3.forward;
                        Vector3 travel=Quaternion.Euler(0,heading,0)*facing*5;
                        DetailedHeroPlayer3D.Pose(hero,rifle,false,aiming,facing,phase,travel,1);
                        float scale=hero.transform.lossyScale.y;
                        float rightError=Vector3.Distance(hero.rightHand.position,rifle.trigger.position+rifle.root.up*.065f*scale-rifle.root.forward*.01f*scale);
                        float leftError=Vector3.Distance(hero.leftHand.position,rifle.support.position-rifle.root.forward*.065f*scale-rifle.root.up*.01f*scale);
                        if(rightError>.015f||leftError>.015f)errors++;
                        checks.AppendLine(aiming+" move="+heading+" phase="+phase+" right="+rightError+" left="+leftError);
                        var rt=new RenderTexture(320,360,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
                        var tile=new Texture2D(320,360,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,320,360),0,0);tile.Apply();sheet.SetPixels((frame%3)*320,(1-frame/3)*360,320,360,tile.GetPixels());
                        c.targetTexture=null;RenderTexture.active=old;Destroy(tile);rt.Release();Destroy(rt);
                    }
                    sheet.Apply();File.WriteAllBytes(Path.Combine(path,(aiming?"aim-":"carry-")+"movement-"+heading+".png"),sheet.EncodeToPNG());Destroy(sheet);
                }
                checks.AppendLine("Failures="+errors);File.WriteAllText(Path.Combine(path,"locomotion-checks.txt"),checks.ToString());
                foreach(float phase in new[]{.15f,.28f,.31f,.65f,.78f,.81f})
                {
                    DetailedHeroPlayer3D.Pose(hero,rifle,false,false,Vector3.right,phase,Vector3.right*5,1);
                    Capture(c,Path.Combine(path,"boot-roll-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                    checks.AppendLine("foot "+phase+" Lheel="+hero.LeftHeelContact.y+" Ltoe="+hero.LeftToeContact.y+" Rheel="+hero.RightHeelContact.y+" Rtoe="+hero.RightToeContact.y);
                }
                rifle.root.gameObject.SetActive(false);pistol.root.gameObject.SetActive(false);
                for(int model=0;model<Arsenal.ModelCount;model++)
                {
                    var w=DetailedHeroArsenal3D.BuildWeapon(stage,model,null);
                    DetailedHeroPlayer3D.Pose(hero,w,Arsenal.IsOneHanded(model),true,Quaternion.Euler(0,-35,0)*Vector3.forward,-1);
                    Vector3 expect=w.trigger.position+w.root.up*.065f-w.root.forward*.01f;float error=Vector3.Distance(hero.rightHand.position,expect);checks.AppendLine("weapon "+model+" Rerror="+error);if(error>.015f)errors++;
                    Capture(c,Path.Combine(path,"weapon-"+model+".png"));w.root.gameObject.SetActive(false);Destroy(w.root.gameObject);
                }
                foreach(var list in equipment.slots)foreach(var part in list)part.gameObject.SetActive(false);
                var armor=DetailedHeroArsenal3D.ApplyArmor(hero,null);
                for(int variant=0;variant<5;variant++)
                {
                    var items=new ArmorItem[5];for(int slot=0;slot<5;slot++){items[slot]=Armory.Starter((ArmorSlot)slot);items[slot].visualVariant=variant;}
                    armor.SetItems(items);DetailedHeroPlayer3D.Pose(hero,rifle,false,false,Quaternion.Euler(0,-35,0)*Vector3.forward,-1);rifle.root.gameObject.SetActive(true);
                    Capture(c,Path.Combine(path,"armor-"+variant+".png"));
                    DetailedHeroPlayer3D.Pose(hero,rifle,false,false,Quaternion.Euler(0,145,0)*Vector3.forward,-1);Capture(c,Path.Combine(path,"armor-"+variant+"-rear.png"));
                    rifle.root.gameObject.SetActive(false);
                }
                checks.AppendLine("FinalFailures="+errors);File.WriteAllText(Path.Combine(path,"locomotion-checks.txt"),checks.ToString());
                for(int family=0;family<5;family++){var melee=DetailedHeroArsenal3D.BuildMelee(stage,(MeleeWeaponFamily)family);melee.position=stage.position+Vector3.up;melee.localScale=Vector3.one*2;Capture(c,Path.Combine(path,"melee-"+family+".png"));melee.gameObject.SetActive(false);Destroy(melee.gameObject);}
                checks.AppendLine("Player "+MeshStats(hero.transform));
                hero.gameObject.SetActive(false);rifle.root.gameObject.SetActive(false);
                var owned=new System.Collections.Generic.List<Material>();var art=new VaultArt(stage,owned,new System.Random(183));
                for(int kind=0;kind<10;kind++)
                {
                    var enemy=art.Character(stage.position,false,kind);enemy.rotation=Quaternion.Euler(0,-35,0);enemy.position=stage.position+Vector3.up*.02f;
                    enemy.GetComponent<ActorEquipmentVisual>()?.SynchronizePose();c.orthographicSize=kind==3?2.5f:1.65f;
                    Capture(c,Path.Combine(path,"enemy-"+kind+".png"));
                    checks.AppendLine("Enemy "+kind+" "+MeshStats(enemy));
                    var enemyRig=enemy.GetComponent<FaithfulEnemy3D>();enemyRig.NotifyAttack();yield return new WaitForSeconds(.12f);Capture(c,Path.Combine(path,"enemy-"+kind+"-attack.png"));enemyRig.BeginDeath();yield return new WaitForSeconds(.40f);Capture(c,Path.Combine(path,"enemy-"+kind+"-death.png"));
                    enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);
                }
                File.WriteAllText(Path.Combine(path,"geometry-stats.txt"),checks.ToString());
                File.WriteAllText(Path.Combine(path,"result.txt"),"CAPTURED actual Unity mesh renders; visual acceptance pending.\nRenderers="+hero.GetComponentsInChildren<Renderer>().Length);
            }
            finally{Application.Quit();}
        }
        IEnumerator BossPoseReview()
        {
            foreach(var game in FindObjectsByType<VaultGame>(FindObjectsSortMode.None))game.enabled=false;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");string path=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/BossPoseQA";Directory.CreateDirectory(path);
            var stage=new GameObject("Boss pose studio").transform;stage.position=new Vector3(1000,0,1000);
            var model=FaithfulEnemy3D.Create(stage,stage.position,3).GetComponent<FaithfulEnemy3D>();
            var foe=model.gameObject.AddComponent<Foe>();foe.Init(null,3,0);model.Init(foe);
            var camera=new GameObject("Boss pose camera").AddComponent<Camera>();camera.depth=100;camera.orthographic=true;camera.orthographicSize=2.3f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.15f);camera.transform.position=stage.position+new Vector3(0,3.4f,7f);camera.transform.LookAt(stage.position+Vector3.up*1.45f);
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.54f);
            foreach(var old in FindObjectsByType<Light>(FindObjectsSortMode.None))old.enabled=false;
            var light=new GameObject("Boss pose light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(45,-35,0);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var move=typeof(Foe).GetField("bossMove",flags);var charging=typeof(Foe).GetField("charging",flags);var charge=typeof(Foe).GetField("charge",flags);
            string[] names={"rush","slam","sweep"};
            for(int style=0;style<3;style++)
            {
                move.SetValue(foe,Enum.ToObject(move.FieldType,style));charging.SetValue(foe,true);charge.SetValue(foe,foe.TelegraphSeconds*.35f);
                yield return null;Capture(camera,Path.Combine(path,names[style]+"-warning.png"));
                charging.SetValue(foe,false);model.NotifyAttack();yield return new WaitForSeconds(.12f);
                Capture(camera,Path.Combine(path,names[style]+"-strike.png"));
                yield return new WaitForSeconds(.35f);
            }
            File.WriteAllText(Path.Combine(path,"result.txt"),"CAPTURED three distinct boss warning and strike poses / "+Application.unityVersion);
            Application.Quit();
        }
        static string MeshStats(Transform root)
        {
            long triangles=0,vertices=0;int draws=0;foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){if(!filter.gameObject.activeInHierarchy||!filter.sharedMesh)continue;var r=filter.GetComponent<Renderer>();if(!r||!r.enabled)continue;vertices+=filter.sharedMesh.vertexCount;triangles+=filter.sharedMesh.triangles.Length/3;draws++;}return "triangles="+triangles+" vertices="+vertices+" renderers="+draws;
        }
        static void Capture(Camera c,string path)
        {
            var rt=new RenderTexture(1440,1200,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1440,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,1200),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            c.targetTexture=null;RenderTexture.active=old;Destroy(tex);rt.Release();Destroy(rt);
        }
        IEnumerator PlayCheck()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");string path=at>=0?args[at+1]:"D:/Unity/Hero3DPlayQA";Directory.CreateDirectory(path);
            var game=FindAnyObjectByType<VaultGame>();game.StartRun(18335);game.enabled=false;
            foreach(var enemy in FindObjectsByType<Foe>(FindObjectsSortMode.None))enemy.enabled=false;
            var report=new System.Text.StringBuilder();int failures=0;
            for(int model=0;model<Arsenal.ModelCount;model++)
            {
                game.gun=Arsenal.Model(model,Rarity.Common);game.ammo=game.gun.magazine;
                for(int angle=0;angle<360;angle+=45)
                {
                    Vector3 aim=Quaternion.Euler(0,angle,0)*Vector3.forward;game.AimAt(game.PlayerPosition+aim*20);game.Fire();
                    var driver=game.PlayerAppearance.GetComponent<DetailedHeroPlayer3D>();driver.Synchronize();
                    float grip=Vector3.Distance(driver.body.rightHand.position,game.PlayerWeapon.GripPosition+game.PlayerWeapon.grip.parent.up*(.065f*driver.body.transform.lossyScale.y)-game.PlayerWeapon.grip.parent.forward*(.01f*driver.body.transform.lossyScale.y));
                    bool good=grip<.012f&&Vector3.Dot(game.PlayerWeapon.muzzle.forward,game.PlayerWeapon.Direction)>.999f;
                    if(!good)failures++;report.AppendLine(model+" / "+angle+" grip="+grip.ToString("F4")+" aligned="+good);
                    yield return null;
                }
            }
            foreach(var shot in FindObjectsByType<Shot>(FindObjectsSortMode.None))Destroy(shot.gameObject);yield return new WaitForSeconds(.4f);
                        game.gun=Arsenal.TrailWarden();game.ammo=game.gun.magazine;
            var reviewDriver=game.PlayerAppearance.GetComponent<DetailedHeroPlayer3D>();
            var reviewArmor=new ArmorItem[5];for(int slot=0;slot<5;slot++)reviewArmor[slot]=Armory.TrailWarden((ArmorSlot)slot);
            game.EquippedArmor=reviewArmor;reviewDriver.ApplyArmor(reviewArmor);reviewDriver.Synchronize();
            bool generatedActive=reviewDriver.IsGeneratedLookActive;
            report.AppendLine("Generated loadout active="+generatedActive);if(!generatedActive)failures++;
            foreach(ArmorSlot slot in new[]{ArmorSlot.Chest,ArmorSlot.Arms,ArmorSlot.Legs,ArmorSlot.Feet})
            {int triangles=reviewDriver.GeneratedArmorSlotTriangleCount(slot);report.AppendLine("Slot "+slot+" triangles="+triangles);if(triangles<=0)failures++;}
            reviewDriver.NotifyMelee();reviewDriver.Synchronize();
            bool meleeKeepsGenerated=reviewDriver.IsGeneratedLookActive;
            report.AppendLine("Melee keeps generated loadout="+meleeKeepsGenerated);if(!meleeKeepsGenerated)failures++;
            Capture(game.GameCamera,Path.Combine(path,"actual-game-melee-keeps-loadout.png"));
            yield return new WaitForSeconds(.30f);reviewDriver.Synchronize();
            game.AimAt(game.PlayerPosition+new Vector3(-1,0,-1)*20);reviewDriver.Synchronize();
            yield return null;
            Capture(game.GameCamera,Path.Combine(path,"actual-game-normal-camera.png"));
            game.GameCamera.orthographicSize=2.15f;game.GameCamera.transform.position=game.PlayerPosition+Vector3.up*.95f-game.GameCamera.transform.forward*7;
            yield return null;
            var rt=new RenderTexture(1440,1200,24);var old=RenderTexture.active;game.GameCamera.targetTexture=rt;game.GameCamera.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,1200),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(path,"actual-game-player.png"),tex.EncodeToPNG());game.GameCamera.targetTexture=null;RenderTexture.active=old;Destroy(tex);rt.Release();Destroy(rt);
            reviewDriver.Synchronize();
            foreach(int angle in new[]{0,45,90,135,180,225,270,315})
            {
                Vector3 facing=Quaternion.Euler(0,angle,0)*Vector3.forward;
                game.AimAt(game.PlayerPosition+facing*20);reviewDriver.Synchronize();
                yield return null;
                Capture(game.GameCamera,Path.Combine(path,"actual-game-warden-"+angle+".png"));
                reviewDriver.NotifyFire();reviewDriver.Synchronize();
                Capture(game.GameCamera,Path.Combine(path,"actual-game-aim-"+angle+".png"));
                yield return new WaitForSeconds(.35f);
            }
            game.PlayerAppearance.gameObject.SetActive(false);
            for(int kind=0;kind<10;kind++)
            {
                // PlayerPosition is already the ground-level actor origin; the enemy factory
                // accepts the legacy spawn height used by SectorLayout.Center.
                var enemy=FaithfulEnemy3D.Create(game.transform,game.PlayerPosition+Vector3.up*.62f,kind);
                enemy.rotation=Quaternion.Euler(0,225,0);
                yield return null;
                var renderers=enemy.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds();bool initial=true;
                foreach(var r in renderers){if(!r.enabled||!r.gameObject.activeInHierarchy)continue;if(initial){bounds=r.bounds;initial=false;}else bounds.Encapsulate(r.bounds);}
                game.GameCamera.orthographicSize=Mathf.Max(1.4f,bounds.size.y*.68f);
                game.GameCamera.transform.position=bounds.center-game.GameCamera.transform.forward*9;
                foreach(int yaw in new[]{225,315,45}){enemy.rotation=Quaternion.Euler(0,yaw,0);yield return null;Capture(game.GameCamera,Path.Combine(path,"actual-enemy-"+kind+"-"+yaw+".png"));}
                enemy.rotation=Quaternion.Euler(0,225,0);var model=enemy.GetComponent<FaithfulEnemy3D>();model.NotifyAttack();yield return new WaitForSeconds(.12f);
                Capture(game.GameCamera,Path.Combine(path,"actual-enemy-"+kind+"-attack.png"));
                model.BeginDeath();yield return new WaitForSeconds(.40f);Capture(game.GameCamera,Path.Combine(path,"actual-enemy-"+kind+"-death.png"));
                enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);
            }
            game.PlayerAppearance.gameObject.SetActive(true);
            game.BuildHub();game.enabled=false;yield return null;
            var npc=GameObject.Find("Mara / Salvage Merchant");var actor=game.PlayerAppearance.transform;Bounds nb=new Bounds(),pb=new Bounds();bool first=true;foreach(var r in npc.GetComponentsInChildren<Renderer>()){if(first){nb=r.bounds;first=false;}else nb.Encapsulate(r.bounds);}first=true;foreach(var r in actor.GetComponentsInChildren<Renderer>()){if(!r.enabled||!r.gameObject.activeInHierarchy)continue;if(first){pb=r.bounds;first=false;}else pb.Encapsulate(r.bounds);}report.AppendLine("NPC height="+nb.size.y+" player="+pb.size.y);
            report.AppendLine("Failures="+failures);File.WriteAllText(Path.Combine(path,"result.txt"),report.ToString());Application.Quit();
        }
    }
}

