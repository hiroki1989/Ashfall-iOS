using UnityEngine;

namespace NeonVault
{
    public static class GameCenterSyncManager
    {
        // This iOS build does not use Game Center. Keep the call surface so
        // existing startup code remains compatible without requesting its
        // capability or provisioning-profile entitlement.
        public static bool IsAuthenticated => false;
        public static string PlayerID => "";
        public static string PlayerAlias => "";

        public static void Initialize() { }

        public static void SyncCloudSave() { }
    }
}
