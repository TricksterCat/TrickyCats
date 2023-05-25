using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameRules.Firebase.Runtime;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.UI.Inventory;
using GameRules.TaskManager.Runtime;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GameRules.Scripts.UI.WheelOfFortune
{
    public class WheelOfFortuneLogic : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField]
        private float _distanceItems;
        [SerializeField, Range(0f, 360f)]
        private float _startRotation;
        [SerializeField, Range(-360, 360f)] 
        private float _arrowOffsetAngle;
        [SerializeField] 
        private float _arrowSpeed;
        [SerializeField] 
        private float2 _timeEndRotate;
        [SerializeField] 
        private int _timeEndRotateCircles;
        [SerializeField] 
        private float2 _completeTargetDist;
        
        [SerializeField]
        private Image[] _backs;
        [SerializeField]
        private Color[] _colors;

        [SerializeField]
        private WalletIconsDict _walletIcons;
        
        [SerializeField, BoxGroup("Content items")]
        private WalletWheelItem[] _wallets;
        [SerializeField, BoxGroup("Content items")]
        private ItemWheelItem[] _items;

        [SerializeField]
        private RectTransform _arrow;
        [SerializeField]
        private Image _winLight;

        [SerializeField]
        private AnimationCurve _weightToAverageOfCount;
        [SerializeField]
        private AnimationCurve _weightToAverageOfDifference;
        
        [SerializeField, ChildGameObjectsOnly, HorizontalGroup(GroupID = "Content root", LabelWidth = 100)]
        private Transform _contentRoot;
        [Button, HorizontalGroup(GroupID = "Content root", MinWidth = 90), EnableIf("@this._contentRoot != null")]
        private void InjectItems()
        {
            _wallets = _contentRoot.GetComponentsInChildren<WalletWheelItem>();
            _items = _contentRoot.GetComponentsInChildren<ItemWheelItem>();
        }

        public TaskCoroutine RotateWheelTask { get; private set; }
        public bool WaitReward { get; set; }

        private List<WheelItem> _data = new List<WheelItem>();
        private float _totalChance;
        
        private WheelItem _target;
        public Action OnCompleteRotate;

        [Button, BoxGroup("Debug"), HorizontalGroup("Debug/0", Width = 70), PropertyOrder(0)]
        private void Test()
        {
            _lastJson = JArray.Parse(_testJson);
            TestLast();
        }
        [SerializeField, BoxGroup("Debug"), HorizontalGroup("Debug/0"), HideLabel, PropertyOrder(1)]
        private string _testJson;
        
        private JArray _lastJson;
        [Button, BoxGroup("Debug")]
        private void TestLast()
        {
            Draw(_lastJson);
        }

        private ChangeProperty<bool> _isLocked = new ChangeProperty<bool>();

        private void Awake()
        {
            RotateWheelTask = new TaskCoroutine(RotateWheel);
            _isLocked.OnChange += value => InventoryDiffsHandler.IsLockCounter += value ? 1 : -1;
        }

        private void OnDestroy()
        {
            if (_isLocked.Value)
                _isLocked.Value = false;
        }

        public void Draw(JArray array)
        {
            _lastJson = array;
            _data.Clear();
            _target = null;
            
            var count = math.min(_backs.Length, array.Count);

            int indexWallets = 0, indexItems = 0;
            float total = 0;
            float maxChance = 0;

            for (int i = 0; i < count; i++)
            {
                var jItemModel = (JObject)array[i];
                var type = jItemModel["type"].ToString();
                var chanse = (float)jItemModel["weight"];
                
                switch (type)
                {
                    case "item":
                        var id = jItemModel["value"].ToString();
                        if(!Database.All.TryGetValue(id, out var itemModel))
                            continue;

                        var item = _items[indexItems++];
                        item.Chance = chanse;
                        item.Set(itemModel);
                        _data.Add(item);
                        break;
                    case "soft":
                    case "hard":
                    case "rouletteSpin":
                        var wallet = _wallets[indexWallets++];
                        wallet.Chance = chanse;
                        wallet.Set(type, _walletIcons[type], jItemModel["value"].ToString());
                        _data.Add(wallet);
                        break;
                }
                
                total += chanse;
                if (maxChance < chanse)
                    maxChance = chanse;
            }
            _totalChance = total;
            var average = total / _data.Count;
            var averageForce = _weightToAverageOfCount.Evaluate(_data.Count);
            averageForce += _weightToAverageOfDifference.Evaluate(maxChance / average);

            for (int i = indexWallets; i < _wallets.Length; i++)
                _wallets[i].IsVisible = false;
            
            for (int i = indexItems; i < _items.Length; i++)
                _items[i].IsVisible = false;

            var itemMask = math.min(2 + (_colors.Length % 2), _colors.Length);
            
            float fAngle = 0f;
            for (int i = 0; i < _data.Count; i++)
            {
                var back = _backs[i];

                var data = _data[i];
                data.Chance = Mathf.Lerp(_data[i].Chance, average, averageForce);
                data.BackAngle = fAngle;
                
                var value = _data[i].Chance / total;
                float fOffsetAngle = 360 * value;

                
                data.BackFillAmount = value + 0.001f;
                _data[i] = data;
                
                back.color = _colors[i % itemMask];
                back.enabled = true;
                back.fillAmount = value + 0.001f;
                back.transform.localEulerAngles = new Vector3(0, 0, fAngle);

                
                fAngle += fOffsetAngle / 2f;
                Vector3 vPos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
                var rectTransform = (RectTransform)_data[i].transform;
                
                rectTransform.localPosition = vPos * _distanceItems;
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                
                var angle = rectTransform.localEulerAngles;
                angle.z = _startRotation + fAngle;
                rectTransform.localEulerAngles = angle;
                
                fAngle += fOffsetAngle / 2f;
            }
            
            for (int i = _data.Count; i < _backs.Length; i++)
                _backs[i].enabled = false;
        }

        public void OnBeforeSerialize()
        {
            _walletIcons?.Serialzie();
        }

        public void OnAfterDeserialize()
        {
            _walletIcons?.Deserialize();
        }

        public bool ToWin(string rewardType, string rewardValue)
        {
            var winItems = TmpList<WheelItem>.Get();
            for (int i = 0; i < _data.Count; i++)
            {
                var position = _data[i];
                if(position.Type != rewardType)
                    continue;

                if (position.IsWin(rewardType, rewardValue))
                    winItems.Add(position);
            }

            bool isFound = winItems.Count > 0;
            if (isFound)
                _target = winItems[Random.Range(0, winItems.Count)];
            else
            {
                RotateWheelTask.Stop();
                FirebaseApplication.LogError("Fortune::ToWinFailed");
                Debug.LogError("Fortune::ToWinFailed");
            }
            TmpList<WheelItem>.Release(winItems);
            
            return isFound;
        }

        private IEnumerator RotateWheel()
        {
            _isLocked.Value = true;
            float speed = _arrowSpeed;
            
            var angles = _arrow.localEulerAngles;
            while (_target == null)
            {
                angles.z += Time.deltaTime * speed;
                _arrow.localEulerAngles = angles;
                
                yield return null;
            }
            
            angles.z %= 360;
            if (angles.z < 0)
                angles.z = 360 + angles.z;

            var targetAngle = _target.transform.localEulerAngles.z - _startRotation + _arrowOffsetAngle + math.sign(speed) * _timeEndRotateCircles * 360;
            _winLight.transform.localEulerAngles = new Vector3(0, 0, _target.BackAngle);
            _winLight.fillAmount = _target.BackFillAmount;

            var velocity = speed;
            speed = Mathf.Abs(speed);

            var maxTarget = _completeTargetDist.y - _completeTargetDist.x;
            float left = Mathf.Abs(targetAngle - angles.z);
            while (left > _completeTargetDist.x)
            {
                angles.z = Mathf.SmoothDamp(angles.z, targetAngle, ref velocity, Mathf.Lerp(_timeEndRotate.x, _timeEndRotate.y, (left - _completeTargetDist.x) / maxTarget), speed);
                yield return null;
                
                _arrow.localEulerAngles = angles;
                left = Mathf.Abs(targetAngle - angles.z);
            }

            DOTween.Restart("ShowWheelWinLight");
            var complete = Time.time + 0.7f;
            while (Time.time < complete)
                yield return null;
            
            _isLocked.Value = false;
            
            while (WaitReward)
                yield return null;
            
            OnCompleteRotate?.Invoke();
            CompleteRotateWheel();
        }

        public void CompleteRotateWheel()
        {
            _target = null;
            DOTween.Rewind("ShowWheelWinLight");
            _arrow.localEulerAngles = Vector3.zero;
        }

        public void Error()
        {
            _isLocked.Value = false;
            CrowdAnalyticsMediator.Instance.BeginEvent("RotateWheelError").CompleteBuild();
            
            RotateWheelTask.Stop();
            OnCompleteRotate?.Invoke();
        }
    }
}