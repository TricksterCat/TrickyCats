using System.Collections.Generic;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.UI.Inventory;
using I2.Loc;
using Michsky.UI.ModernUIPack;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.Scripts.UI.RewardViews
{
    public class RewardView : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private Localize _title;
        
        [SerializeField]
        private ModalWindowManager _showController;

        private List<GameObject> _boxes;
        
        [SerializeField, BoxGroup("Wallet")]
        private TextMeshProUGUI _walletValue;
        [SerializeField, BoxGroup("Wallet")]
        private Image _walletIcon;
        [SerializeField, BoxGroup("Wallet")]
        private WalletIconsDict _walletIcons;
        
        [SerializeField, BoxGroup("Crowd")]
        private TextMeshProUGUI _crowdValue;
        [SerializeField, BoxGroup("Crowd")]
        private GameObject _crowdBox;

        [SerializeField, BoxGroup("Items")]
        private Image _itemIcon;
        [SerializeField, BoxGroup("Items")]
        private GameObject _itemBox;

        private void Awake()
        {
            _boxes = new List<GameObject>();
            if(_crowdBox != null)
                _boxes.Add(_crowdBox);
            if(_itemBox != null)
                _boxes.Add(_itemBox);
            if(_walletValue != null)
                _boxes.Add(_walletValue.gameObject);
        }

        private void SetActive(GameObject gameObject)
        {
            for (int i = 0; i < _boxes.Count; i++)
                _boxes[i].SetActive(_boxes[i] == gameObject);
        }

        public void Show(JObject reward, string reason)
        {
            SetTitle(reason);
            
            bool isEnd = true;
                
            if (reward.TryGetValue("add_items", out var addItems))
            {
                if(!Database.All.TryGetValue(addItems[0]["productId"].ToString(), out var item))
                    return;
                DrawItem(item);
            }
            else if (reward.TryGetValue("wallets", out var jWallets))
            {
                isEnd = false;
                foreach (var wallet in (JObject)jWallets)
                {
                    switch (wallet.Key)
                    {
                        case "crowd":
                            if(_crowdBox == null)
                                return;
                    
                            _crowdValue.text = wallet.Value.ToString();
                            SetActive(_crowdBox);
                            isEnd = true;
                            break;
                        case "rouletteSpin":
                            if(!_walletIcons.Contains(wallet.Key))
                                continue;

                            var value = (int) wallet.Value;
                            if (reason == "fortune")
                                value++;
                            
                            if (value > 0)
                            {
                                DrawWallet(wallet.Key, value.ToString());
                                isEnd = true;
                            }
                            break;
                        default:
                            if(!_walletIcons.Contains(wallet.Key))
                                continue;
                            
                            DrawWallet(wallet.Key, wallet.Value.ToString());
                            isEnd = true;
                            break;
                    }
                    
                    if(isEnd)
                        break;
                }
            }
            
            if(isEnd)
                _showController.OpenWindow();
        }

        private void SetTitle(string reason)
        {
            if (_title == null) 
                return;
            
            switch (reason)
            {
                default:
                    _title.SetTerm("MainScreen/REWARD_VIEW_TITLE");
                    break;
                case "gift":
                case "gift_notify":
                case "friends_invite":
                    _title.SetTerm("MainScreen/GIFT_VIEW_TITLE");
                    break;
            }
        }

        private void DrawItem(BaseItem item)
        {
            _itemIcon.sprite = item.Icon;
            
            SetActive(_itemBox);
        }
        
        private void DrawWallet(string type, string value)
        {
            var info = _walletIcons[type];
            _walletValue.text = value;
            _walletValue.color = info.Color;
            _walletIcon.sprite = info.Icon;

            SetActive(_walletValue.gameObject);
        }

        public void OnBeforeSerialize()
        {
            _walletIcons?.Serialzie();
        }

        public void OnAfterDeserialize()
        {
            _walletIcons?.Deserialize();
        }
    }
}