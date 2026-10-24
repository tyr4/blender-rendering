#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AnimationContainer : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    
    private AnimationClip _currentClip;
    public static event Action<AnimationClip> OnAnimationClicked;

    private void Start()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }

    public void Init(AnimationClip clip)
    {
        _currentClip = clip;
        
        text.text = clip.name + ".anim";
        image.sprite = GetFirstSpriteFromClip();
        image.preserveAspect = true;
    }

    private Sprite GetFirstSpriteFromClip()
    {
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(_currentClip);

        foreach (var binding in bindings)
        {
            if (binding.type == typeof(SpriteRenderer) || binding.type == typeof(Image))
            {
                var keyframes = AnimationUtility.GetObjectReferenceCurve(_currentClip, binding);
                if (keyframes.Length > 0)
                    return keyframes[0].value as Sprite;
            }
        }

        return null;
    }

    private void OnButtonClick()
    {
        OnAnimationClicked?.Invoke(_currentClip);
    }
}
#endif
