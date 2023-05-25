using System.Collections;
using GameRules;
using GameRules.Scripts.UI;
using I2.Loc;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

public class NewVersionWindow : MonoBehaviour
{
    private bool _isComplete;
    
    private long Parse(string value, int digitMax)
    {
        long result = 0;

        int i = 0;
        for (; i < math.min(digitMax, value.Length); i++)
            result = result * 10 + value[i] - '0';

        for (; i < digitMax; i++)
            result *= 10;

        return result;
    }

    private long GetValue(string[] values, int index, int digitMax, int multiplayer)
    {
        if (values.Length <= index)
            return 0;
        return Parse(values[index], digitMax) * multiplayer;
    }
    
    private bool CompareVersion(string oldV, string newV)
    {
        var valuesOld = oldV.Split('.');
        var valuesNew = newV.Split('.');

        var old = GetValue(valuesOld, 0, 3, 100000000) +
                  GetValue(valuesOld, 1, 4, 10000) +
                  GetValue(valuesOld, 2, 4, 1);
        
        var target = GetValue(valuesNew, 0, 3, 100000000) +
                  GetValue(valuesNew, 1, 4, 10000) +
                  GetValue(valuesNew, 2, 4, 1);
        
        return old >= target;
    }
    
    private struct NewVersions
    {
        public string min;
        public string target;
    }
    
    public IEnumerator TryShow(string newVersionJson, DialogViewBox dialogViewBox)
    {
        var newVersion = JsonUtility.FromJson<NewVersions>(newVersionJson);
        
        if(CompareVersion(Application.version, newVersion.target))
           yield break;

        _isComplete = false;
        
        
        dialogViewBox.SetParameter("Version", newVersion.target);
        dialogViewBox.Show("Loading/NEW_VERSION_TITLE", "Loading/NEW_VERSION_MESSAGE", 370);

        if (CompareVersion(Application.version, newVersion.min))
        {
            dialogViewBox.NegativeBtn
                .SetLabelValue("Loading/NEW_VERSION_LATER_BNT")
                .SetCallback(() =>
                {
                    _isComplete = true;
                    dialogViewBox.Hide();
                })
                .SetActive(true);
        }
        else
            dialogViewBox.NegativeBtn.SetActive(false);
       
        dialogViewBox.PositiveBtn
            .SetLabelValue("Loading/NEW_VERSION_UPDATE_BTN")
            .SetCallback(UpdateNew)
            .SetActive(true);

        while (!_isComplete)
            yield return null;
    }


    public void CompleteHide()
    {
        _isComplete = true;
    }
    
    public void UpdateNew()
    {
#if UNITY_IOS
        Application.OpenURL("itms-apps://itunes.apple.com/app/id" + GetOrPush.iOS_AppId);
#else
        Application.OpenURL("market://details?id=" + Application.identifier);
#endif
    }
}
