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

    [Header("Son — Flash")]
    [Tooltip("Son joué à chaque pic rouge du flash")]
    [SerializeField] private AudioClip flashSound;
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;

    private AudioSource audioSource;
    private bool wasAtPeak = false; // détecte le pic du PingPong

    private bool isFlashing = false;
    private float flashTimer = 0f;
    private int previousHealth;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    void Start()
    {
        if (playerHealth != null)
            previousHealth = playerHealth.GetHealth();

        if (healthText != null)
            originalPosition = healthText.transform.localPosition;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }

    void Update()
    {
        if (healthText == null || playerHealth == null) return;

        int currentHealth = playerHealth.GetHealth();

        if (currentHealth < previousHealth) TriggerShake();
        previousHealth = currentHealth;

        healthText.text = Mathf.Clamp(currentHealth, 0, 100).ToString("D3");

        // Flash + son synchronisé
        if (currentHealth <= lowHealthThreshold && currentHealth > 0)
        {
            isFlashing = true;
            flashTimer += Time.deltaTime * flashSpeed;

            float pingPong = Mathf.PingPong(flashTimer, 1f);
            healthText.color = Color.Lerp(normalColor, lowHealthColor, pingPong);

            // Détecte le pic (proche de 1) — joue le son une seule fois par cycle
            bool atPeak = pingPong > 0.95f;
            if (atPeak && !wasAtPeak && flashSound != null)
                audioSource.PlayOneShot(flashSound, flashSoundVolume);
            wasAtPeak = atPeak;
        }
        else
        {
            isFlashing = false;
            wasAtPeak = false;
            flashTimer = 0f;
            healthText.color = normalColor;
        }

        // Tremblement
        if (isShaking)
        {
            shakeTimer += Time.deltaTime;
            if (shakeTimer < shakeDuration)
            {
                healthText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);
            }
            else
            {
                isShaking = false;
                healthText.transform.localPosition = originalPosition;
            }
        }
    }

    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }
}