using System.Collections.Generic;
using Firebase.Analytics;
using GameRules.Scripts.UI.RewardViews;
using I2.Loc;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace GameRules.Scripts.Modules.Database
{
    public class InventoryDiffsHandler : MonoBehaviour, IInventoryDiffsListener
    {
        [SerializeField]
        private RewardView _rewardView;
        
        [SerializeField, BoxGroup("LevelUp")]
        private RewardView _levelUpView;
        [SerializeField, BoxGroup("LevelUp")]
        private LocalizationParamsManager _levelPram;
        
        public static int IsLockCounter { get; set; }
        public static bool HaveChange { get; private set; }
        
        private static Dictionary<string, int> _priority = new Dictionary<string, int>
        {
            { "fortune", 0 },
            { "level_up", 1 },
            { "friends_invite", 2},
            { "gift", 2},
            { "gift_notify", 2}
        };
        
        private void Start()
        {
            InventoryHistory.InjectListener(this);   
            if(InventoryHistory.HaveDiffs)
                Change();
        }

        private void OnDestroy()
        {
            InventoryHistory.InjectListener(null);   
        }

        private void Update()
        {
            if(!HaveChange || IsLockCounter != 0)
                return;
            HaveChange = false;
            
            var processedIndex = InventoryHistory.ProcessedDiffIndex;
            var diffs = InventoryHistory.GetDiffs();

            int showRewardByIndex = -1;
            string showRewardReason = string.Empty;
            int priority = -1;

            int softDiff = 0;
            int hardDiff = 0;
            int rouletteSpin = 0;
            int crowd = 0;

            int crowdByReferral = 0;
            
            for (int i = 0; i < diffs.Length; i++)
            {
                var diff = diffs[i];
                var reason = diff["reason"].ToString();
                if (_priority.TryGetValue(reason, out var diffPriority) && diffPriority >= priority)
                {
                    showRewardByIndex = i;
                    showRewardReason = reason;
                    priority = diffPriority;
                }
                
                if(processedIndex > i)
                    continue;

                int softSpend = 0;
                int hardSpend = 0;
                
                if(diff.TryGetValue("wallets", out var jWallets) && jWallets is JObject wallets)
                {
                    if (wallets.TryGetValue("soft", out var jSoft))
                    {
                        var count = (int) jSoft;
                        softDiff += count;
                        if (count > 0)
                            CrowdAnalyticsMediator.LogAddWallet("soft", reason, count);
                        else
                            softSpend = math.abs(count);
                    }

                    if (wallets.TryGetValue("hard", out var jHard))
                    {
                        var count = (int) jHard;
                        hardDiff += count;

                        if (count > 0)
                            CrowdAnalyticsMediator.LogAddWallet("hard", reason, count);
                        else
                            hardSpend = math.abs(count);
                    }

                    if (wallets.TryGetValue("rouletteSpin", out var jRouletteSpin))
                    {
                        var count = (int) jRouletteSpin;
                        rouletteSpin += count;

                        switch (reason)
                        {
                            case "buy_soft":
                                CrowdAnalyticsMediator.Instance.LogBuy("ticket", "soft", softSpend);
                                break;
                            case "buy_ads":
                                CrowdAnalyticsMediator.Instance.LogBuy("ticket", "ads", 1);
                                break;
                            case "buy_hard": 
                                CrowdAnalyticsMediator.Instance.LogBuy("ticket", "hard", hardSpend);
                                break;
                            case "fortune":
                                count++;
                                if(count == 1)
                                    CrowdAnalyticsMediator.Instance.LogBuy("ticket", "ticket", 1);
                                break;
                        }

                        if (count > 0)
                            CrowdAnalyticsMediator.LogAddWallet("ticket", reason, count);
                    }

                    if (wallets.TryGetValue("crowd", out var jCrowd))
                    {
                        crowd += (int) jCrowd;
                        if (reason == "friends_invite")
                            crowdByReferral += (int) jCrowd;
                    }
                }
                
                if (diff.TryGetValue("add_items", out var jAdd) && jAdd is JArray add)
                {
                    var count = add.Count;
                    if (count > 1)
                    {
                        for (int j = 0; j < add.Count; j++)
                        {
                            var item = add[j]["productId"].ToString();
                            Inventory.AddItem(item);
                            CrowdAnalyticsMediator.LogAddItem(reason, item);
                        }
                    }
                    else if(count == 1)
                    {
                        var item = add[0]["productId"].ToString();
                        Inventory.AddItem(item);
                        CrowdAnalyticsMediator.LogAddItem(reason, item);
                        
                        switch (reason)
                        {
                            case "buy_soft":
                                CrowdAnalyticsMediator.Instance.LogBuy(item, "soft", softSpend);
                                break;
                            case "buy_hard": 
                                CrowdAnalyticsMediator.Instance.LogBuy(item, "hard", hardSpend);
                                break;
                            case "fortune": 
                                CrowdAnalyticsMediator.Instance.LogBuy(item, "ticket", 1);
                                break;
                        }
                    }
                }
            }

            Inventory.SoftWallet.Value += softDiff;
            Inventory.HardWallet.Value += hardDiff;
            Inventory.RouletteTickets.Value += rouletteSpin;
            Inventory.Crowd.Value += crowd;
            
            if (showRewardByIndex != -1)
            {
                var diff = diffs[showRewardByIndex];
                switch (showRewardReason)
                {
                    case "gift":
                    case "gift_notify":
                    case "fortune":
                        _rewardView.Show(diff, showRewardReason);
                        break;
                    case "friends_invite":
                        if (diff["add_items"] != null)
                            _rewardView.Show(diff, showRewardReason);
                        else
                            _rewardView.Show(new JObject
                            {
                                { "wallets", new JObject
                                {
                                    { "crowd", crowdByReferral }
                                }}
                            }, showRewardReason);
                        break;
                    case "level_up":
                        var level = diff["level"].ToString();
                        _levelPram.SetParameterValue("Level", level);
                        _levelUpView.Show(diff, showRewardReason);
                        break;
                }
            }
        }

        public void Change()
        {
            HaveChange = true;
        }
    }
}