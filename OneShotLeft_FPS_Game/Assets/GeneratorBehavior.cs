using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ═══════════════════════════════════════════════════════════════════════════════
// MapGenerator — Génère procéduralement une map de type entrepôt à partir d'une
// grille 2D. Il place des pièces rectangulaires, les connecte avec des couloirs,
// puis instancie les prefabs de sol, plafond, murs et portes en 3D.
// Une caméra cinématique survole la map pendant la génération, puis une
// transition fluide ramène la vue sur le joueur.
// Nous avons effectivement pas appris cela en cours, mais c'est un excellent exemple
// de système de génération procédurale. je me suis aidé des resources internet et des videos
// youtubes pour le construire, et j'ai essayé de faire un code propre et commenté
// pour que ce soit facile à comprendre et à modifier.
// Merci d'avoir lu, Arthur.

// ═══════════════════════════════════════════════════════════════════════════════
public class MapGenerator : MonoBehaviour
{
    // ── SINGLETON ─────────────────────────────────────────────────────────────

    // Singleton accessible globalement depuis LoadingScreen et d'autres scripts.
    public static MapGenerator Instance;

    // ── PREFABS ───────────────────────────────────────────────────────────────

    [Header("Prefabs - Sol & Plafond")]
    [Tooltip("Prefab utilisé pour chaque dalle de sol")]
    [SerializeField] private GameObject floorPrefab;
    [Tooltip("Prefab du plafond — si vide, le sol est réutilisé")]
    [SerializeField] private GameObject ceilingPrefab;

    [Header("Prefabs - Murs pleins")]
    [Tooltip("Mur sans ouverture — utilisé partout où il n'y a pas de porte")]
    [SerializeField] private GameObject wallLongPrefab;

    [Header("Prefabs - Murs avec porte integree BOIS")]
    [Tooltip("Mur avec une seule porte en bois (petites pièces)")]
    [SerializeField] private GameObject wallDoorWoodPrefab;
    [Tooltip("Mur avec double porte en bois (grandes pièces)")]
    [SerializeField] private GameObject wallDoubleDoorWoodPrefab;

    [Header("Prefabs - Murs avec porte integree VERRE")]
    [Tooltip("Mur avec une seule porte en verre (petites pièces)")]
    [SerializeField] private GameObject wallDoorGlassPrefab;
    [Tooltip("Mur avec double porte en verre (grandes pièces)")]
    [SerializeField] private GameObject wallDoubleDoorGlassPrefab;

    [Header("Prefabs - Decoration")]
    [Tooltip("Objet de décoration type grosse poubelle")]
    [SerializeField] private GameObject grossePoubellePrefab;
    [Tooltip("Objet de décoration type pile de pallets")]
    [SerializeField] private GameObject pileDePalletsPrefab;
    [Tooltip("Objet de décoration type tonneaux")]
    [SerializeField] private GameObject tonneauxPrefab;

    // ── ÉCHELLES & ROTATIONS ─────────────────────────────────────────────────

    [Header("Echelle des prefabs")]
    [Tooltip("Échelle appliquée aux prefabs de sol à l'instantiation")]
    [SerializeField] private Vector3 floorScale = Vector3.one;
    [Tooltip("Échelle appliquée aux prefabs de plafond")]
    [SerializeField] private Vector3 ceilingScale = Vector3.one;
    [Tooltip("Échelle appliquée aux prefabs de mur")]
    [SerializeField] private Vector3 wallScale = Vector3.one;

    [Header("Rotation de base des prefabs")]
    [Tooltip("Rotation appliquée au sol avant la rotation de placement")]
    [SerializeField] private Vector3 floorBaseRot = Vector3.zero;
    [Tooltip("Rotation appliquée au plafond avant la rotation de placement")]
    [SerializeField] private Vector3 ceilingBaseRot = Vector3.zero;
    [Tooltip("Rotation appliquée aux murs avant la rotation de placement")]
    [SerializeField] private Vector3 wallBaseRot = Vector3.zero;

    [Header("Offset Y des elements")]
    [Tooltip("Hauteur Y du sol")]
    [SerializeField] private float floorYOffset = 0f;
    [Tooltip("Hauteur Y du premier rang de murs")]
    [SerializeField] private float wallYOffset = 0f;
    [Tooltip("Hauteur Y du deuxième rang de murs")]
    [SerializeField] private float wall2YOffset = 4f;
    [Tooltip("Hauteur Y du plafond")]
    [SerializeField] private float ceilingYOffset = 8f;
    [Tooltip("Hauteur Y des objets de décoration")]
    [SerializeField] private float decoYOffset = 0f;

    // ── PORTES ────────────────────────────────────────────────────────────────

