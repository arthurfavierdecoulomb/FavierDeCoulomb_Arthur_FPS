using UnityEngine;
using TMPro;

public class HealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float flashSpeed = 5f;
    [SerializeField] private int lowHealthThreshold = 20;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.2f;

    private bool isFlashing = false;
    private float flashTimer = 0f;
    private int previousHealth;

    // Pour le tremblement
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    void Start()
    {
        if (playerHealth != null)
        {
            previousHealth = playerHealth.GetHealth();
        }

        // Sauvegarde la position d'origine du texte
        if (healthText != null)
        {
            originalPosition = healthText.transform.localPosition;
        }
    }

    void Update()
    {
        if (healthText == null || playerHealth == null) return;

        int currentHealth = playerHealth.GetHealth();

        // Détecte la perte de vie
        if (currentHealth < previousHealth)
        {
            TriggerShake();
        }
        previousHealth = currentHealth;

        // Affiche la vie avec formatage à 3 chiffres (000 à 100)
        int healthValue = Mathf.Clamp(currentHealth, 0, 100);
        healthText.text = healthValue.ToString("D3"); // D3 = 3 chiffres avec zéros

        // Gestion du flash quand la vie est basse
        if (currentHealth <= lowHealthThreshold)
        {
            isFlashing = true;
            flashTimer += Time.deltaTime * flashSpeed;
            healthText.color = Color.Lerp(normalColor, lowHealthColor, Mathf.PingPong(flashTimer, 1f));
        }
        else
        {
            isFlashing = false;
            healthText.color = normalColor;
        }

        // Gestion du tremblement
        if (isShaking)
        {
            shakeTimer += Time.deltaTime;

            if (shakeTimer < shakeDuration)
            {
                // Applique un tremblement aléatoire
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );
                healthText.transform.localPosition = originalPosition + shakeOffset;
            }
            else
            {
                // Fin du tremblement
                isShaking = false;
                healthText.transform.localPosition = originalPosition;
            }
        }
    }

    private void TriggerShake()
    {
        isShaking = true;
        shakeTimer = 0f;
    }
}