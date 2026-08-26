using System;
using UnityEngine;

public class SliderUpdateManager : MonoBehaviour
{
    [SerializeField] private SliderValueUpdateInvoker resolutionX;
    [SerializeField] private SliderValueUpdateInvoker resolutionY;
    
    // [SerializeField] private SliderValueUpdateInvoker cameraShiftX;
    // [SerializeField] private SliderValueUpdateInvoker cameraShiftY;
    [SerializeField] private SliderValueUpdateInvoker cameraOrthographicScale;
    
    [SerializeField] private SliderPairValueUpdateInvoker cameraPosition;
    [SerializeField] private SliderPairValueUpdateInvoker startingRotation;
    [SerializeField] private SliderPairValueUpdateInvoker parentObjectPosition;
    [SerializeField] private SliderPairValueUpdateInvoker parentObjectRotation;
    [SerializeField] private SliderPairValueUpdateInvoker repositionObjectPosition;
    [SerializeField] private SliderPairValueUpdateInvoker repositionObjectRotation;
    
    private UserSettings settings => PythonController.Instance.settings;

    public static event Action OnSettingsChangedRequest;
    
    private void Start()
    {
        UpdateSceneSettingsUI.OnSceneObjectsLoaded += OnSceneLoaded;

        resolutionX.OnSliderSettled += ApplyResolutionX;
        resolutionY.OnSliderSettled += ApplyResolutionY;
        
        cameraOrthographicScale.OnSliderSettled += ApplyCameraOrthographicScale;
        
        cameraPosition.OnSliderPairSettled += ApplyCameraPosition;
        
        startingRotation.OnSliderPairSettled += ApplyStartingRotation;
        
        parentObjectPosition.OnSliderPairSettled += ApplyParentObjectPosition;
        parentObjectRotation.OnSliderPairSettled += ApplyParentObjectRotation;
        
        repositionObjectPosition.OnSliderPairSettled += ApplyRepositionObjectPosition;
        repositionObjectRotation.OnSliderPairSettled += ApplyRepositionObjectRotation;
    }

    private void OnDestroy()
    {
        UpdateSceneSettingsUI.OnSceneObjectsLoaded -= OnSceneLoaded;

        resolutionX.OnSliderSettled -= ApplyResolutionX;
        resolutionY.OnSliderSettled -= ApplyResolutionY;
        
        cameraOrthographicScale.OnSliderSettled -= ApplyCameraOrthographicScale;
        
        cameraPosition.OnSliderPairSettled -= ApplyCameraPosition;
        
        startingRotation.OnSliderPairSettled -= ApplyStartingRotation;
        
        parentObjectPosition.OnSliderPairSettled -= ApplyParentObjectPosition;
        parentObjectRotation.OnSliderPairSettled -= ApplyParentObjectRotation;
        
        repositionObjectPosition.OnSliderPairSettled -= ApplyRepositionObjectPosition;
        repositionObjectRotation.OnSliderPairSettled -= ApplyRepositionObjectRotation;
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
        cameraOrthographicScale.ChangeSliderValue((float)settings.camera_orthographic_scale!);
        
        // cameraPosition.ChangeSliderPairValues(settings.camera_position);
    }
    

    #region OnSettingsChangedRequests 
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
    
    private void ApplyCameraShiftX(object value)
    {
        settings.camera_shift_x = Convert.ToSingle(value);
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyCameraShiftY(object value)
    {
        settings.camera_shift_y = Convert.ToSingle(value);
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyCameraOrthographicScale(object value)
    {
        settings.camera_orthographic_scale = Convert.ToSingle(value);
        
        OnSettingsChangedRequest?.Invoke();
    }

    private void ApplyCameraPosition(object value)
    {
        settings.camera_position = (float[])value;
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyStartingRotation(object value)
    {
        settings.starting_rotation = (float[])value;
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyParentObjectPosition(object value)
    {
        settings.parent_object_position = (float[])value;
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyParentObjectRotation(object value)
    {
        settings.parent_object_rotation = (float[])value;
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyRepositionObjectPosition(object value)
    {
        settings.reposition_object_position = (float[])value;
        
        OnSettingsChangedRequest?.Invoke();
    }
    
    private void ApplyRepositionObjectRotation(object value)
    {
        settings.reposition_object_rotation = (float[])value;
        
        OnSettingsChangedRequest?.Invoke();
    }
    #endregion
}
