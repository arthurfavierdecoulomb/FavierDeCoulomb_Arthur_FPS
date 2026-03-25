using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("La vie maximale du joueur.")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Camera Shake")]
    [SerializeField] private CameraBob cameraBob;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeIntensity = 0.15f;

    [Header("Sons")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 1f;

    [Header("Death Screen")]
    private DeathScreen DeathScreen;
    private AudioSource audioSource;

    void Start()
    {
        currentHealth = maxHealth;

        if (cameraBob == null)
            cameraBob = GetComponentInChildren<CameraBob>();

        DeathScreen = FindFirstObjectByType<DeathScreen>();
        if (DeathScreen == null)
            Debug.LogWarning("DeathScreen non trouvé dans la scène !");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (cameraBob != null)
            cameraBob.Shake(shakeDuration, shakeIntensity);

        if (currentHealth <= 0)
            Die();
        else
            PlaySound(hurtSound);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("le joueur creve");
        PlaySound(deathSound);

        if (DeathScreen != null)
            DeathScreen.ShowDeathScreen();
        Debug.Log("DeathScreen affiché");

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = true;

        transform.position = Vector3.zero;
        Debug.Log("Joueur avec toute sa vie");
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, hurtVolume);
    }

    public int GetHealth() => currentHealth;
}