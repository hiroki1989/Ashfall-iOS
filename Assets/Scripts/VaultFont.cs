using UnityEngine;

namespace NeonVault
{
    // Set to false to restore the previous font choices throughout the game.
    internal static class VaultFont
    {
        internal const bool UseAprilGothic = true;
        internal static Font Preferred() => Resources.Load<Font>("Fonts/GenShinGothic-P-Regular") ?? Resources.Load<Font>("Fonts/NotoSansJP");
    }
}
