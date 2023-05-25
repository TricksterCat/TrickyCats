using System;

namespace GameRules.Scripts.UI.News
{
    public class NewsModel : IComparable<NewsModel>
    {
        public DateTime Date { get; }
        public string Title { get; }
        public string Message { get; }
        
        public string StatusType { get; }
        
        public NewsModel(DateTime date, string statusType, string title, string message)
        {
            Date = date;
            Title = title;
            Message = message;
            StatusType = statusType;
        }

        public int CompareTo(NewsModel other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Date.CompareTo(other.Date);
        }
    }
}