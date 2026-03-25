using UnityEngine;

public class ArmFollowCamera : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform cameraTransform;                             // Référence à la caméra principale (assignée dans l'Inspector)
    [SerializeField] private PlayerMovement playerMovement;                         // Référence au script de mouvement du joueur

    [Header("Position du bras")]
    [SerializeField] private Vector3 localOffset = new Vector3(0.5f, -0.3f, 0.5f); // Décalage du bras par rapport au centre de la caméra

    [Header("Correction de rotation")]
    [SerializeField] private Vector3 rotationCorrection;                            // Correction d'angle pour aligner visuellement le bras

    [Header("Arm Bob")]
    [SerializeField] private float walkBobSpeed = 8f;                            // Vitesse du balancement en marche
    [SerializeField] private float walkBobAmount = 0.03f;                         // Amplitude du balancement en marche
    [SerializeField] private float sprintBobSpeed = 12f;                          // Vitesse du balancement en sprint
    [SerializeField] private float sprintBobAmount = 0.06f;                        // Amplitude du balancement en sprint (plus prononcé)
    [SerializeField] private float crouchBobAmount = 0.01f;                        // Amplitude réduite en mode accroupi

    private float bobTimer;                                                         // Timer interne qui fait avancer l'animation de bob

    void Start()
    {
<<<<<<< Updated upstream
        // Si les références ne sont pas assignées dans l'inspecteur, tente de les trouver automatiquement
=======
        // Si aucune caméra n'est assignée, utilise la caméra principale
>>>>>>> Stashed changes
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;                                // Fallback : utilise la caméra taguée MainCamera
    }

    void Update()
    {
        // Ne pas bouger les bras si la souris n'est pas verrouillée
        if (Cursor.lockState != CursorLockMode.Locked) return;                     // Bloque si on est dans un menu ou écran de mort
    }

    void LateUpdate()
    {
<<<<<<< Updated upstream
        // Vérification des références
        if (cameraTransform == null || playerMovement == null) return;             // Sécurité : évite les erreurs nulles

        // Rotation du bras pour suivre la caméra + correction
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(rotationCorrection); // Le bras regarde toujours dans la même direction que la caméra
=======
        // Assure que les références sont valides
        if (cameraTransform == null || playerMovement == null) return;

        // Applique la rotation de la caméra avec une correction
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(rotationCorrection);
>>>>>>> Stashed changes

        // Position de base du bras
        Vector3 finalOffset = localOffset;                                         // On part de l'offset de base, qu'on va modifier avec le bob

        // Bobbing du bras
        bool isMoving = playerMovement.GetCurrentSpeed() > 0.1f &&
                        playerMovement.IsGrounded();                               // Le bob ne s'active qu'au sol et en mouvement

        // Choix du bob selon l'état du joueur
        if (isMoving)
        {
            float bobSpeed;
            float bobAmount;

<<<<<<< Updated upstream
            // Priorité : accroupi > sprint > marche
=======
            // Ajuste la vitesse et l'amplitude du bob en fonction de l'état du joueur
>>>>>>> Stashed changes
            if (playerMovement.IsCrouching())
            {
                bobSpeed = walkBobSpeed;                                           // Même rythme que la marche
                bobAmount = crouchBobAmount;                                       // Mais amplitude très réduite (discrétion)
            }
            else if (playerMovement.IsSprinting())
            {
                bobSpeed = sprintBobSpeed;                                         // Rythme plus rapide
                bobAmount = sprintBobAmount;                                       // Amplitude plus grande (sensation de vitesse)
            }
            else
            {
                bobSpeed = walkBobSpeed;                                           // Valeurs par défaut pour la marche normale
                bobAmount = walkBobAmount;
            }

            // Avance du timer de bob
            bobTimer += Time.deltaTime * bobSpeed;                                 // Le timer s'écoule plus ou moins vite selon l'état

<<<<<<< Updated upstream
            // Calcul du bob en X et Y
            float bobY = Mathf.Sin(bobTimer) * bobAmount;                         // Mouvement vertical : monte et descend (sinus)
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;          // Mouvement latéral : balancement gauche/droite (cosinus demi-fréquence)
=======
            // Calcule les offsets de bobbing pour les axes X et Y afin de créer un mouvement de va-et-vient
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
>>>>>>> Stashed changes

            finalOffset += new Vector3(bobX, bobY, 0f);                           // On ajoute le bob à l'offset de base
        }
        else
        {
            // Reset du timer de bob pour éviter les sauts lors de la reprise du mouvement
            bobTimer = 0f;                                                         // Repart de zéro pour un bob propre au prochain mouvement
        }

<<<<<<< Updated upstream
        // Position finale du bras : position de la caméra + offset transformé
=======
        // Positionne le bras en fonction de la position de la caméra et de l'offset calculé
>>>>>>> Stashed changes
        transform.position =
            cameraTransform.position +
            cameraTransform.TransformDirection(finalOffset);                       // TransformDirection convertit l'offset local en coordonnées monde
    }
}
