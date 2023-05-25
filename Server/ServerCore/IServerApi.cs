using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GameRules.Scripts.Server.ServerCore
{
    public struct GetInputLine
    {
        public string Key;
        public string Value;
    }
    
    public struct ServerResponse
    {
        public ErrorCode Status { get; }
        public JToken Response { get; }


        public ServerResponse(ErrorCode status, JToken response)
        {
            Status = status;
            Response = response;
        }
    }

    [Flags]
    public enum RequestFlags
    {
        ReTry = 1 << 0,
        WaitAuth = 1 << 1,
        InfReTry = 1 << 2,
        ReTryWithAuth = ReTry | WaitAuth,
        ReTryInfWithAuth = InfReTry | WaitAuth
    }
    
    public interface IServerApi
    {
        void UpdateAccessToken(string token);
        void UpdateRefreshToken(string token);

        Task<ErrorCode> AuthWithAccess();
        Task<ErrorCode> UpdateAccessWithRefresh();
        
        
        Task<ServerResponse> PostRequestWithResult(string postfix, JsonGenerator input, RequestFlags flags = RequestFlags.WaitAuth);
        Task<ErrorCode> PostRequest(string postfix, JsonGenerator input, RequestFlags flags = RequestFlags.WaitAuth);
        
        
        Task<ServerResponse> GetRequestWithResult(string postfix, RequestFlags flags = RequestFlags.WaitAuth);
        Task<ServerResponse> GetRequestWithResult(string postfix, GetParamsGenerator generator, RequestFlags flags = RequestFlags.WaitAuth);
        
        Task<ErrorCode> GetRequest(string postfix, RequestFlags flags = RequestFlags.WaitAuth);
        Task<ErrorCode> GetRequest(string postfix, GetParamsGenerator generator, RequestFlags flags = RequestFlags.WaitAuth);
        
        void DeepLink(Uri uri, RequestFlags flags = RequestFlags.WaitAuth);

        Task<ErrorCode> HasPing();
    }
}