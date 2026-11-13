using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BatchModeAnimationButton : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Button mainButton;
    [SerializeField] private TextMeshProUGUI text;

    public static event Action<int> OnBatchAnimationButtonClicked;
    
    private int _index;
    
    private void Start()
    {
        mainButton.onClick.AddListener(ButtonOnClick);
    }

    private void OnDestroy()
    {
        mainButton.onClick.RemoveListener(ButtonOnClick);
    }

    public void Init(int index, ToggleGroup group, string modelName)
    {
        _index = index;
        
        toggle.group = group;
        toggle.interactable = false;

        text.text = modelName;
    }

    private void ButtonOnClick()
    {
        OnBatchAnimationButtonClicked?.Invoke(_index);
        toggle.isOn = true;
    }

    public void SetToggleActive()
    {
        toggle.isOn = true;
    }
}
