using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Extensions;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Server.FakeApi;
using GameRules.Scripts.Thread;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameRules.Scripts.Server.ServerCore
{
    public class HttpClientServerApi : IServerApi
    {
        private const string root = "https://catroulette.rustygames.net/";
        public const int CheckResponseCount = 3;
        
        private readonly HttpClient Client = new HttpClient();
        private bool _hasAnyInternet = true;
        
        private StringBuilder _stringBuilder = new StringBuilder(root);
        private Dictionary<string, Uri> _urisCache = new Dictionary<string, Uri>();

        private AsyncEventWaitHandle _waitAuth;
        private Dictionary<string, IFakeApi> _fakeApis;
        
        public HttpClientServerApi()
        {
            ServicePointManager.DnsRefreshTimeout = 20;
            
            var google = ServicePointManager.FindServicePoint(new Uri("https://google.com"));
            if (google != null)
                google.ConnectionLeaseTimeout = 0;
        }

        [Conditional("USE_FAKE_SERVER")]
        private void InitializeFakeApi()//TODO: FAKE_API
        {
            if(_fakeApis != null)
                return;
            _fakeApis = new Dictionary<string, IFakeApi>();
            
            //AddFakeApi(new NewSession_Fake());
            //AddFakeApi(new SyncInventory_Fake());
            //AddFakeApi(new GetInventoryDiffs_Fake());
            //AddFakeApi(new GetNews_fake());
        }
        
        [Conditional("USE_FAKE_SERVER")]
        private void AddFakeApi(IFakeApi fakeApi)
        {
            _fakeApis[fakeApi.Postfix] = fakeApi;
        }
        
        public bool FakeResolve(string api, JsonGenerator input, ref ErrorCode errorCode)
        {
            InitializeFakeApi();
#if USE_FAKE_SERVER
            if (_fakeApis.ContainsKey(api))
            {
                errorCode = _fakeApis[api].Resolve(input.ToParams());
                return true;
            }
#endif
            return false;
        }
        
        public bool FakeResolve(string api, Uri input, ref ErrorCode errorCode)
        {
            InitializeFakeApi();
#if USE_FAKE_SERVER
            if (_fakeApis.ContainsKey(api))
            {
                errorCode = _fakeApis[api].Resolve(input.ToParams());
                return true;
            }
#endif
            return false;
        }
        
        public bool FakeResolveWithResonse(string api, JsonGenerator input, ref ServerResponse response)
        {
            InitializeFakeApi();
#if USE_FAKE_SERVER
            if (_fakeApis.ContainsKey(api))
            {
                response = _fakeApis[api].ResolveWithResponse(input.ToParams());
                return true;
            }
#endif
            return false;
        }
        
        public bool FakeResolveWithResonse(string api, Uri input, ref ServerResponse response)
        {
            InitializeFakeApi();
#if USE_FAKE_SERVER
            if (_fakeApis.ContainsKey(api))
            {
                response = _fakeApis[api].ResolveWithResponse(input.ToParams());
                return true;
            }
#endif
            return false;
        }
        
        public HttpClientServerApi(int timeOutMs)
        {
            Client.Timeout = TimeSpan.FromMilliseconds(timeOutMs);
            Client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            Client.DefaultRequestHeaders.Add("Keep-Alive", "false");
            _waitAuth = new AsyncEventWaitHandle(false);
            
            if(ServicePointManager.DefaultConnectionLimit < 5)
                ServicePointManager.DefaultConnectionLimit = 5;
        }

        public void UpdateAccessToken(string token)
        {
            ServerRequest.AccessToken = token;
            
            var defaultHeader = Client.DefaultRequestHeaders;
            
            if (string.IsNullOrEmpty(token))
                defaultHeader.Authorization = null;
            else
            {
                var authorization = defaultHeader.Authorization;
                if(authorization == null || authorization.Parameter != token)
                    defaultHeader.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public void UpdateRefreshToken(string token)
        {
            ServerRequest.RefreshToken = token;
        }

        public async Task<ErrorCode> AuthWithAccess()
        {
            var deviceId = ServerRequest.DeviceId;
            Debug.Log($"AuthProvider: {ServerRequest.AuthProvider} DeviceId: {deviceId}");
            
            var isComplete = AsyncEventWaitHandle.GetNext();
            
            RE_TRY:
            var message = JsonGenerator.Begin()
                .AddParam("provider", ServerRequest.AuthProvider)
                .AddParam("token", deviceId)
                .AddParam("deviceId", deviceId);

            ErrorCode resultCode = ErrorCode.UnknownError;
            isComplete.Reset();
            
            _waitAuth.Reset();
            PostRequestWithResult("auth", message, flags: RequestFlags.ReTry).ContinueWithOnMainThread(task =>
            {
                Debug.Log($"Complete request AccessToken");
                var exception = task.Exception;
                if (exception != null)
                {
                    Debug.LogException(exception);
                    
                    resultCode = ErrorCode.UnknownError;
                    isComplete.Set();
                    return;
                }
                Debug.Log($"PreParse AccessToken_0");
                var authResult = task.Result;
                
                Debug.Log($"PreParse AccessToken");
                if (authResult.Status == ErrorCode.None)
                {
                    try
                    {
                        JObject result = (JObject)authResult.Response;

                        if (result.ContainsKey("error"))
                        {
                            var errorMessage = (string) result["error"];
                            if (errorMessage == "UserNotExistError" || errorMessage == "InvalidRefreshToken" ||
                                errorMessage == "InvalidAccessToken")
                            {
                                UpdateAccessToken(string.Empty);
                                UpdateRefreshToken(string.Empty);
                            }

                            resultCode = ErrorCode.UnknownError;
                            isComplete.Set();
                            return;
                        }
                
                        UpdateAccessToken(result["access"].ToString());
                        UpdateRefreshToken(result["refresh"].ToString());
                    
                        resultCode = ErrorCode.None;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        FirebaseApplication.LogException(e);
                    }
                    
                    
                    Debug.Log($"CompleteParse AccessToken");
                    
                    _waitAuth.Set();
                    isComplete.Set();
                    return;
                }

                switch (authResult.Status)
                {
                    case ErrorCode.TokenExpired:
                    case ErrorCode.InvalidToken:
                        resultCode = authResult.Status;
                        break;
                    default:
                        resultCode = ErrorCode.UnknownError;
                        break;
                }
                
                isComplete.Set();
            });

            await isComplete.WaitAsync();

            if (resultCode == ErrorCode.WaitUpdateAccess)
            {
                Debug.Log($"WaitUpdateAccess");
                await Task.Delay(100);
                
                goto RE_TRY;
            }

            isComplete.Free();

            if (resultCode == ErrorCode.TokenExpired)
            {
                Debug.Log($"Begin UpdateAccessWithRefresh");
                resultCode = await UpdateAccessWithRefresh();
            }
            
            
            Debug.Log($"Complete request AccessToken_2");
            
            return resultCode;
        }

        public async Task<ErrorCode> UpdateAccessWithRefresh()
        {
            if (string.IsNullOrEmpty(ServerRequest.RefreshToken))
                return await AuthWithAccess();
            
            var deviceId = ServerRequest.DeviceId;
            var message = JsonGenerator.Begin()
                .AddParam("deviceId", deviceId)
                .AddParam("refresh", ServerRequest.RefreshToken);
            
            
            var isComplete = AsyncEventWaitHandle.GetNext();
            
            ErrorCode resultCode = ErrorCode.UnknownError;
            _waitAuth.Reset();
            PostRequestWithResult("auth/refresh2", message, flags: RequestFlags.ReTry).ContinueWithOnMainThread(task =>
            {
                var updateWithRefresh = task.Result;
                
                if (updateWithRefresh.Status == ErrorCode.None)
                {
                    JObject result = (JObject)updateWithRefresh.Response;
                
                    if (result.ContainsKey("error"))
                    {
                        var errorMessage = (string)result["error"];
                        if(errorMessage == "UserNotExistError" || errorMessage == "InvalidRefreshToken")
                        {
                            UpdateAccessToken(string.Empty);
                            UpdateRefreshToken(string.Empty);
                        }
                        
                        resultCode = ErrorCode.WaitUpdateAccess;
                        isComplete.Set();
                        return;
                    }

                    UpdateAccessToken(result["access"].ToString());
                    UpdateRefreshToken(result["refresh"].ToString());

                    resultCode = ErrorCode.None;
                    
                    _waitAuth.Set();
                    isComplete.Set();
                    return;
                }
                
                resultCode = ErrorCode.UnknownError;
                isComplete.Set();
            });

            await isComplete.WaitAsyncAndFree();

            if (resultCode == ErrorCode.WaitUpdateAccess)
                return await AuthWithAccess();
            
            return resultCode;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<ErrorCode> CheckResponse(HttpResponseMessage response)
        {
            Debug.Log($"response.StatusCode: {response.StatusCode}");
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                JObject result;
                try
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(body))
                    {
                        Debug.LogError(body);
                        FirebaseApplication.LogError(body);
                        
                        result = JObject.Parse(body);
                        if (result.ContainsKey("code"))
                        {
                            // handle error types
                            // Debug.Log($"resultname: {result["name"].ToString()}");
                            if (result.TryGetValue("name", out var jName))
                            {
                                var name = jName.ToString();
                                switch (name)
                                {
                                    case "JsonWebTokenError":
                                        return ErrorCode.InvalidToken;
                                    case "TokenExpiredError":
                                        return ErrorCode.TokenExpired;
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
                return ErrorCode.UnknownError;
            }
            return ErrorCode.None;
        }
        
        public async Task<ServerResponse> PostRequestWithResult(string postfix, JsonGenerator input, RequestFlags flags = RequestFlags.WaitAuth)
        {
            if ((flags & RequestFlags.WaitAuth) != 0 && !_waitAuth.IsSet)
                await _waitAuth.WaitAsync();
            
#if USE_FAKE_SERVER
            ServerResponse resultResponse = default;
            if(FakeResolveWithResonse(postfix, input, ref resultResponse))
                return resultResponse;
#endif
            
            if (!_urisCache.TryGetValue(postfix, out var uri))
            {
                uri = new Uri(root + postfix);
                _urisCache[postfix] = uri;
            }

            var httpContent = new StringContent(input.Release());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            int tryCount;
            if ((flags & RequestFlags.ReTry) != 0)
                tryCount = 3;
            else if ((flags & RequestFlags.InfReTry) != 0)
                tryCount = int.MaxValue;
            else
                tryCount = 0;

            HttpResponseMessage request = null;
            
            RE_TRY:
            try
            {
                request = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = httpContent
                }, HttpCompletionOption.ResponseContentRead);
            }
            catch (Exception e)
            {
                e = new Exception($@"Failed SendAsync in ""{postfix}""", e);
                Debug.LogException(e);
                FirebaseApplication.LogException(e);
                if (tryCount > 0)
                {
                    tryCount--;
                    await Task.Delay(500);
                    goto RE_TRY;
                }
            }

            if (request != null)
            {
                if (request.IsSuccessStatusCode)
                {
                    bool isSuccess = false;
                    var content = await request.Content.ReadAsStringAsync();
                    JToken result = null;
                    try
                    {
                        result = JToken.Parse(content);
                        isSuccess = true;
                    }
                    catch (Exception e)
                    {
                        e = new Exception($@"Failed try parse json in API: ""{postfix}""", e);
                        Debug.LogException(e);
                        FirebaseApplication.LogException(e);
                    }
                
                    return new ServerResponse(isSuccess ? ErrorCode.None : ErrorCode.BadJson, result);
                }

                var responseError = await CheckResponse(request);
                switch (responseError)
                {
                    case ErrorCode.TokenExpired:
                        FirebaseApplication.LogError($@"TokenExpired in request ""{postfix}""");
                        if ((flags & RequestFlags.WaitAuth) != 0)
                        {
                            if (tryCount == 0)
                            {
                                await Task.Delay(500);
                                goto RE_TRY;
                            }
                        }
                        return new ServerResponse(ErrorCode.TokenExpired, null);
                    case ErrorCode.InvalidToken:
                        FirebaseApplication.LogError($@"InvalidToken in request ""{postfix}""");
                        return new ServerResponse(ErrorCode.InvalidToken, null);
                }
            }
            
            if (tryCount > 0)
            {
                tryCount--;
                await Task.Delay(500);
                goto RE_TRY;
            }
            
            if(request == null)
                return new ServerResponse(ErrorCode.NotInternet, null);
            
            var statusCode = (int)request.StatusCode;
            if (statusCode > 501)
                return new ServerResponse(ErrorCode.UnknownError, null);
            
            FirebaseApplication.LogError($"Request failed! Status: {statusCode}; Postfix: {postfix}");
            return new ServerResponse(ErrorCode.UnknownError, null);
        }

        public async Task<ErrorCode> PostRequest(string postfix, JsonGenerator input, RequestFlags flags = RequestFlags.WaitAuth)
        {
            if ((flags & RequestFlags.WaitAuth) != 0 && !_waitAuth.IsSet)
                await _waitAuth.WaitAsync();
            
#if USE_FAKE_SERVER
            ErrorCode resultResponse = default;
            if(FakeResolve(postfix, input, ref resultResponse))
                return resultResponse;
#endif
            
            
            int tryCount;
            if ((flags & RequestFlags.ReTry) != 0)
                tryCount = 3;
            else if ((flags & RequestFlags.InfReTry) != 0)
                tryCount = int.MaxValue;
            else
                tryCount = 0;
            
            if (!_urisCache.TryGetValue(postfix, out var uri))
            {
                uri = new Uri(root + postfix);
                _urisCache[postfix] = uri;
            }

            var httpContent = new StringContent(input.Release());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            HttpResponseMessage request = null;
            
            RE_TRY:
            try
            {
                request = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = httpContent
                }, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception e)
            {
                e = new Exception($@"Failed SendAsync in ""{postfix}""", e);
                Debug.LogException(e);
                FirebaseApplication.LogException(e);
                if (tryCount > 0)
                {
                    tryCount--;
                    await Task.Delay(500);
                    goto RE_TRY;
                }
            }
            
            if (request != null && request.IsSuccessStatusCode)
                return ErrorCode.None;
            
            if (tryCount > 0)
            {
                tryCount--;
                await Task.Delay(500);
                goto RE_TRY;
            }
            
            if(request == null)
                return ErrorCode.NotInternet;
            
            var statusCode = (int)request.StatusCode;
            if (statusCode > 501)
                return ErrorCode.NotInternet;
            
            FirebaseApplication.LogError($"Request failed! Status: {statusCode}; Postfix: {postfix}");
            return ErrorCode.UnknownError;
        }
        
        
        public async Task<ServerResponse> GetRequestWithResult(string postfix, RequestFlags flags = RequestFlags.WaitAuth)
        {
            if (!_urisCache.TryGetValue(postfix, out var uri))
            {
                uri = new Uri(root + postfix);
                _urisCache[postfix] = uri;
            }

            return await GetRequestWithResult(postfix, uri, flags);
        }

        public async Task<ServerResponse> GetRequestWithResult(string postfix, GetParamsGenerator generator, RequestFlags flags = RequestFlags.WaitAuth)
        {
            var uri = generator.Release(postfix);
            return await GetRequestWithResult(postfix, uri, flags);
        }
        
        private async Task<ServerResponse> GetRequestWithResult(string nameApi, Uri uri, RequestFlags flags)
        {
            if ((flags & RequestFlags.WaitAuth) != 0 && !_waitAuth.IsSet)
                await _waitAuth.WaitAsync();
            
#if USE_FAKE_SERVER
            ServerResponse resultResponse = default;
            if(FakeResolveWithResonse(nameApi, uri, ref resultResponse))
                return resultResponse;
#endif
            
            int count = CheckResponseCount;
            var reTry = (flags & RequestFlags.ReTry) != 0;
            
            RE_TRY:
            
            var request = await Client.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
            if (request.IsSuccessStatusCode)
            {
                bool isSuccess = false;
                var content = await request.Content.ReadAsStringAsync();
                JToken result = null;
                try
                {
                    result = JToken.Parse(content);
                    isSuccess = true;
                }
                catch (Exception e)
                {
                    e = new Exception($@"Failed try parse json in API: ""{nameApi}""", e);
                    Debug.LogException(e);
                    FirebaseApplication.LogException(e);
                }
                
                return new ServerResponse(isSuccess ? ErrorCode.None : ErrorCode.BadJson, result);
            }
            
            var responseError = await CheckResponse(request);
            switch (responseError)
            {
                case ErrorCode.TokenExpired:
                    if ((flags & RequestFlags.WaitAuth) != 0)
                    {
                        if(!reTry || count == 0)
                        {
                            await Task.Delay(100);
                            goto RE_TRY;
                        }
                    }
                    return new ServerResponse(ErrorCode.TokenExpired, null);
            }
            
            if (reTry && count > 0)
            {
                count--;
                await Task.Delay(100);
                goto RE_TRY;
            }
            
            var statusCode = (int)request.StatusCode;
            if (statusCode > 501)
                return new ServerResponse(ErrorCode.NotInternet, null);
            
            FirebaseApplication.LogError($"Request failed! Status: {statusCode}; Api: {nameApi}");
            return new ServerResponse(ErrorCode.UnknownError, null);
        }

        public async Task<ErrorCode> GetRequest(string postfix, RequestFlags flags = RequestFlags.WaitAuth)
        {
            if (!_urisCache.TryGetValue(postfix, out var uri))
            {
                uri = new Uri(root + postfix);
                _urisCache[postfix] = uri;
            }
            
            return await GetRequest(postfix, uri, flags);
        }

        public async Task<ErrorCode> GetRequest(string postfix, GetParamsGenerator generator, RequestFlags flags = RequestFlags.WaitAuth)
        {
            return await GetRequest(postfix, generator.Release(postfix), flags);
        }

        public void DeepLink(Uri uri, RequestFlags flags = RequestFlags.WaitAuth)
        {
            GetRequest("deep_link", uri, flags);
        }

        private async Task<ErrorCode> GetRequest(string apiName, Uri uri, RequestFlags flags = RequestFlags.WaitAuth)
        { 
            if ((flags & RequestFlags.WaitAuth) != 0 && !_waitAuth.IsSet)
                await _waitAuth.WaitAsync();
            
#if USE_FAKE_SERVER
            ErrorCode resultResponse = default;
            if(FakeResolve(apiName, uri, ref resultResponse))
                return resultResponse;
#endif
            
            int count = CheckResponseCount;

            var reTry = (flags & RequestFlags.ReTry) != 0;
            RE_TRY:
            
            var result = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (result.IsSuccessStatusCode)
                return ErrorCode.None;

            if (reTry && count > 0)
            {
                count--;
                await Task.Delay(100);
                goto RE_TRY;
            }
            
            var statusCode = (int)result.StatusCode;
            if (statusCode > 501)
                return ErrorCode.NotInternet;

            FirebaseApplication.LogError($"Request failed! Status: {statusCode}; Api: {apiName}");
            return ErrorCode.UnknownError;
        }

        public async Task<ErrorCode> HasPing()
        {
            bool result = false;
            var cancellationTokenSource = new CancellationTokenSource();

            var isComplete = AsyncEventWaitHandle.GetNext();
        
            try
            {
                var request = Client.GetAsync("https://google.com/generate_204", HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
                cancellationTokenSource.CancelAfter(3000);
            
                request.ContinueWith(task =>
                {
                    result = !request.IsCanceled && request.Result.StatusCode == HttpStatusCode.NoContent;
                    _hasAnyInternet = result;
                    isComplete.Set();
                }, TaskContinuationOptions.None);

                await isComplete.WaitAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        
            isComplete.Free();
            return result ? ErrorCode.None : ErrorCode.NotInternet;
        }
    }
}