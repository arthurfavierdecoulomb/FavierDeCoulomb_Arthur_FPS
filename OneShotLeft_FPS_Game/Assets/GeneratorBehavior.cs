using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ═══════════════════════════════════════════════════════════════════════════════
// MapGenerator — Génère procéduralement une map de type entrepôt à partir d'une
// grille 2D. Il place des pièces rectangulaires, les connecte avec des couloirs,
// puis instancie les prefabs de sol, plafond, murs et portes en 3D.
// Une caméra cinématique survole la map pendant la génération, puis une
// transition fluide ramène la vue sur le joueur.
// Nous avons effectivemùent pas appris cela en cours, mais c'est un excellent exemple
// de système de génération procédurale. je me suis aidé des resources internet et des videos
// youtubes pour le construire, et j'ai essayé de faire un code propre et commenté
// pour que ce soit facile à comprendre et à modifier.
// Merci d'avoir lu, Arthur. 
// ═══════════════════════════════════════════════════════════════════════════════
public class MapGenerator : MonoBehaviour
{
    // ── PREFABS ───────────────────────────────────────────────────────────────
    [Header("Prefabs - Sol & Plafond")]
    [Tooltip("Prefab utilisé pour chaque dalle de sol")]
    [SerializeField] private GameObject floorPrefab;                               // Instancié sur chaque case Floor et Corridor
    [Tooltip("Prefab du plafond — si vide, le sol est réutilisé")]
    [SerializeField] private GameObject ceilingPrefab;                             // Si null, floorPrefab est utilisé à la place

    [Header("Prefabs - Murs pleins")]
    [Tooltip("Mur sans ouverture — utilisé partout où il n'y a pas de porte")]
    [SerializeField] private GameObject wallLongPrefab;                            // Mur de base, référencé dans tout le système de construction

    [Header("Prefabs - Murs avec porte integree BOIS")]
    [Tooltip("Mur avec une seule porte en bois (petites pièces)")]
    [SerializeField] private GameObject wallDoorWoodPrefab;                        // Porte simple bois pour les petites pièces
    [Tooltip("Mur avec double porte en bois (grandes pièces)")]
    [SerializeField] private GameObject wallDoubleDoorWoodPrefab;                  // Double porte bois pour les grandes pièces

    [Header("Prefabs - Murs avec porte integree VERRE")]
    [Tooltip("Mur avec une seule porte en verre (petites pièces)")]
    [SerializeField] private GameObject wallDoorGlassPrefab;                       // Porte simple verre pour les petites pièces
    [Tooltip("Mur avec double porte en verre (grandes pièces)")]
    [SerializeField] private GameObject wallDoubleDoorGlassPrefab;                 // Double porte verre pour les grandes pièces

    [Header("Prefabs - Decoration")]
    [Tooltip("Objet de décoration type grosse poubelle")]
    [SerializeField] private GameObject grossePoubellePrefab;                      // Placé aléatoirement contre les murs des pièces
    [Tooltip("Objet de décoration type pile de pallets")]
    [SerializeField] private GameObject pileDePalletsPrefab;                       // Idem
    [Tooltip("Objet de décoration type tonneaux")]
    [SerializeField] private GameObject tonneauxPrefab;                            // Idem

    // ── ÉCHELLES & ROTATIONS ─────────────────────────────────────────────────
    [Header("Echelle des prefabs")]
    [Tooltip("Échelle appliquée aux prefabs de sol à l'instantiation")]
    [SerializeField] private Vector3 floorScale = Vector3.one;                   // Permet d'ajuster la taille du sol sans modifier le prefab
    [Tooltip("Échelle appliquée aux prefabs de plafond")]
    [SerializeField] private Vector3 ceilingScale = Vector3.one;
    [Tooltip("Échelle appliquée aux prefabs de mur")]
    [SerializeField] private Vector3 wallScale = Vector3.one;

    [Header("Rotation de base des prefabs")]
    [Tooltip("Rotation appliquée au sol avant la rotation de placement")]
    [SerializeField] private Vector3 floorBaseRot = Vector3.zero;                // Utile si le prefab n'est pas orienté correctement par défaut
    [Tooltip("Rotation appliquée au plafond avant la rotation de placement")]
    [SerializeField] private Vector3 ceilingBaseRot = Vector3.zero;
    [Tooltip("Rotation appliquée aux murs avant la rotation de placement")]
    [SerializeField] private Vector3 wallBaseRot = Vector3.zero;

    [Header("Offset Y des elements")]
    [Tooltip("Hauteur Y du sol")]
    [SerializeField] private float floorYOffset = 0f;                            // Décalage vertical du sol — toutes les hauteurs en dépendent
    [Tooltip("Hauteur Y du premier rang de murs")]
    [SerializeField] private float wallYOffset = 0f;                            // Premier étage de murs (bas)
    [Tooltip("Hauteur Y du deuxième rang de murs")]
    [SerializeField] private float wall2YOffset = 4f;                            // Deuxième étage de murs (haut) — doit correspondre à la hauteur du prefab mur
    [Tooltip("Hauteur Y du plafond")]
    [SerializeField] private float ceilingYOffset = 8f;                            // Doit être égal à wall2YOffset + hauteur d'un mur
    [Tooltip("Hauteur Y des objets de décoration")]
    [SerializeField] private float decoYOffset = 0f;                            // Décalage vertical des décorations par rapport au sol

