#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define USE_FAKE_SERVER
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GameRules;
using GameRules.Server;
using GameRules.Server.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

using System.Runtime.CompilerServices;
using System.Threading;
using Firebase.Extensions;
using GameRules.Firebase.Runtime;
using GameRules.Scripts;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Pool;
using GameRules.Scripts.Server;
using GameRules.Scripts.Server.ServerCore;
using GameRules.Scripts.Thread;
using GameRules.Scripts.UI;
using GameRules.Scripts.UI.News;
using GameRules.TaskManager.Runtime;
using Leaderboard = GameRules.UI.Leaderboards.Leaderboard;
using Unity.Mathematics;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using Task = System.Threading.Tasks.Task;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

static class Providers
{
    public const string NotInitialized = "NotInitialized";
    public const string None = "None";
    public const string AdvId = "advid";
    public const string GooglePlay = "googleplay";
    public const string GameCenter = "gamecenter";
    public const string Facebook = "facebook";
    public const string Vk = "vk";
}

// static class Modes
// {
//   public const string week = "week";
//   public const string month = "month";
//   public const string ever = "ever";

// }

public sealed class ServerRequest
{
    //ТУТ ДОКА: https://docs.microsoft.com/ru-ru/dotnet/api/system.net.http.httpclient?view=netstandard-2.0
    //ДЛЯ СОХРАНЕНИЯ ВСЯКИХ ЗНАЧЕНИЙ См: Assets/GameRules/Scripts/GetOrPush.cs
    // авторизация iOS https://github.com/desertkun/GameCenterAuth

    private static AsyncEventWaitHandle _initializeEvent = new AsyncEventWaitHandle(false);
    public static bool IsCompleteInitialize => _initializeEvent.IsSet;

    private IServerApi _serverApi;
    public static ServerRequest Instance { get; }

    string[] PeriodModes = new[] { "week", "month", "ever" };


    public static List<int> FortuneSoftPrice { get; private set; } = new List<int>();
    public static int FortuneHardPrice { get; private set; }
    
    private static string _userId;
    public static string UserId => _userId;
    public static string SessionId { get; private set; }

    private static bool _hasAnyInternet = true;

    private RefEnum<RequestDiffStatus> _lastRequestDiffStatus;
    private int _requestDiffIndex;
    
    public async Task<ErrorCode> HasPing()
    {
        return await _serverApi.HasPing();
    }
    
    private static string _cacheAccess;
    public static string AccessToken
    {
        get => _cacheAccess ?? (_cacheAccess = PlayerPrefs.GetString(nameof(AccessToken), string.Empty));
        set
        {
            PlayerPrefs.SetString(nameof(AccessToken), value);
            _cacheAccess = value;
        }
    }
    
    private static string _cacheRefresh;
    public static string RefreshToken
    {
        get => _cacheRefresh ?? (_cacheRefresh = PlayerPrefs.GetString(nameof(RefreshToken), string.Empty));
        set
        {
            PlayerPrefs.SetString(nameof(RefreshToken), value);
            _cacheRefresh = value;
        }
    }
    
    private static string _cacheProvider;
    public static string AuthProvider
    {
        get
        {
            return _cacheProvider ??
                (_cacheProvider = PlayerPrefs.GetString(nameof(AuthProvider), Providers.None));
        }
        private set
        {
            PlayerPrefs.SetString(nameof(AuthProvider), value);
            _cacheProvider = value;
        }
    }
    
    private static string _deviceId;
    public static string DeviceId
    {
        get => _deviceId ?? (_deviceId = SystemInfo.deviceUniqueIdentifier);
        private set
        {
            _deviceId = value;
            FirebaseApplication.SetUserId(value);
        }
    }

    public event Action SyncCompleted;
    
    static ServerRequest()
    {
        try
        {
            if(ServicePointManager.DefaultConnectionLimit < 5)
                ServicePointManager.DefaultConnectionLimit = 5;
        }
        catch
        {
        }
        
        Instance = new ServerRequest();
    }

