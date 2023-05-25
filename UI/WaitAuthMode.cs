using System.Collections;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Modules;
using UnityEngine;

namespace GameRules.Scripts.UI
{
    public static class WaitAuthMode
    { 
        public static IEnumerator TryWait()
        {
            if(!string.IsNullOrEmpty(GetOrPush.AuthMode))
                yield break;

            var waitView = WaitView.Instance;
            var dialog = DialogViewBox.Instance;
            bool isComplete = false;
            bool isShowWaitView = false;

            void Complete(string result)
            {
                if (result == "iOS_center")
                    GetOrPush.ForceDeviceId = Social.localUser.id;
                
                GetOrPush.AuthMode = result;
                dialog.Hide();
                if(isShowWaitView)
                    waitView.Hide();
                isComplete = true;
                
                FirebaseApplication.SetUserProperty("auth_type", result);
                CrowdAnalyticsMediator.Instance.SingUp(result);
            }
            
            dialog.NegativeBtn
                .SetLabelValue("Loading/AUTH_MODE_DEVICE")
                .SetCallback(() =>
                {
                    Complete("device");
                })
                .SetActive(true);
            
            dialog.PositiveBtn
                .SetLabelValue("Loading/AUTH_MODE_REMOTE")
                .SetCallback(() =>
                {
                    dialog.Hide();
                    isShowWaitView = true;
                    waitView.Show("Loading/WAIT_REQUEST_TITLE");
                    
                    if (!Social.localUser.authenticated || string.IsNullOrWhiteSpace(Social.localUser.id))
                    {
                        Social.localUser.Authenticate((isSuccess, error) =>
                        {
                            if (isSuccess)
                            {
                                Complete("iOS_center");
                            }
                            else
                            {
                                FirebaseApplication.LogError("Failed atuh is iOS center. Error: "+error);
                                Complete("device");
                            }
                        });
                    }
                    else 
                        Complete("iOS_center");
                })
                .SetActive(true);
            
            dialog.Show("Loading/AUTH_MODE_TITLE", "Loading/AUTH_MODE_MESSAGE", 380);

            while (!isComplete)
                yield return null;
        }
    }
}