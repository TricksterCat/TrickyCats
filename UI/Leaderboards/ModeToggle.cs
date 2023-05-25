using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeToggle : MonoBehaviour
{
    [SerializeField]
    private Image[] _images;
    [SerializeField]
    private TextMeshProUGUI _label;

    [SerializeField]
    private Color _onColor;
    [SerializeField]
    private Color _offColor;
    
    public void Toogle(bool value)
    {
        var color = value ? _onColor : _offColor;
        for (int i = 0; i < _images.Length; i++)
            _images[i].color = color;
        _label.color = color;
    }
}
