using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace NeonVault
{
    public sealed class EquipmentScreenReview : MonoBehaviour
    {
        int checks,failures;string output;readonly List<string> report=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--equipment-screen-review")>=0)new GameObject("Equipment screen review").AddComponent<EquipmentScreenReview>();}
        void Check(bool value,string text){checks++;if(!value){failures++;report.Add("FAIL "+text);}}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int p=Array.IndexOf(args,"--qa-output");output=p>=0?args[p+1]:"D:/Unity/QA-equipment-screen";Directory.CreateDirectory(output);
            PlayerPrefs.SetInt("Vault.GeneratedWardenPreview",0);
            PlayerPrefs.DeleteKey("Vault.GeneratedWardenPreview.v2");
            Check(GeneratedWardenPreviewAssets.Enabled,"legacy saved appearance defaults to the approved Warden model after upgrade");
            GeneratedWardenPreviewAssets.SetEnabled(false);
            yield return null;var game=FindAnyObjectByType<VaultGame>();bool language=VaultLanguage.Japanese;
            try{Balance();}catch(Exception e){Check(false,e.ToString());}
            game.Weapons[0]=Arsenal.TrailWarden();game.EquipWeapon(0,0);
            game.ArmorInventory.Clear();for(int s=0;s<5;s++){var a=Armory.TrailWarden((ArmorSlot)s);game.StoreArmor(a);game.EquipArmor(a);}
            game.screen=VaultGame.Screen.Hub;game.ToggleInventory();
            foreach(bool ja in new[]{true,false})
            {
                VaultLanguage.Set(ja,false);
                for(int c=0;c<4;c++){Set(game,"eqCategory",c);Set(game,"eqSelectedKey",null);yield return Capture("inventory-"+(ja?"ja":"en")+"-"+c);}
            }
            Set(game,"eqCategory",1);Set(game,"eqSelectedKey",null);yield return Capture("inventory-armor-drag-before");
            Drag(game,new Vector2(200,500),new Vector2(200,330));Check(Scroll(game,"eqGridScroll")>100,"touch drag scrolls equipment list");yield return Capture("inventory-armor-drag-after");
            Set(game,"eqCategory",0);Set(game,"eqSelectedKey",null);yield return Capture("inventory-detail-before");
            Drag(game,new Vector2(1100,450),new Vector2(1100,290));Check(Scroll(game,"eqDetailScroll")>0,"touch drag scrolls item description");yield return Capture("inventory-detail-after");
            var inspector=FindAnyObjectByType<EquipmentHeroPreview>();Check(inspector!=null,"live preview exists on the equipment screen");
            if(inspector){float angle=inspector.Angle;Drag(game,new Vector2(700,400),new Vector2(780,400));Check(Mathf.Abs(inspector.Angle-angle)>30,"touch drag rotates live character");}
            game.ToggleInventory();game.screen=VaultGame.Screen.Shop;Set(game,"debtPage",false);Check(game.EquipmentMenuVisible&&game.EquipmentGestureRegion(new Vector2(1100,450))==2,"merchant supports independent touch scrolling");
            Set(game,"shopSellingPage",false);Set(game,"eqCategory",0);yield return Capture("merchant-buy");
            Set(game,"shopSellingPage",true);yield return Capture("merchant-sell");
            var preview=FindAnyObjectByType<EquipmentHeroPreview>();Check(preview!=null,"live preview exists");
            var worldPlayer=game.PlayerAppearance?game.PlayerAppearance.GetComponent<DetailedHeroPlayer3D>():null;Check(worldPlayer!=null,"gameplay player presentation exists");
            if(preview)
            {
                preview.Angle=160;yield return Capture("merchant-rear");
                preview.Angle=25;preview.ToggleGeneratedWarden();yield return null;yield return Capture("generated-warden-loadout");
                preview.Angle=90;yield return Capture("generated-warden-side");
                preview.Angle=160;yield return Capture("generated-warden-rear");
                preview.Angle=25;preview.ReviewPhase=.12f;preview.ReviewVelocity=Vector3.forward*4;
                yield return Capture("generated-warden-run-contact");
                preview.ReviewPhase=.39f;yield return Capture("generated-warden-run-flight");
                preview.ReviewFiring=true;preview.ReviewPhase=-1;preview.ReviewVelocity=Vector3.zero;
                yield return Capture("generated-warden-fire");
                preview.ReviewFiring=false;preview.ReviewPhase=-1;
                Check(preview.UsesGeneratedWarden&&preview.GeneratedVisibleRendererCount>=4,"generated base, armor, respirator and weapon render in the equipment preview");
                Check(worldPlayer&&worldPlayer.IsGeneratedLookActive,"appearance switch updates the gameplay player");
                Check(preview.GeneratedLegacyOverlapCount==0,"generated preview hides all legacy armor geometry");
                Check(worldPlayer&&worldPlayer.GeneratedLegacyOverlapCount==0,"generated gameplay player hides all legacy armor geometry");
                if(game.GameCamera)CaptureWorld(game.GameCamera,Path.Combine(output,"generated-warden-world-camera.png"));
                preview.ToggleGeneratedWarden();yield return null;yield return Capture("current-loadout-restored");
                Check(!preview.UsesGeneratedWarden&&preview.GeneratedVisibleRendererCount==0,"switching back restores the existing animated preview");
                Check(worldPlayer&&!worldPlayer.IsGeneratedLookActive,"switching back restores the existing gameplay player");
            }
            VaultLanguage.Set(language,false);Time.timeScale=1;report.Add("Checks="+checks+" Failures="+failures);File.WriteAllLines(Path.Combine(output,"result.txt"),report);Application.Quit(failures==0?0:1);
        }
        static void Set(VaultGame g,string name,object value)=>typeof(VaultGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,value);
        static float Scroll(VaultGame g,string field)=>((Vector2)typeof(VaultGame).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g)).y;
        static void Drag(VaultGame game,Vector2 a,Vector2 b)
        {
            var gestures=FindAnyObjectByType<MobileMenuGestures>();if(!gestures)return;
            Vector2 screenA=VaultGame.MobileCanvasMatrix.MultiplyPoint3x4(a),screenB=VaultGame.MobileCanvasMatrix.MultiplyPoint3x4(b);screenA.y=Screen.height-screenA.y;screenB.y=Screen.height-screenB.y;
            gestures.InjectGesturePointer(819,screenA,TouchPhase.Began);gestures.InjectGesturePointer(819,screenB,TouchPhase.Moved);gestures.InjectGesturePointer(819,screenB,TouchPhase.Ended);
        }
        IEnumerator Capture(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t!=null,"framebuffer "+name);if(t){File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}}
        static void CaptureWorld(Camera camera,string path)
        {
            var texture=new RenderTexture(1440,1200,24);
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
            var image=new Texture2D(1440,1200,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1440,1200),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=previousTarget;RenderTexture.active=previousActive;Destroy(image);texture.Release();Destroy(texture);
        }
        void Balance()
        {
            var random=new System.Random(191019);int regen=0,total=0;
            for(int n=0;n<120;n++)for(int tier=0;tier<5;tier++)
            {
                var g=Arsenal.CreateForDungeon(random,0,(Rarity)tier,3);if(tier<4)Check(g.regenBonus==0&&Array.TrueForAll(g.affixes,x=>x.type!="regen"),"nonlegend regeneration");
                for(int s=0;s<5;s++)
                {
                    var a=Armory.CreateCatalog(ArmorProductCatalog.Get((ArmorSlot)s,0,0).id,(Rarity)tier,3,random);
                    foreach(var x in a.affixes){if(x.type=="regen")regen++;total++;}
                    var baseItem=JsonUtility.FromJson<ArmorItem>(JsonUtility.ToJson(a));
                    foreach(var x in a.affixes)Subtract(baseItem,x);
                    const float epsilon=.0001f;
                    int count=(baseItem.maxHealth>epsilon?1:0)+(baseItem.damageBonus>epsilon?1:0)+(baseItem.reloadBonus>epsilon?1:0)+(baseItem.fireBonus>epsilon?1:0);
                    Check(Mathf.Abs(baseItem.regenBonus)<epsilon&&Mathf.Abs(baseItem.damageReduction)<epsilon&&Mathf.Abs(baseItem.dodgeBonus)<epsilon&&Mathf.Abs(baseItem.ammoBonus)<epsilon,"no extra base effects");
                    Check(s==2?count==2:s==0?baseItem.maxHealth>0&&baseItem.accuracyBonus>0:s==1?baseItem.maxHealth>0&&baseItem.moveBonus<epsilon:baseItem.maxHealth>0&&baseItem.moveBonus>0,"slot base roles");
                    Check(s==2||Mathf.Abs(baseItem.damageBonus)+Mathf.Abs(baseItem.reloadBonus)+Mathf.Abs(baseItem.fireBonus)<epsilon,"offense only gloves");
                    string before=JsonUtility.ToJson(a);GearBalance.Migrate(a);Check(before==JsonUtility.ToJson(a),"migration idempotence");
                }
            }
            Check(regen>0&&regen/(float)total<.04f,"rare regeneration affix "+regen+" / "+total);
            var old=Arsenal.Starter();old.id="legacy-regen";old.regenBonus=5;old.affixes=new[]{new GearAffix{type="regen",value=5}};GearBalance.Migrate(old);Check(old.regenBonus==0&&old.affixes.Length==1&&old.affixes[0].type!="regen","legacy weapon migration replaces forbidden affix");string saved=JsonUtility.ToJson(old);GearBalance.Migrate(old);Check(saved==JsonUtility.ToJson(old),"weapon migration idempotence");
            report.Add("Armor regeneration affixes="+regen+" / "+total);
        }
        static void Subtract(ArmorItem a,GearAffix x){switch(x.type){case "health":a.maxHealth-=x.value;break;case "damage":a.damageBonus-=x.value;break;case "fireRate":a.fireBonus-=x.value;break;case "reload":a.reloadBonus-=x.value;break;case "accuracy":a.accuracyBonus-=x.value;break;case "moveSpeed":a.moveBonus-=x.value;break;case "regen":a.regenBonus-=x.value;break;case "mitigation":a.damageReduction-=x.value;break;case "ammo":a.ammoBonus-=x.value;break;}}
    }
}
