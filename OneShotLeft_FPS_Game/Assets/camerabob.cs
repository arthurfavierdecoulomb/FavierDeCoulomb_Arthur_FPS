using UnityEngine;

public class CameraBob : MonoBehaviour
{
    [Header("Bob - Marche")]
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float walkBobSpeed = 8f;

    [Header("Bob - Sprint")]
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float sprintBobSpeed = 12f;

    [Header("Breathe")]
    [SerializeField] private float breathAmount = 0.015f;
    [SerializeField] private float breathSpeed = 1.5f;

    [Header("Transitions")]
    [SerializeField] private float bobTransitionSpeed = 5f;

    private PlayerMovement playerMovement;
    private float bobTimer = 0f;
    private float currentBobAmount = 0f;
    private float currentBobSpeed = 0f;
    private float targetBobAmount = 0f;
    private float targetBobSpeed = 0f;

    void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
            Debug.LogError("CameraBob : PlayerMovement non trouvé sur un parent !");
    }

    // LateUpdate pour s'appliquer APRÈS PlayerMovement et MouseLook
    void LateUpdate()
    {
        if (playerMovement == null) return;

        bool isMoving = playerMovement.GetCurrentSpeed() > 0.1f;
        bool isSprinting = playerMovement.IsSprinting();
        bool isGrounded = playerMovement.IsGrounded();

        // --- Cibles de bob selon l'état du joueur ---
        if (isMoving && isGrounded)
        {
            targetBobAmount = isSprinting ? sprintBobAmount : walkBobAmount;
            targetBobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;
        }
        else
        {
            targetBobAmount = 0f;
            targetBobSpeed = walkBobSpeed;
        }

        // --- Transition fluide vers la cible ---
        currentBobAmount = Mathf.Lerp(currentBobAmount, targetBobAmount, Time.deltaTime * bobTransitionSpeed);
        currentBobSpeed = Mathf.Lerp(currentBobSpeed, targetBobSpeed, Time.deltaTime * bobTransitionSpeed);

        // --- Calcul du bob sinusoïdal ---
        if (currentBobAmount > 0.001f)
            bobTimer += Time.deltaTime * currentBobSpeed;

        float bobOffset = Mathf.Sin(bobTimer) * currentBobAmount;

        // --- Calcul de la respiration (toujours active) ---
        float breathOffset = Mathf.Sin(Time.time * breathSpeed * Mathf.PI * 2f) * breathAmount;

        // --- Récupère la base Y depuis PlayerMovement (gère le crouch) ---
        float baseY = playerMovement.GetCurrentCameraHeight();

        // --- Applique bob + respiration sur le Y de la caméra ---
        Vector3 camPos = transform.localPosition;
        camPos.y = baseY + bobOffset + breathOffset;
        transform.localPosition = camPos;
    }
}