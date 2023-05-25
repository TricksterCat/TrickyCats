using System.Collections.Generic;
using System.Linq;
using GameRules.Firebase.Runtime;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.WrappersECS;
using Newtonsoft.Json.Linq;
using UnityEngine.GameTune;

namespace GameRules.Scripts.GameTuneImpl
{
    public static class GameTuneManager
    {
        public static bool IsEnable { get; private set; }
        public static bool IsTestMode { get; private set; }
        public static bool IsAdOptimize { get; private set; }

        private static Dictionary<string, bool[]> _ad_variables = new Dictionary<string, bool[]>(16);
        private static Dictionary<string, AdPlacement> _adPlacements = new Dictionary<string, AdPlacement>(2);
        
        public static int IAP_reward_value { get; private set; }
        private static UserAttributesProvider _attributesProvider;
        
        private class UserAttributesProvider : IUserAttributesProvider 
        {
            private Dictionary<string, object> _userAttributes = new Dictionary<string, object>(16);

            public Dictionary<string, object> Dic => _userAttributes;
            
            public Dictionary<string, object> GetUserAttributes()
            {
                FillGameScore();
                FillUserInfo();
                FillWalletInfo();
                return _userAttributes;
            }

            public void Update(string key)
            {
                if(_userAttributes.TryGetValue(key, out var value))
                    GameTune.SetUserAttribute(key, value);
            }

            public void Update(string key, object value)
            {
                _userAttributes[key] = value;
                GameTune.SetUserAttribute(key, value);
            }
        }
       
        public static void Initialize()
        {
            _attributesProvider = new UserAttributesProvider();
            
            IsEnable = RemoteConfig.GetBool("GameTune_enable");
            IsTestMode = RemoteConfig.GetBool("GameTune_testMode");
            IsAdOptimize = RemoteConfig.GetBool("GameTune_optimize_ad");

            IAP_reward_value = RemoteConfig.GetInt("GameTune_iap_reward");

            var adVariablesKeys = TmpList<string>.Get();
            var adVariablesJsonStr =RemoteConfig.GetString("ad_variable", string.Empty);
            if (!string.IsNullOrWhiteSpace(adVariablesJsonStr))
            {
                var jAdVariables = JObject.Parse(adVariablesJsonStr);
                foreach (var adVariable in jAdVariables)
                {
                    var values = (JArray) adVariable.Value;
                    var valuesBool = new bool[values.Count];
                    for (int i = 0; i < valuesBool.Length; i++)
                        valuesBool[i] = (int)values[i] == 1;

                    _ad_variables[adVariable.Key] = valuesBool;
                    adVariablesKeys.Add(adVariable.Key);
                }
            }
            
            InitializeOptions options = new InitializeOptions();
            options.SetUserId(ServerRequest.DeviceId);
            options.SetPrivacyConsent(GetOrPush.GDPR_consent == 1);
            options.SetTestMode(IsTestMode);
            options.SetGameTuneOff(!IsEnable);
            
            FillGameScore();
            
            GameTune.Initialize("bee0903d-48df-4cae-9169-1bda6282fae2", options, userAttributesProvider: _attributesProvider);

            AdPlacement beforeGame;
            AdPlacement afterGame;
            
            if (IsAdOptimize)
            {
                var alt = TmpList<string>.ReleaseAndToArray(adVariablesKeys);

                beforeGame = new AdPlacement("ad_before_game", alt, GlobalSettings.AdsChance.BeforeMatch, RemoteConfig.GetInt("AdsBeforeGame"));
                afterGame = new AdPlacement("ad_after_game", alt, GlobalSettings.AdsChance.AfterMatch, RemoteConfig.GetInt("AdsAfterGame"));
            }
            else
            {
                beforeGame = new AdPlacement("ad_before_game", new string[0], GlobalSettings.AdsChance.BeforeMatch, RemoteConfig.GetInt("AdsBeforeGame"));
                afterGame = new AdPlacement("ad_after_game", new string[0], GlobalSettings.AdsChance.AfterMatch, RemoteConfig.GetInt("AdsAfterGame"));
            }
            
            _adPlacements["Level_Complete"] = afterGame;
            _adPlacements["Level_Start"] = beforeGame;
            
            if (IsAdOptimize)
                GameTune.AskQuestions(beforeGame.ShowAdQuestion, afterGame.ShowAdQuestion);
        }

