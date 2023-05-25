using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Analytics;
using Firebase.Extensions;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Server;
using GameRules.Scripts.WrappersECS;
using GameRulez.Modules.PlayerSystems;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Advertisements;
using Random = Unity.Mathematics.Random;

namespace GameRules.Scripts.UI.Results
{
    public class GameResult : MonoBehaviour
#if HAVE_APPODEAL
        , IRewardedVideoAdListener
#endif
    {
        [SerializeField]
        private List<ResultLine> _lines;

        [SerializeField]
        private I2.Loc.LocalizationParamsManager _place;
        [SerializeField]
        private TextMeshProUGUI _score;

        private int _playerScore;
        private int _playerPlace;
        private int _playerDoublePlace;
        
        private bool _isNewRecordIfDouble;

        private bool _haveAdsBonus;

        [SerializeField]
        private CanvasGroup _waitBlock;
        [SerializeField]
        private CanvasGroup _rewards;
        
        [SerializeField]
        private RectTransform _content;
        [SerializeField]
        private Vector2 _sizeContent;

        [SerializeField]
        private RewardBlock _normalRewards;
        [SerializeField]
        private RewardBlock _adsRewards;
        [SerializeField]
        private GameObject _adsRewardsBlock;
        
        [Serializable]
        private struct RewardBlock
        {
            public ResultRewardItem Xp;
            public ResultRewardItem Coins;
            public ResultRewardItem Place;
        }

        private void Awake()
        {
            _waitBlock.alpha = 1f;
            _rewards.alpha = 0f;
            _rewards.blocksRaycasts = false;
            _haveAdsBonus = true;
            
            ADS_ActiveBlock(IronSource.Agent.isRewardedVideoAvailable());
            
            IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += OnOnRewardedVideoAvailabilityChangedEvent;
        }

        private void OnOnRewardedVideoAvailabilityChangedEvent(bool available)
        {
            ADS_ActiveBlock(available);
        }

        private void ADS_ActiveBlock(bool value)
        {
            value &= _haveAdsBonus;
            
            _adsRewardsBlock.SetActive(value);
            var contentSizeDelta = _content.sizeDelta;
            contentSizeDelta.y = value ? _sizeContent.y : _sizeContent.x;
            _content.sizeDelta = contentSizeDelta;
            _content.ForceUpdateRectTransforms();
        }

        private void OnDestroy()
        {
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent -= OnOnRewardedVideoAvailabilityChangedEvent;
            IronSourceEvents.onRewardedVideoAdRewardedEvent -= RewardedVideoAdRewardedEvent;
        }

        private void FillRewardsBlock(RewardBlock block, JObject rewards, out ResultRewardItem item)
        {
            item = null;
                        
            block.Xp.Visible = rewards.TryGetValue("add_exp", out var jXP);
            if (block.Xp.Visible)
            {
                item = block.Xp;
                item.SetValue(jXP.ToString());
                item.HaveNext = false;
            }

            bool coinVisible = false;
            if (rewards.TryGetValue("reward", out var jRewardsToken))
            {
                var jRewards = (JObject)jRewardsToken;
                if (jRewards.TryGetValue("type", out var jTypes) && jTypes.ToString() == "soft")
                {
                    if (item != null)
                        item.HaveNext = true;
                                
                    coinVisible = true;
                    item = block.Coins;
                    item.SetValue(jRewards["value"].ToString());
                    item.HaveNext = false;
                }
            }
            block.Coins.Visible = coinVisible;
        }
        
        
        public void Show(IList<KeyValuePair<Team, int>> scores, Team playerTeam, int scorePlayer, int teamSize)
        {
            int doubleScore = scorePlayer * 2;
            _playerDoublePlace = int.MaxValue;

            for (var i = 0; i < scores.Count; i++)
            {
                if (scores[i].Value > doubleScore) 
                    continue;
                _playerDoublePlace = i + 1;
                break;
            }
            
            for (var i = 0; i < scores.Count; i++)
            {
                if (scores[i].Key == playerTeam)
                    _playerPlace = i + 1;
            }
            
            _place.SetParameterValue("VALUE", _playerPlace.ToString());
            _score.text = $"<color=#FFF000FF>x</color> {scorePlayer}";

            _playerDoublePlace = math.min(_playerPlace, _playerDoublePlace);

            ServerRequest.Instance.PreviewScore(scorePlayer, _playerPlace, _playerDoublePlace).ContinueWithOnMainThread(task =>
            {
                var result = task.Result;
                if (result.Status == ErrorCode.None && result.Response is JObject json)
                {
                    if (json.TryGetValue("normal", out var normalJsonToken))
                        FillRewardsBlock(_normalRewards, (JObject)normalJsonToken, out _);
                    
                    if (json.TryGetValue("with_ads", out var adsJsonToken))
                    {
                        FillRewardsBlock(_adsRewards, (JObject)adsJsonToken, out var item);
                        if (_playerPlace != _playerDoublePlace)
                        {
                            if (item != null)
                                item.HaveNext = true;
                            _adsRewards.Place.Visible = true;
                            _adsRewards.Place.SetValue(_playerDoublePlace.ToString());
                        }
                        else
                        {
                            _adsRewards.Place.Visible = false;
                        }
                    }
                    else
                        _haveAdsBonus = false;
                }
                else
                {
                    _haveAdsBonus = false;
                    
                    _normalRewards.Xp.Visible = true;
                    _normalRewards.Xp.SetValue("?");
                    _normalRewards.Xp.HaveNext = true;
                    
                    _normalRewards.Coins.Visible = true;
                    _normalRewards.Coins.SetValue("?");
                }
                
                if(!_haveAdsBonus)
                    ADS_ActiveBlock(false);
                
                _waitBlock.alpha = 0f;
                _rewards.alpha = 1f;
                _rewards.blocksRaycasts = true;
            });
            
            FirebaseAnalytics.SetCurrentScreen("game_result", null);
            FirebaseAnalytics.SetUserProperty("PlayGamesCount", GetOrPush.PlayGames.ToString());

            var completeGameEvent = CrowdAnalyticsMediator.Instance.BeginEvent("complete_match");
            var newRecord = scorePlayer > GetOrPush.BestScore;
            
            completeGameEvent.AddField("new_best_score", newRecord);
            if (newRecord)
                GetOrPush.BestScore = scorePlayer;
            else
                _isNewRecordIfDouble = scorePlayer * 2 > GetOrPush.BestScore;
            
            _playerScore = scorePlayer;
            
            var index = 0;
            var maxIndex = _lines.Count;

            var firstScore = scores[0];
            index = 0;
            while (index < maxIndex)
            {
                var score = scores[index];
                var isMain = score.Key == playerTeam;
                
                var line = _lines[index++];
                if (score.Key == null)
                    break;
                var width = index == 1 ? -1 : ((float) score.Value / firstScore.Value) *
                            ((RectTransform) _lines[0].transform).rect.width;
                    
                line.SetValues(width, score.Key.PlayerColor, score.Key.Name, score.Value, score.Key.Skin.Icon);
                line.UpdateTextPlace(score.Key.PlayerColor, isMain);
                line.gameObject.SetActive(true);
            }
            
            completeGameEvent.AddField("team_size", teamSize);
            completeGameEvent.AddField("score", scorePlayer);
            completeGameEvent.AddField("skin", Modules.Database.Inventory.PlayerSkin.Value.Id);
            completeGameEvent.AddField("position", _playerPlace);
            
            completeGameEvent.CompleteBuild();

            while (index < maxIndex)
                _lines[index++].gameObject.SetActive(false);
        }

        public void DoubleScore()
        {
            if(!_haveAdsBonus)
                return;
            _haveAdsBonus = false;
            
            var ironSourceAgent = IronSource.Agent;
            if (ironSourceAgent.isRewardedVideoAvailable())
                ironSourceAgent.showRewardedVideo("DoubleScore");
        }

        public void ToHome(bool isDouble)
        {
            var score = _playerScore;
            
            if (isDouble)
            {
                score *= 2;
                if (score > GetOrPush.BestScore)
                    GetOrPush.BestScore = score;
            }

            GetOrPush.TotalPosition += _playerPlace;
            GetOrPush.TotalPositionWithAdBonus += _playerDoublePlace;

            GetOrPush.IncrementScore(score);

            GameTuneImpl.GameTuneManager.GameComplete();
            
            try
            {
                ServerRequest.Instance.UpdateScore(score, isDouble ? _playerDoublePlace : _playerPlace, isDouble);
            }
            catch
            {
            }
            
            if (isDouble)
            {
                var doubleScoreEvent = CrowdAnalyticsMediator.Instance.BeginEvent("double_score");
                
                doubleScoreEvent.AddField("score", score);
                doubleScoreEvent.AddField("new_record", _isNewRecordIfDouble);
                
                doubleScoreEvent.CompleteBuild();
            }

            LoadingController.TryShowInterstitialAd("Level_Complete", out var adStatus);
            
            var toHomeEvent = CrowdAnalyticsMediator.Instance.BeginEvent("match_to_home");

            if (adStatus != -1)
                toHomeEvent.AddField("ads_show", adStatus);
                        
            toHomeEvent.CompleteBuild();
            
            LoadingController.ToMainMenu();
        }
        
        
        private void RewardedVideoAdRewardedEvent(IronSourcePlacement placement)
        {
            if(placement.getPlacementName() != "DoubleScore")
                return;

            ToHome(true);
        }
    }
}
