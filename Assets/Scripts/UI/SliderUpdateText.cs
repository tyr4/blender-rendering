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
    
    public void OnValueChanged()
    {
        if (targetText == null || _slider == null) return;

        targetText.text = _slider.wholeNumbers ? $"{_slider.value:F0}" : $"{_slider.value:F2}";
    }
}
