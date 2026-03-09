using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA Zombie intelligente :
/// - Patrouille via NavMesh (reste dans les pièces/couloirs)
/// - Poursuite par pathfinding (contourne les murs, traverse les portes)
/// - Détection par ligne de vue ET distance
/// - Perd le joueur si hors de vue trop longtemps
/// - Retourne à sa pièce d'origine après avoir perdu la trace
/// </summary>
public class ZombieEnemy : MonoBehaviour
{
    [Header("Références")]
    public Animator animator;
    public Transform player;
    private NavMeshAgent agent;
    private PlayerHealth playerHealth;

    [Header("Paramètres de combat")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;

    [Header("Détection")]
    [SerializeField] private float detectionRange = 12f;  // Distance de détection directe
    [SerializeField] private float hearingRange = 6f;   // Distance d'alerte sans ligne de vue
    [SerializeField] private float losePlayerTime = 4f;   // Secondes avant de perdre le joueur
    [SerializeField] private LayerMask obstacleMask;         // Layers qui bloquent la vue (murs, etc.)

    [Header("Mouvement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float rotateSpeed = 5f;

    [Header("Patrouille NavMesh")]
    [SerializeField] private float patrolRadius = 8f;   // Rayon de recherche de destination
    [SerializeField] private float patrolWaitMin = 2f;
    [SerializeField] private float patrolWaitMax = 5f;
    [SerializeField] private int patrolSamples = 5;    // Tentatives pour trouver un point valide

    [Header("Mort")]
    [SerializeField] private float timeBeforeDestroy = 5f;

    // Paramètres Animator
    private const string PARAM_IS_RUNNING = "IsRunning";
    private const string PARAM_ATTACK = "Attack";
    private const string PARAM_DEATH = "Death";

    // État interne
    private int currentHealth;
    private bool isDead = false;
    private float lastAttackTime = 0f;
    private float losePlayerTimer = 0f;
    private bool canSeePlayer = false;
    private Vector3 lastKnownPlayerPos;
    private Vector3 spawnPosition;  // Position de spawn pour retour si perd le joueur

    // Patrouille
    private float patrolWaitTimer = 0f;
    private float patrolWaitDuration = 0f;
    private bool isWaitingAtPoint = true;

    public enum ZombieState { Idle, Patrolling, Suspicious, Chasing, Attacking, Returning, Dead }
    private ZombieState currentState = ZombieState.Idle;

    // =============================================
    void Start()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        InitComponents();
        SetState(ZombieState.Idle);
    }

    void InitComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) { Debug.LogError("NavMeshAgent manquant sur " + name); enabled = false; return; }

        agent.speed = walkSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.angularSpeed = 0f;

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) { Debug.LogError("Animator manquant sur " + name); enabled = false; return; }

