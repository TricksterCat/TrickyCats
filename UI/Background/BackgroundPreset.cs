using UnityEngine;

namespace GameRules.Scripts.UI.Background
{
    [CreateAssetMenu(menuName = "GameRules/Background preset", fileName = "background.asset")]
    public class BackgroundPreset : ScriptableObject
    {
        [SerializeField]
        private string _decor;
        [SerializeField]
        private Gradient _gradient;

        public string Decor => _decor;
        public Gradient Gradient1 => _gradient;
    }
}