using System.Runtime.CompilerServices;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS;
using GameRules.Scripts.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace Players
{
    public class PlayerInputLogic : IInputLogic
    {
        private readonly IPlayerController _controller;

        private Vector2 _target;
        private Vector2 _fire;
        private bool _isDispose;
        
        private Vector2 _lastDir;
        
        public PlayerInputLogic(IPlayerController controller)
        {
            _controller = controller;
            
            //Object.Destroy((controller as MonoBehaviour)?.GetComponent<AIPath>());
            _target = Vector2.zero;
        }


        public void Update(ref JobHandle handle)
        {
            Update();
        }

        public void LateUpdate()
        {
            
        }

        public IPlayerController GetController()
        {
            return _controller;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            _target.x = SimpleInput.GetAxis("Horizontal");
            _target.y  = SimpleInput.GetAxis("Vertical");
            
            _controller.MoveInDirection(_target);
        }
        
        public void Dispose()
        {
            _isDispose = true;
        }
    }
}