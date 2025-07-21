using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public string gameSceneName = "GameScene"; // Match this with your actual game scene name

    public void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }
}
