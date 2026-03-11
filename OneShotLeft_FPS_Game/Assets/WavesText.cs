using UnityEngine;
using TMPro;

// Affiche le numéro de vague en cours dans l'UI et déclenche un tremblement
// du texte à chaque changement de vague pour attirer l'attention du joueur.
public class WaveUI : MonoBehaviour
{
    // ─── Références ───────────────────────────────────────────────────────

    [Header("Références")]

    // Texte TMPro affichant le numéro de vague courant (format "D2" → "01", "05"...).
    [SerializeField] private TextMeshProUGUI waveText;

    // Référence au WaveManager pour lire le numéro de vague en cours.
    [SerializeField] private WaveManager waveManager;


    // ─── Couleur ──────────────────────────────────────────────────────────

    [Header("Couleur")]

    // Couleur appliquée au texte en permanence.
    [SerializeField] private Color normalColor = Color.white;


    // ─── Shake Settings ───────────────────────────────────────────────────

    [Header("Shake Settings")]

    // Amplitude du tremblement en pixels lors d'un changement de vague.
    [SerializeField] private float shakeIntensity = 10f;

    // Durée totale en secondes de l'effet de tremblement.
    [SerializeField] private float shakeDuration = 0.2f;


    // ─── Variables privées ────────────────────────────────────────────────

    // Mémorise la vague affichée à la frame précédente pour détecter un changement.
    // Initialisée à -1 pour forcer une mise à jour dès la première frame.
    private int lastWave = -1;

    // Position locale initiale du texte, utilisée comme ancre du tremblement.
    private Vector3 originalPosition;

    // Accumulateur de temps pour la durée du tremblement en cours.
    private float shakeTimer = 0f;

    // Indique si un tremblement est actuellement en cours.
    private bool isShaking = false;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        if (waveText != null)
        {
            // Mémorise la position locale du texte avant tout tremblement.
            originalPosition = waveText.transform.localPosition;

            // Applique la couleur normale définie dans l'Inspector.
            waveText.color = normalColor;

            // Affiche "00" par défaut en attendant que le WaveManager démarre.
            waveText.text = "00";
        }
    }


    // ─── Mise à jour ──────────────────────────────────────────────────────

    void Update()
    {
        // Ne fait rien si les références essentielles sont manquantes.
        if (waveText == null || waveManager == null) return;

        int currentWave = waveManager.GetCurrentWave();

        // Détecte le changement de vague : met à jour le texte et déclenche le shake.
        // La condition currentWave > 0 évite une mise à jour parasite avant le démarrage.
        if (currentWave != lastWave && currentWave > 0)
        {
            // Affiche le numéro de vague sur 2 chiffres (ex : 3 → "03").
            waveText.text = currentWave.ToString("D2");

            // Déclenche le tremblement pour signaler visuellement le changement de vague.
            TriggerShake();

            // Mémorise la vague courante pour éviter de rejouer l'animation à la prochaine frame.
            lastWave = currentWave;
        }

        // ── Tremblement ───────────────────────────────────────────────────

        if (isShaking)
        {
            shakeTimer += Time.deltaTime;

            if (shakeTimer < shakeDuration)
            {
                // Déplace le texte aléatoirement autour de sa position d'origine.
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );
                waveText.transform.localPosition = originalPosition + shakeOffset;
            }
            else
            {
                // Fin du tremblement : remet le texte exactement à sa position initiale.
                isShaking = false;
                waveText.transform.localPosition = originalPosition;
            }
        }
    }


    // ─── Déclenchement du tremblement ────────────────────────────────────

    // Démarre un nouveau tremblement en réinitialisant le timer.
    // Appelé automatiquement à chaque changement de vague détecté.
    private void TriggerShake()
    {
        isShaking = true;
        shakeTimer = 0f;
    }
}