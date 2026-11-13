using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BatchModeExportHandler : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private BatchModeAnimationButton animationButtonPrefab;
    [SerializeField] private RectTransform animationButtonsContainer;
    [SerializeField] private ToggleGroup toggleGroup;
    
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button exportButton;

    public static event Action<UserSettings> OnBatchModelSelected;
    
    private List<UserSettings> _currentModels;
    private List<BatchModeAnimationButton> _activeButtons = new();
    private int _currentlySelected;

    private void Awake()
    {
        ToggleCanvasGroup(false);
    }
    
    private void Start()
    {
        // export button gets handled by the pythoncontroller
        previousButton.onClick.AddListener(PreviousButtonOnClick);
        nextButton.onClick.AddListener(NextButtonOnClick);
        
        PythonController.OnBatchModeBegin += HandleBatchBegin;
        PythonController.OnBatchModeFinish += HandleBatchFinish;
        BatchModeAnimationButton.OnBatchAnimationButtonClicked += OnBatchAnimationButtonClicked;
        
        previousButton.onClick.AddListener(PythonController.Instance.ExportBatchButton);
    }

    private void OnDestroy()
    {
        previousButton.onClick.RemoveListener(PreviousButtonOnClick);
        nextButton.onClick.RemoveListener(NextButtonOnClick);
        
        PythonController.OnBatchModeBegin -= HandleBatchBegin;
        PythonController.OnBatchModeFinish -= HandleBatchFinish;
        BatchModeAnimationButton.OnBatchAnimationButtonClicked -= OnBatchAnimationButtonClicked;
    }

    private void HandleBatchBegin(List<UserSettings> modelSettings)
    {
        ToggleCanvasGroup(true);
        
        _currentModels = modelSettings;
        
        SpawnAnimationButtons();
        TriggerModelLoad();
    }

    private void TriggerModelLoad()
    {
        var currentModel = _currentModels[_currentlySelected];
        _activeButtons[_currentlySelected].SetToggleActive();
       
        OnBatchModelSelected?.Invoke(currentModel);
    }
    
    private void HandleBatchFinish()
    {
        ToggleCanvasGroup(false);
    }

    private void ToggleCanvasGroup(bool value)
    {
        canvasGroup.alpha = value? 1 : 0;
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }

    private void NextButtonOnClick()
    {
        if (_currentlySelected + 1 < _currentModels.Count)
        {
            _currentlySelected++;
            TriggerModelLoad();            
        }
    }

    private void PreviousButtonOnClick()
    {
        if (_currentlySelected > 0)
        {
            _currentlySelected--;
            TriggerModelLoad();
        }
    }

    private void SpawnAnimationButtons()
    {
        foreach (var obj in _activeButtons) Destroy(obj.gameObject);
        _activeButtons.Clear();

        for (int index = 0; index < _currentModels.Count; index++)
        {
            var obj = Instantiate(animationButtonPrefab, animationButtonsContainer);
            var modelName = Path.GetFileNameWithoutExtension(_currentModels[index].fbx_path);
            
            obj.Init(index, toggleGroup, modelName);
            
            _activeButtons.Add(obj);
        }
    }

    private void OnBatchAnimationButtonClicked(int index)
    {
        _currentlySelected = index;
        
        TriggerModelLoad();
    }
}
