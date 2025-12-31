using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class EdgegapLauncher : MonoBehaviour
{
    public static EdgegapLauncher Instance { get; private set; }

    [Header("Configuration Source")]
    [Tooltip("Optional: Drag your EdgegapConfig asset here. If null, uses fields below.")]
    [SerializeField] private EdgegapConfig configAsset;

    [Header("Fallback Settings (Used if Config Asset is missing)")]
    [SerializeField] private string apiUrl = "https://api.edgegap.com/v1";
    [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";
    [SerializeField] private string appName = "vidar-game";
    [SerializeField] private string appVersion = "v1";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Try to load from Resources if not assigned
        if (configAsset == null)
        {
            configAsset = Resources.Load<EdgegapConfig>("Config/EdgegapConfig");
        }

        if (configAsset == null)
        {
            Debug.LogWarning("[EdgegapLauncher] Config Asset not found. Using Inspector Fallback settings.");
        }
    }

    public void DeployServer(string clientIp, Action<string, int> onSuccess, Action<string> onFailure)
    {
        StartCoroutine(DeployRoutine(clientIp, onSuccess, onFailure));
    }

    private IEnumerator DeployRoutine(string clientIp, Action<string, int> onSuccess, Action<string> onFailure)
    {
        // Determine values to use
        string targetUrl = configAsset ? configAsset.ApiUrl : apiUrl;
        string targetKey = configAsset ? configAsset.ApiKey : apiKey;
        string targetApp = configAsset ? configAsset.AppName : appName;
        string targetVer = configAsset ? configAsset.AppVersion : appVersion;

        string url = $"{targetUrl}/deploy";
        string jsonBody = $"{{\"app_name\": \"{targetApp}\", \"version_name\": \"{targetVer}\", \"ip_list\": [\"{clientIp}\"]}}";
        
        Debug.Log($"[EdgegapLauncher] Deploying: {jsonBody}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            string authHeader = targetKey.StartsWith("token") ? targetKey : "token " + targetKey;
            request.SetRequestHeader("Authorization", authHeader);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[EdgegapLauncher] Deploy Failed: {request.error} : {request.downloadHandler.text}");
                onFailure?.Invoke(request.error);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log($"[EdgegapLauncher] Response: {responseText}");

            DeployResponse response = JsonUtility.FromJson<DeployResponse>(responseText);
            if (response != null && !string.IsNullOrEmpty(response.request_id))
            {
                StartCoroutine(PollStatusRoutine(response.request_id, targetUrl, targetKey, onSuccess, onFailure));
            }
            else
            {
                onFailure?.Invoke("Could not parse request_id");
            }
        }
    }

    private IEnumerator PollStatusRoutine(string requestId, string urlBase, string key, Action<string, int> onSuccess, Action<string> onFailure)
    {
        string url = $"{urlBase}/status/{requestId}";
        float timeout = 60f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                string authHeader = key.StartsWith("token") ? key : "token " + key;
                request.SetRequestHeader("Authorization", authHeader);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    StatusResponse status = JsonUtility.FromJson<StatusResponse>(json);

                    if (status.running)
                    {
                        Debug.Log($"[EdgegapLauncher] Server Running! IP: {status.public_ip}");
                        int port = ExtractPort(json);
                        if (port > 0)
                        {
                            onSuccess?.Invoke(status.public_ip, port);
                            yield break;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(2f);
            elapsed += 2f;
        }
        onFailure?.Invoke("Timeout waiting for server");
    }

    private int ExtractPort(string json)
    {
        string search = "\"external\":";
        int idx = json.IndexOf(search);
        if (idx != -1)
        {
            int start = idx + search.Length;
            int end = json.IndexOf(",", start);
            if (end == -1) end = json.IndexOf("}", start);
            string portStr = json.Substring(start, end - start).Trim();
            if (int.TryParse(portStr, out int port)) return port;
        }
        return 0;
    }

    [System.Serializable] class DeployResponse { public string request_id; }
    [System.Serializable] class StatusResponse { public bool running; public string public_ip; }
}

