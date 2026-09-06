using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleLogUI : MonoBehaviour
{
    [Serializable]
    public struct LogTagColor
    {
        public string tag;
        public Color color;
    }
    
    [Serializable]
    public struct LogTypeColor
    {
        public LogType logType;
        public Color color;
    }
    
    [Header("Log Variables")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private int maxObjectCount = 100;
    
    [Header("Color coding")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private List<LogTagColor> tagColors = new();
    [SerializeField] private List<LogTypeColor> typeColors = new();
    
    private List<GameObject> _pool = new();
    private int _nextIndex = 0;
    private const string IgnoreTag = "[INTERNAL]";
    
    private ConcurrentQueue<(string message, string stackTrace, LogType type)> _pendingLogs = new();

    private void Awake()
    {
        for (int i = 0; i < maxObjectCount; i++)
        {
            var obj = Instantiate(textPrefab, transform);
            obj.SetActive(false);
            _pool.Add(obj);
        }
    }
    
    private void Start()
    {
        Application.logMessageReceivedThreaded += OnLogReceivedThreaded;
    }

    private void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= OnLogReceivedThreaded;
    }

    private void OnLogReceivedThreaded(string message, string stackTrace, LogType type)
    {
        if (message.StartsWith(IgnoreTag)) return;
        
        _pendingLogs.Enqueue((message, stackTrace, type));
    }

    private void Update()
    {
        while (_pendingLogs.TryDequeue(out var entry))
        {
            SpawnLogEntry(entry.message, entry.stackTrace, entry.type);
        }
    }
    
    private void SpawnLogEntry(string logString, string stackTrace, LogType type)
    {
        var obj = _pool[_nextIndex];
        _nextIndex = (_nextIndex + 1) % _pool.Count;

        obj.SetActive(true);
        obj.transform.SetAsLastSibling();

        var text = obj.GetComponentInChildren<TextMeshProUGUI>();
        var log = ParseLogString(logString, type);

        text.SetText(log.displayText);
        text.color = log.color;

        Debug.Log($"{IgnoreTag}setting color {log.color}");

        // obj.GetComponentInChildren<TextMeshProUGUI>().SetText(logString);
        obj.GetComponentInChildren<ConsoleTextButton>().SetFullLogText(stackTrace, log.color);
    }

    private (string displayText, Color color) ParseLogString(string logString, LogType type)
    {
        var color = GetColorByLogType(type);
        var displayText = logString;

        foreach (var entry in tagColors)
        {
            if (logString.StartsWith(entry.tag))
            {
                displayText = displayText.Substring(entry.tag.Length);
                color = entry.color;
                break;
            }
        }
        
        return (displayText, color);
    }

    private Color GetColorByLogType(LogType type)
    {
        foreach (var entry in typeColors)
        {
            if (entry.logType == type)
            {
                return entry.color;
            }
        }

        return defaultColor;
    }
}

