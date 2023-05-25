using System.Collections.Generic;
using GameRules;
using GameRules.Scripts.Players;
using GameRules.Scripts.Weapons;
using GameRulez.Modules.PlayerSystems;
using GameRulez.Units;
using Unity.Entities;
using UnityEngine;

namespace Players
{
    public interface IPlayerController : ITeamUnit
    {
        SpawnInfo SpawnInfo { get; set; }
        
        Transform transform { get; }
        float Speed { get; }
        
        Team Team { get; }
        int TeamSize { get; }
        int Score { get; set; }
        
        bool IsMain { get; }
        
        Vector2 Position { get; }

        void SetEnable(bool value);
        
        void MoveInDirection(Vector2 direction);
        void SetTeam(Team team, int index);
        void Fire();

        //void Attach(IUnit unit);
        //void Detach(IUnit unit);
        
        //IEnumerable<IUnit> GetUnits();
    }
}