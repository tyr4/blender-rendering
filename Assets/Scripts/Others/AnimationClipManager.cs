#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationClipManager : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private string _controllerSavePath;
    
    private void Start()
    {
        _controllerSavePath = Application.dataPath + "/AnimationClips/";
        PythonController.OnAnimationClipGenerated += AssignClipToGameObject;  
    }

    private void OnDestroy()
    {
        PythonController.OnAnimationClipGenerated -= AssignClipToGameObject;
    }

    private void AssignClipToGameObject(AnimationClip clip)
    {
        ConsoleLog.ProcessLog("am intrat in assign clip to gameobject");
        ConsoleLog.ProcessLog($"save path {_controllerSavePath}");
        
        var animator = targetObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = targetObject.AddComponent<Animator>();
        }

        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(_controllerSavePath);
            animator.runtimeAnimatorController = controller;
        }
        
        var stateMachine = controller.layers[0].stateMachine;
        var existing = System.Array.Find(stateMachine.states, s => s.state.name == clip.name);

        AnimatorState targetState;
        
        if (existing.state != null)
        {
            existing.state.motion = clip; // update existing state to point at the new clip
            targetState = existing.state;
        }
        else
        {
            targetState = stateMachine.AddState(clip.name);
            targetState.motion = clip;
        }

        stateMachine.defaultState = targetState;
        ConsoleLog.ProcessLog($"set default state to {stateMachine.defaultState.name} (intended: {targetState.name})");
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }
}
#endif