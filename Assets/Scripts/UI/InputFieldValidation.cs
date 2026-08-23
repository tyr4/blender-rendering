using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldValidation : MonoBehaviour
{
    private TMP_InputField _inputField;
    
    private readonly string _patternFloat = @"^-?[0-9]*\.?[0-9]*$";
    private readonly string _patternInt = @"^(?!-*[0-9]*$).*";

    private string _selectedPattern = @"^-?[0-9]*\.?[0-9]*$";
    private string _lastValidText;
    
    public event Action<string> OnValueSanitized;

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();

        if (_inputField != null)
        {
            _inputField.onValueChanged.AddListener(OnValueChanged);
        }
    }

    public void SetInputSanitizerType(Slider slider)
    {
        if (slider == null)
        {
            Debug.LogError("NU AI SLIDER");
            _selectedPattern = _patternInt;
            
            return;
        }
        
        _selectedPattern = slider.wholeNumbers ? _patternInt : _patternFloat;
    }

    private void OnDestroy()
    {
        if (_inputField == null) return;
        
        _inputField.onValueChanged.RemoveListener(OnValueChanged);
    }
    
    private void OnValueChanged(string text)
    {
        Debug.Log($"Current {text} last valid {_lastValidText}");
        
        if (Regex.IsMatch(text, _selectedPattern))
        {
            _lastValidText = text;
            OnValueSanitized?.Invoke(text);
        }

        else
        {
            _inputField.SetTextWithoutNotify(_lastValidText);
        }
    }
}
