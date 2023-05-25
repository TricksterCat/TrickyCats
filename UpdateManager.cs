using System;
using System.Collections.Generic;
using Firebase.Crashlytics;
using GameRules.Firebase.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts
{
    public interface IUpdateInvoke : IEquatable<IUpdateInvoke>
    {
        int id { get; set; }
        
        void Update();
    }

    public class UpdateInvokeEqualityComparer : IEqualityComparer<IUpdateInvoke>
    {
        public bool Equals(IUpdateInvoke x, IUpdateInvoke y)
        {
            return ReferenceEquals(x, y) || x.id.Equals(y.id);
        }

        public int GetHashCode(IUpdateInvoke obj)
        {
            return obj == null ? 0 : obj.id;
        }
    }
    
    public class UpdateManager : MonoBehaviour
    {
        private static readonly Queue<Action> _nextFrame = new Queue<Action>();
        
        [ShowInInspector, ReadOnly]
        private static readonly LinkedList<IUpdateInvoke> _items = new LinkedList<IUpdateInvoke>();
        private static readonly List<IUpdateInvoke> _tmp = new List<IUpdateInvoke>();
        private static int id = 0;

        public event Action<bool> PauseStateChangeEvent;
        public event Action<bool> FocusChangeEvent;
        public event Action ApplicationQuitEvent;

        public event Action OnApplicationDataSave;

        private static UpdateManager _instance;
        public static UpdateManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameObject(nameof(UpdateManager), typeof(UpdateManager)).GetComponent<UpdateManager>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }


        public void Add(IUpdateInvoke updateInvoke)
        {
            if (_items.Contains(updateInvoke)) 
                return;
            
            updateInvoke.id = ++id;
            _items.AddLast(updateInvoke);
        }

        public void Remove(IUpdateInvoke updateInvoke)
        {
            _items.Remove(updateInvoke);
        }

        private void Update()
        {
            try
            {
                var nextFrame = _nextFrame;
                while (nextFrame.Count != 0)
                    nextFrame.Dequeue()?.Invoke();
            }
            catch(Exception ex)
            {
                FirebaseApplication.LogException(ex);
            }
            
            if(_items.Count == 0)
                return;

            if (_tmp.Capacity < _items.Count)
                _tmp.Capacity = _items.Count;

            _tmp.Clear();
            _tmp.AddRange(_items);
            foreach (var invoke in _tmp)
                invoke.Update();
        }

        private void OnApplicationQuit()
        {
            if(GetOrPush.RemoveAll)
                return;
            
            ApplicationQuitEvent?.Invoke();

            if (OnApplicationDataSave != null)
            {
                OnApplicationDataSave.Invoke();
                PlayerPrefs.Save();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            PauseStateChangeEvent?.Invoke(pauseStatus);

            if (pauseStatus && OnApplicationDataSave != null)
            {
                OnApplicationDataSave.Invoke();
                PlayerPrefs.Save();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            FocusChangeEvent?.Invoke(hasFocus);
            
            if (!hasFocus && OnApplicationDataSave != null)
            {
                OnApplicationDataSave.Invoke();
                PlayerPrefs.Save();
            }
        }
        
        

        public static void OnNextFrame(Action action)
        {
            _nextFrame.Enqueue(action);
        }
    }
}