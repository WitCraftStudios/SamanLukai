using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;


public class AssignCameraToPlayer : NetworkBehaviour
{
    public CinemachineCamera cinemachineCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (cinemachineCamera == null)
        {
            cinemachineCamera = Object.FindFirstObjectByType<CinemachineCamera>();
        }
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = this.transform;
        }
    }
}
