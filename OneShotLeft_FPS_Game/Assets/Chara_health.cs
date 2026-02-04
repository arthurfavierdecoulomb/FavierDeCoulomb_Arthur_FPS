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

    void Start()
    {
        currentHealth = maxHealth;

        if (cameraBob == null)
            cameraBob = GetComponentInChildren<CameraBob>();
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
        Debug.Log("Player Dead ");
        // Respawn / Game Over ici
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
    }

}
