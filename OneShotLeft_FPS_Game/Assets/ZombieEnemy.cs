using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Script d'IA pour un ennemi zombie avec gestion complète des animations
/// Compatible avec le système PlayerHealth existant
/// </summary>
public class ZombieEnemy : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("L'Animator du zombie (auto-assigné si vide)")]
    public Animator animator;

    [Tooltip("Le Transform du joueur (auto-détecté par le tag 'Player' si vide)")]
    public Transform player;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;

    [Header("Paramètres de combat")]
    [Tooltip("Points de vie du zombie")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Tooltip("Distance à laquelle le zombie peut attaquer")]
    [SerializeField] private float attackRange = 2f;

    [Tooltip("Distance à laquelle le zombie détecte le joueur")]
    [SerializeField] private float detectionRange = 15f;

    [Tooltip("Temps entre deux attaques")]
    [SerializeField] private float attackCooldown = 2f;

    [Tooltip("Dégâts infligés au joueur par attaque")]
    [SerializeField] private int attackDamage = 10;

    [Header("Paramètres de mouvement")]
    [Tooltip("Vitesse de marche (idle/patrol)")]
    [SerializeField] private float walkSpeed = 2f;

    [Tooltip("Vitesse de course (poursuite)")]
    [SerializeField] private float runSpeed = 4f;

    [Header("Paramètres d'animation")]
    [Tooltip("Temps avant que le corps ne disparaisse après la mort")]
    [SerializeField] private float timeBeforeDestroy = 5f;

    // État du zombie
    private float lastAttackTime;
    private bool isDead = false;
    private ZombieState currentState = ZombieState.Idle;

    // Noms des paramètres de l'Animator
    private const string PARAM_IS_RUNNING = "IsRunning";
    private const string PARAM_ATTACK = "Attack";
    private const string PARAM_DEATH = "Death";

    // Enum pour les états du zombie
    public enum ZombieState
    {
        Idle,
        Chasing,
        Attacking,
        Dead
    }

    void Start()
    {
        InitializeComponents();
        currentHealth = maxHealth;
    }

    void InitializeComponents()
    {
        // Récupérer le NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent manquant sur " + gameObject.name + " - Ajoutez Component ? Navigation ? Nav Mesh Agent");
            enabled = false;
            return;
        }

        // Auto-assigner l'animator
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator manquant sur " + gameObject.name);
                enabled = false;
                return;
            }
        }

        // Auto-détecter le joueur
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealth = player.GetComponent<PlayerHealth>();

                if (playerHealth == null)
                {
                    Debug.LogError("PlayerHealth manquant sur le joueur !");
                }
            }
            else
            {
                Debug.LogWarning("Aucun joueur trouvé avec le tag 'Player' - Vérifiez que votre joueur a bien ce tag !");
            }
        }
        else
        {
            // Si le player est assigné manuellement, récupérer son PlayerHealth
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        // Configuration initiale
        agent.speed = walkSpeed;
        currentState = ZombieState.Idle;
    }

    void Update()
    {
        if (isDead || player == null) return;

        // Calculer la distance au joueur
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Machine à états
        UpdateState(distanceToPlayer);
    }

    void UpdateState(float distanceToPlayer)
    {
        ZombieState newState = currentState;

        // Déterminer le nouvel état en fonction de la distance
        if (distanceToPlayer <= attackRange)
        {
            newState = ZombieState.Attacking;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            newState = ZombieState.Chasing;
        }
        else
        {
            newState = ZombieState.Idle;
        }

        // Si l'état change, effectuer les actions de transition
        if (newState != currentState)
        {
            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(currentState);
        }

        // Exécuter le comportement de l'état actuel
        ExecuteState(currentState);
    }

    void OnStateEnter(ZombieState state)
    {
        switch (state)
        {
            case ZombieState.Idle:
                agent.isStopped = true;
                animator.SetBool(PARAM_IS_RUNNING, false);
                break;

            case ZombieState.Chasing:
                agent.isStopped = false;
                agent.speed = runSpeed;
                animator.SetBool(PARAM_IS_RUNNING, true);
                break;

            case ZombieState.Attacking:
                agent.isStopped = true;
                animator.SetBool(PARAM_IS_RUNNING, false);
                break;
        }
    }

    void OnStateExit(ZombieState state)
    {
        // Nettoyage si nécessaire lors de la sortie d'un état
    }

    void ExecuteState(ZombieState state)
    {
        switch (state)
        {
            case ZombieState.Idle:
                // Rien de spécial en idle, juste l'animation
                break;

            case ZombieState.Chasing:
                ChasePlayer();
                break;

            case ZombieState.Attacking:
                AttackPlayer();
                break;
        }
    }

    void ChasePlayer()
    {
        // Déplacer vers le joueur
        agent.SetDestination(player.position);

        // Rotation fluide vers le joueur
        RotateTowards(player.position);
    }

    void AttackPlayer()
    {
        // Regarder le joueur pendant l'attaque
        RotateTowards(player.position);

        // Attaquer si le cooldown est terminé
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            animator.SetTrigger(PARAM_ATTACK);
            lastAttackTime = Time.time;

            Debug.Log(gameObject.name + " lance une attaque !");
        }
    }

    void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Garder la rotation horizontale seulement

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// ?? IMPORTANT : Appelé via Animation Event pendant l'animation d'attaque
    /// 
    /// COMMENT L'AJOUTER :
    /// 1. Window ? Animation ? Animation
    /// 2. Sélectionnez votre zombie
    /// 3. Sélectionnez l'animation "Zombie Attack"
    /// 4. Trouvez le frame où le zombie frappe (environ au milieu)
    /// 5. Clic droit sur la timeline ? Add Animation Event
    /// 6. Dans Function, sélectionnez : DealDamageToPlayer
    /// </summary>
    public void DealDamageToPlayer()
    {
        if (isDead || player == null || playerHealth == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Vérifier que le joueur est toujours à portée (avec une petite marge)
        if (distanceToPlayer <= attackRange * 1.5f)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log(gameObject.name + " a touché le joueur ! -" + attackDamage + " HP");
        }
        else
        {
            Debug.Log(gameObject.name + " a raté son attaque (joueur trop loin)");
        }
    }

    /// <summary>
    /// Méthode publique pour infliger des dégâts au zombie
    /// Appelez cette méthode depuis vos armes/projectiles
    /// 
    /// EXEMPLE D'UTILISATION depuis un script d'arme :
    /// 
    /// RaycastHit hit;
    /// if (Physics.Raycast(ray, out hit, range))
    /// {
    ///     ZombieEnemy zombie = hit.collider.GetComponent<ZombieEnemy>();
    ///     if (zombie != null)
    ///     {
    ///         zombie.TakeDamage(weaponDamage);
    ///     }
    /// }
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log(gameObject.name + " a reçu " + damage + " dégâts. Santé: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = ZombieState.Dead;

        Debug.Log(gameObject.name + " est mort !");

        // Déclencher l'animation de mort
        animator.SetTrigger(PARAM_DEATH);
        animator.SetBool(PARAM_IS_RUNNING, false);

        // Arrêter le mouvement
        agent.isStopped = true;
        agent.enabled = false;

        // Désactiver le collider pour qu'on puisse marcher à travers le cadavre
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Optionnel : Désactiver le rigidbody s'il y en a un
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Détruire le zombie après l'animation de mort
        Destroy(gameObject, timeBeforeDestroy);
    }

    // Getters publics pour accéder aux informations du zombie
    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public ZombieState GetCurrentState() => currentState;

    /// <summary>
    /// Gizmos pour visualiser les ranges dans l'éditeur Unity
    /// Les cercles apparaissent quand le zombie est sélectionné
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Range d'attaque (rouge)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Range de détection (jaune)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Ligne vers le joueur si détecté
        if (player != null && !isDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= detectionRange)
            {
                Gizmos.color = dist <= attackRange ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
            }
        }
    }
}