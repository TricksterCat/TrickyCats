using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AppsFlyerSDK;
using Core.Base;
using Core.Base.Modules;
using DefaultNamespace;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.DynamicLinks;
using Firebase.Extensions;
using Firebase.Messaging;
using GameRules;
using GameRules.Analytics.Runtime;
using GameRules.Core.Runtime;
using GameRules.Facebook.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.ModuleAdapters.Runtime;
using GameRules.Scripts;
using GameRules.Scripts.ECS.Render.Static;
using GameRules.Scripts.GameTuneImpl;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.Server;
using GameRules.Scripts.UI;
using GameRules.Scripts.UI.News;
using GameRules.Scripts.WrappersECS;
using GameRules.TaskManager.Runtime;
using GameRules.UI;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Advertisements;
using UnityEngine.Analytics;
using UnityEngine.GameTune;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using Task = System.Threading.Tasks.Task;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

#if UNITY_IOS
using UnityEngine.iOS;
#endif

#if HAVE_APPODEAL
using AppodealAds.Unity.Api;
using AppodealAds.Unity.Common;
#endif

public class LoadingController : MonoBehaviour
{
    
    private static ShowController _showController;
    private static Coroutine _process;
    
    public enum Scene
    {
        Loading,
        Menu,
        Game
    }

    private static Action _onPressToScreen;
    private static LoadingController _instance;
    
    public static bool IsCompleteGameLoading { get; private set; }
    public static bool IsFistRun { get; private set; }

    public static Scene ActiveScene { get; private set; }
    public static Scene NextScene { get; private set; }
    
    [SerializeField]
    private Localize _messageLabel;
    
    [SerializeField]
    private NewVersionWindow _newVersionWindow;
    [SerializeField]
    private NotInternet _notInternet;
    [SerializeField]
    private GDPR_view _gdprView;

    [SerializeField]
    private DialogViewBox _messageView;
    
    private IEnumerator WaitSyncInventory(int tryCount)
    {
        Task<ErrorCode> syncInventory = ServerRequest.Instance.SyncInventory();
        while (!syncInventory.IsCompleted)
            yield return null;

        switch (syncInventory.Result)
        {
            case ErrorCode.None:
                FirebaseApplication.CanReceiveMessages();
                FirebaseApplication.CanReceiveDynamicLinks();
                yield break;
            default:
                yield return _notInternet.WaitInternet(_messageView);
                if (tryCount > 2)
                {
                    tryCount = 0;
                    yield return UnknownErrorWindow.TryShow(_messageView);
                }
                yield return WaitSyncInventory(tryCount + 1);
                yield break;
        }
    }
    
    private IEnumerator WaitSyncFortune(int tryCount)
    {
        Task<ErrorCode> syncInventory = ServerRequest.Instance.SyncFortuneConfig();
        while (!syncInventory.IsCompleted)
            yield return null;

        switch (syncInventory.Result)
        {
            case ErrorCode.None:
                yield break;
            default:
                yield return _notInternet.WaitInternet(_messageView);
                if (tryCount > 2)
                {
                    tryCount = 0;
                    yield return UnknownErrorWindow.TryShow(_messageView);
                }
                yield return WaitSyncFortune(tryCount + 1);
                yield break;
        }
    }
    
    private IEnumerator ReAuthorize(int tryCount)
    {
        int status = -1;
        ServerRequest.Instance.Authorize().ContinueWithOnMainThread(task =>
        {
            var exception = task.Exception;
            if(exception != null)
                Debug.LogException(exception);
            
            status = exception == null && task.Result.IsSuccess ? 1 : 0;
        });
        
        while (status == -1)
            yield return null;

        if (status == 0)
        {
            yield return _notInternet.WaitInternet(_messageView);
            if (tryCount > 2)
            {
                tryCount = 0;
                yield return UnknownErrorWindow.TryShow(_messageView);
            }
            yield return ReAuthorize(tryCount + 1);
        }
    }

