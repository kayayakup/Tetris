using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class GoogleAdMobController : MonoBehaviour
{
    public static GoogleAdMobController Instance;

    // Banner & Interstitial
    private BannerView bannerView;
    private InterstitialAd interstitial;

    // Reklamın yüklenip yüklenmediğini takip ettiğimiz değişken
    private bool isBannerLoaded = false;

    // DİKKAT: Geliştirme aşamasında kendi gerçek ID'lerin yerine Google Test ID'lerini kullanmalısın!
    private string bannerID = "";
    private string interstitialID = "";

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
        // 1. ÇÖZÜM: AdMob'un başlatılması zaman alır. Yükleme komutlarını 
        // SDK'nın "ben hazırlandım" dediği bu callback bloğunun içine alıyoruz.
        MobileAds.Initialize(initStatus => 
        {
            LoadBanner();
            LoadInterstitial();
        });
    }

    // -----------------------------------------------------------
    // BANNER
    // -----------------------------------------------------------
    public void LoadBanner()
    {
        // Destroy old banner
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(bannerID, AdSize.Banner, AdPosition.Bottom);

        // Banner başarıyla yüklendiğinde tetiklenir
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner başarıyla yüklendi.");
            isBannerLoaded = true;
        };

        // Banner yüklenemediğinde tetiklenir
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner yüklenemedi: " + error.GetMessage());
            isBannerLoaded = false;

            // 2. ÇÖZÜM: Eğer reklam gelmezse sistemi kendi haline bırakmıyoruz.
            // 5 saniye sonra CheckAndShowBanner metodunu çağırarak tekrar denemesini sağlıyoruz.
            Invoke(nameof(CheckAndShowBanner), 5f);
        };

        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    // İstediğin kontrol metodu: Gelmiş mi kontrol et, gelmediyse yükle/göster
    public void CheckAndShowBanner()
    {
        if (bannerView == null || !isBannerLoaded)
        {
            Debug.Log("Banner henüz gelmemiş veya null. Yeniden yükleniyor...");
            LoadBanner(); // Yükleme isteği atar, yüklendiğinde otomatik görünür.
        }
        else
        {
            Debug.Log("Banner zaten yüklü, gösteriliyor.");
            bannerView.Show(); 
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
            isBannerLoaded = false; // Yıkıldığında durumu sıfırla
        }
    }

    // -----------------------------------------------------------
    // INTERSTITIAL
    // -----------------------------------------------------------
    public void LoadInterstitial()
    {
        // Destroy old ad
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
                    Debug.Log("Interstitial failed to load: " + error.GetMessage());
                    
                    // Interstitial için de garantiye almak istersen 10 saniye sonra tekrar deneyebilirsin:
                    Invoke(nameof(LoadInterstitial), 10f);
                    return;
                }

                interstitial = ad;
            });
    }

    public void ShowInterstitialAd()
    {
        if (interstitial != null && interstitial.CanShowAd())
        {
            interstitial.Show();
            interstitial = null; // must reload after showing
            LoadInterstitial();
        }
    }
}