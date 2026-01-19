using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float crouchSpeed = 3f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

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
    private float currentHeight;
    private float targetHeight;
    private float currentCameraHeight;
    private float targetCameraHeight;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentHeight = standingHeight;
        targetHeight = standingHeight;
        controller.height = standingHeight;

        // Initialiser la hauteur de la caméra
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
        float speed = isCrouching ? crouchSpeed : walkSpeed;
        Vector3 move = transform.right * x + transform.forward * z;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Saut uniquement si debout et au sol
        if (Input.GetButtonDown("Jump") && !isCrouching && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Appliquer la gravité
        velocity.y += gravity * Time.deltaTime;

        // Mouvement
        Vector3 finalMove = move * speed + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);

        HandleCrouch();
        UpdateCameraHeight();
    }

    void HandleCrouch()
    {
        // Définir la hauteur cible
        if (Input.GetKey(KeyCode.LeftControl))
        {
            targetHeight = crouchHeight;
            targetCameraHeight = crouchCameraHeight;
            isCrouching = true;
        }
        else
        {
            // Vérifier s'il y a de la place pour se lever
            if (CanStandUp())
            {
                targetHeight = standingHeight;
                targetCameraHeight = standingCameraHeight;
                isCrouching = false;
            }
        }

        // Transition progressive de la hauteur
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float previousHeight = currentHeight;
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            // Ajuster le centre pour que les pieds restent au sol
            Vector3 center = controller.center;
            float heightDifference = currentHeight - previousHeight;
            center.y += heightDifference / 2f;

            controller.height = currentHeight;
            controller.center = center;
        }
    }

    void UpdateCameraHeight()
    {
        if (cameraTransform == null) return;

        // Transition fluide de la caméra
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
        // Raycast pour vérifier si on peut se lever
        float checkDistance = standingHeight - crouchHeight;
        Vector3 startPos = transform.position + Vector3.up * crouchHeight;

        return !Physics.Raycast(startPos, Vector3.up, checkDistance);
    }

    // Méthodes publiques pour le WeaponController
    public bool IsGrounded()
    {
        return controller.isGrounded;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public float GetCurrentSpeed()
    {
        return isCrouching ? crouchSpeed : walkSpeed;
    }
}