    private IEnumerator Start()
    {
        FirebaseApplication.TokenReceived += FirebaseApplicationOnTokenReceived;
        FirebaseApplication.MessageReceived += FirebaseApplicationOnMessageReceived;
        FirebaseApplication.DynamicLinkReceived += FirebaseApplicationOnDynamicLinkReceived;
        
        _instance = this;
        
        NextScene = ActiveScene = Scene.Loading;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        DontDestroyOnLoad(transform.root.gameObject);
        _showController = GetComponent<ShowController>();
        
        if (GetOrPush.GameRunCount == 0)
            IsFistRun = true;

        yield return null;
        
        yield return _notInternet.WaitInternet(_messageView);

        
        FirebaseApplication.Awake();
        FacebookApplication.BeginInitializee();
        
        while (FirebaseApplication.Status == StatusInitialize.Wait)
            yield return null;
        
        try
        {
            BaseTenjin instance = CrowdAnalyticsMediator.GetTenjin();
            
            if(GetOrPush.GDPR_consent == 1)
                instance.OptIn();
            else
                instance.OptOut();
            
            instance.Connect();
        }
        catch (Exception e)
        {
            FirebaseApplication.LogException(e);
        }

        
        if(_newVersionWindow != null)
            yield return _newVersionWindow.TryShow(RemoteConfig.GetString("Version"), _messageView);
        
        GetOrPush.Load();
        
        yield return WaitAuthMode.TryWait();
        
        NewsData.Initialize();
        
        if(Debug.isDebugBuild || GetOrPush.UserName == "Maya2")
            FirebaseApplication.SetUserProperty("IsDebug", "1");
        else 
            FirebaseApplication.SetUserProperty("IsDebug", "0");
        

        int isAuthorize = -1;
        ServerRequest.Instance.Authorize().ContinueWithOnMainThread(task =>
        {
            var exception = task.Exception;
            if(exception != null)
                Debug.LogException(exception);
            isAuthorize = task.Exception == null && task.Result.IsSuccess ? 1 : 0;
        });
        
        yield return _gdprView.Show(_messageView);
        
        GlobalSettings.Initialize();
        
#if UNITY_ANDROID
        var permissions =  TmpList<string>.Get();
        
        permissions.Add(Permission.ExternalStorageWrite);
        if (GetOrPush.GDPR_consent == 1)
        {
            permissions.Add(Permission.CoarseLocation);
            permissions.Add(Permission.FineLocation);
        }
        var accessResults = AndroidRuntimePermissions.RequestPermissions(TmpList<string>.ReleaseAndToArray(permissions));
        
        FirebaseApplication.SetUserProperty("AccessStorageWrite", accessResults[0].ToString());
        if(GetOrPush.GDPR_consent == 1)
            FirebaseApplication.SetUserProperty("AccessLocation", accessResults[1].ToString());
#endif
        
        InitAds();
        
        while (isAuthorize == -1)
            yield return null;

        if (isAuthorize == 0)
            yield return ReAuthorize(0);
        
        yield return Database.WaitInitializeEnumerator();
        yield return WaitSyncInventory(0);
        yield return WaitSyncFortune(0);
        
        if (IsFistRun)
        {
            FirebaseApplication.SetUserProperty("IsReferralUser", "0");
            FirebaseApplication.SetUserProperty("GDPR", GetOrPush.GDPR_consent.ToString());
            GetOrPush.VersionFirst = Application.version;

            try
            {
                FirebaseAnalytics.LogEvent("device_info", new[]
                {
                    new Parameter("CPU_Count", SystemInfo.processorCount), 
                    new Parameter("CPU_Frequency", SystemInfo.processorFrequency), 
                    new Parameter("CPU_name", SystemInfo.processorType),
                    new Parameter("SYS_MemorySize", SystemInfo.systemMemorySize), 
                    new Parameter("GPU_MultiThreaded", SystemInfo.graphicsMultiThreaded ? 1 : 0),
                    new Parameter("GPU_name", SystemInfo.graphicsDeviceName),
                    new Parameter("GPU_MemorySize", SystemInfo.graphicsMemorySize), 
                    new Parameter("RenderTargetCount", SystemInfo.supportedRenderTargetCount), 
                    new Parameter("ShaderLevel", SystemInfo.graphicsShaderLevel), 
                    new Parameter("ScreenResolution", $"{Screen.width}:{Screen.height}"), 
                    new Parameter("ScreenHeight", Screen.height), 
                    new Parameter("ScreenDpi", Screen.dpi), 
                });
            }
            catch
            {
                
            }
        }

        GameTuneManager.Initialize();

        GetOrPush.GameRunCount++;
        if (GetOrPush.VersionLast != Application.version)
            GetOrPush.VersionLast = Application.version;
        
        if(GetOrPush.PlayGames == 0)
            FirebaseAnalytics.SetUserProperty("PlayGamesCount", "0");
        
        FirebaseApplication.SetUserProperty("ShaderLevel", SystemInfo.graphicsShaderLevel.ToString());

        IsCompleteGameLoading = true;
        
        while (FacebookApplication.Status == StatusInitialize.Wait)
            yield return null;
        
        Facebook.Unity.FB.Mobile.SetAutoLogAppEventsEnabled(!RemoteConfig.GetBool("Facebook_customEvent"));
        
        #if UNITY_IOS && !UNITY_EDITOR
        FirebaseApplication.SetUserProperty("NotificationAccess", UnityEngine.iOS.NotificationServices.enabledNotificationTypes != NotificationType.None ? "1" : "0");
        #endif
        
        ToMainMenu();
    }

