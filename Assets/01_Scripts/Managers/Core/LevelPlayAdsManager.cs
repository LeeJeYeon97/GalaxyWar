using System;
using UnityEngine;
using Unity.Services.LevelPlay;

[Serializable]
public class LevelPlayAdsManager
{
    // 아이언소스 대시보드에서 발급받은 App Key를 여기에 넣습니다.
    [Header("App key")]
    [SerializeField] private string androidAppKey;
    [SerializeField] private string iosAppKey;

    [Header("Banner Ad Unit Id")]
    [SerializeField] private string androidBannerAdUnitId;
    [SerializeField] private string iosBannerAdUnitId;

    [Header("Interstitial Ad Unit Id")]
    [SerializeField] private string androidInterstitialAdUnitId;
    [SerializeField] private string iosInterstitialAdUnitId;

    [Header("Rewarded Ad Unit Id")]
    [SerializeField] private string androidRewardedAdUnitId;
    [SerializeField] private string iosRewardedAdUnitId;

    // 마지막으로 전면/보상형 광고를 본 시간 (게임 시작 시점 기준)
    private float _lastAdTime = -999f;

    // 전면 광고 쿨타임 (예: 180초 = 3분)
    private float _adCooldown = 180f;

    private string appKey
    {
        get
        {
#if UNITY_ANDROID
            return androidAppKey;
#elif UNITY_IOS
            return iosAppKey;
#else
            return string.Empty;
#endif
        }
    }
    private string bannerAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidBannerAdUnitId;
#elif UNITY_IOS
            return iosBannerAdUnitId;
#else
            return string.Empty;
#endif
        }
    }

    private string interstitialAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidInterstitialAdUnitId;
#elif UNITY_IOS
            return iosInterstitialAdUnitId;
#else
            return string.Empty;
#endif
        }
    }

    private string rewardedAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidRewardedAdUnitId;
#elif UNITY_IOS
            return iosRewardedAdUnitId;
#else
            return string.Empty;
#endif
        }
    }

    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    public void Init()
    {
        LevelPlay.ValidateIntegration();
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        // SDK init
        LevelPlay.Init(appKey);
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError("LevelPlay Init Failed" + error);
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay Init Success");
        CreateBannerAd();
        CreateInterstitialAd();
        CreateRewardedAd();
    }

    #region 배너광고
    private void CreateBannerAd()
    {
        // 1. Config 객체를 조립하기 위한 Builder 생성
        var configBuilder = new LevelPlayBannerAd.Config.Builder();

        // 2. 사이즈 세팅: 기기 화면 비율에 맞추는 스마트 배너(Adaptive)
        configBuilder.SetSize(LevelPlayAdSize.CreateAdaptiveAdSize());

        // 3. 위치 세팅: 화면 하단 중앙
        configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);

        // 4. (선택) 로드 완료 시 즉시 띄울 것인지 설정 (기본값 true)
        //configBuilder.SetDisplayOnLoad(true);

        // 5. 조립 완료! Build()를 눌러서 최종 config 객체를 뽑아냅니다.
        var bannerConfig = configBuilder.Build();

        // 6. 말씀하신 대로 id와 config 딱 2개만 넣어서 배너 생성!
        bannerAd = new LevelPlayBannerAd(bannerAdUnitId, bannerConfig);

        bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
        bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
        bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
        bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
        bannerAd.OnAdClicked += BannerOnAdClickedEvent;
        bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
        bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
        bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;

    }
    public void ShowBanner()
    {
        bannerAd.LoadAd();
    }
    public void HideBanner()
    {
        bannerAd.HideAd();
    }
    public void DestroyBanner()
    {
        bannerAd.DestroyAd();
    }
    // Implement the events
    void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdLoadFailedEvent(LevelPlayAdError ironSourceError) { }
    void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo) 
    {
        Debug.Log("배너 광고 클릭함");
    }
    void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo) { }

    #endregion

    #region 전면광고
    private void CreateInterstitialAd()
    {

        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);

        interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

        LoadInterstitialAd();
    }
    // 배너광고와 다르게 미리 불러오고 버튼을 누르면 띄운다.
    public void LoadInterstitialAd()
    {
        interstitialAd.LoadAd();
        Debug.Log("interstitalAd Loaded");
    }
    public void ShowInterstitialAd()
    {
        // 1. 마지막으로 광고를 본 지 180초가 지났는지 확인
        if (Time.time - _lastAdTime >= _adCooldown)
        {
            if (interstitialAd.IsAdReady())
            {
                interstitialAd.ShowAd();
                // 2. 광고를 띄웠으니 쿨타임 초기화
                _lastAdTime = Time.time;
            }
        }
        else
        {
            Debug.Log($"아직 쿨타임입니다. 남은 시간: {_adCooldown - (Time.time - _lastAdTime)}초");
            // 쿨타임 중이면 광고 없이 그냥 조용히 넘어갑니다!
        }
    }
    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error) 
    { 
        LoadInterstitialAd();
    }
    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo) 
    {
        LoadInterstitialAd(); 
    }
    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
    #endregion

    #region 보상형 광고
    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += RewardedOnAdLoadedEvent;
        rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
        rewardedAd.OnAdDisplayed += RewardedOnAdDisplayedEvent;
        rewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailedEvent;
        rewardedAd.OnAdRewarded += RewardedOnAdRewardedEvent;
        rewardedAd.OnAdClosed += RewardedOnAdClosedEvent;
        // Optional 
        rewardedAd.OnAdClicked += RewardedOnAdClickedEvent;
        rewardedAd.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;

        LoadRewardedAd();
    }
    public void LoadRewardedAd()
    {
        rewardedAd.LoadAd();
    }
    public void ShowRewardedAd()
    {
        if(rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();
        }
    }
    void RewardedOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    void RewardedOnAdLoadFailedEvent(LevelPlayAdError error) 
    {
        LoadRewardedAd();
    }
    void RewardedOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    void RewardedOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    void RewardedOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward adReward) 
    {
        // 보상 처리
        string rewardName = adReward.Name;
        int rewardAmount = adReward.Amount;
        Debug.Log($"reawrdName : {rewardName}, rewardAmount : {rewardAmount}");

        _lastAdTime = Time.time;

        Managers.Game.RevivePlayer();
    }
    void RewardedOnAdClosedEvent(LevelPlayAdInfo adInfo) 
    {
        LoadRewardedAd();
    }
    void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
    #endregion
}

