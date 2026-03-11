using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour                                                 // Affiche les munitions en UI et déclenche effets visuels/sonores
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI ammoText;                              // Texte TMP qui affiche le compteur de munitions
    [SerializeField] private WeaponController weaponController;                     // Référence au WeaponController pour lire les munitions

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = Color.white;                       // Couleur du texte quand les munitions sont disponibles
    [SerializeField] private Color emptyColor = Color.red;                         // Couleur du flash quand les munitions sont à zéro
    [SerializeField] private float flashSpeed = 5f;                                // Vitesse du flash (plus élevé = plus rapide)

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f;                            // Amplitude du tremblement en pixels
    [SerializeField] private float shakeDuration = 0.2f;                          // Durée du tremblement en secondes

    [Header("Son — Flash")]
    [Tooltip("Son court joué à chaque pic rouge — idéalement un clic sec ou bip d'erreur")]
    [SerializeField] private AudioClip flashSound;                                  // Son joué en sync avec chaque pic rouge du flash
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;         // Volume du son de flash, à équilibrer avec les autres sons

    private AudioSource audioSource;                                                // AudioSource pour jouer le son de flash
    private bool wasAtPeak = false;                                                 // Détecte le pic du PingPong pour jouer le son une seule fois par cycle

    private int lastBulletCount = -1;                                             // Dernier nombre de munitions connu — détecte les changements
    private bool isFlashing = false;                                          // Indique si le flash est actif
    private float flashTimer = 0f;                                             // Timer qui fait avancer l'animation de flash
    private Vector3 originalPosition;                                               // Position d'origine du texte avant tremblement
    private float shakeTimer = 0f;                                             // Timer du tremblement
    private bool isShaking = false;                                          // Indique si le tremblement est actif

    void Start()
    {
        if (weaponController != null)
            lastBulletCount = weaponController.GetCurrentBullets();                 // Initialise le compteur pour éviter un faux déclenchement au démarrage

        if (ammoText != null)
            originalPosition = ammoText.transform.localPosition;                   // Stocke la position de repos du texte pour le tremblement

        audioSource = gameObject.AddComponent<AudioSource>();          // Crée l'AudioSource dynamiquement
        audioSource.playOnAwake = false;                                           // Ne joue pas automatiquement
        audioSource.spatialBlend = 0f;                                              // Son 2D — entendu partout, pas d'atténuation spatiale
        audioSource.loop = false;                                           // Son ponctuel, pas en boucle
    }

    void Update()
    {
        if (ammoText == null || weaponController == null) return;                   // Sécurité : évite les erreurs si les références manquent

        int currentBullets = weaponController.GetCurrentBullets();                 // Lit le nombre de munitions actuel

        if (currentBullets < lastBulletCount) TriggerShake();                      // Déclenche le tremblement à chaque tir

        ammoText.text = "0" + currentBullets.ToString();                           // Affiche avec un zéro devant (ex: 01, 00)

        if (currentBullets == 0 && lastBulletCount > 0)
        {
            isFlashing = true;                                                      // Active le flash au moment exact où les munitions passent à 0
            flashTimer = 0f;                                                        // Repart du début pour un cycle propre
        }

        lastBulletCount = currentBullets;                                           // Met à jour pour la prochaine frame

        if (currentBullets == 0)
        {
            if (isFlashing)
            {
                flashTimer += Time.deltaTime * flashSpeed;                          // Avance le timer du flash
                float pingPong = Mathf.PingPong(flashTimer, 1f);                   // Valeur oscillante entre 0 et 1
                ammoText.color = Color.Lerp(normalColor, emptyColor, pingPong);    // Interpolation de couleur blanc → rouge → blanc

                bool atPeak = pingPong > 0.95f;                                    // Détecte le pic rouge (proche de 1)
                if (atPeak && !wasAtPeak && flashSound != null)
                    audioSource.PlayOneShot(flashSound, flashSoundVolume);          // Joue le son une seule fois par pic
                wasAtPeak = atPeak;                                                 // Mémorise l'état du pic pour éviter les répétitions
            }
            else
            {
                ammoText.color = emptyColor;                                        // Reste rouge fixe si le flash n'est pas actif
            }
        }
        else
        {
            isFlashing = false;                                                 // Désactive le flash dès que les munitions reviennent
            wasAtPeak = false;                                                 // Réinitialise le détecteur de pic
            ammoText.color = normalColor;                                           // Remet la couleur normale
        }

        if (isShaking)
        {
            shakeTimer += Time.deltaTime;                                           // Avance le timer du tremblement
            if (shakeTimer < shakeDuration)
            {
                ammoText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);             // Déplacement aléatoire pour l'effet de tremblement
            }
            else
            {
                isShaking = false;                                                  // Fin du tremblement
                ammoText.transform.localPosition = originalPosition;               // Remet le texte à sa position d'origine
            }
        }
    }

    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }            // Démarre le tremblement — appelé à chaque tir
}