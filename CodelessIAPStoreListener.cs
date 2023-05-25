using System.Collections;
using System.Globalization;
using GameRules.Scripts.Modules;
using UnityEngine.Purchasing.Security;
#if UNITY_PURCHASING
using System;
using System.Collections.Generic;
using System.Linq;
using Facebook.Unity;
using Firebase.Analytics;
using Firebase.Crashlytics;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Extensions;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Automatically initializes Unity IAP with the products defined in the IAP Catalog (if enabled in the UI).
    /// Manages IAPButtons and IAPListeners.
    /// </summary>
    public class CodelessIAPStoreListener : IStoreListener
    {
        private static CodelessIAPStoreListener instance;
        private static bool unityPurchasingInitialized;
        public static ChangeProperty<bool> IsProcessPurchasing { get; } = new ChangeProperty<bool>();

        private static LinkedList<CompletePurchaseData> _completePurchaseData = new LinkedList<CompletePurchaseData>();
        private static Queue<CompletePurchaseData> _execudedPurchaseData = new Queue<CompletePurchaseData>();
        

        protected IStoreController controller;
        protected IExtensionProvider extensions;
        protected ProductCatalog catalog;
        
        private IAppleExtensions m_AppleExtensions;

        // Allows outside sources to know whether the full initialization has taken place.
        public static bool initializationComplete;

        private static bool _waitRestoreTransaction;
        private static Action _onSuccessRestoreTransaction;

        public static bool IsRestoreTransaction {get; private set; }

        public static void RestoreTransaction(Action onSuccess)
        {
            _onSuccessRestoreTransaction = onSuccess;
            
            var extensions = Instance.extensions;
            if (extensions == null)
            {
                _waitRestoreTransaction = true;
                return;
            }

            RestoreTransaction(extensions);
        }

        private static void RestoreTransaction(IExtensionProvider extensions)
        {
            _waitRestoreTransaction = false;
            IsRestoreTransaction = true;

            try
            {
#if UNITY_UDP_PLATFORMM
#elif UNITY_ANDROID
                extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(
#elif UNITY_IOS
            extensions.GetExtension<IAppleExtensions>().RestoreTransactions(
#endif
            
#if !UNITY_UDP_PLATFORM && (UNITY_ANDROID || UNITY_IOS)
                    result =>
                    {
                        if (result)
                        {
                            _onSuccessRestoreTransaction?.Invoke();
                            // This does not mean anything was restored,
                            // merely that the restoration process succeeded.
                        }
                        else
                        {
                            // Restoration failed.
                        }
                        
                        IsRestoreTransaction = false;
                    });
#endif
            }
            catch (Exception e)
            {
                Crashlytics.LogException(e);
            }
        }

        [RuntimeInitializeOnLoadMethod]
        static void InitializeCodelessPurchasingOnLoad() 
        {
            ProductCatalog catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog.enableCodelessAutoInitialization && !catalog.IsEmpty() && instance == null)
                CreateCodelessIAPStoreListenerInstance();
        }

        private static void InitializePurchasing()
        {
            StandardPurchasingModule module = StandardPurchasingModule.Instance();
            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

            ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);

            IAPConfigurationHelper.PopulateConfigurationBuilder(ref builder, instance.catalog);

            UnityPurchasing.Initialize(instance, builder);

            unityPurchasingInitialized = true;
        }

        private CodelessIAPStoreListener()
        {
            catalog = ProductCatalog.LoadDefaultCatalog();
        }

        public static CodelessIAPStoreListener Instance
        {
            get
            {
                if (instance == null)
                {
                    CreateCodelessIAPStoreListenerInstance();
                }
                return instance;
            }
        }

        /// <summary>
        /// Creates the static instance of CodelessIAPStoreListener and initializes purchasing
        /// </summary>
        private static void CreateCodelessIAPStoreListenerInstance()
        {
            instance = new CodelessIAPStoreListener();
            if (!unityPurchasingInitialized)
            {
                Debug.Log("Initializing UnityPurchasing via Codeless IAP");
                InitializePurchasing();
            }
        }

        public IStoreController StoreController => controller;

        public IExtensionProvider ExtensionProvider => extensions;

        public bool HasProductInCatalog(string productID)
        {
            foreach (var product in catalog.allProducts)
            {
                if (product.id == productID)
                    return true;
            }
            return false;
        }

        public bool CanBuy(string productID)
        {
            var product = GetProduct(productID);
            return product != null && product.availableToPurchase;
        }

        public Product GetProduct(string productID)
        {
            if (controller != null && controller.products != null && !string.IsNullOrEmpty(productID))
                return controller.products.WithID(productID);
            Debug.LogError("CodelessIAPStoreListener attempted to get unknown product " + productID);
            return null;
        }

        public void InitiatePurchase(string productID)
        {
            if(IsProcessPurchasing.Value)
                return;
            
            if (controller == null)
            {
                Debug.LogError("Purchase failed because Purchasing was not initialized correctly");
                return;
            }

            var product = GetProduct(productID);
            if(product == null || !product.availableToPurchase)
                return;
            
            IsProcessPurchasing.Value = true;
            if(RemoteConfig.GetBool("Facebook_customEvent"))
                FB.LogAppEvent(AppEventName.InitiatedCheckout, (float)product.metadata.localizedPrice, new Dictionary<string, object>
                {
                    { AppEventParameterName.Currency, product.metadata.isoCurrencyCode },
                    { AppEventParameterName.ContentID, product.definition.id },
                    { AppEventParameterName.NumItems, 1 }
                });
            controller.InitiatePurchase(product);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            initializationComplete = true;
            this.controller = controller;
            this.extensions = extensions;
            
            m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
            
            m_AppleExtensions.RegisterPurchaseDeferredListener(OnDeferred);

            foreach (var item in controller.products.all)
            {
                if (item.availableToPurchase)
                    m_AppleExtensions?.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
            }

            if (_waitRestoreTransaction)
                RestoreTransaction(extensions);
        }

        private void OnDeferred(Product product)
        {
            
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError(string.Format("Purchasing failed to initialize. Reason: {0}", error.ToString()));
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            bool isSuccess = true;
            var product = e.purchasedProduct;
            
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !UNITY_UDP_PLATFORM
        var analytic = CrowdAnalyticsMediator.Instance;
        var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
        if (!product.hasReceipt)
        {
            if(!IsRestoreTransaction)
            {
                analytic.BeginEvent("iap_validate_failed")
                    .AddField("reason", "not receipt")
                    .AddField("id", product.definition.id)
                    .CompleteBuild();
            }
            isSuccess = false;
        }
        else
        {
            try
            {
                validator.Validate(product.receipt);
            }
            catch(IAPSecurityException exception)
            {
                if(!IsRestoreTransaction)
                {
                    analytic.BeginEvent("iap_validate_failed")
                        .AddField("reason", exception.Message)
                        .AddField("id", product.definition.id)
                        .CompleteBuild();
                }

                isSuccess = false;
            }
        }
#endif
            IsProcessPurchasing.Value = false;
            
            if (isSuccess)
            {
                var meta = product.metadata;
                _completePurchaseData.AddLast(new CompletePurchaseData
                {
                    Id = product.definition.id,
                    TransactionId = product.transactionID,
                    Price = meta.localizedPrice,
                    Currency = meta.isoCurrencyCode,
                    Reciept = product.receipt,
                    IsRestore = IsRestoreTransaction
                });

                if (IsRestoreTransaction)
                {
                    CrowdAnalyticsMediator.Instance
                        .BeginEvent("iap_restore")
                        .AddField("product_id", product.definition.id)
                        .CompleteBuild();
                }
                else
                {
                }
            }
            
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            IsProcessPurchasing.Value = false;
            CrowdAnalyticsMediator.Instance
                .BeginEvent("purchase_failed")
                .AddField("product_id", product.definition.id)
                .AddField("price", (double)product.metadata.localizedPrice)
                .AddField("currency", product.metadata.isoCurrencyCode)
                .AddField("reason", reason.ToString())
                .CompleteBuild();
        }
        
        public void CompleteExecute(CompletePurchaseData iapData)
        {
            _execudedPurchaseData.Enqueue(iapData);
        }

        public void RemoveExecuted()
        {
            while (_execudedPurchaseData.Count > 0)
            {
                var data = _execudedPurchaseData.Dequeue();
                _completePurchaseData.Remove(data);
            }
        }

        public IEnumerable<CompletePurchaseData> Execute()
        {
            return _completePurchaseData;
        }

    }
}

#endif
