using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using GameRules.Scripts.Server;
using GameRules.Scripts.UI;
using UnityEngine;


[System.Serializable]
public class NotInternet : MonoBehaviour
{
    public static bool HasInternet { get; private set; }
    [SerializeField, Range(0.01f, 0.6f)]
    private float _timePreOrPost;

    [SerializeField]
    private Sprite _refreshIcon;
    
    public static NotInternet Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator WaitInternet(DialogViewBox dialog, bool autoPing = true)
    {
        DOTween.Complete("ToLoadingContent", true);
        if(autoPing)
            DOTween.PlayBackwards("ToLoadingContent");

        var time = Time.unscaledTime + 0.1f;
        while (Time.unscaledTime < time)
            yield return null;
        
        dialog.NegativeBtn
            .SetLabelValue("Loading/BTN_TRY_AGAIN", _refreshIcon)
            .SetCallback(ReTryScan)
            .SetActive(true);
        
        dialog.PositiveBtn.SetActive(false);
        
        dialog.Show("Loading/NOT_INTERNET_TITLE", "Loading/NOT_INTERNET_MESSAGE", 240);
        HasInternet = false;

        if (autoPing)
        {
            var ping = HasPing();
        
            time = Time.unscaledTime + 1f;
            while (!(ping.IsCompleted || ping.IsFaulted || ping.IsCanceled))
                yield return null;
        
            while (Time.unscaledTime < time)
                yield return null;
        
            if (ping.IsCompleted && ping.Result)
            {
                HasInternet = true;
                dialog.OnNextHide += () =>
                {
                    DOTween.PlayForward("ToLoadingContent");
                    DOTween.Complete("ToLoadingContent", true);
                };
            
                dialog.Hide();
                yield break;
            }
        }
        
        DOTween.PlayForward("ToLoadingContent");
        
        while (!HasInternet)
            yield return null;
        
        dialog.Hide();
    }

    private async Task<bool> HasPing()
    {
        return await ServerRequest.Instance.HasPing() != ErrorCode.NotInternet;
    }

    private async void ReTryScan()
    {
        DOTween.PlayBackwards("ToLoadingContent");
        var task = HasPing();

        await Task.Delay((int)(_timePreOrPost * 1000));
        await task;
        
        if (task.Result)
            HasInternet = true;
        
        DOTween.PlayForward("ToLoadingContent");
    }
}
