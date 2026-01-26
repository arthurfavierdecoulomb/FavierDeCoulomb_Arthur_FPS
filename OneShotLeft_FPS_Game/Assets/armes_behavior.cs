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
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null || playerMovement == null) return;

        // Rotation = caméra
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(rotationCorrection);

        Vector3 finalOffset = localOffset;

        // Conditions de mouvement
        bool isMoving = playerMovement.GetCurrentSpeed() > 0.1f &&
                        playerMovement.IsGrounded();

        if (isMoving)
        {
            float bobSpeed;
            float bobAmount;

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

            bobTimer += Time.deltaTime * bobSpeed;

            // Bob FPS classique
            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;

            finalOffset += new Vector3(bobX, bobY, 0f);
        }
        else
        {
            bobTimer = 0f;
        }

        // Position finale
        transform.position =
            cameraTransform.position +
            cameraTransform.TransformDirection(finalOffset);
    }
}
