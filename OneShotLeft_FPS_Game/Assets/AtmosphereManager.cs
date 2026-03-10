using UnityEngine;

/// <summary>
/// Joue un son d'ambiance en boucle dès que la génération procédurale démarre.
/// 
/// SETUP :
///   1. Ajoute ce script sur un GameObject vide "AtmosphereManager"
///   2. Assigne le clip audio dans l'Inspector
///   3. C'est tout — MapGenerator l'appelle automatiquement
/// </summary>
public class AtmosphereManager : MonoBehaviour
{
    [Header("Son d'ambiance")]
    [Tooltip("Clip audio joué en boucle pendant et après la génération")]
    [SerializeField] private AudioClip atmosphereClip;

    [SerializeField][Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private float fadeInTime = 2f;

    private AudioSource audioSource;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = atmosphereClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // son 2D
        audioSource.volume = 0f;
    }

    /// <summary>
    /// Appelé par MapGenerator au début de la génération.
    /// </summary>
    public void StartAtmosphere()
    {
        if (atmosphereClip == null)
        {
            Debug.LogWarning("AtmosphereManager: aucun clip assigné !");
            return;
        }

        if (audioSource.isPlaying) return;

        audioSource.volume = 0f;
        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Arrête l'ambiance avec un fade out.
    /// </summary>
    public void StopAtmosphere()
    {
        if (!audioSource.isPlaying) return;
        StartCoroutine(FadeOut());
    }

    // ─────────────────────────────────────────────────────────────────────
    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeInTime);
            yield return null;
        }
        audioSource.volume = volume;
    }

    System.Collections.IEnumerator FadeOut()
    {
        float startVol = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeInTime);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = 0f;
    }
}