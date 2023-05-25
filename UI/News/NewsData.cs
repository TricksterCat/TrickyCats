using System;
using System.Collections.Generic;
using Firebase.Messaging;
using GameRules.Firebase.Runtime;
using UnityEngine;

namespace GameRules.Scripts.UI.News
{
    public static class NewsData
    {
        private static List<NewsModel> _news = new List<NewsModel>();
        
        public static int LastVisitNewsIndex
        {
            get => PlayerPrefs.GetInt(nameof(LastVisitNewsIndex), 0);
            set => PlayerPrefs.SetInt(nameof(LastVisitNewsIndex), value);
        }
        
        public static int LastIndex { get; private set; }
        public static IReadOnlyList<NewsModel> News => _news;
        
        public static event Action<NewsModel> GetNewNewsEvent;

        public static async void Initialize()
        {
            var news = await ServerRequest.Instance.GetAllNews();
            _news.AddRange(news.items);
            LastIndex = news.index;
            if (LoadingController.IsFistRun)
                LastVisitNewsIndex = LastIndex;

            if (GetNewNewsEvent != null)
            {
                for (int i = 0; i < _news.Count; i++)
                    GetNewNewsEvent.Invoke(_news[i]);
            }
            
            //FirebaseApplication.MessageReceived += OnMessageReceived;
            //TODO: подписывемся на пуш
        }
    }
}