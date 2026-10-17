using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NeonVault
{
    // Finite, synthetic-pointer review. This is not a physical multitouch/device test.
    public sealed class MobileInputReview : MonoBehaviour
    {
        string output;
        readonly List<string> results = new List<string>();
        int checks, failures;
        VaultGame game;
        MobileTouchControls controls;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--mobile-review") >= 0)
                new GameObject("Mobile input review").AddComponent<MobileInputReview>();
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "--qa-output");
            output = at >= 0 && at + 1 < args.Length ? args[at + 1] : "D:/Unity/Ashfall-iOS/QA/Mobile";
            Directory.CreateDirectory(output);
            var run = RunReview();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception e) { Check(false, "Unhandled review exception: " + e); break; }
                if (!next) break;
                yield return run.Current;
            }
            MobileTouchControls.ResetInput();
            if (controls) controls.ReviewSafeArea = null;
            Time.timeScale = 1;
            results.Add("Synthetic pointer injection only; physical touch, Metal, IL2CPP and device performance remain unverified.");
            results.Add("Checks=" + checks + " Failures=" + failures);
            File.WriteAllLines(Path.Combine(output, "mobile-review-result.txt"), results);
            Debug.Log("ASHFALL_MOBILE_REVIEW Checks=" + checks + " Failures=" + failures);
            Application.Quit(failures == 0 ? 0 : 1);
        }
        public IEnumerator RunReview()
        {
            for (int i = 0; i < 120; i++)
            {
                game = FindAnyObjectByType<VaultGame>(); controls = FindAnyObjectByType<MobileTouchControls>();
                if (game && controls) break;
                yield return null;
            }
            Check(game && controls && MobileTouchControls.Enabled, "Review game and mobile controls exist");
            if (!game || !controls) yield break;
            game.StartRun(240017); game.invincible = 99999;
            foreach (var enemy in game.Enemies) if (enemy) enemy.enabled = false;
            game.enabled = false;
            // Wait for controls to adopt Playing mode before injecting synthetic input.
            yield return null; yield return null;
            foreach (var size in new[] { new Vector2Int(2532,1170), new Vector2Int(1600,1200) })
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                for (int i=0;i<90 && (Screen.width!=size.x || Screen.height!=size.y);i++) yield return null;
                yield return null;
                bool phone = size.x == 2532;
                // Screen.safeArea uses bottom-left origin. Simulate landscape notch and home indicator.
                controls.ReviewSafeArea = phone ? new Rect(132,63,Screen.width-264,Screen.height-63) : new Rect(0,24,Screen.width,Screen.height-24);
                yield return null; yield return null;
                Check(Screen.width == size.x && Screen.height == size.y, "Framebuffer resolution " + size);
                VerifyLayout(size.ToString());
                VerifyInput();
                yield return VerifyModal();
                yield return CaptureScreens(phone ? "iphone" : "ipad");
                game.StartRun(240017); game.invincible=99999; foreach(var enemy in game.Enemies) if(enemy) enemy.enabled=false; game.enabled = false;
                yield return null; yield return null;
            }
        }
        void Check(bool pass, string label)
        {
            checks++; if (!pass) failures++;
            results.Add((pass ? "PASS " : "FAIL ") + label);
        }
        static Vector2 Bottom(Vector2 top) => new Vector2(top.x, Screen.height-top.y);
        void Pointer(int id, Rect rect, Vector2 offset, TouchPhase phase)
        { controls.InjectPointer(id,Bottom(rect.center+offset),phase); }
        void VerifyLayout(string label)
        {
            var rects = new List<Rect> { MobileTouchControls.MoveRect,MobileTouchControls.AimRect };
            foreach(MobileAction action in Enum.GetValues(typeof(MobileAction))) rects.Add(MobileTouchControls.GetControlRect(action));
            var safe=MobileTouchControls.SafeRect;
            for(int i=0;i<rects.Count;i++)
            {
                var r=rects[i];
                Check(r.xMin>=safe.xMin-.1f && r.yMin>=safe.yMin-.1f && r.xMax<=safe.xMax+.1f && r.yMax<=safe.yMax+.1f,label+" safe rect "+i);
                float minimum=44*Mathf.Max(1,Mathf.Min(safe.width,safe.height)/390f);
                Check(r.width>=minimum-.1f && r.height>=minimum-.1f,label+" minimum target "+i);
                for(int j=0;j<i;j++) Check(!r.Overlaps(rects[j]),label+" no overlap "+j+"/"+i);
            }
        }
        void VerifyInput()
        {
            MobileTouchControls.ResetInput();
            Rect move=MobileTouchControls.MoveRect, aim=MobileTouchControls.AimRect;
            Pointer(1,move,Vector2.zero,TouchPhase.Began);
            Check(MobileTouchControls.Movement==Vector2.zero,"Movement center deadzone");
            Pointer(1,move,new Vector2(move.width*.19f,0),TouchPhase.Moved);
            Check(Mathf.Abs(MobileTouchControls.Movement.x-.5f)<.02f,"Movement analog half strength");
            Pointer(2,aim,new Vector2(0,-aim.height*.3f),TouchPhase.Began);
            Check(MobileTouchControls.Firing && MobileTouchControls.Movement.x>.45f,"Independent simultaneous move and fire");
            Check(Vector2.Dot(MobileTouchControls.Aim,Vector2.up)>.99f,"Aim GUI to screen axis");
            Check((VaultMobileInputBridge.ToWorldMovement(Vector2.up)-Vector3.right).sqrMagnitude<.001f,"Aim world conversion");
            Pointer(1,move,Vector2.zero,TouchPhase.Ended);
            Check(MobileTouchControls.Movement==Vector2.zero && MobileTouchControls.Firing,"Move release retains aim finger");
            Pointer(1,move,new Vector2(move.width*.5f,0),TouchPhase.Began);
            Pointer(2,aim,Vector2.zero,TouchPhase.Canceled);
            Check(!MobileTouchControls.Firing && MobileTouchControls.Movement.x>.99f,"Aim cancel retains move finger and clamps magnitude");
            Pointer(1,move,Vector2.zero,TouchPhase.Canceled);
            Check(MobileTouchControls.Movement==Vector2.zero,"Move cancellation");
            foreach(MobileAction action in Enum.GetValues(typeof(MobileAction)))
            {
                var r=MobileTouchControls.GetControlRect(action);
                Pointer(20+(int)action,r,Vector2.zero,TouchPhase.Began);
                Check(MobileTouchControls.Consume(action),"Button queued "+action);
                Check(!MobileTouchControls.Consume(action),"Button consumed once "+action);
                Pointer(20+(int)action,r,Vector2.zero,TouchPhase.Ended);
            }
            Pointer(1,move,new Vector2(move.width*.5f,0),TouchPhase.Began);
            Pointer(2,aim,new Vector2(aim.width*.3f,0),TouchPhase.Began);
            Pointer(3,MobileTouchControls.GetControlRect(MobileAction.Reload),Vector2.zero,TouchPhase.Began);
            MobileTouchControls.ResetInput();
            Check(MobileTouchControls.Movement==Vector2.zero && !MobileTouchControls.Firing && !MobileTouchControls.Consume(MobileAction.Reload),"Focus/pause reset path clears held fingers and actions");
        }
        IEnumerator VerifyModal()
        {
            game.ToggleInventory(); yield return null; yield return null;
            Pointer(9,MobileTouchControls.AimRect,Vector2.zero,TouchPhase.Began);
            Check(!MobileTouchControls.Firing,"Inventory blocks firing input");
            Check(!game.Active,"Inventory blocks active combat");
            game.ToggleInventory(); yield return null; yield return null;
            game.screen=VaultGame.Screen.Pause; yield return null; yield return null;
            Pointer(9,MobileTouchControls.AimRect,Vector2.zero,TouchPhase.Began);
            Check(!MobileTouchControls.Firing,"Pause blocks firing input");
            game.screen=VaultGame.Screen.Playing; yield return null; yield return null;
        }
        IEnumerator CaptureScreens(string device)
        {
            game.enabled=true; game.invincible=99999;
            if(game.ArtifactInventory.Count==0) for(int i=0;i<6;i++) game.ArtifactInventory.Add(ArtifactCatalog.CreateCatalog(ModuleProductCatalog.Get(i%4,i).id,Rarity.Rare,1));
            foreach(bool ja in new[]{false,true})
            {
                VaultLanguage.Set(ja,false); string lang=ja?"ja":"en";
                game.StartRun(240017); game.invincible=99999; foreach(var enemy in game.Enemies) if(enemy) enemy.enabled=false; Time.timeScale=1;
                yield return Capture(device+"-combat-"+lang);
                game.ToggleInventory();
                typeof(VaultGame).GetField("artifactPage",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(game,false);
                yield return Capture(device+"-inventory-guns-"+lang);
                typeof(VaultGame).GetField("artifactPage",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(game,true);
                yield return Capture(device+"-inventory-modules-"+lang);
                game.ToggleInventory(); game.screen=VaultGame.Screen.Pause; Time.timeScale=0;
                yield return Capture(device+"-pause-"+lang);
                game.EnterHub(); Time.timeScale=1;
                yield return Capture(device+"-hub-"+lang);
            }
            MobileTouchControls.ResetInput();
        }
        IEnumerator Capture(string name)
        {
            yield return null; yield return null; yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image) { File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG()); Destroy(image); }
            Check(image!=null,"Visible framebuffer captured "+name);
        }
    }
}
