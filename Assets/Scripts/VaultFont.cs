using UnityEngine;

namespace NeonVault
{
    // Main UI, headings, mobile controls and combat text use the same asset.
    internal static class VaultFont
    {
        internal static Font Preferred() => Resources.Load<Font>("Fonts/JNRfont_n");
    }
}
