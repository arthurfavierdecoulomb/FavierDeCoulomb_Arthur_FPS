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

    [Header("Son — Flash")]
    [SerializeField] private AudioClip flashSound;
    [SerializeField][Range(0f, 1f)] private float flashSoundVolume = 0.7f;

    private AudioSource audioSource;
    private bool wasAtPeak = false;
    private int lastBulletCount = -1;
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    void Start()
    {
        if (weaponController != null)
            lastBulletCount = weaponController.GetCurrentBullets();

        if (ammoText != null)
            originalPosition = ammoText.transform.localPosition;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
    }

    void Update()
    {
        if (ammoText == null || weaponController == null) return;

        int currentBullets = weaponController.GetCurrentBullets();

        if (currentBullets < lastBulletCount)
            TriggerShake();

        ammoText.text = "0" + currentBullets.ToString();

        if (currentBullets == 0 && lastBulletCount > 0)
        {
            isFlashing = true;
            flashTimer = 0f;
        }

        lastBulletCount = currentBullets;

        if (currentBullets == 0)
        {
            if (isFlashing)
            {
                flashTimer += Time.deltaTime * flashSpeed;
                float pingPong = Mathf.PingPong(flashTimer, 1f);
                ammoText.color = Color.Lerp(normalColor, emptyColor, pingPong);

                bool atPeak = pingPong > 0.95f;
                if (atPeak && !wasAtPeak && flashSound != null)
                    audioSource.PlayOneShot(flashSound, flashSoundVolume);
                wasAtPeak = atPeak;
            }
            else
            {
                ammoText.color = emptyColor;
            }
        }
        else
        {
            isFlashing = false;
            wasAtPeak = false;
            ammoText.color = normalColor;
        }

        if (isShaking)
        {
            shakeTimer += Time.deltaTime;
            if (shakeTimer < shakeDuration)
            {
                ammoText.transform.localPosition = originalPosition + new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity), 0f);
            }
            else
            {
                isShaking = false;
                ammoText.transform.localPosition = originalPosition;
            }
        }
    }

    private void TriggerShake() { isShaking = true; shakeTimer = 0f; }
}