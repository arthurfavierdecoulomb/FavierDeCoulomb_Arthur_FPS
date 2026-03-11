using UnityEngine;

public class PlayerMovement : MonoBehaviour                                                         // Gère tous les déplacements du joueur : marche, sprint, saut, accroupi
{
    [Header("Movement")]
    [Tooltip("Vitesse de marche normale")]
    public float walkSpeed = 6f;                                                                  // Vitesse de base en déplacement normal
    [Tooltip("Vitesse en sprint — consomme de la stamina")]
    public float sprintSpeed = 10f;                                                                 // Vitesse en sprint, plus élevée que la marche
    [Tooltip("Vitesse réduite en mode accroupi")]
    public float crouchSpeed = 3f;                                                                  // Vitesse réduite quand le joueur est accroupi
    [Tooltip("Hauteur de saut en mètres")]
    public float jumpHeight = 1.5f;                                                                // Hauteur du saut — convertie en vélocité via la formule physique
    [Tooltip("Gravité appliquée au joueur — valeur négative")]
    public float gravity = -20f;                                                                // Gravité personnalisée, plus forte que celle d'Unity par défaut

    [Header("Sprint")]
    [Tooltip("Touche maintenue pour sprinter — LeftShift par défaut")]
    public KeyCode sprintKey = KeyCode.LeftShift;                                                   // Touche de sprint, modifiable dans l'Inspector

    [Header("Crouch")]
    [Tooltip("Hauteur du CharacterController en position debout")]
    public float standingHeight = 2f;                                                        // Hauteur normale du joueur
    [Tooltip("Hauteur du CharacterController en position accroupie")]
    public float crouchHeight = 1f;                                                        // Hauteur réduite quand accroupi
    [Tooltip("Vitesse de transition entre debout et accroupi")]
    public float crouchTransitionSpeed = 10f;                                                       // Plus élevé = transition plus rapide

    [Header("Camera")]
    [Tooltip("Transform de la caméra — doit être un enfant du joueur positionné à la hauteur des yeux")]
    public Transform cameraTransform;                                                               // Référence à la caméra enfant du joueur
    [Tooltip("Hauteur de la caméra en position debout")]
    public float standingCameraHeight = 1.6f;                                                      // Position verticale de la caméra debout
    [Tooltip("Hauteur de la caméra en position accroupie")]
    public float crouchCameraHeight = 0.8f;                                                      // Position verticale de la caméra accroupi
    [Tooltip("Vitesse de transition de la caméra entre debout et accroupi")]
    public float cameraTransitionSpeed = 8f;                                                        // Fluidité de la caméra lors du passage accroupi/debout

    private CharacterController controller;                                                         // Composant Unity qui gère les collisions et le déplacement physique
    private Vector3 velocity;                                                                       // Vélocité verticale du joueur (saut + gravité)
    private bool isCrouching;                                                                      // Vrai si le joueur est actuellement accroupi
    private bool isSprinting;                                                                      // Vrai si le joueur est actuellement en sprint
    private float currentHeight;                                                                    // Hauteur actuelle interpolée du CharacterController
    private float targetHeight;                                                                     // Hauteur cible vers laquelle on interpole
    private float currentCameraHeight;                                                              // Hauteur actuelle interpolée de la caméra
    private float targetCameraHeight;                                                               // Hauteur cible de la caméra
    private StaminaSystem staminaSystem;                                                            // Référence au système de stamina pour conditionner sprint et accroupi
    private PlayerFootsteps footsteps;                                                              // Référence aux footsteps pour déclencher le son de saut

