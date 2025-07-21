using Unity.Netcode;
using UnityEngine;

public class PlayerRoleManager : NetworkBehaviour
{
    public NetworkVariable<PlayerRole> role = new NetworkVariable<PlayerRole>(PlayerRole.Seeker);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("My role is: " + role.Value);
        }
    }
}
