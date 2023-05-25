
using UnityEngine;

namespace GameRules.Scripts.Modules.Database.Items
{
    [CreateAssetMenu(menuName = "GameRules/Items/DatabaseVersion")]
    public class DatabaseVersion : ScriptableObject
    {
        [SerializeField]
        private string _version;

        public string Version => _version;
    }
}