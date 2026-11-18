using System;
using UnityEngine;
using UnityEngine.UI;

public class ExtraLightToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleClick);
        
        PythonController.OnSettingsChanged += ChangeToggleValue;
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleClick);
        
        PythonController.OnSettingsChanged -= ChangeToggleValue;
    }

    private void OnToggleClick(bool value)
    {
        PythonController.Instance.settings.extra_light = value;
        PythonController.Instance.ApplyExtraLight();
    }

    private void ChangeToggleValue()
    {
        var toggled = PythonController.Instance.settings.extra_light;
        
        toggle.SetIsOnWithoutNotify(toggled);
    }
}