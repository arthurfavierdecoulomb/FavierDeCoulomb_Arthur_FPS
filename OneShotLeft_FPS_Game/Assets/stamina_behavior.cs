using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;

    [Header("Stamina Costs")]
    [SerializeField] private float sprintCost = 2f;      // Par seconde
    [SerializeField] private float crouchCost = 1.5f;    // Par seconde

    [Header("Stamina Regen")]
    [SerializeField] private float regenRate = 5f;       // Par seconde quand inactif
    [SerializeField] private float regenDelay = 1f;      // Délai avant de commencer à régénérer

    private PlayerMovement playerMovement;
    private float timeSinceLastUse = 0f;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (playerMovement == null) return;

        bool isSprinting = playerMovement.IsSprinting();
        bool isCrouching = playerMovement.IsCrouching();

        // Consommation de stamina
        if (isSprinting && currentStamina > 0)
        {
            currentStamina -= sprintCost * Time.deltaTime;
            timeSinceLastUse = 0f;
        }
        else if (isCrouching && currentStamina > 0)
        {
            currentStamina -= crouchCost * Time.deltaTime;
            timeSinceLastUse = 0f;
        }
        else
        {
            // Régénération après le délai
            timeSinceLastUse += Time.deltaTime;

            if (timeSinceLastUse >= regenDelay && currentStamina < maxStamina)
            {
                currentStamina += regenRate * Time.deltaTime;
            }
        }

        // Clamp la stamina entre 0 et max
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    // Méthodes publiques pour l'UI
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

    public bool HasStamina()
    {
        return currentStamina > 0;
    }
}