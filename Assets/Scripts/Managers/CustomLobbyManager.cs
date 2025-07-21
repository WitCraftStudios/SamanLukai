using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CustomLobbyManager : MonoBehaviour
{
    public GameObject buttonsPanel;
    public GameObject inputPanel;
    public GameObject choicePanel;
    public TMP_InputField lobbyIdInputField;
    public TMP_Text hostLobbyIdText;
    public TMP_Dropdown maxPlayersDropdown; // Assign in Inspector
    public int defaultMaxPlayers = 5;

    // For demo: static lobby ID (in real use, this would be managed by a server)
    public static string CurrentLobbyId;

    void Start()
    {
        inputPanel.SetActive(false);
        hostLobbyIdText.gameObject.SetActive(false);
        // Setup dropdown options if not set in Inspector
        if (maxPlayersDropdown != null && maxPlayersDropdown.options.Count == 0)
        {
            maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData("5 Players"));
            maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData("8 Players"));
            maxPlayersDropdown.value = 0;
        }
    }

    public void OnHostLobbyClicked()
    {
        buttonsPanel.SetActive(false);
        choicePanel.SetActive(true);
    }

    public void OnHostClicked()
    {
        // Generate a random 6-digit lobby ID
        CurrentLobbyId = Random.Range(100000, 999999).ToString();
        PlayerPrefs.SetString("LobbyId", CurrentLobbyId);
        PlayerPrefs.SetInt("StartAsHost", 1);
        // Get max players from dropdown
        int maxPlayers = defaultMaxPlayers;
        if (maxPlayersDropdown != null)
        {
            if (maxPlayersDropdown.value == 0) maxPlayers = 5;
            else if (maxPlayersDropdown.value == 1) maxPlayers = 8;
        }
        PlayerPrefs.SetInt("LobbyMaxPlayers", maxPlayers);
        // Load LobbyScene using Unity's SceneManager (not Netcode)
        SceneManager.LoadScene("LobbyScene");
    }

    public void OnJoinLobbyClicked()
    {
        buttonsPanel.SetActive(false);
        inputPanel.SetActive(true);
    }

    public void OnSubmitLobbyId()
    {
        string enteredId = lobbyIdInputField.text;
        string hostLobbyId = PlayerPrefs.GetString("LobbyId", "");
        // For demo, allow joining if the entered ID matches the static or PlayerPrefs value
        if (enteredId == CurrentLobbyId || enteredId == hostLobbyId)
        {
            PlayerPrefs.SetString("LobbyId", enteredId);
            PlayerPrefs.SetInt("StartAsClient", 1);
            // Load LobbyScene using Unity's SceneManager (not Netcode)
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            // Show error (you can add a TMP_Text for error messages)
            Debug.Log("Invalid Lobby ID");
        }
    }

    public void OnExitButtonClicked()
    {
        if (inputPanel.activeInHierarchy)
        {
            inputPanel.SetActive(false);
        }
        else if(choicePanel.activeInHierarchy)
        {
            choicePanel.SetActive(false);
        }
        buttonsPanel.SetActive(true);
    }
}
