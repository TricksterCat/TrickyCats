using System.Collections;
using System.Collections.Generic;
using System.Text;
using GameRules.Core.Runtime;
using GameRules.Scripts.Modules.Game;
using TMPro;
using UnityEngine;

public class Units : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private IWorldGenerator _world;

    private StringBuilder _timeValue;
    private int _defLength;

    private int _count;

    void Start()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _world = App.GetModule<IWorldGenerator>();
        _timeValue = new StringBuilder("Count: ", "Count: ".Length + 4);
        _defLength = _timeValue.Length;
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
