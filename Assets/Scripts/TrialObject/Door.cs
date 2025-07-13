using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public void OnInteract()
    {
        Debug.Log("Door opened!");
        // Add your door opening logic here
    }
}