        public static bool IsShowAd(string placement)
        {
            return _adPlacements.TryGetValue(placement, out var adPlacement) && adPlacement.IsShow();
        }

        public static bool RequiredAd(string placement)
        {
            return _adPlacements.TryGetValue(placement, out var adPlacement) && adPlacement.IsRequired();
        }

        public static void CompleteInterstitialAd()
        {
            if(!IsEnable)
                return;
            
            var interstitialAttributes = new Dictionary<string, object> { { "value", 1 } };
            GameTune.RewardEvent("interstitial_ad_watched", interstitialAttributes);
        }
        
        public static void CompleteRewardedAd()
        {
            if(!IsEnable)
                return;
            
            var interstitialAttributes = new Dictionary<string, object> { { "value", 1.5 } };
            GameTune.RewardEvent("rewarded_ad_watched", interstitialAttributes);
        }
        
        public static void CompleteIAP ()
        {
            if(!IsEnable)
                return;
            
            var interstitialAttributes = new Dictionary<string, object> { { "value", IAP_reward_value } };
            GameTune.RewardEvent("purchased", interstitialAttributes);
        }

        public static void GameComplete()
        {
            if(!IsEnable)
                return;
            
            FillGameScore();
            
            _attributesProvider.Update("arg_position");
            _attributesProvider.Update("arg_positionWithAd");
            _attributesProvider.Update("play_games");
            _attributesProvider.Update("arg_score");
        }
        
        public static void LevelUp()
        {
            if(!IsEnable)
                return;
            
            FillUserInfo();
            _attributesProvider.Update("level");
            _attributesProvider.Update("total_crowd");
            _attributesProvider.Update("items_count");
        }

        public static void UpdateParam(string key, object value)
        {
            _attributesProvider.Update(key, value);
        }
        
        private static void FillWalletInfo()
        {
            var dic = _attributesProvider.Dic;
            dic["soft_w"] = Inventory.SoftWallet.Value;
            dic["hard_w"] = Inventory.HardWallet.Value;
            dic["spin_w"] = Inventory.RouletteTickets.Value;
        }
        
        private static void FillUserInfo()
        {
            var dic = _attributesProvider.Dic;
            dic["level"] = GetOrPush.Level;
            dic["total_crowd"] = GetOrPush.TotalPositionWithAdBonus;
            dic["items_count"] = Inventory.ItemsCount;
            dic["spin_count"] = GetOrPush.RouletteSpins;
            dic["referral_count"] = GetOrPush.Referral;
        }

        private static void FillGameScore()
        {
            var dic = _attributesProvider.Dic;
            if (GetOrPush.PlayGames > 0)
            {
                dic["arg_position"] = (float) GetOrPush.TotalPosition / GetOrPush.PlayGames;
                dic["arg_positionWithAd"] = (float)GetOrPush.TotalPositionWithAdBonus / GetOrPush.PlayGames;
            }

            dic["play_games"] = GetOrPush.PlayGames;
            dic["arg_score"] = GetOrPush.ArgScore;
        }
        
        private class AdPlacement
        {
            public Question ShowAdQuestion { get; }

            private int _chance;
            private int _requiredPlayGame;
            private Queue<bool> _showAdQueue = new Queue<bool>(10);

            public AdPlacement(string name, string[] alt, int chance, int requiredPlayGame)
            {
                _requiredPlayGame = requiredPlayGame;
                _chance = chance;
                if(alt.Length != 0)
                    ShowAdQuestion = GameTune.CreateQuestion(name, alt, AnswerType.ALWAYS_NEW, OnGetAnswer);
            }

            private void OnGetAnswer(Answer answer)
            {
                var result = _ad_variables[answer.Value];
                for (int i = 0; i < result.Length; i++)
                    _showAdQueue.Enqueue(result[i]);
                answer.Use();
            }

            public bool IsShow()
            {
                if (_showAdQueue.Count == 0)
                    return UnityEngine.Random.Range(0, 100) > _chance;

                if (_showAdQueue.Count == 1 && ShowAdQuestion != null)
                    GameTune.AskQuestions(ShowAdQuestion);
            
                return _showAdQueue.Dequeue();
            }

            public bool IsRequired()
            {
                return GetOrPush.PlayGames >= _requiredPlayGame;
            }
        }

    }
}