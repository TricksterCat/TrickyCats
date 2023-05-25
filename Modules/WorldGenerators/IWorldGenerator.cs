using Core.Base.Attributes;
using Core.Base.Modules;
using GameRules.Scripts.ECS.Game.Components;
using UnityEngine;

[BaseModule]
public interface IWorldGenerator : IModule
{
    void Fill(ref GameSetting gameSetting);
}
