using UnityEngine;
using Unity.Netcode;

/// <summary>
/// A simple utility script that makes a GameObject visible only to the host/server.
/// Attach this to any UI element (like a "Start Game" button) that only the host should see.
/// </summary>
public class ShowToHostOnly : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // OnNetworkSpawn is a great place for this because it's called
        // after the network connection is established.
        
        // IsHost is true only for the player who started the lobby.
        // IsServer is also true for the host, but IsHost is more specific.
        gameObject.SetActive(IsHost);
    }
} 