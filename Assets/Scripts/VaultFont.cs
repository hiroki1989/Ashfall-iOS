using UnityEngine;

namespace NeonVault
{
    // Shared font assets for UI and combat text.
    internal static class VaultFont
    {
        internal static Font Preferred() => Resources.Load<Font>("Fonts/Corporate-Logo-Medium-ver3");
        internal static Font Legacy() => Resources.Load<Font>("Fonts/Corporate-Logo-Medium-ver3");
        internal static Font Damage() => Resources.Load<Font>("Fonts/Corporate-Logo-Medium-ver3");
    }
}
