#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationClipManager : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Start()
    {
        if (!AssetDatabase.IsValidFolder("Assets/AnimationClips"))
        {
            AssetDatabase.CreateFolder("Assets", "AnimationClips");
        }
        
        PythonController.OnAnimationClipGenerated += AssignClipToGameObject;
        PythonController.OnAnimationClipWithSkipsGenerated += AssignClipToGameObject;
        AnimationContainer.OnAnimationClicked += AssignClipToGameObject;
    }

    private void OnDestroy()
    {
        PythonController.OnAnimationClipGenerated -= AssignClipToGameObject;
        PythonController.OnAnimationClipWithSkipsGenerated -= AssignClipToGameObject;
        AnimationContainer.OnAnimationClicked -= AssignClipToGameObject;
    }

    private void AssignClipToGameObject(AnimationClip clip)
    {
        // ConsoleLog.ProcessLog($"save path {_controllerSavePath}");
        
        var animator = targetObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = targetObject.AddComponent<Animator>();
        }

        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;

        if (controller == null)
        {
            string controllerPath = $"Assets/AnimationClips/{targetObject.name}.controller";

            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

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
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif