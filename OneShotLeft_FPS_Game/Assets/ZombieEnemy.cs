using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private float snapRadius = 8f;
    [SerializeField] private float navCheckInterval = 0.4f;
    [SerializeField] private float maxLinkTime = 2f;
    [SerializeField] private float snapGraceDelay = 2f;

    [Header("Sons — Pas")]
    [Tooltip("Pool de sons de marche (tirés aléatoirement)")]
    [SerializeField] private AudioClip[] walkFootsteps;
    [Tooltip("Pool de sons de course (tirés aléatoirement)")]
    [SerializeField] private AudioClip[] runFootsteps;
    [SerializeField] private float walkStepInterval = 0.55f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.8f;

    [Header("Sons — Zombie")]
    [Tooltip("Grognements / bruits ambiants joués aléatoirement")]
    [SerializeField] private AudioClip[] idleSounds;
    [Tooltip("Intervalle min/max entre deux bruits ambiants (secondes)")]
    [SerializeField] private float idleSoundIntervalMin = 4f;
    [SerializeField] private float idleSoundIntervalMax = 10f;
    [SerializeField][Range(0f, 1f)] private float idleVolume = 0.9f;

    [Header("Sons — Dégâts & Mort")]
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip landSound;
    [Tooltip("Délai entre la mort et le bruit de chute au sol")]
    [SerializeField] private float landDelay = 0.6f;
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float deathVolume = 1f;

    [Header("Son 3D — Portée")]
    [SerializeField] private float audioMinDistance = 1f;
    [SerializeField] private float audioMaxDistance = 20f;

    // AudioSources séparées : pas (loop) et FX ponctuels
    private AudioSource stepsSource;
    private AudioSource fxSource;

    // Timers sons
    private float _stepTimer = 0f;
    private float _idleTimer = 0f;
    private float _idleInterval = 0f;
    private int _lastStepIdx = -1;
    private int _lastIdleIdx = -1;

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
    void Awake() { stats = GetComponent<ZombieStats>(); }

    void Start()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        _lastValidPos = transform.position;

        ApplyWaveStats();
        InitComponents();
        InitAudio();
        SetState(ZombieState.Idle);

        // Démarre le timer des bruits ambiants
        _idleInterval = Random.Range(idleSoundIntervalMin, idleSoundIntervalMax);
    }

    // ─── Audio init ───────────────────────────────────────────────────────
    void InitAudio()
    {
        // Source pour les pas — configurée en 3D
        stepsSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(stepsSource);
        stepsSource.loop = false;

        // Source pour les FX ponctuels (hurt, mort, idle)
        fxSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(fxSource);
        fxSource.loop = false;
    }

    void ConfigureSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.spatialBlend = 1f;           // 100% 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = audioMinDistance;
        src.maxDistance = audioMaxDistance;
        src.dopplerLevel = 0f;
    }

    void ApplyWaveStats()
    {
        if (stats == null) return;
        runSpeed = stats.moveSpeed;
        walkSpeed = stats.moveSpeed * 0.45f;
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
        if (agent == null || !agent.isOnNavMesh) return;

        UpdateNavMeshGuard();
        UpdatePerception();
        UpdateStateMachine();
        UpdateSounds();
    }

    // =============================================
    // SONS
    // =============================================
    void UpdateSounds()
    {
        UpdateFootsteps();
        UpdateIdleSounds();
    }

    void UpdateFootsteps()
    {
        bool moving = agent.velocity.magnitude > 0.3f;
        if (!moving) { if (stepsSource.isPlaying) stepsSource.Stop(); _stepTimer = 0f; return; }

        bool sprinting = currentState == ZombieState.Chasing || currentState == ZombieState.Attacking;
        float interval = sprinting ? runStepInterval : walkStepInterval;
        AudioClip[] pool = sprinting ? runFootsteps : walkFootsteps;

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= interval && !stepsSource.isPlaying)
        {
            _stepTimer = 0f;
            PlayRandomFromPool(pool, stepsSource, footstepVolume, ref _lastStepIdx);
        }
    }

    void UpdateIdleSounds()
    {
        // Bruits ambiants joués périodiquement peu importe l'état
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _idleInterval)
        {
            _idleTimer = 0f;
            _idleInterval = Random.Range(idleSoundIntervalMin, idleSoundIntervalMax);
            PlayRandomFromPool(idleSounds, fxSource, idleVolume, ref _lastIdleIdx);
        }
    }

    void PlayRandomFromPool(AudioClip[] pool, AudioSource src, float volume, ref int lastIdx)
    {
        if (pool == null || pool.Length == 0 || src == null) return;

        int idx;
        int attempts = 0;
        do { idx = Random.Range(0, pool.Length); attempts++; }
        while (idx == lastIdx && pool.Length > 1 && attempts < 10);

        lastIdx = idx;
        if (pool[idx] == null) return;
        src.clip = pool[idx];
        src.volume = volume;
        src.Play();
    }

    void PlayFX(AudioClip clip, float volume)
    {
        if (fxSource == null || clip == null) return;
        fxSource.PlayOneShot(clip, volume);
    }

    // =============================================
    // NAVMESH GUARD
    // =============================================
    void UpdateNavMeshGuard()
    {
        if (agent == null || !agent.enabled) return;

        if (_graceTimer < snapGraceDelay)
        {
            _graceTimer += Time.deltaTime;
            if (agent.isOnNavMesh) _lastValidPos = transform.position;
            return;
        }
        if (agent.isOnOffMeshLink)
        {
            _onLinkTimer += Time.deltaTime;
            if (_onLinkTimer >= maxLinkTime) { ForceCompleteOffMeshLink(); _onLinkTimer = 0f; }
            return;
        }
        _onLinkTimer = 0f;

        if (agent.isOnNavMesh) { _lastValidPos = transform.position; return; }

        _navCheckTimer += Time.deltaTime;
        if (_navCheckTimer >= navCheckInterval) { _navCheckTimer = 0f; SnapToNavMesh(); }
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
        }
    }

    void SnapToNavMesh()
    {
        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(transform.position, out hit, snapRadius, NavMesh.AllAreas);
        if (!found) found = NavMesh.SamplePosition(_lastValidPos, out hit, snapRadius, NavMesh.AllAreas);

        if (found)
        {
            agent.enabled = false;
            transform.position = hit.position;
            agent.enabled = true;
            _lastValidPos = hit.position;
            if (agent.hasPath) { Vector3 dest = agent.destination; agent.ResetPath(); agent.SetDestination(dest); }
        }
        else Debug.LogWarning($"[NavMeshGuard] {name} introuvable sur NavMesh dans {snapRadius}m.");
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
            if (!Physics.Linecast(origin, target, obstacleMask)) lineOfSight = true;
        }

        bool hearsPlayer = dist <= hearingRange;
        canSeePlayer = lineOfSight || hearsPlayer;

        if (canSeePlayer) { lastKnownPlayerPos = player.position; losePlayerTimer = 0f; }
        else if (currentState == ZombieState.Chasing || currentState == ZombieState.Attacking)
            losePlayerTimer += Time.deltaTime;
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
            ExecuteState(); return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
            case ZombieState.Patrolling:
                if (canSeePlayer) SetState(ZombieState.Chasing);
                break;
            case ZombieState.Chasing:
                if (dist <= attackRange) SetState(ZombieState.Attacking);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime) SetState(ZombieState.Returning);
                break;
            case ZombieState.Attacking:
                if (dist > attackRange * 1.3f) SetState(ZombieState.Chasing);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime) SetState(ZombieState.Returning);
                break;
            case ZombieState.Returning:
                if (canSeePlayer) SetState(ZombieState.Chasing);
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
        if (agent == null || !agent.isOnNavMesh) return;

        switch (newState)
        {
            case ZombieState.Idle:
                agent.isStopped = true; agent.ResetPath();
                animator.SetBool(PARAM_IS_RUNNING, false);
                patrolWaitTimer = 0f; patrolWaitDuration = Random.Range(patrolWaitMin, patrolWaitMax);
                isWaitingAtPoint = true;
                break;
            case ZombieState.Patrolling:
                agent.speed = walkSpeed; agent.isStopped = false;
                TrySetPatrolDestination();
                break;
            case ZombieState.Chasing:
                agent.speed = runSpeed; agent.isStopped = false;
                animator.SetBool(PARAM_IS_RUNNING, true);
                break;
            case ZombieState.Attacking:
                agent.isStopped = true; agent.ResetPath();
                animator.SetBool(PARAM_IS_RUNNING, false);
                animator.ResetTrigger(PARAM_ATTACK);
                break;
            case ZombieState.Returning:
                agent.speed = walkSpeed; agent.isStopped = false;
                animator.SetBool(PARAM_IS_RUNNING, false);
                agent.SetDestination(lastKnownPlayerPos);
                StartCoroutine(ReturnToSpawnAfterDelay(1.5f));
                break;
        }
    }

    IEnumerator ReturnToSpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == ZombieState.Returning) agent.SetDestination(spawnPosition);
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
                if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget); break;
        }
    }

    void ExecuteIdle()
    {
        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer >= patrolWaitDuration) SetState(ZombieState.Patrolling);
    }

    void ExecutePatrol()
    {
        if (isWaitingAtPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitDuration) { isWaitingAtPoint = false; TrySetPatrolDestination(); }
        }
        else
        {
            if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                agent.isStopped = true;
                animator.SetBool(PARAM_IS_RUNNING, false);
                isWaitingAtPoint = true; patrolWaitTimer = 0f;
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
    // PATROUILLE
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
                    agent.SetDestination(hit.position); agent.isStopped = false;
                    agent.speed = walkSpeed;
                    animator.SetBool(PARAM_IS_RUNNING, true);
                    return;
                }
            }
        }
        SetState(ZombieState.Idle);
    }

    void SmoothRotateTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
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

        // Son de dégâts
        if (hurtSounds != null && hurtSounds.Length > 0)
            PlayFX(hurtSounds[Random.Range(0, hurtSounds.Length)], hurtVolume);

        if (!canSeePlayer && player != null) { lastKnownPlayerPos = player.position; SetState(ZombieState.Chasing); }
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true; currentState = ZombieState.Dead;

        StopAllCoroutines();

        // Arrête les pas
        if (stepsSource != null) stepsSource.Stop();

        // Son de mort
        PlayFX(deathSound, deathVolume);

        // Son de chute au sol après délai
        StartCoroutine(PlayLandSoundDelayed());

        animator.SetTrigger(PARAM_DEATH);
        animator.SetBool(PARAM_IS_RUNNING, false);
        agent.isStopped = true; agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        ZombieDeathNotifier notifier = GetComponent<ZombieDeathNotifier>();
        if (notifier != null) notifier.NotifyDeath();

        Destroy(gameObject, timeBeforeDestroy);
    }

    IEnumerator PlayLandSoundDelayed()
    {
        yield return new WaitForSeconds(landDelay);
        PlayFX(landSound, deathVolume);
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
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, hearingRange);
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f); Gizmos.DrawWireSphere(transform.position, patrolRadius);

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
        { Gizmos.color = Color.magenta; Gizmos.DrawSphere(lastKnownPlayerPos, 0.3f); }
        if (Application.isPlaying)
        { Gizmos.color = Color.green; Gizmos.DrawSphere(_lastValidPos, 0.15f); }
    }
}