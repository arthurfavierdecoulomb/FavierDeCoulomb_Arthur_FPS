using UnityEngine;

public class AtmosphereManager : MonoBehaviour
{
    [Header("Son d'ambiance")]
    [Tooltip("Clip audio joué en boucle pendant et après la génération")]
    [SerializeField] private AudioClip atmosphereClip;
    [SerializeField][Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private float fadeInTime = 2f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = atmosphereClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
    }

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

    public void StopAtmosphere()
    {
        if (!audioSource.isPlaying) return;
        StartCoroutine(FadeOut());
    }

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