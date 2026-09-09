using System;
using UnityEngine;
using UnityEngine.UI;

public class ArmatureUIObject : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Button playButton;
    
    private string _animName;
    private int _animIndex;
    

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnToggleClick);
        playButton.onClick.AddListener(OnPlayButtonClick);

        PythonController.OnActionBegin += DisablePlayButton;
        PythonController.OnActionFinish += EnablePlayButton;
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleClick);
        playButton.onClick.RemoveListener(OnPlayButtonClick);
        
        PythonController.OnActionBegin -= DisablePlayButton;
        PythonController.OnActionFinish -= EnablePlayButton;
    }

    public void Init(string animName, int animIndex)
    {
        _animName = animName;
        _animIndex = animIndex;
    }

    private void OnToggleClick(bool value)
    {
        PythonController.Instance.SetAnimationToggle(_animName, value);
    }

    private void OnPlayButtonClick()
    {
        PythonController.Instance.SetRequestedRenderAnimation(_animIndex);
    }

    private void DisablePlayButton(string _)
    {
        playButton.interactable = false;
    }

    private void EnablePlayButton(string _)
    {
        playButton.interactable = true;
    }
}
