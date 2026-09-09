using UnityEngine;

public static class ConsoleLog
{
    public static void PythonLog(string message) => Debug.Log($"[PYTHON]{message}"); 
    public static void ProcessLog(string message) => Debug.Log($"[PROCESS]{message}"); 
    public static void RenderLog(string message) => Debug.Log($"[RENDER]{message}"); 
    public static void OnActionBeginLog(string message) => Debug.Log($"[ONACTIONBEGIN]{message}"); 
    public static void OnActionFinishLog(string message) => Debug.Log($"[ONACTIONFINISH]{message}");
}
