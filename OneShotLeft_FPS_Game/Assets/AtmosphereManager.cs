using UnityEngine;

<<<<<<< Updated upstream
public class AtmosphereManager : MonoBehaviour                                      // Gère le son d'ambiance pendant et après la génération de la map
=======
public class AtmosphereManager : MonoBehaviour
>>>>>>> Stashed changes
{
    // header et tooltip pour l'inspecteur afin de faciliter la configuration du son d'ambiance
    [Header("Son d'ambiance")]
    [Tooltip("Clip audio joué en boucle pendant et après la génération")]
    [SerializeField] private AudioClip atmosphereClip;                              // Clip audio à assigner dans l'Inspector
    [SerializeField][Range(0f, 1f)] private float volume = 0.6f;                   // Volume cible du son d'ambiance (entre 0 et 1)
    [SerializeField] private float fadeInTime = 2f;                                 // Durée du fondu d'entrée et de sortie en secondes

    private AudioSource audioSource;                                                // Composant audio utilisé pour jouer le clip

<<<<<<< Updated upstream
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();                                  // Récupère l'AudioSource si elle existe déjà sur le GameObject
=======
    private AudioSource audioSource;

    
    void Awake()
    {
        // Assure qu'il y a un AudioSource sur ce GameObject
        audioSource = GetComponent<AudioSource>();
>>>>>>> Stashed changes
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();                   // Sinon en crée une automatiquement

<<<<<<< Updated upstream
        audioSource.clip = atmosphereClip;                                          // Assigne le clip à jouer
        audioSource.loop = true;                                                    // Le son tourne en boucle indéfiniment
        audioSource.playOnAwake = false;                                            // Ne démarre pas automatiquement au lancement
        audioSource.spatialBlend = 0f;                                              // Son 2D (pas d'atténuation spatiale, entendu partout)
        audioSource.volume = 0f;                                                    // Démarre silencieux — le fade in gèrera le volume
    }

=======
        // Configure l'AudioSource pour jouer le clip d'ambiance
        audioSource.clip = atmosphereClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // garde le son sur le meme volume partout
        audioSource.volume = 0f;
    }

    
>>>>>>> Stashed changes
    public void StartAtmosphere()
    {
        // Vérifie que le clip d'ambiance est assigné, securité pour éviter les erreurs à l'exécution
        if (atmosphereClip == null)
        {
            Debug.LogWarning("AtmosphereManager: aucun clip assigné !");            // M'avertit si le clip a été oublié dans l'Inspector
            return;
        }

<<<<<<< Updated upstream
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
=======
        // Si le son est déjà en train de jouer, ne rien faire
        if (audioSource.isPlaying) return;

        // Démarre le son d'ambiance avec un fade in
        audioSource.volume = 0f;
        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    
    public void StopAtmosphere()
    {
        // Si le son n'est pas en train de jouer, ne rien faire
        if (!audioSource.isPlaying) return;
        StartCoroutine(FadeOut());
    }

    
    System.Collections.IEnumerator FadeIn()
    {
        // Fait un fade in du volume du son d'ambiance sur la durée spécifiée
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            // Incrémente le temps écoulé et ajuste le volume en conséquence
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeInTime);
            yield return null;
        }
        // Assure que le volume est à la valeur finale
        audioSource.volume = volume;
>>>>>>> Stashed changes
    }

    System.Collections.IEnumerator FadeOut()
    {
<<<<<<< Updated upstream
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
=======
        // meme principe que le fade in mais en sens inverse, on diminue le volume jusqu'à 0 avant d'arrêter le son
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
>>>>>>> Stashed changes
    }
}