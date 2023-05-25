using System.Collections.Generic;

namespace GameRules.Scripts.Modules.Database
{
    public class NewsItem
    {
        public NewsItem(string title, string message)
        {
            Title = title;
            Message = message;
        }

        public string Title { get; }
        public string Message { get; }
    }
    
    public static class News
    {
        public static IReadOnlyCollection<NewsItem> Data { get; }


    }
}