using System.Collections;
using System.Collections.Generic;
using GameRules;
using GameRules.Scripts.Modules.Database;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelInfo : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI _textReward;
    [SerializeField]
    public Image _iconReward;

    [SerializeField]
    public TextMeshProUGUI _levelValue;
    [SerializeField]
    public Image[] _colorImages;
    [SerializeField]
    public TextMeshProUGUI[] _colorTexts;
    
    [SerializeField]
    public TextMeshProUGUI _wallet;

    [SerializeField]
    private GameObject _soon;
    [SerializeField]
    private GameObject _crowd;
    [SerializeField]
    private GameObject _skin;

    [SerializeField]
    public GameObject _isComplete;

    public void SetValue(JObject levelInfo, int level, bool isCompleteLevel)
    {
        _levelValue.text = (level + 1).ToString();

        var crowd = false;
        var isSkin = false;
        var soon = false;
        var isWallet = false;

        if (levelInfo != null)
        {
            levelInfo.TryGetValue("value", out var jValue);
            var type = levelInfo["type"].ToString();

            switch (type)
            {
                case "soon":
                    //_textReward.text = I2.Loc.LocalizationManager.GetTranslation("Progress/SOON");
                    _levelValue.text = "N";
                    soon = true;
                    break;
                case "item":
                    var skinName = jValue.ToString();
                    isSkin = Database.All.TryGetValue(skinName, out var skin);
                    if(isSkin)
                        _iconReward.sprite = skin.Icon;
                    break;
                case "crowd":
                    _textReward.text = $"+{jValue}";
                    crowd = true;
                    break;
                case "soft":
                case "hard":
                case "rouletteSpin":
                    isWallet = true;
                    _wallet.text = $"+{jValue} <sprite name={type}>";
                    break;
            }
        }
        
        _skin.gameObject.SetActive(isSkin);
        _crowd.gameObject.SetActive(crowd);
        _soon.gameObject.SetActive(soon);
        _wallet.gameObject.SetActive(isWallet);
            
        _isComplete.SetActive(isCompleteLevel);
    }

    public void SetColor(Color color)
    {
        for (int i = 0; i < _colorImages.Length; i++)
        {
            var c = _colorImages[i].color;
            c.r = color.r;
            c.g = color.g;
            c.b = color.b;
            
            _colorImages[i].color = c;
        }

        for (int i = 0; i < _colorTexts.Length; i++)
        {
            var c = _colorTexts[i].color;
            c.r = color.r;
            c.g = color.g;
            c.b = color.b;
            
            _colorTexts[i].color = c;
        }
    }
}
