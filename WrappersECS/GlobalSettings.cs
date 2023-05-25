using System;
using Firebase.Crashlytics;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using UnityEngine;

namespace GameRules.Scripts.WrappersECS
{
    public static class GlobalSettings
    {
        [Serializable]
        public struct AdsChance_t
        {
            public int BeforeMatch;
            public int AfterMatch;
        }
        
        public static AdsChance_t AdsChance { get; private set; }


        public static void Initialize()
        {
            var adsChanceJson = RemoteConfig.GetString("AdsChance");
            if (!string.IsNullOrEmpty(adsChanceJson))
            {
                try
                {
                    AdsChance = JsonUtility.FromJson<AdsChance_t>(adsChanceJson);
                }
                catch (Exception e)
                {
                    FirebaseApplication.LogException(e);
                    
                    AdsChance = new AdsChance_t
                    {
                        AfterMatch = 100,
                        BeforeMatch = 100
                    };
                }
            }
            else
            {
                AdsChance = new AdsChance_t
                {
                    AfterMatch = 100,
                    BeforeMatch = 100
                };
            }
        }
    }
}