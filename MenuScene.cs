using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AppsFlyerSDK;
using DefaultNamespace;
using Firebase.Analytics;
using Firebase.Extensions;
using GameRules;
using GameRules.Firebase.Runtime;
using GameRules.Modules.TutorialEngine;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.UI;
using GameRules.Scripts.UI.WheelOfFortune;
using GameRules.Scripts.WrappersECS;
using GameRules.TaskManager.Runtime;
using I2.Loc;
using Michsky.UI.ModernUIPack;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Task = System.Threading.Tasks.Task;

#if !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.Purchasing.Security;
using Firebase.Crashlytics;
#endif

#if HAVE_APPODEAL
using AppodealAds.Unity.Api;
using AppodealAds.Unity.Common;
#endif

public class MenuScene : MonoBehaviour
#if HAVE_APPODEAL
    , IRewardedVideoAdListener
#endif
{
    [SerializeField] private TextMeshProUGUI _bestScore;
    [SerializeField] private TextMeshProUGUI _levelValue;
    [SerializeField] private TextMeshProUGUI _xpValue;
    [SerializeField] private TextMeshProUGUI _crowdSizeValue;
    [SerializeField] private TMP_InputField _userName;

    [SerializeField] private Slider _levelProgress;

    [SerializeField] private Image _skinImage;

    private IList<PlayerItem> _playerSkins;
    private int _skinIndex = 0;

    [SerializeField, BoxGroup("PlayBtn")] private Localize _playLabel;

    [SerializeField] private GameObject _adsBonusBtn;

    [SerializeField] private ShopOffer _specialOfferShop;

    private static int[] ShowRateUsInGames;

    [SerializeField] private RateUs _rateUs;

    [SerializeField] private GameObject _ourGamesBtn;
    [SerializeField] private GameObject _refferalBtn;

    [SerializeField] private GameObject _noAdsBuyBtn;
    [SerializeField] private GameObject _noAdsBuySale;


    [SerializeField] private TextMeshProUGUI _softWalletValue;
    [SerializeField] private TextMeshProUGUI _hardWalletValue;

    private static bool _isFreeStartBigger;

    public static bool IsFreeStartBigger
    {
        get => _isFreeStartBigger;
        set
        {
            _isFreeStartBigger = value;
            if (value)
                FindObjectOfType<MenuScene>()?._adsBonusBtn.SetActive(true);
        }
    }

    private static string GetUserName()
    {
        var userName = GetOrPush.UserName;
        return LoadingController.IsFistRun && userName.StartsWith("Player#", StringComparison.Ordinal)
            ? string.Empty
            : userName;
    }

    [Button]
    private void UpdateDiffs()
    {
        ServerRequest.Instance.RequestDiffsNow();
    }
    
    [Button]
    private void ResetAccount()
    {
        ServerRequest.Instance.ResetAccount();
    }
    
    [Button]
    private void ResetWheel()
    {
        ServerRequest.Instance.ResetWheel();
    }

    [Button]
    public void ClearUserInfo()
    {
        PlayerPrefs.DeleteKey(nameof(GetOrPush.LowPriceNoAdsDateTime));
    }

private void TryParseShowRateUsAfterGames()
    {
        if(ShowRateUsInGames != null)
            return;
        
        try
        {
            var array = JArray.Parse(RemoteConfig.GetString("ShowRateUsAfterGames"));
            var arrayInts = new int[array.Count];
            for (int i = 0; i < array.Count; i++)
                arrayInts[i] = (int)array[i];

            ShowRateUsInGames = arrayInts;
        }
        catch (Exception e)
        {
            FirebaseApplication.LogException(e);
        }
    }
    
    private void Start()
    {
        TryParseShowRateUsAfterGames();
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            var gameSystem = world.GetExistingSystem<GameMatchSystem>();
            gameSystem?.CompleteDestroyLastGame();
        }
        
        var ap = Holder.Instance;
        if (ap != null && ap.Active)
        {
            var dpc = ap.DevicePerformanceControl;
            dpc.CpuLevel = 0;
            dpc.GpuLevel = 0;
        }
        
        Application.targetFrameRate = 30;
        
        IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent; 
        IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += OnOnRewardedVideoAvailabilityChangedEvent;
        
        Inventory.SoftWallet.OnChange += SoftWalletOnOnChange;
        Inventory.HardWallet.OnChange += HardWalletOnOnChange;
        Inventory.Crowd.OnChange += CrowdOnChange;

        _softWalletValue.text = Inventory.SoftWallet.Value.ToString();
        _hardWalletValue.text = Inventory.HardWallet.Value.ToString();

        var now = DateTime.UtcNow;
        var currentHours = now.TotalHours();
        var lastLaunchDateTime = DateTimeExtensions.FromHours(GetOrPush.LastGameLaunch);

        if (GetOrPush.LastGameLaunch != 0)
        {
            CrowdAnalyticsMediator.Instance
                .BeginEvent("retention_launch")
                .AddField("dif", currentHours - GetOrPush.LastGameLaunch)
                .AddField("nextDay", now.TotalDay() > lastLaunchDateTime.TotalDay())
                .CompleteBuild();
            
            GetOrPush.LastGameLaunch = currentHours;
        }
        
        if(_refferalBtn != null)
            _refferalBtn.SetActive(!string.IsNullOrEmpty(ServerRequest.UserId));
        
        if(_ourGamesBtn != null)
            _ourGamesBtn.SetActive(!string.IsNullOrEmpty(RemoteConfig.GetString("OurGamesLink")));

        CrowdOnChange(Inventory.Crowd.Value);
        
        _playerSkins = Database.Players;
        Inventory.PlayerSkin.OnChange += OnSkinUpdate;
        OnSkinUpdate(Inventory.PlayerSkin.Value);
        
        _bestScore.text = GetOrPush.BestScore.ToString();
        _userName.text = GetUserName();
        _userName.onSelect.AddListener(name =>
        {
            if (name.StartsWith("Player#", StringComparison.Ordinal))
                _userName.text = string.Empty;
        });
        
        _userName.onEndEdit.AddListener(name => {
            if (name.Length < 2)
            {
                _userName.text = GetUserName();
                return;
            }
            GetOrPush.UserName = name;
            ServerRequest.Instance.UpdateNameReq(name);
        });

        GetOrPush.Xp.OnChange += OnExpOrLevelChange;
        GetOrPush.Level.OnChange += OnExpOrLevelChange;
        DrawLevel();
        
        SoundManager.Instance.PlayMenuMusic();

        _adsBonusBtn.SetActive(!GetOrPush.AdsBonusActive && IsActiveAdsBounus(IronSource.Agent.isRewardedVideoAvailable()));

        var playGames = GetOrPush.PlayGames;
#if UNITY_UDP_PLATFORM
#else
        
        var showRateUs = RemoteConfig.GetInt("IsShowRateUs");
        var showLastRateUs = PlayerPrefs.GetInt("showLastRateUs");

        var forceShow = false; //Database.All.TryGetValue("PLAYER_COWBOY", out var cowboy) && !Inventory.IsAvailability(cowboy);
        if (showRateUs != -1 && (ShowRateUsInGames.Contains(playGames) || forceShow) && showLastRateUs != playGames)
        {
            PlayerPrefs.SetInt("showLastRateUs", playGames);
            _rateUs.Show(forceShow);
        }
        else
#endif
            _specialOfferShop.TryShow(playGames);
        
        
        ServerRequest.Instance.SyncCompleted += OnSyncCompleted;
        
        _noAdsBuyBtn.SetActive(GetOrPush.AdsCurrentType == GetOrPush.AdsType.AdsEnable);
        if(_noAdsBuyBtn.activeSelf)
            _noAdsBuySale.SetActive(GetOrPush.HasSales("com.rustygames.tc.none_ads.forever"));
        ServerRequest.Instance.UpdateUserInfo();
        ServerRequest.Instance.RequestDiffs();
        
        IAPHandler.CompleteExecutePurchase += OnCompleteExecutePurchase;
    }

    private void OnSkinUpdate(PlayerItem player)
    {
        _skinIndex = _playerSkins.IndexOf(player);
        var skin = _playerSkins[_skinIndex];
        _skinImage.sprite = skin.Preview;
        var rect = skin.PreviewOffsets;
        _skinImage.rectTransform.anchoredPosition = rect.position;
        _skinImage.rectTransform.sizeDelta = rect.size;

        if (GetOrPush.UnlockedAllSkins || Inventory.IsAvailability(skin))
        {
            _playLabel.SetTerm("MainScreen/PLAY_BTN");
            _skinImage.color = Color.white;
        }
        else
        {
            _playLabel.SetTerm("MainScreen/SKIN_DISABLE_BTN");
            _skinImage.color = Color.gray;
        }
    }

    private void CrowdOnChange(int value)
    {
        if (_crowdSizeValue != null)
            _crowdSizeValue.text = "+" + (value + GetOrPush.CrowdAdBonus() + GetOrPush.CrowdReferalBonus());
    }

    private void HardWalletOnOnChange(int value)
    {
        _hardWalletValue.text = value.ToString();
    }

    private void SoftWalletOnOnChange(int value)
    {
        _softWalletValue.text = value.ToString();
    }

    private void OnOnRewardedVideoAvailabilityChangedEvent(bool available)
    {
        _adsBonusBtn.SetActive(IsActiveAdsBounus(available));
    }

    private bool IsActiveAdsBounus(bool haveAds)
    {
        return IsFreeStartBigger || (!GetOrPush.AdsBonusActive && haveAds);
    }

    private void OnSyncCompleted()
    {
        if(_refferalBtn != null)
            _refferalBtn.SetActive(!string.IsNullOrEmpty(ServerRequest.UserId));
        
        CrowdOnChange(Inventory.Crowd.Value);
    }

    public void BuyNoAdsOnClick()
    {
        string buyId = GetOrPush.HasSales("com.rustygames.tc.none_ads.forever")
            ? "com.rustygames.tc.none_ads.forever_sale"
            : "com.rustygames.tc.none_ads.forever";

        CodelessIAPStoreListener.Instance.InitiatePurchase(buyId);
    }

    private void OnExpOrLevelChange(int value)
    {
        DrawLevel();
    }
    
    private void DrawLevel()
    {
        _levelValue.text = GetOrPush.Level.ToString();
        int exp = GetOrPush.Xp.Value;
        int nextLvlExp = GetOrPush.XpToNext.Value;
        _xpValue.text = nextLvlExp ==0 ? string.Empty : $"{exp.ToString()}/{nextLvlExp.ToString()}";

        float fill = 0;
        if(exp > 0 && nextLvlExp > 0) 
            fill = (float) exp / nextLvlExp;
        _levelProgress.value = math.clamp(fill, 0.01f, 1f);
    }
    
    [Button]
    private void ActivateAdsBonus()
    {
        if(!GetOrPush.AdsBonusActive)
            GetOrPush.AdCrowdBonus += RemoteConfig.GetInt("AdsCrowdBonus");
        GetOrPush.AdsBonusActive = true;
        CrowdOnChange(Inventory.Crowd.Value);
    }
    
    private void OnDestroy()
    {
        IAPHandler.CompleteExecutePurchase -= OnCompleteExecutePurchase;
        Inventory.PlayerSkin.OnChange -= OnSkinUpdate;
        
        GetOrPush.Xp.OnChange -= OnExpOrLevelChange;
        GetOrPush.Level.OnChange -= OnExpOrLevelChange;
        
        Inventory.SoftWallet.OnChange -= SoftWalletOnOnChange;
        Inventory.HardWallet.OnChange -= HardWalletOnOnChange;
        Inventory.Crowd.OnChange -= CrowdOnChange;
        
        IronSourceEvents.onRewardedVideoAdRewardedEvent -= RewardedVideoAdRewardedEvent;
        IronSourceEvents.onRewardedVideoAvailabilityChangedEvent -= OnOnRewardedVideoAvailabilityChangedEvent;
        
        ServerRequest.Instance.SyncCompleted -= OnSyncCompleted;
    }

    private void OnCompleteExecutePurchase(string id)
    {
        switch (id)
        {
            case "com.rustygames.tc.none_ads.forever_sale":
            case "com.rustygames.tc.none_ads.forever":
                _noAdsBuyBtn.SetActive(false);
                break;
        }
    }

    public void OurGames()
    {
        CrowdAnalyticsMediator.Instance.BeginEvent("out_games")
            .AddField("games_count", GetOrPush.PlayGames)
            .CompleteBuild();
        
        Application.OpenURL(RemoteConfig.GetString("OurGamesLink"));
    }
    
