using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using GameRules;
using GameRules.Scripts;
using GameRules.Scripts.Modules;
using GameRules.Scripts.UI;
using GameRules.TaskManager.Runtime;
using GameRules.UI;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;


public class SettingsView : MonoBehaviour
{
    private List<string> _languages;
    private int _currentIndex;

    [SerializeField, BoxGroup("Sounds")]
    private Slider _musicVolume;
    [SerializeField, BoxGroup("Sounds")]
    private Slider _soundVolume;
    
    [SerializeField, BoxGroup("Graphics")]
    private Toggle _highQualityToggle;

    [SerializeField]
    private GameObject _rateUsBtn;
    
    [SerializeField]
    private ShowController _modalWindow;

    
    private void Awake()
    {
        _languages = LocalizationManager.GetAllLanguages();
        _currentIndex = _languages.IndexOf(LocalizationManager.CurrentLanguage);
    }

    public void RemoveAccount()
    {
        var dialog = DialogViewBox.Instance;
        dialog.PositiveBtn
            .SetLabelValue("Settings/REMOVE_ACCOUNT_CONFIRMATION_NO")
            .SetCallback(() => dialog.Hide())
            .SetActive(true);
        
        dialog.NegativeBtn
            .SetLabelValue("Settings/REMOVE_ACCOUNT_CONFIRMATION_YES")
            .SetCallback(BeginRemoveAccount)
            .SetActive(true);
        dialog.Show("Settings/REMOVE_ACCOUNT_CONFIRMATION_TITLE", "Settings/REMOVE_ACCOUNT_CONFIRMATION_MESSAGE", 240);
    }

    private void BeginRemoveAccount()
    {
        WaitView.Instance.Show("Loading/WAIT_REQUEST_TITLE");
        ServerRequest.Instance.RemoveAccount().ContinueWithOnMainThread(task =>
        {
            WaitView.Instance.Hide();
            if (task.Exception != null || !task.Result)
            {
                UpdateManager.Instance.StartCoroutine(ReRemoveAccount());
                return;
            }

            GetOrPush.RemoveAll = true;
            PlayerPrefs.DeleteAll();
            Application.Quit();
        });
    }

    private IEnumerator ReRemoveAccount()
    {
        yield return NotInternet.Instance.WaitInternet(DialogViewBox.Instance, false);
        BeginRemoveAccount();
    }


    private IEnumerator Start()
    {
        if (_highQualityToggle.gameObject.activeSelf)
        {
            _highQualityToggle.isOn = GetOrPush.HighQuality;
            ChangeGraphicsQuality(_highQualityToggle.isOn);
            _highQualityToggle.onValueChanged.AddListener(ChangeGraphicsQuality);
        }
        
        while (SoundManager.Instance == null)
            yield return null;
        
        _musicVolume.value = SoundManager.Instance.MusicVolume;
        _soundVolume.value = SoundManager.Instance.SoundVolume;
        
        _musicVolume.onValueChanged.AddListener(OnChangeMusicVolume);
        _soundVolume.onValueChanged.AddListener(OnChangeSoundVolume);
    }

    public void Open()
    {
        _rateUsBtn.SetActive(GetOrPush.PlayGames > 1);
        _modalWindow.Show();
    }
    
    private void ChangeGraphicsQuality(bool highQuality)
    {
        GetOrPush.HighQuality = highQuality;
        QualitySettings.SetQualityLevel(highQuality ? 1 : 0, transform);
    }

    public void RateUs()
    {
        var analytics = CrowdAnalyticsMediator.Instance;
        
        analytics
            .BeginEvent("request_rating")
            .AddField("result", "Rate")
            .CompleteBuild();
        
        analytics
            .BeginEvent("rate_from_settings")
            .AddField("games_count", GetOrPush.PlayGames)
            .CompleteBuild();

        GetOrPush.CanShowRateUs = false;
#if UNITY_IOS
            if(!UnityEngine.iOS.Device.RequestStoreReview())
                Application.OpenURL($"itms-apps://itunes.apple.com/app/id{GetOrPush.iOS_AppId}?action=write-review");
#else
        Application.OpenURL("market://details?id=" + Application.identifier);
#endif
        
        ServerRequest.Instance.RateUsResult("Rate");
    }
    
    private void OnChangeSoundVolume(float volume)
    {
        SoundManager.Instance.SoundVolume = volume;
    }

    private void OnChangeMusicVolume(float volume)
    {
        SoundManager.Instance.MusicVolume = volume;
    }

    public void NextLanguage(bool isRight)
    {
        if (isRight)
        {
            _currentIndex++;
            if (_currentIndex == _languages.Count)
                _currentIndex = 0;
        }
        else
        {
            if (_currentIndex == 0)
                _currentIndex = _languages.Count;
            _currentIndex--;
        }

        LocalizationManager.CurrentLanguage = _languages[_currentIndex];
    }
}
