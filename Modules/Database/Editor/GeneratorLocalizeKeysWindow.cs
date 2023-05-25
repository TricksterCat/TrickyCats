using System.Collections.Generic;
using System.Linq;
using GameRules.Scripts.Modules.Database.Items;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class GeneratorLocalizeKeysWindow : Sirenix.OdinInspector.Editor.OdinEditorWindow
{
    public LanguageSourceAsset Source;
    
    [FolderPath, SerializeField]
    private string _folder = "Assets/GameRules/Datebase/Groups";

    [Button]
    private void Execute()
    {
        var guids = AssetDatabase.FindAssets("t: BaseItem", new[] {_folder});
        if (guids.Length == 0)
        {
            Debug.LogError("Items not found!");
            return;
        }


        List<string> newTermDatas = new List<string>();
        var data = Source.mSource;
        for (int i = 0; i < guids.Length; i++)
        {
            var guid = guids[i];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<BaseItem>(path);
            var ser = new SerializedObject(asset);

            var titleProp = ser.FindProperty("_titleKey");
            var descProp = ser.FindProperty("_descriptionKey");
            
            var titleKey = string.IsNullOrEmpty(titleProp.stringValue) ? $"Items/{asset.Id}_TITLE" : titleProp.stringValue;
            var descKey = string.IsNullOrEmpty(descProp.stringValue) ? $"Items/{asset.Id}_DESC" : descProp.stringValue;
            
            newTermDatas.Add(titleKey);
            newTermDatas.Add(descKey);

            bool isChange = false;
            if (string.IsNullOrEmpty(titleProp.stringValue))
            {
                titleProp.stringValue = titleKey;
                isChange = true;
            }

            if (string.IsNullOrEmpty(descProp.stringValue))
            {
                descProp.stringValue = descKey;
                isChange = true;
            }

            if (isChange)
                ser.ApplyModifiedProperties();
        }

        var old = data.mTerms.Select(term => term.Term).ToArray();
        for (var i = 0; i < old.Length; i++)
        {
            if(!newTermDatas.Contains(old[i]))
                data.RemoveTerm(old[i]);
        }

        for (int i = 0; i < newTermDatas.Count; i++)
        {
            if (!data.ContainsTerm(newTermDatas[i]))
            {
                var term = data.AddTerm(newTermDatas[i]);
                term.Description = string.Empty;
            }
        }
        
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/Tools/Generate LanguageSource")]
    public static void CreateWindow()
    {
        var source = (LanguageSourceAsset) Selection.activeObject;
        var window = GetWindow<GeneratorLocalizeKeysWindow>(true, "Generate LanguageSource");
        window.Source = source;
        
        window.ShowUtility();
    }


    [MenuItem("Assets/Tools/Generate LanguageSource", true)]
    public static bool CreateWindowValidation()
    {
        return Selection.activeObject is LanguageSourceAsset;
    }
}
