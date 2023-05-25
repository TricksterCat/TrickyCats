using System;
using AppsFlyerSDK;
using GameRules.Analytics.Runtime;
using GameRules.Firebase.Runtime.Analytics;

namespace GameRules.Scripts.Modules
{
    public class CrowdAnalyticsMediator : BaseAnalyticsMediator<CrowdAnalyticsMediator>
    {
        private const string TenjinKey = "16CM9RAXOZCSZH9YDN749D42CFY3QGVM";

        public static BaseTenjin GetTenjin()
        {
            return Tenjin.getInstance(TenjinKey);
        }
        
        protected override IAnalytics[] SupportAnalytics => new IAnalytics[]
        {
            new FirebaseAnalytics(EventPriority.Low),
        };

        public void LevelUp(int value)
        {
            BeginEvent(BaseEvents.LevelUp)
                .SetLevel(value)
                .CompleteBuild();
        }

        public void AddConversionValue(float profit, string currency)
        {
            BeginEvent("AdsProfit")
                .AddConversionValue(profit, currency)
                .CompleteBuild();
        }

        public void CompleteTutorial()
        {
            BeginEvent(BaseEvents.CompleteTutorial)
                .CompleteBuild();
        }

        public static void LogAddWallet(string walletType, string reason, int amount)
        {
            Instance
                .BeginEvent(BaseEvents.AddWallet)
                .SetWalletValue(amount, walletType)
                .AddField("reason", reason)
                .CompleteBuild();
        }

        public static void LogAddItem(string reason, string id)
        {
            Instance
                .BeginEvent("AddItem")
                .SetItem(id)
                .AddField("type", Database.Database.All.TryGetValue(id, out var item) ? item.Type.ToString() : "unknown")
                .AddField("reason", reason)
                .CompleteBuild();
        }

        public static void BuyTicketComplete(string wallet)
        {
            Instance
                .BeginEvent("BuyTickets")
                .AddField("type", wallet)
                .CompleteBuild();
        }

        public void LogBuy(string item_id, string walletType, int amount)
        {
            Instance.BeginEvent(BaseEvents.SpendWallet)
                .SetItem(item_id)
                .SetWalletValue(amount, walletType)
                .CompleteBuild();
        }

        public static void DynamicLinkReceived()
        {
            Instance
                .BeginEvent("dynamic_link_open")
                .CompleteBuild();
        }
    }
}