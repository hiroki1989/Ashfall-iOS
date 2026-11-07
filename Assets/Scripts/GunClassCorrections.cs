using UnityEngine;

namespace NeonVault
{
    // Keep the original illustration/mesh recipe while correcting gameplay class.
    public static class GunClassCorrections
    {
        public static bool IsStrongholdBastion(Gun gun)=>gun!=null&&gun.catalogId=="distinct.gun.473";

        public static void Migrate(Gun gun)
        {
            if(!IsStrongholdBastion(gun)||gun.classCorrectionVersion>=1)return;
            // Convert the old helical-feed automatic's base tuning to the balanced
            // assault-rifle profile, preserving existing level and random rolls.
            gun.damage*=2.02f;
            gun.interval*=1.74f;
            gun.magazine=Mathf.Max(1,Mathf.RoundToInt(gun.magazine*.505f));
            gun.reloadSeconds*=.75f;
            gun.speed*=28f/21f;
            gun.accuracy=Mathf.Clamp(gun.accuracy+15f,1f,100f);
            gun.modelVariant=12;
            // Regenerate only the new range stat for the corrected weapon class.
            gun.rangeRetention=0f;
            gun.classCorrectionVersion=1;
        }
    }
}
