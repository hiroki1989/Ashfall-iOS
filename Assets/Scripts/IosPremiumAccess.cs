using System.Runtime.InteropServices;
using UnityEngine;

// StoreKit delivers transactions independently of the lifetime of a purchase screen.
public sealed class IosPremiumAccess : MonoBehaviour
{
    public const string ProductId = "com.hiroki1989.ashfall.premium.unlock";
    static IosPremiumAccess instance;
    static string status = "";
    static string price = "";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void AshPremiumStart(string receiver);
    [DllImport("__Internal")] static extern int AshPremiumIsUnlocked();
    [DllImport("__Internal")] static extern void AshPremiumPurchase();
    [DllImport("__Internal")] static extern void AshPremiumRestore();
#endif

    public static bool IsUnlocked {
        get {
            EnsureStarted();
#if UNITY_IOS && !UNITY_EDITOR
            return AshPremiumIsUnlocked() != 0;
#else
            return false;
#endif
        }
    }
    public static string StatusMessage { get { return status; } }
    public static string PriceText { get { EnsureStarted(); return price; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureStarted() {
        if (instance != null) return;
        var go = new GameObject("IosPremiumAccess");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<IosPremiumAccess>();
#if UNITY_IOS && !UNITY_EDITOR
        AshPremiumStart(go.name);
#endif
    }
    public static void Purchase() {
        EnsureStarted();
#if UNITY_IOS && !UNITY_EDITOR
        status = "Connecting to the App Store / App Storeに接続中";
        AshPremiumPurchase();
#else
        status = "Available on iOS / iOSで利用できます";
#endif
    }
    public static void Restore() {
        EnsureStarted();
#if UNITY_IOS && !UNITY_EDITOR
        status = "Restoring purchases / 購入を復元中";
        AshPremiumRestore();
#else
        status = "Available on iOS / iOSで利用できます";
#endif
    }
    // Called on the Unity main thread by the native StoreKit observer.
    public void OnPremiumStatus(string message) { status = message; }
    public void OnPremiumPrice(string value) { price = value ?? ""; }
}
