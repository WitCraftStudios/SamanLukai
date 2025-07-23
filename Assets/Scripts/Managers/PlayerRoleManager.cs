using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRoleManager : NetworkBehaviour
{
    public NetworkVariable<PlayerRole> role = new NetworkVariable<PlayerRole>(PlayerRole.Seeker);

    public override void OnNetworkSpawn()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "GameScene")
        {
            if (IsOwner)
            {
                Debug.Log("My role is: " + role.Value);
            }
        }
    }
}
