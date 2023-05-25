using System;
using System.Collections;
using System.Collections.Generic;
using Core.Base.Modules;
using GameRules.Core.Runtime;
using GameRules.Core.Runtime.Modules;
using GameRules.Scripts.Weapons;
using GameRulez.Modules.PlayerSystems;
using Sirenix.OdinInspector;
using UnityEngine;

public class ImageAmmo : MonoBehaviour
{
    [SerializeField]
    private int _countMax;

    [SerializeField, ChildGameObjectsOnly]
    private RectTransform _empty;
    [SerializeField, ChildGameObjectsOnly]
    private RectTransform _full;
    
    private int _count;

    private ModuleProxy<IPlayerSystem> _playerSystem;
    private ModuleProxy<IWeaponSystem> _weaponSystem;
    
    // Start is called before the first frame update
    private void Start()
    {
        _count = -1;
        _weaponSystem = new ModuleProxy<IWeaponSystem>();
        _playerSystem = new ModuleProxy<IPlayerSystem>();
    }

    // Update is called once per frame
    private void Update()
    {
        var playerSystem = _playerSystem.Get();
        if (playerSystem == null || playerSystem.CompareStatus(ModuleStatus.Disable))
            UpdateValue(0);
        else
        {
            var weaponData = _weaponSystem.Get().GetWeaponData(playerSystem.PlayerTeamIndex);
            UpdateValue(weaponData.Ammo);
        }
    }

    private void UpdateValue(int ammoCount)
    {
        ammoCount = Math.Min(ammoCount, _countMax);
        if(_count == ammoCount)
            return;

        _count = ammoCount;
        var position = (float) ammoCount / _countMax;
        _empty.anchorMin = new Vector2(position, 0f);
        _full.anchorMax = new Vector2(position, 1f);

    }
}
