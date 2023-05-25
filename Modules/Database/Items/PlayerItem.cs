using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameRules.Scripts.Modules.Database.Items
{
    [CreateAssetMenu(menuName = "GameRules/Items/New Player")]
    public class PlayerItem : BaseItem
    {
        public override ItemType Type => ItemType.Character;
        
        [SerializeField, FoldoutGroup("Previews")]
        private Rect _previewOffsets;
        
        [SerializeField]
        private AssetReferenceGameObject _prefab;
        public AssetReferenceGameObject PrefabReference => _prefab;
        
        public Rect PreviewOffsets => _previewOffsets;
        
#if UNITY_EDITOR
        private void Reset()
        {
            UnityEditor.AssetDatabase.SetLabels(this, new[]
            {
                "players"
            });
        }
#endif
    }
}
