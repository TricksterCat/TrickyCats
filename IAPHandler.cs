using System;
using System.Collections.Generic;
using System.Globalization;
using Firebase.Analytics;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.UI.WheelOfFortune;
using UnityEngine;
using UnityEngine.Purchasing;
using AppsFlyerSDK;
using DefaultNamespace;
using Facebook.Unity;
using GameRules.Scripts.GameTuneImpl;
using GameRules.Scripts.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameRules.Scripts
{
    public class IAPHandler : MonoBehaviour
    {
        public static event Action<string> CompleteExecutePurchase; 
        public static event Action CompleteNextExecutePurchases;
        
        public void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void Update()
        {
            if(!CodelessIAPStoreListener.initializationComplete || string.IsNullOrEmpty(ServerRequest.AccessToken))
                return;
            
            var iapController = CodelessIAPStoreListener.Instance;

            iapController.RemoveExecuted();
            
            bool hasPurchases = false;
            foreach (var iapData in iapController.Execute())
            {
                hasPurchases = true;
                iapController.CompleteExecute(iapData);
                
                var id = iapData.Id;
                ServerRequest.Instance.IAP(id, iapData.Reciept ?? string.Empty, isSuccess =>
                {
                    if(!isSuccess)
                        return;

                    if (!iapData.IsRestore)
                        GameTuneManager.CompleteIAP();
                    
                    switch (id)
                    {
                        case "com.rustygames.tc.all_players":
                            WheelOfFortuneWindow.UpdateWheel.Value = true;
                            GetOrPush.UnlockedAllSkins = true;
                            break;
                        case "com.rustygames.tc.none_ads.forever_sale":
                        case "com.rustygames.tc.none_ads.forever":
                            if (GetOrPush.AdsCurrentType == GetOrPush.AdsType.AdsEnable)
                                GetOrPush.AdsCurrentType = GetOrPush.AdsType.AdsDisable;
                            break;
                    }
                    FirebaseAnalytics.SetUserProperty("HavePurchase", "1");
                    CompleteExecutePurchase?.Invoke(id);

                    if (RemoteConfig.GetBool("Facebook_customEvent"))
                        FB.LogPurchase(iapData.Price, iapData.Currency, new Dictionary<string, object>()
                        {
                            { AppEventParameterName.ContentID, id },
                        });

                    if (!string.IsNullOrWhiteSpace(iapData.Reciept))
                    {
                        try
                        {
                            var tenjin = CrowdAnalyticsMediator.GetTenjin();
                            
                            var wrapper = JObject.Parse(iapData.Reciept);
                            var payload = wrapper["Payload"];
                            
#if UNITY_ANDROID

  var gpJson    = payload["json"].ToString(Formatting.None);
  var gpSig     = payload["signature"].ToString(Formatting.None);

                            tenjin.Transaction(iapData.Id, iapData.Currency, 1, decimal.ToDouble(iapData.Price), iapData.TransactionId, gpJson, gpSig);

#elif UNITY_IOS
                            tenjin.Transaction(iapData.Id, iapData.Currency, 1, decimal.ToDouble(iapData.Price), iapData.TransactionId, payload.ToString(Formatting.None), null);
#endif
                            
                        }
                        catch (Exception e)
                        {
                            FirebaseApplication.LogException(e);
                        }
                    }

                });
                
                if (RemoteConfig.GetBool("AppsFlyer_validatePurchase"))
                {
#if !UNITY_EDITOR
#if UNITY_IOS
                AppsFlyeriOS.validateAndSendInAppPurchase(
                        id, 
                        iapData.Price.ToString(CultureInfo.InvariantCulture), 
                        iapData.Currency, 
                        iapData.TransactionId, 
                        null, 
                        this);
#elif UNITY_ANDROID
//TODO: add android part
#endif
#endif
                }
            }
            
            if(hasPurchases)
                CompleteNextExecutePurchases?.Invoke();
        }
    }
}