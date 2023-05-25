using System;
using System.Collections;
using GameRules.Core.Runtime;
using GameRules.TaskManager.Runtime;
using GameRules.UI;
using GameRules.UI.Shop;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameRules.Scripts.UI.Shop
{
    public class ShopWindow : MonoBehaviour
    {
        [SerializeField]
        private ShowController _showController;

        [SerializeField]
        private ShopContainer _mainBox;

        private void Awake()
        {
            IAPHandler.CompleteNextExecutePurchases += OnCompleteExecutePurchases;
        }

        private void OnDestroy()
        {
            IAPHandler.CompleteNextExecutePurchases -= OnCompleteExecutePurchases;
        }
        
        private void OnCompleteExecutePurchases()
        {
            Invoke(nameof(DrawItems), 0.01f);
        }

        public void Open()
        {
            App.GetModule<ITaskSystem>().Subscribe(DrawOpenShopItems());
        
            _showController.Show();
        
        }

        private IEnumerator DrawOpenShopItems()
        {
            while(!CodelessIAPStoreListener.initializationComplete)
                yield return null;
            
            DrawItems();
        }

        public void RestorePurchase()
        {
            CodelessIAPStoreListener.RestoreTransaction(null);
        }
    
        private void DrawItems()
        {
            var products = CodelessIAPStoreListener.Instance.StoreController.products;
            _mainBox.Initialize(products, IsCanBuy);
        }

        private bool IsCanBuy(string id)
        {
            switch (id)
            {
                case "com.rustygames.tc.all_players":
                    return !GetOrPush.UnlockedAllSkins;
                case "com.rustygames.tc.none_ads.forever":
                case "com.rustygames.tc.none_ads.forever_sale":
                    return GetOrPush.AdsCurrentType == GetOrPush.AdsType.AdsEnable;
                default:
                    return true;
            }
        }
    }
}
