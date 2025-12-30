using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private string nextScene = "Menu-Global";
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (AuthenticationManager.Instance != null)
        {
            statusText.text = $"Welcome, Player {AuthenticationManager.Instance.PlayerId.Substring(0, 5)}...";
        }
        else
        {
            statusText.text = "Error: Auth Service Missing";
        }
    }

    public void OnPlayButtonClicked()
    {
        // Here we could add extra checks (maintenance mode, version check, etc.)
        SceneManager.LoadScene(nextScene);
    }
}