    private ServerRequest()
    {
        _serverApi = new HttpClientServerApi(6000);
        
        _serverApi.UpdateAccessToken(AccessToken);
        _serverApi.UpdateRefreshToken(RefreshToken);
        
        Debug.Log($"HasInternet when ServerRequest creating: {Application.internetReachability}");
        //UpdateAdsPrices();
    }
    
    public void UpdateNameReq(string name)
    {
        UpdateUserInfo(JsonGenerator.Begin().AddParam("name", name));
    }

    public void CompleteSync(JObject result)
    {
        if (result.ContainsKey("error"))
        {
            // handle error types
            Debug.Log("UnknownError");
        }
        else if (result.ContainsKey("id"))
        {
            _userId = result["id"].ToString();

            if (result.TryGetValue("level", out var jLevel))
                GetOrPush.UpdateLevel((int)jLevel);

            if (result.TryGetValue("rouletteSpins", out var jRouletteSpins))
                GetOrPush.RouletteSpins = (int)jRouletteSpins;
            
            if (result.TryGetValue("exp", out var jExp))
                GetOrPush.Xp.Value = (int)jExp;

            if (result.TryGetValue("expToLevelUp", out var jExpToLevelUp))
                GetOrPush.XpToNext.Value = (int)jExpToLevelUp;
            
            var jBest = result["bestScore"];
            if (jBest != null)
                GetOrPush.BestScore = (int)jBest;
            
            var refLink = result["refLink"];
            if (refLink != null)
            {
                var refLinkString = refLink.ToString();
                if(!string.IsNullOrEmpty(refLinkString))
                    GetOrPush.RefCode = refLinkString;
            }

            var jSlavesNumber = result["slavesNumber"];
            if (jSlavesNumber != null)
                GetOrPush.Referral = (int)jSlavesNumber;

            var jName = result["name"];
            if (jName == null && !string.IsNullOrEmpty(GetOrPush.UserName))
                UpdateNameReq(GetOrPush.UserName);
            else
            {
                var name = jName.ToString();
                if(!string.IsNullOrEmpty(GetOrPush.UserName) && name.StartsWith("Player#"))
                    UpdateNameReq(GetOrPush.UserName);
                else if (string.IsNullOrEmpty(GetOrPush.UserName))
                    GetOrPush.UserName = name;
            }
            
            if(result["hasMaster"] != null && FirebaseApplication.IsCheckDependenciesComplete)
                FirebaseApplication.SetUserProperty("IsReferralUser", result["hasMaster"].ToString());

            var jGamesCount = result["games"];
            if (jGamesCount != null)
            {
                var gamesCount = (int)jGamesCount;
                GetOrPush.PlayGames = math.max(GetOrPush.PlayGames, gamesCount);
                GetOrPush.PlayGamesStarted = math.max(GetOrPush.PlayGamesStarted, gamesCount);
                
                FirebaseApplication.SetUserProperty("PlayGamesCount", GetOrPush.PlayGames.ToString());
            }
            
            SyncCompleted?.Invoke();
        }
    }

    public async Task Sync()
    {
        var isComplete = AsyncEventWaitHandle.GetNext();
        _serverApi.GetRequestWithResult("user", flags: RequestFlags.ReTryWithAuth).ContinueWithOnMainThread(task =>
        {
            var userResult = task.Result;
            
            if (userResult.Status == ErrorCode.None)
            {
                JObject result = (JObject)userResult.Response;
                CompleteSync(result);
            }
            
            isComplete.Set();
        });

        await isComplete.WaitAsyncAndFree();
    }
    
    public void UpdateScore(int score, int position, bool isDouble)
    {
        _serverApi.PostRequestWithResult("user/updateScore", JsonGenerator.Begin()
            .AddParam("score", score)
            .AddParam("position", position)
            .AddParam("useAds", isDouble ? 1 : 0), RequestFlags.ReTryInfWithAuth).ContinueWithOnMainThread(task =>
        {
            RequestDiffs();
            
            var result = task.Result.Response as JObject;
            if(result == null)
                return;

            if (result.TryGetValue("levels", out var levelsToken) && levelsToken is JObject levels)
            {
                GetOrPush.UpdateLevel((int) levels["level"]);
                GetOrPush.Xp.Value = (int)levels["exp"];
                GetOrPush.XpToNext.Value = (int)levels["expToLevelUp"];
            }
        });
    }
    
