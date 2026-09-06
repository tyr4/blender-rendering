using TMPro;
using UnityEngine;

public class ConsoleExpandedLogUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textObject;

    private void Start()
    {
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
