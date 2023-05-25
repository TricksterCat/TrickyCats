using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace GameRules.Scripts.SubSceneHelper.Editor
{
    public class SubSceneGuidAttributeDrawer : OdinAttributeDrawer<SubSceneGuidAttribute, string>
    {
        private Object _sceneAsset;
        
        protected override void Initialize()
        {
            base.Initialize();
            var path = AssetDatabase.GUIDToAssetPath(ValueEntry.SmartValue);
            _sceneAsset = string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            _sceneAsset = EditorGUILayout.ObjectField(GUIHelper.TempContent("SubScene"), _sceneAsset, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = _sceneAsset == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_sceneAsset));
                ValueEntry.ApplyChanges();
            }
        }
    }
}