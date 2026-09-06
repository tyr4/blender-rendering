using System;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleToggle : MonoBehaviour
{
    [SerializeField] private Color toggledColor;
    
    private Toggle _toggle;
    private Image _image;
    private Color _initialColor;
    
    private void Start()
    {
        _toggle = GetComponent<Toggle>();
        _image = GetComponent<Image>();
        
        _initialColor = _image.color;
        _toggle.onValueChanged.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        _toggle.onValueChanged.RemoveListener(OnClick);
    }

    private void OnClick(bool value)
    {
        _image.color = value ? toggledColor : _initialColor;
    }
}
