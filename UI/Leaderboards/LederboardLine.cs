using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LederboardLine : MonoBehaviour
{
    [SerializeField]
    private Image[] _colorImages;
    
    [SerializeField]
    private Image _icon;

    [SerializeField]
    private TextMeshProUGUI _nameLabel;
    [SerializeField]
    private TextMeshProUGUI _score;

    [SerializeField]
    private Image _isMain;

    public void SetOffset(Vector2 offset)
    {
        (transform.GetChild(0) as RectTransform).anchoredPosition = offset;
    }

    public void SetColors(Color color, Sprite icons)
    {
        foreach (var colorImage in _colorImages)
            colorImage.color = color;

        _icon.sprite = icons;
    }

    public void SetValues(string name, int score, bool isMain)
    {
        _nameLabel.SetText(name);
        _score.SetText(score.ToString());

        _isMain.enabled = isMain;
    }
}