        // Tente de trouver le joueur au Start — si inactif, on réessaiera dans Update
        TryFindPlayer();
    }

    // Cherche le joueur (appelé au Start et dans Update si pas encore trouvé)
    void TryFindPlayer()
    {
        if (player != null) return; // déjà trouvé

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
            Debug.Log(name + " a trouvé le joueur : " + player.name);
        }
    }

    // =============================================
    void Update()
    {
        if (isDead) return;

        // Tant que le joueur n'est pas trouvé, on réessaie chaque frame
        if (player == null)
        {
            TryFindPlayer();
            return; // on attend d'avoir le joueur avant de faire quoi que ce soit
        }

        UpdatePerception();
        UpdateStateMachine();
    }

    // =============================================
    // PERCEPTION
    // =============================================
    void UpdatePerception()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Ligne de vue : raycast vers le joueur
        bool lineOfSight = false;
        if (dist <= detectionRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            // On vise le torse du joueur
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = player.position + Vector3.up * 1.0f;

            if (!Physics.Linecast(origin, target, obstacleMask))
                lineOfSight = true;
        }

        // Entend le joueur même sans ligne de vue (très proche)
        bool hearsPlayer = dist <= hearingRange;

        canSeePlayer = lineOfSight || hearsPlayer;

        if (canSeePlayer)
        {
            lastKnownPlayerPos = player.position;
            losePlayerTimer = 0f;
        }
        else if (currentState == ZombieState.Chasing || currentState == ZombieState.Attacking)
        {
            losePlayerTimer += Time.deltaTime;
        }
    }

    // =============================================
    // MACHINE D'ÉTATS
    // =============================================
    void UpdateStateMachine()
    {
        // Le joueur est mort ? retour à la patrouille
        if (playerHealth != null && playerHealth.GetHealth() <= 0)
        {
            if (currentState != ZombieState.Patrolling && currentState != ZombieState.Idle)
                SetState(ZombieState.Patrolling);
            ExecuteState();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
            case ZombieState.Patrolling:
                if (canSeePlayer)
                    SetState(ZombieState.Chasing);
                break;

            case ZombieState.Chasing:
                if (dist <= attackRange)
                    SetState(ZombieState.Attacking);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime)
                    SetState(ZombieState.Returning); // Retourne à sa zone
                break;

            case ZombieState.Attacking:
                if (dist > attackRange * 1.3f) // Légère marge pour pas osciller
                    SetState(ZombieState.Chasing);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime)
                    SetState(ZombieState.Returning);
                break;

            case ZombieState.Returning:
                if (canSeePlayer)
                    SetState(ZombieState.Chasing);
                else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                    SetState(ZombieState.Patrolling); // Arrivé à destination ? reprend patrouille
                break;
        }

        ExecuteState();
    }

    void SetState(ZombieState newState)
    {
        if (newState == currentState) return;

        // Nettoyage sortie état
        switch (currentState)
        {
            case ZombieState.Chasing:
            case ZombieState.Attacking:
                break;
        }

        currentState = newState;

        // Init entrée état
        switch (newState)
        {
            case ZombieState.Idle:
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetBool(PARAM_IS_RUNNING, false);
                patrolWaitTimer = 0f;
                patrolWaitDuration = Random.Range(patrolWaitMin, patrolWaitMax);
                isWaitingAtPoint = true;
                break;

            case ZombieState.Patrolling:
                agent.speed = walkSpeed;
                agent.isStopped = false;
                // Cherche immédiatement une destination
                TrySetPatrolDestination();
                break;

            case ZombieState.Chasing:
                agent.speed = runSpeed;
                agent.isStopped = false;
                animator.SetBool(PARAM_IS_RUNNING, true);
                break;

            case ZombieState.Attacking:
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetBool(PARAM_IS_RUNNING, false);
                animator.ResetTrigger(PARAM_ATTACK);
                break;

            case ZombieState.Returning:
                agent.speed = walkSpeed;
                agent.isStopped = false;
                animator.SetBool(PARAM_IS_RUNNING, false);
                // NavMesh trouve le chemin vers la dernière position connue puis vers le spawn
                agent.SetDestination(lastKnownPlayerPos);
                StartCoroutine(ReturnToSpawnAfterDelay(1.5f));
                break;
        }
    }

    IEnumerator ReturnToSpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == ZombieState.Returning)
            agent.SetDestination(spawnPosition);
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case ZombieState.Idle:
                ExecuteIdle();
                break;
            case ZombieState.Patrolling:
                ExecutePatrol();
                break;
            case ZombieState.Chasing:
                ExecuteChase();
                break;
            case ZombieState.Attacking:
                ExecuteAttack();
                break;
            case ZombieState.Returning:
                // Le NavMesh gère le déplacement, on tourne juste vers la destination
                if (agent.hasPath)
                    SmoothRotateTowards(agent.steeringTarget);
                break;
        }
    }

    // =============================================
    // ÉTATS
    // =============================================
    void ExecuteIdle()
    {
        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer >= patrolWaitDuration)
            SetState(ZombieState.Patrolling);
    }

    void ExecutePatrol()
    {
        if (isWaitingAtPoint)
        {
            // Attend avant de bouger
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitDuration)
            {
                isWaitingAtPoint = false;
                TrySetPatrolDestination();
            }
        }
        else
        {
            // En déplacement vers un point de patrouille
            if (agent.hasPath)
                SmoothRotateTowards(agent.steeringTarget);

            // Arrivé ?
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                agent.isStopped = true;
                animator.SetBool(PARAM_IS_RUNNING, false);
                isWaitingAtPoint = true;
                patrolWaitTimer = 0f;
                patrolWaitDuration = Random.Range(patrolWaitMin, patrolWaitMax);
            }
        }
    }

    void ExecuteChase()
    {
        // Met à jour la destination vers le joueur (via NavMesh ? contourne les murs)
        if (canSeePlayer)
            agent.SetDestination(player.position);
        else
            agent.SetDestination(lastKnownPlayerPos);

        if (agent.hasPath)
            SmoothRotateTowards(agent.steeringTarget);

        animator.SetBool(PARAM_IS_RUNNING, true);
    }

    void ExecuteAttack()
    {
        // Reste face au joueur pendant l'attaque
        SmoothRotateTowards(player.position);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            animator.ResetTrigger(PARAM_ATTACK);
            animator.SetTrigger(PARAM_ATTACK);
            lastAttackTime = Time.time;
        }
    }

    // =============================================
    // PATROUILLE NAVMESH
    // =============================================
    void TrySetPatrolDestination()
    {
        // Essaie plusieurs fois de trouver un point valide sur le NavMesh
        for (int i = 0; i < patrolSamples; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir += transform.position;
            randomDir.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
            {
                // Vérifie qu'un chemin complet existe (pas seulement un point proche)
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    agent.isStopped = false;
                    agent.speed = walkSpeed;
                    animator.SetBool(PARAM_IS_RUNNING, true);
                    return;
                }
            }
        }

        // Aucun point valide trouvé ? reste en idle un moment
        SetState(ZombieState.Idle);
    }

    // =============================================
    // ROTATION FLUIDE
    // =============================================
    void SmoothRotateTowards(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
    }

    // =============================================
    // DÉGÂTS & MORT
    // =============================================

    /// <summary>
    /// Appelé via Animation Event au moment de l'impact dans l'animation d'attaque
    /// </summary>
    public void DealDamageToPlayer()
    {
        if (isDead || player == null || playerHealth == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange * 1.5f)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log(name + " frappe le joueur ! -" + attackDamage + " HP");
        }
    }

    /// <summary>
    /// Appelé par les armes/projectiles pour blesser le zombie
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        Debug.Log(name + " reçoit " + damage + " dégâts ? " + currentHealth + "/" + maxHealth);

        // Alerte si touché sans voir le joueur
        if (!canSeePlayer && player != null)
        {
            lastKnownPlayerPos = player.position;
            SetState(ZombieState.Chasing);
        }

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = ZombieState.Dead;

        StopAllCoroutines();

        animator.SetTrigger(PARAM_DEATH);
        animator.SetBool(PARAM_IS_RUNNING, false);

        agent.isStopped = true;
        agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Destroy(gameObject, timeBeforeDestroy);
        Debug.Log(name + " est mort.");
    }

    // =============================================
    // GETTERS
    // =============================================
    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public ZombieState GetCurrentState() => currentState;

    // =============================================
    // GIZMOS
    // =============================================
    void OnDrawGizmosSelected()
    {
        // Attaque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Détection visuelle
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Écoute
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Rayon de patrouille
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // Ligne vers joueur
        if (player != null && !isDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= detectionRange)
            {
                Gizmos.color = canSeePlayer ? Color.red : Color.gray;
                Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up);
            }
        }

        // Dernière position connue du joueur
        if (Application.isPlaying && currentState == ZombieState.Returning)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPlayerPos, 0.3f);
        }
    }
}