using System;
using UnityEngine;

public class SliderUpdateManager : MonoBehaviour
{
    [SerializeField] private SliderValueUpdateInvoker resolutionX;
    [SerializeField] private SliderValueUpdateInvoker resolutionY;

    private UserSettings settings => PythonController.Instance.settings;

    public static event Action OnSettingsChangedRequest;
    
    private void Start()
    {
        UpdateSceneSettingsUI.OnSceneObjectsLoaded += OnSceneLoaded;

        resolutionX.OnSliderSettled += ApplyResolutionX;
        resolutionY.OnSliderSettled += ApplyResolutionY;
        
    }

    private void OnDestroy()
    {
        UpdateSceneSettingsUI.OnSceneObjectsLoaded -= OnSceneLoaded;
        
        resolutionX.OnSliderSettled -= ApplyResolutionX;
        resolutionY.OnSliderSettled -= ApplyResolutionY;
    }

    private void OnSettingsLoaded()
    {
        ApplyNonSceneRelatedSliderValues();
    }
    
    private void OnSceneLoaded()
    {
        ApplyNonSceneRelatedSliderValues();
        ApplySceneRelatedSceneSliderValues();
    }

    // this only modifies values that arent tied to scene objects
    private void ApplyNonSceneRelatedSliderValues()
    {
        resolutionX.ChangeSliderValue(settings.resolution_x);
        resolutionY.ChangeSliderValue(settings.resolution_y);
    }

    private void ApplySceneRelatedSceneSliderValues()
    {
        
    }

    private void ApplyResolutionX(object value)
    {
        settings.resolution_x = Convert.ToInt32(value);
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyResolutionY(object value)
    {
        settings.resolution_y = Convert.ToInt32(value);
        
        OnSettingsChangedRequest?.Invoke();
    }
}
