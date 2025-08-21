using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteraction player)
    {
        Debug.Log(player.name + " opened the door!");
        // Add door open animation or logic here
    }
}