using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour                                                 // Affiche les munitions en UI et d√©clenche effets visuels/sonores
{
    // celui ci est diffÈrent du script de comportement des armes,
    // il est spÈcifiquement dÈdiÈ ‡ la gestion de l'affichage du
    // nombre de munitions et de ses effets visuels (flash rouge et tremblement)
    [Header("References")]
    [SerializeField] private TextMeshProUGUI ammoText;                              // Texte TMP qui affiche le compteur de munitions
    [SerializeField] private WeaponController weaponController;                     // R√©f√©rence au WeaponController pour lire les munitions

    [Header("Flash Settings")]
<<<<<<< Updated upstream
    [SerializeField] private Color normalColor = Color.white;                       // Couleur du texte quand les munitions sont disponibles
    [SerializeField] private Color emptyColor = Color.red;                         // Couleur du flash quand les munitions sont √† z√©ro
    [SerializeField] private float flashSpeed = 5f;                                // Vitesse du flash (plus √©lev√© = plus rapide)
=======
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private float flashSpeed = 5f; 
>>>>>>> Stashed changes

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f;                            // Amplitude du tremblement en pixels
    [SerializeField] private float shakeDuration = 0.2f;                          // Dur√©e du tremblement en secondes

    [Header("Son ‚Äî Flash")]
    [Tooltip("Son court jou√© √† chaque pic rouge ‚Äî id√©alement un clic sec ou bip d'erreur")]
    [SerializeField] private AudioClip flashSound;                                  // Son jou√© en sync avec chaque pic rouge du flash
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;         // Volume du son de flash, √† √©quilibrer avec les autres sons

    private AudioSource audioSource;                                                // AudioSource pour jouer le son de flash
    private bool wasAtPeak = false;                                                 // D√©tecte le pic du PingPong pour jouer le son une seule fois par cycle

    private int lastBulletCount = -1;                                             // Dernier nombre de munitions connu ‚Äî d√©tecte les changements
    private bool isFlashing = false;                                          // Indique si le flash est actif
    private float flashTimer = 0f;                                             // Timer qui fait avancer l'animation de flash
    private Vector3 originalPosition;                                               // Position d'origine du texte avant tremblement
    private float shakeTimer = 0f;                                             // Timer du tremblement
    private bool isShaking = false;                                          // Indique si le tremblement est actif

    void Start()
    {
        // Assure que les rÈfÈrences sont assignÈes
        if (weaponController != null)
<<<<<<< Updated upstream
            lastBulletCount = weaponController.GetCurrentBullets();                 // Initialise le compteur pour √©viter un faux d√©clenchement au d√©marrage
=======
        {
            // Initialise le nombre de munitions affichÈ
            lastBulletCount = weaponController.GetCurrentBullets();
        }
>>>>>>> Stashed changes

        if (ammoText != null)
            originalPosition = ammoText.transform.localPosition;                   // Stocke la position de repos du texte pour le tremblement

        audioSource = gameObject.AddComponent<AudioSource>();          // Cr√©e l'AudioSource dynamiquement
        audioSource.playOnAwake = false;                                           // Ne joue pas automatiquement
        audioSource.spatialBlend = 0f;                                              // Son 2D ‚Äî entendu partout, pas d'att√©nuation spatiale
        audioSource.loop = false;                                           // Son ponctuel, pas en boucle
    }

    void Update()
    {
<<<<<<< Updated upstream
        if (ammoText == null || weaponController == null) return;                   // S√©curit√© : √©vite les erreurs si les r√©f√©rences manquent
=======
        // Assure que les rÈfÈrences sont valides avant de continuer
        if (ammoText == null || weaponController == null) return;
>>>>>>> Stashed changes

        int currentBullets = weaponController.GetCurrentBullets();                 // Lit le nombre de munitions actuel

<<<<<<< Updated upstream
        if (currentBullets < lastBulletCount) TriggerShake();                      // D√©clenche le tremblement √† chaque tir
=======
        // DÈtecte une diminution du nombre de munitions pour dÈclencher le tremblement
        if (currentBullets < lastBulletCount)
        {
            TriggerShake();
        }
>>>>>>> Stashed changes

        ammoText.text = "0" + currentBullets.ToString();                           // Affiche avec un z√©ro devant (ex: 01, 00)

<<<<<<< Updated upstream
=======
        // DÈtecte le passage ‡ zÈro munitions pour dÈclencher le flash rouge
>>>>>>> Stashed changes
        if (currentBullets == 0 && lastBulletCount > 0)
        {
            isFlashing = true;                                                      // Active le flash au moment exact o√π les munitions passent √† 0
            flashTimer = 0f;                                                        // Repart du d√©but pour un cycle propre
        }

<<<<<<< Updated upstream
        lastBulletCount = currentBullets;                                           // Met √† jour pour la prochaine frame

=======
        // Gestion du flash rouge
>>>>>>> Stashed changes
        if (currentBullets == 0)
        {
            if (isFlashing)
            {
<<<<<<< Updated upstream
                flashTimer += Time.deltaTime * flashSpeed;                          // Avance le timer du flash
                float pingPong = Mathf.PingPong(flashTimer, 1f);                   // Valeur oscillante entre 0 et 1
                ammoText.color = Color.Lerp(normalColor, emptyColor, pingPong);    // Interpolation de couleur blanc ‚Üí rouge ‚Üí blanc

                bool atPeak = pingPong > 0.95f;                                    // D√©tecte le pic rouge (proche de 1)
                if (atPeak && !wasAtPeak && flashSound != null)
                    audioSource.PlayOneShot(flashSound, flashSoundVolume);          // Joue le son une seule fois par pic
                wasAtPeak = atPeak;                                                 // M√©morise l'√©tat du pic pour √©viter les r√©p√©titions
            }
            else
            {
                ammoText.color = emptyColor;                                        // Reste rouge fixe si le flash n'est pas actif
=======
                flashTimer += Time.deltaTime * flashSpeed;
                // Lerp entre la couleur normale et la couleur vide pour crÈer un effet de flash pulsant
                ammoText.color = Color.Lerp(normalColor, emptyColor, Mathf.PingPong(flashTimer, 1f));
            }
            else
            {
                // Si pour une raison quelconque le flash n'est pas actif, assure que la couleur est rouge
                ammoText.color = emptyColor;
>>>>>>> Stashed changes
            }
        }
        else
        {
<<<<<<< Updated upstream
            isFlashing = false;                                                 // D√©sactive le flash d√®s que les munitions reviennent
            wasAtPeak = false;                                                 // R√©initialise le d√©tecteur de pic
            ammoText.color = normalColor;                                           // Remet la couleur normale
=======
            // Si le nombre de munitions est supÈrieur ‡ zÈro, arrÍte le flash et rÈtablit la couleur normale
            isFlashing = false;
            ammoText.color = normalColor;
>>>>>>> Stashed changes
        }

        if (isShaking)
        {
<<<<<<< Updated upstream
            shakeTimer += Time.deltaTime;                                           // Avance le timer du tremblement
            if (shakeTimer < shakeDuration)
            {
                ammoText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);             // D√©placement al√©atoire pour l'effet de tremblement
=======
            // IncrÈmente le timer de tremblement
            shakeTimer += Time.deltaTime;

            // Tant que le timer de tremblement est infÈrieur ‡ la durÈe dÈfinie, applique un tremblement alÈatoire
            if (shakeTimer < shakeDuration)
            {
                // GÈnËre un offset de tremblement alÈatoire dans les axes X et Y
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );
                // Applique l'offset de tremblement ‡ la position du texte
                ammoText.transform.localPosition = originalPosition + shakeOffset;
>>>>>>> Stashed changes
            }
            else
            {
                isShaking = false;                                                  // Fin du tremblement
                ammoText.transform.localPosition = originalPosition;               // Remet le texte √† sa position d'origine
            }
        }
    }

<<<<<<< Updated upstream
    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }            // D√©marre le tremblement ‚Äî appel√© √† chaque tir
=======
    // MÈthode pour dÈclencher le tremblement du texte
    private void TriggerShake()
    {
        // RÈinitialise le timer de tremblement et active le tremblement
        isShaking = true;
        shakeTimer = 0f;
    }
>>>>>>> Stashed changes
}