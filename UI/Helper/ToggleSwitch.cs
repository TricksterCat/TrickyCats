using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSwitch : MonoBehaviour
{
    public UnityEvent OnEvent;
    public UnityEvent OffEvent;
    
    public void OnChangeToggle(bool isActive)
    {
        if(isActive)
            OnEvent?.Invoke();
        else 
            OffEvent?.Invoke();
    }
}
