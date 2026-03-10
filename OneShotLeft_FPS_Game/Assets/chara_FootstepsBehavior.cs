using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Sons de pas — Pool (4 sons)")]
    [SerializeField] private AudioClip[] footstepSounds = new AudioClip[4];

    [Header("Sons spéciaux")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;

    [Header("Intervalles (secondes entre chaque pas)")]
    [SerializeField] private float walkInterval = 0.5f;
    [SerializeField] private float sprintInterval = 0.3f;
    [SerializeField] private float crouchInterval = 0.7f;

    [Header("Volumes")]
    [SerializeField][Range(0f, 1f)] private float walkVolume = 0.8f;
    [SerializeField][Range(0f, 1f)] private float sprintVolume = 1.0f;
    [SerializeField][Range(0f, 1f)] private float crouchVolume = 0.3f;
    [SerializeField][Range(0f, 1f)] private float jumpVolume = 0.9f;
    [SerializeField][Range(0f, 1f)] private float landVolume = 1.0f;

    // AudioSource dédié aux pas (évite les conflits avec sauts/atterrissage)
    private AudioSource stepsSource;
    // AudioSource dédié aux sons ponctuels (saut, stomp)
    private AudioSource fxSource;

    private PlayerMovement movement;
    private float stepTimer = 0f;
    private bool wasGrounded = true;
    private int lastStepIndex = -1;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        movement = GetComponent<PlayerMovement>();

        // Crée deux AudioSources séparées pour pas et FX
        stepsSource = gameObject.AddComponent<AudioSource>();
        stepsSource.playOnAwake = false;
        stepsSource.spatialBlend = 0f;
        stepsSource.loop = false;

        fxSource = gameObject.AddComponent<AudioSource>();
        fxSource.playOnAwake = false;
        fxSource.spatialBlend = 0f;
        fxSource.loop = false;
    }

    void Update()
    {
        bool grounded = movement.IsGrounded();
        bool crouching = movement.IsCrouching();
        bool sprinting = movement.IsSprinting();
        bool moving = HasHorizontalInput();

        // ── Atterrissage ──────────────────────────────────────────────────
        if (!wasGrounded && grounded)
            PlayFX(landSound, landVolume);
        wasGrounded = grounded;

        // ── Pas ───────────────────────────────────────────────────────────
        if (grounded && moving)
        {
            float interval = crouching ? crouchInterval
                           : sprinting ? sprintInterval
                           : walkInterval;

            stepTimer += Time.deltaTime;

            // Lance un pas seulement si le timer est écoulé ET que
            // le son précédent est terminé — empêche l'accumulation
            if (stepTimer >= interval && !stepsSource.isPlaying)
            {
                stepTimer = 0f;
                float vol = crouching ? crouchVolume
                          : sprinting ? sprintVolume
                          : walkVolume;
                PlayStep(vol);
            }
        }
        else
        {
            // Arrête proprement le son si plus en mouvement
            if (stepsSource.isPlaying)
                stepsSource.Stop();
            stepTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    public void OnJump()
    {
        PlayFX(jumpSound, jumpVolume);
    }

    // ─────────────────────────────────────────────────────────────────────
    bool HasHorizontalInput()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
               Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
    }

    void PlayStep(float volume)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        // Evite de rejouer le même son deux fois de suite
        int index;
        int attempts = 0;
        do
        {
            index = Random.Range(0, footstepSounds.Length);
            attempts++;
        } while (index == lastStepIndex && footstepSounds.Length > 1 && attempts < 10);

        lastStepIndex = index;

        if (footstepSounds[index] == null) return;

        stepsSource.clip = footstepSounds[index];
        stepsSource.volume = volume;
        stepsSource.Play(); // Play() (pas PlayOneShot) — un seul son à la fois
    }

    void PlayFX(AudioClip clip, float volume)
    {
        if (fxSource == null || clip == null) return;
        fxSource.PlayOneShot(clip, volume);
    }
}