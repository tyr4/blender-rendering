using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class UpdateArmatureUI : MonoBehaviour
{
    [SerializeField] private GameObject objPrefab;
    [SerializeField] private GameObject helperText;
    [SerializeField] private GameObject sliderContainer;

    private readonly List<GameObject> _activeObjects = new();
        
    private void Start()
    {
        helperText.SetActive(true);
        sliderContainer.SetActive(false);

        PythonController.OnArmatureLoaded += UpdateTextArmatureLoaded;
        PythonController.OnSceneLoaded += UpdateTextOnlySceneLoaded;
    }

    private void OnDestroy()
    {
        PythonController.OnArmatureLoaded -= UpdateTextArmatureLoaded;
        PythonController.OnSceneLoaded -= UpdateTextOnlySceneLoaded;
    }

    private void UpdateTextArmatureLoaded(List<string> animations)
    {
        helperText.SetActive(false);
        sliderContainer.SetActive(true);
        
        for (int i = 0; i < animations.Count; i++)
        {
            var anim = animations[i];
            GameObject obj;

            if (i < _activeObjects.Count)
            {
                obj = _activeObjects[i];
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(objPrefab, transform);
                _activeObjects.Add(obj);            
            }
            
            var text = obj.GetComponentInChildren<TMP_Text>();
            text.text = anim;
        }

        // disable extra objects
        for (int i = animations.Count; i < _activeObjects.Count; i++)
        {
            _activeObjects[i].SetActive(false);
        }
        
        ConsoleLog.RenderLog($"Found {animations.Count} animations: " + string.Join(", ", animations));
    }
    
    private void UpdateTextOnlySceneLoaded()
    {
        foreach (var obj in _activeObjects)
        {
            obj.SetActive(false);
        }
        
        helperText.SetActive(true);
    }
}
