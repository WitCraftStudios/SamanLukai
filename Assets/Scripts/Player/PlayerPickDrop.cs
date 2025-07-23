using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickDrop : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private IInteractable currentInteractable;

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }
    void Update()
    {
        if (inputActions.Player.Interact.WasPressedThisFrame() && currentInteractable != null)
        {
            currentInteractable.OnInteract();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable = null;
        }
    }
}
