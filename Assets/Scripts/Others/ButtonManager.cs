using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private Button loadDefaultScene;
    [SerializeField] private Button loadScene;
    [SerializeField] private Button loadFbx;
    [SerializeField] private Button loadSceneFbx;
    
    [SerializeField] private Button reloadScene;
    [SerializeField] private Button reloadFbx;
    [SerializeField] private Button reloadSceneFbx;
    
    [SerializeField] private Button centerArmature;
    [SerializeField] private Button exportSelectedAnims;
    [SerializeField] private Button exportAllAnims;
    
    [SerializeField] private Button saveAnimation;
    [SerializeField] private Button resetKeyframes;

    private Dictionary<Button, bool> _savedState = new();
    private List<Button> _buttons = new();
    private bool _hasBegunAction;

    private void Start()
    {
        Init();
        
        PythonController.OnSceneLoaded += OnLoadScene;
        // PythonController.OnArmatureLoaded += OnLoadArmature;
        PythonController.OnFbxLoaded += OnLoadFbx;
        PythonController.OnSceneLoadedNoFbx += OnLoadSceneNoFbx;
        
        PythonController.OnActionBegin += OnActionBegin;
        PythonController.OnActionFinish += OnActionFinish;
    }

    private void AddButtonsToList()
    {
        _buttons = new List<Button>
        {
            loadDefaultScene, loadScene, loadFbx, loadSceneFbx,
            
            reloadScene, reloadFbx, reloadSceneFbx,
            
            centerArmature, exportSelectedAnims, exportAllAnims,
            
            saveAnimation, resetKeyframes
        };
    }

    private void OnDestroy()
    {
        PythonController.OnSceneLoaded -= OnLoadScene;
        // PythonController.OnArmatureLoaded -= OnLoadArmature;
        PythonController.OnSceneLoadedNoFbx -= OnLoadSceneNoFbx;
        PythonController.OnFbxLoaded -= OnLoadFbx;

        PythonController.OnActionBegin -= OnActionBegin;
        PythonController.OnActionFinish -= OnActionFinish;
    }

    private void BuildDictionary()
    {
        _savedState.Clear();
        
        foreach (var button in _buttons)
            _savedState.Add(button, button.interactable);
    }

    private void UpdateButtonState()
    {
        foreach (var button in _buttons)
            _savedState[button] = button.interactable;
    }

    private void ToggleAll(bool value)
    {
        foreach (var button in _buttons)
            button.interactable = value;
    }

    private void RestoreState()
    {
        foreach (var button in _buttons)
            button.interactable = _savedState[button];
    }

    private void OnActionBegin(string actionName)
    {
        ConsoleLog.OnActionBeginLog($"Started action: {actionName}");
        
        if (_hasBegunAction) return;
        _hasBegunAction = true;

        UpdateButtonState();
        ToggleAll(false);
    }

    private void OnActionFinish(string actionName)
    {
        ConsoleLog.OnActionFinishLog($"Finished action: {actionName}");
        
        RestoreState();
        _hasBegunAction = false;
    }

    private void Init()
    {
        AddButtonsToList(); // very important to do this first
        
        ToggleAll(false);
        BuildDictionary();

        loadDefaultScene.interactable = true;
        loadScene.interactable = true;
        loadSceneFbx.interactable = true;
        
        UpdateButtonState();
    }
    
    private void OnLoadScene()
    {
        ToggleAll(false);

        loadDefaultScene.interactable = true;
        loadSceneFbx.interactable = true;
        loadScene.interactable = true;
        loadFbx.interactable = true;
        reloadScene.interactable = true;
        
        UpdateButtonState();
    }

    private void OnLoadSceneNoFbx()
    {
        // if (_hasLoadedFbx) return;
        OnLoadScene();
        
        exportSelectedAnims.interactable = true;
        exportAllAnims.interactable = true;
        centerArmature.interactable = true;
        
        UpdateButtonState();
    }

    private void OnLoadFbx()
    {
        // _hasLoadedFbx = true;

        reloadFbx.interactable = true;
        reloadSceneFbx.interactable = true;
        centerArmature.interactable = true;
        exportSelectedAnims.interactable = true;
        exportAllAnims.interactable = true;
        saveAnimation.interactable = true;
        resetKeyframes.interactable = true;
        
        UpdateButtonState();
    }
}
