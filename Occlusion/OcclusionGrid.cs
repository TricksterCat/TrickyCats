using GameRules.Scripts.Extensions;
using UnityEngine;

public class OcclusionGrid : MonoBehaviour
{
    [SerializeField]
    private Vector2Int _size;
    [SerializeField]
    private Collider _world;

    [ContextMenu("Clear")]
    private void Clear()
    {
        var occlusions = GetComponentsInChildren<OcclusionArea>();
        for (int i = occlusions.Length - 1; i >= 0; i--)
            DestroyImmediate(occlusions[i].gameObject);

    }
    
    [ContextMenu("UpdateGrid")]
    private void UpdateGrid()
    {
        Clear();
        
        var bounds = _world.bounds;
        var size = bounds.size.To2D();


        var w = size.x / _size.x;
        var h = size.y / _size.y;
        var start = new Vector3(_world.bounds.min.x + w / 2, 8, _world.bounds.max.z - h / 2);
        
        for (int x = 0 ; x < _size.x; x++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                var area = new GameObject($"Area {x}_{y}").AddComponent<OcclusionArea>();
                area.transform.SetParent(transform);
                area.size = new Vector3(w, 10, h);
                area.center = start + new Vector3(x * w, 0, -h * y);
            }
        }
    }
}
