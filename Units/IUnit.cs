using Players;
using UnityEngine;

namespace GameRulez.Units
{
    public interface IUnit : ITeamUnit
    {
        IPlayerController Player { get; }
        Vector2 Position { get; }
        Vector2 Direction { get; }
        bool CanTarget(IPlayerController controller);
        void ForceUpdateTeam(byte teamIndex);
    }
}