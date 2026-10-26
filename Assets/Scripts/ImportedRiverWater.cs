using System.Collections.Generic;
using UnityEngine;

namespace NeonVault
{
    /// <summary>Lightweight built-in river surface; no WaterWorks SSR/refraction or URP features.</summary>
    public static class ImportedRiverWater
    {
        public static Material Create(List<Material> owned)
        {
            var shader=Resources.Load<Shader>("ImportedRiver")??Shader.Find("NeonVault/ImportedRiver");
            if(shader==null) shader=Shader.Find("Unlit/Color");
            var water=new Material(shader){name="Imported river / built-in flow",color=new Color(.12f,.24f,.25f,.82f)};
            // The currently supplied WaterWorks package has procedural wave subgraphs,
            // but no wave-normal texture. This optional path supports a curated normal
            // if one is supplied later, without loading Editor assets at runtime.
            var normal=Resources.Load<Texture2D>("Art/ImportedEnvironment/WaterWorksWaveNormal");
            if(water.HasProperty("_HasNormal"))
            {
                water.SetFloat("_HasNormal",normal!=null?1:0);
                if(normal!=null)water.SetTexture("_BumpMap",normal);
                water.SetFloat("_WorldScale",.14f);
                water.SetVector("_Flow",new Vector4(.018f,-.06f,0,0));
                // Matches OverworldGeography.RiverX and its 30m half-width.
                water.SetVector("_RiverShape",new Vector4(325,180,65,30));
                water.SetVector("_RiverFrequency",new Vector4(.0008f,.0021f,0,0));
            }
            owned?.Add(water);
            return water;
        }
    }
}
