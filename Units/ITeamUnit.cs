using UnityEngine;

namespace GameRulez.Units
{
    public interface ITeamUnit
    {
        Vector2 Position { get; }
        
        string TeamName { get; }
        int TeamIndex { get; }
    }
}