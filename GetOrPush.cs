using System;
using System.Collections.Generic;
using System.Globalization;
using GameRules.Firebase.Runtime;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.GameTuneImpl;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.GameTune;

namespace GameRules
{
    public static class GetOrPush
    {
        public const string iOS_AppId = "1518787204";

        private static Dictionary<string, JObject> _mapConfigs;

        public static void Load()
        {
            _levelSteps = new int[4];
            _levelSteps[0] = RemoteConfig.GetInt("LevelStep_1", 0);
            _levelSteps[1] = RemoteConfig.GetInt("LevelStep_2", 0);
            _levelSteps[2] = RemoteConfig.GetInt("LevelStep_3", 0);
            _levelSteps[3] = RemoteConfig.GetInt("LevelStep_4", 0);
            
            _mapConfigs = new Dictionary<string, JObject>(16);

            var mapConfigs = RemoteConfig.GetString("maps_config");
            if (string.IsNullOrEmpty(mapConfigs))
            {
                try
                {
                    var mapConfigsJson = JObject.Parse(mapConfigs);
                    foreach (var map in mapConfigsJson)
                    {
                        if(map.Value is JObject mapConfig)
                            _mapConfigs[map.Key] = mapConfig;
                    }
                }
                catch (Exception e)
                {
                    FirebaseApplication.LogException(e);
                }
            }
        }

        public static bool TryGetConfig(string mapName, out JObject mapConfig)
        {
            if (_mapConfigs == null)
            {
                mapConfig = null;
                return false;
            }
            return _mapConfigs.TryGetValue(mapName, out mapConfig);
        }
        
        public enum AdsType
        {
            AdsEnable = 0,
            AdsDisable = 1,
        }
        
        public static int TotalPosition
        {
            get => PlayerPrefs.GetInt(nameof(TotalPosition), 0);
            set => PlayerPrefs.SetInt(nameof(TotalPosition), value);
        }
        
        public static int TotalPositionWithAdBonus
        {
            get => PlayerPrefs.GetInt(nameof(TotalPositionWithAdBonus), 0);
            set => PlayerPrefs.SetInt(nameof(TotalPositionWithAdBonus), value);
        }
        
        public static int PlayGames
        {
            get => PlayerPrefs.GetInt(nameof(PlayGames), 0);
            set => PlayerPrefs.SetInt(nameof(PlayGames), value);
        }

        public static int PlayGamesStarted
        {
            get => PlayerPrefs.GetInt(nameof(PlayGamesStarted), 0);
            set => PlayerPrefs.SetInt(nameof(PlayGamesStarted), value);
        }

        public static int TotalCrowdSize => Inventory.Crowd.Value + CrowdAdBonus() + CrowdReferalBonus();
        public static int TotalCrowdSizeWithoutAd => Inventory.Crowd.Value + CrowdReferalBonus();
        
        public static int AdCrowdBonus { get; set; }

        public static int CrowdAdBonus()
        {
            return AdCrowdBonus;
        }

        public static int CrowdReferalBonus()
        {
            return math.clamp(Referral, 0, 10);
        }

        public static int BestScore
        {
            get => PlayerPrefs.GetInt(nameof(BestScore), 0);
            set => PlayerPrefs.SetInt(nameof(BestScore), value);
        }
        
        public static float ArgScore 
        {
            get => PlayerPrefs.GetFloat(nameof(ArgScore), 0);
            private set => PlayerPrefs.SetFloat(nameof(ArgScore), value);
        }

        public static void IncrementScore(float result)
        {
            var total = PlayerPrefs.GetFloat("total_score");
            var total_count = PlayerPrefs.GetInt("total_score_count");

            total += result;
            total_count++;

            if (total > float.MaxValue / 2 && total_count % 4 == 0)
            {
                total /= 4;
                total_count /= 4;
            }
            
            PlayerPrefs.SetFloat("total_score", total);
            PlayerPrefs.SetInt("total_score_count", total_count);
            
            ArgScore = total / total_count;
        }
        
        
        public static ChangeProperty<int> Xp { get; } = new ChangeProperty<int>();
        
        public static ChangeProperty<int> XpToNext{ get; } = new ChangeProperty<int>();

        private static ChangeProperty<int> _level;

        public static ChangeProperty<int> Level
        {
            get
            {
                if (_level != null)
                    return _level;
                _level = new ChangeProperty<int>();
                _level.Value = PlayerPrefs.GetInt("user_level", 1);
                _level.OnChange += value => PlayerPrefs.SetInt("user_level", value);
                return _level;
            }
        }

        private static int[] _levelSteps;
        public static void UpdateLevel(int level, bool checkLevelUp = true)
        {
            try
            {
                if (checkLevelUp && level != Level.Value)
                {
                    if (_levelSteps != null)
                    {
                        for (int i = 0; i < _levelSteps.Length; i++)
                        {
                            if (_levelSteps[i] == level)
                            {
                                CrowdAnalyticsMediator.Instance.BeginEvent("level_step_"+(i+1))
                                    .SetLevel(level)
                                    .CompleteBuild();
                                break;
                            }
                        }
                    }
                    
                    CrowdAnalyticsMediator.Instance.LevelUp(level);
                    GameTuneManager.LevelUp();
                }
            }
            catch (Exception e)
            {
                FirebaseApplication.LogException(e);
            }

            Level.Value = level;
        }
        
        public static string UserName
        {
            get => PlayerPrefs.GetString(nameof(UserName), string.Empty);
            set => PlayerPrefs.SetString(nameof(UserName), value.Trim());
        }

