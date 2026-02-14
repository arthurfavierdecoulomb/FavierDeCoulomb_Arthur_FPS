using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Camera Shake")]
    [SerializeField] private CameraBob cameraBob;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeIntensity = 0.15f;

    [Header("Death Screen")]
    private DeathScreen DeathScreen;

    void Start()
    {
        currentHealth = maxHealth;

        if (cameraBob == null)
            cameraBob = GetComponentInChildren<CameraBob>();

        // Trouve l'écran de mort dans la scène
        DeathScreen = FindFirstObjectByType<DeathScreen>();
        if (DeathScreen == null)
        {
            Debug.LogWarning("DeathScreenSimple non trouvé dans la scène !");
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (cameraBob != null)
            cameraBob.Shake(shakeDuration, shakeIntensity);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log("Player Dead");

        // Affiche l'écran de mort
        if (DeathScreen != null)
        {
            DeathScreen.ShowDeathScreen();
        }

        // Optionnel : désactive les contrôles du joueur
        GetComponent<PlayerMovement>().enabled = false;
    }

    public void Respawn()
    {
        // Réinitialise la vie
        currentHealth = maxHealth;

        // Réactive les contrôles
        GetComponent<PlayerMovement>().enabled = true;

        // Repositionne le joueur (optionnel)
        transform.position = Vector3.zero; // ou ta position de spawn

        Debug.Log("Player respawned with full health!");
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    void Update()
    {
        // DEBUG : test des dégâts avec une touche
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
            Debug.Log("DEBUG : dégâts test (-10 HP)");
        }

        // DEBUG : test écran de mort direct
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (DeathScreen != null)
            {
                DeathScreen.ShowDeathScreen();
                Debug.Log("DEBUG : Écran de mort affiché");
            }
        }
    }
}