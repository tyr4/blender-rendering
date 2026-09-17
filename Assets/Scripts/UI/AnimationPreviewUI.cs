using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class AnimationPreviewUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject spacerPrefab;
    [SerializeField] private GameObject timestampPrefab;
    [SerializeField] private KeyframeComponent keyframePrefab;
    
    [Header("Layout Groups and Components")] 
    [SerializeField] private Transform keyframeGroupParent;
    [SerializeField] private Transform rulerGroupParent;
    [SerializeField] private RectTransform contentRect;
    
    [Header("Timeline variables")] 
    [SerializeField] private float itemWidth = 32f;
    [SerializeField] private float itemHeight = 32f;
    [SerializeField] private float pixelsPerSecond = 16f;
    [SerializeField] private float targetSpacing = 50f;
    [SerializeField] private float itemWidthMin = 16f;
    [SerializeField] private float itemWidthMax = 500f;
    
    [SerializeField] private float itemHeightMin = 32f;
    [SerializeField] private float itemHeightMax = 500f;
    
    [SerializeField] private float pixelsPerSecondMin = 16f;
    [SerializeField] private float pixelsPerSecondMax = 10000f;

    private struct KeyframeEntry
    {
        public float time;
        public Sprite sprite;
    }
    
    private AnimationClip _currentClip;
    private float _clipLength;
    private float _frameRate;
    
    private List<KeyframeEntry> _keyframeEntries = new();
    private List<KeyframeComponent> _spawnedKeyframes = new();
    private List<GameObject> _spawnedAll = new();
    private List<GameObject> _rulerSpawned = new();

    private void Start()
    {
        PythonController.OnAnimationClipGenerated += OnAnimationClipGenerated;
    }

    private void OnDestroy()
    {
        PythonController.OnAnimationClipGenerated -= OnAnimationClipGenerated;
    }

    public void OnAnimationClipGenerated(AnimationClip clip)
    {
        _currentClip = clip;
        _clipLength = clip.length;
        _frameRate = clip.frameRate;
        Clear();
        
#if UNITY_EDITOR
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

        foreach (var keyframe in keyframes)
        {
            _keyframeEntries.Add(new KeyframeEntry { time = keyframe.time, sprite = keyframe.value as Sprite });
        }

#endif
        Rebuild();
    }

    public void Rebuild()
    {
        int frameIndex = 0;
        
        foreach (var entry in _keyframeEntries)
        {
            var kfInstance = Instantiate(keyframePrefab, keyframeGroupParent, false);
            kfInstance.Init(entry.sprite, frameIndex, itemWidth, itemHeight, _currentClip.name);
            // kfInstance.SetWidth(itemWidth);
            // kfInstance.SetHeight(itemHeight);
            
            var rt = kfInstance.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, rt.pivot.y);
            PlaceAtTime(rt, SnapToFrame(entry.time));
            
            _spawnedAll.Add(kfInstance.gameObject);
            _spawnedKeyframes.Add(kfInstance);

            frameIndex++;
        }

        RebuildRuler();
        UpdateContentWidth();
    }

    private void RebuildRuler()
    {
        foreach (var go in _rulerSpawned) Destroy(go);
        _rulerSpawned.Clear();

        if (_keyframeEntries.Count == 0 || _frameRate <= 0f) return;

        float totalFrames = Mathf.CeilToInt(_clipLength * _frameRate);
        int frameInterval = Mathf.Max(1, Mathf.RoundToInt(GetNiceInterval()));

        Debug.Log($"frames {totalFrames} interval {frameInterval} cliplength {_clipLength}");
        for (int frame = 0; frame < totalFrames; frame += frameInterval)
        {
            float t = Mathf.Min(frame / _frameRate, _clipLength);
            
            var timestamp = Instantiate(timestampPrefab, rulerGroupParent);
            var rt = timestamp.GetComponent<RectTransform>();
            
            rt.pivot = new Vector2(0f, rt.pivot.y);
            
            var size = rt.sizeDelta;
            size.x = itemWidth;
            rt.sizeDelta = size;
            
            PlaceAtTime(rt, t);
            
            var label = timestamp.GetComponent<TextMeshProUGUI>();
            if (label != null) label.text = FormatUnityTime(t);
            
            _rulerSpawned.Add(timestamp);
        }
    }

    private float SnapToFrame(float time)
    {
        return Mathf.Round(time * _frameRate) / _frameRate;
    }

    private float GetNiceInterval()
    {
        float pixelsPerFrame = pixelsPerSecond / _frameRate;
        float rawFrames = targetSpacing / pixelsPerFrame;
        int[] niceFrameSteps = { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600 };
        
        foreach (var step in niceFrameSteps)
            if (step >= rawFrames)
                return step;

        return niceFrameSteps[^1];
    }

    private string FormatUnityTime(float time)
    {
        int totalFrames = Mathf.RoundToInt(time * _frameRate);
        int seconds = totalFrames / Mathf.RoundToInt(_frameRate);
        int frames = totalFrames % Mathf.RoundToInt(_frameRate);
        
        return $"{seconds}:{frames:00}";
    }

    private void UpdateContentWidth()
    {
        float totalDuration = _keyframeEntries.Count > 0 ? _keyframeEntries[^1].time : 0f;
        float width = totalDuration * pixelsPerSecond + itemWidth;
        
        contentRect.sizeDelta = new Vector2(width, contentRect.sizeDelta.y);
    }
    
    // TODO: replace this with object pool logic
    private void Clear()
    {
        foreach (var go in _spawnedAll) Destroy(go);

        _keyframeEntries.Clear();
        _spawnedAll.Clear();
        _spawnedKeyframes.Clear();
    }

    public void ChangeSizeConstraints(float spacerValue, float itemWidthValue, float itemHeightValue)
    {
        pixelsPerSecond = Mathf.Clamp(pixelsPerSecond + spacerValue, pixelsPerSecondMin, pixelsPerSecondMax);
        Debug.Log("pixelsPerSecond: " + pixelsPerSecond);

        itemWidth = Mathf.Clamp(itemWidth + itemWidthValue, itemWidthMin, itemWidthMax);
        Debug.Log($"itemWidth: {itemWidth}");

        itemHeight = Mathf.Clamp(itemHeight + itemHeightValue, itemHeightMin, itemHeightMax);
        
        if (_currentClip == null) return;
        OnAnimationClipGenerated(_currentClip);
    }

    private void PlaceAtTime(RectTransform rt, float time)
    {
        var pos = rt.anchoredPosition;
        pos.x = time * pixelsPerSecond;
        rt.anchoredPosition = pos;
    }

    private void ReapplyTogglesAfterGeneration()
    {
        foreach (var go in _spawnedAll)
        {
            // if (go.isi)
        }
    }
}
