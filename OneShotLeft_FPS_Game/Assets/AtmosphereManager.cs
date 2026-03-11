using UnityEngine;

public class AtmosphereManager : MonoBehaviour                                      // Gère le son d'ambiance pendant et après la génération de la map
{
    [Header("Son d'ambiance")]
    [Tooltip("Clip audio joué en boucle pendant et après la génération")]
    [SerializeField] private AudioClip atmosphereClip;                              // Clip audio à assigner dans l'Inspector
    [SerializeField][Range(0f, 1f)] private float volume = 0.6f;                   // Volume cible du son d'ambiance (entre 0 et 1)
    [SerializeField] private float fadeInTime = 2f;                                 // Durée du fondu d'entrée et de sortie en secondes

    private AudioSource audioSource;                                                // Composant audio utilisé pour jouer le clip

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();                                  // Récupère l'AudioSource si elle existe déjà sur le GameObject
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();                   // Sinon en crée une automatiquement

        audioSource.clip = atmosphereClip;                                          // Assigne le clip à jouer
        audioSource.loop = true;                                                    // Le son tourne en boucle indéfiniment
        audioSource.playOnAwake = false;                                            // Ne démarre pas automatiquement au lancement
        audioSource.spatialBlend = 0f;                                              // Son 2D (pas d'atténuation spatiale, entendu partout)
        audioSource.volume = 0f;                                                    // Démarre silencieux — le fade in gèrera le volume
    }

    public void StartAtmosphere()
    {
        if (atmosphereClip == null)
        {
            Debug.LogWarning("AtmosphereManager: aucun clip assigné !");            // M'avertit si le clip a été oublié dans l'Inspector
            return;
        }

        if (audioSource.isPlaying) return;                                          // Évite de relancer le son s'il est déjà en cours

        audioSource.volume = 0f;                                                    // Repart de zéro pour un fade in propre
        audioSource.Play();                                                         // Lance la lecture
        StartCoroutine(FadeIn());                                                   // Démarre le fondu d'entrée progressif
    }

    public void StopAtmosphere()
    {
        if (!audioSource.isPlaying) return;                                         // Ne fait rien si le son est déjà arrêté

        StartCoroutine(FadeOut());                                                  // Lance le fondu de sortie avant d'arrêter
    }

    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;                                                         // Temps écoulé depuis le début du fade

        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;                                              // Avance le compteur à chaque frame
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeInTime);    // Interpolation linéaire de 0 vers le volume cible
            yield return null;                                                      // Attend la prochaine frame
        }

        audioSource.volume = volume;                                                // Force la valeur finale pour éviter les imprécisions flottantes
    }

    System.Collections.IEnumerator FadeOut()
    {
        float startVol = audioSource.volume;                                        // Capture le volume actuel comme point de départ
        float elapsed = 0f;                                                         // Temps écoulé depuis le début du fade

        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;                                              // Avance le compteur à chaque frame
            audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeInTime);  // Interpolation linéaire du volume actuel vers 0
            yield return null;                                                      // Attend la prochaine frame
        }

        audioSource.Stop();                                                         // Arrête la lecture une fois le volume à zéro
        audioSource.volume = 0f;                                                    // Remet proprement le volume à zéro
    }
}