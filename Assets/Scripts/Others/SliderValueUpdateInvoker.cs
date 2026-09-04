using System;
using System.Threading.Tasks.Sources;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueUpdateInvoker : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] [CanBeNull] private Toggle toggle;

    public bool IsEnabled = true;
    
    private float _debounceDelay = 0.35f;
    private float _lastChangedTime;
    private float _lastValue;
    private bool _pending;
    
    public event Action<object> OnSliderSettled;
    
    private void Start()
    {
        inputField.onEndEdit.AddListener(OnInputFieldValueChangedEnd);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        toggle?.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnDestroy()
    {
        inputField.onEndEdit.RemoveListener(OnInputFieldValueChangedEnd);
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        toggle?.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnInputFieldValueChangedEnd(string text)
    {
        var value = slider.wholeNumbers ? int.Parse(text) : float.Parse(text);

        if (Mathf.Approximately(_lastValue, value)) return;
        
        _lastValue = value;
        OnSliderSettled?.Invoke(value);
        
        Debug.Log($"AM INVOCAT {value}");
    }

    private void OnSliderValueChanged(float value)
    {
        _lastChangedTime = Time.time;
        _lastValue = value;
        _pending = true;
    }

    private void Update()
    {
        if (IsEnabled && _pending && Time.time - _lastChangedTime >= _debounceDelay)
        {
            _pending = false;
            var fixedValue = slider.wholeNumbers ? (int)_lastValue : _lastValue;
            
            OnSliderSettled?.Invoke(fixedValue);
            Debug.Log($"AM INVOCAT {fixedValue}");
        }
    }

    private void OnToggleValueChanged(bool value)
    {
        IsEnabled = value;
    }

    public void ChangeSliderValue(float value)
    {
        if (inputField == null || slider == null) return;
        
        slider.SetValueWithoutNotify(value);
        Debug.Log($"changed value to {value}");
        
        inputField.text = slider.wholeNumbers ? $"{slider.value:F0}" : $"{slider.value:F2}";
    }
}
