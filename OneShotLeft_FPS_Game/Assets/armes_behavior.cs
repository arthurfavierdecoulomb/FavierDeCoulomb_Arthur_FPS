using UnityEngine;



public class ArmFollowCamera : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Position du bras")]
    [SerializeField] private Vector3 localOffset = new Vector3(0.5f, -0.3f, 0.5f);

    [Header("Correction de rotation")]
    [SerializeField] private Vector3 rotationCorrection;

    [Header("Arm Bob")]
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.03f;

    [SerializeField] private float sprintBobSpeed = 12f;
    [SerializeField] private float sprintBobAmount = 0.06f;

    [SerializeField] private float crouchBobAmount = 0.01f;

    private float bobTimer;

    
    void Start()
    {
        // Si les références ne sont pas assignées dans l'inspecteur, tente de les trouver automatiquement
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Ne pas bouger les bras si la souris n'est pas verrouillée
        if (Cursor.lockState != CursorLockMode.Locked) return;

    }

    
    void LateUpdate()
    {
        // Vérification des références
        if (cameraTransform == null || playerMovement == null) return;

        // Rotation du bras pour suivre la caméra + correction
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(rotationCorrection);

        // Position de base du bras
        Vector3 finalOffset = localOffset;

        // Bobbing du bras
        bool isMoving = playerMovement.GetCurrentSpeed() > 0.1f &&
                        playerMovement.IsGrounded();

        // Choix du bob selon l'état du joueur
        if (isMoving)
        {
            float bobSpeed;
            float bobAmount;

            // Priorité : accroupi > sprint > marche
            if (playerMovement.IsCrouching())
            {
                bobSpeed = walkBobSpeed;
                bobAmount = crouchBobAmount;
            }
            else if (playerMovement.IsSprinting())
            {
                bobSpeed = sprintBobSpeed;
                bobAmount = sprintBobAmount;
            }
            else
            {
                bobSpeed = walkBobSpeed;
                bobAmount = walkBobAmount;
            }

            // Avance du timer de bob
            bobTimer += Time.deltaTime * bobSpeed;

            // Calcul du bob en X et Y
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;

            finalOffset += new Vector3(bobX, bobY, 0f);
        }
        else
        {
            // Reset du timer de bob pour éviter les sauts lors de la reprise du mouvement
            bobTimer = 0f;
        }

        // Position finale du bras : position de la caméra + offset transformé
        transform.position =
            cameraTransform.position +
            cameraTransform.TransformDirection(finalOffset);
    }
}
