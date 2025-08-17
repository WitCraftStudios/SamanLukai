using Unity.Netcode;
using UnityEngine;

public class SeekerInteraction : NetworkBehaviour
{
    public GameTimer gameTimer;
    public ulong hiderClientId;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Interactable"))
        {
            Debug.Log($"Seeker {NetworkManager.Singleton.LocalClientId} reached the object!");
            gameTimer.NotifyObjectFoundServerRpc(NetworkManager.Singleton.LocalClientId, hiderClientId);
        }
    }
}
