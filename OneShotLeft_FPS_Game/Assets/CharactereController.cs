using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float standingCameraHeight = 1.6f;
    public float crouchCameraHeight = 0.8f;
    public float cameraTransitionSpeed = 8f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isCrouching;
    private bool isSprinting;
    private float currentHeight;
    private float targetHeight;
    private float currentCameraHeight;
    private float targetCameraHeight;
    private StaminaSystem staminaSystem;
    private PlayerFootsteps footsteps;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        staminaSystem = GetComponent<StaminaSystem>();
        footsteps = GetComponent<PlayerFootsteps>();

        currentHeight = standingHeight;
        targetHeight = standingHeight;
        controller.height = standingHeight;

        if (cameraTransform != null)
        {
            currentCameraHeight = standingCameraHeight;
            targetCameraHeight = standingCameraHeight;
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = standingCameraHeight;
            cameraTransform.localPosition = camPos;
        }
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool canSprint = staminaSystem == null || staminaSystem.CanSprint();
        isSprinting = Input.GetKey(sprintKey) && z > 0.1f && !isCrouching && controller.isGrounded && canSprint;

        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        Vector3 move = transform.right * x + transform.forward * z;

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Saut
        if (Input.GetButtonDown("Jump") && !isCrouching && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Son de saut
            if (footsteps != null) footsteps.OnJump();
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);

        HandleCrouch();
        UpdateCameraHeight();
    }

    void HandleCrouch()
    {
        bool canCrouch = staminaSystem == null || staminaSystem.CanCrouch();

        if (Input.GetKey(KeyCode.LeftControl) && canCrouch)
        {
            targetHeight = crouchHeight;
            targetCameraHeight = crouchCameraHeight;
            isCrouching = true;
        }
        else
        {
            if (isCrouching && !canCrouch)
            {
                targetHeight = standingHeight;
                targetCameraHeight = standingCameraHeight;
                isCrouching = false;
            }
            else if (CanStandUp())
            {
                targetHeight = standingHeight;
                targetCameraHeight = standingCameraHeight;
                isCrouching = false;
            }
        }

        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float previousHeight = currentHeight;
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            Vector3 center = controller.center;
            center.y += (currentHeight - previousHeight) / 2f;
            controller.height = currentHeight;
            controller.center = center;
        }
    }

    void UpdateCameraHeight()
    {
        if (cameraTransform == null) return;

        if (Mathf.Abs(currentCameraHeight - targetCameraHeight) > 0.001f)
        {
            currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * cameraTransitionSpeed);
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = currentCameraHeight;
            cameraTransform.localPosition = camPos;
        }
    }

    bool CanStandUp()
    {
        float checkDistance = standingHeight - crouchHeight;
        Vector3 startPos = transform.position + Vector3.up * crouchHeight;
        return !Physics.Raycast(startPos, Vector3.up, checkDistance);
    }

    public bool IsGrounded() => controller.isGrounded;
    public bool IsCrouching() => isCrouching;
    public bool IsSprinting() => isSprinting;
    public float GetCurrentSpeed() => isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
    public float GetCurrentCameraHeight() => currentCameraHeight;
}