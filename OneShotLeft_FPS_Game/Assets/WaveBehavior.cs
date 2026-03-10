using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gestionnaire de vagues de zombies.
/// - 5 vagues, chaque vague les zombies sont plus nombreux et plus rapides.
/// - Spawn dans toutes les pièces ET les couloirs (sauf la pièce de spawn joueur).
/// - Après la vague 5 : écran de victoire.
/// - L'UI est gérée externement par WaveUI.cs — ce script n'en a pas besoin.
///
/// SETUP :
///   1. Ajoute ce script sur un GameObject vide "WaveManager" dans la scène.
///   2. Assigne zombiePrefab, player et victoryPanel dans l'Inspector.
///   3. Le WaveManager est appelé automatiquement par MapGenerator après la génération.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ── Références ────────────────────────────────────────────────────────
    [Header("Références")]
    [Tooltip("Prefab zombie (doit avoir un composant ZombieStats)")]
    [SerializeField] private GameObject zombiePrefab;

    [Tooltip("Le joueur (pour exclure sa zone du spawn)")]
    [SerializeField] private GameObject player;

    // ── Paramètres des vagues ─────────────────────────────────────────────
    [Header("Paramètres des vagues")]
    [Tooltip("Nombre total de vagues")]
    [SerializeField] private int totalWaves = 5;

    [Tooltip("Nombre de zombies à la vague 1")]
    [SerializeField] private int baseZombieCount = 5;

    [Tooltip("Zombies supplémentaires ajoutés à chaque vague")]
    [SerializeField] private int zombiesPerWaveIncrease = 3;

    [Tooltip("Vitesse de base des zombies (vague 1)")]
    [SerializeField] private float baseSpeed = 2f;

    [Tooltip("Vitesse ajoutée à chaque vague")]
    [SerializeField] private float speedPerWave = 0.4f;

    [Tooltip("Délai en secondes entre deux vagues")]
    [SerializeField] private float timeBetweenWaves = 5f;

    [Tooltip("Offset Y de spawn des zombies au-dessus du sol")]
    [SerializeField] private float spawnYOffset = 0.1f;

    // ── État interne ──────────────────────────────────────────────────────
    // Commence à 1 : la première vague affichera directement "01"
    private int currentWave = 1;
    private int aliveCount = 0;
    private bool gameFinished = false;

    // Données de map passées par MapGenerator via InitAndStart()
    private List<RectInt> rooms = new List<RectInt>();
    private TileType[,] grid;
    private int mapWidth, mapHeight;
    private float tileSize;
    private float floorYOffset;

    // Enum exposé pour que MapGenerator puisse convertir sa grille
    public enum TileType { Empty, Floor, Corridor }

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
    }

    /// <summary>
    /// Appelé par MapGenerator une fois la map générée.
    /// Lance immédiatement la première vague.
    /// </summary>
    public void InitAndStart(
        List<RectInt> roomList,
        TileType[,] mapGrid,
        int width,
        int height,
        float tile,
        float yOffset)
    {
        rooms = new List<RectInt>(roomList);
        grid = mapGrid;
        mapWidth = width;
        mapHeight = height;
        tileSize = tile;
        floorYOffset = yOffset;

        StartCoroutine(RunWaves());
    }

    // ─── Boucle principale des vagues ─────────────────────────────────────
    IEnumerator RunWaves()
    {
        for (int wave = 1; wave <= totalWaves; wave++)
        {
            currentWave = wave;

            // Compte à rebours entre vagues (sauf avant la première)
            if (wave > 1)
                yield return StartCoroutine(Countdown(timeBetweenWaves));

            yield return StartCoroutine(SpawnWave(wave));

            // Attend que tous les zombies de la vague soient morts
            yield return StartCoroutine(WaitForWaveClear());

            yield return new WaitForSeconds(wave < totalWaves ? 1.5f : 0f);
        }

        TriggerVictory();
    }

    // ─── Compte à rebours ─────────────────────────────────────────────────
    IEnumerator Countdown(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
    }

    // ─── Spawn d'une vague ────────────────────────────────────────────────
    IEnumerator SpawnWave(int wave)
    {
        int count = baseZombieCount + (wave - 1) * zombiesPerWaveIncrease;
        float speed = baseSpeed + (wave - 1) * speedPerWave;

        List<Vector2Int> spawnTiles = GetValidSpawnTiles();

        if (spawnTiles.Count == 0)
        {
            Debug.LogWarning("WaveManager: aucune tile de spawn disponible !");
            yield break;
        }

        aliveCount = 0;

        for (int i = 0; i < count; i++)
        {
            Vector2Int tile = spawnTiles[Random.Range(0, spawnTiles.Count)];
            Vector3 pos = new Vector3(tile.x * tileSize, floorYOffset + spawnYOffset, tile.y * tileSize);

            // Trouve le point NavMesh le plus proche pour éviter "not close enough to NavMesh"
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(pos, out navHit, 3f, NavMesh.AllAreas))
                pos = new Vector3(navHit.position.x, navHit.position.y + spawnYOffset, navHit.position.z);
            else
            {
                // Aucun NavMesh trouvé dans 3m — on skip ce zombie
                Debug.LogWarning($"WaveManager: aucun NavMesh trouvé pour spawner un zombie à {pos}, tile ignorée.");
                continue;
            }

            GameObject go = Instantiate(
                zombiePrefab,
                pos,
                Quaternion.Euler(0, Random.Range(0f, 360f), 0)
            );

            // Injecte les stats de vague
            ZombieStats stats = go.GetComponent<ZombieStats>();
            if (stats != null) stats.Init(speed, wave);

            // Abonnement mort zombie
            ZombieDeathNotifier notifier = go.GetComponent<ZombieDeathNotifier>();
            if (notifier == null) notifier = go.AddComponent<ZombieDeathNotifier>();
            notifier.OnDeath += OnZombieDied;

            aliveCount++;

            // Étale le spawn sur plusieurs frames pour éviter un freeze
            if (i % 5 == 4) yield return null;
        }

        Debug.Log($"Vague {wave} : {count} zombies, vitesse {speed:F1}");
    }

    // ─── Attend que la vague soit vidée ───────────────────────────────────
    IEnumerator WaitForWaveClear()
    {
        while (aliveCount > 0)
            yield return new WaitForSeconds(0.5f);
    }

    // ─── Callback mort zombie ─────────────────────────────────────────────
    void OnZombieDied()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    // ─── Collecte les tiles de spawn valides ──────────────────────────────
    List<Vector2Int> GetValidSpawnTiles()
    {
        var tiles = new List<Vector2Int>();

        // Pièces (sauf la pièce 0 = spawn joueur)
        for (int i = 1; i < rooms.Count; i++)
        {
            RectInt r = rooms[i];
            for (int x = r.x + 1; x < r.x + r.width - 1; x++)
                for (int y = r.y + 1; y < r.y + r.height - 1; y++)
                    tiles.Add(new Vector2Int(x, y));
        }

        // Couloirs
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                if (grid[x, y] == TileType.Corridor)
                    tiles.Add(new Vector2Int(x, y));

        // Exclut les tiles trop proches du joueur (rayon 3 tiles)
        if (player != null)
        {
            Vector2Int playerTile = new Vector2Int(
                Mathf.RoundToInt(player.transform.position.x / tileSize),
                Mathf.RoundToInt(player.transform.position.z / tileSize)
            );
            tiles.RemoveAll(t => Vector2Int.Distance(t, playerTile) < 3f);
        }

        return tiles;
    }

    // ─── Victoire ─────────────────────────────────────────────────────────
    void TriggerVictory()
    {
        gameFinished = true;

        // Appelle le VictoryScreen s'il existe dans la scène
        VictoryScreen vs = Object.FindFirstObjectByType<VictoryScreen>();
        if (vs != null)
            vs.ShowVictoryScreen();
        else
            Debug.LogWarning("WaveManager: VictoryScreen non trouvé dans la scène !");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("WaveManager: victoire !");
    }

    // ─── Getter public (utilisé par WaveUI) ──────────────────────────────
    public int GetCurrentWave() => currentWave;
    public int GetAliveCount() => aliveCount;
    public int GetTotalWaves() => totalWaves;
    public bool IsGameFinished() => gameFinished;
}


// ══════════════════════════════════════════════════════════════════════════════
// ZombieStats — À attacher sur le prefab zombie.
// ══════════════════════════════════════════════════════════════════════════════
public class ZombieStats : MonoBehaviour
{
    [Header("Stats (injectées par le WaveManager)")]
    public float moveSpeed = 2f;
    public int wave = 1;

    public void Init(float speed, int waveNumber)
    {
        moveSpeed = speed;
        wave = waveNumber;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.speed = moveSpeed;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
            foreach (var p in anim.parameters)
                if (p.name == "Speed") { anim.SetFloat("Speed", moveSpeed); break; }
    }
}


// ══════════════════════════════════════════════════════════════════════════════
// ZombieDeathNotifier — Notifie le WaveManager quand un zombie meurt.
// ══════════════════════════════════════════════════════════════════════════════
public class ZombieDeathNotifier : MonoBehaviour
{
    public event System.Action OnDeath;
    private bool notified = false;

    public void NotifyDeath()
    {
        if (notified) return;
        notified = true;
        OnDeath?.Invoke();
    }

    void OnDestroy()
    {
        if (!notified) NotifyDeath();
    }
}