using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private Button loadScene;
    [SerializeField] private Button loadFbx;
    [SerializeField] private Button loadSceneFbx;
    
    [SerializeField] private Button reloadScene;
    [SerializeField] private Button reloadFbx;
    [SerializeField] private Button reloadSceneFbx;
    
    [SerializeField] private Button centerArmature;
    [SerializeField] private Button exportSelectedAnims;
    [SerializeField] private Button exportAllAnims;
    // [SerializeField] private Button ;

    private Dictionary<Button, bool> _savedState = new();
    
    private void Start()
    {
        Init();
        
        PythonController.OnSceneLoaded += OnLoadScene;
        PythonController.OnArmatureLoaded += OnLoadArmature;
        PythonController.OnSceneLoadedNoFbx += OnLoadSceneNoFbx;
        
        PythonController.OnActionBegin += OnActionBegin;
        PythonController.OnActionFinish += OnActionFinish;
    }

    private void OnDestroy()
    {
        PythonController.OnSceneLoaded -= OnLoadScene;
        PythonController.OnArmatureLoaded -= OnLoadArmature;
        PythonController.OnSceneLoadedNoFbx -= OnLoadSceneNoFbx;

        PythonController.OnActionBegin -= OnActionBegin;
        PythonController.OnActionFinish -= OnActionFinish;
    }

    private void BuildDictionary()
    {
        _savedState.Add(loadScene, loadScene.interactable);
        _savedState.Add(loadFbx, loadFbx.interactable);
        _savedState.Add(loadSceneFbx, loadSceneFbx.interactable);
        
        _savedState.Add(reloadScene, reloadScene.interactable);
        _savedState.Add(reloadFbx, reloadFbx.interactable);
        _savedState.Add(reloadSceneFbx, reloadSceneFbx.interactable);
        
        _savedState.Add(centerArmature, centerArmature.interactable);
        
        _savedState.Add(exportSelectedAnims, exportSelectedAnims.interactable);
        _savedState.Add(exportAllAnims, exportAllAnims.interactable);
    }

    private void UpdateButtonState()
    {
        _savedState[loadScene] = loadScene.interactable;
        _savedState[loadFbx] = loadFbx.interactable;
        _savedState[loadSceneFbx] = loadSceneFbx.interactable;
        
        _savedState[reloadScene] = reloadScene.interactable;
        _savedState[reloadFbx] = reloadFbx.interactable;
        _savedState[reloadSceneFbx] = reloadSceneFbx.interactable;
        
        _savedState[centerArmature] = centerArmature.interactable;
        _savedState[exportSelectedAnims] = exportSelectedAnims.interactable;
        _savedState[exportAllAnims] = exportAllAnims.interactable;
    }

    private void ToggleAll(bool value)
    {
        loadScene.interactable = value;
        loadFbx.interactable = value;
        loadSceneFbx.interactable = value;
        
        reloadScene.interactable = value;
        reloadFbx.interactable = value;
        reloadSceneFbx.interactable = value;
        
        centerArmature.interactable = value;
        
        exportSelectedAnims.interactable = value;
        exportAllAnims.interactable = value;
    }

    private void RestoreState()
    {
        loadScene.interactable = _savedState[loadScene];
        loadFbx.interactable = _savedState[loadFbx];
        loadSceneFbx.interactable = _savedState[loadSceneFbx];
        
        reloadScene.interactable = _savedState[reloadScene];
        reloadFbx.interactable = _savedState[reloadFbx];
        reloadSceneFbx.interactable = _savedState[reloadSceneFbx];
        
        centerArmature.interactable = _savedState[centerArmature];
        exportSelectedAnims.interactable = _savedState[exportSelectedAnims];
        exportAllAnims.interactable = _savedState[exportAllAnims];
    }

    private void OnActionBegin()
    {
        UpdateButtonState();
        ToggleAll(false);
    }

    private void OnActionFinish()
    {
        RestoreState();
    }

    private void Init()
    {
        ToggleAll(false);
        BuildDictionary();
        
        loadScene.interactable = true;
        loadSceneFbx.interactable = true;
        
        UpdateButtonState();
    }
    
    private void OnLoadScene()
    {
        ToggleAll(false);
        
        loadSceneFbx.interactable = true;
        loadScene.interactable = true;
        loadFbx.interactable = true;
        reloadScene.interactable = true;
        
        UpdateButtonState();
    }

    private void OnLoadArmature(List<string> _)
    {
        OnLoadScene();

        Debug.Log("AM INTRAT IN LOADFBX");
        reloadFbx.interactable = true;
        reloadSceneFbx.interactable = true;
        centerArmature.interactable = true;
        exportSelectedAnims.interactable = true;
        exportAllAnims.interactable = true;

        Debug.Log(centerArmature.interactable + " " + " center armature");
        UpdateButtonState();
    }

    private void OnLoadSceneNoFbx()
    {
        reloadFbx.interactable = false;
        reloadSceneFbx.interactable = false;
    }
    
    /* TODO:
     - thread safe stdin/stdout
     - reenable buttons in logerror (to not cause a softlock)
     - (important) update sliders with scene values on load
     - fix onloadscenenofbx
     - make settings toggles work
     - settings edit section
     - console section (console manager with custom function to send output there)
       -> color coded would be nice
    */
}
