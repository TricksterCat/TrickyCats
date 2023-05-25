/*
 * Created by jiadong chen
 * http://www.chenjd.me
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using GameRules.Scripts.AnimationSystem.AnimMapBakerTools;

public class AnimMapBakerWindow : EditorWindow {

    private enum SaveStrategy
    {
        AnimMap,//only anim map
    }

    #region 字段

    public static GameObject targetGo;
    private static AnimMapBaker baker;
    private static string path;
    private static string subPath = "SubPath";
    private static SaveStrategy stratege = SaveStrategy.AnimMap;
    private static Shader animMapShader;

    private TextureFormat _textureFormat = TextureFormat.RGBAHalf;

    #endregion


    #region  方法

    [MenuItem("Window/AnimMapBaker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AnimMapBakerWindow));
        baker = new AnimMapBaker();
    }

    private void OnEnable()
    {
        path = EditorPrefs.GetString($"{nameof(AnimMapBakerWindow)}_Path", "DefaultPath");
    }

    void OnGUI()
    {
        targetGo = (GameObject)EditorGUILayout.ObjectField(targetGo, typeof(GameObject), true);
        subPath = targetGo == null ? subPath : targetGo.name;
        EditorGUILayout.LabelField(string.Format("output path:{0}", Path.Combine(path, subPath)));
        EditorGUI.BeginChangeCheck();
        path = EditorGUILayout.TextField(path);
        if(EditorGUI.EndChangeCheck())
            EditorPrefs.SetString($"{nameof(AnimMapBakerWindow)}_Path", path);
        subPath = EditorGUILayout.TextField(subPath);

        stratege = (SaveStrategy)EditorGUILayout.EnumPopup("output type:", stratege);
        _textureFormat = (TextureFormat)EditorGUILayout.EnumPopup("Texture format", _textureFormat);

        if (GUILayout.Button("Bake"))
        {
            if(targetGo == null)
            {
                EditorUtility.DisplayDialog("err", "targetGo is null！", "OK");
                return;
            }
            
            if(animMapShader == null)
                animMapShader = Shader.Find("Animation/AnimationSimple");

            if(baker == null)
            {
                baker = new AnimMapBaker();
            }

            baker.SetAnimData(targetGo, _textureFormat);

            List<BakedData> list = baker.Bake();

            if(list != null)
            {
                for(int i = 0; i < list.Count; i++)
                {
                    BakedData data = list[i];
                    Save(ref data);
                }
            }
        }
    }


    private void Save(ref BakedData data)
    {
        switch(stratege)
        {
            case SaveStrategy.AnimMap:
                SaveAsAsset(ref data);
                break;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private Texture2D SaveAsAsset(ref BakedData data)
    {
        string folderPath = CreateFolder();
        Texture2D animMap = new Texture2D(data.animMapWidth, data.animMapHeight, data.Format, false);
        animMap.LoadRawTextureData(data.rawAnimMap);
        
        var path = Path.Combine(folderPath, data.name + ".asset");
        
        AssetDatabase.DeleteAsset(path);
        
        var bakeAnimation = CreateInstance<BakeAnimation>();
        bakeAnimation.Texture = animMap;
        bakeAnimation.RemapRange = data.RemapRange;
        bakeAnimation.RAnimLength = 1f / data.animLen;

        animMap.name = "animTexture";
        
        AssetDatabase.CreateAsset(bakeAnimation, path);
        AssetDatabase.AddObjectToAsset(animMap, path);
        
        return animMap;
    }

    private Material SaveAsMat(ref BakedData data)
    {
        if(animMapShader == null)
        {
            EditorUtility.DisplayDialog("err", "shader is null!!", "OK");
            return null;
        }

        if(targetGo == null || !targetGo.GetComponentInChildren<SkinnedMeshRenderer>())
        {
            EditorUtility.DisplayDialog("err", "SkinnedMeshRender is null!!", "OK");
            return null;
        }

        SkinnedMeshRenderer smr = targetGo.GetComponentInChildren<SkinnedMeshRenderer>();
        Material mat = new Material(animMapShader);
        Texture2D animMap = SaveAsAsset(ref data);
        mat.SetTexture("_BaseMap", smr.sharedMaterial.mainTexture);
        mat.SetTexture("_AnimMap", animMap);
        mat.SetFloat("_RAnimLen", data.animLen > 0.01 ? 1f / data.animLen : 0f);

        string folderPath = CreateFolder();
        AssetDatabase.CreateAsset(mat, Path.Combine(folderPath, data.name + ".mat"));

        return mat;
    }

    private void SaveAsPrefab(ref BakedData data)
    {
        Material mat = SaveAsMat(ref data);

        if(mat == null)
        {
            EditorUtility.DisplayDialog("err", "mat is null!!", "OK");
            return;
        }

        GameObject go = new GameObject();
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshFilter>().sharedMesh = targetGo.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

        string folderPath = CreateFolder();
        PrefabUtility.CreatePrefab(Path.Combine(folderPath, data.name + ".prefab").Replace("\\", "/"), go);
    }

    private string CreateFolder()
    {
        string folderPath = Path.Combine("Assets", path,  subPath);
        var paths = folderPath.Split(Path.DirectorySeparatorChar);

        StringBuilder newPath = new StringBuilder(folderPath.Length);
        
        for (int i = 0; i < paths.Length; i++)
        {
            var old = newPath.ToString();
            var folder = paths[i];
            newPath.Append(folder);
            if(!AssetDatabase.IsValidFolder(newPath.ToString()))
                AssetDatabase.CreateFolder(old, folder);
            newPath.Append(Path.DirectorySeparatorChar);
        }
        
        AssetDatabase.Refresh();
        return folderPath;
    }

    #endregion


}