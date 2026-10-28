using System.Collections.Generic;
using UnityEngine;
namespace NeonVault
{
    // Inventory renders the exact equipped geometry rather than a recolored legacy drawing.
    public static class ManufacturerPortraits
    {
        static readonly Dictionary<string,RenderTexture> cache=new Dictionary<string,RenderTexture>();
        static readonly Queue<string> order=new Queue<string>();
        sealed class Request{public string key;public Gun gun;public ArmorItem armor;public MeleeWeaponItem melee;}
        static readonly Queue<Request> pending=new Queue<Request>();
        static readonly HashSet<string> queued=new HashSet<string>();
        static PortraitPump pump;static DetailedHero3D armorBody;static Renderer[] originalBody;
        sealed class PortraitPump:MonoBehaviour
        {
            void LateUpdate(){if(pending.Count==0)return;var job=pending.Dequeue();queued.Remove(job.key);Render(job.key,job.gun,job.armor,job.melee);}
        }
        static Texture RequestPortrait(string key,Gun gun,ArmorItem armor,MeleeWeaponItem melee=null)
        {
            if(cache.TryGetValue(key,out var ready))return ready;
            if(!pump)pump=new GameObject("One-per-frame equipment portrait renderer").AddComponent<PortraitPump>();
            if(queued.Add(key))pending.Enqueue(new Request{key=key,gun=gun,armor=armor,melee=melee});
            return Texture2D.blackTexture;
        }
        static Texture Render(string key,Gun gun,ArmorItem armor,MeleeWeaponItem melee=null)
        {
            if(cache.TryGetValue(key,out var texture))return texture;
            var stage=new GameObject("Catalog portrait isolated studio");stage.transform.position=new Vector3(1500,-1500,1500);
            Transform target;
            if(gun!=null)target=ManufacturerEquipment3D.BuildWeapon(stage.transform,gun).root;
            else if(melee!=null)target=ManufacturerEquipment3D.BuildMelee(stage.transform,melee);
            else
            {
                if(!armorBody){armorBody=DetailedHero3D.Create(stage.transform,DetailedHeroMaterials3D.Get);originalBody=armorBody.GetComponentsInChildren<Renderer>();}
                var hero=armorBody;hero.gameObject.SetActive(true);hero.transform.SetParent(stage.transform,false);
                foreach(var renderer in originalBody)renderer.enabled=false;
                var pieces=new ArmorItem[5];pieces[(int)armor.slot]=armor;
                DetailedHeroArsenal3D.ApplyArmor(hero,pieces);ManufacturerEquipment3D.ApplyArmor(hero,pieces);
                foreach(var renderer in originalBody)renderer.enabled=false;
                target=hero.transform;
            }
            SetLayer(stage.transform,30);
            Bounds bounds=new Bounds();bool found=false;
            foreach(var renderer in target.GetComponentsInChildren<Renderer>())if(renderer.enabled&&renderer.gameObject.activeInHierarchy){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
            var cameraObj=new GameObject("Catalog portrait camera");cameraObj.transform.SetParent(stage.transform,false);var camera=cameraObj.AddComponent<Camera>();
            camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.065f,.075f,1);camera.orthographic=true;
            Vector3 direction=gun!=null?new Vector3(1,.42f,.28f):new Vector3(.7f,.3f,1);
            camera.transform.position=bounds.center+direction.normalized*5;camera.transform.LookAt(bounds.center);
            camera.orthographicSize=Mathf.Max(.12f,Mathf.Max(bounds.size.y,bounds.size.magnitude)*.60f);camera.nearClipPlane=.01f;camera.farClipPlane=15;
            var lightObj=new GameObject("Catalog local key");lightObj.transform.SetParent(stage.transform,false);var light=lightObj.AddComponent<Light>();light.type=LightType.Point;light.range=9;light.intensity=4;light.cullingMask=1<<30;light.transform.position=bounds.center+new Vector3(1,2,2);
            texture=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32){name="Actual equipment catalog / "+key};texture.Create();
            camera.targetTexture=texture;camera.Render();camera.targetTexture=null;
            if(armor!=null){armorBody.gameObject.SetActive(false);armorBody.transform.SetParent(null,true);}
            stage.SetActive(false);Object.Destroy(stage);
            while(cache.Count>=48){string oldest=order.Dequeue();if(cache.TryGetValue(oldest,out var old)){old.Release();Object.Destroy(old);cache.Remove(oldest);}}
            cache[key]=texture;order.Enqueue(key);return texture;
        }
        static void SetLayer(Transform t,int layer){t.gameObject.layer=layer;foreach(Transform child in t)SetLayer(child,layer);}
        public static void Gun(Rect rect,Gun item){GUI.color=Color.white;GUI.DrawTexture(rect,RequestPortrait("gun/"+ManufacturerEquipment3D.WeaponKey(item),item,null),ScaleMode.ScaleToFit);}
        public static void Armor(Rect rect,ArmorItem item){GUI.color=Color.white;GUI.DrawTexture(rect,RequestPortrait("armor/"+item.visualVariant+"/"+item.slot+"/"+ManufacturerBranding.Maker(item)+"/"+item.designVariant,null,item),ScaleMode.ScaleToFit);}
        public static void Melee(Rect rect,MeleeWeaponItem item){GUI.color=Color.white;var atlas=Resources.Load<Texture2D>("Art/MeleeWeaponIcons-portrait-v2")??Resources.Load<Texture2D>("Art/MeleeWeaponIcons");if(atlas){float width=.2f;GUI.DrawTextureWithTexCoords(rect,atlas,new Rect((int)item.family*width,0,width,1),true);}ManufacturerBranding.PortraitBadge(rect,ManufacturerBranding.Maker(item));}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){foreach(var t in cache.Values){t.Release();Object.Destroy(t);}cache.Clear();order.Clear();pending.Clear();queued.Clear();if(armorBody)Object.Destroy(armorBody.gameObject);if(pump)Object.Destroy(pump.gameObject);armorBody=null;pump=null;originalBody=null;}
    }
}
