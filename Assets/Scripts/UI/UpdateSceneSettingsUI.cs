using System;
using System.Collections.Generic;
using UnityEngine;

public class UpdateSceneSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject helperText;

    private List<GameObject> _settingObjects = new();

    public static event Action OnSceneUILoaded;

    private void Start()
    {
        helperText.SetActive(true);
        
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

        foreach (var obj in _settingObjects)
        {
            obj.SetActive(true);
        }
        
        OnSceneUILoaded?.Invoke();
    }
}