    private void FirebaseApplicationOnDynamicLinkReceived(ReceivedDynamicLinkEventArgs e)
    {
        ServerRequest.Instance.ApplyDeepLink(e.ReceivedDynamicLink.Url);
        CrowdAnalyticsMediator.DynamicLinkReceived();
    }

    private void FirebaseApplicationOnMessageReceived(MessageReceivedEventArgs e)
    {
        try
        {
            if (e.Message.Data != null && e.Message.Data.TryGetValue("action", out var action))
            {
                switch (action)
                {
                    case "sync":
                        ServerRequest.Instance.Sync();
                        break;
                }
            }
        }
        catch (Exception exception)
        {
            FirebaseApplication.LogException(exception);
        }
    }

    private void FirebaseApplicationOnTokenReceived(TokenReceivedEventArgs e)
    {
        ServerRequest.Instance.UpdateFirebaseToken(e.Token);
        Debug.LogError($"firebaseToken:: {e.Token}");
    }

    private void InitAds()
    {
        if(Application.isEditor)
            return;

        string appKey;
#if UNITY_IOS
        appKey = "c8cf5175";
#else
        appKey = "b72917ad";
#endif

        var ironSource = IronSource.Agent;
        ironSource.setConsent(GetOrPush.GDPR_consent == 1);

        var adsKeys = GameRules.RustyPool.Runtime.TmpList<string>.Get();
        adsKeys.Add(IronSourceAdUnits.REWARDED_VIDEO);
        
        if(GetOrPush.IsAdsActive)
            adsKeys.Add(IronSourceAdUnits.INTERSTITIAL);
        
        ironSource.setAdaptersDebug(Debug.isDebugBuild);
        
        ironSource.init (appKey, GameRules.RustyPool.Runtime.TmpList<string>.ReleaseAndToArray(adsKeys));
        
        if(Debug.isDebugBuild)
            ironSource.validateIntegration();

        if (GetOrPush.IsAdsActive)
        {
            IronSourceEvents.onInterstitialAdClosedEvent += InterstitialAdClosedEvent;
            IronSourceEvents.onInterstitialAdLoadFailedEvent += InterstitialAdLoadFailedEvent;      
            IronSourceEvents.onInterstitialAdShowFailedEvent += InterstitialAdShowFailedEvent;
            
            
#if TASK_REINIT_AD
            IsWaitNextRequestAds = true;

            App.GetModule<ITaskSystem>().Subscribe(RequestNextInterstitial());
#else
            ironSource.loadInterstitial();
#endif
        }
        
        IronSourceEvents.onRewardedVideoAdRewardedEvent += OnRewardedVideoAdRewardedEvent;
        
        ironSource.shouldTrackNetworkState (true);
    }

    private void OnRewardedVideoAdRewardedEvent(IronSourcePlacement obj)
    {
        GameTuneManager.CompleteRewardedAd();
    }

    public static bool IsLockRequestAds { get; set; }
    public static bool IsWaitNextRequestAds { get; set; }

    public static bool HaveInterstitialAd => IronSource.Agent.isInterstitialReady();
    
    public static bool TryShowInterstitialAd(string placement, out int status)
    {
        if (!GetOrPush.IsAdsActive || !GameTuneManager.RequiredAd(placement))
        {
            status = -1;
            return false;
        }
        
        var ironSource = IronSource.Agent;
        bool result = ironSource.isInterstitialReady();
        if (result && GameTuneManager.IsShowAd(placement))
        {
            IsLockRequestAds = true;
            ironSource.showInterstitial(placement);

            status = 1;
        }
        else
            status = 0;
        
        return result;
    }
    
    private void InterstitialAdShowFailedEvent(IronSourceError error)
    {
        IsLockRequestAds = false;
        
        var errorString = error.ToString();
        Debug.LogError(errorString);
        FirebaseApplication.LogError(errorString);
    }

    private void InterstitialAdLoadFailedEvent(IronSourceError error)
    {
        var errorString = error.ToString();
        Debug.LogError(errorString);
        FirebaseApplication.LogError(errorString);

        try
        {
            IronSource.Agent.loadInterstitial();
        }
        catch (Exception e)
        {
            FirebaseApplication.LogException(e);
        }
    }