    private static JArray _progressTimeline;
    public static JArray ProgressTimeline() => _progressTimeline;
    
    public void UpdateFirebaseToken(string token)
    {
        string device;
#if UNITY_ANDROID
        device = "android";
#elif UNITY_IOS
        device = "ios";
#else
        device = "other";
#endif
        
        var message = JsonGenerator.Begin()
            .AddParam("token", token)
            .AddParam("datetime", DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture))
            .AddParam("lang", Application.systemLanguage.ToString())
            .AddParam("os", device);
        
        UpdateUserInfo(message);
    }

    public void UpdateUserInfo(JsonGenerator message)
    {
        _serverApi.PostRequest("user", message, RequestFlags.ReTryWithAuth);
    }
    
    public async void ApplyDeepLink(Uri url)
    {
        if(!IsCompleteInitialize)
            await _initializeEvent.WaitAsync();

        _serverApi.DeepLink(url, flags: RequestFlags.ReTryWithAuth);
        RequestDiffs();
    }
    
    public async Task<LeaderboardResponse> GetLeaderboard(Leaderboard.Mode period)
    {
        var message = JsonGenerator.Begin()
            .AddParam("period", PeriodModes[(int) period]);

        var leaderboardResult = await _serverApi.PostRequestWithResult("api/leaderboard", message, RequestFlags.ReTryWithAuth);
        if (leaderboardResult.Status == ErrorCode.None)
        {
            JObject result = (JObject) leaderboardResult.Response;
            
            if (result.ContainsKey("error"))
            {
                // handle error types
                return new LeaderboardResponse(ErrorCode.UnknownError);
            }
            
            // AccessToken = result["access"].ToString();
            // topPercentile
            // playerGlobalRank
            // playersList
            // {
            //     "id": "42fa71e6-7e94-47ca-96f6-5c989ebb852b",
            //     "s": 31698,
            //     "n": "Player#1",
            //     "l": 1
            // },


            var playersList = (JArray) result["playersList"];
            var UserScores = new Leaderboard.UserScore[playersList.Count];

            var totalPlayers = result["totalPlayers"].ToString();
            var jPlayerRank = result["playerGlobalRank"];
            var playerRank = jPlayerRank == null ? totalPlayers : jPlayerRank.ToString();

            var i = 0;
            foreach (var item in (playersList).Children())
            {
                Leaderboard.UserScore us;
                us.Score = (int)item["s"];
                us.Name = (string)item["n"];

                var s = item["self"];
                us.Self = s != null && (bool)s;
                
                UserScores[i] = us;
                i++;
            }

            return new LeaderboardResponse(ErrorCode.None, UserScores, playerRank, totalPlayers);

        }
        
        return new LeaderboardResponse(leaderboardResult.Status);
    }

    public async Task<AuthResponse> Authorize()
    {
        _initializeEvent.Reset();
        Debug.Log($"Authorize");
        
        /*#if UNITY_EDITOR
            AuthProvider = Providers.AdvId;
        #endif
        
        //Кейс игрок запустил игру и в сохранке нет инфы о авторизации
        if(AuthProvider == Providers.None)
            GameServices.ManagedInit();

         while (AuthProvider == Providers.None)
            await Task.Delay(50);*/

        if (AuthProvider == Providers.None)
            AuthProvider = Providers.AdvId;

        if (AuthProvider == Providers.AdvId)
        {
            bool complete = false;
            
            if (GetOrPush.AuthMode == "device")
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                
                var isComplete = AsyncEventWaitHandle.GetNext();
                
                Application.RequestAdvertisingIdentifierAsync((id, enabled, msg) =>
                {
                    if (enabled && !string.IsNullOrEmpty(id))
                    {
                        deviceId = id;
                    }
                    else
                    {
                        if(!string.IsNullOrEmpty(msg))
                            UpdateManager.OnNextFrame(() => FirebaseApplication.LogError($"Failed get advertisingIdentifier. Reason: {msg}"));
                    }
                
                    complete = true;

                    isComplete.Set();
                
#if DEVELOPMENT_BUILD 
                    UpdateManager.OnNextFrame(() => Debug.Log($"RequestAdvertisingIdentifierAsync: id: {id} enabled: {enabled} msg {msg}"));
#endif
                });
                
                await Task.WhenAny(isComplete.WaitAsync(), Task.Delay(3000));
                isComplete.Free();
                
                DeviceId = deviceId;
            }
            else
            {
                DeviceId = GetOrPush.ForceDeviceId;
                complete = true;
            }
            
            
            if (!complete)
            {
#if DEVELOPMENT_BUILD 
                Debug.Log($"RequestAdvertisingIdentifierAsync failed!");
#endif
            }
        }
        else
            throw new NotImplementedException();

        //Тут можно использовать Id для авторизации...
        // if(AuthProvider == Providers.GooglePlay || AuthProvider == Providers.GameCenter)
        //     GameServices.ManagedInit();

        Debug.Log("Begin request AccessToken");
        
        bool isSuccess = false;
        int count = 0;
        while (count < 3)
        {
            count++;
            
            Debug.Log("Request AccessToken");
            var result = await _serverApi.AuthWithAccess();
            switch (result)
            {
                case ErrorCode.NotInternet:
                    isSuccess = false;
                    count = int.MaxValue;
                    break;
                case ErrorCode.None:
                    isSuccess = true;
                    count = int.MaxValue;
                    break;
                case ErrorCode.InvalidToken:
                    _serverApi.UpdateAccessToken(string.Empty);
                    _serverApi.UpdateRefreshToken(string.Empty);
                    break;
            }
        }

        //if (isSuccess)
        //    await Sync();

        _initializeEvent.Set();

        return new AuthResponse(isSuccess ? ErrorCode.None : ErrorCode.UnknownError);
        // return await AuthReq();
    }
    
    public void RateUsResult(string result)
    {
        UpdateUserInfo(JsonGenerator.Begin().AddParam("appRating", result));
    }
    
    public void UpdateUserInfo()
    {
        var userName = GetOrPush.UserName;
        var data = JsonGenerator.Begin()
            .AddParam("games", GetOrPush.PlayGames);

        if (!string.IsNullOrEmpty(userName))
            data.AddParam("name", userName);
        
        UpdateUserInfo(data);
        /*var bestScore = GetOrPush.BestScore;
        if(bestScore > 0)
            UpdateScore(bestScore);*/
    }

    public void UpdateRefCode(string newRefCode)
    {
        var message = JsonGenerator.Begin().AddParam("link", newRefCode);
        _serverApi.PostRequest("user/ref", message, RequestFlags.ReTryWithAuth);
    }

    public async Task<ErrorCode> SyncInventory()
    {
        var result = await _serverApi.PostRequestWithResult("auth/session", JsonGenerator.Begin().AddParam("version", Application.version).AddParam("version_DB", Database.Version));
        if (result.Status != ErrorCode.None)
            return result.Status;

        var sessionResult = (JObject)result.Response;

        if (!sessionResult.TryGetValue("success", out var isSuccess) || !(bool) isSuccess)
            return ErrorCode.UnknownError;

        if(sessionResult.TryGetValue("userinfo", out var jUser))
            CompleteSync((JObject) jUser);

        _progressTimeline = (JArray)sessionResult["levelUpRewards"];
        
        SessionId = sessionResult["sessionId"].ToString();
        Database.Sync((JObject)sessionResult["database"]);
        
        var inventoryResult = await _serverApi.PostRequestWithResult("inventory", JsonGenerator.Begin().AddParam("from", InventoryHistory.LastIndex));
        if (inventoryResult.Status != ErrorCode.None)
            return inventoryResult.Status;

        var index = (int)inventoryResult.Response["index"];
        InventoryHistory.UpdateDiffs(index, (JArray)inventoryResult.Response["diff"], true);
        Inventory.SyncItems((JArray) inventoryResult.Response["items"]);
        Inventory.SyncWallets((JObject)inventoryResult.Response["wallets"]);

        if (inventoryResult.Response["flags"] is JObject flags)
        {
            if (flags.TryGetValue("players", out var players))
            {
                var isUnlock = (int)players == 1;
                if (isUnlock)
                    GetOrPush.UnlockedAllSkins = isUnlock;
            }

            if (flags.TryGetValue("ads", out var ads))
            {
                var isUnlock = (int)ads == 1;
                if (isUnlock)
                    GetOrPush.AdsCurrentType = GetOrPush.AdsType.AdsDisable;
            }
            
        }
        
        return ErrorCode.None;
    }

    public async Task<ErrorCode> SyncFortuneConfig()
    {
        var result = await _serverApi.GetRequestWithResult("roulette/globalconf", RequestFlags.ReTry);
        if (result.Status != ErrorCode.None)
            return result.Status;
        
        var prices = result.Response["spinPrice"];

        var hardPrice = prices["hard"];
        if (hardPrice.Type == JTokenType.Array)
            FortuneHardPrice = (int) hardPrice[0];
        else
            FortuneHardPrice = (int) hardPrice;
        
        FortuneSoftPrice.Clear();
        var softPrices = (JArray)prices["soft"];
        for (int i = 0; i < softPrices.Count; i++)
            FortuneSoftPrice.Add((int)softPrices[i]);

        return ErrorCode.None;
    }

    private void GetInventoryDiffs(int from, RefEnum<RequestDiffStatus> status)
    {
        _serverApi.PostRequestWithResult("inventory/delta", JsonGenerator.Begin().AddParam("from", from), RequestFlags.ReTryInfWithAuth).ContinueWithOnMainThread(
            task =>
            {
                if (status.Value == RequestDiffStatus.Complete)
                {
                    status.Release();
                    Interlocked.Decrement(ref _syncCount);
                    return;
                }
        
                status.Set(RequestDiffStatus.Complete);
                Interlocked.Decrement(ref _syncCount);
        
                if (task.Exception != null || task.Result.Status != ErrorCode.None)
                    return;
        
                var inventoryResult = task.Result;
                var result = (JObject)inventoryResult.Response;
        
                int index = -1;

                if (result.TryGetValue("index", out var jIndex))
                {
                    index = (int)jIndex;
            
                    if(InventoryHistory.LastIndex >= index)
                        return;
                }


                if (result.TryGetValue("diff", out var jDiffs))
                {
                    var diffs = (JArray) jDiffs;
                
                    if (index == -1)
                        index = from + diffs.Count;

                    InventoryHistory.UpdateDiffs(index, diffs, false);
                }
                _waitRequest.Set();
            });
        
    }

    public async Task<(NewsModel[] items, int index)> GetAllNews()
    {
        var result = await _serverApi.PostRequestWithResult("news", JsonGenerator.Begin(), RequestFlags.ReTry);

        int index = 0;
        NewsModel[] news = null;
        if (result.Status == ErrorCode.None && result.Response is JObject jResponse)
            ParseNews(jResponse, ref index, ref news);
        
        return (news ?? new NewsModel[0], index);
    }
    
    public async Task<(NewsModel[] items, int index)> GetNews(int from, int to)
    {
        var result = await _serverApi.PostRequestWithResult("news", JsonGenerator.Begin()
            .AddParam("from", from)
            .AddParam("to", to), RequestFlags.ReTry);

        int index = 0;
        NewsModel[] news = null;
        if (result.Status == ErrorCode.None && result.Response is JObject jResponse)
            ParseNews(jResponse, ref index, ref news);
        
        return (news ?? new NewsModel[0], index);
    }

    public Task<ServerResponse> BuyTicket(string currency)
    {
        return _serverApi.PostRequestWithResult("exchange", JsonGenerator.Begin()
            .AddParam("productId", "rouletteSpin")
            .AddParam("qty", 1)
            .AddParam("currency", currency), RequestFlags.ReTryInfWithAuth);
    }

    private void ParseNews(JObject jResponse, ref int index, ref NewsModel[] news)
    {
        if(jResponse.TryGetValue("index", out var jIndex))
            index = (int)jIndex;

        if (jResponse.TryGetValue("range", out var jValue) && jValue is JArray jNews)
        {
            var nullData = new DateTime(1970, 1, 1);
            news = new NewsModel[jNews.Count];

            for (int i = 0; i < news.Length; i++)
            {
                var data = (JObject)jNews[i];
                var body = (JObject)data["body"]["en"];
                news[i] = new NewsModel(nullData.AddMilliseconds((long)data["date"]), data["priority"].ToString(), body["title"].ToString(), body["message"].ToString());
            }
        }
    }

    public Task<ServerResponse> RotateWheel()
    {
        return _serverApi.GetRequestWithResult("roulette/go", RequestFlags.ReTryWithAuth);
    }

    public async Task<JObject> GetNextFortuneRewards()
    {
        var result = await _serverApi.GetRequestWithResult("roulette/wheelconf", RequestFlags.ReTryWithAuth);
        if (result.Status == ErrorCode.None)
            return result.Response as JObject;
        return null;
    }

    public Task<ItemErrorCode> BuyItem(string id, string walletType)
    {
        return _serverApi.PostRequestWithResult("exchange", JsonGenerator.Begin()
            .AddParam("productId", id)
            .AddParam("qty", 1)
            .AddParam("currency", walletType), RequestFlags.ReTryInfWithAuth).ContinueWith(task =>
        {
            if (task.Exception != null)
                return ItemErrorCode.UnknownError;
            switch (task.Result.Status)
            {
                case ErrorCode.None:
                    var json = task.Result.Response as JObject;
                    if(json == null)
                        goto default;
                    
                    ItemErrorCode result = ItemErrorCode.None;
                    if (json.TryGetValue("error", out var error))
                    {
                        try
                        {
                            if (!Enum.TryParse(error.ToString(), true, out result))
                                result = ItemErrorCode.UnknownError;
                        }
                        catch (Exception e)
                        {
                            result = ItemErrorCode.UnknownError;

                            FirebaseApplication.LogException(e);
                            Debug.LogException(e);
                        }
                    }
                    return result;
                case ErrorCode.NotInitialize:
                case ErrorCode.InvalidToken:
                case ErrorCode.UnknownError:
                case ErrorCode.NotInternet:
                    return ItemErrorCode.NotInternet;
                default:
                    return ItemErrorCode.UnknownError;
            }

            if (task.Result.Status == ErrorCode.NotInternet)
                return ItemErrorCode.NotInternet;
            return ItemErrorCode.UnknownError;
        });
    }

    private enum RequestDiffStatus
    {
        WaitNotify,
        Execute,
        Complete
    }
    
    private RequestDiffStatus GetStartRequestDiffsStatus()
    {
        var status = RequestDiffStatus.WaitNotify;
#if UNITY_EDITOR
        status = RequestDiffStatus.Execute;
#elif UNITY_IOS
        if (UnityEngine.iOS.NotificationServices.enabledNotificationTypes == NotificationType.None)
            status = RequestDiffStatus.Execute;
#endif
        return status;
    }

    private static int _syncCount;
    public static bool IsSyncProcessed => _syncCount != 0;
    private readonly ManualResetEventSlim _waitRequest = new ManualResetEventSlim(false);
    private Task _waitRequestDiffsTask;

    private Task waitRequestDiffsTask
    {
        get
        {
            return Task.Factory.StartNew(() =>
            {
                if(!_waitRequest.IsSet)
                    _waitRequest.Wait();
            });
        }
    }

    public Task RequestDiffs()
    {
        InternalRequestDiffs(RequestDiffStatus.Execute, InventoryHistory.LastIndex);
        return waitRequestDiffsTask;
    }
    
    public void RequestDiffsNow()
    {
        InternalRequestDiffs(RequestDiffStatus.Execute, -1);
    }

    private void InternalRequestDiffs(RequestDiffStatus startStatus, int index)
    {
        _requestDiffIndex = index;
        if (_lastRequestDiffStatus == null || _lastRequestDiffStatus.IsRelease)
        {
            _waitRequest.Reset();
            UpdateManager.Instance.StartCoroutine(InternalRequestDiffs(_lastRequestDiffStatus = RefEnum<RequestDiffStatus>.Create(startStatus)));
        }
        else if (_lastRequestDiffStatus != null)
        {
            if (_lastRequestDiffStatus.Value < startStatus)
                _lastRequestDiffStatus.Set(startStatus);
            else if(_lastRequestDiffStatus.Value > startStatus)
            {
                _waitRequest.Reset();
                _lastRequestDiffStatus.Set(RequestDiffStatus.Complete);
                UpdateManager.Instance.StartCoroutine(InternalRequestDiffs(_lastRequestDiffStatus = RefEnum<RequestDiffStatus>.Create(startStatus)));
            }
        }
    }

    private IEnumerator InternalRequestDiffs(RefEnum<RequestDiffStatus> status)
    {
        Interlocked.Increment(ref _syncCount);
        var time = Time.unscaledTime + 3f;
        while (Time.unscaledTime < time && status.Value == RequestDiffStatus.WaitNotify)
            yield return null;

        if (status.Value == RequestDiffStatus.Complete)
        {
            status.Release();
            Interlocked.Decrement(ref _syncCount);
            yield break;
        }
        
        status.Set(RequestDiffStatus.Execute);
        GetInventoryDiffs(InventoryHistory.LastIndex, status);
    }

    [Conditional("UNITY_EDITOR")]
    public void ResetAccount()
    {
        _serverApi.PostRequest("dash/resetUser", JsonGenerator.Begin().AddParam("id", UserId)).ContinueWithOnMainThread(
            task => { Debug.LogError($"ResetAccountStatus: {task.Result == ErrorCode.None}"); });
    }

    [Conditional("UNITY_EDITOR")]
    public void ResetWheel()
    {
        _serverApi.PostRequest("dash/rouletteReset", JsonGenerator.Begin().AddParam("id", UserId)).ContinueWithOnMainThread(
            task =>
            {
                RequestDiffs();
                Debug.LogError($"ResetWheelStatus: {task.Result == ErrorCode.None}");
            });
    }

    public Task<ServerResponse> PreviewScore(int score, int place, int placeWithAds)
    {
        return _serverApi.PostRequestWithResult("user/previewUpdateScore", JsonGenerator.Begin()
            .AddParam("score", score)
            .AddParam("position", place)
            .AddParam("positionWithAds", placeWithAds), RequestFlags.ReTryWithAuth);
    }
    
    public void IAP(string id, string receipt, Action<bool> onComplete)
    {
        _serverApi.PostRequestWithResult("iap/validate", JsonGenerator.Begin()
            .AddParam("productId", id)
            .AddParam("receipt", receipt), RequestFlags.ReTryWithAuth).ContinueWithOnMainThread(task =>
        {
            var result = task.Result;
            if (task.Exception != null || result.Status != ErrorCode.None)
            {
                UpdateManager.Instance.StartCoroutine(ReIAP(id, receipt, onComplete));
                return;
            }
            
            var isSuccess = (bool)task.Result.Response["success"];
            RequestDiffs().ContinueWithOnMainThread(task1 =>
            {
                onComplete?.Invoke(isSuccess);
            });
        });
    }
    
    private IEnumerator ReIAP(string id, string receipt, Action<bool> onComplete)
    {
        yield return NotInternet.Instance.WaitInternet(DialogViewBox.Instance, false);
        IAP(id, receipt, onComplete);
    }

    public Task<bool> RemoveAccount()
    {
        return _serverApi.PostRequestWithResult("user/delete", JsonGenerator.Begin(), RequestFlags.ReTryWithAuth)
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                    return false;

                var json = task.Result.Response as JObject;
                if (json.TryGetValue("success", out var result))
                    return (bool)result;
                return false;
            });
    }
}
