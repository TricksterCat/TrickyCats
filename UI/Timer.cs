using System.Collections;
using System.Collections.Generic;
using System.Text;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.Modules.Game;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private StringBuilder _timeValue;

    private int _time;

    void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GameUiSystem>().Attach(this);
        
        _text = GetComponent<TextMeshProUGUI>();
        _timeValue = new StringBuilder(5);
    }

    public void UpdateTimer(int seconds)
    {
        if(_time == seconds)
            return;
        _time = seconds;
        var minutes = seconds / 60;
        seconds %= 60;

        _timeValue.Length = 0;
        _timeValue.Append(minutes.ToString("00"));
        _timeValue.Append(':');
        _timeValue.Append(seconds.ToString("00"));
        _text.text = _timeValue.ToString();
    }
}
