using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LobbyNetworkStarter : MonoBehaviour
{
    public TMP_Text hostLobbyIdText; // Optional: assign in Inspector to display lobby ID for host
    public TMP_Text maxPlayersText;  // Assign in Inspector

    void Start()
    {
        // Check if we should start as host or client after scene load
        if (PlayerPrefs.GetInt("StartAsHost", 0) == 1)
        {
            PlayerPrefs.SetInt("StartAsHost", 0);
            string lobbyId = PlayerPrefs.GetString("LobbyId", "");
            if (hostLobbyIdText != null && !string.IsNullOrEmpty(lobbyId))
            {
                hostLobbyIdText.text = "Lobby ID: " + lobbyId;
                hostLobbyIdText.gameObject.SetActive(true);
            }
            NetworkManager.Singleton.StartHost();
        }
        else if (PlayerPrefs.GetInt("StartAsClient", 0) == 1)
        {
            PlayerPrefs.SetInt("StartAsClient", 0);
            NetworkManager.Singleton.StartClient();
        }

        // Display max players if the text field is assigned
        if (maxPlayersText != null)
        {
            int maxPlayers = PlayerPrefs.GetInt("LobbyMaxPlayers", 5);
            maxPlayersText.text = "Max Players: " + maxPlayers;
            maxPlayersText.gameObject.SetActive(true);
        }
    }
} 