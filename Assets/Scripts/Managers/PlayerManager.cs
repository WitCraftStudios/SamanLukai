using Unity.Netcode;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log("PlayerManager OnEnable called");
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("NetworkManager.Singleton is not null, registering callback");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            Debug.Log("NetworkManager.Singleton is null in OnEnable");
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"OnClientConnected called for clientId: {clientId}, localClientId: {NetworkManager.Singleton.LocalClientId}");
        // Only do this for the local player
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Not the local player, skipping script enabling.");
            return;
        }

        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        Debug.Log($"PlayerObject found: {(playerObject != null)}");
        if (playerObject == null) return;

        // Enable relevant scripts on the player
        var playerMovement = playerObject.GetComponent<PlayerMovement>();
        Debug.Log($"PlayerMovement found: {(playerMovement != null)}");
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            Debug.Log("PlayerMovement enabled.");
        }

        var playerInteraction = playerObject.GetComponent<PlayerInteraction>();
        Debug.Log($"PlayerInteraction found: {(playerInteraction != null)}");
        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;
            Debug.Log("PlayerInteraction enabled.");
        }

        var animationController = playerObject.GetComponent<PlayerAnimationController>();
        Debug.Log($"PlayerAnimationController found: {(animationController != null)}");
        if (animationController != null)
        {
            animationController.enabled = true;
            Debug.Log("PlayerAnimationController enabled.");
        }

        // Enable script on FollowTarget child (if any)
        var followTarget = playerObject.transform.Find("FollowTarget");
        Debug.Log($"FollowTarget found: {(followTarget != null)}");
        if (followTarget != null)
        {
            // Replace 'YourFollowTargetScript' with the actual script name if known
            var followTargetScript = followTarget.GetComponent<MonoBehaviour>();
            Debug.Log($"FollowTargetScript found: {(followTargetScript != null)}");
            if (followTargetScript != null)
            {
                followTargetScript.enabled = true;
                Debug.Log("FollowTargetScript enabled.");
            }
        }
        // Add more scripts to enable as needed
    }
}
