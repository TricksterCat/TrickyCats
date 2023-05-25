using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.Pool
{
    public class PoolGameObjects : MonoBehaviour
    {
        [SerializeField]
        private string _id;

        [SerializeField, OnValueChanged(nameof(ChangeBaseGo))]
        private GameObject _base;

        [SerializeField, Range(1, 1000)]
        private int _size;

        [SerializeField, ReadOnly]
        private List<GameObject> _pool;

        private static Dictionary<string, PoolContainer> _poolObjects;

        static PoolGameObjects()
        {
            _poolObjects = new Dictionary<string, PoolContainer>();
        }
    
        private class PoolContainer
        {
            private GameObject _baseObject;
            private List<GameObject> _objects;
        
            public PoolContainer(GameObject @base, List<GameObject> pool)
            {
                _baseObject = @base;
                _objects = pool;
            }

            public GameObject Get(bool activate)
            {
                if (_objects.Count == 0)
                    return GameObject.Instantiate(_baseObject);
            
                var result = _objects[0];
                if(activate)
                    result.SetActive(true);
                Unity.Collections.ListExtensions.RemoveAtSwapBack(_objects, 0);
                return result;
            }

            public void Return(GameObject go)
            {
                go.SetActive(false);
                _objects.Add(go);
            }
        }
    
        private void Awake()
        {
            _poolObjects[_id] = new PoolContainer(_base, _pool);
        }

        private bool _canFill => _base != null;

        private void ChangeBaseGo()
        {
            ClearOld();
        }

        private void ClearOld()
        {
            if (_pool == null)
                return;
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                var go = _pool[i];
                DestroyImmediate(go);
            }
            _pool.Clear();
        }
    
        [Button, EnableIf(nameof(_canFill))]
        private void FillPool()
        {
            if(_pool == null)
                _pool = new List<GameObject>(_size);
        
            ClearOld();
        
            _pool.Capacity = _size;
            for (int i = 0; i < _size; i++)
            {
#if UNITY_EDITOR
                var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_base, transform);
#else
            var go = Instantiate(_base, transform);
#endif
                go.SetActive(false);
                _pool.Add(go);
            }
        }

        public static bool Contains(string id)
        {
            return _poolObjects != null && _poolObjects.ContainsKey(id);
        }

        public static void Destroy(GameObject gameObject, string id)
        {
            if(_poolObjects.TryGetValue(id, out var list))
                list.Return(gameObject);
        }

        public static GameObject GetNextObject(string id)
        {
            if (_poolObjects.TryGetValue(id, out var list))
                return list.Get(false);

            return null;
        }
    }
}
