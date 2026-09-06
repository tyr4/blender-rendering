using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleFilterToggles : MonoBehaviour
{
    [SerializeField] private ConsoleLogUI consoleLog;
    
    [SerializeField] private Toggle processToggle;
    [SerializeField] private Toggle renderToggle;
    [SerializeField] private Toggle pythonToggle;
    [SerializeField] private Toggle onActionsToggle;
    [SerializeField] private Toggle errorToggle;

    public bool ErrorsVisible => errorToggle.isOn;
    public bool AnyFieldActive => processToggle.isOn || renderToggle.isOn || pythonToggle.isOn ||  onActionsToggle.isOn || errorToggle.isOn;
    
    private void Start()
    {
        processToggle.onValueChanged.AddListener(_ => consoleLog.ApplyToggles(this));
        renderToggle.onValueChanged.AddListener(_ => consoleLog.ApplyToggles(this));
        pythonToggle.onValueChanged.AddListener(_ => consoleLog.ApplyToggles(this));
        onActionsToggle.onValueChanged.AddListener(_ => consoleLog.ApplyToggles(this));
        errorToggle.onValueChanged.AddListener(_ => consoleLog.ApplyToggles(this));

        consoleLog.ApplyToggles(this); // apply initial toggle states on startup
    }

    public bool IsTagVisible(string tag)
    {
        return tag switch
        {
            "[PROCESS]" => processToggle.isOn,
            "[RENDER]" => renderToggle.isOn,
            "[PYTHON]" => pythonToggle.isOn,
            "[ONACTIONBEGIN]" or "[ONACTIONFINISH]" => onActionsToggle.isOn, 
            _ => true // untagged logs always show
        };
    }
}
