using System;

namespace GameRules.Scripts.Modules.Database.Items
{
    [Flags]
    public enum ItemFlags
    {
        None,
        AvailabilityItem = 1 << 0,
        EventItem = 1 << 1,
        TimeLimitedItem = 1 << 2
    }
}