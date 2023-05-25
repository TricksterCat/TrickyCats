using GameRules.Scripts.ECS.Render;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.Modules.Database.Items
{
    [CreateAssetMenu(menuName = "GameRules/Items/New Unit")]
    public class UnitItem : BaseItem
    {
        public override ItemType Type => ItemType.Minion;
        
        [SerializeField, FoldoutGroup("Previews")]
        private Rect _previewOffsets;
        
        [SerializeField]
        private UnitRenderSystem.DrawSettings _drawSettings;

        public Rect PreviewOffsets => _previewOffsets;
        public UnitRenderSystem.DrawSettings DrawSettings => _drawSettings;

        
#if UNITY_EDITOR
        private void Reset()
        {
            UnityEditor.AssetDatabase.SetLabels(this, new[]
            {
                "units"
            });
        }
#endif
    }
}