using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using Firebase.Extensions;
using Firebase.Messaging;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Server;
using GameRules.Scripts.Server.ServerCore;
using GameRules.UI;
using I2.Loc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using InventoryLogic = GameRules.Scripts.Modules.Database.Inventory;

namespace GameRules.Scripts.UI.WheelOfFortune
{
    public class WheelOfFortuneWindow : MonoBehaviour
    {
        [SerializeField]
        private ShowController _showController;
        [SerializeField]
        private WheelOfFortuneLogic _wheelOfFortuneLogic;
        
        [BoxGroup("Buy"), SerializeField, BoxGroup("Buy/From soft")]
        private Button _buyFromSoftBtn;
        [SerializeField, BoxGroup("Buy/From soft")]
        private TextMeshProUGUI _buyFromSoftPrice;
        [SerializeField, BoxGroup("Buy/From soft")]
        private CanvasGroup _buyFromSoftPriceBox;
        [SerializeField, BoxGroup("Buy/From soft")]
        private CanvasGroup _buyFromSoftAreOver;
        
        [SerializeField, BoxGroup("Buy/From hard")]
        private Button _buyFromHardBtn;
        [SerializeField, BoxGroup("Buy/From hard")]
        private TextMeshProUGUI _buyFromHardPrice;
        
        [SerializeField, BoxGroup("Buy/From ads")]
        private Button _buyFromAdsBtn;
        [SerializeField, BoxGroup("Buy/From ads")]
        private CanvasGroup _buyFromAdsCanUse;
        [SerializeField, BoxGroup("Buy/From ads")]
        private CanvasGroup _buyFromAdsDisable;
        [SerializeField, BoxGroup("Buy/From ads")]
        private TextMeshProUGUI _buyFromAdsDisableText;
        
        
        [SerializeField]
        private Button _rotateBtn;
        [SerializeField]
        private TextMeshProUGUI _ticketCount;
        [SerializeField]
        private TextMeshProUGUI _reloadTimer;
        
        private JArray _data;

        private readonly ChangeProperty<bool> _isProcess = new ChangeProperty<bool>();
        public readonly ChangeProperty<bool> WaitRewardDiff = new ChangeProperty<bool>();
        private readonly ChangeProperty<bool> _waitNextRewards = new ChangeProperty<bool>();
        
        private int _buyFromSoftIndex;
        private double _buyFromSoftReset;
        
        private double _buyFromAdsResetTime;
        private float _buyFromAdsResetTimeCooldown; //RemoteConfig: TicketBuyFromAdsCooldown
        private bool _buyFromAdsIsAvailability;
        
        private int _secondLeft;
        
        private float _lastReloadTimerComplete;
        private int _endReloadTimer;

        private JObject _lastReward;
        private JObject _nextReward;

        public static readonly ChangeProperty<bool> UpdateWheel = new ChangeProperty<bool>();
        public static readonly ChangeProperty<bool> UpdateWheelConf = new ChangeProperty<bool>();

        public bool CanRotate => _rotateBtn.interactable;

        [SerializeField]
        private TabBtn _shopTab;
        

        private double GetUtc()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }
        
