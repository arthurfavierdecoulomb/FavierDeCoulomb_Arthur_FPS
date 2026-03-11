using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Gère l'IA complète du zombie : machine d'états (Idle, Patrouille, Poursuite, Attaque,
// Retour, Mort), perception visuelle et auditive, NavMesh guard, sons 3D et dégâts.
public class ZombieEnemy : MonoBehaviour
{
    // ─── Références ───────────────────────────────────────────────────────

    [Header("Références")]

    // Animator du zombie, utilisé pour piloter les transitions d'animation.
    public Animator animator;

    // Transform du joueur, cible principale de l'IA.
    public Transform player;

    // Agent NavMesh gérant le déplacement pathfinding du zombie.
    private NavMeshAgent agent;

    // Composant de santé du joueur, utilisé pour infliger des dégâts et détecter sa mort.
    private PlayerHealth playerHealth;

    // Stats injectées par le WaveManager au spawn (vitesse, numéro de vague).
    private ZombieStats stats;


    // ─── Paramètres de combat ─────────────────────────────────────────────

    [Header("Paramètres de combat")]

    // Points de vie maximaux du zombie.
    [SerializeField] private int maxHealth = 100;

    // Distance en unités Unity à partir de laquelle le zombie peut attaquer le joueur.
    [SerializeField] private float attackRange = 2f;

    // Délai minimum en secondes entre deux attaques consécutives.
    [SerializeField] private float attackCooldown = 2f;

    // Dégâts infligés au joueur à chaque attaque.
    [SerializeField] private int attackDamage = 10;


    // ─── Détection ────────────────────────────────────────────────────────

    [Header("Détection")]

    // Distance maximale à laquelle le zombie peut détecter le joueur visuellement (raycasting).
    [SerializeField] private float detectionRange = 12f;

    // Distance à laquelle le zombie "entend" le joueur sans ligne de vue directe.
    [SerializeField] private float hearingRange = 6f;

    // Temps en secondes sans percevoir le joueur avant de repasser en état Returning.
    [SerializeField] private float losePlayerTime = 4f;

    // Masque de layers utilisé pour le raycasting de ligne de vue (murs, obstacles...).
    [SerializeField] private LayerMask obstacleMask;


    // ─── Mouvement ────────────────────────────────────────────────────────

    [Header("Mouvement")]

    // Vitesse de déplacement en patrouille (marche lente).
    [SerializeField] private float walkSpeed = 1.5f;

    // Vitesse de déplacement en poursuite (course).
    [SerializeField] private float runSpeed = 4f;

    // Vitesse de rotation interpolée pour que le zombie tourne progressivement vers sa cible.
    [SerializeField] private float rotateSpeed = 5f;


    // ─── Patrouille NavMesh ───────────────────────────────────────────────

    [Header("Patrouille NavMesh")]

    // Rayon en unités Unity dans lequel le zombie cherche un point de patrouille aléatoire.
    [SerializeField] private float patrolRadius = 8f;

    // Durée minimale d'attente sur un point de patrouille avant de repartir.
    [SerializeField] private float patrolWaitMin = 2f;

    // Durée maximale d'attente sur un point de patrouille avant de repartir.
    [SerializeField] private float patrolWaitMax = 5f;

    // Nombre de tentatives pour trouver un point de patrouille valide sur le NavMesh.
    [SerializeField] private int patrolSamples = 5;


    // ─── Mort ─────────────────────────────────────────────────────────────

    [Header("Mort")]

    // Délai en secondes avant la destruction du GameObject après la mort.
    [SerializeField] private float timeBeforeDestroy = 5f;


    // ─── Sécurité NavMesh ─────────────────────────────────────────────────

    [Header("Sécurité NavMesh")]

    // Rayon de recherche en unités Unity utilisé pour replacer le zombie sur le NavMesh.
    [SerializeField] private float snapRadius = 8f;

    // Intervalle en secondes entre deux vérifications de position NavMesh.
    [SerializeField] private float navCheckInterval = 0.4f;

    // Durée maximale tolérée sur un OffMeshLink avant forçage de complétion.
    [SerializeField] private float maxLinkTime = 2f;

    // Délai de grâce en secondes après le spawn avant d'activer le NavMesh Guard
    // (évite un snap prématuré pendant que le NavMesh se finalise).
    [SerializeField] private float snapGraceDelay = 2f;


    // ─── Sons — Pas ───────────────────────────────────────────────────────

    [Header("Sons — Pas")]

    // Pool de sons de marche tirés aléatoirement pour varier les pas.
    [Tooltip("Pool de sons de marche (tirés aléatoirement)")]
    [SerializeField] private AudioClip[] walkFootsteps;

    // Pool de sons de course tirés aléatoirement pour varier les pas.
    [Tooltip("Pool de sons de course (tirés aléatoirement)")]
    [SerializeField] private AudioClip[] runFootsteps;

    // Intervalle en secondes entre deux sons de pas en marchant.
    [SerializeField] private float walkStepInterval = 0.55f;

    // Intervalle en secondes entre deux sons de pas en courant.
    [SerializeField] private float runStepInterval = 0.3f;

    // Volume des sons de pas (0 = muet, 1 = plein volume).
    [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.8f;


    // ─── Sons — Zombie ────────────────────────────────────────────────────

    [Header("Sons — Zombie")]

    // Pool de grognements et bruits ambiants joués périodiquement, quelle que soit l'action.
    [Tooltip("Grognements / bruits ambiants joués aléatoirement")]
    [SerializeField] private AudioClip[] idleSounds;

    // Intervalle minimal en secondes entre deux bruits ambiants.
    [Tooltip("Intervalle min/max entre deux bruits ambiants (secondes)")]
    [SerializeField] private float idleSoundIntervalMin = 4f;

    // Intervalle maximal en secondes entre deux bruits ambiants.
    [SerializeField] private float idleSoundIntervalMax = 10f;

    // Volume des bruits ambiants (0 = muet, 1 = plein volume).
    [SerializeField][Range(0f, 1f)] private float idleVolume = 0.9f;


    // ─── Sons — Dégâts & Mort ─────────────────────────────────────────────

    [Header("Sons — Dégâts & Mort")]

    // Pool de sons joués aléatoirement lorsque le zombie reçoit des dégâts.
    [SerializeField] private AudioClip[] hurtSounds;

    // Son joué une seule fois au moment de la mort.
    [SerializeField] private AudioClip deathSound;

    // Son joué lorsque le corps du zombie touche le sol après la mort.
    [SerializeField] private AudioClip landSound;

    // Délai en secondes entre la mort et le son de chute au sol.
    [Tooltip("Délai entre la mort et le bruit de chute au sol")]
    [SerializeField] private float landDelay = 0.6f;

    // Volume des sons de dégâts.
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 1f;

    // Volume du son de mort et de chute.
    [SerializeField][Range(0f, 1f)] private float deathVolume = 1f;


    // ─── Son 3D — Portée ──────────────────────────────────────────────────

    [Header("Son 3D — Portée")]

    // Distance minimale en unités Unity à partir de laquelle le son est au volume maximum.
    [SerializeField] private float audioMinDistance = 1f;

    // Distance maximale en unités Unity au-delà de laquelle le son n'est plus audible.
    [SerializeField] private float audioMaxDistance = 20f;


    // ─── Sources audio ────────────────────────────────────────────────────

    // Source dédiée aux sons de pas, jouée en mode "clip" pour contrôler l'intervalle.
    private AudioSource stepsSource;

    // Source dédiée aux FX ponctuels (grognements, dégâts, mort).
    private AudioSource fxSource;


    // ─── Timers sons ──────────────────────────────────────────────────────

    // Accumulateur de temps pour déclencher les pas à l'intervalle correct.
    private float _stepTimer = 0f;

    // Accumulateur de temps pour déclencher les bruits ambiants.
    private float _idleTimer = 0f;

    // Intervalle aléatoire actuel entre deux bruits ambiants.
    private float _idleInterval = 0f;

    // Index du dernier son de pas joué, pour éviter deux fois le même son consécutif.
    private int _lastStepIdx = -1;

    // Index du dernier bruit ambiant joué, pour éviter deux fois le même son consécutif.
    private int _lastIdleIdx = -1;


    // ─── Paramètres Animator ──────────────────────────────────────────────

    // Constantes des noms de paramètres Animator pour éviter les fautes de frappe.
    private const string PARAM_IS_RUNNING = "IsRunning";
    private const string PARAM_ATTACK = "Attack";
    private const string PARAM_DEATH = "Death";


    // ─── État interne ─────────────────────────────────────────────────────

    // Points de vie courants du zombie.
    private int currentHealth;

    // Passe à true au moment de la mort pour bloquer toute logique ultérieure.
    private bool isDead = false;

    // Timestamp de la dernière attaque pour respecter le cooldown.
    private float lastAttackTime = 0f;

    // Compteur de temps depuis que le zombie a perdu la trace du joueur.
    private float losePlayerTimer = 0f;

    // True si le zombie perçoit actuellement le joueur (vue ou ouïe).
    private bool canSeePlayer = false;

    // Dernière position connue du joueur, utilisée pour la poursuite hors ligne de vue.
    private Vector3 lastKnownPlayerPos;

    // Position de spawn du zombie, utilisée comme point de retour en état Returning.
    private Vector3 spawnPosition;


    // ─── Patrouille ───────────────────────────────────────────────────────

    // Compteur de temps d'attente sur le point de patrouille courant.
    private float patrolWaitTimer = 0f;

    // Durée aléatoire d'attente tirée à chaque arrivée sur un point de patrouille.
    private float patrolWaitDuration = 0f;

    // True quand le zombie attend sur un point de patrouille avant de repartir.
    private bool isWaitingAtPoint = true;


    // ─── NavMesh Guard ────────────────────────────────────────────────────

    // Timer pour la vérification périodique du NavMesh.
    private float _navCheckTimer = 0f;

    // Durée passée sur un OffMeshLink, pour détecter un blocage.
    private float _onLinkTimer = 0f;

    // Timer du délai de grâce post-spawn avant activation du guard.
    private float _graceTimer = 0f;

    // Dernière position valide sur le NavMesh, utilisée comme fallback si le snap échoue.
    private Vector3 _lastValidPos;


    // ─── Machine d'états ──────────────────────────────────────────────────

    // Tous les états possibles du zombie.
    public enum ZombieState { Idle, Patrolling, Chasing, Attacking, Returning, Dead }

    // État courant du zombie, géré par SetState() et mis à jour dans UpdateStateMachine().
    private ZombieState currentState = ZombieState.Idle;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Awake()
    {
        // Récupère les stats injectées par WaveManager dès l'Awake pour les avoir dans Start().
        stats = GetComponent<ZombieStats>();
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Mémorise la position de spawn pour le retour en état Returning.
        spawnPosition = transform.position;
        _lastValidPos = transform.position;

        // Applique la vitesse de vague avant d'initialiser le NavMeshAgent.
        ApplyWaveStats();
        InitComponents();
        InitAudio();
        SetState(ZombieState.Idle);

        // Tire un premier intervalle aléatoire pour les bruits ambiants.
        _idleInterval = Random.Range(idleSoundIntervalMin, idleSoundIntervalMax);
    }


    // ─── Initialisation audio ─────────────────────────────────────────────

    void InitAudio()
    {
        // Deux sources séparées : une pour les pas (évite les chevauchements avec les FX),
        // une pour les sons ponctuels (dégâts, mort, grognements).
        stepsSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(stepsSource);
        stepsSource.loop = false;

        fxSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(fxSource);
        fxSource.loop = false;
    }

    // Configure une AudioSource en son 3D linéaire avec les distances définies dans l'Inspector.
    void ConfigureSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.spatialBlend = 1f;                       // 100% 3D : le volume varie avec la distance.
        src.rolloffMode = AudioRolloffMode.Linear;   // Atténuation linéaire plus prévisible.
        src.minDistance = audioMinDistance;
        src.maxDistance = audioMaxDistance;
        src.dopplerLevel = 0f;                       // Désactive l'effet Doppler (peu réaliste sur des zombies lents).
    }

    // Applique la vitesse de la vague courante depuis ZombieStats.
    // La marche est définie à 45 % de la vitesse de course pour garder un ratio cohérent.
    void ApplyWaveStats()
    {
        if (stats == null) return;
        runSpeed = stats.moveSpeed;
        walkSpeed = stats.moveSpeed * 0.45f;
    }

    // Initialise le NavMeshAgent, l'Animator et la référence au joueur.
    void InitComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) { Debug.LogError("NavMeshAgent manquant sur " + name); enabled = false; return; }

        agent.speed = walkSpeed;

        // stoppingDistance légèrement inférieur à attackRange pour que le zombie s'arrête juste avant de toucher.
        agent.stoppingDistance = attackRange * 0.8f;

        // angularSpeed = 0 : Unity ne pilote pas la rotation — c'est SmoothRotateTowards() qui s'en charge.
        agent.angularSpeed = 0f;

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) { Debug.LogError("Animator manquant sur " + name); enabled = false; return; }

        TryFindPlayer();
    }

    // Cherche le joueur par tag si la référence n'est pas encore assignée.
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


    // ─── Boucle principale ────────────────────────────────────────────────

    void Update()
    {
        if (isDead) return;

        // Tente de trouver le joueur si la référence est perdue (ex : respawn).
        if (player == null) { TryFindPlayer(); return; }

        // Ne fait rien si l'agent n'est pas encore sur le NavMesh (évite les erreurs de pathfinding).
        if (agent == null || !agent.isOnNavMesh) return;

        UpdateNavMeshGuard();
        UpdatePerception();
        UpdateStateMachine();
        UpdateSounds();
    }


    // ─── Sons ─────────────────────────────────────────────────────────────

    void UpdateSounds()
    {
        UpdateFootsteps();
        UpdateIdleSounds();
    }

    void UpdateFootsteps()
    {
        bool moving = agent.velocity.magnitude > 0.3f;

        // Si le zombie est arrêté, coupe les pas et remet le timer à zéro.
        if (!moving) { if (stepsSource.isPlaying) stepsSource.Stop(); _stepTimer = 0f; return; }

        // Détermine le pool et l'intervalle selon l'état : course (Chasing/Attacking) ou marche.
        bool sprinting = currentState == ZombieState.Chasing || currentState == ZombieState.Attacking;
        float interval = sprinting ? runStepInterval : walkStepInterval;
        AudioClip[] pool = sprinting ? runFootsteps : walkFootsteps;

        _stepTimer += Time.deltaTime;

        // Déclenche le prochain pas uniquement si l'intervalle est écoulé et qu'aucun son ne joue encore.
        if (_stepTimer >= interval && !stepsSource.isPlaying)
        {
            _stepTimer = 0f;
            PlayRandomFromPool(pool, stepsSource, footstepVolume, ref _lastStepIdx);
        }
    }

    void UpdateIdleSounds()
    {
        // Les bruits ambiants jouent périodiquement indépendamment de l'état du zombie.
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _idleInterval)
        {
            _idleTimer = 0f;

            // Tire un nouvel intervalle aléatoire pour le prochain bruit.
            _idleInterval = Random.Range(idleSoundIntervalMin, idleSoundIntervalMax);
            PlayRandomFromPool(idleSounds, fxSource, idleVolume, ref _lastIdleIdx);
        }
    }

    // Pioche un clip aléatoire dans un pool en évitant de rejouer le même que le précédent.
    // lastIdx est passé par référence pour mémoriser le choix entre les appels.
    void PlayRandomFromPool(AudioClip[] pool, AudioSource src, float volume, ref int lastIdx)
    {
        if (pool == null || pool.Length == 0 || src == null) return;

        int idx;
        int attempts = 0;

        // Boucle jusqu'à trouver un index différent du précédent (max 10 tentatives).
        do { idx = Random.Range(0, pool.Length); attempts++; }
        while (idx == lastIdx && pool.Length > 1 && attempts < 10);

        lastIdx = idx;
        if (pool[idx] == null) return;

        src.clip = pool[idx];
        src.volume = volume;
        src.Play();
    }

    // Joue un clip ponctuel en one-shot sur la source FX.
    void PlayFX(AudioClip clip, float volume)
    {
        if (fxSource == null || clip == null) return;
        fxSource.PlayOneShot(clip, volume);
    }


    // ─── NavMesh Guard ────────────────────────────────────────────────────

    // Surveille la position du zombie sur le NavMesh et le replace si nécessaire.
    // Gère trois cas : délai de grâce post-spawn, blocage sur OffMeshLink, et sortie du NavMesh.
    void UpdateNavMeshGuard()
    {
        if (agent == null || !agent.enabled) return;

        // Délai de grâce : on attend snapGraceDelay secondes avant d'activer le guard
        // pour laisser le NavMesh se stabiliser après le spawn.
        if (_graceTimer < snapGraceDelay)
        {
            _graceTimer += Time.deltaTime;
            if (agent.isOnNavMesh) _lastValidPos = transform.position;
            return;
        }

        // Détecte un blocage sur un OffMeshLink (pont, saut...) dépassant maxLinkTime.
        if (agent.isOnOffMeshLink)
        {
            _onLinkTimer += Time.deltaTime;
            if (_onLinkTimer >= maxLinkTime) { ForceCompleteOffMeshLink(); _onLinkTimer = 0f; }
            return;
        }
        _onLinkTimer = 0f;

        // Si le zombie est correctement sur le NavMesh, mémorise sa position.
        if (agent.isOnNavMesh) { _lastValidPos = transform.position; return; }

        // Si le zombie est hors NavMesh, tente un snap périodique.
        _navCheckTimer += Time.deltaTime;
        if (_navCheckTimer >= navCheckInterval) { _navCheckTimer = 0f; SnapToNavMesh(); }
    }

    // Force la complétion d'un OffMeshLink bloqué et warp le zombie à sa destination.
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

    // Replace le zombie sur le NavMesh en cas de sortie.
    // Essaie d'abord depuis la position courante, puis depuis la dernière position valide.
    void SnapToNavMesh()
    {
        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(transform.position, out hit, snapRadius, NavMesh.AllAreas);
        if (!found) found = NavMesh.SamplePosition(_lastValidPos, out hit, snapRadius, NavMesh.AllAreas);

        if (found)
        {
            // Désactive temporairement l'agent pour le Warp (requis par Unity NavMesh).
            agent.enabled = false;
            transform.position = hit.position;
            agent.enabled = true;
            _lastValidPos = hit.position;

            // Restaure la destination si un chemin était calculé avant le snap.
            if (agent.hasPath) { Vector3 dest = agent.destination; agent.ResetPath(); agent.SetDestination(dest); }
        }
        else Debug.LogWarning($"[NavMeshGuard] {name} introuvable sur NavMesh dans {snapRadius}m.");
    }


    // ─── Perception ───────────────────────────────────────────────────────

    void UpdatePerception()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);

        // Teste la ligne de vue via un Linecast : si aucun obstacle ne bloque, le zombie voit le joueur.
        bool lineOfSight = false;
        if (dist <= detectionRange)
        {
            // Décale les origines à hauteur de tête pour un raycasting plus réaliste.
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = player.position + Vector3.up * 1.0f;
            if (!Physics.Linecast(origin, target, obstacleMask)) lineOfSight = true;
        }

        // Le zombie "entend" le joueur si celui-ci est dans hearingRange, même sans ligne de vue.
        bool hearsPlayer = dist <= hearingRange;
        canSeePlayer = lineOfSight || hearsPlayer;

        if (canSeePlayer)
        {
            // Met à jour la dernière position connue et remet à zéro le timer de perte.
            lastKnownPlayerPos = player.position;
            losePlayerTimer = 0f;
        }
        else if (currentState == ZombieState.Chasing || currentState == ZombieState.Attacking)
        {
            // Incrémente le timer de perte uniquement si le zombie était en train de poursuivre.
            losePlayerTimer += Time.deltaTime;
        }
    }


    // ─── Machine d'états ──────────────────────────────────────────────────

    void UpdateStateMachine()
    {
        // Si le joueur est mort, le zombie repasse en patrouille sans chercher de nouvelle cible.
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
                // Dès que le joueur est perçu, passe en poursuite.
                if (canSeePlayer) SetState(ZombieState.Chasing);
                break;

            case ZombieState.Chasing:
                // Passe en attaque dès que la portée d'attaque est atteinte.
                if (dist <= attackRange) SetState(ZombieState.Attacking);
                // Abandonne la poursuite si le joueur est perdu trop longtemps.
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime) SetState(ZombieState.Returning);
                break;

            case ZombieState.Attacking:
                // Repasse en poursuite si le joueur s'éloigne au-delà de 130 % de attackRange.
                if (dist > attackRange * 1.3f) SetState(ZombieState.Chasing);
                else if (!canSeePlayer && losePlayerTimer >= losePlayerTime) SetState(ZombieState.Returning);
                break;

            case ZombieState.Returning:
                // Reprend la poursuite si le joueur est à nouveau perçu pendant le retour.
                if (canSeePlayer) SetState(ZombieState.Chasing);
                // Passe en patrouille une fois arrivé à destination.
                else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                    SetState(ZombieState.Patrolling);
                break;
        }

        ExecuteState();
    }

    // Effectue la transition vers un nouvel état : configure le NavMeshAgent et l'Animator en conséquence.
    void SetState(ZombieState newState)
    {
        if (newState == currentState) return;
        currentState = newState;
        if (agent == null || !agent.isOnNavMesh) return;

        switch (newState)
        {
            case ZombieState.Idle:
                // Arrête le zombie et lance un timer d'attente aléatoire avant la prochaine patrouille.
                agent.isStopped = true; agent.ResetPath();
                animator.SetBool(PARAM_IS_RUNNING, false);
                patrolWaitTimer = 0f;
                patrolWaitDuration = Random.Range(patrolWaitMin, patrolWaitMax);
                isWaitingAtPoint = true;
                break;

            case ZombieState.Patrolling:
                // Reprend le mouvement à vitesse de marche et cherche un point de patrouille.
                agent.speed = walkSpeed; agent.isStopped = false;
                TrySetPatrolDestination();
                break;

            case ZombieState.Chasing:
                // Accélère à la vitesse de course et active l'animation de course.
                agent.speed = runSpeed; agent.isStopped = false;
                animator.SetBool(PARAM_IS_RUNNING, true);
                break;

            case ZombieState.Attacking:
                // Arrête le mouvement et prépare le trigger d'attaque (reset préventif pour éviter les doublons).
                agent.isStopped = true; agent.ResetPath();
                animator.SetBool(PARAM_IS_RUNNING, false);
                animator.ResetTrigger(PARAM_ATTACK);
                break;

            case ZombieState.Returning:
                // Repasse en marche et se dirige vers la dernière position connue du joueur,
                // puis une coroutine envoie vers le spawn après un court délai.
                agent.speed = walkSpeed; agent.isStopped = false;
                animator.SetBool(PARAM_IS_RUNNING, false);
                agent.SetDestination(lastKnownPlayerPos);
                StartCoroutine(ReturnToSpawnAfterDelay(1.5f));
                break;
        }
    }

    // Se dirige d'abord vers la dernière position connue du joueur, puis vers le spawn.
    // Le délai permet au zombie de "fouiller" brièvement avant de renoncer.
    IEnumerator ReturnToSpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == ZombieState.Returning) agent.SetDestination(spawnPosition);
    }

    // Exécute la logique frame-par-frame de l'état courant.
    void ExecuteState()
    {
        switch (currentState)
        {
            case ZombieState.Idle: ExecuteIdle(); break;
            case ZombieState.Patrolling: ExecutePatrol(); break;
            case ZombieState.Chasing: ExecuteChase(); break;
            case ZombieState.Attacking: ExecuteAttack(); break;
            case ZombieState.Returning:
                // En retour, oriente simplement le zombie vers le prochain point de chemin.
                if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget); break;
        }
    }

    void ExecuteIdle()
    {
        // Attend la fin de la durée d'idle avant de passer en patrouille.
        patrolWaitTimer += Time.deltaTime;
        if (patrolWaitTimer >= patrolWaitDuration) SetState(ZombieState.Patrolling);
    }

    void ExecutePatrol()
    {
        if (isWaitingAtPoint)
        {
            // En attente sur un point : comptabilise le temps avant de repartir.
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitDuration)
            {
                isWaitingAtPoint = false;
                TrySetPatrolDestination();
            }
        }
        else
        {
            // En déplacement : oriente le zombie et vérifie l'arrivée à destination.
            if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                // Arrivé à destination : passe en attente sur ce point.
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
        // Suit la position courante du joueur si visible, sinon sa dernière position connue.
        agent.SetDestination(canSeePlayer ? player.position : lastKnownPlayerPos);
        if (agent.hasPath) SmoothRotateTowards(agent.steeringTarget);
        animator.SetBool(PARAM_IS_RUNNING, true);
    }

    void ExecuteAttack()
    {
        // Tourne en continu vers le joueur pendant l'attaque.
        SmoothRotateTowards(player.position);

        // Déclenche le trigger d'attaque dès que le cooldown est écoulé.
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            // Reset préventif pour éviter qu'un trigger précédent non consommé reste en attente.
            animator.ResetTrigger(PARAM_ATTACK);
            animator.SetTrigger(PARAM_ATTACK);
            lastAttackTime = Time.time;
        }
    }


    // ─── Patrouille ───────────────────────────────────────────────────────

    // Cherche un point de patrouille valide sur le NavMesh dans patrolRadius.
    // Essaie patrolSamples fois : si aucun chemin complet n'est trouvé, repasse en Idle.
    void TrySetPatrolDestination()
    {
        for (int i = 0; i < patrolSamples; i++)
        {
            // Génère une direction aléatoire dans la sphère, puis écrase Y pour rester au sol.
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;
            randomDir.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
            {
                // Vérifie qu'un chemin complet (sans coupure) existe avant d'assigner la destination.
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

        // Aucun point valide trouvé après patrolSamples tentatives : repasse en Idle.
        SetState(ZombieState.Idle);
    }

    // Interpole la rotation du zombie vers une cible via Slerp pour un mouvement fluide.
    // L'axe Y est écrasé à 0 pour que le zombie ne s'incline pas vers le haut ou le bas.
    void SmoothRotateTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
    }


    // ─── Dégâts & Mort ────────────────────────────────────────────────────

    // Appelé par l'Animator via Animation Event au moment de l'impact de l'attaque.
    // Vérifie la distance réelle avant d'infliger des dégâts pour éviter les hits fantômes.
    public void DealDamageToPlayer()
    {
        if (isDead || player == null || playerHealth == null) return;
        float dist = Vector3.Distance(transform.position, player.position);

        // Marge de 150 % de attackRange pour absorber les légères imprécisions de position.
        if (dist <= attackRange * 1.5f)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"{name} frappe le joueur ! -{attackDamage} HP");
        }
    }

    // Inflige des dégâts au zombie et déclenche mort ou alerte selon le résultat.
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

        // Joue un son de dégâts aléatoire.
        if (hurtSounds != null && hurtSounds.Length > 0)
            PlayFX(hurtSounds[Random.Range(0, hurtSounds.Length)], hurtVolume);

        // Si le zombie ne voyait pas le joueur, son attaque l'alerte et le met en poursuite.
        if (!canSeePlayer && player != null) { lastKnownPlayerPos = player.position; SetState(ZombieState.Chasing); }

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        currentState = ZombieState.Dead;

        // Stoppe toutes les coroutines en cours (patrouille, retour, sons...).
        StopAllCoroutines();

        // Coupe les sons de pas immédiatement.
        if (stepsSource != null) stepsSource.Stop();

        // Joue le son de mort puis le son de chute au sol après un délai.
        PlayFX(deathSound, deathVolume);
        StartCoroutine(PlayLandSoundDelayed());

        // Déclenche l'animation de mort.
        animator.SetTrigger(PARAM_DEATH);
        animator.SetBool(PARAM_IS_RUNNING, false);

        // Arrête et désactive l'agent NavMesh pour que le zombie ne continue pas à se déplacer.
        agent.isStopped = true; agent.enabled = false;

        // Désactive le collider pour que les balles et les physiques ignorent le corps.
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Passe le Rigidbody en kinematic pour éviter que le corps ne soit projeté par la physique.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Notifie le WaveManager via ZombieDeathNotifier pour décrémenter le compteur de la vague.
        ZombieDeathNotifier notifier = GetComponent<ZombieDeathNotifier>();
        if (notifier != null) notifier.NotifyDeath();

        // Détruit le GameObject après timeBeforeDestroy secondes (laisse l'animation se jouer).
        Destroy(gameObject, timeBeforeDestroy);
    }

    // Joue le son de chute au sol après le délai configuré (synchronisé avec l'animation).
    IEnumerator PlayLandSoundDelayed()
    {
        yield return new WaitForSeconds(landDelay);
        PlayFX(landSound, deathVolume);
    }


    // ─── Getters publics ──────────────────────────────────────────────────

    // Retourne les points de vie courants du zombie.
    public int GetHealth() => currentHealth;

    // Retourne les points de vie maximaux du zombie.
    public int GetMaxHealth() => maxHealth;

    // Retourne true si le zombie est mort.
    public bool IsDead() => isDead;

    // Retourne l'état courant du zombie (utile pour le debug et l'UI).
    public ZombieState GetCurrentState() => currentState;


    // ─── Gizmos (debug éditeur) ───────────────────────────────────────────

    // Affiche dans la Scene View les rayons de détection, d'attaque et de patrouille.
    void OnDrawGizmosSelected()
    {
        // Rouge : portée d'attaque.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Jaune : portée de détection visuelle.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Cyan : portée d'écoute.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Vert semi-transparent : rayon de patrouille.
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // Ligne joueur → zombie : rouge si visible, gris sinon.
        if (player != null && !isDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= detectionRange)
            {
                Gizmos.color = canSeePlayer ? Color.red : Color.gray;
                Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up);
            }
        }

        // Sphère magenta : dernière position connue du joueur (visible en état Returning).
        if (Application.isPlaying && currentState == ZombieState.Returning)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPlayerPos, 0.3f);
        }

        // Sphère verte : dernière position valide sur le NavMesh (debug du NavMesh Guard).
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_lastValidPos, 0.15f);
        }
    }
}