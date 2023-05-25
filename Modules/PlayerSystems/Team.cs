using System;
using GameRules;
using GameRules.Scripts.Modules.Database.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRulez.Modules.PlayerSystems
{
    [Serializable, HideLabel]
    public sealed class Team
    {
        [NonSerialized]
        private string _name;
        
        [SerializeField]
        private Color _playerColor;
        [SerializeField]
        private Color _unitColor;
        
        public string Name => _name;
        
        public Color PlayerColor  => _playerColor;
        public Color UnitColor  => _unitColor;
        public PlayerItem Skin { get; private set; }

        public override string ToString()
        {
            return $"Team_{_name}";
        }

        public void UpdateName(string nickName)
        {
            _name = nickName.TrimEnd();
        }

        public override int GetHashCode()
        {
            if (_name == null)
                return 0;
            return _name.GetHashCode();
        }

        public void SetSkin(PlayerItem skin)
        {
            Skin = skin;
        }
    }
}