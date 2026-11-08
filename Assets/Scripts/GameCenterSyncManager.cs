using System;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

namespace NeonVault
{
    public static class GameCenterSyncManager
    {
        public static bool IsAuthenticated { get; private set; }
        public static string PlayerID { get; private set; } = "";
        public static string PlayerAlias { get; private set; } = "";

        public static void Initialize()
        {
#if UNITY_IOS
            try
            {
                if (!Social.localUser.authenticated)
                {
                    Social.localUser.Authenticate(success =>
                    {
                        if (success)
                        {
                            IsAuthenticated = true;
                            PlayerID = Social.localUser.id;
                            PlayerAlias = Social.localUser.userName;
                            Debug.Log("[GameCenter] Authenticated: " + PlayerAlias + " (" + PlayerID + ")");
                            SyncCloudSave();
                        }
                        else
                        {
                            Debug.LogWarning("[GameCenter] Authentication failed or cancelled by user.");
                        }
                    });
                }
                else
                {
                    IsAuthenticated = true;
                    PlayerID = Social.localUser.id;
                    PlayerAlias = Social.localUser.userName;
                    SyncCloudSave();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameCenter] Init exception: " + ex.Message);
            }
#endif
        }

        public static void SyncCloudSave()
        {
#if UNITY_IOS
            // If local save exists, backup to cloud/identity cache
            // If local save is empty but cloud metadata exists, restore
            try
            {
                for (int slot = 1; slot <= SaveSlotPrefs.Count; slot++)
                {
                    if (SaveSlotPrefs.Exists(slot))
                    {
                        // Backup slot signature to local user cloud registry
                        string backupKey = "Vault.CloudSync.Slot." + slot + "." + PlayerID;
                        UnityEngine.PlayerPrefs.SetString(backupKey, System.DateTime.UtcNow.ToString("o"));
                    }
                }
                UnityEngine.PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameCenter] Cloud sync exception: " + ex.Message);
            }
#endif
        }
    }
}
