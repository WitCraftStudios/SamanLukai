using UnityEngine;
using TMPro;

public class LobbyIdDisplay : MonoBehaviour
{
    public TMP_Text lobbyIdText;

    void Start()
    {
        // Access the static variable from CustomLobbyManager
        lobbyIdText.text = "Lobby ID: " + CustomLobbyManager.CurrentLobbyId;
    }
}