    // ── PORTES ────────────────────────────────────────────────────────────────
    [Header("Options - Portes")]
    [Tooltip("Probabilité qu'une porte soit en verre plutôt qu'en bois (0 = toujours bois, 1 = toujours verre)")]
    [SerializeField][Range(0f, 1f)] private float glassVsWoodChance = 0.4f;        // Chaque porte tire au sort entre verre et bois

    // ── PARAMÈTRES DE GÉNÉRATION ──────────────────────────────────────────────
    [Header("Parametres de generation")]
    [Tooltip("Nombre de pièces à générer — le générateur essaie d'en placer autant que possible")]
    [SerializeField] private int roomCount = 6;                                 // Nombre cible de pièces (peut être inférieur si la map est trop petite)
    [Tooltip("Taille d'une case de grille en unités Unity")]
    [SerializeField] private float tileSize = 4f;                                // Taille d'une case — aussi la largeur d'un mur ou d'une dalle
    [Tooltip("Largeur de la grille en nombre de cases")]
    [SerializeField] private int mapWidth = 30;                                // Nombre de cases horizontales
    [Tooltip("Hauteur de la grille en nombre de cases")]
    [SerializeField] private int mapHeight = 30;                                // Nombre de cases verticales
    [Tooltip("Graine aléatoire — 0 = aléatoire à chaque lancement")]
    [SerializeField] private int seed = 0;                                 // Fixe la graine pour reproduire une map identique

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
    [SerializeField][Range(0f, 1f)] private float largeRoomRatio = 0.4f;           // Ex: 0.4 = 40% de chances qu'une pièce soit grande

    // ── JOUEUR ────────────────────────────────────────────────────────────────
    [Header("Joueur")]
    [Tooltip("Glisse ici le joueur déjà présent dans la scène (pas un prefab)")]
    [SerializeField] private GameObject player;                                    // Le joueur est repositionné au centre de la première pièce
    [Tooltip("Décalage vertical du joueur par rapport au sol")]
    [SerializeField] private float playerSpawnYOffset = 1f;                        // Évite que le joueur apparaisse dans le sol

    // ── UI ────────────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("L'UI du jeu à cacher pendant le chargement")]
    [SerializeField] private GameObject gameUIPanel;                               // Caché pendant la génération, réactivé à la fin
    [Tooltip("Panel de mort à cacher pendant la génération")]
    [SerializeField] private GameObject deathScreenPanel;                          // Évite que l'écran de mort s'affiche pendant le chargement
    [Tooltip("Panel de victoire à cacher pendant la génération")]
    [SerializeField] private GameObject victoryScreenPanel;                        // Idem pour l'écran de victoire

    // ── ENNEMIS ───────────────────────────────────────────────────────────────
    [Header("Ennemis")]
    [Tooltip("Assigne le WaveManager de la scène — s'il est assigné, il gère les vagues. Sinon le spawn classique est utilisé")]
    [SerializeField] private WaveManager waveManager;                              // Mode principal : vagues gérées par WaveManager

    [Tooltip("(Fallback sans WaveManager) Prefab zombie")]
    [SerializeField] private GameObject zombiePrefab;                              // Utilisé uniquement si waveManager est null
    [Tooltip("(Fallback sans WaveManager) Zombies par petite pièce")]
    [SerializeField] private int zombiesPerSmallRoom = 1;
    [Tooltip("(Fallback sans WaveManager) Zombies par grande pièce")]
    [SerializeField] private int zombiesPerLargeRoom = 3;
    [Tooltip("Décalage vertical des zombies par rapport au sol")]
    [SerializeField] private float zombieSpawnYOffset = 0.1f;

    // ── DÉCORATION ────────────────────────────────────────────────────────────
    [Header("Decoration")]
    [Tooltip("Nombre d'objets de décoration par pièce")]
    [SerializeField] private int decoPerRoom = 2;                                  // Placés contre les murs, loin des couloirs

    // ── CAMÉRA CINÉMATIQUE ────────────────────────────────────────────────────
    [Header("Caméra cinématique")]
    [Tooltip("Caméra dédiée au survol de la map pendant la génération — désactivée en jeu")]
    [SerializeField] private Camera cinematicCamera;                               // Caméra séparée de celle du joueur
    [Tooltip("Hauteur de survol au-dessus de la map")]
    [SerializeField] private float cinemaHeight = 40f;                       // Plus élevé = vue plus large
    [Tooltip("Vitesse de déplacement de la caméra pendant le survol")]
    [SerializeField] private float cinematicMoveSpeed = 6f;                        // Se déplace au-dessus de chaque tile générée
    [Tooltip("Durée de la transition caméra cinématique → caméra joueur")]
    [SerializeField] private float returnDuration = 1.8f;                      // Fondu de transition fluide en secondes

