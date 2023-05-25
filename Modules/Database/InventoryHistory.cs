using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Messaging;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Pool;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace GameRules.Scripts.Modules.Database
{
    public static class InventoryHistory
    {
        public static int LastIndex { get; private set; }
        private static int _lastNotifyIndex;
        
        private static readonly Queue<JObject> _diffs = new Queue<JObject>();
        public static IInventoryDiffsListener Listener { get; private set; }
        public static bool HaveDiffs => _diffs.Count > 0;
        public static int ProcessedDiffIndex { get; private set; }

        public static event Action<string> RewardWithReasonEvent; 
        
        public static void Load()
        {
            LastIndex = PlayerPrefs.GetInt("InventoryIndex", 0);
            //TODO: Сохранять ли данных (уведомления о изменениях) до закрытия?
            
            UpdateManager.Instance.OnApplicationDataSave += Save;
        }

        private static void Save()
        {
            PlayerPrefs.SetInt("InventoryIndex", LastIndex);
        }

        public static void InjectListener(IInventoryDiffsListener listener)
        {
            Listener = listener;
        }

        public static void UpdateDiffs(int index, JArray jArray, bool isForce)
        {
            if (isForce)
            {
                _diffs.Clear();
                LastIndex = 0;
                _lastNotifyIndex = 0;
            }
            
            if(LastIndex >= index || _lastNotifyIndex > index)
                return;
            
            int i = math.max(0, jArray.Count + LastIndex - index);
            LastIndex = index;
            Save();
            for (int iMax = jArray.Count; i < iMax; i++)
            {
                var diff = (JObject) jArray[i];
                var reason = (string) diff["reason"];
                RewardWithReasonEvent?.Invoke(reason);
                _diffs.Enqueue(diff);
            }

            if (isForce && RemoteConfig.GetBool("IsReadPushGetDiff"))
                FirebaseApplication.MessageReceived += OnMessageReceived;

            if (_diffs.Count > 0)
            {
                if (isForce)
                    ProcessedDiffIndex = _diffs.Count;
                
                Listener?.Change();
            }
        }

        private static void OnMessageReceived(MessageReceivedEventArgs messageReceivedEvent)
        {
            var message = messageReceivedEvent.Message;
            var data = message.Data;
            if(data.TryGetValue("sessionId", out var sessionId) && sessionId != ServerRequest.SessionId)
                return;

            if (data.TryGetValue("type", out var type) && type == "new_diffs")
            {
                var index = (int)JObject.Parse(data["content"])["index"];
                if(_lastNotifyIndex >= index)
                    return;
                
                _lastNotifyIndex = index;
                
                ServerRequest.Instance.RequestDiffsNow();
            }
        }
        
        public static JObject[] GetDiffs()
        {
            if (_diffs.Count > 0)
            {
                var result = _diffs.ToArray();
                _diffs.Clear();
                ProcessedDiffIndex = 0;
                return result;
            }
            return null;
        }
    }
}