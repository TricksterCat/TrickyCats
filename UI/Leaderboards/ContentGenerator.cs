using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ContentGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject _prefab;

    [System.Serializable]
    private struct ColorInfo
    {
        public Color Color;
        public Sprite Icon;
    }
    
    [SerializeField]
    private ColorInfo[] _colors;
    [SerializeField]
    private Vector2[] _offsets;

    [SerializeField]
    private int _items;
    
    [Button]
    private void Execute()
    {
        var childs = transform.childCount;
        for (int i = childs - 1; i >= 0; i--)
        {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < _items; i++)
        {
#if UNITY_EDITOR
            var prefab = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_prefab, transform);
#else
            var prefab = Instantiate(_prefab, transform);
#endif
            var line = prefab.GetComponent<LederboardLine>();

            var colorInfo = _colors[i % _colors.Length];
            var offset = _offsets[i % _offsets.Length];
            
            line.SetColors(colorInfo.Color, colorInfo.Icon);
            line.SetOffset(offset);
        }
    }
}
