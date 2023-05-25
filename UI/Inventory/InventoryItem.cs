using System;
using GameRules.Modules.TutorialEngine.UI.Mask;
using GameRules.Scripts.Modules.Database.Items;
using UnityEngine;
using UnityEngine.UI;
using InventoryLogic = GameRules.Scripts.Modules.Database.Inventory;

namespace GameRules.Scripts.UI.Inventory
{
    public class InventoryItem : MonoBehaviour
    {
        private BaseItem _model;

        [SerializeField]
        public Toggle _toggle;
        [SerializeField]
        private GameObject _isLocked;
        [SerializeField]
        private GameObject _isSelected;

        [SerializeField]
        private Image _icon;

        private Action _onDisposeLastModel;
        public Toggle Toggle => _toggle;
        public BaseItem Model => _model;

        [SerializeField]
        private CanvasGroup _canvasGroup;
        [SerializeField]
        private LayoutElement _layoutElement;

        [SerializeField]
        private UnmaskTagImage _tutorialUnmask;
        
        public void Inject(BaseItem item)
        {
            if (item.Id == "PLAYER_COWBOY")
            {
                _tutorialUnmask.SetTag("cowboy_item");
                _tutorialUnmask.enabled = true;
            }
            else
                _tutorialUnmask.enabled = false;
            
            _onDisposeLastModel?.Invoke();
            
            _model = item;
            _icon.sprite = item.Icon;

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _layoutElement.ignoreLayout = false;
            
            if (!InventoryLogic.IsAvailability(item))
            {
                _isLocked.SetActive(true);
                
                if(_isSelected.activeSelf)
                    _isSelected.SetActive(false);
                return;
            }
            _isLocked.SetActive(false);

            BaseItem selected = null;
            switch (item.Type)
            {
                case ItemType.Character:
                    selected = InventoryLogic.PlayerSkin.Value;
                    break;
                case ItemType.Minion:
                    selected = InventoryLogic.UnitSkin.Value;
                    break;
                case ItemType.Map:
                    if(_isSelected.activeSelf)
                        _isSelected.SetActive(false);
                    break;
            }
            if(item == selected)
                CompleteSelected();
            else 
                _isSelected.SetActive(false);
        }

        private void CompleteSelected()
        {
            _isSelected.SetActive(true);

            switch (_model.Type)
            {
                case ItemType.Character:
                    void DeSelectPlayer(PlayerItem newItem)
                    {
                        if (_model == newItem)
                            return;
                        if(_isSelected != null && !_isSelected.Equals(null))
                            _isSelected.SetActive(false);
                        _onDisposeLastModel?.Invoke();
                    }

                    _onDisposeLastModel = () => InventoryLogic.PlayerSkin.OnChange -= DeSelectPlayer;
                    InventoryLogic.PlayerSkin.OnChange += DeSelectPlayer;
                    break;
                case ItemType.Minion:
                    void DeSelectMinion(UnitItem newItem)
                    {
                        if (_model == newItem) 
                            return;
                        if(_isSelected != null && !_isSelected.Equals(null))
                            _isSelected.SetActive(false);
                        _onDisposeLastModel?.Invoke();
                    }

                    _onDisposeLastModel = () => InventoryLogic.UnitSkin.OnChange -=  DeSelectMinion;
                    InventoryLogic.UnitSkin.OnChange += DeSelectMinion;
                    break;
            }
        }
        
        public void Select()
        {
            if(_model == null)
                return;
            
            switch (_model.Type)
            {
                case ItemType.Map:
                    return;
                case ItemType.Character:
                    InventoryLogic.UpdatePlayerSkin((PlayerItem)_model);
                    break;
                case ItemType.Minion:
                    InventoryLogic.UpdateUnitSkin((UnitItem)_model);
                    break;
            }
            CompleteSelected();
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _layoutElement.ignoreLayout = true;

            _model = null;
        }
    }
}