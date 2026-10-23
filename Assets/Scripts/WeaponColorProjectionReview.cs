using System;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Finite, off-screen art check. It never edits a scene or the player's save.
    public sealed class WeaponColorProjectionReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--weapon-color-review")>=0)
                new GameObject("Weapon color projection review").AddComponent<WeaponColorProjectionReview>();
        }

        void Start()
        {
            string output="D:/Unity/AshfallArtValidation/weapon-color-targeted";
            var args=Environment.GetCommandLineArgs();
            int arg=Array.IndexOf(args,"--qa-output");
            if(arg>=0&&arg+1<args.Length)output=args[arg+1];
            Directory.CreateDirectory(output);
            int failures=0,rendered=0;
            var stage=new GameObject("Weapon color stage").transform;
            stage.position=new Vector3(1000,1000,1000);
            var camera=new GameObject("Weapon color camera").AddComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.12f,.14f,.16f);camera.cullingMask=1<<30;
            camera.nearClipPlane=.01f;camera.farClipPlane=30;
            var light=new GameObject("Weapon color light").AddComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.15f;
            light.transform.rotation=Quaternion.Euler(35,-45,0);light.cullingMask=1<<30;
            foreach(var recipe in DistinctEquipmentDesigns.Guns)
            {
                bool chosen=!recipe.legendary&&recipe.seriesVariant==0&&
                    ((recipe.maker==0&&recipe.gunClass==0&&recipe.series==2)||
                     (recipe.maker==1&&recipe.gunClass==1&&recipe.series==1)||
                     (recipe.maker==5&&recipe.gunClass==5&&recipe.series==3));
                if(!chosen)continue;
                var gun=Arsenal.FromRecipe(recipe,Rarity.Rare);
                var root=ManufacturerEquipment3D.BuildWeapon(stage,gun).root;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                bool projected=false;
                var property=new MaterialPropertyBlock();
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.GetPropertyBlock(property);
                    projected|=property.GetFloat("_ProjectionMode")>2.5f&&property.GetTexture("_ProjectionSheet")!=null;
                }
                if(!projected)failures++;
                var bounds=new Bounds(root.position,Vector3.zero);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
                camera.transform.position=bounds.center+new Vector3(3,.42f,.1f);
                camera.transform.LookAt(bounds.center);
                camera.aspect=2.4f;
                camera.orthographicSize=Mathf.Max(.10f,Mathf.Max(bounds.extents.y*1.3f,bounds.extents.z/2.4f*1.3f));
                var target=new RenderTexture(1200,500,24);
                camera.targetTexture=target;camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=target;
                var picture=new Texture2D(1200,500,TextureFormat.RGB24,false);
                picture.ReadPixels(new Rect(0,0,1200,500),0,0);picture.Apply();
                string file=recipe.id.Replace('.','-')+".png";
                File.WriteAllBytes(Path.Combine(output,file),picture.EncodeToPNG());
                Debug.Log("WEAPON_COLOR_REVIEW "+recipe.id+" fourView="+projected+" image="+file);
                camera.targetTexture=null;RenderTexture.active=previous;target.Release();
                // Destroy is deferred until the frame ends; hide this sample now so
                // the next off-screen render contains exactly one weapon.
                root.gameObject.SetActive(false);
                Destroy(target);Destroy(picture);Destroy(root.gameObject);rendered++;
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"Rendered="+rendered+" Failures="+failures);
            Application.Quit(rendered==3&&failures==0?0:1);
        }
    }
}