    [Header("Options - Portes")]
    [Tooltip("Probabilité qu'une porte soit en verre plutôt qu'en bois (0 = toujours bois, 1 = toujours verre)")]
    [SerializeField][Range(0f, 1f)] private float glassVsWoodChance = 0.4f;
    [Tooltip("Offset Y des portes simples (bois et verre)")]
    [SerializeField] private float singleDoorYOffset = 0f;
    [Tooltip("Offset Y des doubles portes (bois et verre)")]
    [SerializeField] private float doubleDoorYOffset = 0f;

    // ── PARAMÈTRES DE GÉNÉRATION ──────────────────────────────────────────────

    [Header("Parametres de generation")]
    [Tooltip("Nombre de pièces à générer")]
    [SerializeField] private int roomCount = 6;
    [Tooltip("Taille d'une case de grille en unités Unity")]
    [SerializeField] private float tileSize = 4f;
    [Tooltip("Largeur de la grille en nombre de cases")]
    [SerializeField] private int mapWidth = 30;
    [Tooltip("Hauteur de la grille en nombre de cases")]
    [SerializeField] private int mapHeight = 30;
    [Tooltip("Graine aléatoire — 0 = aléatoire à chaque lancement")]
    [SerializeField] private int seed = 0;

    [Header("Tailles des pieces")]
    [Tooltip("Taille minimale d'une petite pièce (en cases)")]
    [SerializeField] private int smallRoomMin = 3;
    [Tooltip("Taille maximale d'une petite pièce (en cases)")]
    [SerializeField] private int smallRoomMax = 5;
    [Tooltip("Taille minimale d'une grande pièce (en cases)")]
    [SerializeField] private int largeRoomMin = 6;
    [Tooltip("Taille maximale d'une grande pièce (en cases)")]
    [SerializeField] private int largeRoomMax = 10;
    [Tooltip("Proportion de grandes pièces parmi toutes les pièces (0 = toutes petites, 1 = toutes grandes)")]
    [SerializeField][Range(0f, 1f)] private float largeRoomRatio = 0.4f;

    // ── JOUEUR ────────────────────────────────────────────────────────────────

    [Header("Joueur")]
    [Tooltip("Glisse ici le joueur déjà présent dans la scène (pas un prefab)")]
    [SerializeField] private GameObject player;
    [Tooltip("Décalage vertical du joueur par rapport au sol")]
    [SerializeField] private float playerSpawnYOffset = 1f;

    // ── UI ────────────────────────────────────────────────────────────────────

    [Header("UI")]
    [Tooltip("L'UI du jeu à cacher pendant le chargement")]
    [SerializeField] private GameObject gameUIPanel;
    [Tooltip("Panel de mort à cacher pendant la génération")]
    [SerializeField] private GameObject deathScreenPanel;
    [Tooltip("Panel de victoire à cacher pendant la génération")]
    [SerializeField] private GameObject victoryScreenPanel;

    // ── ENNEMIS ───────────────────────────────────────────────────────────────

    [Header("Ennemis")]
    [Tooltip("Assigne le WaveManager de la scène")]
    [SerializeField] private WaveManager waveManager;
    [Tooltip("(Fallback sans WaveManager) Prefab zombie")]
    [SerializeField] private GameObject zombiePrefab;
    [Tooltip("(Fallback sans WaveManager) Zombies par petite pièce")]
    [SerializeField] private int zombiesPerSmallRoom = 1;
    [Tooltip("(Fallback sans WaveManager) Zombies par grande pièce")]
    [SerializeField] private int zombiesPerLargeRoom = 3;
    [Tooltip("Décalage vertical des zombies par rapport au sol")]
    [SerializeField] private float zombieSpawnYOffset = 0.1f;

    // ── DÉCORATION ────────────────────────────────────────────────────────────

    [Header("Decoration")]
    [Tooltip("Nombre d'objets de décoration par pièce")]
    [SerializeField] private int decoPerRoom = 2;

    // ── CAMÉRA CINÉMATIQUE ────────────────────────────────────────────────────

    [Header("Caméra cinématique")]
    [Tooltip("Caméra dédiée au survol de la map pendant la génération")]
    [SerializeField] private Camera cinematicCamera;
    [Tooltip("Hauteur de survol au-dessus de la map")]
    [SerializeField] private float cinemaHeight = 40f;
    [Tooltip("Vitesse de déplacement de la caméra pendant le survol")]
    [SerializeField] private float cinematicMoveSpeed = 6f;
    [Tooltip("Durée de la transition caméra cinématique → caméra joueur")]
    [SerializeField] private float returnDuration = 1.8f;

    // ── ÉTAT INTERNE ──────────────────────────────────────────────────────────

    private enum TileType { Empty, Floor, Corridor }

