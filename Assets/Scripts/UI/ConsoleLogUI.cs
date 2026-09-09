using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting;
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

    private struct PoolEntry
    {
        public GameObject obj;
        public TextMeshProUGUI displayText;
        public LogType logType;
        public string cleanText;
        public string tag;
        public bool hasContent;
    }
    
    [Header("Log Variables")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private int maxObjectCount = 100;
    
    [Header("Color coding")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private List<LogTagColor> tagColors = new();
    [SerializeField] private List<LogTypeColor> typeColors = new();
    
    private List<PoolEntry> _pool = new();
    private int _nextIndex = 0;
    private const string IgnoreTag = "[INTERNAL]";
    private const string HighlightedColor = "<color=#FFFF00>";
    
    private ConcurrentQueue<(string message, string stackTrace, LogType type)> _pendingLogs = new();
    private StringBuilder _sb = new();
    
    private ConsoleFilterToggles _activeFilters;
    private string _currentSearchKeyword;
    
    private void Awake()
    {
        for (int i = 0; i < maxObjectCount; i++)
        {
            var obj = Instantiate(textPrefab, transform);
            obj.SetActive(false);
            
            _pool.Add(new PoolEntry
            {
                obj = obj,
                logType = LogType.Log,
                tag = null,
                hasContent = false,
                cleanText = null,
                displayText = obj.GetComponentInChildren<TextMeshProUGUI>()
            });
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
        var entry = _pool[_nextIndex];
        var obj = entry.obj;
        var log = ParseLogString(logString, type);
        var text = entry.displayText;
        
        entry.logType = type;
        entry.tag = log.tag;
        entry.hasContent = true;
        entry.cleanText = log.displayText;
        _pool[_nextIndex] = entry;
        
        obj.SetActive(true);
        obj.transform.SetAsLastSibling();
        
        text.SetText(log.displayText);
        text.color = log.color;

        // Debug.Log($"{IgnoreTag}setting color {log.color}");

        // obj.GetComponentInChildren<TextMeshProUGUI>().SetText(logString);
        obj.GetComponentInChildren<ConsoleTextButton>().SetFullLogText(stackTrace, log.color);
        
        _nextIndex = (_nextIndex + 1) % _pool.Count;
        RefreshVisibility();
    }

    private (string displayText, Color color, string tag) ParseLogString(string logString, LogType type)
    {
        var color = GetColorByLogType(type);
        var displayText = logString;
        string tag = null;

        foreach (var entry in tagColors)
        {
            if (logString.StartsWith(entry.tag))
            {
                displayText = displayText.Substring(entry.tag.Length).Replace('\\', '/');
                color = entry.color;
                tag = entry.tag;
                break;
            }
        }
        
        return (displayText, color, tag);
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

    public void ApplyToggles(ConsoleFilterToggles filters)
    {
        _activeFilters = filters;
        RefreshVisibility();
    }

    public void ApplySearchFilter(string keyword)
    {
        _currentSearchKeyword = keyword;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            var entry = _pool[i];
            if (!entry.hasContent) continue;

            bool toggleVisible = IsVisibleByToggles(entry);
            bool searchVisible = string.IsNullOrEmpty(_currentSearchKeyword) ||
                                 entry.cleanText.Contains(_currentSearchKeyword, StringComparison.OrdinalIgnoreCase);

            entry.obj.SetActive(toggleVisible && searchVisible);

            entry.displayText.SetText(
                searchVisible && !string.IsNullOrEmpty(_currentSearchKeyword)
                    ? HighlightSearchTerm(entry.cleanText, _currentSearchKeyword)
                    : entry.cleanText
            );
        }
    }
    
    private bool IsVisibleByToggles(PoolEntry entry)
    {
        if (_activeFilters == null || !_activeFilters.AnyFieldActive)
            return true;

        bool isErrorLike = entry.logType == LogType.Error;
        return isErrorLike ? _activeFilters.ErrorsVisible : _activeFilters.IsTagVisible(entry.tag);
    }

    private string HighlightSearchTerm(string source, string keyword)
    {
        _sb.Clear();
        int searchStart = 0;
        int matchIndex;

        while ((matchIndex = source.IndexOf(keyword, searchStart, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            _sb.Append(source, searchStart, matchIndex - searchStart);
            _sb.Append(HighlightedColor);
            _sb.Append(source, matchIndex, keyword.Length);
            _sb.Append("</color>");
            searchStart = matchIndex + keyword.Length;
        }

        _sb.Append(source, searchStart, source.Length - searchStart);
        return _sb.ToString();
    }
}

