using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderUpdateText : MonoBehaviour
{
    [SerializeField] private TMP_InputField targetText;

    private Slider _slider;
    
    private void Start()
    {
        _slider = GetComponent<Slider>();
        
        OnValueChanged();
    }

    // TODO: update this when loading the scenes settings, make a ui manager
    // TODO: that holds a reference to all UI elements (sliders, toggles) and updates them
    // TODO: based on the settings loaded at the scart
    public void SetSliderValue(float value)
    {
        _slider.value = value;
    }
    
    public void OnValueChanged()
    {
        if (targetText == null || _slider == null) return;

        targetText.text = _slider.wholeNumbers ? $"{_slider.value:F0}" : $"{_slider.value:F2}";
    }
}
