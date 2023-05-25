using System;
using I2.Loc;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.Modules.Database.Items
{
    public enum ItemType
    {
        Character,
        Minion,
        Map
    }

    public struct ItemConditions
    {
        public ItemConditions(int softPrice, int hardPrice, int unlockLevel, bool inWheelOfFortune)
        {
            SoftPrice = softPrice;
            HardPrice = hardPrice;
            UnlockLevel = unlockLevel;
            InWheelOfFortune = inWheelOfFortune;
        }

        public int SoftPrice { get; }
        public int HardPrice { get; }
        public int UnlockLevel { get; }

        public bool InWheelOfFortune { get; }
    }
    
    public abstract class BaseItem : ScriptableObject, IEquatable<BaseItem>
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private ItemFlags _flags;
        
        [SerializeField, FoldoutGroup("Localize")]
        private string _titleKey;
        [SerializeField, FoldoutGroup("Localize")]
        private string _descriptionKey;
        
        [SerializeField, FoldoutGroup("Previews")]
        private Sprite _icon;
        [SerializeField, FoldoutGroup("Previews")]
        private Sprite _preview;

        public string Id => _id;
        public abstract ItemType Type { get; }
        public ItemFlags Flags => _flags;

        public Sprite Icon => _icon;
        public Sprite Preview => _preview;
        
        public string Title
        {
            get
            {
                var localize = Database.Localize;
                var termData = localize.GetTermData(_titleKey);
                if (termData == null)
                    return "Error! Please inform developers";
                var localizeIndex = localize.GetLanguageIndex(LocalizationManager.CurrentLanguage);
                if (localizeIndex == -1)
                    localizeIndex = 0;
                
                return termData.Languages[localizeIndex];
            }
        }

        public string Description
        {
            get
            {
                var localize = Database.Localize;
                var termData = localize.GetTermData(_descriptionKey);
                if (termData == null)
                    return "Error! Please inform developers";
                var localizeIndex = localize.GetLanguageIndex(LocalizationManager.CurrentLanguage);
                if (localizeIndex == -1)
                    localizeIndex = 0;
                
                return termData.Languages[localizeIndex];
            }
        }
        
        public ItemConditions Conditions { get; private set; }

        public void UpdateFromServer(JObject data)
        {
            if (data.TryGetValue("flags", out var flags))
                _flags = (ItemFlags)(int)flags;

            if (data.TryGetValue("condition", out var jConditionToken))
            {
                var jCondition = (JObject) jConditionToken;
                
                Conditions = new ItemConditions(
                    jCondition.TryGetValue("soft", out var jSoft) ? (int)jSoft : 0, 
                    jCondition.TryGetValue("hard", out var jHard) ? (int)jHard : 0,
                    jCondition.TryGetValue("unlockLevel", out var jUnlockLevel) ? (int)jUnlockLevel : 0,
                    jCondition.TryGetValue("wheelOfFortune", out var jWheelOfFortune) && (int)jWheelOfFortune == 1);
            }
        }

        public bool Equals(BaseItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && string.Equals(_id, other._id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BaseItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (_id != null ? _id.GetHashCode() : 0);
            }
        }
    }
}