        private static DateTime? _lowPriceNoAdsDateTime;
        public static DateTime LowPriceNoAdsDateTime
        {
            get
            {
                if (_lowPriceNoAdsDateTime == null)
                {
                    var time = PlayerPrefs.GetString(nameof(LowPriceNoAdsDateTime), string.Empty);
                    if (DateTime.TryParse(time, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal, out var timeValue))
                        _lowPriceNoAdsDateTime = timeValue;
                    else
                        _lowPriceNoAdsDateTime = new DateTime(1970, 1, 1);
                }

                return _lowPriceNoAdsDateTime.Value;
            }
            set
            {
                PlayerPrefs.SetString(nameof(LowPriceNoAdsDateTime), value.ToString("u", DateTimeFormatInfo.InvariantInfo));
                _lowPriceNoAdsDateTime = value;
            }
        }
        
        public static int GDPR_consent
        {
            get => PlayerPrefs.GetInt(nameof(GDPR_consent), -1);
            set => PlayerPrefs.SetInt(nameof(GDPR_consent), value);
        }
        
        public static int GameRunCount
        {
            get => PlayerPrefs.GetInt(nameof(GameRunCount), 0);
            set => PlayerPrefs.SetInt(nameof(GameRunCount), value);
        }

        public static AdsType AdsCurrentType
        {
            get => (AdsType)PlayerPrefs.GetInt(nameof(AdsType), 0);
            set => PlayerPrefs.SetInt(nameof(AdsType), (int)value);
        }
        
        public static bool IsAdsActive => AdsCurrentType == AdsType.AdsEnable;

        public static bool UnlockedAllSkins
        {
            get => PlayerPrefs.GetInt(nameof(UnlockedAllSkins), 0) == 1;
            set => PlayerPrefs.SetInt(nameof(UnlockedAllSkins), value ? 1 : 0);
        }
        
        public static bool AdsBonusActive
        {
            get; set;
        }

        public static bool HighQuality
        {
            get => PlayerPrefs.GetInt(nameof(HighQuality), SystemInfo.graphicsShaderLevel >= 35 && SystemInfo.systemMemorySize > 1000 && SystemInfo.processorCount > 2 ? 1 : 0) == 1;
            set => PlayerPrefs.SetInt(nameof(HighQuality), value ? 1 : 0);
        }

        public static string VersionFirst
        {
            get => PlayerPrefs.GetString(nameof(VersionFirst), "DarkAges");
            set => PlayerPrefs.SetString(nameof(VersionFirst), value);
        }
        
        public static string VersionLast
        {
            get => PlayerPrefs.GetString(nameof(VersionLast), "DarkAges");
            set => PlayerPrefs.SetString(nameof(VersionLast), value);
        }

        public static int Referral
        {
            get => PlayerPrefs.GetInt(nameof(Referral), 0);
            set => PlayerPrefs.SetInt(nameof(Referral), value);
        }

        public static int LastMatchResult
        {
            get => PlayerPrefs.GetInt(nameof(LastMatchResult), 0);
            set => PlayerPrefs.SetInt(nameof(LastMatchResult), value);
        }

        public static int LastGameLaunch
        {
            get => PlayerPrefs.GetInt(nameof(LastGameLaunch), 0);
            set => PlayerPrefs.SetInt(nameof(LastGameLaunch), value);
        }

        public static string RefCode
        {
            get => PlayerPrefs.GetString(nameof(RefCode), string.Empty);
            set => PlayerPrefs.SetString(nameof(RefCode), value);
        }
        
        public static string RefCodeRev
        {
            get => PlayerPrefs.GetString(nameof(RefCodeRev), string.Empty);
            set => PlayerPrefs.SetString(nameof(RefCodeRev), value);
        }
        
        public static bool CanShowRateUs
        {
            get => PlayerPrefs.GetInt(nameof(CanShowRateUs), 1) == 1;
            set => PlayerPrefs.SetInt(nameof(CanShowRateUs), value ? 1 : 0);
        }

        public static int RouletteSpins { get; set; }
        
        public static string AuthMode
        {
            get => PlayerPrefs.GetString(nameof(AuthMode), string.Empty);
            set => PlayerPrefs.SetString(nameof(AuthMode), value);
        }

        public static string ForceDeviceId 
        {
            get => PlayerPrefs.GetString(nameof(ForceDeviceId), string.Empty);
            set => PlayerPrefs.SetString(nameof(ForceDeviceId), value);
        }

        public static bool RemoveAll { get; set; }

        public static void UpdateRefCode(string newRefCode, string rev)
        {
            RefCode = newRefCode;
            RefCodeRev = rev;
            ServerRequest.Instance.UpdateRefCode(newRefCode);
        }
        
        public static bool HasSales(string id)
        {
            switch (id)
            {
                case "com.rustygames.tc.none_ads.forever":
                    var sec = (LowPriceNoAdsDateTime - DateTime.UtcNow).TotalSeconds;
                    return sec > 0 && sec < TimeSpan.FromDays(2).TotalSeconds;
            }
            return false;
        }

        public static int TimeLeftForSale(string id)
        {
            switch (id)
            {
                case "com.rustygames.tc.none_ads.forever":
                    var sec = (LowPriceNoAdsDateTime - DateTime.UtcNow).TotalSeconds;
                    return math.max((int)sec, 0);
                default:
                    return 0;
            }
        }
    }
}