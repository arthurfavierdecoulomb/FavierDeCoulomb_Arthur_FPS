using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Prefabs - Sol & Plafond")]
    [SerializeField] private GameObject floorPrefab;

    [Header("Prefabs - Murs")]
    [SerializeField] private GameObject wallLongPrefab;
    [SerializeField] private GameObject wallDoorFramePrefab;
    [SerializeField] private GameObject wallDoubleDoorFramePrefab;

    [Header("Prefabs - Portes")]
    [SerializeField] private GameObject doorWoodPrefab;
    [SerializeField] private GameObject doorGlassPrefab;

    [Header("Prefabs - Angles")]
    [SerializeField] private GameObject cornerLargePrefab;

    [Header("Échelle des prefabs")]
    [SerializeField] private Vector3 floorScale = Vector3.one;
    [SerializeField] private Vector3 wallScale = Vector3.one;
    [SerializeField] private Vector3 doorScale = Vector3.one;
    [SerializeField] private Vector3 cornerScale = Vector3.one;

    [Header("Rotation de base des prefabs")]
    [SerializeField] private Vector3 floorBaseRot = Vector3.zero;
    [SerializeField] private Vector3 wallBaseRot = Vector3.zero;
    [SerializeField] private Vector3 doorBaseRot = Vector3.zero;
    [SerializeField] private Vector3 cornerBaseRot = Vector3.zero;

    [Header("Offset Y des éléments")]
    [SerializeField] private float floorYOffset = 0f;
    [SerializeField] private float wallYOffset = 0f;   // Y du premier niveau de mur
    [SerializeField] private float wall2YOffset = 4f;   // Y du deuxième niveau de mur (étage)
    [SerializeField] private float ceilingYOffset = 8f;   // Y du plafond (= 2x hauteur mur)
    [SerializeField] private float doorYOffset = 0f;
    [SerializeField] private float cornerYOffset = 0f;
    [SerializeField] private float corner2YOffset = 4f;   // Coins du deuxième niveau

    [Header("Offset porte dans l'interstice")]
    [SerializeField] private Vector3 doorOffset = Vector3.zero;

    [Header("Paramètres de génération")]
    [SerializeField] private int roomCount = 8;
    [SerializeField] private float tileSize = 4f;
    [SerializeField] private int mapWidth = 50;
    [SerializeField] private int mapHeight = 50;
    [SerializeField] private int seed = 0;

    [Header("Tailles des pièces")]
    [SerializeField] private int smallRoomMin = 2;
    [SerializeField] private int smallRoomMax = 4;
    [SerializeField] private int largeRoomMin = 5;
    [SerializeField] private int largeRoomMax = 8;
    [SerializeField][Range(0f, 1f)] private float largeRoomRatio = 0.4f;

    [Header("Options - Portes")]
    [SerializeField][Range(0f, 1f)] private float glassVsWoodChance = 0.4f;

    [Header("Joueur")]
    [SerializeField] private Transform playerTransform; // Joueur déjà dans la scène
    [SerializeField] private GameObject playerPrefab;   // OU prefab à instancier
    [SerializeField] private float playerSpawnYOffset = 1f; // Hauteur au-dessus du sol

    private enum TileType { Empty, Floor, Corridor }
    private TileType[,] grid;
    private Dictionary<int, bool> roomIsLarge = new Dictionary<int, bool>();
    private GameObject mapParent;
    private List<RectInt> rooms = new List<RectInt>();

    void Start() => GenerateMap();

    public void GenerateMap()
    {
        if (seed != 0) Random.InitState(seed);
        else Random.InitState(System.DateTime.Now.Millisecond);

        if (mapParent != null) Destroy(mapParent);
        mapParent = new GameObject("=== MAP GENEREE ===");
        rooms.Clear();
        roomIsLarge.Clear();

        grid = new TileType[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                grid[x, y] = TileType.Empty;

        GenerateRooms();
        ConnectRooms();
        BuildGeometry();
        SpawnPlayer();
        Debug.Log("Map générée ! " + rooms.Count + " pièces.");
    }

    // =============================================
    // PIÈCES
    // =============================================
    void GenerateRooms()
    {
        int attempts = 0, maxAttempts = roomCount * 15;
        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;
            bool isLarge = Random.value < largeRoomRatio;
            int w = Random.Range(isLarge ? largeRoomMin : smallRoomMin,
                                 (isLarge ? largeRoomMax : smallRoomMax) + 1);
            int h = Random.Range(isLarge ? largeRoomMin : smallRoomMin,
                                 (isLarge ? largeRoomMax : smallRoomMax) + 1);
            // Clamp strict pour éviter tout dépassement de grille
            int maxX = mapWidth - w - 1;
            int maxY = mapHeight - h - 1;
            if (maxX < 2 || maxY < 2) continue; // pièce trop grande pour la grille

            int x = Random.Range(2, maxX);
            int y = Random.Range(2, maxY);
            RectInt newRoom = new RectInt(x, y, w, h);

            if (!OverlapsExistingRoom(newRoom, 2))
            {
                roomIsLarge[rooms.Count] = isLarge;
                rooms.Add(newRoom);
                CarveRoom(newRoom);
            }
        }
    }

    bool OverlapsExistingRoom(RectInt room, int margin)
    {
        foreach (RectInt existing in rooms)
        {
            RectInt expanded = new RectInt(
                existing.x - margin, existing.y - margin,
                existing.width + margin * 2, existing.height + margin * 2);
            if (room.Overlaps(expanded)) return true;
        }
        return false;
    }

    void CarveRoom(RectInt room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
            for (int y = room.y; y < room.y + room.height; y++)
                if (IsInBounds(x, y)) // Guard anti-crash
                    grid[x, y] = TileType.Floor;
    }

    // =============================================
    // COULOIRS
    // =============================================
    void ConnectRooms()
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2Int a = GetRoomCenter(rooms[i]);
            Vector2Int b = GetRoomCenter(rooms[i + 1]);
            CarveCorridor(a, b);
        }
    }

    Vector2Int GetRoomCenter(RectInt room) =>
        new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);

    void CarveCorridor(Vector2Int from, Vector2Int to)
    {
        if (Random.value > 0.5f)
        {
            CarveH(from.x, to.x, from.y);
            CarveV(from.y, to.y, to.x);
        }
        else
        {
            CarveV(from.y, to.y, from.x);
            CarveH(from.x, to.x, to.y);
        }
    }

    void CarveH(int xFrom, int xTo, int y)
    {
        for (int x = Mathf.Min(xFrom, xTo); x <= Mathf.Max(xFrom, xTo); x++)
            if (grid[x, y] == TileType.Empty) grid[x, y] = TileType.Corridor;
    }

    void CarveV(int yFrom, int yTo, int x)
    {
        for (int y = Mathf.Min(yFrom, yTo); y <= Mathf.Max(yFrom, yTo); y++)
            if (grid[x, y] == TileType.Empty) grid[x, y] = TileType.Corridor;
    }

    // =============================================
    // CONSTRUCTION 3D
    // =============================================
    void BuildGeometry()
    {
        var floorParent = Child("Floors");
        var wallParent = Child("Walls");
        var cornerParent = Child("Corners");

        // Jonctions pièce ↔ couloir = tiles qui auront une porte
        HashSet<Vector2Int> doorTiles = ComputeDoorTiles();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileType tile = grid[x, y];
                if (tile == TileType.Empty) continue;

                Vector3 wp = GridToWorld(x, y);

                // SOL
                SpawnPrefab(floorPrefab, wp, 0f, floorParent, floorScale, floorBaseRot, floorYOffset);

                // PLAFOND (Floor retourné, hauteur double)
                var ceilRot = new Vector3(floorBaseRot.x + 180f, floorBaseRot.y, floorBaseRot.z);
                SpawnRaw(floorPrefab, wp, ceilRot, floorParent, floorScale, ceilingYOffset);

                // MURS EXTÉRIEURS — niveau 1 : porte ou mur, niveau 2 : toujours mur plein
                if (IsEmpty(x, y + 1)) { SpawnWallOrDoor(x, y, 0f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 0f, wallParent, wall2YOffset); }
                if (IsEmpty(x, y - 1)) { SpawnWallOrDoor(x, y, 180f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 180f, wallParent, wall2YOffset); }
                if (IsEmpty(x + 1, y)) { SpawnWallOrDoor(x, y, 90f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 90f, wallParent, wall2YOffset); }
                if (IsEmpty(x - 1, y)) { SpawnWallOrDoor(x, y, 270f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 270f, wallParent, wall2YOffset); }

                // PORTES : jonctions pièce ↔ couloir (ces murs ne sont PAS sur un bord Empty !)
                if (tile == TileType.Floor)
                {
                    if (IsCorridor(x, y + 1)) { SpawnWallOrDoor(x, y, 0f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 0f, wallParent, wall2YOffset); }
                    if (IsCorridor(x, y - 1)) { SpawnWallOrDoor(x, y, 180f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 180f, wallParent, wall2YOffset); }
                    if (IsCorridor(x + 1, y)) { SpawnWallOrDoor(x, y, 90f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 90f, wallParent, wall2YOffset); }
                    if (IsCorridor(x - 1, y)) { SpawnWallOrDoor(x, y, 270f, wallParent, doorTiles, wallYOffset); SpawnWall(x, y, 270f, wallParent, wall2YOffset); }
                }

                // MURS LATÉRAUX COULOIR (tunnel)
                if (tile == TileType.Corridor)
                {
                    SpawnCorridorSideWalls(x, y, wallParent, wallYOffset);
                    SpawnCorridorSideWalls(x, y, wallParent, wall2YOffset);
                }
            }
        }

        // COINS — deux niveaux
        SpawnCorners(cornerParent, cornerYOffset);
        SpawnCorners(cornerParent, corner2YOffset);
    }

    // Tiles COULOIR adjacents à une pièce → jonction = porte
    HashSet<Vector2Int> ComputeDoorTiles()
    {
        var doorTiles = new HashSet<Vector2Int>();
        int floorCount = 0, corridorCount = 0;

        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (grid[x, y] == TileType.Floor) floorCount++;
                if (grid[x, y] == TileType.Corridor) corridorCount++;
                if (grid[x, y] == TileType.Corridor)
                    if (IsFloor(x + 1, y) || IsFloor(x - 1, y) || IsFloor(x, y + 1) || IsFloor(x, y - 1))
                        doorTiles.Add(new Vector2Int(x, y));
            }

        Debug.Log($"=== DIAGNOSTIC === Floor:{floorCount} Corridor:{corridorCount} DoorTiles:{doorTiles.Count}");
        Debug.Log($"wallDoorFrame:{wallDoorFramePrefab != null} | doorWood:{doorWoodPrefab != null} | doorGlass:{doorGlassPrefab != null}");

        return doorTiles;
    }

    // Mur plein (niveau 2 ou couloir)
    void SpawnWall(int x, int y, float angle, Transform parent, float wallYOffset = 0f)
    {
        float h = tileSize / 2f;
        Vector3 offset = angle switch
        {
            0f => new Vector3(0, 0, h),
            180f => new Vector3(0, 0, -h),
            90f => new Vector3(h, 0, 0),
            270f => new Vector3(-h, 0, 0),
            _ => Vector3.zero
        };
        SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + offset, angle, parent, wallScale, wallBaseRot, wallYOffset);
    }

    // Mur niveau 1 : plein OU porte si jonction couloir/pièce
    void SpawnWallOrDoor(int x, int y, float angle, Transform parent,
        HashSet<Vector2Int> doorTiles, float yOff)
    {
        float h = tileSize / 2f;
        Vector3 offset = angle switch
        {
            0f => new Vector3(0, 0, h),
            180f => new Vector3(0, 0, -h),
            90f => new Vector3(h, 0, 0),
            270f => new Vector3(-h, 0, 0),
            _ => Vector3.zero
        };
        Vector3 wp = GridToWorld(x, y) + offset;

        // Le tile voisin dans la direction du mur
        int nx = x, ny = y;
        if (angle == 0f) ny += 1;
        else if (angle == 180f) ny -= 1;
        else if (angle == 90f) nx += 1;
        else if (angle == 270f) nx -= 1;

        // Porte si :
        // Ce tile est couloir adjacent à pièce, et le mur pointe vers la pièce
        // OU ce tile est pièce, et le mur pointe vers un couloir marqué doorTile
        bool isDoor =
            (grid[x, y] == TileType.Floor && doorTiles.Contains(new Vector2Int(nx, ny))) ||
            (grid[x, y] == TileType.Corridor && doorTiles.Contains(new Vector2Int(x, y)) && IsFloor(nx, ny));

        if (isDoor)
        {
            // On prend les coordonnées de la pièce pour savoir si grande ou petite
            int roomX = IsFloor(x, y) ? x : nx;
            int roomY = IsFloor(x, y) ? y : ny;
            bool isLarge = IsLargeRoom(roomX, roomY);
            bool isGlass = Random.value < glassVsWoodChance;
            GameObject dp = isGlass && doorGlassPrefab != null ? doorGlassPrefab : doorWoodPrefab;

            if (isLarge)
            {
                SpawnPrefab(wallDoubleDoorFramePrefab != null ? wallDoubleDoorFramePrefab : wallLongPrefab,
                    wp, angle, parent, wallScale, wallBaseRot, yOff);
                Vector3 dpos = wp + doorOffset;
                SpawnPrefab(dp, dpos, angle, parent, doorScale, doorBaseRot, doorYOffset);
                SpawnPrefab(dp, dpos, angle - 180f, parent, doorScale, doorBaseRot, doorYOffset);
            }
            else
            {
                SpawnPrefab(wallDoorFramePrefab != null ? wallDoorFramePrefab : wallLongPrefab,
                    wp, angle, parent, wallScale, wallBaseRot, yOff);
                SpawnPrefab(dp, wp + doorOffset, angle, parent, doorScale, doorBaseRot, doorYOffset);
            }
        }
        else
        {
            SpawnPrefab(wallLongPrefab, wp, angle, parent, wallScale, wallBaseRot, yOff);
        }
    }

    void SpawnCorridorSideWalls(int x, int y, Transform parent, float yOff)
    {
        bool horizontal = IsCorridorOrFloor(x + 1, y) || IsCorridorOrFloor(x - 1, y);
        bool vertical = IsCorridorOrFloor(x, y + 1) || IsCorridorOrFloor(x, y - 1);
        float h = tileSize / 2f;

        if (horizontal && !IsCorridorOrFloor(x, y + 1))
            SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(0, 0, h), 0f, parent, wallScale, wallBaseRot, yOff);
        if (horizontal && !IsCorridorOrFloor(x, y - 1))
            SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(0, 0, -h), 180f, parent, wallScale, wallBaseRot, yOff);
        if (vertical && !IsCorridorOrFloor(x + 1, y))
            SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(h, 0, 0), 90f, parent, wallScale, wallBaseRot, yOff);
        if (vertical && !IsCorridorOrFloor(x - 1, y))
            SpawnPrefab(wallLongPrefab, GridToWorld(x, y) + new Vector3(-h, 0, 0), 270f, parent, wallScale, wallBaseRot, yOff);
    }

    void SpawnCorners(Transform parent, float yOff)
    {
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (grid[x, y] == TileType.Empty) continue;
                TrySpawnCorner(x, y, 1, 1, 0f, parent, yOff);
                TrySpawnCorner(x, y, -1, 1, 90f, parent, yOff);
                TrySpawnCorner(x, y, 1, -1, 270f, parent, yOff);
                TrySpawnCorner(x, y, -1, -1, 180f, parent, yOff);
            }
    }

    void TrySpawnCorner(int x, int y, int dx, int dz, float angle, Transform parent, float yOff)
    {
        if (!IsInBounds(x + dx, y + dz)) return;
        if (!IsEmpty(x + dx, y + dz)) return;
        if (!IsEmpty(x + dx, y) || !IsEmpty(x, y + dz)) return;
        float h = tileSize / 2f;
        Vector3 pos = GridToWorld(x, y) + new Vector3(dx * h, 0, dz * h);
        SpawnPrefab(cornerLargePrefab, pos, angle, parent, cornerScale, cornerBaseRot, yOff);
    }

    // =============================================
    // SPAWN JOUEUR
    // =============================================
    void SpawnPlayer()
    {
        if (rooms.Count == 0)
        {
            Debug.LogWarning("Aucune pièce générée, impossible de spawner le joueur !");
            return;
        }

        // Centre de la première pièce générée
        Vector2Int center = GetRoomCenter(rooms[0]);
        Vector3 spawnPos = GridToWorld(center.x, center.y);
        spawnPos.y = floorYOffset + playerSpawnYOffset;

        if (playerTransform != null)
        {
            // Joueur déjà dans la scène → repositionnement
            playerTransform.position = spawnPos;
            Debug.Log("Joueur repositionné dans la pièce 0 : " + spawnPos);
        }
        else if (playerPrefab != null)
        {
            // Prefab → instanciation
            Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            Debug.Log("Joueur instancié dans la pièce 0 : " + spawnPos);
        }
        else
        {
            Debug.LogWarning("Aucun joueur assigné dans MapGenerator ! Assigne Player Transform ou Player Prefab.");
        }
    }

    // =============================================
    // UTILITAIRES
    // =============================================
    bool IsLargeRoom(int x, int y)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i].Contains(new Vector2Int(x, y)))
                return roomIsLarge.ContainsKey(i) && roomIsLarge[i];
        return false;
    }

    Transform Child(string name)
    {
        var go = new GameObject(name);
        go.transform.parent = mapParent.transform;
        return go.transform;
    }

    void SpawnPrefab(GameObject prefab, Vector3 pos, float angleY, Transform parent,
        Vector3 scale, Vector3 baseRot, float yOffset)
    {
        if (prefab == null) return;
        pos.y += yOffset;
        var obj = Instantiate(prefab, pos, Quaternion.Euler(baseRot.x, baseRot.y + angleY, baseRot.z), parent);
        obj.transform.localScale = scale;
    }

    void SpawnRaw(GameObject prefab, Vector3 pos, Vector3 euler, Transform parent, Vector3 scale, float yOffset)
    {
        if (prefab == null) return;
        pos.y += yOffset;
        var obj = Instantiate(prefab, pos, Quaternion.Euler(euler), parent);
        obj.transform.localScale = scale;
    }

    Vector3 GridToWorld(int x, int y) => new Vector3(x * tileSize, 0, y * tileSize);

    bool IsEmpty(int x, int y) => !IsInBounds(x, y) || grid[x, y] == TileType.Empty;
    bool IsFloor(int x, int y) => IsInBounds(x, y) && grid[x, y] == TileType.Floor;
    bool IsCorridor(int x, int y) => IsInBounds(x, y) && grid[x, y] == TileType.Corridor;
    bool IsCorridorOrFloor(int x, int y) => IsInBounds(x, y) && (grid[x, y] == TileType.Floor || grid[x, y] == TileType.Corridor);
    bool IsInBounds(int x, int y) => x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;

    // =============================================
    // GIZMOS
    // =============================================
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