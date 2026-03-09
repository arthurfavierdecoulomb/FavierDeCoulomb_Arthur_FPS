using UnityEngine;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private WaveManager waveManager;

    [Header("Couleur")]
    [SerializeField] private Color normalColor = Color.white;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.2f;

    private int lastWave = -1;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    void Start()
    {
        if (waveText != null)
        {
            originalPosition = waveText.transform.localPosition;
            waveText.color = normalColor;
            waveText.text = "00";
        }
    }

    void Update()
    {
        if (waveText == null || waveManager == null) return;

        int currentWave = waveManager.GetCurrentWave();

        // Détecte le changement de vague → met à jour le texte + tremblement
        if (currentWave != lastWave && currentWave > 0)
        {
            waveText.text = currentWave.ToString("D2");
            TriggerShake();
            lastWave = currentWave;
        }

        // Gestion du tremblement
        if (isShaking)
        {
            shakeTimer += Time.deltaTime;
            if (shakeTimer < shakeDuration)
            {
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );
                waveText.transform.localPosition = originalPosition + shakeOffset;
            }
            else
            {
                isShaking = false;
                waveText.transform.localPosition = originalPosition;
            }
        }
    }

    private void TriggerShake()
    {
        isShaking = true;
        shakeTimer = 0f;
    }
}