using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonVault
{
    // Side art is packed per gun family into four non-overlapping projection views.
    // Read only the equipped family's atlas, keeping iOS memory independent of the
    // number of catalog products.
    internal static class WeaponFourViewAtlas
    {
        [Serializable] sealed class Product { public string catalogId, atlas; }
        [Serializable] sealed class Catalog { public Product[] products; }
        [Serializable] sealed class View { public int[] visibleRect; }
        [Serializable] sealed class Views { public View left; }
        [Serializable] sealed class AtlasInfo { public int tilePixels; public Views views; }
        sealed class Cached { public Texture2D texture; public int references; public Vector4 sideUv; }
        static Dictionary<string,string> paths;
        static readonly Dictionary<string,Cached> loaded=new Dictionary<string,Cached>();

        static void ReadCatalog()
        {
            if(paths!=null)return;
            paths=new Dictionary<string,string>();
            string path=Path.Combine(Application.streamingAssetsPath,"WeaponFourView","product-catalog.json");
            if(!File.Exists(path))return;
            try
            {
                var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
                if(catalog?.products==null)return;
                foreach(var item in catalog.products)
                    if(item!=null&&!string.IsNullOrEmpty(item.catalogId)&&!string.IsNullOrEmpty(item.atlas))
                        paths[item.catalogId]=item.atlas;
            }
            catch(Exception error){Debug.LogError("Weapon atlas manifest: "+error.Message);}
        }

        static Vector4 SideUv(string imagePath)
        {
            // The painting's occupied rectangle differs greatly between a long rifle
            // and a compact handgun. A single fixed vertical strip samples filler
            // colors or clips artwork, even when the right atlas is loaded.
            var fallback=new Vector4(.02f,.35f,.96f,.30f);
            string infoPath=Path.ChangeExtension(imagePath,".json");
            if(!File.Exists(infoPath))return fallback;
            try
            {
                var info=JsonUtility.FromJson<AtlasInfo>(File.ReadAllText(infoPath));
                var rect=info?.views?.left?.visibleRect;
                if(info==null||info.tilePixels<=0||rect==null||rect.Length!=4||rect[2]<=0||rect[3]<=0)return fallback;
                float size=info.tilePixels;
                const float inset=1.5f;
                return new Vector4((rect[0]+inset)/size,(size-rect[1]-rect[3]+inset)/size,
                    Mathf.Max(1,rect[2]-2*inset)/size,Mathf.Max(1,rect[3]-2*inset)/size);
            }
            catch(Exception error){Debug.LogWarning("Weapon atlas bounds: "+infoPath+" / "+error.Message);return fallback;}
        }

        public static Texture2D Retain(string id,out string atlasKey,out Vector4 sideUv)
        {
            atlasKey=null;sideUv=new Vector4(.02f,.35f,.96f,.30f);ReadCatalog();
            if(string.IsNullOrEmpty(id)||!paths.TryGetValue(id,out atlasKey))return null;
            if(loaded.TryGetValue(atlasKey,out var cached)){cached.references++;sideUv=cached.sideUv;return cached.texture;}
            try
            {
                string path=Path.Combine(Application.streamingAssetsPath,"WeaponFourView",atlasKey.Replace('/',Path.DirectorySeparatorChar));
                if(!File.Exists(path))return null;
                var texture=new Texture2D(2,2,TextureFormat.RGB24,true,false){name="Weapon four-view / "+atlasKey};
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),true)){UnityEngine.Object.Destroy(texture);return null;}
                texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=4;
                sideUv=SideUv(path);
                loaded.Add(atlasKey,new Cached{texture=texture,references=1,sideUv=sideUv});
                return texture;
            }
            catch(Exception error){Debug.LogError("Weapon atlas load: "+atlasKey+" / "+error.Message);return null;}
        }

        public static string ModelKey(string id)
        {
            ReadCatalog();
            return !string.IsNullOrEmpty(id)&&paths.TryGetValue(id,out var atlas)
                ?Path.GetFileNameWithoutExtension(atlas):null;
        }

        public static void Release(string atlasKey)
        {
            if(string.IsNullOrEmpty(atlasKey)||!loaded.TryGetValue(atlasKey,out var cached))return;
            if(--cached.references>0)return;
            loaded.Remove(atlasKey);
            if(cached.texture)UnityEngine.Object.Destroy(cached.texture);
        }
    }
}
