using UnityEngine;
using Unity.Netcode;

public class PlayerAnimationController : NetworkBehaviour
{
    private Animator animator;

    private NetworkVariable<float> netSpeed = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Owner);

    private NetworkVariable<bool> netGrounded = new NetworkVariable<bool>(
        writePerm: NetworkVariableWritePermission.Owner);

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (IsOwner)
        {
            // Owner sets values locally and updates network variables
            animator.SetFloat("Speed", netSpeed.Value);
            animator.SetBool("Grounded", netGrounded.Value);
        }
        else
        {
            // Non-owners read from network variables
            animator.SetFloat("Speed", netSpeed.Value);
            animator.SetBool("Grounded", netGrounded.Value);
        }
    }

    public void SetSpeed(float speed)
    {
        if (IsOwner)
            netSpeed.Value = speed;
    }

    public void SetGrounded(bool isGrounded)
    {
        if (IsOwner)
            netGrounded.Value = isGrounded;
    }

    public void TriggerJump()
    {
        if (IsOwner)
            TriggerJumpServerRpc();
    }

    [ServerRpc]
    private void TriggerJumpServerRpc()
    {
        TriggerJumpClientRpc();
    }

    [ClientRpc]
    private void TriggerJumpClientRpc()
    {
        animator.SetTrigger("Jump");
    }
}
