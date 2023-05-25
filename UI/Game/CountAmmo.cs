using System.Collections;
using System.Collections.Generic;
using Core.Base.Modules;
using GameRules.Core.Runtime.Modules;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.Weapons;
using GameRulez.Modules.PlayerSystems;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class CountAmmo : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _countValue;

    private int _lastValue;
    
    // Start is called before the first frame update
    private void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GameUiSystem>().Attach(this);
    }


    public void UpdateValue(int value)
    {
        if(_lastValue == value)
            return;
        _lastValue = value;
        _countValue.text = value.ToString();
    }
}
