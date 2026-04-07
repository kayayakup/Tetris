using UnityEngine;
using GoogleMobileAds.Api;
using System.Collections;

public class GoogleAdMobController : MonoBehaviour
{
    public static GoogleAdMobController Instance;

    private BannerView bannerView;
    private InterstitialAd interstitial;

    [Header("Ad Unit IDs")]
    [SerializeField] private string bannerID = "ca-app-pub-6153418042545894/5561270909";
    [SerializeField] private string interstitialID = "ca-app-pub-6153418042545894/2898712457";

    private bool isBannerLoaded = false;
    private bool isInitializing = false;

    private int bannerRetryCount = 0;
    private const int maxRetry = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeAds();
    }

    public void InitializeAds()
    {
        if (isInitializing) return;

        isInitializing = true;

        MobileAds.Initialize(initStatus =>
        {
            isInitializing = false;

            LoadBanner();
            LoadInterstitial();
        });
    }

    // -----------------------------------------------------------
    // BANNER
    // -----------------------------------------------------------
    public void LoadBanner()
    {
        DestroyBannerAd();

        isBannerLoaded = false;

        bannerView = new BannerView(bannerID, AdSize.Banner, AdPosition.Bottom);

        // EVENTLER
        bannerView.OnBannerAdLoaded += () =>
        {
            isBannerLoaded = true;
            bannerRetryCount = 0;

            Debug.Log("Banner Loaded!");
            bannerView.Show();
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            isBannerLoaded = false;

            Debug.Log("Banner Load Failed: " + error.GetMessage());

            if (bannerRetryCount < maxRetry)
            {
                bannerRetryCount++;
                float delay = Mathf.Clamp(2f * bannerRetryCount, 2f, 20f);

                Debug.Log("Retry banner in " + delay + " sec...");
                StartCoroutine(RetryBanner(delay));
            }
        };

        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    private IEnumerator RetryBanner(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadBanner();
    }

    public void EnsureBannerVisible()
    {
        if (bannerView == null)
        {
            LoadBanner();
            return;
        }

        if (!isBannerLoaded)
        {
            LoadBanner();
            return;
        }

        bannerView.Show();
    }

    public void HideBanner()
    {
        if (bannerView != null)
            bannerView.Hide();
    }

    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    // -----------------------------------------------------------
    // INTERSTITIAL
    // -----------------------------------------------------------
    public void LoadInterstitial()
    {
        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        InterstitialAd.Load(interstitialID, new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    Debug.Log("Interstitial failed: " + error.GetMessage());
                    return;
                }

                interstitial = ad;
                Debug.Log("Interstitial Loaded!");
            });
    }

    public void ShowInterstitialAd()
    {
        if (interstitial != null && interstitial.CanShowAd())
        {
            interstitial.Show();
            interstitial = null;
            LoadInterstitial();
        }
        else
        {
            Debug.Log("Interstitial not ready.");
            LoadInterstitial();
        }
    }

    // -----------------------------------------------------------
    // APP PAUSE / RESUME KONTROL
    // -----------------------------------------------------------
    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            // uygulama geri geldiðinde banner kaybolabiliyor
            Invoke(nameof(EnsureBannerVisible), 1f);
        }
    }
}