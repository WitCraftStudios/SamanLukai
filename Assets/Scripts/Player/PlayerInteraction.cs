using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    public Transform holdPoint; // Must have a NetworkObject on this transform

    private InputSystem_Actions inputActions;
    private PickableObject nearbyObject = null;
    private PickableObject heldObject = null;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        if (!IsOwner) return; // only local player handles input

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            if (heldObject == null && nearbyObject != null)
            {
                TryPickUpObjectServerRpc(nearbyObject.NetworkObject);
            }
            else if (heldObject != null)
            {
                DropObjectServerRpc();
            }
        }
    }

    // --- Pick up ---
    // --- Pick up ---
    [ServerRpc]
    private void TryPickUpObjectServerRpc(NetworkObjectReference objRef)
    {
        if (objRef.TryGet(out NetworkObject netObj))
        {
            PickableObject pickable = netObj.GetComponent<PickableObject>();
            if (pickable != null && !pickable.isBeingHeld.Value)
            {
                pickable.SetHeldServerRpc(true);

                // Parent to hold point
                netObj.TrySetParent(holdPoint, false);

                heldObject = pickable;
            }
        }
    }

    // --- Drop ---
    [ServerRpc]
    private void DropObjectServerRpc()
    {
        if (heldObject == null) return;

        NetworkObject netObj = heldObject.GetComponent<NetworkObject>();

        heldObject.SetHeldServerRpc(false);

        // Detach
        netObj.TrySetParent((Transform)null, false);

        // Apply small throw force
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * 2f, ForceMode.Impulse);

        heldObject = null;
    }


    // --- Triggers ---
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        PickableObject pickable = other.GetComponent<PickableObject>();
        if (pickable != null && !pickable.isBeingHeld.Value)
        {
            nearbyObject = pickable;
            Debug.Log("Entered pickup range: " + other.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        PickableObject pickable = other.GetComponent<PickableObject>();
        if (pickable != null && pickable == nearbyObject)
        {
            Debug.Log("Exited pickup range: " + other.name);
            nearbyObject = null;
        }
    }
}
