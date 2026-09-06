using System;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleTextButton : MonoBehaviour
{
    private Button _button;
    private string _fullLogText;
    private Color _color;

    public static event Action<string, Color> OnConsoleButtonClicked;
    
    private void Awake()
    {
        _button = GetComponent<Button>();

        if (_button == null) return;
        
        _button.onClick.AddListener(OnClick);
    }
    
    private void OnDestroy()
    {
        if (_button == null) return;
        
        _button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        OnConsoleButtonClicked?.Invoke(_fullLogText, _color);
    }

    public void SetFullLogText(string msg, Color color)
    {
        _fullLogText = msg;
        _color = color;
    }
}
