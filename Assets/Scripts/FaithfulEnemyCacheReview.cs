using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace NeonVault
{
    // Standalone opt-in geometry/lifetime review. No game, inventory or PlayerPrefs access.
    public sealed class FaithfulEnemyCacheReview:MonoBehaviour
    {
        const string Flag="--enemy-cache-review";
        readonly List<string> log=new List<string>();int checks,failures;string output;
        readonly List<FaithfulEnemy3D> survivors=new List<FaithfulEnemy3D>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0)new GameObject("Enemy cache review").AddComponent<FaithfulEnemyCacheReview>();}
        void Check(bool value,string name){checks++;if(!value)failures++;log.Add((value?"PASS ":"FAIL ")+name);}
        static Transform Joint(FaithfulEnemy3D model,string name)=>(Transform)typeof(FaithfulEnemy3D).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(model);
        static bool Finite(Vector3 value)=>!float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&!float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.z);
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--qa-output");output=at>=0&&at+1<args.Length?args[at+1]:"D:/Unity/QA-enemy-cache";Directory.CreateDirectory(output);Time.timeScale=1;
            var review=Run();while(true){bool next;try{next=review.MoveNext();}catch(Exception e){Check(false,e.ToString());break;}if(!next)break;yield return review.Current;}
            log.Add("Checks="+checks+" Failures="+failures);File.WriteAllLines(output+"/enemy-cache-review-result.txt",log);Debug.Log("ASHFALL_ENEMY_CACHE_REVIEW Checks="+checks+" Failures="+failures);Application.Quit(failures==0?0:1);
        }
        IEnumerator Run()
        {
            Check(FindAnyObjectByType<VaultGame>()==null,"standalone review has no game or preference startup");
            var cam=new GameObject("Review camera").AddComponent<Camera>();cam.backgroundColor=new Color(.13f,.15f,.18f);cam.clearFlags=CameraClearFlags.SolidColor;cam.orthographic=true;cam.orthographicSize=7.8f;cam.transform.position=new Vector3(0,9,-23);cam.transform.LookAt(new Vector3(0,2,0));
            RenderSettings.ambientLight=new Color(.6f,.6f,.6f);var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(35,-30,0);
            for(int kind=0;kind<10;kind++)
            {
                var parent=new GameObject("Kind "+kind).transform;var position=new Vector3((kind%5-2)*3.8f,.65f,(kind/5)*5.5f);
                var clock=System.Diagnostics.Stopwatch.StartNew();var first=FaithfulEnemy3D.Create(parent,position,kind).GetComponent<FaithfulEnemy3D>();clock.Stop();double firstMs=clock.Elapsed.TotalMilliseconds;
                clock.Restart();var second=FaithfulEnemy3D.Create(parent,position,kind).GetComponent<FaithfulEnemy3D>();clock.Stop();double secondMs=clock.Elapsed.TotalMilliseconds;log.Add("TIMING kind="+kind+" firstMs="+firstMs.ToString("F2")+" cachedMs="+secondMs.ToString("F2"));
                var a=first.GetComponentsInChildren<MeshFilter>();var b=second.GetComponentsInChildren<MeshFilter>();Check(a.Length>0&&a.Length==b.Length,"kind "+kind+" same part count");
                for(int i=0;i<Math.Min(a.Length,b.Length);i++){Check(a[i].sharedMesh==b[i].sharedMesh&&a[i].sharedMesh.vertexCount==b[i].sharedMesh.vertexCount,"kind "+kind+" shared immutable mesh "+i);var ar=a[i].GetComponent<MeshRenderer>();var br=b[i].GetComponent<MeshRenderer>();Check(ar.sharedMaterials.Length==br.sharedMaterials.Length&&ar.enabled==br.enabled,"renderer state");for(int m=0;m<ar.sharedMaterials.Length;m++)Check(ar.sharedMaterials[m]==br.sharedMaterials[m],"shared material");}
                foreach(string name in new[]{"rig","pelvis","chest","head","muzzle"})Check(Joint(first,name)!=Joint(second,name)&&Joint(second,name).IsChildOf(second.transform),"kind "+kind+" independent "+name);
                UnityEngine.Object.Destroy(first.gameObject);yield return null;Check(second.Ready&&b[0].sharedMesh&&b[0].sharedMesh.vertexCount>0,"kind "+kind+" survives first destruction");
                second.transform.position+=Vector3.right*.1f;second.NotifyAttack();second.NotifyHit(second.transform.position+Vector3.right);yield return null;Check(Finite(Joint(second,"rig").localPosition)&&Finite(Joint(second,"head").eulerAngles),"kind "+kind+" movement attack hit finite");survivors.Add(second);
            }
            yield return null;yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();Check(image&&image.width>0&&image.height>0,"actual ten-kind framebuffer");if(image){File.WriteAllBytes(output+"/enemy-cache-sheet.png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
            foreach(var model in survivors)model.BeginDeath();for(int frame=0;frame<75;frame++)yield return null;
            foreach(var model in survivors)Check(Finite(Joint(model,"rig").localPosition)&&Finite(Joint(model,"rig").eulerAngles),"kind "+model.Kind+" death animation finite");
        }
    }
}
