using System;
using System.Collections.Generic;
using System.Linq;
using GameRules.Scripts.UI.Shop;
using I2.Loc;
using Microsoft.Win32.SafeHandles;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace GameRules.UI.Shop
{
    public class ShopItem : BaseShopContainer
    {
        [SerializeField]
        private string _id;

        [SerializeField, BoxGroup("Override ids")]
        private string _androidId;
        [SerializeField, BoxGroup("Override ids")]
        private string _iosId;
    
        [SerializeField]
        private TextMeshProUGUI _oldPrice;
        [SerializeField]
        private TextMeshProUGUI _price;

        [SerializeField]
        private GameObject _saleBox;
        [SerializeField]
        private LocalizationParamsManager _saleValue;
        [SerializeField]
        private TextMeshProUGUI _saleTimer;

        [SerializeField]
        private Button _buyButton;
        
        private Product _product;
        private int _lastSaleTimer;

        private void Awake()
        {
            CodelessIAPStoreListener.IsProcessPurchasing.OnChange += IsProcessPurchasingChange;
        }

        private void OnDestroy()
        {
            CodelessIAPStoreListener.IsProcessPurchasing.OnChange -= IsProcessPurchasingChange;
        }

        private void IsProcessPurchasingChange(bool value)
        {
            if(_buyButton != null)
                _buyButton.interactable = !value;
        }

        public override int Initialize(ProductCollection products, Func<string, bool> isCanBuy)
        {
            if (products == null)
                return 0;

            var id = _id;
            
#if UNITY_IOS
            if(!string.IsNullOrWhiteSpace(_iosId))
                id = _iosId;
#elif UNITY_ANDROID
            if(!string.IsNullOrWhiteSpace(_androidId))
                id = _androidId;
#endif
            
            if (string.IsNullOrWhiteSpace(id) || (isCanBuy != null && !isCanBuy.Invoke(id)))
                return 0;

            _id = id;
            
            var product = _product = products.WithID(_id);
            if (product == null || product.metadata == null || !product.availableToPurchase)
            {
                Debug.LogError($@"NotFound product with ID: ""{_id}""");
                return 0;
            }

            bool saleActive = false;
            int saleValue = 0;
            _lastSaleTimer = -1;
            
            if (_oldPrice != null)
            {
                var saleId = _id + "_sale";
                var saleAnalog = products.WithID(saleId);

                if (saleAnalog == null || !GetOrPush.HasSales(_id))
                {
                    _oldPrice.gameObject.SetActive(false);
                    SetPrice(product, _price);
                }
                else
                {
                    _oldPrice.gameObject.SetActive(true);
                    
                    SetPrice(product, _oldPrice);
                    SetPrice(saleAnalog, _price);
                    
                    saleValue = ((int)math.round((float)(1 - saleAnalog.metadata.localizedPrice / product.metadata.localizedPrice) * 100 / 5)) * 5;
                    saleActive = saleValue > 0;

                    _product = saleAnalog;
                }
            }
            else
                SetPrice(product, _price);


            if (_saleBox != null)
            {
                _saleBox.SetActive(saleActive);
                if (saleActive)
                {
                    _saleValue.SetParameterValue("SALE", saleValue.ToString());
                    UpdateLeftTimeTimer();
                }
            }
                
            
            return 1;
        }

        private void Update()
        {
            UpdateLeftTimeTimer();
        }

        private void UpdateLeftTimeTimer()
        {
            if(_saleBox == null || !_saleBox.activeSelf)
                return;
            var saleTimeLeft = GetOrPush.TimeLeftForSale(_id);
            if(_lastSaleTimer == saleTimeLeft)
                return;
            _lastSaleTimer = saleTimeLeft;

            _saleTimer.text = $"{saleTimeLeft / 3600 % 60:00}:{saleTimeLeft / 60 % 60:00}:{saleTimeLeft % 60:00}";
        }

        private void SetPrice(Product product, TextMeshProUGUI textMesh)
        {
            if (product.definition.type == ProductType.Subscription)
                textMesh.text = $"{product.metadata.localizedPrice.ToString()} {product.metadata.isoCurrencyCode}<size=65%>/ MONTH</size>";
            else
                textMesh.text = $"{product.metadata.localizedPrice.ToString()} {product.metadata.isoCurrencyCode}";
        }


        public void OnClick()
        {
            CodelessIAPStoreListener.Instance.InitiatePurchase(_product.definition.id);
        }
    }

}
