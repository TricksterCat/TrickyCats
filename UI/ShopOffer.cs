using System;
using GameRules.Firebase.Runtime;
using GameRules.UI;
using Michsky.UI.ModernUIPack;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameRules.Scripts.UI
{
    public class ShopOffer : MonoBehaviour
    {
        [SerializeField]
        private ModalWindowManager _showController;

        [SerializeField]
        private TextMeshProUGUI _saleValue;
        
        
        [SerializeField]
        private string _productId;
        

        public void TryShow(int playGames)
        {
            if(GetOrPush.AdsCurrentType == GetOrPush.AdsType.AdsDisable)
                return;
            
            if (GetOrPush.LowPriceNoAdsDateTime >= new DateTime(2000, 1, 1) ||
                playGames <= RemoteConfig.GetInt("OfferLowNoAds_AfterGame", 2)) return;
            
            var iapManager = CodelessIAPStoreListener.Instance;
            var original = iapManager.GetProduct(_productId);
            var saleProduct = iapManager.GetProduct($"{_productId}_sale");
            if(original == null || !original.availableToPurchase || saleProduct == null || !saleProduct.availableToPurchase)
                return;

            var saleValue = ((int)math.round((float)(1 - saleProduct.metadata.localizedPrice / original.metadata.localizedPrice) * 100 / 5)) * 5;
            _saleValue.text = $"-{saleValue}%";
            GetOrPush.LowPriceNoAdsDateTime = DateTime.UtcNow.AddDays(1);
            _showController.OpenWindow();
        }
    }
}