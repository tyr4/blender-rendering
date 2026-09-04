using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SliderUpdateManager : MonoBehaviour
{
    [SerializeField] private SliderValueUpdateInvoker resolutionX;
    [SerializeField] private SliderValueUpdateInvoker resolutionY;
    
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
        // subscribe to events
        PythonController.OnObjectDataRequested += OnObjectDataRequested;

        resolutionX.OnSliderSettled += ApplyResolutionX;
        resolutionY.OnSliderSettled += ApplyResolutionY;
        
        cameraOrthographicScale.OnSliderSettled += ApplyCameraOrthographicScale;
        
        cameraPosition.OnSliderPairSettled += ApplyCameraPosition;
        
        startingRotation.OnSliderPairSettled += ApplyStartingRotation;
        
        parentObjectPosition.OnSliderPairSettled += ApplyParentObjectPosition;
        parentObjectRotation.OnSliderPairSettled += ApplyParentObjectRotation;
        
        repositionObjectPosition.OnSliderPairSettled += ApplyRepositionObjectPosition;
        repositionObjectRotation.OnSliderPairSettled += ApplyRepositionObjectRotation;
        
        // set correct UserSettings value to each pair slider
        cameraPosition.Bind(() => PythonController.Instance.settings.camera_position,
                            v => PythonController.Instance.settings.camera_position = v);
        
        startingRotation.Bind(() => PythonController.Instance.settings.starting_rotation,
            v => PythonController.Instance.settings.starting_rotation = v);
        
        parentObjectPosition.Bind(() => PythonController.Instance.settings.parent_object_position,
            v => PythonController.Instance.settings.parent_object_position = v);
        
        parentObjectRotation.Bind(() => PythonController.Instance.settings.parent_object_rotation,
            v => PythonController.Instance.settings.parent_object_rotation = v);
        
        repositionObjectPosition.Bind(() => PythonController.Instance.settings.reposition_object_position,
            v => PythonController.Instance.settings.reposition_object_position = v);
        
        repositionObjectRotation.Bind(() => PythonController.Instance.settings.reposition_object_rotation,
            v => PythonController.Instance.settings.reposition_object_rotation = v);
    }

    private void OnDestroy()
    {
        PythonController.OnObjectDataRequested -= OnObjectDataRequested;

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
    
    private void OnObjectDataRequested(Dictionary<string, JToken> data)
    {
        ChangeFloatValue(cameraOrthographicScale, data["camera_orthographic_scale"]);
        ChangeFloatValuePair(cameraPosition, data["camera_position"]);
        
        ChangeFloatValuePair(startingRotation, data["starting_rotation"]);
        
        ChangeFloatValuePair(parentObjectPosition, data["parent_object_position"]);
        ChangeFloatValuePair(parentObjectRotation, data["parent_object_rotation"]);
        
        ChangeFloatValuePair(repositionObjectPosition, data["reposition_object_position"]);
        ChangeFloatValuePair(repositionObjectRotation, data["reposition_object_rotation"]);
        
        
        foreach (var (key, value) in data)
        {
            Debug.Log($"key {key} value {value}");
        }
    }

    private void ChangeFloatValue(SliderValueUpdateInvoker slider, JToken value)
    {
        var result = value.ToObject<float>();
        
        slider.ChangeSliderValue(result);
    }

    private void ChangeFloatValuePair(SliderPairValueUpdateInvoker slider, JToken value)
    {
        var result = value.ToObject<float[]>();

        Debug.Log(string.Join(", ", result));
        slider.ChangeSliderPairValues(result);
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

    #region  OnSettingsUpdated

    private void ApplySettingsUpdated(UserSettings userSettings)
    {
        // cameraPosition.v
    }
    #endregion
}