    private void InterstitialAdClosedEvent()
    {
        GameTuneManager.CompleteInterstitialAd();
        #if TASK_REINIT_AD
        IsLockRequestAds = false;
        IsWaitNextRequestAds = true;
        #else
        IronSource.Agent.loadInterstitial();
        #endif
    }

    
#if TASK_REINIT_AD
    private IEnumerator RequestNextInterstitial()
    {
        const float timer = 20f;
        while (true)
        {
            var time = Time.unscaledTime + timer;

            while (IsLockRequestAds || !IsWaitNextRequestAds && time > Time.unscaledTime)
                yield return null;
            
            IsWaitNextRequestAds = false;

            if (!IsLockRequestAds)
            {
                var ironSource = IronSource.Agent;
                if(!ironSource.isInterstitialReady())
                    ironSource.loadInterstitial();
            }
        }
    }
#endif
    
    void OnApplicationPause(bool isPaused) 
    {                 
        IronSource.Agent.onApplicationPause(isPaused);

        if (!isPaused)
        {
            try
            {
                BaseTenjin instance = CrowdAnalyticsMediator.GetTenjin();
                instance.Connect();
            }
            catch (Exception e)
            {
                FirebaseApplication.LogException(e);
            }
        }
    }

    private static int BannerAdsType
    {
        get
        {
#if HAVE_APPODEAL
#if UNITY_ANDROID
            return Appodeal.BANNER_VIEW;
#else
            return Appodeal.BANNER;
#endif
#else
            return 0;
#endif
        }
    }

    public static string MapName { get; private set; }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToMainMenu()
    {
        NextScene = Scene.Menu;
        ActiveScene = Scene.Loading;
        Show(LoadMainMenu());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToGameScene(bool pressToContinue, AnalyticsMediatorEvent playEvent)
    {
        NextScene = Scene.Game;
        ActiveScene = Scene.Loading;
        var map = Inventory.GetRandomMap();
        playEvent
            .AddField("map", map.Id)
            .CompleteBuild();

        MapName = map.Id;
        WindowsManager.ActiveWindows.Clear();
        #if UNITY_EDITOR
        pressToContinue = true;
        #endif
        Show(LoadGameScene(pressToContinue, map));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerator LoadMainMenu()
    {
        WindowsManager.ActiveWindows.Clear();
        yield return SceneManager.LoadSceneAsync(1);
        Hide(CompleteLoadMainMenu());
    }

    private static IEnumerator CompleteLoadMainMenu()
    {
        yield return null;
        yield return null;
        ActiveScene = Scene.Menu;
    }

    public void OnTouchToScreen()
    {
        _onPressToScreen?.Invoke();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerator LoadGameScene(bool pressToContinue, MapItem map)
    {
        GameScene.CanExecute = !pressToContinue;

        yield return map.Scene.LoadSceneAsync();
        
        yield return null;
        
        while (!GameScene.IsExist)
            yield return null;

        yield return null;
        
        if (pressToContinue)
        {
            _instance?._messageLabel.SetTerm("Loading/LOADING_COMPLETE_MESSAGE");
            _onPressToScreen = () =>
            {
                try
                {
                    GameScene.CanExecute = true;
                }
                catch (Exception e)
                {
                    FirebaseApplication.LogException(e);
                }
                _onPressToScreen = null;
            };
        }

        while (!GameScene.CanExecute)
            yield return null;

        yield return null;
        
        Hide(CompleteLoadGame());
    }
    
    private static IEnumerator CompleteLoadGame()
    {
        yield return null;
        ActiveScene = Scene.Game;
    }

    private interface IWaitFunc
    {
        bool Invoke();
    }
    
    private struct WaitShow : IWaitFunc
    {
        private readonly CanvasGroup CanvasGroup;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaitShow(ShowController showController)
        {
            CanvasGroup = showController.CanvasGroup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Invoke()
        {
            return CanvasGroup.alpha > 0.95f;
        }
    }
    
    private struct WaitHide : IWaitFunc
    {
        private readonly CanvasGroup CanvasGroup;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaitHide(ShowController showController)
        {
            CanvasGroup = showController.CanvasGroup;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Invoke()
        {
            return CanvasGroup.alpha < 0.05f;
        }
    }

    private void OnDisable()
    {
        _process = null;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Show(IEnumerator onComplete)
    {
        _instance?._messageLabel.SetTerm("Loading/LOADING_MESSAGE");
        
        _showController.Show();
        
        if(_process != null)
            _showController.StopCoroutine(_process);
        _process = _showController.StartCoroutine(WaitAlpha(new WaitShow(_showController), onComplete));
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Hide(IEnumerator onComplete)
    {
        _showController.Hide();
        
        if(_process != null)
            _showController.StopCoroutine(_process);
        _process = _showController.StartCoroutine(WaitAlpha(new WaitHide(_showController), onComplete));
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerator WaitAlpha(IWaitFunc waitFunc, IEnumerator onComplete)
    {
        while (!waitFunc.Invoke())
            yield return null;

        if(onComplete != null)
            yield return onComplete;
        _process = null;
    }
}
