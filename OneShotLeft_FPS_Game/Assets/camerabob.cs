using UnityEngine;

public class CameraBob : MonoBehaviour                                                  // Gère le bobbing de la caméra, la respiration et le camera shake
{
    [Header("Bob - Marche")]
    [SerializeField] private float walkBobAmount = 0.05f;                               // Amplitude du bob en marche — faible pour rester confortable
    [SerializeField] private float walkBobSpeed = 8f;                                   // Vitesse du bob en marche

    [Header("Bob - Sprint")]
    [SerializeField] private float sprintBobAmount = 0.1f;                              // Amplitude plus prononcée en sprint pour accentuer la sensation de vitesse
    [SerializeField] private float sprintBobSpeed = 12f;                                // Vitesse plus rapide en sprint

    [Header("Breathe")]
    [SerializeField] private float breathAmount = 0.015f;                               // Amplitude de la respiration — très subtile pour ne pas gêner
    [SerializeField] private float breathSpeed = 1.5f;                                  // Vitesse de la respiration (cycles par seconde)

    [Header("Transitions")]
    [SerializeField] private float bobTransitionSpeed = 5f;                             // Vitesse de transition entre les états de bob (marche → sprint → immobile)

    private PlayerMovement playerMovement;                                             // Référence au script de mouvement du joueur
    private float bobTimer = 0f;                                                        // Timer qui fait avancer l'animation de bob
    private float currentBobAmount = 0f;                                           // Amplitude actuelle interpolée
    private float currentBobSpeed = 0f;                                           // Vitesse actuelle interpolée
    private float targetBobAmount = 0f;                                           // Amplitude cible selon l'état du joueur
    private float targetBobSpeed = 0f;                                           // Vitesse cible selon l'état du joueur

    [Header("Camera Shake Smooth")]
    [SerializeField] private float shakeFrequency = 25f;                           // Fréquence du bruit de Perlin pour le shake — plus élevé = plus chaotique

    private float shakeTimeRemaining;                                               // Temps restant du shake en cours
    private float shakeTotalDuration;                                               // Durée totale du shake (pour normaliser le fade out)
    private float shakeStartIntensity;                                              // Intensité de départ du shake

    void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();                    // Cherche PlayerMovement sur le parent (la caméra est enfant du joueur)
        if (playerMovement == null)
            Debug.LogError("CameraBob : PlayerMovement non trouvé sur un parent !"); // Erreur critique si la référence manque
    }

    void LateUpdate()                                                               // LateUpdate pour s'appliquer APRÈS PlayerMovement et MouseLook
    {
        if (playerMovement == null) return;                                         // Sécurité : ne fait rien si la référence manque

        bool isMoving = playerMovement.GetCurrentSpeed() > 0.1f;                 // Vrai si le joueur se déplace
        bool isSprinting = playerMovement.IsSprinting();                           // Vrai si le joueur est en sprint
        bool isGrounded = playerMovement.IsGrounded();                            // Vrai si le joueur est au sol

        if (isMoving && isGrounded)
        {
            targetBobAmount = isSprinting ? sprintBobAmount : walkBobAmount;       // Bob prononcé en sprint, discret en marche
            targetBobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;        // Rythme adapté à l'état
        }
        else
        {
            targetBobAmount = 0f;                                                   // Pas de bob si immobile ou en l'air
            targetBobSpeed = walkBobSpeed;                                         // Garde la vitesse de marche comme base de transition
        }

        currentBobAmount = Mathf.Lerp(currentBobAmount, targetBobAmount, Time.deltaTime * bobTransitionSpeed); // Transition fluide de l'amplitude
        currentBobSpeed = Mathf.Lerp(currentBobSpeed, targetBobSpeed, Time.deltaTime * bobTransitionSpeed); // Transition fluide de la vitesse

        if (currentBobAmount > 0.001f)
            bobTimer += Time.deltaTime * currentBobSpeed;                          // Avance le timer seulement si le bob est actif

        float bobOffset = Mathf.Sin(bobTimer) * currentBobAmount;                                   // Déplacement vertical en sinus — effet de pas classique
        float breathOffset = Mathf.Sin(Time.time * breathSpeed * Mathf.PI * 2f) * breathAmount;     // Respiration : oscillation lente indépendante du mouvement
        float baseY = playerMovement.GetCurrentCameraHeight();                                      // Hauteur de base de la caméra (gérée par PlayerMovement)

        // ── Camera Shake ──────────────────────────────────────────────────
        Vector3 shakeOffset = Vector3.zero;                                        // Offset du shake, nul par défaut

        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining -= Time.deltaTime;                                  // Décompte du shake

            float normalizedTime = 1f - (shakeTimeRemaining / shakeTotalDuration); // Progression de 0 à 1 pendant le shake

            float currentIntensity = Mathf.Lerp(
                shakeStartIntensity, 0f,
                Mathf.SmoothStep(0f, 1f, normalizedTime)                           // Fade out smooth : l'intensité diminue progressivement
            );

            float shakeX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f; // Bruit de Perlin entre -1 et 1 sur l'axe X
            float shakeY = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) * 2f - 1f; // Bruit de Perlin entre -1 et 1 sur l'axe Y

            shakeOffset = new Vector3(shakeX, shakeY, 0f) * currentIntensity;     // Applique l'intensité au vecteur de shake
        }

        // ── Application finale ────────────────────────────────────────────
        Vector3 camPos = transform.localPosition;                                  // Récupère la position locale actuelle de la caméra
        camPos.y = baseY + bobOffset + breathOffset + shakeOffset.y;              // Combine hauteur de base + bob + respiration + shake vertical
        camPos.x = shakeOffset.x;                                                  // Shake horizontal uniquement (pas de bob latéral sur la caméra)
        transform.localPosition = camPos;                                          // Applique la position finale
    }

    public void Shake(float duration, float intensity)                             // Appelé depuis l'extérieur pour déclencher un camera shake
    {
        shakeTotalDuration = duration;                                           // Stocke la durée totale pour le calcul du fade out
        shakeTimeRemaining = duration;                                           // Initialise le décompte
        shakeStartIntensity = intensity;                                          // Intensité maximale au début du shake
    }
}