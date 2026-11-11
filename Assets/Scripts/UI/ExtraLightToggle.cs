using UnityEngine;
using UnityEngine.UI;

public class ExtraLightToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleClick);
    }
    
    private void OnToggleClick(bool value)
    {
        PythonController.Instance.settings.extra_light = value;
        PythonController.Instance.ApplyExtraLight();
    }
}