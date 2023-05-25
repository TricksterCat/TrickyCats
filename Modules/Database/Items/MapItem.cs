using GameRules.Scripts.Modules.Database.SupportClasses;
using UnityEngine;

namespace GameRules.Scripts.Modules.Database.Items
{
    [CreateAssetMenu(menuName = "GameRules/Items/New Map")]
    public class MapItem : BaseItem
    {
        public override ItemType Type => ItemType.Map;
        
        [SerializeField]
        private SceneReference _scene;
        
        public SceneReference Scene => _scene;
        
#if UNITY_EDITOR
        private void Reset()
        {
            UnityEditor.AssetDatabase.SetLabels(this, new[]
            {
                "maps"
            });
        }
#endif
    }
}