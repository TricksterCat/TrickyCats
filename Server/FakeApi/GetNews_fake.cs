using System;
using System.Collections.Generic;
using GameRules.Scripts.Server.ServerCore;
using Newtonsoft.Json.Linq;

namespace GameRules.Scripts.Server.FakeApi
{
    public class GetNews_fake : IFakeApi
    {
        public string Postfix => "news";
        private static readonly DateTime NullDate = new DateTime(1970, 1, 1); 
        
        public ErrorCode Resolve(Dictionary<string, string> paramsDic)
        {
            return ErrorCode.None;
        }

        public ServerResponse ResolveWithResponse(Dictionary<string, string> paramsDic)
        {
            return new ServerResponse(ErrorCode.None, new JObject
            {
                {"index", 3},
                {"range", new JArray
                {
                    new JObject
                    {
                        {"date", (new DateTime(2020, 5, 1) - NullDate).TotalSeconds},
                        {"priority", 0},
                        {
                            "body", new JObject
                            {
                                {
                                    "en", new JObject
                                    {
                                        {"title", "С первым маем!"},
                                        {"message", "Сёдня первое мая"}
                                    }
                                }
                            }
                        }
                    },
                    new JObject
                    {
                        {"date", (new DateTime(2020, 5, 2) - NullDate).TotalSeconds},
                        {"priority", 1},
                        {
                            "body", new JObject
                            {
                                {
                                    "en", new JObject
                                    {
                                        {"title", "Первое мая прошло..."},
                                        {"message", "Сёдня уже не персое мая, можно расходиться... Шашлыков не будет. Надеюсь текста хватает на 2 строку... Ещё раз много точек..."}
                                    }
                                }
                            }
                        }
                    },
                    new JObject
                    {
                        {"date", (new DateTime(2099, 12, 31) - NullDate).TotalSeconds},
                        {"priority", 3},
                        {
                            "body", new JObject
                            {
                                {
                                    "en", new JObject
                                    {
                                        {"title", "Завтра кончится столетие!"},
                                        {"message", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."}
                                    }
                                }
                            }
                        }
                    },
                }}
            });
        }
    }
}