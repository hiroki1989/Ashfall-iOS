using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Opt-in isolated capture for a built player; never runs during normal play.
    public sealed class HumanFactionVisualReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--human-faction-review")>=0)
                new GameObject("Human faction review").AddComponent<HumanFactionVisualReview>();
        }
        IEnumerator Start()
        {
            foreach(var game in FindObjectsByType<VaultGame>(FindObjectsSortMode.None))game.enabled=false;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");
            string output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/HumanFactionReview";Directory.CreateDirectory(output);
            var stage=new GameObject("Human review stage").transform;stage.position=new Vector3(1000,0,1000);
            var camera=new GameObject("Human review camera").AddComponent<Camera>();camera.depth=100;camera.orthographic=true;camera.orthographicSize=1.15f;
            camera.backgroundColor=new Color(.24f,.25f,.25f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.05f;camera.farClipPlane=20;
            camera.transform.position=stage.position+new Vector3(0,2.4f,4);camera.transform.LookAt(stage.position+Vector3.up*.95f);
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.48f);
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))light.enabled=false;
            var key=new GameObject("Review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.15f;key.transform.rotation=Quaternion.Euler(42,-35,0);
            for(int faction=0;faction<3;faction++)for(int variant=0;variant<2;variant++)
            {
                var actor=new GameObject("Review actor");actor.transform.SetParent(stage,false);
                var visual=HumanFactionVisual.Create(actor.transform,faction,variant);
                foreach(int yaw in new[]{0,90,180})
                {
                    actor.transform.localRotation=Quaternion.Euler(0,yaw,0);yield return null;
                    var rt=new RenderTexture(960,960,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(960,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,960),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,"faction-"+faction+"-"+variant+"-"+yaw+".png"),image.EncodeToPNG());
                    camera.targetTexture=null;RenderTexture.active=old;Destroy(image);rt.Release();Destroy(rt);
                }
                Destroy(actor);yield return null;
            }
            Debug.Log("HUMAN_FACTION_REVIEW_CAPTURED " + output);Application.Quit();
        }
    }
}

