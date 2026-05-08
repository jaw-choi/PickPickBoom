using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using GoogleMobileAds.Api;
#endif

public sealed class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [SerializeField] private string androidAdUnitId = "ca-app-pub-8502618733998421/9276800383";
    [SerializeField] private string iosAdUnitId = "ca-app-pub-3940256099942544/4411468910";

#if UNITY_ANDROID || UNITY_IOS
    private InterstitialAd interstitialAd;
#endif

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        InitializeAds();
    }

    private void InitializeAds()
    {
#if UNITY_ANDROID || UNITY_IOS
        MobileAds.Initialize(_ => LoadInterstitialAd());
#endif
    }

    public void LoadInterstitialAd()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        string adUnitId;
#if UNITY_ANDROID
        adUnitId = androidAdUnitId;
#else
        adUnitId = iosAdUnitId;
#endif

        InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogWarning($"Interstitial ad failed to load: {error}");
                return;
            }
            interstitialAd = ad;
            interstitialAd.OnAdFullScreenContentClosed += LoadInterstitialAd;
        });
#endif
    }

    public void ShowInterstitialAd()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
        }
#endif
    }
}
