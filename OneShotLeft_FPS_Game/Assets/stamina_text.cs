using UnityEngine;
using TMPro;

// Gère l'affichage de la stamina en UI : texte numérique, flash rouge synchronisé
// avec un son quand la stamina est critique, et tremblement à chaque perte de stamina.
public class StaminaUI : MonoBehaviour
{
    // ─── Références ───────────────────────────────────────────────────────

    [Header("References")]

    // Texte TMPro affichant la valeur numérique de la stamina (format "D3" → "075").
    [SerializeField] private TextMeshProUGUI staminaText;

    // Référence au système de stamina pour lire les valeurs courante et maximale.
    [SerializeField] private StaminaSystem staminaSystem;


    // ─── Flash Settings ───────────────────────────────────────────────────

    [Header("Flash Settings")]

    // Couleur du texte en état normal (stamina suffisante).
    [SerializeField] private Color normalColor = Color.white;

    // Couleur du texte au pic du flash (stamina critique).
    [SerializeField] private Color emptyColor = Color.red;

    // Vitesse du cycle de flash : valeur élevée = clignotement plus rapide.
    [SerializeField] private float flashSpeed = 5f;


    // ─── Shake Settings ───────────────────────────────────────────────────

    [Header("Shake Settings")]

    // Amplitude du tremblement en pixels lors d'une perte de stamina.
    [SerializeField] private float shakeIntensity = 10f;

    // Durée totale en secondes de chaque effet de tremblement.
    [SerializeField] private float shakeDuration = 0.2f;


    // ─── Son – Flash ──────────────────────────────────────────────────────

    [Header("Son — Flash")]

    // Son joué à chaque pic rouge du flash (stamina critique).
    [Tooltip("Son joué à chaque pic rouge du flash")]
    [SerializeField] private AudioClip flashSound;

    // Volume appliqué au son de flash (0 = muet, 1 = plein volume).
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;


    // ─── Variables privées ────────────────────────────────────────────────

    // Source audio 2D créée dynamiquement pour les sons d'interface.
    private AudioSource audioSource;

    // Mémorise si le PingPong était au pic lors de la frame précédente,
    // pour ne déclencher le son qu'une seule fois par cycle (front montant).
    private bool wasAtPeak = false;

    // Indique si le flash est actif (stamina ≤ 20 % et > 0).
    private bool isFlashing = false;

    // Accumulateur de temps pour l'animation PingPong du flash.
    private float flashTimer = 0f;

    // Valeur de stamina à la frame précédente, pour détecter une baisse.
    private float previousStamina;

    // Position locale initiale du texte, utilisée comme ancre du tremblement.
    private Vector3 originalPosition;

    // Accumulateur de temps pour la durée du tremblement en cours.
    private float shakeTimer = 0f;

    // Indique si un tremblement est actuellement en cours.
    private bool isShaking = false;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        // Mémorise la stamina initiale pour pouvoir détecter les baisses dès la première frame.
        if (staminaSystem != null)
            previousStamina = staminaSystem.GetCurrentStamina();

        // Mémorise la position locale du texte avant tout tremblement.
        if (staminaText != null)
            originalPosition = staminaText.transform.localPosition;

        // Crée une source audio 2D dédiée à l'UI (spatialBlend = 0 = pas de positionnement 3D).
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }


    // ─── Mise à jour ──────────────────────────────────────────────────────

    void Update()
    {
        // Ne fait rien si les références essentielles sont manquantes.
        if (staminaText == null || staminaSystem == null) return;

        // Récupère les valeurs courantes depuis StaminaSystem.
        float currentStamina = staminaSystem.GetCurrentStamina();
        float maxStamina = staminaSystem.GetMaxStamina();
        float staminaPct = (currentStamina / maxStamina) * 100f;

        // Déclenche un tremblement du texte dès que la stamina diminue.
        if (currentStamina < previousStamina) TriggerShake();

        // Mémorise la stamina courante pour la comparaison à la prochaine frame.
        previousStamina = currentStamina;

        // Affiche la stamina en entier sur 3 chiffres (ex : 75 → "075", 0 → "000").
        staminaText.text = Mathf.Clamp(Mathf.RoundToInt(currentStamina), 0, 100).ToString("D3");

        // ── Flash + son synchronisé ───────────────────────────────────────

        if (staminaPct <= 20f && staminaPct > 0f)
        {
            isFlashing = true;

            // Avance le timer du flash selon la vitesse configurée.
            flashTimer += Time.deltaTime * flashSpeed;

            // PingPong produit une valeur oscillant entre 0 et 1 en continu,
            // utilisée pour interpoler la couleur du texte entre normal et critique.
            float pingPong = Mathf.PingPong(flashTimer, 1f);
            staminaText.color = Color.Lerp(normalColor, emptyColor, pingPong);

            // Détecte le front montant du pic (pingPong > 0.95) pour jouer le son
            // une seule fois par cycle, sans le répéter tant que le pic est maintenu.
            bool atPeak = pingPong > 0.95f;
            if (atPeak && !wasAtPeak && flashSound != null)
                audioSource.PlayOneShot(flashSound, flashSoundVolume);

            // Mémorise l'état du pic pour la détection du front à la prochaine frame.
            wasAtPeak = atPeak;
        }
        else
        {
            // Hors zone critique : remet le texte à sa couleur normale et réinitialise le flash.
            isFlashing = false;
            wasAtPeak = false;
            flashTimer = 0f;
            staminaText.color = normalColor;
        }

        // ── Tremblement ───────────────────────────────────────────────────

        if (isShaking)
        {
            shakeTimer += Time.deltaTime;

            if (shakeTimer < shakeDuration)
            {
                // Déplace le texte aléatoirement autour de sa position d'origine.
                staminaText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);
            }
            else
            {
                // Fin du tremblement : remet le texte exactement à sa position initiale.
                isShaking = false;
                staminaText.transform.localPosition = originalPosition;
            }
        }
    }


    // ─── Déclenchement du tremblement ────────────────────────────────────

    // Démarre un nouveau tremblement en réinitialisant le timer.
    // Appelé automatiquement dès qu'une baisse de stamina est détectée.
    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }
}