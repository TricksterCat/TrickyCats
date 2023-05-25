using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace GameRules.Scripts.Modules.Database.SupportClasses
{
    [OdinDrawer]
    public class SceneReferenceDrawer : OdinValueDrawer<SceneReference>
    {
        private Object _scene;

        protected override void Initialize()
        {
            _scene = ValueEntry.SmartValue.editorAsset;

            if (_scene != null)
            {
                var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
                var entry = aaSettings.FindAssetEntry(ValueEntry.SmartValue.AssetGUID);
                if (entry == null)
                {
                    entry = aaSettings.CreateOrMoveEntry (ValueEntry.SmartValue.AssetGUID, aaSettings.DefaultGroup);
                    entry.address = AssetDatabase.GetAssetOrScenePath(_scene);
                }
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            _scene = EditorGUILayout.ObjectField(label, _scene, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue.SetEditorAsset(_scene);
                if (_scene != null)
                {
                    var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
                    var entry = aaSettings.FindAssetEntry(ValueEntry.SmartValue.AssetGUID);
                    if (entry == null)
                    {
                        entry = aaSettings.CreateOrMoveEntry (ValueEntry.SmartValue.AssetGUID, aaSettings.DefaultGroup);
                        entry.address = AssetDatabase.GetAssetOrScenePath(_scene);
                        
                        ValueEntry.SmartValue.SetEditorAsset(_scene);
                    }
                }

                Property.RecordForUndo("Update SceneReference value");
            }
        }
    }
}