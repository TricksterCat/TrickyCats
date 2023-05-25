using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.Server;
using GameRules.Scripts.UI.WheelOfFortune;
using I2.Loc;
using Michsky.UI.ModernUIPack;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InventoryLogic = GameRules.Scripts.Modules.Database.Inventory;

namespace GameRules.Scripts.UI.Inventory
{
    public class InventoryItemInfoView : MonoBehaviour
    {
        [SerializeField]
        private ModalWindowManager _showController;

        [SerializeField, BoxGroup("Preview")]
        private Image _preview;
        [SerializeField, BoxGroup("Preview")]
        private TextMeshProUGUI _title;
        [SerializeField, BoxGroup("Preview")]
        private TextMeshProUGUI _desc;

        [SerializeField, BoxGroup("Soft")]
        private Button _buyFromSoftBtn;
        [SerializeField, BoxGroup("Soft")]
        private TextMeshProUGUI _butFromSoftValue;
        
        [SerializeField, BoxGroup("Hard")]
        private Button _buyFromHardBtn;
        [SerializeField, BoxGroup("Hard")]
        private TextMeshProUGUI _buyFromHardValue;

        [SerializeField, BoxGroup("LevelCondition")]
        private GameObject _levelCondition;
        [SerializeField, BoxGroup("LevelCondition")]
        private LocalizationParamsManager _levelConditionParam;
        
        [SerializeField]
        private GameObject _wheelOfFortuneCondition;
        
        [SerializeField]
        private GameObject _selectBtn;

        private BaseItem _model;
        public Action<BaseItem> OnSelect { get; set; }

        private void Awake()
        {
            InventoryLogic.ItemsChangeEvent += OnUpdateItems;
        }

        private void OnDestroy()
        {
            InventoryLogic.ItemsChangeEvent -= OnUpdateItems;
        }

        public void Show(InventoryItem item)
        {
            _model = item.Model;
            
            var model = item.Model;

            _preview.sprite = model.Preview;
            _title.text = model.Title;
            _desc.text = model.Description;
            
            var conditions = model.Conditions;
            DrawItemActions(true);
            
            _levelCondition.gameObject.SetActive(conditions.UnlockLevel != 0);
            if (_levelCondition.gameObject.activeSelf)
                _levelConditionParam.SetParameterValue("LEVEL", conditions.UnlockLevel.ToString());
            
            _wheelOfFortuneCondition.SetActive(conditions.InWheelOfFortune);

            _showController.OpenWindow();
        }

        private void OnUpdateItems()
        {
            DrawItemActions(false);
        }

        private void DrawItemActions(bool isForce)
        {
            if(!isForce && !_showController.IsShow)
                return;
            
            var model = _model;
            var conditions = model.Conditions;
            
            var isAvailable = InventoryLogic.IsAvailability(model);
            
            _selectBtn.SetActive(isAvailable && !InventoryLogic.IsSelect(model) && model.Type != ItemType.Map);
            
            _buyFromSoftBtn.gameObject.SetActive(!isAvailable && conditions.SoftPrice != 0);
            if (_buyFromSoftBtn.gameObject.activeSelf)
            {
                _butFromSoftValue.text = conditions.SoftPrice.ToString();
                _buyFromSoftBtn.interactable = InventoryLogic.SoftWallet.Value >= conditions.SoftPrice;
            }
            
            _buyFromHardBtn.gameObject.SetActive(!isAvailable && conditions.HardPrice != 0);
            if (_buyFromHardBtn.gameObject.activeSelf)
            {
                _buyFromHardValue.text = conditions.HardPrice.ToString();
                _buyFromHardBtn.interactable = InventoryLogic.HardWallet.Value >= conditions.HardPrice;
            }
        }

        public void BuyFromSoft()
        {
            BuyItem("soft");
        }

        public void BuyFromHard()
        {
            BuyItem("hard");
        }

        private void BuyItem(string walletType, float startTime = -1)
        {
            if (startTime < 0)
            {
                startTime = Time.unscaledTime;
                WaitView.Instance.Show("Loading/WAIT_REQUEST_TITLE");
            }
            
            ServerRequest.Instance.BuyItem(_model.Id, walletType).ContinueWithOnMainThread(task =>
            {
                if (task.Exception != null || task.Result == ItemErrorCode.NotInternet)
                {
                    UpdateManager.Instance.StartCoroutine(ReBuyItem(walletType, startTime));
                    return;
                }
                OnBuyComplete(task.Result, startTime);
            });
        }

        private IEnumerator ReBuyItem(string walletType, float startTime)
        {
            yield return NotInternet.Instance.WaitInternet(DialogViewBox.Instance, false);
            BuyItem(walletType, startTime);
        }

        private static async void OnBuyComplete(ItemErrorCode result, float startTime)
        {
            var indexDiffs = InventoryHistory.LastIndex;
            
            int wait = 800 - (int)((Time.unscaledTime - startTime) * 1000);
            if (result == ItemErrorCode.None)
            {
                ServerRequest.Instance.RequestDiffs().ContinueWithOnMainThread(async task =>
                {
                    WheelOfFortuneWindow.UpdateWheel.Value = true;
                    
                    wait = 800 - (int)((Time.unscaledTime - startTime) * 1000);
                    if (wait > 0)
                        await Task.Delay(wait).ConfigureAwait(true);
                    
                    if (InventoryHistory.LastIndex == indexDiffs)
                        FirebaseApplication.LogError("OnBuyComplete error! IndexHistoryNotChange");
                    
                    WaitView.Instance.Hide();
                });
                return;
            }
            
            if (wait > 0)
                await Task.Delay(wait).ConfigureAwait(true);
            
            WaitView.Instance.Hide();
            
            string title;
            string message;
            switch (result)
            {
                case ItemErrorCode.session_not_initialized:
                case ItemErrorCode.exchange_not_available:
                    title = "Inventory/DIALOG_ERROR_TITLE";
                    message = "Inventory/DIALOG_ERROR_CLIENT_PROBLEM";
                    break;
                case ItemErrorCode.UnknownError:
                    title = "Inventory/DIALOG_ERROR_TITLE";
                    message = "Inventory/DIALOG_ERROR_UNKNOWN";
                    break;
                default:
                    return;
            }

            var dialog = DialogViewBox.Instance;

            dialog.NegativeBtn
                .SetLabelValue("OK_BTN")
                .SetCallback(() => { dialog.Hide(); }).SetActive(true);
            dialog.PositiveBtn.SetActive(false);
            dialog.Show(title, message, 240);
        }

        public void Select()
        {
            var model = _model;
            if(InventoryLogic.IsSelect(model))
                return;
            
            OnSelect?.Invoke(model);
            UpdateManager.OnNextFrame(() => _selectBtn.SetActive(false));
        }
    }
}