using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.UI.Helper;
using GameRules.Scripts.UI.Inventory;
using GameRules.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class InventoryWindow : MonoBehaviour
{
    [SerializeField]
    private ShowController _showController;
    
    [SerializeField]
    private ToggleGroup _toggleGroup;
    
    [SerializeField]
    private RectTransform _content;
    [SerializeField]
    private GameObject _prefab;

    private MonoPool<InventoryItem> _items;
    private ItemType? _tabType = null;

    private bool _waitChangeContent;

    private InventoryItem _selected;
    
    [SerializeField, BoxGroup("Actions")]
    private GameObject _selectItemBtn;
    [SerializeField, BoxGroup("Actions")]
    private GameObject _moreInfoBtn;

    [SerializeField]
    private InventoryItemInfoView _itemInfoView;

    private void Awake()
    {
        _itemInfoView.OnSelect = SelectItemTarget;
        
        _items = new MonoPool<InventoryItem>(_content, CreatePrefab);
        foreach (var item in _items)
        {
            var toggle = item.Toggle;
            toggle.group = _toggleGroup;
            toggle.onValueChanged.AddListener(value =>
            {
                if(value) 
                    OnValueSelected(item);
            });
        }

        _toggleGroup.SetAllTogglesOff();
        
        _selectItemBtn.SetActive(false);
        _moreInfoBtn.SetActive(false);
        
        
        Inventory.ItemsChangeEvent += OnUpdateItems;
    }

    private void SelectItemTarget(BaseItem model)
    {
        if (_selected == null || _selected.Model != model)
        {
            _items.Reset();
            foreach (var item in _items)
            {
                if (item.Model == model)
                {
                    item.Toggle.isOn = true;
                    break;
                }
            }
        }

        SelectItem();
    }

    private void OnDestroy()
    {
        Inventory.ItemsChangeEvent -= OnUpdateItems;
    }

    private void OnUpdateItems()
    {
        if (_showController.CurrentState == ShowController.StateAnimation.Show)
            UpdateTab();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private InventoryItem CreatePrefab(Transform content)
    {
        var view = Instantiate(_prefab, content).GetComponent<InventoryItem>();
        var toggle = view.Toggle;
        toggle.group = _toggleGroup;
        toggle.onValueChanged.AddListener(value =>
        {
            if(toggle.isOn)
                OnValueSelected(view);
        });
        return view;
    }
    
    private void OnValueSelected(InventoryItem item)
    {
        _selected = item;
        _selectItemBtn.SetActive(item.Model != null && item.Model.Type != ItemType.Map && Inventory.IsAvailability(item.Model) && !Inventory.IsSelect(item.Model));
        _moreInfoBtn.SetActive(item.Model != null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateTab()
    {
        IEnumerable<BaseItem> items;
        switch (_tabType)
        {
            case ItemType.Character:
                items = Database.Players;
                break;
            case ItemType.Minion:
                items = Database.Units;
                break;
            case ItemType.Map:
                items = Database.Maps;
                break;
            default:
                throw new NotImplementedException();
        }

        _items.Reset();
        foreach (var item in items.OrderByDescending(Inventory.IsAvailability))
        {
            var view = _items.MoveNextOrCreate();
            view.Inject(item);
        }

        foreach (var item in _items.ToEnd())
            item.Hide();
        
        if (_items.Count > 0)
        {
            _items.Reset();
            _items.MoveNext();
            
            _items.Current.Toggle.isOn = true;
            OnValueSelected(_items.Current);
        }
    }
    
    public void Open()
    {
        _showController.Show();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ChangeTab(ItemType type)
    {
        if(_tabType == type)
            return;
        _tabType = type;

        _waitChangeContent = true;
        DOTween.Restart("InventoryChangeContent");
    }

    public void ChangeContentStep()
    {
        if(!_waitChangeContent)
            return;
        _waitChangeContent = false;
        UpdateTab();
    }

    public void ToCharacters()
    {
        ChangeTab(ItemType.Character);
    }
    
    public void ToMinions()
    {
        ChangeTab(ItemType.Minion);
    }
    
    public void ToMaps()
    {
        ChangeTab(ItemType.Map);
    }

    public void CallAdvancedInfo()
    {
        if(_selected.Model != null)
            _itemInfoView.Show(_selected);
    }

    public void SelectItem()
    {
        if (_selected != null && _selected.Model != null)
        {
            _selected.Select();
            OnValueSelected(_selected);
        }
    }
}
