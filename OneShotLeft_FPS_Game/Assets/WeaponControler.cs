using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform rightArmTransform; // Bras droit
    public Transform leftArmTransform; // Bras gauche
    public Transform weaponTransform; // L'arme
    public PlayerMovement playerMovement;
    public Transform cameraTransform;

    [Header("Right Arm Position")]
    public Vector3 rightArmPositionOffset = new Vector3(0.3f, -0.2f, 0.4f);
    public Vector3 rightArmRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Left Arm Position (Sprint Only)")]
    public Vector3 leftArmPositionOffset = new Vector3(-0.3f, -0.15f, 0.4f);
    public Vector3 leftArmRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Weapon Position")]
    public Vector3 weaponPositionOffset = new Vector3(0.5f, -0.3f, 0.5f);
    public Vector3 weaponRotationOffset = new Vector3(0f, -90f, 0f);

    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmooth = 6f;

    [Header("Weapon Bob")]
    public float bobSpeed = 10f;
    public float bobAmount = 0.05f;
    public float bobAmountVertical = 0.03f;

    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public Vector3 rightArmSprintOffset = new Vector3(0.2f, -0.3f, 0.3f);
    public Vector3 weaponSprintOffset = new Vector3(0.2f, -0.2f, 0.3f);
    public Vector3 weaponSprintRotation = new Vector3(-10f, -80f, -5f);
    public float sprintBobMultiplier = 1.8f;
    public float sprintTransitionSpeed = 8f;

    [Header("Crouch")]
    public Vector3 crouchOffset = new Vector3(0.1f, -0.1f, 0.05f);
    public float crouchRotation = 5f;
    public float crouchTransitionSpeed = 8f;

    [Header("Jump/Landing")]
    public float jumpRotation = -10f;
    public float landRotation = 15f;
    public float jumpTransitionSpeed = 10f;

    // Variables pour le bras droit
    private Vector3 rightArmInitialPos;
    private Quaternion rightArmInitialRot;
    private Vector3 rightArmSwayPos;
    private Vector3 rightArmTargetOffset;

    // Variables pour le bras gauche
    private Vector3 leftArmInitialPos;
    private Quaternion leftArmInitialRot;
    private float leftArmAlpha = 0f;

    // Variables pour l'arme
    private Vector3 weaponInitialPos;
    private Quaternion weaponInitialRot;
    private Vector3 weaponSwayPos;
    private Vector3 weaponSwayRot;
    private Vector3 weaponTargetOffset;
    private Vector3 weaponTargetRotation;

    // Variables communes
    private float bobTimer;
    private bool wasGrounded;
    private float landingTimer;
    private bool isSprinting;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        SetupArms();
        wasGrounded = true;
    }

    void SetupArms()
    {
        // Setup bras droit
        if (rightArmTransform != null)
        {
            if (rightArmTransform.parent != cameraTransform)
                rightArmTransform.SetParent(cameraTransform);

            rightArmTransform.localPosition = rightArmPositionOffset;
            rightArmTransform.localRotation = Quaternion.Euler(rightArmRotationOffset);
            rightArmTransform.localScale = Vector3.one;
            rightArmInitialPos = rightArmTransform.localPosition;
            rightArmInitialRot = rightArmTransform.localRotation;
        }

        // Setup bras gauche
        if (leftArmTransform != null)
        {
            if (leftArmTransform.parent != cameraTransform)
                leftArmTransform.SetParent(cameraTransform);

            leftArmTransform.localPosition = leftArmPositionOffset;
            leftArmTransform.localRotation = Quaternion.Euler(leftArmRotationOffset);
            leftArmTransform.localScale = Vector3.one;
            leftArmInitialPos = leftArmTransform.localPosition;
            leftArmInitialRot = leftArmTransform.localRotation;
            leftArmTransform.gameObject.SetActive(false);
        }

        // Setup arme
        if (weaponTransform != null)
        {
            if (weaponTransform.parent != cameraTransform)
                weaponTransform.SetParent(cameraTransform);

            weaponTransform.localPosition = weaponPositionOffset;
            weaponTransform.localRotation = Quaternion.Euler(weaponRotationOffset);
            weaponTransform.localScale = Vector3.one;
            weaponInitialPos = weaponTransform.localPosition;
            weaponInitialRot = weaponTransform.localRotation;
        }
    }

    void LateUpdate()
    {
        HandleSprint();
        HandleSway();
        HandleBob();
        HandleCrouch();
        HandleJumpAndLanding();
        ApplyMovements();
    }

    void HandleSprint()
    {
        float vertical = Input.GetAxis("Vertical");
        bool wantsToSprint = Input.GetKey(sprintKey) && vertical > 0.1f &&
                            playerMovement != null && playerMovement.IsGrounded() &&
                            !playerMovement.IsCrouching();

        isSprinting = wantsToSprint;

        if (isSprinting)
        {
            // Offsets pour le sprint
            rightArmTargetOffset = Vector3.Lerp(rightArmTargetOffset, rightArmSprintOffset, Time.deltaTime * sprintTransitionSpeed);
            weaponTargetOffset = Vector3.Lerp(weaponTargetOffset, weaponSprintOffset, Time.deltaTime * sprintTransitionSpeed);
            weaponTargetRotation = Vector3.Lerp(weaponTargetRotation, weaponSprintRotation, Time.deltaTime * sprintTransitionSpeed);
        }
        else if (playerMovement != null && !playerMovement.IsCrouching())
        {
            rightArmTargetOffset = Vector3.Lerp(rightArmTargetOffset, Vector3.zero, Time.deltaTime * sprintTransitionSpeed);
            weaponTargetOffset = Vector3.Lerp(weaponTargetOffset, Vector3.zero, Time.deltaTime * sprintTransitionSpeed);
            weaponTargetRotation = Vector3.Lerp(weaponTargetRotation, Vector3.zero, Time.deltaTime * sprintTransitionSpeed);
        }
    }

    void HandleSway()
    {
        if (isSprinting)
        {
            // Réinitialiser le sway en sprint
            rightArmSwayPos = Vector3.Lerp(rightArmSwayPos, Vector3.zero, Time.deltaTime * swaySmooth);
            weaponSwayPos = Vector3.Lerp(weaponSwayPos, Vector3.zero, Time.deltaTime * swaySmooth);
            weaponSwayRot = Vector3.Lerp(weaponSwayRot, Vector3.zero, Time.deltaTime * swaySmooth);
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float targetSwayX = Mathf.Clamp(-mouseX * swayAmount, -maxSwayAmount, maxSwayAmount);
        float targetSwayY = Mathf.Clamp(-mouseY * swayAmount, -maxSwayAmount, maxSwayAmount);

        // Appliquer au bras droit
        rightArmSwayPos.x = Mathf.Lerp(rightArmSwayPos.x, targetSwayX, Time.deltaTime * swaySmooth);
        rightArmSwayPos.y = Mathf.Lerp(rightArmSwayPos.y, targetSwayY, Time.deltaTime * swaySmooth);

        // Appliquer à l'arme
        weaponSwayPos.x = Mathf.Lerp(weaponSwayPos.x, targetSwayX, Time.deltaTime * swaySmooth);
        weaponSwayPos.y = Mathf.Lerp(weaponSwayPos.y, targetSwayY, Time.deltaTime * swaySmooth);

        weaponSwayRot.y = Mathf.Lerp(weaponSwayRot.y, -mouseX * swayAmount * 100, Time.deltaTime * swaySmooth);
        weaponSwayRot.x = Mathf.Lerp(weaponSwayRot.x, mouseY * swayAmount * 100, Time.deltaTime * swaySmooth);
    }

    void HandleBob()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        if (isMoving && playerMovement != null && playerMovement.IsGrounded())
        {
            float speedMultiplier = playerMovement.IsCrouching() ? 0.5f : 1f;
            if (isSprinting) speedMultiplier = sprintBobMultiplier;

            bobTimer += Time.deltaTime * bobSpeed * speedMultiplier;

            float bobX = Mathf.Cos(bobTimer) * bobAmount * (isSprinting ? 1.5f : 1f);
            float bobY = Mathf.Sin(bobTimer * 2) * bobAmountVertical * (isSprinting ? 2f : 1f);

            rightArmSwayPos.x += bobX;
            rightArmSwayPos.y += bobY;

            weaponSwayPos.x += bobX;
            weaponSwayPos.y += bobY;
        }
        else
        {
            bobTimer = 0;
        }
    }

    void HandleCrouch()
    {
        if (isSprinting) return;

        if (playerMovement != null && playerMovement.IsCrouching())
        {
            rightArmTargetOffset = Vector3.Lerp(rightArmTargetOffset, crouchOffset, Time.deltaTime * crouchTransitionSpeed);
            weaponTargetOffset = Vector3.Lerp(weaponTargetOffset, crouchOffset, Time.deltaTime * crouchTransitionSpeed);
            weaponTargetRotation.z = Mathf.Lerp(weaponTargetRotation.z, crouchRotation, Time.deltaTime * crouchTransitionSpeed);
        }
        else
        {
            rightArmTargetOffset = Vector3.Lerp(rightArmTargetOffset, Vector3.zero, Time.deltaTime * crouchTransitionSpeed);
            weaponTargetOffset = Vector3.Lerp(weaponTargetOffset, Vector3.zero, Time.deltaTime * crouchTransitionSpeed);
            weaponTargetRotation.z = Mathf.Lerp(weaponTargetRotation.z, 0, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    void HandleJumpAndLanding()
    {
        if (playerMovement == null) return;

        bool isGrounded = playerMovement.IsGrounded();

        if (wasGrounded && !isGrounded)
        {
            landingTimer = 0;
            weaponSwayRot.x += jumpRotation;
        }

        if (!wasGrounded && isGrounded)
        {
            landingTimer = 1f;
        }

        if (landingTimer > 0)
        {
            float landRotAmount = Mathf.Lerp(0, landRotation, landingTimer);
            weaponSwayRot.x = Mathf.Lerp(weaponSwayRot.x, landRotAmount, Time.deltaTime * jumpTransitionSpeed);
            landingTimer -= Time.deltaTime * 2f;
        }

        wasGrounded = isGrounded;
    }

    void ApplyMovements()
    {
        // Appliquer au bras droit
        if (rightArmTransform != null)
        {
            rightArmTransform.localScale = Vector3.one;
            Vector3 rightArmFinalPos = rightArmInitialPos + rightArmSwayPos + rightArmTargetOffset;
            rightArmTransform.localPosition = rightArmFinalPos;
            rightArmTransform.localRotation = rightArmInitialRot;
        }

        // Appliquer au bras gauche (sprint uniquement)
        if (leftArmTransform != null)
        {
            if (isSprinting)
            {
                leftArmAlpha = Mathf.Lerp(leftArmAlpha, 1f, Time.deltaTime * 10f);
                if (!leftArmTransform.gameObject.activeSelf)
                    leftArmTransform.gameObject.SetActive(true);

                // Animation de balancement
                float handBob = Mathf.Sin(bobTimer * 2) * 0.08f;
                Vector3 leftArmPos = leftArmInitialPos;
                leftArmPos.y += handBob;
                leftArmTransform.localPosition = Vector3.Lerp(leftArmTransform.localPosition, leftArmPos, Time.deltaTime * 10f);
            }
            else
            {
                leftArmAlpha = Mathf.Lerp(leftArmAlpha, 0f, Time.deltaTime * 10f);
                if (leftArmAlpha < 0.01f && leftArmTransform.gameObject.activeSelf)
                    leftArmTransform.gameObject.SetActive(false);
            }
        }

        // Appliquer à l'arme
        if (weaponTransform != null)
        {
            weaponTransform.localScale = Vector3.one;
            Vector3 weaponFinalPos = weaponInitialPos + weaponSwayPos + weaponTargetOffset;
            Quaternion additionalRot = Quaternion.Euler(weaponTargetRotation);
            Quaternion swayRot = Quaternion.Euler(weaponSwayRot);
            Quaternion weaponFinalRot = weaponInitialRot * additionalRot * swayRot;

            weaponTransform.localPosition = weaponFinalPos;
            weaponTransform.localRotation = weaponFinalRot;
        }
    }
}