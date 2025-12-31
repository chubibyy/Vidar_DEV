using UnityEngine;

[CreateAssetMenu(fileName = "EdgegapConfig", menuName = "Vidar/Config/EdgegapConfig")]
public class EdgegapConfig : ScriptableObject
{
    [Header("Edgegap API Settings")]
    public string ApiUrl = "https://api.edgegap.com/v1";
    public string ApiKey = "YOUR_API_KEY_HERE";
    
    [Header("Application Details")]
    public string AppName = "vidar-game";
    public string AppVersion = "v1";
}
