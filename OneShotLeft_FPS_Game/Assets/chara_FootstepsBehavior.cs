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

    private AudioSource stepsSource;
    private AudioSource fxSource;

    private PlayerMovement movement;
    private float stepTimer = 0f;
    private bool wasGrounded = true;
    private int lastStepIndex = -1;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();

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

        if (!wasGrounded && grounded)
            PlayFX(landSound, landVolume);
        wasGrounded = grounded;

        if (grounded && moving)
        {
            float interval = crouching ? crouchInterval
                           : sprinting ? sprintInterval
                           : walkInterval;

            stepTimer += Time.deltaTime;

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
            if (stepsSource.isPlaying)
                stepsSource.Stop();
            stepTimer = 0f;
        }
    }

    public void OnJump()
    {
        PlayFX(jumpSound, jumpVolume);
    }

    bool HasHorizontalInput()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
               Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
    }

    void PlayStep(float volume)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

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
        stepsSource.Play();
    }

    void PlayFX(AudioClip clip, float volume)
    {
        if (fxSource == null || clip == null) return;
        fxSource.PlayOneShot(clip, volume);
    }
}