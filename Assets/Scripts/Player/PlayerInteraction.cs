using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    public InputSystem_Actions inputActions;
    public LayerMask interactableLayer;
    public float interactRange = 2f;
    public Transform holdPoint;

    private bool isHolding = false;
    private GameObject heldObject = null;
    private PlayerRoleManager roleManager; // To check player role

    // Store the delegate so we can unsubscribe correctly
    private System.Action<InputAction.CallbackContext> pickCallback;

    private void Awake()
    {
        roleManager = GetComponent<PlayerRoleManager>();
    }

    private void Update()
    {
        if (!IsOwner) return; // Only local player handles their own pickup logic

        if (roleManager == null)
        {
            Debug.LogWarning("roleManager is null!");
            return;
        }

        if (roleManager.role.Value != PlayerRole.Hider)
        {
            // Only hiders can hold objects
            return;
        }

        if (isHolding && heldObject != null)
        {
            var netObj = heldObject.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                heldObject.transform.position = holdPoint.position;
                heldObject.transform.rotation = holdPoint.rotation;
            }
        }
    }

    private void OnEnable()
    {
        if (!IsOwner) return;

        if (inputActions == null)
            inputActions = new InputSystem_Actions();

        inputActions.Enable();

        pickCallback = ctx => OnInteract();
        inputActions.Player.Pick.performed += pickCallback;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        if (inputActions != null && pickCallback != null)
        {
            inputActions.Player.Pick.performed -= pickCallback;
            pickCallback = null;
        }

        if (inputActions != null)
            inputActions.Disable();
    }

    private void OnInteract()
    {
        Debug.Log("OnInteract called");

        if (roleManager == null || roleManager.role.Value != PlayerRole.Hider)
        {
            Debug.Log("Role check failed or player is not Hider");
            return;
        }

        if (heldObject == null)
        {
            Debug.Log("Trying to pick up object");
            TryPickupObject();
        }
        else
        {
            Debug.Log("Dropping object");
            DropObject();
        }
    }

    private void TryPickupObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
            PickupObject(hit.collider.gameObject);
        }
        else
        {
            Debug.Log("Raycast did not hit any interactable object");
        }
    }

    private void PickupObject(GameObject obj)
    {
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner)
        {
            RequestPickupServerRpc(netObj);
        }

        heldObject = obj;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        isHolding = true;
    }

    private void DropObject()
    {
        if (heldObject == null) return;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.AddForce(transform.forward * 2f, ForceMode.VelocityChange);
        }

        if (heldObject.TryGetComponent(out NetworkObject netObj) && netObj.IsOwner)
        {
            netObj.RemoveOwnership();
        }

        heldObject = null;
        isHolding = false;
    }

    [ServerRpc]
    private void RequestPickupServerRpc(NetworkObjectReference objectRef, ServerRpcParams rpcParams = default)
    {
        if (!objectRef.TryGet(out NetworkObject netObj))
        {
            Debug.LogWarning("Failed to get NetworkObject from reference");
            return;
        }

        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            var playerObject = networkClient.PlayerObject;
            if (playerObject == null)
            {
                Debug.LogWarning("PlayerObject is null for clientId " + clientId);
                return;
            }

            var roleManager = playerObject.GetComponent<PlayerRoleManager>();
            if (roleManager == null)
            {
                Debug.LogWarning("No PlayerRoleManager found on player object");
                return;
            }

            if (roleManager.role.Value == PlayerRole.Hider)
            {
                netObj.ChangeOwnership(clientId);
                Debug.Log("Ownership granted to clientId " + clientId);
            }
            else
            {
                Debug.Log("Ownership request denied: player is not a Hider");
            }
        }
    }
}
