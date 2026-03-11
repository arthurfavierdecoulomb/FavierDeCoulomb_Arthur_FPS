using UnityEngine;

// Gère la stamina du joueur : consommation au sprint et à l'accroupissement,
// régénération automatique après un délai d'inactivité, et seuil minimum d'utilisation.
public class StaminaSystem : MonoBehaviour
{
    // ─── Stamina Settings ─────────────────────────────────────────────────

    [Header("Stamina Settings")]

    // Valeur maximale de stamina atteignable par le joueur.
    [SerializeField] private float maxStamina = 100f;

    // Valeur courante de stamina, visible dans l'Inspector pour le debug en play mode.
    [SerializeField] private float currentStamina = 100f;


    // ─── Stamina Costs ────────────────────────────────────────────────────

    [Header("Stamina Costs")]

    // Quantité de stamina consommée par seconde pendant le sprint.
    [SerializeField] private float sprintCost = 2f;

    // Quantité de stamina consommée par seconde pendant l'accroupissement.
    [SerializeField] private float crouchCost = 1.5f;


    // ─── Stamina Regen ────────────────────────────────────────────────────

    [Header("Stamina Regen")]

    // Quantité de stamina récupérée par seconde lorsque le joueur est inactif.
    [SerializeField] private float regenRate = 5f;

    // Temps en secondes sans consommation avant que la régénération ne démarre.
    [SerializeField] private float regenDelay = 1f;


    // ─── Minimum Stamina ──────────────────────────────────────────────────

    [Header("Minimum Stamina")]

    // Seuil minimal en pourcentage (0–100) en dessous duquel sprint et accroupissement
    // sont interdits, évitant que le joueur reste bloqué à 0 stamina indéfiniment.
    [SerializeField] private float minStaminaPercent = 5f;


    // ─── Références privées ───────────────────────────────────────────────

    // Référence au composant de mouvement pour lire les états sprint et accroupissement.
    private PlayerMovement playerMovement;

    // Compteur de temps écoulé depuis la dernière consommation de stamina.
    // Sert à respecter le regenDelay avant de commencer à régénérer.
    private float timeSinceLastUse = 0f;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        // Récupère le PlayerMovement sur le même GameObject.
        playerMovement = GetComponent<PlayerMovement>();

        // Initialise la stamina au maximum au démarrage de la partie.
        currentStamina = maxStamina;
    }


    // ─── Mise à jour ──────────────────────────────────────────────────────

    void Update()
    {
        // Ne fait rien si le PlayerMovement est absent (évite une NullReferenceException).
        if (playerMovement == null) return;

        // Lit les états de mouvement depuis PlayerMovement à chaque frame.
        bool isSprinting = playerMovement.IsSprinting();
        bool isCrouching = playerMovement.IsCrouching();

        // ── Consommation ──────────────────────────────────────────────────

        if (isSprinting && currentStamina > 0)
        {
            // Réduit la stamina proportionnellement au temps écoulé pendant le sprint.
            currentStamina -= sprintCost * Time.deltaTime;

            // Réinitialise le compteur : la régénération ne peut pas démarrer pendant l'action.
            timeSinceLastUse = 0f;
        }
        else if (isCrouching && currentStamina > 0)
        {
            // Réduit la stamina proportionnellement au temps écoulé pendant l'accroupissement.
            currentStamina -= crouchCost * Time.deltaTime;

            // Réinitialise le compteur pour retarder la régénération.
            timeSinceLastUse = 0f;
        }
        else
        {
            // ── Régénération ──────────────────────────────────────────────

            // Incrémente le temps depuis la dernière utilisation à chaque frame inactive.
            timeSinceLastUse += Time.deltaTime;

            // Ne régénère qu'une fois le délai écoulé et si la stamina n'est pas pleine.
            if (timeSinceLastUse >= regenDelay && currentStamina < maxStamina)
            {
                currentStamina += regenRate * Time.deltaTime;
            }
        }

        // Garantit que la stamina reste dans l'intervalle [0, maxStamina] quoi qu'il arrive.
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }


    // ─── API publique (UI & autres systèmes) ─────────────────────────────

    // Retourne la valeur absolue de stamina courante (ex : pour remplir une barre UI).
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    // Retourne la valeur maximale de stamina (ex : pour calculer le ratio d'une barre UI).
    public float GetMaxStamina()
    {
        return maxStamina;
    }

    // Retourne true si le joueur possède encore de la stamina (> 0).
    public bool HasStamina()
    {
        return currentStamina > 0;
    }

    // Retourne true si le pourcentage de stamina dépasse le seuil minimum pour sprinter.
    // Empêche de lancer un sprint si la stamina est trop basse.
    public bool CanSprint()
    {
        float staminaPercent = (currentStamina / maxStamina) * 100f;
        return staminaPercent >= minStaminaPercent;
    }

    // Retourne true si le pourcentage de stamina dépasse le seuil minimum pour s'accroupir.
    // Utilise le même seuil que CanSprint(), modifiable indépendamment si besoin.
    public bool CanCrouch()
    {
        float staminaPercent = (currentStamina / maxStamina) * 100f;
        return staminaPercent >= minStaminaPercent;
    }

    // Retourne la stamina courante exprimée en pourcentage (0–100),
    // pratique pour piloter directement le fillAmount d'une barre UI normalisée.
    public float GetStaminaPercent()
    {
        return (currentStamina / maxStamina) * 100f;
    }
}