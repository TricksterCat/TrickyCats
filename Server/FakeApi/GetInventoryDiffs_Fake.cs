using System.Collections.Generic;
using GameRules.Scripts.Server.ServerCore;
using Newtonsoft.Json.Linq;

namespace GameRules.Scripts.Server.FakeApi
{
    public class GetInventoryDiffs_Fake : IFakeApi
    {
        public string Postfix => "inventory_diff";
        
        public ErrorCode Resolve(Dictionary<string, string> paramsDic)
        {
            return ErrorCode.None;
        }

        public ServerResponse ResolveWithResponse(Dictionary<string, string> paramsDic)
        {
            return new ServerResponse(ErrorCode.None, new JObject());
        }
    }
}