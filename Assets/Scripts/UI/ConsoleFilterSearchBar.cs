using System;
using TMPro;
using UnityEngine;

public class ConsoleFilterSearchBar : MonoBehaviour
{
    [SerializeField] private ConsoleLogUI consoleLog;
    [SerializeField] private TMP_InputField inputField;

    private void Start()
    {
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(string value)
    {
        consoleLog.ApplySearchFilter(value);
    }
}
