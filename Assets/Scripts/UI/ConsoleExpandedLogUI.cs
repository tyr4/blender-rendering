using TMPro;
using UnityEngine;

public class ConsoleExpandedLogUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textObject;

    private void Start()
    {
        textObject.SetText("Click on a Console Log to view its details!");
        
        ConsoleTextButton.OnConsoleButtonClicked += UpdateExpandedLogText;
    }

    private void OnDestroy()
    {
        ConsoleTextButton.OnConsoleButtonClicked -= UpdateExpandedLogText;
    }
    
    private void UpdateExpandedLogText(string msg, Color color)
    {
        textObject.SetText(msg);
        textObject.color = color;
    }
}
