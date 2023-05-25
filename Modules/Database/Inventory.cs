using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GameRules.Analytics.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.Modules.Database.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameRules.Scripts.Modules.Database
{
    public static class Inventory
    {
        public static ChangeProperty<int> SoftWallet { get; }
        public static ChangeProperty<int> HardWallet { get; }
        public static ChangeProperty<int> Crowd { get; }
        public static ChangeProperty<int> RouletteTickets { get; }

        public static ChangeReferenceProperty<PlayerItem> PlayerSkin { get; }
        public static ChangeReferenceProperty<UnitItem> UnitSkin { get; }
        public static int ItemsCount => _items.Count;

        public static event Action ItemsChangeEvent;
        private static readonly HashSet<string> _items = new HashSet<string>();

        private static Unity.Mathematics.Random _random;
        private static readonly string[] _lastMaps = new string[4];
        private static int _mapIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<string> AvailabilityItems() => _items;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsItem(string item) => _items.Contains(item);

        public static bool IsAvailability(string id)
        {
            if (!Database.All.TryGetValue(id, out var item))
                return false;
            return IsAvailability(item);
        }

        public static bool IsAvailability(BaseItem item)
        {
            return item.Type == ItemType.Character && GetOrPush.UnlockedAllSkins || ContainsItem(item.Id) || (item.Flags & ItemFlags.AvailabilityItem) != 0;
        }

        public static MapItem GetRandomMap()
        {
            var maps = Database.Maps;
            var availabilityMaps = TmpList<MapItem>.Get();
            for (int i = 0; i < maps.Count; i++)
            {
                if (IsAvailability(maps[i]))
                    availabilityMaps.Add(maps[i]);
            }

            MapItem result;
            if (availabilityMaps.Count > 1)
            {
                var min = 1 / (_lastMaps.Length - 1f) + 0.01f;
                bool reRandom;
                do
                {
                    result = availabilityMaps[_random.NextInt(0, availabilityMaps.Count)];
                    var mapName = result.Id;
                    _lastMaps[_mapIndex] = mapName;
                    var repeats = _lastMaps.Count(item => item == mapName) - 1f;
                    var value = repeats / (_lastMaps.Length - 1f);
                    reRandom = value > _random.NextFloat(min, 1.01f);
                } while (reRandom);
                _mapIndex = (_mapIndex + 1) % _lastMaps.Length;
            }
            else 
                result = availabilityMaps[_random.NextInt(0, availabilityMaps.Count)];
            
            TmpList<MapItem>.Release(availabilityMaps);
            return result;
        }

        public static void UpdatePlayerSkin(PlayerItem item)
        {
            if(!IsAvailability(item))
                return;
            PlayerSkin.Value = item;
        }
        
        public static void UpdateUnitSkin(UnitItem item)
        {
            if(!IsAvailability(item))
                return;
            UnitSkin.Value = item;
        }

        static Inventory()
        {
            SoftWallet = new ChangeProperty<int>();
            HardWallet = new ChangeProperty<int>();
            Crowd = new ChangeProperty<int>();
            RouletteTickets = new ChangeProperty<int>();
            
            PlayerSkin = new ChangeReferenceProperty<PlayerItem>();
            UnitSkin = new ChangeReferenceProperty<UnitItem>();
            
            _random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, Int32.MaxValue));
        }

        private static void SaveInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }
        
        private static void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public static void Load()
        {
            var all = Database.All;
            
            var playerSkinId = PlayerPrefs.GetString(nameof(PlayerSkin), null);
            if (playerSkinId != null && all.TryGetValue(playerSkinId, out var playerSkin) && IsAvailability(playerSkin))
                PlayerSkin.Value = (PlayerItem)playerSkin;
            
            var unitSkinId = PlayerPrefs.GetString(nameof(UnitSkin), null);
            if (unitSkinId != null && all.TryGetValue(unitSkinId, out var unitSkin) && IsAvailability(unitSkin))
                UnitSkin.Value = (UnitItem)unitSkin;
            
            PlayerSkin.OnChange += value =>
            {
                SaveString(nameof(PlayerSkin), value.Id);
                
                CrowdAnalyticsMediator.Instance.BeginEvent(BaseEvents.SelectContent)
                    .SetContentType("character")
                    .SetItemId(value.Id)
                    .CompleteBuild();
            };
            UnitSkin.OnChange += value =>
            {
                SaveString(nameof(UnitSkin), value.Id);
                
                CrowdAnalyticsMediator.Instance.BeginEvent(BaseEvents.SelectContent)
                    .SetContentType("minion")
                    .SetItemId(value.Id)
                    .CompleteBuild();
            };

            if (PlayerSkin.Value == null)
                PlayerSkin.Value = Database.Players.FirstOrDefault(IsAvailability);
            
            if (UnitSkin.Value == null)
                UnitSkin.Value = Database.Units.FirstOrDefault(IsAvailability);
        }

        public static void SyncItems(JArray jArray)
        {
            var items = _items;
            items.Clear();
            for (int i = 0; i < jArray.Count; i++)
                items.Add((string)jArray[i]);
            
            ItemsChangeEvent?.Invoke();
        }

        public static void SyncWallets(JObject wallets)
        {
            if (wallets.TryGetValue("soft", StringComparison.Ordinal, out var jSorft))
                SoftWallet.Value = (int)jSorft;
            if (wallets.TryGetValue("hard", StringComparison.Ordinal, out var jHard))
                HardWallet.Value = (int)jHard;
            if (wallets.TryGetValue("rouletteSpin", StringComparison.Ordinal, out var jTickets))
                RouletteTickets.Value = (int) jTickets;
            if (wallets.TryGetValue("crowd", out var jCrowd))
                Crowd.Value = (int)jCrowd;
        }

        public static bool IsSelect(BaseItem itemModel)
        {
            switch (itemModel.Type)
            {
                default:
                    return false;
                case ItemType.Character:
                    return PlayerSkin.Value == itemModel;
                case ItemType.Minion:
                    return UnitSkin.Value == itemModel;
            }
        }

        public static void AddItem(string item)
        {
            if(_items.Add(item))
                ItemsChangeEvent?.Invoke();
        }
    }
}