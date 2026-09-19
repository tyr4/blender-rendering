using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyframeComponent : MonoBehaviour
{
    [SerializeField] private RectTransform rt;
    [SerializeField] private Toggle toggle;
    
    [SerializeField] private Image keyframeImage;
    [SerializeField] private Image spriteImage;
    
    [SerializeField] private LayoutElement keyframeLayoutElement;
    [SerializeField] private LayoutElement spriteLayoutElement;

    [SerializeField] private Color toggledColor;
    
    private int _frameIndex;
    private string _animName;
    private Color _initialColor = Color.white;

    public static event Action<string, int, bool> OnKeyframeToggleClicked;
    
    private void Start()
    {
        keyframeImage.preserveAspect = true;
        spriteImage.preserveAspect = true;
        
        toggle.onValueChanged.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnClick);
    }

    public void Init(Sprite sprite, int frameIndex, float width, float height, string clipName)
    {
        spriteImage.sprite = sprite;
        spriteImage.enabled = sprite != null;
        
        _frameIndex = frameIndex;
        _animName = clipName;
        
        SetWidth(width);
        SetHeight(height);
    }

    private void SetWidth(float width)
    {
        var size = rt.sizeDelta;
        size.x = width;
        
        rt.sizeDelta = size;
        spriteLayoutElement.preferredWidth = width;
        keyframeLayoutElement.preferredWidth = width;
    }

    private void SetHeight(float height)
    {
        var size = rt.sizeDelta;
        size.y = height;
        
        rt.sizeDelta = size;

        spriteLayoutElement.preferredHeight = 0.85f * height;
        keyframeLayoutElement.preferredHeight = 0.15f * height;
    }

    public void OnClick(bool value)
    {
        spriteImage.color = value ? toggledColor : _initialColor;
        
        OnKeyframeToggleClicked?.Invoke(_animName, _frameIndex, value);
    }

    public void ReapplyToggle(bool value)
    {
        spriteImage.color = value ? toggledColor : _initialColor;
        toggle.SetIsOnWithoutNotify(value);
    }
}
