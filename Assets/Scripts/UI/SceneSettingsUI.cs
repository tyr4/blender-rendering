using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject helperText;
    [SerializeField] private GameObject sliderContainer;

    private List<GameObject> _settingObjects = new();

    public static event Action OnSceneUILoaded;

    private void Start()
    {
        helperText.SetActive(true);
        sliderContainer.SetActive(false);
        
        for (int i = 0; i < transform.childCount; i++)
        {
            _settingObjects.Add(transform.GetChild(i).gameObject);
            _settingObjects[i].SetActive(false);
        }

        PythonController.OnSceneLoaded += UpdateTextSceneLoaded;
    }

    private void OnDestroy()
    {
        PythonController.OnSceneLoaded -= UpdateTextSceneLoaded;
    }

    private void UpdateTextSceneLoaded()
    {
        helperText.SetActive(false);
        sliderContainer.SetActive(true);

        foreach (var obj in _settingObjects)
        {
            obj.SetActive(true);
        }
        
        OnSceneUILoaded?.Invoke();
    }
}
