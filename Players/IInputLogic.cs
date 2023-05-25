using System;
using Unity.Jobs;

namespace Players
{
    public interface IInputLogic : IDisposable
    {
        void Update(ref JobHandle handle);
        void LateUpdate();
        
        IPlayerController GetController();
    }
}