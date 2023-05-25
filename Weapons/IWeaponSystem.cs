using Core.Base.Attributes;
using Core.Base.Modules;
using GameRules.Scripts.ECS.Game.Components;
using Players;

namespace GameRules.Scripts.Weapons
{
    [BaseModule]
    public interface IWeaponSystem : IModule
    {
        IWeapon DefaultWeapon { get; }
        
        IWeapon GetWeapon(string name);

        void UpdateWeapon(IWeapon weapon, int player);
        bool TryFire(int player);
        PlayerWeaponData GetWeaponData(int playerTeam);
        bool CanFire(int playerTeam);
        void Fill(ref GameSetting gameSetting);
    }
}
