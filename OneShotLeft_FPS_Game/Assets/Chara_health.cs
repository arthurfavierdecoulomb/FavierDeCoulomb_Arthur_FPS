using UnityEngine;

public class PlayerHealth : MonoBehaviour                                           // Gère la vie du joueur, les dégâts, la mort et le respawn
{
    [Header("Health")]
    [Tooltip("La vie maximale du joueur. Ajustez cette valeur dans l'Inspector pour équilibrer la difficulté.")]
    [SerializeField] private int maxHealth = 100;                                   // Vie maximale du joueur, ajustable dans l'Inspector
    private int currentHealth;                                                     // Vie actuelle du joueur
    private bool isDead = false;                                                    // Flag anti double-appel — empêche de mourir deux fois

    [Header("Camera Shake")]
    [Tooltip("Tremblement de la caméra déclenché à chaque dégât pour un feedback visuel immédiat. Ajustez la durée et l'intensité dans l'Inspector.")]
    [SerializeField] private CameraBob cameraBob;                                   // Référence au script de bob pour déclencher un shake à chaque dégât
    [SerializeField] private float shakeDuration = 0.15f;                         // Durée du shake lors d'un dégât
    [SerializeField] private float shakeIntensity = 0.15f;                         // Intensité du shake lors d'un dégât

    [Header("Sons")]
    [Tooltip("Son joué quand le joueur prend des dégâts")]
    [SerializeField] private AudioClip hurtSound;                                   // Son de douleur — joué si le joueur survit au coup
    [Tooltip("Son joué quand le joueur meurt")]
    [SerializeField] private AudioClip deathSound;                                  // Son de mort — joué une seule fois à la mort
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 1f;                 // Volume partagé pour les sons de dégâts et de mort

    [Header("Death Screen")]
    [Tooltip("Ecran de mort animé qui s'affiche à la mort du joueur. Doit être présent dans la scène et assigné automatiquement.")]
    private DeathScreen DeathScreen;                                                // Référence à l'écran de mort, trouvé automatiquement dans la scène
    private AudioSource audioSource;                                                // AudioSource pour jouer les sons de dégâts et de mort

    void Start()
    {
        currentHealth = maxHealth;                                                  // Initialise la vie au maximum au démarrage

        if (cameraBob == null)
            cameraBob = GetComponentInChildren<CameraBob>();                        // Cherche CameraBob dans les enfants si non assigné

        DeathScreen = FindFirstObjectByType<DeathScreen>();                         // Trouve l'écran de mort dans la scène automatiquement
        if (DeathScreen == null)
            Debug.LogWarning("DeathScreen non trouvé dans la scène !");             // Avertit si l'écran de mort est absent de la scène

        audioSource = GetComponent<AudioSource>();                                  // Récupère l'AudioSource si elle existe déjà
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();                   // Sinon en crée une automatiquement
        audioSource.playOnAwake = false;                                           // Ne joue pas au démarrage
        audioSource.spatialBlend = 0f;                                              // Son 2D — entendu partout, pas d'atténuation spatiale
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;                                                         // Ignore les dégâts si le joueur est déjà mort

        currentHealth -= damage;                                                    // Réduit la vie du montant des dégâts reçus
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);                 // Empêche la vie de passer en négatif ou dépasser le max

        if (cameraBob != null)
            cameraBob.Shake(shakeDuration, shakeIntensity);                        // Déclenche un camera shake pour un retour visuel immédiat

        if (currentHealth <= 0)
            Die();                                                                  // Déclenche la mort si la vie atteint zéro
        else
            PlaySound(hurtSound);                                                   // Joue le son de douleur si le joueur survit au coup
    }

    private void Die()
    {
        if (isDead) return;                                                         // Sécurité anti double-appel — Die() ne s'exécute qu'une seule fois
        isDead = true;                                                              // Verrouille l'état mort

        Debug.Log("le joueur creve");                                                   // Log de confirmation dans la console

        PlaySound(deathSound);                                                      // Joue le son de mort

        if (DeathScreen != null)
            DeathScreen.ShowDeathScreen();                                          // Affiche l'écran de mort animé
        Debug.Log("DeathScreen affiché");                                      // Log de confirmation de l'affichage

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;                                        // Désactive les contrôles du joueur à la mort
    }

    public void Respawn()
    {
        isDead = false;                                                      // Réactive les dégâts
        currentHealth = maxHealth;                                                  // Restaure la vie complète

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = true;                                         // Réactive les contrôles du joueur

        transform.position = Vector3.zero;                                         // Téléporte le joueur à l'origine de la scène
        Debug.Log("Joueur avec toute sa vie");                                      // Log de confirmation du respawn
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;                           // Sécurité : ne fait rien si la source ou le clip manque
        audioSource.PlayOneShot(clip, hurtVolume);                                 // PlayOneShot — peut se superposer sans interrompre les sons existants
    }

    public int GetHealth() => currentHealth;                                        // Accesseur public — utilisé par HealthUI pour afficher la vie

    void Update() { }                                                               // Vide — j'avais mis des touches clavier de test ici, mais ont été retirés pour éviter les triches accidentelles


}