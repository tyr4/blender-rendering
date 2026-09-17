using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnimationPreviewScrollBar : MonoBehaviour, IScrollHandler
{
    [SerializeField] private AnimationPreviewUI previewUI;
    [SerializeField] private float deltaSpacerMultiplier = 15f;
    [SerializeField] private float deltaWidthMultiplier = 0.1f;
    [SerializeField] private float deltaHeightMultiplier = 0.1f;

    
    public void OnScroll(PointerEventData eventData)
    {
        OnMouseWheel(eventData.scrollDelta.y);
    
        eventData.Use();
    }

    private void OnMouseWheel(float delta)
    {
        var spacerValue = delta * deltaSpacerMultiplier;
        var widthValue = delta * deltaWidthMultiplier;
        var heightValue = delta * deltaHeightMultiplier;
        
        previewUI.ChangeSizeConstraints(spacerValue, widthValue, heightValue);
        Debug.Log($"mwheel: {delta}");
    }
}
