using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameRules.Scripts.UI.Helper
{
    public class MonoPool<T> : IEnumerable<T>, IEnumerator<T> where T : MonoBehaviour
    {
        private Transform _root;
        private List<T> _items;
        private int _index;
        
        private readonly Func<Transform, T> _createPrefab;

        public MonoPool(Transform root, Func<Transform, T> createPrefab)
        {
            _createPrefab = createPrefab;
            _items = new List<T>();
            root.GetComponentsInChildren(_items);
            Reset();
        }

        public void Reset()
        {
            _index = -1;
        }

        public T MoveNextOrCreate()
        {
            if (!MoveNext())
                _items.Add(_createPrefab.Invoke(_root));
            return Current;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            
        }

        public bool MoveNext()
        {
            return ++_index < _items.Count;
        }

        public T Current => _items[_index];
        object IEnumerator.Current => _items[_index];
        
        public int Count => _items.Count;

        public IEnumerable<T> ToEnd()
        {
            for (int i = _index + 1; i < _items.Count; i++)
                yield return _items[i];
            
            _index = _items.Count;
        }
    }
}