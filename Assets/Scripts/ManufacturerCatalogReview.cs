using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NeonVault
{
    // Opt-in real-player validation. Output and screenshots belong on the D drive.
    public sealed class ManufacturerCatalogReview:MonoBehaviour
    {
        string output;int checks,failures;readonly StringBuilder report=new StringBuilder();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--manufacturer-review")>=0)new GameObject("Manufacturer catalog review").AddComponent<ManufacturerCatalogReview>();}
        void Check(bool success,string message)
        {
            checks++;report.AppendLine((success?"PASS ":"FAIL ")+message);
            if(success)return;
            failures++;File.WriteAllText(Path.Combine(output,"manufacturer-review-result.txt"),"FAIL / "+checks+" / "+message);
            File.WriteAllText(Path.Combine(output,"manufacturer-checks.txt"),report.ToString());Debug.LogError("Manufacturer review: "+message);
        }
        void Finish(){File.WriteAllText(Path.Combine(output,"manufacturer-checks.txt"),report.ToString());File.WriteAllText(Path.Combine(output,"manufacturer-review-result.txt"),(failures==0?"PASS":"FAIL")+" / "+checks+" checks / "+failures+" failures / "+Application.unityVersion+" / "+DateTime.UtcNow.ToString("O"));Application.Quit(failures==0?0:1);}
        static string StatSnapshot(object value)
        {
            var b=new StringBuilder();foreach(var field in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                if(field.FieldType==typeof(float)||field.FieldType==typeof(int)||field.FieldType.IsEnum)b.Append(field.Name).Append('=').Append(field.GetValue(value)).Append(';');
                else if(field.FieldType==typeof(GearAffix[]))b.Append(JsonUtility.ToJson(new AffixWrapper{affixes=(GearAffix[])field.GetValue(value)}));
            }return b.ToString();
        }
        [Serializable] sealed class AffixWrapper{public GearAffix[] affixes;}
        void Finite(object value,string label)
        {
            foreach(var field in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public))if(field.FieldType==typeof(float)){float number=(float)field.GetValue(value);Check(!float.IsNaN(number)&&!float.IsInfinity(number),label+" finite "+field.Name);}
            var affixField=value.GetType().GetField("affixes");var affixes=affixField?.GetValue(value) as GearAffix[];
            if(affixes!=null)foreach(var affix in affixes)Check(affix!=null&&!float.IsNaN(affix.value)&&!float.IsInfinity(affix.value),label+" finite affix");
        }
        void RoundTrip<T>(T value,string label)
        {
            string json=JsonUtility.ToJson(value);var restored=JsonUtility.FromJson<T>(json);
            Check(JsonUtility.ToJson(restored)==json,label+" JSON identity, design, effective stats and affixes preserved");
            Check(StatSnapshot(value)==StatSnapshot(restored),label+" no stat reapplication");
        }
        void VerifyCatalogs()
        {
            Check(Arsenal.ModelCount==18&&Arsenal.CatalogCount==180,"18 gun base models / 180 products");
            Check(ArmorProductCatalog.Count==250&&ModuleProductCatalog.Count==70,"250 armor / 70 module products");
            var ids=new HashSet<string>();var makers=new HashSet<string>();
            for(int i=0;i<Arsenal.CatalogCount;i++)
            {
                var g=Arsenal.Catalog(i,Rarity.Rare);string label="gun "+i;
                Check(ids.Add(g.catalogId)&&ManufacturerCatalog.Index(g.manufacturerId)>=0,label+" unique ID / known maker");makers.Add(g.manufacturerId);
                Check(g.modelVariant==i%18&&g.designVariant==i/18&&Enum.IsDefined(typeof(WeaponClass),Arsenal.Category(g.modelVariant)),label+" base family and design");
                Check(g.damage>0&&g.interval>0&&g.speed>0&&g.reloadSeconds>0&&g.magazine>0&&g.pellets>0&&Arsenal.Accuracy(g)>0,label+" positive combat stats");Finite(g,label);RoundTrip(g,label);
            }
            Check(makers.Count==6,"guns cover six manufacturers");makers.Clear();
            foreach(var p in ArmorProductCatalog.All)
            {
                var a=Armory.CreateCatalog(p.id,Rarity.Rare,5,new System.Random(211));string label=p.id;
                Check(ids.Add(a.catalogId)&&ManufacturerCatalog.Index(a.manufacturerId)>=0,label+" unique ID / known maker");makers.Add(a.manufacturerId);
                Check(a.slot==p.slot&&a.visualVariant==p.visualVariant&&a.designVariant==p.designVariant&&a.visualVariant>=0&&a.visualVariant<5,label+" correct slot and base visual");
                Check(GearScoring.Score(a)>0&&a.damageReduction>=0,label+" positive score / nonnegative mitigation");Finite(a,label);RoundTrip(a,label);
                Check(!string.IsNullOrEmpty(Armory.Name(a,true))&&!string.IsNullOrEmpty(Armory.Name(a,false)),label+" Japanese and English names");
            }
            Check(makers.Count==6,"armor covers six manufacturers");makers.Clear();
            foreach(var p in ModuleProductCatalog.All)
            {
                var a=ArtifactCatalog.CreateCatalog(p.id,Rarity.Rare,5,new System.Random(307));string label=p.id;
                Check(ids.Add(a.catalogId)&&ManufacturerCatalog.Index(a.manufacturerId)>=0,label+" unique ID / known maker");makers.Add(a.manufacturerId);
                Check(a.effect==p.effect&&a.designVariant==p.designVariant&&(p.family<4?a.uniqueEffect==0:a.uniqueEffect>0&&a.dungeon==p.family-4),label+" correct effect / legendary family");
                Check(a.power>0&&a.cooldown>=0,label+" positive power / valid cooldown");Finite(a,label);RoundTrip(a,label);
            }
            Check(makers.Count==6&&ids.Count==500,"500 distinct products / modules cover six manufacturers");
            for(int i=0;i<MeleeWeaponCatalog.CatalogCount;i++){
                var melee=MeleeWeaponCatalog.Catalog(i,Rarity.Rare,3);
                Check(ids.Add(melee.catalogId)&&ManufacturerCatalog.Index(melee.manufacturerId)>=0,"melee unique maker "+i);
                Check(melee.damage>0&&melee.interval>0&&melee.range>0&&melee.arcDegrees>0,"melee positive stats "+i);
                Finite(melee,"melee "+i);RoundTrip(melee,"melee "+i);
            }
            Check(ids.Count==550,"550 total products including melee");
            var oldScatter=new Gun("legacy","",Rarity.Common,GunEffect.Scatter,12,.6f,18,6,6);
            oldScatter.modelVariant=0;float oldDamage=oldScatter.damage;string oldMaker=ManufacturerBranding.Maker(oldScatter);
            Check(VaultLanguage.DisplayModel(oldScatter)==3&&oldScatter.damage==oldDamage,"legacy scatter keeps model and damage after maker inference");
            var legacyGun=Arsenal.Model(3,Rarity.Rare);legacyGun.catalogId=null;legacyGun.manufacturerId=null;string stats=StatSnapshot(legacyGun);string maker=ManufacturerBranding.Maker(legacyGun);
            Check(ManufacturerCatalog.Index(maker)>=0&&StatSnapshot(legacyGun)==stats,"legacy gun identity inference preserves stats");RoundTrip(legacyGun,"legacy gun");
            var legacyArmor=new ArmorItem{id="legacy-armor",slot=ArmorSlot.Chest,visualVariant=3,level=7,damageReduction=.147f,maxHealth=31.5f,moveBonus=-.02f};stats=StatSnapshot(legacyArmor);string json=JsonUtility.ToJson(legacyArmor);maker=ManufacturerBranding.Maker(legacyArmor);
            Check(ManufacturerCatalog.Index(maker)>=0&&stats==StatSnapshot(legacyArmor)&&json==JsonUtility.ToJson(legacyArmor),"legacy armor maker display preserves all saved fields");RoundTrip(legacyArmor,"legacy armor");
            var legacyModule=new ArtifactItem{id="legacy-module",effect=ArtifactEffect.ReloadSurge,level=9,power=3.127f,cooldown=6.32f};stats=StatSnapshot(legacyModule);json=JsonUtility.ToJson(legacyModule);maker=ManufacturerBranding.Maker(legacyModule);
            Check(ManufacturerCatalog.Index(maker)>=0&&stats==StatSnapshot(legacyModule)&&json==JsonUtility.ToJson(legacyModule),"legacy module maker display preserves all saved fields");RoundTrip(legacyModule,"legacy module");
        }
        bool VisibleMesh(Transform root)
        {
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {var renderer=filter.GetComponent<Renderer>();if(filter.sharedMesh&&filter.sharedMesh.vertexCount>0&&renderer&&renderer.enabled&&filter.gameObject.activeInHierarchy)return true;}return false;
        }
        IEnumerator Capture(string name)
        {
            for(int frame=0;frame<15;frame++)yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            Check(texture&&texture.width>0,"visible framebuffer / "+name);
            if(!texture)yield break;
            var pixels=texture.GetPixels32();int visible=0;foreach(var p in pixels)if(p.r+p.g+p.b>90)visible++;
            Check(visible>pixels.Length/100,"nonblack screenshot / "+name);File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());Destroy(texture);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/ManufacturerCatalogQA";Directory.CreateDirectory(output);
            // Write an incomplete marker before any operation that could throw.
            File.WriteAllText(Path.Combine(output,"manufacturer-review-result.txt"),"INCOMPLETE");yield return null;
            if(Array.IndexOf(args,"--illustration-only")>=0){yield return IllustrationReview();yield break;}
            VerifyCatalogs();
            var game=FindAnyObjectByType<VaultGame>();Check(game,"live VaultGame exists");if(!game){Finish();yield break;}
            for(int maker=0;maker<6;maker++){var logo=ManufacturerBranding.Logo(ManufacturerCatalog.Id(maker));Check(logo&&logo.width>0,"manufacturer logo "+maker);File.WriteAllBytes(Path.Combine(output,"logo-"+ManufacturerCatalog.Id(maker)+".png"),logo.EncodeToPNG());}
            game.StartRun(130363);game.invincible=999;game.enabled=false;foreach(var enemy in game.Enemies)if(enemy)enemy.enabled=false;
            var savedArtifacts=game.EquippedArtifacts;
            var fast=new ArtifactItem{id="review-fast",effect=ArtifactEffect.ReloadSurge,power=1,cooldown=2};
            var slow=new ArtifactItem{id="review-slow",effect=ArtifactEffect.ReloadSurge,power=3,cooldown=10};
            game.EquippedArtifacts=new[]{fast,slow};game.ResetArtifactCombat();game.ArtifactOnReload();
            Check(Mathf.Abs(game.ArtifactDamageMultiplier-1.6f)<.001f,"both reload surges initially activate");
            game.TickArtifacts(4);game.ArtifactOnReload();
            Check(Mathf.Abs(game.ArtifactDamageMultiplier-1.15f)<.001f&&game.ArtifactReloadSurgeRemaining(slow)==0,"short cooldown cannot reactivate long cooldown module");
            game.EquippedArtifacts=savedArtifacts;game.ResetArtifactCombat();
            var parent=new GameObject("Manufacturer isolated weapon checks").transform;parent.position=new Vector3(1000,0,1000);
            for(int i=0;i<180;i++)
            {
                VerifyWeapon(parent,i);yield return null;
            }
            Destroy(parent.gameObject);
            var appearance=game.PlayerAppearance;Check(appearance,"live armor appearance exists");if(!appearance){Finish();yield break;}
            for(int visual=0;visual<5;visual++)for(int slot=0;slot<5;slot++)
            {
                var armor=Armory.CreateCatalog(ArmorProductCatalog.Get((ArmorSlot)slot,visual,0).id,Rarity.Rare,5);var items=new ArmorItem[5];items[slot]=armor;
                appearance.ApplyArmor(items);appearance.SynchronizePose();Check(appearance.AppliedArmorCount==1,"base armor slot "+slot+" visual "+visual);yield return null;
                if(slot==1)yield return Capture("armor-base-"+visual+".png");
            }
            for(int maker=0;maker<6;maker++)
            {
                var items=new ArmorItem[5];foreach(var p in ArmorProductCatalog.All)if(p.manufacturerId==ManufacturerCatalog.Id(maker)&&items[(int)p.slot]==null)items[(int)p.slot]=Armory.CreateCatalog(p.id,Rarity.Rare,5);
                game.EquippedArmor=items;appearance.ApplyArmor(items);appearance.SynchronizePose();Check(appearance.AppliedArmorCount==5,"maker armor complete loadout "+maker);yield return null;yield return Capture("armor-maker-"+maker+".png");
            }
            var setReview=typeof(VaultGame).GetMethod("SetManufacturerReview",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);Check(setReview!=null,"manufacturer registry review hook");
            if(setReview==null){Finish();yield break;}
            game.enabled=true;
            foreach(bool ja in new[]{false,true})for(int maker=0;maker<6;maker++)for(int kind=-1;kind<4;kind++)
            {
                VaultLanguage.Set(ja,false);setReview.Invoke(game,new object[]{maker,kind});yield return null;yield return Capture("registry-"+(ja?"ja":"en")+"-maker-"+maker+"-kind-"+kind+".png");
            }
            Finish();
        }
        void VerifyWeapon(Transform parent,int index)
        {
            DetailedHeroEquipment3D.WeaponSockets weapon=null;
            try
            {
                var gun=Arsenal.Catalog(index);weapon=ManufacturerEquipment3D.BuildWeapon(parent,gun);
                bool sockets=weapon!=null&&weapon.root&&weapon.trigger&&weapon.support&&weapon.muzzle;Check(sockets,"weapon sockets "+index);
                if(sockets){Check(VisibleMesh(weapon.root),"visible weapon meshes "+index);Check(Vector3.Distance(weapon.trigger.position,weapon.muzzle.position)>.01f,"separate grip / muzzle "+index);}
            }
            catch(Exception error){Check(false,"weapon "+index+" exception "+error.GetType().Name+": "+error.Message);}
            finally{if(weapon!=null&&weapon.root){weapon.root.gameObject.SetActive(false);Destroy(weapon.root.gameObject);}else foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        }
        IEnumerator IllustrationReview()
        {
            var game=FindAnyObjectByType<VaultGame>();game.StartRun(130363);game.invincible=999;
            foreach(var enemy in game.Enemies)if(enemy)enemy.enabled=false;
            Check(ManufacturerBranding.ModuleIllustrationAvailable,"painted module atlas loaded");
            game.Weapons.Clear();for(int i=0;i<6;i++)game.Weapons.Add(Arsenal.Catalog(18+i*19,Rarity.Rare));
            game.ArmorInventory.Clear();for(int i=0;i<6;i++)game.ArmorInventory.Add(Armory.CreateCatalog(ArmorProductCatalog.Get((ArmorSlot)(i%5),i%5,1+i).id,Rarity.Rare,1));
            game.ArtifactInventory.Clear();for(int i=0;i<6;i++)game.ArtifactInventory.Add(ArtifactCatalog.CreateCatalog(ModuleProductCatalog.Get(i%4,i).id,Rarity.Rare,1));
            game.ToggleInventory();
            foreach(bool ja in new[]{false,true})
            {
                VaultLanguage.Set(ja,false);typeof(VaultGame).GetField("artifactPage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,false);
                yield return Capture("inventory-illustrated-"+(ja?"ja":"en")+".png");
                typeof(VaultGame).GetField("artifactPage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,true);
                yield return Capture("inventory-modules-"+(ja?"ja":"en")+".png");
            }
            game.ToggleInventory();
            foreach(int maker in new[]{0,2,4}){game.SetManufacturerReview(maker,2);yield return Capture("module-illustrations-maker-"+maker+".png");}
            Check(GameObject.Find("One-per-frame equipment portrait renderer")==null,"gun armor and module inventory never creates 3D portrait renderer");
            Finish();
        }
    }
}
