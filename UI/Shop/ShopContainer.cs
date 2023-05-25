using System;
using GameRules.Scripts.UI.Shop;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameRules.UI.Shop
{
    public class ShopContainer : BaseShopContainer
    {
        [SerializeField]
        private BaseShopContainer[] _containers;
        
        public override int Initialize(ProductCollection products, Func<string, bool> isCanBuy)
        {
            int result = 0;
            if (_containers == null || _containers.Length == 0)
                return 0;
            
            for (int i = 0, iMax = _containers.Length; i < iMax; i++)
            {
                var count = _containers[i].Initialize(products, isCanBuy);
                _containers[i].gameObject.SetActive(count != 0);
                result += count;
            }

            return result;
        }
    }
}