    // ── ÉTAT INTERNE ──────────────────────────────────────────────────────────
    private enum TileType { Empty, Floor, Corridor }                               // Les trois états possibles d'une case de grille

    private TileType[,] grid;                                                      // Grille 2D qui stocke l'état de chaque case (vide, sol, couloir)
    private Dictionary<int, bool> roomIsLarge = new Dictionary<int, bool>();       // Associe l'index d'une pièce à sa taille (true = grande)
    private GameObject mapParent;                                                   // Parent Unity de tous les objets générés — facilite la suppression/régénération
    private List<RectInt> rooms = new List<RectInt>();                             // Liste de toutes les pièces générées (position + taille en cases)
    private bool isGenerating = false;                                              // Verrou anti-double génération

    // ─────────────────────────────────────────────────────────────────────────
    void Start() => StartCoroutine(GenerateMapRoutine());                          // Lance automatiquement la génération au démarrage de la scène

    public void GenerateMap()
    {
        if (isGenerating)
        {
            Debug.LogWarning("MapGenerator: génération déjà en cours, ignorée.");  // Empêche deux générations simultanées
            return;
        }
        StartCoroutine(GenerateMapRoutine());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COROUTINE PRINCIPALE — orchestre toutes les étapes de génération
    // Chaque étape est séparée par des yield pour permettre l'affichage
    // de la progression et le déplacement de la caméra cinématique.
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator GenerateMapRoutine()
    {
        if (!ValidateParameters()) yield break;                                    // Vérifie que tous les paramètres sont cohérents avant de commencer
        isGenerating = true;

        AtmosphereManager atmo = FindFirstObjectByType<AtmosphereManager>();
        if (atmo != null) atmo.StartAtmosphere();                                  // Lance le son d'ambiance dès le début de la génération

        // Clamp des paramètres pour éviter les valeurs aberrantes
        roomCount = Mathf.Max(1, roomCount);
        mapWidth = Mathf.Max(10, mapWidth);
        mapHeight = Mathf.Max(10, mapHeight);
        tileSize = Mathf.Max(0.1f, tileSize);

        if (seed != 0) Random.InitState(seed);                                     // Graine fixe pour reproduire la même map
        else Random.InitState(System.DateTime.Now.Millisecond);                    // Graine aléatoire basée sur l'heure

        if (mapParent != null) Destroy(mapParent);                                 // Supprime la map précédente si elle existe
        mapParent = new GameObject("=== MAP GENEREE ===");                         // Nouveau parent vide pour regrouper tous les objets
        rooms.Clear();
        roomIsLarge.Clear();
        grid = new TileType[mapWidth, mapHeight];                                  // Réinitialise la grille — tout est Empty par défaut

        if (gameUIPanel != null) gameUIPanel.SetActive(false);                     // Cache l'UI pendant la génération

        if (LoadingScreen.Instance != null)
        {
            LoadingScreen.Instance.Show();
            yield return LoadingScreen.Instance.WaitUntilReady();                  // Attend que l'intro du loading screen soit terminée
        }

        if (player != null) player.SetActive(false);                               // Cache le joueur pendant la génération
        Camera playerCam = player != null ? player.GetComponentInChildren<Camera>() : null;
        if (playerCam != null) playerCam.enabled = false;                          // Désactive la caméra joueur pour laisser place à la cinématique

        if (cinematicCamera != null)
        {
            cinematicCamera.enabled = true;                                         // Active la caméra cinématique
            Vector3 mapCenter = new Vector3((mapWidth / 2f) * tileSize, cinemaHeight, (mapHeight / 2f) * tileSize);
            cinematicCamera.transform.position = mapCenter;                         // Positionne la caméra au centre de la map en hauteur
            cinematicCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // Regarde vers le bas (vue de dessus)
        }

        yield return null;

        // ── ÉTAPE 1 : Pièces ─────────────────────────────────────────────────
        SetProgress(0.05f, "Génération des pièces...");
        yield return null;
        GenerateRooms();                                                            // Place les pièces aléatoirement sur la grille sans chevauchement

        if (rooms.Count == 0)
        {
            Debug.LogError("MapGenerator: aucune pièce générée — vérifiez les paramètres.");
            FinishGeneration(); yield break;
        }
        yield return null;

        // ── ÉTAPE 2 : Couloirs ───────────────────────────────────────────────
        SetProgress(0.2f, "Création des couloirs...");
        yield return null;
        ConnectRooms();                                                             // Relie chaque pièce à la suivante par un couloir en L
        yield return null;

        // ── ÉTAPE 3 : Géométrie ──────────────────────────────────────────────
        SetProgress(0.35f, "Construction des murs...");
        yield return StartCoroutine(BuildGeometryWithCamera());                    // Instancie tous les prefabs tile par tile avec déplacement de caméra

        // ── ÉTAPE 4 : NavMesh ────────────────────────────────────────────────
        SetProgress(0.6f, "Calcul de la navigation...");
        yield return StartCoroutine(BakeNavMeshIfPresent());                       // Bake le NavMesh pour que les zombies puissent naviguer
        yield return new WaitForSeconds(0.35f);                                    // Petite pause pour que le NavMesh soit bien stabilisé

        // ── ÉTAPE 5 : Décoration ─────────────────────────────────────────────
        SetProgress(0.75f, "Placement de la décoration...");
        SpawnDecoration();                                                          // Place les objets décoratifs contre les murs

        // ── ÉTAPE 6 : Ennemis / Vagues ───────────────────────────────────────
        SetProgress(0.88f, "Préparation des vagues...");
        if (waveManager != null)
        {
            waveManager.InitAndStart(                                              // Passe les données de la map au WaveManager pour le spawn par vagues
                rooms, ConvertGrid(), mapWidth, mapHeight, tileSize, floorYOffset
            );
        }
        else
        {
            Debug.LogWarning("MapGenerator: WaveManager non assigné — spawn classique utilisé.");
            SpawnEnemies();                                                         // Fallback : spawn immédiat de zombies dans chaque pièce
        }

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();                                     // Attend 3 frames physiques pour que tout soit bien initialisé

        // ── ÉTAPE 7 : Joueur ─────────────────────────────────────────────────
        SetProgress(1f, "Prêt !");
        SpawnPlayer();                                                              // Positionne le joueur au centre de la première pièce

        yield return new WaitForSeconds(0.4f);
        if (LoadingScreen.Instance != null) LoadingScreen.Instance.Hide();         // Ferme le loading screen

        if (cinematicCamera != null && player != null)
            yield return StartCoroutine(ReturnCameraToPlayer());                   // Transition fluide de la caméra cinématique vers la caméra joueur

        FinishGeneration();
        Debug.Log("Map generee ! " + rooms.Count + " pieces.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void SetProgress(float value, string message)
    {
        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.SetProgress(value, message);                    // Met à jour la barre de progression et le texte du loading screen
    }

    void FinishGeneration()
    {
        if (cinematicCamera != null) cinematicCamera.enabled = false;              // Désactive la caméra cinématique — plus nécessaire en jeu
        Camera playerCamFinal = player != null ? player.GetComponentInChildren<Camera>() : null;
        if (playerCamFinal != null) playerCamFinal.enabled = true;                 // Réactive la caméra du joueur
        if (player != null) player.SetActive(true);                                // Réactive le joueur
        if (gameUIPanel != null) gameUIPanel.SetActive(true);                      // Réaffiche l'UI de jeu
        isGenerating = false;                                                       // Libère le verrou de génération
    }

    // ═════════════════════════════════════════════════════════════════════════
    // VALIDATION — vérifie que les paramètres sont cohérents avant de générer
    // Affiche une erreur précise dans la console si quelque chose ne va pas
    // ═════════════════════════════════════════════════════════════════════════
    bool ValidateParameters()
    {
        int maxAllowedRoom = Mathf.Min(mapWidth, mapHeight) / 2;                   // Une pièce ne peut pas être plus grande que la moitié de la map
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
        return true;                                                                // Tous les paramètres sont valides — on peut générer
    }

    // ─────────────────────────────────────────────────────────────────────────
    WaveManager.TileType[,] ConvertGrid()
    {
        var result = new WaveManager.TileType[mapWidth, mapHeight];                // Convertit la grille interne en type compatible avec WaveManager
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                result[x, y] = grid[x, y] switch
                {
                    TileType.Floor => WaveManager.TileType.Floor,               // Case de sol → Floor WaveManager
                    TileType.Corridor => WaveManager.TileType.Corridor,            // Case de couloir → Corridor WaveManager
                    _ => WaveManager.TileType.Empty                // Tout le reste → Empty
                };
            }
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CONSTRUCTION GÉOMÉTRIQUE — instancie les prefabs tile par tile
    // La caméra cinématique se déplace vers chaque tile pendant la construction
    // tilesPerFrame est adaptatif selon la taille de la map
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator BuildGeometryWithCamera()
    {
        var fp = Child("Floors");                                                  // Conteneur Unity pour tous les sols et plafonds
        var wp2 = Child("Walls");                                                   // Conteneur Unity pour tous les murs
        HashSet<Vector2Int> dt = ComputeDoorTiles();                               // Pré-calcule quelles tiles doivent avoir une porte

        int maxObjects = mapWidth * mapHeight * 16;                             // Limite de sécurité pour éviter les dépassements mémoire
        int sc = 0;                                                     // Compteur d'objets instanciés
        int tilesPerFrame = Mathf.Clamp(mapWidth * mapHeight / 200, 2, 12);       // Plus la map est grande, plus on traite de tiles par frame
        int counter = 0;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileType t = grid[x, y];
                if (t == TileType.Empty) continue;                                 // Pas de géométrie sur les cases vides

                if (sc >= maxObjects)
                { Debug.LogError($"MapGenerator: limite d'objets atteinte ({maxObjects}). Arrêt prématuré."); yield break; }

                Vector3 w = GridToWorld(x, y);

                // Sol
                if (floorPrefab != null)
                { SpawnPrefab(floorPrefab, w, 0f, fp, floorScale, floorBaseRot, floorYOffset); sc++; }

                // Plafond (réutilise floorPrefab si ceilingPrefab est null)
                GameObject ceilPref = ceilingPrefab != null ? ceilingPrefab : floorPrefab;
                SpawnPrefab(ceilPref, w, 0f, fp, ceilingScale, ceilingBaseRot, ceilingYOffset); sc++;

                // Murs — un mur est placé sur chaque face adjacente à une case vide
                if (IsEmpty(x, y + 1)) { SpawnWallOrDoor(x, y, 0f, wp2, dt, wallYOffset); SpawnWall(x, y, 0f, wp2, wall2YOffset); sc += 2; }
                if (IsEmpty(x, y - 1)) { SpawnWallOrDoor(x, y, 180f, wp2, dt, wallYOffset); SpawnWall(x, y, 180f, wp2, wall2YOffset); sc += 2; }
                if (IsEmpty(x + 1, y)) { SpawnWallOrDoor(x, y, 90f, wp2, dt, wallYOffset); SpawnWall(x, y, 90f, wp2, wall2YOffset); sc += 2; }
                if (IsEmpty(x - 1, y)) { SpawnWallOrDoor(x, y, 270f, wp2, dt, wallYOffset); SpawnWall(x, y, 270f, wp2, wall2YOffset); sc += 2; }

                // Murs entre pièces et couloirs — pour séparer visuellement les espaces
                if (t == TileType.Floor)
                {
                    if (IsCorridor(x, y + 1)) { SpawnWallOrDoor(x, y, 0f, wp2, dt, wallYOffset); SpawnWall(x, y, 0f, wp2, wall2YOffset); sc += 2; }
                    if (IsCorridor(x, y - 1)) { SpawnWallOrDoor(x, y, 180f, wp2, dt, wallYOffset); SpawnWall(x, y, 180f, wp2, wall2YOffset); sc += 2; }
                    if (IsCorridor(x + 1, y)) { SpawnWallOrDoor(x, y, 90f, wp2, dt, wallYOffset); SpawnWall(x, y, 90f, wp2, wall2YOffset); sc += 2; }
                    if (IsCorridor(x - 1, y)) { SpawnWallOrDoor(x, y, 270f, wp2, dt, wallYOffset); SpawnWall(x, y, 270f, wp2, wall2YOffset); sc += 2; }
                }

                // Murs latéraux des couloirs — referment les côtés ouverts
                if (t == TileType.Corridor)
                {
                    SpawnCorridorSideWalls(x, y, wp2, wallYOffset);
                    SpawnCorridorSideWalls(x, y, wp2, wall2YOffset);
                    sc += 4;
                }

                // Déplace la caméra cinématique vers la tile en cours de construction
                if (cinematicCamera != null)
                {
                    float dt2 = Mathf.Max(Time.deltaTime, 0.016f);
                    Vector3 tgt = new Vector3(w.x, cinemaHeight, w.z);
                    cinematicCamera.transform.position = Vector3.MoveTowards(
                        cinematicCamera.transform.position, tgt,
                        cinematicMoveSpeed * dt2 * tilesPerFrame               // Vitesse adaptée au nombre de tiles traitées par frame
                    );
                }

                counter++;
                if (counter >= tilesPerFrame) { counter = 0; yield return null; } // Cède le contrôle à Unity pour éviter le freeze
            }
        }
        Debug.Log($"BuildGeometry: {sc} objets.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TRANSITION CAMÉRA — interpole position et rotation de la caméra
    // cinématique vers la caméra joueur avec une courbe ease-out cubique
    // ═════════════════════════════════════════════════════════════════════════
    IEnumerator ReturnCameraToPlayer()
    {
        if (cinematicCamera == null || player == null) yield break;

        Vector3 startPos = cinematicCamera.transform.position;
        Quaternion startRot = cinematicCamera.transform.rotation;

        Camera playerCam = player.GetComponentInChildren<Camera>();
        Vector3 endPos = playerCam != null ? playerCam.transform.position
                                              : player.transform.position + Vector3.up * 1.6f;
        Quaternion endRot = playerCam != null ? playerCam.transform.rotation
                                              : player.transform.rotation;

        float duration = Mathf.Max(0.01f, returnDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f); // Courbe ease-out cubique : démarre vite, ralentit à l'arrivée
            cinematicCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        cinematicCamera.transform.position = endPos;                               // Snap final pour éviter les imprécisions
        cinematicCamera.transform.rotation = endRot;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GÉNÉRATION DES PIÈCES — place des rectangles aléatoires sur la grille
    // Chaque pièce est vérifiée pour ne pas chevaucher les pièces existantes
    // avec une marge de sécurité de 2 cases
    // ═════════════════════════════════════════════════════════════════════════
    void GenerateRooms()
    {
        smallRoomMin = Mathf.Max(2, smallRoomMin);
        smallRoomMax = Mathf.Max(smallRoomMin, smallRoomMax);
        largeRoomMin = Mathf.Max(2, largeRoomMin);
        largeRoomMax = Mathf.Max(largeRoomMin, largeRoomMax);

        int attempts = 0, maxAttempts = roomCount * 15;                            // Limite le nombre de tentatives pour éviter une boucle infinie
        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;
            bool isLarge = Random.value < largeRoomRatio;                          // Détermine aléatoirement si c'est une grande ou petite pièce
            int minW = isLarge ? largeRoomMin : smallRoomMin;
            int maxW = isLarge ? largeRoomMax : smallRoomMax;

            int w = (minW == maxW) ? minW : Random.Range(minW, maxW + 1);          // Largeur aléatoire dans les limites
            int h = (minW == maxW) ? minW : Random.Range(minW, maxW + 1);          // Hauteur aléatoire dans les limites

            int maxX = mapWidth - w - 1;
            int maxY = mapHeight - h - 1;
            if (maxX < 2 || maxY < 2) continue;                                   // Pas assez de place — on ré-essaie

            int x = Random.Range(2, maxX);
            int y = Random.Range(2, maxY);

            RectInt newRoom = new RectInt(x, y, w, h);
            if (!OverlapsExistingRoom(newRoom, 2))                                 // Vérifie l'absence de chevauchement avec une marge de 2 cases
            {
                roomIsLarge[rooms.Count] = isLarge;                                // Enregistre si la pièce est grande pour les portes et le spawn
                rooms.Add(newRoom);
                CarveRoom(newRoom);                                                 // Marque les cases de la grille comme Floor
            }
        }

        if (rooms.Count < roomCount)
            Debug.LogWarning($"MapGenerator: seulement {rooms.Count}/{roomCount} pièces générées."); // Map trop petite ou paramètres trop grands
    }

    bool OverlapsExistingRoom(RectInt room, int margin)
    {
        foreach (RectInt e in rooms)
        {
            RectInt ex = new RectInt(e.x - margin, e.y - margin, e.width + margin * 2, e.height + margin * 2);
            if (room.Overlaps(ex)) return true;                                    // Chevauchement détecté — la pièce ne peut pas être placée ici
        }
        return false;
    }

    void CarveRoom(RectInt room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
            for (int y = room.y; y < room.y + room.height; y++)
                if (IsInBounds(x, y)) grid[x, y] = TileType.Floor;                // Marque chaque case de la pièce comme sol
    }

    // ── COULOIRS ─────────────────────────────────────────────────────────────
    void ConnectRooms()
    {
        if (rooms.Count < 2) return;                                               // Besoin d'au moins deux pièces pour créer un couloir
        for (int i = 0; i < rooms.Count - 1; i++)
            CarveCorridor(GetRoomCenter(rooms[i]), GetRoomCenter(rooms[i + 1]));   // Relie chaque pièce à la suivante dans l'ordre de génération
    }

    Vector2Int GetRoomCenter(RectInt r) => new Vector2Int(r.x + r.width / 2, r.y + r.height / 2); // Centre d'une pièce en coordonnées de grille

    void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        if (Random.value > 0.5f) { CarveH(a.x, b.x, a.y); CarveV(a.y, b.y, b.x); } // Couloir en L : d'abord horizontal puis vertical
        else { CarveV(a.y, b.y, a.x); CarveH(a.x, b.x, b.y); } // Ou d'abord vertical puis horizontal — aléatoire pour varier
    }

    void CarveH(int x0, int x1, int y)
    {
        for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            if (IsInBounds(x, y) && grid[x, y] == TileType.Empty)
                grid[x, y] = TileType.Corridor;                                    // Ne remplace que les cases vides — préserve les sols existants
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
                    dt.Add(new Vector2Int(x, y));                                  // Une tile de porte = case de couloir adjacente à une pièce
        return dt;
    }

    // ── PLACEMENT DES MURS & PORTES ───────────────────────────────────────────
    void SpawnWall(int x, int y, float angle, Transform parent, float yOff = 0f)
    {
        if (wallLongPrefab == null) return;
        SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + AngleToOffset(angle, tileSize / 2f), angle, parent, wallScale, wallBaseRot, yOff);
    }                                                                               // Place un mur plein sur la face indiquée par l'angle

    void SpawnWallOrDoor(int x, int y, float angle, Transform parent, HashSet<Vector2Int> dt, float yOff)
    {
        Vector3 wp = GridToWorld(x, y) + AngleToOffset(angle, tileSize / 2f);
        int nx = x, ny = y;
        if (angle == 0f) ny++;                                              // Calcule les coordonnées de la case voisine selon l'angle
        else if (angle == 180f) ny--;
        else if (angle == 90f) nx++;
        else if (angle == 270f) nx--;

        bool isDoor = (grid[x, y] == TileType.Floor && dt.Contains(new Vector2Int(nx, ny))) ||
                      (grid[x, y] == TileType.Corridor && dt.Contains(new Vector2Int(x, y)) && IsFloor(nx, ny));
        // Une porte est placée quand un sol touche une tile de couloir marquée
        if (isDoor)
        {
            int rx = IsFloor(x, y) ? x : nx;
            int ry = IsFloor(x, y) ? y : ny;
            bool isLarge = IsLargeRoom(rx, ry);                                    // Grande pièce → double porte, petite pièce → porte simple
            bool isGlass = Random.value < glassVsWoodChance;                       // Tirage au sort bois vs verre
            GameObject pref;
            if (isLarge) pref = isGlass ? (wallDoubleDoorGlassPrefab ?? wallLongPrefab) : (wallDoubleDoorWoodPrefab ?? wallLongPrefab);
            else pref = isGlass ? (wallDoorGlassPrefab ?? wallLongPrefab) : (wallDoorWoodPrefab ?? wallLongPrefab);
            SpawnPrefab(pref, wp, angle, parent, wallScale, wallBaseRot, yOff);    // Fallback sur mur plein si le prefab de porte n'est pas assigné
        }
        else SpawnPrefab(wallLongPrefab, wp, angle, parent, wallScale, wallBaseRot, yOff);
    }

    Vector3 AngleToOffset(float angle, float half) => angle switch
    {
        0f => new Vector3(0, 0, half),                                          // Nord
        180f => new Vector3(0, 0, -half),                                          // Sud
        90f => new Vector3(half, 0, 0),                                          // Est
        270f => new Vector3(-half, 0, 0),                                          // Ouest
        _ => Vector3.zero
    };

    void SpawnCorridorSideWalls(int x, int y, Transform parent, float yOff)
    {
        bool h = IsCorridorOrFloor(x + 1, y) || IsCorridorOrFloor(x - 1, y);          // Le couloir est orienté horizontalement
        bool v = IsCorridorOrFloor(x, y + 1) || IsCorridorOrFloor(x, y - 1);          // Le couloir est orienté verticalement
        float s = tileSize / 2f;
        if (h && !IsCorridorOrFloor(x, y + 1)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(0, 0, s), 0f, parent, wallScale, wallBaseRot, yOff);
        if (h && !IsCorridorOrFloor(x, y - 1)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(0, 0, -s), 180f, parent, wallScale, wallBaseRot, yOff);
        if (v && !IsCorridorOrFloor(x + 1, y)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(s, 0, 0), 90f, parent, wallScale, wallBaseRot, yOff);
        if (v && !IsCorridorOrFloor(x - 1, y)) SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(-s, 0, 0), 270f, parent, wallScale, wallBaseRot, yOff);
    }                                                                               // Place des murs sur les côtés ouverts du couloir selon son orientation

