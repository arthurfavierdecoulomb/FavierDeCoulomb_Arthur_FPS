using UnityEngine;
using TMPro;

// conrtrairement au script bullet_behavior, ce script est attaché à un UI TextMeshPro qui affiche le nombre de munitions restantes. Il écoute les
// changements de munitions dans le WeaponController pour déclencher
// des effets visuels et sonores lorsque les munitions sont vides ou lorsqu'on tire.
public class AmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private WeaponController weaponController;

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private float flashSpeed = 5f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Son — Flash")]
    [Tooltip("Son joué à chaque pic rouge du flash (munitions vides)")] // c'est pratique pour rappeler à l'équipe de designer que ce son doit être court et percutant,
                                                                        // idéalement un "clic sec" ou un "bip d'erreur", pas une longue alerte qui pourrait devenir agaçante
    [SerializeField] private AudioClip flashSound;
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f; // volume du son de flash, ajustable dans l'inspecteur pour
                                                                           // trouver le bon équilibre avec les autres sons du jeu

    private AudioSource audioSource;
    private bool wasAtPeak = false;

    // Suivi du nombre de munitions pour détecter les changements
    private int lastBulletCount = -1;
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    
    void Start()
    {
        // Initialisation du texte et du compteur de munitions
        if (weaponController != null)
            lastBulletCount = weaponController.GetCurrentBullets();

        // Stocke la position originale du texte pour le tremblement
        if (ammoText != null)
            originalPosition = ammoText.transform.localPosition;

        // Assure qu'il y a un AudioSource sur ce GameObject, sinon en ajoute un
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }

    
    void Update()
    {
        // Vérifie les références avant de continuer
        if (ammoText == null || weaponController == null) return;

        int currentBullets = weaponController.GetCurrentBullets();

        if (currentBullets < lastBulletCount) TriggerShake();

        ammoText.text = "0" + currentBullets.ToString();

        // Détecte le passage à 0 munitions pour déclencher le flash
        if (currentBullets == 0 && lastBulletCount > 0)
        {
            isFlashing = true;
            flashTimer = 0f;
        }

        lastBulletCount = currentBullets;

        // Flash + son synchronisé au pic
        if (currentBullets == 0)
        {
            if (isFlashing)
            {
                // Avance le timer de flash
                flashTimer += Time.deltaTime * flashSpeed;
                float pingPong = Mathf.PingPong(flashTimer, 1f);
                ammoText.color = Color.Lerp(normalColor, emptyColor, pingPong);

                // Son au pic rouge — une seule fois par cycle
                bool atPeak = pingPong > 0.95f;
                if (atPeak && !wasAtPeak && flashSound != null)
                    audioSource.PlayOneShot(flashSound, flashSoundVolume);
                wasAtPeak = atPeak;
            }
            else
            {
                // Si pour une raison quelconque le flash n'est pas actif alors que les munitions sont à 0, s'assure que le texte est rouge
                ammoText.color = emptyColor;
            }
        }
        else
        {

            // Réinitialise le flash si les munitions sont rechargées ou si on tire
            isFlashing = false;
            wasAtPeak = false;
            ammoText.color = normalColor;
        }

        // Tremblement lorsqu'on tire, avec une durée limitée
        if (isShaking)
        {
            // Avance le timer de tremblement
            shakeTimer += Time.deltaTime;
            if (shakeTimer < shakeDuration)
            {
                // Applique un déplacement aléatoire au texte pour créer l'effet de tremblement
                ammoText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);
            }
            else
            {
                // Fin du tremblement, réinitialise la position du texte
                isShaking = false;
                ammoText.transform.localPosition = originalPosition;
            }
        }
    }


    // Permet de déclencher le tremblement depuis d'autres scripts, comme le WeaponController lorsqu'on tire
    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }
}