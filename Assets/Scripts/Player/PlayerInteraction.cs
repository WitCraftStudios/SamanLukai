using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private GameObject interactableObject;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Interact.performed += ctx => {
            Debug.Log("E pressed - Interact performed");
            Interact();
        };
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Interactable"))
        {
            interactableObject = other.gameObject;
            Debug.Log("Ready to interact with: " + interactableObject.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == interactableObject)
        {
            interactableObject = null;
            Debug.Log("Left interaction range.");
        }
    }

    private void Interact()
    {
        if (interactableObject != null)
        {
            Debug.Log("Interacted with: " + interactableObject.name);
            // Add your interaction logic here
        }
    }
}