    // ── SPAWN ENNEMIS (FALLBACK) ──────────────────────────────────────────────
    void SpawnEnemies()
    {
        if (zombiePrefab == null) { Debug.LogWarning("MapGenerator: aucun prefab zombie !"); return; }
        if (rooms.Count == 0) return;

        var ep = new GameObject("Enemies");
        ep.transform.parent = mapParent.transform;

        for (int i = 1; i < rooms.Count; i++)                                     // Commence à 1 — la pièce 0 est réservée au spawn du joueur
        {
            RectInt r = rooms[i];
            bool lg = roomIsLarge.ContainsKey(i) && roomIsLarge[i];
            int cnt = Mathf.Max(0, lg ? zombiesPerLargeRoom : zombiesPerSmallRoom);

            for (int z = 0; z < cnt; z++)
            {
                int rx, ry;
                if (r.width <= 2 || r.height <= 2) { rx = r.x + r.width / 2; ry = r.y + r.height / 2; }
                else { rx = Random.Range(r.x + 1, r.x + r.width - 1); ry = Random.Range(r.y + 1, r.y + r.height - 1); } // Position aléatoire à l'intérieur de la pièce
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
        return false;                                                               // Évite de placer des décos trop près des entrées de couloir
    }

    bool IsTileAgainstWall(int x, int y) =>
        IsEmpty(x + 1, y) || IsEmpty(x - 1, y) || IsEmpty(x, y + 1) || IsEmpty(x, y - 1);   // Vrai si la tile touche au moins un mur

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
        var usedTiles = new HashSet<Vector2Int>();                                  // Évite de placer deux objets sur la même case
        int total = 0;

        for (int i = 1; i < rooms.Count; i++)                                     // Commence à 1 — pas de déco dans la pièce de spawn joueur
        {
            RectInt room = rooms[i];
            if (room.width <= 2 || room.height <= 2) continue;                    // Trop petite pour accueillir de la déco

            var candidates = new List<Vector2Int>();
            for (int x = room.x + 1; x < room.x + room.width - 1; x++)
                for (int y = room.y + 1; y < room.y + room.height - 1; y++)
                {
                    var tile = new Vector2Int(x, y);
                    if (IsTileAgainstWall(x, y) && !IsTileNearCorridor(x, y, 2) && !usedTiles.Contains(tile))
                        candidates.Add(tile);                                       // Priorité aux tiles contre les murs, loin des couloirs
                }

            if (candidates.Count < decoPerRoom)                                    // Si pas assez de candidats idéaux, élargit la sélection
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

        Vector2Int c = GetRoomCenter(rooms[0]);                                    // Toujours la première pièce générée — la plus sûre (sans zombies)
        Vector3 p = new Vector3(c.x * tileSize, floorYOffset + playerSpawnYOffset, c.y * tileSize);

        CharacterController cc = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (cc != null) cc.enabled = false;                                        // Désactive le CharacterController avant de téléporter
        if (rb != null) rb.isKinematic = true;                                     // Désactive la physique pour le téléportation
        player.transform.SetPositionAndRotation(p, Quaternion.identity);
        if (cc != null) cc.enabled = true;                                         // Réactive après la téléportation
        if (rb != null) rb.isKinematic = false;

        Debug.Log("Joueur repositionné à : " + p);
    }

    // ── NAVMESH ───────────────────────────────────────────────────────────────
    IEnumerator BakeNavMeshIfPresent()
    {
        NavMeshBaker b = GetComponent<NavMeshBaker>();
        if (b != null)
        {
            b.navMeshCheckPoint = new Vector3((mapWidth / 2f) * tileSize, floorYOffset, (mapHeight / 2f) * tileSize); // Centre de la map — point de vérification NavMesh
            yield return StartCoroutine(b.BakeNavMeshRoutine());                   // Bake asynchrone pour ne pas bloquer la génération
        }
        else { Debug.LogWarning("MapGenerator: NavMeshBaker non trouvé !"); yield break; }
    }

    // ── UTILITAIRES ───────────────────────────────────────────────────────────
    bool IsLargeRoom(int x, int y)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i].Contains(new Vector2Int(x, y)))
                return roomIsLarge.ContainsKey(i) && roomIsLarge[i];               // Retourne true si la case appartient à une grande pièce
        return false;
    }

    public Vector3 GetMapCenter() =>
        new Vector3((mapWidth / 2f) * tileSize, 0f, (mapHeight / 2f) * tileSize);         // Centre géométrique de la grille en coordonnées monde

    Transform Child(string name)
    {
        var g = new GameObject(name);
        g.transform.parent = mapParent.transform;
        return g.transform;                                                         // Crée un conteneur enfant nommé dans la hiérarchie Unity
    }

    void SpawnPrefab(GameObject prefab, Vector3 pos, float angleY, Transform parent, Vector3 scale, Vector3 baseRot, float yOffset)
    {
        if (prefab == null) return;
        if (parent == null) { Debug.LogWarning("MapGenerator: parent null dans SpawnPrefab, ignoré."); return; }
        pos.y += yOffset;                                                           // Applique le décalage vertical avant d'instancier
        var o = Instantiate(prefab, pos, Quaternion.Euler(baseRot.x, baseRot.y + angleY, baseRot.z), parent);
        o.transform.localScale = scale;                                             // Applique l'échelle définie dans l'Inspector
    }

    Vector3 GridToWorld(int x, int y) => new Vector3(x * tileSize, 0, y * tileSize);         // Convertit des coordonnées de grille en position 3D monde
    bool IsEmpty(int x, int y) => !IsInBounds(x, y) || grid[x, y] == TileType.Empty; // Hors limites = traité comme vide
    bool IsFloor(int x, int y) => IsInBounds(x, y) && grid[x, y] == TileType.Floor;
    bool IsCorridor(int x, int y) => IsInBounds(x, y) && grid[x, y] == TileType.Corridor;
    bool IsCorridorOrFloor(int x, int y) => IsInBounds(x, y) && (grid[x, y] == TileType.Floor || grid[x, y] == TileType.Corridor);
    bool IsInBounds(int x, int y) => x >= 0 && x < mapWidth && y >= 0 && y < mapHeight; // Vérifie que la case existe dans la grille

    // ── GIZMOS ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (grid == null) return;
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (grid[x, y] == TileType.Floor) Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Vert = pièce
                else if (grid[x, y] == TileType.Corridor) Gizmos.color = new Color(0.2f, 0.4f, 0.8f, 0.5f); // Bleu = couloir
                else continue;
                Gizmos.DrawCube(GridToWorld(x, y), new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f));      // Visualisation de la grille dans la Scene View
            }
        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2Int c = GetRoomCenter(rooms[i]);
            Gizmos.color = (roomIsLarge.ContainsKey(i) && roomIsLarge[i]) ? Color.red : Color.yellow;         // Rouge = grande pièce, jaune = petite pièce
            Gizmos.DrawSphere(GridToWorld(c.x, c.y) + Vector3.up, 0.5f);                                      // Sphère au centre de chaque pièce
        }
    }
}