using UnityEngine;
using TMPro;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private float flashSpeed = 5f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Son — Flash")]
    [Tooltip("Son joué à chaque pic rouge du flash")]
    [SerializeField] private AudioClip flashSound;
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;

    private AudioSource audioSource;
    private bool wasAtPeak = false;

    private bool isFlashing = false;
    private float flashTimer = 0f;
    private float previousStamina;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    void Start()
    {
        if (staminaSystem != null)
            previousStamina = staminaSystem.GetCurrentStamina();

        if (staminaText != null)
            originalPosition = staminaText.transform.localPosition;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }

    void Update()
    {
        if (staminaText == null || staminaSystem == null) return;

        float currentStamina = staminaSystem.GetCurrentStamina();
        float maxStamina = staminaSystem.GetMaxStamina();
        float staminaPct = (currentStamina / maxStamina) * 100f;

        if (currentStamina < previousStamina) TriggerShake();
        previousStamina = currentStamina;

        staminaText.text = Mathf.Clamp(Mathf.RoundToInt(currentStamina), 0, 100).ToString("D3");

        // Flash + son synchronisé
        if (staminaPct <= 20f && staminaPct > 0f)
        {
            isFlashing = true;
            flashTimer += Time.deltaTime * flashSpeed;

            float pingPong = Mathf.PingPong(flashTimer, 1f);
            staminaText.color = Color.Lerp(normalColor, emptyColor, pingPong);

            // Détecte le pic — joue le son une seule fois par cycle
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
            staminaText.color = normalColor;
        }

        // Tremblement
        if (isShaking)
        {
            shakeTimer += Time.deltaTime;
            if (shakeTimer < shakeDuration)
            {
                staminaText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);
            }
            else
            {
                isShaking = false;
                staminaText.transform.localPosition = originalPosition;
            }
        }
    }

    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }
}