using UnityEngine;

// Gère le son d'ambiance pendant la génération de la map et après.
public class AtmosphereManager : MonoBehaviour
{
    // header et tooltip pour l'inspecteur afin de faciliter l'assignation du clip et la configuration du volume et du fade
    [Header("Son d'ambiance")]
    [Tooltip("Clip audio joué en boucle pendant et après la génération")]
    [SerializeField] private AudioClip atmosphereClip;

    [SerializeField][Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private float fadeInTime = 2f;

    private AudioSource audioSource;

    
    void Awake()
    {
        // Assure qu'il y a un AudioSource sur ce GameObject, sinon en ajoute un
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configuration de base de l'AudioSource
        audioSource.clip = atmosphereClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; 
        audioSource.volume = 0f;
    }

    
    public void StartAtmosphere()
    {
        // Vérifie que le clip est assigné avant de tenter de jouer
        if (atmosphereClip == null)
        {
            // Affiche un avertissement dans la console si aucun clip n'est assigné
            Debug.LogWarning("AtmosphereManager: aucun clip assigné !");
            return;
        }

        // Si l'ambiance est déjà en train de jouer, ne rien faire
        if (audioSource.isPlaying) return;

        audioSource.volume = 0f;
        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    
    public void StopAtmosphere()
    {
        // Si l'ambiance n'est pas en train de jouer, ne rien faire
        if (!audioSource.isPlaying) return;
        StartCoroutine(FadeOut());
    }

    
    System.Collections.IEnumerator FadeIn()
    {
        // Fait un fondu d'entrée du volume de 0 à la valeur définie sur une durée de fadeInTime
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            // Incrémente le temps écoulé et ajuste le volume en conséquence
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeInTime);
            yield return null;
        }
        audioSource.volume = volume;
    }


    System.Collections.IEnumerator FadeOut()
    {
        // pareil que FadeIn mais en sens inverse, de la valeur actuelle du volume à 0, puis arrête le son
        float startVol = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            // Incrémente le temps écoulé et ajuste le volume en conséquence
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeInTime);
            yield return null;
        }
        // Assure que le volume est à 0 et arrête le son
        audioSource.Stop();
        audioSource.volume = 0f;
    }
}