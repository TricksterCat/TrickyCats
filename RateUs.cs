using GameRules;
using GameRules.Scripts;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using GameRules.UI;
using I2.Loc;
using Michsky.UI.ModernUIPack;
using UnityEngine;


public class RateUs : MonoBehaviour
{
    [SerializeField]
    private ModalWindowManager _modalWindow;
    
    [SerializeField]
    private Localize _title;
    [SerializeField]
    private Localize _message;

    public bool DontAskAgain { get; set; }
    
    public void Show(bool forceShow)
    {
        if(!forceShow && !GetOrPush.CanShowRateUs)
            return;
        
        _title.SetTerm("RateUs/RATE_US_TITLE");
        _message.SetTerm("RateUs/RATE_US_MESSAGE");
            
        _modalWindow.OpenWindow();
    }

    public void Close()
    {
        _modalWindow.CloseWindow();
        //Invoke(nameof(OnCompleteClose), (float)_hideAnimation.duration);

        var analyticsModule = CrowdAnalyticsMediator.Instance.BeginEvent("request_rating");
        var result = DontAskAgain ? "Refuse" : "Postpone";
        analyticsModule.AddField("result", result).CompleteBuild();
        if (DontAskAgain)
            GetOrPush.CanShowRateUs = false;
        
        ServerRequest.Instance.RateUsResult(result);
    }

    public void CallRateUs()
    {
        _modalWindow.CloseWindow();
        
        var analyticsModule = CrowdAnalyticsMediator.Instance;
        analyticsModule.BeginEvent("request_rating").AddField("result", "Rate").CompleteBuild();
        analyticsModule.BeginEvent("rate_game").CompleteBuild();
        
        GetOrPush.CanShowRateUs = false;
        
        #if UNITY_IOS
            if(!UnityEngine.iOS.Device.RequestStoreReview())
                    Application.OpenURL($"itms-apps://itunes.apple.com/app/id{GetOrPush.iOS_AppId}?action=write-review");
        #else
            Application.OpenURL($"market://details?id={Application.identifier}");
        #endif
        
        ServerRequest.Instance.RateUsResult("Rate");
    }
}
