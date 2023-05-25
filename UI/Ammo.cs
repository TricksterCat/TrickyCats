using System.Text;
using Core.Base.Modules;
using GameRules.Core.Runtime;
using GameRules.Scripts.Weapons;
using GameRulez.Modules.PlayerSystems;
using TMPro;
using UnityEngine;

public class Ammo : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _text;

    private StringBuilder _stringBuilder;
    private int _defLength;
    private int _oldValue;

    private void Start()
    {
        _stringBuilder = new StringBuilder(_text.text);
        _defLength = _stringBuilder.Length;
        
        _oldValue = -1;
        UpdateValue(_oldValue);
    }

    private void Update()
    {
        var playerSystem = App.GetModule<IPlayerSystem>();
        if (playerSystem == null || playerSystem.CompareStatus(ModuleStatus.Disable))
            UpdateValue(0);
        else
        {
            var weaponData = App.GetModule<IWeaponSystem>().GetWeaponData(playerSystem.PlayerTeamIndex);
            UpdateValue(weaponData.Ammo);
        }
        
    }

    private void UpdateValue(int ammo)
    {
        if(ammo == _oldValue)
            return;
        _oldValue = ammo;
        
        _stringBuilder.Length = _defLength;
        _stringBuilder.Append(ammo);
        _text.text = _stringBuilder.ToString();
    }
}
