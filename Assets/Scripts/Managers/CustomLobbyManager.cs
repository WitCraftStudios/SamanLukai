using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CustomLobbyManager : MonoBehaviour
{
    public GameObject buttonsPanel;
    public GameObject inputPanel;
    public TMP_InputField lobbyIdInputField;
    public TMP_Text hostLobbyIdText;

    // For demo: static lobby ID (in real use, this would be managed by a server)
    public static string CurrentLobbyId;

    void Start()
    {
        inputPanel.SetActive(false);
        hostLobbyIdText.gameObject.SetActive(false);
    }

    public void OnHostLobbyClicked()
    {
        // Generate a random 6-digit lobby ID
        CurrentLobbyId = Random.Range(100000, 999999).ToString();
        hostLobbyIdText.text = "Lobby ID: " + CurrentLobbyId;
        hostLobbyIdText.gameObject.SetActive(true);

        // Start host
        NetworkManager.Singleton.StartHost();

        // Load LobbyScene via Netcode
        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }

    public void OnJoinLobbyClicked()
    {
        buttonsPanel.SetActive(false);
        inputPanel.SetActive(true);
    }

    public void OnSubmitLobbyId()
    {
        string enteredId = lobbyIdInputField.text;
        if (enteredId == CurrentLobbyId)
        {
            // Start client
            NetworkManager.Singleton.StartClient();

            // Load LobbyScene via Netcode
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }
        else
        {
            // Show error (you can add a TMP_Text for error messages)
            Debug.Log("Invalid Lobby ID");
        }
    }
}
