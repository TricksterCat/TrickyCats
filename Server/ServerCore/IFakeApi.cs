using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameRules.Scripts.Server.ServerCore
{
    public interface IFakeApi
    {
        string Postfix { get; }
        
        ErrorCode Resolve(Dictionary<string, string> paramsDic);
        ServerResponse ResolveWithResponse(Dictionary<string, string> paramsDic);
    }

    public static class IFakeApiExtensions
    {
        public static Dictionary<string, string> ToParams(this Uri uri)
        {
            var stringUri = uri.ToString();
            var start = stringUri.IndexOf('?');
            if (start > 0)
            {
                stringUri = stringUri.Remove(start);
                var pairs = stringUri.Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string> dic = new Dictionary<string, string>(pairs.Length);
                if (pairs.Length > 0)
                {
                    dic = new Dictionary<string, string>(pairs.Length);
                    for (int i = 0; i < pairs.Length; i++)
                    {
                        var kvp = pairs[i].Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                        if (kvp.Length > 1)
                            dic[kvp[0]] = kvp[1];
                    }
                }
                else
                {
                    dic = new Dictionary<string, string>(1);
                    var kvp = stringUri.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                    if (kvp.Length > 1)
                        dic[kvp[0]] = kvp[1];
                }

                return dic;
            }
            return new Dictionary<string, string>();
        }
        
        public static Dictionary<string, string> ToParams(this JsonGenerator generator)
        {
            var json = generator.Release();
            var jDic = JObject.Parse(json);

            var dic = new Dictionary<string, string>(jDic.Count);
            
            foreach (var kvp in jDic)
                dic[kvp.Key] = kvp.Value.ToString(Formatting.None);
            
            return dic;
        }
    }
}