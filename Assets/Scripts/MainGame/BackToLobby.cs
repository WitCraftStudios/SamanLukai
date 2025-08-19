using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToLobby : NetworkBehaviour
{
    [SerializeField] private Button backToLobbyButton;

    private void Start()
    {
        if (IsOwner) // only show button for local player
        {
            backToLobbyButton.gameObject.SetActive(true);
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
        }
        else
        {
            backToLobbyButton.gameObject.SetActive(false); // hide on remote players
        }
    }

    private void OnBackToLobbyClicked()
    {
        if (IsHost)
        {
            // Host can directly change the scene
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }
        else
        {
            // Client must ask the server (host) to load the lobby
            RequestLobbyReturnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLobbyReturnServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only the host changes scenes
        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }
}
