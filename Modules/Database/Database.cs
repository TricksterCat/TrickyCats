using System;
using System.Collections;
using System.Collections.Generic;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.Thread;
using I2.Loc;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using Task = System.Threading.Tasks.Task;

namespace GameRules.Scripts.Modules.Database
{    
    public static class Database
    {
        private static Dictionary<string, BaseItem> _all;

        private static IList<PlayerItem> _players;
        private static IList<UnitItem> _units;
        private static IList<MapItem> _maps;

        public static IReadOnlyDictionary<string, BaseItem> All => _all;
        
        public static string Version { get; private set; }
        
        public static IList<PlayerItem> Players => _players;
        public static IList<UnitItem> Units => _units;
        public static IList<MapItem> Maps => _maps;

        private static bool _isCompleteInitialize;
        private static readonly AsyncEventWaitHandle _waitHandle;
        public static bool IsCompleteInitialize => _isCompleteInitialize;

        public static LanguageSourceData Localize { get; private set; }

        static Database()
        {
            _waitHandle = new AsyncEventWaitHandle(false);
        }
        
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Addressables.InitializeAsync().Completed += OnCompletedInitialize;
        }

        public static async Task WaitInitialize()
        {
            if(_isCompleteInitialize)
                return;

            await _waitHandle.WaitAsync();
        }

        public static IEnumerator WaitInitializeEnumerator()
        {
            while (!_isCompleteInitialize)
                yield return null;
        }

        private static Dictionary<string, SpriteAtlas> _atlases = new Dictionary<string, SpriteAtlas>();

        public static bool TryGetSpriteAtlas(string tag, out SpriteAtlas spriteAtlas)
        {
            return _atlases.TryGetValue(tag, out spriteAtlas);
        }
        
        private static async void OnCompletedInitialize(AsyncOperationHandle<IResourceLocator> resourceLocator)
        {
            resourceLocator.Result.Locate("DatabaseVersion", typeof(DatabaseVersion), out var versionLocator);
            resourceLocator.Result.Locate("DatabaseLocalize", typeof(LanguageSourceAsset), out var localizeLocator);
            
            resourceLocator.Result.Locate("unit_items", typeof(UnitItem), out var units);
            resourceLocator.Result.Locate("player_items", typeof(PlayerItem), out var players);
            resourceLocator.Result.Locate("map_items", typeof(MapItem), out var maps);
            
            var loadUnits = Addressables.LoadAssetsAsync<UnitItem>(units, null).Task;
            var loadPlayers = Addressables.LoadAssetsAsync<PlayerItem>(players, null).Task;
            var loadMaps = Addressables.LoadAssetsAsync<MapItem>(maps, null).Task;
            
            Addressables.LoadAssetAsync<LanguageSourceAsset>(localizeLocator[0]).Completed += handle =>
            {
                Localize = handle.Result.SourceData;
            };
            
            await Task.WhenAll(loadUnits, loadPlayers, loadMaps);

            Version = (await Addressables.LoadAssetAsync<DatabaseVersion>(versionLocator[0]).Task).Version;
            
            _units = new List<UnitItem>(loadUnits.Result);
            _players = new List<PlayerItem>(loadPlayers.Result);
            _maps = new List<MapItem>(loadMaps.Result);
            
            _all = new Dictionary<string, BaseItem>(_players.Count + _units.Count + _maps.Count);
            InjectToAll(_all, _players);
            InjectToAll(_all, _units);
            InjectToAll(_all, _maps);
            
            _isCompleteInitialize = true;
            _waitHandle.Set();
            
            Inventory.Load();
            InventoryHistory.Load();
        }

        private static void InjectToAll<T>(Dictionary<string, BaseItem> all, IList<T> list) where T : BaseItem
        {
            for (int i = 0; i < list.Count; i++)
                all[list[i].Id] = list[i];
        }

        public static void Sync(JObject jObject)
        {
            var client_items = TmpList<string>.Get();
            client_items.AddRange(_all.Keys);
            
            foreach (var item in jObject)
            {
                if (_all.TryGetValue(item.Key, out var clientItem))
                {
                    client_items.RemoveSwapBack(item.Key);
                    clientItem.UpdateFromServer((JObject)item.Value);
                }
                else
                    Debug.LogError($@"Item from server not found: ""{item.Key}""");
            }

            var players = (List<PlayerItem>)_players;
            var units = (List<UnitItem>)_units;
            var maps = (List<MapItem>)_maps;
            
            for (int i = 0; i < client_items.Count; i++)
            {
                var key = client_items[i];
                
                Debug.LogError($@"Item from client not found: ""{key}""");
                var item = _all[key];
                _all.Remove(key);

                switch (item.Type)
                {
                    case ItemType.Character:
                        players.RemoveSwapBack((PlayerItem)item);
                        break;
                    case ItemType.Minion:
                        units.RemoveSwapBack((UnitItem)item);
                        break;
                    case ItemType.Map:
                        maps.RemoveSwapBack((MapItem)item);
                        break;
                }
            }
        }
    }
}