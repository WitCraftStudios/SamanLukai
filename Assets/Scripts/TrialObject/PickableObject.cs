using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PickableObject : NetworkBehaviour
{
    private Rigidbody rb;

    // Sync whether the object is held or free
    public NetworkVariable<bool> isBeingHeld = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // When the object spawns, make sure physics state matches
        UpdatePhysics(isBeingHeld.Value);

        // Subscribe to changes
        isBeingHeld.OnValueChanged += (oldVal, newVal) =>
        {
            UpdatePhysics(newVal);
        };
    }

    private void UpdatePhysics(bool held)
    {
        rb.useGravity = !held;
        rb.isKinematic = held;
    }

    // Called by the server to update held state
    [ServerRpc(RequireOwnership = false)]
    public void SetHeldServerRpc(bool held)
    {
        isBeingHeld.Value = held;
    }
}
