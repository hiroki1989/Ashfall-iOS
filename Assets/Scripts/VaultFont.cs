using UnityEngine;

namespace NeonVault
{
    // Shared font assets for UI and combat text.
    internal static class VaultFont
    {
        internal static Font Preferred() => Resources.Load<Font>("Fonts/JNRfont_n");
        internal static Font Legacy() => Resources.Load<Font>("Fonts/GenShinGothic-P-Regular");
        internal static Font Damage() => Resources.Load<Font>("Fonts/AprilGothicOne-R");
    }
}