    private TileType[,] grid;
    private Dictionary<int, bool> roomIsLarge = new Dictionary<int, bool>();
    private GameObject mapParent;
    private List<RectInt> rooms = new List<RectInt>();
    private bool isGenerating = false;

    private Transform wallsParent;
    private HashSet<Vector2Int> doorTilesCache;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Enregistre ce composant comme singleton accessible globalement.
        

        // La génération n'est plus lancée automatiquement au démarrage.
        // Elle est désormais déclenchée par le bouton "Jouer" via LoadingScreen.OnPlayButtonClicked().
    }

    // Appelé publiquement par LoadingScreen.OnPlayButtonClicked().
    public void GenerateMap()
    {
        if (isGenerating)
        {
            Debug.LogWarning("MapGenerator: génération déjà en cours, ignorée.");
            return;
        }
        StartCoroutine(GenerateMapRoutine());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COROUTINE PRINCIPALE
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator GenerateMapRoutine()
    {
        if (!ValidateParameters()) yield break;
        isGenerating = true;

        AtmosphereManager atmo = FindFirstObjectByType<AtmosphereManager>();
        if (atmo != null) atmo.StartAtmosphere();

        roomCount = Mathf.Max(1, roomCount);
        mapWidth = Mathf.Max(10, mapWidth);
        mapHeight = Mathf.Max(10, mapHeight);
        tileSize = Mathf.Max(0.1f, tileSize);

        if (seed != 0) Random.InitState(seed);
        else Random.InitState(System.DateTime.Now.Millisecond);

        if (mapParent != null) Destroy(mapParent);
        mapParent = new GameObject("=== MAP GENEREE ===");
        rooms.Clear();
        roomIsLarge.Clear();
        grid = new TileType[mapWidth, mapHeight];

        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        // L'écran de chargement est déjà ouvert et sur le panneau progression
        // (activé par LoadingScreen.OnPlayButtonClicked), donc on ne rappelle pas Show().

        if (player != null) player.SetActive(false);
        Camera playerCam = player != null ? player.GetComponentInChildren<Camera>() : null;
        if (playerCam != null) playerCam.enabled = false;

        if (cinematicCamera != null)
        {
            cinematicCamera.enabled = true;
            Vector3 mapCenter = new Vector3((mapWidth / 2f) * tileSize, cinemaHeight, (mapHeight / 2f) * tileSize);
            cinematicCamera.transform.position = mapCenter;
            cinematicCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        yield return null;

        // ── ÉTAPE 1 : Pièces ─────────────────────────────────────────────────
        SetProgress(0.05f, "Génération des pièces...");
        yield return null;
        GenerateRooms();

        if (rooms.Count == 0)
        {
            Debug.LogError("MapGenerator: aucune pièce générée — vérifiez les paramètres.");
            FinishGeneration(); yield break;
        }
        yield return null;

        // ── ÉTAPE 2 : Couloirs ───────────────────────────────────────────────
        SetProgress(0.2f, "Création des couloirs...");
        yield return null;
        ConnectRooms();
        yield return null;

        doorTilesCache = ComputeDoorTiles();

        // ── ÉTAPE 3a : Géométrie SANS portes ─────────────────────────────────
        SetProgress(0.35f, "Construction des murs...");
        yield return StartCoroutine(BuildGeometry(doorsOnly: false));

        // ── ÉTAPE 3b : NavMesh bake ───────────────────────────────────────────
        SetProgress(0.6f, "Calcul de la navigation...");
        yield return new WaitForSeconds(0.3f);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        yield return StartCoroutine(BakeNavMeshIfPresent());
        yield return new WaitForSeconds(0.35f);

        // ── ÉTAPE 3c : Placement des portes ───────────────────────────────────
        SetProgress(0.65f, "Placement des portes...");
        yield return StartCoroutine(BuildGeometry(doorsOnly: true));
        yield return null;

        // ── ÉTAPE 4 : Décoration ─────────────────────────────────────────────
        SetProgress(0.75f, "Placement de la décoration...");
        SpawnDecoration();

        // ── ÉTAPE 5 : Ennemis / Vagues ───────────────────────────────────────
        SetProgress(0.88f, "Préparation des vagues...");
        if (waveManager != null)
            waveManager.InitAndStart(rooms, ConvertGrid(), mapWidth, mapHeight, tileSize, floorYOffset);
        else
        {
            Debug.LogWarning("MapGenerator: WaveManager non assigné — spawn classique utilisé.");
            SpawnEnemies();
        }

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // ── ÉTAPE 6 : Joueur ─────────────────────────────────────────────────
        SetProgress(1f, "Prêt !");
        SpawnPlayer();

        yield return new WaitForSeconds(0.4f);
        if (LoadingScreen.Instance != null) LoadingScreen.Instance.Hide();

        if (cinematicCamera != null && player != null)
            yield return StartCoroutine(ReturnCameraToPlayer());

        FinishGeneration();
        Debug.Log("Map generee ! " + rooms.Count + " pieces.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void SetProgress(float value, string message)
    {
        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.SetProgress(value, message);
    }

    void FinishGeneration()
    {
        if (cinematicCamera != null) cinematicCamera.enabled = false;
        Camera playerCamFinal = player != null ? player.GetComponentInChildren<Camera>() : null;
        if (playerCamFinal != null) playerCamFinal.enabled = true;
        if (player != null) player.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
        isGenerating = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // VALIDATION
    // ═════════════════════════════════════════════════════════════════════════
    bool ValidateParameters()
    {
        int maxAllowedRoom = Mathf.Min(mapWidth, mapHeight) / 2;
        if (largeRoomMax <= 0 || smallRoomMax <= 0)
        { Debug.LogError("MapGenerator: les tailles de pièces doivent être > 0."); return false; }
        if (largeRoomMin > largeRoomMax)
        { Debug.LogError($"MapGenerator: largeRoomMin ({largeRoomMin}) > largeRoomMax ({largeRoomMax})."); return false; }
        if (smallRoomMin > smallRoomMax)
        { Debug.LogError($"MapGenerator: smallRoomMin ({smallRoomMin}) > smallRoomMax ({smallRoomMax})."); return false; }
        if (largeRoomMax >= maxAllowedRoom)
        { Debug.LogError($"MapGenerator: largeRoomMax trop grand ! Max autorisé : {maxAllowedRoom - 1}"); return false; }
        if (smallRoomMax >= maxAllowedRoom)
        { Debug.LogError($"MapGenerator: smallRoomMax trop grand ! Max autorisé : {maxAllowedRoom - 1}"); return false; }
        if (floorPrefab == null)
        { Debug.LogError("MapGenerator: floorPrefab non assigné !"); return false; }
        if (wallLongPrefab == null)
        { Debug.LogError("MapGenerator: wallLongPrefab non assigné !"); return false; }
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    WaveManager.TileType[,] ConvertGrid()
    {
        var result = new WaveManager.TileType[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                result[x, y] = grid[x, y] switch
                {
                    TileType.Floor => WaveManager.TileType.Floor,
                    TileType.Corridor => WaveManager.TileType.Corridor,
                    _ => WaveManager.TileType.Empty
                };
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CONSTRUCTION GÉOMÉTRIQUE — deux passes séparées
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator BuildGeometry(bool doorsOnly)
    {
        if (!doorsOnly)
        {
            var fp = Child("Floors");
            wallsParent = Child("Walls");
        }

        int tilesPerFrame = Mathf.Clamp(mapWidth * mapHeight / 200, 2, 12);
        int counter = 0;
        int sc = 0;
        int maxObjects = mapWidth * mapHeight * 16;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileType t = grid[x, y];
                if (t == TileType.Empty) continue;

                if (sc >= maxObjects)
                { Debug.LogError($"MapGenerator: limite d'objets atteinte ({maxObjects})."); yield break; }

                Vector3 w = GridToWorld(x, y);

                if (!doorsOnly)
                {
                    // Sol
                    if (floorPrefab != null)
                    { SpawnPrefab(floorPrefab, w, 0f, wallsParent.parent.Find("Floors"), floorScale, floorBaseRot, floorYOffset); sc++; }

                    // Plafond
                    GameObject ceilPref = ceilingPrefab != null ? ceilingPrefab : floorPrefab;
                    SpawnPrefab(ceilPref, w, 0f, wallsParent.parent.Find("Floors"), ceilingScale, ceilingBaseRot, ceilingYOffset); sc++;

                    // Murs vers cases vides — rang 1 ET rang 2
                    if (IsEmpty(x, y + 1)) { SpawnWall(x, y, 0f, wallsParent, wallYOffset); SpawnWall(x, y, 0f, wallsParent, wall2YOffset); sc += 2; }
                    if (IsEmpty(x, y - 1)) { SpawnWall(x, y, 180f, wallsParent, wallYOffset); SpawnWall(x, y, 180f, wallsParent, wall2YOffset); sc += 2; }
                    if (IsEmpty(x + 1, y)) { SpawnWall(x, y, 90f, wallsParent, wallYOffset); SpawnWall(x, y, 90f, wallsParent, wall2YOffset); sc += 2; }
                    if (IsEmpty(x - 1, y)) { SpawnWall(x, y, 270f, wallsParent, wallYOffset); SpawnWall(x, y, 270f, wallsParent, wall2YOffset); sc += 2; }

                    // Murs entre pièces et couloirs — rang 1 ET rang 2
                    if (t == TileType.Floor)
                    {
                        if (IsCorridor(x, y + 1)) { SpawnWall(x, y, 0f, wallsParent, wallYOffset); SpawnWall(x, y, 0f, wallsParent, wall2YOffset); sc += 2; }
                        if (IsCorridor(x, y - 1)) { SpawnWall(x, y, 180f, wallsParent, wallYOffset); SpawnWall(x, y, 180f, wallsParent, wall2YOffset); sc += 2; }
                        if (IsCorridor(x + 1, y)) { SpawnWall(x, y, 90f, wallsParent, wallYOffset); SpawnWall(x, y, 90f, wallsParent, wall2YOffset); sc += 2; }
                        if (IsCorridor(x - 1, y)) { SpawnWall(x, y, 270f, wallsParent, wallYOffset); SpawnWall(x, y, 270f, wallsParent, wall2YOffset); sc += 2; }
                    }

                    // Murs latéraux des couloirs
                    if (t == TileType.Corridor)
                    {
                        SpawnCorridorSideWalls(x, y, wallsParent, wallYOffset);
                        SpawnCorridorSideWalls(x, y, wallsParent, wall2YOffset);
                        sc += 4;
                    }
                }
                else
                {
                    // Passe 2 : remplacement des murs pleins par des portes
                    TryReplaceDoorWall(x, y, 0f);
                    TryReplaceDoorWall(x, y, 180f);
                    TryReplaceDoorWall(x, y, 90f);
                    TryReplaceDoorWall(x, y, 270f);
                }

                if (!doorsOnly && cinematicCamera != null)
                {
                    float dt2 = Mathf.Max(Time.deltaTime, 0.016f);
                    Vector3 tgt = new Vector3(w.x, cinemaHeight, w.z);
                    cinematicCamera.transform.position = Vector3.MoveTowards(
                        cinematicCamera.transform.position, tgt,
                        cinematicMoveSpeed * dt2 * tilesPerFrame
                    );
                }

                counter++;
                if (counter >= tilesPerFrame) { counter = 0; yield return null; }
            }
        }

        if (!doorsOnly) Debug.Log($"BuildGeometry passe 1 : {sc} objets.");
        else Debug.Log("BuildGeometry passe 2 : portes placées.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TryReplaceDoorWall — CORRIGÉ
    // Compare uniquement X et Z pour trouver et détruire TOUS les murs
    // sur la même colonne (rang 1 ET rang 2), puis instancie une seule porte.
    // ─────────────────────────────────────────────────────────────────────────
    void TryReplaceDoorWall(int x, int y, float angle)
    {
        int nx = x, ny = y;
        if (angle == 0f) ny++;
        else if (angle == 180f) ny--;
        else if (angle == 90f) nx++;
        else if (angle == 270f) nx--;

        bool isDoor = (grid[x, y] == TileType.Floor && doorTilesCache.Contains(new Vector2Int(nx, ny))) ||
                      (grid[x, y] == TileType.Corridor && doorTilesCache.Contains(new Vector2Int(x, y)) && IsFloor(nx, ny));

        if (!isDoor) return;

        // Position XZ du mur à remplacer (Y ignoré — on veut attraper les deux rangs)
        Vector3 wallXZ = GridToWorld(x, y) + AngleToOffset(angle, tileSize / 2f);

        // Détruit TOUS les murs à ce XZ (rang bas + rang haut)
        if (wallsParent != null)
        {
            var toDestroy = new List<Transform>();
            foreach (Transform child in wallsParent)
            {
                Vector3 cp = child.position;
                if (Mathf.Abs(cp.x - wallXZ.x) < 0.1f && Mathf.Abs(cp.z - wallXZ.z) < 0.1f)
                    toDestroy.Add(child);
            }
            foreach (Transform t in toDestroy)
                Destroy(t.gameObject);

            if (toDestroy.Count == 0)
                Debug.LogWarning($"MapGenerator: aucun mur trouvé à ({x},{y}) angle {angle}");
        }

        // Sélection du prefab selon taille de pièce et matière
        int rx = IsFloor(x, y) ? x : nx;
        int ry = IsFloor(x, y) ? y : ny;
        bool isLarge = IsLargeRoom(rx, ry);
        bool isGlass = Random.value < glassVsWoodChance;

        GameObject pref;
        if (isLarge) pref = isGlass ? (wallDoubleDoorGlassPrefab ?? wallLongPrefab) : (wallDoubleDoorWoodPrefab ?? wallLongPrefab);
        else pref = isGlass ? (wallDoorGlassPrefab ?? wallLongPrefab) : (wallDoorWoodPrefab ?? wallLongPrefab);

        // Offset Y indépendant selon le type de porte
        float doorYOff = isLarge ? doubleDoorYOffset : singleDoorYOffset;
        SpawnPrefab(pref, GridToWorld(x, y) + AngleToOffset(angle, tileSize / 2f), angle, wallsParent, wallScale, wallBaseRot, doorYOff);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TRANSITION CAMÉRA
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator ReturnCameraToPlayer()
    {
        if (cinematicCamera == null || player == null) yield break;

        Vector3 startPos = cinematicCamera.transform.position;
        Quaternion startRot = cinematicCamera.transform.rotation;

        Camera playerCam = player.GetComponentInChildren<Camera>();
        Vector3 endPos = playerCam != null ? playerCam.transform.position : player.transform.position + Vector3.up * 1.6f;
        Quaternion endRot = playerCam != null ? playerCam.transform.rotation : player.transform.rotation;

        float duration = Mathf.Max(0.01f, returnDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            cinematicCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        cinematicCamera.transform.position = endPos;
        cinematicCamera.transform.rotation = endRot;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GÉNÉRATION DES PIÈCES
    // ═════════════════════════════════════════════════════════════════════════
    void GenerateRooms()
    {
        smallRoomMin = Mathf.Max(2, smallRoomMin);
        smallRoomMax = Mathf.Max(smallRoomMin, smallRoomMax);
        largeRoomMin = Mathf.Max(2, largeRoomMin);
        largeRoomMax = Mathf.Max(largeRoomMin, largeRoomMax);

        int attempts = 0, maxAttempts = roomCount * 15;
        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;
            bool isLarge = Random.value < largeRoomRatio;
            int minW = isLarge ? largeRoomMin : smallRoomMin;
            int maxW = isLarge ? largeRoomMax : smallRoomMax;

            int w = (minW == maxW) ? minW : Random.Range(minW, maxW + 1);
            int h = (minW == maxW) ? minW : Random.Range(minW, maxW + 1);

            int maxX = mapWidth - w - 1;
            int maxY = mapHeight - h - 1;
            if (maxX < 2 || maxY < 2) continue;

            int rx = Random.Range(2, maxX);
            int ry = Random.Range(2, maxY);

            RectInt newRoom = new RectInt(rx, ry, w, h);
            if (!OverlapsExistingRoom(newRoom, 2))
            {
                roomIsLarge[rooms.Count] = isLarge;
                rooms.Add(newRoom);
                CarveRoom(newRoom);
            }
        }

        if (rooms.Count < roomCount)
            Debug.LogWarning($"MapGenerator: seulement {rooms.Count}/{roomCount} pièces générées.");
    }

    bool OverlapsExistingRoom(RectInt room, int margin)
    {
        foreach (RectInt e in rooms)
        {
            RectInt ex = new RectInt(e.x - margin, e.y - margin, e.width + margin * 2, e.height + margin * 2);
            if (room.Overlaps(ex)) return true;
        }
        return false;
    }

    void CarveRoom(RectInt room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
            for (int y = room.y; y < room.y + room.height; y++)
                if (IsInBounds(x, y)) grid[x, y] = TileType.Floor;
    }

    // ── COULOIRS ─────────────────────────────────────────────────────────────
    void ConnectRooms()
    {
        if (rooms.Count < 2) return;
        for (int i = 0; i < rooms.Count - 1; i++)
            CarveCorridor(GetRoomCenter(rooms[i]), GetRoomCenter(rooms[i + 1]));
    }

    Vector2Int GetRoomCenter(RectInt r) => new Vector2Int(r.x + r.width / 2, r.y + r.height / 2);

    void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        if (Random.value > 0.5f) { CarveH(a.x, b.x, a.y); CarveV(a.y, b.y, b.x); }
        else { CarveV(a.y, b.y, a.x); CarveH(a.x, b.x, b.y); }
    }

    void CarveH(int x0, int x1, int y)
    {
        for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            if (IsInBounds(x, y) && grid[x, y] == TileType.Empty)
                grid[x, y] = TileType.Corridor;
    }

    void CarveV(int y0, int y1, int x)
    {
        for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
            if (IsInBounds(x, y) && grid[x, y] == TileType.Empty)
                grid[x, y] = TileType.Corridor;
    }

    // ── CALCUL DES TILES DE PORTE ─────────────────────────────────────────────
    HashSet<Vector2Int> ComputeDoorTiles()
    {
        var dt = new HashSet<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                if (grid[x, y] == TileType.Corridor &&
                    (IsFloor(x + 1, y) || IsFloor(x - 1, y) || IsFloor(x, y + 1) || IsFloor(x, y - 1)))
                    dt.Add(new Vector2Int(x, y));
        return dt;
    }

    // ── PLACEMENT DES MURS ────────────────────────────────────────────────────
    void SpawnWall(int x, int y, float angle, Transform parent, float yOff = 0f)
    {
        if (wallLongPrefab == null) return;
        SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + AngleToOffset(angle, tileSize / 2f), angle, parent, wallScale, wallBaseRot, yOff);
    }

    Vector3 AngleToOffset(float angle, float half) => angle switch
    {
        0f => new Vector3(0, 0, half),
        180f => new Vector3(0, 0, -half),
        90f => new Vector3(half, 0, 0),
        270f => new Vector3(-half, 0, 0),
        _ => Vector3.zero
    };

    void SpawnCorridorSideWalls(int x, int y, Transform parent, float yOff)
    {
        bool h = IsCorridorOrFloor(x + 1, y) || IsCorridorOrFloor(x - 1, y);
        bool v = IsCorridorOrFloor(x, y + 1) || IsCorridorOrFloor(x, y - 1);
        float s = tileSize / 2f;
        if (h && !IsCorridorOrFloor(x, y + 1)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(0, 0, s), 0f, parent, wallScale, wallBaseRot, yOff);
        if (h && !IsCorridorOrFloor(x, y - 1)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(0, 0, -s), 180f, parent, wallScale, wallBaseRot, yOff);
        if (v && !IsCorridorOrFloor(x + 1, y)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(s, 0, 0), 90f, parent, wallScale, wallBaseRot, yOff);
        if (v && !IsCorridorOrFloor(x - 1, y)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(-s, 0, 0), 270f, parent, wallScale, wallBaseRot, yOff);
    }

    // ── SPAWN ENNEMIS (FALLBACK) ──────────────────────────────────────────────
    void SpawnEnemies()
    {
        if (zombiePrefab == null) { Debug.LogWarning("MapGenerator: aucun prefab zombie !"); return; }
        if (rooms.Count == 0) return;

        var ep = new GameObject("Enemies");
        ep.transform.parent = mapParent.transform;

        for (int i = 1; i < rooms.Count; i++)
        {
            RectInt r = rooms[i];
            bool lg = roomIsLarge.ContainsKey(i) && roomIsLarge[i];
            int cnt = Mathf.Max(0, lg ? zombiesPerLargeRoom : zombiesPerSmallRoom);

            for (int z = 0; z < cnt; z++)
            {
                int rx, ry;
                if (r.width <= 2 || r.height <= 2) { rx = r.x + r.width / 2; ry = r.y + r.height / 2; }
                else { rx = Random.Range(r.x + 1, r.x + r.width - 1); ry = Random.Range(r.y + 1, r.y + r.height - 1); }
                Vector3 p = GridToWorld(rx, ry);
                p.y = floorYOffset + zombieSpawnYOffset;
                Instantiate(zombiePrefab, p, Quaternion.Euler(0, Random.Range(0f, 360f), 0), ep.transform);
            }
        }
    }

    // ── DÉCORATION ────────────────────────────────────────────────────────────
    bool IsTileNearCorridor(int x, int y, int radius = 1)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (IsCorridor(x + dx, y + dy)) return true;
        return false;
    }

    bool IsTileAgainstWall(int x, int y) =>
        IsEmpty(x + 1, y) || IsEmpty(x - 1, y) || IsEmpty(x, y + 1) || IsEmpty(x, y - 1);

    void SpawnDecoration()
    {
        var validPrefabs = new List<GameObject>();
        if (grossePoubellePrefab != null) validPrefabs.Add(grossePoubellePrefab);
        if (pileDePalletsPrefab != null) validPrefabs.Add(pileDePalletsPrefab);
        if (tonneauxPrefab != null) validPrefabs.Add(tonneauxPrefab);
        if (validPrefabs.Count == 0) { Debug.LogWarning("MapGenerator: aucun prefab de décoration !"); return; }
        if (rooms.Count == 0) return;

        GameObject decoParent = new GameObject("Decoration");
        decoParent.transform.parent = mapParent.transform;
        var usedTiles = new HashSet<Vector2Int>();
        int total = 0;

        for (int i = 1; i < rooms.Count; i++)
        {
            RectInt room = rooms[i];
            if (room.width <= 2 || room.height <= 2) continue;

            var candidates = new List<Vector2Int>();
            for (int x = room.x + 1; x < room.x + room.width - 1; x++)
                for (int y = room.y + 1; y < room.y + room.height - 1; y++)
                {
                    var tile = new Vector2Int(x, y);
                    if (IsTileAgainstWall(x, y) && !IsTileNearCorridor(x, y, 2) && !usedTiles.Contains(tile))
                        candidates.Add(tile);
                }

            if (candidates.Count < decoPerRoom)
                for (int x = room.x + 1; x < room.x + room.width - 1; x++)
                    for (int y = room.y + 1; y < room.y + room.height - 1; y++)
                    {
                        var tile = new Vector2Int(x, y);
                        if (!IsTileNearCorridor(x, y, 1) && !usedTiles.Contains(tile) && !candidates.Contains(tile))
                            candidates.Add(tile);
                    }

            int decoCount = Mathf.Max(0, decoPerRoom);
            for (int d = 0; d < decoCount && candidates.Count > 0; d++)
            {
                int idx = Random.Range(0, candidates.Count);
                Vector2Int tile = candidates[idx];
                candidates.RemoveAt(idx);
                usedTiles.Add(tile);
                Vector3 pos = GridToWorld(tile.x, tile.y);
                pos.y = floorYOffset + decoYOffset;
                Instantiate(validPrefabs[Random.Range(0, validPrefabs.Count)], pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), decoParent.transform);
                total++;
            }
        }
        Debug.Log($"Décoration: {total} objets placés.");
    }

    // ── SPAWN JOUEUR ──────────────────────────────────────────────────────────
    void SpawnPlayer()
    {
        if (rooms.Count == 0) { Debug.LogWarning("MapGenerator: aucune pièce !"); return; }
        if (player == null) { Debug.LogWarning("MapGenerator: aucun joueur assigné !"); return; }

        Vector2Int c = GetRoomCenter(rooms[0]);
        Vector3 p = new Vector3(c.x * tileSize, floorYOffset + playerSpawnYOffset, c.y * tileSize);

        CharacterController cc = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (cc != null) cc.enabled = false;
        if (rb != null) rb.isKinematic = true;
        player.transform.SetPositionAndRotation(p, Quaternion.identity);
        if (cc != null) cc.enabled = true;
        if (rb != null) rb.isKinematic = false;

        Debug.Log("Joueur repositionné à : " + p);
    }

    // ── NAVMESH ───────────────────────────────────────────────────────────────
    IEnumerator BakeNavMeshIfPresent()
    {
        NavMeshBaker b = GetComponent<NavMeshBaker>();
        if (b != null)
        {
            b.navMeshCheckPoint = new Vector3((mapWidth / 2f) * tileSize, floorYOffset, (mapHeight / 2f) * tileSize);
            yield return StartCoroutine(b.BakeNavMeshRoutine());
        }
        else { Debug.LogWarning("MapGenerator: NavMeshBaker non trouvé !"); yield break; }
    }

    // ── UTILITAIRES ───────────────────────────────────────────────────────────
    bool IsLargeRoom(int x, int y)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i].Contains(new Vector2Int(x, y)))
                return roomIsLarge.ContainsKey(i) && roomIsLarge[i];
        return false;
    }

    public Vector3 GetMapCenter() =>
        new Vector3((mapWidth / 2f) * tileSize, 0f, (mapHeight / 2f) * tileSize);

    Transform Child(string name)
    {
        var g = new GameObject(name);
        g.transform.parent = mapParent.transform;
        return g.transform;
    }

    void SpawnPrefab(GameObject prefab, Vector3 pos, float angleY, Transform parent, Vector3 scale, Vector3 baseRot, float yOffset)
    {
        if (prefab == null) return;
        if (parent == null) { Debug.LogWarning("MapGenerator: parent null dans SpawnPrefab, ignoré."); return; }
        pos.y += yOffset;
        var o = Instantiate(prefab, pos, Quaternion.Euler(baseRot.x, baseRot.y + angleY, baseRot.z), parent);
        o.transform.localScale = scale;
    }

    Vector3 GridToWorld(int x, int y) => new Vector3(x * tileSize, 0, y * tileSize);
    bool IsEmpty(int x, int y) => !IsInBounds(x, y) || grid[x, y] == TileType.Empty;
    bool IsFloor(int x, int y) => IsInBounds(x, y) && grid[x, y] == TileType.Floor;
    bool IsCorridor(int x, int y) => IsInBounds(x, y) && grid[x, y] == TileType.Corridor;
    bool IsCorridorOrFloor(int x, int y) => IsInBounds(x, y) && (grid[x, y] == TileType.Floor || grid[x, y] == TileType.Corridor);
    bool IsInBounds(int x, int y) => x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;

    // ── GIZMOS ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (grid == null) return;
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (grid[x, y] == TileType.Floor) Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
                else if (grid[x, y] == TileType.Corridor) Gizmos.color = new Color(0.2f, 0.4f, 0.8f, 0.5f);
                else continue;
                Gizmos.DrawCube(GridToWorld(x, y), new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f));
            }
        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2Int c = GetRoomCenter(rooms[i]);
            Gizmos.color = (roomIsLarge.ContainsKey(i) && roomIsLarge[i]) ? Color.red : Color.yellow;
            Gizmos.DrawSphere(GridToWorld(c.x, c.y) + Vector3.up, 0.5f);
        }
    }
}