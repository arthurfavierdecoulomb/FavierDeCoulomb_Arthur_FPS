using UnityEngine;
using TMPro;

public class HealthUI : MonoBehaviour                                               // Affiche la vie du joueur en UI et déclenche effets visuels/sonores
{
    [Header("References")]
    [Tooltip("Texte TMP qui affiche le compteur de vie")]
    [SerializeField] private TextMeshProUGUI healthText;                            // Référence au composant texte UI
    [Tooltip("Script PlayerHealth à surveiller")]
    [SerializeField] private PlayerHealth playerHealth;                             // Source de données pour la vie actuelle

    [Header("Flash Settings")]
    [Tooltip("Couleur du texte quand la vie est normale")]
    [SerializeField] private Color normalColor = Color.white;                  // Couleur de repos du texte
    [Tooltip("Couleur du flash quand la vie est basse")]
    [SerializeField] private Color lowHealthColor = Color.red;                    // Couleur cible du flash d'alerte
    [Tooltip("Vitesse du flash — plus élevé = plus rapide")]
    [SerializeField] private float flashSpeed = 5f;                           // Contrôle la fréquence du PingPong
    [Tooltip("Seuil de vie en dessous duquel le flash s'active")]
    [SerializeField] private int lowHealthThreshold = 20;                         // Exprimé en points de vie (pas en pourcentage)

    [Header("Shake Settings")]
    [Tooltip("Amplitude du tremblement en pixels")]
    [SerializeField] private float shakeIntensity = 10f;                            // Plus élevé = tremblement plus violent
    [Tooltip("Durée du tremblement en secondes")]
    [SerializeField] private float shakeDuration = 0.2f;                          // Tremblement court pour un retour immédiat

    [Header("Son — Flash")]
    [Tooltip("Son court joué à chaque pic rouge du flash")]
    [SerializeField] private AudioClip flashSound;                                  // Idéalement un bip ou battement de cœur court
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;         // Volume du son de flash

    private AudioSource audioSource;                                                // AudioSource pour jouer le son de flash
    private bool wasAtPeak = false;                                           // Détecte le pic du PingPong pour jouer le son une seule fois par cycle

    private bool isFlashing = false;                                           // Vrai si le flash est actif
    private float flashTimer = 0f;                                              // Timer qui fait avancer l'animation de flash
    private int previousHealth;                                                   // Vie à la frame précédente — détecte les pertes
    private Vector3 originalPosition;                                               // Position de repos du texte avant tremblement
    private float shakeTimer = 0f;                                              // Timer du tremblement
    private bool isShaking = false;                                           // Vrai si le tremblement est actif

    void Start()
    {
        if (playerHealth != null)
            previousHealth = playerHealth.GetHealth();                              // Initialise pour éviter un faux déclenchement au démarrage

        if (healthText != null)
            originalPosition = healthText.transform.localPosition;                 // Stocke la position de repos pour le tremblement

        audioSource = gameObject.AddComponent<AudioSource>();          // Crée l'AudioSource dynamiquement
        audioSource.playOnAwake = false;                                           // Ne joue pas automatiquement
        audioSource.spatialBlend = 0f;                                              // Son 2D — entendu partout sans atténuation spatiale
        audioSource.loop = false;                                           // Son ponctuel, pas en boucle
    }

    void Update()
    {
        if (healthText == null || playerHealth == null) return;                     // Sécurité : ne fait rien si les références manquent

        int currentHealth = playerHealth.GetHealth();                               // Lit la vie actuelle du joueur

        if (currentHealth < previousHealth) TriggerShake();                        // Déclenche le tremblement à chaque perte de vie
        previousHealth = currentHealth;                                             // Met à jour pour la prochaine frame

        healthText.text = Mathf.Clamp(currentHealth, 0, 100).ToString("D3");      // Affiche sur 3 chiffres (ex: 075, 020, 000)

        if (currentHealth <= lowHealthThreshold && currentHealth > 0)
        {
            isFlashing = true;
            flashTimer += Time.deltaTime * flashSpeed;                             // Avance le timer du flash
            float pingPong = Mathf.PingPong(flashTimer, 1f);                       // Valeur oscillante entre 0 et 1
            healthText.color = Color.Lerp(normalColor, lowHealthColor, pingPong);  // Interpolation blanc → rouge → blanc

            bool atPeak = pingPong > 0.95f;                                        // Détecte le pic rouge (proche de 1)
            if (atPeak && !wasAtPeak && flashSound != null)
                audioSource.PlayOneShot(flashSound, flashSoundVolume);              // Joue le son une seule fois par pic
            wasAtPeak = atPeak;                                                     // Mémorise l'état pour éviter les répétitions
        }
        else
        {
            isFlashing = false;                                               // Désactive le flash si la vie remonte
            wasAtPeak = false;                                               // Réinitialise le détecteur de pic
            flashTimer = 0f;                                                  // Remet le timer à zéro pour un cycle propre au prochain flash
            healthText.color = normalColor;                                         // Remet la couleur normale
        }

        if (isShaking)
        {
            shakeTimer += Time.deltaTime;                                           // Avance le timer du tremblement
            if (shakeTimer < shakeDuration)
            {
                healthText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);             // Déplacement aléatoire pour l'effet de tremblement
            }
            else
            {
                isShaking = false;                                                  // Fin du tremblement
                healthText.transform.localPosition = originalPosition;             // Remet le texte exactement à sa position d'origine
            }
        }
    }

    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }            // Démarre le tremblement — appelé à chaque perte de vie
}