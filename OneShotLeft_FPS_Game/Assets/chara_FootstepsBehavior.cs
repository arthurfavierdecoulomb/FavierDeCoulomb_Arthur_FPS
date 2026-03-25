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

<<<<<<< Updated upstream
    private AudioSource stepsSource;                                                // Source dédiée aux pas — évite les conflits avec les FX
    private AudioSource fxSource;                                                   // Source dédiée aux sons ponctuels (saut, atterrissage)
=======
    // AudioSource dédié aux sons de pas (en boucle)
    private AudioSource stepsSource;
    // AudioSource dédié aux sons ponctuels (jump, land, etc.)
    private AudioSource fxSource;
>>>>>>> Stashed changes

    private PlayerMovement movement;                                                // Référence au script de mouvement pour lire l'état du joueur
    private float stepTimer = 0f;                                                // Timer qui contrôle l'intervalle entre deux pas
    private bool wasGrounded = true;                                              // État du sol à la frame précédente — détecte l'atterrissage
    private int lastStepIndex = -1;                                               // Index du dernier son joué — évite deux fois le même de suite

<<<<<<< Updated upstream
=======
    
>>>>>>> Stashed changes
    void Start()
    {
        movement = GetComponent<PlayerMovement>();                                  // Récupère PlayerMovement sur le même GameObject

<<<<<<< Updated upstream
        stepsSource = gameObject.AddComponent<AudioSource>();          // Crée la source des pas dynamiquement
        stepsSource.playOnAwake = false;                                           // Ne joue pas au démarrage
        stepsSource.spatialBlend = 0f;                                              // Son 2D — pas d'atténuation spatiale
        stepsSource.loop = false;                                           // Pas en boucle — géré manuellement par le timer
=======
        // Crée deux AudioSources : une pour les pas (en boucle) et une pour les FX ponctuels
        stepsSource = gameObject.AddComponent<AudioSource>();
        stepsSource.playOnAwake = false;
        stepsSource.spatialBlend = 0f;
        stepsSource.loop = false;
>>>>>>> Stashed changes

        fxSource = gameObject.AddComponent<AudioSource>();             // Crée la source des FX dynamiquement
        fxSource.playOnAwake = false;                                              // Ne joue pas au démarrage
        fxSource.spatialBlend = 0f;                                                 // Son 2D
        fxSource.loop = false;                                              // Son ponctuel
    }

    void Update()
    {
<<<<<<< Updated upstream
        bool grounded = movement.IsGrounded();                                     // Vrai si le joueur touche le sol
        bool crouching = movement.IsCrouching();                                    // Vrai si le joueur est accroupi
        bool sprinting = movement.IsSprinting();                                    // Vrai si le joueur est en sprint
        bool moving = HasHorizontalInput();                                      // Vrai si une touche de déplacement est pressée
=======
        // Conditions de mouvement
        bool grounded = movement.IsGrounded();
        bool crouching = movement.IsCrouching();
        bool sprinting = movement.IsSprinting();
        bool moving = HasHorizontalInput();
>>>>>>> Stashed changes

        if (!wasGrounded && grounded)
            PlayFX(landSound, landVolume);                                          // Détecte l'atterrissage : était en l'air, maintenant au sol
        wasGrounded = grounded;                                                     // Mémorise l'état pour la prochaine frame

        if (grounded && moving)
        {
            // Détermine l'intervalle de pas en fonction de l'état du joueur
            float interval = crouching ? crouchInterval
                           : sprinting ? sprintInterval
                           : walkInterval;                                          // Choisit l'intervalle selon l'état du joueur

<<<<<<< Updated upstream
            stepTimer += Time.deltaTime;                                            // Avance le timer de pas

            if (stepTimer >= interval && !stepsSource.isPlaying)
            {
                stepTimer = 0f;                                                     // Remet le timer à zéro pour le prochain pas
=======
            // Incrémente le timer de pas
            stepTimer += Time.deltaTime;

            // Si le timer dépasse l'intervalle et que le son de pas n'est pas déjà en train de jouer,
            // joue un nouveau son de pas
            if (stepTimer >= interval && !stepsSource.isPlaying)
            {
                // Réinitialise le timer et joue un son de pas avec le volume approprié
                stepTimer = 0f;
>>>>>>> Stashed changes
                float vol = crouching ? crouchVolume
                          : sprinting ? sprintVolume
                          : walkVolume;                                             // Choisit le volume selon l'état du joueur
                PlayStep(vol);                                                      // Joue le prochain pas
            }
        }
        else
        {
<<<<<<< Updated upstream
=======
            // Arrête le son si plus en mouvement
>>>>>>> Stashed changes
            if (stepsSource.isPlaying)
                stepsSource.Stop();                                                 // Coupe le son proprement si le joueur s'arrête ou saute
            stepTimer = 0f;                                                         // Remet le timer à zéro pour un départ propre
        }
    }

<<<<<<< Updated upstream
    public void OnJump()
    {
        PlayFX(jumpSound, jumpVolume);                                              // Appelé par PlayerMovement au moment exact du saut
    }

=======
    // ── Sons ponctuels (jump, land) ───────────────────────────────────
    public void OnJump()
    {
        // Arrête les pas en cours
        PlayFX(jumpSound, jumpVolume);
    }

    // ── Méthodes utilitaires ───────────────────────────────────────────
>>>>>>> Stashed changes
    bool HasHorizontalInput()
    {
        // Vérifie si le joueur appuie sur les touches de déplacement horizontal ou vertical
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
               Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;                     // Détecte toute entrée de déplacement (WASD ou joystick)
    }

    // ── Lecture des sons de pas ─────────────────────────────────────────
    void PlayStep(float volume)
    {
<<<<<<< Updated upstream
        if (footstepSounds == null || footstepSounds.Length == 0) return;          // Sécurité : ne fait rien si le pool est vide

=======
        // Sécurité : vérifie que la pool de sons de pas est assignée et non vide
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        // Choisit un son de pas aléatoire différent du dernier joué pour éviter les répétitions
>>>>>>> Stashed changes
        int index;
        int attempts = 0;
        do
        {
<<<<<<< Updated upstream
            index = Random.Range(0, footstepSounds.Length);                        // Tire un index aléatoire dans le pool
            attempts++;
        } while (index == lastStepIndex && footstepSounds.Length > 1 && attempts < 10); // Évite de rejouer le même son deux fois de suite

        lastStepIndex = index;                                                      // Mémorise l'index joué

        if (footstepSounds[index] == null) return;                                 // Sécurité : ignore les slots vides dans le pool

        stepsSource.clip = footstepSounds[index];                                // Assigne le clip à jouer
        stepsSource.volume = volume;                                                // Applique le volume selon l'état
        stepsSource.Play();                                                         // Play() et non PlayOneShot — garantit un seul son actif à la fois
=======
            // Choisit un index aléatoire dans la pool de sons de pas
            index = Random.Range(0, footstepSounds.Length);
            attempts++;
        } while (index == lastStepIndex && footstepSounds.Length > 1 && attempts < 10);
        // Enregistre l'index du son de pas joué pour éviter les répétitions immédiates
        lastStepIndex = index;

        // Vérifie que le son de pas à l'index choisi est assigné
        if (footstepSounds[index] == null) return;

        // Configure et joue le son de pas choisi avec le volume spécifié
        stepsSource.clip = footstepSounds[index];
        stepsSource.volume = volume;
        stepsSource.Play(); // Play() (pas PlayOneShot) — un seul son à la fois
>>>>>>> Stashed changes
    }


    // ── Lecture des sons ponctuels (jump, land) ─────────────────────────
    void PlayFX(AudioClip clip, float volume)
    {
<<<<<<< Updated upstream
        if (fxSource == null || clip == null) return;                              // Sécurité : ignore si la source ou le clip est manquant
        fxSource.PlayOneShot(clip, volume);                                        // PlayOneShot pour les FX — peuvent se superposer si nécessaire
=======
        // Sécurité : vérifie que le clip est assigné avant de tenter de le jouer
        if (fxSource == null || clip == null) return;
        fxSource.PlayOneShot(clip, volume);
>>>>>>> Stashed changes
    }
}