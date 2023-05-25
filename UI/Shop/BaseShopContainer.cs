using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameRules.Scripts.UI.Shop
{
    public abstract class BaseShopContainer : MonoBehaviour
    {
        public abstract int Initialize(ProductCollection products, Func<string, bool> isCanBuy);
    }
}