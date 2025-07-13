using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    public float playerSpeed = 2.0f;
    private float jumpHeight = 1.0f;
    private float gravityValue = -9.81f;
    private float turnSmoothVelocity;
    private bool jumpRequested = false;
    public float sprintSpeed = 5.0f;

    public Transform cameraTransform;
    private InputSystem_Actions inputActions;
    private PlayerAnimationController animationController;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null)
            inputActions.Player.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animationController = GetComponent<PlayerAnimationController>();
    }

    void Update()
    {
        if(!IsOwner)  return;
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        float currentSpeed = playerSpeed;
        if (inputActions.Player.Sprint.IsPressed())
        {
            currentSpeed = sprintSpeed;
        }

        Vector3 moveDirection = Vector3.zero;
        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.1f);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            moveDirection = moveDir.normalized;
        }

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Revert: Only set speed and grounded for animation
        animationController.SetSpeed(moveInput.magnitude);
        animationController.SetGrounded(groundedPlayer);

        if (inputActions.Player.Jump.WasPressedThisFrame())
        {
            jumpRequested = true;
        }

        // When jump is performed and grounded
        if (jumpRequested && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
            jumpRequested = false;
            animationController.TriggerJump();
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
}
