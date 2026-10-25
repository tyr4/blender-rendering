using UnityEngine;
using UnityEngine.UI;

public class RenderAsMeshToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleClick);
    }
    
    private void OnToggleClick(bool value)
    {
        PythonController.Instance.settings.render_as_mesh = value;
    }
}
