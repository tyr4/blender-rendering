using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ArmatureUI : MonoBehaviour
{
    [SerializeField] private GameObject objPrefab;
    [SerializeField] private GameObject helperText;
    [SerializeField] private GameObject sliderContainer;

    private readonly List<ArmatureUIObject> _activeObjects = new();
        
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
            ArmatureUIObject armatureObj;
            
            if (i < _activeObjects.Count)
            {
                armatureObj = _activeObjects[i];
                obj = _activeObjects[i].gameObject;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(objPrefab, transform);
                armatureObj = obj.GetComponent<ArmatureUIObject>();
                _activeObjects.Add(armatureObj);
            }
            
            armatureObj.Init(anim, i);
            var text = obj.GetComponentInChildren<TMP_Text>();
            text.text = anim;
        }

        // disable extra objects
        for (int i = animations.Count; i < _activeObjects.Count; i++)
        {
            _activeObjects[i].gameObject.SetActive(false);
        }
        
        ConsoleLog.RenderLog($"Found {animations.Count} animations: " + string.Join(", ", animations));
    }
    
    private void UpdateTextOnlySceneLoaded()
    {
        foreach (var obj in _activeObjects)
        {
            obj.gameObject.SetActive(false);
        }
        
        helperText.SetActive(true);
    }
}
