using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class GroupGameObjectsToPrefabWindow : OdinEditorWindow
{
    [MenuItem("Tools/GameRulez/GroupGameObjectsToPrefab")]
    private static void Create()
    {
        var window = GetWindow<GroupGameObjectsToPrefabWindow>(true, "Group GameObjects to Prefab");
        window.ShowUtility();
    }

    [SceneObjectsOnly]
    public GameObject[] GameObjects;
    [FolderPath]
    public string Folder;

    private bool isDisable => string.IsNullOrWhiteSpace(Folder);

    [Button, DisableIf(nameof(isDisable)), InfoBox("Выберите папку, куда экспортировать", InfoMessageType.Error, nameof(isDisable))]
    public void Run()
    {
        if(GameObjects == null || GameObjects.Length == 0)
            return;

        var count = GameObjects.Length;
        for (int i = 0; i < count; i++)
        {
            var go = GameObjects[i];
            if(go == null)
                return;

            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{Folder}/{go.name}.prefab", InteractionMode.AutomatedAction);
            EditorUtility.DisplayProgressBar("GroupGameObjectsToPrefab", $"Convert gameObject to prefabs {i}/{count}", (float)(i+1) / count);
        }
        EditorUtility.ClearProgressBar();
    }
}
