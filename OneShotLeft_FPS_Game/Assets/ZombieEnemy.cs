using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie compatible avec le WaveManager.
/// - Reçoit ses stats (vitesse, vague) via ZombieStats.Init()
/// - Notifie sa mort via ZombieDeathNotifier (utilisé par le WaveManager)
/// - Conserve toute la logique IA : patrouille, perception, poursuite, attaque, retour
/// - Intègre un NavMeshGuard : replace automatiquement le zombie sur le NavMesh
///   s'il en sort (ex: passage par une porte sans NavMesh en dessous)
/// </summary>
public class ZombieEnemy : MonoBehaviour
{
    [Header("Références")]
    public Animator animator;
    public Transform player;
    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private ZombieStats stats;

    [Header("Paramètres de combat")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;

    [Header("Détection")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float hearingRange = 6f;
    [SerializeField] private float losePlayerTime = 4f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Mouvement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float rotateSpeed = 5f;

    [Header("Patrouille NavMesh")]
    [SerializeField] private float patrolRadius = 8f;
    [SerializeField] private float patrolWaitMin = 2f;
    [SerializeField] private float patrolWaitMax = 5f;
    [SerializeField] private int patrolSamples = 5;

    [Header("Mort")]
    [SerializeField] private float timeBeforeDestroy = 5f;

    [Header("Sécurité NavMesh")]
    [Tooltip("Rayon de recherche du point NavMesh le plus proche si hors NavMesh")]
    [SerializeField] private float snapRadius = 8f;
    [Tooltip("Fréquence de vérification hors NavMesh (secondes)")]
    [SerializeField] private float navCheckInterval = 0.4f;
    [Tooltip("Durée max sur un OffMeshLink avant force-complétion (secondes)")]
    [SerializeField] private float maxLinkTime = 2f;
    [Tooltip("Délai de grâce après le spawn — le guard n'est pas actif pendant ce temps")]
    [SerializeField] private float snapGraceDelay = 2f;

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
    private Vector3 spawnPosition;

    // Patrouille
    private float patrolWaitTimer = 0f;
    private float patrolWaitDuration = 0f;
    private bool isWaitingAtPoint = true;

    // NavMesh Guard
    private float _navCheckTimer = 0f;
    private float _onLinkTimer = 0f;
    private float _graceTimer = 0f;
    private Vector3 _lastValidPos;

    public enum ZombieState { Idle, Patrolling, Chasing, Attacking, Returning, Dead }
    private ZombieState currentState = ZombieState.Idle;

    // =============================================
    void Awake()
    {
        stats = GetComponent<ZombieStats>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        _lastValidPos = transform.position;

        ApplyWaveStats();
        InitComponents();
        SetState(ZombieState.Idle);
    }

    void ApplyWaveStats()
    {
        if (stats == null) return;
        runSpeed = stats.moveSpeed;
        walkSpeed = stats.moveSpeed * 0.45f;
        Debug.Log($"{name} — Vague {stats.wave} | run : {runSpeed:F1} | walk : {walkSpeed:F1}");
    }

    void InitComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        { Debug.LogError("NavMeshAgent manquant sur " + name); enabled = false; return; }

        agent.speed = walkSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.angularSpeed = 0f;

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null)
        { Debug.LogError("Animator manquant sur " + name); enabled = false; return; }

        TryFindPlayer();
    }

    void TryFindPlayer()
    {
        if (player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
        }
    }

    // =============================================
    void Update()
    {
        if (isDead) return;
        if (player == null) { TryFindPlayer(); return; }

        UpdateNavMeshGuard();
        UpdatePerception();
        UpdateStateMachine();
    }

    // =============================================
    // NAVMESH GUARD
    // =============================================
    void UpdateNavMeshGuard()
    {
        if (agent == null || !agent.enabled) return;

        // Délai de grâce après le spawn — laisse le temps à l'agent de s'initialiser
        if (_graceTimer < snapGraceDelay)
        {
            _graceTimer += Time.deltaTime;
            if (agent.isOnNavMesh) _lastValidPos = transform.position;
            return;
        }
        if (agent.isOnOffMeshLink)
        {
            _onLinkTimer += Time.deltaTime;
            if (_onLinkTimer >= maxLinkTime)
            {
                ForceCompleteOffMeshLink();
                _onLinkTimer = 0f;
            }
            return;
        }
        _onLinkTimer = 0f;

        // Sur le NavMesh : mémorise la position valide
        if (agent.isOnNavMesh)
        {
            _lastValidPos = transform.position;
            return;
        }

        // Hors NavMesh : vérification périodique
        _navCheckTimer += Time.deltaTime;
        if (_navCheckTimer >= navCheckInterval)
        {
            _navCheckTimer = 0f;
            SnapToNavMesh();
        }
    }

