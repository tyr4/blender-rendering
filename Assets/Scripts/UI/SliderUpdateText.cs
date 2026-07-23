using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderUpdateText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;

    [Header("Slider Properties")] 
    [SerializeField] private bool wholeNumbers;
    [SerializeField] private float min, max;

    private Slider _slider;
    
    private void Start()
    {
        _slider = GetComponent<Slider>();
        
        _slider.wholeNumbers = wholeNumbers;
        _slider.minValue = min;
        _slider.maxValue = max;
        
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
        
        targetText.text = $"{_slider.value:F2}";
    }
}