#if UNITY_EDITOR
    [Button]
    private void CreateScreenshots()
    {
        ScreenCapture.CaptureScreenshot("screen.png");
    }
#endif

    public void PlayBtnPress()
    {
        if (_skinIndex < 0 || _skinIndex >= _playerSkins.Count)
            _skinIndex = _playerSkins.IndexOf(Inventory.PlayerSkin.Value);
        
        var skin = _playerSkins[_skinIndex];
        if (GetOrPush.UnlockedAllSkins || Inventory.IsAvailability(skin))
            Play();
    }

    private void Play()
    {
        if (string.IsNullOrWhiteSpace(GetOrPush.UserName))
        {
            var name = "Player#" + Random.Range(0, 100000);
            GetOrPush.UserName = name;
            ServerRequest.Instance.UpdateNameReq(name);
        }
        
        SoundManager.Instance.PlayBattleMusic();

        GetOrPush.PlayGamesStarted++;

        LoadingController.TryShowInterstitialAd("Level_Start", out var adsStatus);
        
        var playEvent = CrowdAnalyticsMediator.Instance
            .BeginEvent("play")
            .AddField("skin", Inventory.PlayerSkin.Value.Id)
            .AddField("minion", Inventory.UnitSkin.Value.Id);
        if (adsStatus != -1)
            playEvent.AddField("ads_status", adsStatus);
        
        LoadingController.ToGameScene(adsStatus == 1, playEvent);
    }
    
    public void ShowRewardAds()
    {
        if (IsFreeStartBigger)
        {
            IsFreeStartBigger = false;
            ActivateAdsBonus();

            UpdateManager.OnNextFrame(() =>
            {
                if(!this.Equals(null) && _adsBonusBtn != null)
                    _adsBonusBtn.SetActive(false);
            });
            return;
        }

        var showAdsSuccess = -1;
        var ironSource = IronSource.Agent;
        showAdsSuccess = ironSource.isRewardedVideoAvailable() ? 1 : 0;
        if(showAdsSuccess == 1)
            ironSource.showRewardedVideo("CrowdBonus");

        if(showAdsSuccess == -1)
            return;
        
        CrowdAnalyticsMediator.Instance
            .BeginEvent("begin_reward_ads")
            .AddField("isSuccess", showAdsSuccess)
            .CompleteBuild();
    }
    
    public void ShareFacebook()
    {
        CrowdAnalyticsMediator.Instance.BeginEvent("to_facebook").CompleteBuild();
        Application.OpenURL(RemoteConfig.GetString("FacebookLink"));
    }
    
    public void onRewardedVideoLoaded(bool precache)
    {
        if(this == null || this.Equals(null))
            return;

        UpdateManager.OnNextFrame(() =>
        {
            if(!this.Equals(null) && _adsBonusBtn != null)
                _adsBonusBtn.SetActive(!GetOrPush.AdsBonusActive);
        });
    }

    public void onRewardedVideoFailedToLoad()
    {
    }

    public void onRewardedVideoShowFailed()
    {
        
    }

    public void onRewardedVideoShown()
    {
    }

    public void onRewardedVideoFinished(double amount, string name)
    {
        
    }

    
    private void RewardedVideoAdRewardedEvent(IronSourcePlacement placement)
    {
        if(placement.getRewardName() != "Crowd")
            return;

        GetOrPush.AdCrowdBonus += placement.getRewardAmount();
        onRewardedVideoClosed(true);
    }
    
    public void onRewardedVideoClosed(bool finished)
    {
        if (!finished) 
            return;
        
        GetOrPush.AdsBonusActive = true;
        if(this == null || this.Equals(null))
            return;
            
        _adsBonusBtn.SetActive(false);
        CrowdOnChange(Inventory.Crowd.Value);
    }

    public void onRewardedVideoExpired()
    {
        
    }

    public void onRewardedVideoClicked()
    {
        
    }
}
