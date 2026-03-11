using UnityEngine;

public class CameraBob : MonoBehaviour
{
    [Header("Bob - Marche")] // paramètres de bobbing pour la marche, ajustables dans l'inspecteur pour
                             // trouver le bon équilibre entre réalisme et confort visuel
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float walkBobSpeed = 8f;

    [Header("Bob - Sprint")] // paramètres de bobbing pour le sprint, généralement
                             // plus prononcés que pour la marche, ajustables dans l'inspecteur
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float sprintBobSpeed = 12f;

    [Header("Breathe")] // paramètres de respiration, qui ajoutent un léger mouvement même
                        // lorsque le joueur est immobile, pour plus de réalisme
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

    [Header("Camera Shake Smooth")]
    [SerializeField] private float shakeFrequency = 25f;

    private float shakeTimeRemaining;
    private float shakeTotalDuration;
    private float shakeStartIntensity;



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

        // --- Transition fluide ---
        currentBobAmount = Mathf.Lerp(currentBobAmount, targetBobAmount, Time.deltaTime * bobTransitionSpeed);
        currentBobSpeed = Mathf.Lerp(currentBobSpeed, targetBobSpeed, Time.deltaTime * bobTransitionSpeed);

        if (currentBobAmount > 0.001f)
            bobTimer += Time.deltaTime * currentBobSpeed;

        float bobOffset = Mathf.Sin(bobTimer) * currentBobAmount;

        // --- Respiration ---
        float breathOffset = Mathf.Sin(Time.time * breathSpeed * Mathf.PI * 2f) * breathAmount;

        float baseY = playerMovement.GetCurrentCameraHeight();

        // =========================
        // CAMERA SHAKE (SMOOTH)
        // =========================
        Vector3 shakeOffset = Vector3.zero;

        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining -= Time.deltaTime;

            float normalizedTime = 1f - (shakeTimeRemaining / shakeTotalDuration);

            float currentIntensity = Mathf.Lerp(
                shakeStartIntensity,
                0f,
                Mathf.SmoothStep(0f, 1f, normalizedTime)
            );

            float shakeX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f;

            shakeOffset = new Vector3(shakeX, shakeY, 0f) * currentIntensity;
        }

        // =========================
        // APPLICATION CAMERA
        // =========================
        Vector3 camPos = transform.localPosition;
        camPos.y = baseY + bobOffset + breathOffset + shakeOffset.y;
        camPos.x = shakeOffset.x;
        transform.localPosition = camPos;
    }


    public void Shake(float duration, float intensity)
    {
        shakeTotalDuration = duration;
        shakeTimeRemaining = duration;
        shakeStartIntensity = intensity;
    }



}
