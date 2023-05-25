using System.Collections.Generic;
using GameRules.Scripts.Server.ServerCore;
using Newtonsoft.Json.Linq;

namespace GameRules.Scripts.Server.FakeApi
{
    public class NewSession_Fake : IFakeApi
    {
        public string Postfix => "auth/session";
        
        public ErrorCode Resolve(Dictionary<string, string> paramsDic)
        {
            return ErrorCode.None;
        }

        public ServerResponse ResolveWithResponse(Dictionary<string, string> paramsDic)
        {
            return new ServerResponse(ErrorCode.None, new JObject());
        }
    }
    
    public class SyncInventory_Fake : IFakeApi
    {
        public string Postfix => "inventory";
        
        public ErrorCode Resolve(Dictionary<string, string> paramsDic)
        {
            return ErrorCode.None;
        }

        public ServerResponse ResolveWithResponse(Dictionary<string, string> paramsDic)
        {
            return new ServerResponse(ErrorCode.None, new JObject()
            {
                {"index", -1},
                {"diffs", new JArray()},
                {"items", new JArray()},
                {"wallets", new JObject
                {
                    {"soft", 0},
                    {"hard", 0}
                }}
            });
        }
    }
}