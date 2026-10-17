using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace NeonVault
{
    public sealed class WorldVisualReview:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--world-visual-review")>=0)new GameObject("World visual review").AddComponent<WorldVisualReview>();}
        IEnumerator Start()
        {
            yield return null;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");
            string path=at>=0?args[at+1]:"D:/Unity/WorldVisualQA";Directory.CreateDirectory(path);
            UnityEngine.Screen.SetResolution(1440,900,false);yield return null;
            var game=FindAnyObjectByType<VaultGame>();
            game.StartRun(18335);game.enabled=false;
            foreach(var enemy in FindObjectsByType<Foe>(FindObjectsSortMode.None))enemy.enabled=false;
            yield return null;
            var camera=game.GameCamera;
            Capture(camera,Path.Combine(path,"stage-player.png"));
            var batches=GameObject.Find("Ruined district / collapsed civic and industrial fabric");
            if(batches!=null)
            {
                var filters=batches.GetComponentsInChildren<MeshFilter>();int i=0;
                foreach(var filter in filters)
                {
                    if(i>=3)break;
                    var verts=filter.sharedMesh.vertices;if(verts.Length==0)continue;
                    Vector3 p=filter.transform.TransformPoint(verts[Mathf.Min(verts.Length-1,verts.Length*(i+1)/4)]);
                    camera.orthographicSize=5.5f;camera.transform.position=p-camera.transform.forward*15;
                    yield return null;Capture(camera,Path.Combine(path,"ruin-detail-"+i+".png"));i++;
                }
            }
            var driver=game.PlayerAppearance.GetComponent<DetailedHeroPlayer3D>();
            driver.ApplyArmor(new ArmorItem[5]);game.AimAt(game.PlayerPosition+Vector3.forward*10);driver.Synchronize();
            camera.orthographicSize=.255f;
            camera.transform.position=driver.body.head.position+new Vector3(.26f,.16f,1.4f);
            camera.transform.LookAt(driver.body.head.position+Vector3.up*.10f);
            yield return null;Capture(camera,Path.Combine(path,"actual-face-unarmored.png"));
            game.screen=VaultGame.Screen.DungeonSelect;game.enabled=true;Time.timeScale=0;
            foreach(bool japanese in new[]{false,true})
            {
                VaultLanguage.Set(japanese,false);yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(path,japanese?"city-map-ja.png":"city-map-en.png"));
                yield return null;yield return new WaitForEndOfFrame();
            }
            File.WriteAllText(Path.Combine(path,"result.txt"),"CAPTURED actual Unity scene and UI; visual acceptance pending. No user save mutation requested.");
            yield return new WaitForEndOfFrame();Application.Quit();
        }
        static void Capture(Camera camera,string file)
        {
            var rt=new RenderTexture(1440,1000,24);var old=RenderTexture.active;var target=camera.targetTexture;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1440,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,1000),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());
            camera.targetTexture=target;RenderTexture.active=old;Destroy(tex);rt.Release();Destroy(rt);
        }
    }
}
