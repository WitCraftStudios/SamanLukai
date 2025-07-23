using UnityEngine;
using Unity.Netcode;

public class LoadAssignManager : MonoBehaviour
{
    void Start()
    {
        // Wait a moment to ensure players are spawned
        Invoke(nameof(EnablePlayerRoleScript), 1f); // delay just in case
    }

    void EnablePlayerRoleScript()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;

            if (playerObject != null)
            {
                var roleManager = playerObject.GetComponent<PlayerRoleManager>();
                if (roleManager != null && !roleManager.enabled)
                {
                    roleManager.enabled = true;
                    Debug.Log("Enabled PlayerRoleManager for player: " + client.ClientId);
                }
            }
        }
    }
}
