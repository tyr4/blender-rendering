#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class AnimationPreviewUI : MonoBehaviour
{
    [Header("Helper text and containers")]
    [SerializeField] private GameObject helperText;
    [SerializeField] private GameObject timelineObject;
    [SerializeField] private GameObject animationResultsObject;
    [SerializeField] private GameObject animationResultsContentObject;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject spacerPrefab;
    [SerializeField] private GameObject timestampPrefab;
    [SerializeField] private KeyframeComponent keyframePrefab;
    [SerializeField] private AnimationContainer animationPrefab;
    
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

    private bool _triggeredByZoom;
    
    private List<KeyframeEntry> _keyframeEntries = new();
    private List<KeyframeComponent> _spawnedKeyframes = new();
    private List<GameObject> _spawnedAll = new();
    private List<GameObject> _rulerSpawned = new();
    private List<AnimationContainer> _animationsSpawned = new();
    
    private void Start()
    {
        EnableHelperText();
        
        PythonController.OnAnimationClipGenerated += OnAnimationClipGenerated;
     
        PythonController.OnSceneLoaded += EnableHelperText;
        PythonController.OnFbxLoaded += EnableHelperText;

        PythonController.OnBulkAnimationsFinished += OnBulkAnimationsFinished;
    }

    private void OnDestroy()
    {
        PythonController.OnAnimationClipGenerated -= OnAnimationClipGenerated;     
        
        PythonController.OnSceneLoaded -= EnableHelperText;
        PythonController.OnFbxLoaded -= EnableHelperText;
        
        PythonController.OnBulkAnimationsFinished -= OnBulkAnimationsFinished;
    }

    private void EnableHelperText()
    {
        helperText.SetActive(true);
        
        timelineObject.SetActive(false);
        animationResultsObject.SetActive(false);
    }

    private void DisableHelperText()
    {
        helperText.SetActive(false);

        timelineObject.SetActive(true);
        // animationResultsObject.SetActive(true);
    }

    private void EnableAnimationResultsObject()
    {
        animationResultsObject.SetActive(true);
        
        helperText.SetActive(false);
        timelineObject.SetActive(false);
    }

    private void EnableTimelineObject()
    {
        timelineObject.SetActive(true);
        
        helperText.SetActive(false);
        animationResultsObject.SetActive(false);
    }
    
    public void OnAnimationClipGenerated(AnimationClip clip)
    {
        _currentClip = clip;
        _clipLength = clip.length;
        _frameRate = clip.frameRate;
        
        Clear();
        EnableTimelineObject();
        
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

    private void Rebuild()
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
        ReapplyTogglesAfterGeneration();
    }

    private void RebuildRuler()
    {
        foreach (var go in _rulerSpawned) Destroy(go);
        _rulerSpawned.Clear();

        if (_keyframeEntries.Count == 0 || _frameRate <= 0f) return;

        float totalFrames = Mathf.CeilToInt(_clipLength * _frameRate);
        int frameInterval = Mathf.Max(1, Mathf.RoundToInt(GetNiceInterval()));

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
        _triggeredByZoom = true;
        
        pixelsPerSecond = Mathf.Clamp(pixelsPerSecond + spacerValue, pixelsPerSecondMin, pixelsPerSecondMax);

        itemWidth = Mathf.Clamp(itemWidth + itemWidthValue, itemWidthMin, itemWidthMax);

        itemHeight = Mathf.Clamp(itemHeight + itemHeightValue, itemHeightMin, itemHeightMax);
        
        if (_currentClip == null) return;
        OnAnimationClipGenerated(_currentClip);

        _triggeredByZoom = false;
    }

    private void PlaceAtTime(RectTransform rt, float time)
    {
        var pos = rt.anchoredPosition;
        pos.x = time * pixelsPerSecond;
        rt.anchoredPosition = pos;
    }

    private void ReapplyTogglesAfterGeneration()
    {
        var toggleList = PythonController.Instance.GetCurrentAnimationSkipList();
        if (toggleList == null) return;

        for (int i = 0; i < _spawnedKeyframes.Count; i++)
        {
            var kfInstance = _spawnedKeyframes[i];

            bool toggle;
            
            if (i < toggleList.Count) 
                toggle = toggleList[i];
            else break;

            kfInstance.ReapplyToggle(toggle);
        }
        
        // trigger an animation clip rebuild
        if (_triggeredByZoom) return;
        EditorApplication.delayCall += () =>
        {
            _spawnedKeyframes[0].OnClick(toggleList[0]);
        };
    }

    public void SaveAnimationButton()
    {
        StartCoroutine(SaveAnimationButtonHandler());
    }

    private IEnumerator SaveAnimationButtonHandler()
    {
        yield return new WaitForEndOfFrame();
    }

    public void ResetKeyframesButton()
    {
        PythonController.Instance.SetAnimationSkipListToValue(false);
        ReapplyTogglesAfterGeneration();
    }

    private void OnBulkAnimationsFinished(List<AnimationClip> clips)
    {
        foreach (var go in _animationsSpawned) Destroy(go);
        _animationsSpawned.Clear();

        foreach (var clip in clips)
        {
            var animationContaier = Instantiate(animationPrefab, animationResultsContentObject.transform);
            animationContaier.Init(clip);
            
            _animationsSpawned.Add(animationContaier);
        }
        
        EnableAnimationResultsObject();
    }
}
#endif
