using UnityEngine;

public class UnityLogger : InGameMCP.Core.ILogger
{
    public void LogInfo(string message)
    {
        Debug.Log(message);
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }
}