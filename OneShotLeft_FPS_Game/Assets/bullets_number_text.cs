using UnityEngine;
using TMPro;

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

    private int lastBulletCount = -1;
    private bool isFlashing = false;
    private float flashTimer = 0f;

    // Pour le tremblement
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    void Start()
    {
        if (weaponController != null)
        {
            lastBulletCount = weaponController.GetCurrentBullets();
        }

        // Sauvegarde la position d'origine du texte
        if (ammoText != null)
        {
            originalPosition = ammoText.transform.localPosition;
        }
    }

    void Update()
    {
        if (ammoText == null || weaponController == null) return;

        int currentBullets = weaponController.GetCurrentBullets();

        // Détecte la perte de munitions (tir)
        if (currentBullets < lastBulletCount)
        {
            TriggerShake();
        }

        // Format avec zéro devant (00, 01, 02, etc.)
        ammoText.text = "0" + currentBullets.ToString();

        // Détecte quand on passe à zéro
        if (currentBullets == 0 && lastBulletCount > 0)
        {
            isFlashing = true;
            flashTimer = 0f;
        }
        lastBulletCount = currentBullets;

        // Gère le flash rouge
        if (currentBullets == 0)
        {
            if (isFlashing)
            {
                flashTimer += Time.deltaTime * flashSpeed;
                // Ping-pong entre normalColor et emptyColor
                ammoText.color = Color.Lerp(normalColor, emptyColor, Mathf.PingPong(flashTimer, 1f));
            }
            else
            {
                ammoText.color = emptyColor;
            }
        }
        else
        {
            isFlashing = false;
            ammoText.color = normalColor;
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
                ammoText.transform.localPosition = originalPosition + shakeOffset;
            }
            else
            {
                // Fin du tremblement
                isShaking = false;
                ammoText.transform.localPosition = originalPosition;
            }
        }
    }

    private void TriggerShake()
    {
        isShaking = true;
        shakeTimer = 0f;
    }
}