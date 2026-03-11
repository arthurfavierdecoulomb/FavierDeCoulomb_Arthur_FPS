using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerFootsteps : MonoBehaviour                                        // Gère les sons de pas, de saut et d'atterrissage du joueur
{
    [Header("Sons de pas — Pool (4 sons)")]
    [SerializeField] private AudioClip[] footstepSounds = new AudioClip[4];        // Pool de 4 sons tirés aléatoirement pour varier les pas

    [Header("Sons spéciaux")]
    [SerializeField] private AudioClip jumpSound;                                   // Son joué au moment du saut
    [SerializeField] private AudioClip landSound;                                   // Son joué à l'atterrissage

    [Header("Intervalles (secondes entre chaque pas)")]
    [SerializeField] private float walkInterval = 0.5f;                          // Temps entre deux pas en marche normale
    [SerializeField] private float sprintInterval = 0.3f;                          // Temps entre deux pas en sprint (plus rapide)
    [SerializeField] private float crouchInterval = 0.7f;                          // Temps entre deux pas accroupi (plus lent)

    [Header("Volumes")]
    [SerializeField][Range(0f, 1f)] private float walkVolume = 0.8f;             // Volume des pas en marche
    [SerializeField][Range(0f, 1f)] private float sprintVolume = 1.0f;             // Volume des pas en sprint (plus fort)
    [SerializeField][Range(0f, 1f)] private float crouchVolume = 0.3f;             // Volume des pas accroupi (discret)
    [SerializeField][Range(0f, 1f)] private float jumpVolume = 0.9f;             // Volume du son de saut
    [SerializeField][Range(0f, 1f)] private float landVolume = 1.0f;             // Volume de l'atterrissage (impact franc)

    private AudioSource stepsSource;                                                // Source dédiée aux pas — évite les conflits avec les FX
    private AudioSource fxSource;                                                   // Source dédiée aux sons ponctuels (saut, atterrissage)

    private PlayerMovement movement;                                                // Référence au script de mouvement pour lire l'état du joueur
    private float stepTimer = 0f;                                                // Timer qui contrôle l'intervalle entre deux pas
    private bool wasGrounded = true;                                              // État du sol à la frame précédente — détecte l'atterrissage
    private int lastStepIndex = -1;                                               // Index du dernier son joué — évite deux fois le même de suite

    void Start()
    {
        movement = GetComponent<PlayerMovement>();                                  // Récupère PlayerMovement sur le même GameObject

        stepsSource = gameObject.AddComponent<AudioSource>();          // Crée la source des pas dynamiquement
        stepsSource.playOnAwake = false;                                           // Ne joue pas au démarrage
        stepsSource.spatialBlend = 0f;                                              // Son 2D — pas d'atténuation spatiale
        stepsSource.loop = false;                                           // Pas en boucle — géré manuellement par le timer

        fxSource = gameObject.AddComponent<AudioSource>();             // Crée la source des FX dynamiquement
        fxSource.playOnAwake = false;                                              // Ne joue pas au démarrage
        fxSource.spatialBlend = 0f;                                                 // Son 2D
        fxSource.loop = false;                                              // Son ponctuel
    }

    void Update()
    {
        bool grounded = movement.IsGrounded();                                     // Vrai si le joueur touche le sol
        bool crouching = movement.IsCrouching();                                    // Vrai si le joueur est accroupi
        bool sprinting = movement.IsSprinting();                                    // Vrai si le joueur est en sprint
        bool moving = HasHorizontalInput();                                      // Vrai si une touche de déplacement est pressée

        if (!wasGrounded && grounded)
            PlayFX(landSound, landVolume);                                          // Détecte l'atterrissage : était en l'air, maintenant au sol
        wasGrounded = grounded;                                                     // Mémorise l'état pour la prochaine frame

        if (grounded && moving)
        {
            float interval = crouching ? crouchInterval
                           : sprinting ? sprintInterval
                           : walkInterval;                                          // Choisit l'intervalle selon l'état du joueur

            stepTimer += Time.deltaTime;                                            // Avance le timer de pas

            if (stepTimer >= interval && !stepsSource.isPlaying)
            {
                stepTimer = 0f;                                                     // Remet le timer à zéro pour le prochain pas
                float vol = crouching ? crouchVolume
                          : sprinting ? sprintVolume
                          : walkVolume;                                             // Choisit le volume selon l'état du joueur
                PlayStep(vol);                                                      // Joue le prochain pas
            }
        }
        else
        {
            if (stepsSource.isPlaying)
                stepsSource.Stop();                                                 // Coupe le son proprement si le joueur s'arrête ou saute
            stepTimer = 0f;                                                         // Remet le timer à zéro pour un départ propre
        }
    }

    public void OnJump()
    {
        PlayFX(jumpSound, jumpVolume);                                              // Appelé par PlayerMovement au moment exact du saut
    }

    bool HasHorizontalInput()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
               Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;                     // Détecte toute entrée de déplacement (WASD ou joystick)
    }

    void PlayStep(float volume)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;          // Sécurité : ne fait rien si le pool est vide

        int index;
        int attempts = 0;
        do
        {
            index = Random.Range(0, footstepSounds.Length);                        // Tire un index aléatoire dans le pool
            attempts++;
        } while (index == lastStepIndex && footstepSounds.Length > 1 && attempts < 10); // Évite de rejouer le même son deux fois de suite

        lastStepIndex = index;                                                      // Mémorise l'index joué

        if (footstepSounds[index] == null) return;                                 // Sécurité : ignore les slots vides dans le pool

        stepsSource.clip = footstepSounds[index];                                // Assigne le clip à jouer
        stepsSource.volume = volume;                                                // Applique le volume selon l'état
        stepsSource.Play();                                                         // Play() et non PlayOneShot — garantit un seul son actif à la fois
    }

    void PlayFX(AudioClip clip, float volume)
    {
        if (fxSource == null || clip == null) return;                              // Sécurité : ignore si la source ou le clip est manquant
        fxSource.PlayOneShot(clip, volume);                                        // PlayOneShot pour les FX — peuvent se superposer si nécessaire
    }
}