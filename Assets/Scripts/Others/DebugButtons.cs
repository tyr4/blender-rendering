using UnityEngine;

public class DebugButtons : MonoBehaviour
{
    [SerializeField] private AnimationClip walkAnim;
    [SerializeField] private AnimationPreviewUI previewUI;
    
    public void InvokeAnimationClipGenerated()
    {
        previewUI.OnAnimationClipGenerated(walkAnim);
    }
}