        [Button, BoxGroup("Debug")]
        private void TicketFromAdsComplete()
        {
            BuyTicket("ads");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnWheelInitialize()
        {
            FirebaseApplication.MessageReceived -= OnMessageReceived;
            FirebaseApplication.MessageReceived += OnMessageReceived;
        }
        
        private static void OnMessageReceived(MessageReceivedEventArgs obj)
        {
            var message = obj.Message;

            if (message.Data.TryGetValue("type", out var type))
            {
                switch (type)
                {
                    case "roulette_wheel_update":
                        UpdateWheel.Value = true;
                        break;
                    case "roulette_conf_change":
                        ServerRequest.Instance.SyncFortuneConfig().ContinueWithOnMainThread(task =>
                        {
                            UpdateWheelConf.Value = true;
                        });
                        break;
                }
            }
        }
        
        private void Awake()
        {
            _endReloadTimer = int.MinValue;
            _secondLeft = int.MaxValue;
            
            _reloadTimer.text = "...";

            LoadSave();
            
            LoadBuyFromSoft();
            LoadBuyFromHard();
            LoadBuyFromAds();

            WaitRewardDiff.Value = false;
            WaitRewardDiff.OnChange += value =>
            {
                _wheelOfFortuneLogic.WaitReward = value;
                if (value)
                    InventoryHistory.RewardWithReasonEvent += FreeCanRotate;
                else
                    InventoryHistory.RewardWithReasonEvent -= FreeCanRotate;
                
                if (_showController.CurrentState == ShowController.StateAnimation.Show)
                    RouletteTicketsOnOnChange(InventoryLogic.RouletteTickets.Value);
            };

            _isProcess.Value = false;
            _isProcess.OnChange += value =>
            {
                RouletteTicketsOnOnChange(InventoryLogic.RouletteTickets.Value);
            };

            _waitNextRewards.OnChange += value =>
            {
                if (value)
                    DOTween.Restart("LoadingNextFortune");
                else
                    DOTween.Rewind("LoadingNextFortune");
                
                if (_showController.CurrentState == ShowController.StateAnimation.Show)
                    RouletteTicketsOnOnChange(InventoryLogic.RouletteTickets.Value);
            };

            if (_lastReward != null && !UpdateWheel.Value)
            {
                _nextReward = _lastReward;
                UnpackNextReward();
            }
            else
                RequestNextReward();
            
            UpdateWheel.OnChange += UpdateWheelOnChange;
            UpdateWheelConf.OnChange += UpdateWheelConfOnChange;
        }

        private void UpdateWheelConfOnChange(bool value)
        {
            if (!value) 
                return;
            
            if (_showController.CurrentState == ShowController.StateAnimation.Show)
            {
                UpdateBuyFromSoft(InventoryLogic.SoftWallet.Value);
                UpdateBuyFromHard(InventoryLogic.HardWallet.Value);
            }
                
            UpdateWheelConf.Value = false;
        }

        private void UpdateWheelOnChange(bool value)
        {
            if (value)
                RequestNextReward();
        }

        private void RequestNextReward()
        {
            if (this == null || this.Equals(null))
                return;
            
            UpdateWheel.Value = false;
            _waitNextRewards.Value = true;
            ServerRequest.Instance.GetNextFortuneRewards()
                .ContinueWithOnMainThread(result =>
                {
                    var json = result.Result;
                    if (json == null)
                    {
                        Task.Delay(1000).ContinueWithOnMainThread(task => RequestNextReward());
                        return;
                    }
                    
                    _nextReward = json;
                });
        }

        private void UnpackNextReward()
        {
            var json = _nextReward;
            if(json == null)
                return;

            if (json.TryGetValue("shuffleAt", out var shuffleAt))
                _lastReloadTimerComplete = (float)shuffleAt / 1000;

            if (json.TryGetValue("lastBuy", out var lastBuy))
            {
                var lastBuyData = (JObject)lastBuy;

                if (lastBuyData.TryGetValue("soft", out var lastSoftBuy))
                {
                    _buyFromSoftReset = (double) lastSoftBuy;

                    if (DateTime.UtcNow - new DateTime(1970, 1, 1).AddMilliseconds((double) lastSoftBuy) >
                        TimeSpan.FromDays(1))
                    {
                        _buyFromSoftIndex = 0;
                        UpdateBuyFromSoft(InventoryLogic.SoftWallet.Value);
                    }
                }
                else
                {
                    _buyFromSoftIndex = 0;
                    UpdateBuyFromSoft(InventoryLogic.SoftWallet.Value);
                }
            }

            if (json.TryGetValue("lastBuyCounters", out var jLastBuyCounters) && jLastBuyCounters["soft"] != null)
            {
                _buyFromSoftIndex = (int)jLastBuyCounters["soft"];
                UpdateBuyFromSoft(InventoryLogic.SoftWallet.Value);
            }
            
            _wheelOfFortuneLogic.Draw((JArray)json["rewards"]);
            _lastReward = _nextReward;
            _nextReward = null;
            
            _waitNextRewards.Value = false;
        }

        private void FreeCanRotate(string reason)
        {
            if(reason != "fortune")
                return;

            WaitRewardDiff.Value = false;
        }
        
        private void OnDestroy()
        {
            UpdateWheel.OnChange -= UpdateWheelOnChange;
            
            InventoryLogic.SoftWallet.OnChange -= UpdateBuyFromSoft;
            InventoryHistory.RewardWithReasonEvent -= FreeCanRotate;

            Save();
        }
        
        private void LoadSave()
        {
            try
            {
                var json = JObject.Parse(PlayerPrefs.GetString("FortuneReloads", @"{""softReload"":0,""adsReload"":0}"));
                _buyFromSoftReset = (double)json["softReload"];
                _buyFromAdsResetTime = (double)json["adsReload"];
            }
            catch (Exception e)
            {
                FirebaseApplication.LogException(e);
            }
        }
        
        private void Save()
        {
            PlayerPrefs.SetString("FortuneReloads", new JObject
            {
                {"softReload", _buyFromSoftReset},
                {"adsReload", _buyFromAdsResetTime}
            }.ToString(Formatting.None));
        }

        private void Update()
        {
            if(_showController.CurrentState != ShowController.StateAnimation.Show)
                return;

            if (_waitNextRewards.Value)
                UnpackNextReward();

            double utc = GetUtc();
            if (!_buyFromAdsBtn.interactable)
            {
                if (_buyFromAdsIsAvailability && utc > _buyFromAdsResetTime)
                {
                    UpdateBuyFromAds();
                }
                else
                {
                    var timeToEnd = (int)(_buyFromAdsResetTime - utc);
                    if(timeToEnd > 0 && timeToEnd != _secondLeft)
                    {
                        var second = timeToEnd;
                        var minutes = second / 60;
                        var hours = minutes / 60;

                        _buyFromAdsDisableText.text = $"{hours:00}:{minutes % 60:00}:{second % 60:00}";
                    }
                    else if (_secondLeft > 0 && timeToEnd <= 0)
                    {
                        _buyFromAdsDisableText.text = LocalizationManager.GetTranslation("Fortune/ADS_BTN_WAIT");
                    }
                    _secondLeft = timeToEnd;
                }
            }

            var endReload = (int)(_lastReloadTimerComplete - utc);
            if (endReload > 1 && _lastReloadTimerComplete > 1)
            {
                if (_endReloadTimer != endReload)
                {
                    _endReloadTimer = endReload;

                    var minutes = endReload / 60;
                    var hours = minutes / 60;
                    _reloadTimer.text = $"{hours:00}:{minutes % 60:00}:{endReload % 60:00}";
                }
            }
            else if(_reloadTimer.text.Length == 0 || _reloadTimer.text[0] != '.')
            {
                if(!_waitNextRewards.Value && !WaitRewardDiff.Value)
                    RequestNextReward();
                
                _reloadTimer.text = "...";
            }
        }

        public void Open()
        { 
            _buyFromAdsIsAvailability = IronSource.Agent.isRewardedVideoAvailable();
            UpdateBuyFromSoft(InventoryLogic.SoftWallet.Value);
            UpdateBuyFromHard(InventoryLogic.HardWallet.Value);
            RouletteTicketsOnOnChange(InventoryLogic.RouletteTickets.Value);
            UpdateBuyFromAds();
            
            TryResetBuyFromSoftIndex();
            
            InventoryLogic.SoftWallet.OnChange += UpdateBuyFromSoft;
            InventoryLogic.HardWallet.OnChange += UpdateBuyFromHard;
            InventoryLogic.RouletteTickets.OnChange += RouletteTicketsOnOnChange;
            
            _showController.OnBeginHide -= ShowControllerOnBeginHide;
            _showController.OnBeginHide += ShowControllerOnBeginHide;
            
            IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent; 
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += OnRewardedVideoAvailabilityChangedEvent;
            
            _showController.Show();
        }

        private void OnRewardedVideoAvailabilityChangedEvent(bool available)
        {
            _buyFromAdsIsAvailability = available;

            if (available && GetUtc() > _buyFromAdsResetTime)
                UpdateBuyFromAds();
        }

        private void RewardedVideoAdRewardedEvent(IronSourcePlacement placement)
        {
            if(placement.getRewardName() != "tickets")
                return;

            _buyFromAdsResetTime = GetUtc() + _buyFromAdsResetTimeCooldown;
            UpdateBuyFromAds();
            TicketFromAdsComplete();
        }

        private void ShowControllerOnBeginHide()
        {
            _showController.OnBeginHide -= ShowControllerOnBeginHide;
            
            InventoryLogic.SoftWallet.OnChange -= UpdateBuyFromSoft;
            InventoryLogic.HardWallet.OnChange -= UpdateBuyFromHard;
            InventoryLogic.RouletteTickets.OnChange -= RouletteTicketsOnOnChange;
            
            IronSourceEvents.onRewardedVideoAdRewardedEvent -= RewardedVideoAdRewardedEvent; 
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent -= OnRewardedVideoAvailabilityChangedEvent;
        }

        private void LoadBuyFromSoft()
        {
            _buyFromSoftIndex = PlayerPrefs.GetInt("TicketsBuyFromSoftIndex", 0);
            
            TryResetBuyFromSoftIndex();
        }
        
        private void LoadBuyFromHard()
        {
            
        }

        private void LoadBuyFromAds()
        {
            _buyFromAdsResetTimeCooldown = (float)RemoteConfig.GetDouble("TicketBuyFromAdsCooldown", TimeSpan.FromHours(2).TotalSeconds);
        }
        

        private void TryResetBuyFromSoftIndex()
        {
            var utc = GetUtc();
            if (utc > _buyFromSoftReset)
            {
                _buyFromSoftIndex = 0;
                _buyFromSoftReset = (float)(utc + TimeSpan.FromDays(1).TotalMilliseconds);
                //PlayerPrefs.SetFloat("TicketsBuyFromSoftReset", _buyFromSoftReset);
            }
        }
        
        private void UpdateBuyFromSoft(int softWallet)
        {
            var softPrices = ServerRequest.FortuneSoftPrice;
            
            var haveTickets = _buyFromSoftIndex < softPrices.Count;
            _buyFromSoftBtn.interactable = haveTickets && softWallet >= softPrices[_buyFromSoftIndex];

            if (haveTickets)
            {
                _buyFromSoftPrice.text = softPrices[_buyFromSoftIndex].ToString();
                _buyFromSoftAreOver.alpha = 0f;
                _buyFromSoftPriceBox.alpha = 1f;
            }
            else
            {
                _buyFromSoftAreOver.alpha = 1f;
                _buyFromSoftPriceBox.alpha = 0f;
            }
        }
        
        private void UpdateBuyFromHard(int hardWallet)
        {
            _buyFromHardBtn.interactable = true;
            _buyFromHardPrice.text = ServerRequest.FortuneHardPrice.ToString();
        }
        
        
        private void RouletteTicketsOnOnChange(int value)
        {
            _rotateBtn.interactable = value > 0 && !WaitRewardDiff.Value && !_waitNextRewards.Value && !_isProcess.Value;
            _ticketCount.text = value.ToString();
        }
        
        private void UpdateBuyFromAds()
        {
            var isAvailability = _buyFromAdsIsAvailability && GetUtc() > _buyFromAdsResetTime;
            _buyFromAdsBtn.interactable = isAvailability;
            
            if (!isAvailability)
            {
                _buyFromAdsDisable.alpha = 1f;
                _buyFromAdsCanUse.alpha = 0f;

                _secondLeft = int.MaxValue;
                _buyFromAdsDisableText.text = LocalizationManager.GetTranslation("Fortune/ADS_BTN_WAIT");
            }
            else
            {
                _buyFromAdsDisable.alpha = 0f;
                _buyFromAdsCanUse.alpha = 1f;
            }
        }

        private void BuyTicket(string wallet)
        {
            var startTime = Time.unscaledTime;
            WaitView.Instance.Show("Loading/WAIT_REQUEST_TITLE");
            var indexDiffs = InventoryHistory.LastIndex;
            ServerRequest.Instance.BuyTicket(wallet).ContinueWithOnMainThread(task =>
            {
                CrowdAnalyticsMediator.BuyTicketComplete(wallet);
                if (wallet == "soft" && task.Exception == null && task.Result.Status == ErrorCode.None)
                    _buyFromSoftIndex++;
                ServerRequest.Instance.RequestDiffs().ContinueWithOnMainThread(task1 =>
                {
                    if (InventoryHistory.LastIndex == indexDiffs)
                        FirebaseApplication.LogError("BuyTicket error! IndexHistoryNotChange");
                    HideRequestScreen(startTime);
                });
            });
        }
        
        public void BuyFromSoft()
        {
            BuyTicket("soft");
        }
        
        public void BuyFromHard()
        {
            if(InventoryLogic.HardWallet.Value < ServerRequest.FortuneHardPrice)
                _shopTab.ChangeTab();
            else
                BuyTicket("hard");
        }
        
        public void BuyFromAds()
        {
            var ironSource = IronSource.Agent;
            var showAdsSuccess = ironSource.isRewardedVideoAvailable() ? 1 : 0;
            if (showAdsSuccess == 1)
                ironSource.showRewardedVideo("wheelOfFortune");
        }

        private async void HideRequestScreen(float startTime)
        {
            int wait = 800 - (int)((Time.unscaledTime - startTime) * 1000);
            if (wait > 0)
                await Task.Delay(wait).ConfigureAwait(true);
            
            WaitView.Instance.Hide();
        }
        
        public void RotateWheel()
        {
            WaitRewardDiff.Value = true;
            CrowdAnalyticsMediator.Instance.BeginEvent("RotateWheel").CompleteBuild();
            
            _wheelOfFortuneLogic.OnCompleteRotate = OnCompleteRotate;
            _wheelOfFortuneLogic.RotateWheelTask.Start();

            RequestRotateWheel();
        }

        private void RequestRotateWheel()
        {
            _isProcess.Value = true;
            ServerRequest.Instance.RotateWheel().ContinueWithOnMainThread(task =>
            {
                var result = task.Result.Response as JObject;
                if (result == null || task.Result.Status != ErrorCode.None)
                {
                    UpdateManager.Instance.StartCoroutine(ReRotateWheel());
                    return;
                }
                
                if (result.TryGetValue("wheel", out var wheel))
                    _nextReward = wheel as JObject;
                
                if (!result.TryGetValue("reward", out var rewardToken))
                {
                    _wheelOfFortuneLogic.Error();
                    return;
                }

                GetOrPush.RouletteSpins++;
                GameTuneImpl.GameTuneManager.UpdateParam("spin_count", GetOrPush.RouletteSpins);
                
                var reward = (JObject)rewardToken;
                if (!_wheelOfFortuneLogic.ToWin((string) reward["type"], (string) reward["value"]))
                    _wheelOfFortuneLogic.Error();
                else
                    ServerRequest.Instance.RequestDiffs();
            });
        }
        
        
        private IEnumerator ReRotateWheel()
        {
            yield return NotInternet.Instance.WaitInternet(DialogViewBox.Instance, false);
            RequestRotateWheel();
        }

        private void OnCompleteRotate()
        {
            _isProcess.Value = false;
            if (_nextReward != null)
                UnpackNextReward();
            else if (!_waitNextRewards.Value)
                RequestNextReward();
        }

        public bool IsOpen()
        {
            return _showController.CurrentState == ShowController.StateAnimation.Show;
        }
    }
}