using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

public class SetSettingsValuesUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button spritesheetButton;
    [SerializeField] private Button renderTempPathButton;
    [SerializeField] private TMP_InputField renderTempNameField;
    
    [Header("Output Text")]
    [SerializeField] private TextMeshProUGUI spritesheetPathText;
    [SerializeField] private TextMeshProUGUI renderTempPathText;
    
    [Header("Variables")]
    [SerializeField] private int outputPathLengthLimit = 25;
    
    private StringBuilder _stringBuilder = new();
    private string _lastValidFilename = "anim_";
    
    private void Start()
    {
        spritesheetButton.onClick.AddListener(SpritesheetButton);
        renderTempPathButton.onClick.AddListener(RenderTempOutputPathButton);

        renderTempNameField.onValueChanged.AddListener(OnInputFieldChanged);
        
        LoadSettingsData();
    }

    private void OnDestroy()
    {        
        spritesheetButton.onClick.RemoveListener(SpritesheetButton);
        renderTempPathButton.onClick.RemoveListener(RenderTempOutputPathButton);
        
        renderTempNameField.onValueChanged.RemoveListener(OnInputFieldChanged);
    }

    private void LoadSettingsData()
    {
        var settings = PythonController.Instance.settings;

        if (!string.IsNullOrEmpty(settings.spritesheet_output_path))
            spritesheetPathText.text = ShortenStringOutput(settings.spritesheet_output_path);
        
        if (!string.IsNullOrEmpty(settings.render_temp_output_path))
            renderTempPathText.text = ShortenStringOutput(settings.render_temp_output_path);

        if (!string.IsNullOrEmpty(settings.render_temp_output_name))
        {
            renderTempNameField.text = settings.render_temp_output_name;
            _lastValidFilename = settings.render_temp_output_name;            
        }
    }

    private void SpritesheetButton()
    {
        StartCoroutine(ChooseSpritesheet());
    }

    private void RenderTempOutputPathButton()
    {
        StartCoroutine(ChooseRenderTempPath());
    }

    private IEnumerator ChooseSpritesheet()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders,
            false,
            null,
            null,
            "Select Folder",
            "Load"
            );

        if (!FileBrowser.Success) yield break;
        
        var result = FileBrowser.Result[0];
        
        spritesheetPathText.text = ShortenStringOutput(result);
        
        PythonController.Instance.settings.spritesheet_output_path = result;
        PythonController.Instance.SaveSettings();
    }

    private IEnumerator ChooseRenderTempPath()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders,
            false,
            null,
            null,
            "Select Folder",
            "Load"
        );

        if (!FileBrowser.Success) yield break;

        var result = FileBrowser.Result[0];
        
        renderTempPathText.text = ShortenStringOutput(result);
        
        PythonController.Instance.settings.render_temp_output_path = result;
        PythonController.Instance.SaveSettings();
    }
    
    private void OnInputFieldChanged(string value)
    {
        var sanitized = SanitizeFileName(value);
        
        renderTempNameField.SetTextWithoutNotify(sanitized);
        
        PythonController.Instance.settings.render_temp_output_name = sanitized;
        PythonController.Instance.SaveSettings();
    }

    private string SanitizeFileName(string input, int maxLength = 20)
    {
        if (string.IsNullOrEmpty(input)) return _lastValidFilename;

        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            sb.Append(invalidChars.Contains(c) ? '_' : c);
        }

        var result = sb.ToString();
        var formatted = result.Length > maxLength ? result[..maxLength] : result;
        _lastValidFilename = formatted;
        
        return formatted;
    }

    private string ShortenStringOutput(string input)
    {
        if (input.Length < outputPathLengthLimit) return input;

        var keep = outputPathLengthLimit - 3; // reserve space for "..."
        var headLength = keep / 2;
        var tailLength = keep - headLength;

        _stringBuilder.Clear();
        _stringBuilder.Append(input, 0, headLength);
        _stringBuilder.Append("...");
        _stringBuilder.Append(input, input.Length - tailLength, tailLength);
        _stringBuilder.Replace('\\', '/');

        return _stringBuilder.ToString();
    }
}