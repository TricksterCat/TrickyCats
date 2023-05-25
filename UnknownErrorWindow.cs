using System.Collections;
using GameRules.Scripts.UI;
public static class UnknownErrorWindow
{
    public static IEnumerator TryShow(DialogViewBox dialogViewBox)
    {
        dialogViewBox.Show("Loading/UNKNOWN_ERROR_TITLE", "Loading/UNKNOWN_ERROR_MESSAGE", 370);
        bool isComplete = false;
        
        dialogViewBox.NegativeBtn
            .SetLabelValue("Loading/BTN_TRY_AGAIN")
            .SetCallback(() =>
            {
                isComplete = true;
                dialogViewBox.Hide();
            })
            .SetActive(true);
        
        dialogViewBox.PositiveBtn
            .SetActive(false);

        while (!isComplete)
            yield return null;
    }
}
