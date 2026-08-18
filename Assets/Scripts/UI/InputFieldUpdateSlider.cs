using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldUpdateSlider : MonoBehaviour
{
    [SerializeField] private InputFieldValidation validator;
    
    private Slider _slider;
    
    private void Start()
    {
        _slider = GetComponent<Slider>();
        
        // decide whether to use the float or int regex
        validator.OnValueSanitized += SetSliderValue;
        validator.SetInputSanitizerType(_slider);
    }

    private void OnDestroy()
    {
        validator.OnValueSanitized -= SetSliderValue;
    }

    private void SetSliderValue(string inputText)
    {
        if (float.TryParse(inputText, out var parsed))
        {
            _slider.value = parsed;
        }
    }
}
