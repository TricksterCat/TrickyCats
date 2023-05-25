using UnityEngine;

namespace GameRules.Scripts.UI.News
{
    [System.Serializable]
    public struct PriorityInfo
    {
        public string PriorityId;
                
        public string StatusText;
        public Color StatusColor;
        public Color TitleBoxColor;
    }
}