using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SliderPairValueUpdateInvoker : MonoBehaviour
{
    [SerializeField] private SliderValueUpdateInvoker pairX;
    [SerializeField] private SliderValueUpdateInvoker pairY;
    [SerializeField] private SliderValueUpdateInvoker pairZ;
    [SerializeField] private Toggle toggle;

    public event Action<float[]> OnSliderPairSettled;
    private Action<object> _onX, _onY, _onZ;

    private Func<float[]> _currentSettingsGetter;
    private Action<float[]> _currentSettingsSetter;
    
    private Action<bool> _snapshotSettingsSetter;
    
    private bool _shouldInvoke = true;

    private void Start()
    {
        _onX = v => UpdateComponent(0, v);
        _onY = v => UpdateComponent(1, v);
        _onZ = v => UpdateComponent(2, v);
        
        pairX.OnSliderSettled += _onX;
        pairY.OnSliderSettled += _onY;
        pairZ.OnSliderSettled += _onZ;
        
        toggle?.onValueChanged.AddListener(OnToggleValueChanged);
        
    }

    private void OnDestroy()
    {
        pairX.OnSliderSettled -= _onX;
        pairY.OnSliderSettled -= _onY;
        pairZ.OnSliderSettled -= _onZ;
        
        toggle?.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    public void BindOnSettingsChanged(Func<float[]> getter, Action<float[]> setter)
    {
        _currentSettingsGetter = getter;
        _currentSettingsSetter = setter;
        
        RefreshFromSource();
    }
    
    public void BindOnToggleValueChanged(Action<bool> setter)
    {
        _snapshotSettingsSetter = setter;
        
        // RefreshFromSource();
    }

    private void UpdateComponent(int index, object value)
    {
        if (_currentSettingsGetter == null) return;
        
        var currentValue = _currentSettingsGetter();
        currentValue[index] = Convert.ToSingle(value);
        
        _currentSettingsSetter?.Invoke(currentValue);
        if (_shouldInvoke) OnSliderPairSettled?.Invoke(currentValue);
    }

    private void RefreshFromSource()
    {
        if (_currentSettingsGetter == null) return;
        
        ChangeSliderPairValues(_currentSettingsGetter());
    }

    public void ChangeSliderPairValues(float[] vector)
    {
        if (vector == null) return;
        
        _shouldInvoke = false;
        
        pairX.ChangeSliderValue(vector[0]);
        pairY.ChangeSliderValue(vector[1]);
        pairZ.ChangeSliderValue(vector[2]);
        
        _shouldInvoke = true; 
    }

    private void OnToggleValueChanged(bool value)
    {
        _snapshotSettingsSetter?.Invoke(!value);
        PythonController.Instance.ApplySceneSettings();
    }
}
