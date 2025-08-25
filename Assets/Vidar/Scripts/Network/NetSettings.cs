using UnityEngine;

[CreateAssetMenu(fileName = "NetSettings", menuName = "Vidar/Net Settings")]
public class NetSettings : ScriptableObject
{
    public string serverAddress = "127.0.0.1";
    [Range(1, 65535)] public int port = 7979;
    public string serverListenAddress = "0.0.0.0";

    [Header("Transport options")]
    public bool useWebSockets = false;
}
