using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

// iOS production rewarded ad unit for this app.
public sealed class VaultRewardedAds : MonoBehaviour
{
    private const string IosProductionRewardedUnit = "ca-app-pub-1886549062462061/1309428724"; // Set to this app's AdMob rewarded unit ID.
    private static VaultRewardedAds instance;
    private RewardedAd ad;
    private Action<bool> completion;
    private bool initialized, initializing, loading, showing, consentUpdating;
    private float retryAt;

    public static bool IsReady
    {
        get
        {
            if (instance == null) return false;
            instance.TryLoad();
            return instance.ad != null && instance.ad.CanShowAd() && !instance.showing;
        }
    }

    public static void Show(Action<bool> onFinished)
    {
        if (!IsReady) { onFinished?.Invoke(false); return; }
        instance.completion = onFinished;
        instance.showing = true;
        try { instance.ad.Show(reward => instance.Finish(true)); }
        catch (Exception e) { Debug.LogWarning("Rewarded ad: " + e.Message); instance.Close(); }
    }

    // Call from a visible privacy-options entry point when required by UMP.
    public static bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
    public static void ShowPrivacyOptions(Action onClosed = null)
    {
        ConsentForm.ShowPrivacyOptionsForm(error => { if (error != null) Debug.LogWarning("Privacy options: " + error.Message); onClosed?.Invoke(); });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (instance != null) return;
        var obj = new GameObject("VaultRewardedAds");
        DontDestroyOnLoad(obj);
        instance = obj.AddComponent<VaultRewardedAds>();
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        instance.RequestConsent();
#endif
    }

    private void RequestConsent()
    {
        if (consentUpdating || initialized || Time.realtimeSinceStartup < retryAt) return;
        consentUpdating = true;
        ConsentInformation.Update(new ConsentRequestParameters(), error =>
        {
            if (error != null) { consentUpdating = false; retryAt = Time.realtimeSinceStartup + 30f; StartAdsIfAllowed(); return; }
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                consentUpdating = false;
                if (formError != null) { Debug.LogWarning("Consent form: " + formError.Message); retryAt = Time.realtimeSinceStartup + 30f; }
                StartAdsIfAllowed();
            });
        });
    }

    private void StartAdsIfAllowed()
    {
        if (!ConsentInformation.CanRequestAds() || initialized || initializing) return;
        initializing = true;
        MobileAds.Initialize(status => { initializing = false; initialized = true; TryLoad(); });
    }

    private void TryLoad()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!initialized) { RequestConsent(); return; }
        if (loading || showing || ad != null || Time.realtimeSinceStartup < retryAt) return;
        loading = true;
        if (string.IsNullOrEmpty(IosProductionRewardedUnit)) { loading = false; retryAt = Time.realtimeSinceStartup + 60f; Debug.LogWarning("AdMob production rewarded unit ID is not configured."); return; }
        RewardedAd.Load(IosProductionRewardedUnit, new AdRequest(), (loaded, error) =>
        {
            loading = false;
            if (error != null || loaded == null) { retryAt = Time.realtimeSinceStartup + 30f; Debug.LogWarning("Rewarded ad load failed: " + error); return; }
            ad = loaded;
            ad.OnAdFullScreenContentClosed += Close;
            ad.OnAdFullScreenContentFailed += error => { Debug.LogWarning("Rewarded ad failed: " + error); Close(); };
        });
#endif
    }

    private void Finish(bool earned)
    {
        var callback = completion;
        completion = null;
        callback?.Invoke(earned);
    }

    private void Close()
    {
        showing = false;
        Finish(false);
        if (ad != null) { ad.Destroy(); ad = null; }
        TryLoad();
    }
}
