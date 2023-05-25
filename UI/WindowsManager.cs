using System.Collections.Generic;

namespace GameRules.UI
{
    public static class WindowsManager
    {
        public static HashSet<string> ActiveWindows = new HashSet<string>();

        public static bool IsOnly(string name)
        {
            return ActiveWindows.Count == 1 && ActiveWindows.Contains(name);
        }

        public static bool IsNotOpen(string name)
        {
            return !ActiveWindows.Contains(name);
        }
    }
}