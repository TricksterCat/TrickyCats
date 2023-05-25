using System.Collections;
using System.Collections.Generic;
using Core.Base.Attributes;
using Core.Base.Modules;
using Players;

namespace GameRulez.Modules.PlayerSystems
{
    [BaseModule]
    public interface IPlayerSystem : IModule
    {
        IPlayerController GetPlayer(int teamIndex);
        IPlayerController GetPlayer(string team);
        IEnumerable<Team> GetTeams();
        
        Team GetTeam(int team);

        int PlayersCount { get; }
        string PlayerTeam { get; }
        int PlayerTeamIndex { get; }
        IEnumerable<IPlayerController> GetPlayers();

        int TeamNameToIndex(string team);
        string TeamNameByIndex(int teamIndex);
        
        IEnumerator SpawnPlayers();
    }
}