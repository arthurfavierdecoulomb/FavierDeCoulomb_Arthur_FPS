using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform handTransform; // La main qui tient l'arme
    public PlayerMovement playerMovement;
    public Transform cameraTransform;

    [Header("Hand Position")]
    public Vector3 handPositionOffset = new Vector3(0.5f, -0.3f, 0.5f);
    public Vector3 handRotationOffset = new Vector3(0f, -90f, 0f);

    [Header("Weapon Sway")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmooth = 6f;

    [Header("Weapon Bob")]
    public float bobSpeed = 10f;
    public float bobAmount = 0.05f;
    public float bobAmountVertical = 0.03f;

    [Header("Crouch")]
    public Vector3 crouchOffset = new Vector3(0.1f, -0.1f, 0.05f);
    public float crouchRotation = 5f;
    public float crouchTransitionSpeed = 8f;

    [Header("Jump/Landing")]
    public float jumpRotation = -10f;
    public float landRotation = 15f;
    public float jumpTransitionSpeed = 10f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 swayPos;
    private Vector3 swayRot;
    private float bobTimer;
    private bool wasGrounded;
    private float landingTimer;
    private Vector3 targetOffset;
    private float targetRotationZ;

    void Start()
    {
        if (handTransform == null)
            handTransform = transform;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        SetupHandPosition();

        initialPosition = handTransform.localPosition;
        initialRotation = handTransform.localRotation;
        wasGrounded = true;
    }

    void SetupHandPosition()
    {
        // Positionner la main comme enfant de la caméra si ce n'est pas déjà fait
        if (handTransform.parent != cameraTransform)
        {
            handTransform.SetParent(cameraTransform);
        }

        // Appliquer la position et rotation initiale
        handTransform.localPosition = handPositionOffset;
        handTransform.localRotation = Quaternion.Euler(handRotationOffset);
    }

    void Update()
    {
        HandleSway();
        HandleBob();
        HandleCrouch();
        HandleJumpAndLanding();
        ApplyMovements();
    }

    void HandleSway()
    {
        // Récupérer le mouvement de la souris
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Calculer le sway cible
        float targetSwayX = Mathf.Clamp(-mouseX * swayAmount, -maxSwayAmount, maxSwayAmount);
        float targetSwayY = Mathf.Clamp(-mouseY * swayAmount, -maxSwayAmount, maxSwayAmount);

        // Appliquer le sway avec smoothing
        swayPos.x = Mathf.Lerp(swayPos.x, targetSwayX, Time.deltaTime * swaySmooth);
        swayPos.y = Mathf.Lerp(swayPos.y, targetSwayY, Time.deltaTime * swaySmooth);

        // Rotation du sway
        swayRot.y = Mathf.Lerp(swayRot.y, -mouseX * swayAmount * 100, Time.deltaTime * swaySmooth);
        swayRot.x = Mathf.Lerp(swayRot.x, mouseY * swayAmount * 100, Time.deltaTime * swaySmooth);
    }

    void HandleBob()
    {
        // Vérifier si le joueur bouge
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        if (isMoving && playerMovement != null && playerMovement.IsGrounded())
        {
            float speedMultiplier = playerMovement.IsCrouching() ? 0.5f : 1f;

            // Incrémenter le timer
            bobTimer += Time.deltaTime * bobSpeed * speedMultiplier;

            // Calculer le bob
            float bobX = Mathf.Cos(bobTimer) * bobAmount;
            float bobY = Mathf.Sin(bobTimer * 2) * bobAmountVertical;

            swayPos.x += bobX;
            swayPos.y += bobY;
        }
        else
        {
            // Réinitialiser progressivement
            bobTimer = 0;
        }
    }

    void HandleCrouch()
    {
        if (playerMovement != null && playerMovement.IsCrouching())
        {
            targetOffset = Vector3.Lerp(targetOffset, crouchOffset, Time.deltaTime * crouchTransitionSpeed);
            targetRotationZ = Mathf.Lerp(targetRotationZ, crouchRotation, Time.deltaTime * crouchTransitionSpeed);
        }
        else
        {
            targetOffset = Vector3.Lerp(targetOffset, Vector3.zero, Time.deltaTime * crouchTransitionSpeed);
            targetRotationZ = Mathf.Lerp(targetRotationZ, 0, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    void HandleJumpAndLanding()
    {
        if (playerMovement == null) return;

        bool isGrounded = playerMovement.IsGrounded();

        // Détection du saut
        if (wasGrounded && !isGrounded)
        {
            landingTimer = 0;
            swayRot.x += jumpRotation;
        }

        // Détection de l'atterrissage
        if (!wasGrounded && isGrounded)
        {
            landingTimer = 1f;
        }

        // Animation d'atterrissage
        if (landingTimer > 0)
        {
            float landRotAmount = Mathf.Lerp(0, landRotation, landingTimer);
            swayRot.x = Mathf.Lerp(swayRot.x, landRotAmount, Time.deltaTime * jumpTransitionSpeed);
            landingTimer -= Time.deltaTime * 2f;
        }

        wasGrounded = isGrounded;
    }

    void ApplyMovements()
    {
        // Combiner toutes les positions et rotations
        Vector3 finalPosition = initialPosition + swayPos + targetOffset;
        Quaternion swayRotation = Quaternion.Euler(swayRot.x, swayRot.y, swayRot.z + targetRotationZ);
        Quaternion finalRotation = initialRotation * swayRotation;

        // Appliquer à la main
        handTransform.localPosition = finalPosition;
        handTransform.localRotation = finalRotation;
    }
}