using System;
using System.Collections;
using Facebook.Unity;
using GameRules;
using GameRules.Facebook.Runtime;
using GameRules.ModuleAdapters.Runtime;
using GameRules.Scripts.UI;
using GameRules.UI;
using UnityEngine;

public class GDPR_view : MonoBehaviour
{
    [SerializeField]
    private bool _isDebug;
    
    public IEnumerator Show(DialogViewBox dialogViewBox)
    {
		if (_isDebug)
            GetOrPush.GDPR_consent = -1;
        
        if (GetOrPush.GDPR_consent != -1)
            yield break;

        string subText = string.Empty;
        float size = 820;
#if UNITY_ANDROID
        subText += @"

Also for after this window the following permissions will be requested:
1) Write to external storage. (To save network traffic)
2) Access location service. (To show ads that are more suitable for you)";
#endif
        dialogViewBox.SetParameter("SubText", subText);
        
        dialogViewBox.NegativeBtn
            .SetLabelValue("Loading/GDPR_DONT_AGREE")
            .SetCallback(Close)
            .SetActive(true);
        
        dialogViewBox.PositiveBtn
            .SetLabelValue("Loading/GDPR_AGREE")
            .SetCallback(Agree)
            .SetActive(true);
        
        dialogViewBox.Show("Loading/GDPR_TITLE", "Loading/GDPR_MESSAGE", size);
        
        while (GetOrPush.GDPR_consent == -1)
            yield return null;
        
        dialogViewBox.Hide();
        
        var time = Time.time + 0.5f;
        while (time > Time.time)
            yield return null;
    }
    
    private void Agree()
    {
        Complete(true);
    }

    private void Close()
    {
        Complete(false);
    }


    private async void Complete(bool isConsent)
    {
        GetOrPush.GDPR_consent = isConsent ? 1 : 0;
        if (FacebookApplication.Status == StatusInitialize.Wait)
            await FacebookApplication.WaitInitialize();
        
        FB.Mobile.SetAutoLogAppEventsEnabled(isConsent);
    }
}
