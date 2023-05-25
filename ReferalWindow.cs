using System;
using System.Threading.Tasks;
using Firebase.DynamicLinks;
using GameRules;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.Modules;
using Michsky.UI.ModernUIPack;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class ReferalWindow : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI _progressText;
    [SerializeField]
    public Slider _progress;

    [SerializeField]
    private ModalWindowManager _manager;
    
    private Task<string> _updateReferralCode;
    
    private void Start()
    {
        ServerRequest.Instance.SyncCompleted += OnSyncCompleted;
        InternalUpdateRefCode();
    }
    
    private async void InternalUpdateRefCode()
    {
        if (_updateReferralCode != null && !_updateReferralCode.IsCompleted)
            return;
        
        var refCode = GetOrPush.RefCode;
        var settings = JsonUtility.FromJson<ReferralSettings>(RemoteConfig.GetString("ReferralSetting"));
        if (string.IsNullOrEmpty(refCode) || settings.Rev != GetOrPush.RefCodeRev)
        {
            _updateReferralCode = UpdateRefCode(settings);
            refCode = await _updateReferralCode;
            if (!string.IsNullOrEmpty(refCode))
                GetOrPush.UpdateRefCode(refCode, settings.Rev);
        }
    }

    private void OnDestroy()
    {
        ServerRequest.Instance.SyncCompleted -= OnSyncCompleted;
    }

    private void OnSyncCompleted()
    {
        Draw();
        InternalUpdateRefCode();
    }

    public void Open()
    {
        Draw();
        
        _manager.OpenWindow();
    }

    private void Draw()
    {
        var value = math.clamp(GetOrPush.Referral, 0, _progress.maxValue) ;
        _progress.value = value;
        _progressText.text = $"{GetOrPush.Referral}/{(int) _progress.maxValue}";
    }
    
    [Serializable]
    internal struct ReferralSettings
    {
        public string Id;
        public string Title;
        public string Description;
        public string PreviewUrl;
        public string Rev ;
    }

    private static async Task<string> UpdateRefCode(ReferralSettings settings)
    {
        while (string.IsNullOrEmpty(ServerRequest.UserId))
            await Task.Yield();
        
        Uri url = new Uri($"https://catroulette.rustygames.net/invite/{ServerRequest.UserId}");
        var androidParameters = new AndroidParameters(Application.identifier)
        {
            FallbackUrl = new Uri($"market://detals?id={Application.identifier}")
        };
        var iOSParameters = new IOSParameters(Application.identifier)
        {
            FallbackUrl = new Uri($"itms-apps://itunes.apple.com/app/id{GetOrPush.iOS_AppId}"),
            AppStoreId = GetOrPush.iOS_AppId
        };

        var component = new DynamicLinkComponents(url, "https://trickycats.rustygames.net/invites");
        component.AndroidParameters = androidParameters;
        component.IOSParameters = iOSParameters;
        component.SocialMetaTagParameters = new SocialMetaTagParameters
        {
            Title = settings.Title,
            Description = settings.Description,
            ImageUrl = new Uri(settings.PreviewUrl)
        };
        component.GoogleAnalyticsParameters = new GoogleAnalyticsParameters
        {
            Source = "AppReferral",
            Medium = "Referral",
            Content = settings.Id
        };
        
        var link = await DynamicLinks.GetShortLinkAsync(component, new DynamicLinkOptions
        {
            PathLength = DynamicLinkPathLength.Short
        });

        return link.Url.ToString();
    }
    
    public async void Share()
    {
        var refCode = GetOrPush.RefCode;
        if (string.IsNullOrEmpty(refCode))
        {
            InternalUpdateRefCode();
            if (_updateReferralCode != null)
                refCode = await _updateReferralCode;
        }
        
        if(string.IsNullOrEmpty(refCode))
            return;
        
        CrowdAnalyticsMediator.Instance.BeginEvent("share_invite")
            .AddField("games_count", GetOrPush.PlayGames)
            .CompleteBuild();
        
        var settings = JsonUtility.FromJson<ReferralSettings>(RemoteConfig.GetString("ReferralSetting"));
        new NativeShare().SetText($"{settings.Description}\n{refCode}").Share();
    }
}