    void Start()
    {
        controller = GetComponent<CharacterController>();                                        // Récupère le CharacterController sur ce GameObject
        staminaSystem = GetComponent<StaminaSystem>();                                              // Récupère le système de stamina (optionnel)
        footsteps = GetComponent<PlayerFootsteps>();                                            // Récupère le script de pas (optionnel)

        currentHeight = standingHeight;                                                             // Initialise la hauteur au maximum
        targetHeight = standingHeight;
        controller.height = standingHeight;

        if (cameraTransform != null)
        {
            currentCameraHeight = standingCameraHeight;                                             // Initialise la caméra à la hauteur debout
            targetCameraHeight = standingCameraHeight;
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = standingCameraHeight;
            cameraTransform.localPosition = camPos;
        }
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");                                                      // Entrée latérale Q et D ou fleche gauche et droite
        float z = Input.GetAxis("Vertical");                                                        // Entrée avant/arrière Z ou S ou les flèches haut ou bas

        bool canSprint = staminaSystem == null || staminaSystem.CanSprint();                        // Autorise le sprint si pas de stamina ou stamina suffisante
        isSprinting = Input.GetKey(sprintKey) && z > 0.1f && !isCrouching && controller.isGrounded && canSprint; // Sprint uniquement en avançant, au sol, debout

        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);       // Choisit la vitesse selon l'état du joueur
        Vector3 move = transform.right * x + transform.forward * z;                               // Calcule la direction de déplacement en espace local

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;                                                                       // Maintient le joueur collé au sol pour éviter l'accumulation de gravité

        if (Input.GetButtonDown("Jump") && !isCrouching && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);                                   // la gravité est diminué à -20
            if (footsteps != null) footsteps.OnJump();                                             // Déclenche le son de saut au moment exact du départ
        }

        velocity.y += gravity * Time.deltaTime;                                                     // Applique la gravité frame par frame

        Vector3 finalMove = move * speed + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);                                                // Déplace le joueur avec le CharacterController

        HandleCrouch();                                                                             // Gère la logique d'accroupissement
        UpdateCameraHeight();                                                                       // Met à jour la position de la caméra
    }

    void HandleCrouch()
    {
        bool canCrouch = staminaSystem == null || staminaSystem.CanCrouch();                        // Autorise l'accroupissement selon la stamina

        if (Input.GetKey(KeyCode.LeftControl) && canCrouch)
        {
            targetHeight = crouchHeight;                                                      // Réduit la hauteur cible du CharacterController
            targetCameraHeight = crouchCameraHeight;                                                // Baisse la caméra cible
            isCrouching = true;
        }
        else
        {
            if (isCrouching && !canCrouch)
            {
                targetHeight = standingHeight;                                                // Force le retour debout si la stamina est insuffisante
                targetCameraHeight = standingCameraHeight;
                isCrouching = false;
            }
            else if (CanStandUp())
            {
                targetHeight = standingHeight;                                                // Retour debout si rien au-dessus du joueur
                targetCameraHeight = standingCameraHeight;
                isCrouching = false;
            }
        }

        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float previousHeight = currentHeight;
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed); // Transition fluide de la hauteur

            Vector3 center = controller.center;
            center.y += (currentHeight - previousHeight) / 2f;                                     // Ajuste le centre du collider pour rester ancré au sol
            controller.height = currentHeight;
            controller.center = center;
        }
    }

    void UpdateCameraHeight()
    {
        if (cameraTransform == null) return;                                                        // Sécurité : ne fait rien si la caméra n'est pas assignée

        if (Mathf.Abs(currentCameraHeight - targetCameraHeight) > 0.001f)
        {
            currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * cameraTransitionSpeed); // Transition fluide de la hauteur caméra
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = currentCameraHeight;
            cameraTransform.localPosition = camPos;                                                 // Applique la nouvelle hauteur à la caméra
        }
    }

    bool CanStandUp()
    {
        float checkDistance = standingHeight - crouchHeight;                                        // Distance à vérifier au-dessus de la tête
        Vector3 startPos = transform.position + Vector3.up * crouchHeight;                      // Part du sommet du collider accroupi
        return !Physics.Raycast(startPos, Vector3.up, checkDistance);                              // Vrai si rien ne bloque le retour debout
    }

    public bool IsGrounded() => controller.isGrounded;                                  // Vrai si le joueur touche le sol
    public bool IsCrouching() => isCrouching;                                            // Vrai si le joueur est accroupi
    public bool IsSprinting() => isSprinting;                                            // Vrai si le joueur est en sprint
    public float GetCurrentSpeed() => isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed); // Retourne la vitesse active selon l'état
    public float GetCurrentCameraHeight() => currentCameraHeight;                                   // Retourne la hauteur actuelle de la caméra (utilisée par CameraBob)
}