    void ForceCompleteOffMeshLink()
    {
        if (!agent.isOnOffMeshLink) return;

        Vector3 endPos = agent.currentOffMeshLinkData.endPos;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(endPos, out hit, snapRadius, NavMesh.AllAreas))
        {
            agent.CompleteOffMeshLink();
            agent.Warp(hit.position);
            _lastValidPos = hit.position;
            Debug.Log($"[NavMeshGuard] {name} — OffMeshLink forcé vers {hit.position}");
        }
    }

    void SnapToNavMesh()
    {
        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(transform.position, out hit, snapRadius, NavMesh.AllAreas);
        if (!found)
            found = NavMesh.SamplePosition(_lastValidPos, out hit, snapRadius, NavMesh.AllAreas);

        if (found)
        {
            agent.enabled = false;
            transform.position = hit.position;
            agent.enabled = true;
            _lastValidPos = hit.position;

            if (agent.hasPath)
            {
                Vector3 dest = agent.destination;
                agent.ResetPath();
                agent.SetDestination(dest);
            }
            Debug.Log($"[NavMeshGuard] {name} replacé sur le NavMesh à {hit.position}");
        }
        else
        {
            // Pas de NavMesh trouvé — on log mais on ne tue pas
            Debug.LogWarning($"[NavMeshGuard] {name} introuvable sur NavMesh dans {snapRadius}m — augmente snapRadius.");
        }
    }

    // =============================================
    // PERCEPTION
    // =============================================
    void UpdatePerception()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        bool lineOfSight = false;
        if (dist <= detectionRange)
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = player.position + Vector3.up * 1.0f;
            if (!Physics.Linecast(origin, target, obstacleMask))
                lineOfSight = true;
        }

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
                if (canSeePlayer) SetState(ZombieState.Chasing);
                break;

            case ZombieState.Chasing:
                if (dist <= attackRange)
                    SetState(ZombieState.Attacking);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime)
                    SetState(ZombieState.Returning);
                break;

            case ZombieState.Attacking:
                if (dist > attackRange * 1.3f)
                    SetState(ZombieState.Chasing);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime)
                    SetState(ZombieState.Returning);
                break;

            case ZombieState.Returning:
                if (canSeePlayer)
                    SetState(ZombieState.Chasing);
                else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                    SetState(ZombieState.Patrolling);
                break;
        }

        ExecuteState();
    }

    void SetState(ZombieState newState)
    {
        if (newState == currentState) return;
        currentState = newState;

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
            case ZombieState.Idle: ExecuteIdle(); break;
            case ZombieState.Patrolling: ExecutePatrol(); break;
            case ZombieState.Chasing: ExecuteChase(); break;
            case ZombieState.Attacking: ExecuteAttack(); break;
            case ZombieState.Returning:
                if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget);
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
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitDuration)
            {
                isWaitingAtPoint = false;
                TrySetPatrolDestination();
            }
        }
        else
        {
            if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget);

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
        agent.SetDestination(canSeePlayer ? player.position : lastKnownPlayerPos);
        if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget);
        animator.SetBool(PARAM_IS_RUNNING, true);
    }

    void ExecuteAttack()
    {
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
        for (int i = 0; i < patrolSamples; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;
            randomDir.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
            {
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
        SetState(ZombieState.Idle);
    }

    // =============================================
    // ROTATION FLUIDE
    // =============================================
    void SmoothRotateTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotateSpeed * Time.deltaTime
        );
    }

    // =============================================
    // DÉGÂTS & MORT
    // =============================================
    public void DealDamageToPlayer()
    {
        if (isDead || player == null || playerHealth == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange * 1.5f)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"{name} frappe le joueur ! -{attackDamage} HP");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

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

        ZombieDeathNotifier notifier = GetComponent<ZombieDeathNotifier>();
        if (notifier != null) notifier.NotifyDeath();

        Destroy(gameObject, timeBeforeDestroy);
        Debug.Log($"{name} est mort.");
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        if (player != null && !isDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= detectionRange)
            {
                Gizmos.color = canSeePlayer ? Color.red : Color.gray;
                Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up);
            }
        }

        if (Application.isPlaying && currentState == ZombieState.Returning)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPlayerPos, 0.3f);
        }

        // Dernière position valide NavMesh (debug)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_lastValidPos, 0.15f);
        }
    }
}