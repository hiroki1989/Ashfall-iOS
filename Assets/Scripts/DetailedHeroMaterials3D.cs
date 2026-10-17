using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    /// <summary>Shared, image-free survivor palette. Renderers must use sharedMaterial.</summary>
    public static class DetailedHeroMaterials3D
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public const string ShaderName = "NeonVault/DetailedHeroInk3D";

        public static Material Get(string key)
        {
            key = key ?? "Dark";
            if (cache.TryGetValue(key, out var existing) && existing) return existing;
            Color color; float kind = 0, gloss = .025f, metal = 0;
            switch (key)
            {
                case "Skin": color = new Color(.66f,.43f,.28f); kind=5; break;
                case "Hair": color = new Color(.105f,.075f,.052f); kind=6; gloss=.38f; break;
                case "HairHighlight": color = new Color(.145f,.103f,.071f); kind=6; gloss=.38f; break;
                case "Cloth": color = new Color(.265f,.265f,.175f); kind=1; break;
                case "ClothDark": color = new Color(.285f,.23f,.155f); kind=1; break;
                case "Leather": color = new Color(.29f,.18f,.105f); kind=2; break;
                case "LeatherDark": color = new Color(.15f,.105f,.07f); kind=2; break;
                case "Stitch": color = new Color(.36f,.30f,.19f); kind=2; break;
                case "Steel": color = new Color(.29f,.32f,.31f); kind=3; gloss=.48f; metal=.7f; break;
                case "Rust": color = new Color(.255f,.105f,.043f); kind=3; gloss=.04f; break;
                case "Brass": color = new Color(.27f,.22f,.125f); kind=3; gloss=.32f; metal=.7f; break;
                case "TealGlass": color = new Color(.035f,.22f,.235f); kind=7; gloss=.95f; break;
                case "AmberGlass": color = new Color(.42f,.20f,.03f); kind=7; gloss=.9f; break;
                case "Wood": color = new Color(.265f,.14f,.065f); kind=4; gloss=.12f; break;
                default: color = new Color(.065f,.068f,.06f); break;
            }
            var shader = Shader.Find(ShaderName);
            if (!shader || !shader.isSupported) shader = Shader.Find("Standard");
            if (!shader) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (!shader) shader = Shader.Find("Hidden/InternalErrorShader");
            var mat = new Material(shader) { name = "Detailed survivor / " + key, hideFlags = HideFlags.DontSave };
            mat.color = color;
            if(mat.HasProperty("_SurfaceKind")) mat.SetFloat("_SurfaceKind",kind);
            if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",gloss);
            if(mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metal);
            var atlas=Resources.Load<Texture2D>("Art/SurvivorMaterials/SurvivorSurfaceAtlas-v1");
            if(atlas&&kind>=1&&kind<=4&&mat.HasProperty("_MaterialAtlas"))
            {
                mat.SetTexture("_MaterialAtlas",atlas);
                mat.SetFloat("_AtlasStrength",kind==3?.85f:.68f);
            }
            if((key=="Hair"||key=="HairHighlight")&&mat.HasProperty("_Outline"))mat.SetFloat("_Outline",.00012f);
            cache[key]=mat;
            return mat;
        }

        // Domain reload disabled and repeated play sessions must not retain generated materials.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            foreach (var material in cache.Values)
                if (material) { if(Application.isPlaying) Object.Destroy(material); else Object.DestroyImmediate(material); }
            cache.Clear();
        }
    }
}


