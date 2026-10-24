using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Captures both sides of the 25% threshold for visual review.
    public sealed class HealthUIReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--health-ui-review")>=0)
                new GameObject("Health UI review").AddComponent<HealthUIReview>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");
            string output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/AshfallArtValidation/health-ui-review";
            Directory.CreateDirectory(output);
            VaultGame game=null;
            for(int i=0;i<120;i++){game=FindAnyObjectByType<VaultGame>();if(game)break;yield return null;}
            if(!game){File.WriteAllText(Path.Combine(output,"result.txt"),"FAIL no game");Application.Quit(1);yield break;}
            game.StartRun(240017);game.invincible=99999;
            foreach(var enemy in game.Enemies)if(enemy)enemy.enabled=false;
            game.enabled=true;
            bool original=VaultLanguage.Japanese;
            foreach(bool japanese in new[]{true,false})
            {
                VaultLanguage.Set(japanese,false);
                string language=japanese?"ja":"en";
                game.hp=game.maxHp;
                yield return Capture(output,"full-"+language);
                game.hp=game.maxHp*.26f;
                yield return Capture(output,"above-threshold-"+language);
                game.hp=game.maxHp*.25f;
                yield return Capture(output,"critical-"+language);
            }
            VaultLanguage.Set(original,false);
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS full, 26%, 25% screenshots in Japanese and English");
            Application.Quit(0);
        }
        static IEnumerator Capture(string output,string name)
        {
            yield return null;yield return null;yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image){File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        }
    }
}
