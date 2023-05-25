using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.UI.Inventory
{
    [System.Serializable]
    public struct WalletInfo
    {
        public Color Color;
        public Sprite Icon;
    }
    
    [System.Serializable]
    public class WalletIconsDict
    {
        [SerializeField, HideInInspector]
        private string[] _walletModels_keys;
        [SerializeField, HideInInspector]
        private WalletInfo[] _walletModels_values;

        [ShowInInspector]
        private Dictionary<string, WalletInfo> _walletModelsDic;

        public void Serialzie()
        {
            if (_walletModelsDic == null)
            {
                _walletModels_keys = new string[0];
                _walletModels_values = new WalletInfo[0];
                return;
            }
            
            var count = _walletModelsDic.Count;
            _walletModels_values = new WalletInfo[count];
            _walletModels_keys = new string[count];

            int index = 0;
            foreach (var model in _walletModelsDic)
            {
                _walletModels_keys[index] = model.Key;
                _walletModels_values[index] = model.Value;
                index++;
            }
        }

        public void Deserialize()
        {
            if (_walletModelsDic == null)
                _walletModelsDic = new Dictionary<string, WalletInfo>();
            else
                _walletModelsDic.Clear();

            for (int i = 0; i < _walletModels_keys.Length; i++)
                _walletModelsDic[_walletModels_keys[i]] = _walletModels_values[i];
        }

        public WalletInfo this[string type] => _walletModelsDic[type];

        public bool Contains(string propertyName)
        {
            return _walletModelsDic.ContainsKey(propertyName);
        }
    }
}